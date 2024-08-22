# Verwendung von Docker Compose für Entwicklungsabhängigkeiten

<!--category-- Docker -->
<datetime class="hidden">2024-08-09T17:17</datetime>

# Einleitung

Bei der Entwicklung von Software würden wir traditionell eine Datenbank, eine Nachrichten-Warteschlange, einen Cache und vielleicht ein paar andere Dienste spinnen. Dies kann ein Schmerz zu verwalten, vor allem, wenn Sie an mehreren Projekten arbeiten. Docker Compose ist ein Tool, mit dem Sie Multi-Container Docker-Anwendungen definieren und ausführen können. Es ist ein guter Weg, um Ihre Entwicklungsabhängigkeiten zu verwalten.

In diesem Beitrag werde ich Ihnen zeigen, wie Sie Docker Compose verwenden, um Ihre Entwicklungsabhängigkeiten zu verwalten.

[TOC]

# Voraussetzungen

Zuerst müssen Sie Docker-Desktop auf welcher Plattform Sie auch immer installieren. Sie können es herunterladen von [Hierher](https://www.docker.com/products/docker-desktop).

**HINWEIS: Ich habe festgestellt, dass Sie unter Windows wirklich Docker Desktop Installer als Admin ausführen müssen, um sicherzustellen, dass es korrekt installiert.**

# Erstellen einer Docker Compose-Datei

Docker Compose verwendet eine YAML-Datei, um die Dienste zu definieren, die Sie ausführen möchten. Hier ist ein Beispiel für eine einfache `devdeps-docker-compose.yml` Datei, die einen Datenbankdienst und einen E-Mail-Dienst definiert:

```yaml
services: 
  smtp4dev:
    image: rnwood/smtp4dev
    ports:
      - "3002:80"
      - "2525:25"
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    restart: always
  postgres:
    image: postgres:16-alpine
    container_name: postgres
    ports:
      - "5432:5432"
    env_file:
      - .env
    volumes:
      - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
    restart: always	
networks:
  mynetwork:
        driver: bridge
```

Anmerkung hier Ich habe Volumen für die Fortdauer der Daten für jeden Dienst angegeben, hier habe ich angegeben

```yaml
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    volumes:
        - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
```

Dadurch wird sichergestellt, dass die Daten zwischen den Durchläufen der Container bestehen bleiben.

Ich spezifizieren auch eine `env_file` für die `postgres` ............................................................................................................................................ Dies ist eine Datei, die Umgebungsvariablen enthält, die an den Container übergeben werden.
Sie können eine Liste von Umgebungsvariablen sehen, die an den PostgreSQL-Container übergeben werden können [Hierher](https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/#1-Environment-variables).
Hier ist ein Beispiel für eine `.env` Datei:

```shell
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<somepassword>
```

Dies konfiguriert eine Standarddatenbank, Passwort und Benutzer für PostgreSQL.

Hier laufe ich auch den SMTP4Dev Service, das ist ein tolles Tool zum Testen von E-Mail-Funktionen in Ihrer Anwendung. Mehr Informationen dazu finden Sie hier [Hierher](https://github.com/rnwood/smtp4dev/wiki/Installation#how-to-run-smtp4dev-in-docker).

Wenn du in meine `appsettings.Developmet.json` Datei, die Sie sehen werden Ich habe die folgende Konfiguration für den SMTP-Server:

```json
  "SmtpSettings":
{
"Server": "localhost",
"Port": 2525,
"SenderName": "Mostlylucid",
"Username": "",
"SenderEmail": "scott.galloway@gmail.com",
"Password": "",
"EnableSSL": "false",
"EmailSendTry": 3,
"EmailSendFailed": "true",
"ToMail": "scott.galloway@gmail.com",
"EmailSubject": "Mostlylucid"

}
```

Dies funktioniert für SMTP4Dev und es ermöglicht mir, diese Funktionalität zu testen (Ich kann an jede Adresse senden, und sehen Sie die E-Mail in der SMTP4Dev-Schnittstelle unter http://localhost:3002/).

Sobald Sie sicher sind, dass alles funktioniert, können Sie auf einem echten SMTP-Server wie GMAIL testen (z.B. siehe [Hierher](addingasyncsendingforemails) für die Art und Weise, wie das zu tun ist)

# Betrieb der Dienste

Um die in der `devdeps-docker-compose.yml` file, müssen Sie den folgenden Befehl in das gleiche Verzeichnis wie die Datei ausführen:

```shell
docker compose -f .\devdeps-docker-compose.yml up -d
```

Beachten Sie, dass Sie es zunächst so ausführen sollten; dies stellt sicher, dass Sie die aus der Konfiguration übergebenen Elemente sehen können. `.env` ..............................................................................................................................

```shell
docker compose -f .\devdeps-docker-compose.yml config
```

Wenn Sie jetzt in Docker Desktop suchen, können Sie diese Dienste laufen sehen

![Docker-Arbeitsfläche](dockerdesktopdev.png)