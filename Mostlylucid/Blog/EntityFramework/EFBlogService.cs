﻿using Microsoft.EntityFrameworkCore;
using Mostlylucid.EntityFramework;
using Mostlylucid.EntityFramework.Models;
using Mostlylucid.Helpers;
using Mostlylucid.Mappers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog.EntityFramework;

public class EFBlogService(
    IMostlylucidDBContext context,
    MarkdownRenderingService markdownRenderingService,
    ILogger<EFBlogService> logger)
    : EFBaseService(context, logger), IBlogService
{

    private IQueryable<BlogPostEntity> NoTrackingQuery() => PostsQuery().AsNoTrackingWithIdentityResolution();
    public async Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "")
    {
        
        var posts = await NoTrackingQuery().ToListAsync();
        Logger.LogInformation("Getting posts");
        return posts.Select(p => p.ToPostModel()).ToList();
    }

    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10,
        string language = MarkdownBaseService.EnglishLanguage)
    {
        
        var count = await NoTrackingQuery()
            .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language).CountAsync();
        var posts = await PostsQuery()
            .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
            .OrderByDescending(x=>x.PublishedDate.DateTime)
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

    public async Task<List<BlogPostViewModel>> GetAllPosts()
    {
        var posts = await NoTrackingQuery().ToListAsync();
        return posts.Select(p => p.ToPostModel()).ToList();
    }
    
    public async Task<string> GetSlug(int postId )
    {
        var post = await context.BlogPosts.FindAsync(postId);
        if (post == null) return "";
        return post.Slug;
    }
    
    public Task<List<PostListModel>> GetPostsForLanguage(DateTime? startDate = null, string category = "",
        string language = MarkdownBaseService.EnglishLanguage)
    {
        var query = NoTrackingQuery();
        if (!string.IsNullOrEmpty(category)) query = query.Where(x => x.Categories.Any(c => c.Name == category));
        if (startDate != null) query = query.Where(x => x.PublishedDate >= startDate);

        return query.Where(x => x.LanguageEntity.Name == language)
            .Select(x => x.ToListModel(new[] { language }))
            .ToListAsync();
    }

    public Task<bool> EntryExists(string slug, string language)
    {
        return PostsQuery().AnyAsync(x => x.Slug == slug && x.LanguageEntity.Name == language);
    }
    
    public Task<bool> EntryChanged(string slug, string language, string hash)
    {
        return PostsQuery().AnyAsync(x => x.Slug == slug && x.LanguageEntity.Name == language && x.ContentHash != hash);
    }


  
    public async  Task<BlogPostViewModel> SavePost(string slug, string language, string markdown)
    {
      var post = await PostsQuery().FirstOrDefaultAsync(x => x.Slug == slug && x.LanguageEntity.Name == language);
      var model = markdownRenderingService.GetPageFromMarkdown(markdown, DateTime.Now, slug);
     await SavePost(model, post);
     await Context.SaveChangesAsync();
        return model;
    }

    public async Task<BlogPostViewModel?> GetPost(string slug, string language = "")
    {
        if (string.IsNullOrEmpty(language)) language =MarkdownBaseService.EnglishLanguage;
        var post = await NoTrackingQuery().FirstOrDefaultAsync(x => x.Slug == slug && x.LanguageEntity.Name == language);
        if (post == null) return null;
        var langArr = await GetLanguagesForSlug(slug);
        return post.ToPostModel(langArr);
    }

    public async Task<PostListViewModel> GetPagedPosts(int page = 1, int pageSize = 10,
        string language = MarkdownBaseService.EnglishLanguage)
    {
        var query =NoTrackingQuery().Where(x => x.LanguageEntity.Name == language);
        var count = await query.CountAsync();
        var posts = await query
            .OrderByDescending(x=>x.PublishedDate.DateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return await GetPostList(count, posts, page, pageSize);
    }

    private async Task<List<string>> GetLanguagesForSlug(string slug)=> await NoTrackingQuery()
        .Where(x => x.Slug == slug).Select(x=>x.LanguageEntity.Name).ToListAsync();
 


    private async Task<Dictionary<string, List<string>>> GetLanguagesForSlugs(List<string> slugs)
    {
        var langSlugs = await NoTrackingQuery()
            .Where(x => slugs.Contains(x.Slug))
            .Select(x => new { x.Slug, x.LanguageEntity.Name }).OrderBy(x=>x.Name).ToListAsync();

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
        var languages = await NoTrackingQuery().Select(x =>
                new { x.Slug, x.LanguageEntity.Name }
            ).OrderBy(x=>x.Name).ToListAsync();

        var postModels = new List<PostListModel>();

        foreach (var postResult in posts)
        {
            var langArr = languages.Where(x => x.Slug == postResult.Slug).Select(x => x.Name).OrderBy(x=>x).ToArray();

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