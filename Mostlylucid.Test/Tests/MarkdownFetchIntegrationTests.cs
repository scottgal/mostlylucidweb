using Markdig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Markdig.FetchExtension;
using Mostlylucid.Markdig.FetchExtension.Models;
using Mostlylucid.Markdig.FetchExtension.Services;
using Mostlylucid.Shared.Entities;
using Mostlylucid.Test.Extensions;
using System.Net;

namespace Mostlylucid.Test.Tests;

/// <summary>
/// Integration tests for the complete markdown fetch feature
/// </summary>
public class MarkdownFetchIntegrationTests
{
    private readonly Mock<IMostlylucidDBContext> _dbContextMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly IServiceProvider _serviceProvider;

    public MarkdownFetchIntegrationTests()
    {
        var services = new ServiceCollection();
        _dbContextMock = new Mock<IMostlylucidDBContext>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        services.AddSingleton(_dbContextMock.Object);
        services.AddSingleton(_httpClientFactoryMock.Object);
        services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Mock IWebHostEnvironment
        var mockWebHostEnvironment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        mockWebHostEnvironment.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());
        mockWebHostEnvironment.Setup(m => m.ContentRootPath).Returns(Path.GetTempPath());
        services.AddSingleton(mockWebHostEnvironment.Object);

        // Add ImageConfig
        services.AddSingleton(new Mostlylucid.Shared.Config.Markdown.ImageConfig
        {
            DefaultFormat = "webp",
            DefaultQuality = 50,
            PrimaryImageFolder = "articleimages"
        });

        // Add BlogPostProcessingContext - required by MarkdownFetchService and MarkdownRenderingService
        services.AddScoped<Mostlylucid.Services.Blog.BlogPostProcessingContext>();
        services.AddScoped<IMarkdownFetchService, MarkdownFetchService>();
        services.AddScoped<MarkdownRenderingService>();

