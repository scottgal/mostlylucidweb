# Hintergrund hinzufügen Senden nach E-Mails

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-07T08:15</datetime>

#Einführung

In meinem vorherigen Beitrag habe ich detailliert, wie E-Mails mit FluentEmail und dem SMTP Client zu senden. Allerdings ist ein Problem mit diesem ist die Verzögerung beim Senden von E-Mails. SMTP-Server neigen dazu, langsam zu sein und kann eine Weile dauern, um E-Mails zu senden.

Eine Möglichkeit, dies zu umgehen, ist E-Mails im Hintergrund zu senden. So kann der Benutzer die Anwendung weiter verwenden, ohne warten zu müssen, bis die E-Mail gesendet wird. Dies ist ein gängiges Muster in Web-Anwendungen und kann mit einem Hintergrundjob erreicht werden.

[TOC]

## Hintergrundoptionen im ASP.NET Core

In ASP.NET Core haben Sie zwei Hauptoptionen (neben erweiterten Optionen wie Hangfire / Quartz)

- IHostedService - diese Option gibt Ihnen grundlegendes Lebenszyklusmanagement für Ihre Hintergrundaufgaben. Sie können den Dienst starten und stoppen und er wird im Hintergrund ausgeführt.
- IHostedLifetime - diese Option gibt Ihnen mehr Kontrolle über den Lebenszyklus Ihrer Hintergrundaufgaben. Sie können auch starten und stoppen Sie den Service und es wird im Hintergrund laufen, aber Sie haben mehr Kontrolle aroudn Starten, Stoppen, Starten, Stoppen etc...

In diesem Beispiel verwende ich einen einfachen IHostedService, um E-Mails im Hintergrund zu senden.

## Quellencode

Die vollständige Quelle dafür ist unten.

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
Hier sehen Sie, dass wir mit dem Start des Dienstes umgehen und einen neuen BufferBlock einrichten, um die E-Mails zu halten.

```csharp
public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();
```

Wir haben auch eine neue Aufgabe eingerichtet, um die E-Mails im Hintergrund zu liefern.
und eine CancelTokenSource, um die Aufgabe anmutig zu stornieren, wenn wir den Service stoppen wollen.

Dann starten wir den HostedService mit StartAsync und stellen den Einstiegspunkt für andere Dienste zur Verfügung, um eine E-Mail zu senden.

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

In unserer Setup-Klasse müssen wir jetzt den Service mit dem DI-Container registrieren und den HostedService starten

```csharp
       services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Jetzt können wir E-Mails im Hintergrund senden, indem wir die SendEmailAsync-Methode auf dem EmailSenderHostedService aufrufen.
z.B. für das Kontaktformular tun wir dies.

```csharp
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
```

Im obigen Code fügt dies diese Botschaft zu unserer`BufferBlock<BaseEmailModel>` _mailNachrichten und die Hintergrundaufgabe werden sie abholen und die E-Mail senden.

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

Dies wird dann loop, bis wir den Service stoppen und weiterhin die BufferBlock für neue E-Mails zu senden überwachen.