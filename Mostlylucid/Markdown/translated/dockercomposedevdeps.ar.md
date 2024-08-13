# استخدام Dokker Compus for Devel التنمية

<!--category-- Docker -->
<datetime class="hidden">2024-08-08-09-TT17: 17</datetime>

# أولاً

عندما نطوّر البرمجيات تقليدياً كنا نطوّر قاعدة بيانات، و قائمة رسائل، ومخبأ، وربما بعض الخدمات الأخرى. هذا يمكن أن يكون من الصعب إدارته، خاصة إذا كنت تعمل على مشاريع متعددة. Doker Compus أداة تسمح لك بتعريف وتشغيل تطبيقات Ducker متعددة الحاويات. إنّها طريقة عظيمة لإدارة مُعتمديّات تطوّرك.

في هذا الموقع، سأريكم كيف تستخدمون Doker Compus لإدارة معتمديات تطويركم.

[رابعاً -

# النفقات قبل الاحتياجات

أولاً ستحتاج لتثبيت سطح المكتب الدوكر على أي منصة تستخدمها. يمكنك تحميلها من [هنا هنا](https://www.docker.com/products/docker-desktop).

**ملاحظة: لقد وجدت أنه على النوافذ تحتاج حقاً لتشغيل Doker file pleter كمدير لضمان تثبيتها بشكل صحيح.**

# يجري إنشاء ملفّ

يستخدم Doker Compus ملف YAML لتعريف الخدمات التي تريد تشغيلها. هنا مثال لـ a `devdeps-docker-compose.yml` ملفّ تعريف a قاعدة بيانات خدمة:

```yaml
services: 
  smtp4dev:
    image: rnwood/smtp4dev
    ports:
      - "3002:80"
      - "2525:25"
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    restart: always
  postgres:
    image: postgres:16-alpine
    container_name: postgres
    ports:
      - "5432:5432"
    env_file:
      - .env
    volumes:
      - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
    restart: always	
networks:
  mynetwork:
        driver: bridge
```

ملاحظة هنا قمت بتحديد مجلدات لاستمرار البيانات لكل خدمة، هنا قمت بتحديد

```yaml
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    volumes:
        - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
```

ويكفل ذلك استمرار البيانات بين فترات سير الحاويات.

حسناً على ذلك `env_file` - - - - - - - - - `postgres` (بدولارات الولايات المتحدة) هذا ملف يحتوي على المتغيرات البيئية التي تنتقل إلى الحاوية.
يمكنك أن ترى قائمة بالمتغيرات البيئية التي يمكن نقلها إلى حاوية PostgreSQL [هنا هنا](https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/#1-Environment-variables).
هنا مثال لـ a `.env` 

```shell
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<somepassword>
```

هذا هو a افتراضي قاعدة بيانات كلمة مرور و مستخدم لـ PostgreSQL.

هنا أيضاً أدير خدمة SMTP4Dev، هذه أداة عظيمة لاختبار وظيفة البريد الإلكتروني في تطبيقك. يمكنك أن تجد المزيد من المعلومات عنه [هنا هنا](https://github.com/rnwood/smtp4dev/wiki/Installation#how-to-run-smtp4dev-in-docker).

إذا نظرت في `appsettings.Developmet.json` الملف سوف ترون لدي تشكيل التال لخادم SMTP:

```json
  "SmtpSettings":
{
"Server": "smtp.gmail.com",
"Port": 587,
"SenderName": "Mostlylucid",
"Username": "",
"SenderEmail": "scott.galloway@gmail.com",
"Password": "",
"EnableSSL": "true",
"EmailSendTry": 3,
"EmailSendFailed": "true",
"ToMail": "scott.galloway@gmail.com",
"EmailSubject": "Mostlylucid"

}
```

هذا يعمل لـ SMTP4Dev وهو يُمْكِنُني أَنْ أُختبرَ هذه الخاصيةِ (يُمْكِنُ أَنْ أُرسلَ إلى أيّ عنوان، ويَرى البريد الإلكتروني في واجهةِ SMTP4Dev على http://docalhost:3002/).

بمجرد أن تتأكد من أنها تعمل كلها يمكنك اختبار على خادم SMTP الحقيقي مثل GMAIL (على سبيل المثال، انظر [هنا هنا](addingasyncsendingforemails) عن كيفية القيام بذلك)

# تشغيل الخدمات

إلى تشغيل الخدمات المحددة في `devdeps-docker-compose.yml` ملفّ، تحتاج إلى تنفيذ الأمر التالي في نفس دليل الملف:

```shell
docker compose -f .\devdeps-docker-compose.yml up -d
```

ملاحظة يجب عليك تشغيلها في البداية هكذا; هذا يضمن أنك تستطيع رؤية عناصر `.env` ملف ملفّيّاً.

```shell
docker compose -f .\devdeps-docker-compose.yml config
```

الآن إذا نظرت في مكتب Docكر يمكنك أن ترى هذه الخدمات تعمل

![تنفيذ تنفيذ](dockerdesktopdev.png)