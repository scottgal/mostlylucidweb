# Self Hosting Seq for ASP.NET Logging

<datetime class="hidden">2024-08-28T09:37</datetime>
<!--category-- ASP.NET, Seq, Serilog, Docker -->

# Introduction
Seq is an application which lets you view and analyse logs. It's a great tool for debugging and monitoring your application. In this post I'll cover how I set up Seq to log my ASP.NET Core application.
Can never have too many dashboards :)

![SeqDashboard](seqdashboard.png)

[TOC]

# Setting up Seq
Seq comes in a couple of flavours. You can either use the cloud version or self host it. I chose to self host it as I wanted to keep my logs private. 

First I started by visiting the Seq website and finding the [Docker install instructions](https://docs.datalust.co/docs/getting-started-with-docker).

## Locally
To run locally you first need to get a hashed password. You can do this by running the following command:

```bash
echo '<password>' | docker run --rm -i datalust/seq config hash
```
To run it locally you can use the following command:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hashfromabove> -v C:\seq:/data -p 5443:443 -p 45341:45341 -p 5341:5341 -p 82:80 datalust/seq

````

On my Ubuntu local machine I made this into an sh script:

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

Then 
```shell
chmod +x seq.sh
./seq.sh
```

This will get you up and running then go to `http://localhost:82` / `http://<machineip>:82` to see your seq install (default admin password is the one you entered for <password> above.

## In Docker
I added seq to my Docker compose file as follows:

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
Note that I have a directory called `/mnt/seq` (for windows, use a windows path). This is where the logs will be stored.

I also have a `SEQ_DEFAULT_HASH` environment variable which is the hashed password for the admin user in my .env file.

# Setting up ASP.NET Core
As I use [Serilog](https://serilog.net/) for my logging it's actually pretty easy to set up Seq. It even has docs on how to do this [here](https://docs.datalust.co/docs/using-serilog). 

Basically you just add the sink to your project:
```shell
dotnet add package Serilog.Sinks.Seq
```

I prefer to use `appsettings.json` for my config so I just have the 'standard' setup in my `Program.cs`:

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
});
```
Then in my `appsettings.json' I have this configuration
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

You'll see that I have a `serverUrl` of `http://seq:5341`. This is because I have seq running in a docker container called `seq` and it's on port `5341`. If you're running it locally you can use `http://localhost:5341`.
I also use API key so I can use the key to specify the log level dynamically (you can set a key to only accept a certain level of log messages).

You set it up in your seq instance by going to `http://<machine>:82` and clicking on the settings cog in the top right. Then click on the `API Keys` tab and add a new key. You can then use this key in your `appsettings.json` file.

![Seq](seqapikey.png)

# Docker Compose
Now we have this set up we need to configure our ASP.NET application to pick up a key. I use a `.env` file to store my secrets.

```dotenv
SEQ_DEFAULT_HASH="<adminpasswordhash>"
SEQ_API_KEY="<apikey>"
```

Then in my docker compose file I specify that the value should be injected as an environment variable into my ASP.NET app:

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

Note that the `Serilog__WriteTo__0__Args__apiKey` is set to the value of `SEQ_API_KEY` from the `.env` file. The '0' is the index of the `WriteTo` array in the `appsettings.json` file.

# Caddy
Note for both Seq and my ASP.NET app I've specified they both belong to my `app_network` network. This is because I use Caddy as a reverse proxy and it's on the same network. This means I can use the service name as the URL in my Caddyfile.

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

So this is able to map `seq.mostlylucid.net` to my seq instance.

# Conclusion
Seq is a great tool for logging and monitoring your application. It's easy to set up and use and integrates well with Serilog. I've found it invaluable in debugging my applications and I'm sure you will too.