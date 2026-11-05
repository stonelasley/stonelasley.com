using StoneLasley.Domain.Posts;

namespace StoneLasley.Domain.UnitTests.Posts;

public class MakesFileTests
{
    [Test]
    public void Reads_names_and_aliases()
    {
        var result = MakesFile.Parse("""
            - name: Rolex
            - name: TAG Heuer
              aliases: [TAG-Heuer, TagHeuer]
            """);

        result.Errors.ShouldBeEmpty();
        result.Makes.ShouldBe([new WatchMake("Rolex", []), new WatchMake("TAG Heuer", ["TAG-Heuer", "TagHeuer"])], ignoreOrder: false);
    }

    [Test]
    public void An_empty_file_declares_no_makes() =>
        MakesFile.Parse("").Makes.ShouldBeEmpty();

    [TestCase("- name: ''", "Make 1: 'name' is required.")]
    [TestCase("- aliases: [Rolex]", "Make 1: 'name' is required.")]
    [TestCase("- name: Rolex\n  aliases: ['']", "Make Rolex: aliases must not be empty.")]
    [TestCase("- name: Rolex\n- name: rolex", "'rolex' is declared more than once.")]
    [TestCase("- name: TAG Heuer\n  aliases: [Tag Heuer]", "'Tag Heuer' is declared more than once.")]
    [TestCase("- name: Rolex\n- name: Tudor\n  aliases: [ROLEX]", "'ROLEX' is declared more than once.")]
    [TestCase("- name: [Rolex", "The makes file is not valid YAML")]
    public void Rejects_an_invalid_file(string yaml, string error)
    {
        var result = MakesFile.Parse(yaml);

        result.Makes.ShouldBeEmpty();
        result.Errors.ShouldHaveSingleItem().ShouldStartWith(error);
    }

    [TestCase("TAG Heuer")]
    [TestCase("tag heuer")]
    [TestCase("Tag Heuer")]
    [TestCase("TAG-Heuer")]
    public void Resolves_a_name_or_alias_to_the_name_ignoring_case(string written)
    {
        var makes = new WatchMakes([new WatchMake("Rolex", []), new WatchMake("TAG Heuer", ["TAG-Heuer"])]);

        makes.Resolve(written).ShouldBe("TAG Heuer");
    }

    [Test]
    public void Resolves_a_name_with_accents() =>
        new WatchMakes([new WatchMake("A. Lange & Söhne", [])]).Resolve("a. lange & SÖHNE").ShouldBe("A. Lange & Söhne");

    [Test]
    public void An_undeclared_make_does_not_resolve() =>
        new WatchMakes([new WatchMake("Rolex", [])]).Resolve("Acme").ShouldBeNull();
}
