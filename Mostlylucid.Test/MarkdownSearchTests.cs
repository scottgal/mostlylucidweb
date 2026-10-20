using Mostlylucid.Blog.Markdown;
using Mostlylucid.Services.Blog;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Test;

[CollectionDefinition("Markdown search cache", DisableParallelization = true)]
public class MarkdownSearchCacheCollection;

[Collection("Markdown search cache")]
public class MarkdownSearchTests : IDisposable
{
    private readonly Dictionary<(string slug, string lang), BlogPostDto> _original = new(PageCacheHelper.GetPageCache());
    private readonly MarkdownBlogSearchService _search = new();

    public MarkdownSearchTests()
    {
        var posts = new[]
        {
            Post("bots", "Bot detection in ASP.NET", "Adaptive traffic signals", "Bot Detection", new(2026, 10, 1)),
            Post("mail", "Mail filtering", "Adaptive spam signals", "StyloMail", new(2026, 9, 1)),
            Post("hidden", "Hidden adaptive signals", "Traffic", "Bot Detection", new(2026, 10, 2), hidden: true),
            Post("scheduled", "Scheduled adaptive signals", "Traffic", "Bot Detection", new(2026, 10, 3), scheduled: DateTimeOffset.UtcNow.AddDays(10))
        };
        PageCacheHelper.SetPageCache(posts.ToDictionary(post => (post.Slug, post.Language)));
    }

    [Fact]
    public async Task Search_MatchesBodyAndExcludesUnpublishedPosts()
    {
        var result = await _search.HybridSearchWithPagingAsync("adaptive", "en", null, null);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(new[] { "bots", "mail" }, result.Data.Select(post => post.Slug));
    }

    [Fact]
    public async Task Search_CombinesMultiwordTagDatesAndSort()
    {
        var result = await _search.HybridSearchWithPagingAsync("adaptive category:\"Bot Detection\"", "en", new(2026, 10, 1), new(2026, 10, 20), order: "title_asc");
        Assert.Equal("bots", Assert.Single(result.Data).Slug);
        Assert.Empty((await _search.HybridSearchWithPagingAsync("adaptive", "en", null, new(2026, 8, 1))).Data);
    }

    [Fact]
    public async Task Search_SupportsTechnicalTermsAndPaging()
    {
        Assert.Equal("bots", Assert.Single((await _search.GetSearchResultForComplete("ASP.NET"))).Slug);
        var result = await _search.HybridSearchWithPagingAsync("adaptive", "en", null, null, page: 2, pageSize: 1);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal("mail", Assert.Single(result.Data).Slug);
    }

    [Fact]
    public async Task Search_WhenNothingMatchesSuggestsNewestPublishedPostsRegardlessOfSort()
    {
        var result = await _search.HybridSearchWithPagingAsync("qzxnomatch", "en", null, null, order: "date_asc");
        Assert.True(result.NoMatchFound);
        Assert.Equal(new[] { "bots", "mail" }, result.Data.Select(post => post.Slug));
        Assert.Empty(await _search.GetSearchResultForComplete("qzxnomatch"));
    }

    [Fact]
    public async Task Search_NoMatchSuggestionsRespectTagAndDates()
    {
        var result = await _search.HybridSearchWithPagingAsync("qzxnomatch category:\"Bot Detection\"", "en", new(2026, 10, 1), null);
        Assert.True(result.NoMatchFound);
        Assert.Equal("bots", Assert.Single(result.Data).Slug);
    }

    [Fact]
    public void Parser_PreservesMultiwordCategoriesWithoutAddingSearchTerms()
    {
        var result = new SearchQueryParser().Parse("signals category:\"Bot Detection\"");
        Assert.Equal(new[] { "Bot Detection" }, result.Categories);
        Assert.Equal(new[] { "signals" }, result.IncludeTerms);
    }

    [Fact]
    public async Task Listings_DefaultToNewestFirstAcrossPagesAndCategoriesDespitePins()
    {
        var posts = new[]
        {
            Post("old", "A old pinned", "Signals", "Bot Detection", new(2026, 1, 1)),
            Post("new", "Z newest", "Signals", "Bot Detection", new(2026, 10, 1)),
            Post("middle", "M middle", "Signals", "Bot Detection", new(2026, 6, 1))
        };
        posts[0].IsPinned = true;
        PageCacheHelper.SetPageCache(posts.ToDictionary(post => (post.Slug, post.Language)));
        var service = new MarkdownBlogViewService(new Mostlylucid.Shared.Config.Markdown.MarkdownConfig(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MarkdownBlogViewService>.Instance);
        var paged = new List<string>();
        var categories = new List<string>();
        for (var page = 1; page <= 3; page++)
        {
            paged.Add(Assert.Single((await service.GetPagedPosts(page, 1)).Data).Slug);
            categories.Add(Assert.Single((await service.GetPostsByCategory("Bot Detection", page, 1)).Data).Slug);
        }
        Assert.Equal(new[] { "new", "middle", "old" }, paged);
        Assert.Equal(paged, categories);
        Assert.Equal("old", Assert.Single((await service.GetPagedPosts(1, 1, orderBy: "date", orderDir: "asc")).Data).Slug);
    }

    private static BlogPostDto Post(string slug, string title, string body, string category, DateTime date, bool hidden = false, DateTimeOffset? scheduled = null) =>
        new() { Slug = slug, Title = title, PlainTextContent = body, Categories = new[] { category }, Language = "en", Languages = new[] { "en" }, PublishedDate = date, IsHidden = hidden, ScheduledPublishDate = scheduled };

    public void Dispose() => PageCacheHelper.SetPageCache(_original);
}
