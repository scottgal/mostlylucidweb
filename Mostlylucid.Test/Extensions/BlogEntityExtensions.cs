using System.Globalization;
using Mostlylucid.EntityFramework.Models;

namespace Mostlylucid.Test.Extensions;

public class BlogEntityExtensions
{
    public static BlogPostEntity GetBlogPostEntity(int id, string? langName = "")
    {
        var lang = LanguageExtensions.GetLanguageEntity("en");
        var cat = CategoryEntityExtensions.GetCategoryEntity("Category 1");
        return new BlogPostEntity
        {
            Id = id,
            Title = $"Title {id}",
            Slug = $"slug-{id}",
            HtmlContent = $"<p>Html Content {id}</p>",
            PlainTextContent = $"PlainTextContent {id}",
            Markdown = $"# Markdown {id}",
            PublishedDate = DateTime.ParseExact("2025-01-01T07:01", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            UpdatedDate = DateTime.ParseExact("2025-01-01T07:01", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            LanguageEntity = lang,
            Categories = new List<CategoryEntity> { cat }
        };
    }

    public static List<BlogPostEntity> GetBlogPostEntities(int count, string? langName = "")
    {
        var langs = LanguageExtensions.GetLanguageEntities();

        if (!string.IsNullOrEmpty(langName)) langs = new List<LanguageEntity> { langs.First(x => x.Name == langName) };

        var langCount = langs.Count;
        var categories = CategoryEntityExtensions.GetCategoryEntities();
        var entities = new List<BlogPostEntity>();

        var enLang = langs.First();
        var cat1 = categories.First();

        // Add a root post to the list to test the category filter.
        var rootPost = new BlogPostEntity
        {
            Id = 0,
            Title = "Root Post",
            Slug = "root-post",
            HtmlContent = "<p>Html Content</p>",
            PlainTextContent = "PlainTextContent",
            Markdown = "# Markdown",
            PublishedDate = DateTime.ParseExact("2025-01-01T07:01", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            UpdatedDate = DateTime.ParseExact("2025-01-01T07:01", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            LanguageEntity = enLang,
            Categories = new List<CategoryEntity> { cat1 }
        };
        entities.Add(rootPost);
        for (var i = 1; i < count; i++)
        {
            var langIndex = (i - 1) % langCount;
            var language = langs[langIndex];
            var postCategories = categories.Take(i - 1 % categories.Count).ToList();
            var dayDate = (i + 1 % 30 + 1).ToString("00");
            entities.Add(new BlogPostEntity
            {
                Id = i,
                Title = $"Title {i}",
                Slug = $"slug-{i}",
                HtmlContent = $"<p>Html Content {i}</p>",
                PlainTextContent = $"PlainTextContent {i}",
                Markdown = $"# Markdown {i}",
                PublishedDate = DateTime.ParseExact($"2025-01-{dayDate}T07:01", "yyyy-MM-ddTHH:mm",
                    CultureInfo.InvariantCulture),
                UpdatedDate = DateTime.ParseExact($"2025-01-{dayDate}T07:01", "yyyy-MM-ddTHH:mm",
                    CultureInfo.InvariantCulture),
                LanguageEntity = new LanguageEntity
                {
                    Id = language.Id,
                    Name = language.Name
                },
                Categories = postCategories
            });
        }

        return entities;
    }
}