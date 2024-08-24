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

Hierin gebruiken we de Context.Categorieën.Lokaal om de categorieën te volgen die momenteel aan de Context zijn toegevoegd (ze worden opgeslagen in de Database tijdens de `SaveAsync` call).
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

Opnieuw controleert dit of de categorie bestaat en zo niet toevoegt aan de database.

#### De blogberichten toevoegen

De Voorzitter. - Aan de orde is het gecombineerd debat over `AddBlogPostToContext` methode, dit dan oproept in de `EFBaseService` om de post op te slaan in de database.

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

We doen dit door de `SavePost` methode die een methode is die de post in de database opslaat. Deze methode is een beetje complex omdat het moet controleren of de post is veranderd en als dat zo is het bijwerken van de post in de database.

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

Zoals je kunt zien heeft dit een LOT van verandering detectie om ervoor te zorgen dat we niet opnieuw toevoegen berichten die niet zijn veranderd. We controleren de hash van de inhoud, de categorieën, de datum en de titel. Als een van deze zijn veranderd, werken we de post in de database bij.

Een ding om op te merken is hoe vervelend het controleren van een DateTimeOffset is; we moeten het omzetten naar UTC en dan krijgen de datum om het te vergelijken. Dit is omdat de `DateTimeOffset` heeft een tijdcomponent en we willen alleen de datum te vergelijken.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# Conclusie

Nu hebben we een volledig werkend blogsysteem dat kan worden bevolkt uit markdown-bestanden en vertaald markdown-bestanden. In het volgende deel behandelen we de eenvoudige dienst die we gebruiken om berichten weer te geven die zijn opgeslagen in de database.