﻿using Microsoft.EntityFrameworkCore;
using Mostlylucid.EntityFramework;
using Mostlylucid.EntityFramework.Models;
using Mostlylucid.Mappers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog.EntityFramework;

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
 

    private IQueryable<BlogPostEntity> PostsQuery()=>_context.BlogPosts.Include(x => x.Categories)
            .Include(x => x.LanguageEntity);
    
    
    public async Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "")
    {
        var posts = await PostsQuery().ToListAsync();
        _logger.LogInformation("Getting posts");
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
            .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language).CountAsync();
        var posts = await PostsQuery()
            .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
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

        return query.Where(x => x.LanguageEntity.Name == language)
            .Select(x => x.ToListModel(new[] { language }))
            .ToListAsync();
    }




    public async Task<BlogPostViewModel?> GetPost(string slug, string language = "")
    {
        if (string.IsNullOrEmpty(language)) language = EnglishLanguage;
        var post = await PostsQuery().FirstOrDefaultAsync(x => x.Slug == slug && x.LanguageEntity.Name == language);
        if (post == null) return null;
        var langArr = await GetLanguagesForSlug(slug);
        return post.ToPostModel(langArr);
    }

    public async Task<PostListViewModel> GetPagedPosts(int page = 1, int pageSize = 10,
        string language = EnglishLanguage)
    {
        var query =PostsQuery().Where(x => x.LanguageEntity.Name == language);
        var count = await query.CountAsync();
        var posts = await query
            .OrderByDescending(x=>x.PublishedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return await GetPostList(count, posts, page, pageSize);
    }

    private async Task<List<string>> GetLanguagesForSlug(string slug)=> await PostsQuery()
        .Where(x => x.Slug == slug).Select(x=>x.LanguageEntity.Name).ToListAsync();
 


    private async Task<Dictionary<string, List<string>>> GetLanguagesForSlugs(List<string> slugs)
    {
        var langSlugs = await PostsQuery()
            .Where(x => slugs.Contains(x.Slug))
            .Select(x => new { x.Slug, x.LanguageEntity.Name }).ToListAsync();

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



    private async Task<PostListViewModel> GetPostList(int count, List<BlogPostEntity> posts, int page, int pageSize)
    {
        var languages = await _context.BlogPosts.Include(x => x.LanguageEntity)
            .Where(x => posts.Select(x => x.Slug).Contains(x.Slug)).Select(x =>
                new { x.Slug, x.LanguageEntity.Name }
            ).ToListAsync();

        var postModels = new List<PostListModel>();

        foreach (var postResult in posts)
        {
            var langArr = languages.Where(x => x.Slug == postResult.Slug).Select(x => x.Name).ToArray();

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