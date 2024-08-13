# Використання розмітки для ведення блогів

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024- 08- 02T17: 00</datetime>

## Вступ

Позначка - це легка мова розмітки, яку ви можете використовувати для додавання елементів форматування до текстових документів. У 2004 році його створив Джон Ґюбер, і Маркдаут тепер є однією з найпопулярніших мов розмітки.

На цьому сайті я використовую дуже простий підхід до блогу, спробувавши і не зміг зберегти блог в минулому, я хотів зробити його якомога простішим для написання та оприлюднення дописів. Я використовую позначку, щоб написати свої дописи, і цей сайт використовує одну службу. [Markdig](https://github.com/xoofx/markdig) перетворення позначки у HTML.

[TOC]

## Чому б не створити Статичний веб - генератор?

Просто кажучи. Це не буде супер-високий сайт, я використовую ASP.NET OutCache для кешування сторінок і я не збираюся так часто оновлювати їх. Я хотів, щоб сайт був якомога простішим і не хвилювався про накриття статичного генератора сайту як щодо процесу збирання, так і про складність сайту.

Для прояснення; генератори статичних сайтів на зразок [ХьюгоCity in Gyeongbuk Korea](https://gohugo.io/) / [Jekill](https://jekyllrb.com/) тощо... може бути добрим розв'язком для багатьох сайтів, але для цього я хотів зробити його простим *для мене* наскільки це можливо. Я 25-річний ветеран ASPNET, так що розумію це всередині і зовні. Цей дизайн сайта додає складність; у мене є перегляди, служби, контролери і LOT ручного HTML- CSS, але мені це зручно.

## Чому не база даних?

1. Простота дизайну; бази даних - це потужні системи для збереження даних (і я додам їх для коментарів незабаром), як би вони не додавали складності. До *правильно* використовувати бази даних, особливо у програмі ASP.NET, ви додаєте LOT коду, байдуже, чи ви використовуєте [Корінь EF](https://learn.microsoft.com/en-us/ef/core/), [Dapper](https://github.com/DapperLib/Dapper) або сирий SQL з ADO.NET. Я хотів зробити сайт якомога простішим *почати з*.
2. Простость поновления и распространения. Цей сайт призначено для того, щоб продемонструвати, наскільки простим може бути композа Докера і Докера для запуску сайта. Я можу оновити сайт, перевіряючи новий код (включаючи зміст) на GitHub, Дія виконується, будуючи зображення, тоді метод створення " Вартової башти " у моєму файлі Docker автоматично оновлює зображення сайта. Це дуже простий спосіб оновлення сайту, і я хотів зберегти його таким чином.
3. Виконання дублікатів; оскільки у мене є дані ZERO, які не знаходяться у межах зображення об' єму, це означає, що я можу ASI running точних дублікатів локально (на моєму маленькому скупченні Ubuntu тут, у себе вдома). Це чудовий спосіб перевірки змін у Docker (наприклад, [коли я вніс зміни до розміру ImageSharp](/blog/imagesharpwithdocker) ) перед тим, як подати їх на місце проживання.
4. Потому что я не хотела! Я хотів побачити, як далеко я можу зайти з простим дизайном сайту і до цього часу я цілком задоволений цим.

## Як ви пишете свої повідомлення?

Я просто скинув новий файл.md до теки Markdown і сайт знімав його і передавав його (коли я пам' ятаю, щоб поставити його як вміст, це забезпечує його доступність у виведених файлах! )

Потім, коли я перевіряю, що відбувається на сайті GitHub, і сайт оновлюється. Просто!

```mermaid
flowchart LR
    A[Write New Markdown File] -->|Checkin To Github| B(Github Action Triggers) -->  C(Builds Docker Image) --> D(Watchtower Pulls new Image) --> E(Site Updated)
   
  
```

![setascontent.png](setascontent.png)

## Як додати зображення?

Оскільки я щойно додав зображення тут, я покажу вам, як я це зробив. Я просто додав зображення до теки wwwroot/articleimages і посилається на неї у файлі markdown таким чином:

```markdown
![setascontent.png](setascontent.png)
```

Потім я додаю розширення до мого трубопроводу Markdig, який переписує ці дані до правильної адреси URL (все про простоту). [Тут ви можете знайти початкові коди суфікса.](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/MarkDigExtensions/ImgExtension.cs)

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

## BlogService.

Служба BlogService є простою службою, яка читає файли з позначки з теки Markdown і перетворює їх на HTML за допомогою Markdig.

Повне джерело для цього знаходиться нижче і [тут](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/BlogService.cs).

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
Як бачите, це має декілька елементів:

### Обробка файлів

Код для обробки файлів postdown у HTML досить простий, я використовую бібліотеку Markdig для перетворення markdown у HTML, а потім я використовую декілька формальних виразів для видобування категорій і друкованої дати з файла markdown.

Метод GetPage використовується для видобування вмісту файла markdown, у ньому є декілька кроків:

1. Видобути заголовок
   Під час конгресу я використовую перший рядок файла postdown як заголовок повідомлення. Так що я можу просто зробити:

```csharp
        var lines = File.ReadAllLines(page);

        // Get the title from the first line
        var title = lines.Length > 0 ? Markdown.ToPlainText(lines[0].Trim()) : string.Empty;
```

Оскільки назва починається з " #," автор використовує метод Markdown. toPlainText для вилучення рядка " # " з заголовка.

2. Видобути категорії
   Кожен допис може мати до двох категорій. Цей метод витягує їх, а потім я видаляю мітку з файла з позначкою.

```csharp
// Concatenate the rest of the lines with newline characters
        var restOfTheLines = string.Join(Environment.NewLine, lines.Skip(1));

        // Extract categories from the text
        var categories = GetCategories(restOfTheLines);

   // Remove category tags from the text
        restOfTheLines = CategoryRegex.Replace(restOfTheLines, "");

```

Метод GetCategories використовує формальний вираз для видобування категорій з файла markdown.

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

3. Видобути надруковану дату
   Після цього я витягаю дату з допису (Я користувалася створеною датою, але спосіб, у який я впроваджую це зображення, означає, що це більше не є корисним), тому я не користуюся формальним виразом.
   За допомогою цього пункту можна проаналізувати теґ у формі, який зберігається у кожному з файлів. md.

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

4. Видобути вміст
   Насправді, отримання вмісту досить просто використовує трубопровод (для заміни теґу зображення, згаданого вище), а потім за бажання надає авторові звичайний текст для списку дописів або HTML для поточного допису.

```csharp
    pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Use<ImgExtension>().Build();
    
   var processed =
            html ? Markdown.ToHtml(restOfTheLines, pipeline) : Markdown.ToPlainText(restOfTheLines, pipeline);
```

5. Візьми "Slug"
   Це просто назва файла без суфікса:
   
   ```csharp
       private string GetSlug(string fileName)
       {
           var slug = System.IO.Path.GetFileNameWithoutExtension(fileName);
           return slug.ToLowerInvariant();
       }
   ```

6. Повернути вміст
   Тепер у нас є вміст сторінки, яку ми можемо показувати у блозі!

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
Код, наведений нижче, показує як я створюю список дописів блогу, він використовує `GetPage(page, false)` метод видобування заголовка, категорій, оприлюдненої дати і обробленого вмісту.

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