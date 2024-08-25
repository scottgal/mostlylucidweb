# (Semplice) Test dell'unità Il Blog Parte 1 - Servizi

<datetime class="hidden">2024-08-25T23:00</datetime>

<!--category-- xUnit, Moq, Unit Testing -->
## Introduzione

In questo post inizierò ad aggiungere Unit Testing per questo sito. Questo non sarà un tutorial completo su Unit Testing, ma piuttosto una serie di post su come sto aggiungendo Unit Testing a questo sito.
In questo post testo alcuni servizi prendendo in giro DbContext; questo è per evitare qualsiasi shennanigan DB specifico.

[TOC]

## Perche' il test dell'unita'?

Unit Testing è un modo per testare i singoli componenti del codice in modo isolato. Ciò è utile per una serie di motivi:

1. Isola ogni componente del tuo codice rendendo semplice vedere eventuali problemi in aree specifiche.
2. E' un modo per documentare il tuo codice. Se hai un test che fallisce, sai che qualcosa è cambiato in quell'area del tuo codice.

### Quali altri tipi di test ci sono?

Ci sono un certo numero di altri tipi di test che si possono fare. Eccone alcuni:

1. Test di integrazione - Verificare come diversi componenti del codice funzionano insieme. In ASP.NET potremmo usare strumenti come [Verifica](https://github.com/VerifyTests/Verify) testare l'output degli endpoint e confrontarli con i risultati attesi. Lo aggiungeremo in futuro.
2. End-to-End Testing - Testare l'intera applicazione dal punto di vista dell'utente. Questo potrebbe essere fatto con strumenti come [Selenio](https://www.selenium.dev/).
3. Performance Testing - Verifica dell'esecuzione dell'applicazione sotto carico. Questo potrebbe essere fatto con strumenti come [Apache JMeter](https://jmeter.apache.org/), [Post ManCity name (optional, probably does not need a translation)](https://www.postman.com/). La mia opzione preferita è comunque uno strumento chiamato [k6](https://k6.io/).
4. Test di sicurezza - Verificare la sicurezza dell'applicazione. Questo potrebbe essere fatto con strumenti come [OWASP ZAP](https://www.zaproxy.org/), [Burp Suite](https://portswigger.net/burp), [NessusCity name (optional, probably does not need a translation)](https://www.tenable.com/products/nessus).
5. Test dell'utente finale - Verifica del funzionamento dell'applicazione per l'utente finale. Questo potrebbe essere fatto con strumenti come [Prova dell'utente](https://www.usertesting.com/), [Zoom utente](https://www.userzoom.com/), [Userlytics](https://www.userlytics.com/).

## Configurazione del progetto di prova

Usero' la xUnit per i miei test. Questo viene utilizzato per impostazione predefinita nei progetti ASP.NET Core. Userò anche Moq per deridere il DbContext insieme a

- MoqQueryable - Questo ha estensioni utili per deridere gli oggetti IQueryable.
- Moq.EntityFrameworkCore - Questo ha estensioni utili per deridere gli oggetti DbContext.

## Sconfiggere il DbContext

In preparazione ho aggiunto un'interfaccia per il mio DbContext. Questo è così che posso deridere il DbContext nei miei test. Ecco l'interfaccia:

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

E' abbastanza semplice, solo esporre i nostri DBSets e il metodo SaveChangesAsync.

I *Non farlo.* usare uno schema di repository nel mio codice. Questo perché Entity Framework Core è già un modello di repository. Uso un livello di servizio per interagire con il DbContext. Questo perché non voglio astrarre il potere di Entity Framework Core.

Poi aggiungiamo una nuova classe alla nostra `Mostlylucid.Test` project with an extension method to set up our querying:

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

Vedrete che questo sta usando il `MockQueryable.Moq` metodo di estensione per creare il mock. Che poi imposta i nostri oggetti IQueryable e IAsyncQueryable oggetti.

### Impostazione del test

Un principio fondamentale del test dell'unità è che ogni test dovrebbe essere un 'unità' di lavoro e non dipendere dal risultato di qualsiasi altro test (questo è il motivo per cui deridiamo il nostro DbContext).

Nel nostro nuovo `BlogServiceFetchTests` classe abbiamo impostato il nostro contesto di test nel costruttore:

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

L'ho commentato parecchio, cosi' puoi vedere cosa sta succedendo. Stiamo installando un `ServiceCollection` che è una raccolta di servizi che possiamo utilizzare nei nostri test. Poi creiamo una beffa della nostra `IMostlylucidDBContext` e registrarlo nel `ServiceCollection`. In seguito registriamo tutti gli altri servizi di cui abbiamo bisogno per i nostri test. Finalmente costruiamo il `ServiceProvider` che possiamo usare per ottenere i nostri servizi da.

## Scrivere il test

Ho iniziato aggiungendo una sola classe di test, il suddetto `BlogServiceFetchTests` classe. Questo è un corso di prova per il Post ottenere metodi del mio `EFBlogService` classe.

Ogni prova utilizza un comune `SetupBlogService` metodo per ottenere un nuovo popolamento `EFBlogService` Oggetto. Questo è in modo da poter testare il servizio in modo isolato.

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

### BlogExtensionsEntity

Questa è una semplice classe di estensione che ci dà un certo numero di pupulate `BlogPostEntity` oggetti. Questo è in modo da poter testare il nostro servizio con una serie di oggetti diversi.

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

Potete vedere che tutto questo non fa altro che restituire un certo numero di post sul blog con Lingue e Categorie. Tuttavia aggiungiamo sempre un oggetto 'root' che ci permette di poter contare su un oggetto conosciuto nei nostri test.

### Le prove

Ogni test è progettato per testare un aspetto dei risultati dei post.

Per esempio nei due sotto testiamo semplicemente che possiamo ottenere tutti i post e che possiamo ottenere i post per lingua.

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

#### Prova di guasto

Un concetto importante nel test di unità è il 'testing failure' in cui si stabilisce che il codice fallisce nel modo che ci si aspetta.

Nei test qui sotto testiamo per primi che il nostro codice di ricerca funziona come previsto. Quindi testiamo che se chiediamo più pagine di quanto abbiamo, otteniamo un risultato vuoto (e non un errore).

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

## In conclusione

Questo è un semplice inizio del nostro test dell'unità. Nel prossimo post aggiungeremo test per ulteriori servizi e endpoint. Guarderemo anche come possiamo testare i nostri endpoint usando il test di integrazione.