# Перевірка модулів Umami.Net - перевірка даних Umami без використання Moq

# Вступ

У попередній частині цієї серії, де я перевірив[ Методи стеження за Umami.Net ](/blog/unittestingumaminet)

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024- 09- 04T20: 30</datetime>
[TOC]

## Проблема

В попередній частині я використовував Moq, щоб дати мені `Mock<HttpMessageHandler>` і повернути обробник, який використовується у `UmamiClient`, це типовий шаблон під час тестування коду, який використовує `HttpClient`. У цьому полі я покажу вам, як перевірити новий `UmamiDataService` Без мока.

```csharp
    public static HttpMessageHandler Create()
    {
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("api/send")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                // Read the request content
                var requestBody = request.Content != null
                    ? request.Content.ReadAsStringAsync(cancellationToken).Result
                    : null;

                // Create a response that echoes the request body
                var responseContent = requestBody != null
                    ? requestBody
                    : "No request body";


                // Return the response
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                };
            });

        return mockHandler.Object;
    }
```

## Навіщо користуватися моком?

Moq - це потужна бібліотека для висміювання, яка надає вам змогу створювати висміливі об' єкти для інтерфейсів і класів. Вона широко використовується у тестуванні одиниць, щоб виділити код під час перевірки залежностей. А втім, існують випадки, коли використання мока може бути нестерпним або навіть неможливим. Наприклад, під час перевірки коду, який використовує статичні методи або під час перевірки, код тісно поєднується з залежностями.

Приклад, поданий вище, дає нам багато гнучкості у тестуванні `UmamiClient` Клас, але він також має деякі недоліки. Это ОЧЕНЬ-код и делает то, что мне не нужно. Отже, під час тестування `UmamiDataService` Я вирішив спробувати інший підхід.

# Перевірка WamamiDataService

