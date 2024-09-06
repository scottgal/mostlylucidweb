# Χρήση δεδομένων Umami για τα Στατιστικά Ιστοσελίδας

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-05T23:45</datetime>

# Εισαγωγή

Ένα από τα έργα μου από την έναρξη αυτού του blog είναι μια σχεδόν εμμονή επιθυμία να παρακολουθείτε πόσοι χρήστες κοιτάζουν την ιστοσελίδα μου. Για να το κάνω αυτό χρησιμοποιώ Umami και έχω ένα [ΠΛΗΘΥΣΜΟΣ θέσεων](/blog/category/Umami) γύρω από τη χρήση και τη δημιουργία Umami. Έχω επίσης ένα πακέτο Nuget που καθιστά δυνατή την παρακολούθηση δεδομένων από μια ιστοσελίδα ASP.NET Core.

Τώρα έχω προσθέσει μια νέα υπηρεσία που σας επιτρέπει να μεταφέρετε τα δεδομένα πίσω από Umami σε μια εφαρμογή C#. Αυτή είναι μια απλή υπηρεσία που χρησιμοποιεί το Umami API για να τραβήξει τα δεδομένα από την περίπτωσή σας Umami και να τα χρησιμοποιήσει στην ιστοσελίδα / εφαρμογή σας.

Ως συνήθως, μπορεί να βρεθεί όλος ο πηγαίος κώδικας γι' αυτό. [στο GitHub μου](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net) για αυτό το site.

[TOC]

# Εγκατάσταση

Αυτό είναι ήδη στο πακέτο Umami.Net Nuget, εγκαταστήστε το χρησιμοποιώντας την ακόλουθη εντολή:

```bash
dotnet add package Umami.Net
```

Στη συνέχεια, θα πρέπει να ρυθμίσετε την υπηρεσία σας `Program.cs` αρχείο:

```csharp
    services.SetupUmamiData(config);
```

Αυτό χρησιμοποιεί το `Analytics' element from your `appsettings.json* αρχείο:

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo",
   "UserName": "admin",
   "Password": ""
 }
```

Ορίστε... `UmamiScript` είναι το σενάριο που χρησιμοποιείτε για την παρακολούθηση πλευρά του πελάτη στο Umami ([Δες εδώ.](/blog/usingumamiforlocalanalytics) για το πώς να το ρυθμίσετε αυτό επάνω).
Η `WebSiteId` είναι η ταυτότητα της ιστοσελίδας που δημιουργήσατε στην περίπτωσή σας Umami.
`UmamiPath` είναι το μονοπάτι για την περίπτωσή σου στο Umami.

Η `UserName` και `Password` είναι τα διαπιστευτήρια για την περίπτωση Umami (σε αυτή την περίπτωση χρησιμοποιώ τον κωδικό Admin).

# Χρήση

Τώρα έχεις το... `UmamiDataService` στη συλλογή υπηρεσιών σας μπορείτε να αρχίσετε να τη χρησιμοποιείτε!

## Μέθοδοι

Οι μέθοδοι είναι όλες από τον ορισμό Umami API μπορείτε να διαβάσετε γι 'αυτούς εδώ:
https://umami.is/docs/api/website-stats

Όλες οι αποδόσεις είναι τυλιγμένες σε ένα `UmamiResults<T>` αντικείμενο που έχει ένα `Success` περιουσιακά στοιχεία και `Result` ιδιοκτησία. Η `Result` ιδιοκτησία είναι το αντικείμενο που επιστρέφεται από το Umami API.

```csharp
public record UmamiResult<T>(HttpStatusCode Status, string Message, T? Data);
```

Όλα τα αιτήματα εκτός από `ActiveUsers` έχουν ένα βασικό αντικείμενο αίτησης με δύο υποχρεωτικές ιδιότητες. Πρόσθεσα ευκολία DateTimes στο βασικό αίτημα αντικείμενο για να καταστεί ευκολότερη η ρύθμιση των ημερομηνιών έναρξης και λήξης.

```csharp
public class BaseRequest
{
    [QueryStringParameter("startAt", isRequired: true)]
    public long StartAt => StartAtDate.ToMilliseconds(); // Timestamp (in ms) of starting date
    [QueryStringParameter("endAt", isRequired: true)]
    public long EndAt => EndAtDate.ToMilliseconds(); // Timestamp (in ms) of end date
    public DateTime StartAtDate { get; set; }
    public DateTime EndAtDate { get; set; }
}
```

Η υπηρεσία έχει τις ακόλουθες μεθόδους:

### Ενεργοί χρήστες

Αυτό μόλις παίρνει τον συνολικό αριθμό των δραστήριων χρηστών στο site

```csharp
public async Task<UmamiResult<ActiveUsersResponse>> GetActiveUsers()
```

### Στατιστικά

Αυτό επιστρέφει ένα μάτσο στατιστικά στοιχεία σχετικά με την ιστοσελίδα, συμπεριλαμβανομένου του αριθμού των χρηστών, προβολές σελίδας, κλπ.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStats(StatsRequest statsRequest)    
```

