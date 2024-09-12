# Μονάδα δοκιμής Umami.Net - Δοκιμή δεδομένων Umami χωρίς χρήση Moq

# Εισαγωγή

Στο προηγούμενο μέρος αυτής της σειράς όπου δοκίμασα[ Umami.Net tracking methods ](/blog/unittestingumaminet)

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T20:30</datetime>
[TOC]

## Το Πρόβλημα

Στο προηγούμενο μέρος χρησιμοποίησα Moq για να μου δώσει ένα `Mock<HttpMessageHandler>` και να επιστρέψει τον χειριστή που χρησιμοποιείται σε `UmamiClient`, αυτό είναι ένα κοινό μοτίβο κατά τη δοκιμή κώδικα που χρησιμοποιεί `HttpClient`. Σε αυτή τη θέση θα σας δείξω πώς να δοκιμάσετε το νέο `UmamiDataService` χωρίς χρήση Moq.

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

## Γιατί να χρησιμοποιήσεις τον Μακ;

Moq είναι μια ισχυρή βιβλιοθήκη κοροϊδίας που σας επιτρέπει να δημιουργήσετε ψεύτικα αντικείμενα για διεπαφές και τάξεις. Χρησιμοποιείται ευρέως σε δοκιμές μονάδας για την απομόνωση του υπό δοκιμή κωδικού από τις εξαρτήσεις του. Ωστόσο, υπάρχουν ορισμένες περιπτώσεις όπου η χρήση του Moq μπορεί να είναι δυσκίνητη ή ακόμη και αδύνατη. Για παράδειγμα, όταν ο κωδικός δοκιμής χρησιμοποιεί στατικές μεθόδους ή όταν ο υπό δοκιμή κωδικός συνδέεται στενά με τις εξαρτήσεις του.

Το παράδειγμα που έδωσα παραπάνω δίνει πολλή ευελιξία στη δοκιμή `UmamiClient` Τάξη, αλλά έχει και κάποια μειονεκτήματα. Είναι άσχημος κώδικας και κάνει πολλά πράγματα που δεν χρειάζομαι. Έτσι, όταν δοκιμάζετε `UmamiDataService` Αποφάσισα να δοκιμάσω μια διαφορετική προσέγγιση.

# Δοκιμή UmamiDataService

