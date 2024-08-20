# Lägga till Entity Framework för blogginlägg (Del 2)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-15T18:00</datetime>

Du kan hitta alla källkoden för blogginläggen på [GitHub Ordförande](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Del 2 i serien om att lägga till Entity Framework till ett kärnprojekt.NET.**
Del 1 kan hittas [här](/blog/addingentityframeworkforblogpostspt1).

# Inledning

I föregående inlägg har vi satt upp databasen och sammanhanget för våra blogginlägg. I det här inlägget kommer vi att lägga till tjänsterna för att interagera med databasen.

I nästa inlägg kommer vi att redogöra för hur dessa tjänster nu fungerar med de befintliga kontrollanterna och synpunkterna.

[TOC]

### Ställ in

Vi har nu en BlogSetup förlängning klass som sätter upp dessa tjänster. Detta är en förlängning från vad vi gjorde i [Häfte 1](/blog/addingentityframeworkforblogpostspt1), där vi upprättar databasen och sammanhanget.

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

Detta använder det enkla `BlogConfig` klass för att definiera vilket läge vi befinner oss i, antingen `File` eller `Database`....................................... Baserat på detta registrerar vi de tjänster vi behöver.

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

## Gränssnitt

Eftersom jag vill både stödja fil och databas i detta program (eftersom varför inte! Jag har använt en gränssnittsbaserad metod som tillåter dessa att bytas in baserat på konfiguration.

Vi har tre nya gränssnitt. `IBlogService`, `IMarkdownBlogService` och `IBlogPopulator`.

#### IBlogService

Detta är det viktigaste gränssnittet för bloggtjänsten. Den innehåller metoder för att få inlägg, kategorier och enskilda inlägg.

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

Denna tjänst används av `EFlogPopulatorService` på första kör för att fylla databasen med inlägg från markdown-filer.

```csharp
public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();
}
```

Som du kan se är det ganska enkelt och bara har två metoder, `GetPages` och `LanguageList`....................................... Dessa används för att behandla Markdown-filerna och få listan över språk.

#### IBlogPopulator

BloggPopulators används i vår inställningsmetod ovan för att fylla databasen eller statiskt cacheobjekt (för det filbaserade systemet) med inlägg.

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

Du kan se att detta är en förlängning till `WebApplication` med konfiguration som gör att databasmigreringen kan köras om det behövs (vilket också skapar databasen om den inte finns). Den kallar sedan den inställda `IBlogPopulator` tjänst för att fylla databasen.

Detta är gränssnittet för den tjänsten.

```csharp
public interface IBlogPopulator
{
    Task Populate();
}
```

### Genomförande

Ganska enkelt, eller hur? Detta genomförs i båda `MarkdownBlogPopulator` och `EFBlogPopulator` Klasser.

- Markdown - här kallar vi in `GetPages` metod och fylla cachen.

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

- EF - här kallar vi in `IMarkdownBlogService` för att få sidorna och sedan fylla databasen.

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

Vi har delat upp denna funktionalitet i gränssnitt för att göra koden mer begriplig och "segregerad" (som i SOLID-principerna). Detta gör att vi enkelt kan byta ut tjänsterna baserat på konfigurationen.

# Slutsatser

I nästa inlägg kommer vi att titta mer i detalj på genomförandet av Controllers and Views att använda dessa tjänster.