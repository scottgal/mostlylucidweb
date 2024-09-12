# Umami.Net und Bot Detection

# Einleitung

Also habe ich [eine LOT gepostet](/blog/category/Umami) in der Vergangenheit über die Verwendung von Umami für die Analyse in einer selbst-hosted Umgebung und sogar veröffentlicht die [Umami.Net Nuget pacakge](https://www.nuget.org/packages/Umami.Net/). Allerdings war ich mit einem Problem, wo ich die Nutzer meines RSS-Feed verfolgen wollte; dieser Beitrag geht in, warum und wie ich es gelöst.

[TOC]

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-12T14:50</datetime>

# Das Problem

Das Problem ist, dass RSS-Feed-Reader versuchen, passieren *nützlich* User Agents beim Anfordern des Feeds. Dies ermöglicht **konform** Anbieter, die die Anzahl der Nutzer und die Art der Nutzer verfolgen, die den Feed verbrauchen. Dies bedeutet jedoch auch, dass Umami diese Anfragen als *Bot-* Anfragen. Dies ist ein Problem für meinen Gebrauch, da es dazu führt, dass die Anfrage ignoriert und nicht verfolgt wird.

Der Feedbin-Benutzeragent sieht so aus:

```plaintext
Feedbin feed-id:1234 - 21 subscribers
```

So ziemlich nützlich rechts, es gibt einige nützliche Details darüber, was Ihre Feed-ID ist, die Anzahl der Benutzer und der Benutzer-Agent. Allerdings ist dies auch ein Problem, da es bedeutet, dass Umami die Anfrage ignorieren wird; in der Tat wird es einen 200 Status BUT der Inhalt enthält `{"beep": "boop"}` bedeutet, dass dies als Bot-Anfrage identifiziert wird. Dies ist ärgerlich, da ich dies nicht durch normale Fehlerbehandlung behandeln kann (es ist ein 200, nicht sagen, ein 403 etc).

# Die Lösung

Also, was ist die Lösung dafür? Ich kann nicht alle diese Anfragen manuell analysieren und erkennen, ob Umami sie als Bot erkennt; es verwendet IsBot (https://www.npmjs.com/package/isbot), um festzustellen, ob eine Anfrage ein Bot ist oder nicht. Es gibt kein C#-Äquivalent und es ist eine wechselnde Liste, so dass ich nicht einmal diese Liste verwenden kann (in Zukunft kann ich klug werden und die Liste verwenden, um zu erkennen, ob eine Anfrage ein Bot ist oder nicht).
Also muss ich die Anfrage abfangen, bevor sie zu Umami kommt und den User Agent in etwas ändern, das Umami für bestimmte Anfragen akzeptieren wird.

So habe ich nun einige zusätzliche Parameter zu meinen Tracking-Methoden in Umami.Net hinzugefügt. Diese ermöglichen es Ihnen, den neuen 'Standard-Benutzeragenten' an Umami anstelle des ursprünglichen Benutzeragenten zu senden. Hiermit kann ich festlegen, dass der User Agent auf einen bestimmten Wert für bestimmte Anfragen geändert werden soll.

## Die Methoden

Auf meine `UmamiBackgroundSender` Ich habe Folgendes hinzugefügt:

```csharp
   public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
```

Dies existiert auf allen Tracking-Methoden dort und setzt einfach einen Parameter auf der `UmamiPayload` Gegenstand.

An `UmamiClient` Diese können wie folgt eingestellt werden:

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

In diesem Test verwende ich die neue `TrackPageViewAndDecode` Methode, die eine `UmamiDataResponse` Gegenstand. Dieses Objekt enthält dekodiertes JWT-Token (das ungültig ist, wenn es ein Bot ist, so dass dies nützlich ist zu überprüfen) und den Status der Anfrage.

## `PayloadService`

Dies alles wird in der `Payload` Dienst, der für die Bevölkerung des Nutzlastobjekts verantwortlich ist. Dies ist, wo die `UseDefaultUserAgent` ist bereit.

Standardmäßig bevölkere ich die Nutzlast aus der `HttpContext` so bekommen Sie normalerweise dieses Set richtig; Ich werde später zeigen, wo dieses zurück von Umami gezogen wird.

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

Dann habe ich einen Code namens `PopulateFromPayload` wo das Request-Objekt die eingestellten Daten erhält:

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

Sie werden sehen, dass dies definiert einen neuen Useragent an der Spitze der Datei (die ich bestätigt habe, ist nicht *zur Zeit* ) wurde als Bot erkannt. Dann in der Methode erkennt es, ob entweder der UserAgent null ist (was nicht passieren sollte, es sei denn, es wird aus Code ohne HttpContext aufgerufen) oder wenn die `UseDefaultUserAgent` ist bereit. Wenn es dann ist, setzt es den UserAgent auf den Standard und fügt den ursprünglichen UserAgent zum Datenobjekt hinzu.

Dies wird dann protokolliert, so dass Sie sehen können, was UserAgent verwendet wird.

## Entschlüsseln der Antwort.

In Umami.Net 0.3.0 habe ich eine Reihe neuer 'AndDecode' Methoden hinzugefügt, die eine `UmamiDataResponse` Gegenstand. Dieses Objekt enthält das dekodierte JWT-Token.

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

Sie können sehen, dass dies in den normalen ruft `TrackPageView` Methode ruft dann eine Methode namens `DecodeResponse` die die Antwort auf die `beep` und `boop` Zeichenketten (für Bot-Erkennung). Wenn es sie findet, dann protokolliert es eine Warnung und gibt eine `BotDetected` Wenn es sie nicht findet, entschlüsselt es das JWT-Token und gibt die Nutzlast zurück.

Das JWT-Token selbst ist nur ein Base64-kodierter String, der die Daten enthält, die Umami gespeichert hat. Dies wird dekodiert und als eine `UmamiDataResponse` Gegenstand.

Die vollständige Quelle dafür ist unten:

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
Sie können sehen, dass dies eine Reihe von nützlichen Informationen über die Anfrage enthält, die Umami gespeichert hat. Wenn Sie zum Beispiel verschiedene Inhalte auf der Grundlage von Locale, Sprache, Browser etc. zeigen möchten, können Sie dies tun.

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

# Schlussfolgerung

Also nur ein kurzer Beitrag, der einige neue Funktionen in Umami.Net 0.4.0 abdeckt, mit dem Sie einen Standard-Benutzeragenten für bestimmte Anfragen festlegen können. Dies ist nützlich für die Verfolgung von Anfragen, die Umami sonst ignorieren würde.