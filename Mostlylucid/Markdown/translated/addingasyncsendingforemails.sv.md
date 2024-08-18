# Lägga till bakgrundsutskick för e- post

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-07T08:15</datetime>

#Introduktion

I mitt tidigare inlägg beskrev jag hur man skickar e-post med FluentEmail och SMTP Client. Men ett problem med detta är förseningen i att skicka e-post. SMTP-servrar tenderar att vara långsamma och kan ta ett tag att skicka e-post. Detta kan vara irriterande för användare och kännas som en logjam i din ansökan.

Ett sätt att komma runt detta är att skicka e-post i bakgrunden. På så sätt kan användaren fortsätta att använda programmet utan att behöva vänta på att e-postmeddelandet ska skickas. Detta är ett vanligt mönster i webbapplikationer och kan uppnås med hjälp av ett bakgrundsjobb.

[TOC]

## Bakgrundsalternativ i ASP.NET Core

I ASP.NET Core har du två huvudalternativ (förutom mer avancerade alternativ som Hangfire / Quartz)

- IHostedService - detta alternativ ger dig grundläggande livscykelhantering för dina bakgrundsuppgifter. Du kan starta och stoppa tjänsten och den kommer att köras i bakgrunden.
- IHostedLifetime - detta alternativ ger dig mer kontroll över livscykeln för dina bakgrundsuppgifter. Du kan också starta och stoppa tjänsten och det kommer att köras i bakgrunden men du har mer kontroll aroudn start, stoppa, starta, stoppa etc...

I det här exemplet kommer jag att använda en enkel IHostedService för att skicka e-post i bakgrunden.

## Källkod

Den fullständiga källan för detta är nedan.

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
Här kan du se att vi hanterar start av tjänsten och sätta upp en ny BufferBlock för att hålla e-post.

```csharp
public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();
```

Vi har också satt upp en ny uppgift för att leverera e-posten i bakgrunden.
och en AnnulleringTokenSource att avbryta uppgiften graciöst när vi vill stoppa tjänsten.

Vi startar sedan HostedService med StartAsync och tillhandahåller ingångspunkten för andra tjänster att skicka ett e-postmeddelande.

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

I vår Setup klass måste vi nu registrera tjänsten med DI container och starta HostedService

```csharp
       services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Nu kan vi skicka e-post i bakgrunden genom att ringa SendEmailAsync-metoden på EmailSenderHostedService.
t.ex. för kontaktformuläret vi gör detta.

```csharp
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
```

I koden ovan lägger detta meddelande till vår `BufferBlock<BaseEmailModel>` _mailMessages och bakgrundsuppgiften kommer att plocka upp den och skicka e-postmeddelandet.

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

Detta kommer sedan loop tills vi stoppar tjänsten och fortsätter att övervaka BufferBlock för nya e-postmeddelanden att skicka.