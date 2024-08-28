# Self Hosting Seq for ASP.NET Logging

<datetime class="hidden">2024-08-28T09:37</datetime>

<!--category-- ASP.NET, Seq, Serilog, Docker -->
# Johdanto

Seq on sovellus, jonka avulla voit tarkastella ja analysoida lokitietoja. Se on loistava työkalu vianetsintään ja sovelluksen seurantaan. Tässä viestissä käsittelen, miten pystytin Seqin kirjautumaan ASP.NET Core -sovellukseeni.
Kojelautaa ei voi koskaan olla liikaa :)

![SeqDashboard](seqdashboard.png)

[TÄYTÄNTÖÖNPANO

# Seqin perustaminen

Seqissä on pari makua. Voit joko käyttää pilviversiota tai isännöidä sitä itse. Päätin itse isännöidä sitä, kun halusin pitää lokit salassa.

Ensin kävin Seqin nettisivuilla ja löysin [Docker asentaa ohjeet](https://docs.datalust.co/docs/getting-started-with-docker).

## Paikallisesti

Jos haluat ajaa paikallisesti, sinun on ensin saatava hashed-salasana. Voit tehdä tämän käyttämällä seuraavaa komentoa:

```bash
echo '<password>' | docker run --rm -i datalust/seq config hash
```

Voit ajaa sitä paikallisesti käyttämällä seuraavaa komentoa:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hashfromabove> -v C:\seq:/data -p 5443:443 -p 45341:45341 -p 5341:5341 -p 82:80 datalust/seq

```

Ubuntu paikallisella koneellani tein tästä käsikirjoituksen:

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

Sitten

```shell
chmod +x seq.sh
./seq.sh
```

Tämä saa sinut käyntiin ja sitten mene `http://localhost:82` / `http://<machineip>:82` Katso seuraava asennus (hallinan oletussalasana on se, jolle syötit <password> yllä.

## Dockerissa

Lisäsin seuraavat Dockerin säveltämän tiedoston:

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

Huomaa, että minulla on hakemisto nimeltä `/mnt/seq` (ikkunoita varten käytä ikkunapolkua). Täällä lokit säilytetään.

Minulla on myös `SEQ_DEFAULT_HASH` Ympäristömuuttuja, joka on admin-käyttäjän hashed-salasana.env-tiedostossani.

# ASP.NET-ytimen perustaminen

Käyttäessäni [Serilog](https://serilog.net/) Hakkuitani varten Seqin perustaminen on itse asiassa aika helppoa. Siinä on jopa dokumentteja siitä, miten tämä tehdään. [täällä](https://docs.datalust.co/docs/using-serilog).

Pohjimmiltaan lisäät vain lavuaarin projektiisi:

```shell
dotnet add package Serilog.Sinks.Seq
```

Käytän mieluummin `appsettings.json` Minun konfiguraationi, joten minulla on vain "normaali" asetus minun `Program.cs`:

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
});
```

Sitten minun "apsettings.json" minulla on tämä kokoonpano

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

Huomaat, että minulla on `serverUrl` huhtikuuta `http://seq:5341`...................................................................................................................................... Tämä johtuu siitä, että minulla on seuraajat juoksemassa docker-kontissa nimeltä `seq` Ja se on satamassa. `5341`...................................................................................................................................... Jos pyörität sitä paikallisesti, voit käyttää `http://localhost:5341`.
Käytän myös API-näppäintä, jotta voin käyttää avainta määrittääkseni lokitason dynaamisesti (voit asettaa avaimen, jolla voit hyväksyä vain tietyn tason lokiviestejä).

Perustit sen myöhemmässä tapauksessasi menemällä `http://<machine>:82` ja klikkaamalla asetusraitaa oikeassa yläkulmassa. Klikkaa sitten `API Keys` välilehti ja lisää uusi avain. Voit sitten käyttää tätä avainta `appsettings.json` Kansio.

![Seq](seqapikey.png)

# Dockerin sävellys

Nyt meillä on tämä järjestely, jonka avulla meidän täytyy määrittää ASP.NET-sovelluksemme, jotta voimme hakea avaimen. Käytän `.env` Kansio tallentaa salaisuuteni.

```dotenv
SEQ_DEFAULT_HASH="<adminpasswordhash>"
SEQ_API_KEY="<apikey>"
```

Sitten määritän docker compose -tiedostossani, että arvo tulisi injektoida ASP.NET-sovellukseeni ympäristömuuttujana:

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

Huomaa, että `Serilog__WriteTo__0__Args__apiKey` on asetettu arvoon `SEQ_API_KEY` Euroopan unionin toiminnasta tehdyn sopimuksen 107 artiklan 3 kohdan c alakohdan nojalla `.env` Kansio. "0" on hakemisto, jossa `WriteTo` array in the `appsettings.json` Kansio.

# Caddy

Muistiinpano sekä Seqille että ASP.NET-sovellukselleni. Olen tarkentanut, että molemmat kuuluvat minun `app_network` verkko. Tämä johtuu siitä, että käytän Caddya käänteisenä valtakirjana ja se on samassa verkossa. Tämä tarkoittaa, että voin käyttää Caddy-tiedostossani palvelunimeä URL-osoitteena.

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

Joten tämä pystyy kartoittamaan `seq.mostlylucid.net` Seuraavaan tapaukseeni.

# Päätelmät

Seq on loistava työkalu hakemuksesi kirjaukseen ja seurantaan. Serilogin kanssa on helppo perustaa, käyttää ja integroitua hyvin. Olen huomannut, että se on korvaamatonta sovellusteni vian selvittämisessä, ja olen varma, että se on teidänkin ansiotanne.