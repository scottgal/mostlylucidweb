# (Simple) Unit Testing The Blog Part 1 - Υπηρεσίες

<datetime class="hidden">2024-08-25T23:00</datetime>

<!--category-- xUnit, Moq, Unit Testing -->
## Εισαγωγή

Σε αυτή τη θέση θα αρχίσω να προσθέτω τις δοκιμές μονάδας για αυτό το site. Αυτό δεν θα είναι ένα πλήρες φροντιστήριο για τις δοκιμές μονάδας, αλλά μάλλον μια σειρά από θέσεις σχετικά με το πώς είμαι προσθήκη δοκιμή μονάδας σε αυτό το site.
Σε αυτό το άρθρο δοκιμάζω κάποιες υπηρεσίες χλευάζοντας DbContext?Αυτό είναι για να αποφευχθεί οποιαδήποτε συγκεκριμένη DB shenanigans.

[TOC]

## Γιατί Τεστ Μονάδας;

Μονάδα δοκιμής είναι ένας τρόπος δοκιμής μεμονωμένων συστατικών του κώδικα σας σε απομόνωση. Αυτό είναι χρήσιμο για διάφορους λόγους:

1. Απομονώνει κάθε συστατικό του κώδικα σας καθιστώντας απλό να δείτε τυχόν ζητήματα σε συγκεκριμένους τομείς.
2. Είναι ένας τρόπος να καταγράψεις τον κωδικό σου. Αν έχετε ένα τεστ που αποτυγχάνει, ξέρετε ότι κάτι έχει αλλάξει σε αυτόν τον τομέα του κώδικα σας.

### Τι άλλα είδη δοκιμών υπάρχουν;

Υπάρχουν πολλοί άλλοι τύποι δοκιμών που μπορείτε να κάνετε. Εδώ είναι μερικά:

