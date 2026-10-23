using System.Net.Http.Json;
using System.Text.Json;
using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Infrastructure.Worker;

/// <summary>
/// Reaches D1 through the Worker's <c>/d1/query</c> passthrough.
/// </summary>
internal sealed class WorkerD1Database(HttpClient http) : ID1Database
{
    public async Task<IReadOnlyList<JsonElement>> QueryAsync(
        string sql,
        IReadOnlyList<object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            "d1/query",
            new QueryRequest(sql, parameters ?? []),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<QueryResponse>(cancellationToken)
            ?? throw new InvalidOperationException("The Worker returned an empty D1 response.");
        return body.Results;
    }

    private sealed record QueryRequest(string Sql, IReadOnlyList<object?> Params);

    private sealed record QueryResponse(IReadOnlyList<JsonElement> Results);
}
