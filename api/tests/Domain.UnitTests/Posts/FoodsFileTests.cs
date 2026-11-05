using StoneLasley.Domain.Posts;

namespace StoneLasley.Domain.UnitTests.Posts;

public class FoodsFileTests
{
    [Test]
    public void Reads_names_aliases_macros_and_units()
    {
        var result = FoodsFile.Parse("""
            - name: rolled oats
              aliases: [oats]
              protein: 13.2
              carbs: 67.7
              fat: 6.5
            - name: whey protein
              protein: 80
              carbs: 8
              fat: 6
              units: { Scoop: 30 }
            """);

        result.Errors.ShouldBeEmpty();
        result.Foods.ShouldBe(
        [
            new Food("rolled oats", ["oats"], 13.2m, 67.7m, 6.5m, new Dictionary<string, decimal>()),
            new Food("whey protein", [], 80m, 8m, 6m, new Dictionary<string, decimal> { ["scoop"] = 30m }),
        ], ignoreOrder: false);
    }

    [Test]
    public void An_empty_file_declares_no_foods() =>
        FoodsFile.Parse("").Foods.ShouldBeEmpty();

    [TestCase("- protein: 1\n  carbs: 1\n  fat: 1", "Food 1: 'name' is required.")]
    [TestCase("- name: oats\n  carbs: 1\n  fat: 1", "Food oats: 'protein', 'carbs' and 'fat' are required")]
    [TestCase("- name: oats\n  protein: -1\n  carbs: 1\n  fat: 1", "Food oats: 'protein', 'carbs' and 'fat' are required")]
    [TestCase("- name: oats\n  protein: 60\n  carbs: 50\n  fat: 1", "Food oats: protein, carbs and fat add up to more than 100 g")]
    [TestCase("- name: oats\n  aliases: ['']\n  protein: 1\n  carbs: 1\n  fat: 1", "Food oats: aliases must not be empty.")]
    [TestCase("- name: oats\n  protein: 1\n  carbs: 1\n  fat: 1\n  units: { g: 2 }", "Food oats: unit 'g' can't be declared")]
    [TestCase("- name: oats\n  protein: 1\n  carbs: 1\n  fat: 1\n  units: { gram: 2 }", "Food oats: unit 'gram' can't be declared")]
    [TestCase("- name: oats\n  protein: 1\n  carbs: 1\n  fat: 1\n  units: { cup: 0 }", "Food oats: unit 'cup' must weigh more than 0 g.")]
    [TestCase("- name: oats\n  protein: 1\n  carbs: 1\n  fat: 1\n  units: { cup: 80, Cup: 90 }", "Food oats: unit 'cup' is declared more than once.")]
    [TestCase("- name: oats\n  protein: 1\n  carbs: 1\n  fat: 1\n- name: OATS\n  protein: 1\n  carbs: 1\n  fat: 1", "'OATS' is declared more than once.")]
    [TestCase("- name: [oats", "The foods file is not valid YAML")]
    public void Rejects_an_invalid_file(string yaml, string error)
    {
        var result = FoodsFile.Parse(yaml);

        result.Foods.ShouldBeEmpty();
        result.Errors.ShouldHaveSingleItem().ShouldStartWith(error);
    }

    private static readonly Foods Pantry = new(
    [
        new Food("rolled oats", ["oats"], 10m, 60m, 5m, new Dictionary<string, decimal> { ["cup"] = 80m }),
        new Food("egg", [], 12m, 1m, 10m, new Dictionary<string, decimal> { ["each"] = 50m }),
    ]);

    [TestCase(40, "g", 40)]
    [TestCase(40, "gram", 40)]
    [TestCase(40, "Grams", 40)]
    [TestCase(0.5, "kg", 500)]
    [TestCase(2, "cup", 160)]
    [TestCase(2, "Cup", 160)]
    public void Weighs_an_ingredient_by_its_unit(decimal quantity, string unit, decimal grams) =>
        Pantry.Weigh(new Ingredient(quantity, unit, "Oats", null, false)).ShouldNotBeNull().Grams.ShouldBe(grams);

    [Test]
    public void A_quantity_without_a_unit_weighs_by_each() =>
        Pantry.Weigh(new Ingredient(3, null, "egg", null, false)).ShouldNotBeNull().Grams.ShouldBe(150m);

    [TestCase(1, "tbsp", "rolled oats")]
    [TestCase(1, null, "rolled oats")]
    [TestCase(1, "g", "quinoa")]
    [TestCase(null, null, "rolled oats")]
    public void An_undeclared_food_or_unit_does_not_weigh(int? quantity, string? unit, string name) =>
        Pantry.Weigh(new Ingredient(quantity, unit, name, null, false)).ShouldBeNull();

    [Test]
    public void Calories_are_4_per_gram_of_protein_and_carbs_and_9_per_gram_of_fat() =>
        new Macros(10m, 20m, 5.5m).Kcal.ShouldBe(170);
}
