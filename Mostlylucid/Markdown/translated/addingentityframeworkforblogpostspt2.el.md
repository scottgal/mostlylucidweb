# Προσθήκη πλαισίου οντοτήτων για δημοσιεύσεις blog (μέρος 2)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-15T18:00</datetime>

Μπορείτε να βρείτε όλο τον πηγαίο κώδικα για τις δημοσιεύσεις blog στο [GitHubCity name (optional, probably does not need a translation)](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Μέρος 2 της σειράς σχετικά με την προσθήκη πλαισίου οντότητας σε έργο NET Core.**
Μέρος 1 μπορεί να βρεθεί [Ορίστε.](/blog/addingentityframeworkforblogpostspt1).

# Εισαγωγή

Στην προηγούμενη ανάρτηση, δημιουργήσαμε τη βάση δεδομένων και το πλαίσιο για τις δημοσιεύσεις μας στο blog. Σε αυτή τη θέση, θα προσθέσουμε τις υπηρεσίες για να αλληλεπιδράσουν με τη βάση δεδομένων.

Στην επόμενη θέση θα εξετάσουμε λεπτομερώς πώς αυτές οι υπηρεσίες λειτουργούν τώρα με τους υφιστάμενους ελεγκτές και απόψεις.

[TOC]

### Ρύθμιση

Έχουμε τώρα ένα μάθημα επέκτασης BlogSetup που δημιουργεί αυτές τις υπηρεσίες. Αυτό είναι μια επέκταση από ό, τι κάναμε σε [Μέρος 1](/blog/addingentityframeworkforblogpostspt1), όπου δημιουργούμε τη βάση δεδομένων και το πλαίσιο.

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

Αυτό χρησιμοποιεί το απλό `BlogConfig` κατηγορία για τον προσδιορισμό της κατάστασης στην οποία βρισκόμαστε, είτε `File` ή `Database`. Με βάση αυτό, καταγράφουμε τις υπηρεσίες που χρειαζόμαστε.

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

## Διεπαφές

Όπως θέλω τόσο να υποστηρίξει το αρχείο και τη βάση δεδομένων σε αυτή την εφαρμογή (γιατί όχι! Χρησιμοποίησα μια προσέγγιση βασισμένη σε διασύνδεση που επιτρέπει την ανταλλαγή αυτών με βάση την ρύθμιση.

Έχουμε τρεις νέες διασυνδέσεις, `IBlogService`, `IMarkdownBlogService` και `IBlogPopulator`.

#### IBlogService

Αυτή είναι η κύρια διεπαφή για την υπηρεσία blog. Περιέχει μεθόδους για την απόκτηση θέσεων, κατηγοριών και μεμονωμένων θέσεων.

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

Αυτή η υπηρεσία χρησιμοποιείται από το `EFlogPopulatorService` στην πρώτη εκτέλεση για να πλημμυρίσει τη βάση δεδομένων με τις θέσεις από τα αρχεία markdown.

```csharp
public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();
}
```

Όπως μπορείτε να δείτε είναι αρκετά απλό και απλά έχει δύο μεθόδους, `GetPages` και `LanguageList`. Αυτά χρησιμοποιούνται για την επεξεργασία των αρχείων Markdown και να πάρει τη λίστα των γλωσσών.

#### IBlogPopulatorName

Το BlogPopulators χρησιμοποιείται στη μέθοδο εγκατάστασης μας παραπάνω για να κατοικήσει τη βάση δεδομένων ή στατικό αντικείμενο cache (για το σύστημα αρχείων) με δημοσιεύσεις.

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

Μπορείτε να δείτε ότι αυτό είναι μια επέκταση για `WebApplication` με config που επιτρέπει τη μετανάστευση βάσης δεδομένων να εκτελείται εάν χρειάζεται (η οποία δημιουργεί επίσης τη βάση δεδομένων εάν δεν υπάρχει). Στη συνέχεια καλεί το ρυθμισμένο `IBlogPopulator` υπηρεσία για τον πληθυσμό της βάσης δεδομένων.

Αυτή είναι η διεπαφή για αυτή την υπηρεσία.

```csharp
public interface IBlogPopulator
{
    Task Populate();
}
```

### Εφαρμογή

Αρκετά απλό, σωστά; Αυτό εφαρμόζεται και στις δύο περιπτώσεις. `MarkdownBlogPopulator` και `EFBlogPopulator` Μαθήματα.

- Markdown - εδώ καλούμε στο `GetPages` μέθοδος και τον πληθυσμό της κρύπτης.

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

- EF - εδώ καλούμε στην `IMarkdownBlogService` για να πάρει τις σελίδες και στη συνέχεια να κατοικήσει τη βάση δεδομένων.

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

Έχουμε χωρίσει αυτή τη λειτουργία σε διεπαφές για να κάνουμε τον κώδικα πιο κατανοητό και "αναγνωρισμένο" (όπως στις αρχές του SOLID). Αυτό μας επιτρέπει να ανταλλάξουμε εύκολα τις υπηρεσίες με βάση τη διαμόρφωση.

# Συμπέρασμα

Στην επόμενη δημοσίευση, θα εξετάσουμε λεπτομερέστερα την εφαρμογή των Controllers and Views για τη χρήση αυτών των υπηρεσιών.