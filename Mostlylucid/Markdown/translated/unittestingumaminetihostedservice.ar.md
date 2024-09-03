# (أ) وحدة اختبار وحدة اختبار أمومي.

# أولاً

وفي المادة السابقة، ناقشنا كيفية اختبار `UmamiClient` XUnit and Muq. في هذه المادة، سنناقش كيفية اختبار `UmamiBackgroundSender` -مصنفة. -مصنفة. الـ `UmamiBackgroundSender` مختلف نوعاً ما عن `UmamiClient` على النحو الذي تستخدمه `IHostedService` (ب) أن تظل تعمل في الخلفية وترسل الطلبات من خلال `UmamiClient` تماماً خارج الخيط التنفيذ الرئيسي (لذا فإنه لا يمنع التنفيذ).

كالمعتاد يمكنك أن ترى كل شفرة المصدر لهذا على بلدي جيت هوب [هنا هنا](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net.Test/UmamiBackgroundSender_Tests.cs).

[TOC]

<!--category-- xUnit, Umami, IHostedService, Moq -->
<datetime class="hidden">2024-09-03-03T09-00</datetime>

## `UmamiBackgroundSender`

الهيكل الفعلي `UmamiBackgroundSender` هو بسيط جداً. وهي خدمة مستضافة ترسل الطلبات إلى خادوم أومامي بمجرد أن تكتشف طلباً جديداً. الهيكل الأساسي `UmamiBackgroundSender` وفيما يلي بيان بالصنف:

```csharp
public class UmamiBackgroundSender(IServiceScopeFactory scopeFactory, ILogger<UmamiBackgroundSender> logger) : IHostedService
{

    private  Channel<SendBackgroundPayload> _channel = Channel.CreateUnbounded<SendBackgroundPayload>();

    private Task _sendTask = Task.CompletedTask;
    
        public Task StartAsync(CancellationToken cancellationToken)
    {

        _sendTask = SendRequest(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }
    
            public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("UmamiBackgroundSender is stopping.");

            // Signal cancellation and complete the channel
            await _cancellationTokenSource.CancelAsync();
            _channel.Writer.Complete();
            try
            {
                // Wait for the background task to complete processing any remaining items
                await Task.WhenAny(_sendTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("StopAsync operation was canceled.");
            }
        }
        
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
                    await client.Send(payload.Payload, type:payload.EventType);

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

    private record SendBackgroundPayload(string EventType, UmamiPayload Payload);
    
    }

```

كما يمكنك أن ترى أن هذا مجرد كلاسيكي `IHostedService` يضاف إلى مجموعتنا الخدمية في ASP.net مستخدماً `services.AddHostedService<UmamiBackgroundSender>()` من الناحية العملية. هذه الرُفَقَةِ مِنْ `StartAsync` عندما يبدأ التطبيق.
النظرات داخل `SendRequest` الطريقة هي حيث يحدث السحر. هنا حيث نقرأ من القناة ونرسل الطلب إلى خادم أمامي.

وهذا يستبعد الأساليب الفعلية لإرسال الطلبات (الواردة أدناه).

```csharp
public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)

public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)   

        public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null)

public async Task Track(string eventName, UmamiEventData? eventData = null)

public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

كل ما تقوم به هذه حقاً هو حزم الطلب حتى `SendBackgroundPayload` سجلها وأرسلها إلى القناة.

"عشنا يُستقبل حلقة في" `SendRequest` سوف تستمر في القراءة من القناة حتى يتم إغلاقها. وهذا هو المكان الذي سنركز فيه جهود الاختبار التي نبذلها.

```csharp
  while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
            }
        }    

```

الخدمة الخلفية لديها بعض الدلالات التي تسمح لها بإطلاق الرسالة بمجرد وصولها.
هذا يثير مشكلة، إذا لم نحصل على قيمة عائدة من `Send` كيف نختبر أن هذا في الواقع يفعل أي شيء؟

## اختبار الاختبار `UmamiBackgroundSender`

اذاً السؤال هو كيف نختبر هذه الخدمة 5n ليس هناك استجابة للاختبار ضدها؟

الجواب هو حقن `HttpMessageHandler` ونرسلها إلى مركزنا الخاص بـ "أمامي" هذا سيسمح لنا باعتراض الطلب والتحقق من محتوياته

### ايكو صدر المُرْندلر

ستتذكرين من المقالة السابقة التي أعددنا لها نسخة من (هاندرلر) هذه الحياة داخل `EchoMockHandler` الصف الثابت:

```csharp
public static class EchoMockHandler
{
    public static HttpMessageHandler Create(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFunc)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) =>
                responseFunc(request, cancellationToken).Result);

        return mockHandler.Object;
    }
```

يمكنك أن ترى هنا نستخدم Mok لوضع `SendAsync` (في HtttpClient يتم تنفيذ جميع طلبات Async من خلال `SendAsync`).

ترى نحن أولاً نُهيّئُ الموك

```csharp
     var mockHandler = new Mock<HttpMessageHandler>();
```

ثم نستخدم سحر `Protected` من أجل إنشاء `SendAsync` من الناحية العملية. هذا هو السبب `SendAsync` لا يمكن الوصول إليه عادة في الجمهور `HttpMessageHandler`.

```csharp
public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
        }
        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