Μπορείτε να ορίσετε έναν αριθμό παραμέτρων για να φιλτράρετε τα δεδομένα που επιστρέφονται από το API. Για παράδειγμα, χρήση `url` θα επιστρέψει τα στατιστικά για ένα συγκεκριμένο URL.

<details>
<summary>StatsRequest object</summary>
```csharp
public class StatsRequest : BaseRequest
{
    [QueryStringParameter("url")]
    public string? Url { get; set; } // Name of URL
    
    [QueryStringParameter("referrer")]
    public string? Referrer { get; set; } // Name of referrer
    
    [QueryStringParameter("title")]
    public string? Title { get; set; } // Name of page title
    
    [QueryStringParameter("query")]
    public string? Query { get; set; } // Name of query
    
    [QueryStringParameter("event")]
    public string? Event { get; set; } // Name of event
    
    [QueryStringParameter("host")]
    public string? Host { get; set; } // Name of hostname
    
    [QueryStringParameter("os")]
    public string? Os { get; set; } // Name of operating system
    
    [QueryStringParameter("browser")]
    public string? Browser { get; set; } // Name of browser
    
    [QueryStringParameter("device")]
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    
    [QueryStringParameter("country")]
    public string? Country { get; set; } // Name of country
    
    [QueryStringParameter("region")]
    public string? Region { get; set; } // Name of region/state/province
    
    [QueryStringParameter("city")]
    public string? City { get; set; } // Name of city
}
```

</details>
Το αντικείμενο JSON Umami επιστρέφει είναι ως εξής.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

Αυτό είναι τυλιγμένο μέσα μου. `StatsResponseModel` αντικείμενο.

```csharp
namespace Umami.Net.UmamiData.Models.ResponseObjects;

public class StatsResponseModels
{
    public Pageviews pageviews { get; set; }
    public Visitors visitors { get; set; }
    public Visits visits { get; set; }
    public Bounces bounces { get; set; }
    public Totaltime totaltime { get; set; }


    public class Pageviews
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Visitors
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Visits
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Bounces
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Totaltime
    {
        public int value { get; set; }
        public int prev { get; set; }
    }
}
```

### Μετρικός

Μετρική στο Umami σας παρέχει τον αριθμό των απόψεων για συγκεκριμένους τύπους ιδιοτήτων.

#### Γεγονότα

Ένα παράδειγμα αυτών είναι τα Γεγονότα:

"Γεγονότα" στο Umami είναι συγκεκριμένα στοιχεία που μπορείτε να παρακολουθείτε σε ένα site. Όταν παρακολουθείτε γεγονότα χρησιμοποιώντας Umami.Net μπορείτε να ορίσετε μια σειρά από ιδιότητες που παρακολουθούνται με το όνομα του γεγονότος. Για παράδειγμα εδώ... `Search` αιτήσεις με το URL και τον όρο αναζήτησης.

```csharp
       await  umamiBackgroundSender.Track( "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
```

Για να πάρετε τα δεδομένα σχετικά με αυτό το γεγονός θα χρησιμοποιήσετε το `Metrics` μέθοδος:

```csharp
public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
```

Όπως και με τις άλλες μεθόδους, αυτό δέχεται την `MetricsRequest` αντικείμενο (με το υποχρεωτικό `BaseRequest` ιδιότητες) και μια σειρά προαιρετικών ιδιοτήτων για τη διήθηση των δεδομένων.

<details>
<summary>MetricsRequest object</summary>
```csharp
public class MetricsRequest : BaseRequest
{
    [QueryStringParameter("type", isRequired: true)]
    public MetricType Type { get; set; } // Metrics type

    [QueryStringParameter("url")]
    public string? Url { get; set; } // Name of URL
    
    [QueryStringParameter("referrer")]
    public string? Referrer { get; set; } // Name of referrer
    
    [QueryStringParameter("title")]
    public string? Title { get; set; } // Name of page title
    
    [QueryStringParameter("query")]
    public string? Query { get; set; } // Name of query
    
    [QueryStringParameter("host")]
    public string? Host { get; set; } // Name of hostname
    
    [QueryStringParameter("os")]
    public string? Os { get; set; } // Name of operating system
    
    [QueryStringParameter("browser")]
    public string? Browser { get; set; } // Name of browser
    
    [QueryStringParameter("device")]
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    
    [QueryStringParameter("country")]
    public string? Country { get; set; } // Name of country
    
    [QueryStringParameter("region")]
    public string? Region { get; set; } // Name of region/state/province
    
    [QueryStringParameter("city")]
    public string? City { get; set; } // Name of city
    
    [QueryStringParameter("language")]
    public string? Language { get; set; } // Name of language
    
    [QueryStringParameter("event")]
    public string? Event { get; set; } // Name of event
    
    [QueryStringParameter("limit")]
    public int? Limit { get; set; } = 500; // Number of events returned (default: 500)
}
```

