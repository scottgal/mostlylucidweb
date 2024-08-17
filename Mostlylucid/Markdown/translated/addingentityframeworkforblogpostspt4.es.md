# Añadiendo marco de entidad para entradas de blog (Pt. 4)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-17T20:00</datetime>

Ver partes [1](/blog/addingentityframeworkforblogpostspt1) y [2](/blog/addingentityframeworkforblogpostspt2) y [3](/blog/addingentityframeworkforblogpostspt3) para los pasos anteriores.

# Introducción

En partes anteriores cubrimos cómo configurar la base de datos, cómo se estructuran nuestros controladores y vistas y cómo funcionaban nuestros servicios. En esta parte cubriremos detalles sobre cómo sembrar la base de datos con algunos datos iniciales y cómo funcionan los servicios basados en la FE.

Como siempre puedes ver toda la fuente de esto en mi GitHub [aquí](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog), en la carpeta Mostlylucid/Blog.

[TOC]

# Sembrando la base de datos

En la parte anterior nos ocupamos de cómo [inicializar y configurar los servicios](/blog/addingentityframeworkforblogpostspt2#setup). En esta parte cubriremos cómo sembrar la base de datos con algunos datos iniciales. Esto se hace en el `EFBlogPopulator` clase. Esta clase está registrada como servicio en el `SetupBlog` método de extensión.

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

Usted puede ver que en el `Populate` método que llamamos a la `_markdownBlogService.GetPages()` esto se ejecuta a través de nuestros archivos makrdown y pobla un montón de `BlogViewModels` que contiene todos los puestos.
Entonces hacemos lo mismo por los idiomas; esto mira a nuestro `translated` carpeta para todos los archivos Markdown traducidos que generamos utilizando EasyNMT (ver [aquí](/blog/autotranslatingmarkdownfiles) por cómo hacemos esa parte).

## Añadir los idiomas

Luego llamamos a nuestro `EnsureLanguages` método que garantiza que todos los idiomas están en la base de datos. Este es un método simple que comprueba si el idioma existe y si no lo añade a la base de datos.

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

Verás que esto es muy simple y simplemente asegura que todos los lenguajes que tenemos de los postes de marca hacia abajo están en la base de datos; y como especificamos que los Ids son generados automáticamente necesitamos `SaveChanges` para asegurar que se generen los Ids.

### Añadiendo las categorías y los posts

Luego llamamos a nuestro `EnsureCategoriesAndPosts` método que garantice que todas las categorías y puestos están en la base de datos. Esto es un poco más complejo, ya que tenemos que asegurarnos de que las categorías están en la base de datos y luego tenemos que asegurarnos de que los puestos están en la base de datos.

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

Usted puede ver que llamamos a la `PostsQuery` método de nuestra clase base que es un método simple que devuelve una consultable de la `BlogPostEntity` así que podemos consultar la base de datos para los posts.

```csharp
  protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);
   
```

#### Añadir las categorías

Luego llamamos a la `AddCategoriesToContext` método que garantice que todas las categorías están en la base de datos. Este es un método simple que comprueba si la categoría existe y si no se añade a la base de datos.

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

De nuevo, esto comprueba si la categoría existe y si no se añade a la base de datos.

#### Añadiendo las entradas del blog

Luego llamamos a la `AddBlogPostToContext` método que garantiza que todos los puestos están en la base de datos. Esto es un poco más complejo, ya que tenemos que asegurarnos de que el post está en la base de datos y luego tenemos que asegurarnos de que las categorías están en la base de datos.

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

Como puedes ver, esto tiene un montón de detección de cambios para asegurarnos de que no añadamos mensajes que no hayan cambiado. Comprobamos el hash del contenido, las categorías, la fecha y el título. Si alguno de estos ha cambiado actualizamos el post en la base de datos.

Una cosa a notar es cómo molesto comprobar un DateTimeOffset es; tenemos que convertirlo a UTC y luego obtener la fecha para compararlo. Esto es porque el `DateTimeOffset` tiene un componente de tiempo y queremos comparar sólo la fecha.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# Conclusión

Ahora tenemos un sistema de blog completamente funcional que puede ser poblado de archivos Markdown y traducidos archivos Markdown. En la siguiente parte cubriremos el simple Servicio que usamos para mostrar las publicaciones almacenadas en la base de datos.