The `UmamiDataService` є майбутнім додатком до бібліотеки Umami.Net, яка надасть вам змогу отримувати дані з Умамі за такі речі, як перегляд сторінок, події певного типу, що сталися, відфільтровано великою кількістю параметрів країни, міста, ОС, розміру екрану тощо. Це дуже потужна, але зараз [API umami працює лише за допомогою JavaScript](https://umami.is/docs/api/website-stats). Отож, я хотів пограти з цими даними, щоб створити для них клієнта C#1.

The `UmamiDataService` Клас поділяється на multple часткові класи ( методи є SUPER довго), наприклад, ось тут `PageViews` метод.

Ви можете бачити, що багато коду створює queryString з переданого класу PageViewsRequest (у цьому можна скористатися іншими способами, але це можна зробити, наприклад, з використанням атрибутів або віддзеркалення працює у цьому розділі).

<details>
<summary>GetPageViews</summary>

```csharp
    public async Task<UmamiResult<PageViewsResponseModel>> GetPageViews(PageViewsRequest pageViewsRequest)
    {
        if (await authService.LoginAsync() == false)
            return new UmamiResult<PageViewsResponseModel>(HttpStatusCode.Unauthorized, "Failed to login", null);
        // Start building the query string
        var queryParams = new List<string>
        {
            $"startAt={pageViewsRequest.StartAt}",
            $"endAt={pageViewsRequest.EndAt}",
            $"unit={pageViewsRequest.Unit.ToLowerString()}"
        };

        // Add optional parameters if they are not null
        if (!string.IsNullOrEmpty(pageViewsRequest.Timezone)) queryParams.Add($"timezone={pageViewsRequest.Timezone}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Url)) queryParams.Add($"url={pageViewsRequest.Url}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Referrer)) queryParams.Add($"referrer={pageViewsRequest.Referrer}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Title)) queryParams.Add($"title={pageViewsRequest.Title}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Host)) queryParams.Add($"host={pageViewsRequest.Host}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Os)) queryParams.Add($"os={pageViewsRequest.Os}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Browser)) queryParams.Add($"browser={pageViewsRequest.Browser}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Device)) queryParams.Add($"device={pageViewsRequest.Device}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Country)) queryParams.Add($"country={pageViewsRequest.Country}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Region)) queryParams.Add($"region={pageViewsRequest.Region}");
        if (!string.IsNullOrEmpty(pageViewsRequest.City)) queryParams.Add($"city={pageViewsRequest.City}");

        // Combine the query parameters into a query string
        var queryString = string.Join("&", queryParams);

        // Make the HTTP request
        var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/pageviews?{queryString}");

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Successfully got page views");
            var content = await response.Content.ReadFromJsonAsync<PageViewsResponseModel>();
            return new UmamiResult<PageViewsResponseModel>(response.StatusCode, response.ReasonPhrase ?? "Success",
                content ?? new PageViewsResponseModel());
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await authService.LoginAsync();
            return await GetPageViews(pageViewsRequest);
        }

        logger.LogError("Failed to get page views");
        return new UmamiResult<PageViewsResponseModel>(response.StatusCode,
            response.ReasonPhrase ?? "Failed to get page views", null);
    }
```

</details>
Як ви можете бачити, це просто будує рядок запиту. Автентифікація виклику (див. [Остання стаття](/blog/unittestinglogginginaspnetcore) і потім дзвонить до API Умамі. Отже, як нам перевірити це?

## Перевірка служби umamiData

На відміну від тестування Умамілієнта, я вирішив перевірити `UmamiDataService` Без мока. Натомість, я створив просту `DelegatingHandler` Клас, який дозволяє допитувати прохання, а потім повертати відповідь. Цей підхід набагато простіший, ніж використання Moq і дозволяє мені перевірити `UmamiDataService` не обов'язково глузувати `HttpClient`.

У коді нижче ви можете побачити, що я просто розширюю `DelegatingHandler` і перевизначити `SendAsync` метод. Завдяки цьому я можу переглянути прохання і повернути відповідь, яка ґрунтується на його проханні.

```csharp
public class UmamiDataDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var absPath = request.RequestUri.AbsolutePath;
        switch (absPath)
        {
            case "/api/auth/login":
                var authContent = await request.Content.ReadFromJsonAsync<AuthRequest>(cancellationToken);
                if (authContent?.username == "username" && authContent?.password == "password")
                    return ReturnAuthenticatedMessage();
                else if (authContent?.username == "bad")
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }
            default:

                if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }

                if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/metrics"))
                {
                    var metricsRequest = GetParams<MetricsRequest>(request);
                    return ReturnMetrics(metricsRequest);
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
 }
```

## Налаштування

Щоб налаштувати новий `UmamiDataService` те саме, що і цей обробник.

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

Ви побачите, що я щойно встановив `ServiceCollection`, додати `FakeLogger<T>` (знову ж побачити [Остання стаття для подробиць щодо цього](/blog/unittestinglogginginaspnetcore) а потім встановити `UmamiData` служба з іменем користувача і паролем, яку я хочу використовувати (так що я можу перевірити помилку).

Потім я дзвоню `services.SetupUmamiData(username, password);` який є способом розширення, який я створив для налаштування `UmamiDataService` з `UmamiDataDelegatingHandler` і `AuthService`;

```csharp
    public static void SetupUmamiData(this IServiceCollection services, string username="username", string password="password")
    {
        var umamiSettings = new UmamiDataSettings()
        {
            UmamiPath = Consts.UmamiPath,
            Username = username,
            Password = password,
            WebsiteId = Consts.WebSiteId
        };
        services.AddSingleton(umamiSettings);
        services.AddHttpClient<AuthService>((provider,client) =>
        {
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
            

        }).AddHttpMessageHandler<UmamiDataDelegatingHandler>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));  //Set lifetime to five minutes

        services.AddScoped<UmamiDataDelegatingHandler>();
        services.AddScoped<UmamiDataService>();
    }
```

Ви бачите, що це те, в якому я занурююсь `UmamiDataDelegatingHandler` і `AuthService` до `UmamiDataService`. Шлях, яким це є структурованим, це те, що `AuthService` 'owns' `HttpClient` і `UmamiDataService` використовує `AuthService` щоб зробити дзвінки в API amami з `bearer` one і `BaseAddress` вже готова.

## Тести

Насправді це робить тестування дуже простим. Це трохи докладно, тому що я також хотів перевірити лісозаготівлю. Все, що він робить, це розміщує через мене. `DelegatingHandler` і я симулюю відповідь на основі запиту.

```csharp
public class UmamiData_PageViewsRequest_Test : UmamiDataBase
{
    private readonly DateTime StartDate = DateTime.ParseExact("2021-10-01", "yyyy-MM-dd", null);
    private readonly DateTime EndDate = DateTime.ParseExact("2021-10-07", "yyyy-MM-dd", null);
    
    [Fact]
    public async Task SetupTest_Good()
    {
        var serviceProvider = GetServiceProvider();
        var umamiDataService = serviceProvider.GetRequiredService<UmamiDataService>();
        var authLogger = serviceProvider.GetRequiredService<ILogger<AuthService>>();
        var umamiDataLogger = serviceProvider.GetRequiredService<ILogger<UmamiDataService>>();
        var result = await umamiDataService.GetPageViews(StartDate, EndDate);
        var fakeAuthLogger = (FakeLogger<AuthService>)authLogger;
        FakeLogCollector collector = fakeAuthLogger.Collector; 
        IReadOnlyList<FakeLogRecord> logs = collector.GetSnapshot();
        Assert.Contains("Login successful", logs.Select(x => x.Message));
        
        var fakeUmamiDataLogger = (FakeLogger<UmamiDataService>)umamiDataLogger;
        FakeLogCollector umamiDataCollector = fakeUmamiDataLogger.Collector;
        IReadOnlyList<FakeLogRecord> umamiDataLogs = umamiDataCollector.GetSnapshot();
        Assert.Contains("Successfully got page views", umamiDataLogs.Select(x => x.Message));
        
        Assert.NotNull(result);
    }
}
```

### Симуляція реакції

Щоб симулювати відповідь цього методу ви пам'ятаєте, я маю цю лінію в `UmamiDataDelegatingHandler`:

```csharp
  if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }
```

Все, що це робить - це витягає інформацію з рядка діалогу і створює "реалістичну " відповідь (засновану на Live Tests, які я зібрав, знову ж таки дуже мало документації). Ви побачите, що я тестую кількість днів між початковою та кінцевою датою, а потім повертаю відповідь з такою ж кількістю днів.

```csharp
    private static HttpResponseMessage ReturnPageViewsMessage(PageViewsRequest request)
    {
        var startAt = request.StartAt;
        var endAt = request.EndAt;
        var startDate = DateTimeOffset.FromUnixTimeMilliseconds(startAt).DateTime;
        var endDate = DateTimeOffset.FromUnixTimeMilliseconds(endAt).DateTime;
        var days = (endDate - startDate).Days;

        var pageViewsList = new List<PageViewsResponseModel.Pageviews>();
        var sessionsList = new List<PageViewsResponseModel.Sessions>();
        for(int i=0; i<days; i++)
        {
            
            pageViewsList.Add(new PageViewsResponseModel.Pageviews()
            {
                x = startDate.AddDays(i).ToString("yyyy-MM-dd"),
                y = i*4
            });
            sessionsList.Add(new PageViewsResponseModel.Sessions()
            {
                x = startDate.AddDays(i).ToString("yyyy-MM-dd"),
                y = i*8
            });
        }
        var pageViewResponse = new PageViewsResponseModel()
        {
            pageviews = pageViewsList.ToArray(),
            sessions = sessionsList.ToArray()
        };
        var json = JsonSerializer.Serialize(pageViewResponse);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
```

# Включення

Отже, це дійсно досить просто перевірити `HttpClient` Просьба, не використовуючи Мока, і я думаю, що це набагато чистіше. Ви втрачаєте трохи витонченості мока, але для простих тестів, як цей, я думаю, це гарна відмова.