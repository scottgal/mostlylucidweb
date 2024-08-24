# Aggiunta di sfondo per l'invio di email

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-07T08:15</datetime>

##Introduzione

Nel mio post precedente ho spiegato come inviare email utilizzando FluentEmail e il client SMTP. Tuttavia un problema con questo è il ritardo nell'invio di email. I server SMTP tendono ad essere lenti e possono richiedere un po' di tempo per inviare email. Questo può essere fastidioso per gli utenti e sentirsi come un logjam nella vostra applicazione.

Un modo per aggirare questo è quello di inviare e-mail in background. In questo modo l'utente può continuare a utilizzare l'applicazione senza dover attendere che l'email da inviare. Questo è un modello comune nelle applicazioni web e può essere raggiunto utilizzando un lavoro di background.

[TOC]

## Opzioni di sfondo in ASP.NET Core

In ASP.NET Core hai due opzioni principali (oltre a opzioni più avanzate come Hangfire / Quartz)

- IHostedService - questa opzione ti offre la gestione del ciclo di vita di base per le tue attività di background. È possibile avviare e interrompere il servizio e verrà eseguito in background.
- IHostedLifetime - questa opzione ti dà più controllo sul ciclo di vita delle tue attività di sfondo. È anche possibile avviare e fermare il servizio e verrà eseguito in background, ma si dispone di più controllo aroudn inizio, fermarsi, avviato, fermato ecc...

In questo esempio userò un semplice IHostedService per inviare email in background.

## Codice sorgente

La fonte completa per questo è di seguito.

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
Qui potete vedere che gestiamo l'inizio del servizio e la creazione di un nuovo BufferBlock per tenere le email.

```csharp
public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();
```

Abbiamo anche creato un nuovo Task per consegnare le email in background.
e una CancellazioneTokenSource per annullare il compito con grazia quando vogliamo fermare il servizio.

Abbiamo quindi avviare il servizio Hosted con StartAsync e fornire il punto di ingresso per altri servizi per inviare una e-mail.

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

Nella nostra classe di configurazione abbiamo ora bisogno di registrare il servizio con il contenitore DI e avviare il HostedService

```csharp
       services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Ora possiamo inviare email in background chiamando il metodo SendEmailAsync su EmailSenderHostedService.
Per esempio, per il modulo di contatto che facciamo.

```csharp
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
```

Nel codice qui sopra questo aggiunge questo messaggio al nostro `BufferBlock<BaseEmailModel>` _mailMessaggi e l'attività di sfondo lo raccoglieranno e invieranno l'email.

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

Questo loop quindi fino a quando non fermiamo il servizio e continuiamo a monitorare il BufferBlock per nuove email da inviare.