using StoneLasley.Domain.Posts;

namespace StoneLasley.Domain.UnitTests.Posts;

public class PostImagesTests
{
    [Test]
    public void Finds_relative_image_references_once_each()
    {
        var markdown = """
            ![Hero](hero.jpg)

            ![Step](./steps/one.png "Step one") and ![again](hero.jpg)

            ![Remote](https://example.com/x.png) ![Rooted](/images/y.png) ![Data](data:image/png;base64,AAAA)
            """;

        PostImages.References(markdown).ShouldBe(["hero.jpg", "steps/one.png"]);
    }

    [Test]
    public void Ignores_images_inside_code()
    {
        PostImages.References("```md\n![not](real.png)\n```\n\nInline `![no](nope.png)` too.").ShouldBeEmpty();
    }

    [Test]
    public void Rewrites_references_to_their_public_urls_keeping_alt_and_title()
    {
        var rewritten = PostImages.Rewrite(
            "![Hero](./hero.jpg) then ![Step](steps/one.png \"Step one\")",
            new Dictionary<string, string>
            {
                ["hero.jpg"] = "https://img.test/abc.jpg",
                ["steps/one.png"] = "https://img.test/def.png",
            });

        rewritten.ShouldBe("![Hero](https://img.test/abc.jpg) then ![Step](https://img.test/def.png \"Step one\")");
    }

    [TestCase("hero.jpg", "image/jpeg")]
    [TestCase("hero.JPEG", "image/jpeg")]
    [TestCase("a.png", "image/png")]
    [TestCase("a.webp", "image/webp")]
    [TestCase("a.avif", "image/avif")]
    [TestCase("a.gif", "image/gif")]
    [TestCase("a.svg", "image/svg+xml")]
    public void Knows_the_content_type_of_supported_images(string path, string contentType)
    {
        PostImages.ContentType(path).ShouldBe(contentType);
    }

    [Test]
    public void Has_no_content_type_for_other_files()
    {
        PostImages.ContentType("notes.pdf").ShouldBeNull();
    }

    [Test]
    public void Keys_an_image_by_the_hash_of_its_bytes()
    {
        PostImages.Key("hero.JPG", "hello"u8.ToArray())
            .ShouldBe("images/2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824.jpg");
    }
}