        _serviceProvider = services.BuildServiceProvider();
        // Configure fetch extension with the service provider
        FetchMarkdownExtension.ConfigureServiceProvider(_serviceProvider);
    }

    [Fact]
    public async Task EndToEnd_FetchAndRenderMarkdown()
    {
        // Arrange
        var remoteUrl = "https://example.com/remote-content.md";
        var remoteMarkdown = @"
## Remote Heading

This content was fetched from a remote source.

- Remote item 1
- Remote item 2

**Bold remote text**
";

        var blogMarkdown = $@"
# My Blog Post

Here is some local content.

<fetch markdownurl=""{remoteUrl}"" pollfrequency=""12h""/>

More local content after the fetch.
";

        // Setup HTTP mock
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(remoteMarkdown)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Setup database
        var fetchEntities = new List<MarkdownFetchEntity>();
        _dbContextMock.SetupDbSet(fetchEntities, x => x.MarkdownFetches);

        // Use MarkdownRenderingService which does the preprocessing
        var renderingService = _serviceProvider.GetRequiredService<MarkdownRenderingService>();

        // Act
        var result = renderingService.GetPageFromMarkdown(blogMarkdown, DateTime.Now, "test.md");
        var html = result.HtmlContent;

        // Assert
        Assert.NotNull(html);
        Assert.Equal("My Blog Post", result.Title); // Title is extracted, not part of body HTML
        Assert.Contains("<h2", html); // Remote heading
        Assert.Contains("local content", html);
        Assert.Contains("remote source", html);
        Assert.Contains("<strong>Bold remote text</strong>", html);
        Assert.Contains("Remote item 1", html);

        // Cached with a null BlogPostId - fetches are persisted even without a blog post
        _dbContextMock.Verify(x => x.MarkdownFetches.Add(It.Is<MarkdownFetchEntity>(e => e.BlogPostId == null)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EndToEnd_MultipleFetches_InSameDocument()
    {
        // Arrange
        var url1 = "https://example.com/intro.md";
        var url2 = "https://example.com/outro.md";
        var content1 = "## Introduction\n\nThis is the intro.";
        var content2 = "## Conclusion\n\nThis is the outro.";

        var blogMarkdown = $@"
# My Blog Post

<fetch markdownurl=""{url1}"" pollfrequency=""6h""/>

Some middle content.

<fetch markdownurl=""{url2}"" pollfrequency=""24h""/>
";

        // Setup HTTP mock to return different content based on URL
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url1),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content1)
            })
            .Verifiable();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url2),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content2)
            })
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handlerMock.Object)); // Return new instance each time

        var fetchEntities = new List<MarkdownFetchEntity>();
        _dbContextMock.SetupDbSet(fetchEntities, x => x.MarkdownFetches);

        // Use MarkdownRenderingService which does the preprocessing
        var renderingService = _serviceProvider.GetRequiredService<MarkdownRenderingService>();

        // Act
        var result = renderingService.GetPageFromMarkdown(blogMarkdown, DateTime.Now, "test.md");
        var html = result.HtmlContent;

        // Assert
        Assert.Contains("Introduction", html);
        Assert.Contains("Conclusion", html);
        Assert.Contains("This is the intro", html);
        Assert.Contains("This is the outro", html);
        Assert.Contains("middle content", html);

        // Verify NO database entities were created (blogPostId = 0)
        _dbContextMock.Verify(x => x.MarkdownFetches.Add(It.Is<MarkdownFetchEntity>(e => e.BlogPostId == null)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EndToEnd_WithoutBlogPostId_SkipsCache()
    {
        // Arrange
        var url = "https://example.com/content.md";
        var remoteContent = "## Fresh Content\n\nThis is freshly fetched.";

        // Setup HTTP mock to return content
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(remoteContent)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var fetchEntities = new List<MarkdownFetchEntity>();
        _dbContextMock.SetupDbSet(fetchEntities, x => x.MarkdownFetches);

        var blogMarkdown = $@"
# Blog Post

<fetch markdownurl=""{url}"" pollfrequency=""12h""/>
";

        // Use MarkdownRenderingService which does the preprocessing
        var renderingService = _serviceProvider.GetRequiredService<MarkdownRenderingService>();

        // Act
        var result = renderingService.GetPageFromMarkdown(blogMarkdown, DateTime.Now, "test.md");
        var html = result.HtmlContent;

        // Assert
        Assert.Contains("Fresh Content", html);
        Assert.Contains("This is freshly fetched", html);

        // Verify HTTP call was made (no cache check with blogPostId = 0)
        _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.AtLeastOnce);

        // Cached with a null BlogPostId - fetches are persisted even without a blog post
        _dbContextMock.Verify(x => x.MarkdownFetches.Add(It.Is<MarkdownFetchEntity>(e => e.BlogPostId == null)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EndToEnd_FetchFailure_ShowsComment()
    {
        // Arrange
        var url = "https://example.com/notfound.md";

        var blogMarkdown = $@"
# Blog Post

<fetch markdownurl=""{url}"" pollfrequency=""12h""/>
";

        // Setup HTTP mock to return 404
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("Not Found")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var fetchEntities = new List<MarkdownFetchEntity>();
        _dbContextMock.SetupDbSet(fetchEntities, x => x.MarkdownFetches);

        // Use MarkdownRenderingService which does the preprocessing
        var renderingService = _serviceProvider.GetRequiredService<MarkdownRenderingService>();

        // Act
        var result = renderingService.GetPageFromMarkdown(blogMarkdown, DateTime.Now, "test.md");
        var html = result.HtmlContent;

        // Assert
        Assert.Contains("<!--", html); // Should contain HTML comment
        Assert.Contains("Failed to fetch", html);
        Assert.Contains(url, html);
    }

    [Fact]
    public async Task EndToEnd_NestedMarkdownFeatures_RenderCorrectly()
    {
        // Arrange
        var url = "https://example.com/complex.md";
        var complexMarkdown = @"
## Complex Content

Here's a [link](https://example.com) and some `inline code`.

```csharp
var x = 42;
Console.WriteLine(x);
```

| Column 1 | Column 2 |
|----------|----------|
| Cell 1   | Cell 2   |

> This is a blockquote
";

        // Setup HTTP mock to return complex markdown
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(complexMarkdown)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var fetchEntities = new List<MarkdownFetchEntity>();
        _dbContextMock.SetupDbSet(fetchEntities, x => x.MarkdownFetches);

        var blogMarkdown = $@"# Test Post

<fetch markdownurl=""{url}"" pollfrequency=""12h""/>";

        // Use MarkdownRenderingService which does the preprocessing
        var renderingService = _serviceProvider.GetRequiredService<MarkdownRenderingService>();

        // Act
        var result = renderingService.GetPageFromMarkdown(blogMarkdown, DateTime.Now, "test.md");
        var html = result.HtmlContent;

        // Assert
        Assert.Contains("<a href=", html);
        Assert.Contains("<code>", html);
        Assert.Contains("<table>", html);
        Assert.Contains("<blockquote>", html);
        Assert.Contains("var x = 42", html);
    }

    [Fact]
    public async Task EndToEnd_FetchTag_WithAdditionalAttributes()
    {
        // Arrange
        var url = "https://example.com/content.md";
        var remoteMarkdown = "## Remote Content\n\nThis is fetched content.";

        var blogMarkdown = $@"
# Blog Post

<fetch class=""hidden"" markdownurl=""{url}"" pollfrequency=""2h""/>

More content.
";

        // Setup HTTP mock
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(remoteMarkdown)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var fetchEntities = new List<MarkdownFetchEntity>();
        _dbContextMock.SetupDbSet(fetchEntities, x => x.MarkdownFetches);

        // Use MarkdownRenderingService which does the preprocessing
        var renderingService = _serviceProvider.GetRequiredService<MarkdownRenderingService>();

        // Act
        var result = renderingService.GetPageFromMarkdown(blogMarkdown, DateTime.Now, "test.md");
        var html = result.HtmlContent;

        // Assert
        Assert.Contains("Remote Content", html);
        Assert.Contains("This is fetched content", html);
        Assert.Contains("More content", html);

        // Cached with a null BlogPostId - fetches are persisted even without a blog post
        _dbContextMock.Verify(x => x.MarkdownFetches.Add(It.Is<MarkdownFetchEntity>(e => e.BlogPostId == null)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EndToEnd_FetchedMarkdown_GeneratesTableOfContents()
    {
        // Arrange
        var url = "https://example.com/content-with-toc.md";
        var remoteMarkdown = @"[TOC]

# Introduction

This is the introduction section.

## Getting Started

Here's how to get started.

### Prerequisites

You'll need these prerequisites.

## Advanced Topics

This covers advanced topics.

### Configuration

How to configure the system.

### Deployment

How to deploy the application.

# Conclusion

Final thoughts.
";

        var blogMarkdown = $@"# My Blog Post

Here is some local content before the fetched content.

<fetch markdownurl=""{url}"" pollfrequency=""12h""/>

More local content after the fetch.
";

        // Setup HTTP mock - return new HttpClient each time to avoid timeout issues
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(remoteMarkdown)
            });

        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handlerMock.Object)); // Return new instance each time

        var fetchEntities = new List<MarkdownFetchEntity>();
        _dbContextMock.SetupDbSet(fetchEntities, x => x.MarkdownFetches);

        // Use MarkdownRenderingService which does the preprocessing
        var renderingService = _serviceProvider.GetRequiredService<MarkdownRenderingService>();

        // Act
        var result = renderingService.GetPageFromMarkdown(blogMarkdown, DateTime.Now, "test.md");
        var html = result.HtmlContent;

        // Write HTML to console for debugging
        Console.WriteLine("=== Generated HTML ===");
        Console.WriteLine(html);
        Console.WriteLine("=== End HTML ===");

        // Assert - Verify TOC was generated
        // The [TOC] marker should be replaced with an actual table of contents
        // Leisn.MarkdigToc uses <nav> element with class="ml_toc"
        Assert.Contains("<nav", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<ul>", html);

        // Verify TOC contains links to the headings
        Assert.Contains("<a href=\"#introduction\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<a href=\"#getting-started\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<a href=\"#prerequisites\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<a href=\"#advanced-topics\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<a href=\"#configuration\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<a href=\"#deployment\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<a href=\"#conclusion\">", html, StringComparison.OrdinalIgnoreCase);

        // Verify content from fetched markdown is present
        Assert.Contains("Introduction", html);
        Assert.Contains("Getting Started", html);
        Assert.Contains("Prerequisites", html);
        Assert.Contains("Advanced Topics", html);
        Assert.Contains("Configuration", html);
        Assert.Contains("Deployment", html);
        Assert.Contains("Conclusion", html);

        // Verify headings have IDs (generated by TOC extension)
        Assert.Contains("id=\"introduction\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"getting-started\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"prerequisites\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"advanced-topics\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"conclusion\"", html, StringComparison.OrdinalIgnoreCase);

        // Verify local content is still present
        Assert.Contains("local content before", html);
        Assert.Contains("local content after", html);

        // Cached with a null BlogPostId - fetches are persisted even without a blog post
        _dbContextMock.Verify(x => x.MarkdownFetches.Add(It.Is<MarkdownFetchEntity>(e => e.BlogPostId == null)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EndToEnd_FetchedMarkdown_TOC_WithNestedHeadings()
    {
        // Arrange
        var url = "https://example.com/nested-toc.md";
        var remoteMarkdown = @"[TOC]

# Chapter 1

First chapter content.

## Section 1.1

Section one content.

### Subsection 1.1.1

Detailed content here.

### Subsection 1.1.2

More detailed content.

## Section 1.2

Section two content.

# Chapter 2

Second chapter content.

## Section 2.1

Another section.
";

        var blogMarkdown = $@"# Documentation

<fetch markdownurl=""{url}"" pollfrequency=""24h""/>

End of documentation.
";

        // Setup HTTP mock
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(remoteMarkdown)
            });

        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handlerMock.Object));

        var fetchEntities = new List<MarkdownFetchEntity>();
        _dbContextMock.SetupDbSet(fetchEntities, x => x.MarkdownFetches);

        var renderingService = _serviceProvider.GetRequiredService<MarkdownRenderingService>();

        // Act
        var result = renderingService.GetPageFromMarkdown(blogMarkdown, DateTime.Now, "test.md");
        var html = result.HtmlContent;

        // Write HTML for debugging
        Console.WriteLine("=== Generated HTML (Nested) ===");
        Console.WriteLine(html);
        Console.WriteLine("=== End HTML ===");

        // Assert - Verify nested TOC structure
        Assert.Contains("<nav", html, StringComparison.OrdinalIgnoreCase);

        // Verify all heading links in TOC
        Assert.Contains("<a href=\"#chapter-1\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<a href=\"#section-11\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<a href=\"#subsection-111\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<a href=\"#subsection-112\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<a href=\"#section-12\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<a href=\"#chapter-2\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<a href=\"#section-21\">", html, StringComparison.OrdinalIgnoreCase);

        // Verify nested UL structure (nested lists for subsections)
        var ulCount = html.Split("<ul>", StringSplitOptions.None).Length - 1;
        Assert.True(ulCount >= 2, $"Expected at least 2 nested <ul> tags for hierarchy, but found {ulCount}");

        // Verify heading IDs exist (dots are converted to periods in IDs)
        Assert.Contains("id=\"chapter-1\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"section-1.1\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"subsection-1.1.1\"", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EndToEnd_FetchedMarkdown_NoTOC_Marker_NoTableOfContents()
    {
        // Arrange
        var url = "https://example.com/no-toc.md";
        var remoteMarkdown = @"# Simple Document

No TOC marker here.

## Section One

Content without table of contents.

## Section Two

More content.
";

        var blogMarkdown = $@"# Blog Post

<fetch markdownurl=""{url}"" pollfrequency=""12h""/>

Additional content.
";

        // Setup HTTP mock
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(remoteMarkdown)
            });

        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handlerMock.Object));

        var fetchEntities = new List<MarkdownFetchEntity>();
        _dbContextMock.SetupDbSet(fetchEntities, x => x.MarkdownFetches);

        var renderingService = _serviceProvider.GetRequiredService<MarkdownRenderingService>();

        // Act
        var result = renderingService.GetPageFromMarkdown(blogMarkdown, DateTime.Now, "test.md");
        var html = result.HtmlContent;

        // Assert - Verify NO TOC was generated (no [TOC] marker)
        Assert.DoesNotContain("<nav", html, StringComparison.OrdinalIgnoreCase);

        // But headings should still have IDs (TOC extension adds them even without [TOC])
        Assert.Contains("id=\"simple-document\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"section-one\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"section-two\"", html, StringComparison.OrdinalIgnoreCase);

        // Verify content is present
        Assert.Contains("Simple Document", html);
        Assert.Contains("Section One", html);
        Assert.Contains("Section Two", html);
    }
}
