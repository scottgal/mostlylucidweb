# Додавання клієнта стеження за C# amami

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024- 08- 18T20: 13</datetime>

## Вступ

У попередньому дописі ми додали клієнт для отримання [Дані аналітичних аналітичних даних уаммі](/blog/addingascsharpclientforumamiapi). У цьому полі ми додамо клієнта для надсилання даних про відстеження до Умамі з програми C#.
[Умаміzambia_ districts. kgm](https://umami.is/) це легка аналітична служба, яка може бути самостійною. Це чудова альтернатива для аналітики Google і зосереджена на приватності.
Але типово цей клієнт має лише клієнт для стеження за даними (і навіть у такому випадку він не є великим). Так что я решил написать клиента C# для отслеживания данных.

### <span style="color:red"> **ЗАУВАЖЕННЯ Я тільки що оновив це, я оновлю допис блогу пізніше - просто зараз є 26/08/2024**  </span>

[TOC]

## Передумови

Встановити umami [Ви бачите, як я це роблю.](/blog/usingumamiforlocalanalytics).

## Клієнт

Ви можете бачити всі початкові коди клієнта [тут](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net).

Цей параметр використовує визначені мною параметри `appsettings.json` файл.

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo"
 },
```

Оскільки API доріжки не розпізнано, я не додав розпізнавання до клієнта.

### Налаштування

Для налаштування клієнта, з яким я додав свій традиційний метод розширення, буде викликано з вашого `Program.cs` файл.

```csharp
services.SetupUmamiClient(config);
```

Цей спосіб надає простий спосіб зв' язатися в `UmamiClient` до вашої програми.

У коді, наведеному нижче, показано спосіб налаштування програми.

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

Як ви можете бачити, це робить наступне:

1. Налаштування об' єкта налаштування
2. Перевірити коректні параметри
3. Додати журнал (якщо у режимі зневаджування)
4. Встановити HtpClient за базовою адресою і правилом повторення.

### Клієнт сам

The `UmamiClient` досить просто. Він має один базовий метод `Send` что отправит данные на сервер Амами.

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

Як ви побачите, це використовує об'єкт під назвою `UmamiPayload` У ньому містяться всі можливі параметри стеження за запитами у Умамі.

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

Єдине потрібне поле - `Website` який є ідентифікатором веб-сайту. Решта необов' язкова (але `Url` дуже корисна!).

У клієнті є метод, який називається `GetPayload()` які надсилають дані про цей об' єкт вантаж автоматично з запитом (за допомогою впорскування) `IHttpContextAccessor`).

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

Цей інтерфейс потім використовується у наступних допоміжних методах, які надають кращий інтерфейс для цих даних.

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

За допомогою цього пункту ви можете стежити за подіями, адресами URL і ідентифікувати користувачів.

## Nuget

В майбутньому я планую перетворити це на пакунок NuGet. Перевірка на те, що я маю запис `Umami.Client.csproj` файл, який створює новий пакунок з версіями " Preview " під час збирання у режимі зневаджування.

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

Цей параметр буде додано перед закінченням `</Project>` tag в `.csproj` файл.

Це залежить від місця, де nuget називається "локальний," що визначено у `Nuget.config` файл. Которую я нанес в местную папку на своей машинке.

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

## Включення

В майбутньому я планую зробити це пакунком NuGet.
Я використовую це в блозі, наприклад, для того, щоб відстежити тривалість перекладу.

```csharp
        var translationTask = tasks.FirstOrDefault(t => t.TaskId == taskId);
        if (translationTask == null) return TypedResults.BadRequest("Task not found");
        await  umamiClient.Send(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
```