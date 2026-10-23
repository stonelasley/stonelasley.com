using System.Text.Json;

namespace StoneLasley.Application.Common.Interfaces;

/// <summary>
/// The site's D1 database. Each row comes back as a JSON object keyed by column name.
/// </summary>
public interface ID1Database
{
    Task<IReadOnlyList<JsonElement>> QueryAsync(
        string sql,
        IReadOnlyList<object?>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>Runs the statements as one transaction: all of them apply, or none do.</summary>
    Task BatchAsync(IReadOnlyList<D1Statement> statements, CancellationToken cancellationToken = default);
}

public sealed record D1Statement(string Sql, IReadOnlyList<object?> Params);
