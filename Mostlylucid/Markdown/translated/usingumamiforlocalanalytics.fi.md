# Umamin käyttö paikallisissa analyyseissä

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-08T15:53</datetime>

## Johdanto

Yksi asia, joka ärsytti minua nykyisessä asetelmassani, oli Google Analyticsin käyttäminen kävijätietojen hankkimiseen (mitä vähän siitä on?). Joten halusin löytää jotain, jota voisin isännöidä itse, joka ei välittäisi dataa Googlelle tai muulle kolmannelle osapuolelle. Löysin [Umami](https://umami.is/) joka on yksinkertainen, omatoiminen verkkoanalytiikkaratkaisu. Se on loistava vaihtoehto Google Analyticsille ja se on (suhteellisesti) helppo asentaa.

[TÄYTÄNTÖÖNPANO

## Asennus

Asennus on PRETTTY yksinkertainen, mutta kesti melkoisen rukkaset, jotta se todella lähti käyntiin...

### Dockerin sävellys

Kun halusin lisätä Umamin nykyiseen docker-kokonaisuuteeni, tarvitsin uuden palvelun `docker-compose.yml` Kansio. Lisäsin tiedoston pohjaan seuraavan:

```yaml
  umami:
    image: ghcr.io/umami-software/umami:postgresql-latest
    env_file: .env
    environment:
      DATABASE_URL: ${DATABASE_URL}
      DATABASE_TYPE: ${DATABASE_TYPE}
      HASH_SALT: ${HASH_SALT}
      APP_SECRET: ${APP_SECRET}
      TRACKER_SCRIPT_NAME: getinfo
      API_COLLECT_ENDPOINT: all
    ports:
      - "3000:3000"
    depends_on:
      - db
    networks:
      - app_network
    restart: always
  db:
    image: postgres:16-alpine
    env_file:
      - .env
    networks:
      - app_network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 5s
      timeout: 5s
      retries: 5
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
    restart: always
  cloudflaredumami:
    image: cloudflare/cloudflared:latest
    command: tunnel --no-autoupdate run --token ${CLOUDFLARED_UMAMI_TOKEN}
    env_file:
      - .env
    restart: always
    networks:
      - app_network


```

Tämä docker-compose.yml-tiedosto sisältää seuraavat asetukset:

1. Uusi palvelu nimeltä `umami` jossa käytetään `ghcr.io/umami-software/umami:postgresql-latest` kuva. Tätä palvelua käytetään Umamin analytiikkapalvelun johtamiseen.
2. Uusi palvelu nimeltä `db` jossa käytetään `postgres:16-alpine` kuva. Tätä palvelua käytetään hoitamaan Postin tietokantaa, jota Umami käyttää tietojensa tallentamiseen.
   Huomaa tämä palvelu Olen kartoittanut sen palvelimellani olevaan hakemistoon, jotta tiedot pysyvät käynnissä uudelleen.

```yaml
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
```

Tarvitset tämän ohjaajan olemassaolon ja kirjoitettavaksi palvelimellasi olevan docker-käyttäjän toimesta (ei taaskaan Linux-asiantuntijaa, joten 777 on todennäköisesti ylilyönti täällä!).

```shell
chmod 777 /mnt/umami/postgres
```

3. Uusi palvelu nimeltä `cloudflaredumami` jossa käytetään `cloudflare/cloudflared:latest` kuva. Tätä palvelua käytetään tunneleiden rakentamiseen Cloudflaren kautta, jotta palveluun pääsee internetistä.

### Env- tiedosto

Tämän tueksi päivitin myös `.env` tiedosto, joka sisältää seuraavat tiedot:

```shell
CLOUDFLARED_UMAMI_TOKEN=<cloudflaretoken>
DATABASE_TYPE=postgresql
HASH_SALT=<salt>

POSTGRES_DB=postgres
POSTGRES_USER=<postgresuser>
POSTGRES_PASSWORD=<postgrespassword>
UMAMI_SECRET=<umamisecret>

APP_SECRET=${UMAMI_SECRET}
UMAMI_USER=${POSTGRES_USER}
UMAMI_PASS=${POSTGRES_PASSWORD}
DATABASE_URL=postgresql://${UMAMI_USER}:${UMAMI_PASS}@db:5432/${POSTGRES_DB}
```

Tämä asettaa kokoonpanon docker compose ( `<>` Elemetit täytyy tietenkin korvata omilla arvoilla). Erytropoietiini `cloudflaredumami` Palvelua käytetään tunneleihin, jotka kulkevat Cloudflaren kautta, jotta palveluun pääsee internetistä. On mahdollista käyttää BASE_PATHia, mutta Umamille se tarvitsee ärsyttävästi peruspolun uudelleenrakentamista, joten olen jättänyt sen juuripoluksi toistaiseksi.

### Cloudflore-tunneli

Tätä varten (joka toimii analytiikkaan käytettävän js-tiedoston polkuna - getinfo.js) pystytin pilvenpiirtäjätunnelin verkkosivulla:

![Cloudflore-tunneli](umamisetup.png)

Tämä luo tunnelin Umami-palveluun ja mahdollistaa sen pääsyn internetistä. Huomaa, että osoitan tämän: `umami` Docker-kokonaisuuden tiedoston palvelu (koska se on samassa verkossa kuin pilvitunneli, se on pätevä nimi).

### Umami-asetus sivulle

Ottaaksesi skriptin polun käyttöön (kutsutaan `getinfo` Asetuksissani yllä) Olen lisännyt asetuksiini konfiguraation

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net/getinfo",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
 },
```

Voit myös lisätä nämä.env-tiedostoosi ja siirtää ne ympäristömuuttujina docker-kokonaisuuteen.

```shell
ANALYTICS__UMAMIPATH="https://umamilocal.mostlylucid.net/getinfo"
ANALYTICS_WEBSITEID="32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
```

```yaml
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    environment:
    ...
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
```

Lavastit WebsiteIdin Umamin kojelautaan, kun perustit sivuston. (Huomaa, että Umami-palvelun oletuskäyttäjätunnus ja -salasana on `admin` sekä `umami`, Sinun täytyy muuttaa näitä asennuksen jälkeen).
![Umami Dashboard](umamiaddwebsite.png)

Aiheeseen liittyvällä asetuksilla cs-tiedosto:

```csharp
public class AnalyticsSettings : IConfigSection
{
    public static string Section => "Analytics";
    public string? UmamiPath { get; set; }
}
```

Tässäkin käytetään POCO-konfiguraatiota ([täällä](/blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)) Asetukset asetetaan.
Aseta se ohjelmaani.cs:

```csharp
builder.Configure<AnalyticsSettings>();
```

Ja lopulta minun `BaseController.cs` `OnGet` Method I've additioned the play for the analytics script:

```csharp
   public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!Request.IsHtmx())
        {
            ViewBag.UmamiPath = _analyticsSettings.UmamiPath;
            ViewBag.UmamiWebsiteId = _analyticsSettings.WebsiteId;
        }
        base.OnActionExecuting(filterContext);
    }
    
