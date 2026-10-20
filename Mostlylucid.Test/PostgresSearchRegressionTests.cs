using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.SemanticSearch.Models;
using Mostlylucid.SemanticSearch.Services;
using Mostlylucid.Services.Blog;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared.Entities;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Test;

// Opt in against an isolated PostgreSQL database; never modify the site's database.
public sealed class PostgresRegressionFactAttribute : FactAttribute
{
    public PostgresRegressionFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MOSTLYLUCID_TEST_POSTGRES")))
            Skip = "Set MOSTLYLUCID_TEST_POSTGRES to an isolated mostlylucid_review_* database.";
    }
}

public class PostgresSearchRegressionTests
{
    [PostgresRegressionFact]
    public async Task SearchTopicChoicesTranslateToSqlAndCountOnlyVisiblePosts()
    {
        await using var db = await CreateDatabaseAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await SeedAsync(db);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var categories = await CreateSearch(db, cache).GetTopCategoriesAsync();
        Assert.Equal(3, categories.Single(category => category.Name == "Bot Detection").PostCount);
        Assert.Equal(1, categories.Single(category => category.Name == "StyloMail").PostCount);
    }

    [PostgresRegressionFact]
    public async Task SemanticResultsRespectTopicDateOperatorsAndPublicationVisibility()
    {
        await using var db = await CreateDatabaseAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await SeedAsync(db);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateSearch(db, cache, "latest-bot", "mail", "next-day", "hidden", "scheduled");
        var result = await service.HybridSearchWithPagingAsync(
            "adaptive category:\"Bot Detection\" after:2026-10-19 before:2026-10-20", "en", null, null, order: "relevance");
        Assert.False(result.NoMatchFound);
        Assert.Equal(new[] { "latest-bot", "older-bot" }, result.Data.Select(post => post.Slug).Order().ToArray());
    }

    [PostgresRegressionFact]
    public async Task NoMatchRecommendationsKeepQueryDatesAndTopicAndUseNewestFirst()
    {
        await using var db = await CreateDatabaseAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await SeedAsync(db);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var result = await CreateSearch(db, cache).HybridSearchWithPagingAsync(
            "qzxnomatch category:\"Bot Detection\" after:2026-10-19 before:2026-10-20", "en", null, null, order: "date_asc");
        Assert.True(result.NoMatchFound);
        Assert.Equal(new[] { "latest-bot", "older-bot" }, result.Data.Select(post => post.Slug));
    }

    [PostgresRegressionFact]
    public async Task UntilDateIncludesLateArticlesAndExcludesFollowingMidnight()
    {
        await using var db = await CreateDatabaseAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await SeedAsync(db);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateSearch(db, cache);
        foreach (var query in new[] { "adaptive", "qzxnomatch" })
        {
            var result = await service.HybridSearchWithPagingAsync(query, "en", new(2026, 10, 20), new(2026, 10, 20));
            Assert.Equal(new[] { "latest-bot", "mail" }, result.Data.Select(post => post.Slug).Order().ToArray());
        }
    }

    [PostgresRegressionFact]
    public async Task BlogListingUntilDateIncludesLateArticlesAndExcludesFollowingMidnight()
    {
        await using var db = await CreateDatabaseAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await SeedAsync(db);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new BlogService(db, null!, new BlogPostProcessingContext(), cache, NullLogger<BlogService>.Instance);
        var result = await service.Get(new PostListQueryModel(StartDate: new(2026, 10, 20), EndDate: new(2026, 10, 20)));
        Assert.NotNull(result);
        Assert.Equal(new[] { "latest-bot", "mail" }, result.Data.Select(post => post.Slug).Order().ToArray());
    }

    private static BlogSearchService CreateSearch(MostlylucidDbContext db, IMemoryCache cache, params string[] slugs)
    {
        var semantic = new Mock<ISemanticSearchService>();
        semantic.Setup(service => service.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(slugs.Select(slug => new SearchResult { Slug = slug, Score = .9f }).ToList());
        return new BlogSearchService(db, semantic.Object, cache);
    }

    private static async Task<MostlylucidDbContext> CreateDatabaseAsync()
    {
        var connection = Environment.GetEnvironmentVariable("MOSTLYLUCID_TEST_POSTGRES")!;
        if (!new Npgsql.NpgsqlConnectionStringBuilder(connection).Database!.StartsWith("mostlylucid_review_", StringComparison.Ordinal))
            throw new InvalidOperationException("Regression tests require an isolated mostlylucid_review_* database.");
        var db = new MostlylucidDbContext(new DbContextOptionsBuilder<MostlylucidDbContext>().UseNpgsql(connection).Options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static async Task SeedAsync(MostlylucidDbContext db)
    {
        var language = new LanguageEntity { Name = "en" };
        var bots = new CategoryEntity { Name = "Bot Detection" };
        var mail = new CategoryEntity { Name = "StyloMail" };
        db.AddRange(
            Post("latest-bot", new(2026, 10, 20, 23, 59, 59, TimeSpan.Zero), bots),
            Post("older-bot", new(2026, 10, 19, 23, 30, 0, TimeSpan.Zero), bots),
            Post("mail", new(2026, 10, 20, 16, 0, 0, TimeSpan.Zero), mail),
            Post("next-day", new(2026, 10, 21, 0, 0, 0, TimeSpan.Zero), bots),
            Post("hidden", new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero), bots, hidden: true),
            Post("scheduled", new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero), bots, scheduled: DateTimeOffset.UtcNow.AddDays(10)));
        await db.SaveChangesAsync();
        return;

        BlogPostEntity Post(string slug, DateTimeOffset published, CategoryEntity category, bool hidden = false, DateTimeOffset? scheduled = null) => new()
        {
            Title = slug, Slug = slug, LanguageEntity = language, Categories = new List<CategoryEntity> { category },
            PublishedDate = published, PlainTextContent = "Adaptive signals", HtmlContent = "<p>Adaptive signals</p>",
            ContentHash = slug, IsHidden = hidden, ScheduledPublishDate = scheduled
        };
    }
}
