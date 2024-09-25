using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mostlylucid.Blog;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Services.Blog;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Services.Umami;
using Mostlylucid.Test.Extensions;

namespace Mostlylucid.Test.Tests;

public class BlogServiceSaveTests
{
    private readonly Mock<IMostlylucidDBContext> _dbContextMock;
    private readonly IServiceProvider _serviceProvider;

    public BlogServiceSaveTests()
    {
        // 1. Setup ServiceCollection for DI
        var services = new ServiceCollection();

        // 2. Create a mock of IMostlylucidDbContext
        _dbContextMock = new Mock<IMostlylucidDBContext>();

        // 3. Register the mock of IMostlylucidDbContext into the ServiceCollection
        services.AddSingleton(_dbContextMock.Object);
        services.AddScoped<IUmamiDataSortService, UmamiDataSortFake>();
        // Optionally register other services
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IBlogViewService, BlogPostViewService>(); // Example service that depends on IMostlylucidDbContext
        services.AddLogging(configure => configure.AddConsole());
        services.AddScoped<MarkdownRenderingService>();
        // 4. Build the service provider
        _serviceProvider = services.BuildServiceProvider();
    }

    private IBlogViewService SetupBlogService()
    {
        var blogPosts = BlogEntityExtensions.GetBlogPostEntities(1);
        _dbContextMock.SetupDbSet(blogPosts, x => x.BlogPosts);


        var languages = LanguageExtensions.GetLanguageEntities(1);

        _dbContextMock.SetupDbSet(languages, x => x.Languages);

        // Resolve the IBlogService from the service provider
        return _serviceProvider.GetRequiredService<IBlogViewService>();
    }

    [Fact]
    public async Task SaveBlogPost()
    {
        var blogService = SetupBlogService();


        // Act
        await blogService.SavePost("Test Title", "en", "#Test Category");

        var cancellationToken = CancellationToken.None;
        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
    }
}