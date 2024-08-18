# Bloggaamisen yksikkökehysten lisääminen (osa 1, Tietokannan perustaminen)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-11T04:53</datetime>

Turvavyö kiinni, koska tästä tulee pitkä!

Voit katsoa osat 2 ja 3 [täällä](/blog/addingentityframeworkforblogpostspt2) sekä [täällä](/blog/addingentityframeworkforblogpostspt3).

## Johdanto

Vaikka olen ollut tyytyväinen tiedostopohjaiseen lähestymistapaani bloggaamiseen, harjoituksena päätin siirtyä käyttämään Postgresiä blogikirjoitusten ja kommenttien tallentamiseen. Tässä viestissä näytän, miten se tehdään muutaman vinkin ja tempun ohella, joita olen oppinut matkan varrella.

[TÄYTÄNTÖÖNPANO

## Tietokannan perustaminen

Postgres on ilmainen tietokanta, jossa on hienoja ominaisuuksia. Olen pitkään SQL Server -käyttäjä (juonsin jopa suoritustyöpajoja Microsoftilla muutama vuosi sitten), mutta Postgres on loistava vaihtoehto. Se on ilmainen, avoin lähdekoodi, ja sillä on mahtava yhteisö, ja PGAdminin työkalu sen hallinnoimiseksi on pää ja hartiat SQL Server Management Studion yläpuolella.

Voidaksesi aloittaa, sinun täytyy asentaa Postgres ja PGAdmin. Voit perustaa sen joko ikkunoiden palveluksi tai käyttää Dockeria, kuten esitin edellisessä viestissä. [Docker](/blog/dockercomposedevdeps).

## EF-ydin

Tässä viestissä käytän Code First in EF Corea, näin voit hallita tietokantaasi kokonaan koodin avulla. Voit tietenkin perustaa tietokannan manuaalisesti ja käyttää EF Corea mallien rakennustelineeseen. Tai tietenkin käytä Dapperia tai muuta työkalua ja kirjoita SQL käsin (tai MicroORM-lähestymistavalla).

Ensimmäinen asia, joka sinun täytyy tehdä, on asentaa EF Core NuGet -paketit. Tässä käytän:

- Microsoft.EntityFrameworkCore - EF:n ydinpaketti
- Microsoft.EntityFrameworkCore.Design - Tätä tarvitaan, jotta EF Core -työkalut toimivat
- Npgsql.EntityFrameworkCore.PostgreSQL - EF Coren postgres -toimittaja

Voit asentaa nämä paketit NuGet-paketin hallinnoijalla tai pisteverkko-CLI:llä.

Seuraavaksi on mietittävä Database-objektien malleja, jotka eroavat ViewModelsista, joita käytetään tietojen välittämiseen näkymille. Käytän blogikirjoituksissa ja kommenteissa yksinkertaista mallia.

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

Huomaa, että olen koristellut nämä parilla ominaisuudella

```csharp
 [Key]
 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
```

Ne antavat EF Corelle tiedon siitä, että Id-kenttä on ensisijainen avain ja että sen pitäisi olla tietokannan tuottama.

Minulla on myös kategoria

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

Kielet

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

Ja kommentteja

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

Huomaat, että viittaan BlogPost kommenteissa, ja ICollections of Kommentteja ja Kategoriat B;ogPost. Nämä ovat navigaatio-ominaisuuksia, ja näin EF Core osaa liittyä pöytiin.

## DbContextin asennus

DbContext-luokassa täytyy määritellä pöydät ja suhteet. Tässä on minun:

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
OnModelCreating -menetelmässä määrittelen taulukoiden väliset suhteet. Olen käyttänyt Fluent API:tä määritelläkseni taulukoiden väliset suhteet. Tämä on hieman puheliaampaa kuin Data-huomautusten käyttö, mutta minusta se on luettavampaa.

Voit nähdä, että asetin BlogPostin pöydälle pari indeksiä. Tämä auttaa suorituskyvyssä tietokannan kyselyssä. Indeksejä kannattaa valita sen perusteella, miten tietoja tiedustellaan. Tässä tapauksessa hasis, etana, julkaisupäivä ja kieli ovat kaikki kenttiä, joista aion kysellä.

### Asetukset

Nyt meillä on mallimme ja DbContext valmiina, meidän on kytkettävä se DB:hen. Tavanomainen käytäntöni on lisätä laajennusmenetelmiä, mikä auttaa pitämään kaiken organisoidumpana:

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

Perustin tietokantayhteyden ja suoritin muuttoliikkeet. Kutsun myös menetelmää tietokantaan (minun tapauksessani käytän edelleen tiedostopohjaista lähestymistapaa, joten minun täytyy kansoittaa tietokanta olemassa olevien viestien avulla).

Yhteytesi näyttää tältä:

```json
 "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Mostlylucid;port=5432;Username=postgres;Password=<PASSWORD>;"
  },
```

Laajennuslähestymistavan käyttäminen tarkoittaa, että ohjelma.cs-tiedostoni on mukava ja puhdas:

```csharp
services.SetupEntityFramework(config.GetConnectionString("DefaultConnection") ??
                              throw new Exception("No Connection String"));

//Then later in the app section

await app.InitializeDatabase();
```

Alla oleva jakso vastaa muuttoliikkeen johtamisesta ja tietokannan perustamisesta. Erytropoietiini `MigrateAsync` Menetelmä luo tietokannan, jos sitä ei ole olemassa, ja pyörittää tarvittavia muuttoliikkeitä. Tämä on loistava tapa pitää tietokantasi ajan tasalla malliesi kanssa.

```csharp
     await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
```

## Muuttoliikkeet

Kun olet saanut kaiken tämän valmiiksi, sinun täytyy luoda alkuperäinen muuttoliikkeesi. Tämä on kuva malliesi nykytilasta, ja sitä käytetään tietokannan luomiseen. Voit tehdä tämän pisteen CLI:n avulla (ks. [täällä](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) Tarkempia tietoja dotnet ef -työkalun asentamisesta tarvittaessa:

```bash
dotnet ef migrations add InitialCreate
```

Tämä luo projektiisi kansion, jossa on muuttotiedostot. Sen jälkeen voit hakea muuttoa tietokantaan:

```bash
dotnet ef database update
```

Tämä luo tietokantaa ja taulukoita sinulle.