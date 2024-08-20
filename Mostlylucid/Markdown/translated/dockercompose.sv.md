# Docker komposera

<datetime class="hidden">2024-07-30T13:30 Ordförande</datetime>

<!--category-- Docker -->
Docker Compose är ett verktyg för att definiera och köra multi-container Docker program. Med Compose använder du en YAML-fil för att konfigurera programtjänsterna. Sedan, med ett enda kommando, skapar du och startar alla tjänster från din konfiguration.

För tillfället använder jag Docker Compose för att köra några tjänster på min server.

- Mestlylucid - min blogg (den här)
- Cloudflared - en tjänst som tunnlar trafik till min server
- Vakttornet - en tjänst som kontrollerar uppdateringar av mina behållare och startar om dem om det behövs.

Här är `docker-compose.yml` Fil jag använder för att köra dessa tjänster:

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