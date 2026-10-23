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
}
