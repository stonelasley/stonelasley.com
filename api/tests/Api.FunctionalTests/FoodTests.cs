using System.Net;
using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

public sealed record MacrosResponse(decimal Protein, decimal Carbs, decimal Fat, int Kcal);

public sealed record MacrosPostResponse(string Id, MacrosResponse? Macros);

public class FoodTests
{
    // 40 g of oats over 4 servings, and an optional pinch of salt.
    private static string Foods(decimal protein) => $$"""
        { "oats": { "per100g": { "kcal": 350, "protein": {{protein}}, "carbs": 60, "fat": 8 }, "source": "test label" } }
        """;

    private static string Recipe(string slug) => TestPosts.Markdown(
        TestPosts.NewUlid(), slug, area: "recipes", extra: TestPosts.RecipeCore.Replace("quantity: 0.5, unit: cup", "quantity: 40, unit: g", StringComparison.Ordinal));

    private static async Task<string> IngestRecipeAsync(HttpClient client, string? foods) =>
        await IngestAsync(client, Recipe(TestPosts.NewSlug("recipe")), foods);

    private static async Task<string> IngestAsync(HttpClient client, string markdown, string? foods)
    {
        var slug = markdown.Split('\n').First(line => line.StartsWith("slug: ", StringComparison.Ordinal))["slug: ".Length..].Trim();
        var manifest = await Ingest.ManifestAsync(client, Ingest.ById(markdown), foods: foods);
        foreach (var requested in manifest.Requested)
        {
            (await Ingest.UploadAsync(client, manifest.RunId, requested, markdown)).EnsureSuccessStatusCode();
        }
        (await Ingest.CommitAsync(client, manifest.RunId)).EnsureSuccessStatusCode();
        return slug;
    }

    [Test]
    public async Task A_recipe_is_read_with_its_macros_per_serving()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var slug = await IngestRecipeAsync(client, Foods(protein: 10));

        var post = await client.GetFromJsonAsync<MacrosPostResponse>($"/areas/recipes/posts/{slug}");

        post.ShouldNotBeNull().Macros.ShouldBe(new MacrosResponse(1m, 6m, 0.8m, 35));
    }

    [Test]
    public async Task Correcting_a_food_corrects_the_macros_without_re_ingesting_the_recipe()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var markdown = Recipe(TestPosts.NewSlug("recipe"));
        var slug = await IngestAsync(client, markdown, Foods(protein: 10));
        var revalidations = api.Revalidator.Calls;

        // Only the foods file changes: the Recipe's hash is the same, so it isn't uploaded again.
        var manifest = await Ingest.ManifestAsync(client, Ingest.ById(markdown), foods: Foods(protein: 20));
        manifest.Requested.ShouldBeEmpty();
        (await Ingest.CommitAsync(client, manifest.RunId)).EnsureSuccessStatusCode();

        var post = await client.GetFromJsonAsync<MacrosPostResponse>($"/areas/recipes/posts/{slug}");
        post.ShouldNotBeNull().Macros.ShouldNotBeNull().Protein.ShouldBe(2m);
        api.Revalidator.Calls.ShouldBe(revalidations + 1);
    }

    [Test]
    public async Task Sending_the_same_foods_again_does_not_revalidate_the_site()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var markdown = Recipe(TestPosts.NewSlug("recipe"));
        await IngestAsync(client, markdown, Foods(protein: 10));
        var revalidations = api.Revalidator.Calls;

        await IngestAsync(client, markdown, Foods(protein: 10));

        api.Revalidator.Calls.ShouldBe(revalidations);
    }

    [Test]
    public async Task A_recipe_with_an_undeclared_ingredient_has_no_macros()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var slug = await IngestRecipeAsync(client, """{ "rice": { "per100g": { "kcal": 360, "protein": 7, "carbs": 80, "fat": 1 }, "source": "test label" } }""");

        var post = await client.GetFromJsonAsync<MacrosPostResponse>($"/areas/recipes/posts/{slug}");

        post.ShouldNotBeNull().Macros.ShouldBeNull();
    }

    [Test]
    public async Task An_invalid_foods_file_fails_the_manifest()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();

        var response = await client.PostAsJsonAsync(
            "/ingest/manifest?dryRun=true",
            new { posts = new Dictionary<string, string>(), foods = """{ "oats": { "per100g": { "protein": 1 }, "source": "test label" } }""" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("'per100g' needs 'kcal', 'protein', 'carbs' and 'fat'");
    }
}
