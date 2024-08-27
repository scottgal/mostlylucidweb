# Añadiendo seguimiento al cliente de seguimiento de Umami

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-27T02:00</datetime>

# Introducción

En una [Cargo anterior](/blog/addingumamitrackingclient.md) Esbocé cómo podría funcionar un cliente de rastreo para Umami en C#.
Bueno, por fin he tenido la oportunidad de probarlo ampliamente y mejorar su funcionamiento (sí, OTRO `IHostedService`).

[TOC]

# Quirks de la API de Umami

La API de seguimiento de Umami es a la vez muy perspicaz y muy tersa. Así que tuve que actualizar el código del cliente para manejar lo siguiente:

1. La API espera una cadena'real' con aspecto de User-Agent. Así que tuve que actualizar el cliente para usar una cadena real User-Agent (o para ser más preciso capté una cadena real User-Agent desde un navegador y la usé).
2. La API espera que sea JSON en un formato muy particular; las cadenas vacías no están permitidas. Así que tuve que actualizar al cliente para manejar esto.
3. Los [Cliente API de nodo](https://github.com/umami-software/node) tiene un poco de una superficie extraña. No está claro de inmediato lo que la API espera. Así que tuve que hacer un poco de ensayo y error para hacerlo funcionar.

## El cliente API de nodo

El cliente API de Nodo en total está por debajo, es súper flexible pero REALMENTE no está bien documentado.

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

Como ve, expone los siguientes métodos:

1. `init` - Para establecer las opciones.
2. `send` - Para enviar la carga útil.
3. `track` - Para rastrear un evento.
4. `identify` - Para identificar a un usuario.
5. `reset` - Para restablecer las propiedades.

El meollo de esto es el `send` método que envía la carga útil a la API.

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

# El cliente C#

Para empezar, he copiado prácticamente el cliente API de Node `UmamiOptions` y `UmamiPayload` clases (no las pasaré de nuevo son grandes).

Así que ahora mi `Send` el método se ve así:

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

Hay dos partes críticas aquí:

1. Los `PopulateFromPayload` método que pobla la carga útil con el siteId y el eventData.
2. La serialización JSON de la carga útil, necesita excluir valores nulos.

## Los `PopulateFromPayload` Método

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

Usted puede ver que siempre aseguramos el `websiteId` se establece y sólo establecemos los otros valores si no son nulos. Esto nos da flexibilidad a expensas de un poco de verbosidad.

## La configuración de HttpClient

Como se mencionó antes, necesitamos dar una cadena real User-Agent a la API. Esto se hace en el `HttpClient` Prepárate.

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

## Servicio de Antecedentes

Este es otro. `IHostedService`, hay un montón de artículos sobre cómo configurar estos para que no voy a entrar en él aquí (intenta la barra de búsqueda!).

El único punto de dolor fue el uso de la inyección `HttpClient` en la ventana `UmamiClient` clase. Debido al análisis del cliente y el servicio que utilicé `IServiceScopeFactory` inyectado en el constructor del HostedService y luego agarrarlo para cada solicitud de envío.

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

### Uso del servicio alojado

Ahora que tenemos este servicio alojado, podemos mejorar dramáticamente el rendimiento enviando los eventos en el fondo.

He usado esto en un par de lugares diferentes, en mi `Program.cs` Decidí experimentar con el seguimiento de la solicitud de feed RSS usando Middleware, sólo detecta cualquier ruta que termina en 'RSS' y envía un evento de fondo.

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

También he pasado más datos de mi `TranslateAPI` Endpoint.
Lo que me permite ver cuánto tiempo están tomando las traducciones; note que ninguna de estas están bloqueando el hilo principal O rastreando a los usuarios individuales.

```csharp
    
       await  umamiClient.SendBackground(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
        var result = new TranslateResultTask(translationTask, true);
```

# Conclusión

La API de Umami es un poco peculiar, pero es una gran manera de rastrear eventos de una manera auto-anfitriona. Con suerte tendré la oportunidad de limpiarlo aún más y conseguir un paquete de pepitas Umami por ahí.
Además de de un [Artículo anterior](/blog/addingascsharpclientforumamiapi)  Quiero sacar los datos de Umami para proporcionar características como clasificación de popularidad.