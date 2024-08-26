# Προσθήκη φόντου αποστολής για μηνύματα ηλεκτρονικού ταχυδρομείου

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-07T08:15</datetime>

Απόδοση Διαλόγων/Συγχρονισμός X-SimpsonsTeam [taokla007]

Στην προηγούμενη ανάρτηση μου, έστειλα λεπτομερή πώς να στείλω email χρησιμοποιώντας το FluentEmail και το SMTP Client. Ωστόσο, ένα θέμα με αυτό είναι η καθυστέρηση στην αποστολή μηνυμάτων ηλεκτρονικού ταχυδρομείου. Οι διακομιστές SMTP τείνουν να είναι αργοί και μπορεί να πάρει λίγο χρόνο για να στείλετε μηνύματα ηλεκτρονικού ταχυδρομείου. Αυτό μπορεί να είναι ενοχλητικό για τους χρήστες και να αισθάνονται σαν ένα logjam στην εφαρμογή σας.

Ένας τρόπος για να πάρετε γύρω από αυτό είναι να στείλετε μηνύματα ηλεκτρονικού ταχυδρομείου στο παρασκήνιο. Με αυτόν τον τρόπο ο χρήστης μπορεί να συνεχίσει να χρησιμοποιεί την εφαρμογή χωρίς να χρειάζεται να περιμένει το email για να στείλει. Αυτό είναι ένα κοινό μοτίβο σε web εφαρμογές και μπορεί να επιτευχθεί με τη χρήση μιας εργασίας υποβάθρου.

[TOC]

## Επιλογές φόντου στο πυρήνα ASP.NET

Στο ASP.NET Core έχετε δύο κύριες επιλογές (εκτός από πιο προηγμένες επιλογές όπως Hangfire / Quartz)

- IHostedService - αυτή η επιλογή σας δίνει τη βασική διαχείριση κύκλου ζωής για τις εργασίες σας στο παρελθόν. Μπορείτε να ξεκινήσετε και να σταματήσετε την υπηρεσία και θα τρέξει στο παρασκήνιο.
- IHostedLifetime - αυτή η επιλογή σας δίνει περισσότερο έλεγχο στον κύκλο ζωής των εργασιών φόντο σας. Μπορείτε επίσης να ξεκινήσετε και να σταματήσετε την υπηρεσία και θα τρέξει στο παρασκήνιο, αλλά έχετε περισσότερο έλεγχο...

Σε αυτό το παράδειγμα θα χρησιμοποιήσω ένα απλό IHostedService για να στείλω email στο παρασκήνιο.

## Πηγαίος κώδικας

Η πλήρης πηγή για αυτό είναι παρακάτω.

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
Εδώ μπορείτε να δείτε ότι θα χειριστούμε την έναρξη της υπηρεσίας και τη δημιουργία ενός νέου BufferBlock για να κρατήσει τα μηνύματα ηλεκτρονικού ταχυδρομείου.

```csharp
public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();
```

Δημιουργήσαμε επίσης μια νέα εργασία για να παραδώσουμε τα μηνύματα ηλεκτρονικού ταχυδρομείου στο παρασκήνιο.
και μια ΑκύρωσηTokenΠηγή για να ακυρώσετε το έργο χαριτωμένα όταν θέλουμε να σταματήσει η υπηρεσία.

Στη συνέχεια, ξεκινάμε το HostedService με StartAsync και παρέχουμε το σημείο εισόδου για άλλες υπηρεσίες για να στείλετε ένα email.

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

Στην τάξη μας Setup πρέπει τώρα να καταχωρήσετε την υπηρεσία με το δοχείο DI και να ξεκινήσετε το HostedService

```csharp
       services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Τώρα μπορούμε να στείλουμε μηνύματα ηλεκτρονικού ταχυδρομείου στο παρασκήνιο καλώντας τη μέθοδο SendEmailAsync στο EmailSenderHostedService.
π.χ., για τη φόρμα επικοινωνίας που κάνουμε.

```csharp
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
```

Στον παραπάνω κώδικα αυτό προσθέτει αυτό το μήνυμα στο `BufferBlock<BaseEmailModel>` _mailMessages και η εργασία φόντου θα το πάρει και θα στείλει το email.

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

Αυτό στη συνέχεια θα βρόχο μέχρι να σταματήσουμε την υπηρεσία και να συνεχίσουμε να παρακολουθούμε το BufferBlock για νέα μηνύματα ηλεκτρονικού ταχυδρομείου για να στείλουμε.