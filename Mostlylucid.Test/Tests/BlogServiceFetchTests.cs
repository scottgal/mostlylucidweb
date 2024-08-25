using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mostlylucid.Blog;
using Mostlylucid.Blog.EntityFramework;
using Mostlylucid.EntityFramework;
using Mostlylucid.EntityFramework.Models;
using Mostlylucid.Test.Extensions;

namespace Mostlylucid.Test.Tests;

public class BlogServiceFetchTests
{
    private readonly Mock<IMostlylucidDBContext> _dbContextMock;
    private readonly IServiceProvider _serviceProvider;

    public BlogServiceFetchTests()
    {
        // 1. Setup ServiceCollection for DI
        var services = new ServiceCollection();
        // 2. Create a mock of IMostlylucidDbContext
        _dbContextMock = new Mock<IMostlylucidDBContext>();
        // 3. Register the mock of IMostlylucidDbContext into the ServiceCollection
        services.AddSingleton(_dbContextMock.Object);
        // Optionally register other services
        services.AddScoped<IBlogService, EFBlogService>(); // Example service that depends on IMostlylucidDbContext
        services.AddLogging(configure => configure.AddConsole());
        services.AddScoped<MarkdownRenderingService>();
        // 4. Build the service provider
        _serviceProvider = services.BuildServiceProvider();
    }


    private IBlogService SetupBlogService(List<BlogPostEntity>? blogPosts = null)
    {
        blogPosts ??= BlogEntityExtensions.GetBlogPostEntities(5);

        // Setup the DbSet for BlogPosts in the mock DbContext
        _dbContextMock.SetupDbSet(blogPosts, x => x.BlogPosts);

        // Resolve the IBlogService from the service provider
        return _serviceProvider.GetRequiredService<IBlogService>();
    }

    [Fact]
    public async Task TestBlogService_FailsToGetBlogsByCategory_ReturnsEmptyList()
    {
        var blogService = SetupBlogService();

        // Act
        var result = await blogService.GetPostsByCategory("BOOP");

        // Assert
        Assert.Empty(result.Posts);
    }

    [Fact]
    public async Task TestBlogService_GetBlogsByCategory_ReturnsBlogs()
    {
        var blogPosts = BlogEntityExtensions.GetBlogPostEntities(1);
        var blogService = SetupBlogService(blogPosts);

        // Act
        var result = await blogService.GetPostsByCategory("Category 1");

        // Assert
        Assert.Single(result.Posts);
    }

    [Fact]
    public async Task TestBlogServicePagination_GetBlogsByCategory_ReturnsBlogs()
    {
        var blogPosts = BlogEntityExtensions.GetBlogPostEntities(10, "en");
        var blogService = SetupBlogService(blogPosts);

        // Act
        var result = await blogService.GetPagedPosts(2, 5);

        // Assert
        Assert.Equal(5, result.Posts.Count);
    }

    [Fact]
    public async Task TestBlogServicePagination_GetBlogsByCategory_FailsBlogs()
    {
        var blogPosts = BlogEntityExtensions.GetBlogPostEntities(10, "en");
        var blogService = SetupBlogService(blogPosts);

        // Act
        var result = await blogService.GetPagedPosts(10, 5);

        // Assert
        Assert.Empty(result.Posts);
    }

    [Fact]
    public async Task TestBlogService_GetBlogsByLanguage_ReturnsBlogs()
    {
        var blogService = SetupBlogService();

        // Act
        var result = await blogService.GetPostsForLanguage(language: "es");

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task TestBlogService_GetAllBlogs_ReturnsBlogs()
    {
        var blogs = BlogEntityExtensions.GetBlogPostEntities(2);
        var blogService = SetupBlogService(blogs);
        // Act
        var result = await blogService.GetAllPosts();

        // Assert
        Assert.Equal(2, result.Count());
    }
}