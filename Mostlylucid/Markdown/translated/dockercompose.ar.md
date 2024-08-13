# الأمر المُقْرِر

<datetime class="hidden">2024-07-30TT 13:30</datetime>

<!--category-- Docker -->
Dokker Compus هو أداة لتعريف وتشغيل تطبيقات Ducker متعددة الحاويات. مع مُكوّن إستعمل a ملفّ إلى ضبط تطبيق s خدمة. ثم، مع أمر واحد، تقوم بإنشاء وبدء كل الخدمات من تشكيلك.

في هذه اللحظة أستخدم Doker Compus لتشغيل عدد قليل من الخدمات على خادمي.

- مدونتي (هذه)
- سحابة - خدمة تنقل حركة المرور إلى خادمي
- - برج المراقبة الذي يفحص تحديثات حاوياتي ويعيد تشغيلها إذا لزم الأمر.

هذا هو `docker-compose.yml` أنا استخدام إلى تشغيل هذه الخدمات:

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