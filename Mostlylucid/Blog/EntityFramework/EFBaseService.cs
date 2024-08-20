using Microsoft.EntityFrameworkCore;
using Mostlylucid.EntityFramework;
using Mostlylucid.EntityFramework.Models;
using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog.EntityFramework;

public class EFBaseService
{
    protected  readonly MostlylucidDbContext Context;
    protected readonly ILogger<EFBaseService> Logger;

    public EFBaseService(MostlylucidDbContext context, ILogger<EFBaseService> logger)
    {
        Context = context;
        Logger = logger;
    }

    public async Task<List<string>> GetCategories() => await Context.Categories.Select(x => x.Name).ToListAsync();
    
    protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);

    public async Task<BlogPostEntity?> SavePost(BlogPostViewModel post, BlogPostEntity? currentPost =null ,
        List<CategoryEntity>? categories = null,
        List<LanguageEntity>? languages = null)
    {
        if (languages == null)
            languages = await Context.Languages.ToListAsync();

    var postLanguageEntity = languages.FirstOrDefault(x => x.Name == post.Language);
        if (postLanguageEntity == null)
        {
            Logger.LogError("Language {Language} not found", post.Language);
            return null;
        }
        categories ??= await Context.Categories.Where(x => post.Categories.Contains(x.Name)).ToListAsync();
         currentPost ??= await PostsQuery().Where(x=>x.Slug == post.Slug).FirstOrDefaultAsync();
        try
        {
            var hash = post.HtmlContent.ContentHash();
            var currentCategoryNames = currentPost?.Categories.Select(x => x.Name).ToArray() ?? Array.Empty<string>();
            var categoriesChanged = false;
            if (!currentCategoryNames.All(post.Categories.Contains) ||
                !post.Categories.All(currentCategoryNames.Contains))
            {
                categoriesChanged = true;
                Logger.LogInformation("Categories have changed for post {Post}", post.Slug);
            }

            var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
            var titleChanged = currentPost?.Title != post.Title;
            if (!titleChanged && !dateChanged && hash == currentPost?.ContentHash && !categoriesChanged)
            {
                Logger.LogInformation("Post {Post} has not changed", post.Slug);
                return currentPost;
            }

            var blogPost = new BlogPostEntity
            {
                Title = post.Title,
                Slug = post.Slug,
                OriginalMarkdown = post.OriginalMarkdown,
                HtmlContent = post.HtmlContent,
                PlainTextContent = post.PlainTextContent,
                ContentHash = hash,
                PublishedDate = post.PublishedDate,
                LanguageEntity = postLanguageEntity,
                Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList()
            };


            if (currentPost != null)
            {
                Logger.LogInformation("Updating post {Post}", post.Slug);
                Context.BlogPosts.Update(blogPost);
            }
            else
            {
                Logger.LogInformation("Adding post {Post}", post.Slug);
                await Context.BlogPosts.AddAsync(blogPost);
            }
            return blogPost;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error adding post {Post}", post.Slug);
        }

        return null;
    }
    

}