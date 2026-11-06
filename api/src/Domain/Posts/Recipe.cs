namespace StoneLasley.Domain.Posts;

public enum Difficulty
{
    Easy,
    Medium,
    Veteran,
}

/// <summary>One line of a Recipe's ingredients list. Not an entity: it exists only inside its Recipe.</summary>
public sealed record Ingredient(decimal? Quantity, string? Unit, string Name, string? Note, bool Optional);

/// <summary>
/// The typed core every Post in the Recipes area carries, stored metric-only.
/// </summary>
public sealed record Recipe(
    int PrepTimeMin,
    int CookTimeMin,
    int Servings,
    Difficulty Difficulty,
    int? OvenTempC,
    IReadOnlyList<Ingredient> Ingredients)
{
    public int TotalTimeMin => PrepTimeMin + CookTimeMin;

    public int? OvenTempF => OvenTempC is { } celsius ? ToFahrenheit(celsius) : null;

    /// <summary>
    /// Macros for one serving, to one decimal place (ADR 0007). Optional ingredients and ingredients without a
    /// quantity count for nothing; null when any other ingredient's food isn't declared or its unit doesn't convert, since a partial
    /// total would read as a whole one.
    /// </summary>
    public Macros? MacrosPerServing(Foods foods)
    {
        var counted = Ingredients.Where(i => !i.Optional && i.Quantity is not null).ToList();
        if (counted.Count == 0)
        {
            return null;
        }
        decimal kcal = 0, protein = 0, carbs = 0, fat = 0;
        foreach (var ingredient in counted)
        {
            if (foods.Weigh(ingredient) is not (var food, var grams))
            {
                return null;
            }
            kcal += food.Kcal * grams / 100;
            protein += food.Protein * grams / 100;
            carbs += food.Carbs * grams / 100;
            fat += food.Fat * grams / 100;
        }
        return new Macros(PerServing(protein), PerServing(carbs), PerServing(fat), (int)Math.Round(kcal / Servings, MidpointRounding.AwayFromZero));
    }

    private decimal PerServing(decimal grams) => Math.Round(grams / Servings, 1, MidpointRounding.AwayFromZero);

    /// <summary>Rounded to the nearest 5 °F, the step most oven dials use.</summary>
    public static int ToFahrenheit(int celsius) =>
        (int)(Math.Round((celsius * 9m / 5m + 32m) / 5m, MidpointRounding.AwayFromZero) * 5m);

    // Equality compares ingredients by value, not by list reference.
    public bool Equals(Recipe? other) =>
        other is not null
        && (PrepTimeMin, CookTimeMin, Servings, Difficulty, OvenTempC) == (other.PrepTimeMin, other.CookTimeMin, other.Servings, other.Difficulty, other.OvenTempC)
        && Ingredients.SequenceEqual(other.Ingredients);

    public override int GetHashCode() => HashCode.Combine(PrepTimeMin, CookTimeMin, Servings, Difficulty, OvenTempC, Ingredients.Count);
}
