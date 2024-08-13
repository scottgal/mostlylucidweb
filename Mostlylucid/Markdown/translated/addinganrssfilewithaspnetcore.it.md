# Aggiunta di un feed RSS con ASP.NET Core

<!--category-- ASP.NET, RSS -->
<datetime class="hidden">2024-08-07T13:53</datetime>

## Introduzione

RSS (e Atom) è ancora l'unico formato ampiamente adottato per i contenuti di syndicating. Si tratta di un semplice formato XML che può essere consumato da una vasta gamma di lettori di feed. In questo post, ti mostrerò come aggiungere un feed RSS alla tua applicazione ASP.NET Core.

[TOC]

## Creazione del feed

Davvero il nucleo di questo è la creazione del documento XML per il feed RSS.
Il codice qui sotto prende una lista di `RssFeedItem` oggetti e genera l'XML per il feed. La `RssFeedItem` classe è una classe semplice che rappresenta un elemento nel feed. Ha proprietà per il titolo, link, descrizione, data di pubblicazione e categorie.

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

Cose da notare nel codice di cui sopra:
Dobbiamo creare lo spazio dei nomi Atom e iniettarlo nel documento XML per supportare il `atom:link` elemento.

```csharp
     XNamespace atom = "http://www.w3.org/2005/Atom";
        var feed = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",    new XAttribute(XNamespace.Xmlns + "atom", atom.NamespaceName), new XAttribute("version", "2.0"),
```

### Codifica UTF-8

Anche se si specifica `utf-8` qui bene ASP.NET Core ignora che... perché è speciale. Invece dobbiamo assicurarci che le stringhe generate nel documento siano in realtà UTF-8, avevo pensato che questo

```csharp
   var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false) // UTF-8 without BOM
        };

```

Ma NOPE... dobbiamo farlo:

```csharp
        using (var memoryStream = new MemoryStream())
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            feed.Save(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
```

In questo modo viene effettivamente prodotto un documento XML con codifica utf-8.

## Il controllore

Da lì è abbastanza semplice nel RSSController, abbiamo un metodo che accetta `category` e `startdate` (attualmente una caratteristica super segreta??) e restituisce il feed.

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

E poi nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito, nel Regno Unito e nel Regno Unito._Layout.cshtml' aggiungiamo un link al feed:

```html
        <link rel="alternate" type="application/rss+xml"
              title="RSS Feed for mostlylucid.net"
              href="/rss" />
```

Questo assicura che il feed possa essere 'autoscoperto' dai lettori di feed.