# Προσθήκη πελάτη παρακολούθησης Umami

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-27T02:00</datetime>

# Εισαγωγή

Σε μια [παλαιότερη θέση](/blog/addingumamitrackingclient.md) Σχεδίασα πώς ένας πελάτης εντοπισμού για Umami σε C# θα μπορούσε να λειτουργήσει.
Λοιπόν, επιτέλους είχα την ευκαιρία να το δοκιμάσω εκτενώς και να βελτιώσω την λειτουργία του (ναι, άλλη μια φορά) `IHostedService`).

[TOC]

# Quirks of the Umami API

Το Umami Tracking API είναι τόσο πολύ πειθαρχημένο όσο και πολύ terse. Έτσι έπρεπε να ενημερώσω τον κωδικό του πελάτη για να χειριστώ τα ακόλουθα:

1. Το API αναμένει μια "πραγματική" συμβολοσειρά χρήστη-προμηθευτή. Έτσι έπρεπε να ενημερώσω τον πελάτη για να χρησιμοποιήσει μια πραγματική συμβολοσειρά User-Agent (ή για να είμαι πιο ακριβής κατέγραψα μια πραγματική συμβολοσειρά User-Agent από ένα πρόγραμμα περιήγησης και το χρησιμοποίησα αυτό).
2. Το API αναμένει ότι είναι είσοδο JSON σε μια πολύ συγκεκριμένη μορφή? κενό συμβολοσειρές δεν επιτρέπονται. Έπρεπε να ενημερώσω τον πελάτη για να το χειριστεί.
3. Η [Πελάτης του κόμβου API](https://github.com/umami-software/node) έχει λίγο περίεργη επιφάνεια. Δεν είναι αμέσως σαφές τι αναμένει το API. Έτσι έπρεπε να κάνω λίγη δίκη και λάθος για να το κάνω να δουλέψει.

## Ο Πελάτης του κόμβου API

Ο πελάτης του κόμβου API συνολικά είναι κάτω, είναι εξαιρετικά ευέλικτος αλλά πραγματικά δεν είναι καλά τεκμηριωμένος.

```javascript
export interface UmamiOptions {
  hostUrl?: string;
  websiteId?: string;
  sessionId?: string;
  userAgent?: string;
}

export interface UmamiPayload {
  website: string;
  session?: string;
  hostname?: string;
  language?: string;
  referrer?: string;
  screen?: string;
  title?: string;
  url?: string;
  name?: string;
  data?: {
    [key: string]: string | number | Date;
  };
}

export interface UmamiEventData {
  [key: string]: string | number | Date;
}

export class Umami {
  options: UmamiOptions;
  properties: object;

  constructor(options: UmamiOptions = {}) {
    this.options = options;
    this.properties = {};
  }

  init(options: UmamiOptions) {
    this.options = { ...this.options, ...options };
  }

  send(payload: UmamiPayload, type: 'event' | 'identify' = 'event') {
    const { hostUrl, userAgent } = this.options;

    return fetch(`${hostUrl}/api/send`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': userAgent || `Mozilla/5.0 Umami/${process.version}`,
      },
      body: JSON.stringify({ type, payload }),
    });
  }

  track(event: object | string, eventData?: UmamiEventData) {
    const type = typeof event;
    const { websiteId } = this.options;

    switch (type) {
      case 'string':
        return this.send({
          website: websiteId,
          name: event as string,
          data: eventData,
        });
      case 'object':
        return this.send({ website: websiteId, ...(event as UmamiPayload) });
    }

    return Promise.reject('Invalid payload.');
  }

  identify(properties: object = {}) {
    this.properties = { ...this.properties, ...properties };
    const { websiteId, sessionId } = this.options;

    return this.send(
      { website: websiteId, session: sessionId, data: { ...this.properties } },
      'identify',
    );
  }

  reset() {
    this.properties = {};
  }
}

const umami = new Umami();

export default umami;
```

Όπως βλέπετε, εκθέτει τις ακόλουθες μεθόδους:

1. `init` - Για να ρυθμίσω τις επιλογές.
2. `send` - Να στείλω το φορτίο.
3. `track` - Για να εντοπίσουμε ένα γεγονός.
4. `identify` - Για να αναγνωρίσω έναν χρήστη.
5. `reset` - Για να επαναφέρουμε τις ιδιοκτησίες.

Ο πυρήνας αυτού είναι η `send` μέθοδος που αποστέλλει το ωφέλιμο φορτίο στο API.

```javascript
  send(payload: UmamiPayload, type: 'event' | 'identify' = 'event') {
    const { hostUrl, userAgent } = this.options;

    return fetch(`${hostUrl}/api/send`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': userAgent || `Mozilla/5.0 Umami/${process.version}`,
      },
      body: JSON.stringify({ type, payload }),
    });
  }
```

# Πελάτης C#

Για να ξεκινήσω με αντιγράφω λίγο πολύ τον πελάτη του κόμβου API `UmamiOptions` και `UmamiPayload` μαθήματα (Δεν θα τα περάσω και πάλι είναι μεγάλα).

Οπότε τώρα... `Send` Η μέθοδος μοιάζει κάπως έτσι:

```csharp
     public async Task<HttpResponseMessage> Send(UmamiPayload? payload=null, UmamiEventData? eventData =null,  string type = "event")
        {
            var websiteId = settings.WebsiteId;
             payload = PopulateFromPayload(websiteId, payload, eventData);
            
            var jsonPayload = new { type, payload };
            logger.LogInformation("Sending data to Umami: {Payload}", JsonSerializer.Serialize(jsonPayload, options));

            var response = await client.PostAsJsonAsync("api/send", jsonPayload, options);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to send data to Umami: {StatusCode}, {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Successfully sent data to Umami: {StatusCode}, {ReasonPhrase}, {Content}", response.StatusCode, response.ReasonPhrase, content);
            }

            return response;
        }

```

Υπάρχουν δύο κρίσιμα μέρη εδώ:

1. Η `PopulateFromPayload` μέθοδος η οποία συγκεντρώνει το ωφέλιμο φορτίο με τον ιστότοποId και το eventData.
2. Η serialization JSON του ωφέλιμου φορτίου, πρέπει να αποκλείσει τις μηδενικές τιμές.

## Η `PopulateFromPayload` Μέθοδος

```csharp
        public static UmamiPayload PopulateFromPayload(string webSite, UmamiPayload? payload, UmamiEventData? data)
        {
            var newPayload = GetPayload(webSite, data: data);
            if(payload==null) return newPayload;
            if(payload.Hostname != null)
                newPayload.Hostname = payload.Hostname;
            if(payload.Language != null)
                newPayload.Language = payload.Language;
            if(payload.Referrer != null)
                newPayload.Referrer = payload.Referrer;
            if(payload.Screen != null)
                newPayload.Screen = payload.Screen;
            if(payload.Title != null)
                newPayload.Title = payload.Title;
            if(payload.Url != null)
                newPayload.Url = payload.Url;
            if(payload.Name != null)
                newPayload.Name = payload.Name;
            if(payload.Data != null)
                newPayload.Data = payload.Data;
            return newPayload;          
        }
        
        private static UmamiPayload GetPayload(string websiteId, string? url = null, UmamiEventData? data = null)
        {
            var payload = new UmamiPayload
            {
            Website = websiteId,
                Data = data,
                Url = url ?? string.Empty
            };
            

            return payload;
        }

```

Μπορείς να δεις ότι πάντα φροντίζουμε... `websiteId` έχει οριστεί και ορίζουμε τις άλλες τιμές μόνο αν δεν είναι άκυρες. Αυτό μας δίνει ευελιξία εις βάρος της verbosity.

## Η ρύθμιση HttpClient

Όπως αναφέρθηκε πριν πρέπει να δώσουμε μια κάπως πραγματική συμβολοσειρά χρήστη-προμηθευτή στο API. Αυτό γίνεται στο `HttpClient` Στήσιμο.

```csharp
              services.AddHttpClient<UmamiClient>((serviceProvider, client) =>
            {
                 umamiSettings = serviceProvider.GetRequiredService<UmamiClientSettings>();
            client.DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
        }).SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy())
       #if DEBUG 
        .AddLogger<HttpLogger>();
        #else
        ;
        #endif

```

## Υπηρεσία υποβάθρου

Αυτό είναι άλλο ένα. `IHostedService`, υπάρχουν ένα μάτσο άρθρα σχετικά με το πώς να εγκαταστήσετε αυτά τα επάνω έτσι δεν θα πάω σε αυτό εδώ (δοκιμάστε το μπαρ αναζήτησης!).

Το μόνο σημείο πόνου ήταν η χρήση της ένεσης `HttpClient` στην `UmamiClient` Μαθήματα. Λόγω της βαθμολογίας του πελάτη & την υπηρεσία που χρησιμοποίησα `IServiceScopeFactory` εγχέεται στον κατασκευαστή του HostedService στη συνέχεια αρπάξτε το για κάθε αίτηση αποστολής.

```csharp
    

    private async Task SendRequest(CancellationToken token)
    {
        logger.LogInformation("Umami background delivery started");

        while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
                try
                {
                   using  var scope = scopeFactory.CreateScope();
                    var client = scope.ServiceProvider.GetRequiredService<UmamiClient>();
                    // Send the event via the client
                    await client.Send(payload.Payload);

                    logger.LogInformation("Umami background event sent: {EventType}", payload.EventType);
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Umami background delivery canceled.");
                    return; // Exit the loop on cancellation
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending Umami background event.");
                }
            }
        }
    }
   
```

### Χρήση της Φιλοξενούμενης Υπηρεσίας

Τώρα που έχουμε αυτή την υπηρεσία φιλοξενίας, μπορούμε να βελτιώσουμε δραματικά την απόδοση στέλνοντας τα γεγονότα στο παρασκήνιο.

Έχω χρησιμοποιήσει αυτό σε μερικά διαφορετικά μέρη, στο δικό μου `Program.cs` Αποφάσισα να πειραματιστώ με την παρακολούθηση του αιτήματος RSS feed χρησιμοποιώντας το Middleware, απλά ανιχνεύει οποιοδήποτε μονοπάτι τελειώνει στο 'RSS' και στέλνει ένα background event.

```csharp
app.Use( async (context, next) =>
{
var path = context.Request.Path.Value;
if (path.EndsWith("RSS", StringComparison.OrdinalIgnoreCase))
{
var rss = context.RequestServices.GetRequiredService<UmamiBackgroundSender>();
// Send the event in the background
await rss.SendBackground(new UmamiPayload(){Url  = path, Name = "RSS Feed"});
}
await next();
});
```

Έχω επίσης περάσει περισσότερα δεδομένα από μου `TranslateAPI` τελικό σημείο.
Κάτι που μου επιτρέπει να δω πόσο καιρό οι μεταφράσεις λαμβάνουν; Σημειώστε ότι καμία από αυτές δεν μπλοκάρει το κύριο νήμα Ή παρακολουθεί τους μεμονωμένους χρήστες.

```csharp
    
       await  umamiClient.SendBackground(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
        var result = new TranslateResultTask(translationTask, true);
```

# Συμπέρασμα

Το Umami API είναι λίγο ιδιότροπο αλλά είναι ένας πολύ καλός τρόπος για να παρακολουθείτε τα γεγονότα με αυτο-ξεχωριστό τρόπο. Ας ελπίσουμε ότι θα έχω την ευκαιρία να το καθαρίσω ακόμα περισσότερο και να πάρω ένα πακέτο Ουμάμι Νιούγκετ εκεί έξω.
Επιπλέον από ένα [προηγούμενο άρθρο](/blog/addingascsharpclientforumamiapi)  Θέλω να πάρω πίσω τα δεδομένα από το Umami για να παρέχω χαρακτηριστικά όπως η διαλογή δημοτικότητας.