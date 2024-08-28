# Seq auto-hébergement pour ASP.NET Logging

<datetime class="hidden">2024-08-28T09:37</datetime>

<!--category-- ASP.NET, Seq, Serilog, Docker -->
# Présentation

Seq est une application qui vous permet de visualiser et d'analyser les journaux. C'est un excellent outil de débogage et de surveillance de votre application. Dans ce post, je vais couvrir comment j'ai configuré Seq pour enregistrer mon application ASP.NET Core.
Ne peut jamais avoir trop de tableaux de bord :)

![Tableau de bord Seq](seqdashboard.png)

[TOC]

# Mise en place de Seq

Seq vient dans quelques saveurs. Vous pouvez soit utiliser la version cloud ou vous-même l'héberger. J'ai choisi de m'héberger comme je voulais garder mes registres privés.

Tout d'abord, j'ai commencé par visiter le site web de Seq et trouver le [Docker installe les instructions](https://docs.datalust.co/docs/getting-started-with-docker).

## Localement

Pour courir localement, vous devez d'abord obtenir un mot de passe hashed. Vous pouvez le faire en exécutant la commande suivante :

```bash
echo '<password>' | docker run --rm -i datalust/seq config hash
```

Pour l'exécuter localement, vous pouvez utiliser la commande suivante :

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hashfromabove> -v C:\seq:/data -p 5443:443 -p 45341:45341 -p 5341:5341 -p 82:80 datalust/seq

```

Sur ma machine locale Ubuntu j'ai fait ceci en un sh script:

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

Alors

```shell
chmod +x seq.sh
./seq.sh
```

Ça va vous faire courir, puis aller à `http://localhost:82` / `http://<machineip>:82` pour voir votre suite d'installation (le mot de passe admin par défaut est celui que vous avez saisi pour <password> ci-dessus.

## Dans Docker

J'ai ajouté la suite de mon fichier de composition Docker comme suit:

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

Notez que j'ai un répertoire appelé `/mnt/seq` (pour les fenêtres, utilisez un chemin de fenêtres). C'est là que les journaux seront stockés.

J'ai aussi une `SEQ_DEFAULT_HASH` variable d'environnement qui est le mot de passe hashed pour l'utilisateur admin dans mon fichier.env.

# Configuration du noyau ASP.NET

Comme j'utilise [Sérilog](https://serilog.net/) Pour mon enregistrement, c'est assez facile d'installer Seq. Il a même des docs sur la façon de faire cela [Ici.](https://docs.datalust.co/docs/using-serilog).

Fondamentalement, vous ajoutez simplement l'évier à votre projet:

```shell
dotnet add package Serilog.Sinks.Seq
```

Je préfère utiliser `appsettings.json` pour ma config donc j'ai juste la configuration'standard' dans mon `Program.cs`:

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
});
```

Puis dans mon `appsettings.json' j'ai cette configuration

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

Vous verrez que j'ai un `serverUrl` des `http://seq:5341`C'est ce que j'ai dit. C'est parce que j'ai la suite courir dans un conteneur de docker appelé `seq` et c'est sur le port `5341`C'est ce que j'ai dit. Si vous l'utilisez localement, vous pouvez l'utiliser. `http://localhost:5341`.
J'utilise aussi la clé API afin de pouvoir utiliser la clé pour spécifier dynamiquement le niveau de log (vous pouvez définir une clé pour n'accepter qu'un certain niveau de messages log).

Vous l'avez mis en place dans votre instance suivante en allant à `http://<machine>:82` et en cliquant sur les paramètres en haut à droite. Ensuite, cliquez sur le `API Keys` onglet et ajouter une nouvelle clé. Vous pouvez alors utiliser cette clé dans votre `appsettings.json` fichier.

![Séq.](seqapikey.png)

# Composez Docker

Maintenant, nous avons cette configuration nous devons configurer notre application ASP.NET pour récupérer une clé. J'utilise un `.env` fichier pour stocker mes secrets.

```dotenv
SEQ_DEFAULT_HASH="<adminpasswordhash>"
SEQ_API_KEY="<apikey>"
```

Ensuite, dans mon fichier de composition, je précise que la valeur doit être injectée comme variable d'environnement dans mon application ASP.NET:

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

Il convient de noter que les `Serilog__WriteTo__0__Args__apiKey` est défini à la valeur de `SEQ_API_KEY` de l'Organisation des Nations Unies pour l'élimination de toutes les formes de discrimination à l'égard des femmes `.env` fichier. Le '0' est l'indice de la `WriteTo` tableau dans le `appsettings.json` fichier.

# Caddy

Note pour Seq et mon application ASP.NET J'ai précisé qu'ils appartiennent tous les deux à mon `app_network` réseau. C'est parce que j'utilise Caddy comme proxy inversé et c'est sur le même réseau. Cela signifie que je peux utiliser le nom de service comme URL dans mon Caddyfile.

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

Donc ceci est capable de cartographier `seq.mostlylucid.net` à ma suite.

# Le présent règlement entre en vigueur le vingtième jour suivant celui de sa publication au Journal officiel de l'Union européenne.

Seq est un excellent outil pour enregistrer et surveiller votre application. Il est facile à configurer et à utiliser et s'intègre bien avec Serilog. Je l'ai trouvé inestimable dans le débogage de mes applications et je suis sûr que vous le ferez aussi.