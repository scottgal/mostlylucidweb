# (Enkel) Enhetstestning Bloggen Del 1 - Tjänster

<datetime class="hidden">2024-08-25T23:00</datetime>

<!--category-- xUnit, Moq, Unit Testing -->
## Inledning

I det här inlägget kommer jag att börja lägga till Unit Testing för denna webbplats. Detta kommer inte att vara en fullständig handledning om Unit Testing, utan snarare en serie inlägg om hur jag lägger Unit Testing till denna webbplats.
I det här inlägget testar jag några tjänster genom att håna DbContext; detta är för att undvika eventuella DB-specifika shennanigans.

[TOC]

## Varför ett enhetstest?

Unit Testing är ett sätt att testa enskilda komponenter i din kod isolerat. Detta är användbart av flera skäl:

1. Det isolerar varje komponent i din kod vilket gör det enkelt att se några problem inom specifika områden.
2. Det är ett sätt att dokumentera din kod. Om du har ett test som misslyckas, du vet att något har förändrats i det området av din kod.

### Vilka andra typer av tester finns det?

Det finns ett antal andra typer av tester som du kan göra. Här följer några exempel:

1. Integration Testing - Testa hur olika komponenter i din kod fungerar tillsammans. I ASP.NET kunde vi använda verktyg som [Verifiera](https://github.com/VerifyTests/Verify) för att testa resultaten av endpoints och jämföra dem med förväntade resultat. Vi lägger till det här i framtiden.
2. End-to-End Testing - Testa hela programmet ur användarens perspektiv. Detta kan göras med verktyg som [Med en tjocklek av mer än 0,15 mm men högst 0,15 mm](https://www.selenium.dev/).
3. Prestandatest - Testa hur din applikation fungerar under belastning. Detta kan göras med verktyg som [Apache JMeter Ordförande](https://jmeter.apache.org/), [Postman](https://www.postman.com/)....................................... Mitt föredragna alternativ är dock ett verktyg som kallas [k6 Ordförande](https://k6.io/).
4. Säkerhetstestning - Testa hur säker din applikation är. Detta kan göras med verktyg som [OWASP ZAP](https://www.zaproxy.org/), [Burp-sviten](https://portswigger.net/burp), [Nessus Ordförande](https://www.tenable.com/products/nessus).
5. Slutanvändartest - Testa hur din applikation fungerar för slutanvändaren. Detta kan göras med verktyg som [Användartest](https://www.usertesting.com/), [AnvändareZoom](https://www.userzoom.com/), [Analysmetod [1]Bestämning av halten triklorisocyanat i fodertillsatsen och i aromämnesförblandningar:](https://www.userlytics.com/).

## Sätta upp testprojektet

Jag kommer att använda xUnit för mina tester. Detta används som standard i ASP.NET Core-projekt. Jag kommer också att använda Moq för att håna DbContext tillsammans med

- MoqQueryable - Detta har användbara tillägg för att håna IQueryable objekt.
- Moq.EntityFrameworkCore - Detta har användbara tillägg för att håna DbContext objekt.

## Mockning av DbContext

Som förberedelse för detta lade jag till ett gränssnitt för min DbContext. Det här är så att jag kan håna DbContext i mina tester. Här är gränssnittet:

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

Det är ganska enkelt, bara att exponera våra DBSets och SaveChangesAsync-metoden.

I *Gör det inte.* Använd ett arkivmönster i min kod. Detta beror på att Entity Framework Core redan är ett arkivmönster. Jag använder ett servicelager för att interagera med DbContext. Det beror på att jag inte vill ta bort kraften i Entity Framework Core.

Vi lägger sedan till en ny klass till vår `Mostlylucid.Test` projekt med en förlängningsmetod för att ställa in vår förfrågan:

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

Du kommer att se att detta använder `MockQueryable.Moq` förlängningsmetod för att skapa mock. Som sedan sätter upp våra IQueryable objekt och IAsyncQueryable objekt.

### Sätta upp testet

En kärna av enhetstestning är att varje test ska vara en "enhet" av arbete och inte beror på resultatet av någon annan test (det är därför vi hånar vår DbContext).

I vår nya `BlogServiceFetchTests` klass vi satte upp vårt test sammanhang i konstruktören:

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

Jag har kommenterat detta ganska tungt så att du kan se vad som händer. Vi sätter upp en `ServiceCollection` vilket är en samling tjänster som vi kan använda i våra tester. Vi gör då ett hån mot vår `IMostlylucidDBContext` och registrera det i `ServiceCollection`....................................... Vi registrerar sedan alla andra tjänster som vi behöver för våra tester. Slutligen bygger vi `ServiceProvider` som vi kan använda för att få våra tjänster från.

## Att skriva provet

Jag började med att lägga till en enda test klass, den ovannämnda `BlogServiceFetchTests` Klassen. Detta är en test klass för Post få metoder för min `EFBlogService` Klassen.

Varje test använder en vanlig `SetupBlogService` metod för att få en nybefolkad `EFBlogService` motsätter sig detta. Detta för att vi ska kunna testa tjänsten isolerat.

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

### BloggEntityExtensions

Detta är en enkel förlängning klass som ger oss ett antal pupulerade `BlogPostEntity` Föremål. Detta för att vi ska kunna testa vår service med ett antal olika objekt.

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

Du kan se att allt detta gör är att returnera ett antal blogginlägg med språk och kategorier. Men vi lägger alltid till ett "root"-objekt som gör att vi kan lita på ett känt objekt i våra tester.

### Testerna

Varje test är utformat för att testa en aspekt av inläggens resultat.

Till exempel i de två nedan så testar vi helt enkelt att vi kan få alla inlägg och att vi kan få inlägg efter språk.

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

#### Test för misslyckande

Ett viktigt begrepp i Unit testing är "testa misslyckande" där du fastställer att din kod misslyckas på det sätt du förväntar dig det.

I testerna nedan testar vi först att vår personsökningskod fungerar som förväntat. Vi testar sedan att om vi ber om fler sidor än vi har, får vi ett tomt resultat (och inte ett fel).

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

## Slutsatser

Det här är en enkel start på vår enhetstestning. I nästa inlägg lägger vi till testning för fler tjänster och slutpunkter. Vi ska också titta på hur vi kan testa våra endpoints med Integration Testing.