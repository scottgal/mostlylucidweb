# Umami.Net en Botdetectie

# Inleiding

Dus ik heb... [Ik heb een LOT gepost.](/blog/category/Umami) in het verleden over het gebruik van Umami voor analytics in een zelf-gehoste omgeving en zelfs gepubliceerd de [Umami.Net Nuget pacakge](https://www.nuget.org/packages/Umami.Net/). Echter ik had een probleem waar ik wilde bijhouden gebruikers van mijn RSS-feed; dit bericht gaat in waarom en hoe ik het opgelost.

[TOC]

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-12T14:50</datetime>

# Het probleem

Het probleem is dat RSS feed lezers proberen te passeren *nuttig* Gebruikersagenten bij het aanvragen van de feed. Dit maakt het mogelijk **conform** aanbieders om het aantal gebruikers en het type gebruikers dat de feed verbruikt, te volgen. Dit betekent echter ook dat Umami deze verzoeken zal identificeren als: *bot* verzoeken. Dit is een probleem voor mijn gebruik, omdat het resulteert in het verzoek wordt genegeerd en niet gevolgd.

De Feedbin-gebruiker ziet er zo uit:

```plaintext
Feedbin feed-id:1234 - 21 subscribers
```

Dus vrij nuttig rechts, het geeft enkele nuttige details over wat uw feed-id is, het aantal gebruikers en de gebruiker agent. Dit is echter ook een probleem omdat het betekent dat Umami het verzoek zal negeren; in feite zal het een 200 status teruggeven maar de inhoud bevat `{"beep": "boop"}` wat betekent dat dit wordt geïdentificeerd als een bot verzoek. Dit is vervelend omdat ik dit niet aankan door normale foutafhandeling (het is een 200, niet zeggen een 403 etc).

# De oplossing

Wat is de oplossing hiervoor? Ik kan al deze verzoeken niet handmatig verwerken en detecteren of Umami ze als een bot zal detecteren; het gebruikt IsBot (https://www.npmjs.com/package/isbot) om te detecteren of een verzoek een bot is of niet. Er is geen C# equivalent en het is een veranderende lijst dus ik kan niet eens die lijst te gebruiken (in de toekomst kan ik krijgen slim en gebruik de lijst om te detecteren of een verzoek is een bot of niet).
Dus ik moet het verzoek onderscheppen voordat het in Umami komt en de User Agent veranderen in iets dat Umami zal accepteren voor specifieke verzoeken.

Dus nu heb ik een aantal extra parameters toegevoegd aan mijn tracking methoden in Umami.Net. Hiermee kunt u aangeven dat de nieuwe 'Standaard Gebruiker Agent' naar Umami zal worden verzonden in plaats van de oorspronkelijke Gebruiker Agent. Dit stelt me in staat om te specificeren dat de Gebruiker Agent moet worden veranderd in een specifieke waarde voor specifieke verzoeken.

## Methoden

Op mijn `UmamiBackgroundSender` Ik heb het volgende toegevoegd:

```csharp
   public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
```

Dit bestaat op alle tracking methoden daar en zet gewoon een parameter op de `UmamiPayload` object.

Aan `UmamiClient` Deze kunnen als volgt worden ingesteld:

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

In deze test gebruik ik de nieuwe `TrackPageViewAndDecode` methode die een `UmamiDataResponse` object. Dit object bevat gedecodeerde JWT token (wat ongeldig is als het een bot is dus dit is nuttig om te controleren) en de status van het verzoek.

## `PayloadService`

Dit wordt allemaal behandeld in de `Payload` Dienst die verantwoordelijk is voor het bevolken van het payload object. Dit is waar de `UseDefaultUserAgent` is klaar.

Standaard vul ik de lading uit de `HttpContext` Dus meestal krijg je deze set correct; Ik zal later laten zien waar dit terug wordt getrokken uit Umami.

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

Dan heb ik een stukje code genaamd `PopulateFromPayload` dat is waar het verzoek object krijgt zijn gegevens ingesteld:

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

U zult zien dat dit een nieuwe Useragent aan de bovenkant van het bestand definieert (wat ik heb bevestigd is niet *momenteel* gedetecteerd als een bot). Dan in de methode het detecteert of de UserAgent is null (wat niet zou moeten gebeuren tenzij het wordt aangeroepen van code zonder een HttpContext) of als de `UseDefaultUserAgent` is klaar. Als het dan is, zet het de UserAgent op de standaard en voegt de originele UserAgent toe aan het gegevensobject.

Dit wordt dan gelogd zodat u kunt zien wat UserAgent wordt gebruikt.

## Decoderen van het antwoord.

In Umami.Net 0.3.0 heb ik een aantal nieuwe 'AndDecode' methoden toegevoegd die een `UmamiDataResponse` object. Dit object bevat het gedecodeerde JWT token.

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

U kunt zien dat dit oproept naar de normale `TrackPageView` methode dan noemt een methode genaamd `DecodeResponse` die het antwoord voor het `beep` en `boop` strings (voor botdetectie). Als het ze vindt dan logt het een waarschuwing en geeft een `BotDetected` status. Als het ze niet vindt, decodeert het de JWT token en geeft het de lading terug.

De JWT token zelf is slechts een Base64 gecodeerde string die de gegevens bevat die Umami heeft opgeslagen. Dit is gedecodeerd en teruggestuurd als een `UmamiDataResponse` object.

De volledige bron hiervoor is hieronder:

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
U kunt zien dat dit een heleboel nuttige informatie bevat over het verzoek dat Umami heeft opgeslagen. Als u bijvoorbeeld verschillende inhoud wilt tonen op basis van locale, taal, browser etc. kunt u dit doen.

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

# Conclusie

Dus gewoon een korte post die een aantal nieuwe functionaliteit in Umami.Net 0.4.0 die u toelaat om een standaard User Agent voor specifieke verzoeken op te geven. Dit is handig voor het volgen van verzoeken die Umami anders zou negeren.