using System.Text.Json;

namespace StoneLasley.Domain.Posts;

/// <summary>
/// A food Recipe ingredients may name: its calories and macros per 100 g, and the gram weight of one of it when a
/// Recipe counts it by <c>each</c>.
/// </summary>
public sealed record Food(string Name, decimal Kcal, decimal Protein, decimal Carbs, decimal Fat, decimal? Each);

/// <summary>Calories, and protein, carbs and fat in grams.</summary>
public sealed record Macros(decimal Protein, decimal Carbs, decimal Fat, int Kcal);

/// <summary>
/// The foods declared in the content repo's foods file (ADR 0007). An ingredient matches a food's name, ignoring case.
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
            foods.TryAdd(food.Name, food);
        }
    }

    /// <summary>
    /// The ingredient's food and its weight in grams: grams and kilograms always convert, and <c>each</c> or no unit
    /// by the food's <c>each</c>. Null when the food isn't declared, the unit doesn't convert, or the ingredient has
    /// no quantity.
    /// </summary>
    public (Food Food, decimal Grams)? Weigh(Ingredient ingredient)
    {
        if (foods.GetValueOrDefault(ingredient.Name.Trim()) is not { } food || ingredient.Quantity is not { } quantity)
        {
            return null;
        }
        var unit = ingredient.Unit?.Trim().ToLowerInvariant() ?? "each";
        decimal? perUnit = unit == "each" ? food.Each : MetricUnits.TryGetValue(unit, out var grams) ? grams : null;
        return perUnit is { } weight ? (food, quantity * weight) : null;
    }
}

public sealed record FoodsFileResult(IReadOnlyList<Food> Foods, IReadOnlyList<string> Errors);

/// <summary>
/// Reads the content repo's foods file, the recipe skill's sourced nutrition table: a JSON object from each food's
/// name to <c>{ per100g: { kcal, protein, carbs, fat, … }, source, each? }</c>. Other fields are the skill's own.
/// </summary>
public static class FoodsFile
{
    public static FoodsFileResult Parse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            return new FoodsFileResult([], [$"The foods file is not valid JSON: {exception.Message}"]);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new FoodsFileResult([], ["The foods file must be a JSON object from each food's name to its entry."]);
            }

            var foods = new List<Food>();
            var errors = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                var name = property.Name.Trim();
                if (name.Length == 0)
                {
                    errors.Add("A food's name must not be empty.");
                    continue;
                }
                if (!seen.Add(name))
                {
                    errors.Add($"'{name}' is declared more than once.");
                    continue;
                }
                var count = errors.Count;
                var entry = property.Value;
                var per100g = entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("per100g", out var p) && p.ValueKind == JsonValueKind.Object ? p : default;
                decimal? Amount(JsonElement element, string field) =>
                    element.ValueKind == JsonValueKind.Object && element.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.Number
                        ? value.GetDecimal()
                        : null;
                var (kcal, protein, carbs, fat) = (Amount(per100g, "kcal"), Amount(per100g, "protein"), Amount(per100g, "carbs"), Amount(per100g, "fat"));
                if (kcal is not >= 0 || protein is not >= 0 || carbs is not >= 0 || fat is not >= 0)
                {
                    errors.Add($"Food {name}: 'per100g' needs 'kcal', 'protein', 'carbs' and 'fat', each 0 or more.");
                }
                else if (protein + carbs + fat > 100)
                {
                    errors.Add($"Food {name}: protein, carbs and fat add up to more than 100 g per 100 g.");
                }
                // Every number traces to USDA or a label, never to memory.
                if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("source", out var source) || source.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(source.GetString()))
                {
                    errors.Add($"Food {name}: 'source' is required: the USDA entry or label the numbers come from.");
                }
                var each = Amount(entry, "each");
                if (entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("each", out _) && each is not > 0)
                {
                    errors.Add($"Food {name}: 'each' must weigh more than 0 g.");
                }
                if (errors.Count == count)
                {
                    foods.Add(new Food(name, kcal!.Value, protein!.Value, carbs!.Value, fat!.Value, each));
                }
            }
            return new FoodsFileResult(errors.Count == 0 ? foods : [], errors);
        }
    }
}
