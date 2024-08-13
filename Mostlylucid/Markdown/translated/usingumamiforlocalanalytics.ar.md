# محلات التحليل المحلّي

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-08-08-08-TT15: 53</datetime>

## أولاً

أحد الأشياء التي أزعجتني حول إعدادي الحالي كان اضطراري لاستخدام تحليل جوجل للحصول على بيانات الزائرين (ما القليل منها؟). لذا أردت أن أجد شيئاً يمكنني أن أدعمه ذاتياً والذي لم ينقل البيانات إلى جوجل أو أي طرف ثالث آخر. وَجدَتُ [ما قبل ما ما قبل ما ما قبل](https://umami.is/) وهو عبارة عن حل تحليلي بسيط ومبني ذاتياً على شبكة الإنترنت. إنه بديل عظيم لـ جوجل التحليلي و (بشكل نسبي) سهل الإعداد.

[رابعاً -

## عدد أفراد

التركيب بسيط جداً لكنه أخذ القليل من التشويش ليبدأ حقاً...

### الأمر المُقْرِر

كما أردت أن أضيف (أوميامي) إلى وضعي الحالي لـ (دوككر) المدمج أحتاج إلى إضافة خدمة جديدة إلى `docker-compose.yml` ملف ملفّيّاً. لقد اضفت التالي لأسفل الملف:

```yaml
  umami:
    image: ghcr.io/umami-software/umami:postgresql-latest
    env_file: .env
    environment:
      DATABASE_URL: ${DATABASE_URL}
      DATABASE_TYPE: ${DATABASE_TYPE}
      HASH_SALT: ${HASH_SALT}
      APP_SECRET: ${APP_SECRET}
      TRACKER_SCRIPT_NAME: getinfo
      API_COLLECT_ENDPOINT: all
    ports:
      - "3000:3000"
    depends_on:
      - db
    networks:
      - app_network
    restart: always
  db:
    image: postgres:16-alpine
    env_file:
      - .env
    networks:
      - app_network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 5s
      timeout: 5s
      retries: 5
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
    restart: always
  cloudflaredumami:
    image: cloudflare/cloudflared:latest
    command: tunnel --no-autoupdate run --token ${CLOUDFLARED_UMAMI_TOKEN}
    env_file:
      - .env
    restart: always
    networks:
      - app_network


```

هذا doker- compunse.yml ملفّ يحتوي على المُعَزِّز التالي:

1. خدمة جديدة تدعى `umami` التي تستخدم `ghcr.io/umami-software/umami:postgresql-latest` (أ) صورة مصوّرة. وتُستخدم هذه الخدمة لإدارة خدمة تحليل أميمي.
2. خدمة جديدة تدعى `db` التي تستخدم `postgres:16-alpine` (أ) صورة مصوّرة. وتستخدم هذه الخدمة لإدارة قاعدة بيانات Postgres التي تستخدمها أومامي لتخزين بياناتها.
   ملاحظة لهذه الخدمة قمت بترقيمها إلى دليل على خادمي بحيث أن البيانات مستمرة بين عمليات إعادة التشغيل.

```yaml
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
```

ستحتاج لهذا المخرج أن يكون موجوداً و قابلاً للكتابة من قبل مستخدم الدوكر على خادمك (مجدداً ليس خبير لينكس لذا 777 من المحتمل أن يبالغ في القتل هنا!)ع(

```shell
chmod 777 /mnt/umami/postgres
```

3. خدمة جديدة تدعى `cloudflaredumami` التي تستخدم `cloudflare/cloudflared:latest` (أ) صورة مصوّرة. تستخدم هذه الخدمة لحفر خدمة أمامي من خلال سحاب فلادر للسماح بالوصول إليها من الإنترنت.

### 

ودعماً لذلك، قمت أيضاً بتحديث `.env` إلى تضمين ما يلي:

```shell
CLOUDFLARED_UMAMI_TOKEN=<cloudflaretoken>
DATABASE_TYPE=postgresql
HASH_SALT=<salt>

POSTGRES_DB=postgres
POSTGRES_USER=<postgresuser>
POSTGRES_PASSWORD=<postgrespassword>
UMAMI_SECRET=<umamisecret>

APP_SECRET=${UMAMI_SECRET}
UMAMI_USER=${POSTGRES_USER}
UMAMI_PASS=${POSTGRES_PASSWORD}
DATABASE_URL=postgresql://${UMAMI_USER}:${UMAMI_PASS}@db:5432/${POSTGRES_DB}
```

هذا تشكيل تشكيل لـ dokker Comnus (الـ `<>` من الواضح أن النخبة تحتاج إلى استبدالها بقيمك الخاصة). الـ `cloudflaredumami` تُستخدم هذه الخدمة في نفق خدمة أمامي عبر سحابة الفلور للسماح بالوصول إليها من الإنترنت. من الممكن أن نستخدم Base_PATH لكن بالنسبة لأمامي تحتاج بشكل مزعج إلى إعادة بناء لتغيير مسار القاعدة لذا تركته كمسار الجذر في الوقت الراهن.

### طُرْق سُنْ

لتثبيت نفق سحابة flare لهذا (الذي يعمل كمسار لملف js المستخدم للتحليلات - get info. js) استخدمت الموقع الإلكتروني:

![طُرْق سُنْ](umamisetup.png)

هذا يُنشئ النفق إلى خدمة أمّامي ويُمكّن من الوصول إليه من الإنترنت. ملاحظة، أُشير بهذا إلى `umami` كما هو موجود على نفس الشبكة التي يعمل بها نفق الغيوم فهو اسم ساري المفعول.

### إعدادات صورة بوصة صفحة

إلى تمكين المسار لـ نص `getinfo` في وضعي على أعلى) لقد أضفت مدخلاً للحل إلى حساباتي

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net/getinfo",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
 },
```

يمكنك أيضاً إضافة هذه إلى ملف env و تمريرها كمتغيرات بيئة لملف doker- compus.

```shell
ANALYTICS__UMAMIPATH="https://umamilocal.mostlylucid.net/getinfo"
ANALYTICS_WEBSITEID="32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
```

```yaml
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    environment:
    ...
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
```

لقد قمت بإنشاء الموقع الإلكتروني في لوحة "أمامي" عندما قمت بإنشاء الموقع. (ملاحظة اسم المستخدم الأصلي وكلمة السر لخدمة أمومي هو: `admin` وقد عقد مؤتمراً بشأن `umami`يجب أن تغير هذه بعد الإعداد
![نوع المُنْسِج](umamiaddwebsite.png)

مع مُرفقة هذه العملية

```csharp
public class AnalyticsSettings : IConfigSection
{
    public static string Section => "Analytics";
    public string? UmamiPath { get; set; }
}
```

مرة أخرى مرة أخرى هذا يستخدم بلدي POCO conforg that[هنا هنا](/blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)) لوضع الإعدادات.
ضعها في برامجي:

```csharp
builder.Configure<AnalyticsSettings>();
```

وأخيراً في `BaseController.cs` `OnGet` اضف التالي إلى set المسار لـ نص:

```csharp
   public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!Request.IsHtmx())
        {
            ViewBag.UmamiPath = _analyticsSettings.UmamiPath;
            ViewBag.UmamiWebsiteId = _analyticsSettings.WebsiteId;
        }
        base.OnActionExecuting(filterContext);
    }
    
