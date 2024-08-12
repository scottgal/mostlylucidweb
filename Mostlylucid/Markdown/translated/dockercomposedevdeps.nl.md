# Gebruik van Docker Compose voor ontwikkeling afhankelijkheden

<!--category-- Docker -->
<datetime class="hidden">2024-08-09T17:17</datetime>

# Inleiding

Bij het ontwikkelen van software zouden we traditioneel een database, een bericht wachtrij, een cache, en misschien een paar andere diensten. Dit kan een pijn zijn om te beheren, vooral als je werkt aan meerdere projecten. Docker Compose is een hulpmiddel waarmee u multi-container Docker toepassingen te definiëren en uit te voeren. Het is een geweldige manier om uw ontwikkeling afhankelijkheden te beheren.

In dit bericht, Ik zal u laten zien hoe u Docker Compose gebruiken om uw ontwikkeling afhankelijkheden te beheren.

[TOC]

# Vereisten

Eerst moet u docker desktop installeren op welk platform u ook gebruikt. U kunt het downloaden vanaf[Hier.](https://www.docker.com/products/docker-desktop).

**OPMERKING: Ik heb gevonden dat op Windows je echt nodig hebt om Docker Desktop installer als admin te draaien om ervoor te zorgen dat het correct installeert.**

# Aanmaken van een Docker-compose-bestand

Docker Compose gebruikt een YAML-bestand om de diensten te definiëren die u wilt uitvoeren. Hier is een voorbeeld van een eenvoudig`devdeps-docker-compose.yml`bestand dat een databasedienst definieert:

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

Notitie hier Ik heb opgegeven volumes voor het aanhouden van de gegevens voor elke dienst, hier heb ik gespecificeerd

```yaml
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    volumes:
        - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
```

Dit zorgt ervoor dat de gegevens tussen de ritten van de containers blijven bestaan.

Ik geef ook een`env_file`voor de`postgres`service. Dit is een bestand dat omgevingsvariabelen bevat die aan de container worden doorgegeven.
U kunt een lijst zien van omgevingsvariabelen die kunnen worden doorgegeven aan de PostgreSQL container[Hier.](https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/#1-Environment-variables).
Hier is een voorbeeld van een`.env`bestand:

```shell
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<somepassword>
```

Dit configureert een standaard database, wachtwoord en gebruiker voor PostgreSQL.

Hier run ik ook de SMTP4Dev service, dit is een geweldige tool voor het testen van e-mail functionaliteit in uw applicatie. U kunt meer informatie over het vinden[Hier.](https://github.com/rnwood/smtp4dev/wiki/Installation#how-to-run-smtp4dev-in-docker).

Als je kijkt in mijn`appsettings.Developmet.json`file you'll see I have the following configuration for the SMTP server:

```json
  "SmtpSettings":
{
"Server": "smtp.gmail.com",
"Port": 587,
"SenderName": "Mostlylucid",
"Username": "",
"SenderEmail": "scott.galloway@gmail.com",
"Password": "",
"EnableSSL": "true",
"EmailSendTry": 3,
"EmailSendFailed": "true",
"ToMail": "scott.galloway@gmail.com",
"EmailSubject": "Mostlylucid"

}
```

Dit werkt voor SMTP4Dev en het stelt me in staat om deze functionaliteit te testen (ik kan sturen naar elk adres, en zie de e-mail in de SMTP4Dev interface op http://localhost:3002/).

Zodra je zeker bent dat het allemaal werkt kun je testen op een echte SMTP-server zoals GMAIL (bijv., zie[Hier.](addingasyncsendingforemails)voor hoe dat te doen)

# Het uitvoeren van de diensten

Voor het uitvoeren van de diensten gedefinieerd in de`devdeps-docker-compose.yml`bestand, u moet het volgende commando uitvoeren in dezelfde map als het bestand:

```shell
docker compose -f .\devdeps-docker-compose.yml up -d
```

Merk op dat je het in eerste instantie op deze manier moet uitvoeren; dit zorgt ervoor dat je de config elementen kunt zien die zijn doorgegeven vanuit de`.env`bestand.

```shell
docker compose -f .\devdeps-docker-compose.yml config
```

Nu als je kijkt in Docker Desktop kunt u deze diensten draaiende zien

![Docker-bureaublad](dockerdesktopdev.png)