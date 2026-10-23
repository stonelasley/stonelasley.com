using System.Globalization;

namespace StoneLasley.Domain.Posts;

public static partial class MarkdownPost
{
    private static Recipe? ReadRecipe(Frontmatter frontmatter, List<string> errors)
    {
        var count = errors.Count;

        var prep = WholeNumber(frontmatter.PrepTimeMin, "prepTimeMin", 0, errors);
        var cook = WholeNumber(frontmatter.CookTimeMin, "cookTimeMin", 0, errors);
        var servings = WholeNumber(frontmatter.Servings, "servings", 1, errors);
        // Names only: Enum.TryParse would also take "1".
        var difficulty = Difficulty.Easy;
        if (Enum.GetNames<Difficulty>().Contains(frontmatter.Difficulty?.Trim()))
        {
            difficulty = Enum.Parse<Difficulty>(frontmatter.Difficulty!.Trim());
        }
        else
        {
            errors.Add("'difficulty' must be Easy, Medium or Veteran.");
        }
        var oven = frontmatter.OvenTempC is null ? null : WholeNumber(frontmatter.OvenTempC, "ovenTempC", 1, errors);

        var ingredients = new List<Ingredient>();
        if (frontmatter.Ingredients is not { Count: > 0 })
        {
            errors.Add("'ingredients' must list at least one ingredient.");
        }
        else
        {
            for (var i = 0; i < frontmatter.Ingredients.Count; i++)
            {
                if (ReadIngredient(frontmatter.Ingredients[i], $"ingredients[{i + 1}]", errors) is { } ingredient)
                {
                    ingredients.Add(ingredient);
                }
            }
        }

        return errors.Count > count ? null : new Recipe(prep!.Value, cook!.Value, servings!.Value, difficulty, oven, ingredients);
    }

    private static Ingredient? ReadIngredient(IngredientFrontmatter? line, string path, List<string> errors)
    {
        var count = errors.Count;
        line ??= new IngredientFrontmatter();

        decimal? quantity = null;
        if (line.Quantity is not null)
        {
            if (decimal.TryParse(line.Quantity.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value > 0)
            {
                quantity = value;
            }
            else
            {
                errors.Add($"'{path}.quantity' must be a number above 0.");
            }
        }

        var name = line.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            errors.Add($"'{path}.name' is required.");
        }

        var optional = false;
        if (line.Optional is not null && !bool.TryParse(line.Optional.Trim(), out optional))
        {
            errors.Add($"'{path}.optional' must be true or false.");
        }

        return errors.Count > count
            ? null
            : new Ingredient(quantity, Blank(line.Unit), name!, Blank(line.Note), optional);
    }

    private static int? WholeNumber(string? text, string field, int minimum, List<string> errors)
    {
        if (int.TryParse(text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= minimum)
        {
            return value;
        }
        errors.Add($"'{field}' must be a whole number, {minimum} or more.");
        return null;
    }

    private static string? Blank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private sealed class IngredientFrontmatter
    {
        public string? Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Name { get; set; }
        public string? Note { get; set; }
        public string? Optional { get; set; }
    }
}
