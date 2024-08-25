using Microsoft.EntityFrameworkCore;
using Mostlylucid.EntityFramework;
using Mostlylucid.EntityFramework.Models;
using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog.EntityFramework;

public class EFBaseService(IMostlylucidDBContext context, ILogger<EFBaseService> logger)
{
    protected  readonly IMostlylucidDBContext Context = context;
    protected readonly ILogger<EFBaseService> Logger = logger;

    public async Task<List<string>> GetCategories() => await Context.Categories.Select(x => x.Name).ToListAsync();
    
    protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);

    protected async Task<BlogPostEntity?> SavePost(BlogPostViewModel post, BlogPostEntity? currentPost =null ,
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
         currentPost ??= await PostsQuery().Where(x=>x.Slug == post.Slug && x.LanguageEntity == postLanguageEntity).FirstOrDefaultAsync();
        try
        {
            var hash = post.Markdown.ContentHash();
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

            
            var blogPost = currentPost ?? new BlogPostEntity();
            blogPost.Title = post.Title;
            blogPost.Slug = post.Slug;
            blogPost.Markdown = post.Markdown;
            blogPost.HtmlContent = post.HtmlContent;
            blogPost.PlainTextContent = post.PlainTextContent;
            blogPost.ContentHash = hash;
            blogPost.PublishedDate = post.PublishedDate;
            blogPost.LanguageEntity = postLanguageEntity;
            blogPost.Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList();
            blogPost.UpdatedDate = DateTimeOffset.UtcNow;
            if (currentPost != null)
            {
                Logger.LogInformation("Updating post {Post}", post.Slug);
                Context.BlogPosts.Update(blogPost); // Update the existing post
            }
            else
            {
                Logger.LogInformation("Adding new post {Post}", post.Slug);
                Context.BlogPosts.Add(blogPost); // Add a new post
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