# Lägga till Entity Framework för blogginlägg (Pt. 4) Förstainstansrätten har beslutat följande dom:

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-17T20:00.........................................................................................................</datetime>

Se även de delar som är avsedda att användas vid tillverkning av varor enligt kapitel 87. [1](/blog/addingentityframeworkforblogpostspt1) och [2](/blog/addingentityframeworkforblogpostspt2) och [3](/blog/addingentityframeworkforblogpostspt3) för de föregående stegen.

# Inledning

I tidigare delar tog vi upp hur vi skulle skapa databasen, hur våra controllers och vyer är strukturerade och hur våra tjänster fungerade. I den här delen kommer vi att behandla detaljer om hur man kan så databasen med några inledande data och hur EF-baserade tjänster fungerar.

Som vanligt kan du se alla källor för detta på min GitHub [här](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog), i mappen Mostlylucid/Blog.

[TOC]

# Lägga till databasen

I den föregående delen tog vi upp hur vi [initiera och inrätta tjänsterna](/blog/addingentityframeworkforblogpostspt2#setup)....................................... I denna del kommer vi att täcka hur man så databasen med några inledande data. Detta görs i `EFBlogPopulator` Klassen. Denna klass är registrerad som en tjänst i `SetupBlog` Utökningsmetod.

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

Du kan se det i `Populate` metod som vi kallar in i `_markdownBlogService.GetPages()` Detta går igenom våra Macrdown-filer och befolkar en massa `BlogViewModels` som innehåller alla inlägg.
Vi gör sedan samma sak för språken; detta tittar på våra `translated` mapp för alla översatta markdown-filer vi genererade med EasyNMT (se [här](/blog/autotranslatingmarkdownfiles) för hur vi gör den delen).

## Lägga till språk

Vi kallar sedan in vår `EnsureLanguages` Metod som säkerställer att alla språk finns i databasen. Detta är en enkel metod som kontrollerar om språket finns och om inte lägger till det i databasen.

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

Du kommer att se detta är ppretty enkel och bara ser till att alla språk vi fick från markdown inlägg är i databasen; och som vi specificerat att de Ids är automatiskt genererade vi måste `SaveChanges` för att säkerställa att identifieringarna genereras.

### Lägga till kategorier och inlägg

Vi kallar sedan in vår `EnsureCategoriesAndPosts` Metod som säkerställer att alla kategorier och inlägg finns i databasen. Detta är lite mer komplicerat eftersom vi måste se till att kategorierna finns i databasen och sedan måste vi se till att inläggen finns i databasen.

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

Här använder vi Context.Kategorier.Lokal för att spåra de kategorier som för närvarande läggs till i Context (de sparas i databasen under `SaveAsync` - Jag vet inte vad du ska göra.
Du kan se att vi kallar in `PostsQuery` metod för vår basklass som är en enkel metod som returnerar en frågeställare av `BlogPostEntity` så att vi kan fråga databasen efter inläggen.

```csharp
  protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);
   
```

#### Lägga till kategorier

Vi kallar sedan in `AddCategoriesToContext` Metod som säkerställer att alla kategorier finns i databasen. Detta är en enkel metod som kontrollerar om kategorin finns och om inte lägger till den i databasen.

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

Återigen kontrollerar detta om kategorin finns och om den inte läggs till i databasen.

#### Lägga till blogginlägg

Vi kallar sedan in `AddBlogPostToContext` metod, detta kallar sedan in `EFBaseService` för att spara inlägget till databasen.

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

Vi gör detta genom att kalla `SavePost` metod som är en metod som sparar inlägget till databasen. Denna metod är lite komplex eftersom det måste kontrollera om inlägget har ändrats och i så fall uppdatera inlägget i databasen.

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

Som ni kan se har detta en LOT av förändring upptäckt för att se till att vi inte lägga till inlägg som inte har förändrats. Vi kontrollerar hash av innehållet, kategorierna, datum och titeln. Om något av dessa har ändrats uppdaterar vi inlägget i databasen.

En sak att lägga märke till är hur irriterande att kontrollera en DateTimeOffset är; vi måste konvertera det till UTC och sedan få datumet för att jämföra det. Detta beror på att `DateTimeOffset` har en tidskomponent och vi vill jämföra bara datum.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# Slutsatser

Nu har vi ett fullt fungerande bloggsystem som kan befolkas från markdown-filer och översatta markdown-filer. I nästa del täcker vi den enkla tjänsten som vi använder för att visa inlägg som lagras i databasen.