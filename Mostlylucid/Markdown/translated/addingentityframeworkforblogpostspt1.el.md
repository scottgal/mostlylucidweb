# Προσθήκη πλαισίου οντοτήτων για δημοσιεύσεις blog (Μέρος 1, Ρύθμιση της βάσης δεδομένων)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-11T04:53</datetime>

Μπες μέσα γιατί αυτό θα είναι μεγάλο!

Μπορείτε να δείτε τα μέρη 2 και 3 [Ορίστε.](/blog/addingentityframeworkforblogpostspt2) και [Ορίστε.](/blog/addingentityframeworkforblogpostspt3).

## Εισαγωγή

Ενώ ήμουν ευχαριστημένος με την προσέγγιση με βάση το αρχείο μου στο blogging, ως απόσπασμα αποφάσισα να προχωρήσω στη χρήση Postgres για την αποθήκευση αναρτήσεων blog και σχολίων. Σε αυτή τη θέση θα δείξω πώς γίνεται αυτό μαζί με μερικές συμβουλές και κόλπα που έχω πάρει κατά μήκος του δρόμου.

[TOC]

## Ρύθμιση της βάσης δεδομένων

Postgres είναι μια δωρεάν βάση δεδομένων με μερικά μεγάλα χαρακτηριστικά. Είμαι πολύς καιρός χρήστης SQL Server (έτρεξα ακόμη και εργαστήρια απόδοσης στη Microsoft λίγα χρόνια πριν) αλλά Postgres είναι μια μεγάλη εναλλακτική λύση. Είναι δωρεάν, ανοιχτή πηγή, και έχει μια μεγάλη κοινότητα? και PGAdmin, να εργαλείο για τη διαχείριση του είναι κεφάλι και ώμους πάνω από SQL Server Management Studio.

Για να ξεκινήσετε, θα πρέπει να εγκαταστήσετε Postgres και PGadmin. Μπορείτε να το ρυθμίσετε είτε ως υπηρεσία παραθύρων ή χρησιμοποιώντας Docker όπως παρουσίασα σε μια προηγούμενη θέση στο [ΝτόκερCity name (optional, probably does not need a translation)](/blog/dockercomposedevdeps).

## Πυρήνας EF

Σε αυτή τη θέση θα χρησιμοποιήσω τον κωδικό πρώτα στο EF Core, με αυτόν τον τρόπο μπορείτε να διαχειριστείτε τη βάση δεδομένων σας εξ ολοκλήρου μέσω του κώδικα. Μπορείτε φυσικά να ρυθμίσετε τη βάση δεδομένων χειροκίνητα και να χρησιμοποιήσετε EF Core για να σκαλωσιάσει τα μοντέλα. Ή φυσικά να χρησιμοποιήσετε Dapper ή άλλο εργαλείο και να γράψετε SQL σας με το χέρι (ή με μια προσέγγιση MicroORM).

Το πρώτο πράγμα που πρέπει να κάνετε είναι να εγκαταστήσετε τα πακέτα EF Core NuGet. Εδώ χρησιμοποιώ:

- Microsoft.Entity REGISCore - Το πακέτο πυρήνα EF
- Microsoft.Entity CandidaCore.Design - Αυτό είναι απαραίτητο για τα εργαλεία πυρήνα EF για να λειτουργήσει
- Npgsql.Entity frameworkCore.PostgreSQL - Ο πάροχος Postgres για EF Core

Μπορείτε να εγκαταστήσετε αυτά τα πακέτα χρησιμοποιώντας το NuGet διαχειριστή πακέτων ή το dotnet CLI.

Στη συνέχεια πρέπει να σκεφτούμε τα μοντέλα για τα αντικείμενα βάσης δεδομένων? αυτά είναι διακριτά από τα ViewModels που χρησιμοποιούνται για να περάσουν τα δεδομένα στις απόψεις. Θα χρησιμοποιήσω ένα απλό μοντέλο για τις δημοσιεύσεις και τα σχόλια του blog.

