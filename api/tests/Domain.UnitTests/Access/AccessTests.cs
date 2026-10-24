using StoneLasley.Domain.Access;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Domain.UnitTests.Access;

public class AudienceTests
{
    private const string Client = "01JBX5Q7Z9K3M8N2P4R6T0V1WA";

    private static string Post(string audience) => $"""
        ---
        id: 01JBX5Q7Z9K3M8N2P4R6T0V1W2
        title: T
        slug: t
        area: training
        date: 2026-10-01
        audience: {audience}
        status: published
        ---
        Body.
        """;

    [Test]
    public void Reads_public_clients_and_one_client()
    {
        MarkdownPost.Parse(Post("public")).Post!.Audience.ShouldBe(Audience.Public);
        MarkdownPost.Parse(Post("clients")).Post!.Audience.ShouldBe(Audience.Clients);
        MarkdownPost.Parse(Post(Client)).Post!.Audience.ShouldBe(Audience.ForClient(Client));
    }

    [Test]
    public void Rejects_anything_else()
    {
        MarkdownPost.Parse(Post("everyone")).Errors.ShouldBe(["'audience' must be public, clients, or a Client's ULID."]);
    }

    [Test]
    public void Only_public_posts_are_public()
    {
        Audience.Public.IsPublic.ShouldBeTrue();
        Audience.Clients.IsPublic.ShouldBeFalse();
        Audience.ForClient(Client).IsPublic.ShouldBeFalse();
    }

    [Test]
    public void A_client_reads_the_shared_library_and_their_own_posts_only()
    {
        const string other = "01JBX5Q7Z9K3M8N2P4R6T0V1WB";

        Audience.Clients.Admits(Client).ShouldBeTrue();
        Audience.ForClient(Client).Admits(Client).ShouldBeTrue();
        Audience.ForClient(other).Admits(Client).ShouldBeFalse();
    }

    [Test]
    public void Round_trips_through_its_stored_form()
    {
        foreach (var audience in new[] { Audience.Public, Audience.Clients, Audience.ForClient(Client) })
        {
            Audience.FromStored(audience.Stored).ShouldBe(audience);
        }
        Audience.ForClient(Client).Stored.ShouldBe($"client:{Client}");
    }
}

public class ClientsFileTests
{
    [Test]
    public void Parses_clients_by_ulid_and_display_name()
    {
        var result = ClientsFile.Parse("""
            - id: 01JBX5Q7Z9K3M8N2P4R6T0V1WA
              name: Alex Example
            - id: 01JBX5Q7Z9K3M8N2P4R6T0V1WB
              name: Sam Sample
            """);

        result.Errors.ShouldBeEmpty();
        result.Clients.ShouldBe(
        [
            new Client("01JBX5Q7Z9K3M8N2P4R6T0V1WA", "Alex Example"),
            new Client("01JBX5Q7Z9K3M8N2P4R6T0V1WB", "Sam Sample"),
        ]);
    }

    [Test]
    public void Reports_bad_ulids_missing_names_and_duplicates()
    {
        var result = ClientsFile.Parse("""
            - id: nope
              name: A
            - id: 01JBX5Q7Z9K3M8N2P4R6T0V1WA
            - id: 01JBX5Q7Z9K3M8N2P4R6T0V1WB
              name: B
            - id: 01JBX5Q7Z9K3M8N2P4R6T0V1WB
              name: C
            """);

        result.Errors.ShouldBe(
        [
            "Client 1: 'id' must be a ULID.",
            "Client 2: 'name' is required.",
            "Client 01JBX5Q7Z9K3M8N2P4R6T0V1WB is declared more than once.",
        ]);
    }

    [Test]
    public void An_empty_file_has_no_clients()
    {
        ClientsFile.Parse("").Clients.ShouldBeEmpty();
    }
}

public class AccessCodeTests
{
    [Test]
    public void A_new_code_carries_120_random_bits_in_readable_groups()
    {
        var code = AccessCode.New();

        code.Length.ShouldBe(29); // 24 Crockford characters (120 bits) in six dash-separated groups of four
        code.Split('-').ShouldAllBe(group => group.Length == 4);
        AccessCode.New().ShouldNotBe(code);
    }

    [Test]
    public void Hashing_ignores_case_dashes_and_spaces_and_crockford_lookalikes()
    {
        var code = AccessCode.New();

        AccessCode.Hash(code.ToLowerInvariant().Replace("-", " ")).ShouldBe(AccessCode.Hash(code));
        AccessCode.Hash("ABCD-EFGH-JKMN-PQRS-TVWX-YZ01").ShouldBe(AccessCode.Hash("abcd efgh jkmn pqrs tvwx yzOl"));
    }

    [Test]
    public void Different_codes_hash_differently()
    {
        AccessCode.Hash(AccessCode.New()).ShouldNotBe(AccessCode.Hash(AccessCode.New()));
    }
}
