# बिंग के लिए मार्क नीचे का उपयोग कर रहा है

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024- 08- 02टी17: 00</datetime>

## परिचय

सन्‌ 2004 में जॉन गस्टरर द्वारा बनाया गया था, जो अब दुनिया की सबसे लोकप्रिय मार्कअप भाषाओं में से एक है ।

इस साइट पर मैं ब्लॉगिंग के लिए एक सुपर सरल समीप का उपयोग करके पिछले में एक ब्लॉग को सरल बनाए रखने में असफल रहा जिन्हें मैं लिखना और पोस्ट को प्रकाशित करना चाहता था. मैं अपने पोस्ट तथा इस साइट में एक ही सेवा का उपयोग करता हूँ[सिगिग](https://github.com/xoofx/markdig)चिह्न नीचे को एचटीएमएल में परिवर्तित करने के लिए.

[विषय

## क्यों न स्थिर साइट जेनरेटर?

एक शब्द सरल में. यह एक सुपर ऊँची यातायात स्थल होने के लिए नहीं जा रहा है, मैं Amack का उपयोग पृष्ठों को कैश करने के लिए करें और मैं इसे अक्सर को रोकने के लिए नहीं जा रहा हूँ. मैं इस साइट को सरल के रूप में सरल के रूप में बनाए रखना चाहता था और एक स्थिर साइट के बारे में चिंता नहीं करना चाहता था के बारे में दोनों में एक स्थिर स्थल और प्रक्रिया की प्रक्रिया की प्रक्रिया।

स्पष्ट करने के लिए; स्थिर साइट जेनरेटर जैसे[हूगोworld. kgm](https://gohugo.io/) / [जेकील](https://jekyllrb.com/)... कई साइटों के लिए एक अच्छा समाधान हो सकता है लेकिन इस एक के लिए मैं इसे सरल के रूप में रखना चाहता था*मेरे लिए*मैं 25 साल का हूँ। हम इसे अंदर और बाहर समझ सकते हैं। इस साइट डिजाइन में जटिलता है; मैं सोचता हूँ, सेवाओं, नियंत्रण, नियंत्रण, और HTML सीएसएस के एक जहाज है लेकिन मैं उस के साथ आराम कर रहा हूँ।

## क्यों न एक डाटाबेस?

1. डिजाइन की सरलता; डाटाबेसों को जमा करने के लिए शक्तिशाली सिस्टम हैं (और मैं कुछ ही देर के लिए एक जोड़ दूंगा) हालांकि वे जटिल भी जोड़ते हैं.*सही*डाटाबेस को विशेष रूप से भंय में प्रयोग करें.ENT अनुप्रयोग जो आप कोड के एक तत्व को जोड़ते हैं, चाहे आप इस्तेमाल कर रहे हों[ईएफ कोर](https://learn.microsoft.com/en-us/ef/core/), [छोड़ें (h)](https://github.com/DapperLib/Dapper)मैंने इस साइट को जितना संभव हो उतना सरल रखना चाहते हैं*इससे प्रारंभ करना है*.
2. अद्यतन और कड़ियों की प्रतिक्रिया. इस साइट को प्रदर्शित करने के लिए यह साइट है कि कैसे सरल डॉकर और डॉकर एक साइट चलाने के लिए जा सकते हैं. मैं नए कोड में जाँच करने के द्वारा साइट अद्यतन कर सकते हैं (कुछ सामग्री के साथ), काम करने के लिए, और काम करने के लिए छवि बनाता है, फिर अपने डॉकर फ़ाइल को बनाने का काम करता है. यह बहुत ही सरल तरीका है और मैं इसे बनाए रखना चाहता था कि यह एक आसान तरीका है.
3. नक़ली चल रही है, जैसे कि ZERO डाटा जो डॉकर छवि के भीतर नहीं है इसका मतलब है कि मैं सटीक रूप से बाहर चल सकते हैं (घर में मेरे छोटे से टाइट क्लैप पर). यह एक बड़ा तरीका है, जो डॉकर के साथ परिवर्तन करने के लिए।[जब मैंने छवि को सुस्पष्ट परिवर्तन किया](/blog/imagesharpwithdocker)) जीवित साइट पर उन्हें तैनात करने से पहले।
4. क्योंकि मैं नहीं चाहता था! मैं देखना चाहता था कि मैं कितना दूर तक एक आसान साइट डिजाइन के साथ मिल सकता है और इतनी दूर तक मैं इसके साथ बहुत खुश हूँ.

## आप अपने पोस्ट कैसे लिखते हैं?

मैं सिर्फ एक नया.md फ़ाइल को चिह्नित फ़ोल्डर में छोड़ देता है और साइट इसे चुन लेता है (जब मैं इसे सामग्री के रूप में याद करता हूँ तो यह निश्चित करता हूँ कि यह आउटपुट फ़ाइलों में एक जावाीय है!)

फिर जब मैं काम करता है और साइट अद्यतन किया जाता है.

```mermaid
flowchart LR
    A[Write New Markdown File] -->|Checkin To Github| B(Github Action Triggers) -->  C(Builds Docker Image) --> D(Watchtower Pulls new Image) --> E(Site Updated)
   
  
```

![setascontent.png](setascontent.png)

## आप छवियों को कैसे जोड़ते हैं?

मैं सिर्फ यहाँ जोड़ने के बाद से, मैं आपको दिखाता हूँ कि मैंने यह कैसे किया। मैंने तो सिर्फ wwwrothrrips फ़ोल्डर में छवि जोड़ा और इस तरह के निशान फ़ाइल में यह उपयोग किया:

```markdown
![setascontent.png](setascontent.png)
```

फिर मैं अपने मार्कडिस्क में एक एक्सटेंशन जोड़ता हूँ जो इन्हें सही यूआरएल में फिर से लिख देता है (सभी सरलता के बारे में).[एक्सटेंशन के लिए स्रोत कोड के लिए यहाँ देखें.](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/MarkDigExtensions/ImgExtension.cs)

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

## ब्लॉग सर्विस.

ब्लॉगेज एक सरल सेवा है जो मार्क नीचे दिए गए फ़ोल्डर से चिह्न नीचे की फ़ाइलों को पढ़ता है तथा उन्हें एचटीएमएल में परिवर्तित करता है.

इस के लिए पूरा स्रोत नीचे है और[यहाँ](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/BlogService.cs).

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
जैसा कि आप देख सकते हैं यह कुछ तत्वों में है:

### प्रक्रिया फ़ाइल

कोड जो फ़ाइलें HTML में खड़ी हैं वह बहुत ही सरल है, मैं मार्किडी लाइब्रेरी का उपयोग करता हूँ जो चिह्न नीचे को एचटीएमएल में परिवर्तित करने के लिए करता हूँ और फिर मैं कुछ नियमित एक्सप्रेशन का उपयोग करता हूँ वर्ग और तारीख़ चिह्न फ़ाइल से प्रकाशित.

प्राप्त पृष्ठ विधि चिह्न फ़ाइल की सामग्री निकालने के लिए प्रयोग में आता है, इसमें कुछ चरण हैं:

1. शीर्षक निकालें
   अधिवेशन के द्वारा मैं पोस्ट का शीर्षक के रूप में चिह्न की पहली पंक्ति का प्रयोग करता हूँ ।

```csharp
        var lines = File.ReadAllLines(page);

        // Get the title from the first line
        var title = lines.Length > 0 ? Markdown.ToPlainText(lines[0].Trim()) : string.Empty;
```

शीर्षक के रूप में कार्य के साथ उपसर्ग किया गया है "मैं मरकुस नीचे का प्रयोग करता हूँ. किसी विशेष पाठ विधि को शीर्षक से हटाने के लिए.

2. वर्गों को निकालें
   प्रत्येक पोस्ट के पास दो वर्गों में से दो श्रेणी के लिए हो सकते हैं इस प्रकार से मैं उस टैग को निशान नीचे की फ़ाइल से हटा देता हूँ.

```csharp
// Concatenate the rest of the lines with newline characters
        var restOfTheLines = string.Join(Environment.NewLine, lines.Skip(1));

        // Extract categories from the text
        var categories = GetCategories(restOfTheLines);

   // Remove category tags from the text
        restOfTheLines = CategoryRegex.Replace(restOfTheLines, "");

```

ढूंढने का तरीका एक रेगुलर एक्सप्रेशन का प्रयोग करता है जो कि चिह्न नीचे फ़ाइल से वर्गों को निकालने के लिए करता है.

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

3. प्रकाशित तिथि निकालें
   तब मैं पोस्ट से तारीख निकाल लेता हूँ (मैं बनाया गया था) लेकिन कैसे मैं इस पूरे डॉकर छवि का उपयोग कर रहा था मतलब यह वास्तव में उपयोगी नहीं है) तो मैं एक नियमित अभिव्यक्ति का उपयोग नहीं कर रहा हूँ.
   यह फ़ॉर्म में टैग को पार्स करता है जो प्रत्येक ई. एमडी फ़ाइल में है.

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

4. सामग्री निकालें
   असल में सामग्री प्राप्त करना बहुत सरल है यह काफी सरल है (ऊपर बताए गए छवि टैग बदलने के लिए) फिर वैकल्पिक रूप से मुझे पोस्ट या एचटीएमएल की सूची के लिए सादे पाठ देता है.

```csharp
    pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Use<ImgExtension>().Build();
    
   var processed =
            html ? Markdown.ToHtml(restOfTheLines, pipeline) : Markdown.ToPlainText(restOfTheLines, pipeline);
```

5. 'lulug' प्राप्त करें
   यह सिर्फ एक्सटेंशन के बगैर फ़ाइलनाम है:
   
   ```csharp
       private string GetSlug(string fileName)
       {
           var slug = System.IO.Path.GetFileNameWithoutExtension(fileName);
           return slug.ToLowerInvariant();
       }
   ```

6. विषयवस्तु वापस लें
   अब हमारे पास पृष्ठ सामग्री है हम ब्लॉग के लिए प्रदर्शन कर सकते हैं!

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
नीचे दिया गया कोड दिखाता है कि मैं ब्लॉग पोस्ट की सूची कैसे तैयार करता हूँ यह प्रयोग करता है`GetPage(page, false)`शीर्षक, श्रेणी, प्रकाशित तारीख़ तथा प्रोसेस सामग्री निकालने का विधि.

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