# Lägga till ett RSS-flöde med ASP.NET Core

<!--category-- ASP.NET, RSS -->
<datetime class="hidden">Försäkrings- och återförsäkringsföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, värdepappersföretag, försäkringsholdingföretag, värdepappersföretag, försäkringsholding och andra finansiella företag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag och andra finansiella institut, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag och andra finansiella institut, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag och andra finansiella institut, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag och andra finansiella institut, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag och värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag och värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag</datetime>

## Inledning

RSS (och Atom) är fortfarande det enda allmänt antagna formatet för syndikering av innehåll. Det är en enkel XML-format som kan konsumeras av ett brett utbud av matar läsare. I det här inlägget, Jag ska visa dig hur du lägger till en RSS-flöde till din ASP.NET Core ansökan.

[TOC]

## Skapa foder

Verkligen kärnan i detta är att skapa XML-dokument för RSS-flöde.
Koden nedan innehåller en lista över `RssFeedItem` objekt och genererar XML för flödet. I detta sammanhang är det viktigt att se till att `RssFeedItem` klass är en enkel klass som representerar ett objekt i fodret. Den har egenskaper för titel, länk, beskrivning, publiceringsdatum och kategorier.

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

Saker att notera i koden ovan:
Vi måste skapa Atom namnrymden och injicera den i XML-dokumentet för att stödja `atom:link` Förutsättningar.

```csharp
     XNamespace atom = "http://www.w3.org/2005/Atom";
        var feed = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",    new XAttribute(XNamespace.Xmlns + "atom", atom.NamespaceName), new XAttribute("version", "2.0"),
```

### UTF-8- kodning

Även om vi specificerar `utf-8` ASP.NET Core ignorerar det... för det är speciellt. Istället måste vi se till att de strängar som skapas i dokumentet är faktiskt UTF-8, Jag hade trott detta

```csharp
   var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false) // UTF-8 without BOM
        };

```

Men NEOPE... vi måste göra det här:

```csharp
        using (var memoryStream = new MemoryStream())
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            feed.Save(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
```

På så sätt matar den faktiskt ut ett XML-dokument med utf-8-kodning.

## Den personuppgiftsansvarige

Därifrån är det ganska enkelt i RSSController, vi har en metod som accepterar `category` och `startdate` (för närvarande en super hemlig funktion??) och returnerar flödet.

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

DESSA I `EN_Layout.cshtml' lägger vi till en länk till kanalen:

```html
        <link rel="alternate" type="application/rss+xml"
              title="RSS Feed for mostlylucid.net"
              href="/rss" />
```

Detta säkerställer att fodret kan "autodiscovered" av foderläsare.