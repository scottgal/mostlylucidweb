# إضافة تغذية RSS مع ASP.net

<!--category-- ASP.NET, RSS -->
<datetime class="hidden">2024-2024-08-08-07-TT 13:53</datetime>

## أولاً

ولا يزال نظام RSS (والذرة) هو الشكل الوحيد المعتمد على نطاق واسع للمحتوى التجميعي. إنها صيغة XML البسيطة التي يمكن أن تستهلك من قبل مجموعة واسعة من قراء التغذية. في هذا المنصب، سأريكم كيف تضيفون تغذية RSS إلى تطبيقاتكم الأساسية.

[رابعاً -

## يجري إنشاء التلث

& جوهر هذا هو إنشاء مستند XML لتغذية RSS.
الشفرة الواردة أدناه تأخذ قائمة `RssFeedItem` كائنات و توليد XML لـ. الـ `RssFeedItem` الصف هو صنف بسيط يمثل عنصر في التغذية. ولها خصائص للعنوان، والرابط، والوصف، وتاريخ النشر، والفئات.

```csharp
    public string GenerateFeed(IEnumerable<RssFeedItem> items, string categoryName = "")
    {
        XNamespace atom = "http://www.w3.org/2005/Atom";
        var feed = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",    new XAttribute(XNamespace.Xmlns + "atom", atom.NamespaceName), new XAttribute("version", "2.0"),
                new XElement("channel",
                    new XElement("title", !string.IsNullOrEmpty(categoryName) ? $"mostlylucid.net for {categoryName}" : $"mostlylucid.net"),
                    new XElement("link", $"{GetSiteUrl()}/rss"),
                    new XElement("description", "The latest posts from mostlylucid.net"),
                    new XElement("pubDate", DateTime.UtcNow.ToString("R")),
                    new XElement(atom + "link", 
                        new XAttribute("href", $"{GetSiteUrl()}/rss"), 
                        new XAttribute("rel", "self"), 
                        new XAttribute("type", "application/rss+xml")),
                    from item in items
                    select new XElement("item",
                        new XElement("title", item.Title),
                        new XElement("link", item.Link),
                        new XElement("guid", item.Guid, new XAttribute("isPermaLink", "false")),
                        new XElement("description", item.Description),
                        new XElement("pubDate", item.PubDate.ToString("R")),
                        from category in item.Categories
                        select new XElement("category", category)
                    )
                )
            )
        );

        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false) // UTF-8 without BOM
        };

        using (var memoryStream = new MemoryStream())
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            feed.Save(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
    }
```

أشياء ملحوظة في الشفرة أعلاه:
نحن بحاجة إلى إنشاء الذرة الاسم الاسم الفضاء و حقنها في مستند XML لدعم `atom:link` (ج) عنصر من العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر أو العنصر.

```csharp
     XNamespace atom = "http://www.w3.org/2005/Atom";
        var feed = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",    new XAttribute(XNamespace.Xmlns + "atom", atom.NamespaceName), new XAttribute("version", "2.0"),
```

### تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش تش

على الرغم من أننا نحدد `utf-8` هنا أيضا ASP.NET الرئيسية تتجاهل أن... لأنه خاص. بدلاً من ذلك علينا أن نضمن أن السلاسل المتولدة في الوثيقة هي في الواقع UTF-8، كنت قد فكرت في هذا

```csharp
   var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false) // UTF-8 without BOM
        };

```

لكن يجب أن نفعل هذا

```csharp
        using (var memoryStream = new MemoryStream())
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            feed.Save(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
```

بهذه الطريقة تخرج مستند XML مع تشفير utf-8.

## الـ مُؤَمِل المُمَمِن

من هناك هو بسيط جداً في RSS Pass، عِنْدَنا a أسلوب الذي يَقْبلُ `category` وقد عقد مؤتمراً بشأن `startdate` (حاليًا خاصية سريّة خارقة؟) ويُرجعُ التغذّي.

```csharp
    [HttpGet]
    public IActionResult Index([FromQuery] string category = null, [FromQuery] string startDate = null)
    {
        DateTime? startDateTime = null;
        if (DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out DateTime startDateTIme))
        {
            logger.LogInformation("Start date is {startDate}", startDate);
        }

        var rssFeed = rssFeedService.GenerateFeed(startDateTime, category);
        return Content(rssFeed, "application/rss+xml", Encoding.UTF8);
    }
```

في ما يلي:_التصميم. cshtml، نضيف رابط إلى التغذية:

```html
        <link rel="alternate" type="application/rss+xml"
              title="RSS Feed for mostlylucid.net"
              href="/rss" />
```

وهذا يضمن أن التغذية يمكن أن تكون 'غير مغطاة' من قِبَل قراء التغذية.