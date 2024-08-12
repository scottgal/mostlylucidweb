# Ajout de l'arrière-plan Envoi d'e-mails

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-07T08:15</datetime>

##Introduction

Dans mon post précédent, j'ai détaillé comment envoyer des emails en utilisant FluentEmail et le client SMTP. Cependant, un problème avec cela est le retard dans l'envoi des emails. Les serveurs SMTP ont tendance à être lents et peuvent prendre un certain temps pour envoyer des emails. Cela peut être ennuyeux pour les utilisateurs et se sentir comme un logjam dans votre application.

Une façon de contourner cela est d'envoyer des e-mails en arrière-plan. De cette façon, l'utilisateur peut continuer à utiliser l'application sans avoir à attendre que l'e-mail à envoyer. Il s'agit d'un modèle commun dans les applications Web et peut être réalisé à l'aide d'un travail de fond.

[TOC]

## Options de fond dans ASP.NET Core

Dans ASP.NET Core, vous avez deux options principales (en plus d'options plus avancées comme Hangfire / Quartz)

- IHostedService - cette option vous donne une gestion de base du cycle de vie pour vos tâches de fond. Vous pouvez démarrer et arrêter le service et il fonctionnera en arrière-plan.
- IHostedLifetime - cette option vous donne plus de contrôle sur le cycle de vie de vos tâches d'arrière-plan. Vous pouvez également démarrer et arrêter le service et il fonctionnera en arrière-plan, mais vous avez plus de contrôle sur le démarrage, l'arrêt, le démarrage, l'arrêt etc...

Dans cet exemple, je vais utiliser un simple IHostedService pour envoyer des courriels en arrière-plan.

## Code source

La source complète pour cela est ci-dessous.

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
Ici vous pouvez voir que nous gérons le démarrage du service et la configuration d'un nouveau BufferBlock pour tenir les emails.

```csharp
public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();
```

Nous avons également mis en place une nouvelle tâche pour livrer les e-mails en arrière-plan.
et une AnnulationTokenSource pour annuler gracieusement la tâche lorsque nous voulons arrêter le service.

Nous commençons ensuite le service hébergé avec StartAsync et fournissons le point d'entrée pour d'autres services pour envoyer un e-mail.

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

Dans notre classe de configuration, nous devons maintenant enregistrer le service avec le conteneur DI et démarrer le service hébergé

```csharp
       services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Maintenant, nous pouvons envoyer des courriels en arrière-plan en appelant la méthode SendEmailAsync sur le service EmailSenderHostedService.
Par exemple, pour le formulaire de contact, nous le faisons.

```csharp
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
```

Dans le code ci-dessus, ceci ajoute ce message à notre`BufferBlock<BaseEmailModel>` _mailMessages et la tâche d'arrière-plan vont la récupérer et envoyer l'email.

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

Cela sera alors en boucle jusqu'à ce que nous arrêtions le service et continuons à surveiller le BufferBlock pour de nouveaux courriels à envoyer.