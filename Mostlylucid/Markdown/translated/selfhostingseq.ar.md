# المكافئ المتعلق باستضافة ذات ذات ذات ذاتية لشبكة ASP.net loggging

<datetime class="hidden">2024-08-08-28- تـتـت تـ09: 37</datetime>

<!--category-- ASP.NET, Seq, Serilog, Docker -->
# أولاً

Seq هو تطبيق يسمح لك بعرض وتحليل اللوغاريتمات. إنها أداة عظيمة لإزالة الإبتزاز ومراقبة تطبيقك في هذا الموقع سوف أغطي كيفية إعداد ما يلي لتسجيل تطبيقي الأساسي ASP.NET.
« لا » زائدة « يكون » بالياء والتاء زائدة « من آيات كثيرة ».

![سندSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSS](seqdashboard.png)

[رابعاً -

# تثبيت تثبيت تثبيت

يأتي Seq في زوجين من النكهات. يمكنك إما استخدام نسخة السحابة أو استضافة نفسها. اخترت أن أستضيفها بنفسي كما أردت أن أبقي سجلاتي خاصة

أولاً بدأت بزيارة موقع Seq على شبكة الإنترنت والعثور على [تنفيذ](https://docs.datalust.co/docs/getting-started-with-docker).

## لية

لتشغيل محليّاً تحتاج أولاً للحصول على كلمة مرور مُهَدَّدة. يمكنك أن تفعل هذا عن طريق تنفيذ الأمر التالي:

```bash
echo '<password>' | docker run --rm -i datalust/seq config hash
```

إلى تشغيله محليًّا، يمكنك استخدام الأمر التالي:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hashfromabove> -v C:\seq:/data -p 5443:443 -p 45341:45341 -p 5341:5341 -p 82:80 datalust/seq

```

على آلتي المحلية "أوبونتو" صنعت هذا إلى نص sh:

```shell
#!/bin/bash
PH=$(echo 'Abc1234!' | docker run --rm -i datalust/seq config hash)

mkdir -p /mnt/seq
chmod 777 /mnt/seq

docker run \
  --name seq \
  -d \
  --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -e SEQ_FIRSTRUN_ADMINPASSWORDHASH="$PH" \
  -v /mnt/seq:/data \
  -p 5443:443 \
  -p 45341:45341 \
  -p 5341:5341 \
  -p 82:80 \
  datalust/seq
```

ثم

```shell
chmod +x seq.sh
./seq.sh
```

وهذا سوف تحصل على ما يصل ويركض ثم يذهب إلى `http://localhost:82` / `http://<machineip>:82` إلى s set تثبيت (كلمة المرور الإدارية الافتراضية هي التي أدخلتها لـ <password> (أ) انظر أعلاه.

## داخل

أضفت ما يلي إلى ملفي المؤلف من Dokker على النحو التالي:

```docker
  seq:
    image: datalust/seq
    container_name: seq
    restart: unless-stopped
    environment:
      ACCEPT_EULA: "Y"
      SEQ_FIRSTRUN_ADMINPASSWORDHASH: ${SEQ_DEFAULT_HASH}
    volumes:
      - /mnt/seq:/data
    networks:
      - app_network
```

ملاحظة أن لدي دليل يسمى `/mnt/seq` (بالنسبة للنوافذ، استخدم مساراً للنوافذ). هذا هو حيث سيتم تخزين اللوغاريتمات.

لدي أيضاً `SEQ_DEFAULT_HASH` متغير بيئة هو كلمة مرور لـ مدير مستخدم بوصة ملفّ. env.

# باء - إنشاء قاعدة البيانات الإحصائية الأساسية الأساسية

كما استخدم [S](https://serilog.net/) في الواقع أنه من السهل جداً أن أُنشئ (سيكس) من أجل قطع الأشجار الخاص بي حتى أن لديها وثائق على كيفية القيام بذلك [هنا هنا](https://docs.datalust.co/docs/using-serilog).

أساسًا أنت فقط اضف المغسلة إلى مشروعك:

```shell
dotnet add package Serilog.Sinks.Seq
```

أفضل أن أستعمل `appsettings.json` لكي يكون لدي فقط "المعايير" الإعداد في بلدي `Program.cs`:

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
});
```

ثم في 'تعفيفي. Jjson' لدي هذا التشكيل

```json
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq"],
    "MinimumLevel": "Warning",
    "WriteTo": [
        {
          "Name": "Seq",
          "Args":
          {
            "serverUrl": "http://seq:5341",
            "apiKey": ""
          }
        },
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }

    ],
    "Enrich": ["FromLogContext", "WithMachineName"],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }

