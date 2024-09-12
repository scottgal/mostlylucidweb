# Umami.Net- ja Bot-havainnointi

# Johdanto

Niin olen tehnyt [Posted a LOT](/blog/category/Umami) aiemmin Umamin käytöstä analytiikassa itseohjautuneessa ympäristössä ja julkaisi jopa [Umami.Net Nuget packge](https://www.nuget.org/packages/Umami.Net/)...................................................................................................................................... Minulla oli kuitenkin ongelma, jossa halusin seurata RSS-syötteeni käyttäjiä; tämä viesti menee siihen, miksi ja miten ratkaisin sen.

[TOC]

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-12T14:50</datetime>

# Ongelma

Ongelmana on, että RSS-syötteen lukijat yrittävät ohittaa *hyödyllistä* Käyttäjäagentit syötettä pyytäessä. Tämä mahdollistaa **joka on vaatimusten mukainen** palveluntarjoajat jäljittävät rehua kuluttavien käyttäjien määrää ja käyttäjätyyppiä. Tämä tarkoittaa kuitenkin myös sitä, että Umami tunnistaa nämä pyynnöt *botti* pyyntöjä. Tämä on käyttöni kannalta ongelma, koska se johtaa siihen, että pyyntö jätetään huomiotta eikä sitä jäljitetä.

Feedbin-käyttäjäagentti näyttää tältä:

```plaintext
Feedbin feed-id:1234 - 21 subscribers
```

Joten aika hyödyllinen oikein, se välittää joitakin hyödyllisiä yksityiskohtia siitä, mikä syöttötunniste on, käyttäjämäärä ja käyttäjäagentti. Tämä on kuitenkin myös ongelma, koska se tarkoittaa, että Umami jättää pyynnön huomiotta; itse asiassa se palauttaa 200 statuksen MUTTA sisältö sisältää `{"beep": "boop"}` tarkoittaa, että tämä tunnistetaan bottipyynnöksi. Tämä on ärsyttävää, koska en pysty käsittelemään tätä normaalilla virhekäsittelyllä (se on 200, ei vaikkapa 403 jne.).

# Ratkaisu

Mikä on ratkaisu tähän? En pysty käsikirjoittamaan kaikkia pyyntöjä ja havaitsemaan, havaitseeko Umami ne botiksi. Se käyttää IsBotia (https://www.npmjs.com/package/isbot) havaitakseen, onko pyyntö bot vai ei. C#-vastaavuutta ei ole ja se on vaihtuva lista, joten en voi edes käyttää sitä listaa (tulevaisuudessa saatan tulla fiksuksi ja käyttää listaa havaitakseni, onko pyyntö botti vai ei).
Joten minun täytyy siepata pyyntö, ennen kuin se ehtii Umamiin ja vaihtaa Käyttäjä-agentin johonkin, jonka Umami hyväksyy tietyissä pyynnöissä.

Joten nyt lisäsin lisää parametreja seurantamenetelmiini Umami.Netissä. Niiden avulla voit määrittää, että uusi "default User Agent" lähetetään Umamiin alkuperäisen Käyttäjäagentin sijaan. Tämän perusteella voin tarkentaa, että Käyttäjäasiamies tulisi vaihtaa tiettyyn arvoon tietyissä pyynnöissä.

## Menetelmät

Oman onneni varaan `UmamiBackgroundSender` Lisäsin asian seuraavasti:

```csharp
   public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
```

Tämä on olemassa kaikilla seurantamenetelmillä siellä ja vain asettaa parametrin `UmamiPayload` Esine.

Päällä `UmamiClient` nämä voi asettaa seuraavasti:

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

Tässä testissä käytän uutta `TrackPageViewAndDecode` menetelmä, joka palauttaa a `UmamiDataResponse` Esine. Tämä objekti sisältää puretun JWT-tokenon (joka on virheellinen, jos se on botti, joten se kannattaa tarkistaa) ja pyynnön tilan.

## `PayloadService`

Tämä kaikki on hoidettu `Payload` Palvelu, joka vastaa hyötykuormaobjektin asuttamisesta. Tämä on se paikka, jossa `UseDefaultUserAgent` on asetettu.

Oletuksena olen kansoittanut hyötykuorman `HttpContext` Joten yleensä saat tämän setin oikein; näytän myöhemmin, missä tämä vedetään takaisin Umamista.

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

Sitten minulla on koodinpala nimeltä `PopulateFromPayload` jossa pyyntö-objekti saa tietonsa:

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

Huomaat, että tämä määrittää uuden Useragentin tiedoston ylälaidassa (jota olen vahvistanut, ei ole *tällä hetkellä* havaittu bottina). Sitten menetelmässä selvitetään, onko Käyttäjäagentti joko nolla (mikä ei pitäisi tapahtua, ellei sitä kutsuta koodista ilman HttpContextiä) tai onko käyttäjä, joka käyttää HttpContextiä. `UseDefaultUserAgent` on asetettu. Jos näin on, se asettaa Käyttäjäagentin oletukseen ja lisää alkuperäisen Käyttäjäagentin dataobjektiin.

Tämä on sitten kirjautunut, jotta näet, mitä UserAgentia käytetään.

## Dekoodataan vastausta.

Urami.netissä 0.30 Lisäsin useita uusia AndDecode-menetelmiä, jotka palauttavat `UmamiDataResponse` Esine. Tämä esine sisältää puretun JWT-token.

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

Huomaat, että tämä kutsuu normaaliin `TrackPageView` menetelmä sitten kutsuu menetelmää nimeltä `DecodeResponse` joka tarkistaa vastauksen `beep` sekä `boop` narut (bottien havaitsemiseksi). Jos se löytää heidät, se kirjaa varoituksen ja palauttaa `BotDetected` Tilanne. Jos se ei löydä niitä, se purkaa JWT-todentimen ja palauttaa hyötykuorman.

Itse JWT on vain Base64-koodattu merkkijono, joka sisältää Umamin tallentamat tiedot. Tämä on purettu ja palautettu `UmamiDataResponse` Esine.

Täydellinen lähde tälle on alla:

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
Huomaat, että tässä on paljon hyödyllistä tietoa siitä pyynnöstä, jonka Umami on tallentanut. Jos haluat esimerkiksi näyttää erilaista sisältöä localen, kielen, selaimen jne. perusteella, voit tehdä sen.

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

# Johtopäätöksenä

Joten vain lyhyt viesti, joka kattaa joitakin uusia toimintoja Umamissa.Net 0.4.0, jonka avulla voit määrittää oletuskäyttäjän tietyissä pyynnöissä. Tämä on hyödyllistä, kun seurataan pyyntöjä, joita Umami ei muuten välittäisi.