# Докер Композитний

<datetime class="hidden">2024- 07- 30T13: 30</datetime>

<!--category-- Docker -->
Docker Compose - це інструмент для визначення і запуску програм з декількома контейнерами. За допомогою Compose ви можете скористатися файлом YAML для налаштування служб вашої програми. Потім за однією командою ви створюєте і запускаєте всі служби з вашої конфігурації.

Зараз я використовую Docker Compose, щоб запустити декілька служб на моєму сервері.

- В основному - мій блог (цей)
- Об' єднаний хмаром - послуга, яка забезпечує трафік тунелів на мій сервер
- " Вартова башта " - сервіс, який перевіряє наявність оновлень у моїх контейнерах і, якщо потрібно, перезапускає їх.

Ось, будь ласка. `docker-compose.yml` файл, який використовується для запуску таких служб:

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