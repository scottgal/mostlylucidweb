# Μονάδα δοκιμής Umami.Net - Δοκιμή UmamiClient

# Εισαγωγή

Τώρα έχω το... [Πακέτο Umami.Net](https://www.nuget.org/packages/Umami.Net/) Εκεί έξω θέλω φυσικά να διασφαλίσω ότι όλα θα δουλέψουν όπως αναμενόταν. Για να το κάνουμε αυτό ο καλύτερος τρόπος είναι να δοκιμάσουμε κάπως ολοκληρωτικά όλες τις μεθόδους και τις τάξεις. Εδώ είναι που έρχεται η δοκιμή μονάδας.
Σημείωση: Αυτό δεν είναι μια "τέλεια προσέγγιση" θέση τύπου, είναι ακριβώς το πώς το έχω κάνει αυτή τη στιγμή. Στην πραγματικότητα δεν χρειάζεται πραγματικά να Mock το `IHttpMessageHandler` Εδώ ένα μπορείτε να επιτεθείτε σε ένα DelegatingMessageHandler σε ένα κανονικό HttpClient για να το κάνετε αυτό. Απλά ήθελα να σου δείξω πώς μπορείς να το κάνεις με μια Μοκ.

[TOC]

<!--category-- xUnit, Umami -->
<datetime class="hidden">2024-09-01T17:22</datetime>

# Δοκιμή μονάδας

Η δοκιμή μονάδας αναφέρεται στη διαδικασία δοκιμής μεμονωμένων μονάδων κώδικα για να εξασφαλιστεί ότι λειτουργούν όπως αναμένεται. Αυτό γίνεται με τη συγγραφή δοκιμών που αποκαλούν τις μεθόδους και τις τάξεις με ελεγχόμενο τρόπο και στη συνέχεια ο έλεγχος της εξόδου είναι όπως αναμένεται.

Για ένα πακέτο όπως Umami.Net αυτό είναι τόσο δύσκολο, καθώς και οι δύο αποκαλούν ένα απομακρυσμένο πελάτη πάνω `HttpClient` και έχει ένα `IHostedService` χρησιμοποιεί για να καταστήσει την αποστολή νέων δεδομένων γεγονότων όσο το δυνατόν πιο απρόσκοπτη.

## Δοκιμή UmamiClient

Το κύριο μέρος της δοκιμής `HttpClient` Η βασική βιβλιοθήκη αποφεύγει την πραγματική κλήση "HttpClient." Αυτό γίνεται με τη δημιουργία ενός `HttpClient` που χρησιμοποιεί ένα `HttpMessageHandler` που επιστρέφει μια γνωστή απάντηση. Αυτό γίνεται με τη δημιουργία ενός `HttpClient` με `HttpMessageHandler` που επιστρέφει μια γνωστή απάντηση? Σε αυτή την περίπτωση απλά αντηχώ πίσω την απάντηση εισόδου και ελέγξτε ότι δεν έχει παραμορφωθεί από το `UmamiClient`.

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

Όπως θα δείτε αυτό δημιουργεί ένα `Mock<HttpMessageHandler>` Στη συνέχεια, περνάω στο `UmamiClient`.
Σε αυτόν τον κώδικα θα το συνδέσω με το δικό μας `IServiceCollection` Μέθοδος ρύθμισης. Αυτό προσθέτει όλες τις υπηρεσίες που απαιτούνται από την `UmamiClient` συμπεριλαμβανομένου του νέου μας `HttpMessageHandler` και στη συνέχεια επιστρέφει το `IServiceCollection` για χρήση στις δοκιμές.

```csharp
    public static IServiceCollection SetupServiceCollection(string webSiteId = Consts.WebSiteId,
        string umamiPath = Consts.UmamiPath, HttpMessageHandler? handler = null)
    {
        var services = new ServiceCollection();
        var umamiClientSettings = new UmamiClientSettings
        {
            WebsiteId = webSiteId,
            UmamiPath = umamiPath
        };
        services.AddSingleton(umamiClientSettings);
        services.AddScoped<PayloadService>();
        services.AddLogging(x => x.AddConsole());
        // Mocking HttpMessageHandler with Moq
        var mockHandler = handler ?? EchoMockHandler.Create();
        services.AddHttpClient<UmamiClient>((serviceProvider, client) =>
        {
            var umamiSettings = serviceProvider.GetRequiredService<UmamiClientSettings>();
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
        }).ConfigurePrimaryHttpMessageHandler(() => mockHandler);
        return services;
    }
```

Για να χρησιμοποιήσετε αυτό και να κάνετε την ένεση στο `UmamiClient` Στη συνέχεια, χρησιμοποιώ αυτές τις υπηρεσίες στο `UmamiClient` Στήσιμο.

```csharp
    public static UmamiClient GetUmamiClient(IServiceCollection? serviceCollection = null,
        HttpContextAccessor? contextAccessor = null)
    {
        serviceCollection ??= SetupServiceCollection();
        SetupUmamiClient(serviceCollection, contextAccessor);
        if (serviceCollection == null) throw new NullReferenceException(nameof(serviceCollection));
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider.GetRequiredService<UmamiClient>();
    }
```

Θα δείτε ότι έχω ένα μάτσο εναλλακτικών προαιρετικών παραμέτρων που μου επιτρέπουν να εισάγω διαφορετικές επιλογές για διαφορετικούς τύπους δοκιμών.

### Οι δοκιμές

Έτσι τώρα έχω όλα αυτά στη θέση τους Μπορώ τώρα να αρχίσω να γράφω δοκιμές για το `UmamiClient` μέθοδοι.

#### Αποστολή

Αυτό που σημαίνει όλη αυτή η ρύθμιση είναι ότι οι δοκιμές μας μπορούν να είναι αρκετά απλές

```csharp
public class UmamiClient_SendTests
{
    [Fact]
    public async Task Send_Wrong_Type()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        await Assert.ThrowsAsync<ArgumentException>(async () => await umamiClient.Send(type: "boop"));
    }

    [Fact]
    public async Task Send_Empty_Success()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        var response = await umamiClient.Send();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

Εδώ μπορείτε να δείτε την απλούστερη περίπτωση δοκιμής, απλά εξασφαλίζει ότι `UmamiClient` Μπορεί να στείλει ένα μήνυμα και να λάβει μια απάντηση? `type` Είναι λάθος. Αυτό είναι ένα συχνά παραβλεφθεί μέρος των δοκιμών, εξασφαλίζοντας ότι ο κώδικας αποτυγχάνει όπως αναμένεται.

#### Προβολή σελίδας

Για να δοκιμάσουμε τη μέθοδο μας, μπορούμε να κάνουμε κάτι παρόμοιο. Στον παρακάτω κώδικα χρησιμοποιώ το δικό μου `EchoHttpHandler` Απλά να αναλογιστείς την απάντηση που μου έστειλες και να διασφαλίσεις ότι θα στείλει πίσω αυτό που περιμένω.

```csharp
    [Fact]
    public async Task TrackPageView_WithNoUrl()
    {
        var defaultUrl = "/testpath";
        var contextAccessor = SetupExtensions.SetupHttpContextAccessor(path: "/testpath");
        var umamiClient = SetupExtensions.GetUmamiClient(contextAccessor: contextAccessor);
        var response = await umamiClient.TrackPageView();

        var content = await response.Content.ReadFromJsonAsync<EchoedRequest>();
        Assert.NotNull(response);
        Assert.NotNull(content);
        Assert.Equal(content.Payload.Url, defaultUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
```

### HttpContextAccessor

Αυτό χρησιμοποιεί το `HttpContextAccessor` να ρυθμίσετε το μονοπάτι `/testpath` και στη συνέχεια ελέγχει ότι η `UmamiClient` Στέλνει αυτό σωστά.

```csharp
    public static HttpContextAccessor SetupHttpContextAccessor(string host = Consts.Host,
        string path = Consts.Path, string ip = Consts.Ip, string userAgent = Consts.UserAgent,
        string referer = Consts.Referer)
    {
        HttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        httpContext.Request.Path = new PathString(path);
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        httpContext.Request.Headers.UserAgent = userAgent;
        httpContext.Request.Headers.Referer = referer;

        var context = new HttpContextAccessor { HttpContext = httpContext };
        return context;
    }

```

Αυτό είναι σημαντικό για μας Umami κώδικα πελάτη, καθώς πολλά από τα δεδομένα που αποστέλλονται από κάθε αίτηση είναι στην πραγματικότητα δυναμικά παράγονται από το `HttpContext` αντικείμενο. Οπότε δεν μπορούμε να στείλουμε τίποτα. `await umamiClient.TrackPageView();` Καλέστε και θα εξακολουθούν να στέλνουν τα σωστά δεδομένα με την εξαγωγή του URL από το `HttpContext`.

Όπως θα δούμε αργότερα είναι επίσης σημαντικό το δέος στέλνει αντικείμενα όπως το `UserAgent` και `IPAddress` όπως αυτά χρησιμοποιούνται από τον εξυπηρετητή Umami για να παρακολουθείτε τα δεδομένα και τις "παρακολουθήστε" απόψεις των χρηστών χωρίς να χρησιμοποιείτε cookies.

Για να έχουμε αυτό το προβλέψιμο, ορίζουμε ένα μάτσο Consts στο `Consts` Μαθήματα. Έτσι μπορούμε να δοκιμάσουμε ενάντια σε προβλέψιμες απαντήσεις και αιτήματα.

```csharp
public class Consts
{
    public const string UmamiPath = "https://example.com";
    public const string WebSiteId = "B41A9964-FD33-4108-B6EC-9A6B68150763";
    public const string Host = "example.com";
    public const string Path = "/example";
    public const string Ip = "127.0.0.1";
    public const string UserAgent = "Test User Agent";
    public const string Referer = "Test Referer";
    public const string DefaultUrl = "/testpath";
    public const string DefaultTitle = "Example Page";
    public const string DefaultName = "RSS";
    public const string DefaultType = "event";

    public const string Email = "test@test.com";

    public const string UserId = "11224456";
    
    public const string UserName = "Test User";
    
    public const string SessionId = "B41A9964-FD33-4108-B6EC-9A6B68150763";
}
```

## Περαιτέρω δοκιμές

Αυτό είναι μόνο η αρχή της στρατηγικής δοκιμών μας για Umami.Net, πρέπει ακόμα να δοκιμάσουμε το `IHostedService` και δοκιμή με βάση τα πραγματικά δεδομένα Umami παράγει (το οποίο δεν είναι τεκμηριωμένο οπουδήποτε, αλλά περιέχει ένα JWT μάρκα με κάποια χρήσιμα δεδομένα.)

```json
{
  "alg": "HS256",
  "typ": "JWT"
}{
  "id": "b9836672-feee-55c5-985a-a5a23d4a23ad",
  "websiteId": "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
  "hostname": "example.com",
  "browser": "chrome",
  "os": "Windows 10",
  "device": "desktop",
  "screen": "1920x1080",
  "language": "en-US",
  "country": "GB",
  "subdivision1": null,
  "subdivision2": null,
  "city": null,
  "createdAt": "2024-09-01T09:26:14.418Z",
  "visitId": "e7a6542f-671a-5573-ab32-45244474da47",
  "iat": 1725182817
}2|Y*: �(N%-ޘ^1>@V
```

Οπότε θα θέλουμε να δοκιμάσουμε γι' αυτό, να προσομοιώσουμε το σύμβολο και πιθανόν να επιστρέψουμε τα δεδομένα για κάθε επίσκεψη (όπως θα θυμάστε αυτό είναι φτιαγμένο από ένα `uuid(websiteId,ipaddress, useragent)`).

# Συμπέρασμα

Αυτό είναι μόνο η αρχή της δοκιμής του πακέτου Umami.Net, υπάρχουν πολλά περισσότερα να κάνουμε, αλλά αυτό είναι μια καλή αρχή. Θα προσθέσω κι άλλα τεστ καθώς θα πηγαίνω και σίγουρα θα τα βελτιώνω.