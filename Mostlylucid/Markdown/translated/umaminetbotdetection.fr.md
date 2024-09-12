# Umami.Net et la détection du bot

# Présentation

Donc j'ai [affiché un LOT](/blog/category/Umami) dans le passé sur l'utilisation d'Umami pour l'analyse dans un environnement auto-organisé et même publié le [Umami.Net Nuget pacakge](https://www.nuget.org/packages/Umami.Net/)C'est ce que j'ai dit. Cependant, j'avais un problème où je voulais suivre les utilisateurs de mon flux RSS; ce post va dans pourquoi et comment je l'ai résolu.

[TOC]

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-12T14:50</datetime>

# Le problème

Le problème est que les lecteurs de flux RSS essaient de passer *utile* Agents d'utilisateur lors de la demande du flux. Cela permet **conforme** les fournisseurs pour suivre le nombre d'utilisateurs et le type d'utilisateurs qui consomment l'aliment. Cependant, cela signifie également qu'Umami identifiera ces demandes comme *bot* les demandes. Il s'agit d'un problème pour mon utilisation car il résulte que la demande est ignorée et ne fait pas l'objet d'un suivi.

L'agent utilisateur Feedbin ressemble à ceci:

```plaintext
Feedbin feed-id:1234 - 21 subscribers
```

Donc assez utile à droite, il passe quelques détails utiles sur ce que votre id de flux est, le nombre d'utilisateurs et l'agent utilisateur. Cependant, c'est aussi un problème car cela signifie qu'Umami ignorera la requête; en fait, il retournera un statut 200 MAIS le contenu contient `{"beep": "boop"}` ce qui signifie qu'il s'agit d'une demande de bot. C'est ennuyeux car je ne peux pas gérer cela à travers la manipulation normale des erreurs (c'est un 200, pas dire un 403 etc).

# La solution

Alors quelle est la solution à cela? Je ne peux pas analyser manuellement toutes ces requêtes et détecter si Umami va les détecter en tant que bot; il utilise IsBot (https://www.npmjs.com/package/isbot) pour détecter si une requête est un bot ou non. Il n'y a pas d'équivalent C# et c'est une liste changeante donc je ne peux même pas utiliser cette liste (à l'avenir, je PEUT être intelligent et utiliser la liste pour détecter si une requête est un bot ou non).
Donc j'ai besoin d'intercepter la requête avant qu'elle n'arrive à Umami et de changer l'agent utilisateur en quelque chose que Umami acceptera pour des requêtes spécifiques.

J'ai donc ajouté quelques paramètres supplémentaires à mes méthodes de suivi dans Umami.Net. Ceux-ci vous permettent de spécifier le nouveau 'Agent d'utilisateur par défaut' sera envoyé à Umami au lieu de l'Agent d'utilisateur original. Cela me permet de spécifier que l'Agent d'utilisateur devrait être changé pour une valeur spécifique pour des requêtes spécifiques.

## Les méthodes

Sur mon `UmamiBackgroundSender` J'ai ajouté ce qui suit:

```csharp
   public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
```

Cela existe sur toutes les méthodes de suivi là-bas et il suffit de définir un paramètre sur le `UmamiPayload` objet.

À l'adresse suivante: `UmamiClient` Ceux-ci peuvent être définis comme suit:

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

Dans ce test, j'utilise le nouveau `TrackPageViewAndDecode` méthode qui renvoie une `UmamiDataResponse` objet. Cet objet contient le jeton JWT décodé (qui est invalide s'il s'agit d'un bot donc c'est utile à vérifier) et l'état de la requête.

## `PayloadService`

Tout cela est géré dans le `Payload` Service qui est chargé de remplir l'objet de charge utile. C'est là que les `UseDefaultUserAgent` est prêt.

Par défaut, je peuple la charge utile à partir de la `HttpContext` Donc vous obtenez habituellement ce set correctement; Je vais montrer plus tard où ceci est tiré en arrière d'Umami.

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

J'ai donc un code appelé `PopulateFromPayload` qui est où l'objet request obtient qu'il est des données configurées:

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

Vous verrez que cela définit un nouvel Useragent en haut du fichier (ce que j'ai confirmé n'est pas *actuellement* détecté comme un bot). Ensuite, dans la méthode, il détecte si soit l'UtilisateurAgent est nul (ce qui ne devrait pas se produire à moins qu'il soit appelé à partir de code sans HttpContext) ou si le `UseDefaultUserAgent` est prêt. S'il est alors il définit l'utilisateurAgent à la valeur par défaut et ajoute l'utilisateurAgent d'origine à l'objet de données.

Ceci est ensuite enregistré afin que vous puissiez voir ce que UserAgent est utilisé.

## Décoder la réponse.

Dans Umami.Net 0.3.0 j'ai ajouté un certain nombre de nouvelles méthodes 'AndDecode' qui retournent une `UmamiDataResponse` objet. Cet objet contient le jeton JWT décodé.

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

Vous pouvez voir que cela appelle dans la normale `TrackPageView` méthode appelle alors une méthode appelée `DecodeResponse` qui vérifie la réponse pour le `beep` et `boop` ficelles (pour la détection des robots). S'il les trouve alors il enregistre un avertissement et retourne un `BotDetected` le statut. S'il ne les trouve pas, il décode le jeton JWT et retourne la charge utile.

Le jeton JWT lui-même n'est qu'une chaîne encodée Base64 qui contient les données que Umami a stockées. Ceci est décodé et renvoyé comme un `UmamiDataResponse` objet.

La source complète de cette information est la suivante :

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
Vous pouvez voir que cela contient un tas d'informations utiles sur la requête que Umami a stockée. Si vous vouliez par exemple afficher différents contenus basés sur la locale, la langue, le navigateur etc cela vous permet de le faire.

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

# En conclusion

Donc juste un court post couvrant quelques nouvelles fonctionnalités dans Umami.Net 0.4.0 qui vous permet de spécifier un Agent utilisateur par défaut pour des requêtes spécifiques. Ceci est utile pour le suivi des demandes que Umami ignorerait autrement.