```

سترين أن لدي `serverUrl` :: `http://seq:5341`/ / / / هذا لأن لدي ما يركض في حاوية (دبوكر) تدعى `seq` وهو على الميناء `5341`/ / / / إذا كنت تديره محلياً يمكنك استخدامه `http://localhost:5341`.
أنا أيضاً استخدم API مفتاح حتى أتمكن من استخدام المفتاح لتحديد لوغاريتم المستوى دياميكي (يمكنك تعيين مفتاح لقبول مستوى معين فقط من رسائل اللوغارتم).

قمت بوضعها في موقعك على سبيل المثال عن طريق الذهاب إلى `http://<machine>:82` و النقر على إعدادات توت في الأعلى لليمين. ثم انقر على `API Keys` تبويب و إضافة a جديد مفتاح. يمكنك استخدام هذا المفتاح في `appsettings.json` ملف ملفّيّاً.

![التكسس](seqapikey.png)

# الأمر المُقْرِر

الآن لدينا هذا الجهاز نحتاج إلى إعداد تطبيق ASP.net للحصول على مفتاح. أنا استخدم `.env` لإخفاء أسراري.

```dotenv
SEQ_DEFAULT_HASH="<adminpasswordhash>"
SEQ_API_KEY="<apikey>"
```

ثم في ملفي المؤلف من الدوكرز سأحدد أن القيمة يجب أن تُحقن كمتغير بيئة في تطبيق ASP.net الخاص بي:

```docker
services:
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    labels:
        - "com.centurylinklabs.watchtower.enable=true"
    env_file:
      - .env
    environment:
      - Auth__GoogleClientId=${AUTH_GOOGLECLIENTID}
      - Auth__GoogleClientSecret=${AUTH_GOOGLECLIENTSECRET}
      - Auth__AdminUserGoogleId=${AUTH_ADMINUSERGOOGLEID}
      - SmtpSettings__UserName=${SMTPSETTINGS_USERNAME}
      - SmtpSettings__Password=${SMTPSETTINGS_PASSWORD}
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
      - ConnectionStrings__DefaultConnection=${POSTGRES_CONNECTIONSTRING}
      - TranslateService__ServiceIPs=${EASYNMT_IPS}
      - Serilog__WriteTo__0__Args__apiKey=${SEQ_API_KEY}
    volumes:
      - /mnt/imagecache:/app/wwwroot/cache
      - /mnt/markdown/comments:/app/Markdown/comments
      - /mnt/logs:/app/logs
    networks:
      - app_network
```

ملاحظة أن `Serilog__WriteTo__0__Args__apiKey`  `SEQ_API_KEY` باء - `.env` ملف ملفّيّاً. '0' هو مؤشر `WriteTo` في `appsettings.json` ملف ملفّيّاً.

# أُعد

ملاحظة لكل من Seq و ASP.net `app_network` :: الشبكة الدولية لشبكة الملاحة الجوية. هذا لأنني أستخدم (كادي) كوكيل عكسي وهو على نفس الشبكة هذا يعني أنه يمكنني استخدام اسم الخدمة كعنوان في ملفي Cadedifile.

```caddy
{
    email scott.galloway@gmail.com
}
seq.mostlylucid.net
{
   reverse_proxy seq:80
}

http://seq.mostlylucid.net
{
   redir https://{host}{uri}
}
```

اذاً هذا `seq.mostlylucid.net` إلى حالتي العليا.

# ثالثاً - استنتاج

Seq أداة عظيمة لقطع الأشجار ومراقبة طلبك. من السهل ان تجهز وتستعمل وتدمج بشكل جيد مع سيريلوج لقد وجدتها قيّمة في تصحيح تطبيقاتي وأنا متأكد أنك ستفعل أيضاً