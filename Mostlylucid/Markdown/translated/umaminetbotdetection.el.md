# Umami.Net and Bot Detection

# Εισαγωγή

Οπότε το έκανα. [Αναρτήθηκε ένα LOT](/blog/category/Umami) στο παρελθόν σχετικά με τη χρήση Umami για την ανάλυση σε ένα περιβάλλον που φιλοξενείται από τον εαυτό του και μάλιστα δημοσίευσε το [Umami.Net Nuget pakakge](https://www.nuget.org/packages/Umami.Net/). Ωστόσο, είχα ένα θέμα όπου ήθελα να παρακολουθήσω τους χρήστες των RSS feed μου· αυτή η ανάρτηση πηγαίνει στο γιατί και πώς το έλυσα.

[TOC]

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-12T14:50</datetime>

# Το Πρόβλημα

Το πρόβλημα είναι ότι οι αναγνώστες RSS feed προσπαθούν να περάσουν *χρήσιμο* Πράκτορες χρηστών όταν ζητούν την τροφοδοσία. Αυτό επιτρέπει **συμμορφώνεται με τις απαιτήσεις του κανονισμού (ΕΕ) αριθ. 182/2011 του Ευρωπαϊκού Κοινοβουλίου και του Συμβουλίου** οι πάροχοι να παρακολουθούν τον αριθμό των χρηστών και τον τύπο των χρηστών που καταναλώνουν τις ζωοτροφές. Ωστόσο, αυτό σημαίνει επίσης ότι η Umami θα προσδιορίσει αυτά τα αιτήματα ως *ρομπότCity name (optional, probably does not need a translation)* Ζητάω συγγνώμη. Αυτό είναι ένα θέμα για τη χρήση μου, καθώς έχει ως αποτέλεσμα να αγνοείται το αίτημα και να μην παρακολουθείται.

Ο χρήστης Feedbin μοιάζει με αυτό:

```plaintext
Feedbin feed-id:1234 - 21 subscribers
```

Οπότε πολύ χρήσιμο δικαίωμα, περνά μερικές χρήσιμες λεπτομέρειες σχετικά με το τι είναι η ταυτότητα τροφοδοσίας σας, τον αριθμό των χρηστών και τον πράκτορα χρήστη. Ωστόσο, αυτό είναι επίσης ένα πρόβλημα, καθώς σημαίνει ότι Umami θα αγνοήσει το αίτημα? στην πραγματικότητα θα επιστρέψει μια κατάσταση 200 BUT the content contains `{"beep": "boop"}` που σημαίνει ότι αυτό αναγνωρίζεται ως αίτημα bot. Αυτό είναι ενοχλητικό καθώς δεν μπορώ να το χειριστώ αυτό μέσω του κανονικού χειρισμού σφαλμάτων (είναι 200, δεν λέω ένα 403 κ.λπ.).

# Η Λύση

Λοιπόν, ποια είναι η λύση σε αυτό; Δεν μπορώ να αναλύσω χειροκίνητα όλα αυτά τα αιτήματα και να ανιχνεύσω αν η Umami θα τα εντοπίσει ως ρομπότ, χρησιμοποιεί το IsBot (https://www.npmjs.com/package/isbot) για να ανιχνεύσει αν ένα αίτημα είναι bot ή όχι. Δεν υπάρχει ισοδύναμο C# και είναι μια αλλαγμένη λίστα έτσι δεν μπορώ καν να χρησιμοποιήσω αυτή τη λίστα (στο μέλλον μπορεί να γίνω έξυπνος και να χρησιμοποιήσω τη λίστα για να ανιχνεύσω αν ένα αίτημα είναι ένα ρομπότ ή όχι).
Οπότε πρέπει να αναχαιτίσω το αίτημα πριν φτάσει στο Umami και να αλλάξω τον Πράκτορα Χρήστη σε κάτι που ο Umami θα δεχτεί για συγκεκριμένα αιτήματα.

Έτσι τώρα πρόσθεσα μερικές πρόσθετες παραμέτρους στις μεθόδους εντοπισμού μου στο Umami.Net. Αυτά σας επιτρέπουν να καθορίσετε το νέο 'Προκαθορισμένο Πράκτορα χρήστη' θα σταλεί στο Umami αντί του αρχικού Πράκτορα χρήστη. Αυτό μου επιτρέπει να διευκρινίσω ότι ο Πράκτορας Χρήστης θα πρέπει να αλλάξει σε μια συγκεκριμένη τιμή για συγκεκριμένα αιτήματα.

## Οι Μέθοδοι

Πάνω μου... `UmamiBackgroundSender` Πρόσθεσα τα εξής:

```csharp
   public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
```

Αυτό υπάρχει σε όλες τις μεθόδους παρακολούθησης εκεί και απλά θέτει μια παράμετρο για την `UmamiPayload` αντικείμενο.

OnName `UmamiClient` μπορούν να καθοριστούν ως εξής:

```csharp
    [Fact]
    public async Task TrackPageView_WithUrl()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.TrackPageViewAndDecode("https://example.com", "Example Page",
            new UmamiPayload { UseDefaultUserAgent = true });
        Assert.NotNull(response);
        Assert.Equal(UmamiDataResponse.ResponseStatus.Success, response.Status);
    }
```

Σε αυτή τη δοκιμή χρησιμοποιώ το νέο `TrackPageViewAndDecode` μέθοδος που επιστρέφει a `UmamiDataResponse` αντικείμενο. Αυτό το αντικείμενο περιέχει αποκωδικοποιημένο σήμα JWT (το οποίο είναι άκυρο εάν είναι ένα bot έτσι ώστε να είναι χρήσιμο να ελεγχθεί) και την κατάσταση του αιτήματος.

## `PayloadService`

Όλα αυτά είναι υπό έλεγχο. `Payload` Υπηρεσία η οποία είναι υπεύθυνη για τον πληθυσμό του αντικειμένου ωφέλιμο φορτίο. Εδώ είναι που... `UseDefaultUserAgent` είναι έτοιμος.

Από προεπιλογή Έχω κατοικήσει το ωφέλιμο φορτίο από το `HttpContext` Έτσι συνήθως παίρνεις αυτό το σύνολο σωστά? Θα δείξω αργότερα όπου αυτό τράβηξε πίσω από Umami.

```csharp
    private UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var request = httpContext?.Request;

        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Data = data,
            Url = url ?? httpContext?.Request?.Path.Value,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = request?.Headers["User-Agent"].FirstOrDefault(),
            Referrer = request?.Headers["Referer"].FirstOrDefault(),
            Hostname = request?.Host.Host
        };

        return payload;
    }
```

Τότε έχω ένα κομμάτι κώδικα που λέγεται `PopulateFromPayload` που είναι όπου το αντικείμενο αίτησης παίρνει τα δεδομένα του που έχουν συσταθεί:

```csharp
    public static string DefaultUserAgent =>
        $"Mozilla/5.0 (Windows 11)  Umami.Net/{Assembly.GetAssembly(typeof(UmamiClient))!.GetName().Version}";

    public UmamiPayload PopulateFromPayload(UmamiPayload? payload, UmamiEventData? data)
    {
        var newPayload = GetPayload(data: data);
        ...
        
        newPayload.UserAgent = payload.UserAgent ?? DefaultUserAgent;

        if (payload.UseDefaultUserAgent)
        {
            var userData = newPayload.Data ?? new UmamiEventData();
            userData.TryAdd("OriginalUserAgent", newPayload.UserAgent ?? "");
            newPayload.UserAgent = DefaultUserAgent;
            newPayload.Data = userData;
        }


        logger.LogInformation("Using UserAgent: {UserAgent}", newPayload.UserAgent);
     }        
        
```

Θα δείτε ότι αυτό ορίζει ένα νέο πράκτορα χρήστη στην κορυφή του αρχείου (το οποίο έχω επιβεβαιώσει δεν είναι *προς το παρόν* Ανιχνεύθηκε ως ρομπότ). Στη συνέχεια, στη μέθοδο ανιχνεύει είτε το UserAgent είναι κενό (το οποίο δεν θα πρέπει να συμβεί εκτός αν καλείται από τον κώδικα χωρίς ένα HttpContext) ή αν το `UseDefaultUserAgent` είναι έτοιμος. Εάν είναι τότε θέτει το UserAgent στην προεπιλογή και προσθέτει το αρχικό UserAgent στο αντικείμενο δεδομένων.

Αυτό είναι στη συνέχεια συνδεδεμένοι έτσι μπορείτε να δείτε τι UserAgent χρησιμοποιείται.

## Αποκωδικοποιώ την απάντηση.

Στο Umami.Net 0.3.0 Πρόσθεσα μια σειρά από νέες μεθόδους "AndDecode" που επιστρέφουν `UmamiDataResponse` αντικείμενο. Αυτό το αντικείμενο περιέχει το αποκωδικοποιημένο σήμα JWT.

```csharp
    public async Task<UmamiDataResponse?> TrackPageViewAndDecode(
        string? url = "",
        string? title = "",
        UmamiPayload? payload = null,
        UmamiEventData? eventData = null)
    {
        var response = await TrackPageView(url, title, payload, eventData);
        return await DecodeResponse(response);
    }
    
        private async Task<UmamiDataResponse?> DecodeResponse(HttpResponseMessage responseMessage)
    {
        var responseString = await responseMessage.Content.ReadAsStringAsync();

        switch (responseMessage.IsSuccessStatusCode)
        {
            case false:
                return new UmamiDataResponse(UmamiDataResponse.ResponseStatus.Failed);
            case true when responseString.Contains("beep") && responseString.Contains("boop"):
                logger.LogWarning("Bot detected data not stored in Umami");
                return new UmamiDataResponse(UmamiDataResponse.ResponseStatus.BotDetected);

            case true:
                var decoded = await jwtDecoder.DecodeResponse(responseString);
                if (decoded == null)
                {
                    logger.LogError("Failed to decode response from Umami");
                    return null;
                }

                var payload = UmamiDataResponse.Decode(decoded);

                return payload;
        }
    }
```

Μπορείς να δεις ότι αυτό έρχεται στο φυσιολογικό. `TrackPageView` μέθοδος στη συνέχεια καλεί μια μέθοδο που ονομάζεται `DecodeResponse` που ελέγχει την απάντηση για την `beep` και `boop` συμβολοσειρές (για ανίχνευση bot). Αν τους βρει τότε καταγράφει μια προειδοποίηση και επιστρέφει ένα `BotDetected` Κατάσταση. Αν δεν τους βρει, τότε αποκωδικοποιεί το σήμα JWT και επιστρέφει το ωφέλιμο φορτίο.

Το ίδιο το σύμβολο JWT είναι απλά μια κωδικοποιημένη συμβολοσειρά Base64 που περιέχει τα δεδομένα που έχει αποθηκεύσει ο Umami. Αυτό είναι αποκωδικοποιημένο και επιστρέφεται ως ένα `UmamiDataResponse` αντικείμενο.

Η πλήρης πηγή για αυτό είναι παρακάτω:

<details>
<summary>Response Decoder</summary>

```csharp
using System.IdentityModel.Tokens.Jwt;

namespace Umami.Net.Models;

public class UmamiDataResponse
{
    public enum ResponseStatus
    {
        Failed,
        BotDetected,
        Success
    }

    public UmamiDataResponse(ResponseStatus status)
    {
        Status = status;
    }

    public ResponseStatus Status { get; set; }

    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public string? Hostname { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Screen { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? Subdivision1 { get; set; }
    public string? Subdivision2 { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public long Iat { get; set; }

    public static UmamiDataResponse Decode(JwtPayload? payload)
    {
        if (payload == null) return new UmamiDataResponse(ResponseStatus.Failed);
        payload.TryGetValue("visitId", out var visitIdObj);
        payload.TryGetValue("iat", out var iatObj);
        //This should only happen then the payload is dummy.
        if (payload.Count == 2)
        {
            var visitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty;
            var iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0;

            return new UmamiDataResponse(ResponseStatus.Success)
            {
                VisitId = visitId,
                Iat = iat
            };
        }

        payload.TryGetValue("id", out var idObj);
        payload.TryGetValue("websiteId", out var websiteIdObj);
        payload.TryGetValue("hostname", out var hostnameObj);
        payload.TryGetValue("browser", out var browserObj);
        payload.TryGetValue("os", out var osObj);
        payload.TryGetValue("device", out var deviceObj);
        payload.TryGetValue("screen", out var screenObj);
        payload.TryGetValue("language", out var languageObj);
        payload.TryGetValue("country", out var countryObj);
        payload.TryGetValue("subdivision1", out var subdivision1Obj);
        payload.TryGetValue("subdivision2", out var subdivision2Obj);
        payload.TryGetValue("city", out var cityObj);
        payload.TryGetValue("createdAt", out var createdAtObj);

        return new UmamiDataResponse(ResponseStatus.Success)
        {
            Id = idObj != null ? Guid.Parse(idObj.ToString()!) : Guid.Empty,
            WebsiteId = websiteIdObj != null ? Guid.Parse(websiteIdObj.ToString()!) : Guid.Empty,
            Hostname = hostnameObj?.ToString(),
            Browser = browserObj?.ToString(),
            Os = osObj?.ToString(),
            Device = deviceObj?.ToString(),
            Screen = screenObj?.ToString(),
            Language = languageObj?.ToString(),
            Country = countryObj?.ToString(),
            Subdivision1 = subdivision1Obj?.ToString(),
            Subdivision2 = subdivision2Obj?.ToString(),
            City = cityObj?.ToString(),
            CreatedAt = createdAtObj != null ? DateTime.Parse(createdAtObj.ToString()!) : DateTime.MinValue,
            VisitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty,
            Iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0
        };
    }
}
```

</details>
Μπορείτε να δείτε ότι αυτό περιέχει ένα μάτσο χρήσιμες πληροφορίες σχετικά με το αίτημα που έχει αποθηκεύσει ο Umami. Αν θέλετε για παράδειγμα να δείξετε διαφορετικό περιεχόμενο με βάση την τοποθεσία, τη γλώσσα, το πρόγραμμα περιήγησης κ.λπ. αυτό σας επιτρέπει να το κάνετε.

```csharp
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public string? Hostname { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Screen { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? Subdivision1 { get; set; }
    public string? Subdivision2 { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public long Iat { get; set; }
```

# Συμπέρασμα

Έτσι, απλά μια σύντομη θέση που καλύπτει κάποια νέα λειτουργικότητα στο Umami.Net 0.4.0 που σας επιτρέπει να καθορίσετε ένα προκαθορισμένο Πράκτορα χρήστη για συγκεκριμένα αιτήματα. Αυτό είναι χρήσιμο για την παρακολούθηση αιτημάτων που Umami διαφορετικά θα αγνοήσει.