</details>
Εδώ μπορείτε να δείτε ότι μπορείτε να καθορίσετε μια σειρά ιδιοτήτων στο στοιχείο αίτησης για να καθορίσετε ποιες μετρήσεις θέλετε να επιστρέψετε.

Μπορείτε επίσης να ρυθμίσετε ένα `Limit` περιουσιακό στοιχείο για τον περιορισμό του αριθμού των επιστρεφόμενων αποτελεσμάτων.

Για παράδειγμα, για να πάρετε το γεγονός κατά τη διάρκεια της προηγούμενης ημέρας ανέφερα παραπάνω θα χρησιμοποιήσετε το ακόλουθο αίτημα:

```csharp
var metricsRequest = new MetricsRequest
{
    StartAtDate = DateTime.Now.AddDays(-1),
    EndAtDate = DateTime.Now,
    Type = MetricType.@event,
    Event = "searchEvent"
};
```

Το αντικείμενο JSON που επιστρέφεται από το API έχει ως εξής:

```json
[
  { "x": "searchEvent", "y": 46 }
]
```

Και πάλι τυλίγω αυτό στο δικό μου `MetricsResponseModels` αντικείμενο.

```csharp
public class MetricsResponseModels
{
    public string x { get; set; }
    public int y { get; set; }
}
```

Όπου x είναι το όνομα του γεγονότος και y είναι ο αριθμός των φορές που έχει ενεργοποιηθεί.

#### Προβολές σελίδας

Μια από τις πιο χρήσιμες μετρήσεις είναι ο αριθμός των προβολές σελίδων. Αυτός είναι ο αριθμός των φορές που μια σελίδα έχει προβληθεί στο site. Παρακάτω είναι το τεστ που χρησιμοποιώ για να πάρω τον αριθμό των προβολές σελίδας κατά τις τελευταίες 30 ημέρες. Θα σημειώσετε το `Type` Η παράμετρος έχει οριστεί ως `MetricType.url` Ωστόσο, αυτό είναι επίσης η προεπιλεγμένη τιμή, έτσι δεν χρειάζεται να το ρυθμίσετε.

```csharp
  [Fact]
    public async Task Metrics_StartEnd()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var websiteDataService = serviceProvider.GetRequiredService<UmamiDataService>();
        
        var metrics = await websiteDataService.GetMetrics(new MetricsRequest()
        {
            StartAtDate = DateTime.Now.AddDays(-30),
            EndAtDate = DateTime.Now,
            Type = MetricType.url,
            Limit = 500
        });
        Assert.NotNull(metrics);
        Assert.Equal( HttpStatusCode.OK, metrics.Status);

    }
```

Αυτό επιστρέφει `MetricsResponse` αντικείμενο που έχει την ακόλουθη δομή JSON:

```json
[
  {
    "x": "/",
    "y": 1
  },
  {
    "x": "/blog",
    "y": 1
  },
  {
    "x": "/blog/usingumamidataforwebsitestats",
    "y": 1
  }
]
```

Πού; `x` είναι το URL και `y` είναι ο αριθμός των φορές που έχει δει.

### PageViews

Αυτό επιστρέφει τον αριθμό των προβολές σελίδας για ένα συγκεκριμένο URL.

Και πάλι εδώ είναι ένα τεστ που χρησιμοποιώ για αυτή τη μέθοδο:

```csharp
    [Fact]
    public async Task PageViews_StartEnd_Day_Url()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var websiteDataService = serviceProvider.GetRequiredService<UmamiDataService>();
    
        var pageViews = await websiteDataService.GetPageViews(new PageViewsRequest()
        {
            StartAtDate = DateTime.Now.AddDays(-7),
            EndAtDate = DateTime.Now,
            Unit = Unit.day,
            Url = "/blog"
        });
        Assert.NotNull(pageViews);
        Assert.Equal( HttpStatusCode.OK, pageViews.Status);

    }
```

Αυτό επιστρέφει `PageViewsResponse` αντικείμενο που έχει την ακόλουθη δομή JSON:

```json
[
  {
    "date": "2024-09-06 00:00",
    "value": 1
  }
]
```

Πού; `date` είναι η ημερομηνία και `value` είναι ο αριθμός των προβολές σελίδας, αυτό επαναλαμβάνεται για κάθε ημέρα στο εύρος που καθορίζεται (ή ώρα, μήνας, κλπ.). ανάλογα με την `Unit` ιδιοκτησία).

