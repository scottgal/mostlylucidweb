# Self Hosting Seq voor ASP.NET Logging

<datetime class="hidden">2024-08-28T09:37</datetime>

<!--category-- ASP.NET, Seq, Serilog, Docker -->
# Inleiding

Seq is een toepassing waarmee u logs kunt bekijken en analyseren. Het is een geweldig hulpmiddel voor het debuggen en monitoren van uw toepassing. In dit bericht zal ik behandelen hoe ik Seq heb ingesteld om mijn ASP.NET Core applicatie in te loggen.
Kan nooit te veel dashboards hebben:)

![SeqDashboard](seqdashboard.png)

[TOC]

# Instelling van Seq

Seq komt in een paar smaken. Je kunt de cloudversie gebruiken of zelf hosten. Ik koos ervoor om het zelf te hosten omdat ik mijn logs privé wilde houden.

Eerst begon ik met het bezoeken van de Seq website en het vinden van de [Gebruiksaanwijzing voor de installatie van de docker](https://docs.datalust.co/docs/getting-started-with-docker).

## Lokaal

Om lokaal te draaien moet je eerst een gehashed wachtwoord krijgen. U kunt dit doen door het volgende commando uit te voeren:

```bash
echo '<password>' | docker run --rm -i datalust/seq config hash
```

Om het lokaal uit te voeren kunt u het volgende commando gebruiken:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hashfromabove> -v C:\seq:/data -p 5443:443 -p 45341:45341 -p 5341:5341 -p 82:80 datalust/seq

```

Op mijn lokale Ubuntu machine maakte ik dit tot een sh script:

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

Daarna

```shell
chmod +x seq.sh
./seq.sh
```

Dit zal je aan de gang krijgen en dan ga je naar `http://localhost:82` / `http://<machineip>:82` om uw volgende installatie te zien (standaard admin wachtwoord is degene die u invoerde voor <password> Boven.

## In Docker

Ik heb volgende toegevoegd aan mijn Docker bestand als volgt samengesteld:

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

Merk op dat ik een map heb genaamd `/mnt/seq` (voor windows, gebruik een windows pad). Hier worden de logs opgeslagen.

Ik heb ook een `SEQ_DEFAULT_HASH` omgevingsvariabele die het gehashed wachtwoord is voor de admin gebruiker in mijn.env bestand.

# ASP.NET-kern instellen

Als ik gebruik [Serilog](https://serilog.net/) Voor mijn houtkap is het eigenlijk vrij makkelijk om Seq te installeren. Het heeft zelfs documenten over hoe dit te doen [Hier.](https://docs.datalust.co/docs/using-serilog).

In principe voeg je gewoon de gootsteen toe aan je project:

```shell
dotnet add package Serilog.Sinks.Seq
```

Ik gebruik liever `appsettings.json` voor mijn config zodat ik gewoon de'standaard' setup in mijn `Program.cs`:

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
});
```

Dan heb ik in mijn Appsettings.json' deze configuratie

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

Je zult zien dat ik een `serverUrl` van `http://seq:5341`. Dit is omdat ik seq running in een docker container genaamd `seq` En het is aan bakboord. `5341`. Als je het lokaal draait, kun je het gebruiken. `http://localhost:5341`.
Ik gebruik ook de API-sleutel zodat ik de sleutel kan gebruiken om het logniveau dynamisch te specificeren (u kunt een sleutel instellen om alleen een bepaald niveau van logberichten te accepteren).

Je hebt het ingesteld in je volgende instantie door naar `http://<machine>:82` en klik op de instellingen linksboven. Klik vervolgens op de `API Keys` tab en voeg een nieuwe sleutel toe. U kunt dan deze sleutel gebruiken in uw `appsettings.json` bestand.

![Seq](seqapikey.png)

# Docker-composeComment

Nu we deze set-up hebben moeten we onze ASP.NET applicatie configureren om een sleutel op te halen. Ik gebruik een `.env` bestand om mijn geheimen op te slaan.

```dotenv
SEQ_DEFAULT_HASH="<adminpasswordhash>"
SEQ_API_KEY="<apikey>"
```

Dan in mijn docker componeer bestand Ik geef aan dat de waarde moet worden geïnjecteerd als een omgevingsvariabele in mijn ASP.NET app:

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

Merk op dat de `Serilog__WriteTo__0__Args__apiKey` is ingesteld op de waarde van `SEQ_API_KEY` van de `.env` bestand. De "0" is de index van de `WriteTo` array in de `appsettings.json` bestand.

# Caddy.

Notitie voor zowel Seq als mijn ASP.NET app Ik heb aangegeven dat ze beiden behoren tot mijn `app_network` netwerk. Dit komt omdat ik Caddy als omgekeerde proxy gebruik en het op hetzelfde netwerk zit. Dit betekent dat ik de servicenaam kan gebruiken als de URL in mijn Caddyfile.

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

Dus dit is in staat om in kaart te brengen `seq.mostlylucid.net` op mijn volgende instantie.

# Conclusie

Seq is een geweldige tool voor het loggen en monitoren van uw toepassing. Het is eenvoudig op te zetten en te gebruiken en integreert goed met Serilog. Ik vond het van onschatbare waarde in het debuggen van mijn sollicitaties en ik weet zeker dat jij dat ook doet.