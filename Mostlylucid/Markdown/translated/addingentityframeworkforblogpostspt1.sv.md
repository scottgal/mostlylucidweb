# Lägga till Entity Framework för blogginlägg (Del 1, Sätta upp databasen)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-11T04:53.........................................................................................................</datetime>

Spänn fast dig, för det här blir långt!

Du kan se delarna 2 och 3 [här](/blog/addingentityframeworkforblogpostspt2) och [här](/blog/addingentityframeworkforblogpostspt3).

## Inledning

Medan jag har varit nöjd med min filbaserade strategi för bloggning, som en övning bestämde jag mig för att flytta till att använda Postgres för att lagra blogginlägg och kommentarer. I det här inlägget ska jag visa hur det görs tillsammans med några tips och tricks som jag har plockat upp längs vägen.

[TOC]

## Ställa in databasen

Postgres är en gratis databas med några bra funktioner. Jag är en lång tid SQL Server användare (Jag körde även prestanda workshops på Microsoft för några år sedan) men Postgres är ett bra alternativ. Det är gratis, öppen källkod, och har en stor gemenskap; och PGAdmin, till verktyg för att administrera det är huvud och axlar ovanför SQL Server Management Studio.

För att komma igång måste du installera Postgres och PGAdmin. Du kan ställa in det antingen som en windows service eller använda Docker som jag presenterade i ett tidigare inlägg på [Docka](/blog/dockercomposedevdeps).

## EF-kärna

I detta inlägg kommer jag att använda kod först i EF Core, på detta sätt kan du hantera din databas helt genom kod. Du kan naturligtvis ställa in databasen manuellt och använda EF Core för att byggnadsställningar modellerna. Eller naturligtvis använda Dapper eller ett annat verktyg och skriva din SQL för hand (eller med en MicroORM approach).

Det första du behöver göra är att installera EF Core NuGet-paketen. Här använder jag:

- Microsoft.EntityFrameworkCore - Kärnan EF-paket
- Microsoft.EntityFrameworkCore.Design - Detta behövs för att EF Core-verktygen ska fungera
- Npgsql.EntityFrameworkCore.PostgreSQL - Postgres leverantör för EF Core

Du kan installera dessa paket med NuGet pakethanteraren eller dotnet CLI.

Därefter måste vi tänka på modellerna för databasobjekten; dessa skiljer sig från ViewModels som används för att överföra data till vyerna. Jag kommer att använda en enkel modell för blogginlägg och kommentarer.

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

Observera att jag har dekorerat dessa med ett par attribut

```csharp
 [Key]
 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
```

Dessa låt EF Core veta att id-fältet är den primära nyckeln och att det bör genereras av databasen.

Jag har också kategori

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

Språk

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

Och kommentarer

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

Du kommer att se Jag hänvisar till bloggenPostat i Kommentarer, och ISamlingar av kommentarer och kategorier i B;ogPost. Dessa är navigationsegenskaper och är hur EF Core vet hur man går samman med tabellerna.

## Ställa in DbContext

I DbContext-klassen måste du definiera tabeller och relationer. Här är min:

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
I OnModelCreating metoden definierar jag sambanden mellan tabellerna. Jag har använt Fluent API för att definiera förhållandet mellan tabellerna. Detta är lite mer verbose än att använda Data Annotations men jag tycker att det är mer läsbart.

Du kan se att jag satte upp ett par index på bloggpost tabellen. Detta är för att hjälpa till med prestanda när du frågar i databasen; du bör välja index baserat på hur du kommer att fråga data. I det här fallet är hash, snigel, publicerat datum och språk alla fält jag kommer att fråga på.

### Ställ in

Nu har vi våra modeller och DbContext som vi behöver för att koppla in den i DB. Min vanliga praxis är att lägga till förlängningsmetoder, detta bidrar till att hålla allt mer organiserat:

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

Här sätter jag upp databasanslutningen och kör sedan migreringar. Jag kallar också en metod för att fylla databasen (i mitt fall använder jag fortfarande den filbaserade metoden så jag måste fylla databasen med befintliga inlägg).

Din anslutningssträng kommer att se ut ungefär så här:

```json
 "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Mostlylucid;port=5432;Username=postgres;Password=<PASSWORD>;"
  },
```

Genom att använda förlängningen tillvägagångssätt innebär att min Program.cs-fil är trevlig och ren:

```csharp
services.SetupEntityFramework(config.GetConnectionString("DefaultConnection") ??
                              throw new Exception("No Connection String"));

//Then later in the app section

await app.InitializeDatabase();
```

Nedanstående avsnitt ansvarar för att hantera migreringen och faktiskt upprätta databasen. I detta sammanhang är det viktigt att se till att `MigrateAsync` metoden kommer att skapa databasen om den inte finns och köra några migreringar som behövs. Detta är ett bra sätt att hålla din databas i synk med dina modeller.

```csharp
     await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
```

## Flyttningar

När du har allt detta som du behöver för att skapa din första migration. Detta är en ögonblicksbild av det aktuella tillståndet för dina modeller och kommer att användas för att skapa databasen. Du kan göra detta med hjälp av dotnet CLI (se [här](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) För detaljer om installation av verktyget dotnet ef vid behov:

```bash
dotnet ef migrations add InitialCreate
```

Detta skapar en mapp i ditt projekt med migreringsfilerna. Du kan sedan tillämpa migreringen till databasen med hjälp av:

```bash
dotnet ef database update
```

Detta kommer att skapa databasen och tabeller för dig.