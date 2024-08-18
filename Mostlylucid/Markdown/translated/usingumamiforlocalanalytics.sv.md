# Använda Umami för lokal analys

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-08T15:53</datetime>

## Inledning

En av de saker som irriterade mig om min nuvarande inställning var att behöva använda Google Analytics för att få besöksdata (vad finns det för lite av det??). Så jag ville hitta något jag kunde själv-värd som inte skickade data till Google eller någon annan tredje part. Jag hittade [Umami Ordförande](https://umami.is/) vilket är en enkel, självupptagen webbanalyslösning. Det är ett bra alternativ till Google Analytics och är (relativt) lätt att konfigurera.

[TOC]

## Anläggning

Installationen är ganska enkel men tog en hel del fiddling för att verkligen komma igång...

### Docker komposera

Som jag ville lägga till Umami till min nuvarande Docker-compose setup Jag behövde lägga till en ny tjänst till min `docker-compose.yml` En akt. Jag lade till följande i slutet av filen:

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

Denna docker-compose.yml-fil innehåller följande inställning:

1. En ny tjänst kallas `umami` som använder `ghcr.io/umami-software/umami:postgresql-latest` bild. Denna tjänst används för att köra Umami analytics tjänsten.
2. En ny tjänst kallas `db` som använder `postgres:16-alpine` bild. Denna tjänst används för att köra Postgres databas som Umami använder för att lagra sina data.
   Observera för denna tjänst Jag mappade den till en katalog på min server så att datan finns kvar mellan omstarter.

```yaml
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
```

Du behöver denna regissör för att existera och vara skrivbar av Docker användaren på din server (igen inte en Linux expert så 777 är sannolikt överdöd här!)..............................................................................................

```shell
chmod 777 /mnt/umami/postgres
```

3. En ny tjänst kallas `cloudflaredumami` som använder `cloudflare/cloudflared:latest` bild. Denna tjänst används för att tunnel Umami tjänsten genom Cloudflare för att göra det möjligt att nås från internet.

### Env fil

För att stödja detta har jag också uppdaterat min `.env` fil för att inkludera följande:

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

Detta ställer in konfigurationen för Docker komponera (den `<>` Elemets behöver uppenbarligen ersättas med dina egna värderingar). I detta sammanhang är det viktigt att se till att `cloudflaredumami` tjänsten används för att tunnel Umami tjänsten genom Cloudflare för att göra det möjligt att nås från internet. Det är möjligt att använda en BASE_PATH men för Umami det irriterande behöver en rekonstruktion för att ändra basvägen så jag har lämnat det som roten vägen för nu.

### Molnflare Tunnel

För att ställa in molnflaretunneln för detta (som fungerar som sökvägen för js-filen som används för analys - getinfo.js) använde jag webbplatsen:

![Molnflare Tunnel](umamisetup.png)

Detta sätter upp tunneln till Umami tjänsten och gör det möjligt att nås från internet. Notera, jag pekar detta till `umami` tjänst i Docker-compose-filen (eftersom den är på samma nätverk som molneldade tunneln är det ett giltigt namn).

### Umami Ställ in på sidan

För att aktivera sökvägen för skriptet (kallas `getinfo` i min inställning ovan) Jag har lagt till en inställningspost i mina inställningar

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net/getinfo",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
 },
```

Du kan också lägga till dessa i din.env-fil och skicka in dem som miljövariabler till Docker-compose-filen.

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

Du satte upp WebsiteId i Umami instrumentpanelen när du satte upp webbplatsen. (Observera det förvalda användarnamnet och lösenordet för tjänsten Umami är `admin` och `umami`, du måste ändra dessa efter installationen).
![Umami Dashboard](umamiaddwebsite.png)

Med de tillhörande inställningarna cs-fil:

```csharp
public class AnalyticsSettings : IConfigSection
{
    public static string Section => "Analytics";
    public string? UmamiPath { get; set; }
}
```

Återigen detta använder min POCO konfiguration saker ([här](/blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)) för att ställa in inställningarna.
Ställ in det i mitt program.cs:

```csharp
builder.Configure<AnalyticsSettings>();
```

OCH slutligen i min `BaseController.cs` `OnGet` Metod Jag har lagt till följande för att ställa in sökvägen för analysskriptet:

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

Detta ställer in sökvägen för analysskriptet som ska användas i layoutfilen.

### Layoutfil

Slutligen har jag lagt till följande i min layoutfil för att inkludera analysskriptet:

```html
<script defer src="@ViewBag.UmamiPath" data-website-id="@ViewBag.UmamiWebsiteId"></script>
```

Detta inkluderar manuset på sidan och ställer in webbplatsens id för analystjänsten.

## Utom dig själv från analys

För att utesluta dina egna besök från analysdata kan du lägga till följande lokala lagring i din webbläsare:

I Chrome dev-verktyg (Ctrl+Shift+I på fönster) kan du lägga till följande i konsolen:

```javascript
localStorage.setItem("umami.disabled", 1)
```

## Slutsatser

Detta var lite av en faff att sätta upp men jag är nöjd med resultatet. Jag har nu en egen analystjänst som inte skickar data till Google eller någon annan tredje part. Det är lite av en smärta att ställa upp men när det är gjort är det ganska lätt att använda. Jag är nöjd med resultatet och skulle rekommendera det till alla som letar efter en självvärd analys lösning.