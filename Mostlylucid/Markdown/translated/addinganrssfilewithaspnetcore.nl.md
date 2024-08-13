# Een RSS-feed toevoegen met ASP.NET Core

<!--category-- ASP.NET, RSS -->
<datetime class="hidden">2024-08-07T13:53</datetime>

## Inleiding

RSS (en Atom) is nog steeds het enige veel gebruikte formaat voor het synchroniseren van inhoud. Het is een eenvoudig XML formaat dat kan worden verbruikt door een breed scala van feed lezers. In dit bericht, zal ik u laten zien hoe u een RSS-feed toe te voegen aan uw ASP.NET Core applicatie.

[TOC]

## Aanmaken van de feed

De kern hiervan is het aanmaken van het XML-document voor de RSS-feed.
De code hieronder neemt een lijst van `RssFeedItem` objecten en genereert de XML voor de feed. De `RssFeedItem` klasse is een eenvoudige klasse die een item in de feed vertegenwoordigt. Het heeft eigenschappen voor de titel, link, beschrijving, publicatiedatum en categorieën.

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

Opmerkelijke dingen in de bovenstaande code:
We moeten de Atom namespace maken en injecteren in het XML-document om de `atom:link` element.

```csharp
     XNamespace atom = "http://www.w3.org/2005/Atom";
        var feed = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",    new XAttribute(XNamespace.Xmlns + "atom", atom.NamespaceName), new XAttribute("version", "2.0"),
```

### UTF-8-codering

Hoewel we specificeren... `utf-8` ASP.NET Core negeert dat... omdat het speciaal is. In plaats daarvan moeten we ervoor zorgen dat de strings gegenereerd in het document zijn eigenlijk UTF-8, Ik dacht dat dit

```csharp
   var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false) // UTF-8 without BOM
        };

```

Maar NOPE... we moeten dit doen:

```csharp
        using (var memoryStream = new MemoryStream())
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            feed.Save(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
```

Op deze manier geeft het daadwerkelijk een XML-document uit met utf-8-codering.

## De controller

Vanaf daar is het vrij eenvoudig in de RSSController, we hebben een methode die accepteert `category` en `startdate` (momenteel een super geheim kenmerk??) en geeft de feed terug.

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

DAN in de_Layout.cshtml' voegen we een link toe aan de feed:

```html
        <link rel="alternate" type="application/rss+xml"
              title="RSS Feed for mostlylucid.net"
              href="/rss" />
```

Dit zorgt ervoor dat de feed 'autoontdekt' kan worden door feedlezers.