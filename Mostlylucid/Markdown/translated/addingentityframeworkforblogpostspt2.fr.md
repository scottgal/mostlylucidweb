# Ajouter un cadre d'entité pour les billets de blog (partie 2)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-15T18:00</datetime>

Vous pouvez trouver tout le code source pour les messages de blog sur [GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Partie 2 de la série sur l'ajout d'un cadre d'entités à un projet de base.NET.**
La première partie peut être trouvée [Ici.](/blog/addingentityframeworkforblogpostspt1).

# Présentation

Dans le post précédent, nous avons mis en place la base de données et le contexte de nos billets de blog. Dans ce post, nous ajouterons les services pour interagir avec la base de données.

Dans le post suivant, nous détaillerons comment ces services fonctionnent maintenant avec les contrôleurs et les vues existants.

[TOC]

### Configuration

Nous avons maintenant une classe d'extension BlogSetup qui met en place ces services. C'est une extension de ce que nous avons fait en [Première partie](/blog/addingentityframeworkforblogpostspt1), où nous avons mis en place la base de données et le contexte.

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

Ceci utilise le simple `BlogConfig` classe pour définir le mode dans lequel nous sommes, soit `File` ou `Database`C'est ce que j'ai dit. Sur cette base, nous enregistrons les services dont nous avons besoin.

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

Comme je veux à la fois prendre en charge le fichier et la base de données dans cette application (parce que pourquoi pas! J'ai utilisé une approche basée sur l'interface permettant d'échanger ceux-ci en fonction de config.

Nous avons trois nouvelles interfaces, `IBlogService`, `IMarkdownBlogService` et `IBlogPopulator`.

#### IBlogService

C'est l'interface principale pour le service de blog. Il contient des méthodes pour obtenir des postes, des catégories et des postes individuels.

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

Ce service est utilisé par `EFlogPopulatorService` sur la première fois pour remplir la base de données avec des messages à partir des fichiers de balisage.

```csharp
public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();
}
```

Comme vous pouvez le voir c'est assez simple et a juste deux méthodes, `GetPages` et `LanguageList`C'est ce que j'ai dit. Ils sont utilisés pour traiter les fichiers Markdown et obtenir la liste des langues.

#### IBlogPopulateur

Les BlogPopulators sont utilisés dans notre méthode de configuration ci-dessus pour remplir la base de données ou l'objet cache statique (pour le système basé sur le fichier) avec des messages.

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

Vous pouvez voir que c'est une extension à `WebApplication` avec config permettant l'exécution de la migration de base de données si nécessaire (qui crée également la base de données si elle n'existe pas). Il appelle alors la configuration `IBlogPopulator` service pour remplir la base de données.

C'est l'interface pour ce service.

```csharp
public interface IBlogPopulator
{
    Task Populate();
}
```

### Mise en œuvre

Plutôt simple, n'est-ce pas? Cela est mis en œuvre dans les deux `MarkdownBlogPopulator` et `EFBlogPopulator` les cours.

- Markdown - ici nous appelons dans le `GetPages` méthode et peupler le cache.

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

- EF - ici nous appelons à la `IMarkdownBlogService` pour obtenir les pages et puis remplir la base de données.

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

Nous avons divisé cette fonctionnalité en interfaces pour rendre le code plus compréhensible et « séparé » (comme dans les principes SOLID). Cela nous permet d'échanger facilement les services en fonction de la configuration.

# En conclusion

Dans le prochain post, nous examinerons plus en détail la mise en œuvre des Contrôleurs et des Vues pour utiliser ces services.