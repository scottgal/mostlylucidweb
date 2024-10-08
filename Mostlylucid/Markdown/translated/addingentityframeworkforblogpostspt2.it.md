# Aggiunta del quadro dell'entità per i post del blog (parte 2)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-15T18:00</datetime>

Potete trovare tutto il codice sorgente per i post del blog su [GitHubCity name (optional, probably does not need a translation)](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Parte 2 della serie sull'aggiunta di Entity Framework a un progetto.NET Core.**
Si può trovare la parte 1 [qui](/blog/addingentityframeworkforblogpostspt1).

# Introduzione

Nel post precedente, abbiamo creato il database e il contesto per i nostri post sul blog. In questo post, aggiungeremo i servizi per interagire con il database.

Nel prossimo post spiegheremo come questi servizi funzionano ora con i controllori e le opinioni esistenti.

[TOC]

### Configurazione

Ora abbiamo una classe di estensione BlogSetup che imposta questi servizi. Questa è un'estensione da quello che abbiamo fatto in [Parte 1](/blog/addingentityframeworkforblogpostspt1), dove abbiamo creato il database e il contesto.

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

Questo usa il semplice `BlogConfig` classe per definire in quale modalità siamo, o `File` oppure `Database`. In base a questo, registriamo i servizi di cui abbiamo bisogno.

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

## Interfacce

Dato che voglio supportare sia il file che il Database in questa applicazione (perché no! Ho usato un approccio basato sull'interfaccia che permette di scambiarli in base alla configurazione.

Abbiamo tre nuove interfacce, `IBlogService`, `IMarkdownBlogService` e `IBlogPopulator`.

#### IBlogServiceCity name (optional, probably does not need a translation)

Questa è l'interfaccia principale per il servizio blog. Contiene metodi per ottenere posti, categorie e singoli posti.

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

#### ImarkdownBlogService

Questo servizio è utilizzato dal `EFlogPopulatorService` in prima esecuzione per popolare il database con i post dai file markdown.

```csharp
public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();
}
```

Come potete vedere è abbastanza semplice e ha solo due metodi, `GetPages` e `LanguageList`. Questi sono utilizzati per elaborare i file Markdown e ottenere l'elenco delle lingue.

#### IBlogPopulatorCity name (optional, probably does not need a translation)

I blogPopulatori sono utilizzati nel nostro metodo di configurazione sopra per popolare il database o oggetto di cache statico (per il sistema basato su file) con i post.

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

Potete vedere che questa è un'estensione a `WebApplication` con la configurazione che consente di eseguire la migrazione del database se necessario (che crea anche il database se non esiste). Poi chiama il configurato `IBlogPopulator` servizio per popolare il database.

Questa è l'interfaccia per quel servizio.

```csharp
public interface IBlogPopulator
{
    Task Populate();
}
```

### Attuazione

Semplice, vero? Ciò è attuato in entrambi i casi. `MarkdownBlogPopulator` e `EFBlogPopulator` lezioni.

- Markdown - qui chiamiamo nella `GetPages` metodo e popolare la cache.

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

- EF - qui chiamiamo in `IMarkdownBlogService` per ottenere le pagine e poi popolare il database.

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

Abbiamo suddiviso questa funzionalità in interfacce per rendere il codice più comprensibile e "segregato" (come nei principi SOLID). Questo ci permette di scambiare facilmente i servizi in base alla configurazione.

# In conclusione

Nel prossimo post, esamineremo più in dettaglio l'implementazione dei Controller e Views per utilizzare questi servizi.