Η `UmamiDataService` είναι μια μελλοντική προσθήκη στην βιβλιοθήκη Umami.Net που θα σας επιτρέψει να πάρετε τα δεδομένα από Umami για πράγματα όπως να δείτε πόσες απόψεις μια σελίδα είχε, τι γεγονότα συνέβη ενός συγκεκριμένου τύπου, φιλτραρισμένο από έναν τόνο των παραμέτρων liek χώρα, πόλη, OS, μέγεθος οθόνης, κλπ. Αυτό είναι ένα πολύ ισχυρό, αλλά αυτή τη στιγμή το [Το Umami API λειτουργεί μόνο μέσω της JavaScript](https://umami.is/docs/api/website-stats). Έτσι, θέλοντας να παίξω με αυτά τα δεδομένα πέρασα την προσπάθεια της δημιουργίας ενός πελάτη C# για αυτό.

Η `UmamiDataService` Η τάξη είναι χωρισμένη σε μωβ μερικές τάξεις (οι μέθοδοι είναι SUPER μακρύ) για παράδειγμα εδώ είναι η `PageViews` μέθοδος.

Μπορείτε να δείτε ότι πολλή από τον κώδικα κατασκευάζει το QueryString από το πέρασε στην τάξη PageViewsRequest (υπάρχουν άλλοι τρόποι για να το κάνετε αυτό, αλλά αυτό, για παράδειγμα χρησιμοποιώντας χαρακτηριστικά ή αντανάκλαση λειτουργεί εδώ).

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
Όπως μπορείτε να δείτε αυτό πραγματικά απλά κατασκευάζει μια συμβολοσειρά ερώτημα. επικυρώνει την πρόσκληση (βλ. [τελευταίο άρθρο](/blog/unittestinglogginginaspnetcore) για κάποιες λεπτομέρειες σχετικά με αυτό) και στη συνέχεια κάνει την κλήση στο Umami API. Λοιπόν, πώς θα το δοκιμάσουμε αυτό;

## Δοκιμή του UmamiDataService

Σε αντίθεση με τη δοκιμή UmamiClient, αποφάσισα να δοκιμάσω το `UmamiDataService` χωρίς χρήση Moq. Αντ' αυτού, δημιούργησα ένα απλό `DelegatingHandler` Τάξη που μου επιτρέπει να ανακρίνω το αίτημα και μετά να απαντήσω. Αυτή είναι μια πολύ απλούστερη προσέγγιση από τη χρήση Moq και μου επιτρέπει να δοκιμάσετε το `UmamiDataService` χωρίς να χρειάζεται να κοροϊδεύουν το `HttpClient`.

Στον παρακάτω κώδικα μπορείτε να δείτε ότι απλά επεκτείνω `DelegatingHandler` και να παρακάμψει το `SendAsync` μέθοδος. Αυτή η μέθοδος μου επιτρέπει να επιθεωρήσω το αίτημα και να επιστρέψω μια απάντηση με βάση το αίτημα.

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

## Ρύθμιση

Για τη δημιουργία του νέου `UmamiDataService` Η χρήση αυτού του χειριστή είναι εξίσου απλή.

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

Θα δεις ότι μόλις έφτιαξα το... `ServiceCollection`, προσθέστε το `FakeLogger<T>` (Πάλι δείτε το [τελευταίο άρθρο για λεπτομέρειες σχετικά με αυτό](/blog/unittestinglogginginaspnetcore) και στη συνέχεια να ρυθμίσετε το `UmamiData` service with the username and password I want to use ( so I can test failure).

Στη συνέχεια, καλώ σε `services.SetupUmamiData(username, password);` που είναι μια μέθοδος επέκτασης που δημιούργησα για τη δημιουργία του `UmamiDataService` με το `UmamiDataDelegatingHandler` και το `AuthService`;

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

Μπορείς να δεις ότι εδώ είναι που κολλάω... `UmamiDataDelegatingHandler` και το `AuthService` έως την `UmamiDataService`. Ο τρόπος με τον οποίο δομείται αυτό είναι ότι `AuthService` 'Ιδιοκτήτες' `HttpClient` και το `UmamiDataService` χρησιμοποιεί το `AuthService` για να κάνει τις κλήσεις προς το Umami API με το `bearer` Μάρκα και `BaseAddress` Είσαι έτοιμος.

## Οι δοκιμές

Πραγματικά αυτό κάνει πραγματικά δοκιμή αυτό είναι πραγματικά απλό. Είναι απλά λίγο ρήμα καθώς ήθελα να δοκιμάσω και την καταγραφή. Το μόνο που κάνει είναι να αναρτάει μέσα από το δικό μου `DelegatingHandler` και προσομοιώνω μια απάντηση με βάση το αίτημα.

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

### Προσομοίωση της Ανταπόκρισης

Για να προσομοιώσω την ανταπόκριση για αυτή τη μέθοδο θα θυμάστε ότι έχω αυτή τη γραμμή στο `UmamiDataDelegatingHandler`:

```csharp
  if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }
```

Το μόνο που κάνει αυτό είναι να τραβήξει πληροφορίες από το ερώτημα και κατασκευάζει μια "ρεαλιστική" απάντηση (βάσει των Ζωντανών Δοκιμών που έχω συντάξει, και πάλι πολύ λίγα έγγραφα σχετικά με αυτό). Θα δείτε ότι ελέγχω για τον αριθμό των ημερών μεταξύ της έναρξης και της ημερομηνίας λήξης και στη συνέχεια θα επιστρέψω μια απάντηση με τον ίδιο αριθμό ημερών.

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

# Συμπέρασμα

Έτσι, αυτό είναι πραγματικά είναι αρκετά απλό να δοκιμάσετε ένα `HttpClient` Αίτημα χωρίς χρήση του Μοκ και νομίζω ότι είναι πολύ πιο καθαρό έτσι. Χάνεις κάποιες από τις εκλεπτύσεις που έγιναν δυνατές στο Μακ, αλλά για απλές εξετάσεις όπως αυτό, νομίζω ότι είναι μια καλή ανταλλαγή.