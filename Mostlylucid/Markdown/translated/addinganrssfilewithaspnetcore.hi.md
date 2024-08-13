# एसईएसईसी के साथ RSS फ़ीड जोड़ रहा है. एनईटी कोर

<!--category-- ASP.NET, RSS -->
<datetime class="hidden">2024- 08- 0. 7T13: 53</datetime>

## परिचय

यू. एस. यह एक सरल XML फ़ॉर्मेट है जो फ़ीड रीडरों की एक विस्तृत सीमा से भस्म किया जा सकता है. इस पोस्ट में, मैं आपको दिखाता हूँ कि कैसे एक RSS फ़ीड आपके एएसई को जोड़ने के लिए।NT कोर अनुप्रयोग।

[विषय

## फ़ीड बनाया जा रहा है

सचमुच इस में का मुख्य दस्तावेज़ RSS फीड के लिए एक्सएमएल दस्तावेज़ बनाया जा रहा है.
नीचे दिए गए कोड की सूची लेता है `RssFeedItem` फीड के लिए एक्सएमएल बनाता है. वह `RssFeedItem` कक्षा एक सरल वर्ग है जो फ़ीड में वस्तु का प्रतिनिधित्व करता है. इसमें शीर्षक, लिंक, विवरण, तिथि, और वर्ग के लिए गुण हैं ।

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

ऊपर दिए कोड में टिप्पणी की बातें:
हम आणविक नेमस्पेस बनाने की जरूरत है और इसे XML दस्तावेज़ को समर्थन देने के लिए `atom:link` तत्व.

```csharp
     XNamespace atom = "http://www.w3.org/2005/Atom";
        var feed = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",    new XAttribute(XNamespace.Xmlns + "atom", atom.NamespaceName), new XAttribute("version", "2.0"),
```

### यूटीएफ़- 8 एनकोडिंग

हालाँकि हम निर्दिष्ट करते हैं `utf-8` यहाँ अच्छी तरह से एक भावना है कि कोरस अनदेखा करते हैं... क्योंकि यह विशेष है. इसके बजाय हमें यह सुनिश्चित करने की ज़रूरत है कि दस्तावेज़ में उत्पन्न वाक्यांश वास्तव में UTF-8 हैं, मैंने सोचा था कि यह

```csharp
   var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false) // UTF-8 without BOM
        };

```

लेकिन NPE... हमें यह करने की जरूरत है:

```csharp
        using (var memoryStream = new MemoryStream())
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            feed.Save(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
```

यह तरीका सामान्य रूप से एक XML दस्तावेज़ को उदाफ-8 एनकोडिंग के साथ अलग करता है.

## संदेश नियंत्रण

वहाँ से यह RSS नियंत्रणकर्ता में बहुत सरल है, हम एक तरीका है जो स्वीकार करता है `category` और `startdate` (वर्तमान में एक सुपर गुप्त सुविधा?) और फीड लौटाता है.

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

`.' में_अभिन्यास.cml हम फ़ीड के लिए एक लिंक जोड़ते हैं:

```html
        <link rel="alternate" type="application/rss+xml"
              title="RSS Feed for mostlylucid.net"
              href="/rss" />
```

यह फीड इस तरह सुनिश्चित करता है कि 'अपने आप पता लगा लिया गया' रीडर के द्वारा खोज किया जा सकता है.