# C# उममी खोज ग्राहक जोड़ रहे हैं

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024- 0. 141820:13</datetime>

## परिचय

एक पिछले पोस्ट में हम लाने के लिए एक क्लाइंट जोड़ा [उममी एक कुल्स डाटा](/blog/addingascsharpclientforumamiapi)___ इस पोस्ट में हम एक ग्राहक को जोड़ देंगे C# अनुप्रयोग से उममी डाटा भेजने के लिए
[उममी](https://umami.is/) यह एक हल्का सापूर्ण सेवा है जो आत्म-host किया जा सकता है. इस लेख में नाम बदल दिए गए हैं ।
लेकिन डिफ़ॉल्ट द्वारा यह सिर्फ एक नोड ग्राहक है डेटा ट्रैक करने के लिए (और तब भी यह महत्वपूर्ण नहीं है). तो मैं ट्रैक डाटा के लिए एक C# ग्राहक लिखने का फैसला किया.

### <span style="color:red"> **अब मैं इस पर टिप्पणी करता हूँ, मैं बाद में ब्लॉग पोस्ट को अद्यतन करूँगा - बस अब 26/08/20 होकर जा रहा हूँ**  </span>

[विषय

## पूर्वपाराईज़

उममी संस्थापित करें [आप देख सकते हैं कि मैं यहाँ कैसे कर सकते हैं](/blog/usingumamiforlocalanalytics).

## क्लाइंट

आप क्लाइंट के लिए सभी श्रोत कोड को देख सकते हैं [यहाँ](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net).

यह विन्यास प्रयोग करता है मैंने अपने में पारिभाषित किया है `appsettings.json` फ़ाइल.

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo"
 },
```

ट्रैक एपीआई सत्यापित नहीं है क्योंकि मैंने क्लाइंट को कोई सत्यापन नहीं जोड़ा है.

### सेटअप

ग्राहक को सेटअप करने के लिए मैं अपने प्रथागत विस्तार विधि के साथ कहा जाता है अपने से बुलाया जाता है `Program.cs` फ़ाइल.

```csharp
services.SetupUmamiClient(config);
```

यह में हुक करने के लिए एक सरल तरीका प्रदान करता है `UmamiClient` आपके अनुप्रयोग के लिए.

नीचे दिए गए कोड सेटअप विधि को दिखाता है.

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

जैसा कि आप इसे देख सकते हैं निम्नलिखित करता है:

1. कॉन्फ़िग वस्तु नियत करें
2. विन्यास वैध हैं
3. एक लॉगर जोड़ें (यदि डिबग मोड में)
4. बेस पता और फिर नीति के साथ HttpICient सेट करें.

### क्लाएंट तेंतू

वह `UmamiClient` बहुत सरल है. इसमें एक कोर विधि है `Send` जो डाटाबेस सर्वर में ट्रैकिंग डाटा भेजता है.

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

जैसा कि आप इसे एक वस्तु कहा जाता है उपयोग करेंगे `UmamiPayload` इसमें उममी में ट्रैक करने के लिए सभी संभावित पैरामीटर्स हैं.

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

केवल वांछित क्षेत्र है `Website` जो वेबसाइट आईडी है. बाकी वैकल्पिक हैं (लेकिन) `Url` सचमुच उपयोगी है!___

क्लाएंट में मैं एक विधि कहा जाता है `GetPayload()` जो इस भुगतान वस्तु को स्वचालित लोड करता है निवेदन से जानकारी के साथ (शोधन के प्रयोग से) `IHttpContextAccessor`).

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

यह तब अतिरिक्त यूटिलिटी विधियों द्वारा प्रयोग किया जाता है जो कि इस डाटा के लिए एक बढ़िया इंटरफ़ेस प्रदान करते हैं.

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

यह आपको उन घटनाओं, यूआरएलों तथा उपयोक्ताओं को पहचानने देता है.

## नोरूट

भविष्य में मैं एक नुपा पैकेज में यह बनाने की योजना. जाँच की कि मुझे एक प्रविष्टि है `Umami.Client.csproj` फ़ाइल जो एक नया संस्करण तैयार करता है 'प्रयोग' पैकेज बनाता है जब डीबग मोड में बनाया गया.

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

अंत में यह जोड़ा गया दायाँ जोड़ा गया है `</Project>` टैग इंच `.csproj` फ़ाइल.

यह nuget स्थान पर निर्भर करता है जिन्हें 'ooo' कहा जाता है जो कि में पारिभाषित है `Nuget.config` फ़ाइल. मैं अपने मशीन पर एक स्थानीय फ़ोल्डर पर मैप किया है.

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

## ऑन्टियम

भविष्य में मैं इस एक Nuop पैकेज बनाने की योजना.
मैं इसे ब्लॉग में इस्तेमाल करता हूँ, उदाहरण के लिए कि कब तक अनुवाद किया जाए

```csharp
        var translationTask = tasks.FirstOrDefault(t => t.TaskId == taskId);
        if (translationTask == null) return TypedResults.BadRequest("Task not found");
        await  umamiClient.Send(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
```