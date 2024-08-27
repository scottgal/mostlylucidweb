# Lisään Umami-seurannan asiakasseurantaa

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-27T02:00</datetime>

# Johdanto

• • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • > • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • [aiempaa virkaa](/blog/addingumamitrackingclient.md) hahmottelin, miten Umamin jäljitysasiakas C#:ssä voisi toimia.
Minulla on vihdoin ollut mahdollisuus testata sitä laajasti ja parantaa sen toimintaa (kyllä toinen `IHostedService`).

[TÄYTÄNTÖÖNPANO

# Umamin API:n Quirks

Umami Tracking API on sekä hyvin mielipiteellinen että hyvin terskeinen. Joten jouduin päivittämään asiakaskoodin käsitelläkseni seuraavaa:

1. API odottaa "todellisen" näköistä Käyttäjä-Agent -jonoa. Joten minun piti päivittää asiakasta käyttämään oikeaa Käyttäjä-Agent-jonoa (tai tarkemmin sanottuna kaappasin oikean Käyttäjä-Agent-jonon selaimesta ja käytin sitä).
2. API odottaa JSON-syötettä tietyssä muodossa; tyhjät narut eivät ole sallittuja. Joten minun piti päivittää asiakasta hoitaakseni tämän.
3. Erytropoietiini [Node API -asiakas](https://github.com/umami-software/node) Siinä on hieman outo pinta-ala. Ei ole heti selvää, mitä API odottaa. Joten minun täytyi tehdä pieni yritys ja virhe saadakseni sen toimimaan.

## Node API -asiakas

Node API -asiakas on yhteensä alla, se on superjoustava, mutta ei todella hyvin dokumentoitu.

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

Kuten näette, se paljastaa seuraavat menetelmät:

1. `init` - Asettamaan vaihtoehdot.
2. `send` - Lähettämään lastin.
3. `track` - Seurata tapahtumaa.
4. `identify` - Käyttäjän tunnistamiseksi.
5. `reset` - Resetoida kiinteistöt.

Keskeistä tässä on `send` menetelmä, joka lähettää hyötykuorman API:hen.

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

# C#-asiakas

Aluksi kopioin aika lailla Node API -asiakkaan `UmamiOptions` sekä `UmamiPayload` Luennot (en aio ohittaa niitä uudelleen ne ovat isoja).

Joten nyt minun `Send` menetelmä näyttää tältä:

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

Tässä on kaksi kriittistä osaa:

1. Erytropoietiini `PopulateFromPayload` Menetelmä, joka kansoittaa hyötykuorman verkkosivullaId ja tapahtumaData.
2. Hyötykuorman JSON-sarjan on suljettava pois nolla-arvot.

## Erytropoietiini `PopulateFromPayload` Menetelmä

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

Huomaat, että varmistamme aina, että `websiteId` on asetettu, ja me asetamme muut arvot vain, jos ne eivät ole mitättömiä. Tämä antaa meille joustavuutta hieman verboilun kustannuksella.

## HttpClient-asetus

Kuten aiemmin mainittiin, meidän on annettava API:lle hieman todellinen Käyttäjä-agentti-merkkijono. Tämä tapahtuu `HttpClient` Lavastus.

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

## Taustapalvelu

Tämä on taas uutta `IHostedService`, On olemassa joukko artikkeleita siitä, miten järjestää nämä, joten en mene siihen täällä (kokeile hakupalkkia!).

Ainoa kipukohta oli injektion käyttö `HttpClient` in `UmamiClient` Luokka. Asiakkaan hahmottelun ja palvelun käytön vuoksi käytin `IServiceScopeFactory` Injektoidaan HostedServicen rakentajaan ja napataan se jokaisesta lähetyspyynnöstä.

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

### Isännöidyn palvelun käyttö

Nyt kun meillä on tämä isäntäpalvelu, voimme parantaa suoritusta dramaattisesti lähettämällä tapahtumat taustalla.

Olen käyttänyt tätä parissa eri paikassa. `Program.cs` Päätin kokeilla RSS-syötepyynnön seuraamista Middlewaren avulla, se vain havaitsee kaikki polut, jotka päättyvät "RSS:ään" ja lähettää taustatapahtuman.

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

Olen myös siirtänyt lisää dataa omastani. `TranslateAPI` päätetapahtuma.
Näin voin nähdä, kuinka kauan käännökset kestävät. Huomaa, että mikään näistä ei estä OR-pääkierrettä seuraamasta yksittäisiä käyttäjiä.

```csharp
    
       await  umamiClient.SendBackground(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
        var result = new TranslateResultTask(translationTask, true);
```

# Johtopäätöksenä

Umamin API on hieman omituinen, mutta se on hyvä tapa seurata tapahtumia omavaltaisesti. Toivottavasti saan siivottua sitä vielä enemmän ja hankin Umamin nuget-paketin.
Lisäksi: [aiempi artikkeli](/blog/addingascsharpclientforumamiapi)  Haluan vetää datan takaisin pois Umamista tarjotakseni ominaisuuksia, kuten suosion lajittelua.