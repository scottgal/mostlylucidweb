# Μονάδα δοκιμής Umami.Net - Δοκιμή UmamiBackgroundSender

# Εισαγωγή

Στο προηγούμενο άρθρο, συζητήσαμε πώς να δοκιμάσουμε το `UmamiClient` χρησιμοποιώντας xUnit και Moq. Σε αυτό το άρθρο, θα συζητήσουμε πώς να δοκιμάσετε το `UmamiBackgroundSender` Μαθήματα. Η `UmamiBackgroundSender` είναι λίγο διαφορετικό από `UmamiClient` όπως χρησιμοποιεί `IHostedService` να συνεχίσει να τρέχει στο παρασκήνιο και να στείλει αιτήματα μέσω `UmamiClient` εντελώς έξω από το κύριο νήμα εκτέλεσης (ώστε να μην μπλοκάρει την εκτέλεση).

Ως συνήθως μπορείς να δεις όλο τον πηγαίο κώδικα γι' αυτό στο GitHub μου [Ορίστε.](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net.Test/UmamiBackgroundSenderTests.cs).

[TOC]

<!--category-- xUnit, Umami, IHostedService, Moq -->
<datetime class="hidden">2024-09-03T09:00</datetime>

## `UmamiBackgroundSender`

Η πραγματική δομή της `UmamiBackgroundSender` Είναι πολύ απλό. Είναι μια υπηρεσία που φιλοξενείται που στέλνει αιτήματα στον εξυπηρετητή Umami μόλις εντοπίσει ένα νέο αίτημα. Η βασική δομή `UmamiBackgroundSender` Η τάξη παρουσιάζεται παρακάτω:

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

Όπως μπορείτε να δείτε αυτό είναι απλά ένα κλασικό `IHostedService` προστίθεται στη συλλογή υπηρεσιών μας στο ASP.NET χρησιμοποιώντας το `services.AddHostedService<UmamiBackgroundSender>()` μέθοδος. Αυτό ξεκινάει από το... `StartAsync` μέθοδος κατά την έναρξη της εφαρμογής.
Το βλέμμα μέσα στο `SendRequest` μέθοδος είναι όπου η μαγεία συμβαίνει. Εδώ διαβάζουμε από το κανάλι και στέλνουμε το αίτημα στον σέρβερ Umami.

Αυτό αποκλείει τις πραγματικές μεθόδους για την αποστολή των αιτήσεων (shown παρακάτω).

```csharp
public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)

public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)   

        public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null)

public async Task Track(string eventName, UmamiEventData? eventData = null)

public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Το μόνο που κάνουν είναι να πακετάρουν το αίτημα. `SendBackgroundPayload` Ηχογραφήστε και στείλτε το στο κανάλι.

Οι φωλιές μας λαμβάνουν βρόχο σε `SendRequest` θα συνεχίσει να διαβάζει από το κανάλι μέχρι να κλείσει. Εδώ θα εστιάσουμε τις προσπάθειές μας στις δοκιμές.

```csharp
  while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
            }
        }    

```

Η υπηρεσία υποβάθρου έχει κάποια σημασιολογία που της επιτρέπουν να πυροβολήσει το μήνυμα μόλις φτάσει.
Ωστόσο, αυτό εγείρει ένα πρόβλημα? Αν δεν πάρουμε μια επιστροφή αξία από το `Send` Πώς μπορούμε να δοκιμάσουμε αυτό κάνει πραγματικά τίποτα;

## Δοκιμή `UmamiBackgroundSender`

Οπότε το ερώτημα είναι πώς δοκιμάζουμε αυτή την υπηρεσία πέντεn δεν υπάρχει απάντηση για να δοκιμάσουμε πραγματικά ενάντια;

Η απάντηση είναι να ενέσετε ένα `HttpMessageHandler` Στον κοροϊδεμένο HttpClient που στέλνουμε στο UmamiClient μας. Αυτό θα μας επιτρέψει να αναχαιτίσουμε το αίτημα και να ελέγξουμε το περιεχόμενό του.

### EchoMockHttpMessageHandler

Θα θυμάστε από το προηγούμενο άρθρο που φτιάξαμε ένα ψεύτικο HttpMessageHandler. Αυτό ζει μέσα στο `EchoMockHandler` στατική τάξη:

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

Μπορείτε να δείτε εδώ χρησιμοποιούμε Mock για να δημιουργήσει ένα `SendAsync` μέθοδος η οποία θα επιστρέψει μια απάντηση με βάση το αίτημα (σε HttpClient όλα τα αιτήματα async γίνονται μέσω `SendAsync`).

Βλέπεις, θα φτιάξουμε πρώτα το Mock.

```csharp
     var mockHandler = new Mock<HttpMessageHandler>();
