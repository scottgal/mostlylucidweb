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
    : EFBaseService(context, logger), IBlogService
{
    private IQueryable<BlogPostEntity> NoTrackingQuery() => PostsQuery().AsNoTrackingWithIdentityResolution();
    public async Task<BasePagingModel<BlogPostDto>?> Get(PostListQueryModel model)
    {
        using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            model.Categories, model.Page, model.PageSize, model.Language );
        try
        {
            var categories = model.Categories?.ToArray() ?? Array.Empty<string>();
            var page = model.Page;
            var pageSize = model.PageSize;
            var language = model.Language;
            
            var count = await NoTrackingQuery()
                .Where(x => x.Categories.Any(c=>categories.Contains(c.Name)) && x.LanguageEntity.Name == language)
                .CountAsync();
            var postQuery = PostsQuery();
                
                if(categories.Any())
                {
                    postQuery = postQuery.Where(x => x.Categories.Any(c => categories.Contains(c.Name)));
                }
                if (!string.IsNullOrEmpty(language))
                {
                    postQuery = postQuery.Where(x => x.LanguageEntity.Name == language);
                }
                
               postQuery = postQuery.OrderByDescending(x => x.PublishedDate.DateTime);
                   if(page !=null)
                   {
                       if(model.PageSize == null) pageSize = Constants.DefaultPageSize;
                       postQuery = postQuery.Skip((page.Value - 1) * pageSize.Value);
                       postQuery = postQuery.Take(pageSize.Value);
                   }
                   
            var posts = await postQuery.ToListAsync();
            var langSlugs = await GetLanguagesForSlugs(posts.Select(x => x.Slug).ToList());
            var postListViewModel = new BasePagingModel<BlogPostDto>()
            {
                Page = page??1,
                PageSize = pageSize ??0,
                TotalItems = count,
                Data = posts.Select(x=>x.ToDto(langSlugs[x.Slug].ToArray())).ToList()
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
        var post = await context.BlogPosts.FindAsync(postId);
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
            logger.LogError(e, "Error saving post {Slug} in {Language}", slug, language);
        }
        return new BlogPostDto();
    }

    public async Task<BlogPostDto> SavePost(BlogPostDto model)
    {
        using var activity =
            Log.Logger.StartActivity("SavePost {Slug}, {Language}",  model.Slug, model.Language );
        try
        {
            var post = await PostsQuery()
                .FirstOrDefaultAsync(x => x.Slug == model.Slug && x.LanguageEntity.Name == model.Language);
            await base.SavePost(model, post, activity: activity?.Activity);
            await Context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            activity?.Complete(LogEventLevel.Error, e);
            logger.LogError(e, "Error saving post {Slug} in {Language}", model.Slug, model.Language);
        }

        return model;
    }

    public async Task<bool> Delete(string slug, string language)
    {
        using var activity = Log.Logger.StartActivity("Delete {Slug}, {Language}", new { slug, language });
        try
        {
            if (language == MarkdownBaseService.EnglishLanguage)
            {
                var posts = await PostsQuery().Where(x => x.Slug == slug).ToListAsync();
                logger.LogInformation("Deleting {Count} posts", posts.Count);
                activity?.Activity?.AddTag("Post Count", posts.Count);
                if (posts?.Any() != true) return false;
                Context.BlogPosts.RemoveRange(posts);
            }
            else
            {
                var post = await PostsQuery()
                    .FirstOrDefaultAsync(x => x.Slug == slug && x.LanguageEntity.Name == language);
                logger.LogInformation("Deleting post {Slug} in {Language}", slug, language);
                activity?.Activity?.AddTag("Post", post);
                if (post == null) return false;
                Context.BlogPosts.Remove(post);
            }

            await Context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            activity?.Complete(LogEventLevel.Error, e);
            logger.LogError(e, "Error deleting post {Slug} in {Language}", slug, language);
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
        if (post == null) return null;
        var langArr = await GetLanguagesForSlug(slug);
        return post.ToDto();
    }
    

    private async Task<List<string>> GetLanguagesForSlug(string slug) => await NoTrackingQuery()
        .Where(x => x.Slug == slug).Select(x => x.LanguageEntity.Name).ToListAsync();


    private async Task<Dictionary<string, List<string>>> GetLanguagesForSlugs(List<string> slugs)
    {
        var langSlugs = await NoTrackingQuery()
            .Where(x => slugs.Contains(x.Slug))
            .Select(x => new { x.Slug, x.LanguageEntity.Name }).OrderBy(x => x.Name).ToListAsync();

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


}