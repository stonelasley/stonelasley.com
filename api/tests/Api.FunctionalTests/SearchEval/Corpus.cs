using System.Text.Json;

namespace StoneLasley.Api.FunctionalTests.SearchEval;

/// <summary>One graded query. <see cref="Judged"/> maps relevant slugs to grades; it is null for 'none' queries.</summary>
public sealed record EvalQuery(string Type, string Q, IReadOnlyDictionary<string, int>? Judged);

/// <summary>
/// The eval's frozen inputs, copied next to the test assembly: a snapshot of the content repo in corpus/, the
/// graded queries and the committed scores. This repo is public, so the corpus holds only public Posts.
/// </summary>
public static class Corpus
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private static readonly Lazy<IReadOnlyList<EvalQuery>> LoadedQueries = new(() =>
        JsonSerializer.Deserialize<QueryFile>(File.ReadAllText(Path.Combine(Directory, "queries.json")), Json)!.Queries);

    private static string Directory => Path.Combine(AppContext.BaseDirectory, "SearchEval");

    public static IEnumerable<string> Files() =>
        System.IO.Directory.EnumerateFiles(Path.Combine(Directory, "corpus"), "*.md", SearchOption.AllDirectories).Order(StringComparer.Ordinal);

    public static IEnumerable<string> Markdowns() => Files().Select(File.ReadAllText);

    public static IReadOnlyList<EvalQuery> Queries() => LoadedQueries.Value;

    public static EvalScores? CommittedScores()
    {
        var path = Path.Combine(Directory, "scores.json");
        return File.Exists(path) ? JsonSerializer.Deserialize<EvalScores>(File.ReadAllText(path), Json) : null;
    }

    /// <summary>Rewrites the committed scores.json in the source tree (not the copy next to the assembly).</summary>
    public static void WriteScores(string json)
    {
        // The assembly's folder has its own SearchEval copy, so look for the project file, not the folder.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Api.FunctionalTests.csproj")))
        {
            dir = dir.Parent;
        }
        File.WriteAllText(Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("SearchEval source folder"), "SearchEval", "scores.json"), json + "\n");
    }

    private sealed record QueryFile(IReadOnlyList<EvalQuery> Queries);
}
