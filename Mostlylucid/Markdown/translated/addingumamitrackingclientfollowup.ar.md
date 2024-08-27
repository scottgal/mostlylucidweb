# مُضَ إضافة متابعة مُتابعة مُتابعة المُؤْمِم

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-2024-00/08-27/02</datetime>

# أولاً

في 1 [الوظائف السابقة](/blog/addingumamitrackingclient.md) لقد رسمت كيف يمكن لعميل تتبع لـ (أمامي) في (سي) أن ينجح
أخيراً حظيت بفرصة لإختباره على نطاق واسع وتحسين عمليته (نعم مرة أخرى) `IHostedService`).

[رابعاً -

# Quirks من Amami API

ومؤشر القدرة على تتبع الأمومات هو في نفس الوقت شديد الرأي ومثير جدا في آن واحد. لذا كان علي تحديث رمز العميل للتعامل مع ما يلي:

1. الـ API توقّع a حقيقي مستخدم الوكيل سلسلة نص. لذا كان عليّ تحديث العميل لكي أستخدم سلسلة تعريف المستخدم الحقيقي (أو لأكون أكثر دقة قمت بإلتقاط سلسلة تعريف المستخدم الحقيقي من متصفح واستخدم ذلك).
2. الـ API توقّع هو JOSON دَخْل بوصة a تنسيق فارغ غير مسموح. لذا اضطررت إلى تحديث العميل للتعامل مع هذا.
3. الـ [عقد العقد العميل](https://github.com/umami-software/node) لَهُ قليلاً من a منطقة سطحِ شاذّةِ. ليس من الواضح على الفور ما يتوقعه API. لذا كان علي أن أقوم ببعض التجربة والخطأ لأجعلها تعمل.

## المُقرّد المُعْرِف المُقرِف

العميل API العقدة في المجموع هو أدناه، انها مرنة جدا ولكن في الواقع ليست موثقة بشكل جيد.

```javascript
export interface UmamiOptions {
  hostUrl?: string;
  websiteId?: string;
  sessionId?: string;
  userAgent?: string;
}

export interface UmamiPayload {
  website: string;
  session?: string;
  hostname?: string;
  language?: string;
  referrer?: string;
  screen?: string;
  title?: string;
  url?: string;
  name?: string;
  data?: {
    [key: string]: string | number | Date;
  };
}

export interface UmamiEventData {
  [key: string]: string | number | Date;
}

export class Umami {
  options: UmamiOptions;
  properties: object;

  constructor(options: UmamiOptions = {}) {
    this.options = options;
    this.properties = {};
  }

  init(options: UmamiOptions) {
    this.options = { ...this.options, ...options };
  }

  send(payload: UmamiPayload, type: 'event' | 'identify' = 'event') {
    const { hostUrl, userAgent } = this.options;

    return fetch(`${hostUrl}/api/send`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': userAgent || `Mozilla/5.0 Umami/${process.version}`,
      },
      body: JSON.stringify({ type, payload }),
    });
  }

  track(event: object | string, eventData?: UmamiEventData) {
    const type = typeof event;
    const { websiteId } = this.options;

    switch (type) {
      case 'string':
        return this.send({
          website: websiteId,
          name: event as string,
          data: eventData,
        });
      case 'object':
        return this.send({ website: websiteId, ...(event as UmamiPayload) });
    }

    return Promise.reject('Invalid payload.');
  }

  identify(properties: object = {}) {
    this.properties = { ...this.properties, ...properties };
    const { websiteId, sessionId } = this.options;

    return this.send(
      { website: websiteId, session: sessionId, data: { ...this.properties } },
      'identify',
    );
  }

  reset() {
    this.properties = {};
  }
}

const umami = new Umami();

export default umami;
```

كما ترون فإنها تكشف عن الطرق التالية:

1. `init` -لوضع الخيارات
2. `send` -لإرسال الحمولة
3. `track` -لتعقب حدث ما
4. `identify` -لتحديد المستخدم
5. `reset` -لإعادة ضبط الخواص

جوهر هذا هو `send` طريقة إرسال الحمولة إلى API.

```javascript
  send(payload: UmamiPayload, type: 'event' | 'identify' = 'event') {
    const { hostUrl, userAgent } = this.options;

    return fetch(`${hostUrl}/api/send`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': userAgent || `Mozilla/5.0 Umami/${process.version}`,
      },
      body: JSON.stringify({ type, payload }),
    });
  }
```

# الـ مُشغِل

إلى البداية مع أنا إلى حد كبير نسخت العقدة API العميل `UmamiOptions` وقد عقد مؤتمراً بشأن `UmamiPayload` (لن أتجاوزها مرة أخرى إنها كبيرة)

إذاً الآن `Send` هذا ما يلي:

```csharp
     public async Task<HttpResponseMessage> Send(UmamiPayload? payload=null, UmamiEventData? eventData =null,  string type = "event")
        {
            var websiteId = settings.WebsiteId;
             payload = PopulateFromPayload(websiteId, payload, eventData);
            
            var jsonPayload = new { type, payload };
            logger.LogInformation("Sending data to Umami: {Payload}", JsonSerializer.Serialize(jsonPayload, options));

            var response = await client.PostAsJsonAsync("api/send", jsonPayload, options);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to send data to Umami: {StatusCode}, {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Successfully sent data to Umami: {StatusCode}, {ReasonPhrase}, {Content}", response.StatusCode, response.ReasonPhrase, content);
            }

            return response;
        }

```

هناك جزآن أساسيان هنا:

1. الـ `PopulateFromPayload` (ب) الطريقة التي تُمَثِّل بها الحمولة مع التعريف على الموقع الشبكي والحدث Data.
2. تسلسل JOXون للحمولة، يحتاج إلى استبعاد القيم اللاغية.

## الـ `PopulateFromPayload` الم

```csharp
        public static UmamiPayload PopulateFromPayload(string webSite, UmamiPayload? payload, UmamiEventData? data)
        {
            var newPayload = GetPayload(webSite, data: data);
            if(payload==null) return newPayload;
            if(payload.Hostname != null)
                newPayload.Hostname = payload.Hostname;
            if(payload.Language != null)
                newPayload.Language = payload.Language;
            if(payload.Referrer != null)
                newPayload.Referrer = payload.Referrer;
            if(payload.Screen != null)
                newPayload.Screen = payload.Screen;
            if(payload.Title != null)
                newPayload.Title = payload.Title;
            if(payload.Url != null)
                newPayload.Url = payload.Url;
            if(payload.Name != null)
                newPayload.Name = payload.Name;
            if(payload.Data != null)
                newPayload.Data = payload.Data;
            return newPayload;          
        }
        
        private static UmamiPayload GetPayload(string websiteId, string? url = null, UmamiEventData? data = null)
        {
            var payload = new UmamiPayload
            {
            Website = websiteId,
                Data = data,
                Url = url ?? string.Empty
            };
            

            return payload;
        }

```

يمكنك أن ترى أننا دائماً نضمن `websiteId` و نحدد القيم الأخرى فقط إذا لم تكن لاغية. وهذا يعطينا مرونة على حساب القليل من اللفظية.

## الـ HtttttpClib مُسند المُثبث

وكما ذُكر من قبل، يتعين علينا أن نعطي أداة تعريف API خيطاً حقيقياً إلى حد ما من عوامل الاستخدام. هذا ما تم القيام به في `HttpClient` -مُعَدّة. -مُعَدّة.

```csharp
              services.AddHttpClient<UmamiClient>((serviceProvider, client) =>
            {
                 umamiSettings = serviceProvider.GetRequiredService<UmamiClientSettings>();
            client.DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
        }).SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy())
       #if DEBUG 
        .AddLogger<HttpLogger>();
        #else
        ;
        #endif

```

## نعم نعم

هذا هو آخر `IHostedService`هناك مجموعة من المقالات عن كيفية وضع هذه حتى لا أخوض فيها هنا (جرب شريط البحث!)ع(

نقطة اللم الوحيدة كانت استخدام حق `HttpClient` بصفـث `UmamiClient` -مصنفة. -مصنفة. بسبب تحديد نطاق العميل والخدمة التي استخدمتها `IServiceScopeFactory` ويُحقَن في بناية الخدمة المُضيفة ثم يُمسك بها مقابل كل طلب إرسال.

```csharp
    

    private async Task SendRequest(CancellationToken token)
    {
        logger.LogInformation("Umami background delivery started");

        while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
                try
                {
                   using  var scope = scopeFactory.CreateScope();
                    var client = scope.ServiceProvider.GetRequiredService<UmamiClient>();
                    // Send the event via the client
                    await client.Send(payload.Payload);

                    logger.LogInformation("Umami background event sent: {EventType}", payload.EventType);
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Umami background delivery canceled.");
                    return; // Exit the loop on cancellation
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending Umami background event.");
                }
            }
        }
    }
   
```

### باء - الخدمات المستضافة

والآن وقد حصلنا على هذه الخدمة المستضيفة، يمكننا أن نحسن الأداء بشكل كبير بإرسال الأحداث في الخلفية.

لقد استخدمت هذا في عدة أماكن مختلفة، في `Program.cs` قررت أن أختبر تتبع طلب تغذية RSS باستخدام برنامج Middleware، إنه فقط يكشف أي مسار ينتهي في "RSS" ويرسل حدثاً خلفياً.

```csharp
app.Use( async (context, next) =>
{
var path = context.Request.Path.Value;
if (path.EndsWith("RSS", StringComparison.OrdinalIgnoreCase))
{
var rss = context.RequestServices.GetRequiredService<UmamiBackgroundSender>();
// Send the event in the background
await rss.SendBackground(new UmamiPayload(){Url  = path, Name = "RSS Feed"});
}
await next();
});
```

لقد عبرت أيضاً المزيد من البيانات من `TranslateAPI` نقطة النهاية.
مما يسمح لي برؤية المدة التي تستغرقها الترجمات؛ لاحظ أن أياً من هذه لا يحجب الخيط الرئيسي أو تتبع المستخدمين الأفراد.

```csharp
    
       await  umamiClient.SendBackground(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
        var result = new TranslateResultTask(translationTask, true);
```

# في الإستنتاج

AMOMAMI API هو نوع من المراوغة لكنها وسيلة كبيرة لتتبع الأحداث بطريقة ذاتية الاستضافة. آمل أن أحصل على فرصة لتنظيفه أكثر من ذلك والحصول على حزمة أومامي Nuget هناك.
مضافـة مـن مـن [المادة السابقة](/blog/addingascsharpclientforumamiapi)  أريد أن أسحب البيانات من أومامي لأزودها بملامح مثل فرز الشعبية.