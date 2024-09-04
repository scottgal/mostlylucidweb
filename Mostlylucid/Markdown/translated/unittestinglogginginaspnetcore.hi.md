# इकाई जांच उममी - एनईएस में लॉग किया जा रहा है.

# परिचय

मैं मोजे का उपयोग नहीं कर रहा हूँ (हाँ) और मैं एक नई सेवा की जाँच करने की कोशिश कर रहा हूँ मैं उममी, उममीओ। यह एक सेवा है यह मुझे मेरे उममी उदाहरण से डाटा खींचने की अनुमति देता है...

[विषय

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-4T13: 22</datetime>

# समस्या

मैं लॉगिन फंक्शन के लिए एक सरल परीक्षण जोड़ने की कोशिश कर रहा था जब डेटा खींचते समय मुझे उपयोग करना होगा.
जैसा कि आप देख सकते हैं यह एक सरल सेवा है जो एक उपयोक्ता नाम और पासवर्ड आगे जाता है `/api/auth/login` अंत - बिन्दु और परिणाम हो जाता है । यदि परिणाम सफल होगा तो यह टोकन उस संकेत को भंडारित करेगा `_token` क्षेत्र और सेट `Authorization` शीर्षिका के लिए `HttpClient` भविष्य में निवेदन में प्रयोग करने के लिए.

```csharp
public class AuthService(HttpClient httpClient, UmamiDataSettings umamiSettings, ILogger<AuthService> logger)
{
    private string _token = string.Empty;
    public HttpClient HttpClient => httpClient;

    public async Task<bool> LoginAsync()
    {
        var loginData = new
        {
            username = umamiSettings.Username,
            password = umamiSettings.Password
        };

        var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("/api/auth/login", content);

        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (authResponse == null)
            {
                logger.LogError("Login failed");
                return false;
            }

            _token = authResponse.Token;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            logger.LogInformation("Login successful");
            return true;
        }

        logger.LogError("Login failed");
        return false;
    }
}
```

अब मैं लॉगर के खिलाफ भी जांच करना चाहता था सुनिश्चित करने के लिए यह सही संदेशों की गणना कर रहा था. मैं उपयोग कर रहा हूँ `Microsoft.Extensions.Logging` नेमस्पेस और मैं जांच करना चाहता था कि सही लॉग संदेश लॉग लॉग लॉगर के लिए लिखा जा रहा है.

Moquck में जाँच के आसपास पोस्ट्स के एक bUNCH है इन सभी के पास यह मूल रूप है ( http://sssms.co.k/ Mkak/ mak- dw-mp- dw-mer-mer-s/ kuck) के साथ पोस्ट के चारों ओर पोस्टों का एक विशेष रूप से सेट है।

```csharp
public static Mock<ILogger<T>> VerifyDebugWasCalled<T>(this Mock<ILogger<T>> logger, string expectedMessage)
{
    Func<object, Type, bool> state = (v, t) => v.ToString().CompareTo(expectedMessage) == 0;
    
    logger.Verify(
        x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Debug),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => state(v, t)),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));

    return logger;
}
```

कैसे कभी मोक के हाल ही में किए परिवर्तन के कारण (यह. कोई भी प्रकार अब लुप्त हो गया है) और एक अतिरिक्त है. NENINVVV मूल्यों के लिए परिवर्तन मैं काम करने के लिए एक कठिन समय हो रहा था.

मैं संस्करण और भिन्नताओं के एक बिल की कोशिश की लेकिन यह हमेशा असफल रहा. तो... मैं छोड़ दिया.

# हल

"मेरा पहले सहकर्मी और अब का प्रभु" जो एक सरल तरीका दिखाई दिया अभ्यय में लॉगिंग की जाँच करने के लिए. NENT.NT कोर.
यह उपयोग करता है *मेरे लिए नया* `Microsoft.Extensions.Diagnostics.Testing` [पैकेज](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Testing) लॉगिंग जाँच के लिए कुछ वास्तव में उपयोगी विस्तार है.

तो सब के बजाय मैं सिर्फ जोड़ दिया `Microsoft.Extensions.Diagnostics.Testing` पैकेज और मेरी जांच में निम्नलिखित को जोड़ा.

```csharp
    public IServiceProvider GetServiceProvider (string username="username", string password="password")
    {
        var services = new ServiceCollection();
        var mockLogger = new FakeLogger<UmamiDataService>();
        var authLogger = new FakeLogger<AuthService>();
        services.AddScoped<ILogger<UmamiDataService>>(_ => mockLogger);
        services.AddScoped<ILogger<AuthService>>(_ => authLogger);
        services.SetupUmamiData(username, password);
        return  services.BuildServiceProvider();
        
    }
```

आप देखेंगे कि यह मेरे सेवा-पत्रर को सेट करता है, नया जोड़ता है `FakeLogger<T>` फिर एक ज़रूरी चीज़ (बारिश) को तक़सीम करती हैं `UmamiData` उपयोक्ता नाम व पासवर्ड के साथ सेवा मैं प्रयोग करना चाहता हूँ (इसलिए मैं विफलता का परीक्षण कर सकता हूँ).

## परीक्षण झूठे दोषों का उपयोग करते हुए

तो मेरी जाँच हो सकती है:

```csharp
    [Fact]
    public async Task SetupTest_Good()
    {
        var serviceProvider = GetServiceProvider();
        var authService = serviceProvider.GetRequiredService<AuthService>();
        var authLogger = serviceProvider.GetRequiredService<ILogger<AuthService>>();
        var result = await authService.LoginAsync();
        var fakeLogger = (FakeLogger<AuthService>)authLogger;
        FakeLogCollector collector = fakeLogger.Collector; // Collector allows you to access the captured logs
         IReadOnlyList<FakeLogRecord> logs = collector.GetSnapshot();
         Assert.Contains("Login successful", logs.Select(x => x.Message));
        Assert.True(result);
    }
```

तुम कहाँ मैं सिर्फ फोन करूँगा देखते हैं `GetServiceProvider` मेरी सेवा प्रदाता पाने के लिए विधि, तो मिलता है `AuthService` और `ILogger<AuthService>` सेवा प्रदायक से.

क्योंकि मैंने ये सब इस तरह स्थापित किया है `FakeLogger<T>` तब मैं पहुँच सकता हूँ `FakeLogCollector` और `FakeLogRecord` लॉग प्राप्त करने के लिए और उन्हें जाँच करें.

तो मैं सिर्फ सही संदेश के लिए लॉग की जाँच कर सकते हैं.

# ऑन्टियम

तो वहाँ आपके पास है, एक सरल तरीका है इकाई परीक्षणों में लॉग संदेशों की जाँच करने के लिए बिना बकवास के।