```

Tämä asettaa layout-tiedostossa käytettävän analytiikkaskriptin polun.

### Asettelutiedosto

Lopulta lisäsin layout-tiedostouni analytiikkaskriptin seuraavasti:

```html
<script defer src="@ViewBag.UmamiPath" data-website-id="@ViewBag.UmamiWebsiteId"></script>
```

Tämä sisältää sivun käsikirjoituksen ja asettaa analytiikkapalvelun verkkosivun tunnuksen.

## Jätä itsesi pois analytiikasta

Jotta omat vierailut eivät jäisi analytiikkatietojen ulkopuolelle, voit lisätä selaimeesi seuraavan paikallisen tallenteen:

Chrome dev -työkaluissa (Ctrl+Shift+I ikkunoissa) voit lisätä konsoliin seuraavat:

```javascript
localStorage.setItem("umami.disabled", 1)
```

## Päätelmät

Tämä oli aika kummallista, mutta olen tyytyväinen lopputulokseen. Minulla on nyt omapäinen analytiikkapalvelu, joka ei välitä dataa Googlelle tai muulle kolmannelle osapuolelle. Sitä on vähän hankala järjestää, mutta kun se on tehty, sitä on aika helppo käyttää. Olen tyytyväinen tulokseen ja suosittelen sitä kaikille, jotka etsivät itseohjautuvaa analytiikkaratkaisua.