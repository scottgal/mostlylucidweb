# Usando Markdown para bloguear

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-02T17:00</datetime>

## Introducción

Markdown es un lenguaje de marcado ligero que puede utilizar para añadir elementos de formato a los documentos de texto de texto plano. Creado por John Gruber en 2004, Markdown es ahora uno de los idiomas de marcado más populares del mundo.

En este sitio utilizo un enfoque súper simple para bloguear, habiendo intentado y fallado mantener un blog en el pasado quería hacer lo más fácil posible escribir y publicar posts. Uso Markdown para escribir mis posts y este sitio tiene un solo servicio usando [Markdig](https://github.com/xoofx/markdig) para convertir la marca a HTML.

[TOC]

## ¿Por qué no un generador de sitio estático?

En una palabra simplicidad. Esto no va a ser un sitio de tráfico super alto, yo uso ASP.NET OutPutCache para guardar en caché las páginas y no voy a actualizarlo tan a menudo. Quería mantener el sitio tan simple como fuera posible y no tener que preocuparse por la sobrecarga de un generador de sitio estático tanto en términos del proceso de construcción y la complejidad del sitio.

Para aclarar; generadores de sitio estático como [Hugo](https://gohugo.io/) / [Jekyll](https://jekyllrb.com/) etc... puede ser una buena solución para muchos sitios, pero para este quería mantenerlo como simple *para mí* lo más posible. Soy un veterano de 25 años de ASP.NET así que entiéndelo por dentro y por fuera. Este diseño de sitio añade complejidad; Tengo vistas, servicios, controladores y un montón de HTML manual & CSS, pero estoy cómodo con eso.

## ¿Por qué no una base de datos?

1. Simplicidad de diseño; Las bases de datos son sistemas potentes para almacenar datos (y añadiré uno para comentarios en breve), sin embargo, también añaden complejidad. A *correctamente* use bases de datos especialmente en una aplicación ASP.NET que añada un montón de código, no importa si está usando [Núcleo básico de la FE](https://learn.microsoft.com/en-us/ef/core/), [Dapper](https://github.com/DapperLib/Dapper) o SQL crudo con ADO.NET. Quería mantener el sitio tan simple como fuera posible *para empezar con*.
2. Facilidad de actualización y despliegue. Este sitio está destinado a demostrar cómo Docker & Docker Compose simple puede ser para ejecutar un sitio. Puedo actualizar el sitio comprobando en nuevo código (incluyendo contenido) a GitHub, la acción se ejecuta, construye la imagen entonces el método Watchtower en mi docker componer archivo actualiza la imagen del sitio automáticamente. Esta es una manera muy simple de actualizar un sitio y quería mantenerlo de esa manera.
3. Ejecutando duplicados; como tengo datos de ZERO que no están dentro de la imagen Docker, significa que puedo EASILY ejecutar duplicados exactos localmente (en mi pequeño cluster de Ubuntu aquí en casa). Esta es una gran manera de probar los cambios con docker (por ejemplo, [cuando hice los cambios ImageSharp](/blog/imagesharpwithdocker) ) antes de desplegarlos en el sitio en vivo.
4. ¡Porque no quería! Quería ver hasta dónde podía llegar con un simple diseño de sitio y hasta ahora estoy bastante contento con él.

## ¿Cómo escribes tus posts?

Simplemente dejo caer un nuevo archivo.md en la carpeta Markdown y el sitio lo recoge y lo renderiza (cuando recuerdo a aet como contenido, esto asegura que es disponible en los archivos de salida! )

Luego, cuando checkin el sitio a GitHub la acción se ejecuta y el sitio se actualiza. ¡Simple!

```mermaid
flowchart LR
    A[Write New Markdown File] -->|Checkin To Github| B(Github Action Triggers) -->  C(Builds Docker Image) --> D(Watchtower Pulls new Image) --> E(Site Updated)
   
  
```

![setascontent.png](setascontent.png)

## ¿Cómo se agregan imágenes?

Ya que acabo de añadir la imagen aquí, te mostraré cómo lo hice. Simplemente añadí la imagen a la carpeta wwwroot/articleimages y la hice referencia en el archivo Markdown de esta manera:

```markdown
![setascontent.png](setascontent.png)
```

A continuación, añado una extensión a mi tubería de Markdig que reescribe estos a la URL correcta (todo acerca de la simplicidad). [Vea aquí el código fuente de la extensión.](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/MarkDigExtensions/ImgExtension.cs)

```csharp
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Mostlylucid.MarkDigExtensions;

public class ImgExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        pipeline.DocumentProcessed += ChangeImgPath;
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
    }

    public void ChangeImgPath(MarkdownDocument document)
    {
        foreach (var link in document.Descendants<LinkInline>())
            if (link.IsImage)
                link.Url = "/articleimages/" + link.Url;
    }
}
```

## El BlogService.

El BlogService es un servicio sencillo que lee los archivos Markdown desde la carpeta Markdown y los convierte a HTML usando Markdig.

La fuente completa para esto es a continuación y [aquí](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/BlogService.cs).

<details>
<summary>Click to see the source code for the BlogService</summary>
```csharp

using System.Globalization;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.MarkDigExtensions;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Services;

public class BlogService
{
private const string Path = "Markdown";
private const string CacheKey = "Categories";

    private static readonly Regex DateRegex = new(
        @"<datetime class=""hidden"">(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})</datetime>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private static readonly Regex WordCoountRegex = new(@"\b\w+\b",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private static readonly Regex CategoryRegex = new(@"<!--\s*category\s*--\s*([^,]+?)\s*(?:,\s*([^,]+?)\s*)?-->",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly ILogger<BlogService> _logger;

    private readonly IMemoryCache _memoryCache;

    private readonly MarkdownPipeline pipeline;

    public BlogService(IMemoryCache memoryCache, ILogger<BlogService> logger)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Use<ImgExtension>().Build();
        ListCategories();
    }


    private Dictionary<string, List<string>> GetFromCache()
    {
        return _memoryCache.Get<Dictionary<string, List<string>>>(CacheKey) ?? new Dictionary<string, List<string>>();
    }

    private void SetCache(Dictionary<string, List<string>> categories)
    {
        _memoryCache.Set(CacheKey, categories, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
        });
    }

    private void ListCategories()
    {
        var cacheCats = GetFromCache();
        var pages = Directory.GetFiles("Markdown", "*.md");
        var count = 0;

        foreach (var page in pages)
        {
            var pageAlreadyAdded = cacheCats.Values.Any(x => x.Contains(page));

            if (pageAlreadyAdded) continue;


            var text = File.ReadAllText(page);
            var categories = GetCategories(text);
            if (!categories.Any()) continue;
            count++;
            foreach (var category in categories)
                if (cacheCats.TryGetValue(category, out var pagesList))
                {
                    pagesList.Add(page);
                    cacheCats[category] = pagesList;
                    _logger.LogInformation("Added category {Category} for {Page}", category, page);
                }
                else
                {
                    cacheCats.Add(category, new List<string> { page });
                    _logger.LogInformation("Created category {Category} for {Page}", category, page);
                }
        }

        if (count > 0) SetCache(cacheCats);
    }

    public List<string> GetCategories()
    {
        var cacheCats = GetFromCache();
        return cacheCats.Keys.ToList();
    }


    public List<PostListModel> GetPostsByCategory(string category)
    {
        var pages = GetFromCache()[category];
        return GetPosts(pages.ToArray());
    }

    public BlogPostViewModel? GetPost(string postName)
    {
        try
        {
            var path = System.IO.Path.Combine(Path, postName + ".md");
            var page = GetPage(path, true);
            return new BlogPostViewModel
            {
                Categories = page.categories, WordCount = WordCount(page.restOfTheLines), Content = page.processed,
                PublishedDate = page.publishDate, Slug = page.slug, Title = page.title
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting post {PostName}", postName);
            return null;
        }
    }

    private int WordCount(string text)
    {
        return WordCoountRegex.Matches(text).Count;
    }


    private string GetSlug(string fileName)
    {
        var slug = System.IO.Path.GetFileNameWithoutExtension(fileName);
        return slug.ToLowerInvariant();
    }

    private static string[] GetCategories(string markdownText)
    {
        var matches = CategoryRegex.Matches(markdownText);
        var categories = matches
            .SelectMany(match => match.Groups.Cast<Group>()
                .Skip(1) // Skip the entire match group
                .Where(group => group.Success) // Ensure the group matched
                .Select(group => group.Value.Trim()))
            .ToArray();
        return categories;
    }

    public (string title, string slug, DateTime publishDate, string processed, string[] categories, string
        restOfTheLines) GetPage(string page, bool html)
    {
        var fileInfo = new FileInfo(page);

        // Ensure the file exists
        if (!fileInfo.Exists) throw new FileNotFoundException("The specified file does not exist.", page);

        // Read all lines from the file
        var lines = File.ReadAllLines(page);

        // Get the title from the first line
        var title = lines.Length > 0 ? Markdown.ToPlainText(lines[0].Trim()) : string.Empty;

        // Concatenate the rest of the lines with newline characters
        var restOfTheLines = string.Join(Environment.NewLine, lines.Skip(1));

        // Extract categories from the text
        var categories = GetCategories(restOfTheLines);

        var publishedDate = fileInfo.CreationTime;
        var publishDate = DateRegex.Match(restOfTheLines).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(publishDate))
            publishedDate = DateTime.ParseExact(publishDate, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

        // Remove category tags from the text
        restOfTheLines = CategoryRegex.Replace(restOfTheLines, "");
        restOfTheLines = DateRegex.Replace(restOfTheLines, "");
        // Process the rest of the lines as either HTML or plain text
        var processed =
            html ? Markdown.ToHtml(restOfTheLines, pipeline) : Markdown.ToPlainText(restOfTheLines, pipeline);

        // Generate the slug from the page filename
        var slug = GetSlug(page);


        // Return the parsed and processed content
        return (title, slug, publishedDate, processed, categories, restOfTheLines);
    }

    public List<PostListModel> GetPosts(string[] pages)
    {
        List<PostListModel> pageModels = new();

        foreach (var page in pages)
        {
            var pageInfo = GetPage(page, false);

            var summary = Markdown.ToPlainText(pageInfo.restOfTheLines).Substring(0, 100) + "...";
            pageModels.Add(new PostListModel
            {
                Categories = pageInfo.categories, Title = pageInfo.title,
                Slug = pageInfo.slug, WordCount = WordCount(pageInfo.restOfTheLines),
                PublishedDate = pageInfo.publishDate, Summary = summary
            });
        }

        pageModels = pageModels.OrderByDescending(x => x.PublishedDate).ToList();
        return pageModels;
    }


    public List<PostListModel> GetPostsForFiles()
    {
        var pages = Directory.GetFiles("Markdown", "*.md");
        return GetPosts(pages);
    }
}
```

</details>
Como se puede ver esto tiene algunos elementos:

### Procesamiento de archivos

El código para procesar los archivos markdown a HTML es bastante simple, utilizo la biblioteca Markdig para convertir el markdown a HTML y luego utilizo algunas expresiones regulares para extraer las categorías y la fecha publicada del archivo markdown.

El método GetPage se utiliza para extraer el contenido del archivo Markdown, tiene algunos pasos:

1. Extraer el título
   Por convención uso la primera línea del archivo Markdown como título del post. Así que simplemente puedo hacer:

```csharp
        var lines = File.ReadAllLines(page);

        // Get the title from the first line
        var title = lines.Length > 0 ? Markdown.ToPlainText(lines[0].Trim()) : string.Empty;
```

Como el título está prefijado con "#" utilizo el método Markdown.ToPlainText para quitar el "#" del título.

2. Extraer las categorías
   Cada post puede tener hasta dos categorías de este método extrae estos y luego removo esa etiqueta del archivo Markdown.

```csharp
// Concatenate the rest of the lines with newline characters
        var restOfTheLines = string.Join(Environment.NewLine, lines.Skip(1));

        // Extract categories from the text
        var categories = GetCategories(restOfTheLines);

   // Remove category tags from the text
        restOfTheLines = CategoryRegex.Replace(restOfTheLines, "");

```

El método GetCategories utiliza una expresión regular para extraer las categorías del archivo Markdown.

```csharp
    private static readonly Regex CategoryRegex = new(@"<!--\s*category\s*--\s*([^,]+?)\s*(?:,\s*([^,]+?)\s*)?-->",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static string[] GetCategories(string markdownText)
    {
        var matches = CategoryRegex.Matches(markdownText);
        var categories = matches
            .SelectMany(match => match.Groups.Cast<Group>()
                .Skip(1) // Skip the entire match group
                .Where(group => group.Success) // Ensure the group matched
                .Select(group => group.Value.Trim()))
            .ToArray();
        return categories;
        
        
    }
```

3. Extraer la fecha de publicación
   Entonces extraigo la fecha del post (yo estaba usando la fecha creada, pero cómo deploro esto usando una imagen docker completa significa que esto ya no es realmente útil) así que no estoy usando una expresión regular.
   Esto analiza una etiqueta en el formulario que está en cada archivo.md.

```razor
 <datetime class="hidden">2024-08-02T17:00</datetime>
```

```csharp
     private static readonly Regex DateRegex = new(
        @"<datetime class=""hidden"">(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})</datetime>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
     
           var publishedDate = fileInfo.CreationTime;
        var publishDate = DateRegex.Match(restOfTheLines).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(publishDate))
            publishedDate = DateTime.ParseExact(publishDate, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

     
        restOfTheLines = DateRegex.Replace(restOfTheLines, "");
```

4. Extraer el contenido
   En realidad obtener el contenido es bastante simple esto utiliza una tubería (para el reemplazo de la etiqueta de imagen mencionado anteriormente) entonces opcionalmente me da texto plano para la lista de publicaciones o HTML para el mensaje real.

```csharp
    pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Use<ImgExtension>().Build();
    
   var processed =
            html ? Markdown.ToHtml(restOfTheLines, pipeline) : Markdown.ToPlainText(restOfTheLines, pipeline);
```

5. Obtener la 'babosa'
   Esto es simplemente el nombre del archivo sin la extensión:
   
   ```csharp
       private string GetSlug(string fileName)
       {
           var slug = System.IO.Path.GetFileNameWithoutExtension(fileName);
           return slug.ToLowerInvariant();
       }
   ```

6. Devuelve el contenido
   Ahora tenemos contenido de página que podemos mostrar para el blog!

<details>
<summary> The GetPage Method</summary>
```csharp
public (string title, string slug, DateTime publishDate, string processed, string[] categories, string
        restOfTheLines) GetPage(string page, bool html)
    {
        var fileInfo = new FileInfo(page);

        // Ensure the file exists
        if (!fileInfo.Exists) throw new FileNotFoundException("The specified file does not exist.", page);

        // Read all lines from the file
        var lines = File.ReadAllLines(page);

        // Get the title from the first line
        var title = lines.Length > 0 ? Markdown.ToPlainText(lines[0].Trim()) : string.Empty;

        // Concatenate the rest of the lines with newline characters
        var restOfTheLines = string.Join(Environment.NewLine, lines.Skip(1));

        // Extract categories from the text
        var categories = GetCategories(restOfTheLines);

        var publishedDate = fileInfo.CreationTime;
        var publishDate = DateRegex.Match(restOfTheLines).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(publishDate))
            publishedDate = DateTime.ParseExact(publishDate, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

        // Remove category tags from the text
        restOfTheLines = CategoryRegex.Replace(restOfTheLines, "");
        restOfTheLines = DateRegex.Replace(restOfTheLines, "");
        // Process the rest of the lines as either HTML or plain text
        var processed =
            html ? Markdown.ToHtml(restOfTheLines, pipeline) : Markdown.ToPlainText(restOfTheLines, pipeline);

        // Generate the slug from the page filename
        var slug = GetSlug(page);


        // Return the parsed and processed content
        return (title, slug, publishedDate, processed, categories, restOfTheLines);
    }
```

</details>
El siguiente código muestra cómo puedo generar la lista de entradas de blog, que utiliza el `GetPage(page, false)` método para extraer el título, las categorías, la fecha de publicación y el contenido procesado.

```csharp
     public List<PostListModel> GetPosts(string[] pages)
    {
        List<PostListModel> pageModels = new();

        foreach (var page in pages)
        {
            var pageInfo = GetPage(page, false);

            var summary = Markdown.ToPlainText(pageInfo.restOfTheLines).Substring(0, 100) + "...";
            pageModels.Add(new PostListModel
            {
                Categories = pageInfo.categories, Title = pageInfo.title,
                Slug = pageInfo.slug, WordCount = WordCount(pageInfo.restOfTheLines),
                PublishedDate = pageInfo.publishDate, Summary = summary
            });
        }

        pageModels = pageModels.OrderByDescending(x => x.PublishedDate).ToList();
        return pageModels;
    }


    public List<PostListModel> GetPostsForFiles()
    {
        var pages = Directory.GetFiles("Markdown", "*.md");
        return GetPosts(pages);
    }
```