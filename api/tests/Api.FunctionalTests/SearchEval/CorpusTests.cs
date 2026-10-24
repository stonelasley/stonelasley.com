using StoneLasley.Domain.Posts;

namespace StoneLasley.Api.FunctionalTests.SearchEval;

/// <summary>Cheap checks on the eval's inputs, run with the ordinary suite.</summary>
public class CorpusTests
{
    [Test]
    public void Every_corpus_post_is_a_valid_public_published_post()
    {
        var files = Corpus.Files().ToList();
        files.ShouldNotBeEmpty();
        foreach (var file in files)
        {
            var result = MarkdownPost.Parse(File.ReadAllText(file));
            result.Errors.ShouldBeEmpty(file);
            // This repo is public: a refreshed snapshot must never carry a Client's Private Post.
            result.Post!.Audience.ShouldBe(Audience.Public, file);
            result.Post.Status.ShouldBe(PostStatus.Published, file);
        }
    }

    [Test]
    public void Every_judged_slug_is_in_the_corpus()
    {
        var slugs = Corpus.Files().Select(Path.GetFileNameWithoutExtension).ToHashSet();

        Corpus.Queries().Where(q => q.Type != "none").ShouldAllBe(q => q.Judged != null && q.Judged.Count > 0);
        Corpus.Queries().SelectMany(q => q.Judged?.Keys ?? []).Where(slug => !slugs.Contains(slug)).ShouldBeEmpty();
    }
}
