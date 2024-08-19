# إضافة a مُقرّر

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-18T20: 13</datetime>

## أولاً

في وظيفة سابقة أضفنا زبوناً للجلب [بيانات تحليليات أُماجِي](/blog/addingascsharpclientforumamiapi)/ / / / في هذا الموقع سوف نضيف عميل لإرسال بيانات التتبع إلى أومامي من تطبيق C#.
[ما قبل ما ما قبل ما ما قبل](https://umami.is/) هو خدمة تحليلية خفيفة الوزن والتي يمكن أن تكون ذاتية الإستضافة. إنها بديل عظيم لـ "جوجل" للتحليلات، وهي تركز على الخصوصية.
على أي حال من الناحية الإفتراضية لديها فقط عقدة عميل لتتبع البيانات (وحتى بعد ذلك فإنه ليس كبيرا). لذا قررت أن أكتب عميل C# لتعقب البيانات.

[رابعاً -

## النفقات قبل الاحتياجات

وضعة الأمّيّة [يمكنك أن ترى كيف أفعل هذا هنا](/blog/usingumamiforlocalanalytics).

## المُشغِل

يمكنك أن ترى كل شفرة المصدر للعميل [هنا هنا](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net).

هذا استخدام خصائص قد عُرّفت في `appsettings.json` ملف ملفّيّاً.

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo"
 },
```

بما أن المسار API غير موثق فأنا لم أضيف أي توثيق إلى العميل.

### إنشاء

من أجل إعداد العميل لقد أضفت أسلوبي المعتاد للإمتداد مع طلب من `Program.cs` ملف ملفّيّاً.

```csharp
services.SetupUmamiClient(config);
```

هذا يوفر طريقة بسيطة للربط في `UmamiClient` على طلبك.

الـ رمز أسفل الـ مؤكّد طريقة.

```csharp
   public static void SetupUmamiClient(this IServiceCollection services, IConfiguration config)
    {
       var umamiSettings= services.ConfigurePOCO<UmamiClientSettings>(config.GetSection(UmamiClientSettings.Section));
       if(string.IsNullOrEmpty( umamiSettings.UmamiPath)) throw new Exception("UmamiUrl is required");
       if(string.IsNullOrEmpty(umamiSettings.WebsiteId)) throw new Exception("WebsiteId is required");
       services.AddTransient<HttpLogger>();
        services.AddHttpClient<UmamiClient>((serviceProvider, client) =>
            {
                 umamiSettings = serviceProvider.GetRequiredService<UmamiClientSettings>();
            client.DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 Node/{Environment.Version}");
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
        }).SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy())
       #if DEBUG 
        .AddLogger<HttpLogger>();
        #else
        ;
        #endif
        
        services.AddHttpContextAccessor();
    }
    
    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg =>  msg.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
```

وكما ترون، فإن هذا يؤدي إلى ما يلي:

1. إعداد عنصر الضبط
2. 
3. إدخال مُحِل (IFIF في مفك مفك مفك
4. ضبط HttpClient مع القاعدة عنوان و a إعادة تجربة سياسة.

### (العميل نفسه)

الـ `UmamiClient` هو بسيط إلى حد ما. لديه طريقة أساسية واحدة `Send` الذي يرسل بيانات التتبع إلى خادم أمامي.

```csharp
    public async Task<HttpResponseMessage> Send(UmamiPayload payload, string type = "event")
    {
        var jsonPayload = new { type, payload };
        logger.LogInformation("Sending data to Umami {Payload}", JsonSerializer.Serialize(jsonPayload, options));
        var response= await client.PostAsJsonAsync("/api/send", jsonPayload, options);
        if(!response.IsSuccessStatusCode)
        {
           logger.LogError("Failed to send data to Umami {Response}, {Message}", response.StatusCode, response.ReasonPhrase);
        }
        else
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("Successfully sent data to Umami {Response}, {Message} {Content}", response.StatusCode, response.ReasonPhrase, content);
        }
        return response;
    }
```

كما سترى هذا يستخدم كائناً يسمى `UmamiPayload` تحتوي على جميع البارامترات الممكنة لتعقب الطلبات في أومامي.

```csharp
public class UmamiPayload
{
    public string Website { get; set; }=string.Empty;
    public string Hostname { get; set; }=string.Empty;
    public string Language { get; set; }=string.Empty;
    public string Referrer { get; set; }=string.Empty;
    public string Screen { get; set; }=string.Empty;
    public string Title { get; set; }   =string.Empty;
    public string Url { get; set; } =string.Empty;
    public string Name { get; set; } =string.Empty;
    public UmamiEventData? Data { get; set; }
}

