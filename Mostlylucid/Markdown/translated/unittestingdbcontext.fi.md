# (Simple) Yksikkö Testaa blogia Osa 1 - Palvelut

<datetime class="hidden">2024-08-25T23:00</datetime>

<!--category-- xUnit, Moq, Unit Testing -->
## Johdanto

Tässä viestissä aloitan Unit Testauksen lisäämisen tälle sivustolle. Tämä ei ole täysi oppitunti Unit Testaamisesta, vaan sarja viestejä siitä, miten lisään Unit Testaamisen tälle sivustolle.
Tässä viestissä testaan joitakin palveluita pilkkaamalla DbContextiä; näin vältän kaikki DB-erityiset shennaniganit.

[TOC]

## Miksi yksikkötesti?

Yksikkötestaus on tapa testata koodisi yksittäisiä komponentteja eristyksissä. Tämä on hyödyllistä monestakin syystä:

1. Se eristää koodisi jokaisen osan, joten on helppoa nähdä ongelmia tietyillä alueilla.
2. Se on tapa dokumentoida koodisi. Jos testi epäonnistuu, tiedät, että jokin on muuttunut koodisi alueella.

### Millaisia muita testejä on olemassa?

On olemassa useita muita testejä, joita voit tehdä. Tässä muutama esimerkki:

1. Integraatiotestaus - Testaa, miten koodisi eri osat toimivat yhdessä. ASP.NETissä voisimme käyttää työkaluja, kuten [Varmista](https://github.com/VerifyTests/Verify) testaamaan päätetapahtumien tuotosta ja vertaamaan niitä odotettuihin tuloksiin. Lisäämme tämän tulevaisuudessa.
2. End-to-End Testing - Koko sovelluksen testaaminen käyttäjän näkökulmasta. Tämä voitaisiin tehdä seuraavilla työkaluilla: [Seleeni](https://www.selenium.dev/).
3. Performance Testing - Testaa, miten sovelluksesi toimii kuormitettuna. Tämä voitaisiin tehdä seuraavilla työkaluilla: [Apache JMeter](https://jmeter.apache.org/), [Postimies](https://www.postman.com/)...................................................................................................................................... Parhaana pitämäni vaihtoehto on kuitenkin työkalu nimeltä [k6](https://k6.io/).
4. Turvallisuustestaus - Testaa, kuinka turvallinen sovelluksesi on. Tämä voitaisiin tehdä seuraavilla työkaluilla: [OWASP ZAP](https://www.zaproxy.org/), [Burp-sviitti](https://portswigger.net/burp), [Nessus](https://www.tenable.com/products/nessus).
5. End User Testing - Testaa, miten sovelluksesi toimii loppukäyttäjälle. Tämä voitaisiin tehdä seuraavilla työkaluilla: [Käyttäjätestit](https://www.usertesting.com/), [UserZoom](https://www.userzoom.com/), [Käyttäjät](https://www.userlytics.com/).

## Testiprojektin valmistelu

Aion käyttää xUnitia kokeisiini. Tätä käytetään oletuksena ASP.NET Core -hankkeissa. Aion myös käyttää Moqia pilkatakseni DbContextiä.

- MoqQueryable - Tässä on hyödyllisiä laajennuksia IQueryable-esineiden pilkkaamiseen.
- Moq.EntityFrameworkCore - Tässä on hyödyllisiä laajennuksia DbContext-objektien pilkkaamiseen.

## DbContextin peukalointi

Valmistautuessani tähän lisäsin DbContextilleni rajapinnan. Näin voin pilkata DbContextiä testeissäni. Tässä on rajapinta:

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

Se on aika yksinkertaista, paljastaen vain DBSetit ja SaveChangesAsync -menetelmän.

Minä *Älä* Käytä koodissani arkistokuviota. Tämä johtuu siitä, että Entity Framework Core on jo arkistomalli. Käytän palvelukerrosta vuorovaikutuksessa DbContextin kanssa. Tämä johtuu siitä, että en halua ottaa pois Entity Framework Coren voimaa.

Sitten lisäämme uuden luokan meidän `Mostlylucid.Test` Project with a extension method to state our quering:

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

Huomaat, että tämä käyttää `MockQueryable.Moq` laajennusmenetelmä mokan luomiseksi. Joka sitten asettaa meidän IQueryable esineitä ja IAsyncQueryable esineitä.

### Testin valmistelu

Yksikön testaamisen ydinajatus on, että jokaisen testin tulee olla "työn yksikkö" eikä se saa olla riippuvainen minkään muun testin tuloksista (tämän vuoksi pilkkaamme DbContextiä).

Uuteen `BlogServiceFetchTests` Luokalla asetamme testikontekstimme rakentajalle:

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

Olen kommentoinut tätä aika paljon, jotta näet, mitä on tekeillä. Olemme perustamassa `ServiceCollection` joka on kokoelma palveluita, joita voimme käyttää testeissämme. Luomme sitten pilkan `IMostlylucidDBContext` ja rekisteröivät sen `ServiceCollection`...................................................................................................................................... Sitten rekisteröimme kaikki muut testeissä tarvitsemamme palvelut. Viimeinkin rakennamme `ServiceProvider` Jota voimme käyttää saadaksemme palvelumme.

## Testin kirjoittaminen

Aloitin lisäämällä yhden koetunnin, edellä mainitut `BlogServiceFetchTests` Luokka. Tämä on testikurssi Postille saada menetelmiä minun `EFBlogService` Luokka.

Jokaisessa testissä käytetään yhteistä `SetupBlogService` menetelmä uuden asutuksen saamiseksi `EFBlogService` Esine. Näin voimme testata palvelua eristyksissä.

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

### BlogiEntityExtensions

Tämä on yksinkertainen laajennusluokka, joka antaa meille useita pentuja `BlogPostEntity` esineitä. Näin voimme testata palveluamme useilla eri esineillä.

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

Voit nähdä, että kaikki tämä tekee on palauttaa joukko blogikirjoituksia kanssa Languages and Categories. Lisäämme kuitenkin aina "juureen" esineen, jonka avulla voimme testeissämme luottaa johonkin tunnettuun kohteeseen.

### Testit

Jokainen testi on suunniteltu testaamaan yhtä virkatulosten osa-aluetta.

Esimerkiksi kahdessa alla olevassa testaamme yksinkertaisesti, että saamme kaikki virat ja että voimme saada virkoja kielellä.

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

#### Epäonnistumistesti

Yksikön testauksessa tärkeä käsite on "testaaminen", jossa todetaan, että koodisi epäonnistuu niin kuin oletat sen epäonnistuvan.

Alla olevissa testeissä testaamme ensin, että hakukoodimme toimii odotetusti. Sitten testaamme, että jos pyydämme enemmän sivuja kuin meillä on, saamme tyhjän tuloksen (eikä virhettä).

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

## Johtopäätöksenä

Tämä on yksinkertainen alku yksikkömme testaamiselle. Seuraavassa viestissä testaamme lisää palveluita ja päätepisteitä. Tarkastelemme myös, miten voimme testata päätetapahtumiamme integraatiotestauksen avulla.