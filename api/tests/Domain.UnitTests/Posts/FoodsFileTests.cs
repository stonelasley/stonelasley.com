using StoneLasley.Domain.Posts;

namespace StoneLasley.Domain.UnitTests.Posts;

public class FoodsFileTests
{
    [Test]
    public void Reads_each_food_s_calories_macros_and_each_weight()
    {
        var result = FoodsFile.Parse("""
            {
              "Rolled Oats": {
                "per100g": { "kcal": 379, "protein": 13.2, "carbs": 67.7, "sugar": 1, "fat": 6.5, "fiber": 10, "sodiumMg": 6 },
                "source": "USDA FDC 173904"
              },
              "Large Egg": {
                "per100g": { "kcal": 143, "protein": 12.6, "carbs": 0.7, "fat": 9.5 },
                "source": "USDA FDC 171287",
                "each": 50,
                "polyols": 0
              }
            }
            """);

        result.Errors.ShouldBeEmpty();
        result.Foods.ShouldBe(
        [
            new Food("Rolled Oats", 379m, 13.2m, 67.7m, 6.5m, null),
            new Food("Large Egg", 143m, 12.6m, 0.7m, 9.5m, 50m),
        ], ignoreOrder: false);
    }

    [Test]
    public void An_empty_table_declares_no_foods() =>
        FoodsFile.Parse("{}").Foods.ShouldBeEmpty();

    private const string Source = "\"source\": \"label\"";

    [TestCase($$"""{ "oats": { "per100g": { "protein": 1, "carbs": 1, "fat": 1 }, {{Source}} } }""", "Food oats: 'per100g' needs 'kcal', 'protein', 'carbs' and 'fat'")]
    [TestCase($$"""{ "oats": { "per100g": { "kcal": 1, "protein": -1, "carbs": 1, "fat": 1 }, {{Source}} } }""", "Food oats: 'per100g' needs")]
    [TestCase($$"""{ "oats": { {{Source}} } }""", "Food oats: 'per100g' needs")]
    [TestCase($$"""{ "oats": { "per100g": { "kcal": 1, "protein": 60, "carbs": 50, "fat": 1 }, {{Source}} } }""", "Food oats: protein, carbs and fat add up to more than 100 g")]
    [TestCase("""{ "oats": { "per100g": { "kcal": 1, "protein": 1, "carbs": 1, "fat": 1 } } }""", "Food oats: 'source' is required")]
    [TestCase($$"""{ "oats": { "per100g": { "kcal": 1, "protein": 1, "carbs": 1, "fat": 1 }, {{Source}}, "each": 0 } }""", "Food oats: 'each' must weigh more than 0 g.")]
    [TestCase($$"""{ "oats": { "per100g": { "kcal": 1, "protein": 1, "carbs": 1, "fat": 1 }, {{Source}} }, "OATS": { "per100g": { "kcal": 1, "protein": 1, "carbs": 1, "fat": 1 }, {{Source}} } }""", "'OATS' is declared more than once.")]
    [TestCase("""[{ "name": "oats" }]""", "The foods file must be a JSON object")]
    [TestCase("""{ "oats": """, "The foods file is not valid JSON")]
    public void Rejects_an_invalid_table(string json, string error)
    {
        var result = FoodsFile.Parse(json);

        result.Foods.ShouldBeEmpty();
        result.Errors.ShouldHaveSingleItem().ShouldStartWith(error);
    }

    private static readonly Foods Pantry = new(
    [
        new Food("rolled oats", 379m, 10m, 60m, 5m, null),
        new Food("egg", 143m, 12m, 1m, 10m, 50m),
    ]);

    [TestCase(40, "g", 40)]
    [TestCase(40, "gram", 40)]
    [TestCase(40, "Grams", 40)]
    [TestCase(0.5, "kg", 500)]
    public void Weighs_an_ingredient_in_grams_or_kilograms(decimal quantity, string unit, decimal grams) =>
        Pantry.Weigh(new Ingredient(quantity, unit, "Rolled Oats", null, false)).ShouldNotBeNull().Grams.ShouldBe(grams);

    [TestCase(null)]
    [TestCase("each")]
    [TestCase("Each")]
    public void A_counted_ingredient_weighs_by_each(string? unit) =>
        Pantry.Weigh(new Ingredient(3, unit, "egg", null, false)).ShouldNotBeNull().Grams.ShouldBe(150m);

    [TestCase(1, "cup", "rolled oats")]
    [TestCase(1, "each", "rolled oats")]
    [TestCase(1, "g", "quinoa")]
    [TestCase(null, null, "egg")]
    public void An_undeclared_food_or_unit_does_not_weigh(int? quantity, string? unit, string name) =>
        Pantry.Weigh(new Ingredient(quantity, unit, name, null, false)).ShouldBeNull();
}
