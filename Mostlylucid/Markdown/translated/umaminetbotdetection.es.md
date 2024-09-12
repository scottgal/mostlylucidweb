# Umami.Net y detección de bots

# Introducción

Así que tengo [publicado un LOT](/blog/category/Umami) en el pasado sobre el uso de Umami para el análisis en un entorno auto-anfitrión e incluso publicó el [Umami.Net Nuget pacakge](https://www.nuget.org/packages/Umami.Net/). Sin embargo, estaba teniendo un problema en el que quería rastrear a los usuarios de mi feed RSS; este post entra en porqué y cómo lo resolví.

[TOC]

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-12T14:50</datetime>

# El problema

El problema es que los lectores de RSS intentan pasar *útil* Agentes de usuario al solicitar el feed. Esto permite **conforme** proveedores para rastrear el número de usuarios y el tipo de usuarios que están consumiendo la alimentación. Sin embargo, esto también significa que Umami identificará estas solicitudes como *bot* solicitudes. Este es un problema para mi uso, ya que resulta en que la solicitud sea ignorada y no rastreada.

El agente de usuario de Feedbin se ve así:

```plaintext
Feedbin feed-id:1234 - 21 subscribers
```

Así que muy útil derecha, pasa algunos detalles útiles sobre lo que es su id de feed, el número de usuarios y el agente de usuario. Sin embargo, esto también es un problema ya que significa que Umami ignorará la solicitud; de hecho devolverá un estatus de 200 PERO el contenido contiene `{"beep": "boop"}` lo que significa que esto se identifica como una petición de bot. Esto es molesto ya que no puedo manejar esto a través de la manipulación de errores normales (es un 200, no decir un 403 etc).

# La solución

Entonces, ¿cuál es la solución a esto? No puedo analizar manualmente todas estas peticiones y detectar si Umami las detectará como un bot; utiliza IsBot (https://www.npmjs.com/package/isbot) para detectar si una solicitud es un bot o no. No hay equivalente de C# y es una lista cambiante por lo que ni siquiera puedo usar esa lista (en el futuro PUEDO ser inteligente y utilizar la lista para detectar si una solicitud es un bot o no).
Así que necesito interceptar la solicitud antes de que llegue a Umami y cambiar el Agente de Usuario a algo que Umami aceptará para solicitudes específicas.

Así que ahora he añadido algunos parámetros adicionales a mis métodos de seguimiento en Umami.Net. Estos le permiten especificar el nuevo 'Agente de usuario predeterminado' será enviado a Umami en lugar del Agente de usuario original. Esto me permite especificar que el Agente de Usuario debe ser cambiado a un valor específico para solicitudes específicas.

## Los métodos

En mi `UmamiBackgroundSender` He añadido lo siguiente:

```csharp
   public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
```

Esto existe en todos los métodos de seguimiento allí y sólo establece un parámetro en el `UmamiPayload` objeto.

Activar `UmamiClient` Estos pueden ser establecidos de la siguiente manera:

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

En esta prueba utilizo el nuevo `TrackPageViewAndDecode` método que devuelve un `UmamiDataResponse` objeto. Este objeto contiene un token JWT decodificado (que no es válido si es un bot por lo que es útil comprobar) y el estado de la solicitud.

## `PayloadService`

Todo esto se maneja en el `Payload` Servicio que es responsable de poblar el objeto de carga útil. Aquí es donde el `UseDefaultUserAgent` está listo.

Por defecto poblo la carga útil de la `HttpContext` Así que normalmente consigues este set correctamente; te mostraré más tarde dónde se saca esto de Umami.

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

ENTONCES tengo una pieza de código llamada `PopulateFromPayload` que es donde el objeto de solicitud obtiene sus datos configurados:

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

Verás que esto define un nuevo Usernagent en la parte superior del archivo (que he confirmado no es *En la actualidad* detectada como un bot). Entonces en el método se detecta si el UserAgent es nulo (lo que no debería suceder a menos que se llame desde código sin un HttpContext) o si el `UseDefaultUserAgent` está listo. Si es así, establece el UserAgent como predeterminado y añade el UserAgent original al objeto de datos.

Esto se registra entonces para que pueda ver lo que UserAgent está siendo utilizado.

## Decodificando la respuesta.

En Umami.Net 0.3.0 agregué un número de nuevos métodos 'AndDecode' que devuelven un `UmamiDataResponse` objeto. Este objeto contiene el token JWT decodificado.

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

Puedes ver que esto llama a lo normal. `TrackPageView` entonces llama a un método llamado `DecodeResponse` que comprueba la respuesta de la `beep` y `boop` cadenas (para la detección de bots). Si los encuentra entonces registra una advertencia y devuelve un `BotDetected` situación. Si no los encuentra, decodifica el token JWT y devuelve la carga útil.

El token JWT en sí mismo es sólo una cadena codificada Base64 que contiene los datos que Umami ha almacenado. Esto es decodificado y devuelto como un `UmamiDataResponse` objeto.

La fuente completa para esto es a continuación:

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
Puede ver que esto contiene un montón de información útil sobre la solicitud que Umami ha almacenado. Si querías, por ejemplo, mostrar contenido diferente basado en la localización, el idioma, el navegador, etc esto te permite hacerlo.

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

# Conclusión

Así que sólo un breve post que cubre algunas nuevas funciones en Umami.Net 0.4.0 que le permite especificar un agente de usuario por defecto para peticiones específicas. Esto es útil para rastrear solicitudes que Umami ignoraría de otro modo.