```

هذا يُعيّن مُنتقى لـ نصّ إلى مُستخدَم بوصة ملفّ.

### 

وأخيراً، أضفت ما يلي إلى ملف تخطيطي ليشمل نص التحليل:

```html
<script defer src="@ViewBag.UmamiPath" data-website-id="@ViewBag.UmamiWebsiteId"></script>
```

هذا يحتوي نص بوصة صفحة و موقع الهوية لـ خدمة.

## تستثنى نفسك من تحليلك

من أجل استبعاد زياراتك الخاصة من بيانات التحليل يمكنك إضافة التخزين المحلي التالي في متصفحك:

في أدوات Crome dev (Ctrl+ Shift+I على النوافذ) يمكنك إضافة التالي إلى الميز:

```javascript
localStorage.setItem("umami.disabled", 1)
```

## ثالثاً - استنتاج

كان هذا قليلاً من النرجسة لتأسيسه ولكن أنا سعيد مع النتيجة. لدي الآن خدمة تحليلية قائمة بذاتها والتي لا تنقل البيانات إلى غوغل أو أي طرف ثالث آخر. هو قليلاً مِنْ الألمِ للإسْتِعْداد فوق لكن متى هو يُعْمَلُ هو سهلُ جداً للإسْتِعْمال. أنا سعيد بالنتيجة وأوصي بها لأي شخص يبحث عن حل تحليلي يستضيفه بنفسه