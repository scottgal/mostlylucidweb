# (Simple) Unidad de pruebas El Blog Parte 1 - Servicios

<datetime class="hidden">2024-08-25T23:00</datetime>

<!--category-- xUnit, Moq, Unit Testing -->
## Introducción

En este post empezaré a agregar pruebas de unidad para este sitio. Esto no será un tutorial completo sobre Pruebas de Unidad, sino más bien una serie de posts sobre cómo estoy agregando Pruebas de Unidad a este sitio.
En este post pruebo algunos servicios burlándome de DbContext; esto es para evitar cualquier shennanigans específicos de DB.

[TOC]

## ¿Por qué la prueba de unidad?

Pruebas de unidad es una forma de probar componentes individuales de su código de forma aislada. Esto es útil por varias razones:

1. Aisla cada componente de tu código haciendo que sea simple ver cualquier problema en áreas específicas.
2. Es una forma de documentar tu código. Si usted tiene una prueba que falla, usted sabe que algo ha cambiado en esa área de su código.

### ¿Qué otros tipos de pruebas existen?

Hay un número de otros tipos de pruebas que usted puede hacer. Estos son algunos:

1. Pruebas de integración - Pruebas de cómo los diferentes componentes de su código trabajan juntos. En ASP.NET podríamos utilizar herramientas como [Verificar](https://github.com/VerifyTests/Verify) para probar la salida de los puntos finales y compararlos con los resultados esperados. Añadiremos esto en el futuro.
2. Pruebas de extremo a extremo - Pruebas de toda la aplicación desde la perspectiva del usuario. Esto podría hacerse con herramientas como [Selenio](https://www.selenium.dev/).
3. Pruebas de rendimiento - Pruebas de cómo funciona su aplicación bajo carga. Esto podría hacerse con herramientas como [Apache JMeter](https://jmeter.apache.org/), [PostMan](https://www.postman.com/). Mi opción preferida sin embargo es una herramienta llamada [k6](https://k6.io/).
4. Pruebas de seguridad - Pruebas de la seguridad de su aplicación. Esto podría hacerse con herramientas como [OWASP ZAP](https://www.zaproxy.org/), [Burp Suite](https://portswigger.net/burp), [Nessus](https://www.tenable.com/products/nessus).
5. Pruebas de usuario finales - Pruebas de cómo funciona su aplicación para el usuario final. Esto podría hacerse con herramientas como [Pruebas de usuario](https://www.usertesting.com/), [UserZoom](https://www.userzoom.com/), [Userlytics](https://www.userlytics.com/).

## Configuración del proyecto de prueba

Voy a usar xUnit para mis pruebas. Esto se utiliza por defecto en proyectos ASP.NET Core. También voy a usar Moq para burlarme del DbContext junto con

- MoqQueryable - Esto tiene extensiones útiles para burlarse de los objetos IQueryable.
- Moq.EntityFrameworkCore - Esto tiene extensiones útiles para burlarse de los objetos DbContext.

## Burlando el DbContext

En preparación para esto añadí una interfaz para mi DbContext. Esto es para que pueda burlarme del DbContext en mis pruebas. Aquí está la interfaz:

```csharp
namespace Mostlylucid.EntityFramework;

public interface IMostlylucidDBContext
{
    public DbSet<CommentEntity> Comments { get; set; }
    public DbSet<BlogPostEntity> BlogPosts { get; set; }
    public DbSet<CategoryEntity> Categories { get; set; }

    public DbSet<LanguageEntity> Languages { get; set; }
    
    public DatabaseFacade Database { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}

```

Es bastante simple, sólo exponer nuestros conjuntos DBS y el método SaveChangesAsync.

I *No lo hagas.* utilizar un patrón de repositorio en mi código. Esto se debe a que Entity Framework Core ya es un patrón de repositorio. Uso una capa de servicio para interactuar con el DbContext. Esto es porque no quiero abstraer el poder de Entity Framework Core.

Luego añadimos una nueva clase a nuestra `Mostlylucid.Test` proyecto con un método de extensión para configurar nuestra consulta:

```csharp
public static class MockDbSetExtensions
{
    public static Mock<DbSet<T>> CreateDbSetMock<T>(this IEnumerable<T> sourceList) where T : class
    {
        // Use the MockQueryable.Moq extension method to create the mock
        return sourceList.AsQueryable().BuildMockDbSet();
    }

    // SetupDbSet remains the same, just uses the updated CreateDbSetMock
    public static void SetupDbSet<T>(this Mock<IMostlylucidDBContext> mockContext, IEnumerable<T> entities,
        Expression<Func<IMostlylucidDBContext, DbSet<T>>> dbSetProperty) where T : class
    {
        var dbSetMock = entities.CreateDbSetMock();
        mockContext.Setup(dbSetProperty).Returns(dbSetMock.Object);
    }
}
```

Verás que esto está usando el `MockQueryable.Moq` método de extensión para crear el simulacro. Que luego establece nuestros objetos IQueryable y objetos IAsyncQueryable.

### Configuración de la prueba

Un principio básico de las pruebas de unidad es que cada prueba debe ser una "unidad" de trabajo y no depender del resultado de cualquier otra prueba (es por eso que nos burlamos de nuestro DbContext).

En nuestro nuevo `BlogServiceFetchTests` La clase que establecemos nuestro contexto de prueba en el constructor:

```csharp
  public BlogServiceFetchTests()
    {
        // 1. Setup ServiceCollection for DI
        var services = new ServiceCollection();
        // 2. Create a mock of IMostlylucidDbContext
        _dbContextMock = new Mock<IMostlylucidDBContext>();
        // 3. Register the mock of IMostlylucidDbContext into the ServiceCollection
        services.AddSingleton(_dbContextMock.Object);
        // Optionally register other services
        services.AddScoped<IBlogService, EFBlogService>(); // Example service that depends on IMostlylucidDbContext
        services.AddLogging(configure => configure.AddConsole());
        services.AddScoped<MarkdownRenderingService>();
        // 4. Build the service provider
        _serviceProvider = services.BuildServiceProvider();
    }
```

He comentado esto bastante fuerte para que puedan ver lo que está pasando. Estamos montando un `ServiceCollection` que es una colección de servicios que podemos utilizar en nuestras pruebas. Luego creamos una burla de nuestro `IMostlylucidDBContext` y registrarlo en el `ServiceCollection`. Luego registramos cualquier otro servicio que necesitemos para nuestras pruebas. Finalmente construimos el `ServiceProvider` que podemos usar para obtener nuestros servicios de.

## Escribir la prueba

Comencé añadiendo una sola clase de prueba, la antes mencionada `BlogServiceFetchTests` clase. Esta es una clase de prueba para el Post conseguir métodos de mi `EFBlogService` clase.

Cada prueba utiliza un común `SetupBlogService` método para obtener un nuevo poblado `EFBlogService` objeto. Esto es para que podamos probar el servicio de forma aislada.

```csharp
    private IBlogService SetupBlogService(List<BlogPostEntity>? blogPosts = null)
    {
        blogPosts ??= BlogEntityExtensions.GetBlogPostEntities(5);

        // Setup the DbSet for BlogPosts in the mock DbContext
        _dbContextMock.SetupDbSet(blogPosts, x => x.BlogPosts);

        // Resolve the IBlogService from the service provider
        return _serviceProvider.GetRequiredService<IBlogService>();
    }

```

### BlogEntityExtensions

Esta es una simple clase de extensión que nos da un número de pupulados `BlogPostEntity` objetos. Esto es para que podamos probar nuestro servicio con una serie de objetos diferentes.

```csharp
 public static List<BlogPostEntity> GetBlogPostEntities(int count, string? langName = "")
    {
        var langs = LanguageExtensions.GetLanguageEntities();

        if (!string.IsNullOrEmpty(langName)) langs = new List<LanguageEntity> { langs.First(x => x.Name == langName) };

        var langCount = langs.Count;
        var categories = CategoryEntityExtensions.GetCategoryEntities();
        var entities = new List<BlogPostEntity>();

        var enLang = langs.First();
        var cat1 = categories.First();

        // Add a root post to the list to test the category filter.
        var rootPost = new BlogPostEntity
        {
            Id = 0,
            Title = "Root Post",
            Slug = "root-post",
            HtmlContent = "<p>Html Content</p>",
            PlainTextContent = "PlainTextContent",
            Markdown = "# Markdown",
            PublishedDate = DateTime.ParseExact("2025-01-01T07:01", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            UpdatedDate = DateTime.ParseExact("2025-01-01T07:01", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            LanguageEntity = enLang,
            Categories = new List<CategoryEntity> { cat1 }
        };
        entities.Add(rootPost);
        for (var i = 1; i < count; i++)
        {
            var langIndex = (i - 1) % langCount;
            var language = langs[langIndex];
            var postCategories = categories.Take(i - 1 % categories.Count).ToList();
            var dayDate = (i + 1 % 30 + 1).ToString("00");
            entities.Add(new BlogPostEntity
            {
                Id = i,
                Title = $"Title {i}",
                Slug = $"slug-{i}",
                HtmlContent = $"<p>Html Content {i}</p>",
                PlainTextContent = $"PlainTextContent {i}",
                Markdown = $"# Markdown {i}",
                PublishedDate = DateTime.ParseExact($"2025-01-{dayDate}T07:01", "yyyy-MM-ddTHH:mm",
                    CultureInfo.InvariantCulture),
                UpdatedDate = DateTime.ParseExact($"2025-01-{dayDate}T07:01", "yyyy-MM-ddTHH:mm",
                    CultureInfo.InvariantCulture),
                LanguageEntity = new LanguageEntity
                {
                    Id = language.Id,
                    Name = language.Name
                },
                Categories = postCategories
            });
        }

        return entities;
    }
```

Puedes ver que todo lo que esto hace es devolver un número establecido de entradas de blog con Idiomas y Categorías. Sin embargo, siempre añadimos un objeto 'root' que nos permite confiar en un objeto conocido en nuestras pruebas.

### Las pruebas

Cada prueba está diseñada para probar un aspecto de los resultados de los posts.

Por ejemplo, en los dos de abajo simplemente probamos que podemos obtener todos los posts y que podemos obtener posts por idioma.

```csharp
    [Fact]
    public async Task TestBlogService_GetBlogsByLanguage_ReturnsBlogs()
    {
        var blogService = SetupBlogService();

        // Act
        var result = await blogService.GetPostsForLanguage(language: "es");

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task TestBlogService_GetAllBlogs_ReturnsBlogs()
    {
        var blogs = BlogEntityExtensions.GetBlogPostEntities(2);
        var blogService = SetupBlogService(blogs);
        // Act
        var result = await blogService.GetAllPosts();

        // Assert
        Assert.Equal(2, result.Count());
    }
```

#### Prueba de fallo

Un concepto importante en las pruebas de la unidad es el "fracaso de pruebas" donde se establece que el código falla de la manera que se espera que lo haga.

En las pruebas de abajo primero probamos que nuestro código de paginación funciona como se esperaba. Luego probamos que si pedimos más páginas de las que tenemos, obtenemos un resultado vacío (y no un error).

```csharp
    [Fact]
    public async Task TestBlogServicePagination_GetBlogsByCategory_ReturnsBlogs()
    {
        var blogPosts = BlogEntityExtensions.GetBlogPostEntities(10, "en");
        var blogService = SetupBlogService(blogPosts);

        // Act
        var result = await blogService.GetPagedPosts(2, 5);

        // Assert
        Assert.Equal(5, result.Posts.Count);
    }

    [Fact]
    public async Task TestBlogServicePagination_GetBlogsByCategory_FailsBlogs()
    {
        var blogPosts = BlogEntityExtensions.GetBlogPostEntities(10, "en");
        var blogService = SetupBlogService(blogPosts);

        // Act
        var result = await blogService.GetPagedPosts(10, 5);

        // Assert
        Assert.Empty(result.Posts);
    }
```

## Conclusión

Este es un simple comienzo para nuestra Unidad de Pruebas. En el próximo post añadiremos pruebas para más servicios y puntos finales. También veremos cómo podemos probar nuestros puntos finales usando la prueba de integración.