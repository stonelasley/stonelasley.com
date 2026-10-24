using StoneLasley.Domain.Posts;

namespace StoneLasley.Domain.UnitTests.Posts;

public class SearchTextTests
{
    private static Post Parse(string markdown) => MarkdownPost.Parse(markdown).Post!;

    [Test]
    public void Flattens_a_watch_typed_core_into_structured_text()
    {
        var post = Parse("""
            ---
            id: 01JBX5Q7Z9K3M8N2P4R6T0V1W5
            title: Submariner
            slug: sub
            area: watches
            date: 2026-08-01
            audience: public
            status: published
            make: Patek Philippe
            reference: "5711"
            modelName: Nautilus
            variants: [5711/1A]
            calibre: "26-330"
            ---
            Body.
            """);

        SearchText.Structured(post).ShouldBe("Patek Philippe 5711 Nautilus 5711/1A 26-330");
    }

    [Test]
    public void Flattens_recipe_ingredients_into_structured_text()
    {
        var post = Parse("""
            ---
            id: 01JBX5Q7Z9K3M8N2P4R6T0V1W4
            title: Oats
            slug: oats
            area: recipes
            date: 2026-09-20
            audience: public
            status: published
            prepTimeMin: 5
            cookTimeMin: 10
            servings: 2
            difficulty: Easy
            ingredients:
              - { quantity: 80, unit: g, name: rolled oats }
              - { name: cinnamon, note: Ceylon }
            ---
            Stir.
            """);

        SearchText.Structured(post).ShouldBe("rolled oats cinnamon Ceylon");
    }

    [Test]
    public void Turns_the_body_into_plain_text()
    {
        SearchText.Body("## Heading\n\nSome **bold** and a [link](https://x.test).\n\n```\ncode()\n```")
            .ShouldBe("Heading Some bold and a link. code()");
    }

    [TestCase("submariner date", "\"submariner\" OR \"date\"")]
    [TestCase("16610", "\"16610\"")]
    [TestCase("  NEAR(a b) \"quoted\" * - ^col:x  ", "\"NEAR\" OR \"a\" OR \"b\" OR \"quoted\" OR \"col\" OR \"x\"")]
    [TestCase("5711/1A", "\"5711\" OR \"1A\"")]
    [TestCase("crème brûlée", "\"crème\" OR \"brûlée\"")]
    public void Escapes_user_terms_and_or_joins_them(string query, string match)
    {
        SearchText.MatchExpression(query).ShouldBe(match);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("* ^ \"\" ()")]
    public void Has_no_match_expression_without_any_terms(string query)
    {
        SearchText.MatchExpression(query).ShouldBeNull();
    }
}
