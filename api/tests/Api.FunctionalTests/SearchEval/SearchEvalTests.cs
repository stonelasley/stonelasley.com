using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace StoneLasley.Api.FunctionalTests.SearchEval;

/// <summary>
/// Measures search quality on a frozen corpus (SearchEval/corpus) against the real ci Vectorize index: every graded
/// query in queries.json runs through /search in each mode, and the gate compares hybrid with both legs and with the
/// committed scores.json. Explicit, so it runs only in the search-eval workflow:
/// <c>dotnet test api/tests/Api.FunctionalTests --filter TestCategory=SearchEval</c>.
/// Set SL_EVAL_WRITE_SCORES=1 to rewrite the committed scores from this run.
/// </summary>
[Explicit("Slow and needs Cloudflare; run by the search-eval workflow.")]
[Category("SearchEval")]
public class SearchEvalTests
{
    private static readonly string[] Modes = ["hybrid", "keyword", "vector"];

    // Vectorize writes become queryable over minutes; past this the run is an infra failure, not a quality one.
    private static readonly TimeSpan SeedingBudget = TimeSpan.FromMinutes(10);

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    private ApiFactory? _api;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task SeedCorpusAsync()
    {
        Assume.That(WranglerDev.FakeVectors, Is.False, "The eval measures the real embedding model.");
        _api = new ApiFactory().WithRealVectors();
        _client = _api.CreateIngestClient();
        try
        {
            await Ingest.RunAsync(_client, Corpus.Markdowns().ToArray());
            await WaitForVectorsAsync();
        }
        catch (SeedingFailed e)
        {
            Report.Status("infra", e.Message);
            throw new InvalidOperationException($"INFRA: {e.Message}", e);
        }
    }

