# Dockerin sävellys

<datetime class="hidden">2024-07-30T13:30</datetime>

<!--category-- Docker -->
Docker Compose on työkalu monipakkaussovellusten määrittelyyn ja pyörittämiseen. Compose-sovelluksen palveluiden määrittelyyn käytetään YaML-tiedostoa. Sitten yhdellä komennolla luot ja käynnistät kaikki palvelut konfiguraatiostasi.

Tällä hetkellä käytän Docker Composea muutaman palvelun pyörittämiseen palvelimellani.

- Enimmäkseen Lucid - blogini (tämä)
- Cloudflared - palvelu, joka kuljettaa tunneleita palvelimelleni
- Vartiotorni - palvelu, joka tarkistaa konttieni päivitykset ja käynnistää ne tarvittaessa uudelleen.

Tässä on: `docker-compose.yml` Tiedosto, jota käytän näiden palveluiden ajamiseen:

```yaml
services:
  mostlylucid:
    image: scottgal/mostlylucid:latest
    labels:
        - "com.centurylinklabs.watchtower.enable=true"
  cloudflared:
    image: cloudflare/cloudflared:latest
    command: tunnel --no-autoupdate run --token ${CLOUDFLARED_TOKEN}
    env_file:
      - .env
        
  watchtower:
    image: containrrr/watchtower
    container_name: watchtower
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - WATCHTOWER_CLEANUP=true
      - WATCHTOWER_LABEL_ENABLE=true
    command: --interval 300 # Check for updates every 300 seconds (5 minutes)
```