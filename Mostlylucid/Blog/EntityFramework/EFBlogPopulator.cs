using Microsoft.EntityFrameworkCore;
using Mostlylucid.EntityFramework;
using Mostlylucid.EntityFramework.Models;
using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog.EntityFramework;

public class EFBlogPopulator(
    IMarkdownBlogService markdownBlogService,
    MostlylucidDbContext context,
    ILogger<EFBlogPopulator> logger) : BaseService, IBlogPopulator
{
    public async Task Populate()
    {
        var posts = await markdownBlogService.GetPages();
        var languages = markdownBlogService.LanguageList();

        var languageEntities = await EnsureLanguages(languages);
        await EnsureCategoriesAndPosts(posts, languageEntities);

        await context.SaveChangesAsync();
    }


    private async Task<List<LanguageEntity>> EnsureLanguages(Dictionary<string, List<string>> languages)
    {
        var languageList = languages.SelectMany(x => x.Value).ToList();
        var currentLanguages = await context.Languages.Select(x => x.Name).ToListAsync();

        var languageEntities = new List<LanguageEntity>();
        var enLang = new LanguageEntity { Name = EnglishLanguage };

        if (!currentLanguages.Contains(EnglishLanguage)) context.Languages.Add(enLang);
        languageEntities.Add(enLang);

        foreach (var language in languageList)
        {
            if (languageEntities.Any(x => x.Name == language)) continue;

            var langItem = new LanguageEntity { Name = language };

            if (!currentLanguages.Contains(language)) context.Languages.Add(langItem);

            languageEntities.Add(langItem);
        }

        await context.SaveChangesAsync(); // Save the languages first so we can reference them in the blog posts
        return languageEntities;
    }

    private async Task EnsureCategoriesAndPosts(
        IEnumerable<BlogPostViewModel> posts,
        List<LanguageEntity> languageEntities)
    {
        var existingPosts = await context.BlogPosts.Select(x => x.ContentHash).ToListAsync();
        var existingCategories = await context.Categories.Select(x => x.Name).ToListAsync();

        var languages = languageEntities.ToDictionary(x => x.Name, x => x);
        var categories = new List<CategoryEntity>();

        foreach (var post in posts)
        {
            await AddCategoriesToContext(post.Categories, existingCategories, categories);
            await AddBlogPostToContext(post, languages[post.Language], categories, existingPosts);
        }
    }

    private async Task AddCategoriesToContext(
        IEnumerable<string> categoryList,
        List<string> existingCategories,
        List<CategoryEntity> categories)
    {
        foreach (var category in categoryList)
        {
            if (categories.Any(x => x.Name == category)) continue;

            var cat = new CategoryEntity { Name = category };

            if (!existingCategories.Contains(category)) await context.Categories.AddAsync(cat);

            categories.Add(cat);
        }
    }

    private async Task AddBlogPostToContext(
        BlogPostViewModel post,
        LanguageEntity postLanguageEntity,
        List<CategoryEntity> categories,
        List<string> existingPosts)
    {
        var hash = post.HtmlContent.ContentHash();
        if (existingPosts.Contains(hash)) return;

        var blogPost = new BlogPostEntity
        {
            Title = post.Title,
            Slug = post.Slug,
            HtmlContent = post.HtmlContent,
            PlainTextContent = post.PlainTextContent,
            ContentHash = hash,
            PublishedDate = post.PublishedDate,
            LanguageEntity = postLanguageEntity,
            LanguageId = postLanguageEntity.Id,
            Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList()
        };

        var currentPost =
            await context.BlogPosts.FirstOrDefaultAsync(x => x.Slug == post.Slug && x.LanguageEntity.Name == post.Language);
        if (currentPost != null)
        {
            if (currentPost.ContentHash == hash) return;
            logger.LogInformation("Updating post {Post}", post.Slug);
            context.BlogPosts.Update(blogPost);
        }
        else
        {
            logger.LogInformation("Adding post {Post}", post.Slug);
            await context.BlogPosts.AddAsync(blogPost);
        }
    }
}