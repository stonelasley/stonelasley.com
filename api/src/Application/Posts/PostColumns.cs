using System.Text.Json;
using System.Text.Json.Serialization;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Application.Posts;

/// <summary>
/// How domain values are written to D1 columns.
/// </summary>
internal static class PostColumns
{
    public const string PublicAudience = "public";
    public const string PublishedStatus = "published";
    public const string DraftStatus = "draft";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new WatchMakeJsonConverter(), new JsonStringEnumConverter() },
    };

    public static string? RecipeJson(Recipe? recipe) => recipe is null ? null : JsonSerializer.Serialize(recipe, Json);

    public static Recipe? RecipeFromRow(JsonElement row) =>
        row.TryGetProperty("recipe", out var column) && column.ValueKind == JsonValueKind.String
            ? JsonSerializer.Deserialize<Recipe>(column.GetString()!, Json)
            : null;

    public static string? WatchJson(Watch? watch) => watch is null ? null : JsonSerializer.Serialize(watch, Json);

    public static Watch? WatchFromRow(JsonElement row) =>
        row.TryGetProperty("watch", out var column) && column.ValueKind == JsonValueKind.String
            ? JsonSerializer.Deserialize<Watch>(column.GetString()!, Json)
            : null;

    public static string Audience(Audience audience) => audience switch
    {
        Domain.Posts.Audience.Public => PublicAudience,
        _ => throw new ArgumentOutOfRangeException(nameof(audience), audience, null),
    };

    public static string Status(PostStatus status) => status switch
    {
        PostStatus.Published => PublishedStatus,
        PostStatus.Draft => DraftStatus,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
