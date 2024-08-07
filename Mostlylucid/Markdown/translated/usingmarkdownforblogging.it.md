# Utilizzando Markdown per Blogging

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-02T17:00</datetime>

## Introduzione

Markdown è un linguaggio di markup leggero che è possibile utilizzare per aggiungere elementi di formattazione ai documenti di testo in chiaro. Creato da John Gruber nel 2004, Markdown è ora uno dei linguaggi di markup più popolari del mondo.

Su questo sito uso un approccio super semplice al blogging, avendo provato e non riuscito a mantenere un blog in passato ho voluto rendere il più facile possibile per scrivere e pubblicare i post. Uso markdown per scrivere i miei post e questo sito ha un unico servizio utilizzando[MarkdigCity name (optional, probably does not need a translation)](https://github.com/xoofx/markdig)per convertire il markdown in HTML.

[TOC]

## Perché non un generatore statico del sito?

In una parola semplicità. Questo non sarà un sito super alto traffico, io uso ASP.NET OutPutCache per nascondere le pagine e non ho intenzione di aggiornarlo che spesso. Ho voluto mantenere il sito il più semplice possibile e non devono preoccuparsi per la testata di un generatore di sito statico sia in termini di processo di costruzione e la complessità del sito.

Per chiarire; generatori di sito statici come[HugoCity name (optional, probably does not need a translation)](https://gohugo.io/) / [JekyllCity name (optional, probably does not need a translation)](https://jekyllrb.com/)ecc... può essere una buona soluzione per molti siti, ma per questo ho voluto mantenere come semplice*per me*Come possibile. Sono un veterano ASP.NET 25 anni in modo da capire dentro e fuori. Questo design del sito aggiunge complessità; Ho punti di vista, servizi, controller e un sacco di HTML manuale & CSS, ma sono a mio agio con questo.

## Perche' non un database?

1. Semplicità di progettazione; I database sono sistemi potenti per la memorizzazione dei dati (e ne aggiungo uno per i commenti a breve) ma aggiungono anche complessità.*correttamente*utilizzare i database soprattutto in un'applicazione ASP.NET si aggiunge un sacco di codice, non importa se si sta utilizzando[Centrale EF](https://learn.microsoft.com/en-us/ef/core/), [DapperCity name (optional, probably does not need a translation)](https://github.com/DapperLib/Dapper)o SQL grezzo con ADO.NET. Volevo mantenere il sito il più semplice possibile*per iniziare con*.
2. Facilità di aggiornamento e distribuzione. Questo sito ha lo scopo di dimostrare come Docker & Docker Compose può essere semplice per eseguire un sito. Posso aggiornare il sito controllando il nuovo codice (compreso il contenuto) a GitHub, l'azione funziona, costruisce l'immagine poi il metodo Watchtower nel mio docker comporre l'immagine del sito automaticamente. Questo è un modo molto semplice per aggiornare un sito e volevo mantenerlo in questo modo.
3. Eseguire duplicati; poiché ho dati ZERO che non sono all'interno dell'immagine docker significa che posso eseguire esattamente duplicati localmente (sul mio piccolo cluster Ubuntu qui a casa). Questo è un ottimo modo per testare i cambiamenti con docker (ad esempio,[quando ho fatto le modifiche ImageSharp](/blog/imagesharpwithdocker)== Altri progetti ==== Collegamenti esterni ==
4. Perché non volevo! Volevo vedere fino a che punto potevo arrivare con un semplice design del sito e finora sono abbastanza felice con esso.

## Come scrivi i tuoi post?

Ho semplicemente rilasciare un nuovo file.md nella cartella Markdown e il sito lo raccoglie e lo rende (quando mi ricordo di aet come contenuto, questo assicura che è disponibile nei file di output!)

Poi quando controllo il sito a GitHub l'azione funziona e il sito viene aggiornato. Semplice!

```mermaid
flowchart LR
    A[Write New Markdown File] -->|Checkin To Github| B(Github Action Triggers) -->  C(Builds Docker Image) --> D(Watchtower Pulls new Image) --> E(Site Updated)
   
  
```

![setascontent.png](setascontent.png)

## Come si aggiungono le immagini?

Dal momento che ho appena aggiunto l'immagine qui, vi mostrerò come ho fatto. Ho semplicemente aggiunto l'immagine alla cartella wwwroot/articleimages e l'ho fatto riferimento nel file markdown in questo modo:

```markdown
![setascontent.png](setascontent.png)
```

Poi aggiungo un'estensione alla mia pipeline di Markdig che le riscrive all'URL corretto (tutto sulla semplicità).[Vedere qui per il codice sorgente per l'estensione.](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/MarkDigExtensions/ImgExtension.cs)

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

## Il BlogService.

Il BlogService è un servizio semplice che legge i file markdown dalla cartella Markdown e li converte in HTML utilizzando Markdig.

La fonte completa per questo è di seguito e[qui](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/BlogService.cs).

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
Come potete vedere questo ha alcuni elementi:

### Elaborazione dei file

Il codice per elaborare i file markdown in HTML è abbastanza semplice, uso la libreria Markdig per convertire il markdown in HTML e poi uso alcune espressioni regolari per estrarre le categorie e la data pubblicata dal file markdown.

Il metodo GetPage viene utilizzato per estrarre il contenuto del file markdown, ha alcuni passaggi:

1. Estrai il titolo
   Per convenzione uso la prima riga del file markdown come titolo del post. Quindi posso semplicemente fare:

```csharp
        var lines = File.ReadAllLines(page);

        // Get the title from the first line
        var title = lines.Length > 0 ? Markdown.ToPlainText(lines[0].Trim()) : string.Empty;
```

Poiché il titolo è prefisso con "#" uso il metodo Markdown.ToPlainText per togliere il "#" dal titolo.

2. Estrai le categorie
   Ogni post può avere fino a due categorie questo metodo estrae questi poi rimuovo quel tag dal file markdown.

```csharp
// Concatenate the rest of the lines with newline characters
        var restOfTheLines = string.Join(Environment.NewLine, lines.Skip(1));

        // Extract categories from the text
        var categories = GetCategories(restOfTheLines);

   // Remove category tags from the text
        restOfTheLines = CategoryRegex.Replace(restOfTheLines, "");

```

Il metodo GetCategorie utilizza un'espressione regolare per estrarre le categorie dal file markdown.

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

3. Estrai la data di pubblicazione
   Quindi estraggo la data dal post (Ero usando la data creata, ma come implemento questo usando un'intera immagine docker significa che questo non è più davvero utile) quindi non sto usando un'espressione regolare.
   Questo analizza un tag nella forma che è in ogni file.md.

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

4. Estrae il contenuto
   In realtà ottenere il contenuto è abbastanza semplice questo utilizza una pipeline (per la sostituzione tag immagine di cui sopra) quindi opzionalmente mi dà testo semplice per la lista di messaggi o HTML per il post reale.

```csharp
    pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Use<ImgExtension>().Build();
    
   var processed =
            html ? Markdown.ToHtml(restOfTheLines, pipeline) : Markdown.ToPlainText(restOfTheLines, pipeline);
```

5. Ottieni la'slug'
   Questo è semplicemente il nome del file senza l'estensione:
   
   ```csharp
       private string GetSlug(string fileName)
       {
           var slug = System.IO.Path.GetFileNameWithoutExtension(fileName);
           return slug.ToLowerInvariant();
       }
   ```

6. Restituisci il contenuto
   Ora abbiamo contenuti di pagina che possiamo visualizzare per il blog!

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
Il codice qui sotto mostra come generare l'elenco dei post del blog, si utilizza il`GetPage(page, false)`metodo per estrarre il titolo, le categorie, la data di pubblicazione e il contenuto trattato.

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