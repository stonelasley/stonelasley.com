using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace StoneLasley.Domain.Posts;

/// <summary>
/// A food Recipe ingredients may name: its macros in grams per 100 g, and the gram weight of each unit other than
/// <c>g</c> and <c>kg</c> that a Recipe measures it in.
/// </summary>
public sealed record Food(string Name, IReadOnlyList<string> Aliases, decimal Protein, decimal Carbs, decimal Fat, IReadOnlyDictionary<string, decimal> Units)
{
    // Equality compares aliases and units by value, not by reference.
    public bool Equals(Food? other) =>
        other is not null
        && (Name, Protein, Carbs, Fat) == (other.Name, other.Protein, other.Carbs, other.Fat)
        && Aliases.SequenceEqual(other.Aliases)
        && Units.Count == other.Units.Count
        && Units.All(unit => other.Units.TryGetValue(unit.Key, out var grams) && grams == unit.Value);

    public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
}

/// <summary>Protein, carbs and fat in grams, and calories derived from them at 4, 4 and 9 per gram.</summary>
public sealed record Macros(decimal Protein, decimal Carbs, decimal Fat)
{
    public int Kcal => (int)Math.Round(Protein * 4 + Carbs * 4 + Fat * 9, MidpointRounding.AwayFromZero);
}

/// <summary>
/// The foods declared in the content repo's foods file (ADR 0007). An ingredient matches a food's name or an alias,
/// ignoring case.
/// </summary>
public sealed class Foods
{
    /// <summary>Units every food converts by, in grams: Recipes write grams as <c>g</c> or <c>gram</c>.</summary>
    public static readonly IReadOnlyDictionary<string, decimal> MetricUnits = new Dictionary<string, decimal>
    {
        ["g"] = 1,
        ["gram"] = 1,
        ["grams"] = 1,
        ["kg"] = 1000,
        ["kilogram"] = 1000,
        ["kilograms"] = 1000,
    };

    private readonly Dictionary<string, Food> foods = new(StringComparer.OrdinalIgnoreCase);

    public Foods(IEnumerable<Food> declared)
    {
        foreach (var food in declared)
        {
            foreach (var written in food.Aliases.Prepend(food.Name))
            {
                foods.TryAdd(written, food);
            }
        }
    }

    /// <summary>
    /// The ingredient's food and its weight in grams: grams and kilograms always convert, any other unit by the
    /// food's units, and no unit by its <c>each</c>. Null when the food or unit isn't declared, or the ingredient has
    /// no quantity.
    /// </summary>
    public (Food Food, decimal Grams)? Weigh(Ingredient ingredient)
    {
        if (foods.GetValueOrDefault(ingredient.Name.Trim()) is not { } food || ingredient.Quantity is not { } quantity)
        {
            return null;
        }
        var unit = ingredient.Unit?.Trim().ToLowerInvariant() ?? "each";
        return MetricUnits.TryGetValue(unit, out var perUnit) || food.Units.TryGetValue(unit, out perUnit)
            ? (food, quantity * perUnit)
            : null;
    }
}

public sealed record FoodsFileResult(IReadOnlyList<Food> Foods, IReadOnlyList<string> Errors);

/// <summary>Reads the content repo's foods file: a YAML list of <c>{ name, aliases, protein, carbs, fat, units }</c>.</summary>
public static class FoodsFile
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static FoodsFileResult Parse(string yaml)
    {
        List<Entry?>? entries;
        try
        {
            entries = Yaml.Deserialize<List<Entry?>>(yaml);
        }
        catch (YamlException exception)
        {
            return new FoodsFileResult([], [$"The foods file is not valid YAML: {exception.Message}"]);
        }

        var foods = new List<Food>();
        var errors = new List<string>();
        // Names and aliases share one namespace, so an ingredient can only ever mean one food.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (entry, index) in (entries ?? []).Select((e, i) => (e ?? new Entry(), i + 1)))
        {
            var name = entry.Name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                errors.Add($"Food {index}: 'name' is required.");
                continue;
            }
            var count = errors.Count;
            var aliases = (entry.Aliases ?? []).Select(alias => alias?.Trim() ?? "").ToList();
            if (aliases.Any(alias => alias.Length == 0))
            {
                errors.Add($"Food {name}: aliases must not be empty.");
            }
            if (entry.Protein is not >= 0 || entry.Carbs is not >= 0 || entry.Fat is not >= 0)
            {
                errors.Add($"Food {name}: 'protein', 'carbs' and 'fat' are required, in grams per 100 g, 0 or more.");
            }
            else if (entry.Protein + entry.Carbs + entry.Fat > 100)
            {
                errors.Add($"Food {name}: protein, carbs and fat add up to more than 100 g per 100 g.");
            }
            var units = new Dictionary<string, decimal>();
            foreach (var (unit, grams) in entry.Units ?? [])
            {
                var key = unit.Trim().ToLowerInvariant();
                if (key.Length == 0 || Foods.MetricUnits.ContainsKey(key))
                {
                    errors.Add($"Food {name}: unit '{unit}' can't be declared; grams and kilograms always convert.");
                }
                else if (grams is not > 0)
                {
                    errors.Add($"Food {name}: unit '{key}' must weigh more than 0 g.");
                }
                else if (!units.TryAdd(key, grams.Value))
                {
                    errors.Add($"Food {name}: unit '{key}' is declared more than once.");
                }
            }
            foreach (var written in aliases.Prepend(name).Where(written => written.Length > 0 && !seen.Add(written)))
            {
                errors.Add($"'{written}' is declared more than once.");
            }
            if (errors.Count == count)
            {
                foods.Add(new Food(name, aliases, entry.Protein!.Value, entry.Carbs!.Value, entry.Fat!.Value, units));
            }
        }
        return new FoodsFileResult(errors.Count == 0 ? foods : [], errors);
    }

    private sealed class Entry
    {
        public string? Name { get; set; }
        public List<string?>? Aliases { get; set; }
        public decimal? Protein { get; set; }
        public decimal? Carbs { get; set; }
        public decimal? Fat { get; set; }
        public Dictionary<string, decimal?>? Units { get; set; }
    }
}
