# Añadiendo marco de entidad para entradas de blog (Parte 2)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-15T18:00</datetime>

Usted puede encontrar todo el código fuente para las entradas del blog en [GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Parte 2 de la serie sobre la adición de Entity Framework a un proyecto.NET Core.**
Parte 1 se puede encontrar [aquí](/blog/addingentityframeworkforblogpostspt1).

# Introducción

En el post anterior, configuramos la base de datos y el contexto para nuestros posts de blog. En este post, añadiremos los servicios para interactuar con la base de datos.

En el próximo post detallaremos cómo estos servicios ahora funcionan con los controladores y vistas existentes.

[TOC]

### Configuración

Ahora tenemos una clase de extensión BlogSetup que establece estos servicios. Esta es una extensión de lo que hicimos en [Parte 1](/blog/addingentityframeworkforblogpostspt1), donde configuramos la base de datos y el contexto.

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

Esto utiliza el simple `BlogConfig` clase para definir en qué modo estamos, o bien `File` o `Database`. Basándonos en esto, registramos los servicios que necesitamos.

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

Como quiero apoyar tanto el archivo como la base de datos en esta aplicación (porque por qué no! He utilizado un enfoque basado en la interfaz permitiendo que estos sean intercambiados en base a la configuración.

Tenemos tres nuevas interfaces, `IBlogService`, `IMarkdownBlogService` y `IBlogPopulator`.

#### IBlogService

Esta es la interfaz principal para el servicio del blog. Contiene métodos para obtener puestos, categorías y puestos individuales.

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

Este servicio es utilizado por el `EFlogPopulatorService` en la primera ejecución para poblar la base de datos con mensajes de los archivos Markdown.

```csharp
public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();
}
```

Como puedes ver es bastante simple y sólo tiene dos métodos, `GetPages` y `LanguageList`. Estos se utilizan para procesar los archivos Markdown y obtener la lista de idiomas.

#### IBlogPopulador

Los BlogPopulators se utilizan en nuestro método de configuración anterior para poblar la base de datos o el objeto de caché estático (para el sistema basado en archivos) con mensajes.

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

Usted puede ver que esto es una extensión a `WebApplication` con configuración que permite ejecutar la migración de la base de datos si es necesario (que también crea la base de datos si no existe). A continuación, llama a la configuración `IBlogPopulator` servicio para poblar la base de datos.

Esta es la interfaz para ese servicio.

```csharp
public interface IBlogPopulator
{
    Task Populate();
}
```

### Aplicación

Bastante simple, ¿verdad? Esto se aplica tanto en el `MarkdownBlogPopulator` y `EFBlogPopulator` clases.

- Markdown - aquí llamamos a la `GetPages` método y poblar la caché.

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

- EF - aquí llamamos a la `IMarkdownBlogService` para obtener las páginas y luego poblar la base de datos.

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

Hemos dividido esta funcionalidad en interfaces para hacer el código más comprensible y'segregado' (como en los principios SOLID). Esto nos permite intercambiar fácilmente los servicios basados en la configuración.

# Conclusión

En el próximo post, veremos con más detalle la implementación de los Controladores y Vistas para utilizar estos servicios.