// Admin CLI. Talks only to the API, never the Worker (ADR 0001).
var apiUrl = Environment.GetEnvironmentVariable("SL_API_URL") ?? "http://localhost:5180";

switch (args)
{
    case ["health"]:
        using (var client = new HttpClient { BaseAddress = new Uri(apiUrl) })
        {
            var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
            Console.WriteLine($"{(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
            return response.IsSuccessStatusCode ? 0 : 1;
        }
    default:
        Console.Error.WriteLine("usage: sl health    (API from SL_API_URL, default http://localhost:5180)");
        return 2;
}
