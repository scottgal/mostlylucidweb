# Self Hosting Seq per ASP.NET Logging

<datetime class="hidden">2024-08-28T09:37</datetime>

<!--category-- ASP.NET, Seq, Serilog, Docker -->
# Introduzione

Seq è un'applicazione che consente di visualizzare e analizzare i log. È un ottimo strumento per il debug e il monitoraggio dell'applicazione. In questo post coprirò come ho impostato Seq per registrare la mia applicazione ASP.NET Core.
Non si possono mai avere troppi cruscotti :)

![SeqDashboardCity name (optional, probably does not need a translation)](seqdashboard.png)

[TOC]

# Configurazione di Seq

Seq ha un paio di sapori. È possibile utilizzare la versione cloud o self host. Ho scelto di ospitarlo come volevo mantenere i miei registri privati.

Per prima cosa ho iniziato visitando il sito Seq e trovando il [Istruzioni per l'installazione di Docker](https://docs.datalust.co/docs/getting-started-with-docker).

## Locale

Per eseguire localmente è necessario prima ottenere una password hashed. Puoi farlo eseguendo il seguente comando:

```bash
echo '<password>' | docker run --rm -i datalust/seq config hash
```

Per eseguirlo localmente è possibile usare il seguente comando:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hashfromabove> -v C:\seq:/data -p 5443:443 -p 45341:45341 -p 5341:5341 -p 82:80 datalust/seq

```

Sulla mia macchina locale di Ubuntu ho fatto questo in un copione sh:

```shell
#!/bin/bash
PH=$(echo 'Abc1234!' | docker run --rm -i datalust/seq config hash)

mkdir -p /mnt/seq
chmod 777 /mnt/seq

docker run \
  --name seq \
  -d \
  --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -e SEQ_FIRSTRUN_ADMINPASSWORDHASH="$PH" \
  -v /mnt/seq:/data \
  -p 5443:443 \
  -p 45341:45341 \
  -p 5341:5341 \
  -p 82:80 \
  datalust/seq
```

Poi

```shell
chmod +x seq.sh
./seq.sh
```

Questo vi farà alzare e funzionare poi andare a `http://localhost:82` / `http://<machineip>:82` per vedere la tua seconda installazione (password di amministratore predefinita è quella per cui hai inserito <password> sopra.

## In DockerCity name (optional, probably does not need a translation)

Ho aggiunto seguenti al mio Docker comporre il file come segue:

```docker
  seq:
    image: datalust/seq
    container_name: seq
    restart: unless-stopped
    environment:
      ACCEPT_EULA: "Y"
      SEQ_FIRSTRUN_ADMINPASSWORDHASH: ${SEQ_DEFAULT_HASH}
    volumes:
      - /mnt/seq:/data
    networks:
      - app_network
```

Nota che ho una directory chiamata `/mnt/seq` (per le finestre, utilizzare un percorso delle finestre). Qui è dove verranno memorizzati i registri.

Ho anche un... `SEQ_DEFAULT_HASH` variabile d'ambiente che è la password hashed per l'utente amministratore nel mio file.env.

# Configurazione di ASP.NET Core

Come uso [SerilogCity name (optional, probably does not need a translation)](https://serilog.net/) per la mia registrazione è abbastanza facile da impostare Seq. Ha anche dei documenti su come fare questo [qui](https://docs.datalust.co/docs/using-serilog).

Fondamentalmente basta aggiungere il lavandino al tuo progetto:

```shell
dotnet add package Serilog.Sinks.Seq
```

Preferisco usare `appsettings.json` per la mia configurazione così ho appena il setup'standard' nel mio `Program.cs`:

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
});
```

Poi, nel mio photoappsettings.json' ho questa configurazione

```json
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq"],
    "MinimumLevel": "Warning",
    "WriteTo": [
        {
          "Name": "Seq",
          "Args":
          {
            "serverUrl": "http://seq:5341",
            "apiKey": ""
          }
        },
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }

    ],
    "Enrich": ["FromLogContext", "WithMachineName"],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }

```

Vedrai che ho un... `serverUrl` di `http://seq:5341`. Questo perché ho sequel in esecuzione in un contenitore docker chiamato `seq` ed è sul porto `5341`. Se lo stai eseguendo localmente puoi usare `http://localhost:5341`.
Uso anche il tasto API in modo da poter usare il tasto per specificare dinamicamente il livello di log (è possibile impostare una chiave per accettare solo un certo livello di messaggi di log).

L'hai impostato nel tuo secondo caso andando a `http://<machine>:82` e cliccando sulle impostazioni in alto a destra. Quindi fare clic sul `API Keys` scheda e aggiungere una nuova chiave. È quindi possibile utilizzare questa chiave nel vostro `appsettings.json` Archivio.

![SeqCity name (optional, probably does not need a translation)](seqapikey.png)

# Docker Componi

Ora abbiamo questo set up abbiamo bisogno di configurare la nostra applicazione ASP.NET per prendere una chiave. Io uso un `.env` File per memorizzare i miei segreti.

```dotenv
SEQ_DEFAULT_HASH="<adminpasswordhash>"
SEQ_API_KEY="<apikey>"
```

Poi nel mio docker comporre file ho specificato che il valore dovrebbe essere iniettato come variabile d'ambiente nella mia ASP.NET app:

```docker
services:
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    labels:
        - "com.centurylinklabs.watchtower.enable=true"
    env_file:
      - .env
    environment:
      - Auth__GoogleClientId=${AUTH_GOOGLECLIENTID}
      - Auth__GoogleClientSecret=${AUTH_GOOGLECLIENTSECRET}
      - Auth__AdminUserGoogleId=${AUTH_ADMINUSERGOOGLEID}
      - SmtpSettings__UserName=${SMTPSETTINGS_USERNAME}
      - SmtpSettings__Password=${SMTPSETTINGS_PASSWORD}
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
      - ConnectionStrings__DefaultConnection=${POSTGRES_CONNECTIONSTRING}
      - TranslateService__ServiceIPs=${EASYNMT_IPS}
      - Serilog__WriteTo__0__Args__apiKey=${SEQ_API_KEY}
    volumes:
      - /mnt/imagecache:/app/wwwroot/cache
      - /mnt/markdown/comments:/app/Markdown/comments
      - /mnt/logs:/app/logs
    networks:
      - app_network
```

Si noti che la `Serilog__WriteTo__0__Args__apiKey` è impostato al valore di `SEQ_API_KEY` dal `.env` Archivio. Lo "0" è l'indice della `WriteTo` array in the `appsettings.json` Archivio.

# CaddyCity name (optional, probably does not need a translation)

Nota sia per Seq che per la mia app ASP.NET ho specificato che appartengono entrambi alla mia `app_network` rete. Questo perché uso Caddy come proxy inverso ed è sulla stessa rete. Questo significa che posso usare il nome del servizio come URL nel mio Caddyfile.

```caddy
{
    email scott.galloway@gmail.com
}
seq.mostlylucid.net
{
   reverse_proxy seq:80
}

http://seq.mostlylucid.net
{
   redir https://{host}{uri}
}
```

Quindi questo è in grado di mappare `seq.mostlylucid.net` alla mia seconda istanza.

# Conclusione

Seq è un ottimo strumento per registrare e monitorare l'applicazione. E 'facile da configurare e utilizzare e si integra bene con Serilog. L'ho trovato inestimabile nel debug delle mie applicazioni e sono sicuro che lo farai anche tu.