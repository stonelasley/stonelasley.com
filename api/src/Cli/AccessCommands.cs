using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace StoneLasley.Cli;

/// <summary><c>sl issue</c> and <c>sl revoke</c>: Access Codes for a Client, through the API's admin endpoints.</summary>
internal static class AccessCommands
{
    public static async Task<int> IssueAsync(Uri apiUrl, string token, string clientId)
    {
        using var client = Admin(apiUrl, token);
        using var response = await client.PostAsync($"admin/clients/{clientId}/codes", null);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            await Console.Error.WriteLineAsync($"No Client {clientId}. Clients come from the content repo's _clients.yaml.");
            return 1;
        }
        response.EnsureSuccessStatusCode();
        var issued = (await response.Content.ReadFromJsonAsync<Issued>())!;
        // Printed once; only its hash is stored. Any earlier code for this Client no longer works.
        Console.WriteLine(issued.Code);
        return 0;
    }

    public static async Task<int> RevokeAsync(Uri apiUrl, string token, string clientId)
    {
        using var client = Admin(apiUrl, token);
        using var response = await client.DeleteAsync($"admin/clients/{clientId}/codes");
        response.EnsureSuccessStatusCode();
        Console.WriteLine($"Revoked access for {clientId}.");
        return 0;
    }

    private static HttpClient Admin(Uri apiUrl, string token)
    {
        var client = new HttpClient { BaseAddress = apiUrl };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed record Issued(string ClientId, string Code);
}
