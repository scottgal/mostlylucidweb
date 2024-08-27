# Lägga till Umami Tracking Client uppföljning

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-27T02:00</datetime>

# Inledning

I en [tidigare inlägg](/blog/addingumamitrackingclient.md) Jag skissade upp hur en spårningsklient för Umami i C# kunde fungera.
Jag har äntligen fått en chans att testa den grundligt och förbättra dess funktion (ja ANothER `IHostedService`).

[TOC]

# Fråga på Umami API:et

Umami Tracking API är både mycket åskådning och mycket terse. Så jag var tvungen att uppdatera klientkoden för att hantera följande:

1. API:et förväntar sig en "riktig" utseende User-Agent sträng. Så jag var tvungen att uppdatera klienten för att använda en riktig User-Agent sträng (eller för att vara mer exakt jag fångade en riktig User-Agent sträng från en webbläsare och använde det).
2. API förväntar sig att det är JSON indata i ett mycket speciellt format; tomma strängar är inte tillåtna. Så jag var tvungen att uppdatera klienten för att hantera detta.
3. I detta sammanhang är det viktigt att se till att [Node API-klient](https://github.com/umami-software/node) har lite av en udda yta. Det är inte omedelbart klart vad API förväntar sig. Så jag var tvungen att göra lite försök och fel för att få det att fungera.

## Node API-klienten

Node API-klienten totalt är nedan, det är super flexibel men verkligen inte väl dokumenterad.

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

Som ni ser exponerar det följande metoder:

1. `init` - För att ställa in alternativen.
2. `send` - Skicka nyttolasten.
3. `track` - För att spåra en händelse.
4. `identify` - För att identifiera en användare.
5. `reset` - För att återställa fastigheterna.

Kärnan i detta är `send` metod som skickar nyttolasten till API:et.

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

# C#-klienten

Till att börja med kopierade jag i stort sett Node API-klientens `UmamiOptions` och `UmamiPayload` klasser (jag kommer inte förbi dem igen de är stora).

Så nu är jag min `Send` metoden ser ut så här:

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

Det finns två kritiska delar här:

1. I detta sammanhang är det viktigt att se till att `PopulateFromPayload` Metod som fyller nyttolasten med websiteId och eventData.
2. JSON serialisering av nyttolasten, det måste utesluta noll värden.

## I detta sammanhang är det viktigt att se till att `PopulateFromPayload` Metod

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

Du kan se att vi alltid ser till att `websiteId` är inställd och vi ställer bara in de andra värdena om de inte är ogiltiga. Detta ger oss flexibilitet på bekostnad av lite verbositet.

## Inställning av HttpClient

Som tidigare nämnts måste vi ge en något verklig User-Agent sträng till API. Detta görs i `HttpClient` Uppställning.

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

## Bakgrundstjänst

Detta är ännu en `IHostedService`, Det finns en massa artiklar om hur man ställer upp dessa så jag inte går in i det här (försök sökfältet!)..............................................................................................

Den enda smärtpunkten var att använda den injicerade dosen. `HttpClient` I bilaga I till förordning (EU) nr 1094/2010 ska följande punkt läggas till: `UmamiClient` Klassen. På grund av scoping av klienten & tjänsten jag använde en `IServiceScopeFactory` injiceras i konstruktören av HostedService sedan ta den för varje skicka begäran.

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

### Använda värdtjänst

Nu när vi har denna värdtjänst, kan vi dramatiskt förbättra prestandan genom att skicka händelserna i bakgrunden.

Jag har använt detta på ett par olika ställen, på mina `Program.cs` Jag bestämde mig för att experimentera med att spåra RSS-flöde begäran med Middleware, det bara upptäcker alla sökvägar som slutar i 'RSS' och skickar en bakgrundshändelse.

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

Jag har också passerat mer data från min `TranslateAPI` ändpunkt.
Vilket gör att jag kan se hur länge översättningar tar; notera att ingen av dessa blockerar huvudtråden ELLER spårning enskilda användare.

```csharp
    
       await  umamiClient.SendBackground(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
        var result = new TranslateResultTask(translationTask, true);
```

# Slutsatser

Umami API är lite udda men det är ett bra sätt att spåra händelser på ett självupptaget sätt. Förhoppningsvis får jag en chans att städa upp det ännu mer och få ett Umami nugget paket där ute.
Dessutom från en [tidigare artikel](/blog/addingascsharpclientforumamiapi)  Jag vill dra data tillbaka från Umami för att ge funktioner som popularitet sortering.