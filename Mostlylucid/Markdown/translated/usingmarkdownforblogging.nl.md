# Markdown gebruiken voor bloggen

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-02T17:00</datetime>

## Inleiding

Markdown is een lichtgewicht mark-up taal die u kunt gebruiken om opmaakelementen toe te voegen aan tekstdocumenten in platte tekst. Gemaakt door John Gruber in 2004, Markdown is nu een van de meest populaire mark-up talen ter wereld.

Op deze site gebruik ik een super eenvoudige benadering van bloggen, hebben geprobeerd en mislukt om een blog in het verleden te onderhouden Ik wilde het zo gemakkelijk mogelijk te maken om berichten te schrijven en te publiceren. Ik gebruik markdown om mijn berichten te schrijven en deze site heeft een enkele dienst met behulp van[Markdig](https://github.com/xoofx/markdig)om de markdown naar HTML te converteren.

[TOC]

## Waarom geen Static Site generator?

In een woord eenvoud. Dit is niet van plan om een super high traffic site te zijn, Ik gebruik ASP.NET OutPutCache om de pagina's te cachen en ik ben niet van plan om het zo vaak te updaten. Ik wilde de site zo eenvoudig mogelijk te houden en geen zorgen te maken over de overhead van een statische site generator zowel in termen van het bouwproces en de complexiteit van de site.

Om te verduidelijken; statische site generatoren zoals[Hugo](https://gohugo.io/) / [Jekyll](https://jekyllrb.com/)etc...kan een goede oplossing zijn voor veel sites, maar voor deze wilde ik het zo simpel houden*voor mij*Ik ben een 25 jaar ASP.NET veteraan dus begrijp het van binnen en buiten. Dit site ontwerp voegt complexiteit; Ik heb uitzicht, diensten, controllers en een heleboel handmatige HTML & CSS, maar ik ben comfortabel met dat.

## Waarom geen database?

1. Eenvoud van design; Databanken zijn krachtige systemen voor het opslaan van gegevens (en ik voeg er binnenkort een toe voor commentaar), maar ze voegen ook complexiteit toe.*correct*gebruik databases vooral in een ASP.NET applicatie u een LOT van code toe te voegen, ongeacht of u gebruikt[EF-kern](https://learn.microsoft.com/en-us/ef/core/), [Dapper](https://github.com/DapperLib/Dapper)of rauwe SQL met ADO.NET. Ik wilde de site zo eenvoudig mogelijk houden*om te beginnen met*.
2. Gemakkelijk updaten en implementeren. Deze site is bedoeld om aan te tonen hoe eenvoudig Docker & Docker Compose kan zijn om een site te draaien. Ik kan de site bijwerken door het controleren van nieuwe code (inclusief inhoud) naar GitHub, de Actie draait, bouwt de afbeelding dan de Watchtower methode in mijn docker componeert bestand updates van de site afbeelding automatisch. Dit is een zeer eenvoudige manier om een site te updaten en ik wilde het zo houden.
3. Het uitvoeren van duplicaten; omdat ik ZERO-gegevens heb die niet in de docker-afbeelding zitten, betekent dit dat ik gemakkelijk exacte duplicaten lokaal kan uitvoeren (op mijn kleine Ubuntu-cluster hier thuis). Dit is een geweldige manier om veranderingen te testen met docker (bijv.,[toen ik de ImageSharp wijzigingen maakte](/blog/imagesharpwithdocker)) voordat ze worden ingezet op de live-site.
4. Omdat ik niet wilde! Ik wilde zien hoe ver ik kon komen met een eenvoudige site ontwerp en tot nu toe ben ik vrij blij met het.

## Hoe schrijf je je berichten?

Ik zet gewoon een nieuw.md bestand in de map Markdown en de site picks het op en rendert het (als ik herinner om het aet als inhoud, dit zorgt ervoor dat het beschikbaar is in de uitvoerbestanden!)

Dan wanneer ik checkin de site om GitHub de actie loopt en de site wordt bijgewerkt. Eenvoudig!

```mermaid
flowchart LR
    A[Write New Markdown File] -->|Checkin To Github| B(Github Action Triggers) -->  C(Builds Docker Image) --> D(Watchtower Pulls new Image) --> E(Site Updated)
   
  
```

![setascontent.png](setascontent.png)

## Hoe voeg je afbeeldingen toe?

Aangezien ik net de afbeelding hier heb toegevoegd, zal ik je laten zien hoe ik het gedaan heb. Ik heb gewoon de afbeelding toegevoegd aan de map wwwroot/articleimages en ernaar verwezen in het markdown bestand zoals dit:

```markdown
![setascontent.png](setascontent.png)
```

Ik voeg dan een uitbreiding toe aan mijn Markdig pipeline die deze herschrijft naar de juiste URL (alles over eenvoud).[Zie hier voor de broncode voor de extensie.](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/MarkDigExtensions/ImgExtension.cs)

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

## De BlogService.

De BlogService is een eenvoudige dienst die de markdown bestanden van de Markdown map leest en ze converteert naar HTML met behulp van Markdig.

De volledige bron hiervoor is hieronder en[Hier.](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/BlogService.cs).

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
Zoals je kunt zien heeft dit een paar elementen:

### Bestanden verwerken

De code om de markdown-bestanden naar HTML te verwerken is vrij eenvoudig, ik gebruik de Markdig-bibliotheek om de markdown naar HTML te converteren en dan gebruik ik een paar reguliere expressies om de categorieën en de gepubliceerde datum uit het markdown-bestand te halen.

De GetPage methode wordt gebruikt om de inhoud van het markdown bestand te extraheren, het heeft een paar stappen:

1. De titel uitpakken
   Door conventie gebruik ik de eerste regel van het markdown bestand als de titel van de post. Dus ik kan gewoon doen:

```csharp
        var lines = File.ReadAllLines(page);

        // Get the title from the first line
        var title = lines.Length > 0 ? Markdown.ToPlainText(lines[0].Trim()) : string.Empty;
```

Omdat de titel is geprefixeerd met "#" gebruik ik de Markdown.ToPlainText methode om de "#" van de titel te verwijderen.

2. Uitpakken van de categorieën
   Elke post kan hebben tot twee categorieën deze methode haalt deze dan verwijder ik die tag uit het markdown bestand.

```csharp
// Concatenate the rest of the lines with newline characters
        var restOfTheLines = string.Join(Environment.NewLine, lines.Skip(1));

        // Extract categories from the text
        var categories = GetCategories(restOfTheLines);

   // Remove category tags from the text
        restOfTheLines = CategoryRegex.Replace(restOfTheLines, "");

```

De methode GetCategories gebruikt een reguliere expressie om de categorieën uit het markdown-bestand te halen.

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

3. Uitpakken van de gepubliceerde datum
   Ik haal dan de datum uit de post (ik WAS met behulp van de aangemaakte datum, maar hoe ik dit implementeren met behulp van een hele docker image betekent dat dit is niet echt nuttig meer) dus ik ben niet met behulp van een reguliere expressie.
   Dit ontleedt een tag in de vorm die in elk.md bestand staat.

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

4. De inhoud uitpakken
   Eigenlijk is het krijgen van de inhoud is vrij eenvoudig dit maakt gebruik van een pipeline (voor de afbeelding tag vervanging hierboven vermeld) dan optioneel geeft me platte tekst voor de lijst van berichten of HTML voor de werkelijke post.

```csharp
    pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Use<ImgExtension>().Build();
    
   var processed =
            html ? Markdown.ToHtml(restOfTheLines, pipeline) : Markdown.ToPlainText(restOfTheLines, pipeline);
```

5. Haal de'slug'
   Dit is gewoon de bestandsnaam zonder de extensie:
   
   ```csharp
       private string GetSlug(string fileName)
       {
           var slug = System.IO.Path.GetFileNameWithoutExtension(fileName);
           return slug.ToLowerInvariant();
       }
   ```

6. Teruggeven van de inhoud
   Nu hebben we pagina-inhoud die we kunnen weergeven voor de blog!

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
De code hieronder laat zien hoe ik de lijst van blog berichten genereren, het maakt gebruik van de`GetPage(page, false)`methode om de titel, categorieën, gepubliceerde datum en de verwerkte inhoud te extraheren.

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