Όπως και με τις άλλες μεθόδους, αυτό δέχεται την `PageViewsRequest` αντικείμενο (με το υποχρεωτικό `BaseRequest` ιδιότητες) και μια σειρά προαιρετικών ιδιοτήτων για τη διήθηση των δεδομένων.

<details>
<summary>PageViewsRequest object</summary>
```csharp
public class PageViewsRequest : BaseRequest
{
    // Required properties

    [QueryStringParameter("unit", isRequired: true)]
    public Unit Unit { get; set; } = Unit.day; // Time unit (year | month | hour | day)
    
    [QueryStringParameter("timezone")]
    [TimeZoneValidator]
    public string Timezone { get; set; }

    // Optional properties
    [QueryStringParameter("url")]
    public string? Url { get; set; } // Name of URL
    [QueryStringParameter("referrer")]
    public string? Referrer { get; set; } // Name of referrer
    [QueryStringParameter("title")]
    public string? Title { get; set; } // Name of page title
    [QueryStringParameter("host")]
    public string? Host { get; set; } // Name of hostname
    [QueryStringParameter("os")]
    public string? Os { get; set; } // Name of operating system
    [QueryStringParameter("browser")]
    public string? Browser { get; set; } // Name of browser
    [QueryStringParameter("device")]
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    [QueryStringParameter("country")]
    public string? Country { get; set; } // Name of country
    [QueryStringParameter("region")]
    public string? Region { get; set; } // Name of region/state/province
    [QueryStringParameter("city")]
    public string? City { get; set; } // Name of city
}
```

</details>
Όπως και με τις άλλες μεθόδους μπορείτε να ορίσετε μια σειρά από ιδιότητες για να φιλτράρει τα δεδομένα που επιστρέφονται από το API, για παράδειγμα, θα μπορούσατε να ρυθμίσετε το
`Country` ιδιοκτησία για να πάρετε τον αριθμό των προβολές σελίδα από μια συγκεκριμένη χώρα.

# Χρήση της Υπηρεσίας

Σε αυτό το site έχω κάποιο κώδικα που μου επιτρέπει να χρησιμοποιήσω αυτή την υπηρεσία για να πάρει τον αριθμό των προβολές κάθε σελίδα blog έχει. Στον παρακάτω κωδικό παίρνω μια ημερομηνία έναρξης και λήξης και ένα πρόθεμα (το οποίο είναι `/blog` στην περίπτωσή μου) και να πάρει τον αριθμό των προβολές για κάθε σελίδα στο blog.

Μετά καταγράφω τα δεδομένα για μια ώρα, οπότε δεν χρειάζεται να χτυπάω συνέχεια το API του Umami.

```csharp
public class UmamiDataSortService(
    UmamiDataService dataService,
    IMemoryCache cache)
{
    public async Task<List<MetricsResponseModels>?> GetMetrics(DateTime startAt, DateTime endAt, string prefix="" )
    {
        using var activity = Log.Logger.StartActivity("GetMetricsWithPrefix");
        try
        {
            var cacheKey = $"Metrics_{startAt}_{endAt}_{prefix}";
            if (cache.TryGetValue(cacheKey, out List<MetricsResponseModels>? metrics))
            {
                activity?.AddProperty("CacheHit", true);
                return metrics;
            }
            activity?.AddProperty("CacheHit", false);
            var metricsRequest = new MetricsRequest()
            {
                StartAtDate = startAt,
                EndAtDate = endAt,
                Type = MetricType.url,
                Limit = 500
            };
            var metricRequest = await dataService.GetMetrics(metricsRequest);

            if(metricRequest.Status != HttpStatusCode.OK)
            {
                return null;
            }
            var filteredMetrics = metricRequest.Data.Where(x => x.x.StartsWith(prefix)).ToList();
            cache.Set(cacheKey, filteredMetrics, TimeSpan.FromHours(1));
            activity?.AddProperty("MetricsCount", filteredMetrics?.Count()?? 0);
            activity?.Complete();
            return filteredMetrics;
        }
        catch (Exception e)
        {
            activity?.Complete(LogEventLevel.Error, e);
         
            return null;
        }
    }

```

# Συμπέρασμα

Αυτή είναι μια απλή υπηρεσία που σας επιτρέπει να τραβήξετε τα δεδομένα από Umami και να τα χρησιμοποιήσετε στην εφαρμογή σας. Χρησιμοποιώ αυτό για να πάρω τον αριθμό των προβολών για κάθε σελίδα blog και να το εμφανίσω στη σελίδα. Αλλά είναι πολύ χρήσιμο για να πάρει απλά ένα BUNCH των δεδομένων σχετικά με το ποιος χρησιμοποιεί το site σας και πώς το χρησιμοποιούν.