# Umami-Tracking-Client nach oben hinzufügen

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-27T02:00</datetime>

# Einleitung

In einem [früherer Posten](/blog/addingumamitrackingclient.md) Ich habe skizziert, wie ein Tracking Client für Umami in C# funktionieren könnte.
Nun, ich hatte endlich eine Chance, es ausgiebig zu testen und zu verbessern, es ist Betrieb (ja ANOTHER `IHostedService`).

[TOC]

# Quirks der Umami API

Die Umami Tracking API ist sowohl sehr oppositioniert als auch sehr knapp. Also musste ich den Client-Code aktualisieren, um folgendes zu handhaben:

1. Die API erwartet einen'realen' aussehenden User-Agent String. Also musste ich den Client aktualisieren, um einen echten User-Agent String zu verwenden (oder um genauer zu sein, habe ich einen echten User-Agent String aus einem Browser aufgenommen und diesen benutzt).
2. Die API erwartet eine JSON-Eingabe in einem ganz bestimmten Format; leere Strings sind nicht erlaubt. Also musste ich den Kunden aktualisieren, um das zu regeln.
3. Das [Knoten API-Client](https://github.com/umami-software/node) hat ein wenig eine ungerade Oberfläche. Es ist nicht sofort klar, was die API erwartet. Also musste ich ein wenig versuchen und Fehler machen, um es funktionieren zu lassen.

## Der Knoten API Client

Der Node API Client insgesamt ist unten, es ist super flexibel, aber WIRKLICH nicht gut dokumentiert.

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

Wie Sie sehen, entlarven Sie die folgenden Methoden:

1. `init` - Um die Optionen zu setzen.
2. `send` - Um die Nutzlast zu senden.
3. `track` - Um ein Ereignis zu verfolgen.
4. `identify` - Um einen Benutzer zu identifizieren.
5. `reset` - Um die Eigenschaften zurückzusetzen.

Der Kern von diesem ist die `send` Methode, die die Nutzlast an die API sendet.

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

# Der C#-Client

Um mit zu beginnen, kopierte ich ziemlich viel die Node API Clients `UmamiOptions` und `UmamiPayload` Unterricht (Ich werde nicht an ihnen wieder vorbei, sie sind groß).

So jetzt meine `Send` Methode sieht so aus:

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

Hier gibt es zwei kritische Teile:

1. Das `PopulateFromPayload` Methode, die die Nutzlast mit der Website Id und der eventData bevölkert.
2. Die JSON Serialisierung der Nutzlast, sie muss Nullwerte ausschließen.

## Das `PopulateFromPayload` Verfahren

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

Sie können sehen, dass wir immer sicherstellen, dass die `websiteId` ist gesetzt und wir setzen die anderen Werte nur, wenn sie nicht null sind. Das gibt uns Flexibilität auf Kosten einer gewissen Verbosität.

## Das HttpClient Setup

Wie bereits erwähnt, müssen wir der API einen etwas realen User-Agent String geben. Dies geschieht in der `HttpClient` Einrichtung.

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

## Hintergrunddienst

Das ist noch eine andere. `IHostedService`, es gibt eine Reihe von Artikeln, wie man diese aufstellt, so dass ich nicht in sie hier gehen ( versuchen Sie die Suchleiste!)== Einzelnachweise ==

Der einzige Schmerzpunkt war die Anwendung der injizierten `HttpClient` in der `UmamiClient` Unterricht. Durch das Scoping des Clients und des Services habe ich einen `IServiceScopeFactory` in den Konstrukteur des HostedService eingespritzt dann greifen Sie es für jede Anfrage senden.

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

### Nutzung des Hosted Service

Jetzt, da wir diesen gehosteten Service haben, können wir die Leistung drastisch verbessern, indem wir die Ereignisse im Hintergrund senden.

Ich habe dies an einigen verschiedenen Orten, in meinem `Program.cs` Ich entschied mich, mit der Verfolgung der RSS-Feed-Anfrage mit Middleware zu experimentieren, es erkennt einfach jeden Pfad, der in 'RSS' endet und sendet ein Hintergrundereignis.

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

Ich habe auch mehr Daten aus meinem `TranslateAPI` Endpunkt.
Was mir erlaubt zu sehen, wie lange Übersetzungen nehmen; beachten Sie, dass keines davon den Hauptthread oder das Tracking einzelner Benutzer blockiert.

```csharp
    
       await  umamiClient.SendBackground(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
        var result = new TranslateResultTask(translationTask, true);
```

# Schlussfolgerung

Die Umami API ist etwas skurril, aber es ist eine großartige Möglichkeit, Ereignisse auf eine selbstgehostete Weise zu verfolgen. Hoffentlich bekomme ich eine Chance, es noch mehr aufzuräumen und ein Umami Nuget-Paket da draußen zu bekommen.
Darüber hinaus von einem [früherer Artikel](/blog/addingascsharpclientforumamiapi)  Ich möchte Daten zurück aus Umami ziehen, um Funktionen wie Popularität Sortierung zur Verfügung zu stellen.