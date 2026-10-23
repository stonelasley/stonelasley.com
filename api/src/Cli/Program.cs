using StoneLasley.Cli;

// Admin CLI. Talks only to the API, never the Worker (ADR 0001).
var apiUrl = new Uri(Environment.GetEnvironmentVariable("SL_API_URL") ?? "http://localhost:5180");

switch (args)
{
    case ["health"]:
        using (var client = new HttpClient { BaseAddress = apiUrl })
        {
            var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
            Console.WriteLine($"{(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
            return response.IsSuccessStatusCode ? 0 : 1;
        }
    case ["ingest", var directory, .. var flags] when flags.All(f => f is "--dry-run" or "--include-drafts"):
        var token = Environment.GetEnvironmentVariable("SL_INGEST_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            Console.Error.WriteLine("SL_INGEST_TOKEN is not set.");
            return 2;
        }
        return await IngestCommand.RunAsync(apiUrl, token, directory, flags.Contains("--dry-run"), flags.Contains("--include-drafts"));
    default:
        Console.Error.WriteLine("""
            usage: sl health
                   sl ingest <directory> [--dry-run] [--include-drafts]    (token from SL_INGEST_TOKEN)

            The API is SL_API_URL, default http://localhost:5180.
            """);
        return 2;
}
