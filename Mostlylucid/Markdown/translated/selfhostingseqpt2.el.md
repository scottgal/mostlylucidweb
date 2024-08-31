# Seq για ASP.NET Logging - Εντοπισμός με SerilogTracing

<datetime class="hidden">2024-08-31T11:20</datetime>

<!--category-- ASP.NET, Seq, Serilog -->
# Εισαγωγή

Στο προηγούμενο μέρος σου έδειξα πώς να στήνεις [self hosting για Seq χρησιμοποιώντας ASP.NET Core ](/blog/selfhostingseq). Τώρα που το έχουμε ρυθμίσει ήρθε η ώρα να χρησιμοποιήσετε περισσότερα από τα χαρακτηριστικά του για να επιτρέψει την πληρέστερη καταγραφή & εντοπισμού χρησιμοποιώντας το νέο μας παράδειγμα Seq.

[TOC]

# Εντοπισμός

Ανιχνεύοντας είναι όπως loging++ σας δίνει ένα επιπλέον στρώμα πληροφοριών σχετικά με το τι συμβαίνει στην εφαρμογή σας. Είναι ιδιαίτερα χρήσιμο όταν έχετε ένα κατανεμημένο σύστημα και πρέπει να εντοπίσετε ένα αίτημα μέσω πολλαπλών υπηρεσιών.
Σε αυτό το site το χρησιμοποιώ για να εντοπίσω τα θέματα γρήγορα; μόνο και μόνο επειδή αυτό είναι ένα site χόμπι δεν σημαίνει ότι παραιτούμαι από τα επαγγελματικά μου πρότυπα.

## Ρύθμιση Serilog

Ρύθμιση εντοπισμού με Serilog είναι πραγματικά πολύ απλό με τη χρήση του [Serilog Tracing](https://github.com/serilog-tracing/serilog-tracing) Πακέτο. Πρώτα πρέπει να εγκαταστήσετε τα πακέτα:

Εδώ προσθέτουμε επίσης το νεροχύτη κονσόλα και το νεροχύτη Seq

```bash
dotnet add package SerilogTracing
dotnet add package SerilogTracing.Expressions
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
```

Η Κονσόλα είναι πάντα χρήσιμη για αποσφαλμάτωση και ο Σεκ είναι ο λόγος για τον οποίο είμαστε εδώ. Το Seq διαθέτει επίσης ένα μάτσο "πλούτιστες" που μπορούν να προσθέσουν επιπλέον πληροφορίες στα ημερολόγια σας.

```bash
  "Serilog": {
    "Enrich": ["FromLogContext", "WithThreadId", "WithThreadName", "WithProcessId", "WithProcessName", "FromLogContext"],
    "MinimumLevel": "Warning",
    "WriteTo": [
        {
          "Name": "Seq",
          "Args":
          {
            "serverUrl": "http://seq:5341",
            "apiKey": ""
          }
        },
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }

    ],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }
```

Για να χρησιμοποιήσετε αυτούς τους εμπλουτιστές θα πρέπει να τους προσθέσετε στο `Serilog` διαμόρφωση στη ρύθμιση σας `appsettings.json` Φάκελος. Θα πρέπει επίσης να εγκαταστήσετε όλα τα ξεχωριστά εμπλουτιστές χρησιμοποιώντας Nuget.

Είναι ένα από τα καλά και κακά πράγματα σχετικά με Serilog, μπορείτε να καταλήξετε να εγκαθιστάτε ένα BUNCH των πακέτων? αλλά αυτό σημαίνει ότι μπορείτε να προσθέσετε μόνο ό, τι χρειάζεστε και όχι μόνο ένα μονολιθικό πακέτο.
Ορίστε το δικό μου.

![Serilog πλουσιότερος](serilogenrichers.png)

Με όλα αυτά τα βομβαρδισμένα παίρνω μια αρκετά καλή έξοδο καταγραφής σε Seq.

![Σφάλμα Serilog Seq](serilogerror.png)

Εδώ βλέπετε το μήνυμα λάθους, το ίχνος στοίβας, την ταυτότητα κλωστής, την ταυτότητα διαδικασίας και το όνομα διαδικασίας. Όλα αυτά είναι χρήσιμες πληροφορίες όταν προσπαθείς να εντοπίσεις ένα θέμα.

Ένα πράγμα που πρέπει να σημειωθεί είναι ότι έχω θέσει το `  "MinimumLevel": "Warning",` στη δική μου `appsettings.json` Φάκελος. Αυτό σημαίνει ότι μόνο προειδοποιήσεις και παραπάνω θα καταγράφονται στο Seq. Αυτό είναι χρήσιμο για να κρατήσει το θόρυβο κάτω στα ημερολόγια σας.

Ωστόσο σε Seq μπορείτε επίσης να καθορίσετε αυτό ανά Api Key; έτσι μπορείτε να έχετε `Information` (ή αν είσαι πραγματικά ενθουσιώδης) `Debug`) loging set here and limit what Seq really captures by API key.

![Seq Api Key](apikey.png)

