# جاري إضافة::::::::::::::::::::::: جاري جاري تنفيذ العميل حزمة

<!--category-- ASP.NET, Umami, Nuget -->
<datetime class="hidden">2024-2024-08-28-28T02 الساعة 00/02</datetime>

# أولاً

الآن لدي عميل أومامي، أريد أن أحزمه وأجعله متاحاً كحزمة نيوغيت. هذه عملية بسيطة جداً ولكن هناك بعض الأشياء التي يجب أن ندركها.

[رابعاً -

# يجري إنشاء الحزمة

## تنفيذ

قررت أن أنسخ [خالد خالد](@khalidabuhakmeh@mastodon.social) و استخدم الحزمة الممتازة لنسخ حزمتي النوغيت هذه الحزمة البسيطة التي تستخدم شارة النسخة المشتقة لتحديد رقم الإصدارة.

لأستخدمه قمت ببساطة بإضافة التالي إلى `Umami.Net.csproj` 

```xml
    <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```

بهذه الطريقة أستطيع أن ألصق نسختي مع `v` والحزمة سيتم نسخها بشكل صحيح.

```bash
 git tag v0.0.8       
 git push origin v0.0.8

```

سَيَدْفعُ هذه العلامةِ، ثمّ أنا عِنْدي a جيت Hub إجراء ضبط إلى إنتظار لـ بطاقة و بناء حزمة nuget.

## مبنى الحزمة الجديدة

لدي إجراء جيت هوب الذي يبني حزمة النوتات ويدفعها إلى مستودع حزم جيت هوب. هذه عملية بسيطة تستخدم `dotnet pack` إلى بناء الحزمة ثم `dotnet nuget push` إلى دفع الإيطالية إلى nuget مستودع.

```yaml
name: Publish Umami.NET
on:
  push:
    tags:
      - 'v*.*.*'  # This triggers the action for any tag that matches the pattern v1.0.0, v2.1.3, etc.

jobs:
  publish:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.x' # Specify the .NET version you need

    - name: Restore dependencies
      run: dotnet restore ./Umami.Net/Umami.Net.csproj

    - name: Build project
      run: dotnet build --configuration Release ./Umami.Net/Umami.Net.csproj --no-restore

    - name: Pack project
      run: dotnet pack --configuration Release ./Umami.Net/Umami.Net.csproj --no-build --output ./nupkg

    - name: Publish to NuGet
      run: dotnet nuget push ./nupkg/*.nupkg --source https://api.nuget.org/v3/index.json --api-key ${{ secrets.UMAMI_NUGET_API_KEY }}
      env:
        NUGET_API_KEY: ${{ secrets.UMAMI_NUGET_API_KEY }}
```

### إضافة إ_ ad adme و الأيقون

هذا بسيط جداً، اضيف `README.md` إلى جذ الجذر من المشروع و a `icon.png` إلى جذر من مشروع. الـ `README.md` ملفّ مُستخدَم كوصف للحزمة و `icon.png` ملفّ مُستخدَم كأيقونة للحزمة.

```xml
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <LangVersion>12</LangVersion>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>

        <IsPackable>true</IsPackable>
        <PackageId>Umami.Net</PackageId>
        <Authors>Scott Galloway</Authors>
        <PackageIcon>icon.png</PackageIcon>
        <RepositoryUrl>https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net</RepositoryUrl>
        <PackageLicenseExpression>MIT</PackageLicenseExpression>
        <PackageTags>web</PackageTags>
        <PackageReadmeFile>README.md</PackageReadmeFile>
        <Description>
           Adds a simple Umami endpoint to your ASP.NET Core application.
        </Description>
    </PropertyGroup>
```

في ملفي README.md لدي وصلة إلى مستودع GitHub ووصف للحزمة.

يُستَخدَمَ فيما يلي:

# الشبكة الوطنية (Net)

هذا هو العميل الأساسي لتعقب Amamami API.
إنه مبني على عميل "أمامامي ندي" الذي يمكن العثور عليه [هنا هنا](https://github.com/umami-software/node).

يمكنك أن ترى كيفية وضع أومامي على أنها حاوية docker [هنا هنا](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics).
يمكنك قراءة المزيد من التفاصيل عن إنشاءه على مدونتي [هنا هنا](https://www.mostlylucid.net/blog/addingumamitrackingclientfollowup).

إلى استخدام هذا العميل تحتاج إلى التالي: states. jusn recondment:

```json
{
  "Analytics":{
    "UmamiPath" : "https://umamilocal.mostlylucid.net",
    "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  },
}
```

المكان `UmamiPath` هو الطريق إلى أوماج الخاص بك و `WebsiteId` هو هوية الموقع الذي تريد تعقبه.

إلى استخدام العميل الذي تحتاج إلى إضافة التالي إلى `Program.cs`:

```csharp
using Umami.Net;

services.SetupUmamiClient(builder.Configuration);
```

وهذا من شأنه أن يضيف عميل أومامي إلى مجموعة الخدمات.

يمكنك بعد ذلك استخدام العميل بطريقتين:

1.  `UmamiClient` في صفّك واتّصل بـ `Track` طريقة:

```csharp
 // Inject UmamiClient umamiClient
 await umamiClient.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

2.  `UmamiBackgroundSender` لتعقب الأحداث في الخلفية (هذا يستخدم `IHostedService` إلى إرسال أحداث في الخلفية:

```csharp
 // Inject UmamiBackgroundSender umamiBackgroundSender
await umamiBackgroundSender.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

وسيرسل العميل الحدث إلى Aomami API وسيتم تخزينه.

الـ `UmamiEventData` هو قاموس لأزواج القيمة الرئيسية التي سترسل إلى AMAmi API كبيانات الحدث.

وبالإضافة إلى ذلك، هناك أساليب أكثر انخفاضاً يمكن استخدامها لإرسال الأحداث إلى معهد أومامي.

على كل من `UmamiClient` وقد عقد مؤتمراً بشأن `UmamiBackgroundSender` بإمكانك الاتصال بالطريقة التالية.

```csharp


 Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

إذا كنت لا تمر في `UmamiPayload` كائن العميل سينشئ واحد لك مستخدماً `WebsiteId` (من (التقديرات، (جيسون

```csharp
    public  UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var request = httpContext?.Request;

        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Data = data,
            Url = url ?? httpContext?.Request?.Path.Value,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = request?.Headers["User-Agent"].FirstOrDefault(),
            Referrer = request?.Headers["Referer"].FirstOrDefault(),
           Hostname = request?.Host.Host,
        };
        
        return payload;
    }

```

يمكنك أن ترى أن هذا يُعبّر عن `UmamiPayload` مع: `WebsiteId` مـن تـقـييـقـات `Url`, `IpAddress`, `UserAgent`, `Referrer` وقد عقد مؤتمراً بشأن `Hostname` باء - `HttpContext`.

ملاحظة: يمكن أن يكون الحدث "حدثاً" أو "تعريفياً" فقط وفقاً لمعيار AMAMAMI API.

# في الإستنتاج

إذاً يمكنك الآن تثبيت إمامي. Net من Nuget واستخدامها في تطبيق ASP.Net الأساسي الخاص بك. آمل أن تجده مفيداً سأستمر في التعديل و إضافة الإختبارات في المواقع المستقبلية