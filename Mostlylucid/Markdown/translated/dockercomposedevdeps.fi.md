# Dockerin sävellyksen käyttö kehityssidonnaisuuksiin

<!--category-- Docker -->
<datetime class="hidden">2024-08-09T17:17</datetime>

# Johdanto

Kun kehitimme ohjelmistoa perinteisesti, keräsimme tietokannan, viestijonon, välimuistin ja ehkä muutaman muun palvelun. Tätä voi olla vaikea hallita, etenkin jos on tekemässä useita projekteja. Docker Compose on työkalu, jonka avulla voit määritellä ja pyörittää monikonttisia Docker-sovelluksia. Se on hyvä tapa hallita kehitysriippuvuuksia.

Tässä viestissä näytän, miten Docker Composea käytetään kehitysriippuvuuksien hallintaan.

[TÄYTÄNTÖÖNPANO

# Edeltävät opinnot

Ensin sinun täytyy asentaa docker-työpöytä millä tahansa alustalla, jota käytät. Voit ladata sen osoitteesta [täällä](https://www.docker.com/products/docker-desktop).

**HUOMAUTUS: Olen havainnut, että Windowsissa sinun täytyy todella ajaa Docker Desktop -asentajaa ylläpitäjänä varmistaaksesi, että se asennetaan oikein.**

# Docker Composite -tiedoston luominen

Docker Compose käyttää YaML-tiedostoa määritelläkseen palvelut, joita haluat ajaa. Tässä esimerkki yksinkertaisesta `devdeps-docker-compose.yml` tiedosto, joka määrittelee tietokantapalvelun ja sähköpostipalvelun:

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

Huomaa, että olen määrittänyt tilavuudet kunkin palvelun tietojen pysyvyydelle, tässä olen määrittänyt

```yaml
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    volumes:
        - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
```

Näin varmistetaan, että tiedot pysyvät konttien juoksujen välillä.

Määrittelen myös, että `env_file` Euroopan parlamentin ja neuvoston asetus (EU) N:o 1306/2013, annettu 11 päivänä joulukuuta 2013, Euroopan parlamentin ja neuvoston asetuksen (EU) N:o 1306/2013 täytäntöönpanosta (EUVL L 347, 20.12.2013, s. 1). `postgres` palvelu. Tämä on tiedosto, joka sisältää konttiin syötettyjä ympäristömuuttujia.
Voit katsoa listan ympäristömuuttujista, jotka voidaan siirtää PostgreSQL-konttiin [täällä](https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/#1-Environment-variables).
Tässä on esimerkki `.env` tiedosto:

```shell
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<somepassword>
```

Tämä määrittää PostgreSQL:n oletustietokannan, salasanan ja käyttäjän.

Täällä pyöritän myös SMTP4Dev-palvelua, joka on loistava työkalu sähköpostin toimivuuden testaamiseen sovelluksessasi. Lisää tietoa siitä saat [täällä](https://github.com/rnwood/smtp4dev/wiki/Installation#how-to-run-smtp4dev-in-docker).

Jos katsot minun `appsettings.Developmet.json` Tiedosto näet, että minulla on SMTP-palvelimen seuraava kokoonpano:

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

Tämä toimii SMTP4Deville, ja sen avulla voin testata tämän toiminnon (voin lähettää mihin tahansa osoitteeseen ja katsoa sähköpostin SMTP4Dev-rajapintaan osoitteessa http://localhost:3002/).

Kun olet varma, että kaikki toimii, voit testata oikeaa SMTP-palvelinta kuten GMAIL (esim. [täällä](addingasyncsendingforemails) siitä, miten se tehdään)

# Palveluiden johtaminen

Suorittaa palvelut, jotka on määritelty `devdeps-docker-compose.yml` Tiedosto, sinun täytyy suorittaa seuraava komento samassa kansiossa kuin tiedosto:

```shell
docker compose -f .\devdeps-docker-compose.yml up -d
```

Huomaa, että se kannattaa ajaa aluksi näin. Näin näet konfiguraation elementit, jotka on siirretty `.env` Kansio.

```shell
docker compose -f .\devdeps-docker-compose.yml config
```

Jos katsot Docker Desktop -työpöydälle, näet nämä palvelut käynnissä

![Dockerin työpöytä](dockerdesktopdev.png)