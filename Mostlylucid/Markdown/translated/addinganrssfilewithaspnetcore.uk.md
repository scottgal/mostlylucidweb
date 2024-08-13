# Додавання подачі RSS за допомогою ядра ASP. NET

<!--category-- ASP.NET, RSS -->
<datetime class="hidden">2024- 08- 07T13: 53</datetime>

## Вступ

RSS (і Atom) - єдиний широкоприйнятний формат вмісту. Це простий формат XML, який може бути поглинутий широким спектром читачів подач. На цьому дописі я покажу вам, як додати подачу RSS до вашої програми ASP.NET.

[TOC]

## Створення подачі

Дійсно, ядро цього документа створює документ XML для подачі RSS.
Код, розташований нижче, містить список `RssFeedItem` об' єкти і створює XML подачі. The `RssFeedItem` Клас - це простий клас, який відповідає елементу у подачі. У програмі передбачено властивості заголовка, посилання, опису, дати друку і категорій.

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

Пункти уваги у коді, наведеному вище:
Нам потрібно створити простір назв Атомів і ввести його в документ XML, щоб підтримати `atom:link` елемент.

```csharp
     XNamespace atom = "http://www.w3.org/2005/Atom";
        var feed = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",    new XAttribute(XNamespace.Xmlns + "atom", atom.NamespaceName), new XAttribute("version", "2.0"),
```

### Кодування UTF- 8

Хоча ми визначаємо `utf-8` тут добре АСП.NET Core ігнорує це... тому що він особливий. Замість цього нам потрібно переконатися, що рядки, створені в документі, UTF-8, я думав, що це

```csharp
   var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false) // UTF-8 without BOM
        };

```

Но Но Нопе... нам нужно сделать это:

```csharp
        using (var memoryStream = new MemoryStream())
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            feed.Save(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
```

Таким чином документ ACTUALY виводить документ XML з кодуванням utf- 8.

## Контролер

Звідси досить просто взяти RSSController, у нас є метод, який приймає `category` і `startdate` (Поточно, супер таємна функція?) і повертає подачу.

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

ТОН у ·_Компонування. cshtml ми додаємо посилання на подачу:

```html
        <link rel="alternate" type="application/rss+xml"
              title="RSS Feed for mostlylucid.net"
              href="/rss" />
```

Це забезпечує "автоматично виявлену подачу" читанням подач.