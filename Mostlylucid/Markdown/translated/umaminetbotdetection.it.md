# Rilevamento di Umami.Net e Bot

# Introduzione

Cosi' ho... [postato un sacco](/blog/category/Umami) in passato sull'utilizzo di Umami per l'analisi in un ambiente auto-ospitato e anche pubblicato il [Umami.Net Nuget pacakge](https://www.nuget.org/packages/Umami.Net/). Tuttavia stavo avendo un problema in cui volevo monitorare gli utenti del mio feed RSS; questo post va nel perché e come ho risolto.

[TOC]

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-12T14:50</datetime>

# Il problema

Il problema è che i lettori di feed RSS cercano di passare *utile* Agenti utente al momento della richiesta del feed. Questo permette **conforme** fornitori di monitorare il numero di utenti e il tipo di utenti che consumano il feed. Tuttavia, ciò significa anche che Umami individuerà queste richieste come *bot* richieste. Questo è un problema per il mio uso in quanto comporta che la richiesta venga ignorata e non rintracciata.

L'utente di Feedbin ha questo aspetto:

```plaintext
Feedbin feed-id:1234 - 21 subscribers
```

Così abbastanza utile a destra, passa alcuni dettagli utili su ciò che il vostro feed id è, il numero di utenti e l'utente agente. Tuttavia, questo è anche un problema in quanto significa che Umami ignorerà la richiesta; infatti restituirà uno stato di 200 MA il contenuto contiene `{"beep": "boop"}` significa che questo è identificato come una richiesta di bot. Questo è fastidioso come non riesco a gestire questo attraverso la normale gestione degli errori (è un 200, non dire un 403, ecc).

# La soluzione

Allora, qual e' la soluzione? Non posso analizzare manualmente tutte queste richieste e rilevare se Umami le rileverà come un bot; usa IsBot (https://www.npmjs.com/package/isbot) per rilevare se una richiesta è un bot o no. Non c'è nessun equivalente C# ed è una lista che cambia quindi non posso nemmeno usare quella lista (in futuro POSSO ottenere intelligente e utilizzare la lista per rilevare se una richiesta è un bot o no).
Quindi devo intercettare la richiesta prima che arrivi a Umami e cambiare l'Agente Utente in qualcosa che Umami accetterà per richieste specifiche.

Così ora ho aggiunto alcuni parametri aggiuntivi ai miei metodi di tracciamento in Umami.Net. Questi consentono di specificare il nuovo 'Agente utente predefinito' verrà inviato a Umami al posto dell'Agente utente originale. Questo mi permette di specificare che l'Agente Utente dovrebbe essere cambiato in un valore specifico per richieste specifiche.

## I metodi

# On my # `UmamiBackgroundSender` Ho aggiunto quanto segue:

```csharp
   public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
```

Questo esiste su tutti i metodi di tracciamento lì e imposta solo un parametro sulla `UmamiPayload` Oggetto.

Il `UmamiClient` questi possono essere impostati come segue:

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

In questo test uso il nuovo `TrackPageViewAndDecode` metodo che restituisce a `UmamiDataResponse` Oggetto. Questo oggetto contiene il token JWT decodificato (che non è valido se è un bot quindi questo è utile per controllare) e lo stato della richiesta.

## `PayloadService`

Questo è tutto gestito nel `Payload` Servizio che è responsabile per la popolazione dell'oggetto payload. Qui e' dove... `UseDefaultUserAgent` E' pronto.

Per impostazione predefinita popopoggio il payload dal `HttpContext` Quindi di solito si ottiene questo set correttamente; Vi mostrerò più tardi dove questo viene richiamato da Umami.

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

Poi ho un pezzo di codice chiamato `PopulateFromPayload` che è dove l'oggetto della richiesta ottiene i dati impostati:

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

Vedrete che questo definisce un nuovo useragent nella parte superiore del file (che ho confermato non è *attualmente* rilevato come un bot). Poi nel metodo che rileva se l'UserAgent è nullo (che non dovrebbe accadere a meno che non sia chiamato da codice senza un HttpContext) o se il `UseDefaultUserAgent` E' pronto. Se lo è, imposta l'UserAgent al valore predefinito e aggiunge l'UserAgent originale all'oggetto dati.

Questo viene quindi registrato in modo da poter vedere che cosa UserAgent viene utilizzato.

## Decodificare la risposta.

In Umami.Net 0.3.0 ho aggiunto un certo numero di nuovi metodi 'AndDecode' che restituiscono un `UmamiDataResponse` Oggetto. Questo oggetto contiene il token JWT decodificato.

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

Potete vedere che questo chiama nel normale `TrackPageView` metodo poi chiama un metodo chiamato `DecodeResponse` che controlla la risposta per `beep` e `boop` stringhe (per il rilevamento dei bot). Se li trova allora registra un avvertimento e restituisce un `BotDetected` Stato. Se non li trova, decodifica il token JWT e restituisce il carico utile.

Il token JWT stesso è solo una stringa codificata Base64 che contiene i dati che Umami ha memorizzato. Questo è decodificato e restituito come un `UmamiDataResponse` Oggetto.

La fonte completa per questo è di seguito:

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
Potete vedere che questo contiene una serie di informazioni utili sulla richiesta che Umami ha memorizzato. Se si desidera ad esempio mostrare contenuti diversi in base alla localizzazione, lingua, browser ecc questo consente di farlo.

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

# In conclusione

Così solo un breve post che copre alcune nuove funzionalità in Umami.Net 0.4.0 che consente di specificare un User Agent predefinito per richieste specifiche. Questo è utile per rintracciare le richieste che Umami altrimenti ignorerebbe.