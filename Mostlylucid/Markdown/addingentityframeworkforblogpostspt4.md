# Adding Entity Framework for Blog Posts (Pt. 4)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-17T20:00</datetime>

See parts [1](/blog/addingentityframeworkforblogpostspt1) and [2](/blog/addingentityframeworkforblogpostspt2) and [3](/blog/addingentityframeworkforblogpostspt3) for the previous steps.

# Introduction
In previous parts we covered how to set up the database, how our controllers and views are structured, and how our services worked. In this part we'll cover details on how to seed the database with some initial data and how the EF Based services work.

As usual you can see all the source for this on my GitHub [here](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog), in the Mostlylucid/Blog folder.

[TOC]

# Seeding the Database
In the previous part we covered how we [initialize and set up the services](/blog/addingentityframeworkforblogpostspt2#setup). In this part we'll cover how to seed the database with some initial data. This is done in the `EFBlogPopulator` class. This class is registered as a service in the `SetupBlog` extension method. 

```csharp
    public async Task Populate()
    {
        var posts = await _markdownBlogService.GetPages();
        var languages = _markdownBlogService.LanguageList();

        var languageEntities = await EnsureLanguages(languages);
        await EnsureCategoriesAndPosts(posts, languageEntities);

        await Context.SaveChangesAsync();
    }
```
You can see that in the `Populate` method we call into the `_markdownBlogService.GetPages()` this runs through our makrdown files and populates a bunch of `BlogViewModels` containing all the posts. 
We then do the same for the languages; this looks at our `translated` folder for all the translated markdown files we generated using EasyNMT (see [here](/blog/autotranslatingmarkdownfiles) for how we do that part).


## Adding the Languages
We then call into our `EnsureLanguages` method which ensures that all the languages are in the database. This is a simple method that checks if the language exists and if not adds it to the database.

```csharp
  private async Task<List<LanguageEntity>> EnsureLanguages(Dictionary<string, List<string>> languages)
    {
        var languageList = languages.SelectMany(x => x.Value).ToList();
        var currentLanguages = await Context.Languages.Select(x => x.Name).ToListAsync();

        var languageEntities = new List<LanguageEntity>();
        var enLang = new LanguageEntity { Name =MarkdownBaseService.EnglishLanguage };

        if (!currentLanguages.Contains(MarkdownBaseService.EnglishLanguage)) Context.Languages.Add(enLang);
        languageEntities.Add(enLang);

        foreach (var language in languageList)
        {
            if (languageEntities.Any(x => x.Name == language)) continue;

            var langItem = new LanguageEntity { Name = language };

            if (!currentLanguages.Contains(language)) Context.Languages.Add(langItem);

            languageEntities.Add(langItem);
        }

        await Context.SaveChangesAsync(); // Save the languages first so we can reference them in the blog posts
        return languageEntities;
    }
```
You'll see this is ppretty simple and just ensures that all the languages we got from the markdown posts  are in the database; and as we specified that the Ids are auto generated we need to `SaveChanges` to ensure the Ids are generated.
### Adding the Categories and Posts
We then call into our `EnsureCategoriesAndPosts` method which ensures that all the categories and posts are in the database. This is a bit more complex as we need to ensure that the categories are in the database and then we need to ensure that the posts are in the database. 

```csharp
    private async Task EnsureCategoriesAndPosts(
        IEnumerable<BlogPostViewModel> posts,
        List<LanguageEntity> languageEntities)
    {
        var languages = languageEntities.ToDictionary(x => x.Name, x => x);
        var currentPosts = await PostsQuery().ToListAsync();
        foreach (var post in posts)
        {
            var existingCategories = Context.Categories.Local.ToList();
            var currentPost =
                currentPosts.FirstOrDefault(x => x.Slug == post.Slug && x.LanguageEntity.Name == post.Language);
            await AddCategoriesToContext(post.Categories, existingCategories);
            existingCategories = Context.Categories.Local.ToList();
            await AddBlogPostToContext(post, languages[post.Language], existingCategories, currentPost);
        }
    }
```
In here we use the Context.Categories.Local to track the categories currently added to the Context (they're saved in the Database during the `SaveAsync` call).
You can see that we call into the `PostsQuery` method of our Base class which is a simple method that returns a queryable of the `BlogPostEntity` so we can query the database for the posts. 

```csharp
  protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);
   
```
#### Adding the Categories
We then call into the `AddCategoriesToContext` method which ensures that all the categories are in the database. This is a simple method that checks if the category exists and if not adds it to the database.

```csharp
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

```
Again this checks whether the category exists and if not adds it to the database.

#### Adding the Blog Posts

We then call into the `AddBlogPostToContext` method, this then calls into the `EFBaseService` to save the post to the database.
```csharp
    private async Task AddBlogPostToContext(
        BlogPostViewModel post,
        LanguageEntity postLanguageEntity,
        List<CategoryEntity> categories,
        BlogPostEntity? currentPost)
    {
        await SavePost(post, currentPost, categories, new List<LanguageEntity> { postLanguageEntity });
    }
```
We do this by calling the `SavePost` method which is a method that saves the post to the database. This method is a bit complex as it has to check whether the post has changed and if so update the post in the database.

```csharp

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

            
            var blogPost = currentPost ?? new BlogPostEntity();
            
            blogPost.Title = post.Title;
            blogPost.Slug = post.Slug;
            blogPost.OriginalMarkdown = post.OriginalMarkdown;
            blogPost.HtmlContent = post.HtmlContent;
            blogPost.PlainTextContent = post.PlainTextContent;
            blogPost.ContentHash = hash;
            blogPost.PublishedDate = post.PublishedDate;
            blogPost.LanguageEntity = postLanguageEntity;
            blogPost.Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList();

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

```
As you can see this has a LOT of change detection to ensure we don't re-add posts that haven't changed. We check the hash of the content, the categories, the date and the title. If any of these have changed we update the post in the database.

One thing to notice is how annoying checking a DateTimeOffset is; we have to convert it to UTC and then get the date to compare it. This is because the `DateTimeOffset` has a time component and we want to compare just the date.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# In Conclusion
Now we have a fully working blog system that can be populated from markdown files and translated markdown files. In the next part we'll cover the simple Service which we use to  display posts stored in the database.