1. Ενσωμάτωση Δοκιμή - Δοκιμή πώς διαφορετικά συστατικά του κώδικα σας λειτουργούν μαζί. Στο ASP.NET θα μπορούσαμε να χρησιμοποιήσουμε εργαλεία όπως [Επιβεβαίωση](https://github.com/VerifyTests/Verify) να δοκιμάσει την έξοδο των τελικών σημείων και να τα συγκρίνει με τα αναμενόμενα αποτελέσματα. Θα προσθέσουμε αυτό στο μέλλον.
2. Τέλος-to-End Testing - Δοκιμή όλης της εφαρμογής από την οπτική γωνία του χρήστη. Αυτό θα μπορούσε να γίνει με εργαλεία όπως [Σελήνιο](https://www.selenium.dev/).
3. Performance Testing - Testing how your application performance performance under load. Αυτό θα μπορούσε να γίνει με εργαλεία όπως [Απάτσι JMeter](https://jmeter.apache.org/), [PostMan](https://www.postman.com/). Η επιλογή μου όμως είναι ένα εργαλείο που ονομάζεται [k6](https://k6.io/).
4. Security Testing - Δοκιμή πόσο ασφαλής είναι η εφαρμογή σας. Αυτό θα μπορούσε να γίνει με εργαλεία όπως [OWASP ZAP](https://www.zaproxy.org/), [Σουίτα BurpName](https://portswigger.net/burp), [ΝέσσοςCity name (optional, probably does not need a translation)](https://www.tenable.com/products/nessus).
5. Τέλος δοκιμής χρήστη - δοκιμή πώς λειτουργεί η εφαρμογή σας για τον τελικό χρήστη. Αυτό θα μπορούσε να γίνει με εργαλεία όπως [Δοκιμή χρήστη](https://www.usertesting.com/), [ΧρήστηςZoomCity name (optional, probably does not need a translation)](https://www.userzoom.com/), [Χρήστης-Λυτικά](https://www.userlytics.com/).

## Ρύθμιση του έργου δοκιμής

Θα χρησιμοποιήσω το xUnit για τις εξετάσεις μου. Αυτό χρησιμοποιείται εξ ορισμού σε έργα ASP.NET Core. Θα χρησιμοποιήσω επίσης τον Μοκ για να κοροϊδέψω το DbContext μαζί με τον

- MoqQueryable - Αυτό έχει χρήσιμες επεκτάσεις για το χλευασμό IQueryable αντικείμενα.
- Moq.Entity CandidaCore - Αυτό έχει χρήσιμες επεκτάσεις για το χλευασμό αντικειμένων DbContext.

## Κλειδώνοντας το DbContext

Στην προετοιμασία για αυτό πρόσθεσα μια διεπαφή για το DbContext μου. Αυτό είναι για να μπορώ να κοροϊδεύω το DbContext στις δοκιμές μου. Εδώ είναι η διεπαφή:

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

Είναι πολύ απλό, απλά εκθέτει DBSets μας και η μέθοδος SaveChangesAsync.

Ι *Μην το κάνεις.* Χρησιμοποίησε ένα μοτίβο αποθήκευσης στον κωδικό μου. Αυτό οφείλεται στο γεγονός ότι ο πυρήνας πλαισίου οντότητας είναι ήδη ένα πρότυπο αποθετηρίου. Χρησιμοποιώ ένα επίπεδο υπηρεσιών για να αλληλεπιδράσω με το DbContext. Αυτό συμβαίνει επειδή δεν θέλω να αφαιρέσω τη δύναμη του πυρήνα πλαισίου οντοτήτων.

Στη συνέχεια, προσθέτουμε μια νέα τάξη μας `Mostlylucid.Test` σχέδιο με μέθοδο επέκτασης για τη δημιουργία της ερώτησής μας:

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

Θα δείτε ότι αυτό χρησιμοποιεί το `MockQueryable.Moq` μέθοδος επέκτασης για τη δημιουργία του χλευασμού. Που στη συνέχεια δημιουργεί IQueryable αντικείμενα μας και IAsyncQueryable αντικείμενα.

### Ρύθμιση της δοκιμής

Ένας πυρήνας δοκιμής μονάδας είναι ότι κάθε δοκιμή πρέπει να είναι μια "μονάδα" εργασίας και να μην εξαρτάται από το αποτέλεσμα οποιασδήποτε άλλης δοκιμής (για αυτό κοροϊδεύουμε το DbContext μας).

Στο νέο μας `BlogServiceFetchTests` Τάξη θέσαμε το πλαίσιο των δοκιμών μας στον κατασκευαστή:

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

Το έχω σχολιάσει αρκετά για να δείτε τι συμβαίνει. Φτιάχνουμε ένα... `ServiceCollection` που είναι μια συλλογή υπηρεσιών που μπορούμε να χρησιμοποιήσουμε στις δοκιμές μας. Στη συνέχεια, δημιουργούμε ένα χλευασμό μας `IMostlylucidDBContext` και να την καταχωρήσετε στο `ServiceCollection`. Στη συνέχεια καταγράφουμε κάθε άλλη υπηρεσία που χρειαζόμαστε για τις δοκιμές μας. Τέλος, χτίζουμε το `ServiceProvider` που μπορούμε να χρησιμοποιήσουμε για να πάρουμε τις υπηρεσίες μας.

## Γράφοντας τη Δοκιμασία

Ξεκίνησα προσθέτοντας ένα μόνο μάθημα δοκιμών, το παραπάνω `BlogServiceFetchTests` Μαθήματα. Αυτό είναι ένα μάθημα δοκιμής για την Post παίρνει μεθόδους μου `EFBlogService` Μαθήματα.

Κάθε δοκιμή χρησιμοποιεί ένα κοινό `SetupBlogService` μέθοδος για να πάρει ένα νέο κατοικημένο `EFBlogService` αντικείμενο. Αυτό είναι για να μπορέσουμε να δοκιμάσουμε την υπηρεσία στην απομόνωση.

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

Αυτό είναι ένα απλό μάθημα επέκτασης που μας δίνει μια σειρά από pupulated `BlogPostEntity` αντικείμενα. Αυτό είναι έτσι ώστε να μπορούμε να δοκιμάσουμε την υπηρεσία μας με μια σειρά από διαφορετικά αντικείμενα.

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

Μπορείτε να δείτε ότι το μόνο που κάνει αυτό είναι να επιστρέψει έναν καθορισμένο αριθμό αναρτήσεων blog με γλώσσες και κατηγορίες. Ωστόσο, πάντα προσθέτουμε ένα αντικείμενο "ρίζα" που μας επιτρέπει να μπορούμε να βασιζόμαστε σε ένα γνωστό αντικείμενο στις δοκιμές μας.

### Οι δοκιμές

Κάθε δοκιμή έχει σχεδιαστεί για να δοκιμάζει μία πτυχή των αποτελεσμάτων των θέσεων.

Για παράδειγμα, στα δύο παρακάτω δοκιμάζουμε απλώς ότι μπορούμε να πάρουμε όλες τις θέσεις και ότι μπορούμε να πάρουμε θέσεις από τη γλώσσα.

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

#### Δοκιμή αποτυχίας

Μια σημαντική έννοια στη δοκιμή μονάδας είναι η "αποτυχία δοκιμής" όπου διαπιστώνετε ότι ο κώδικας σας αποτυγχάνει με τον τρόπο που τον περιμένετε.

Στις δοκιμές που ακολουθούν δοκιμάζουμε για πρώτη φορά ότι ο κώδικας κλήσης λειτουργεί όπως αναμένεται. Στη συνέχεια δοκιμάζουμε ότι αν ζητήσουμε περισσότερες σελίδες από ό, τι έχουμε, παίρνουμε ένα κενό αποτέλεσμα (και όχι ένα λάθος).

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

## Συμπέρασμα

Αυτή είναι μια απλή αρχή για τις δοκιμές της μονάδας μας. Στην επόμενη θέση θα προσθέσουμε δοκιμές για περισσότερες υπηρεσίες και τελικά σημεία. Θα δούμε επίσης πώς μπορούμε να ελέγξουμε τα τελικά σημεία μας χρησιμοποιώντας τη δοκιμή ενσωμάτωσης.