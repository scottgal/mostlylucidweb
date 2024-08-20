# Bloggaamiseen (osa 2)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-15T18:00</datetime>

Löydät kaikki lähdekoodit blogikirjoituksista [GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Osa 2 sarjasta, joka koskee Entity Frameworkin lisäämistä.NET Core -hankkeeseen.**
Osa 1 löytyy [täällä](/blog/addingentityframeworkforblogpostspt1).

# Johdanto

Edellisessä viestissä perustimme tietokannan ja taustan blogikirjoituksillemme. Tässä viestissä lisäämme palvelut vuorovaikutukseen tietokannan kanssa.

Seuraavassa viestissä kerromme yksityiskohtaisesti, miten nämä palvelut toimivat nyt olemassa olevien ohjaajien ja näkökantojen kanssa.

[TÄYTÄNTÖÖNPANO

### Asetukset

Meillä on nyt BlogSetup-laajennusluokka, joka perustaa nämä palvelut. Tämä on jatkoa sille, mitä teimme. [Osa 1](/blog/addingentityframeworkforblogpostspt1), johon perustimme tietokannan ja kontekstin.

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

Tässä käytetään yksinkertaista `BlogConfig` luokka määrittää, missä tilassa olemme, joko `File` tai `Database`...................................................................................................................................... Tämän perusteella rekisteröimme tarvitsemamme palvelut.

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

## Liitännät

Kuten haluan sekä tukea tiedoston ja Database tässä sovelluksessa (koska miksi ei! Olen käyttänyt rajapintapohjaista lähestymistapaa, jonka avulla nämä voidaan vaihtaa konfiguraation perusteella.

Meillä on kolme uutta rajapintaa. `IBlogService`, `IMarkdownBlogService` sekä `IBlogPopulator`.

#### IBlogService

Tämä on blogipalvelun päärajapinta. Se sisältää keinoja saada virkoja, luokkia ja yksittäisiä virkoja.

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

Tätä palvelua käyttävät `EFlogPopulatorService` ensimmäisen suorituksen aikana tietokantaa kansoitetaan markdown-tiedostojen postauksilla.

```csharp
public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();
}
```

Kuten näette, se on aika yksinkertainen ja siinä on vain kaksi menetelmää. `GetPages` sekä `LanguageList`...................................................................................................................................... Niitä käytetään Markdown-tiedostojen käsittelyyn ja kieliluettelon saamiseen.

#### IBlogPopulaattori

BlogPopulaattoria käytetään yllä olevassa asetusmenetelmässämme, jotta tietokanta tai staattinen välimuistio (File-pohjainen järjestelmä) voidaan kansoittaa viestien avulla.

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

Voit nähdä, että tämä on laajennus `WebApplication` Konfiguroimalla tietokantasiirtolaisuuden tarvittaessa (joka myös luo tietokannan, jos sitä ei ole). Sitten se kutsuu konfiguroitua `IBlogPopulator` palvelu tietokannan kansoittamiseksi.

Tämä on sen palvelun rajapinta.

```csharp
public interface IBlogPopulator
{
    Task Populate();
}
```

### Täytäntöönpano

Aika yksinkertaista, vai mitä? Tämä toteutetaan molemmissa EU:n jäsenvaltioissa. `MarkdownBlogPopulator` sekä `EFBlogPopulator` kursseja.

- Markdown - tässä kutsumme `GetPages` metodia ja kansoita välimuisti.

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

- EF - tässä me kutsumme `IMarkdownBlogService` Saada sivut ja sitten kansoittaa tietokanta.

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

Olemme jakaneet tämän toiminnon rajapintoihin tehdäksemme koodista ymmärrettävämmän ja "eriytetyn" (kuten SOLID-periaatteissa). Näin voimme helposti vaihtaa palvelut pois konfiguraation perusteella.

# Johtopäätöksenä

Seuraavassa viestissä tarkastelemme tarkemmin näiden palveluiden käyttöä ohjaajien ja näkökulmien toteutuksessa.