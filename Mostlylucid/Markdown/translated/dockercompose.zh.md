# docker 合成器

<datetime class="hidden">2024-07-30-13:30</datetime>

<!--category-- Docker -->
Docker Compose 是一个定义和运行多容器 Docker 应用程序的工具。 有了 Commpte, 您可以使用 YAML 文件来配置应用程序的服务。 然后, 如果有一个单项命令, 您可以从配置中创建和启动所有服务 。

此时此刻,我使用多克·康波斯 在我的服务器上运行一些服务。

- Mostlyluccid - 我的博客(这个)
- Cloudflared - 一种服务 隧道通到我的服务器
- 监视器 - 一种服务 检查我的集装箱的更新情况 必要时重新启动

这儿就是`docker-compose.yml`我用文件运行这些服务 :

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