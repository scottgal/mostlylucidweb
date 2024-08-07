# Docker-composeComment

<datetime class="hidden">2024-07-30T13:30</datetime>

<!--category-- Docker -->
Docker Compose is een hulpmiddel voor het definiëren en uitvoeren van multi-container Docker-toepassingen. Met Compose gebruik je een YAML-bestand om de services van je toepassing te configureren. Dan maak en start je met één commando alle diensten vanuit je configuratie.

Op dit moment gebruik ik Docker Compose om een paar services op mijn server uit te voeren.

- Meest lucid - mijn blog (deze)
- Cloudflared - een dienst die tunnels verkeer naar mijn server
- Watchtower - een dienst die controleert op updates van mijn containers en herstart ze indien nodig.

Hier is de`docker-compose.yml`bestand dat ik gebruik om deze diensten uit te voeren:

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