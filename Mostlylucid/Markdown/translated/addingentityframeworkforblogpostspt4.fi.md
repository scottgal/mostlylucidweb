# Lisää Entity Framework blogikirjoituksiin (Pt. 4)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-17T20:00</datetime>

Katso osat [1](/blog/addingentityframeworkforblogpostspt1) sekä [2](/blog/addingentityframeworkforblogpostspt2) sekä [3](/blog/addingentityframeworkforblogpostspt3) edellisten vaiheiden osalta.

# Johdanto

Aiemmissa osissa selvitimme, miten tietokanta perustetaan, miten ohjaimemme ja näkemyksemme rakentuvat ja miten palvelumme toimivat. Tässä osassa kerromme yksityiskohtaisesti, miten tietokantaan voi upottaa alkutietoja ja miten EF-pohjaiset palvelut toimivat.

Kuten tavallista, näet kaiken tämän lähteen GitHubistani. [täällä](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog), Enimmäkseen lucid/Blog-kansiossa.

[TÄYTÄNTÖÖNPANO

# Tietokannan kylväminen

Edellisessä osassa kerroimme, miten [Alusta ja perusta palvelut](/blog/addingentityframeworkforblogpostspt2#setup)...................................................................................................................................... Tässä osassa käsittelemme, miten tietokantaan syötetään alustavia tietoja. Tämä tapahtuu `EFBlogPopulator` Luokka. Tämä luokka on rekisteröity palveluksi `SetupBlog` laajennusmenetelmä.

```csharp
    public async Task Populate()
    {
        var posts = await _markdownBlogService.GetPages();
        var languages = _markdownBlogService.LanguageList();

        var languageEntities = await EnsureLanguages(languages);
        await EnsureCategoriesAndPosts(posts, languageEntities);

        await Context.SaveChangesAsync();
    }
```

Sen voi nähdä seuraavasta kuvasta: `Populate` Metodi, jota kutsumme mukaan `_markdownBlogService.GetPages()` Tämä käy läpi makrdown-tiedostojamme ja kansoittaa joukon `BlogViewModels` sisältää kaikki virat.
Sen jälkeen teemme samoin kielille. `translated` Kansio kaikista EasyNMT:n avulla syntyneistä käännetyistä markown-tiedostoista (ks. [täällä](/blog/autotranslatingmarkdownfiles) siitä, miten me sen osan teemme).

## Kielten lisääminen

Sitten kutsumme meidän `EnsureLanguages` menetelmä, jolla varmistetaan, että kaikki kielet ovat tietokannassa. Tämä on yksinkertainen menetelmä, joka tarkistaa, onko kieltä olemassa ja jos ei lisää sitä tietokantaan.

```csharp
  private async Task<List<LanguageEntity>> EnsureLanguages(Dictionary<string, List<string>> languages)
    {
        var languageList = languages.SelectMany(x => x.Value).ToList();
        var currentLanguages = await Context.Languages.Select(x => x.Name).ToListAsync();

        var languageEntities = new List<LanguageEntity>();
        var enLang = new LanguageEntity { Name =MarkdownBaseService.EnglishLanguage };

        if (!currentLanguages.Contains(MarkdownBaseService.EnglishLanguage)) Context.Languages.Add(enLang);
        languageEntities.Add(enLang);

        foreach (var language in languageList)
        {
            if (languageEntities.Any(x => x.Name == language)) continue;

            var langItem = new LanguageEntity { Name = language };

            if (!currentLanguages.Contains(language)) Context.Languages.Add(langItem);

            languageEntities.Add(langItem);
        }

        await Context.SaveChangesAsync(); // Save the languages first so we can reference them in the blog posts
        return languageEntities;
    }
```

Huomaat, että tämä on ppretty yksinkertainen ja vain varmistaa, että kaikki kielet, jotka saimme markdown posts ovat tietokannassa; ja kuten täsmensimme, että Ids ovat automaattisesti tuotettuja meidän täytyy `SaveChanges` varmistaakseen, että tunnisteet syntyvät.

### Luokkien ja viestien lisääminen

Sitten kutsumme meidän `EnsureCategoriesAndPosts` menetelmä, jolla varmistetaan, että kaikki luokat ja virat ovat tietokannassa. Tämä on hieman monimutkaisempaa, koska meidän on varmistettava, että luokat ovat tietokannassa, ja sen jälkeen meidän on varmistettava, että virat ovat tietokannassa.

```csharp
    private async Task EnsureCategoriesAndPosts(
        IEnumerable<BlogPostViewModel> posts,
        List<LanguageEntity> languageEntities)
    {
        var existingCategories = await Context.Categories.Select(x => x.Name).ToListAsync();

        var languages = languageEntities.ToDictionary(x => x.Name, x => x);
        var categories = new List<CategoryEntity>();

        foreach (var post in posts)
        {
            var currentPost =
                await PostsQuery().FirstOrDefaultAsync(x => x.Slug == post.Slug && x.LanguageEntity.Name == post.Language);
            await AddCategoriesToContext(post.Categories, existingCategories, categories);
            await AddBlogPostToContext(post, languages[post.Language], categories, currentPost);
        }
    }
```

Huomaat, että kutsumme `PostsQuery` Method of our Base class, joka on yksinkertainen menetelmä, joka palauttaa kyseenalaistaa `BlogPostEntity` Voimme siis tiedustella tietokannasta virkoja.

```csharp
  protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);
   
```

#### Luokkien lisääminen

Sitten kutsumme sisään `AddCategoriesToContext` menetelmä, jolla varmistetaan, että kaikki luokat ovat tietokannassa. Tämä on yksinkertainen menetelmä, joka tarkistaa, onko luokka olemassa ja jos ei lisää sitä tietokantaan.

```csharp
    private async Task AddCategoriesToContext(
        IEnumerable<string> categoryList,
        List<string> existingCategories,
        List<CategoryEntity> categories)
    {
        foreach (var category in categoryList)
        {
            if (categories.Any(x => x.Name == category)) continue;

            var cat = new CategoryEntity { Name = category };

            if (!existingCategories.Contains(category)) await Context.Categories.AddAsync(cat);

            categories.Add(cat);
        }
    }

```

Tämäkin tarkistaa, onko luokka olemassa ja jos ei lisää sitä tietokantaan.

#### Lisää blogiviestejä

Sitten kutsumme sisään `AddBlogPostToContext` menetelmä, jolla varmistetaan, että kaikki virat ovat tietokannassa. Tämä on hieman monimutkaisempaa, koska meidän on varmistettava, että viesti on tietokannassa, ja sen jälkeen meidän on varmistettava, että luokat ovat tietokannassa.

```csharp
  private async Task AddBlogPostToContext(
        BlogPostViewModel post,
        LanguageEntity postLanguageEntity,
        List<CategoryEntity> categories,
        BlogPostEntity? currentPost)
    {
        try
        {
            var hash = post.HtmlContent.ContentHash();
            var currentCategoryNames = currentPost?.Categories.Select(x => x.Name).ToArray() ?? Array.Empty<string>();
            var categoriesChanged = false;
            if (!currentCategoryNames.All(post.Categories.Contains) ||
                !post.Categories.All(currentCategoryNames.Contains))
            {
                categoriesChanged = true;
                _logger.LogInformation("Categories have changed for post {Post}", post.Slug);
            }

            var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
            var titleChanged = currentPost?.Title != post.Title;
            if (!titleChanged && !dateChanged && hash == currentPost?.ContentHash && !categoriesChanged)
            {
                _logger.LogInformation("Post {Post} has not changed", post.Slug);
                return;
            }

            var blogPost = new BlogPostEntity
            {
                Title = post.Title,
                Slug = post.Slug,
                HtmlContent = post.HtmlContent,
                PlainTextContent = post.PlainTextContent,
                ContentHash = hash,
                PublishedDate = post.PublishedDate,
                LanguageEntity = postLanguageEntity,
                LanguageId = postLanguageEntity.Id,
                Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList()
            };


            if (currentPost != null)
            {
                _logger.LogInformation("Updating post {Post}", post.Slug);
                Context.BlogPosts.Update(blogPost);
            }
            else
            {
                _logger.LogInformation("Adding post {Post}", post.Slug);
                await Context.BlogPosts.AddAsync(blogPost);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error adding post {Post}", post.Slug);
        }
    }

```

Kuten näette, tässä on paljon muutoshavaitsemista sen varmistamiseksi, että emme lisää viestejä, jotka eivät ole muuttuneet. Tarkistamme sisällön hasista, kategorioista, päivämäärästä ja otsikosta. Jos jokin näistä on muuttunut, päivitämme viestin tietokantaan.

Yksi asia on huomata, kuinka ärsyttävää DateTimeOffset on; meidän täytyy muuntaa se UTC:ksi ja sitten saada päivämäärä, jolloin sitä voi verrata. Tämä johtuu siitä, että `DateTimeOffset` Siinä on aikakomponentti, ja haluamme vertailla vain ajankohtaa.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# Johtopäätöksenä

Nyt meillä on täysin toimiva blogijärjestelmä, joka voidaan pakata markown-tiedostoista ja kääntää markown-tiedostoja. Seuraavassa osassa käsitellään yksinkertaista palvelua, jolla näytämme tietokantaan tallennettuja viestejä.