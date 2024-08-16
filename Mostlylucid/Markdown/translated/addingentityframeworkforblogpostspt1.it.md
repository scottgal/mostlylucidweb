# Aggiunta del quadro dell'entità per i post del blog (parte 1, creazione del database)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-11T04:53</datetime>

Allacciati la cintura perche' sara' lunga!

Potete vedere le parti 2 e 3 [qui](/blog/addingentityframeworkforblogpostspt2) e [qui](/blog/addingentityframeworkforblogpostspt3).

## Introduzione

Mentre sono stato felice con il mio approccio basato sui file al blogging, come un esercizio ho deciso di passare a utilizzare Postgres per memorizzare i post e commenti del blog. In questo post vi mostrerò come questo è fatto insieme ad alcuni suggerimenti e trucchi che ho raccolto lungo la strada.

[TOC]

## Creazione della banca dati

Postgres è un database gratuito con alcune grandi caratteristiche. Sono un utente SQL Server a lungo tempo (ho anche eseguito laboratori di prestazioni a Microsoft alcuni anni fa) ma Postgres è una grande alternativa. È libero, open source, e ha una grande comunità; e PGAdmin, per lo strumento di gestione è testa e spalle sopra SQL Server Management Studio.

Per iniziare, è necessario installare Postgres e PGAdmin. È possibile impostarlo sia come un servizio Windows o utilizzando Docker come ho presentato in un post precedente su [DockerCity name (optional, probably does not need a translation)](/blog/dockercomposedevdeps).

## Centrale EF

In questo post userò Code First in EF Core, in questo modo potrai gestire il tuo database interamente attraverso il codice. Naturalmente è possibile impostare il database manualmente e utilizzare EF Core per impalpare i modelli. O, naturalmente, utilizzare Dapper o un altro strumento e scrivere il vostro SQL a mano (o con un approccio MicroORM).

La prima cosa da fare è installare i pacchetti EF Core NuGet. Qui uso:

- Microsoft.EntityFrameworkCore - Il pacchetto EF principale
- Microsoft.EntityFrameworkCore.Design - Questo è necessario per gli strumenti EF Core per lavorare
- Npgsql.EntityFrameworkCore.PostgreSQL - Il provider Postgres per EF Core

È possibile installare questi pacchetti utilizzando il gestore dei pacchetti NuGet o il dotnet CLI.

Poi dobbiamo pensare ai modelli per gli oggetti Database; questi sono distinti da ViewModel che vengono utilizzati per passare i dati alle viste. Userò un semplice modello per i post del blog e commenti.

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

Nota che li ho decorati con un paio di attributi

```csharp
 [Key]
 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
```

Questi fanno sapere a EF Core che il campo ID è la chiave primaria e che dovrebbe essere generato dal database.

Ho anche la categoria

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

Lingue

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

E commenti

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

Vedrete che mi riferisco al BlogPost in Commenti, e ICollections of Comments and Categories in B;ogPost. Queste sono proprietà di navigazione ed è come EF Core sa come unire le tabelle insieme.

## Impostazione del DbContext

Nella classe DbContext dovrai definire le tabelle e le relazioni. Ecco il mio:

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
Nel metodo OnModelCreating definisco i rapporti tra le tabelle. Ho usato l'API fluente per definire le relazioni tra le tabelle. Questo è un po 'più verboso rispetto all'utilizzo di annotazioni di dati, ma lo trovo più leggibile.

Potete vedere che ho impostato un paio di indici sulla tabella BlogPost. Questo è per aiutare con le prestazioni quando si interroga il database; è necessario selezionare gli indici in base a come si sta interrogando i dati. In questo caso hash, slug, data e lingua pubblicate sono tutti i campi su cui interrogherò.

### Configurazione

Ora abbiamo i nostri modelli e DbContext impostati dobbiamo collegarlo al DB. La mia pratica abituale è quella di aggiungere metodi di estensione, questo aiuta a mantenere tutto più organizzato:

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

Qui ho impostato la connessione al database e poi ho eseguito le migrazioni. Chiamo anche un metodo per popolare il database (nel mio caso sto ancora usando l'approccio basato sui file quindi ho bisogno di popolare il database con i post esistenti).

La tua stringa di connessione assomiglierà a questa:

```json
 "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Mostlylucid;port=5432;Username=postgres;Password=<PASSWORD>;"
  },
```

Utilizzando l'approccio di estensione significa che il mio file Program.cs è bello e pulito:

```csharp
services.SetupEntityFramework(config.GetConnectionString("DefaultConnection") ??
                              throw new Exception("No Connection String"));

//Then later in the app section

await app.InitializeDatabase();
```

La sezione seguente è responsabile dell'esecuzione della migrazione e dell'effettiva creazione della banca dati. La `MigrateAsync` metodo creerà il database se non esiste ed esegue tutte le migrazioni che sono necessarie. Questo è un ottimo modo per mantenere il database in sincronia con i vostri modelli.

```csharp
     await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
```

## Migrazioni

Una volta che si dispone di tutto questo impostare è necessario per creare la migrazione iniziale. Questa è un'istantanea dello stato attuale dei vostri modelli e sarà usata per creare il database. Puoi farlo usando il dotnet CLI (vedere [qui](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) per i dettagli sull'installazione dello strumento dotnet ef se necessario:

```bash
dotnet ef migrations add InitialCreate
```

Questo creerà una cartella nel tuo progetto con i file di migrazione. Puoi quindi applicare la migrazione al database utilizzando:

```bash
dotnet ef database update
```

Questo creerà il database e le tabelle per voi.