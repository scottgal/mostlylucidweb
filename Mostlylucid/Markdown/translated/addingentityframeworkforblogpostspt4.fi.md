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
        var languages = languageEntities.ToDictionary(x => x.Name, x => x);
        var currentPosts = await PostsQuery().ToListAsync();
        foreach (var post in posts)
        {
            var existingCategories = Context.Categories.Local.ToList();
            var currentPost =
                currentPosts.FirstOrDefault(x => x.Slug == post.Slug && x.LanguageEntity.Name == post.Language);
            await AddCategoriesToContext(post.Categories, existingCategories);
            existingCategories = Context.Categories.Local.ToList();
            await AddBlogPostToContext(post, languages[post.Language], existingCategories, currentPost);
        }
    }
```

Täällä käytämme Context.Categories.Local seurata kategorioita lisätään Context (Ne tallennetaan tietokantaan aikana `SaveAsync` puhelu).
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
        List<CategoryEntity> existingCategories)
    {
        foreach (var category in categoryList)
        {
            if (existingCategories.Any(x => x.Name == category)) continue;

            var cat = new CategoryEntity { Name = category };

             await Context.Categories.AddAsync(cat);
        }
    }

```

Tämäkin tarkistaa, onko luokka olemassa ja jos ei lisää sitä tietokantaan.

#### Lisää blogiviestejä

Sitten kutsumme sisään `AddBlogPostToContext` metodin, tämä sitten kutsuu osaksi `EFBaseService` tallentaa viesti tietokantaan.

```csharp
    private async Task AddBlogPostToContext(
        BlogPostViewModel post,
        LanguageEntity postLanguageEntity,
        List<CategoryEntity> categories,
        BlogPostEntity? currentPost)
    {
        await SavePost(post, currentPost, categories, new List<LanguageEntity> { postLanguageEntity });
    }
```

Me teemme tämän kutsumalla `SavePost` menetelmä, joka on menetelmä, joka tallentaa viestin tietokantaan. Tämä menetelmä on hieman monimutkainen, koska sen täytyy tarkistaa, onko viesti muuttunut ja päivittää viesti tietokantaan.

```csharp

   public async Task<BlogPostEntity?> SavePost(BlogPostViewModel post, BlogPostEntity? currentPost =null ,
        List<CategoryEntity>? categories = null,
        List<LanguageEntity>? languages = null)
    {
        if (languages == null)
            languages = await Context.Languages.ToListAsync();

    var postLanguageEntity = languages.FirstOrDefault(x => x.Name == post.Language);
        if (postLanguageEntity == null)
        {
            Logger.LogError("Language {Language} not found", post.Language);
            return null;
        }
        categories ??= await Context.Categories.Where(x => post.Categories.Contains(x.Name)).ToListAsync();
         currentPost ??= await PostsQuery().Where(x=>x.Slug == post.Slug).FirstOrDefaultAsync();
        try
        {
            var hash = post.HtmlContent.ContentHash();
            var currentCategoryNames = currentPost?.Categories.Select(x => x.Name).ToArray() ?? Array.Empty<string>();
            var categoriesChanged = false;
            if (!currentCategoryNames.All(post.Categories.Contains) ||
                !post.Categories.All(currentCategoryNames.Contains))
            {
                categoriesChanged = true;
                Logger.LogInformation("Categories have changed for post {Post}", post.Slug);
            }

            var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
            var titleChanged = currentPost?.Title != post.Title;
            if (!titleChanged && !dateChanged && hash == currentPost?.ContentHash && !categoriesChanged)
            {
                Logger.LogInformation("Post {Post} has not changed", post.Slug);
                return currentPost;
            }

            
            var blogPost = currentPost ?? new BlogPostEntity();
            
            blogPost.Title = post.Title;
            blogPost.Slug = post.Slug;
            blogPost.OriginalMarkdown = post.OriginalMarkdown;
            blogPost.HtmlContent = post.HtmlContent;
            blogPost.PlainTextContent = post.PlainTextContent;
            blogPost.ContentHash = hash;
            blogPost.PublishedDate = post.PublishedDate;
            blogPost.LanguageEntity = postLanguageEntity;
            blogPost.Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList();

            if (currentPost != null)
            {
                Logger.LogInformation("Updating post {Post}", post.Slug);
                Context.BlogPosts.Update(blogPost); // Update the existing post
            }
            else
            {
                Logger.LogInformation("Adding new post {Post}", post.Slug);
                Context.BlogPosts.Add(blogPost); // Add a new post
            }
            return blogPost;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error adding post {Post}", post.Slug);
        }

        return null;
    }

```

Kuten näette, tässä on paljon muutoshavaitsemista sen varmistamiseksi, että emme lisää viestejä, jotka eivät ole muuttuneet. Tarkistamme sisällön hasista, kategorioista, päivämäärästä ja otsikosta. Jos jokin näistä on muuttunut, päivitämme viestin tietokantaan.

Yksi asia on huomata, kuinka ärsyttävää DateTimeOffset on; meidän täytyy muuntaa se UTC:ksi ja sitten saada päivämäärä, jolloin sitä voi verrata. Tämä johtuu siitä, että `DateTimeOffset` Siinä on aikakomponentti, ja haluamme vertailla vain ajankohtaa.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# Johtopäätöksenä

Nyt meillä on täysin toimiva blogijärjestelmä, joka voidaan pakata markown-tiedostoista ja kääntää markown-tiedostoja. Seuraavassa osassa käsitellään yksinkertaista palvelua, jolla näytämme tietokantaan tallennettuja viestejä.