```

ثم نستخدم فقط الـ cas-all `ItExpr.IsAny` بما يتطابق مع أي طلب وإعادة الرد الوارد من `responseFunc` نحن نمر في.

## أساليب الاختبار.

الـ داخل الـ `UmamiBackgroundSender_Tests` لدينا طريقة مشتركة لتعريف جميع طرق الاختبار.

### إنشاء

```csharp
[Fact]
    public async Task Track_Page_View()
    {
        var page = "https://background.com";
        var title = "Background Example Page";
        var tcs = new TaskCompletionSource<bool>();
        // Arrange
        var handler = EchoMockHandler.Create(async (message, token) =>
        {
            try
            {
                var responseContent = EchoMockHandler.ResponseHandler(message, token);
                var jsonContent = await responseContent.Result.Content.ReadFromJsonAsync<EchoedRequest>(token);
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                Assert.Contains("api/send", message.RequestUri.ToString());
                Assert.NotNull(jsonContent);
                Assert.Equal(page, jsonContent.Payload.Url);
                Assert.Equal(title, jsonContent.Payload.Title);
                // Signal completion
                tcs.SetResult(true);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            }
            catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        });

        var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.TrackPageView(page, title);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("The background task did not complete in time.");
        }
        
        await tcs.Task;
        await backgroundSender.StopAsync(CancellationToken.None);
    }
```

ما أن نكون قد حددنا هذا يجب أن ندير `IHostedService` مدة العمر في طريقة الاختبار:

```csharp
       var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.TrackPageView(page, title);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("The background task did not complete in time.");
        }
        
        await tcs.Task;
        await backgroundSender.StopAsync(CancellationToken.None);
    }
```

يمكنكم أن تروا أننا نمر في الطريق إلى `GetServices` طريقة إنشاء:

```csharp
    private (UmamiBackgroundSender, IHostedService) GetServices(HttpMessageHandler handler)
    {
        var services = SetupExtensions.SetupServiceCollection(handler: handler);
        services.AddScoped<UmamiBackgroundSender>();
       

        services.AddScoped<IHostedService, UmamiBackgroundSender>(provider =>
            provider.GetRequiredService<UmamiBackgroundSender>());
        SetupExtensions.SetupUmamiClient(services);
        var serviceProvider = services.BuildServiceProvider();
        var backgroundSender = serviceProvider.GetRequiredService<UmamiBackgroundSender>();
        var hostedService = serviceProvider.GetRequiredService<IHostedService>();
        return (backgroundSender, hostedService);
    }
```

نحن هنا نعبر في معالجتنا إلى خدماتنا لنعلّقها في الـ `UmamiClient` -مُعَدّة. -مُعَدّة.

ثم نضيف `UmamiBackgroundSender` مجموعــة ومــن `IHostedService` من مقدّم الخدمة. ثم نعيد هذا إلى فئة الاختبار للسماح باستخدامه.

#### نوع الخدمة الخدمة

الآن بما أن لدينا كل هذه الأشياء يمكن أن نكون ببساطة `StartAsync` الخدمة المستضيفة، استخدمها ثم انتظر حتى توقف:

```csharp
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.TrackPageView(page, title);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("The background task did not complete in time.");
        }
        
        await tcs.Task;
        await backgroundSender.StopAsync(CancellationToken.None);
```

وسيبدأ هذا النظام الخدمة المستضيفة، ويرسل الطلب، وينتظر الرد ثم يوقف الخدمة.

### عنوان الرسالة

نبدأ أولاً بوضع `EchoMockHandler` وقد عقد مؤتمراً `TaskCompletionSource` الذي سيشير إلى أن الاختبار كامل. وهذا أمر مهم لإعادة السياق إلى الخيط الاختباري الرئيسي حتى نتمكن من التقاط الإخفاقات والفترات الزمنية بشكل صحيح.

الـ ` async (message, token) => {}` هي الوظيفة التي ننقلها إلى معالجنا الذي ذكرناه آنفاً هنا يمكننا التحقق من الطلب وإعادة الرد (الذي في هذه الحالة نحن حقا لا نفعل أي شيء مع).

- - - - - - - - - - - - - `EchoMockHandler.ResponseHandler` هي طريقة مساعدة تعيد الهيئة المطلوبة إلى طريقتنا، وهذا ما يتيح لنا التحقق من أن الرسالة تمر من خلال `UmamiClient` - - - - - - - - - - `HttpClient` حقًّا حقًّا حقًّا.

```csharp
    public static async Task<HttpResponseMessage> ResponseHandler(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Read the request content
        var requestBody = request.Content?.ReadAsStringAsync(cancellationToken).Result;
        // Create a response that echoes the request body
        var responseContent = requestBody ?? "No request body";
        // Return the response
        return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        });
    }
```

ثم نمسك هذا الرد ونزيله إلى `EchoedRequest` (أ) الهدف من الهدف. هذا كائن بسيط يمثل الطلب الذي أرسلناه إلى الخادم.

```csharp
public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}
```

ترى أن هذا يلخص `Type` وقد عقد مؤتمراً بشأن `Payload` (ب) تقديم طلب إلى الجمعية العامة في دورتها الثامنة والخمسين بشأن هذا الطلب. هذا ما سنتحقق منه في اختبارنا

```csharp
      Assert.Contains("api/send", message.RequestUri.ToString());
      Assert.NotNull(jsonContent);
      Assert.Equal(page, jsonContent.Payload.Url);
      Assert.Equal(title, jsonContent.Payload.Title);
```

ما هو المهم هنا هو كيف نتعامل مع الاختبارات الفاشلة، حيث أننا لسنا في السياق الرئيسي الرئيسي للسياق الرئيسي هنا نحن بحاجة إلى استخدام `TaskCompletionSource` أن تشير مرة أخرى إلى الخيط الرئيسي أن الاختبار فشل.

```csharp
     catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
```

هذا سيحدد الاستثناء على `TaskCompletionSource` وارجع a خطأ إلى إختبار.

# في الإستنتاج

إذاً هذه أول وظيفة من وظائفي الأكثر تفصيلاً `IHostedService` الأمر بهذا كما أنه معقد نوعا ما لاختبار عندما مثل هنا فإنه لا يعيد قيمة للمتصل.