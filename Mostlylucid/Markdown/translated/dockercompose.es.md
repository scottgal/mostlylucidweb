# Docker Composite

<datetime class="hidden">2024-07-30T13:30</datetime>

<!--category-- Docker -->
Docker Compose es una herramienta para definir y ejecutar aplicaciones Docker multicontenedor. Con Compose, utiliza un archivo YAML para configurar los servicios de su aplicación. A continuación, con un solo comando, crea e inicia todos los servicios desde su configuración.

Por el momento utilizo Docker Compose para ejecutar algunos servicios en mi servidor.

- Mayormente lúcido - mi blog (este)
- Cloudflared - un servicio que los túneles de tráfico a mi servidor
- Watchtower - un servicio que comprueba si hay actualizaciones en mis contenedores y los reinicia si es necesario.

Aquí está el`docker-compose.yml`archivo que utilizo para ejecutar estos servicios:

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