# (Simple) Test d'unité Le Blog Partie 1 - Services

<datetime class="hidden">2024-08-25T23:00</datetime>

<!--category-- xUnit, Moq, Unit Testing -->
## Présentation

Dans ce post, je vais commencer à ajouter des tests unitaires pour ce site. Ce ne sera pas un tutoriel complet sur le test d'unité, mais plutôt une série de messages sur la façon dont j'ajoute le test d'unité à ce site.
Dans ce post, je teste certains services en se moquant de DbContext; c'est pour éviter les shennanigans spécifiques à la DB.

[TOC]

## Pourquoi le test d'unité?

Unit Testing est un moyen de tester séparément les composants individuels de votre code. Ceci est utile pour un certain nombre de raisons:

1. Il isole chaque composant de votre code, ce qui rend simple de voir n'importe quel problème dans des domaines spécifiques.
2. C'est une façon de documenter votre code. Si vous avez un test qui échoue, vous savez que quelque chose a changé dans cette zone de votre code.

### Quels autres types d'essais y a-t-il?

Il y a un certain nombre d'autres types de tests que vous pouvez faire. Voici quelques-uns :

1. Test d'intégration - Test de la façon dont les différents composants de votre code fonctionnent ensemble. Dans ASP.NET, nous pourrions utiliser des outils comme [Vérifier](https://github.com/VerifyTests/Verify) de tester la sortie des paramètres et de les comparer aux résultats attendus. Nous l'ajouterons à l'avenir.
2. Test de bout en bout - Tester toute l'application du point de vue de l'utilisateur. Cela pourrait être fait avec des outils comme [Sélénium](https://www.selenium.dev/).
3. Tests de performance - Testez comment votre application fonctionne sous charge. Cela pourrait être fait avec des outils comme [Apache JMeter](https://jmeter.apache.org/), [Poste-homme](https://www.postman.com/)C'est ce que j'ai dit. Mon option préférée cependant est un outil appelé [k6](https://k6.io/).
4. Tests de sécurité - Testez à quel point votre application est sécurisée. Cela pourrait être fait avec des outils comme [ZAP OWASP](https://www.zaproxy.org/), [Suite Burp](https://portswigger.net/burp), [Nessus](https://www.tenable.com/products/nessus).
5. Test de l'utilisateur final - Test de la façon dont votre application fonctionne pour l'utilisateur final. Cela pourrait être fait avec des outils comme [Test de l'utilisateur](https://www.usertesting.com/), [UtilisateurZoom](https://www.userzoom.com/), [Userlytics](https://www.userlytics.com/).

## Mise en place du projet d'essai

Je vais utiliser xUnit pour mes tests. Ceci est utilisé par défaut dans les projets ASP.NET Core. Je vais aussi utiliser Moq pour me moquer du DbContext avec

- MoqQueryable - Ceci a des extensions utiles pour se moquer d'objets IQueryable.
- Moq.EntityFrameworkCore - Ceci a des extensions utiles pour maquiller les objets DbContext.

## Faire défiler le DbContext

En préparation, j'ai ajouté une Interface pour mon DbContext. C'est pour que je puisse me moquer du DbContext dans mes tests. Voici l'interface :

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

C'est assez simple, juste exposer nos DBSets et la méthode SaveChangesAsync.

Annexe I *Ne fais pas ça.* utiliser un modèle de dépôt dans mon code. Cela s'explique par le fait que le noyau du cadre d'entités est déjà un modèle de dépôt. J'utilise une couche de service pour interagir avec le DbContext. C'est parce que je ne veux pas effacer le pouvoir de l'Entity Framework Core.

Nous ajoutons ensuite une nouvelle classe à notre `Mostlylucid.Test` projet avec une méthode d'extension pour configurer notre requête:

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

Vous verrez que c'est en utilisant le `MockQueryable.Moq` méthode d'extension pour créer la maquette. Ce qui met alors en place nos objets IQueryable et IAsyncQueryable.

### Mise en place de l'essai

Un principe fondamental des tests unitaires est que chaque test doit être une 'unité' de travail et ne pas dépendre du résultat d'un autre test (c'est pourquoi nous nous moquons de notre DbContext).

Dans notre nouvelle `BlogServiceFetchTests` classe nous avons mis en place notre contexte de test dans le constructeur:

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

J'ai commenté ça assez fortement pour que vous puissiez voir ce qui se passe. Nous mettons en place un `ServiceCollection` qui est une collection de services que nous pouvons utiliser dans nos tests. Nous créons alors une maquette de notre `IMostlylucidDBContext` et de l'enregistrer dans le `ServiceCollection`C'est ce que j'ai dit. Nous enregistrons ensuite tous les autres services dont nous avons besoin pour nos tests. Enfin, nous construisons le `ServiceProvider` que nous pouvons utiliser pour obtenir nos services.

## Écrire le test

J'ai commencé par ajouter une seule classe de test, la ci-dessus `BlogServiceFetchTests` En cours. C'est un cours de test pour le Post obtenant des méthodes de mon `EFBlogService` En cours.

Chaque test utilise un commun `SetupBlogService` méthode pour obtenir une nouvelle population `EFBlogService` objet. C'est pour que nous puissions tester le service isolément.

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

### BlogEntitéExtensions

C'est une classe d'extension simple qui nous donne un certain nombre de pupulés `BlogPostEntity` objets. C'est pour que nous puissions tester notre service avec un certain nombre d'objets différents.

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

Vous pouvez voir que tout ce que cela fait est de retourner un nombre défini de billets de blog avec des langues et des catégories. Cependant, nous ajoutons toujours un objet 'root' qui nous permet de nous fier à un objet connu dans nos tests.

### Les essais

Chaque test est conçu pour tester un aspect des résultats des poteaux.

Par exemple dans les deux ci-dessous, nous testons simplement que nous pouvons obtenir tous les messages et que nous pouvons obtenir des messages par langue.

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

#### Essai de défaillance

Un concept important dans les tests unitaires est "test d'échec" où vous établissez que votre code échoue de la manière que vous attendez de lui.

Dans les tests ci-dessous, nous testons d'abord que notre code de téléappel fonctionne comme prévu. Nous testons ensuite que si nous demandons plus de pages que nous avons, nous obtenons un résultat vide (et non une erreur).

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

## En conclusion

C'est un début simple pour notre test d'unité. Dans le prochain post, nous ajouterons des tests pour plus de services et de paramètres. Nous examinerons également comment nous pouvons tester nos paramètres à l'aide de tests d'intégration.