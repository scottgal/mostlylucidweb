# Ajouter un cadre d'entité pour les billets de blog (Pt. 4) Le présent règlement entre en vigueur le vingtième jour suivant celui de sa publication au Journal officiel de l'Union européenne.

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-17T20:00</datetime>

Voir parties [1](/blog/addingentityframeworkforblogpostspt1) et [2](/blog/addingentityframeworkforblogpostspt2) et [3](/blog/addingentityframeworkforblogpostspt3) pour les étapes précédentes.

# Présentation

Dans les parties précédentes, nous avons traité de la façon de configurer la base de données, de la façon dont nos contrôleurs et nos points de vue sont structurés et de la façon dont nos services fonctionnent. Dans cette partie, nous présenterons des détails sur la façon de semer la base de données avec quelques données initiales et sur le fonctionnement des services EF Based.

Comme d'habitude, vous pouvez voir toute la source pour cela sur mon GitHub [Ici.](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog), dans le dossier Mostlylucid/Blog.

[TOC]

# Mise en place de la base de données

Dans la partie précédente, nous avons traité de la façon dont nous [initialiser et mettre en place les services](/blog/addingentityframeworkforblogpostspt2#setup)C'est ce que j'ai dit. Dans cette partie, nous aborderons la façon de semer la base de données avec quelques données initiales. C'est ce qu'on fait dans le domaine de l'éducation et de la formation tout au long de la vie. `EFBlogPopulator` En cours. Cette classe est enregistrée comme service dans la `SetupBlog` méthode d'extension.

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

Vous pouvez voir cela dans le `Populate` méthode que nous appelons dans le `_markdownBlogService.GetPages()` ceci passe par nos fichiers makrdown et peuple un tas de `BlogViewModels` contenant tous les messages.
Nous faisons alors la même chose pour les langues; ceci regarde notre `translated` dossier pour tous les fichiers de balisage traduits que nous avons générés en utilisant EasyNMT (voir [Ici.](/blog/autotranslatingmarkdownfiles) pour la façon dont nous faisons cette partie).

## Ajouter les langues

Nous appelons alors dans notre `EnsureLanguages` méthode qui garantit que toutes les langues sont dans la base de données. Il s'agit d'une méthode simple qui vérifie si la langue existe et si elle ne l'ajoute pas à la base de données.

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

Vous verrez que c'est pgretty simple et juste s'assurer que toutes les langues que nous avons obtenu des messages de balisage sont dans la base de données; et comme nous avons spécifié que les Ids sont générés automatiquement, nous avons besoin de `SaveChanges` pour s'assurer que les ID sont générés.

### Ajout des catégories et des postes

Nous appelons alors dans notre `EnsureCategoriesAndPosts` la méthode qui garantit que toutes les catégories et tous les postes sont dans la base de données. C'est un peu plus complexe car nous devons nous assurer que les catégories sont dans la base de données et ensuite nous devons nous assurer que les postes sont dans la base de données.

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

Vous pouvez voir que nous appelons dans le `PostsQuery` méthode de notre classe de base qui est une méthode simple qui renvoie une requête de la `BlogPostEntity` afin que nous puissions interroger la base de données pour les messages.

```csharp
  protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);
   
```

#### Ajouter les catégories

Nous appelons alors dans le `AddCategoriesToContext` méthode qui garantit que toutes les catégories sont dans la base de données. Il s'agit d'une méthode simple qui vérifie si la catégorie existe et si elle ne l'ajoute pas à la base de données.

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

Encore une fois, cela vérifie si la catégorie existe et si elle ne l'ajoute pas à la base de données.

#### Ajout des billets de blog

Nous appelons alors dans le `AddBlogPostToContext` méthode qui garantit que tous les messages sont dans la base de données. C'est un peu plus complexe car nous devons nous assurer que le poste est dans la base de données et ensuite nous devons nous assurer que les catégories sont dans la base de données.

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

Comme vous pouvez le voir, il y a un LOT de détection de changement pour s'assurer que nous n'ajoutons pas de posts qui n'ont pas changé. Nous vérifions le hash du contenu, les catégories, la date et le titre. Si l'un d'eux a changé, nous mettons à jour le message dans la base de données.

Une chose à remarquer est à quel point la vérification agaçante d'un DateTimeOffset est; nous devons le convertir en UTC et ensuite obtenir la date pour la comparer. C'est parce que les `DateTimeOffset` a un élément temps et nous voulons comparer juste la date.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# En conclusion

Maintenant, nous avons un système de blog qui fonctionne pleinement qui peut être peuplé à partir des fichiers de balisage et traduit des fichiers de balisage. Dans la partie suivante, nous allons couvrir le service simple que nous utilisons pour afficher les messages stockés dans la base de données.