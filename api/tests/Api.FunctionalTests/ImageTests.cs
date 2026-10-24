using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace StoneLasley.Api.FunctionalTests;

public class ImageTests
{
    // A 1×1 PNG.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    private static string KeyOf(byte[] bytes, string ext = ".png") => $"images/{Convert.ToHexStringLower(SHA256.HashData(bytes))}{ext}";

    private static async Task<(string Slug, HttpResponseMessage Upload)> IngestWithImageAsync(
        HttpClient client, byte[] image, string body = "![A dot](./dot.png)\n\nWords.")
    {
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        var markdown = TestPosts.Markdown(id, slug, body: body);
        var manifest = await Ingest.ManifestAsync(client, Ingest.ById(markdown));
        var upload = await Ingest.UploadAsync(client, manifest.RunId, id, markdown, new Dictionary<string, byte[]> { ["dot.png"] = image });
        if (upload.IsSuccessStatusCode)
        {
            (await Ingest.CommitAsync(client, manifest.RunId)).EnsureSuccessStatusCode();
        }
        return (slug, upload);
    }

    private static async Task<HttpResponseMessage> HeadObjectAsync(string key)
    {
        using var worker = new HttpClient { BaseAddress = Testing.Worker.BaseUrl };
        worker.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", WranglerDev.Secret);
        return await worker.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"/r2/objects/{key}"));
    }

    [Test]
    public async Task A_relative_image_is_stored_under_its_content_hash_and_the_body_points_at_it()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var image = Png.Concat(RandomNumberGenerator.GetBytes(8)).ToArray();

        var (slug, upload) = await IngestWithImageAsync(client, image);

        upload.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var post = await client.GetFromJsonAsync<PostResponse>($"/areas/ai-development/posts/{slug}");
        post.ShouldNotBeNull().Body.ShouldBe($"![A dot]({ApiFactory.ImagesBaseUrl}/{KeyOf(image)})\n\nWords.");
        (await HeadObjectAsync(KeyOf(image))).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task A_broken_image_reference_fails_validation()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();

        var (_, upload) = await IngestWithImageAsync(client, Png, body: "![Gone](missing.png)");

        upload.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await upload.Content.ReadAsStringAsync()).ShouldContain("Image 'missing.png' is referenced but was not uploaded.");
    }

    [Test]
    public async Task An_unsupported_image_type_fails_validation()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        var markdown = TestPosts.Markdown(id, slug, body: "![Doc](notes.pdf)");
        var manifest = await Ingest.ManifestAsync(client, Ingest.ById(markdown));

        var upload = await Ingest.UploadAsync(client, manifest.RunId, id, markdown, new Dictionary<string, byte[]> { ["notes.pdf"] = [1, 2, 3] });

        upload.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await upload.Content.ReadAsStringAsync()).ShouldContain("'notes.pdf' is not a supported image type");
    }

    [Test]
    public async Task Reingesting_an_unchanged_image_does_not_upload_it_again()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var image = Png.Concat(RandomNumberGenerator.GetBytes(8)).ToArray();
        await IngestWithImageAsync(client, image);
        var firstUpload = (await HeadObjectAsync(KeyOf(image))).Headers.GetValues("x-uploaded").Single();
        await Task.Delay(50);

        await IngestWithImageAsync(client, image, body: "![Same dot](dot.png)\n\nNew words.");

        (await HeadObjectAsync(KeyOf(image))).Headers.GetValues("x-uploaded").Single().ShouldBe(firstUpload);
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string body, Dictionary<string, byte[]> images)
    {
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        var markdown = TestPosts.Markdown(id, slug, body: body);
        var manifest = await Ingest.ManifestAsync(client, Ingest.ById(markdown));
        return await Ingest.UploadAsync(client, manifest.RunId, id, markdown, images);
    }

    [Test]
    public async Task An_image_outside_the_posts_folder_fails_validation()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();

        var upload = await UploadAsync(client, "![Escape](../secret.png)", new() { ["../secret.png"] = Png });

        upload.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await upload.Content.ReadAsStringAsync()).ShouldContain("Image '../secret.png' must be inside the Post's folder.");
    }

    [Test]
    public async Task A_reference_style_image_fails_validation()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();

        var upload = await UploadAsync(client, "![Hero][hero]\n\n[hero]: hero.png", new() { ["hero.png"] = Png });

        upload.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await upload.Content.ReadAsStringAsync()).ShouldContain("Image [hero] is reference-style; write it inline");
    }

    [Test]
    public async Task An_image_over_the_size_limit_fails_validation()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();

        var upload = await UploadAsync(client, "![Big](big.png)", new() { ["big.png"] = new byte[(10 * 1024 * 1024) + 1] });

        upload.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await upload.Content.ReadAsStringAsync()).ShouldContain("Image 'big.png' is over the 10 MB limit.");
    }

    [Test]
    public async Task The_same_image_sent_under_two_spellings_is_stored_once()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();

        var upload = await UploadAsync(client, "![Dot](dot.png)", new() { ["./dot.png"] = Png, ["dot.png"] = Png });

        upload.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
