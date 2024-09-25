using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Services.Interfaces;
using Mostlylucid.Shared;
using Mostlylucid.Shared.Entities;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Services.Blog;

public class BlogPopulator : BaseService, IBlogPopulator
{

    private readonly IMarkdownBlogService _markdownBlogService;

    public BlogPopulator(IMarkdownBlogService markdownBlogService,
        IMostlylucidDBContext context,
        ILogger<BlogPopulator> logger) : base(context, logger)
    {
        _markdownBlogService = markdownBlogService;

    }

    public async Task Populate(CancellationToken cancellationToken)
    {
        var posts = await _markdownBlogService.GetPages();
        var languages = _markdownBlogService.LanguageList();

        var languageEntities = await EnsureLanguages(languages);
        await EnsureCategoriesAndPosts(posts, languageEntities, cancellationToken);

        await Context.SaveChangesAsync(cancellationToken);
    }

private async Task<List<LanguageEntity>> EnsureLanguages(Dictionary<string, List<string>> languages)
{
    // Extract the list of distinct languages from the input dictionary
    var languageList = languages.SelectMany(x => x.Value).Distinct().ToList();
    
    // Retrieve the current languages from the database (trimmed and distinct)
    var currentLanguages = await Context.Languages.ToListAsync();

    // List to hold language entities, both from DB and new ones
    var languageEntities = new List<LanguageEntity>();
    
    // Check if English exists in the database, add if missing
    var enLang = new LanguageEntity { Name = Constants.EnglishLanguage };
    
    var currentLanguageNames = currentLanguages.Select(x => x.Name).ToList();
    
    if (!currentLanguageNames.Contains(Constants.EnglishLanguage))
    {
        // Add English to context if it's not in the DB and add to the list
        Context.Languages.Add(enLang);
        languageEntities.Add(enLang); 
    }
    else
    {
        // Fetch the existing English language entity from the DB and add it to the list
        var existingEnglishLanguage = await Context.Languages.FirstOrDefaultAsync(x => x.Name == Constants.EnglishLanguage);
        languageEntities.Add(existingEnglishLanguage);
    }

    // Iterate through the provided language list
    foreach (var language in languageList)
    {
        if (currentLanguageNames.Contains(language))
        {
            // Fetch the existing language entity from the DB and add it to the list
            var existingLanguage =  currentLanguages.First(x => x.Name == language);
            
            languageEntities.Add(existingLanguage);
        }
        else
        {
            // Add the new language to context and to the list
            var newLanguage = new LanguageEntity { Name = language };
            Context.Languages.Add(newLanguage);
            languageEntities.Add(newLanguage);
        }
    }

    await Context.SaveChangesAsync();
    // Do not call SaveChangesAsync here if you want to defer saving
    return languageEntities;
}


    private async Task EnsureCategoriesAndPosts(
        IEnumerable<BlogPostDto> posts,
        List<LanguageEntity> languageEntities, CancellationToken cancellationToken)
    {
        var languages = languageEntities.ToDictionary(x => x.Name, x => x);
        var currentPosts = await PostsQuery().ToListAsync(cancellationToken);
        foreach (var post in posts)
        {
            if(cancellationToken.IsCancellationRequested) return;
            var existingCategories = Context.Categories.Local.ToList();
            var currentPost =
                currentPosts.FirstOrDefault(x => x.Slug == post.Slug && x.LanguageEntity.Name == post.Language);
            await AddCategoriesToContext(post.Categories, existingCategories);
            existingCategories = Context.Categories.Local.ToList();
            await AddBlogPostToContext(post, languages[post.Language], existingCategories, currentPost);
        }
    }

    private async Task AddCategoriesToContext(
        IEnumerable<string> categoryList,
        List<CategoryEntity> existingCategories)
    {
        foreach (var category in categoryList)
        {
            if (existingCategories.Any(x => x.Name == category)) continue;

            var cat = new CategoryEntity { Name = category };

             await Context.Categories.AddAsync(cat);
        }
    }

    private async Task AddBlogPostToContext(
        BlogPostDto post,
        LanguageEntity postLanguageEntity,
        List<CategoryEntity> categories,
        BlogPostEntity? currentPost)
    {
        await SavePost(post, currentPost, categories, new List<LanguageEntity> { postLanguageEntity });
    }
}