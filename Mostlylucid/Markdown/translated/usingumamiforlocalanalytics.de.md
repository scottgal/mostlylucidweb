# Verwendung von Umami für lokale Analysen

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-08T15:53</datetime>

## Einleitung

Eines der Dinge, die mich über mein aktuelles Setup verärgerte war, Google Analytics verwenden zu müssen, um Besucherdaten zu erhalten (welches wenig gibt es davon???). Also wollte ich etwas finden, das ich selbst-hosten konnte, das keine Daten an Google oder andere Dritte weitergegeben hat.[Umami](https://umami.is/)das ist eine einfache, selbst gehostete Web-Analyse-Lösung. Es ist eine großartige Alternative zu Google Analytics und ist (relativ) einfach einzurichten.

[TOC]

## Installation

Installation ist PRETTY einfach, aber nahm ein bisschen fummeln, um wirklich los zu kommen...

### Docker-Komposition

Da ich Umami zu meinem aktuellen Docker-Kompose-Setup hinzufügen wollte, musste ich einen neuen Service zu meinem`docker-compose.yml`file. Ich habe am Ende der Datei Folgendes hinzugefügt:

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

Diese docker-compose.yml-Datei enthält die folgende Einrichtung:

1. Ein neuer Dienst namens`umami`die die`ghcr.io/umami-software/umami:postgresql-latest`image. Dieser Dienst wird verwendet, um den Umami-Analytics-Dienst zu betreiben.
2. Ein neuer Dienst namens`db`die die`postgres:16-alpine`image. Dieser Dienst wird verwendet, um die Postgres-Datenbank, die Umami verwendet, um seine Daten zu speichern laufen.
   Hinweis für diesen Dienst Ich werde ihn in ein Verzeichnis auf meinem Server gemappt, so dass die Daten zwischen den Neustarts bestehen bleiben.

```yaml
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
```

Sie brauchen diesen Direktor zu existieren und beschreibbar sein von der Docker-Benutzer auf Ihrem Server (wieder nicht ein Linux-Experte so 777 ist wahrscheinlich überkill hier!).

```shell
chmod 777 /mnt/umami/postgres
```

3. Ein neuer Dienst namens`cloudflaredumami`die die`cloudflare/cloudflared:latest`Bild. Dieser Dienst wird verwendet, um den Umami-Dienst über Cloudflare zu tunneln, damit er über das Internet abgerufen werden kann.

### Env-Datei

Um dies zu unterstützen, aktualisierte ich auch meine`.env`Datei, die Folgendes enthalten soll:

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

Dies setzt die Konfiguration für die docker komponieren (die`<>`Elemeten müssen offensichtlich durch eigene Werte ersetzt werden.`cloudflaredumami`Der Dienst wird verwendet, um den Umami-Dienst über Cloudflare zu tunneln, damit er über das Internet abgerufen werden kann. Es ist POSSIBLE, um einen BASE_PATH zu verwenden, aber für Umami braucht es ärgerlicherweise einen Wiederaufbau, um den Basispfad zu ändern, also habe ich ihn als Wurzelpfad für jetzt verlassen.

### Cloudflare-Tunnel

Um den Cloudflare-Tunnel dafür einzurichten (der als Pfad für die js-Datei dient, die für die Analyse verwendet wird - getinfo.js)

![Cloudflare-Tunnel](umamisetup.png)

Dies stellt den Tunnel zum Umami-Service auf und ermöglicht den Zugriff aus dem Internet. Beachten Sie, Ich weise dies auf die`umami`service in der docker-compose-Datei (da es sich im selben Netzwerk wie der cloudflared Tunnel befindet, ist es ein gültiger Name).

### Umami Setup auf der Seite

So aktivieren Sie den Pfad für das Skript (genannt`getinfo`in meinem Setup oben) Ich habe einen Config-Eintrag zu meinen Appsettings hinzugefügt

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net/getinfo",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
 },
```

Sie können diese auch zu Ihrer.env-Datei hinzufügen und als Umgebungsvariablen an die docker-compose-Datei übergeben.

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

Sie haben die WebsiteId im Umami-Dashboard eingerichtet, wenn Sie die Website einrichten. (Beachten Sie den Standard-Benutzernamen und das Passwort für den Umami-Dienst ist`admin`und`umami`, müssen Sie diese nach dem Setup ändern).
![Umami Dashboard](umamiaddwebsite.png)

Mit den zugehörigen Einstellungen cs-Datei:

```csharp
public class AnalyticsSettings : IConfigSection
{
    public static string Section => "Analytics";
    public string? UmamiPath { get; set; }
}
```

Auch dies nutzt meine POCO-Konfig-Sache ([Hierher](/blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)) zum Einrichten der Einstellungen.
Stellen Sie es in meinem programm.cs:

```csharp
builder.Configure<AnalyticsSettings>();
```

Und schließlich in meinem`BaseController.cs` `OnGet`Methode Ich habe das folgende hinzugefügt, um den Pfad für das Analyse-Skript festzulegen:

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

Damit wird der Pfad für das in der Layoutdatei zu verwendende Analytics-Skript festgelegt.

### Layout-Datei

Schließlich habe ich das folgende zu meiner Layout-Datei hinzugefügt, um das Analyse-Skript aufzunehmen:

```html
<script defer src="@ViewBag.UmamiPath" data-website-id="@ViewBag.UmamiWebsiteId"></script>
```

Dies schließt das Skript in der Seite ein und legt die Website-ID für den Analysedienst fest.

## Sich von der Analytik ausschließen

Um Ihre eigenen Besuche von den Analysedaten auszuschließen, können Sie folgende lokale Speicherung in Ihrem Browser hinzufügen:

In Chrome-dev-Tools (Strg+Shift+I auf Fenstern) können Sie die folgenden zu der Konsole hinzufügen:

```javascript
localStorage.setItem("umami.disabled", 1)
```

## Schlußfolgerung

Ich habe jetzt einen selbst gehosteten Analytics-Dienst, der keine Daten an Google oder andere Dritte weitergibt. Es ist ein bisschen Schmerz, aber sobald es getan ist, ist es ziemlich einfach zu bedienen. Ich bin mit dem Ergebnis zufrieden und würde es jedem empfehlen, der nach einer selbst gehosteten Analytics-Lösung sucht.