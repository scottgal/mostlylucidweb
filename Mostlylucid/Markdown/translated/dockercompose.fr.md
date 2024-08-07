# Composez Docker

<datetime class="hidden">2024-07-30T13:30</datetime>

<!--category-- Docker -->
Docker Compose est un outil pour définir et exécuter des applications Docker multiconteneurs. Avec Compose, vous utilisez un fichier YAML pour configurer les services de votre application. Ensuite, avec une seule commande, vous créez et démarrez tous les services à partir de votre configuration.

Pour le moment, j'utilise Docker Compose pour exécuter quelques services sur mon serveur.

- Mostlylucide - mon blog (celui-ci)
- Cloudflared - un service qui tunnele le trafic vers mon serveur
- La Tour de Garde - un service qui vérifie les mises à jour de mes conteneurs et les redémarre si nécessaire.

Voici le`docker-compose.yml`fichier J'utilise pour exécuter ces services:

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