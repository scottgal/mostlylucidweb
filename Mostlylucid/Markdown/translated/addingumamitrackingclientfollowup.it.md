# Aggiunta del follow-up del client di monitoraggio Umami

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-27T02:00</datetime>

# Introduzione

In un [posto precedente](/blog/addingumamitrackingclient.md) Ho abbozzato come un Cliente di Tracciamento per Umami in C# potrebbe funzionare.
Beh, ho finalmente avuto la possibilità di testarlo ampiamente e migliorare il suo funzionamento (sì UN'ALTRA) `IHostedService`).

[TOC]

# Quirks of the Umami API

L'API Umami Tracking è molto apprezzata e molto terse. Quindi ho dovuto aggiornare il codice client per gestire quanto segue:

1. L'API si aspetta una stringa User-Agent'reale'. Quindi ho dovuto aggiornare il client per usare una vera stringa User-Agent (o per essere più precisi ho catturato una vera stringa User-Agent da un browser e l'ho usata).
2. L'API si aspetta che sia in ingresso JSON in un formato molto particolare; le stringhe vuote non sono permesse. Quindi ho dovuto aggiornare il cliente per occuparmene.
3. La [Client API nodo](https://github.com/umami-software/node) ha una superficie un po' strana. Non è immediatamente chiaro cosa si aspetta l'API. Così ho dovuto fare un po 'di prova e di errore per farlo funzionare.

## Il client API del nodo

Il client Nodo API in totale è sotto, è super flessibile, ma Davvero non ben documentato.

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

Come vedete esporre i seguenti metodi:

1. `init` - Per impostare le opzioni.
2. `send` - Per mandare il carico.
3. `track` - Per rintracciare un evento.
4. `identify` - Per identificare un utente.
5. `reset` - Per resettare le proprieta'.

Il nucleo di questo è il `send` metodo che invia il carico utile all'API.

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

# Il client C#

Per cominciare ho copiato praticamente il client delle API Node `UmamiOptions` e `UmamiPayload` classi (non li supererò di nuovo sono grandi).

Così ora il mio `Send` metodo assomiglia a questo:

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

Qui ci sono due parti critiche:

1. La `PopulateFromPayload` metodo che popola il carico utile con il sito webId e l'eventoData.
2. La serializzazione JSON del carico utile, deve escludere valori nulli.

## La `PopulateFromPayload` Metodo

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

Potete vedere che ci assicuriamo sempre che `websiteId` è impostato e fissiamo gli altri valori solo se non sono nulli. Questo ci dà flessibilità a scapito di un po 'di verbosità.

## La configurazione HttpClient

Come accennato prima dobbiamo dare una stringa User-Agent un po' reale all'API. Questo è fatto nel `HttpClient` Prepararsi.

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

## Servizio di background

Questo è un altro `IHostedService`, ci sono un sacco di articoli su come impostare questi in modo da non entrare in questo qui (provare la barra di ricerca!).

L'unico punto di dolore era l'uso dell'iniezione `HttpClient` Nella `UmamiClient` classe. A causa dello scoping del client e del servizio che ho usato `IServiceScopeFactory` iniettato nel costruttore del HostedService poi afferrare per ogni richiesta di invio.

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

### Utilizzo del Servizio Hosted

Ora che abbiamo questo servizio ospitato, possiamo migliorare notevolmente le prestazioni inviando gli eventi in background.

Ho usato questo in un paio di posti diversi, nel mio `Program.cs` Ho deciso di sperimentare con il monitoraggio della richiesta di feed RSS utilizzando Middleware, rileva solo qualsiasi percorso che termina in 'RSS' e invia un evento di background.

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

Ho anche passato più dati dalla mia `TranslateAPI` Endpoint.
Che mi permette di vedere quanto tempo le traduzioni stanno prendendo; notare nessuno di questi stanno bloccando il thread principale O tracciando i singoli utenti.

```csharp
    
       await  umamiClient.SendBackground(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
        var result = new TranslateResultTask(translationTask, true);
```

# In conclusione

L'API Umami è un po' strana, ma è un ottimo modo per monitorare gli eventi in modo auto-ospitato. Speriamo di avere la possibilita' di ripulire ancora di piu' e prendere un pacco di nuget Umami la' fuori.
Inoltre da un [articolo precedente](/blog/addingascsharpclientforumamiapi)  Voglio estrarre i dati da Umami per fornire caratteristiche come lo smistamento della popolarità.