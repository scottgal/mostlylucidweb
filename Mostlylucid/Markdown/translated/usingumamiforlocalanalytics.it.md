# Uso di Umami per l'analisi locale

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-08T15:53</datetime>

## Introduzione

Una delle cose che mi ha infastidito circa il mio attuale setup è stato quello di utilizzare Google Analytics per ottenere i dati del visitatore (quale poco c'è di esso??). Così ho voluto trovare qualcosa che potessi auto-ospitare che non passasse i dati a Google o a qualsiasi altra terza parte. Ho trovato... [UmamiCity name (optional, probably does not need a translation)](https://umami.is/) che è una soluzione di analisi web semplice e auto-ospitata. E 'una grande alternativa a Google Analytics ed è (relativamente) facile da configurare.

[TOC]

## Installazione

L'installazione è PRETTY semplice ma ha preso un bel po 'di giocherellare per...

### Docker Componi

Come ho voluto aggiungere Umami al mio attuale docker-compose configurazione avevo bisogno di aggiungere un nuovo servizio per il mio `docker-compose.yml` Archivio. Ho aggiunto quanto segue in fondo al file:

```yaml
  umami:
    image: ghcr.io/umami-software/umami:postgresql-latest
    env_file: .env
    environment:
      DATABASE_URL: ${DATABASE_URL}
      DATABASE_TYPE: ${DATABASE_TYPE}
      HASH_SALT: ${HASH_SALT}
      APP_SECRET: ${APP_SECRET}
      TRACKER_SCRIPT_NAME: getinfo
      API_COLLECT_ENDPOINT: all
    ports:
      - "3000:3000"
    depends_on:
      - db
    networks:
      - app_network
    restart: always
  db:
    image: postgres:16-alpine
    env_file:
      - .env
    networks:
      - app_network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 5s
      timeout: 5s
      retries: 5
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
    restart: always
  cloudflaredumami:
    image: cloudflare/cloudflared:latest
    command: tunnel --no-autoupdate run --token ${CLOUDFLARED_UMAMI_TOKEN}
    env_file:
      - .env
    restart: always
    networks:
      - app_network


```

Questo file docker-compose.yml contiene la seguente configurazione:

1. Un nuovo servizio chiamato `umami` che utilizza il `ghcr.io/umami-software/umami:postgresql-latest` immagine. Questo servizio viene utilizzato per eseguire il servizio di analisi Umami.
2. Un nuovo servizio chiamato `db` che utilizza il `postgres:16-alpine` immagine. Questo servizio viene utilizzato per eseguire il database Postgres che Umami utilizza per memorizzare i suoi dati.
   Nota per questo servizio Sono mappato in una directory sul mio server in modo che i dati persistono tra i riavvii.

```yaml
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
```

Avrete bisogno di questo direttore per esistere ed essere scrivibile dall'utente docker sul vostro server (di nuovo non un esperto di Linux quindi 777 è probabilmente sovraccaricare qui!).

```shell
chmod 777 /mnt/umami/postgres
```

3. Un nuovo servizio chiamato `cloudflaredumami` che utilizza il `cloudflare/cloudflared:latest` immagine. Questo servizio è utilizzato per il tunnel del servizio Umami attraverso Cloudflare per consentire l'accesso da internet.

### File env

Per sostenere questo ho anche aggiornato il mio `.env` file per includere quanto segue:

```shell
CLOUDFLARED_UMAMI_TOKEN=<cloudflaretoken>
DATABASE_TYPE=postgresql
HASH_SALT=<salt>

POSTGRES_DB=postgres
POSTGRES_USER=<postgresuser>
POSTGRES_PASSWORD=<postgrespassword>
UMAMI_SECRET=<umamisecret>

APP_SECRET=${UMAMI_SECRET}
UMAMI_USER=${POSTGRES_USER}
UMAMI_PASS=${POSTGRES_PASSWORD}
DATABASE_URL=postgresql://${UMAMI_USER}:${UMAMI_PASS}@db:5432/${POSTGRES_DB}
```

Questo imposta la configurazione per la composizione del docker (la `<>` elemets ovviamente bisogno di sostituire con i propri valori). La `cloudflaredumami` servizio è utilizzato per il tunnel del servizio Umami attraverso Cloudflare per consentire che sia accessibile da internet. E 'possibile utilizzare un BASE_PATH, ma per Umami ha fastidiosamente bisogno di una ricostruzione per cambiare il percorso di base quindi ho lasciato come il percorso di radice per ora.

### Tunnel Cloudflare

Per impostare il tunnel cloudflare per questo (che funge da percorso per il file js utilizzato per l'analisi - getinfo.js) ho utilizzato il sito web:

![Tunnel Cloudflare](umamisetup.png)

Questo imposta il tunnel per il servizio Umami e consente di accedervi da internet. Si noti che questo punto alla `umami` servizio nel file docker-compose (in quanto è sulla stessa rete del tunnel cloudflared è un nome valido).

### Impostazione Umami nella pagina

Per abilitare il percorso dello script (chiamato `getinfo` nella mia configurazione di cui sopra) Ho aggiunto una voce di configurazione alle mie appsetting

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net/getinfo",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
 },
```

Puoi anche aggiungerli al tuo file.env e passarli come variabili d'ambiente al file docker-compose.

```shell
ANALYTICS__UMAMIPATH="https://umamilocal.mostlylucid.net/getinfo"
ANALYTICS_WEBSITEID="32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
```

```yaml
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    environment:
    ...
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
```

Hai impostato il WebsiteId nel cruscotto Umami quando hai impostato il sito. (Notare il nome utente predefinito e la password per il servizio Umami è `admin` e `umami`, è necessario cambiare questi dopo la configurazione).
![Cruscotto Umami](umamiaddwebsite.png)

Con il file cs delle impostazioni associate:

```csharp
public class AnalyticsSettings : IConfigSection
{
    public static string Section => "Analytics";
    public string? UmamiPath { get; set; }
}
```

Ancora una volta questo usa la mia roba di configurazione del POCO ([qui](/blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)) per impostare le impostazioni.
Impostalo nel mio programma.cs:

```csharp
builder.Configure<AnalyticsSettings>();
```

E finalmente nel mio `BaseController.cs` `OnGet` metodo Ho aggiunto quanto segue per impostare il percorso per lo script analitico:

```csharp
   public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!Request.IsHtmx())
        {
            ViewBag.UmamiPath = _analyticsSettings.UmamiPath;
            ViewBag.UmamiWebsiteId = _analyticsSettings.WebsiteId;
        }
        base.OnActionExecuting(filterContext);
    }
    
```

Questo imposta il percorso per lo script analytics da usare nel file di layout.

### File di disposizione

Infine, ho aggiunto quanto segue al mio file di layout per includere lo script analytics:

```html
<script defer src="@ViewBag.UmamiPath" data-website-id="@ViewBag.UmamiWebsiteId"></script>
```

Questo include lo script nella pagina e imposta l'id del sito web per il servizio di analisi.

## Escludendo te stesso dall'analisi

Al fine di escludere le proprie visite dai dati di analisi è possibile aggiungere la seguente memorizzazione locale nel browser:

In Chrome dev strumenti (Ctrl + Maiusc + I su windows) è possibile aggiungere quanto segue alla console:

```javascript
localStorage.setItem("umami.disabled", 1)
```

## Conclusione

Questo è stato un po 'di un faff per impostare, ma sono contento del risultato. Ora ho un servizio di analisi auto-hosted che non passa i dati a Google o qualsiasi altra terza parte. E 'un po' un dolore da impostare, ma una volta fatto è abbastanza facile da usare. Sono felice del risultato e lo consiglierei a chiunque cerchi una soluzione di analisi auto-ospitata.