# Umami gebruiken voor lokale analytics

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-08T15:53</datetime>

## Inleiding

Een van de dingen die me irriteerde over mijn huidige setup was om Google Analytics te gebruiken om bezoekersgegevens te krijgen (wat weinig is er van het??). Dus ik wilde iets vinden dat ik zelf kon hosten dat geen gegevens doorgaf aan Google of een andere derde partij. Ik heb het gevonden. [Umami](https://umami.is/) dat is een eenvoudige, self-hosted web analytics oplossing. Het is een geweldig alternatief voor Google Analytics en is (relatief) eenvoudig op te zetten.

[TOC]

## Installatie

Installatie is vrij eenvoudig, maar het duurde een beetje friemelen om echt te krijgen...

### Docker-composeComment

Omdat ik Umami wilde toevoegen aan mijn huidige docker-compose setup moest ik een nieuwe service toevoegen aan mijn `docker-compose.yml` bestand. Ik heb het volgende toegevoegd aan de onderkant van het bestand:

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

Dit docker-compose.yml bestand bevat de volgende setup:

1. Een nieuwe dienst genaamd `umami` die gebruik maakt van de `ghcr.io/umami-software/umami:postgresql-latest` afbeelding. Deze dienst wordt gebruikt om de Umami analytics service te draaien.
2. Een nieuwe dienst genaamd `db` die gebruik maakt van de `postgres:16-alpine` afbeelding. Deze dienst wordt gebruikt om de Postgres database die Umami gebruikt om haar gegevens op te slaan.
   Notitie voor deze dienst Ik ben in kaart gebracht naar een directory op mijn server, zodat de gegevens worden aangehouden tussen herstarten.

```yaml
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
```

Je hebt deze director nodig om te bestaan en beschrijfbaar te zijn door de docker gebruiker op je server (weer geen Linux expert dus 777 is waarschijnlijk overkill hier!).

```shell
chmod 777 /mnt/umami/postgres
```

3. Een nieuwe dienst genaamd `cloudflaredumami` die gebruik maakt van de `cloudflare/cloudflared:latest` afbeelding. Deze service wordt gebruikt om de Umami service via Cloudflare te tunnelen om toegang te krijgen tot het internet.

### Env-bestand

Om dit te ondersteunen heb ik ook mijn `.env` bestand om het volgende op te nemen:

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

Dit stelt de configuratie voor de docker componeren (de `<>` Elemets moeten uiteraard vervangen worden door je eigen waarden). De `cloudflaredumami` service wordt gebruikt om de Umami service via Cloudflare te tunnelen om toegang te krijgen tot het internet. Het is mogelijk om een BASE_PATH te gebruiken, maar voor Umami heeft het vervelend genoeg een heropbouw nodig om het basispad te veranderen, dus ik heb het nu als het root pad achtergelaten.

### Cloudflare Tunnel

Om de cloudflare tunnel hiervoor in te stellen (die fungeert als het pad voor het js bestand dat gebruikt wordt voor analytics - getinfo.js) heb ik website gebruikt:

![Cloudflare Tunnel](umamisetup.png)

Dit stelt de tunnel naar de Umami dienst en maakt het mogelijk om toegang te krijgen vanaf het internet. Nota, ik wijs dit op de `umami` service in het docker-compose bestand (omdat het op hetzelfde netwerk staat als de cloudflared tunnel is het een geldige naam).

### Umami instellen in pagina

Het pad voor het script inschakelen (genaamd `getinfo` in mijn instellingen hierboven) Ik heb een configuratie-item toegevoegd aan mijn apps

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net/getinfo",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
 },
```

U kunt deze ook toevoegen aan uw.env bestand en ze doorgeven als omgevingsvariabelen aan het docker-compose bestand.

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

U zet de WebsiteId op in het Umami dashboard wanneer u de site opzet. (Let op de standaard gebruikersnaam en wachtwoord voor de Umami service is `admin` en `umami`, u moet deze wijzigen na de setup).
![Umami Dashboard](umamiaddwebsite.png)

Met het bijbehorende instellingen cs bestand:

```csharp
public class AnalyticsSettings : IConfigSection
{
    public static string Section => "Analytics";
    public string? UmamiPath { get; set; }
}
```

Nogmaals dit maakt gebruik van mijn POCO config spul ([Hier.](/blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)) om de instellingen op te zetten.
Zet het op in mijn programma.cs:

```csharp
builder.Configure<AnalyticsSettings>();
```

En uiteindelijk in mijn `BaseController.cs` `OnGet` methode Ik heb het volgende toegevoegd om het pad voor het analytics script in te stellen:

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

Dit stelt het pad in voor het analytics script dat gebruikt moet worden in het layout bestand.

### Layout-bestand

Tenslotte heb ik het volgende toegevoegd aan mijn layout bestand om het analytics script op te nemen:

```html
<script defer src="@ViewBag.UmamiPath" data-website-id="@ViewBag.UmamiWebsiteId"></script>
```

Dit omvat het script in de pagina en stelt de website id voor de analytics service.

## Exclusief jezelf van analytics

Om uw eigen bezoeken uit te sluiten van de analysegegevens kunt u de volgende lokale opslag in uw browser toevoegen:

In Chrome dev tools (Ctrl+Shift+I op windows) kunt u het volgende toevoegen aan de console:

```javascript
localStorage.setItem("umami.disabled", 1)
```

## Conclusie

Dit was een beetje een mislukking om op te zetten, maar ik ben blij met het resultaat. Ik heb nu een self-hosted analytics service die geen gegevens doorgeeft aan Google of een andere derde partij. Het is een beetje vervelend om op te zetten, maar als het eenmaal gedaan is is het vrij makkelijk te gebruiken. Ik ben blij met het resultaat en zou het aanbevelen aan iedereen op zoek naar een self-hosted analytics oplossing.