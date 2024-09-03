# इकाई जांच उममी. net - जाँच उममी पृष्ठभूमि

# परिचय

पिछले लेख में हमने चर्चा की कि कैसे परीक्षा की जा सकती है `UmamiClient` एक्स- इकाई तथा मोजेजे का उपयोग करें. इस लेख में हम चर्चा करेंगे कि परीक्षा का सामना कैसे किया जा सकता है `UmamiBackgroundSender` वर्ग. वह `UmamiBackgroundSender` थोड़ा अलग है `UmamiClient` जैसा कि यह प्रयोग करता है `IHostedService` पृष्ठ भूमि में चल रहे रहने के लिए तथा से होकर अनुरोध करता है. `UmamiClient` मुख्य कार्य थ्रेड से पूरी तरह बाहर (इसलिए यह चलाने योग्य नहीं है).

हमेशा के रूप में आप मेरे GiHb पर इस सभी स्रोत कोड देख सकते हैं [यहाँ](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net.Test/UmamiBackgroundSender_Tests.cs).

[TOC]

<!--category-- xUnit, Umami, IHostedService, Moq -->
<datetime class="hidden">2024-09-03टी09: 00</datetime>

## `UmamiBackgroundSender`

वास्तविक स्ट्रक्चर `UmamiBackgroundSender` काफी सरल है. यह एक होस्ट सेवा है जो उममी सर्वर को निवेदन करता है जैसे ही यह एक नया निवेदन का पता लगाता है. मूल संरचना `UmamiBackgroundSender` वर्ग नीचे दिखाया गया है:

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

जैसा कि आप देख सकते हैं यह सिर्फ एक क्लासिक है `IHostedService` यह एनईएस में हमारी सेवा संग्रह के लिए जोड़ा है। `services.AddHostedService<UmamiBackgroundSender>()` विधि. यह बंद लात मारता है `StartAsync` जब अनुप्रयोग प्रारंभ होता है तो विधि.
अंदर की तरफ देखो `SendRequest` विधि है जहां जादू होता है. यह वह जगह है जहाँ हम चैनल से पढ़ा और उममी सर्वर को निवेदन भेजें.

यह निवेदन भेजने के लिए वास्तविक तरीकों को अलग करता है (नीचे दिखाया गया है).

```csharp
public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)

public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)   

        public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null)

public async Task Track(string eventName, UmamiEventData? eventData = null)

public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

इन सभी को वास्तव में क्या करना है यह निवेदन को चालू करता है `SendBackgroundPayload` रिकार्ड कर चैनल पर भेजें.

हमारे घोंसले में लूप मिलता है `SendRequest` जब तक चैनल बंद नहीं हो जाता तब तक चैनल से पढ़ने के लिए. इसी में हम अपनी जाँच करने की कोशिशों पर ध्यान केंद्रित करेंगे.

```csharp
  while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
            }
        }    

```

पृष्ठभूमि सेवा में कुछ ऐसे सिद्धांत हैं, जो इसे जल्द - से - जल्द संदेश को आग में डालने की अनुमति देते हैं ।
हालांकि यह एक समस्या पैदा करता है, अगर हम से वापस मूल्य नहीं मिलता है `Send` हम यह कैसे परीक्षण वास्तव में कुछ कर रहा है?

## जाँच की जा रही है `UmamiBackgroundSender`

तो सवाल यह है कि हम इस सेवा की जाँच कैसे करते हैं 5n वहाँ वास्तव में जाँच करने के लिए कोई प्रतिक्रिया नहीं है?

उत्तर बाहर होने वाले में है `HttpMessageHandler` ठट्ठा करने के लिए हम अपने उमामीप में भेजें. यह हमें अनुरोध को रोकने और यह सामग्री की जाँच करने देगा.

### इकोMock ("1. 1) संदेशHander

आप पिछले लेख से याद करेंगे...... हम एक ठट्ठा संदेश Herder सेट किया. इस जीवन के अंदर `EchoMockHandler` स्थिर वर्ग:

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

आप यहाँ देख सकते हैं कि हम एक सेट करने के लिए नकली इस्तेमाल करते हैं `SendAsync` विधि जो निवेदन पर आधारित प्रतिक्रिया प्राप्त करेगा (सूची में सभी अतुल्यकालिक निवेदन के माध्यम से किया जाता है) `SendAsync`).

आप हम पहले नकली सेटअप देखते हैं

```csharp
     var mockHandler = new Mock<HttpMessageHandler>();
