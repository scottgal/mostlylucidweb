# Markdown für Blogging verwenden

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-02T17:00</datetime>

## Einleitung

Markdown ist eine leichte Markup-Sprache, die Sie verwenden können, um Formatierungselemente zu Klartexttextdokumenten hinzuzufügen. Erstellt von John Gruber im Jahr 2004, ist Markdown jetzt eine der weltweit beliebtesten Markup-Sprachen.

Auf dieser Website benutze ich einen super einfachen Ansatz, um Blogging, versucht und versäumt, einen Blog in der Vergangenheit zu pflegen Ich wollte es so einfach wie möglich zu schreiben und veröffentlichen Beiträge. Ich verwende Markdown, um meine Beiträge zu schreiben und diese Website hat einen einzigen Service mit[Markdig](https://github.com/xoofx/markdig)zum Konvertieren des Markdowns in HTML.

[TOC]

## Warum nicht ein Static Site Generator?

In einem Wort Einfachheit. Dies wird nicht eine super hohe Traffic-Website sein, Ich benutze ASP.NET OutPutCache, um die Seiten zu verbergen und ich werde nicht zu aktualisieren, dass oft. Ich wollte die Website so einfach wie möglich zu halten und nicht über den Overhead eines statischen Website-Generator sowohl in Bezug auf den Build-Prozess und die Komplexität der Website kümmern.

Zur Klärung; statische Standortgeneratoren wie[Hugo](https://gohugo.io/) / [Jekyll](https://jekyllrb.com/)etc...kann eine gute Lösung für viele Websites sein, aber für diese wollte ich es so einfach halten*für mich*Ich bin ein 25-jähriger ASP.NET Veteran, also verstehen Sie es innerhalb und außerhalb. Diese Website-Design fügt Komplexität; Ich habe Ansichten, Dienste, Controller und eine große Menge von manuellen HTML & CSS, aber ich fühle mich damit wohl.

## Warum nicht eine Datenbank?

1. Simplicty of design; Datenbanken sind leistungsfähige Systeme zum Speichern von Daten (und ich werde eine für Kommentare in Kürze hinzufügen), aber sie fügen auch Komplexität hinzu.*korrekt*Datenbanken vor allem in einer ASP.NET Anwendung verwenden, fügen Sie eine LOT Code, egal ob Sie verwenden[EF-Kern](https://learn.microsoft.com/en-us/ef/core/), [Dapper](https://github.com/DapperLib/Dapper)oder roh SQL mit ADO.NET. Ich wollte die Website so einfach wie möglich halten*zu Beginn mit*.
2. Diese Website soll zeigen, wie einfach Docker & Docker Compose sein kann, um eine Website zu betreiben. Ich kann die Website aktualisieren, indem ich neuen Code (einschließlich Inhalt) auf GitHub einchecke, die Aktion läuft, baut das Bild dann die Watchtower-Methode in meinem Docker komponiere Datei aktualisiert das Site-Image automatisch. Dies ist eine sehr einfache Weise, eine Website zu aktualisieren, und ich wollte es so halten.
3. Duplikate ausführen; da ich ZERO-Daten habe, die nicht im Docker-Image enthalten sind, bedeutet das, dass ich EASILY genaue Duplikate lokal ausführen kann (auf meinem kleinen Ubuntu-Cluster hier zu Hause).[wenn ich die ImageSharp-Änderungen vorgenommen habe](/blog/imagesharpwithdocker)) vor dem Einsatz auf der Live-Site.
4. Weil ich nicht wollte! Ich wollte sehen, wie weit ich mit einem einfachen Website-Design zu bekommen und so weit bin ich ziemlich glücklich mit ihm.

## Wie schreiben Sie Ihre Beiträge?

Ich lege einfach eine neue.md-Datei in den Markdown-Ordner und die Website nimmt sie auf und rendert sie (wenn ich mich daran erinnere, sie als Inhalt zu verwenden, stellt dies sicher, dass sie in den Ausgabedateien verfügbar ist! )

Wenn ich dann die Seite zu GitHub einchecke, läuft die Aktion und die Seite wird aktualisiert. Einfach!

```mermaid
flowchart LR
    A[Write New Markdown File] -->|Checkin To Github| B(Github Action Triggers) -->  C(Builds Docker Image) --> D(Watchtower Pulls new Image) --> E(Site Updated)
   
  
```

![setascontent.png](setascontent.png)

## Wie fügen Sie Bilder hinzu?

Da ich gerade das Bild hier hinzugefügt habe, werde ich Ihnen zeigen, wie ich es gemacht habe. Ich habe das Bild einfach in den Ordner wwwroot/articleimages eingefügt und in der Markdown-Datei wie folgt referenziert:

```markdown
![setascontent.png](setascontent.png)
```

Ich füge dann eine Erweiterung zu meiner Markdig Pipeline hinzu, die diese auf die korrekte URL umschreibt (alles über Einfachheit).[Siehe hier für den Quellcode für die Erweiterung.](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/MarkDigExtensions/ImgExtension.cs)

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

## Der BlogService.

Der BlogService ist ein einfacher Dienst, der die Markdown-Dateien aus dem Markdown-Ordner liest und mit Markdig in HTML konvertiert.

Die vollständige Quelle dafür ist unten und[Hierher](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/BlogService.cs).

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
Wie Sie sehen können, hat dies ein paar Elemente:

### Verarbeitung von Dateien

Der Code, um die Markdown-Dateien in HTML zu verarbeiten, ist ziemlich einfach, ich benutze die Markdig-Bibliothek, um den Markdown in HTML zu konvertieren und dann benutze ich ein paar reguläre Ausdrücke, um die Kategorien und das veröffentlichte Datum aus der Markdown-Datei zu extrahieren.

Die GetPage-Methode wird verwendet, um den Inhalt der Markdown-Datei zu extrahieren, es hat ein paar Schritte:

1. Den Titel extrahieren
   Durch Konvention verwende ich die erste Zeile der Markdown-Datei als Titel des Posts. So kann ich einfach tun:

```csharp
        var lines = File.ReadAllLines(page);

        // Get the title from the first line
        var title = lines.Length > 0 ? Markdown.ToPlainText(lines[0].Trim()) : string.Empty;
```

Da der Titel mit "#" voreingestellt ist, benutze ich die Markdown.ToPlainText Methode, um das "#" vom Titel zu entfernen.

2. Extrahieren Sie die Kategorien
   Jeder Beitrag kann bis zu zwei Kategorien haben, die diese Methode extrahiert, dann entferne ich dieses Tag aus der Markdown-Datei.

```csharp
// Concatenate the rest of the lines with newline characters
        var restOfTheLines = string.Join(Environment.NewLine, lines.Skip(1));

        // Extract categories from the text
        var categories = GetCategories(restOfTheLines);

   // Remove category tags from the text
        restOfTheLines = CategoryRegex.Replace(restOfTheLines, "");

```

Die GetCategories-Methode verwendet einen regulären Ausdruck, um die Kategorien aus der Markdown-Datei zu extrahieren.

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

3. Auszug des veröffentlichten Datums
   Ich extrahiere dann das Datum aus dem Post (ich war mit dem erstellten Datum, aber wie ich dies mit einem ganzen Docker-Image zu implementieren bedeutet, dass dies nicht mehr wirklich nützlich ist), so dass ich nicht mit einem regulären Ausdruck.
   Dieses parsiert ein Tag in der Form, die in jeder.md-Datei ist.

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

4. Inhalt extrahieren
   Eigentlich ist das Erhalten des Inhalts ziemlich einfach, dies verwendet eine Pipeline (für den oben genannten Bildtag-Ersatz) dann gibt mir optional Klartext für die Liste der Beiträge oder HTML für den tatsächlichen Beitrag.

```csharp
    pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Use<ImgExtension>().Build();
    
   var processed =
            html ? Markdown.ToHtml(restOfTheLines, pipeline) : Markdown.ToPlainText(restOfTheLines, pipeline);
```

5. Holen Sie den 'Schlupf'
   Dies ist einfach der Dateiname ohne die Erweiterung:
   
   ```csharp
       private string GetSlug(string fileName)
       {
           var slug = System.IO.Path.GetFileNameWithoutExtension(fileName);
           return slug.ToLowerInvariant();
       }
   ```

6. Inhalt zurückgeben
   Jetzt haben wir Seiteninhalte, die wir für den Blog anzeigen können!

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
Der Code unten zeigt, wie ich die Liste der Blog-Posts zu generieren, es verwendet die`GetPage(page, false)`Methode zum Extrahieren des Titels, der Kategorien, des veröffentlichten Datums und des verarbeiteten Inhalts.

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