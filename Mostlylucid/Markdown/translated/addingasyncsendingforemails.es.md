# Añadiendo fondo Envío de correos electrónicos

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-07T08:15</datetime>

## Introducción

En mi post anterior detallé cómo enviar correos electrónicos usando FluentEmail y el cliente SMTP. Sin embargo, un problema con esto es el retraso en el envío de correos electrónicos. Los servidores SMTP tienden a ser lentos y pueden tardar un tiempo en enviar correos electrónicos. Esto puede ser molesto para los usuarios y sentirse como un logjam en su aplicación.

Una manera de evitar esto es enviar correos electrónicos en segundo plano. De esta manera el usuario puede continuar usando la aplicación sin tener que esperar a que el correo electrónico se envíe. Este es un patrón común en las aplicaciones web y se puede lograr utilizando un trabajo de fondo.

[TOC]

## Opciones de fondo en ASP.NET Core

En ASP.NET Core tienes dos opciones principales (además de opciones más avanzadas como Hangfire / Cuarzo)

- IHostedService - esta opción le da gestión básica del ciclo de vida para sus tareas de fondo. Puede iniciar y detener el servicio y se ejecutará en segundo plano.
- IHostedLifetime - esta opción le da más control sobre el ciclo de vida de sus tareas de fondo. También puede iniciar y detener el servicio y se ejecutará en segundo plano, pero tiene más control aroundn de inicio, parada, inicio, parada, etc...

En este ejemplo usaré un simple IHostedService para enviar correos electrónicos en segundo plano.

## Código fuente

La fuente completa para esto es abajo.

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
Aquí usted puede ver que manejamos el inicio del servicio y la configuración de un nuevo BufferBlock para mantener los correos electrónicos.

```csharp
public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();
```

También hemos creado una nueva tarea para entregar los correos electrónicos en segundo plano.
y una CancelaciónTokenSource para cancelar la tarea con gracia cuando queremos detener el servicio.

A continuación, iniciamos el HostedService con StartAsync y proporcionamos el punto de entrada para que otros servicios envíen un correo electrónico.

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

En nuestra clase de configuración ahora necesitamos registrar el servicio con el contenedor DI e iniciar el HostedService

```csharp
       services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Ahora podemos enviar correos electrónicos en segundo plano llamando al método SendEmailAsync en el EmailSenderHostedService.
Por ejemplo, para el formulario de contacto hacemos esto.

```csharp
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
```

En el código anterior esto añade este mensaje a nuestro`BufferBlock<BaseEmailModel>` _mailMessages y la tarea de fondo lo recogerá y enviará el correo electrónico.

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

Esto entonces loop hasta que detengamos el servicio y sigamos monitoreando el BufferBlock para enviar nuevos correos electrónicos.