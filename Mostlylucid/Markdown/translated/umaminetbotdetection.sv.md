# Umami.Net och Bot Detection

# Inledning

Så jag har [postade en LOT](/blog/category/Umami) tidigare om att använda Umami för analys i en självupptagen miljö och även publicerade [Ummami.Net Nuget pacakge](https://www.nuget.org/packages/Umami.Net/)....................................... Men jag hade ett problem där jag ville spåra användare av mitt RSS-flöde; detta inlägg går in på varför och hur jag löste det.

[TOC]

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-12T14:50</datetime>

# Problemet

Problemet är att RSS-flödesläsare försöker passera *användbar* Användaragenter när du begär sändningen. Detta tillåter **uppfyller kraven** Leverantörer för att spåra antalet användare och vilken typ av användare som konsumerar fodret. Detta innebär dock också att Umami kommer att identifiera dessa förfrågningar som *bot* förfrågningar. Detta är en fråga för min användning eftersom det resulterar i att begäran ignoreras och inte spåras.

Feedbin-användaragenten ser ut så här:

```plaintext
Feedbin feed-id:1234 - 21 subscribers
```

Så ganska användbart rätt, det passerar några användbara detaljer om vad din feed id är, antalet användare och användaren agent. Men detta är också ett problem eftersom det innebär att Umami kommer att ignorera begäran; i själva verket kommer det att returnera en 200 status men innehållet innehåller `{"beep": "boop"}` vilket innebär att detta identifieras som en bot begäran. Detta är irriterande eftersom jag inte kan hantera detta genom normal felhantering (det är en 200, inte säga en 403 etc).

# Lösningen

Så vad är lösningen på detta? Jag kan inte tolka alla dessa förfrågningar manuellt och upptäcka om Umami kommer att upptäcka dem som en bot; det använder IsBot (https://www.npmjs.com/package/isbot) för att upptäcka om en begäran är en bot eller inte. Det finns ingen C# motsvarighet och det är en ändrande lista så jag kan inte ens använda den listan (i framtiden kan jag bli smart och använda listan för att upptäcka om en begäran är en bot eller inte).
Så jag måste stoppa begäran innan den kommer till Umami och ändra User Agent till något som Umami kommer att acceptera för specifika förfrågningar.

Så nu lade jag till några ytterligare parametrar till mina spårningsmetoder i Umami.Net. Dessa låter dig ange den nya "Default User Agent" kommer att skickas till Umami istället för den ursprungliga User Agent. Detta gör att jag kan ange att Användaragenten bör ändras till ett specifikt värde för specifika förfrågningar.

## Metoderna

På min `UmamiBackgroundSender` Jag har lagt till följande:

```csharp
   public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
```

Detta finns på alla spårningsmetoder där och sätter bara en parameter på `UmamiPayload` motsätter sig detta.

På `UmamiClient` Dessa kan ställas in på följande sätt:

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

I detta test använder jag det nya `TrackPageViewAndDecode` metod som returnerar en `UmamiDataResponse` motsätter sig detta. Detta objekt innehåller avkodad JWT token (som är ogiltig om det är en bot så detta är användbart att kontrollera) och status för begäran.

## `PayloadService`

Allt detta hanteras i `Payload` Tjänst som ansvarar för att fylla nyttolastobjektet. Det är här som `UseDefaultUserAgent` Det är klart.

Som standard befolkar jag nyttolasten från `HttpContext` Så du brukar få denna uppsättning rätt; Jag ska visa senare var detta dras tillbaka från Umami.

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

Jag har en kod som heter `PopulateFromPayload` vilket är där begäran objektet får sin data som är inställd:

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

Du kommer att se att detta definierar en ny Useragent högst upp i filen (som jag har bekräftat är inte *för närvarande* detekteras som en bot). Sedan i metoden det detekterar om antingen Användaragenten är noll (vilket inte bör hända om det inte kallas från kod utan en HttpContext) eller om `UseDefaultUserAgent` Det är klart. Om det är så ställer det in UserAgent till standard och lägger till den ursprungliga UserAgent till dataobjektet.

Detta loggas sedan så att du kan se vad UserAgent används.

## Avlyser svaret.

I Umami.Net 0.3.0 Jag lade till ett antal nya "AndDecode" metoder som returnerar en `UmamiDataResponse` motsätter sig detta. Detta objekt innehåller den avkodade JWT- token.

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

Du kan se att detta kallar in det normala `TrackPageView` metod sedan kallar en metod som kallas `DecodeResponse` som kontrollerar svaret för `beep` och `boop` strängar (för botdetektering). Om den hittar dem, loggar den en varning och returnerar en `BotDetected` Status. Om den inte hittar dem avkodar den JWT-symbolen och returnerar nyttolasten.

JWT token själv är bara en Base64 kodad sträng som innehåller data som Umami har lagrat. Detta avkodas och returneras som en `UmamiDataResponse` motsätter sig detta.

Den fullständiga källan för detta är nedan:

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
Du kan se att detta innehåller en massa användbar information om den begäran som Umami har lagrat. Om du till exempel ville visa olika innehåll baserat på locale, språk, webbläsare etc. kan du göra det.

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

# Slutsatser

Så bara ett kort inlägg som täcker några nya funktioner i Umami.Net 0.0.0 som gör att du kan ange en standard User Agent för specifika förfrågningar. Detta är användbart för spårning förfrågningar som Umami annars skulle ignorera.