# Додавання надсилання тла для повідомлень електронної пошти

<!--category-- ASP.NET -->
<datetime class="hidden">2024- 08- 07T08: 15</datetime>

## Вступ

У попередньому дописі я детально описував, як надсилати листи за допомогою FluentEmail і клієнта SMTP. Проте з цим пов'язана затримка у надсиланні повідомлень електронної пошти. Сервери SMTP зазвичай повільні і можуть потребувати часу для надсилання повідомлень електронної пошти. Це може дратувати користувачів і почуватися, наче лісозаготівля у вашій заяві.

Один зі способів зробити це - надіслати електронні листи на задньому плані. Таким чином, користувач може продовжувати користуватися програмою без потреби чекати на відсилання повідомлення електронної пошти. Це типовий шаблон у веб- програмах, його можна досягти за допомогою фонового завдання.

[TOC]

## Параметри тла у ядрі ASP. NET

У Core ASP. NET передбачено два основних параметри (посередині додаткових параметрів, зокрема Hangfire / Quartz)

- ISHostedService - цей параметр надає вам базове керування життєвим циклом для ваших фонових завдань. Ви можете запустити і зупинити службу, і вона працюватиме на задньому плані.
- Підтримуваний час життя - за допомогою цього пункту ви зможете краще керувати життєвим циклом ваших фонових завдань. Ви також можете запустити і зупинити службу, і вона працюватиме у фоновому режимі, але ви маєте більше керування запуском, зупинкою, запуском, зупинкою тощо...

У цьому прикладі я використаю простий IHosedService для надсилання електронних листів на задньому плані.

## Джерельний код

Нижче знаходиться повне джерело цього.

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
Тут ви можете бачити, як ми керуємо початком служби і налаштовування нового BufferBlock для зберігання повідомлень електронної пошти.

```csharp
public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();
```

Ми також створили нове завдання для доставки листів на задньому плані.
і "Скасування ТокенСоренс," щоб граціозно скасувати завдання, коли ми хочемо зупинити службу.

Потім ми запускаємо "HoveredService" за допомогою StartAsync і забезпечуємо вхідними пунктами для інших служб для надсилання електронної пошти.

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

У нашому класі Setup нам тепер потрібно зареєструвати службу з контейнером DI і запустити HardedService

```csharp
       services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Тепер ми можемо надсилати електронні листи на задньому плані за допомогою методу SendEmailAync на EmailSenderHosedService.
Наприклад, для контакту ми робимо це.

```csharp
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
```

У коді вище це додає це повідомлення до нашого `BufferBlock<BaseEmailModel>` _mailMessages і фонове завдання захопить його і надішле повідомлення електронної пошти.

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

Після цього система зациклиться, поки ми не зупинимо службу і не продовжимо стежити за BufferBlock, щоб надіслати нові листи електронної пошти.