# Μονάδα δοκιμής Umami.Net - Σύνδεση σε πυρήνα ASP.NET

# Εισαγωγή

Είμαι ένας σχετικός νουβ χρησιμοποιώντας Moq (ναι είμαι ενήμερος για τις αντιπαραθέσεις) και προσπαθούσα να δοκιμάσω μια νέα υπηρεσία που προσθέτω στο Umami.Net, UmamiData. Αυτή είναι μια υπηρεσία που μου επιτρέπει να τραβήξει τα δεδομένα από την περίπτωσή μου Umami για να χρησιμοποιήσετε σε πράγματα όπως διαλογή θέσεις με δημοτικότητα κλπ...

[TOC]

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T13:22</datetime>

# Το Πρόβλημα

Προσπαθούσα να προσθέσω ένα απλό τεστ για τη λειτουργία σύνδεσης που πρέπει να χρησιμοποιήσω όταν τραβάω δεδομένα.
Όπως μπορείτε να δείτε είναι μια απλή υπηρεσία που μεταφέρει ένα όνομα χρήστη και τον κωδικό πρόσβασης στο `/api/auth/login` Το τελικό σημείο και παίρνει ένα αποτέλεσμα. Εάν το αποτέλεσμα είναι επιτυχής, αποθηκεύει το σημείο στο `_token` πεδίο και θέτει το `Authorization` κεφαλίδα για το `HttpClient` να χρησιμοποιούνται σε μελλοντικές αιτήσεις.

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

Τώρα ήθελα επίσης να δοκιμάσω ενάντια στον καταγραφέα για να σιγουρευτώ ότι ήταν καταγραφή των σωστών μηνυμάτων. Χρησιμοποιώ το... `Microsoft.Extensions.Logging` Ο χώρος ονομάτων κι εγώ θέλαμε να ελέγξουμε ότι τα σωστά μηνύματα καταγραφής ήταν γραμμένα στον καταγραφέα.

Στο Moq υπάρχει ένα BUNCH των θέσεων γύρω από τη δοκιμή καταγραφής έχουν όλοι αυτή τη βασική μορφή (από τον https://adamstorr.co.uk/blog/mocking-ilogger-with-moq/)

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

HOWEver λόγω των πρόσφατων αλλαγών του Moq (It.IsAanyType είναι τώρα παρωχημένο) και ASP.NET Core αλλαγές σε FormatedLogValues Είχα μια δύσκολη στιγμή να πάρει αυτό για να λειτουργήσει.

Δοκίμασα ένα BUNCH εκδόσεις και παραλλαγές, αλλά πάντα απέτυχε. Οπότε... τα παράτησα.

# Η Λύση

Έτσι διαβάζοντας ένα μάτσο μηνύματα GitHub συνάντησα μια θέση από τον David Fowler (πρώην συνάδελφό μου και τώρα ο Λόρδος του.NET) που έδειξε έναν απλό τρόπο για να δοκιμάσετε την καταγραφή στο ASP.NET Core.
Αυτό χρησιμοποιεί το *Καινούργιο για μένα* `Microsoft.Extensions.Diagnostics.Testing` [συσκευασία](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Testing) που έχει μερικές πραγματικά χρήσιμες επεκτάσεις για τη δοκιμή καταγραφής.

Έτσι, αντί για όλα τα πράγματα Moq μόλις πρόσθεσα το `Microsoft.Extensions.Diagnostics.Testing` συσκευασία και πρόσθεσε τα ακόλουθα στις δοκιμές μου.

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

Θα δείτε ότι αυτό δημιουργεί ServiceCollection μου, προσθέτει το νέο `FakeLogger<T>` και στη συνέχεια να δημιουργήσει το `UmamiData` service with the username and password I want to use ( so I can test failure).

## Οι δοκιμές που χρησιμοποιούν το ψεύτικο λογότυπο

Τότε οι εξετάσεις μου μπορούν να γίνουν:

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

Εκεί που θα δεις, απλά θα καλέσω το... `GetServiceProvider` μέθοδος για να πάρει τον πάροχο υπηρεσιών μου, στη συνέχεια να πάρει το `AuthService` και `ILogger<AuthService>` από τον πάροχο υπηρεσιών.

Γιατί τα έχω κανονίσει όλα αυτά ως... `FakeLogger<T>` Στη συνέχεια, μπορώ να έχω πρόσβαση στο `FakeLogCollector` και `FakeLogRecord` Να πάρω τα αρχεία καταγραφής και να τα ελέγξω.

Τότε μπορώ απλά να ελέγξω τα αρχεία καταγραφής για τα σωστά μηνύματα.

# Συμπέρασμα

Ορίστε λοιπόν, ένας απλός τρόπος για να δοκιμάσετε τα μηνύματα καταγραφής σε δοκιμές μονάδας χωρίς τις ανοησίες των Μοκ.