```

Στη συνέχεια, χρησιμοποιούμε τη μαγεία του `Protected` για τη δημιουργία του `SendAsync` μέθοδος. Αυτό συμβαίνει επειδή... `SendAsync` δεν είναι συνήθως προσβάσιμο στο κοινό API του `HttpMessageHandler`.

```csharp
public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
        }
        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
```

Στη συνέχεια, χρησιμοποιούμε μόνο το catch-all `ItExpr.IsAny` να ταιριάζει με οποιοδήποτε αίτημα και να επιστρέψει την απάντηση από το `responseFunc` Περνάμε μέσα.

## Μέθοδοι δοκιμής.

Μέσα στο `UmamiBackgroundSender_Tests` Έχουμε έναν κοινό τρόπο να ορίσουμε όλες τις μεθόδους δοκιμών.

### Ρύθμιση

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

Μόλις το ορίσουμε αυτό, πρέπει να διαχειριστούμε το `IHostedService` διάρκεια ζωής στη μέθοδο δοκιμής:

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

Μπορείς να δεις ότι περνάμε στον χειριστή μας. `GetServices` Μέθοδος ρύθμισης:

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

Εδώ περνάμε από τον χειριστή μας στις υπηρεσίες μας για να το συνδέσουμε με το `UmamiClient` Στήσιμο.

Στη συνέχεια, προσθέτουμε το `UmamiBackgroundSender` στη συλλογή υπηρεσιών και να πάρει το `IHostedService` από τον πάροχο υπηρεσιών. Τότε επέστρεψε αυτό στην τάξη δοκιμών για να το χρησιμοποιήσεις.

#### Hosted Service Lifetime

Τώρα που τα έχουμε φτιάξει όλα αυτά, μπορούμε απλά... `StartAsync` η Hosted Service, χρησιμοποιήστε το στη συνέχεια περιμένετε μέχρι να σταματήσει:

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

Αυτό θα ξεκινήσει την υπηρεσία υποδοχής, να στείλει το αίτημα, να περιμένει για την απάντηση και στη συνέχεια να σταματήσει την υπηρεσία.

### Χειριστής μηνύματος

Ας ξεκινήσουμε πρώτα με τη δημιουργία του `EchoMockHandler` και το `TaskCompletionSource` το οποίο θα σηματοδοτήσει την ολοκλήρωση της δοκιμής. Αυτό είναι σημαντικό για την επιστροφή του πλαισίου στο κύριο νήμα δοκιμής έτσι ώστε να μπορούμε να συλλάβουμε σωστά τις αποτυχίες και τα timeouts.

Η ` async (message, token) => {}` Είναι η λειτουργία που περνάμε στον κομψό χειριστή μας που αναφέραμε παραπάνω. Εδώ μπορούμε να ελέγξουμε το αίτημα και να επιστρέψουμε μια απάντηση (η οποία σε αυτή την περίπτωση δεν κάνουμε τίποτα με).

Η δική μας `EchoMockHandler.ResponseHandler` είναι μια μέθοδος βοηθός που θα επιστρέψει το σώμα αίτησης πίσω στη μέθοδο μας, αυτό μας επιτρέπει να επιβεβαιώσουμε το μήνυμα που διέρχεται από το `UmamiClient` έως την `HttpClient` Σωστά.

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

Στη συνέχεια, αρπάζουμε αυτή την απάντηση και την απενεργοποιούμε σε ένα `EchoedRequest` αντικείμενο. Αυτό είναι ένα απλό αντικείμενο που αντιπροσωπεύει το αίτημα που στείλαμε στον διακομιστή.

```csharp
public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}
```

Βλέπετε αυτό ενσαρκώνει το `Type` και `Payload` του αιτήματος. Αυτό είναι που θα ελέγξουμε στο τεστ μας.

```csharp
      Assert.Contains("api/send", message.RequestUri.ToString());
      Assert.NotNull(jsonContent);
      Assert.Equal(page, jsonContent.Payload.Url);
      Assert.Equal(title, jsonContent.Payload.Title);
```

Αυτό που είναι κρίσιμο εδώ είναι το πώς αντιμετωπίζουμε τις αποτυχημένες δοκιμές, καθώς δεν είμαστε στο κύριο πλαίσιο του νήματος εδώ πρέπει να χρησιμοποιήσουμε `TaskCompletionSource` να σηματοδοτήσει πίσω στο κύριο νήμα ότι η δοκιμή απέτυχε.

```csharp
     catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
```

Αυτό θα καθορίσει την εξαίρεση για την `TaskCompletionSource` και να επιστρέψει ένα σφάλμα 500 στη δοκιμή.

# Συμπέρασμα

Έτσι, αυτή είναι η πρώτη από τις πιο λεπτομερείς θέσεις μου, `IHostedService` Το εγγυάται αυτό, καθώς είναι μάλλον περίπλοκο να το δοκιμάσεις όταν όπως εδώ δεν επιστρέφει αξία στον καλούντα.