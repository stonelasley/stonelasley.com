using System.Net;
using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

public sealed record IngredientResponse(decimal? Quantity, string? Unit, string Name, string? Note, bool Optional);

public sealed record RecipeResponse(
    int PrepTimeMin,
    int CookTimeMin,
    int TotalTimeMin,
    int Servings,
    string Difficulty,
    int? OvenTempC,
    int? OvenTempF,
    IReadOnlyList<IngredientResponse> Ingredients);

public sealed record RecipePostResponse(string Id, string Title, string Body, RecipeResponse? Recipe);

public sealed record AreaResponse(string Slug, string? Intro);

public class RecipeTests
{
    [Test]
    public async Task A_recipe_is_read_with_its_typed_core_and_derived_times_and_temperatures()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var slug = TestPosts.NewSlug("recipe");
        await Ingest.RunAsync(client, TestPosts.Markdown(TestPosts.NewUlid(), slug, area: "recipes", extra: TestPosts.RecipeCore));

        var post = await client.GetFromJsonAsync<RecipePostResponse>($"/areas/recipes/posts/{slug}");

        var recipe = post.ShouldNotBeNull().Recipe.ShouldNotBeNull();
        recipe.PrepTimeMin.ShouldBe(5);
        recipe.CookTimeMin.ShouldBe(20);
        recipe.TotalTimeMin.ShouldBe(25);
        recipe.Servings.ShouldBe(4);
        recipe.Difficulty.ShouldBe("Medium");
        recipe.OvenTempC.ShouldBe(200);
        recipe.OvenTempF.ShouldBe(390);
        recipe.Ingredients.ShouldBe(
        [
            new IngredientResponse(0.5m, "cup", "oats", null, false),
            new IngredientResponse(null, null, "salt", "a pinch", true),
        ]);
    }

    [Test]
    public async Task A_post_outside_recipes_has_no_recipe()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var slug = TestPosts.NewSlug();
        await Ingest.RunAsync(client, TestPosts.Markdown(TestPosts.NewUlid(), slug));

        var post = await client.GetFromJsonAsync<RecipePostResponse>($"/areas/ai-development/posts/{slug}");

        post.ShouldNotBeNull().Recipe.ShouldBeNull();
    }

    [Test]
    public async Task Ingest_rejects_a_recipe_without_its_typed_core()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var id = TestPosts.NewUlid();
        var markdown = TestPosts.Markdown(id, TestPosts.NewSlug(), area: "recipes");
        var manifest = await Ingest.ManifestAsync(client, Ingest.ById(markdown));

        var upload = await Ingest.UploadAsync(client, manifest.RunId, id, markdown);

        upload.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await upload.Content.ReadAsStringAsync()).ShouldContain("'difficulty' must be Easy, Medium or Veteran.");
    }

    [Test]
    public async Task An_area_intro_is_ingested_with_the_manifest_and_read_with_the_area()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();

        var manifest = await client.PostAsJsonAsync(
            "/ingest/manifest",
            new { posts = new Dictionary<string, string>(), intros = new Dictionary<string, string> { ["recipes"] = "Macro-friendly **meal prep**." } });
        var run = await manifest.Content.ReadFromJsonAsync<ManifestResponse>();
        (await Ingest.CommitAsync(client, run!.RunId)).EnsureSuccessStatusCode();

        (await client.GetFromJsonAsync<AreaResponse>("/areas/recipes")).ShouldBe(new AreaResponse("recipes", "Macro-friendly **meal prep**."));
        (await client.GetFromJsonAsync<AreaResponse>("/areas/watches")).ShouldBe(new AreaResponse("watches", null));
    }

    [Test]
    public async Task An_area_left_out_of_the_intros_loses_its_intro()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var first = await client.PostAsJsonAsync(
            "/ingest/manifest",
            new { posts = new Dictionary<string, string>(), intros = new Dictionary<string, string> { ["training"] = "Lift." } });
        (await Ingest.CommitAsync(client, (await first.Content.ReadFromJsonAsync<ManifestResponse>())!.RunId)).EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync(
            "/ingest/manifest",
            new { posts = new Dictionary<string, string>(), intros = new Dictionary<string, string>() });
        (await Ingest.CommitAsync(client, (await second.Content.ReadFromJsonAsync<ManifestResponse>())!.RunId)).EnsureSuccessStatusCode();

        (await client.GetFromJsonAsync<AreaResponse>("/areas/training")).ShouldNotBeNull().Intro.ShouldBeNull();
    }

    [Test]
    public async Task A_manifest_without_intros_leaves_them_alone()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var first = await client.PostAsJsonAsync(
            "/ingest/manifest",
            new { posts = new Dictionary<string, string>(), intros = new Dictionary<string, string> { ["watches"] = "Steel." } });
        (await Ingest.CommitAsync(client, (await first.Content.ReadFromJsonAsync<ManifestResponse>())!.RunId)).EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/ingest/manifest", new { posts = new Dictionary<string, string>() });
        (await Ingest.CommitAsync(client, (await second.Content.ReadFromJsonAsync<ManifestResponse>())!.RunId)).EnsureSuccessStatusCode();

        (await client.GetFromJsonAsync<AreaResponse>("/areas/watches")).ShouldNotBeNull().Intro.ShouldBe("Steel.");
    }

    [Test]
    public async Task A_manifest_with_an_intro_for_an_unknown_area_is_rejected()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();

        var manifest = await client.PostAsJsonAsync(
            "/ingest/manifest",
            new { posts = new Dictionary<string, string>(), intros = new Dictionary<string, string> { ["blog"] = "Old." } });

        manifest.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task An_unknown_area_is_not_found()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();

        (await client.GetAsync("/areas/blog")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
