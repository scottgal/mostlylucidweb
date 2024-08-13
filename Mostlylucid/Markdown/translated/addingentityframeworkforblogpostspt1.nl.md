# Het toevoegen van Entity Framework voor Blog berichten (Deel 1, Het opzetten van de Database)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-11T04:53</datetime>

Doe je gordel om, want dit wordt een lange.

## Inleiding

Terwijl ik ben blij geweest met mijn file based benadering van bloggen, als een oefening heb ik besloten om te verhuizen naar het gebruik van Postgres voor het opslaan van blog berichten en opmerkingen. In deze post zal ik laten zien hoe dat wordt gedaan samen met een paar tips en trucs die ik heb opgepikt langs de weg.

[TOC]

## Het opzetten van de database

Postgres is een gratis database met enkele geweldige functies. Ik ben een lange tijd SQL Server gebruiker (ik heb zelfs performance workshops bij Microsoft een paar jaar geleden) maar Postgres is een geweldig alternatief. Het is gratis, open source, en heeft een geweldige community; en PGAdmin, om het te gebruiken is hoofd en schouders boven SQL Server Management Studio.

Om te beginnen moet je Postgres en PGAdmin installeren. U kunt het instellen als een windows service of het gebruik van Docker zoals ik gepresenteerd in een vorige post op [Docker](/blog/dockercomposedevdeps).

## EF-kern

In dit bericht zal ik gebruik maken van Code First in EF Core, op deze manier kunt u uw database volledig beheren door middel van code. U kunt natuurlijk de database handmatig instellen en EF Core gebruiken om de modellen te steigeren. Of gebruik natuurlijk Dapper of een ander hulpmiddel en schrijf uw SQL met de hand (of met een MicroORM-aanpak).

Het eerste wat je moet doen is de EF Core NuGet pakketten installeren. Hier gebruik ik:

- Microsoft.Entity FrameworkCore - De kern EF-pakket
- Microsoft.EntityFrameworkCore.Design - Dit is nodig voor de EF Core tools te werken
- Npgsql.EntityFrameworkCore.PostgreSQL - De Postgres provider voor EF Core

U kunt deze pakketten installeren met behulp van de NuGet package manager of de dotnet CLI.

Vervolgens moeten we nadenken over de modellen voor de Database objecten; deze zijn verschillend van ViewModels die worden gebruikt om gegevens door te geven aan de weergaven. Ik zal gebruik maken van een eenvoudig model voor de blog berichten en opmerkingen.

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

Merk op dat ik deze versierd heb met een paar attributen

```csharp
 [Key]
 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
```

Deze laten EF Core weten dat het Id veld de primaire sleutel is en dat het door de database moet worden gegenereerd.

Ik heb ook categorie

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

Talen

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

En opmerkingen

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

U zult zien Ik verwijs naar de BlogPost in Reacties, en ICollecties van Reacties en Categorieën in B;ogPost. Dit zijn navigatie-eigenschappen en is hoe EF Core weet hoe de tabellen samen te voegen.

## De DbContext instellen

In de DbContext klasse moet je de tabellen en relaties definiëren. Hier is de mijne:

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
In de OnModelCreating methode definieer ik de relaties tussen de tabellen. Ik heb de Fluent API gebruikt om de relaties tussen de tabellen te definiëren. Dit is een beetje meer werkboos dan het gebruik van Data Annotaties, maar ik vind het leesbaarder.

U kunt zien dat ik een paar Indexen op de BlogPost tafel. Dit is om te helpen met de prestaties bij het opvragen van de database; u moet de indexen selecteren op basis van hoe u de gegevens zult opvragen. In dit geval hash, slak, gepubliceerde datum en taal zijn alle gebieden die ik zal opzoeken.

### Instellen

Nu hebben we onze modellen en DbContext opgezet moeten we het aansluiten op de DB. Mijn gebruikelijke praktijk is om uitbreidingsmethoden toe te voegen, dit helpt om alles meer georganiseerd te houden:

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

Hier heb ik de database verbinding opgezet en vervolgens de migraties uitgevoerd. Ik bel ook een methode om de database te bevolken (in mijn geval gebruik ik nog steeds de file based aanpak, dus ik moet de database te bevolken met de bestaande berichten).

Uw verbinding string zal er ongeveer zo uitzien:

```json
 "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Mostlylucid;port=5432;Username=postgres;Password=<PASSWORD>;"
  },
```

Met behulp van de extensie aanpak betekent dat mijn Program.cs bestand is mooi en schoon:

```csharp
services.SetupEntityFramework(config.GetConnectionString("DefaultConnection") ??
                              throw new Exception("No Connection String"));

//Then later in the app section

await app.InitializeDatabase();
```

De onderstaande sectie is verantwoordelijk voor het beheren van de migratie en het daadwerkelijk opzetten van de database. De `MigrateAsync` methode zal de database te maken als het niet bestaat en uitvoeren van alle migraties die nodig zijn. Dit is een geweldige manier om uw database te synchroniseren met uw modellen.

```csharp
     await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
```

## Migratie

Zodra je dit alles hebt ingesteld, moet je je eerste migratie maken. Dit is een momentopname van de huidige staat van uw modellen en zal worden gebruikt om de database te maken. U kunt dit doen met behulp van de dotnet CLI (zie [Hier.](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) voor details over de installatie van de dotnet ef tool indien nodig:

```bash
dotnet ef migrations add InitialCreate
```

Dit maakt een map aan in uw project met de migratiebestanden. U kunt de migratie vervolgens toepassen op de database met behulp van:

```bash
dotnet ef database update
```

Dit zal de database en tabellen voor u maken.