```csharp
public class BlogPost
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public string Title { get; set; }
    public string Slug { get; set; }
    public string HtmlContent { get; set; }
    public string PlainTextContent { get; set; }
    public string ContentHash { get; set; }

    
    public int WordCount { get; set; }
    
    public int LanguageId { get; set; }
    public Language Language { get; set; }
    public ICollection<Comments> Comments { get; set; }
    public ICollection<Category> Categories { get; set; }
    
    public DateTimeOffset PublishedDate { get; set; }
    
}
```

Σημειώστε ότι έχω διακοσμήσει αυτά με μερικά χαρακτηριστικά

```csharp
 [Key]
 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
```

Αυτές επιτρέπουν στο EF Core να γνωρίζει ότι το πεδίο Id είναι το κύριο κλειδί και ότι θα πρέπει να δημιουργηθεί από τη βάση δεδομένων.

Έχω επίσης Κατηγορία

```csharp
public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<BlogPost> BlogPosts { get; set; }
}
```

Γλώσσες

```csharp
public class Language
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<BlogPost> BlogPosts { get; set; }
}
```

Και σχόλια

```csharp
public class Comments
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Comment { get; set; }
    public string Slug { get; set; }
    public int BlogPostId { get; set; }
    public BlogPost BlogPost { get; set; } 
}
```

Θα δείτε I refer to the BlogPost in Comments, and ICollections of Comments and Categories in B;ogPost. Αυτές είναι ιδιότητες πλοήγησης και πώς το EF Core ξέρει πώς να ενώνει τους πίνακες μαζί.

## Ρύθμιση του DbContext

Στην τάξη DbContext θα πρέπει να καθορίσετε τους πίνακες και τις σχέσεις. Εδώ είναι το δικό μου:

<details>
<summary>Expand to see the full code</summary>
```csharp
public class MostlylucidDbContext : DbContext
{
    public MostlylucidDbContext(DbContextOptions<MostlylucidDbContext> contextOptions) : base(contextOptions)
    {
    }

    public DbSet<Comments> Comments { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<Category> Categories { get; set; }

    public DbSet<Language> Languages { get; set; }


    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasIndex(x => new { x.Slug, x.LanguageId });
            entity.HasIndex(x => x.ContentHash).IsUnique();
            entity.HasIndex(x => x.PublishedDate);

            entity.HasMany(b => b.Comments)
                .WithOne(c => c.BlogPost)
                .HasForeignKey(c => c.BlogPostId);

            entity.HasOne(b => b.Language)
                .WithMany(l => l.BlogPosts).HasForeignKey(x => x.LanguageId);

            entity.HasMany(b => b.Categories)
                .WithMany(c => c.BlogPosts)
                .UsingEntity<Dictionary<string, object>>(
                    "BlogPostCategory",
                    c => c.HasOne<Category>().WithMany().HasForeignKey("CategoryId"),
                    b => b.HasOne<BlogPost>().WithMany().HasForeignKey("BlogPostId")
                );
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasMany(l => l.BlogPosts)
                .WithOne(b => b.Language);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id); // Assuming Category has a primary key named Id

            entity.HasMany(c => c.BlogPosts)
                .WithMany(b => b.Categories)
                .UsingEntity<Dictionary<string, object>>(
                    "BlogPostCategory",
                    b => b.HasOne<BlogPost>().WithMany().HasForeignKey("BlogPostId"),
                    c => c.HasOne<Category>().WithMany().HasForeignKey("CategoryId")
                );
        });
    }
}
```

</details>
Στη μέθοδο OnModelCreating ορίζω τις σχέσεις μεταξύ των πινάκων. Χρησιμοποίησα το Fluent API για να καθορίσω τις σχέσεις μεταξύ των τραπεζιών. Αυτό είναι λίγο πιο ρήμα από το να χρησιμοποιώ σημειώσεις δεδομένων, αλλά το βρίσκω πιο ευανάγνωστο.

