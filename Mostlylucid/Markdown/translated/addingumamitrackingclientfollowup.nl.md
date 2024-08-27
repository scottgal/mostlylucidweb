# Umami tracking-client toevoegen Follow-up

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-27T02:00</datetime>

# Inleiding

In een [eerdere post](/blog/addingumamitrackingclient.md) Ik schetste hoe een Tracking Client voor Umami in C# zou kunnen werken.
Nou, ik heb eindelijk de kans gehad om het uitgebreid te testen en de werking ervan te verbeteren (ja ANOTHER `IHostedService`).

[TOC]

# Quirks van de Umami API

De Umami Tracking API is zowel zeer eigenzinnig als zeer terse. Dus moest ik de clientcode updaten om het volgende af te handelen:

1. De API verwacht een'real' looking User-Agent string. Dus moest ik de client updaten om een echte User-Agent string te gebruiken (of om preciezer te zijn heb ik een echte User-Agent string van een browser gevangen en gebruikt).
2. De API verwacht JSON-invoer in een zeer specifiek formaat; lege tekenreeksen zijn niet toegestaan. Dus moest ik de klant updaten om dit af te handelen.
3. De [Knooppunt API-client](https://github.com/umami-software/node) heeft een beetje een vreemd oppervlak. Het is niet meteen duidelijk wat de API verwacht. Dus ik moest een beetje trial en error doen om het te laten werken.

## De Knooppunt API-client

De Node API client in totaal is hieronder, het is super flexibel maar ECHT niet goed gedocumenteerd.

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

Zoals u ziet onthult het de volgende methoden:

1. `init` - Om de opties in te stellen.
2. `send` - Om de lading te sturen.
3. `track` - Om een evenement op te sporen.
4. `identify` - Om een gebruiker te identificeren.
5. `reset` - Om de eigendommen te resetten.

De kern hiervan is de `send` methode die de lading naar de API stuurt.

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

# De C#-client

Om te beginnen heb ik bijna gekopieerd de Node API client's `UmamiOptions` en `UmamiPayload` lessen (ik zal ze niet meer passeren ze zijn groot).

Dus nu mijn `Send` methode ziet er als volgt uit:

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

Er zijn hier twee kritieke delen:

1. De `PopulateFromPayload` methode die de lading bevolkt met de websiteId en de eventData.
2. De JSON-serialisatie van de lading, het moet nul waarden uitsluiten.

## De `PopulateFromPayload` Methode

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

U kunt zien dat wij er altijd voor zorgen dat de `websiteId` is ingesteld en we stellen de andere waarden alleen in als ze niet nul zijn. Dit geeft ons flexibiliteit ten koste van een beetje verbosheid.

## De HttpClient-instellingen

Zoals eerder vermeld moeten we een enigszins echte User-Agent string aan de API geven. Dit wordt gedaan in de `HttpClient` Installeren.

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

## Achtergronddienst

Dit is nog een andere `IHostedService`, er zijn een heleboel artikelen over hoe deze op te zetten, zodat ik zal niet gaan in het hier (probeer de zoekbalk!).

Het enige pijnpunt was het gebruik van de geïnjecteerde `HttpClient` in de `UmamiClient` Klas. Vanwege het scopen van de client & de dienst die ik gebruikte een `IServiceScopeFactory` geïnjecteerd in de constructeur van de HostedService dan pak het voor elke verzendaanvraag.

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

### Gebruik van de Hosted Service

Nu we deze gehoste service hebben, kunnen we de prestaties drastisch verbeteren door de gebeurtenissen op de achtergrond te sturen.

Ik heb dit gebruikt op een paar verschillende plaatsen, in mijn `Program.cs` Ik besloot om te experimenteren met het volgen van de RSS feed verzoek met behulp van Middleware, het detecteert gewoon elk pad eindigend in 'RSS' en stuurt een achtergrond gebeurtenis.

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

Ik heb ook meer gegevens van mijn `TranslateAPI` eindpunt.
Dat stelt me in staat om te zien hoe lang vertalingen nemen; Merk op dat geen van deze blokkeren de belangrijkste draad of het bijhouden van individuele gebruikers.

```csharp
    
       await  umamiClient.SendBackground(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
        var result = new TranslateResultTask(translationTask, true);
```

# Conclusie

De Umami API is een beetje eigenzinnig maar het is een geweldige manier om gebeurtenissen op een zelf-gehoste manier te volgen. Hopelijk krijg ik de kans om het nog meer op te ruimen en een Umami nuget pakket te krijgen.
Bovendien van een [Vorig artikel](/blog/addingascsharpclientforumamiapi)  Ik wil data terughalen uit Umami om functies te bieden zoals populariteit sorteren.