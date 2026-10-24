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
    public void Knows_the_content_type_of_supported_images(string path, string contentType)
    {
        PostImages.ContentType(path).ShouldBe(contentType);
    }

    [TestCase("notes.pdf")]
    [TestCase("drawing.svg")] // SVG can carry script; never serve it from our own domain
    public void Has_no_content_type_for_other_files(string path)
    {
        PostImages.ContentType(path).ShouldBeNull();
    }

    [TestCase("./a/./b.png", "a/b.png")]
    [TestCase("a/../b.png", "b.png")]
    [TestCase("steps\\one.png", "steps/one.png")]
    public void Normalises_paths(string path, string normalised)
    {
        PostImages.Normalise(path).ShouldBe(normalised);
    }

    [TestCase("../secret.png")]
    [TestCase("a/../../secret.png")]
    public void Has_no_path_for_one_that_leaves_the_post(string path)
    {
        PostImages.Normalise(path).ShouldBeNull();
    }

    [Test]
    public void Finds_angle_bracket_references_with_spaces_and_single_quoted_titles()
    {
        PostImages.References("![A](<my photo.jpg> 'Title') and ![B](b.png 'T')").ShouldBe(["my photo.jpg", "b.png"]);
    }

    [Test]
    public void Rewrites_angle_bracket_references()
    {
        PostImages.Rewrite("![A](<my photo.jpg>)", new Dictionary<string, string> { ["my photo.jpg"] = "https://img.test/x.jpg" })
            .ShouldBe("![A](https://img.test/x.jpg)");
    }

    [Test]
    public void Reports_reference_style_images_with_relative_targets()
    {
        PostImages.ReferenceStyle("![Hero][hero]\n\n[hero]: hero.jpg\n[site]: https://example.com").ShouldBe(["hero"]);
    }

    [Test]
    public void Keys_an_image_by_the_hash_of_its_bytes()
    {
        PostImages.Key("hero.JPG", "hello"u8.ToArray())
            .ShouldBe("images/2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824.jpg");
    }
}