Μπορείτε να δείτε ότι έφτιαξα μερικά ευρετήρια στο τραπέζι του BlogPost. Αυτό είναι για να βοηθήσει με την απόδοση κατά την ερώτηση της βάσης δεδομένων; θα πρέπει να επιλέξετε τις Indices με βάση το πώς θα είστε ερώτηση των δεδομένων. Σε αυτή την περίπτωση χασίς, γυμνοσάλιαγκας, δημοσιευμένη ημερομηνία και γλώσσα είναι όλα τα πεδία που θα ψάξω.

### Ρύθμιση

Τώρα έχουμε τα μοντέλα μας και το DbContext set up πρέπει να το συνδέσουμε με το DB. Συνηθισμένη πρακτική μου είναι να προσθέσω μεθόδους επέκτασης, αυτό βοηθά να κρατήσουμε τα πάντα πιο οργανωμένα:

```csharp
public static class Setup
{
    public static void SetupEntityFramework(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MostlylucidDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    public static async Task InitializeDatabase(this WebApplication app)
    {
        try
        {
            await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
            
            var blogService = scope.ServiceProvider.GetRequiredService<IBlogService>();
            await blogService.Populate();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Failed to migrate database");
        }        
    }
}
```

Εδώ έφτιαξα τη σύνδεση της βάσης δεδομένων και μετά έτρεξα τις μεταναστεύσεις. Καλώ επίσης μια μέθοδο για τον πληθυσμό της βάσης δεδομένων (στην περίπτωσή μου εξακολουθώ να χρησιμοποιώ την προσέγγιση με βάση το αρχείο, οπότε πρέπει να κατοικήσω τη βάση δεδομένων με τις υπάρχουσες θέσεις).

Η συμβολοσειρά σύνδεσης σου θα μοιάζει κάπως έτσι:

```json
 "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Mostlylucid;port=5432;Username=postgres;Password=<PASSWORD>;"
  },
```

Χρησιμοποιώντας την προσέγγιση επέκτασης σημαίνει ότι το αρχείο Program.cs μου είναι ωραίο και καθαρό:

```csharp
services.SetupEntityFramework(config.GetConnectionString("DefaultConnection") ??
                              throw new Exception("No Connection String"));

//Then later in the app section

await app.InitializeDatabase();
```

Το παρακάτω τμήμα είναι υπεύθυνο για τη διαχείριση της μετανάστευσης και τη δημιουργία της βάσης δεδομένων. Η `MigrateAsync` η μέθοδος θα δημιουργήσει τη βάση δεδομένων αν δεν υπάρχει και να τρέξει οποιαδήποτε μετανάστευση που απαιτούνται. Αυτός είναι ένας πολύ καλός τρόπος για να κρατήσει τη βάση δεδομένων σας σε συγχρονισμό με τα μοντέλα σας.

```csharp
     await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
```

## Μετανάστες

Μόλις έχεις όλα αυτά στημένα θα πρέπει να δημιουργήσεις την αρχική σου μετανάστευση. Αυτό είναι ένα στιγμιότυπο της τρέχουσας κατάστασης των μοντέλων σας και θα χρησιμοποιηθεί για τη δημιουργία της βάσης δεδομένων. Μπορείτε να το κάνετε αυτό χρησιμοποιώντας το dotnet CLI (βλέπε [Ορίστε.](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) για λεπτομέρειες σχετικά με την εγκατάσταση του εργαλείου dotnet, εάν χρειάζεται:

```bash
dotnet ef migrations add InitialCreate
```

Αυτό θα δημιουργήσει ένα φάκελο στο έργο σας με τα αρχεία μετανάστευσης. Στη συνέχεια μπορείτε να εφαρμόσετε τη μετάβαση στη βάση δεδομένων χρησιμοποιώντας:

```bash
dotnet ef database update
```

Αυτό θα δημιουργήσει τη βάση δεδομένων και τα τραπέζια για εσάς.