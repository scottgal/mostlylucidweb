using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared;
using Mostlylucid.Shared.Entities;
using Mostlylucid.Shared.Mapper;
using Mostlylucid.Shared.Models;
using Serilog;
using Serilog.Events;
using SerilogTracing;

namespace Mostlylucid.Services.Blog;

public class BlogService(
    IMostlylucidDBContext context,
    MarkdownRenderingService markdownRenderingService,
    ILogger<BlogService> logger)
    : BaseService(context, logger), IBlogService
{
    private IQueryable<BlogPostEntity> NoTrackingQuery() => PostsQuery().AsNoTrackingWithIdentityResolution();

    public async Task<BasePagingModel<BlogPostDto>?> Get(PostListQueryModel model)
    {
        using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            model.Categories, model.Page, model.PageSize, model.Language);
        try
        {
            var categories = model.Categories?.ToArray() ?? Array.Empty<string>();
            var page = model.Page;
            var pageSize = model.PageSize;
            var language = model.Language;

            var countQuery = NoTrackingQuery();
                
                if(model.Language != null) 
                    countQuery= countQuery.Where(x=>x.LanguageEntity.Name == language);
            if (categories?.Any(x=>!string.IsNullOrEmpty(x)) == true)
                countQuery = countQuery.Where(x =>
                    x.Categories.Any(c => categories.Contains(c.Name)));
                      var count =await  countQuery.CountAsync();
            var postQuery = PostsQuery();

            if (model.StartDate != null)
            {
                postQuery = postQuery.Where(x => x.PublishedDate.DateTime >= model.StartDate);
            }

            if (model.EndDate != null)
            {
                postQuery = postQuery.Where(x => x.PublishedDate.DateTime <= model.EndDate);
            }

            if (categories?.Any(x=>!string.IsNullOrEmpty(x))==true)
            {
                postQuery = postQuery.Where(x => x.Categories.Any(c => categories.Contains(c.Name)));
            }

            if (!string.IsNullOrEmpty(language))
            {
                postQuery = postQuery.Where(x => x.LanguageEntity.Name == language);
            }

            postQuery = postQuery.OrderByDescending(x => x.PublishedDate.DateTime);
            if (page != null)
            {
                if (pageSize == null) pageSize = Constants.DefaultPageSize;
                postQuery = postQuery.Skip((page.Value - 1) * pageSize.Value);
                postQuery = postQuery.Take(pageSize.Value);
            }

            var posts = await postQuery.ToListAsync();
            var langSlugs = await GetLanguagesForSlugs(posts.Select(x => x.Slug).ToList());
            var slugs=posts.Select(x=>x.Slug).ToList();
            if(model.Language!=Constants.EnglishLanguage)
            {
                var categoriesDict = await GetCategories(slugs);
                posts.ForEach(x =>
                {
                    if(categoriesDict.TryGetValue(x.Slug, out var entityCategories))
                        x.Categories = entityCategories;
                });
            }
            var postListViewModel = new BasePagingModel<BlogPostDto>()
            {
                Page = page ?? 1,
                PageSize = pageSize ?? count,
                TotalItems = count,
                Data = posts.Select(x => x.ToDto(langSlugs[x.Slug].ToArray())).ToList()
            };
            activity.Complete();
            return postListViewModel;
        }
        catch (Exception e)
        {
            activity.Complete(LogEventLevel.Error, e);
        }

        return null;
    }


    public async Task<string> GetSlug(int postId)
    {
        var post = await Context.BlogPosts.FindAsync(postId);
        if (post == null) return "";
        return post.Slug;
    }

    public Task<bool> EntryExists(string slug, string language)
    {
        return PostsQuery().AnyAsync(x => x.Slug == slug && x.LanguageEntity.Name == language);
    }

    public Task<bool> EntryChanged(string slug, string language, string hash)
    {
        return PostsQuery().AnyAsync(x => x.Slug == slug && x.LanguageEntity.Name == language && x.ContentHash != hash);
    }


    public async Task<BlogPostDto> SavePost(string slug, string language, string markdown)
    {
        try
        {
            var model = markdownRenderingService.GetPageFromMarkdown(markdown, DateTime.Now, slug);
            return await SavePost(model);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error saving post {Slug} in {Language}", slug, language);
        }

        return new BlogPostDto();
    }

    public async Task<BlogPostDto> SavePost(BlogPostDto model)
    {
        using var activity =
            Log.Logger.StartActivity("SavePost {Slug}, {Language}", model.Slug, model.Language);
        try
        {
            var post = await PostsQuery()
                .FirstOrDefaultAsync(x => x.Slug == model.Slug && x.LanguageEntity.Name == model.Language);
            await base.SavePost(model, post, activity: activity.Activity);
            await Context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            activity.Complete(LogEventLevel.Error, e);
            Logger.LogError(e, "Error saving post {Slug} in {Language}", model.Slug, model.Language);
        }

        return model;
    }

    public async Task<bool> Delete(string slug, string language)
    {
        using var activity = Log.Logger.StartActivity("Delete {Slug}, {Language}", slug, language );
        try
        {
            if (language == MarkdownBaseService.EnglishLanguage)
            {
                var posts = await PostsQuery().Where(x => x.Slug == slug).ToListAsync();
                Logger.LogInformation("Deleting {Count} posts", posts.Count);
                activity.Activity?.AddTag("Post Count", posts.Count);
                if (posts.Any() != true) return false;
                Context.BlogPosts.RemoveRange(posts);
            }
            else
            {
                var post = await PostsQuery()
                    .FirstOrDefaultAsync(x => x.Slug == slug && x.LanguageEntity.Name == language);
                logger.LogInformation("Deleting post {Slug} in {Language}", slug, language);
                activity.Activity?.AddTag("Post", post);
                if (post == null) return false;
                Context.BlogPosts.Remove(post);
            }

            await Context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            activity.Complete(LogEventLevel.Error, e);
            Logger.LogError(e, "Error deleting post {Slug} in {Language}", slug, language);
            return false;
        }
    }

    public async Task<BlogPostDto?> GetPost(BlogPostQueryModel model)
    {
        var language = model.Language;
        var slug = model.Slug;
        if (string.IsNullOrEmpty(language)) language = MarkdownBaseService.EnglishLanguage;
        var post = await NoTrackingQuery()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.LanguageEntity.Name == language);
        if(post==null) return null;
        if (model.Language != Constants.EnglishLanguage)
        {
            var categories =await GetCategories(slug, true);
            post.Categories = categories;
        }

        var langArr = await GetLanguagesForSlug(slug);
        return post.ToDto(langArr);
    }

    private async Task<List<CategoryEntity>> GetCategories(string slug, bool noTracking = false)
    {
        var query = noTracking ? PostsQuery().AsNoTracking() : PostsQuery();
       var post=await query.FirstOrDefaultAsync(x=>x.LanguageEntity.Name == Constants.EnglishLanguage && x.Slug == slug);
        if (post == null) return new List<CategoryEntity>();
       return post.Categories.OrderBy(x => x.Name).ToList();
    }
    
    private async Task<Dictionary<string,List<CategoryEntity>>> GetCategories(IEnumerable<string> slugs, bool noTracking = false)
    {
        var query = noTracking ? PostsQuery().AsNoTracking() : PostsQuery();
        var posts=await query.Where(x=>x.LanguageEntity.Name == Constants.EnglishLanguage && slugs.Contains(x.Slug)).ToListAsync();
        if(posts.Count==0) return new Dictionary<string, List<CategoryEntity>>();
        return posts.ToDictionary(x=>x.Slug,x=>x.Categories.OrderBy(z=>z.Name).ToList());
    }

    private async Task<string[]> GetLanguagesForSlug(string slug) => await NoTrackingQuery()
        .Where(x => x.Slug == slug).Select(x => x.LanguageEntity.Name).ToArrayAsync();


    private async Task<Dictionary<string, List<string>>> GetLanguagesForSlugs(List<string> slugs)
    {
        // Perform the query and grouping server-side
        var groupedLangSlugs = await NoTrackingQuery()
            .Where(x => slugs.Contains(x.Slug))
            .GroupBy(x => x.Slug)
            .Select(g => new 
            {
                Slug = g.Key, 
                Languages = g.Select(x => x.LanguageEntity.Name).Distinct().OrderBy(name => name).ToList()
            })
            .ToListAsync();

        // Convert the result into a dictionary
        var outDict = groupedLangSlugs.ToDictionary(
            g => g.Slug,       // Key is the Slug
            g => g.Languages   // Value is the list of languages
        );

        return outDict;
    }

}