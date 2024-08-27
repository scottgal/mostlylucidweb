# Ajout d'un suivi du client Umami

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-27T02:00</datetime>

# Présentation

Dans un [poste antérieur](/blog/addingumamitrackingclient.md) J'ai esquissé comment un client de suivi pour Umami en C# pourrait fonctionner.
Eh bien, j'ai enfin eu l'occasion de le tester largement et d'améliorer son fonctionnement (oui ANOTHER `IHostedService`).

[TOC]

# Quirks de l'API Umami

L'API Umami Tracking est à la fois très avisée et très terse. J'ai donc dû mettre à jour le code client pour gérer ce qui suit :

1. L'API s'attend à ce que la chaîne User-Agent soit 'réelle'. Donc j'ai dû mettre à jour le client pour utiliser une vraie chaîne User-Agent (ou pour être plus précis, j'ai capturé une vraie chaîne User-Agent à partir d'un navigateur et j'ai utilisé cela).
2. L'API s'attend à ce que ce soit une entrée JSON dans un format très particulier ; les chaînes vides ne sont pas autorisées. Donc j'ai dû mettre à jour le client pour gérer ça.
3. Les [Client API Node](https://github.com/umami-software/node) a un peu de surface étrange. Il n'est pas immédiatement clair ce que l'API attend. Donc j'ai dû faire un peu d'essai et d'erreur pour que ça marche.

## Le client de l'API Node

Le client de l'API Node au total est ci-dessous, il est super flexible mais REALLY pas bien documenté.

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

Comme vous le voyez, il expose les méthodes suivantes:

1. `init` - Pour définir les options.
2. `send` - Pour envoyer la charge utile.
3. `track` - Pour suivre un événement.
4. `identify` - Pour identifier un utilisateur.
5. `reset` - Réinitialiser les propriétés.

L'essentiel de ceci est le `send` méthode qui envoie la charge utile à l'API.

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

# Le client C#

Pour commencer, j'ai à peu près copié le client de l'API Node `UmamiOptions` et `UmamiPayload` les cours (je ne les dépasserai plus, ils sont grands).

Alors maintenant, ma `Send` méthode ressemble à ceci:

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

Il y a deux parties critiques ici :

1. Les `PopulateFromPayload` méthode qui remplit la charge utile avec le site WebId et l'événementDonnées.
2. La sérialisation JSON de la charge utile, elle doit exclure les valeurs nulles.

## Les `PopulateFromPayload` Méthode

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

Vous pouvez voir que nous veillons toujours à ce que `websiteId` est défini et nous ne définissons les autres valeurs que si elles ne sont pas nulles. Cela nous donne de la flexibilité au détriment d'un peu de verbosité.

## La configuration HttpClient

Comme mentionné précédemment, nous devons donner une chaîne de l'utilisateur-agent quelque peu réelle à l'API. C'est ce qu'on fait dans le domaine de l'éducation et de la formation tout au long de la vie. `HttpClient` l'installation.

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

## Service d'information générale

C'est encore un autre `IHostedService`, il y a un tas d'articles sur la façon de les mettre en place pour que je n'y aille pas (essayez la barre de recherche!).............................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................

Le seul point de douleur a été l'utilisation de l'injecté `HttpClient` dans le `UmamiClient` En cours. En raison de la portée du client et du service que j'ai utilisé un `IServiceScopeFactory` injecté dans le constructeur de l'HostedService puis l'attraper pour chaque demande d'envoi.

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

### Utilisation du service hébergé

Maintenant que nous avons ce service hébergé, nous pouvons améliorer considérablement la performance en envoyant les événements en arrière-plan.

J'ai utilisé ça à quelques endroits différents, dans mon `Program.cs` J'ai décidé d'expérimenter le suivi de la demande de flux RSS en utilisant Middleware, il détecte tout chemin se terminant dans 'RSS' et envoie un événement de fond.

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

J'ai également passé plus de données de mon `TranslateAPI` le point final.
Ce qui me permet de voir combien de temps les traductions sont prises; notez qu'aucun de ceux-ci ne bloque le thread principal OU le suivi des utilisateurs individuels.

```csharp
    
       await  umamiClient.SendBackground(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
        var result = new TranslateResultTask(translationTask, true);
```

# En conclusion

L'API Umami est un peu bizarre, mais c'est une excellente façon de suivre les événements d'une manière auto-accueillée. J'espère que j'aurai l'occasion de nettoyer encore plus et d'obtenir un paquet de nuget Umami là-bas.
En plus d'un [article précédent](/blog/addingascsharpclientforumamiapi)  Je veux retirer les données d'Umami pour fournir des fonctionnalités comme le tri de popularité.