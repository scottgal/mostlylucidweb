# Añadiendo un feed RSS con ASP.NET Core

<!--category-- ASP.NET, RSS -->
<datetime class="hidden">2024-08-07T13:53</datetime>

## Introducción

RSS (y Atom) sigue siendo el único formato ampliamente adoptado para sintetizar contenido. Es un formato XML simple que puede ser consumido por una amplia gama de lectores de feed. En este post, te mostraré cómo agregar un feed RSS a tu aplicación ASP.NET Core.

[TOC]

## Creación de la fuente de alimentación

Realmente el núcleo de esto es crear el documento XML para el feed RSS.
El siguiente código toma una lista de `RssFeedItem` objetos y genera el XML para la fuente. Los `RssFeedItem` clase es una clase simple que representa un elemento en la fuente. Tiene propiedades para el título, enlace, descripción, fecha de publicación y categorías.

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

Cosas de interés en el código anterior:
Necesitamos crear el espacio de nombres Atom e inyectarlo en el documento XML para soportar el `atom:link` elemento.

```csharp
     XNamespace atom = "http://www.w3.org/2005/Atom";
        var feed = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",    new XAttribute(XNamespace.Xmlns + "atom", atom.NamespaceName), new XAttribute("version", "2.0"),
```

### Codificación UTF-8

Aunque especificamos `utf-8` aquí bien ASP.NET Core ignora eso... porque es especial. En cambio, necesitamos asegurarnos de que las cadenas generadas en el documento son en realidad UTF-8, había pensado esto

```csharp
   var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false) // UTF-8 without BOM
        };

```

Pero NOPE... tenemos que hacer esto:

```csharp
        using (var memoryStream = new MemoryStream())
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            feed.Save(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
```

De esta manera realmente sale un documento XML con codificación utf-8.

## El controlador

A partir de ahí es bastante simple en el RSSController, tenemos un método que acepta `category` y `startdate` (¿Actualmente una característica súper secreta??) y devuelve la alimentación.

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

ENTONCES en el `_Layout.cshtml' agregamos un enlace a la fuente:

```html
        <link rel="alternate" type="application/rss+xml"
              title="RSS Feed for mostlylucid.net"
              href="/rss" />
```

Esto asegura que el feed pueda ser 'autodescubierto' por los lectores de feed.