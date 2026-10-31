using System.Text.Json;
using System.Text.Json.Serialization;

namespace StoneLasley.Domain.Posts;

/// <summary>Watch makes the site covers. Closed: adding one is a code change.</summary>
[JsonConverter(typeof(WatchMakeJsonConverter))]
public enum WatchMake
{
    Rolex,
    PatekPhilippe,
    Omega,
    AudemarsPiguet,
    VacheronConstantin,
    ALangeAndSohne,
    Tudor,
    Cartier,
    GrandSeiko,
    Iwc,
    JaegerLeCoultre,
    Breitling,
    Seiko,
    Swatch,
    Breguet,
    FPJourne,
    Casio,
    Glycine,
    Panerai,
    Diw,
    ArtisansDeGeneve,
    RichardMille,
    Blancpain,
    Zenith,
    Longines,
    TrueNorth,
    ChristopherWard,
    Yema,
    Baltic,
    FurlanMarri,
    AnOrdain,
    Serica,
    Lorier,
    StudioUnderdog,
    KuronoTokyo,
    AtelierWen,
    Monta,
    Venezianico,
    Unimatic,
    Orient,
    Watchdives,
    Sugess,
    Timex,
    Sinn,
    Bulova,
    Caravelle,
    Shinola,
    D1Milano,
    TagHeuer,
    Helm,
    Vaer,
    Squale,
    Hamilton,
    Marathon,
    Wyler,
    AbercrombieAndFitch,
    Heuer,
    Wakmann,
    Doxa,
    FavreLeuba,
    Eberhard,
    CigaDesign,
    EchoNeutra,
    Scurfa,
    VonDoren,
    Asterisk,
    Oris,
    Ginault,
}

public static class WatchMakes
{
    private static readonly Dictionary<WatchMake, string> Names = new()
    {
        [WatchMake.Rolex] = "Rolex",
        [WatchMake.PatekPhilippe] = "Patek Philippe",
        [WatchMake.Omega] = "Omega",
        [WatchMake.AudemarsPiguet] = "Audemars Piguet",
        [WatchMake.VacheronConstantin] = "Vacheron Constantin",
        [WatchMake.ALangeAndSohne] = "A. Lange & Söhne",
        [WatchMake.Tudor] = "Tudor",
        [WatchMake.Cartier] = "Cartier",
        [WatchMake.GrandSeiko] = "Grand Seiko",
        [WatchMake.Iwc] = "IWC",
        [WatchMake.JaegerLeCoultre] = "Jaeger-LeCoultre",
        [WatchMake.Breitling] = "Breitling",
        [WatchMake.Seiko] = "Seiko",
        [WatchMake.Swatch] = "Swatch",
        [WatchMake.Breguet] = "Breguet",
        [WatchMake.FPJourne] = "F.P. Journe",
        [WatchMake.Casio] = "Casio",
        [WatchMake.Glycine] = "Glycine",
        [WatchMake.Panerai] = "Panerai",
        [WatchMake.Diw] = "DIW",
        [WatchMake.ArtisansDeGeneve] = "Artisans de Genève",
        [WatchMake.RichardMille] = "Richard Mille",
        [WatchMake.Blancpain] = "Blancpain",
        [WatchMake.Zenith] = "Zenith",
        [WatchMake.Longines] = "Longines",
        [WatchMake.TrueNorth] = "True North",
        [WatchMake.ChristopherWard] = "Christopher Ward",
        [WatchMake.Yema] = "Yema",
        [WatchMake.Baltic] = "Baltic",
        [WatchMake.FurlanMarri] = "Furlan Marri",
        [WatchMake.AnOrdain] = "anOrdain",
        [WatchMake.Serica] = "Serica",
        [WatchMake.Lorier] = "Lorier",
        [WatchMake.StudioUnderdog] = "Studio Underd0g",
        [WatchMake.KuronoTokyo] = "Kurono Tokyo",
        [WatchMake.AtelierWen] = "Atelier Wen",
        [WatchMake.Monta] = "Monta",
        [WatchMake.Venezianico] = "Venezianico",
        [WatchMake.Unimatic] = "Unimatic",
        [WatchMake.Orient] = "Orient",
        [WatchMake.Watchdives] = "Watchdives",
        [WatchMake.Sugess] = "Sugess",
        [WatchMake.Timex] = "Timex",
        [WatchMake.Sinn] = "Sinn",
        [WatchMake.Bulova] = "Bulova",
        [WatchMake.Caravelle] = "Caravelle",
        [WatchMake.Shinola] = "Shinola",
        [WatchMake.D1Milano] = "D1 Milano",
        [WatchMake.TagHeuer] = "TAG Heuer",
        [WatchMake.Helm] = "Helm",
        [WatchMake.Vaer] = "Vaer",
        [WatchMake.Squale] = "Squale",
        [WatchMake.Hamilton] = "Hamilton",
        [WatchMake.Marathon] = "Marathon",
        [WatchMake.Wyler] = "Wyler",
        [WatchMake.AbercrombieAndFitch] = "Abercrombie & Fitch",
        [WatchMake.Heuer] = "Heuer",
        [WatchMake.Wakmann] = "Wakmann",
        [WatchMake.Doxa] = "Doxa",
        [WatchMake.FavreLeuba] = "Favre Leuba",
        [WatchMake.Eberhard] = "Eberhard & Co.",
        [WatchMake.CigaDesign] = "CIGA Design",
        [WatchMake.EchoNeutra] = "echo/neutra",
        [WatchMake.Scurfa] = "Scurfa",
        [WatchMake.VonDoren] = "Von Doren",
        [WatchMake.Asterisk] = "Asterisk",
        [WatchMake.Oris] = "Oris",
        [WatchMake.Ginault] = "Ginault",
    };

    public static IReadOnlyCollection<string> All => Names.Values;

    /// <summary>How the make is written, such as "Patek Philippe".</summary>
    public static string Name(this WatchMake make) => Names[make];

    public static bool TryParse(string? name, out WatchMake make)
    {
        foreach (var (candidate, candidateName) in Names)
        {
            if (candidateName == name)
            {
                make = candidate;
                return true;
            }
        }
        make = default;
        return false;
    }
}

/// <summary>Reads and writes a make by its written name, in storage and in API responses alike.</summary>
public sealed class WatchMakeJsonConverter : JsonConverter<WatchMake>
{
    public override WatchMake Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        WatchMakes.TryParse(reader.GetString(), out var make) ? make : throw new JsonException($"Unknown watch make '{reader.GetString()}'.");

    public override void Write(Utf8JsonWriter writer, WatchMake value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Name());
}

/// <summary>
/// The typed core of a Watch Post: one Reference, stored metric-only. A missing production end means the
/// Reference is still made.
/// </summary>
public sealed record Watch(
    WatchMake Make,
    string Reference,
    string ModelName,
    IReadOnlyList<string> Variants,
    int? ProductionStart,
    int? ProductionEnd,
    decimal? CaseDiameterMm,
    string? Calibre,
    string? CaseMaterial,
    int? WaterResistanceM)
{
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
