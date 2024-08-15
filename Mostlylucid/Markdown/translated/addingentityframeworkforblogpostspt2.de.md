# Hinzufügen von Entity Framework für Blog-Posts Teil 2

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-15T18:00</datetime>

Sie finden alle Quellcode für die Blog-Beiträge auf [GitHub](https://github.com/scottgal/mostlylucidweb/tree/local/Mostlylucid/Blog)

**Teil 2 der Reihe über das Hinzufügen von Entity Framework zu einem.NET Core-Projekt.**
Teil 1 kann gefunden werden [Hierher](/blog/addingentityframeworkforblogpostspt1).

# Einleitung

Im vorherigen Beitrag haben wir die Datenbank und den Kontext für unsere Blog-Posts eingerichtet. In diesem Beitrag werden wir die Dienste hinzufügen, um mit der Datenbank zu interagieren.

Im nächsten Beitrag werden wir detailliert darlegen, wie diese Dienste jetzt mit den vorhandenen Controllern und Ansichten funktionieren.

[TOC]

### Einrichtung

Wir haben jetzt eine BlogSetup-Erweiterungsklasse, die diese Dienste aufstellt.

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

Dies nutzt die einfache `BlogConfig` Klasse zu definieren, in welchem Modus wir uns befinden, entweder `File` oder `Database`......................................................................................................... Auf dieser Grundlage registrieren wir die Dienste, die wir benötigen.

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

## Schnittstellen

Da ich sowohl die Datei als auch die Datenbank in dieser Anwendung unterstützen möchte (denn warum nicht! Ich habe einen Interface-basierten Ansatz verwendet, mit dem diese auf Basis von Config ausgetauscht werden können.

Wir haben drei neue Schnittstellen, `IBlogService`, `IMarkdownBlogService` und `IBlogPopulator`.

#### IBlogService

Dies ist die Hauptschnittstelle für den Blog-Service. Es enthält Methoden, um Beiträge, Kategorien und einzelne Beiträge zu erhalten.

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

Dieser Dienst wird von der `EFlogPopulatorService` beim ersten Start die Datenbank mit Beiträgen aus den Markdown-Dateien bevölkern.

```csharp
public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();
}
```

Wie Sie sehen können, ist es ziemlich einfach und hat nur zwei Methoden, `GetPages` und `LanguageList`......................................................................................................... Diese werden verwendet, um die Markdown-Dateien zu verarbeiten und die Liste der Sprachen zu erhalten.

Dies wird in der `MarkdownBlogPopulator` Unterricht.

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

Die BlogPopulatoren werden in unserer obigen Setup-Methode verwendet, um das Datenbank- oder statische Cache-Objekt (für das File-basierte System) mit Posts zu bevölkern.

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

Sie können sehen, dass dies eine Erweiterung auf `WebApplication` Mit config kann die Datenbank-Migration bei Bedarf ausgeführt werden (was auch die Datenbank erzeugt, wenn sie nicht existiert). Es ruft dann die konfigurierte `IBlogPopulator` Service, um die Datenbank zu bevölkern.

Das ist die Schnittstelle für diesen Service.

```csharp
public interface IBlogPopulator
{
    Task Populate();
}
```

Ziemlich einfach, oder? Dies wird in den beiden `MarkdownBlogPopulator` und `EFBlogPopulator` Unterricht.

- Markdown - hier rufen wir in die `GetPages` Methode und bevölkere den Cache.

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

- EF - hier rufen wir in die `IMarkdownBlogService` um die Seiten zu erhalten und dann die Datenbank zu bevölkern.

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

Wir haben diese Funktionalität in Schnittstellen getrennt, um den Code verständlicher und'segregiert' zu machen (wie in den SOLID-Prinzipien). Auf diese Weise können wir die auf der Konfiguration basierenden Dienste einfach austauschen.

Im nächsten Beitrag werden wir uns eingehender mit der Umsetzung der `EFBlogService` und `MarkdownBlogService` Unterricht.