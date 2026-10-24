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
