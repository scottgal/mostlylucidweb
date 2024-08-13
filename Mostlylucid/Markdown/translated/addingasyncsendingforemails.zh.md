# 为电子邮件添加背景发送

<!--category-- ASP.NET -->
<datetime class="hidden">2024-008-007T08:15</datetime>

一. 导言 一. 导言 一. 导言 一. 导言 一. 导言 一. 导言 一. 导言 一. 导言 一. 导言 一. 导言

使用流利电子邮件及SMTP客户端发送电子邮件。 然而,其中一个问题是电子邮件发送的延误。 SMTP服务器往往速度缓慢,可能需要一段时间才能发送电子邮件。 这可能会令用户感到烦恼,

其中一个办法就是在背景中发送邮件。 这样用户就可以继续使用应用程序而不必等待电子邮件发送。 这是网络应用程序中的一种常见模式,可以使用背景工作实现。

[技选委

## ASP.NET核心中的背景备选方案

在 ASP.NET Core 中,你有两个主要选项(除了更先进的选项,如Hangfire/Quartz)

- IPService - 此选项为您提供基本生命周期管理, 用于您的背景任务 。 您可以开始并停止服务, 服务将在背景中运行 。
- IhostedLifetime - 这个选项使您更能控制您背景任务的生命周期 。 您也可以启动并停止服务, 服务将在背景中运行, 但您拥有更多的控制路径, 启动、 停止、 启动、 停止等...

在这个例子中,我将使用一个简单的 IHOSTD Service 来发送背景中的邮件。

## 源代码

完整的资料来源如下。

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
这里您可以看到我们处理服务的启动, 并设置一个新的 ButfferBlock 来控制电子邮件 。

```csharp
public class EmailSenderHostedService(EmailService emailService, ILogger<EmailSenderHostedService> logger)
        : IHostedService, IDisposable
    {
        private readonly BufferBlock<BaseEmailModel> _mailMessages = new();
        private Task _sendTask = Task.CompletedTask;
        private CancellationTokenSource cancellationTokenSource = new();
```

我们还设立了一个新的任务,在背景中发送电子邮件。
以及取消源代码 当我们想停止服务时 优雅地取消任务。

然后我们用StartAsync启动托管服务, 并为其他服务发送电子邮件提供切入点。

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

在我们的设置类中,我们现在需要 注册服务 在DI集装箱 并启动主机服务

```csharp
       services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

现在,我们可以通过在电子邮件SenderHosted Services上 调用SendeEmailAsync 方法, 在背景中发送电子邮件。
例如,对于联系表,我们这样做。

```csharp
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
```

在上文的代码中,这又增加了这一信息。 `BufferBlock<BaseEmailModel>` _邮件邮件和背景任务将接收并发送电子邮件。

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

这将循环, 直到我们停止服务, 并继续监视 ButfferBlock, 以便发送新的电子邮件 。