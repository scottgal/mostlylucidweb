# RSS-syötteen lisääminen ASP.NET Corella

<!--category-- ASP.NET, RSS -->
<datetime class="hidden">2024-08-07T13:53</datetime>

## Johdanto

RSS (ja Atom) on edelleen ainoa laajalti hyväksytty muoto sisällön syntetisoimiseksi. Se on yksinkertainen XML-formaatti, jota voi kuluttaa laaja valikoima rehunlukijoita. Tässä viestissä näytän, kuinka voit lisätä RSS-syötteen ASP.NET Core -sovellukseesi.

[TÄYTÄNTÖÖNPANO

## Syötteen luominen

Tämän ydin on XML-dokumentin luominen RSS-syötteeseen.
Alla olevassa koodissa on luettelo `RssFeedItem` objekteja ja tuottaa XML:n syötettä varten. Erytropoietiini `RssFeedItem` Luokka on yksinkertainen luokka, joka edustaa syötteessä olevaa esinettä. Siinä on ominaisuuksia otsikolle, linkille, kuvaukselle, julkaisupäivälle ja kategorioille.

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

Huomautukset yllä olevassa koodissa:
Meidän on luotava Atom-nimiavaruus ja ruiskutettava se XML-asiakirjaan. `atom:link` elementti.

```csharp
     XNamespace atom = "http://www.w3.org/2005/Atom";
        var feed = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",    new XAttribute(XNamespace.Xmlns + "atom", atom.NamespaceName), new XAttribute("version", "2.0"),
```

### UTF-8:n koodaus

Vaikka me täsmennämme `utf-8` ASP.NET Core ei huomioi sitä, koska se on erityinen. Sen sijaan meidän on varmistettava, että asiakirjassa esitetyt narut ovat itse asiassa UTF-8, olin ajatellut näin

```csharp
   var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false) // UTF-8 without BOM
        };

```

Mutta NOPE... Meidän täytyy tehdä tämä:

```csharp
        using (var memoryStream = new MemoryStream())
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            feed.Save(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
```

Näin se itse asiassa tuottaa XML-dokumentin, jossa on utf-8-koodaus.

## Ohjaaja

Sieltä se on aika yksinkertainen RSSController, meillä on menetelmä, joka hyväksyy `category` sekä `startdate` (tällä hetkellä supersalainen ominaisuus?) ja palauttaa syötön.

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

SITTEN "_Layout.cshtml' lisäämme syötteeseen linkin:

```html
        <link rel="alternate" type="application/rss+xml"
              title="RSS Feed for mostlylucid.net"
              href="/rss" />
```

Näin varmistetaan, että rehunlukijat voivat "autodisoida" syötön.