```

तो हम जादू का उपयोग करें `Protected` सेट करने के लिए `SendAsync` विधि. यह इसलिए है `SendAsync` सामान्य रूप से एपीआई में पहुँच नहीं है `HttpMessageHandler`.

```csharp
public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
        }
        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
```

तो हम सिर्फ ले-सब का उपयोग करते हैं `ItExpr.IsAny` कोई भी अनुरोध को मिलान करने के लिए और प्रतिक्रिया प्राप्त करें `responseFunc` हम अंदर जा रहे हैं.

## जाँच विधियाँ.

भीतर `UmamiBackgroundSender_Tests` वर्ग हम सभी जांच विधियों को परिभाषित करने के लिए एक आम तरीका है.

### सेटअप

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

एक बार हम इस परिभाषित किया है हमें अपने प्रबंधन करने की जरूरत है `IHostedService` जाँच विधि में जीवन:

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

आप हमारे लिए हैंडलर में पारित देख सकते हैं `GetServices` सेटअप विधि:

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

यहाँ हम हमारे हैंडलर में इसे हुक करने के लिए पारित `UmamiClient` सेटअप.

तब हम और भी कुछ करते हैं `UmamiBackgroundSender` सेवा संग्रह के लिए और प्राप्त करने के लिए `IHostedService` सेवा प्रदायक से. फिर इस जाँच वर्ग को यह इस्तेमाल करने की अनुमति देने के लिए वापस आ जाओ.

#### होस्ट सेवा जीवन काल

अब कि हम इन सभी को स्थापित किया है हम सिर्फ कर सकते हैं `StartAsync` होस्ट सेवा, इसे तब तक इंतजार करें जब तक कि यह बंद नहीं हो जाता:

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

यह होस्ट सेवा शुरू करेगा, निवेदन भेजने के लिए, तब प्रतिक्रिया के लिए इंतजार करें सेवा बंद.

### संदेश हैंडलर

शुरू करने से पहले हम शुरू करते हैं `EchoMockHandler` और `TaskCompletionSource` जो कि जाँच पूर्ण है, वह संकेत करता है. मुख्य परीक्षा थ्रेड के संदर्भ को वापस लाने के लिए यह ज़रूरी है ताकि हम सही तरह से हार मान लें और समय - समय पर यह काम पूरा कर सकें ।

वह ` async (message, token) => {}` हम ऊपर उल्लेख किया हमारे उपहास हैंडलर में जाने वाले समारोह है. यहाँ में हम निवेदन की जाँच कर सकते हैं और एक प्रतिक्रिया वापस कर सकते हैं (जो इस मामले में हम वास्तव में कुछ नहीं करते हैं).

हमारा `EchoMockHandler.ResponseHandler` यह एक सहायक तरीक़ा है जो हमारी विधि में वापस निवेदन शरीर को लौटाएगा, यह हमें संदेश की जाँच करने देता है `UmamiClient` करने के लिए `HttpClient` सही.

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

फिर हम इस प्रतिक्रिया को पकड़ और इसे एक में उड़ा देते हैं `EchoedRequest` वस्तु. यह एक सरल वस्तु है जो हमने सर्वर को भेजा है.

```csharp
public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}
```

आप इस Sriproquation देख रहे हैं `Type` और `Payload` निवेदन का. यह है कि हम हमारी परीक्षा में खिलाफ जाँच करेंगे.

```csharp
      Assert.Contains("api/send", message.RequestUri.ToString());
      Assert.NotNull(jsonContent);
      Assert.Equal(page, jsonContent.Payload.Url);
      Assert.Equal(title, jsonContent.Payload.Title);
```

यहाँ महत्वपूर्ण क्या है कि हम असफल परीक्षण संभाल कैसे है, के रूप में हम मुख्य थ्रेड संदर्भ में नहीं हैं के रूप में हम यहाँ इस्तेमाल करने की जरूरत है `TaskCompletionSource` मुख्य लड़ी को संकेत देना है कि परीक्षा असफल हो गई है.

```csharp
     catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
```

यह अपवाद अपवाद पर नियत करेगा `TaskCompletionSource` जाँच में 500 त्रुटि लौटा.

# ऑन्टियम

तो यह है कि मेरे बजाय अधिक विस्तृत पोस्ट का पहला है, `IHostedService` इस के रूप में यह वास्तव में जटिल है जाँच करने के लिए जब यह यहाँ की तरह यह कॉलर के लिए एक मूल्य नहीं वापस आता है.