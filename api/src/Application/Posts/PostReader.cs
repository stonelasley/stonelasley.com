using System.Text.Json;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Application.Posts;

public sealed record PostSummary(string Id, string Title, string Slug, string Area, string Date, IReadOnlyList<string> Tags);

public sealed record PostDetail(string Id, string Title, string Slug, string Area, string Date, IReadOnlyList<string> Tags, string Body);

/// <summary>
/// Public reads: published Posts whose Audience is Public.
/// </summary>
public sealed class PostReader(ID1Database database)
{
    private const string Readable = $"audience = '{PostColumns.PublicAudience}' AND status = '{PostColumns.PublishedStatus}'";

    public async Task<IReadOnlyList<PostSummary>> ListAsync(ContentArea area, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync(
            $"SELECT id, title, slug, area, date, tags FROM posts WHERE area = ? AND {Readable} ORDER BY date DESC, id DESC",
            [area.Slug()],
            cancellationToken);
        return [.. rows.Select(row => new PostSummary(
            Text(row, "id"), Text(row, "title"), Text(row, "slug"), Text(row, "area"), Text(row, "date"), Tags(row)))];
    }

    public async Task<PostDetail?> FindAsync(ContentArea area, string slug, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync(
            $"SELECT id, title, slug, area, date, tags, body FROM posts WHERE area = ? AND slug = ? AND {Readable}",
            [area.Slug(), slug],
            cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        var row = rows[0];
        return new PostDetail(
            Text(row, "id"), Text(row, "title"), Text(row, "slug"), Text(row, "area"), Text(row, "date"), Tags(row), Text(row, "body"));
    }

    private static string Text(JsonElement row, string column) => row.GetProperty(column).GetString()!;

    private static List<string> Tags(JsonElement row) => JsonSerializer.Deserialize<List<string>>(Text(row, "tags"))!;
}
