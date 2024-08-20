# Adding Entity Framework for Blog Posts (Pt. 4)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-17T20:00</datetime>

Siehe Teile [1](/blog/addingentityframeworkforblogpostspt1) und [2](/blog/addingentityframeworkforblogpostspt2) und [3](/blog/addingentityframeworkforblogpostspt3) für die vorherigen Schritte.

# Einleitung

In früheren Teilen befassten wir uns mit der Einrichtung der Datenbank, der Strukturierung unserer Controller und Ansichten und der Funktionsweise unserer Dienstleistungen. In diesem Teil werden wir Details darüber, wie man die Datenbank mit einigen ersten Daten und wie die EF Based Services arbeiten.

Wie immer können Sie alle Quellen dafür auf meinem GitHub sehen [Hierher](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog), im Mostlylucid/Blog Ordner.

[TOC]

# Suchen der Datenbank

Im vorigen Teil haben wir behandelt, wie wir [initialisierung und Einrichtung der Dienste](/blog/addingentityframeworkforblogpostspt2#setup)......................................................................................................... In diesem Teil werden wir abdecken, wie man die Datenbank mit ein paar ersten Daten. Dies geschieht in der `EFBlogPopulator` Unterricht. Diese Klasse ist als Dienst in der `SetupBlog` Erweiterungsmethode.

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

Sie können sehen, dass in der `Populate` Methode, die wir in die `_markdownBlogService.GetPages()` das läuft durch unsere makrdown-Dateien und bevölkert eine Reihe von `BlogViewModels` mit allen Pfosten.
Wir tun dann das gleiche für die Sprachen; dies betrachtet unsere `translated` Ordner für alle übersetzten Markdown-Dateien, die wir mit EasyNMT erstellt haben (siehe [Hierher](/blog/autotranslatingmarkdownfiles) für wie wir diesen Teil tun).

## Hinzufügen der Sprachen

Wir rufen dann in unsere `EnsureLanguages` Methode, die sicherstellt, dass sich alle Sprachen in der Datenbank befinden. Dies ist eine einfache Methode, die überprüft, ob die Sprache existiert und wenn nicht, fügt sie der Datenbank hinzu.

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

Sie werden sehen, dass dies ppretty einfach ist und nur sicherstellt, dass alle Sprachen, die wir von den Markdown-Posts erhalten haben, in der Datenbank sind; und da wir spezifiziert haben, dass die Ids automatisch generiert werden, müssen wir `SaveChanges` um sicherzustellen, dass die Ids generiert werden.

### Hinzufügen der Kategorien und Beiträge

Wir rufen dann in unsere `EnsureCategoriesAndPosts` Methode, die sicherstellt, dass alle Kategorien und Beiträge in der Datenbank sind. Dies ist etwas komplexer, da wir sicherstellen müssen, dass die Kategorien in der Datenbank sind und dann müssen wir sicherstellen, dass die Beiträge in der Datenbank sind.

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

Sie können sehen, dass wir in die `PostsQuery` Methode unserer Basisklasse, die eine einfache Methode ist, die eine abfragebare der `BlogPostEntity` so können wir die Datenbank für die Beiträge abfragen.

```csharp
  protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);
   
```

#### Hinzufügen der Kategorien

Wir rufen dann in die `AddCategoriesToContext` Methode, die sicherstellt, dass sich alle Kategorien in der Datenbank befinden. Dies ist eine einfache Methode, die überprüft, ob die Kategorie existiert und wenn nicht, fügt sie der Datenbank hinzu.

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

Auch dies überprüft, ob die Kategorie existiert und wenn nicht, fügt sie der Datenbank hinzu.

#### Hinzufügen der Blog-Posts

Wir rufen dann in die `AddBlogPostToContext` Methode, die sicherstellt, dass alle Beiträge in der Datenbank sind. Dies ist etwas komplexer, da wir sicherstellen müssen, dass die Post in der Datenbank ist, und dann müssen wir sicherstellen, dass die Kategorien in der Datenbank sind.

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

Wie Sie sehen können, hat dies eine große Anzahl von Änderungserkennung, um sicherzustellen, dass wir keine neuen Beiträge hinzufügen, die sich nicht geändert haben. Wir prüfen den Hash des Inhalts, der Kategorien, des Datums und des Titels. Wenn einer von diesen geändert haben, aktualisieren wir den Beitrag in der Datenbank.

Eine Sache zu beachten ist, wie lästig die Überprüfung eines DateTimeOffset ist; wir müssen es in UTC konvertieren und dann das Datum erhalten, um es zu vergleichen. Dies ist, weil die `DateTimeOffset` hat eine Zeitkomponente und wir wollen nur das Datum vergleichen.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# Schlussfolgerung

Jetzt haben wir ein voll funktionsfähiges Blog-System, das aus Markdown-Dateien und übersetzten Markdown-Dateien bevölkert werden kann. Im nächsten Teil decken wir den einfachen Service ab, den wir verwenden, um in der Datenbank gespeicherte Beiträge anzuzeigen.