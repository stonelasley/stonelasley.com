using StoneLasley.Domain.Posts;

namespace StoneLasley.Domain.UnitTests.Posts;

public class RecipeTests
{
    private const string ValidRecipe = """
        ---
        id: 01JBX5Q7Z9K3M8N2P4R6T0V1W4
        title: Protein oats
        slug: protein-oats
        area: recipes
        date: 2026-09-20
        audience: public
        status: published
        prepTimeMin: 5
        cookTimeMin: 10
        servings: 2
        difficulty: Easy
        ovenTempC: 180
        ingredients:
          - { quantity: 80, unit: g, name: rolled oats }
          - { quantity: 1, unit: scoop, name: whey, note: vanilla }
          - { name: cinnamon, optional: true }
        ---
        Stir.
        """;

    [Test]
    public void Parses_the_recipe_typed_core()
    {
        var recipe = Parse(ValidRecipe).Recipe.ShouldNotBeNull();

        recipe.PrepTimeMin.ShouldBe(5);
        recipe.CookTimeMin.ShouldBe(10);
        recipe.Servings.ShouldBe(2);
        recipe.Difficulty.ShouldBe(Difficulty.Easy);
        recipe.OvenTempC.ShouldBe(180);
        recipe.Ingredients.ShouldBe(
        [
            new Ingredient(80m, "g", "rolled oats", null, false),
            new Ingredient(1m, "scoop", "whey", "vanilla", false),
            new Ingredient(null, null, "cinnamon", null, true),
        ]);
    }

    [Test]
    public void Total_time_is_prep_plus_cook()
    {
        Parse(ValidRecipe).Recipe!.TotalTimeMin.ShouldBe(15);
    }

    [TestCase(180, 355)]
    [TestCase(200, 390)]
    [TestCase(220, 430)]
    [TestCase(100, 210)]
    public void Oven_temperature_converts_to_fahrenheit_rounded_to_the_nearest_five(int celsius, int fahrenheit)
    {
        Recipe.ToFahrenheit(celsius).ShouldBe(fahrenheit);
    }

    [Test]
    public void Oven_temperature_is_optional()
    {
        Parse(ValidRecipe.Replace("ovenTempC: 180\n", "")).Recipe!.OvenTempC.ShouldBeNull();
    }

    [TestCase("difficulty: Easy", "difficulty: Hard", "'difficulty' must be Easy, Medium or Veteran.")]
    [TestCase("difficulty: Easy", "difficulty: 1", "'difficulty' must be Easy, Medium or Veteran.")]
    [TestCase("prepTimeMin: 5", "prepTimeMin: soon", "'prepTimeMin' must be a whole number, 0 or more.")]
    [TestCase("cookTimeMin: 10", "cookTimeMin: -1", "'cookTimeMin' must be a whole number, 0 or more.")]
    [TestCase("servings: 2", "servings: 0", "'servings' must be a whole number, 1 or more.")]
    [TestCase("ovenTempC: 180", "ovenTempC: hot", "'ovenTempC' must be a whole number, 1 or more.")]
    [TestCase("- { name: cinnamon, optional: true }", "- { optional: true }", "'ingredients[3].name' is required.")]
    [TestCase("- { quantity: 80, unit: g, name: rolled oats }", "- { quantity: none, unit: g, name: rolled oats }", "'ingredients[1].quantity' must be a number above 0.")]
    [TestCase("- { quantity: 80, unit: g, name: rolled oats }", "- { quantity: '1,5', unit: g, name: rolled oats }", "'ingredients[1].quantity' must be a number above 0.")]
    [TestCase("- { name: cinnamon, optional: true }", "- { name: cinnamon, optional: maybe }", "'ingredients[3].optional' must be true or false.")]
    public void Rejects_an_invalid_typed_core_field(string valid, string invalid, string error)
    {
        var result = MarkdownPost.Parse(ValidRecipe.Replace(valid, invalid));

        result.Post.ShouldBeNull();
        result.Errors.ShouldBe([error]);
    }

    [Test]
    public void Requires_the_typed_core_on_a_recipe()
    {
        var result = MarkdownPost.Parse(ValidRecipe[..ValidRecipe.IndexOf("prepTimeMin", StringComparison.Ordinal)] + "---\nStir.");

        result.Errors.ShouldBe(
        [
            "'prepTimeMin' must be a whole number, 0 or more.",
            "'cookTimeMin' must be a whole number, 0 or more.",
            "'servings' must be a whole number, 1 or more.",
            "'difficulty' must be Easy, Medium or Veteran.",
            "'ingredients' must list at least one ingredient.",
        ]);
    }

    [Test]
    public void Reports_typed_core_errors_alongside_other_frontmatter_errors()
    {
        var result = MarkdownPost.Parse(ValidRecipe.Replace("title: Protein oats", "title: ''").Replace("servings: 2", "servings: none"));

        result.Errors.ShouldBe(["'title' is required.", "'servings' must be a whole number, 1 or more."]);
    }

    [Test]
    public void Posts_outside_recipes_carry_no_recipe()
    {
        var post = Parse(ValidRecipe.Replace("area: recipes", "area: training"));

        post.Recipe.ShouldBeNull();
    }

    private static Post Parse(string markdown)
    {
        var result = MarkdownPost.Parse(markdown);
        result.Errors.ShouldBeEmpty();
        return result.Post!;
    }
}
