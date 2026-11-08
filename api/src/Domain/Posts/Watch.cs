using System.Text.Json.Serialization;

namespace StoneLasley.Domain.Posts;

/// <summary>
/// The typed core of a Watch Post: one Reference, stored metric-only, or a Make Post (a make and no Reference, so no
/// specs). The make is written the way the makes file names it. A missing production end means the Reference is still
/// made.
/// </summary>
public sealed record Watch(
    string Make,
    string? Reference,
    string? ModelName,
    IReadOnlyList<string> Variants,
    int? ProductionStart,
    int? ProductionEnd,
    decimal? CaseDiameterMm,
    string? Calibre,
    string? CaseMaterial,
    int? WaterResistanceM)
{
    [JsonIgnore]
    public bool IsMakePost => Reference is null;

    public bool InProduction => ProductionStart is not null && ProductionEnd is null;

    public int? WaterResistanceFt => WaterResistanceM is { } metres ? ToFeet(metres) : null;

    public static int ToFeet(int metres) => (int)Math.Round(metres * 3.28084m, MidpointRounding.AwayFromZero);

    /// <summary>Every decade the Reference was made in, such as 1980 for the 1980s. Open-ended runs go to <paramref name="currentYear"/>.</summary>
    public IReadOnlyList<int> Decades(int currentYear)
    {
        if (ProductionStart is not { } start)
        {
            return [];
        }
        var end = ProductionEnd ?? Math.Max(start, currentYear);
        return [.. Enumerable.Range(0, (end / 10) - (start / 10) + 1).Select(i => ((start / 10) + i) * 10)];
    }

    // Equality compares variants by value, not by list reference.
    public bool Equals(Watch? other) =>
        other is not null
        && (Make, Reference, ModelName, ProductionStart, ProductionEnd, CaseDiameterMm, Calibre, CaseMaterial, WaterResistanceM)
            == (other.Make, other.Reference, other.ModelName, other.ProductionStart, other.ProductionEnd, other.CaseDiameterMm, other.Calibre, other.CaseMaterial, other.WaterResistanceM)
        && Variants.SequenceEqual(other.Variants);

    public override int GetHashCode() => HashCode.Combine(Make, Reference, ModelName, ProductionStart, ProductionEnd);
}
