using StoneLasley.Domain.Posts;

namespace StoneLasley.Domain.UnitTests.Posts;

public class MarkdownPostTests
{
    private const string ValidPost = """
        ---
        id: 01JBX5Q7Z9K3M8N2P4R6T0V1W2
        title: Learning with mind maps
        slug: learning-with-mind-maps
        area: ai-development
        date: 2026-10-01
        tags: [learning, notes]
        audience: public
        status: published
        ---
        ## Start here

        Body text.
        """;

    [Test]
    public void Parses_frontmatter_and_body_into_a_post()
    {
        var result = MarkdownPost.Parse(ValidPost);

        result.Errors.ShouldBeEmpty();
        var post = result.Post.ShouldNotBeNull();
        post.Id.ShouldBe("01JBX5Q7Z9K3M8N2P4R6T0V1W2");
        post.Title.ShouldBe("Learning with mind maps");
        post.Slug.ShouldBe("learning-with-mind-maps");
        post.Area.ShouldBe(ContentArea.AiDevelopment);
        post.Date.ShouldBe(new DateOnly(2026, 10, 1));
        post.Tags.ShouldBe(["learning", "notes"]);
        post.Audience.ShouldBe(Audience.Public);
        post.Status.ShouldBe(PostStatus.Published);
        post.Body.ShouldBe("## Start here\n\nBody text.");
    }

    [TestCase("id: 01JBX5Q7Z9K3M8N2P4R6T0V1W2", "id: not-a-ulid", "'id' must be a ULID.")]
    [TestCase("title: Learning with mind maps", "title: ''", "'title' is required.")]
    [TestCase("slug: learning-with-mind-maps", "slug: Learning With Spaces", "'slug' must be lowercase letters, digits and single hyphens.")]
    [TestCase("area: ai-development", "area: blog", "'area' must be one of: recipes, watches, ai-development, training.")]
    [TestCase("date: 2026-10-01", "date: October 1st", "'date' must be a date in the form YYYY-MM-DD.")]
    [TestCase("audience: public", "audience: clients", "'audience' must be 'public'.")]
    [TestCase("status: published", "status: live", "'status' must be 'published' or 'draft'.")]
    public void Rejects_an_invalid_field(string valid, string invalid, string error)
    {
        var result = MarkdownPost.Parse(ValidPost.Replace(valid, invalid));

        result.Post.ShouldBeNull();
        result.Errors.ShouldBe([error]);
    }

    [Test]
    public void Reports_every_invalid_field_at_once()
    {
        var result = MarkdownPost.Parse("---\ntitle: Only a title\n---\nBody");

        result.Errors.Count.ShouldBe(6);
    }

    [Test]
    public void Rejects_markdown_without_frontmatter()
    {
        var result = MarkdownPost.Parse("# Just a heading");

        result.Errors.ShouldBe(["The Post must start with a frontmatter block between '---' lines."]);
    }

    [Test]
    public void Rejects_frontmatter_that_is_not_yaml()
    {
        var result = MarkdownPost.Parse("---\ntitle: [unclosed\n---\nBody");

        result.Errors.Single().ShouldStartWith("The frontmatter is not valid YAML");
    }

    [Test]
    public void Defaults_missing_tags_to_none()
    {
        var result = MarkdownPost.Parse(ValidPost.Replace("tags: [learning, notes]\n", ""));

        result.Post.ShouldNotBeNull().Tags.ShouldBeEmpty();
    }

    [Test]
    public void Reads_a_draft()
    {
        var result = MarkdownPost.Parse(ValidPost.Replace("status: published", "status: draft"));

        result.Post.ShouldNotBeNull().Status.ShouldBe(PostStatus.Draft);
    }

    [Test]
    public void Accepts_windows_line_endings()
    {
        var result = MarkdownPost.Parse(ValidPost.ReplaceLineEndings("\r\n"));

        result.Post.ShouldNotBeNull().Body.ShouldBe("## Start here\n\nBody text.");
    }
}
