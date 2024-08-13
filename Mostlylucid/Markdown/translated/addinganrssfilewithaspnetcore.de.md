# Hinzufügen eines RSS-Feeds mit ASP.NET Core

<!--category-- ASP.NET, RSS -->
<datetime class="hidden">2024-08-07T13:53</datetime>

## Einleitung

RSS (und Atom) ist immer noch das einzige weit verbreitete Format für die Synthese von Inhalten. Es ist ein einfaches XML-Format, das von einer Vielzahl von Feed-Readern verbraucht werden kann. In diesem Beitrag werde ich Ihnen zeigen, wie Sie einen RSS-Feed zu Ihrer ASP.NET Core-Anwendung hinzufügen.

[TOC]

## Erstellen des Feeds

Wirklich der Kern davon ist die Erstellung des XML-Dokuments für den RSS-Feed.
Der folgende Code enthält eine Liste von `RssFeedItem` objects und generiert das XML für den Feed. Das `RssFeedItem` class ist eine einfache Klasse, die ein Element im Feed darstellt. Es hat Eigenschaften für Titel, Link, Beschreibung, Publikationsdatum und Kategorien.

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

Wichtiges im obigen Code:
Wir müssen den Atom-Namensraum erstellen und ihn in das XML-Dokument injizieren, um die `atom:link` ..............................................................................................................................................

```csharp
     XNamespace atom = "http://www.w3.org/2005/Atom";
        var feed = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",    new XAttribute(XNamespace.Xmlns + "atom", atom.NamespaceName), new XAttribute("version", "2.0"),
```

### UTF-8 Kodierung

Obwohl wir spezifizieren `utf-8` ASP.NET Core ignoriert das... weil es etwas Besonderes ist. Stattdessen müssen wir sicherstellen, dass die im Dokument erzeugten Zeichenfolgen tatsächlich UTF-8 sind, hatte ich gedacht, dass

```csharp
   var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false) // UTF-8 without BOM
        };

```

Aber NOPE... wir müssen das tun:

```csharp
        using (var memoryStream = new MemoryStream())
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            feed.Save(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
```

Auf diese Weise wird ein XML-Dokument mit utf-8-Kodierung ausgegeben.

## Der Controller

Von dort aus ist es ziemlich einfach im RSSController, haben wir eine Methode, die akzeptiert `category` und `startdate` (derzeit ein super geheimes Feature??) und gibt das Futter zurück.

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

DANN in der `_Layout.cshtml' wir fügen einen Link zum Feed hinzu:

```html
        <link rel="alternate" type="application/rss+xml"
              title="RSS Feed for mostlylucid.net"
              href="/rss" />
```

Dadurch wird sichergestellt, dass der Feed von den Feedlesern 'autodiscovered' werden kann.