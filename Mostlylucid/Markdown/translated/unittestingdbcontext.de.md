# (Einfach) Unit Testing The Blog Teil 1 - Dienstleistungen

<datetime class="hidden">2024-08-25T23:00</datetime>

<!--category-- xUnit, Moq, Unit Testing -->
## Einleitung

In diesem Beitrag werde ich mit dem Hinzufügen von Unit Testing für diese Website beginnen. Dies wird nicht ein vollständiges Tutorial zu Unit Testing, sondern eine Reihe von Beiträgen, wie ich hinzufügen Unit Testing zu dieser Website.
In diesem Beitrag teste ich einige Dienste durch Spott DbContext; dies ist, um jede DB spezifische Shennanigans zu vermeiden.

[TOC]

## Warum Einheitstest?

Unit Testing ist eine Möglichkeit, einzelne Komponenten Ihres Codes isoliert zu testen. Dies ist aus mehreren Gründen nützlich:

1. Es isoliert jede Komponente Ihres Codes und macht es einfach, Probleme in bestimmten Bereichen zu sehen.
2. Es ist eine Art, Ihren Code zu dokumentieren. Wenn Sie einen Test haben, der fehlschlägt, wissen Sie, dass sich etwas in diesem Bereich Ihres Codes geändert hat.

### Welche anderen Arten von Tests gibt es?

Es gibt eine Reihe von anderen Arten von Tests, die Sie tun können. Hier sind ein paar:

