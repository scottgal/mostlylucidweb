using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mostlylucid.Blog;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Test.Tests;

public class MarkdownParserTest
{
    public enum BlogEntryType
    {
        Good,
        Bad
    }

    private readonly Mock<MarkdownRenderingService> _markdownRenderingService;
    private readonly IServiceProvider _serviceProvider;

    public MarkdownParserTest()
    {
        var services = new ServiceCollection();
        services.AddScoped<MarkdownRenderingService>();
        _serviceProvider = services.BuildServiceProvider();
    }

    public BlogPostViewModel GetBlogPostViewModel(BlogEntryType type = BlogEntryType.Good)
    {
        var resource = "";
        resource = type == BlogEntryType.Bad
            ? "Mostlylucid.Test.Resources.bad_testentry.md"
            : "Mostlylucid.Test.Resources.testentry.md";

        var markdown = GetMarkdownResource(resource);
        markdown = markdown.Replace("$1", "Test");
        var parser = _serviceProvider.GetRequiredService<MarkdownRenderingService>();
        var result = parser.GetPageFromMarkdown(markdown, DateTime.Now, "test.md");
        return result;
    }

    private string GetMarkdownResource(string resourceName)
    {
        var assembly = Assembly.GetAssembly(typeof(MarkdownParserTest));
        // I ALWAYS forget the format var resources = assembly.GetManifestResourceNames();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [Fact]
    public void TestMarkdownParser_BadEntry()
    {
        var result = GetBlogPostViewModel(BlogEntryType.Bad);
        Assert.NotNull(result);
    }

    [Fact]
    public void TestMarkdownParser_BadEntry_Categories()
    {
        var result = GetBlogPostViewModel(BlogEntryType.Bad);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public void TestMarkdownParser_Title()
    {
        var result = GetBlogPostViewModel();
        Assert.True(result.Title == "Blog Entry Test");
    }

    [Fact]
    public void TestMarkdown_Date()
    {
        var result = GetBlogPostViewModel();
        Assert.True(result.PublishedDate.ToString("dddd, dd, MM, yy HH:mm:ss")
                    == "Wednesday, 01, 01, 25 01:01:00");
    }

    [Fact]
    public void TestMarkdown_Categories()
    {
        var result = GetBlogPostViewModel();
        Assert.Equal(new[] { "Category 1", "Category 2", "Category 3" }, result.Categories);
    }

    [Fact]
    public void TestMarkdown_TOC()
    {
        var tocString = "<nav><ul><li><a href='#this-is-some-text'>This is Some Text</a></li></ul></nav>";
        var result = GetBlogPostViewModel();
        Assert.Contains(tocString, result.HtmlContent);
    }

    [Fact]
    public void TestMarkdownParser()
    {
        var result = GetBlogPostViewModel();
        Assert.NotNull(result);
    }
}