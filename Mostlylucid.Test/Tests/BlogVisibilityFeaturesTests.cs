using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mostlylucid.Blog;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Services.Blog;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Services.Umami;
using Mostlylucid.Shared.Entities;
using Mostlylucid.Shared.Models;
using Mostlylucid.Test.Extensions;
using System.Globalization;

namespace Mostlylucid.Test.Tests;

/// <summary>
/// Tests for the blog visibility features: Pinned, Hidden, and Scheduled posts
/// </summary>
public class BlogVisibilityFeaturesTests
{
    private readonly Mock<IMostlylucidDBContext> _dbContextMock;
    private readonly IServiceProvider _serviceProvider;

    public BlogVisibilityFeaturesTests()
    {
        var services = new ServiceCollection();
        _dbContextMock = new Mock<IMostlylucidDBContext>();
        services.AddSingleton(_dbContextMock.Object);
        services.AddScoped<IUmamiDataSortService, UmamiDataSortFake>();
        services.AddScoped<IBlogViewService, BlogPostViewService>();
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<BlogSearchService>();
        services.AddLogging();
        services.AddScoped<BlogPostProcessingContext>();
        services.AddScoped<MarkdownRenderingService>();
        services.AddMemoryCache();

        // Mock IWebHostEnvironment
        var mockWebHostEnvironment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        mockWebHostEnvironment.Setup(m => m.WebRootPath).Returns(System.IO.Path.GetTempPath());
        mockWebHostEnvironment.Setup(m => m.ContentRootPath).Returns(System.IO.Path.GetTempPath());
        services.AddSingleton(mockWebHostEnvironment.Object);

        // Add ImageConfig
        services.AddSingleton(new Mostlylucid.Shared.Config.Markdown.ImageConfig
        {
            DefaultFormat = "webp",
            DefaultQuality = 50,
            PrimaryImageFolder = "articleimages"
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    private IBlogService SetupBlogService(List<BlogPostEntity> blogPosts)
    {
        _dbContextMock.SetupDbSet(blogPosts, x => x.BlogPosts);
        return _serviceProvider.GetRequiredService<IBlogService>();
    }

    #region MarkdownRenderingService Tests

    [Fact]
    public void TestMarkdownRendering_ExtractsPinnedTag()
    {
        // Arrange
        var markdownService = _serviceProvider.GetRequiredService<MarkdownRenderingService>();
        var markdown = @"# Test Post
<!-- category -- Test -->
<pinned/>

This is a test post.";
        var publishedDate = DateTime.UtcNow;

        // Act
        var result = markdownService.GetPageFromMarkdown(markdown, publishedDate, "test-post.md");

        // Assert
        Assert.True(result.IsPinned);
        Assert.DoesNotContain("<pinned/>", result.HtmlContent);
    }

    [Fact]
    public void TestMarkdownRendering_ExtractsHiddenTag()
    {
        // Arrange
        var markdownService = _serviceProvider.GetRequiredService<MarkdownRenderingService>();
        var markdown = @"# Test Post
<!-- category -- Test -->
<hidden/>

This is a test post.";
        var publishedDate = DateTime.UtcNow;

        // Act
        var result = markdownService.GetPageFromMarkdown(markdown, publishedDate, "test-post.md");

        // Assert
        Assert.True(result.IsHidden);
        Assert.DoesNotContain("<hidden/>", result.HtmlContent);
    }

    [Fact]
    public void TestMarkdownRendering_ExtractsScheduledTag()
    {
        // Arrange
        var markdownService = _serviceProvider.GetRequiredService<MarkdownRenderingService>();
        var scheduledDate = "2025-12-31T23:59";
        var markdown = $@"# Test Post
<!-- category -- Test -->
<scheduled datetime=""{scheduledDate}""/>

This is a test post.";
        var publishedDate = DateTime.UtcNow;

        // Act
        var result = markdownService.GetPageFromMarkdown(markdown, publishedDate, "test-post.md");

        // Assert
        Assert.NotNull(result.ScheduledPublishDate);
        Assert.Equal(DateTime.ParseExact(scheduledDate, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            result.ScheduledPublishDate.Value.DateTime);
        Assert.DoesNotContain("<scheduled", result.HtmlContent);
    }

    [Fact]
    public void TestMarkdownRendering_ExtractsAllThreeTags()
    {
        // Arrange
        var markdownService = _serviceProvider.GetRequiredService<MarkdownRenderingService>();
        var scheduledDate = "2025-12-31T23:59";
        var markdown = $@"# Test Post
<!-- category -- Test -->
<pinned/>
<hidden/>
<scheduled datetime=""{scheduledDate}""/>

This is a test post.";
        var publishedDate = DateTime.UtcNow;

        // Act
        var result = markdownService.GetPageFromMarkdown(markdown, publishedDate, "test-post.md");

        // Assert
        Assert.True(result.IsPinned);
        Assert.True(result.IsHidden);
        Assert.NotNull(result.ScheduledPublishDate);
        Assert.DoesNotContain("<pinned/>", result.HtmlContent);
        Assert.DoesNotContain("<hidden/>", result.HtmlContent);
        Assert.DoesNotContain("<scheduled", result.HtmlContent);
    }

    #endregion

    #region BlogService Hidden Posts Tests

    [Fact]
    public async Task TestBlogService_HiddenPosts_AreFilteredOut()
    {
        // Arrange
        var blogPosts = new List<BlogPostEntity>
        {
            CreateTestPost(1, "Visible Post", isHidden: false),
            CreateTestPost(2, "Hidden Post", isHidden: true),
            CreateTestPost(3, "Another Visible Post", isHidden: false)
        };
        var blogService = SetupBlogService(blogPosts);
        var query = new PostListQueryModel { Language = "en", Page = 1, PageSize = 10 };

        // Act
        var result = await blogService.Get(query);

        // Assert
        Assert.Equal(2, result.TotalItems);
        Assert.DoesNotContain(result.Data, p => p.Title == "Hidden Post");
    }

    #endregion

    #region BlogService Scheduled Posts Tests

    [Fact]
    public async Task TestBlogService_ScheduledPosts_FutureDate_AreFilteredOut()
    {
        // Arrange
        var futureDate = DateTimeOffset.UtcNow.AddDays(7);
        var blogPosts = new List<BlogPostEntity>
        {
            CreateTestPost(1, "Published Post", scheduledDate: null),
            CreateTestPost(2, "Scheduled Future Post", scheduledDate: futureDate),
            CreateTestPost(3, "Another Published Post", scheduledDate: null)
        };
        var blogService = SetupBlogService(blogPosts);
        var query = new PostListQueryModel { Language = "en", Page = 1, PageSize = 10 };

        // Act
        var result = await blogService.Get(query);

        // Assert
        Assert.Equal(2, result.TotalItems);
        Assert.DoesNotContain(result.Data, p => p.Title == "Scheduled Future Post");
    }

    [Fact]
    public async Task TestBlogService_ScheduledPosts_PastDate_AreIncluded()
    {
        // Arrange
        var pastDate = DateTimeOffset.UtcNow.AddDays(-7);
        var blogPosts = new List<BlogPostEntity>
        {
            CreateTestPost(1, "Published Post", scheduledDate: null),
            CreateTestPost(2, "Scheduled Past Post", scheduledDate: pastDate),
            CreateTestPost(3, "Another Published Post", scheduledDate: null)
        };
        var blogService = SetupBlogService(blogPosts);
        var query = new PostListQueryModel { Language = "en", Page = 1, PageSize = 10 };

        // Act
        var result = await blogService.Get(query);

        // Assert
        Assert.Equal(3, result.TotalItems);
        Assert.Contains(result.Data, p => p.Title == "Scheduled Past Post");
    }

    #endregion

    #region BlogService Pinned Posts Tests

    [Fact]
    public async Task TestBlogService_PinnedPosts_DoNotOverrideNewestFirst()
    {
        // Arrange
        var blogPosts = new List<BlogPostEntity>
        {
            CreateTestPost(1, "Regular Post 1", publishedDate: DateTime.Parse("2025-01-01"), isPinned: false),
            CreateTestPost(2, "Pinned Post", publishedDate: DateTime.Parse("2025-01-02"), isPinned: true),
            CreateTestPost(3, "Regular Post 2", publishedDate: DateTime.Parse("2025-01-03"), isPinned: false)
        };
        var blogService = SetupBlogService(blogPosts);
        var query = new PostListQueryModel { Language = "en", Page = 1, PageSize = 10, orderBy = "date", orderDir = "desc" };

        // Act
        var result = await blogService.Get(query);

        // Assert
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(new[] { "Regular Post 2", "Pinned Post", "Regular Post 1" }, result.Data.Select(post => post.Title));
    }

    [Fact]
    public async Task TestBlogService_MultiplePinnedPosts_FollowPublicationOrder()
    {
        // Arrange
        var blogPosts = new List<BlogPostEntity>
        {
            CreateTestPost(1, "Regular Post", publishedDate: DateTime.Parse("2025-01-05"), isPinned: false),
            CreateTestPost(2, "Pinned Post A", publishedDate: DateTime.Parse("2025-01-03"), isPinned: true),
            CreateTestPost(3, "Pinned Post B", publishedDate: DateTime.Parse("2025-01-04"), isPinned: true),
            CreateTestPost(4, "Another Regular Post", publishedDate: DateTime.Parse("2025-01-06"), isPinned: false)
        };
        var blogService = SetupBlogService(blogPosts);
        var query = new PostListQueryModel { Language = "en", Page = 1, PageSize = 10, orderBy = "date", orderDir = "desc" };

        // Act
        var result = await blogService.Get(query);

        // Assert
        Assert.Equal(4, result.TotalItems);
        Assert.Equal(new[] { "Another Regular Post", "Regular Post", "Pinned Post B", "Pinned Post A" },
            result.Data.Select(post => post.Title));
    }

    #endregion

    #region BlogService Combined Filters Tests

    [Fact]
    public async Task TestBlogService_CombinedFilters_HiddenAndScheduledAndPinned()
    {
        // Arrange
        var futureDate = DateTimeOffset.UtcNow.AddDays(7);
        var pastDate = DateTimeOffset.UtcNow.AddDays(-7);
        var blogPosts = new List<BlogPostEntity>
        {
            CreateTestPost(1, "Visible Regular Post", isPinned: false, isHidden: false, scheduledDate: null),
            CreateTestPost(2, "Hidden Post", isPinned: false, isHidden: true, scheduledDate: null),
            CreateTestPost(3, "Future Scheduled Post", isPinned: false, isHidden: false, scheduledDate: futureDate),
            CreateTestPost(4, "Past Scheduled Post", isPinned: false, isHidden: false, scheduledDate: pastDate),
            CreateTestPost(5, "Pinned Visible Post", isPinned: true, isHidden: false, scheduledDate: null),
            CreateTestPost(6, "Pinned Hidden Post", isPinned: true, isHidden: true, scheduledDate: null),
            CreateTestPost(7, "Pinned Future Scheduled", isPinned: true, isHidden: false, scheduledDate: futureDate)
        };
        var blogService = SetupBlogService(blogPosts);
        var query = new PostListQueryModel { Language = "en", Page = 1, PageSize = 10 };

        // Act
        var result = await blogService.Get(query);

        // Assert
        // Should only include: Visible Regular, Past Scheduled, and Pinned Visible (3 posts total)
        Assert.Equal(3, result.TotalItems);
        Assert.Contains(result.Data, p => p.Title == "Visible Regular Post");
        Assert.Contains(result.Data, p => p.Title == "Past Scheduled Post");
        Assert.Contains(result.Data, p => p.Title == "Pinned Visible Post");

    }

    #endregion

    [Fact]
    public async Task TestBlogService_DefaultOrderIsStableAcrossPagesDespitePins()
    {
        var service = SetupBlogService(new List<BlogPostEntity>
        {
            CreateTestPost(1, "Old pin", new DateTime(2025, 1, 1), isPinned: true),
            CreateTestPost(2, "Newest", new DateTime(2025, 3, 1)),
            CreateTestPost(3, "Middle", new DateTime(2025, 2, 1))
        });
        var titles = new List<string>();
        for (var page = 1; page <= 3; page++)
        {
            var result = await service.Get(new PostListQueryModel { Language = "en", Page = page, PageSize = 1 });
            titles.Add(Assert.Single(result.Data).Title);
        }
        Assert.Equal(new[] { "Newest", "Middle", "Old pin" }, titles);
    }

    #region Helper Methods

    private BlogPostEntity CreateTestPost(
        int id,
        string title,
        DateTime? publishedDate = null,
        bool isPinned = false,
        bool isHidden = false,
        DateTimeOffset? scheduledDate = null)
    {
        var lang = LanguageExtensions.GetLanguageEntity("en");
        var cat = CategoryEntityExtensions.GetCategoryEntity("Category 1");

        return new BlogPostEntity
        {
            Id = id,
            Title = title,
            Slug = title.ToLower().Replace(" ", "-"),
            HtmlContent = $"<p>Html Content for {title}</p>",
            PlainTextContent = $"PlainTextContent for {title}",
            Markdown = $"# {title}",
            PublishedDate = publishedDate ?? DateTime.Parse("2025-01-01T07:01"),
            UpdatedDate = DateTime.Parse("2025-01-01T07:01"),
            LanguageEntity = lang,
            LanguageId = lang.Id,
            Categories = new List<CategoryEntity> { cat },
            IsPinned = isPinned,
            IsHidden = isHidden,
            ScheduledPublishDate = scheduledDate
        };
    }

    #endregion
}
