# Hinzufügen von Entity Framework für Blog-Posts (Teil 1, Einrichtung der Datenbank)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-11T04:53</datetime>

Schnall dich an, denn das wird ein langer!

## Einleitung

Während ich mit meiner Datei basierten Ansatz zum Bloggen glücklich gewesen, als ein Excercise entschied ich mich, mit Postgres für die Speicherung von Blog-Beiträgen und Kommentare zu bewegen. In diesem Beitrag werde ich zeigen, wie das zusammen mit ein paar Tipps und Tricks, die ich auf dem Weg aufgenommen habe.

[TOC]

## Einrichtung der Datenbank

Postgres ist eine kostenlose Datenbank mit einigen tollen Funktionen. Ich bin ein langjähriger SQL Server Benutzer (Ich habe sogar Performance-Workshops bei Microsoft vor ein paar Jahren laufen lassen), aber Postgres ist eine großartige Alternative. Es ist kostenlos, Open Source, und hat eine große Community; und PGAdmin, um Tool für die Verwaltung es ist Kopf und Schultern über SQL Server Management Studio.

Um loszulegen, müssen Sie Postgres und PGAdmin installieren. Sie können es entweder als Windows-Service oder mit Docker, wie ich in einem früheren Beitrag auf[Schwanzlutscher](/blog/dockercomposedevdeps).

## EF-Kern

In diesem Beitrag werde ich Code First in EF Core verwenden, auf diese Weise können Sie Ihre Datenbank ganz über Code verwalten. Sie können natürlich die Datenbank manuell einrichten und EF Core verwenden, um die Modelle zu gerüsten. Oder natürlich verwenden Sie Dapper oder ein anderes Tool und schreiben Sie Ihre SQL von Hand (oder mit einem MicroORM-Ansatz).

Als erstes müssen Sie die EF Core NuGet Pakete installieren. Hier verwende ich:

- Microsoft.EntityFrameworkCore - Das zentrale EF-Paket
- Microsoft.EntityFrameworkCore.Design - Dies wird benötigt, damit die EF Core Tools funktionieren
- Npgsql.EntityFrameworkCore.PostgreSQL - Der Postgres-Anbieter für EF Core

Sie können diese Pakete mit dem NuGet Paketmanager oder dem dotnet CLI installieren.

Als nächstes müssen wir über die Modelle für die Datenbank-Objekte nachdenken; diese unterscheiden sich von ViewModels, die verwendet werden, um Daten an die Ansichten zu übergeben. Ich werde ein einfaches Modell für die Blog-Beiträge und Kommentare verwenden.

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

Beachten Sie, dass ich diese mit ein paar Attributen dekoriert habe

```csharp
 [Key]
 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
```

Diese lassen EF Core wissen, dass das Id-Feld der primäre Schlüssel ist und dass es von der Datenbank generiert werden sollte.

Ich habe auch die Kategorie

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

Sprachen

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

Und Kommentare

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

Sie werden sehen, ich beziehe mich auf den BlogPost in Kommentare, und ICollections of Comments and Categories in B;ogPost. Dies sind Navigationseigenschaften und ist, wie EF Core weiß, wie die Tabellen zusammen zu verbinden.

## Einrichtung des DbContext

In der Klasse DbContext müssen Sie die Tabellen und Beziehungen definieren. Hier ist meine:

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
In der Methode OnModelCreating definiere ich die Beziehungen zwischen den Tabellen. Ich habe die Fluent API benutzt, um die Beziehungen zwischen den Tabellen zu definieren. Dies ist etwas ausführlicher als die Verwendung von Data Annotations, aber ich finde es lesbarer.

Sie können sehen, dass ich ein paar Indexe in der BlogPost-Tabelle eingerichtet habe. Dies soll bei der Abfrage der Datenbank helfen; Sie sollten die Indizes auswählen, basierend darauf, wie Sie die Daten abfragen. In diesem Fall sind Hash, Slug, veröffentlichtes Datum und Sprache alle Felder, an denen ich abfragen werde.

### Einrichtung

Jetzt haben wir unsere Modelle und DbContext eingerichtet, die wir in die DB einbinden müssen. Meine übliche Praxis ist es, Erweiterungsmethoden hinzuzufügen, dies hilft, alles besser zu organisieren:

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

Hier richte ich die Datenbankverbindung ein und führe dann die Migrationen aus. Ich rufe auch eine Methode auf, um die Datenbank zu bevölkern (in meinem Fall verwende ich immer noch den dateibasierten Ansatz, so dass ich die Datenbank mit den vorhandenen Beiträgen bevölkern muss).

Deine Verbindungskette wird so ähnlich aussehen:

```json
 "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Mostlylucid;port=5432;Username=postgres;Password=<PASSWORD>;"
  },
```

Mit der Erweiterung Ansatz bedeutet, dass meine Program.cs-Datei ist schön und sauber:

```csharp
services.SetupEntityFramework(config.GetConnectionString("DefaultConnection") ??
                              throw new Exception("No Connection String"));

//Then later in the app section

await app.InitializeDatabase();
```

Der folgende Abschnitt ist für die Durchführung der Migration und die Einrichtung der Datenbank verantwortlich.`MigrateAsync`Die Methode erstellt die Datenbank, wenn sie nicht existiert und führt alle erforderlichen Migrationen aus. Dies ist eine großartige Möglichkeit, Ihre Datenbank mit Ihren Modellen zu synchronisieren.

```csharp
     await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
```

## Wanderungsbewegungen

Sobald Sie alle diese eingerichtet haben, müssen Sie Ihre erste Migration erstellen. Dies ist eine Momentaufnahme des aktuellen Zustands Ihrer Modelle und wird verwendet, um die Datenbank zu erstellen. Sie können dies mit dem dotnet CLI (siehe[Hierher](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)Einzelheiten zur Installation des dotnet ef-Tools bei Bedarf):

```bash
dotnet ef migrations add InitialCreate
```

Dadurch wird ein Ordner in Ihrem Projekt mit den Migrationsdateien erstellt. Anschließend können Sie die Migration auf die Datenbank anwenden:

```bash
dotnet ef database update
```

Dadurch werden die Datenbank und Tabellen für Sie erstellt.