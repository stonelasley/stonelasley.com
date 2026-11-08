using StoneLasley.Domain.Posts;

namespace StoneLasley.Domain.UnitTests.Posts;

public class MakePostConflictsTests
{
    private static MakePostAddress MakePost(string id, string slug, string make) => new(new PostAddress(id, ContentArea.Watches, slug), make);

    [Test]
    public void Reports_two_make_posts_for_one_make()
    {
        var errors = MakePostConflicts.Find([MakePost("01B", "rolex-history", "Rolex"), MakePost("01A", "rolex", "Rolex")]);

        errors.ShouldBe(["Posts rolex and rolex-history are both Make Posts for Rolex."]);
    }

    [Test]
    public void Allows_one_make_post_per_make()
    {
        var errors = MakePostConflicts.Find([MakePost("01A", "rolex", "Rolex"), MakePost("01B", "seiko", "Seiko")]);

        errors.ShouldBeEmpty();
    }

    [Test]
    public void Lists_every_post_in_a_three_way_conflict()
    {
        var errors = MakePostConflicts.Find(
            [MakePost("01C", "seiko-c", "Seiko"), MakePost("01A", "seiko-a", "Seiko"), MakePost("01B", "seiko-b", "Seiko")]);

        errors.ShouldBe(["Posts seiko-a, seiko-b and seiko-c are all Make Posts for Seiko."]);
    }
}
