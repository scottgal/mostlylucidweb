# (Simple) Unit Testing The Blog Part 1 - Services

<datetime class="hidden">2024-08-25T23:00</datetime>

<!--category-- xUnit, Moq, Unit Testing -->
## Inleiding

In dit bericht zal ik beginnen met het toevoegen van Unit Testing voor deze site. Dit zal niet een volledige tutorial op Unit Testing, maar eerder een reeks berichten over hoe ik het toevoegen van Unit Testing aan deze site.
In dit bericht test ik een aantal diensten door DbContext te bespotten; dit is om elke DB specifieke shennanigans te vermijden.

[TOC]

## Waarom Unit Test?

Unit Testing is een manier om individuele componenten van uw code afzonderlijk te testen. Dit is nuttig om een aantal redenen:

1. Het isoleert elk onderdeel van uw code waardoor het eenvoudig is om problemen in specifieke gebieden te zien.
2. Het is een manier om je code te documenteren. Als je een test hebt die faalt, weet je dat er iets is veranderd in dat gebied van je code.

### Welke andere soorten tests zijn er?

Er zijn een aantal andere soorten testen die u kunt doen. Hier zijn een paar:

1. Integratie Testen - Testen hoe verschillende componenten van uw code samenwerken. In ASP.NET kunnen we tools gebruiken zoals [Verifiëren](https://github.com/VerifyTests/Verify) om de output van eindpunten te testen en te vergelijken met de verwachte resultaten. We voegen dit toe in de toekomst.
2. End-to-End Testing - Het testen van de hele toepassing vanuit het perspectief van de gebruiker. Dit kan worden gedaan met tools zoals [Selenium](https://www.selenium.dev/).
3. Performance Testing - Testing how your application presteert under load. Dit kan worden gedaan met tools zoals [Apache JMeter](https://jmeter.apache.org/), [PostMan](https://www.postman.com/). Mijn voorkeursoptie is echter een tool genaamd [k6](https://k6.io/).
4. Beveiligingstest - Testen hoe veilig uw toepassing is. Dit kan worden gedaan met tools zoals [OWASP ZAP](https://www.zaproxy.org/), [Burp Suite](https://portswigger.net/burp), [Nessus](https://www.tenable.com/products/nessus).
5. End User Testing - Testen hoe uw applicatie werkt voor de eindgebruiker. Dit kan worden gedaan met tools zoals [UserTesting](https://www.usertesting.com/), [GebruikerZoom](https://www.userzoom.com/), [Userlytics](https://www.userlytics.com/).

## Instellen van het testproject

Ik ga xUnit gebruiken voor mijn testen. Dit wordt standaard gebruikt in ASP.NET Core projecten. Ik ga ook Moq gebruiken om de DbContext te bespotten samen met

- MoqQueryable - Dit heeft nuttige extensies voor het bespotten van IQueryable objecten.
- Moq.EntityFrameworkCore - Dit heeft nuttige extensies voor het bespotten van DbContext objecten.

## De DbContext vermocken

Ter voorbereiding hierop heb ik een Interface toegevoegd voor mijn DbContext. Dit is zodat ik de DbContext kan bespotten in mijn testen. Hier is de interface:

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

Het is vrij eenvoudig, gewoon ontmaskeren van onze DBSets en de SaveChangesAsync methode.

I *Niet doen.* gebruik een repository patroon in mijn code. Dit komt omdat Entity Framework Core al een repository patroon is. Ik gebruik een servicelaag om te communiceren met de DbContext. Dit komt omdat ik de kracht van Entity Framework Core niet wil wegnemen.

We voegen dan een nieuwe klasse toe aan onze `Mostlylucid.Test` project met een extensiemethode om onze zoekopdracht op te zetten:

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

Je zult zien dat dit gebruik maakt van de `MockQueryable.Moq` extensie methode om de mock te maken. Dat stelt dan onze IQueryable objecten en IAsyncQueryable objecten.

### Instellen van de test

Een kernprincipe van Unit Testing is dat elke test een 'eenheid' van het werk moet zijn en niet afhankelijk moet zijn van het resultaat van een andere test (hierom bespotten we onze DbContext).

In onze nieuwe `BlogServiceFetchTests` klasse zetten we onze testcontext in de constructeur op:

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

Ik heb dit behoorlijk zwaar becommentarieerd zodat je kunt zien wat er aan de hand is. We zijn bezig met het opzetten van een `ServiceCollection` Dat is een verzameling van diensten die we kunnen gebruiken in onze tests. Wij maken dan een bespotting van ons `IMostlylucidDBContext` en het in het register op te nemen. `ServiceCollection`. Vervolgens registreren we alle andere diensten die we nodig hebben voor onze tests. De Voorzitter. - Aan de orde is het gecombineerd debat over `ServiceProvider` die we kunnen gebruiken om onze diensten van te krijgen.

## De test schrijven

Ik begon met het toevoegen van een enkele test klasse, de bovengenoemde `BlogServiceFetchTests` Klas. Dit is een testklas voor de Post het krijgen van methoden van mijn `EFBlogService` Klas.

Elke test maakt gebruik van een gemeenschappelijke `SetupBlogService` methode om een nieuwe bevolkte `EFBlogService` object. Dit is zodat we de dienst afzonderlijk kunnen testen.

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

Dit is een eenvoudige extensie klasse die geeft ons een aantal gepupilde `BlogPostEntity` objecten. Dit is zodat we onze service kunnen testen met een aantal verschillende objecten.

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

U kunt zien dat alles wat dit doet is het retourneren van een aantal blog posts met Talen en Categorieën. We voegen echter altijd een 'root' object toe waardoor we in onze testen op een bekend object kunnen vertrouwen.

### De tests

Elke test is ontworpen om één aspect van de resultaten van de posten te testen.

Bijvoorbeeld in de twee hieronder testen we simpelweg dat we alle berichten kunnen krijgen en dat we berichten per taal kunnen krijgen.

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

#### Test voor storing

Een belangrijk concept in Unit testing is 'testfout' waarbij je vaststelt dat je code faalt op de manier die je verwacht.

In de onderstaande tests testen we eerst of onze paging code werkt zoals verwacht. We testen dan dat als we meer pagina's vragen dan we hebben, we een leeg resultaat krijgen (en geen fout).

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

## Conclusie

Dit is een simpele start van onze Unit Testing. In de volgende post voegen we testen voor meer diensten en eindpunten toe. We zullen ook kijken hoe we onze eindpunten kunnen testen met behulp van Integratie Testing.