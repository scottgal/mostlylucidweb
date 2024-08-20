# Aggiunta del Framework di Entità per i post del blog (Pt. 4)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-17T20:00</datetime>

Vedere parti [1](/blog/addingentityframeworkforblogpostspt1) e [2](/blog/addingentityframeworkforblogpostspt2) e [3](/blog/addingentityframeworkforblogpostspt3) per le fasi precedenti.

# Introduzione

Nelle parti precedenti abbiamo esaminato come impostare il database, come i nostri controller e le nostre viste sono strutturati, e come i nostri servizi hanno funzionato. In questa parte tratteremo i dettagli su come creare il database con alcuni dati iniziali e come funzionano i servizi basati sull'impronta ambientale.

Come al solito puoi vedere tutte le fonti per questo sul mio GitHub [qui](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog), nella cartella Mostlylucid/Blog.

[TOC]

# Vedere la banca dati

Nella parte precedente abbiamo trattato come [inizializzare e impostare i servizi](/blog/addingentityframeworkforblogpostspt2#setup). In questa parte ci occuperemo di come seminare il database con alcuni dati iniziali. Questo è fatto nel `EFBlogPopulator` classe. Questa classe è registrata come un servizio nel `SetupBlog` metodo di estensione.

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

Si può vedere che nel `Populate` metodo che chiamiamo in `_markdownBlogService.GetPages()` questo passa attraverso i nostri file makrdown e popola un sacco di `BlogViewModels` contenente tutti i posti.
Noi facciamo lo stesso per le lingue; questo guarda al nostro `translated` cartella per tutti i file di markdown tradotti che abbiamo generato utilizzando EasyNMT (vedi [qui](/blog/autotranslatingmarkdownfiles) per come facciamo quella parte).

## Aggiunta delle lingue

Poi chiamiamo il nostro `EnsureLanguages` metodo che assicura che tutte le lingue siano nella banca dati. Questo è un metodo semplice che controlla se la lingua esiste e se non lo aggiunge al database.

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

Vedrete che questo è abbastanza semplice e solo assicura che tutti i linguaggi che abbiamo ottenuto dai post di markdown sono nel database; e come abbiamo specificato che gli ID sono generati automaticamente abbiamo bisogno di `SaveChanges` per garantire che gli ID siano generati.

### Aggiunta delle categorie e dei messaggi

Poi chiamiamo il nostro `EnsureCategoriesAndPosts` metodo che garantisce che tutte le categorie e i posti siano presenti nella banca dati. Questo è un po' più complesso in quanto dobbiamo garantire che le categorie siano nella banca dati e quindi dobbiamo garantire che i posti siano nella banca dati.

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

Potete vedere che chiamiamo nella `PostsQuery` metodo della nostra classe Base che è un metodo semplice che restituisce una queryable della `BlogPostEntity` Cosi' possiamo interrogare il database per i post.

```csharp
  protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);
   
```

#### Aggiungere le categorie

Abbiamo poi chiamato in `AddCategoriesToContext` metodo che garantisce che tutte le categorie si trovino nella banca dati. Questo è un metodo semplice che controlla se la categoria esiste e se non lo aggiunge al database.

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

Ancora una volta questo verifica se la categoria esiste e se non lo aggiunge al database.

#### Aggiunta dei post del blog

Abbiamo poi chiamato in `AddBlogPostToContext` metodo che garantisce che tutti i posti siano nella banca dati. Questo è un po' più complesso in quanto dobbiamo garantire che il post sia nella banca dati e quindi dobbiamo garantire che le categorie siano nella banca dati.

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

Come potete vedere questo ha un sacco di rilevamento del cambiamento per garantire che non re-aggiungere i post che non sono cambiati. Controlliamo l'hash del contenuto, le categorie, la data e il titolo. Se uno di questi è cambiato, aggiorneremo il post nel database.

Una cosa da notare è quanto fastidioso controllare un DataTimeOffset è; dobbiamo convertirlo in UTC e poi ottenere la data per confrontarlo. Ciò è dovuto al fatto che `DateTimeOffset` ha una componente temporale e vogliamo confrontare solo la data.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# In conclusione

Ora abbiamo un sistema di blog completamente funzionante che può essere popolato da file markdown e file markdown tradotti. Nella prossima parte copriremo il servizio semplice che usiamo per visualizzare i post memorizzati nel database.