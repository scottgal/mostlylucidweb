# Achtergrondverzenden voor e-mails toevoegen

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-07T08:15</datetime>

Introductie

In mijn vorige bericht heb ik gedetailleerd hoe e-mails te versturen met behulp van FluentEmail en de SMTP Client. Echter een probleem met dit is de vertraging in het verzenden van e-mails. SMTP servers hebben de neiging om langzaam en kan een tijdje duren om e-mails te versturen. Dit kan vervelend zijn voor gebruikers en voelt als een logjam in uw toepassing.

Een manier om dit te omzeilen is om e-mails op de achtergrond te versturen. Op deze manier kan de gebruiker doorgaan met het gebruik van de applicatie zonder te hoeven wachten tot de e-mail te versturen. Dit is een veel voorkomend patroon in webapplicaties en kan worden bereikt met behulp van een achtergrondtaak.

[TOC]

## Achtergrondopties in ASP.NET Core

In ASP.NET Core heb je twee hoofdopties (naast meer geavanceerde opties zoals Hangfire / Quartz)

- IHostedService - deze optie geeft u basis lifecycle management voor uw achtergrondtaken. U kunt beginnen en stoppen met de service en het zal draaien op de achtergrond.
- IHostedLifetime - deze optie geeft u meer controle over de levenscyclus van uw achtergrondtaken. U kunt ook starten en stoppen van de service en het zal draaien op de achtergrond, maar je hebt meer controle aroudn starten, stoppen, gestart, gestopt enz...

In dit voorbeeld zal ik een eenvoudige IHostedService gebruiken om e-mails op de achtergrond te versturen.

## Broncode

De volledige bron hiervoor is hieronder.

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
Hier kunt u zien dat wij het starten van de dienst en het opzetten van een nieuwe BufferBlock te houden van de e-mails.

```csharp
public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();
```

We hebben ook een nieuwe taak opgezet om de e-mails op de achtergrond af te leveren.
en een AnnuleringTokenBron om de taak sierlijk te annuleren wanneer we willen stoppen met de service.

Vervolgens starten we de HostedService op met StartAsync en bieden we het ingangspunt voor andere diensten om een e-mail te sturen.

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

In onze Setup klasse moeten we nu de service registreren bij de DI container en de HostedService starten

```csharp
       services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Nu kunnen we e-mails op de achtergrond versturen door de SendEmailAsync methode te bellen op de EmailSenderHostedService.
b.v. voor het contactformulier doen we dit.

```csharp
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
```

In de bovenstaande code voegt dit dit bericht toe aan onze `BufferBlock<BaseEmailModel>` _mailBerichten en de achtergrondtaak zal het ophalen en versturen van de e-mail.

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

Dit zal dan lus totdat we stoppen met de service en blijven de BufferBlock te controleren voor nieuwe e-mails te versturen.