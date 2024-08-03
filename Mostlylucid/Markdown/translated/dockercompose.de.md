# Docker-Komposition

<datetime class="hidden">2024-07-30T13:30</datetime>

<!--category-- Docker -->
Docker Compose ist ein Tool zur Definition und Ausführung von Multi-Container Docker-Anwendungen. Mit Compose konfigurieren Sie die Dienste Ihrer Anwendung mit einer YAML-Datei. Dann erstellen und starten Sie mit einem einzigen Befehl alle Dienste aus Ihrer Konfiguration.

Im Moment benutze ich Docker Compose, um ein paar Dienste auf meinem Server auszuführen.

- Mostlylucid - mein Blog (dieser)
- Cloudflared - ein Dienst, der den Datenverkehr zu meinem Server tunnelt
- Der Wachtturm - ein Dienst, der auf Updates in meinen Containern überprüft und sie bei Bedarf neu startet.

Hier ist die `docker-compose.yml` Datei, die ich benutze, um diese Dienste auszuführen:

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