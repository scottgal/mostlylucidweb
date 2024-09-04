# (ب) وحدة اختبار وحدة اختبار أمومي.

# أولاً

(نعم أنا على علم بالخلافات) وكنت أحاول اختبار خدمة جديدة أنا أضيفها إلى أومامي. نت، أمامي دياتا. هذه خدمة تسمح لي بسحب البيانات من حالة أميامي الخاصة بي لأستخدمها في أشياء مثل تصنيف المواقع حسب الشعبية...

[TOC]

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04-TT13: 22</datetime>

# المشكلة

كنت أحاول إضافة اختبار بسيط لدالة الولوج التي أحتاج إلى استخدامها عند سحب البيانات.
كما ترون انها خدمة بسيطة تمرر اسم مستخدم وكلمة مرور إلى `/api/auth/login` نقطة النهاية و تحصل على نتيجة. إذا كانت النتيجة ناجحة فإنه يخزن الرمز في `_token` فـي ميـان ومـا يرسـل مـن مـن مـن `Authorization` مقدمـة مـن أجـل `HttpClient` (ج) أن تستخدم في الطلبات المقبلة.

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

الآن أردت أيضاً أن أختبر ضد اللوغاريتم للتأكد من أنه يقوم بتسجيل الرسائل الصحيحة. أنا أستخدِم `Microsoft.Extensions.Logging` أنا و أنا أردنا أن نختبر أن السجل الصحيح لرسائل تم كتابته إلى اللوغارتم.

في (موق) هناك BUNCH من المواقع حول اختبار قطع الأشجار لديهم جميعاً هذا الشكل الأساسي (من https://adamstorr.co.uk/blog/mocking-iloger-we-moq/)

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

ما هو ناتج عن التغييرات الأخيرة لموك (هو. Is Anythype هو الآن عتيق) و ASP.NET التغييرات الأساسية إلى LogValues كنت أواجه صعوبة في الحصول على هذا العمل.

لقد جربت مجموعة من النسخ والمتغيرات لكنها فشلت دائماً لذا... استسلمت.

# الإحلال

لذا قراءة مجموعة من رسائل جيت هوب صادفت مقالة لديفيد فاولر (زميلي السابق والآن لورد نيت) والتي أظهرت طريقة بسيطة لاختبار قطع الأشجار في ASP.Net or.
هذا استخدامات *جديد بالنسبة لي* `Microsoft.Extensions.Diagnostics.Testing` [1 ف-4، 1 ف-3، 1 خ م، 1 خ ع (رأ)](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Testing) التي لها بعض التمديدات المفيدة حقاً لاختبار قطع الأشجار.

اذاً بدلاً من كل الاشياء المموقة انا فقط اضيفت `Microsoft.Extensions.Diagnostics.Testing` وأضيف ما يلي إلى اختباراتي.

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

سترين أن هذا يُعدّ مُهمّتي في الخدمة، إضافةً للجديد `FakeLogger<T>` ثم يُثبِّط ثم يُقَرِّر `UmamiData` الخدمة مع اسم المستخدم و كلمة السر التي أريد استخدامها (حتى أتمكن من اختبار الفشل).

## الـ إختبارات مُستخدِم المُرْمِج المُمز

ومن ثم يمكن أن تصبح اختباراتي:

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

حيث سترين أني ببساطة أتصل بـ `GetServiceProvider` للحصول على خدماتي، ثم الحصول على `AuthService` وقد عقد مؤتمراً بشأن `ILogger<AuthService>` من مقدّم الخدمة.

لأن لدي هذه الانشاءات `FakeLogger<T>` يمكنني بعد ذلك الوصول إلى `FakeLogCollector` وقد عقد مؤتمراً بشأن `FakeLogRecord` للحصول على السجلات والتحقق منها.

ثم يمكنني ببساطة التحقق من سجلات الرسائل الصحيحة.

# في الإستنتاج

إذاً ها هو لديك، طريقة بسيطة لاختبار رسائل السجل في اختبارات الوحدة بدون هراء الموك.