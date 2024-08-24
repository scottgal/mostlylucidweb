# Docker Componi

<datetime class="hidden">2024-07-30T13:30</datetime>

<!--category-- Docker -->
Docker Compose è uno strumento per definire ed eseguire applicazioni Docker multi-container. Con Compose, si utilizza un file YAML per configurare i servizi della propria applicazione. Poi, con un unico comando, si creano e si avviano tutti i servizi dalla configurazione.

Al momento uso Docker Compose per eseguire alcuni servizi sul mio server.

- Mostlylucid - il mio blog (questo)
- Cloudflared - un servizio che tunnel il traffico al mio server
- Torre di Controllo - un servizio che controlla gli aggiornamenti ai miei contenitori e li riavvia se necessario.

Qui c'è il `docker-compose.yml` file che uso per eseguire questi servizi:

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