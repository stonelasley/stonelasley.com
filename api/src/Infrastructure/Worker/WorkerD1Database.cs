using System.Net.Http.Json;
using System.Text.Json;
using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Infrastructure.Worker;

/// <summary>
/// Reaches D1 through the Worker's <c>/d1/query</c> and <c>/d1/batch</c> passthroughs.
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

    public async Task BatchAsync(IReadOnlyList<D1Statement> statements, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            "d1/batch",
            new BatchRequest([.. statements.Select(s => new QueryRequest(s.Sql, s.Params))]),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed record BatchRequest(IReadOnlyList<QueryRequest> Statements);

    private sealed record QueryRequest(string Sql, IReadOnlyList<object?> Params);

    private sealed record QueryResponse(IReadOnlyList<JsonElement> Results);
}
