# Self Hosting Seq för ASP.NET Loggning

<datetime class="hidden">2024-08-28T09:37</datetime>

<!--category-- ASP.NET, Seq, Serilog, Docker -->
# Inledning

Seq är ett program som låter dig visa och analysera loggar. Det är ett bra verktyg för felsökning och övervakning av din ansökan. I det här inlägget ska jag täcka hur jag konfigurerade Seq för att logga min ASP.NET Core ansökan.
Kan aldrig ha för många instrumentpaneler :)

![SeqDashboard](seqdashboard.png)

[TOC]

# Ställa in Seq

Seq kommer i ett par smaker. Du kan antingen använda molnversionen eller självvärd det. Jag valde att vara värd för den när jag ville hålla mina stockar privata.

Först började jag med att besöka Seq webbplats och hitta [Instruktioner för installation av Docker](https://docs.datalust.co/docs/getting-started-with-docker).

## Lokalt

För att köra lokalt måste du först få ett haschat lösenord. Du kan göra detta genom att köra följande kommando:

```bash
echo '<password>' | docker run --rm -i datalust/seq config hash
```

För att köra den lokalt kan du använda följande kommando:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hashfromabove> -v C:\seq:/data -p 5443:443 -p 45341:45341 -p 5341:5341 -p 82:80 datalust/seq

```

På min Ubuntu lokala maskin gjorde jag detta till ett sh manus:

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

Och sen...

```shell
chmod +x seq.sh
./seq.sh
```

Detta kommer att få dig upp och igång sedan gå till `http://localhost:82` / `http://<machineip>:82` för att se din följande installation (standardadministratörslösenordet är det du skrev in för <password> ovan.

## I Docker

Jag lade till följande i min Docker komponera fil enligt följande:

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

Observera att jag har en katalog som heter `/mnt/seq` (för fönster, använd en fönstersökväg). Det är här loggarna kommer att lagras.

Jag har också en `SEQ_DEFAULT_HASH` miljövariabeln som är det haschade lösenordet för administratörsanvändaren i min.env-fil.

# Sätta upp ASP.NET Core

Som jag använder [Serilog Ordförande](https://serilog.net/) För min loggning är det faktiskt ganska lätt att ställa in Seq. Det har till och med dokument om hur man gör detta [här](https://docs.datalust.co/docs/using-serilog).

I grund och botten du bara lägga till diskbänken till ditt projekt:

```shell
dotnet add package Serilog.Sinks.Seq
```

Jag föredrar att använda `appsettings.json` för min konfiguration så jag bara har "standard" inställning i min `Program.cs`:

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
});
```

Sedan i min 'appsettings.json' Jag har denna konfiguration

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

Du ska få se att jag har en `serverUrl` från `http://seq:5341`....................................... Det beror på att jag har följande kör i en docka behållare som heter `seq` och det är i hamn `5341`....................................... Om du kör den lokalt kan du använda `http://localhost:5341`.
Jag använder också API-nyckeln så att jag kan använda nyckeln för att ange loggnivån dynamiskt (du kan ställa in en nyckel för att bara acceptera en viss nivå av loggmeddelanden).

Du satte upp det i ditt följande fall genom att gå till `http://<machine>:82` och klicka på inställningarna kugge längst upp till höger. Klicka sedan på `API Keys` fliken och lägga till en ny nyckel. Du kan sedan använda denna nyckel i din `appsettings.json` En akt.

![Seq Ordförande](seqapikey.png)

# Docker komposera

Nu har vi denna uppsättning vi måste konfigurera vår ASP.NET-applikation för att plocka upp en nyckel. Jag använder en `.env` Arkivera mina hemligheter.

```dotenv
SEQ_DEFAULT_HASH="<adminpasswordhash>"
SEQ_API_KEY="<apikey>"
```

Sedan i min docker komponera fil Jag anger att värdet ska injiceras som en miljövariabel i min ASP.NET app:

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

Lägg märke till att `Serilog__WriteTo__0__Args__apiKey` är inställd på värdet av `SEQ_API_KEY` från `.env` En akt. Den "0" är indexet för `WriteTo` Uppställning i `appsettings.json` En akt.

# Nötkreatur och andra ryggradslösa vattendjur, beredda eller konserverade på annat sätt än med ättika eller ättiksyra, beredda eller konserverade på annat sätt än med ättika eller ättiksyra eller ättiksyra

Note för både Seq och min ASP.NET app Jag har specificerat att de båda tillhör min `app_network` nätverk. Det är för att jag använder Caddy som en omvänd proxy och det är på samma nätverk. Detta innebär att jag kan använda servicenamnet som URL i min Caddyfil.

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

Så detta kan kartlägga `seq.mostlylucid.net` till mitt följande exempel.

# Slutsatser

Seq är ett bra verktyg för att logga och övervaka din applikation. Det är lätt att konfigurera och använda och integreras väl med Serilog. Jag har funnit det ovärderligt att felsöka mina ansökningar och jag är säker på att du också kommer att göra det.