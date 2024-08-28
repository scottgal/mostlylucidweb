# Selbst-Hosting Seq für ASP.NET Logging

<datetime class="hidden">2024-08-28T09:37</datetime>

<!--category-- ASP.NET, Seq, Serilog, Docker -->
# Einleitung

Seq ist eine Anwendung, mit der Sie Protokolle anzeigen und analysieren können. Es ist ein großartiges Werkzeug zum Debuggen und Überwachen Ihrer Anwendung. In diesem Beitrag werde ich abdecken, wie ich Seq einrichten, um meine ASP.NET Core-Anwendung zu protokollieren.
Kann nie zu viele Dashboards haben :)

![SeqDashboard](seqdashboard.png)

[TOC]

# Einrichtung von Seq

Seq kommt in ein paar Geschmacksrichtungen. Sie können entweder die Cloud-Version verwenden oder sie selbst hosten. Ich entschied mich, es selbst zu bewirten, da ich meine Logbücher geheim halten wollte.

Zuerst begann ich mit dem Besuch der Seq-Website und der Suche nach [Anleitung für Docker installieren](https://docs.datalust.co/docs/getting-started-with-docker).

## Örtlich

Um lokal laufen zu können, müssen Sie zuerst ein Hashed-Passwort erhalten. Sie können dies tun, indem Sie den folgenden Befehl ausführen:

```bash
echo '<password>' | docker run --rm -i datalust/seq config hash
```

Um es lokal auszuführen, können Sie den folgenden Befehl verwenden:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hashfromabove> -v C:\seq:/data -p 5443:443 -p 45341:45341 -p 5341:5341 -p 82:80 datalust/seq

```

Auf meiner lokalen Ubuntu-Maschine habe ich dies zu einem Sh-Skript gemacht:

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

Dann

```shell
chmod +x seq.sh
./seq.sh
```

Das wird dich zum Laufen bringen und dann zu `http://localhost:82` / `http://<machineip>:82` um Ihre ff. Installation zu sehen (Standard-Admin-Passwort ist das, für das Sie eingegeben haben <password> oben.

## Im Docker

Ich habe ff. zu meiner Docker-Datei wie folgt komponieren:

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

Beachten Sie, dass ich ein Verzeichnis namens `/mnt/seq` (für Fenster verwenden Sie einen Fensterpfad). Hier werden die Protokolle gespeichert.

Ich habe auch eine `SEQ_DEFAULT_HASH` Umgebungsvariable, die das Hashed-Passwort für den Admin-Benutzer in meiner.env-Datei ist.

# Einrichtung des ASP.NET Core

Wie ich benutze [Serilog](https://serilog.net/) Für meine Protokollierung ist es eigentlich ziemlich einfach, Seq einzurichten. Es hat sogar docs, wie dies zu tun [Hierher](https://docs.datalust.co/docs/using-serilog).

Grundsätzlich fügen Sie einfach das Waschbecken zu Ihrem Projekt hinzu:

```shell
dotnet add package Serilog.Sinks.Seq
```

Ich bevorzuge es zu benutzen `appsettings.json` für meine config so habe ich nur die'standard' setup in meinem `Program.cs`:

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
});
```

Dann habe ich in meiner `appsettings.json' diese Konfiguration

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

Du wirst sehen, dass ich eine `serverUrl` von `http://seq:5341`......................................................................................................... Dies ist, weil ich ff laufen in einem Docker Container namens `seq` und es ist am Hafen `5341`......................................................................................................... Wenn Sie es lokal ausführen, können Sie `http://localhost:5341`.
Ich benutze auch API-Taste, so dass ich den Schlüssel verwenden kann, um die Protokollebene dynamisch zu definieren (Sie können einen Schlüssel festlegen, um nur eine bestimmte Ebene von Protokollnachrichten zu akzeptieren).

Sie richten es in Ihrem ff. Fall ein, indem Sie `http://<machine>:82` und klicken Sie oben rechts auf die Einstellungen. Klicken Sie dann auf die `API Keys` tab und fügen Sie einen neuen Schlüssel hinzu. Sie können diesen Schlüssel dann in Ihrem `appsettings.json` ..............................................................................................................................

![Abschnürung](seqapikey.png)

# Docker-Komposition

Jetzt haben wir diese Einrichtung müssen wir unsere ASP.NET-Anwendung konfigurieren, um einen Schlüssel abzuholen. Ich benutze eine `.env` Datei, um meine Geheimnisse zu speichern.

```dotenv
SEQ_DEFAULT_HASH="<adminpasswordhash>"
SEQ_API_KEY="<apikey>"
```

Dann gebe ich in meiner Docker-Kompositionsdatei an, dass der Wert als Umgebungsvariable in meine ASP.NET-App eingespritzt werden soll:

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

Beachten Sie, dass die `Serilog__WriteTo__0__Args__apiKey` wird auf den Wert von `SEQ_API_KEY` von der `.env` .............................................................................................................................. Der „0" ist der Index der `WriteTo` Bereich in der `appsettings.json` ..............................................................................................................................

# Caddy

Hinweis für Seq und meine ASP.NET App Ich habe angegeben, dass sie beide zu meinem gehören `app_network` Netz. Das liegt daran, dass ich Caddy als Reverse-Proxy verwende und es im selben Netzwerk ist. Das bedeutet, dass ich den Servicenamen als URL in meinem Caddyfile verwenden kann.

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

So ist dies in der Lage zu kartieren `seq.mostlylucid.net` an meine ff. Instanz.

# Schlußfolgerung

Seq ist ein großartiges Werkzeug für die Protokollierung und Überwachung Ihrer Anwendung. Es ist einfach einzurichten und zu verwenden und integriert sich gut mit Serilog. Ich fand es unschätzbar, meine Anwendungen zu debuggen, und ich bin sicher, Sie werden es auch.