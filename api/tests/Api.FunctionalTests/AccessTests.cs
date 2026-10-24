using System.Net;
using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

public sealed record IssuedCode(string ClientId, string Code);

public sealed record Exchanged(string Session, string ClientName);

/// <summary>
/// Clients, Access Codes and Private Posts. Each test declares its own made-up Clients, since every ingest
/// replaces the clients file.
/// </summary>
public class AccessTests
{
    private static string Clients(params (string Id, string Name)[] clients) =>
        string.Join('\n', clients.Select(c => $"- id: {c.Id}\n  name: {c.Name}"));

    private static async Task<string> IssueAsync(ApiFactory api, string clientId)
    {
        using var admin = api.CreateAdminClient();
        var response = await admin.PostAsync($"/admin/clients/{clientId}/codes", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IssuedCode>())!.Code;
    }

    private static Task<HttpResponseMessage> ExchangeAsync(HttpClient client, string code, string turnstile = "ok", string ip = "203.0.113.7")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/access/exchange") { Content = JsonContent.Create(new { code, turnstileToken = turnstile }) };
        request.Headers.Add("X-Forwarded-For", ip);
        return client.SendAsync(request);
    }

    private static async Task<string> SessionAsync(HttpClient client, string code) =>
        (await (await ExchangeAsync(client, code)).Content.ReadFromJsonAsync<Exchanged>())!.Session;

    private static Task<HttpResponseMessage> ReadAsync(HttpClient client, string slug, string? session)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/areas/training/posts/{slug}");
        if (session is not null)
        {
            request.Headers.Add("X-Access-Session", session);
        }
        return client.SendAsync(request);
    }

    private sealed record Corpus(string Alex, string Sam, string Shared, string AlexOnly, string SamOnly);

    private static async Task<Corpus> IngestCorpusAsync(ApiFactory api)
    {
        using var ingest = api.CreateIngestClient();
        var (alex, sam) = (TestPosts.NewUlid(), TestPosts.NewUlid());
        var corpus = new Corpus(alex, sam, TestPosts.NewSlug("shared"), TestPosts.NewSlug("alex"), TestPosts.NewSlug("sam"));
        await Ingest.RunWithClientsAsync(
            ingest,
            Clients((alex, "Alex Example"), (sam, "Sam Sample")),
            TestPosts.Markdown(TestPosts.NewUlid(), corpus.Shared, title: "Library", area: "training", audience: "clients"),
            TestPosts.Markdown(TestPosts.NewUlid(), corpus.AlexOnly, title: "Alex plan", area: "training", audience: alex),
            TestPosts.Markdown(TestPosts.NewUlid(), corpus.SamOnly, title: "Sam plan", area: "training", audience: sam));
        return corpus;
    }

    [Test]
    public async Task Private_posts_are_never_served_without_a_session()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();
        var corpus = await IngestCorpusAsync(api);

        (await ReadAsync(client, corpus.Shared, null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadAsync(client, corpus.AlexOnly, null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var listed = await client.GetFromJsonAsync<List<PostSummaryResponse>>("/areas/training/posts");
        listed.ShouldNotBeNull().ShouldNotContain(p => p.Slug == corpus.Shared || p.Slug == corpus.AlexOnly);
        var latest = await client.GetFromJsonAsync<List<PostSummaryResponse>>("/posts/latest?limit=50");
        latest.ShouldNotBeNull().ShouldNotContain(p => p.Slug == corpus.Shared || p.Slug == corpus.AlexOnly);
        var search = await client.GetFromJsonAsync<SearchResponse>("/search?q=plan library");
        search.ShouldNotBeNull().Results.ShouldBeEmpty();
    }

    [Test]
    public async Task A_code_unlocks_the_shared_library_and_the_clients_own_posts_and_never_another_clients()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();
        var corpus = await IngestCorpusAsync(api);
        var session = await SessionAsync(client, await IssueAsync(api, corpus.Alex));

        (await ReadAsync(client, corpus.Shared, session)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadAsync(client, corpus.AlexOnly, session)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadAsync(client, corpus.SamOnly, session)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Exchange_names_the_client_and_accepts_the_code_however_it_is_typed()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();
        var corpus = await IngestCorpusAsync(api);
        var code = await IssueAsync(api, corpus.Alex);

        var response = await ExchangeAsync(client, code.ToLowerInvariant().Replace('-', ' '));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<Exchanged>())!.ClientName.ShouldBe("Alex Example");
    }

    [Test]
    public async Task A_wrong_code_fails_without_saying_why()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();
        var corpus = await IngestCorpusAsync(api);
        var revoked = await IssueAsync(api, corpus.Alex);
        await IssueAsync(api, corpus.Alex); // revokes the first

        var unknown = await ExchangeAsync(client, "AAAA-AAAA-AAAA-AAAA-AAAA-AAAA");
        var old = await ExchangeAsync(client, revoked);

        unknown.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        old.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await unknown.Content.ReadAsStringAsync()).ShouldBe(await old.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task Exchange_requires_a_turnstile_token()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();
        var corpus = await IngestCorpusAsync(api);
        var code = await IssueAsync(api, corpus.Alex);

        (await ExchangeAsync(client, code, turnstile: FakeTurnstile.BadToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ExchangeAsync(client, code, turnstile: "")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Exchange_is_rate_limited_per_ip()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 12; i++)
        {
            statuses.Add((await ExchangeAsync(client, "AAAA-AAAA-AAAA-AAAA-AAAA-AAAA", ip: "198.51.100.1")).StatusCode);
        }
        var otherIp = await ExchangeAsync(client, "AAAA-AAAA-AAAA-AAAA-AAAA-AAAA", ip: "198.51.100.2");

        statuses.ShouldContain(HttpStatusCode.TooManyRequests);
        otherIp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Revoking_a_code_takes_effect_on_the_next_read()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();
        var corpus = await IngestCorpusAsync(api);
        var session = await SessionAsync(client, await IssueAsync(api, corpus.Alex));
        (await ReadAsync(client, corpus.AlexOnly, session)).StatusCode.ShouldBe(HttpStatusCode.OK);

        using (var admin = api.CreateAdminClient())
        {
            (await admin.DeleteAsync($"/admin/clients/{corpus.Alex}/codes")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        (await ReadAsync(client, corpus.AlexOnly, session)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Issuing_a_new_code_revokes_the_previous_one_and_its_sessions()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();
        var corpus = await IngestCorpusAsync(api);
        var first = await SessionAsync(client, await IssueAsync(api, corpus.Alex));

        var second = await SessionAsync(client, await IssueAsync(api, corpus.Alex));

        (await ReadAsync(client, corpus.AlexOnly, first)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadAsync(client, corpus.AlexOnly, second)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task A_forged_session_reads_nothing()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();
        var corpus = await IngestCorpusAsync(api);
        var session = await SessionAsync(client, await IssueAsync(api, corpus.Alex));

        var forged = session[..^4] + (session[^4..] == "AAAA" ? "BBBB" : "AAAA");

        (await ReadAsync(client, corpus.AlexOnly, forged)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadAsync(client, corpus.AlexOnly, "garbage")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestCase(null)]
    [TestCase("not-the-admin-token")]
    [TestCase(ApiFactory.IngestToken)]
    public async Task Admin_endpoints_require_the_admin_token(string? token)
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        var clientId = TestPosts.NewUlid();

        (await client.PostAsync($"/admin/clients/{clientId}/codes", null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync($"/admin/clients/{clientId}/codes")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Issuing_for_an_unknown_client_is_not_found()
    {
        await using var api = new ApiFactory();
        using var admin = api.CreateAdminClient();

        (await admin.PostAsync($"/admin/clients/{TestPosts.NewUlid()}/codes", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Ingest_rejects_a_post_for_an_unknown_client()
    {
        await using var api = new ApiFactory();
        using var ingest = api.CreateIngestClient();
        var id = TestPosts.NewUlid();
        var markdown = TestPosts.Markdown(id, TestPosts.NewSlug(), audience: TestPosts.NewUlid());
        var manifest = await Ingest.ManifestAsync(ingest, Ingest.ById(markdown), clients: Clients((TestPosts.NewUlid(), "Someone")));

        var upload = await Ingest.UploadAsync(ingest, manifest.RunId, id, markdown);

        upload.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await upload.Content.ReadAsStringAsync()).ShouldContain("is not a declared Client");
    }

    [Test]
    public async Task A_bad_clients_file_fails_the_manifest()
    {
        await using var api = new ApiFactory();
        using var ingest = api.CreateIngestClient();

        var response = await ingest.PostAsJsonAsync("/ingest/manifest", new { posts = new Dictionary<string, string>(), clients = "- id: nope\n  name: X" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("'id' must be a ULID");
    }

    [Test]
    public async Task A_client_removed_from_the_clients_file_loses_their_code_for_good()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();
        var corpus = await IngestCorpusAsync(api);
        var code = await IssueAsync(api, corpus.Alex);
        using var ingest = api.CreateIngestClient();
        await Ingest.RunWithClientsAsync(ingest, Clients((corpus.Sam, "Sam Sample")));

        await Ingest.RunWithClientsAsync(ingest, Clients((corpus.Alex, "Alex Example"), (corpus.Sam, "Sam Sample")));

        (await ExchangeAsync(client, code)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task The_rate_limit_keys_on_the_hop_our_ingress_added_not_what_the_caller_sent()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 12; i++)
        {
            // A spoofed left-most entry changes every time; the right-most (ours) doesn't.
            statuses.Add((await ExchangeAsync(client, "AAAA-AAAA-AAAA-AAAA-AAAA-AAAA", ip: $"10.0.0.{i}, 198.51.100.9")).StatusCode);
        }

        statuses.ShouldContain(HttpStatusCode.TooManyRequests);
    }

    public sealed record YourContent(string ClientName, IReadOnlyList<PostSummaryResponse> Posts);

    private static Task<HttpResponseMessage> YourContentAsync(HttpClient client, string? session)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/access/posts");
        if (session is not null)
        {
            request.Headers.Add("X-Access-Session", session);
        }
        return client.SendAsync(request);
    }

    [Test]
    public async Task Your_content_lists_the_shared_library_and_the_clients_own_posts_only()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();
        using (var ingest = api.CreateIngestClient())
        {
            var (alex, sam) = (TestPosts.NewUlid(), TestPosts.NewUlid());
            await Ingest.RunWithClientsAsync(
                ingest,
                Clients((alex, "Alex Example"), (sam, "Sam Sample")),
                TestPosts.Markdown(TestPosts.NewUlid(), "library", title: "Library", area: "training", audience: "clients", date: "2026-09-01"),
                TestPosts.Markdown(TestPosts.NewUlid(), "alex-plan", title: "Alex plan", area: "training", audience: alex, date: "2026-09-02"),
                TestPosts.Markdown(TestPosts.NewUlid(), "sam-plan", title: "Sam plan", area: "training", audience: sam),
                TestPosts.Markdown(TestPosts.NewUlid(), "public-post", title: "Public", area: "training"));
            var session = await SessionAsync(client, await IssueAsync(api, alex));

            var response = await YourContentAsync(client, session);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var mine = await response.Content.ReadFromJsonAsync<YourContent>();
            mine.ShouldNotBeNull().ClientName.ShouldBe("Alex Example");
            mine.Posts.Select(p => p.Title).ShouldBe(["Alex plan", "Library"]);
        }
    }

    [TestCase(null)]
    [TestCase("garbage")]
    public async Task Your_content_needs_a_live_session(string? session)
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();

        (await YourContentAsync(client, session)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
