# Taustan lisääminen sähköpostien lähettämiseksi

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-07T08:15</datetime>

"Käynnistäminen"

Edellisessä viestissäni kerroin yksityiskohtaisesti, kuinka lähettää sähköposteja FluentEmailin ja SMTP-asiakkaan kautta. Yksi ongelma tässä on kuitenkin sähköpostien lähettämisen viivästyminen. SMTP-palvelimet ovat yleensä hitaita ja sähköpostien lähettäminen voi kestää jonkin aikaa. Tämä voi olla ärsyttävää käyttäjille, ja se voi tuntua sovelluksessasi logjamilta.

Yksi tapa kiertää tämä on lähettää taustalle sähköposteja. Näin käyttäjä voi jatkaa sovelluksen käyttöä tarvitsematta odottaa sähköpostin lähettämistä. Tämä on yleinen malli verkkosovelluksissa, ja se voidaan saavuttaa taustatyön avulla.

[TÄYTÄNTÖÖNPANO

## Taustavaihtoehdot ASP.NET-ytimessä

ASP.NET Coressa on kaksi päävaihtoehtoa (kuten Hangfire / Quartz)

- IHostedService - tämä vaihtoehto antaa peruselinkaarenhallinnan taustatehtäviin. Voit aloittaa ja lopettaa palvelun, ja se pyörii taustalla.
- IHostedLifetime - tällä vaihtoehdolla voit hallita taustatehtäväsi elinkaarta. Voit myös aloittaa ja lopettaa palvelun, ja se pyörii taustalla, mutta sinulla on enemmän valvontaa, joka alkaa, pysähtyy, käynnistyy, pysähtyy jne....

Tässä esimerkissä käytän yksinkertaista IHostedServiceä sähköpostien lähettämiseen taustalla.

## Lähdekoodi

Täydellinen lähde tälle on alla.

<details>
<summary>Background Email Service</summary>
```csharp
using System.Threading.Tasks.Dataflow;
using Mostlylucid.Email.Models;

namespace Mostlylucid.Email
{
    public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();

        public async Task SendEmailAsync(BaseEmailModel message)
        {
            await _mailMessages.SendAsync(message);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting background e-mail delivery");
            // Start the background task
            _sendTask = DeliverAsync(cancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping background e-mail delivery");

            // Cancel the token to signal the background task to stop
            await cancellationTokenSource.CancelAsync();

            // Wait until the background task completes or the cancellation token triggers
            await Task.WhenAny(_sendTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }

        private async Task DeliverAsync(CancellationToken token)
        {
            logger.LogInformation("E-mail background delivery started");

            while (!token.IsCancellationRequested)
            {
                BaseEmailModel? message = null;
                try
                {if(_mailMessages.Count == 0) continue;
                    message = await _mailMessages.ReceiveAsync(token);
                    switch (message)
                    {
                        case ContactEmailModel contactEmailModel:
                            await emailService.SendContactEmail(contactEmailModel);
                            break;
                        case CommentEmailModel commentEmailModel:
                            await emailService.SendCommentEmail(commentEmailModel);
                            break;
                    }
                    logger.LogInformation("Email from {SenderEmail} sent", message.SenderEmail);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception exc)
                {
                    logger.LogError(exc, "Couldn't send an e-mail from {SenderEmail}", message?.SenderEmail);
                    await Task.Delay(1000, token); // Delay and respect the cancellation token
                    if (message != null)
                    {
                        await _mailMessages.SendAsync(message, token);
                    }
                }
            }

            logger.LogInformation("E-mail background delivery stopped");
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }
    }
}
```

</details>
Tässä näet, että hoidamme palvelun alun ja perustamme uuden BufferBlockin pitämään sähköpostit.

```csharp
public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();
```

Perustimme myös uuden tehtävän, jolla toimitamme sähköpostit taustalla.
ja PeruutusTokenLähde peruuttaa tehtävän sulavasti, kun haluamme lopettaa palvelun.

Aloitamme sen jälkeen StartAsyncillä HostedService -palvelun ja lähetämme sähköpostia muille palveluille.

```csharp
 public async Task SendEmailAsync(BaseEmailModel message)
        {
            await _mailMessages.SendAsync(message);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting background e-mail delivery");
            // Start the background task
            _sendTask = DeliverAsync(cancellationTokenSource.Token);
            return Task.CompletedTask;
        }
```

Setup-kurssilla meidän täytyy nyt rekisteröidä palvelu DI-konttiin ja aloittaa HostedService

```csharp
       services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Nyt voimme lähettää sähköpostia taustalla soittamalla SenderEmailAsync -menetelmään sähköpostitseSenderSenderHostedService -palvelussa.
Esimerkiksi yhteydenottolomaketta varten teemme näin.

```csharp
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
```

Yllä olevassa koodissa tämä lisää tämän viestin `BufferBlock<BaseEmailModel>` _Postiviestit ja taustatehtävä poimivat sen ja lähettävät sähköpostin.

```csharp
   private async Task DeliverAsync(CancellationToken token)
        {
          ...

            while (!token.IsCancellationRequested)
            {
                BaseEmailModel? message = null;
                try
                {if(_mailMessages.Count == 0) continue;
                    message = await _mailMessages.ReceiveAsync(token);
                    switch (message)
                    {
                        case ContactEmailModel contactEmailModel:
                            await emailService.SendContactEmail(contactEmailModel);
                            break;
                        case CommentEmailModel commentEmailModel:
                            await emailService.SendCommentEmail(commentEmailModel);
                            break;
                    }
                    logger.LogInformation("Email from {SenderEmail} sent", message.SenderEmail);
           ...
            }

            logger.LogInformation("E-mail background delivery stopped");
        }
```

Tämä sitten silmukka kunnes lopetamme palvelun ja jatkamme BufferBlockin seuraamista uusien sähköpostien lähettämiseksi.