# إضافة معلومات خلفية مُرسِل لـ البريد الإلكتروني

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-08-07-TT08: 15</datetime>

مقدمة

في مقالتي السابقة قمت بتفصيل كيفية إرسال رسائل البريد الإلكتروني باستخدام FluentEmail والعميل SMTP. لكن هناك مشكلة واحدة في هذا هي التأخير في إرسال البريد الإلكتروني. وتميل خوادم SMTP إلى أن تكون بطيئة ويمكن أن تستغرق بعض الوقت لإرسال البريد الإلكتروني. يمكن أن يكون هذا مزعجاً للمستخدمين ويشعرون وكأنهم لوجوجام في تطبيقك.

طريقة واحدة للحصول على هذا هو إرسال رسائل بريد إلكتروني في الخلفية. بهذه الطريقة يمكن للمستخدم مواصلة استخدام التطبيق دون الحاجة إلى انتظار إرسال البريد الإلكتروني. وهذا نمط شائع في تطبيقات الشبكة العالمية ويمكن تحقيقه باستخدام وظيفة أساسية.

[رابعاً -

## أولاً - معلومات أساسية

في ASP.net الأساسية لديك خياران رئيسيان (بالإضافة إلى خيارات أكثر تقدماً مثل Hhangfire / Qaartz)

- الخدمة المحصنة - هذا الخيار يتيح لك إدارة دورة الحياة الأساسية لمهامك الأساسية. يمكنك أن تبدأ و توقف الخدمة وسوف تعمل في الخلفية.
- هذا الخيار يعطيك المزيد من السيطرة على دورة حياة مهامك الأساسية. يمكنك أيضاً أن تبدأ و توقف الخدمة وسوف تعمل في الخلفية ولكن لديك المزيد من التحكم

في هذا المثال سأستخدم خدمة IhostedService بسيطة لإرسال رسائل البريد الإلكتروني في الخلفية.

## & شيفر

المصدر الكامل لهذا هو أدناه.

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
يمكنك هنا أن ترى أننا نتعامل مع بداية الخدمة وننشئ BufferBlock جديد لتحمّل البريد الإلكتروني.

```csharp
public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();
```

كما أنشأنا مهمة جديدة لإيصال البريد الإلكتروني في الخلفية.
و الملغيات تَكُونُ المُرَوَّدَ لإلغاء المهمةِ برشاقة عندما نُريدُ إيقاف الخدمةِ.

ثم نبدأ ببدء الخدمة المستضيفة مع Start Async وتوفير نقطة الدخول للخدمات الأخرى لإرسال البريد الإلكتروني.

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

في صف الإعدادات لدينا نحتاج الآن لتسجيل الخدمة مع الحاوية DI وبدء الخدمة المضيفة

```csharp
       services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

الآن يمكننا إرسال رسائل البريد الإلكتروني في الخلفية عن طريق الاتصال بطريقة الإسناد Email Async على البريد الإلكتروني Sender HoustedService.
على سبيل المثال، بالنسبة لنموذج الاتصال نحن نفعل ذلك.

```csharp
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
```

في الرمز فوق هذا يضيف هذه الرسالة إلى `BufferBlock<BaseEmailModel>` _رسائل البريد ومهمة الخلفية ستلتقطها وترسل البريد الإلكتروني.

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

هذا بدوره سوف يدور حتى نوقف الخدمة ونستمر في مراقبة BoverBlock لرسائل البريد الإلكتروني الجديدة لإرسالها.