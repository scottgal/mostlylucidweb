# الشبكة العالمية والتفجيرات

# أولاً

لذلك أنا [عدد الأفراد الذين عُينوا](/blog/category/Umami) في الماضي على استخدام أومامي للتحليلات التحليلية في بيئة ذاتية الاستضافة وحتى نشر [شبكة Nuget pacakge](https://www.nuget.org/packages/Umami.Net/)/ / / / ومع ذلك كنت أواجه مشكلة حيث أردت تتبع المستخدمين من تغذية RSS بلدي؛ هذا المقال يذهب إلى لماذا وكيف قمت بحله.

[TOC]

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-12TT 14:50</datetime>

# المشكلة

المشكلة هي أن قراء تغذية RSS تحاول أن تمر *مُنْفْرِك* المستخدمات عند طلب التّغذية. هذا هذا **ألف -** (ج) أن تتعقب عدد المستخدمين ونوع المستخدمين الذين يستهلكون التغذية. ومع ذلك، يعني هذا أيضاً أن أومامي ستحدد هذه الطلبات على أنها: *لِسِمر لِسِرْ* (ط) الطلبـات من أجـل تقديم الطلبات. وهذه مسألة تخص استخدامي لها لأنها تؤدي إلى تجاهل الطلب وعدم تعقبه.

الـ مُستخدَم وكيل مثل هذا:

```plaintext
Feedbin feed-id:1234 - 21 subscribers
```

هذا حق مفيد جداً، يمرر بعض التفاصيل المفيدة حول ماهية هويتك، عدد المستخدمين وعامل المستخدم. ومع ذلك، هذه أيضاً مشكلة أيضاً لأنها تعني أن أومامي سوف تتجاهل الطلب، في الواقع أنها سوف تعيد حالة 200 ولكن المحتوى يحتوي على `{"beep": "boop"}` وهذا يعني أن هذا يُعرَّف بأنه طلب آلي. هذا مزعج لأنني لا أستطيع التعامل مع هذا من خلال التعامل العادي مع الأخطاء (إنها 200، لا أقول 403 وما إلى ذلك).

# الإحلال

اذاً ما هو الحل لهذا؟ لا يمكنني تقديم كل هذه الطلبات يدوياً واكتشاف ما إذا كانت أمامي ستكتشفها كروبوتة؛ إنها تستخدم إيسبوت (https://www.npmjs.com/package/isbot) للكشف عما إذا كان الطلب عملاً آلياً أم لا. ليس هناك C# مكافئ وهي قائمة متغيرة لذا لا أستطيع حتى استخدام تلك القائمة (في المستقبل يمكنني أن أحصل على الذكاء
لذلك أنا بحاجة إلى اعتراض الطلب قبل أن يصل إلى أومامي وتغيير وكيل المستخدم إلى شيء أن أومامي سوف تقبل طلبات محددة.

لذا الآن أضفت بعض البارامترات الإضافية إلى أساليب تتبعي في موقع أمامي.Net. هذه تسمح لك بتحديد "عامل مستخدم Default" الجديد سيتم إرساله إلى أومامي بدلاً من عامل المستخدم الأصلي. هذا يسمح لي بتحديد أن وكيل المستخدم يجب أن يتغير إلى قيمة محددة لطلبات محددة.

## 

فيما يتعلق `UmamiBackgroundSender` وأضيف ما يلي:

```csharp
   public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
```

هذا موجود على كل طرق التتبع هناك و فقط يُحدّد a معلمة على `UmamiPayload` (أ) الهدف من الهدف.

& من طراز `UmamiClient` يمكن أن تكون على النحو التالي:

```csharp
    [Fact]
    public async Task TrackPageView_WithUrl()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.TrackPageViewAndDecode("https://example.com", "Example Page",
            new UmamiPayload { UseDefaultUserAgent = true });
        Assert.NotNull(response);
        Assert.Equal(UmamiDataResponse.ResponseStatus.Success, response.Status);
    }
```

في هذا الاختبار استخدم الجديد `TrackPageViewAndDecode` الدالة `UmamiDataResponse` (أ) الهدف من الهدف. هذا الكائن يحتوي على رمز JWT رمزي (الذي هو غير صحيح إذا هو a unit لذلك هذا هو مفيد للفحص) وحالة الطلب.

## `PayloadService`

كل هذا تم التعامل مع كل هذا في `Payload` الخدمة التي تكون مسؤولة عن تعبئة جسم الحمولة. هذا هو المكان حيث `UseDefaultUserAgent` مُعَدّة.

على افتراضياً أنا أُحِث الحمولة من `HttpContext` اذاً عادة ما تحصل على هذه المجموعة بشكل صحيح، سأريك لاحقاً أين يتم سحب هذا لاحقاً من أومامي.

```csharp
    private UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
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
            Hostname = request?.Host.Host
        };

        return payload;
    }
```

ثم لدي قطعة من الشيفرة تُدعى `PopulateFromPayload` حيث يحصل كائن الطلب على البيانات هو set:

```csharp
    public static string DefaultUserAgent =>
        $"Mozilla/5.0 (Windows 11)  Umami.Net/{Assembly.GetAssembly(typeof(UmamiClient))!.GetName().Version}";

    public UmamiPayload PopulateFromPayload(UmamiPayload? payload, UmamiEventData? data)
    {
        var newPayload = GetPayload(data: data);
        ...
        
        newPayload.UserAgent = payload.UserAgent ?? DefaultUserAgent;

        if (payload.UseDefaultUserAgent)
        {
            var userData = newPayload.Data ?? new UmamiEventData();
            userData.TryAdd("OriginalUserAgent", newPayload.UserAgent ?? "");
            newPayload.UserAgent = DefaultUserAgent;
            newPayload.Data = userData;
        }


        logger.LogInformation("Using UserAgent: {UserAgent}", newPayload.UserAgent);
     }        
        
```

سترى أن هذا يُعرّف مُستخدماً جديداً في أعلى الملف (الذي أكّدت أنه ليس *حالياً حالياً* (بدولارات الولايات المتحدة) ثم في الطريقة التي تكتشف ما إذا كان سواءً كان العامل المستخدم (مما لا ينبغي أن يحدث إلا إذا كان مدعواً من شفرة بدون HtttpConconft) أو إذا كان `UseDefaultUserAgent` مُعَدّة. إذا هو هو set مستخدم إلى افتراضي و أصلي مستخدم إلى البيانات كائن.

هذا هو من ثمّ هو مُشتَرَك لذا أنت يمكن أن ترى ما هو مُستخدَم.

## جاري إستبعاد الإستجابة.

أضفت عدداً من الأساليب الجديدة لـ "الوطن" التي تعيد ما يلي: `UmamiDataResponse` (أ) الهدف من الهدف. هذا كائن يحتوي رمز JWT.

```csharp
    public async Task<UmamiDataResponse?> TrackPageViewAndDecode(
        string? url = "",
        string? title = "",
        UmamiPayload? payload = null,
        UmamiEventData? eventData = null)
    {
        var response = await TrackPageView(url, title, payload, eventData);
        return await DecodeResponse(response);
    }
    
        private async Task<UmamiDataResponse?> DecodeResponse(HttpResponseMessage responseMessage)
    {
        var responseString = await responseMessage.Content.ReadAsStringAsync();

        switch (responseMessage.IsSuccessStatusCode)
        {
            case false:
                return new UmamiDataResponse(UmamiDataResponse.ResponseStatus.Failed);
            case true when responseString.Contains("beep") && responseString.Contains("boop"):
                logger.LogWarning("Bot detected data not stored in Umami");
                return new UmamiDataResponse(UmamiDataResponse.ResponseStatus.BotDetected);

            case true:
                var decoded = await jwtDecoder.DecodeResponse(responseString);
                if (decoded == null)
                {
                    logger.LogError("Failed to decode response from Umami");
                    return null;
                }

                var payload = UmamiDataResponse.Decode(decoded);

                return payload;
        }
    }
```

يمكنك أن ترى أن هذا يدعو إلى الطبيعي `TrackPageView` ثمّ يُنادِثُ a طريقة مُدّاة `DecodeResponse` الذي يفحص الإجابة لـ `beep` وقد عقد مؤتمراً بشأن `boop` (للكشف عن النسر). إذا كانت تجدها، فعند ذلك تقوم بسجل تحذير و ترجع `BotDetected` (الوضع الراهن) إذا لم يجدهم، فإنه يفك رموز رمز JWT ويعيد الحمولة.

رمز JWT في حد ذاته هو مجرد سلسلة نصية مشفرة من القاعدة 64 تحتوي على البيانات التي خزنها أومامي. هذا مشفرة ومرجعة كـ `UmamiDataResponse` (أ) الهدف من الهدف.

المصدر الكامل لهذا هو أدناه:

<details>
<summary>Response Decoder</summary>

```csharp
using System.IdentityModel.Tokens.Jwt;

namespace Umami.Net.Models;

public class UmamiDataResponse
{
    public enum ResponseStatus
    {
        Failed,
        BotDetected,
        Success
    }

    public UmamiDataResponse(ResponseStatus status)
    {
        Status = status;
    }

    public ResponseStatus Status { get; set; }

    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public string? Hostname { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Screen { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? Subdivision1 { get; set; }
    public string? Subdivision2 { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public long Iat { get; set; }

    public static UmamiDataResponse Decode(JwtPayload? payload)
    {
        if (payload == null) return new UmamiDataResponse(ResponseStatus.Failed);
        payload.TryGetValue("visitId", out var visitIdObj);
        payload.TryGetValue("iat", out var iatObj);
        //This should only happen then the payload is dummy.
        if (payload.Count == 2)
        {
            var visitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty;
            var iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0;

            return new UmamiDataResponse(ResponseStatus.Success)
            {
                VisitId = visitId,
                Iat = iat
            };
        }

        payload.TryGetValue("id", out var idObj);
        payload.TryGetValue("websiteId", out var websiteIdObj);
        payload.TryGetValue("hostname", out var hostnameObj);
        payload.TryGetValue("browser", out var browserObj);
        payload.TryGetValue("os", out var osObj);
        payload.TryGetValue("device", out var deviceObj);
        payload.TryGetValue("screen", out var screenObj);
        payload.TryGetValue("language", out var languageObj);
        payload.TryGetValue("country", out var countryObj);
        payload.TryGetValue("subdivision1", out var subdivision1Obj);
        payload.TryGetValue("subdivision2", out var subdivision2Obj);
        payload.TryGetValue("city", out var cityObj);
        payload.TryGetValue("createdAt", out var createdAtObj);

        return new UmamiDataResponse(ResponseStatus.Success)
        {
            Id = idObj != null ? Guid.Parse(idObj.ToString()!) : Guid.Empty,
            WebsiteId = websiteIdObj != null ? Guid.Parse(websiteIdObj.ToString()!) : Guid.Empty,
            Hostname = hostnameObj?.ToString(),
            Browser = browserObj?.ToString(),
            Os = osObj?.ToString(),
            Device = deviceObj?.ToString(),
            Screen = screenObj?.ToString(),
            Language = languageObj?.ToString(),
            Country = countryObj?.ToString(),
            Subdivision1 = subdivision1Obj?.ToString(),
            Subdivision2 = subdivision2Obj?.ToString(),
            City = cityObj?.ToString(),
            CreatedAt = createdAtObj != null ? DateTime.Parse(createdAtObj.ToString()!) : DateTime.MinValue,
            VisitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty,
            Iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0
        };
    }
}
```

</details>
يمكنك أن ترى أن هذا يحتوي على مجموعة من المعلومات المفيدة حول الطلب الذي خزنته أمامي. إذا كنت تريد على سبيل المثال أن تعرض محتوى مختلفاً بناءً على الموضع، اللغة، المتصفح وما إلى ذلك هذا يتيح لك القيام بذلك.

```csharp
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public string? Hostname { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Screen { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? Subdivision1 { get; set; }
    public string? Subdivision2 { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public long Iat { get; set; }
```

# في الإستنتاج

إذاً مجرد وظيفة قصيرة تغطي بعض الوظائف الجديدة في أومامي. Net 0.4.0 التي تسمح لك بتحديد عميل مستخدم افتراضي لطلبات محددة. وهذا مفيد بالنسبة لطلبات التتبع التي كانت أمامي ستتجاهلها لولا ذلك.