# Ajout d'un cadre d'entité pour les billets de blog (partie 1, Mise en place de la base de données)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-11T04:53</datetime>

Serrez-vous parce que ça va être long!

Vous pouvez voir les parties 2 et 3 [Ici.](/blog/addingentityframeworkforblogpostspt2) et [Ici.](/blog/addingentityframeworkforblogpostspt3).

## Présentation

Alors que j'ai été satisfait de mon approche basée sur les fichiers de blogging, comme un excercise, j'ai décidé de passer à l'utilisation de Postgres pour stocker des messages de blog et des commentaires. Dans ce post, je vais montrer comment c'est fait avec quelques conseils et astuces que j'ai ramassés en cours de route.

[TOC]

## Mise en place de la base de données

Postgres est une base de données gratuite avec quelques grandes fonctionnalités. Je suis un utilisateur de SQL Server depuis longtemps (j'ai même organisé des ateliers de performance chez Microsoft il y a quelques années) mais Postgres est une excellente alternative. C'est gratuit, open source, et a une grande communauté; et PGAdmin, pour l'administrer, c'est la tête et les épaules au-dessus de SQL Server Management Studio.

Pour commencer, vous devrez installer Postgres et PGAdmin. Vous pouvez le configurer soit comme un service de fenêtres ou en utilisant Docker comme je l'ai présenté dans un post précédent sur [Poivrons](/blog/dockercomposedevdeps).

## EF Noyau

Dans ce post, je vais utiliser Code First dans EF Core, de cette façon vous pouvez gérer votre base de données entièrement via code. Vous pouvez bien sûr configurer la base de données manuellement et utiliser EF Core pour échafauder les modèles. Ou bien sûr, utilisez Dapper ou un autre outil et écrivez votre SQL à la main (ou avec une approche MicroORM).

La première chose que vous devrez faire est d'installer les paquets EF Core NuGet. Ici j'utilise:

- Microsoft.EntityFrameworkCore - Le paquet EF de base
- Microsoft.EntityFrameworkCore.Design - Ceci est nécessaire pour que les outils EF Core fonctionnent
- Npgsql.EntityFrameworkCore.PostgreSQL - Le fournisseur de Postgres pour EF Core

Vous pouvez installer ces paquets en utilisant le gestionnaire de paquets NuGet ou le Dotnet CLI.

Ensuite, nous devons réfléchir aux modèles pour les objets de la base de données; ceux-ci sont distincts de ViewModels qui sont utilisés pour transmettre les données aux vues. J'utiliserai un modèle simple pour les billets de blog et les commentaires.

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

Notez que j'ai décoré ceux-ci avec un couple d'attributs

```csharp
 [Key]
 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
```

Ceux-ci permettent à EF Core de savoir que le champ Id est la clé principale et qu'il devrait être généré par la base de données.

J'ai aussi la catégorie

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

Langues

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

Et commentaires

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

Vous verrez que je me réfère au BlogPost dans Commentaires, et ICollections de commentaires et de catégories dans B;ogPost. Ce sont des propriétés de navigation et c'est ainsi que EF Core sait rejoindre les tables ensemble.

## Configuration du DbContext

Dans la classe DbContext, vous aurez besoin de définir les tables et les relations. Voici le mien :

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
Dans la méthode OnModelCreating, je définit les relations entre les tables. J'ai utilisé l'API Fluent pour définir les relations entre les tables. C'est un peu plus verbeux que l'utilisation des annotations de données, mais je le trouve plus lisible.

Vous pouvez voir que j'ai mis en place quelques index sur la table BlogPost. Ceci est pour aider avec les performances lors de la requête de la base de données; vous devez sélectionner les Indices en fonction de la façon dont vous allez interroger les données. Dans ce cas, le hash, le slug, la date et la langue publiées sont tous des champs sur lesquels je vais poser des questions.

### Configuration

Maintenant, nous avons nos modèles et DbContext mis en place nous devons l'accrocher à la DB. Ma pratique habituelle est d'ajouter des méthodes d'extension, ce qui permet de garder tout plus organisé:

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

Ici, j'ai mis en place la connexion de la base de données, puis j'ai lancé les migrations. J'appelle aussi une méthode pour remplir la base de données (dans mon cas, j'utilise toujours l'approche basée sur les fichiers, donc je dois remplir la base de données avec les messages existants).

Votre chaîne de connexion ressemblera à ceci :

```json
 "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Mostlylucid;port=5432;Username=postgres;Password=<PASSWORD>;"
  },
```

L'utilisation de l'approche d'extension signifie que mon fichier Program.cs est agréable et propre :

```csharp
services.SetupEntityFramework(config.GetConnectionString("DefaultConnection") ??
                              throw new Exception("No Connection String"));

//Then later in the app section

await app.InitializeDatabase();
```

La section ci-dessous est chargée de gérer la migration et de mettre en place la base de données. Les `MigrateAsync` méthode créera la base de données si elle n'existe pas et exécutera les migrations qui sont nécessaires. C'est un excellent moyen de maintenir votre base de données en phase avec vos modèles.

```csharp
     await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
```

## Migrations

Une fois que vous avez tout cela mis en place, vous devez créer votre migration initiale. Il s'agit d'un instantané de l'état actuel de vos modèles et sera utilisé pour créer la base de données. Vous pouvez le faire en utilisant le Dotnet CLI (voir [Ici.](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) pour plus de détails sur l'installation de l'outil dotnet ef si nécessaire):

```bash
dotnet ef migrations add InitialCreate
```

Cela créera un dossier dans votre projet avec les fichiers de migration. Vous pouvez ensuite appliquer la migration à la base de données en utilisant :

```bash
dotnet ef database update
```

Cela créera la base de données et les tables pour vous.