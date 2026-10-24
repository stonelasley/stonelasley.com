using StoneLasley.Domain.Posts;

namespace StoneLasley.Domain.UnitTests.Posts;

public class ChunkerTests
{
    [Test]
    public void Splits_on_h2_and_prefixes_each_chunk_with_the_title_and_heading()
    {
        var chunks = Chunker.Split("Oats", "Intro words.\n\n## Method\n\nStir it.\n\n## Storage\n\nFridge, three days.");

        chunks.ShouldBe(
        [
            "Oats\n\nIntro words.",
            "Oats — Method\n\nStir it.",
            "Oats — Storage\n\nFridge, three days.",
        ]);
    }

    [Test]
    public void Breaks_a_long_section_at_paragraph_boundaries()
    {
        var paragraph = string.Join(' ', Enumerable.Repeat("word", 250)); // ~250 tokens
        var chunks = Chunker.Split("Long", $"## Part\n\n{paragraph}\n\n{paragraph}\n\n{paragraph}");

        chunks.Count.ShouldBe(3);
        chunks.ShouldAllBe(chunk => chunk.StartsWith("Long — Part\n\n", StringComparison.Ordinal));
        chunks.ShouldAllBe(chunk => Chunker.EstimateTokens(chunk) <= Chunker.MaxTokens + 10);
    }

    [Test]
    public void Keeps_short_paragraphs_of_a_section_together()
    {
        var chunks = Chunker.Split("T", "## S\n\nOne.\n\nTwo.\n\nThree.");

        chunks.ShouldBe(["T — S\n\nOne.\n\nTwo.\n\nThree."]);
    }

    [Test]
    public void Skips_empty_sections()
    {
        Chunker.Split("T", "## Empty\n\n## Full\n\nText.").ShouldBe(["T — Full\n\nText."]);
    }

    [Test]
    public void A_post_with_no_body_is_one_title_chunk()
    {
        Chunker.Split("Just a title", "").ShouldBe(["Just a title"]);
    }

    [Test]
    public void A_single_paragraph_longer_than_the_limit_is_its_own_chunk()
    {
        var huge = string.Join(' ', Enumerable.Repeat("word", 900));

        Chunker.Split("T", huge).ShouldBe([$"T\n\n{huge}"]);
    }
}
