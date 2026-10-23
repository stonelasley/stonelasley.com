using StoneLasley.Domain.Posts;

namespace StoneLasley.Domain.UnitTests.Posts;

public class SlugConflictsTests
{
    [Test]
    public void Reports_posts_sharing_a_slug_in_one_area()
    {
        var errors = SlugConflicts.Find(
        [
            new PostAddress("01A", ContentArea.Recipes, "oats"),
            new PostAddress("01B", ContentArea.Recipes, "oats"),
            new PostAddress("01C", ContentArea.Recipes, "eggs"),
        ]);

        errors.ShouldBe(["Posts 01A and 01B both use slug 'oats' in recipes."]);
    }

    [Test]
    public void Allows_one_slug_in_different_areas()
    {
        var errors = SlugConflicts.Find(
        [
            new PostAddress("01A", ContentArea.Recipes, "oats"),
            new PostAddress("01B", ContentArea.Training, "oats"),
        ]);

        errors.ShouldBeEmpty();
    }

    [Test]
    public void Lists_every_post_in_a_three_way_conflict()
    {
        var errors = SlugConflicts.Find(
        [
            new PostAddress("01C", ContentArea.Watches, "submariner"),
            new PostAddress("01A", ContentArea.Watches, "submariner"),
            new PostAddress("01B", ContentArea.Watches, "submariner"),
        ]);

        errors.ShouldBe(["Posts 01A, 01B and 01C all use slug 'submariner' in watches."]);
    }
}
