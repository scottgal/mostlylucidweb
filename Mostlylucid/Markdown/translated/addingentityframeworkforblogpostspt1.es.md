# Añadiendo Marco de Entidad para Mensajes de Blog (Parte 1, Configuración de la Base de Datos)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-11T04:53</datetime>

¡Abróchate el cinturón porque será largo!

## Introducción

Mientras que he sido feliz con mi enfoque basado en archivos para bloguear, como un ejercicio decidí pasar a usar Postgres para almacenar entradas de blog y comentarios. En este post voy a mostrar cómo se hace junto con algunos consejos y trucos que he recogido a lo largo del camino.

[TOC]

## Creación de la base de datos

Postgres es una base de datos gratuita con algunas grandes características. Soy un usuario de SQL Server de mucho tiempo (incluso realicé talleres de rendimiento en Microsoft hace unos años), pero Postgres es una gran alternativa. Es libre, de código abierto, y tiene una gran comunidad; y PGAdmin, a la herramienta para administrarlo es cabeza y hombros por encima de SQL Server Management Studio.

Para empezar, tendrás que instalar Postgres y PGAdmin. Usted puede configurarlo ya sea como un servicio de ventanas o utilizando Docker como he presentado en un post anterior en [Docker](/blog/dockercomposedevdeps).

## Núcleo básico de la FE

En este post voy a utilizar Code First en EF Core, de esta manera usted puede administrar su base de datos por completo a través de código. Por supuesto, puede configurar la base de datos manualmente y utilizar EF Core para el andamiaje de los modelos. O, por supuesto, utilice Dapper u otra herramienta y escriba su SQL a mano (o con un enfoque MicroORM).

Lo primero que tendrás que hacer es instalar los paquetes EF Core NuGet. Aquí utilizo:

- Microsoft.EntityFrameworkCore - El paquete central de EF
- Microsoft.EntityFrameworkCore.Design - Esto es necesario para que las herramientas EF Core funcionen
- Npgsql.EntityFrameworkCore.PostgreSQL - El proveedor de Postgres para EF Core

Puede instalar estos paquetes usando el gestor de paquetes NuGet o el CLI dotnet.

A continuación tenemos que pensar en los modelos para los objetos de base de datos; estos son distintos de ViewModels que se utilizan para pasar datos a las vistas. Usaré un modelo simple para los posts y comentarios del blog.

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

Tenga en cuenta que he decorado estos con un par de atributos

```csharp
 [Key]
 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
```

Estos permiten a EF Core saber que el campo Id es la clave principal y que debe ser generada por la base de datos.

También tengo Categoría

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

Idiomas

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

Y comentarios

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

Verá que me refiero al BlogPost en Comentarios, e IColections of Comments and Categories in B;ogPost. Estas son propiedades de navegación y es cómo EF Core sabe cómo unir las tablas.

## Configuración del DbContext

En la clase DbContext necesitarás definir las tablas y relaciones. Aquí está el mío:

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
En el método OnModelCreating defino las relaciones entre las tablas. He utilizado la API Fluent para definir las relaciones entre las tablas. Esto es un poco más verboso que el uso de Anotaciones de Datos, pero lo encuentro más legible.

Puedes ver que establecí un par de índices en la tabla BlogPost. Esto es para ayudar con el rendimiento al consultar la base de datos; debe seleccionar los Índices en función de cómo consultará los datos. En este caso hash, babosa, fecha publicada y el idioma son todos los campos que voy a consultar.

### Configuración

Ahora tenemos nuestros modelos y DbContext configurados tenemos que conectarlo en el DB. Mi práctica habitual es añadir métodos de extensión, esto ayuda a mantener todo más organizado:

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

Aquí configuro la conexión de la base de datos y luego ejecuto las migraciones. También llamo a un método para poblar la base de datos (en mi caso todavía estoy usando el enfoque basado en archivos así que necesito poblar la base de datos con los mensajes existentes).

Su cadena de conexión se verá algo así:

```json
 "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Mostlylucid;port=5432;Username=postgres;Password=<PASSWORD>;"
  },
```

Usar el enfoque de extensión significa que mi archivo Program.cs es agradable y limpio:

```csharp
services.SetupEntityFramework(config.GetConnectionString("DefaultConnection") ??
                              throw new Exception("No Connection String"));

//Then later in the app section

await app.InitializeDatabase();
```

La sección siguiente es la responsable de ejecutar la migración y realmente la creación de la base de datos. Los `MigrateAsync` método creará la base de datos si no existe y ejecutar cualquier migración que se necesita. Esta es una gran manera de mantener su base de datos en sincronía con sus modelos.

```csharp
     await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
```

## Migraciones

Una vez que tengas todo esto configurado, necesitas crear tu migración inicial. Esta es una instantánea del estado actual de sus modelos y se utilizará para crear la base de datos. Puede hacer esto usando el CLI de dotnet (ver [aquí](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) para más detalles sobre la instalación de la herramienta dotnet ef si es necesario):

```bash
dotnet ef migrations add InitialCreate
```

Esto creará una carpeta en su proyecto con los archivos de migración. A continuación, puede aplicar la migración a la base de datos utilizando:

```bash
dotnet ef database update
```

Esto creará la base de datos y tablas para usted.