1. Integration Testing - Testen, wie verschiedene Komponenten Ihres Codes zusammenarbeiten. In ASP.NET könnten wir Werkzeuge wie [Überprüfen](https://github.com/VerifyTests/Verify) die Ausgabe der Endpunkte zu testen und mit den erwarteten Ergebnissen zu vergleichen. Wir werden das in Zukunft hinzufügen.
2. End-to-End Testing - Testen der gesamten Anwendung aus der Sicht des Benutzers. Dies könnte mit Werkzeugen wie [Selen, auch mit Zusatz von Zucker oder anderen Süßmitteln, mit Zusatz von Zucker oder anderen Süßmitteln, mit Zusatz von Zucker oder anderen Süßmitteln, mit Zusatz von Zucker oder anderen Süßmitteln, auch mit Zusatz von Zucker oder anderen Süßmitteln, mit Zusatz von Zucker oder anderen Süßmitteln, mit Zusatz von Zucker oder anderen Süßmitteln, mit Zusatz von Zucker oder anderen Süßmitteln, mit Zusatz von Zucker oder anderen Süßmitteln, mit Zusatz von Zucker oder anderen Süßmitteln, mit Zusatz von Zucker oder anderen Süßmitteln:](https://www.selenium.dev/).
3. Performance Testing - Testen, wie Ihre Anwendung unter Last führt. Dies könnte mit Werkzeugen wie [Apache JMeter](https://jmeter.apache.org/), [PostMan](https://www.postman.com/)......................................................................................................... Meine bevorzugte Option ist jedoch ein Werkzeug namens [K6](https://k6.io/).
4. Security Testing - Testen, wie sicher Ihre Anwendung ist. Dies könnte mit Werkzeugen wie [OWASP ZAP](https://www.zaproxy.org/), [Burp-Suite](https://portswigger.net/burp), [Nessus](https://www.tenable.com/products/nessus).
5. End User Testing - Testen, wie Ihre Anwendung für den Endbenutzer funktioniert. Dies könnte mit Werkzeugen wie [Benutzertests](https://www.usertesting.com/), [BenutzerZoom](https://www.userzoom.com/), [Benutzeranalysen](https://www.userlytics.com/).

## Einrichtung des Testprojekts

Ich werde xUnit für meine Tests verwenden. Dies wird standardmäßig in ASP.NET Core Projekten verwendet. Ich werde auch Moq benutzen, um den DbContext zusammen mit

- MoqQueryable - Dies hat nützliche Erweiterungen für die Spott IQueryable Objekte.
- Moq.EntityFrameworkCore - Dies hat nützliche Erweiterungen zum Spotten von DbContext-Objekten.

## Den DbContext vermasseln

In Vorbereitung darauf habe ich ein Interface für meinen DbContext hinzugefügt. Das ist so, dass ich den DbContext in meinen Tests verspotten kann. Hier ist die Schnittstelle:

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

Es ist ziemlich einfach, nur unsere DBSets und die SaveChangesAsync-Methode zu entlarven.

I. ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNGEN *Nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein.* ein Repository-Muster in meinem Code verwenden. Das liegt daran, dass Entity Framework Core bereits ein Repository-Muster ist. Ich benutze eine Service Layer, um mit dem DbContext zu interagieren. Das liegt daran, dass ich die Macht des Entity Framework Core nicht abstrahieren will.

Wir fügen dann eine neue Klasse zu unserem `Mostlylucid.Test` Projekt mit einer Erweiterungsmethode, um unsere Abfrage einzurichten:

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

Sie werden sehen, dass dies die `MockQueryable.Moq` Erweiterungsmethode, um den Mock zu erstellen. Was dann unsere IQueryable Objekte und IAsyncQueryable Objekte aufstellt.

### Einrichtung des Tests

Ein Kerngrundsatz von Unit Testing ist, dass jeder Test eine 'Einheit' der Arbeit sein sollte und nicht vom Ergebnis eines anderen Tests abhängig ist (aus diesem Grund verspotten wir unseren DbContext).

In unserem neuen `BlogServiceFetchTests` Klasse haben wir unseren Testkontext im Konstruktor eingerichtet:

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

Ich habe das ziemlich heftig kommentiert, damit du sehen kannst, was los ist. Wir bauen eine `ServiceCollection` Das ist eine Sammlung von Dienstleistungen, die wir in unseren Tests nutzen können. Dann erschaffen wir einen Spott über unsere `IMostlylucidDBContext` und registrieren Sie es in der `ServiceCollection`......................................................................................................... Wir registrieren dann alle anderen Dienstleistungen, die wir für unsere Tests benötigen. Schließlich bauen wir die `ServiceProvider` von denen wir unsere Dienste beziehen können.

## Den Test schreiben

Ich begann mit dem Hinzufügen einer einzigen Testklasse, die oben genannten `BlogServiceFetchTests` Unterricht. Dies ist ein Testkurs für die Post immer Methoden meiner `EFBlogService` Unterricht.

Jeder Test verwendet eine gemeinsame `SetupBlogService` Methode, um eine neue Bevölkerung zu erhalten `EFBlogService` Gegenstand. Damit wir den Dienst isoliert testen können.

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

### BlogEntityErweiterungen

Dies ist eine einfache Erweiterungsklasse, die uns eine Reihe von pupulierten `BlogPostEntity` Gegenstand. Damit wir unseren Service mit einer Reihe verschiedener Objekte testen können.

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

Sie können sehen, dass alles, was dies tut, eine bestimmte Anzahl von Blog-Beiträgen mit Sprachen und Kategorien zurückgibt. Wir fügen jedoch immer ein 'root' Objekt hinzu, das es uns erlaubt, uns in unseren Tests auf ein bekanntes Objekt verlassen zu können.

### Die Prüfungen

Jeder Test ist so konzipiert, dass er einen Aspekt der Ergebnisse der Beiträge prüft.

In den beiden untenstehenden Beispielen testen wir einfach, dass wir alle Beiträge bekommen können und dass wir Beiträge nach Sprache bekommen können.

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

#### Prüfung auf Versagen

Ein wichtiges Konzept in Unit-Testing ist 'Testfehler', wo Sie feststellen, dass Ihr Code in der Art und Weise, wie Sie es erwarten, fehlschlägt.

In den Tests unten testen wir zunächst, dass unser Paging-Code wie erwartet funktioniert. Wir testen dann, dass wir, wenn wir nach mehr Seiten fragen, als wir haben, ein leeres Ergebnis (und kein Fehler) erhalten.

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

## Schlussfolgerung

Dies ist ein einfacher Start für unsere Unit Testing. Im nächsten Beitrag werden wir Tests für weitere Dienste und Endpunkte hinzufügen. Wir werden auch untersuchen, wie wir unsere Endpunkte mit Integrationstests testen können.