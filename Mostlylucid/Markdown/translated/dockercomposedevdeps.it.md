# Uso di Docker Compose per le dipendenze di sviluppo

<!--category-- Docker -->
<datetime class="hidden">2024-08-09T17:17</datetime>

# Introduzione

Nello sviluppo di software tradizionalmente facevamo girare un database, una coda di messaggi, una cache, e forse qualche altro servizio. Questo può essere un dolore da gestire, soprattutto se si sta lavorando su più progetti. Docker Compose è uno strumento che consente di definire ed eseguire applicazioni Docker multi-container. È un ottimo modo per gestire le vostre dipendenze di sviluppo.

In questo post, vi mostrerò come usare Docker Compose per gestire le vostre dipendenze di sviluppo.

[TOC]

# Prerequisiti

Per prima cosa dovrai installare docker desktop su qualsiasi piattaforma tu stia usando. Puoi scaricarlo da [qui](https://www.docker.com/products/docker-desktop).

**NOTA: Ho trovato che su Windows è davvero necessario eseguire Docker Desktop installazione come amministratore per assicurarsi che si installa correttamente.**

# Creazione di un file di composizione Docker

Docker Compose utilizza un file YAML per definire i servizi che si desidera eseguire. Ecco un esempio di un semplice `devdeps-docker-compose.yml` file che definisce un servizio di database e un servizio di posta elettronica:

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

Nota qui Ho specificato i volumi per la persistenza dei dati per ogni servizio, qui ho specificato

```yaml
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    volumes:
        - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
```

Ciò assicura che i dati persistano tra le corse dei contenitori.

Ho anche specificato un `env_file` per la `postgres` servizio. Questo è un file che contiene variabili d'ambiente che vengono passate al contenitore.
Puoi vedere un elenco di variabili d'ambiente che possono essere passate al contenitore PostgreSQL [qui](https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/#1-Environment-variables).
Ecco un esempio di `.env` file:

```shell
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<somepassword>
```

Questo configura un database predefinito, una password e un utente per PostgreSQL.

Qui eseguisco anche il servizio SMTP4Dev, questo è un ottimo strumento per testare le funzionalità email nella tua applicazione. Puoi trovare maggiori informazioni al riguardo [qui](https://github.com/rnwood/smtp4dev/wiki/Installation#how-to-run-smtp4dev-in-docker).

Se guardi nel mio... `appsettings.Developmet.json` file che vedrete Ho la seguente configurazione per il server SMTP:

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

Questo funziona per SMTP4Dev e mi permette di testare questa funzionalità (Posso inviare a qualsiasi indirizzo, e vedere l'e-mail nell'interfaccia SMTP4Dev all'indirizzo http://localhost:3002/).

Una volta che sei sicuro che è tutto funzionante è possibile testare su un vero server SMTP come GMAIL (ad esempio, vedere [qui](addingasyncsendingforemails) per come farlo)

# Gestire i servizi

Per eseguire i servizi definiti nel `devdeps-docker-compose.yml` file, è necessario eseguire il seguente comando nella stessa directory del file:

```shell
docker compose -f .\devdeps-docker-compose.yml up -d
```

Si noti che si dovrebbe eseguire inizialmente in questo modo; questo assicura di poter vedere gli elementi di configurazione passati dal `.env` Archivio.

```shell
docker compose -f .\devdeps-docker-compose.yml config
```

Ora se guardi in Docker Desktop puoi vedere questi servizi in esecuzione

![Desktop Docker](dockerdesktopdev.png)