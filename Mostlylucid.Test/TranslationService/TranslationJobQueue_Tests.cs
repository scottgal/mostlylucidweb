using Mostlylucid.MarkdownTranslator;

namespace Mostlylucid.Test.TranslationService;

public class TranslationJobQueue_Tests
{
    private static PageTranslationModel Post(string fileName, string language, string markdown) => new()
    {
        OriginalFileName = fileName,
        Language = language,
        OriginalMarkdown = markdown,
        Persist = true
    };

    private static PageTranslationModel AdHoc(string language) => new()
    {
        OriginalFileName = "",
        Language = language,
        OriginalMarkdown = "some text",
        Persist = false
    };

    [Fact]
    public void ReQueueingSamePostAndLanguage_SupersedesTheOlderJob()
    {
        var queue = new TranslationJobQueue();

        var stale = queue.Enqueue(Post("post.md", "es", "version one"));
        var fresh = queue.Enqueue(Post("post.md", "es", "version two"));

        // The stale job must not run - it carries an outdated snapshot of the markdown and would
        // otherwise overwrite the translation produced from the newer content.
        Assert.False(queue.TryClaim(stale));
        Assert.True(queue.TryClaim(fresh));
        Assert.True(stale.Cancellation.IsCancellationRequested);
        Assert.False(fresh.Cancellation.IsCancellationRequested);
    }

    [Fact]
    public async Task SupersededJob_CompletesItsAwaiterRatherThanHanging()
    {
        var queue = new TranslationJobQueue();

        var stale = queue.Enqueue(Post("post.md", "es", "version one"));
        queue.Enqueue(Post("post.md", "es", "version two"));

        var completion = await stale.Completion.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(completion.Complete);
        Assert.Null(completion.TranslatedMarkdown);
        Assert.Equal("es", completion.Language);
    }

    [Fact]
    public void DifferentLanguagesForSamePost_DoNotSupersedeEachOther()
    {
        var queue = new TranslationJobQueue();

        var spanish = queue.Enqueue(Post("post.md", "es", "content"));
        var french = queue.Enqueue(Post("post.md", "fr", "content"));

        Assert.True(queue.TryClaim(spanish));
        Assert.True(queue.TryClaim(french));
    }

    [Fact]
    public void DifferentPosts_DoNotSupersedeEachOther()
    {
        var queue = new TranslationJobQueue();

        var first = queue.Enqueue(Post("one.md", "es", "content"));
        var second = queue.Enqueue(Post("two.md", "es", "content"));

        Assert.True(queue.TryClaim(first));
        Assert.True(queue.TryClaim(second));
    }

    [Fact]
    public void AdHocTranslations_AreIndependentAndNeverSupersedeEachOther()
    {
        var queue = new TranslationJobQueue();

        // Two API callers translating unrelated text must both get their answer.
        var first = queue.Enqueue(AdHoc("es"));
        var second = queue.Enqueue(AdHoc("es"));

        Assert.True(queue.TryClaim(first));
        Assert.True(queue.TryClaim(second));
    }

    [Fact]
    public void ReleasedJob_NoLongerClaimable()
    {
        var queue = new TranslationJobQueue();

        var job = queue.Enqueue(Post("post.md", "es", "content"));
        Assert.True(queue.TryClaim(job));

        queue.Release(job);

        Assert.False(queue.TryClaim(job));
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public void JobQueuedAfterRelease_IsClaimable()
    {
        var queue = new TranslationJobQueue();

        var first = queue.Enqueue(Post("post.md", "es", "content"));
        queue.Release(first);

        var second = queue.Enqueue(Post("post.md", "es", "newer content"));

        Assert.True(queue.TryClaim(second));
        Assert.False(second.Cancellation.IsCancellationRequested);
    }

    [Fact]
    public async Task AllQueuedJobs_AreDelivered()
    {
        var queue = new TranslationJobQueue();

        queue.Enqueue(Post("one.md", "es", "content"));
        queue.Enqueue(Post("two.md", "fr", "content"));
        queue.Enqueue(Post("three.md", "de", "content"));
        queue.Complete();

        var delivered = new List<TranslationJob>();
        await foreach (var job in queue.ReadAllAsync(CancellationToken.None)) delivered.Add(job);

        Assert.Equal(3, delivered.Count);
    }

    [Fact]
    public async Task Complete_DoesNotLeavePendingAwaitersHanging()
    {
        var queue = new TranslationJobQueue();

        var job = queue.Enqueue(Post("post.md", "es", "content"));
        queue.Complete();

        // Shutdown must not strand callers awaiting a translation that will never run.
        var completion = await job.Completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(completion.Complete);
    }
}
