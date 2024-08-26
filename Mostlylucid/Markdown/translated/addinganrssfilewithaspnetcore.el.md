# Προσθήκη μιας RSS feed με ASP.NET Core

<!--category-- ASP.NET, RSS -->
<datetime class="hidden">2024-08-07T13:53</datetime>

## Εισαγωγή

RSS (και Atom) εξακολουθεί να είναι η μόνη ευρέως υιοθετημένη μορφή για τη σύνθεση του περιεχομένου. Είναι μια απλή μορφή XML που μπορεί να καταναλωθεί από ένα ευρύ φάσμα αναγνωστών ζωοτροφών. Σε αυτή τη δημοσίευση, θα σας δείξω πώς να προσθέσετε μια RSS feed στην εφαρμογή σας ASP.NET Core.

[TOC]

## Δημιουργία της τροφής

Πραγματικά ο πυρήνας αυτού είναι η δημιουργία του εγγράφου XML για την τροφοδοσία RSS.
Ο παρακάτω κώδικας λαμβάνει μια λίστα των `RssFeedItem` αντικείμενα και παράγει το XML για την τροφή. Η `RssFeedItem` κατηγορία είναι μια απλή κατηγορία που αντιπροσωπεύει ένα στοιχείο στην τροφή. Έχει ιδιότητες για τον τίτλο, το σύνδεσμο, την περιγραφή, την ημερομηνία δημοσίευσης και τις κατηγορίες.

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

Σημειωματάρια στον παραπάνω κωδικό:
Πρέπει να δημιουργήσουμε τον χώρο ονομάτων ατόμων και να τον εντάξουμε στο έγγραφο XML για να υποστηρίξουμε το `atom:link` στοιχείο.

```csharp
     XNamespace atom = "http://www.w3.org/2005/Atom";
        var feed = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",    new XAttribute(XNamespace.Xmlns + "atom", atom.NamespaceName), new XAttribute("version", "2.0"),
```

### Κωδικοποίηση UTF-8

Αν και θα καθορίσουμε... `utf-8` Το ASP.NET Core το αγνοεί αυτό... γιατί είναι ξεχωριστό. Αντίθετα, πρέπει να διασφαλίσουμε ότι οι χορδές που παράγονται στο έγγραφο είναι στην πραγματικότητα UTF-8, είχα σκεφτεί αυτό

```csharp
   var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false) // UTF-8 without BOM
        };

```

Αλλά NOPE... πρέπει να το κάνουμε αυτό:

```csharp
        using (var memoryStream = new MemoryStream())
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            feed.Save(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
```

Με αυτόν τον τρόπο παράγει ουσιαστικά ένα έγγραφο XML με κωδικοποίηση utf-8.

## Ο ελεγκτής

Από εκεί είναι αρκετά απλό στο RSSController, έχουμε μια μέθοδο που δέχεται `category` και `startdate` (προς το παρόν ένα σούπερ μυστικό χαρακτηριστικό??) και επιστρέφει την τροφή.

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

ΤΟΤΕ, μέσα στο ~_Διάταξη.cshtml" προσθέτουμε ένα σύνδεσμο με την τροφή:

```html
        <link rel="alternate" type="application/rss+xml"
              title="RSS Feed for mostlylucid.net"
              href="/rss" />
```

Αυτό εξασφαλίζει ότι η τροφή μπορεί να "ανακαλυφθεί" από τους αναγνώστες ζωοτροφών.