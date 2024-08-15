# Adding Entity Framework for Blog Posts Part 2
<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-15T18:00</datetime>

You can find all the source code for the blog posts on [GitHub](https://github.com/scottgal/mostlylucidweb/tree/local/Mostlylucid/Blog)

**Part 2 of the series on adding Entity Framework to a .NET Core project.**
Part 1 can be found [here](/blog/addingentityframeworkforblogpostspt1).

# Introduction
In the previous post, we set up the database and the context for our blog posts. In this post, we will add the services to interact with the database.

In the next post we will detail how these services now work with the existing controllers and views.

[TOC]

### Setup
We now have a BlogSetup extension class which sets up these services.


```csharp
  public static void SetupBlog(this IServiceCollection services, IConfiguration configuration)
    {
        var config = services.ConfigurePOCO<BlogConfig>(configuration.GetSection(BlogConfig.Section));
       services.ConfigurePOCO<MarkdownConfig>(configuration.GetSection(MarkdownConfig.Section));
        switch (config.Mode)
        {
            case BlogMode.File:
                services.AddScoped<IBlogService, MarkdownBlogService>();
                services.AddScoped<IBlogPopulator, MarkdownBlogPopulator>();
                break;
            case BlogMode.Database:
                services.AddDbContext<MostlylucidDbContext>(options =>
                {
                    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
                });
                services.AddScoped<IBlogService, EFBlogService>();
                services.AddScoped<IMarkdownBlogService, MarkdownBlogPopulator>();
                services.AddScoped<IBlogPopulator, EFBlogPopulator>();
                break;
        }
    }
```
This uses the simple `BlogConfig` class to define which mode we are in, either `File` or `Database`. Based on this, we register the services we need.
```json
  "Blog": {
    "Mode": "File"
  }
```
```csharp
public class BlogConfig : IConfigSection
{
    public static string Section => "Blog";
    
    public BlogMode Mode { get; set; }
}

public enum BlogMode
{
    File,
    Database
}
```


## Interfaces
As I want to both support file and Database in this application (because why not! I've used an interface based approach allowing these to be swapped in based on config.

We have three new interfaces, `IBlogService`, `IMarkdownBlogService` and `IBlogPopulator`.


#### IBlogService
This is the main interface for the blog service. It contains methods for getting posts, categories and individual posts.


```csharp
public interface IBlogService
{
   Task<List<string>> GetCategories();
    Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "");
    
    Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10, string language = BaseService.EnglishLanguage);
    
    Task<BlogPostViewModel?> GetPost(string slug, string language = "");
    
    Task<PostListViewModel> GetPagedPosts(int page = 1, int pageSize = 10, string language = BaseService.EnglishLanguage);
    
    Task<List<PostListModel>> GetPostsForLanguage(DateTime? startDate = null, string category = "", string language = BaseService.EnglishLanguage);
}
```

#### IMarkdownBlogService
This service is used by the `EFlogPopulatorService` on first run to populate the database with posts from the markdown files.

```csharp
public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();
}
```
As you can see it's pretty simple and just has two methods, `GetPages` and `LanguageList`. These are used to process the Markdown files and get the list of languages.

This is implemented in the `MarkdownBlogPopulator` class.

```csharp
public class MarkdownBlogPopulator : MarkdownBaseService, IBlogPopulator, IMarkdownBlogService
{
    //Extra methods removed for brevity
     public async Task<List<BlogPostViewModel>> GetPages()
    {
        var pageList = new ConcurrentBag<BlogPostViewModel>();
        var languages = LanguageList();
        var pages = await GetLanguagePages(EnglishLanguage);
        foreach (var page in pages) pageList.Add(page);
        var pageLanguages = languages.Values.SelectMany(x => x).Distinct().ToList();
        await Parallel.ForEachAsync(pageLanguages, ParallelOptions, async (pageLang, ct) =>
        {
            var langPages = await GetLanguagePages(pageLang);
            if (langPages is { Count: > 0 })
                foreach (var page in langPages)
                    pageList.Add(page);
        });
        foreach (var page in pageList)
        {
            var currentPagelangs = languages.Where(x => x.Key == page.Slug).SelectMany(x => x.Value)?.ToList();
            var listLangs = currentPagelangs ?? new List<string>();
            listLangs.Add(EnglishLanguage);
            page.Languages = listLangs.OrderBy(x => x).ToArray();
        }

        return pageList.ToList();
    }
    
     public  Dictionary<string, List<string>> LanguageList()
    {
        var pages = Directory.GetFiles(_markdownConfig.MarkdownTranslatedPath, "*.md");
        Dictionary<string, List<string>> languageList = new();
        foreach (var page in pages)
        {
            var pageName = Path.GetFileNameWithoutExtension(page);
            var languageCode = pageName.LastIndexOf(".", StringComparison.Ordinal) + 1;
            var language = pageName.Substring(languageCode);
            var originPage = pageName.Substring(0, languageCode - 1);
            if (languageList.TryGetValue(originPage, out var languages))
            {
                languages.Add(language);
                languageList[originPage] = languages;
            }
            else
            {
                languageList[originPage] = new List<string> { language };
            }
        }
        return languageList;
    }
 
}
```


#### IBlogPopulator
The BlogPopulators are used in our setup method above to populate the database or static cache object (for the File based system) with posts.

```csharp
  public static async Task PopulateBlog(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var config = scope.ServiceProvider.GetRequiredService<BlogConfig>();
        if(config.Mode == BlogMode.Database)
        {
           var blogContext = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
           await blogContext.Database.MigrateAsync();
        }
    
        var context = scope.ServiceProvider.GetRequiredService<IBlogPopulator>();
        await context.Populate();
    }
```
You can see that this is an extension to `WebApplication` with config allowing the Database Migration to be run if needed (which also creates the Database if it doesn't exist). It then calls the configured `IBlogPopulator` service to populate the database.

This is the interface for that service. 

```csharp
public interface IBlogPopulator
{
    Task Populate();
}
```

Pretty simple right? This is implemented in both the `MarkdownBlogPopulator` and `EFBlogPopulator` classes.

-   Markdown - here we call into the `GetPages` method and populate the cache.
```csharp
  /// <summary>
    ///     The method to preload the cache with pages and Languages.
    /// </summary>
    public async Task Populate()
    {
        await PopulatePages();
    }

    private async Task PopulatePages()
    {
        if (GetPageCache() is { Count: > 0 }) return;
        Dictionary<(string slug, string lang), BlogPostViewModel> pageCache = new();
        var pages = await GetPages();
        foreach (var page in pages) pageCache.TryAdd((page.Slug, page.Language), page);
        SetPageCache(pageCache);
    }
```
- EF - here we call into the `IMarkdownBlogService` to get the pages and then populate the database.
```csharp
    public async Task Populate()
    {
        var posts = await markdownBlogService.GetPages();
        var languages = markdownBlogService.LanguageList();

        var languageEntities = await EnsureLanguages(languages);
        await EnsureCategoriesAndPosts(posts, languageEntities);

        await context.SaveChangesAsync();
    }

```

We have segregated this functionality into interfaces to make the code more understandable and 'segregated' (as in the SOLID principles). This allows us to easily swap out the services based on the configuration.

In the next post, we will look in more detail at the implementation of the `EFBlogService` and `MarkdownBlogService` classes.