public class UmamiEventData : Dictionary<string, object> { }
```

الحقل الوحيد المطلوب هو: `Website` الذي هو الموقع الإلكتروني هو هوية. أما البقية فهي اختيارية (ولكن `Url` حقًا مفيد!)ع(

في العميل لدي طريقة تدعى `GetPayload()` التي تُرسِل تُقطِن هذا الجسم الحمول تلقائياً بمعلومات من الطلب (باستخدام الحقن) `IHttpContextAccessor`).

```csharp

public class UmamiClient(HttpClient client, ILogger<UmamiClient> logger, IHttpContextAccessor accessor, UmamiClientSettings settings)...

    private UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
    {
        // Initialize a new UmamiPayload object
        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Data = data ?? new UmamiEventData(),
            Url = url ?? "" // Default URL to empty string if null
        };

        // Check if HttpContext is available
        if (accessor.HttpContext != null)
        {
            var context = accessor.HttpContext;
            var headers = context.Request.Headers;

            // Fill payload details from HttpContext and headers
            payload.Hostname = context?.Request.Host.Host ?? "";  // Default to empty string if null
            payload.Language = headers?["Accept-Language"].ToString() ?? "";  // Safely retrieve Accept-Language header
            payload.Referrer = headers?["Referer"].ToString() ?? "";  // Safely retrieve Referer header
            payload.Screen = headers?["User-Agent"].ToString() ?? "";  // Safely retrieve User-Agent header
            payload.Title = headers?["Title"].ToString() ?? "";  // Safely retrieve Title header
            payload.Url = string.IsNullOrEmpty(url) ? context.Request.Path.ToString() : url;  // Use the passed URL or fallback to the request path
        }

        return payload;
    }
```

هذا هو مُستخدَم أداء المزيد استخدام a واجهة لـ البيانات.

```csharp
    public async Task<HttpResponseMessage> TrackUrl(string? url="", string? eventname = "event", UmamiEventData? eventData = null)
    {
        var payload = GetPayload(url);
        payload.Name = eventname;
        return await Track(payload, eventData);
    }

    public async Task<HttpResponseMessage> Track(string eventObj, UmamiEventData? eventData = null)
    {
        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Name = eventObj,
            Data = eventData ?? new UmamiEventData()
        };

        return await Send(payload);
    }

    public async Task<HttpResponseMessage> Track(UmamiPayload eventObj, UmamiEventData? eventData = null)
    {
        var payload = eventObj;
        payload.Data = eventData ?? new UmamiEventData();
        payload.Website = settings.WebsiteId;
        return await Send(payload);
    }

    public async Task<HttpResponseMessage> Identify(UmamiEventData eventData)
    {
        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Data = eventData ?? new()
        };

        return await Send(payload, "identify");
    }
```

هذا يسمح لك بتتبع الأحداث و العناوين و تحديد المستخدمين.

## نُونج

في المستقبل أنا أخطط لجعل هذا إلى حزمة نوجت. لإختبار أن لدي مدخل في `Umami.Client.csproj` الـ ملفّ توليد a جديد مُنتقى حزمة عند بوصة مُطَلَق نمط.

```xml
   <Target Name="NugetPackAutoVersioning" AfterTargets="Build" Condition="'$(Configuration)' == 'Debug'">
    <!-- Delete the contents of the target directory -->
    <RemoveDir Directories="$(SolutionDir)nuget" />
    <!-- Recreate the target directory -->
    <MakeDir Directories="$(SolutionDir)nuget" />
    <!-- Run the dotnet pack command -->
    <Exec Command="dotnet pack -p:PackageVersion=$([System.DateTime]::Now.ToString(&quot;yyyy.MM.dd.HHmm&quot;))-preview -p:V --no-build --configuration $(Configuration) --output &quot;$(SolutionDir)nuget&quot;" />
    <Exec Command="dotnet nuget push $(SolutionDir)nuget\*.nupkg --source Local" />
    <Exec Command="del /f /s /q $(SolutionDir)nuget\*.nupkg" />
</Target>
```

هذا مضاف قبل النهاية مباشرة `</Project>` علامة في `.csproj` ملف ملفّيّاً.

يعتمد على موقع نمة يسمى 'مُحلّي' والذي هو معرّف في `Nuget.config` ملف ملفّيّاً. والذي قمت برسمه إلى مجلد محلي على آلتي.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="Local" value="e:\nuget" />
    <add key="Microsoft Visual Studio Offline Packages" value="C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\" />
  </packageSources>
</configuration>
```

## في الإستنتاج

في المستقبل في المستقبل أخطط لجعل هذا