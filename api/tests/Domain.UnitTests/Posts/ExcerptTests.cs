using StoneLasley.Domain.Posts;

namespace StoneLasley.Domain.UnitTests.Posts;

public class ExcerptTests
{
    [Test]
    public void Is_the_first_paragraph_as_plain_text()
    {
        Excerpt.From("A **bold** start with [a link](https://example.com) and `code`.\n\nSecond paragraph.")
            .ShouldBe("A bold start with a link and code.");
    }

    [Test]
    public void Skips_headings_images_tables_code_and_lists()
    {
        var markdown = """
            ## Heading

            ![photo](img.jpg)

            | a | b |
            | - | - |

            ```ts
            const x = 1;
            ```

            - a list item

            > a quote

            The real opening.
            """;

        Excerpt.From(markdown).ShouldBe("The real opening.");
    }

    [Test]
    public void Skips_a_code_block_with_blank_lines_inside()
    {
        Excerpt.From("```\nfirst\n\nsecond\n```\n\nAfter the code.").ShouldBe("After the code.");
    }

    [TestCase("Multiply 2 * 3 * 4 here.", "Multiply 2 * 3 * 4 here.")]
    [TestCase("Call snake_case_names freely.", "Call snake_case_names freely.")]
    [TestCase("See [Foo](https://en.wikipedia.org/wiki/Foo_(bar)) now.", "See Foo now.")]
    [TestCase("Nested **bold *and* italic** text.", "Nested bold and italic text.")]
    [TestCase("1986. The year it began.", "1986. The year it began.")]
    [TestCase("An _italic_ and __strong__ word.", "An italic and strong word.")]
    [TestCase("# Title\nThe paragraph under it.", "The paragraph under it.")]
    public void Keeps_real_world_prose_intact(string markdown, string excerpt)
    {
        Excerpt.From(markdown).ShouldBe(excerpt);
    }

    [Test]
    public void Never_cuts_through_an_emoji()
    {
        var excerpt = Excerpt.From(new string('a', 158) + "😀😀");

        char.IsHighSurrogate(excerpt[^2]).ShouldBeFalse();
    }

    [Test]
    public void Joins_a_wrapped_paragraph_into_one_line()
    {
        Excerpt.From("One line\nwrapped onto two.").ShouldBe("One line wrapped onto two.");
    }

    [Test]
    public void Cuts_a_long_paragraph_at_a_word_with_an_ellipsis()
    {
        var excerpt = Excerpt.From(string.Join(' ', Enumerable.Repeat("word", 60)));

        excerpt.Length.ShouldBeLessThanOrEqualTo(160);
        excerpt.ShouldEndWith("word…");
    }

    [Test]
    public void Is_empty_when_there_is_no_prose()
    {
        Excerpt.From("## Only a heading").ShouldBe("");
    }

    [Test]
    public void Parses_posts_with_their_excerpt()
    {
        var result = MarkdownPost.Parse("""
            ---
            id: 01JBX5Q7Z9K3M8N2P4R6T0V1W2
            title: T
            slug: t
            area: training
            date: 2026-10-01
            audience: public
            status: published
            ---
            ## Intro

            Lift heavy, *recover* well.
            """);

        result.Post.ShouldNotBeNull().Excerpt.ShouldBe("Lift heavy, recover well.");
    }
}
