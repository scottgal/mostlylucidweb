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
    private readonly IMarkdownBlogService _markdownBlogService;

    public EFBlogService(MostlylucidDbContext context, IMarkdownBlogService markdownBlogService,
        ILogger<EFBlogService> logger)
    {
        _context = context;
        _markdownBlogService = markdownBlogService;
    }

    public async Task<List<string>> GetCategories()
    {
        return await _context.Categories.Select(x => x.Name).ToListAsync();
    }

    public async Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "")
    {
        var posts = await _context.BlogPosts.ToListAsync();
        return posts.Select(p => new BlogPostViewModel
        {
            Title = p.Title,
            Slug = p.Slug,
            HtmlContent = p.HtmlContent,
            PlainTextContent = p.PlainTextContent,
            PublishedDate = p.PublishedDate.DateTime
        }).ToList();
    }

    public async Task Populate()
    {
        var posts = await _markdownBlogService.GetPages();
        var languages = _markdownBlogService.LanguageList();

        var languageList = languages.SelectMany(x => x.Value).ToList();

        var currentLanguages = await _context.Languages.Select(x => x.Name).ToListAsync();

        var languageEntities = new List<Language>();
        var enLang = new Language() { Name = "en" };

        if (!currentLanguages.Contains("en")) _context.Languages.Add(enLang);
        languageEntities.Add(enLang);

        foreach (var language in languageList)
        {
            if (languageEntities.Any(x => x.Name == language)) continue;
            var langItem = new Language() { Name = language };

            if (!currentLanguages.Contains(language)) _context.Languages.Add(langItem);
            _context.Languages.Add(langItem);
            languageEntities.Add(langItem);
        }

        await _context.SaveChangesAsync(); // Save the languages first so we can reference them in the blog posts
        var existingPosts = await _context.BlogPosts.Select(x => x.ContentHash).ToListAsync();
        var existingCategories = await _context.Categories.Select(x => x.Name).ToListAsync();

        var categories = new List<Category>();


        foreach (var post in posts)
        {
            var postLanguage = languageEntities.FirstOrDefault(x => x.Name == "en");

            var categoryList = post.Categories;

            foreach (var category in categoryList)
            {
                if (categories.Any(x => x.Name == category)) continue;
                var cat = new Category() { Name = category };
                if (!existingCategories.Contains(category)) _context.Categories.Add(cat);
                categories.Add(cat);
            }

            var hash = post.HtmlContent.ContentHash();
            var postLanguageId = postLanguage.Id;
            BlogPost blogPost;
            if (!existingPosts.Contains(hash))
            {
                blogPost = new BlogPost
                {
                    Title = post.Title,
                    Slug = post.Slug,
                    HtmlContent = post.HtmlContent,
                    PlainTextContent = post.PlainTextContent,
                    ContentHash = hash,
                    PublishedDate = post.PublishedDate,
                    Language = postLanguage,
                    LanguageId = postLanguageId,
                    Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList()
                };

                await _context.BlogPosts.AddAsync(blogPost);
            }

            if (languages.TryGetValue(post.Slug, out var postlanguages))
            {
                foreach (var entryLanguage in postlanguages)
                {
                    postLanguage = languageEntities.FirstOrDefault(x => x.Name == entryLanguage);

                    postLanguageId = postLanguage.Id;
                    var languagePost = await _markdownBlogService.GetPost(post.Slug, entryLanguage);
                    if (languagePost != null)
                    {
                        hash = languagePost.HtmlContent.ContentHash();
                        if (existingPosts.Contains(hash)) continue;
                        blogPost = new BlogPost
                        {
                            Title = languagePost.Title,
                            Slug = languagePost.Slug,
                            HtmlContent = languagePost.HtmlContent,
                            PlainTextContent = languagePost.PlainTextContent,
                            ContentHash = hash,
                            PublishedDate = languagePost.PublishedDate,
                            Language = postLanguage,
                            LanguageId = postLanguageId
                        };
                        await _context.BlogPosts.AddAsync(blogPost);
                    }
                }
            }
        }

        await _context.SaveChangesAsync();
    }


    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10)
    {
        var count = await _context.BlogPosts.Where(x => x.Categories.Any(c => c.Name == category)).CountAsync();
        var posts = await _context.BlogPosts.Where(x => x.Categories.Any(c => c.Name == category))
            .Where(x => x.Language.Name == "en")
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return await GetPostList(count, posts, page, pageSize);
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
            var langArr = languages.Where(x => x.Key == postResult.Slug).Select(x => x.Value)?.ToArray();

            postModels.Add(postResult.ToListModel(langArr));
        }

        var postListViewModel = new PostListViewModel()
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = count,
            Posts = postModels
        };

        return postListViewModel;
    }

    public async Task<BlogPostViewModel?> GetPost(string postName, string language = "")
    {
        if (string.IsNullOrEmpty(language)) language = "en";
        var post = await _context.BlogPosts.FirstOrDefaultAsync(x => x.Slug == postName && x.Language.Name == language);
        if (post == null) return null;
        return BlogPostMapper.ToPostModel(post);
    }

    public async Task<PostListViewModel> GetPosts(int page = 1, int pageSize = 10)
    {
        var count = await _context.BlogPosts.CountAsync();
        var posts = await _context.BlogPosts.Include(x => x.Categories)
            .Include(x => x.Language)
            .Where(x => x.Language.Name == "en")
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return await GetPostList(count, posts, page, pageSize);
    }
}