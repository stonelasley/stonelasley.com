using System.Security.Cryptography;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>
/// Builds fixture Posts. Every Post gets a fresh ULID and slug, so tests can share one D1 database.
/// </summary>
public static class TestPosts
{
    private const string Crockford = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string NewUlid() =>
        "0" + new string([.. Enumerable.Range(0, 25).Select(_ => Crockford[RandomNumberGenerator.GetInt32(32)])]);

    public static string NewSlug(string prefix = "post") => $"{prefix}-{Guid.NewGuid():N}";

    public static string Markdown(
        string id,
        string slug,
        string title = "A fixture Post",
        string area = "ai-development",
        string date = "2026-10-01",
        string status = "published",
        string body = "## Section\n\nSome **bold** text.",
        string extra = "") => $"""
        ---
        id: {id}
        title: {title}
        slug: {slug}
        area: {area}
        date: {date}
        tags: [fixture, testing]
        audience: public
        status: {status}
        {extra}
        ---
        {body}
        """;

    public static string Hash(string markdown) =>
        Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(markdown)));

    public const string RecipeCore = """
        prepTimeMin: 5
        cookTimeMin: 20
        servings: 4
        difficulty: Medium
        ovenTempC: 200
        ingredients:
          - { quantity: 0.5, unit: cup, name: oats }
          - { name: salt, note: a pinch, optional: true }
        """;

    public static string WatchCore(string make = "Rolex", string reference = "16610", string modelName = "Submariner Date", string production = "productionStart: 1988\nproductionEnd: 2010") => $"""
        make: {make}
        reference: "{reference}"
        modelName: {modelName}
        variants: [16610LV]
        {production}
        caseDiameterMm: 40
        calibre: "3135"
        caseMaterial: Oystersteel
        waterResistanceM: 300
        """;
}
