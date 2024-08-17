# Het toevoegen van Entity Framework voor Blog berichten (Pt. 4)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-17T20:00</datetime>

Zie delen [1](/blog/addingentityframeworkforblogpostspt1) en [2](/blog/addingentityframeworkforblogpostspt2) en [3](/blog/addingentityframeworkforblogpostspt3) voor de vorige stappen.

# Inleiding

In eerdere delen bespraken we hoe we de database konden opzetten, hoe onze controllers en visies gestructureerd zijn en hoe onze diensten werkten. In dit deel zullen we details behandelen over hoe we de database kunnen starten met enkele initiële gegevens en hoe de EF-gebaseerde diensten werken.

Zoals gewoonlijk kunt u alle bron te zien voor dit op mijn GitHub [Hier.](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog), in de map Meestallucid/Blog.

[TOC]

# Database aan het activeren

In het vorige deel hebben we besproken hoe we [initialiseren en opzetten van de diensten](/blog/addingentityframeworkforblogpostspt2#setup). In dit deel zullen we bespreken hoe we de database kunnen starten met wat eerste gegevens. Dit wordt gedaan in de `EFBlogPopulator` Klas. Deze klasse is geregistreerd als dienst in de `SetupBlog` uitbreidingsmethode.

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

Dat zie je in de `Populate` methode die we oproepen in de `_markdownBlogService.GetPages()` Dit loopt door onze makrdown bestanden en populeert een heleboel `BlogViewModels` met alle posten.
We doen dan hetzelfde voor de talen; dit kijkt naar onze `translated` map voor alle vertaalde markdown bestanden die we hebben gegenereerd met behulp van EasyNMT (zie [Hier.](/blog/autotranslatingmarkdownfiles) voor hoe we dat deel doen).

## Talen toevoegen

We roepen dan op tot onze `EnsureLanguages` methode die ervoor zorgt dat alle talen in de database staan. Dit is een eenvoudige methode die controleert of de taal bestaat en zo niet toevoegt aan de database.

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

U zult zien dat dit is vrij eenvoudig en gewoon zorgt ervoor dat alle talen die we kregen uit de markdown berichten zijn in de database; en zoals we gespecificeerd dat de ID's zijn automatisch gegenereerd moeten we `SaveChanges` om ervoor te zorgen dat de ID's worden gegenereerd.

### De categorieën en berichten toevoegen

We roepen dan op tot onze `EnsureCategoriesAndPosts` methode die ervoor zorgt dat alle categorieën en posten in de database staan. Dit is een beetje complexer omdat we ervoor moeten zorgen dat de categorieën in de database zitten en dan moeten we ervoor zorgen dat de berichten in de database zitten.

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

U kunt zien dat we oproepen tot de `PostsQuery` methode van onze Base class dat is een eenvoudige methode die een opgevraagde van de `BlogPostEntity` Zodat we de database voor de berichten kunnen doorzoeken.

```csharp
  protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);
   
```

#### De categorieën toevoegen

De Voorzitter. - Aan de orde is het gecombineerd debat over `AddCategoriesToContext` methode die ervoor zorgt dat alle categorieën in de database staan. Dit is een eenvoudige methode die controleert of de categorie bestaat en zo niet toevoegt aan de database.

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

Opnieuw controleert dit of de categorie bestaat en zo niet toevoegt aan de database.

#### De blogberichten toevoegen

De Voorzitter. - Aan de orde is het gecombineerd debat over `AddBlogPostToContext` methode die ervoor zorgt dat alle berichten in de database. Dit is een beetje complexer omdat we ervoor moeten zorgen dat de post in de database zit en dan moeten we ervoor zorgen dat de categorieën in de database zitten.

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

Zoals je kunt zien heeft dit een LOT van verandering detectie om ervoor te zorgen dat we niet opnieuw toevoegen berichten die niet zijn veranderd. We controleren de hash van de inhoud, de categorieën, de datum en de titel. Als een van deze zijn veranderd, werken we de post in de database bij.

Een ding om op te merken is hoe vervelend het controleren van een DateTimeOffset is; we moeten het omzetten naar UTC en dan krijgen de datum om het te vergelijken. Dit is omdat de `DateTimeOffset` heeft een tijdcomponent en we willen alleen de datum te vergelijken.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# Conclusie

Nu hebben we een volledig werkend blogsysteem dat kan worden bevolkt uit markdown-bestanden en vertaald markdown-bestanden. In het volgende deel behandelen we de eenvoudige dienst die we gebruiken om berichten weer te geven die zijn opgeslagen in de database.