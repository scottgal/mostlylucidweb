﻿using System.Diagnostics;
using Mostlylucid.EntityFramework.Models;
using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog.EntityFramework;

public class EFBaseService(IMostlylucidDBContext context, ILogger<EFBaseService> logger)
{
    protected readonly IMostlylucidDBContext Context = context;
    protected readonly ILogger<EFBaseService> Logger = logger;

    public async Task<List<string>> GetCategories(bool noTracking = false)
    {
        if (noTracking)
            return await Context.Categories.AsNoTracking().Select(x => x.Name).ToListAsync();
        return await Context.Categories.Select(x => x.Name).ToListAsync();
    }
  

    protected IQueryable<BlogPostEntity> PostsQuery()
    {
        return Context.BlogPosts.Include(x => x.Categories)
            .Include(x => x.LanguageEntity);
    }

    protected async Task<BlogPostEntity?> SavePost(BlogPostViewModel post, BlogPostEntity? currentPost = null,
        List<CategoryEntity>? categories = null,
        List<LanguageEntity>? languages = null, Activity? activity = null)
    {
        if (languages == null)
            languages = await Context.Languages.ToListAsync();

        var postLanguageEntity = languages.FirstOrDefault(x => x.Name == post.Language);
        if (postLanguageEntity == null)
        {
            activity?.AddTag("Language Not Found", post.Language);
            Logger.LogError("Language {Language} not found", post.Language);
            return null;
        }

        categories ??= await Context.Categories.Where(x => post.Categories.Contains(x.Name)).ToListAsync();
        currentPost ??= await PostsQuery().Where(x => x.Slug == post.Slug && x.LanguageEntity == postLanguageEntity)
            .FirstOrDefaultAsync();
        try
        {
            var hash = post.Markdown.ContentHash();
            var hashChanged = currentPost?.ContentHash != hash;
            //Add an inital check, if the current post is the same as the new post's hash, then we can skip the rest of the checks
            if (!hashChanged)
            {
                activity?.AddTag("Post Hash Not Changed", post.Slug);
                Logger.LogInformation("Post Hash {Post} for language {Language} has not changed", post.Slug,
                    post.Language);
                return currentPost;
            }
            foreach (var postCat in post.Categories)
            {
                if(categories.All(x => x.Name != postCat))
                {
                    categories.Add(new CategoryEntity(){Name = postCat});
                }
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
                activity?.AddTag("Updating Post", post.Slug);
                Logger.LogInformation("Updating post {Post}", post.Slug);
                Context.BlogPosts.Update(blogPost); // Update the existing post
            }
            else
            {
                logger.LogInformation("Adding new post {Post}", post.Slug);
                Context.BlogPosts.Add(blogPost); // Add a new post
            }

            return blogPost;
        }
        catch (Exception e)
        {
            activity?.AddTag("Error Adding Post", post.Slug);
            logger.LogError(e, "Error adding post {Post}", post.Slug);
        }

        return null;
    }
}