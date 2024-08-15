# Het toevoegen van entiteitskader voor blogberichten Deel 2

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-15T18:00</datetime>

U vindt alle broncode voor de blog berichten op [GitHub](https://github.com/scottgal/mostlylucidweb/tree/local/Mostlylucid/Blog)

**Deel 2 van de reeks over het toevoegen van Entity Framework aan een.NET Core project.**
Deel 1 is te vinden [Hier.](/blog/addingentityframeworkforblogpostspt1).

# Inleiding

In de vorige post, zetten we de database en de context voor onze blog berichten. In deze post, zullen we de diensten toe te voegen om te communiceren met de database.

In de volgende post zullen we detailleren hoe deze diensten nu werken met de bestaande controllers en meningen.

[TOC]

### Instellen

We hebben nu een BlogSetup uitbreidingsklasse die deze diensten instelt.

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

Dit maakt gebruik van de eenvoudige `BlogConfig` klasse om te bepalen in welke modus we zitten, ofwel `File` of `Database`. Op basis hiervan registreren we de diensten die we nodig hebben.

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

Omdat ik zowel het bestand als de Database in deze toepassing wil ondersteunen (omdat waarom niet! Ik heb gebruik gemaakt van een interface gebaseerde aanpak waardoor deze kunnen worden verwisseld op basis van config.

We hebben drie nieuwe interfaces. `IBlogService`, `IMarkdownBlogService` en `IBlogPopulator`.

#### IBlogService

Dit is de belangrijkste interface voor de blog service. Het bevat methoden voor het verkrijgen van posten, categorieën en individuele posten.

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

Deze dienst wordt gebruikt door de `EFlogPopulatorService` op de eerste run om de database te vullen met berichten uit de markdown bestanden.

```csharp
public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();
}
```

Zoals je kunt zien is het vrij eenvoudig en heeft slechts twee methoden, `GetPages` en `LanguageList`. Deze worden gebruikt om de Markdown-bestanden te verwerken en de lijst met talen te krijgen.

Dit wordt ten uitvoer gelegd in de `MarkdownBlogPopulator` Klas.

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

De BlogPopulators worden gebruikt in onze setup methode hierboven om de database of statische cache object (voor het op bestanden gebaseerde systeem) met berichten te bevolken.

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

U kunt zien dat dit een uitbreiding is naar `WebApplication` met config waarmee de Database Migration kan worden uitgevoerd indien nodig (die ook de Database aanmaakt als deze niet bestaat). Het noemt dan de geconfigureerde `IBlogPopulator` service om de database te vullen.

Dit is de interface voor die dienst.

```csharp
public interface IBlogPopulator
{
    Task Populate();
}
```

Vrij simpel toch? Dit wordt ten uitvoer gelegd in zowel de Lid-Staten als in de Lid-Staten van de Gemeenschap. `MarkdownBlogPopulator` en `EFBlogPopulator` Lessen.

- Markdown - hier roepen we in de `GetPages` methode en bevolk de cache.

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

- De Voorzitter. - Het woord is aan de Socialistische Fractie. `IMarkdownBlogService` om de pagina's te krijgen en vervolgens de database te bevolken.

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

We hebben deze functionaliteit gescheiden in interfaces om de code begrijpelijker en 'gescheiden' te maken (zoals in de SOLID principes). Dit stelt ons in staat om eenvoudig de diensten te ruilen op basis van de configuratie.

In de volgende post zullen we meer in detail kijken naar de uitvoering van de `EFBlogService` en `MarkdownBlogService` Lessen.