    [OneTimeTearDown]
    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_api is not null)
        {
            await _api.DisposeAsync();
        }
    }

    [Test]
    public async Task Search_quality_meets_the_gate()
    {
        var queries = Corpus.Queries();
        var results = new Dictionary<string, Dictionary<EvalQuery, IReadOnlyList<string>>>();
        foreach (var mode in Modes)
        {
            results[mode] = [];
            foreach (var query in queries)
            {
                results[mode][query] = await SearchAsync(mode, query.Q);
            }
        }

        var scores = Score(queries, results);
        var committed = Corpus.CommittedScores();
        var failures = EvalGate.Failures(scores, committed);
        Report.Write(scores, committed, NoAnswerHits(queries, results), failures);
        if (Environment.GetEnvironmentVariable("SL_EVAL_WRITE_SCORES") == "1")
        {
            Corpus.WriteScores(JsonSerializer.Serialize(scores, Json));
        }

        failures.ShouldBeEmpty(string.Join("\n", failures));
    }

    private async Task<IReadOnlyList<string>> SearchAsync(string mode, string query)
    {
        var response = await _client.GetFromJsonAsync<SearchResponse>($"/search?mode={mode}&q={Uri.EscapeDataString(query)}");
        return [.. response!.Results.Select(r => r.Slug)];
    }

    // Waits until every chunk is stored, then until two passes of the vector queries agree: Vectorize replicas can
    // disagree just after a write, and a ranking measured mid-convergence would be noise.
    private async Task WaitForVectorsAsync()
    {
        var deadline = DateTime.UtcNow + SeedingBudget;
        using var worker = Testing.WorkerClient();
        var chunks = await worker.PostAsJsonAsync("/d1/query", new { sql = "SELECT id, slug, title, vector_chunks FROM posts WHERE vector_chunks > 0" });
        var embedded = (await chunks.Content.ReadFromJsonAsync<ChunkRows>())!.Results;
        var expected = embedded
            .SelectMany(row => Enumerable.Range(0, row.Vector_chunks).Select(i => $"{row.Id}:{i}"))
            .ToHashSet();
        if (expected.Count == 0)
        {
            throw new SeedingFailed("the ingest embedded no Posts, so the vector leg has nothing to search");
        }

        while (true)
        {
            var stored = new HashSet<string>();
            try
            {
                foreach (var batch in expected.Chunk(20))
                {
                    var response = await worker.PostAsJsonAsync("/vectors/get", new { ids = batch });
                    response.EnsureSuccessStatusCode();
                    stored.UnionWith((await response.Content.ReadFromJsonAsync<List<StoredVector>>())!.Select(v => v.Id));
                }
            }
            catch (HttpRequestException)
            {
                // The remote binding drops a request now and then; poll again.
            }
            if (stored.IsSupersetOf(expected))
            {
                break;
            }
            await PauseOrTimeOutAsync(deadline, $"only {stored.Count} of {expected.Count} vectors were stored within {SeedingBudget}");
        }

        // A stored vector can be fetched by id before queries see it. Each chunk starts with its Post's title, so a
        // Post is queryable once a vector search for its title finds it.
        var unseen = embedded.ToList();
        while (true)
        {
            foreach (var post in unseen.ToList())
            {
                try
                {
                    if ((await SearchAsync("vector", post.Title)).Contains(post.Slug))
                    {
                        unseen.Remove(post);
                    }
                }
                catch (HttpRequestException)
                {
                    // Poll again.
                }
            }
            if (unseen.Count == 0)
            {
                break;
            }
            await PauseOrTimeOutAsync(deadline, $"{unseen.Count} of {embedded.Count} Posts never became queryable within {SeedingBudget}");
        }

        // Replicas can still disagree for a moment; measure once two passes of the query set agree.
        var queries = Corpus.Queries();
        string? previous = null;
        while (true)
        {
            var pass = new StringBuilder();
            try
            {
                foreach (var query in queries)
                {
                    pass.AppendLine(string.Join(",", await SearchAsync("vector", query.Q)));
                }
            }
            catch (HttpRequestException)
            {
                pass.Clear().Append("failed");
            }
            if (pass.ToString() == previous && previous != "failed")
            {
                return;
            }
            previous = pass.ToString();
            await PauseOrTimeOutAsync(deadline, $"vector rankings did not settle within {SeedingBudget}");
        }
    }

    private static async Task PauseOrTimeOutAsync(DateTime deadline, string message)
    {
        if (DateTime.UtcNow > deadline)
        {
            throw new SeedingFailed(message);
        }
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    private static EvalScores Score(
        IReadOnlyList<EvalQuery> queries, Dictionary<string, Dictionary<EvalQuery, IReadOnlyList<string>>> results)
    {
        var graded = queries.Where(q => q.Type != "none").ToList();
        return new EvalScores(Modes.ToDictionary(
            mode => mode,
            mode => new ModeScores(
                Aggregate(graded, results[mode]),
                graded.GroupBy(q => q.Type).ToDictionary(g => g.Key, g => Aggregate([.. g], results[mode])))));
    }

    private static TypeScores Aggregate(IReadOnlyList<EvalQuery> queries, Dictionary<EvalQuery, IReadOnlyList<string>> ranked) => new(
        Round(queries.Average(q => EvalMetrics.NdcgAt(5, ranked[q], q.Judged!))),
        Round(queries.Average(q => EvalMetrics.RecallAt(10, ranked[q], q.Judged!))),
        Round(queries.Average(q => EvalMetrics.ReciprocalRank(ranked[q], q.Judged!))),
        queries.Count);

    private static Dictionary<string, int> NoAnswerHits(
        IReadOnlyList<EvalQuery> queries, Dictionary<string, Dictionary<EvalQuery, IReadOnlyList<string>>> results) =>
        Modes.ToDictionary(mode => mode, mode => queries.Count(q => q.Type == "none" && results[mode][q].Count > 0));

    private static double Round(double value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);

    private sealed class SeedingFailed(string message) : Exception(message);

    private sealed record ChunkRows(IReadOnlyList<ChunkRow> Results);

    private sealed record ChunkRow(string Id, string Slug, string Title, int Vector_chunks);

    private sealed record StoredVector(string Id);

    /// <summary>The run's report: a Markdown summary and a status line for the workflow.</summary>
    private static class Report
    {
        private static string Directory => Environment.GetEnvironmentVariable("SL_EVAL_REPORT_DIR") ?? TestContext.CurrentContext.WorkDirectory;

        public static void Status(string status, string detail) =>
            File.WriteAllText(Path.Combine(Directory, "search-eval-status"), $"{status}\n{detail}\n");

        public static void Write(EvalScores scores, EvalScores? committed, Dictionary<string, int> noAnswerHits, IReadOnlyList<string> failures)
        {
            var md = new StringBuilder();
            md.AppendLine(CultureInfo.InvariantCulture, $"## Search eval: {(failures.Count == 0 ? "passed" : "failed")}");
            md.AppendLine();
            md.AppendLine("| Mode | Type | nDCG@5 | committed | recall@10 | MRR | queries |");
            md.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var (mode, modeScores) in scores.Modes)
            {
                foreach (var (type, s) in modeScores.ByType.Prepend(new("overall", modeScores.Overall)))
                {
                    var before = type == "overall" ? committed?.Modes[mode].Overall : committed?.Modes[mode].ByType.GetValueOrDefault(type);
                    md.AppendLine(CultureInfo.InvariantCulture, $"| {mode} | {type} | {s.Ndcg5:F3} | {(before is null ? "–" : before.Ndcg5.ToString("F3", CultureInfo.InvariantCulture))} | {s.Recall10:F3} | {s.Mrr:F3} | {s.Queries} |");
                }
            }
            md.AppendLine();
            md.AppendLine(CultureInfo.InvariantCulture, $"No-answer queries that returned results: {string.Join(", ", noAnswerHits.Select(h => $"{h.Key} {h.Value}"))} (of {Corpus.Queries().Count(q => q.Type == "none")}).");
            foreach (var failure in failures)
            {
                md.AppendLine(CultureInfo.InvariantCulture, $"- ❌ {failure}");
            }

            TestContext.Out.WriteLine(md.ToString());
            File.WriteAllText(Path.Combine(Directory, "search-eval-report.md"), md.ToString());
            File.WriteAllText(Path.Combine(Directory, "search-eval-scores.json"), JsonSerializer.Serialize(scores, Json));
            Status(failures.Count == 0 ? "pass" : "quality", string.Join("; ", failures));
        }
    }
}