Σημείωση: εξακολουθείτε να έχετε app από πάνω, μπορείτε επίσης να κάνετε αυτό πιο δυναμική ώστε να μπορείτε να ρυθμίσετε το επίπεδο στη μύγα). Δείτε το [Seq νεροχύτηςName ](https://github.com/datalust/serilog-sinks-seq)για περισσότερες λεπτομέρειες.

```json
{
    "Serilog":
    {
        "LevelSwitches": { "$controlSwitch": "Information" },
        "MinimumLevel": { "ControlledBy": "$controlSwitch" },
        "WriteTo":
        [{
            "Name": "Seq",
            "Args":
            {
                "serverUrl": "http://localhost:5341",
                "apiKey": "yeEZyL3SMcxEKUijBjN",
                "controlLevelSwitch": "$controlSwitch"
            }
        }]
    }
}
```

## Εντοπισμός

Τώρα προσθέτουμε Tracing, και πάλι χρησιμοποιώντας SerilogTracing είναι αρκετά απλό. Έχουμε την ίδια ρύθμιση με πριν, αλλά προσθέτουμε ένα νέο νεροχύτη για την ανίχνευση.

```bash
dotnet add package SerilogTracing
dontet add package SerilogTracing.Expressions
dotnet add SerilogTracing.Instrumentation.AspNetCore
```

Προσθέτουμε επίσης ένα επιπλέον πακέτο για να καταγράψετε πιο λεπτομερείς πληροφορίες πυρήνα aspnet.

### Ρύθμιση `Program.cs`

Τώρα μπορούμε να αρχίσουμε να χρησιμοποιούμε τον εντοπισμό. Πρώτα πρέπει να προσθέσουμε τον εντοπισμό στο `Program.cs` Φάκελος.

```csharp
    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();
```

Ο εντοπισμός χρησιμοποιεί την έννοια των "Δραστηριοτήτων" που αντιπροσωπεύουν μια μονάδα εργασίας. Μπορείς να ξεκινήσεις μια δραστηριότητα, να κάνεις λίγη δουλειά και μετά να την σταματήσεις. Αυτό είναι χρήσιμο για την παρακολούθηση ενός αιτήματος μέσω πολλαπλών υπηρεσιών.

Σε αυτή την περίπτωση προσθέτουμε επιπλέον εντοπισμό για αιτήματα HttpClient και AspNetCore. Προσθέτουμε επίσης ένα `TraceToSharedLogger` που θα καταγράψει τη δραστηριότητα στον ίδιο καταγραφέα με την υπόλοιπη αίτησή μας.

## Χρήση Εντοπισμού σε Υπηρεσία

Τώρα που έχουμε τον εντοπισμό, μπορούμε να αρχίσουμε να τον χρησιμοποιούμε στην εφαρμογή μας. Εδώ είναι ένα παράδειγμα μιας υπηρεσίας που χρησιμοποιεί εντοπισμό.

```csharp
    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10,
        string language = MarkdownBaseService.EnglishLanguage)
    {
        using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
        try
        {
            var count = await NoTrackingQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .CountAsync();
            var posts = await PostsQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .OrderByDescending(x => x.PublishedDate.DateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var languages = await GetLanguagesForSlugs(posts.Select(x => x.Slug).ToList());
            var postListViewModel = new PostListViewModel
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = count,
                Posts = posts.Select(x => x.ToListModel(
                    languages.FirstOrDefault(entry => entry.Key == x.Slug).Value.ToArray())).ToList()
            };
            activity.Complete();
            return postListViewModel;
        }
        catch (Exception e)
        {
            activity.Complete(LogEventLevel.Error, e);
        }

        return new PostListViewModel();
    }
```

Οι σημαντικές γραμμές εδώ είναι:

```csharp
  using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
```

Αυτό ξεκινά μια νέα "δραστηριότητα" που είναι μια μονάδα εργασίας. Είναι χρήσιμο για την παρακολούθηση ενός αιτήματος μέσω πολλαπλών υπηρεσιών.
Καθώς το έχουμε τυλιγμένο σε μια δήλωση χρησιμοποιώντας αυτό θα ολοκληρώσει και θα διαθέσει στο τέλος της μεθόδου μας, αλλά είναι καλή πρακτική να την ολοκληρώσει ρητά.

```csharp
            activity.Complete();
```

Στην εξαίρεση μας χειρισμού αλιευμάτων ολοκληρώνουμε επίσης τη δραστηριότητα, αλλά με ένα επίπεδο λάθους και την εξαίρεση. Αυτό είναι χρήσιμο για τον εντοπισμό θεμάτων στην αίτησή σας.

## Χρήση Ιχνοστοιχείων

Τώρα έχουμε όλο αυτό το στήσιμο που μπορούμε να αρχίσουμε να το χρησιμοποιούμε. Εδώ είναι ένα παράδειγμα από ένα ίχνος στην αίτησή μου.

![Http Trace](httptrace.png)

Αυτό σας δείχνει τη μετάφραση ενός μόνο post markdown. Μπορείτε να δείτε τα πολλαπλά βήματα για μια ενιαία θέση και όλα τα αιτήματα και τις συγχρονίσεις HttpClient.

Σημείωση Χρησιμοποιώ Postgres για τη βάση δεδομένων μου, σε αντίθεση με τον εξυπηρετητή SQL ο οδηγός npgsql έχει εγγενή υποστήριξη για την ανίχνευση, ώστε να μπορείτε να πάρετε πολύ χρήσιμα δεδομένα από τα ερωτήματα βάσης δεδομένων σας, όπως το SQL που εκτελείται, συγχρονισμούς κλπ. Αυτά σώζονται ως "σπάνσορες" στον Seq και φαίνονται ψέματα τα ακόλουθα:

```json
  "@t": "2024-08-31T15:23:31.0872838Z",
"@mt": "mostlylucid",
"@m": "mostlylucid",
"@i": "3c386a9a",
"@tr": "8f9be07e41f7121cbf2866c6cd886a90",
"@sp": "8d716c5f01ad07a0",
"@st": "2024-08-31T15:23:31.0706848Z",
"@ps": "622f1c86a8b33304",
"@sk": "Client",
"ActionId": "91f5105d-93fa-4e7f-9708-b1692e046a8a",
"ActionName": "Mostlylucid.Controllers.HomeController.Index (Mostlylucid)",
"ApplicationName": "mostlylucid",
"ConnectionId": "0HN69PVEQ9S7C",
"ProcessId": 30496,
"ProcessName": "Mostlylucid",
"RequestId": "0HN69PVEQ9S7C:00000015",
"RequestPath": "/",
"SourceContext": "Npgsql",
"ThreadId": 47,
"ThreadName": ".NET TP Worker",
"db.connection_id": 1565,
"db.connection_string": "Host=localhost;Database=mostlylucid;Port=5432;Username=postgres;Application Name=mostlylucid",
"db.name": "mostlylucid",
"db.statement": "SELECT t.\"Id\", t.\"ContentHash\", t.\"HtmlContent\", t.\"LanguageId\", t.\"Markdown\", t.\"PlainTextContent\", t.\"PublishedDate\", t.\"SearchVector\", t.\"Slug\", t.\"Title\", t.\"UpdatedDate\", t.\"WordCount\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\", t0.\"Id\", t0.\"Name\", t.\"Name\"\r\nFROM (\r\n    SELECT b.\"Id\", b.\"ContentHash\", b.\"HtmlContent\", b.\"LanguageId\", b.\"Markdown\", b.\"PlainTextContent\", b.\"PublishedDate\", b.\"SearchVector\", b.\"Slug\", b.\"Title\", b.\"UpdatedDate\", b.\"WordCount\", l.\"Id\" AS \"Id0\", l.\"Name\", b.\"PublishedDate\" AT TIME ZONE 'UTC' AS c\r\n    FROM mostlylucid.\"BlogPosts\" AS b\r\n    INNER JOIN mostlylucid.\"Languages\" AS l ON b.\"LanguageId\" = l.\"Id\"\r\n    WHERE l.\"Name\" = @__language_0\r\n    ORDER BY b.\"PublishedDate\" AT TIME ZONE 'UTC' DESC\r\n    LIMIT @__p_2 OFFSET @__p_1\r\n) AS t\r\nLEFT JOIN (\r\n    SELECT b0.\"BlogPostId\", b0.\"CategoryId\", c.\"Id\", c.\"Name\"\r\n    FROM mostlylucid.blogpostcategory AS b0\r\n    INNER JOIN mostlylucid.\"Categories\" AS c ON b0.\"CategoryId\" = c.\"Id\"\r\n) AS t0 ON t.\"Id\" = t0.\"BlogPostId\"\r\nORDER BY t.c DESC, t.\"Id\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\"",
"db.system": "postgresql",
"db.user": "postgres",
"net.peer.ip": "::1",
"net.peer.name": "localhost",
"net.transport": "ip_tcp",
"otel.status_code": "OK"
```

Μπορείτε να δείτε αυτό περιλαμβάνει λίγο πολύ όλα όσα πρέπει να ξέρετε για το ερώτημα, το SQL που εκτελείται, τη συμβολοσειρά σύνδεσης κ.λπ. Όλα αυτά είναι χρήσιμες πληροφορίες όταν προσπαθείς να εντοπίσεις ένα θέμα. Σε μια μικρότερη εφαρμογή όπως αυτή είναι απλά ενδιαφέρουσα, σε μια κατανεμημένη εφαρμογή είναι στερεά χρυσό πληροφορίες για να εντοπίσετε τα θέματα.

# Συμπέρασμα

Έχω γρατζουνίσει μόνο την επιφάνεια του Tracing εδώ, είναι μια μικρή περιοχή με παθιασμένους υποστηρικτές. Ελπίζω να έχω δείξει πόσο απλό είναι να προχωρήσουμε με την απλή ανίχνευση χρησιμοποιώντας Seq & Serilog για εφαρμογές ASP.NET Core. Με αυτόν τον τρόπο μπορώ να πάρω μεγάλο μέρος του οφέλους από πιο ισχυρά εργαλεία όπως το Application Insights χωρίς το κόστος του Azure (αυτά τα πράγματα μπορούν να γίνουν δαπανηρά όταν τα αρχεία καταγραφής είναι μεγάλα).