using Microsoft.EntityFrameworkCore;
using Mostlylucid.EntityFramework;
using Mostlylucid.EntityFramework.Models;
using Mostlylucid.Helpers;
using Mostlylucid.Mappers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Services.EntityFramework;

public class EFBlogService : BaseService, IBlogService
{
    private readonly MostlylucidDbContext _context;
    private readonly ILogger<EFBlogService> _logger;
    private readonly IMarkdownBlogService _markdownBlogService;

    public EFBlogService(MostlylucidDbContext context, IMarkdownBlogService markdownBlogService,
        ILogger<EFBlogService> logger)
    {
        _logger = logger;
        _context = context;
        _markdownBlogService = markdownBlogService;
    }

    public async Task<List<string>> GetCategories() => await _context.Categories.Select(x => x.Name).ToListAsync();
 

    private IQueryable<BlogPost> PostsQuery()=>_context.BlogPosts.Include(x => x.Categories)
            .Include(x => x.Language);
    
    
    public async Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "")
    {
        var posts = await PostsQuery().ToListAsync();
        return posts.Select(p => new BlogPostViewModel
        {
            Title = p.Title,
            Slug = p.Slug,
            HtmlContent = p.HtmlContent,
            PlainTextContent = p.PlainTextContent,
            PublishedDate = p.PublishedDate.DateTime
        }).ToList();
    }

    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10,
        string language = EnglishLanguage)
    {
        
        var count = await PostsQuery()
            .Where(x => x.Categories.Any(c => c.Name == category) && x.Language.Name == language).CountAsync();
        var posts = await PostsQuery()
            .Where(x => x.Categories.Any(c => c.Name == category) && x.Language.Name == language)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var languages = await GetLanguagesForSlugs(posts.Select(x => x.Slug).ToList());
        var postListViewModel = new PostListViewModel
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = count,
            Posts = posts.Select(x => x.ToListModel(
                languages.FirstOrDefault(entry => entry.Key == x.Slug).Value.ToArray())).ToList()
        };
        return postListViewModel;
    }

    public Task<List<PostListModel>> GetPostsForLanguage(DateTime? startDate = null, string category = "",
        string language = EnglishLanguage)
    {
        var query = PostsQuery();
        if (category != "") query = query.Where(x => x.Categories.Any(c => c.Name == category));
        if (startDate != null) query = query.Where(x => x.PublishedDate >= startDate);

        return query.Where(x => x.Language.Name == language)
            .Select(x => x.ToListModel(new[] { language }))
            .ToListAsync();
    }

    public async Task Populate()
    {
        var posts = await _markdownBlogService.GetPages();
        var languages = _markdownBlogService.LanguageList();

        var languageEntities = await EnsureLanguages(languages);
        await EnsureCategoriesAndPosts(posts, languageEntities);

        await _context.SaveChangesAsync();
    }

    public async Task<BlogPostViewModel?> GetPost(string slug, string language = "")
    {
        if (string.IsNullOrEmpty(language)) language = EnglishLanguage;
        var post = await PostsQuery().FirstOrDefaultAsync(x => x.Slug == slug && x.Language.Name == language);
        if (post == null) return null;
        var langArr = await GetLanguagesForSlug(slug);
        return post.ToPostModel(langArr);
    }

    public async Task<PostListViewModel> GetPagedPosts(int page = 1, int pageSize = 10,
        string language = EnglishLanguage)
    {
        var query =PostsQuery().Where(x => x.Language.Name == language);
        var count = await query.CountAsync();
        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return await GetPostList(count, posts, page, pageSize);
    }

    private async Task<List<string>> GetLanguagesForSlug(string slug)=> await PostsQuery()
        .Where(x => x.Slug == slug).Select(x=>x.Language.Name).ToListAsync();
 


    private async Task<Dictionary<string, List<string>>> GetLanguagesForSlugs(List<string> slugs)
    {
        var langSlugs = await PostsQuery()
            .Where(x => slugs.Contains(x.Slug))
            .Select(x => new { x.Slug, x.Language.Name }).ToListAsync();

        var outDict = new Dictionary<string, List<string>>();

        foreach (var lang in langSlugs)
            if (!outDict.TryGetValue(lang.Slug, out var langArr))
            {
                langArr = new List<string>();
                outDict.Add(lang.Slug, langArr);
            }
            else
            {
                langArr.Add(lang.Name);
            }

        return outDict;
    }

    private async Task<List<Language>> EnsureLanguages(Dictionary<string, List<string>> languages)
    {
        var languageList = languages.SelectMany(x => x.Value).ToList();
        var currentLanguages = await _context.Languages.Select(x => x.Name).ToListAsync();

        var languageEntities = new List<Language>();
        var enLang = new Language { Name = EnglishLanguage };

        if (!currentLanguages.Contains(EnglishLanguage)) _context.Languages.Add(enLang);
        languageEntities.Add(enLang);

        foreach (var language in languageList)
        {
            if (languageEntities.Any(x => x.Name == language)) continue;

            var langItem = new Language { Name = language };

            if (!currentLanguages.Contains(language)) _context.Languages.Add(langItem);

            languageEntities.Add(langItem);
        }

        await _context.SaveChangesAsync(); // Save the languages first so we can reference them in the blog posts
        return languageEntities;
    }

    private async Task EnsureCategoriesAndPosts(
        IEnumerable<BlogPostViewModel> posts,
        List<Language> languageEntities)
    {
        var existingPosts = await _context.BlogPosts.Select(x => x.ContentHash).ToListAsync();
        var existingCategories = await _context.Categories.Select(x => x.Name).ToListAsync();

        var languages = languageEntities.ToDictionary(x => x.Name, x => x);
        var categories = new List<Category>();

        foreach (var post in posts)
        {
            await AddCategoriesToContext(post.Categories, existingCategories, categories);
            await AddBlogPostToContext(post, languages[post.Language], categories, existingPosts);
        }
    }

    private async Task AddCategoriesToContext(
        IEnumerable<string> categoryList,
        List<string> existingCategories,
        List<Category> categories)
    {
        foreach (var category in categoryList)
        {
            if (categories.Any(x => x.Name == category)) continue;

            var cat = new Category { Name = category };

            if (!existingCategories.Contains(category)) await _context.Categories.AddAsync(cat);

            categories.Add(cat);
        }
    }

    private async Task AddBlogPostToContext(
        BlogPostViewModel post,
        Language postLanguage,
        List<Category> categories,
        List<string> existingPosts)
    {
        var hash = post.HtmlContent.ContentHash();
        if (existingPosts.Contains(hash)) return;

        var blogPost = new BlogPost
        {
            Title = post.Title,
            Slug = post.Slug,
            HtmlContent = post.HtmlContent,
            PlainTextContent = post.PlainTextContent,
            ContentHash = hash,
            PublishedDate = post.PublishedDate,
            Language = postLanguage,
            LanguageId = postLanguage.Id,
            Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList()
        };

        var currentPost =
            await _context.BlogPosts.FirstOrDefaultAsync(x => x.Slug == post.Slug && x.Language.Name == post.Language);
        if (currentPost != null)
        {
            if (currentPost.ContentHash == hash) return;
            _logger.LogInformation("Updating post {Post}", post.Slug);
            _context.BlogPosts.Update(blogPost);
        }
        else
        {
            _logger.LogInformation("Adding post {Post}", post.Slug);
            await _context.BlogPosts.AddAsync(blogPost);
        }
    }




    private async Task<PostListViewModel> GetPostList(int count, List<BlogPost> posts, int page, int pageSize)
    {
        var languages = await _context.BlogPosts.Include(x => x.Language)
            .Where(x => posts.Select(x => x.Slug).Contains(x.Slug)).Select(x =>
                new { x.Slug, x.Language.Name }
            ).ToDictionaryAsync(x => x.Slug, x => x.Name);

        var postModels = new List<PostListModel>();

        foreach (var postResult in posts)
        {
            var langArr = languages.Where(x => x.Key == postResult.Slug).Select(x => x.Value).ToArray();

            postModels.Add(postResult.ToListModel(langArr));
        }

        var postListViewModel = new PostListViewModel
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = count,
            Posts = postModels
        };

        return postListViewModel;
    }
}