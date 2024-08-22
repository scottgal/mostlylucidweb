# Använda Docker Komposit för utvecklingsberoenden

<!--category-- Docker -->
<datetime class="hidden">2024-08-09T17:17</datetime>

# Inledning

När vi utvecklade programvara brukade vi snurra upp en databas, en meddelande kö, en cache, och kanske några andra tjänster. Detta kan vara en smärta att hantera, särskilt om du arbetar på flera projekt. Docker Compose är ett verktyg som låter dig definiera och köra flera behållare Docker program. Det är ett bra sätt att hantera din utveckling beroenden.

I det här inlägget ska jag visa dig hur du använder Docker Compose för att hantera dina utvecklingsberoenden.

[TOC]

# Förutsättningar

Först måste du installera docker skrivbord på vilken plattform du än använder. Du kan ladda ner den från [här](https://www.docker.com/products/docker-desktop).

**OBS: Jag har funnit att på Windows måste du verkligen köra Docker Desktop installationsprogram som administratör för att se till att det installeras korrekt.**

# Skapa en dockerkomponera fil

Docker Compose använder en YAML-fil för att definiera de tjänster du vill köra. Här är ett exempel på en enkel `devdeps-docker-compose.yml` fil som definierar en databastjänst och en e-posttjänst:

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

Observera här att jag har specificerat volymer för att bevara data för varje tjänst, här har jag specificerat

```yaml
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    volumes:
        - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
```

Detta säkerställer att uppgifterna finns kvar mellan transporterna av behållarna.

Jag specificerar också en `env_file` för `postgres` service. Detta är en fil som innehåller miljövariabler som skickas till behållaren.
Du kan se en lista över miljövariabler som kan skickas till PostgreSQL-behållaren [här](https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/#1-Environment-variables).
Här är ett exempel på en `.env` fil:

```shell
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<somepassword>
```

Detta konfigurerar en standarddatabas, lösenord och användare för PostgreSQL.

Här kör jag också SMTP4Dev-tjänsten, detta är ett bra verktyg för att testa e-postfunktioner i ditt program. Du kan hitta mer information om det [här](https://github.com/rnwood/smtp4dev/wiki/Installation#how-to-run-smtp4dev-in-docker).

Om du tittar i min `appsettings.Developmet.json` fil du ser Jag har följande konfiguration för SMTP-servern:

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

Detta fungerar för SMTP4Dev och gör det möjligt för mig att testa denna funktionalitet (jag kan skicka till vilken adress som helst, och se e-posten i SMTP4Dev-gränssnittet på http://localhost:3002/).

När du är säker på att allt fungerar kan du testa på en riktig SMTP-server som GMAIL (t.ex. se [här](addingasyncsendingforemails) för hur man gör det)

# Drift av tjänsterna

Köra de tjänster som definieras i `devdeps-docker-compose.yml` fil, du måste köra följande kommando i samma katalog som filen:

```shell
docker compose -f .\devdeps-docker-compose.yml up -d
```

Observera att du bör köra den från början så här. Detta säkerställer att du kan se konfigurationselementen som skickas in från `.env` En akt.

```shell
docker compose -f .\devdeps-docker-compose.yml config
```

Nu om du tittar i Docker Desktop kan du se dessa tjänster köras

![Dockningsskrivbord](dockerdesktopdev.png)