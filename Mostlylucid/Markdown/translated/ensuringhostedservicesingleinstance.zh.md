# 确保您所住的服务(或IHEPLSYSYS)仅举一例

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-221T16:08</datetime>

## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

这是一个愚蠢的小文章 因为我有点困惑 如何确保 `IHostedService` 仅举一个例子。 我觉得这比实际的复杂多了一点 所以我想写一篇关于它的文章 以防别人对此困惑

在 [前一条文](/blog/addingasyncsendingforemails),我们讨论了如何创建背景服务 使用 `IHostedService` 发送电子邮件的界面 。 本条将涵盖如何确保贵国 `IHostedService` 是一个实例。
这或许对有些人来说是显而易见的, 但对其他人来说并非如此(而且对我而言也不是直接的!) )

[技选委

## 为什么这是个问题?

因为大部分条款 都涉及到如何使用 `IHostedService` 但它们并不包括如何确保 服务是一个单一的例子。 这一点很重要,因为你不希望 多种服务同时运行。

什麽意思? 在 ASP.NET 中,您使用这个方法来重新注册“一服务或一服务或一服务”或“一服务或一服务/一服务” (基本上与使用寿命周期管理的更多超控制功能基本相同)

```csharp
  services.AddHostedService(EmailSenderHostedService);
```

这需要在这个后端代码中调用什么 :

```csharp
public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

```

如果您想直接向此服务发送一条新信息, `Controller` 动作?

```csharp

public class ContactController(EmailSenderHostedService sender,ILogger<BaseController> logger) ...
{
   [HttpPost]
    [Route("submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit([Bind(Prefix = "")] ContactViewModel comment)
    {
        ViewBag.Title = "Contact";
        //Only allow HTMX requests
        if(!Request.IsHtmx())
        {
            return RedirectToAction("Index", "Contact");
        }
      
        if (!ModelState.IsValid)
        {
            return PartialView("_ContactForm", comment);
        }

        var commentHtml = commentService.ProcessComment(comment.Comment);
        var contactModel = new ContactEmailModel()
        {
            SenderEmail = string.IsNullOrEmpty(comment.Email) ? "Anonymous" : comment.Email,
            SenderName = string.IsNullOrEmpty(comment.Name) ? "Anonymous" : comment.Name,
            Comment = commentHtml,
        };
        await sender.SendEmailAsync(contactModel);
        return PartialView("_Response",
            new ContactViewModel() { Email = comment.Email, Name = comment.Name, Comment = commentHtml });

        return RedirectToAction("Index", "Home");
    }
   }
```

要么您需要创建一个可自行执行的界面 `IHostedService` 然后请使用该方法,或者您需要确保该服务是一个单一实例。 后者是这样做最容易的方法(但最好以你的假设情况为依据,以测试接口方法)。

### 长期服务

你在这里会注意到 它把服务登记为 `IHostedService`,这与这项服务的生命周期管理有关,因为ASP.NET框架将利用这项登记来解雇这项服务的事件(ASP.NET)。`StartAsync` 和 `StopAsync` 服务处)) 见下文: `IHostedlifeCycleService` 只是一个更详细版本的IHOSTD Services。

```csharp
  /// <summary>
  /// Defines methods for objects that are managed by the host.
  /// </summary>
  public interface IHostedService
  {
    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous Start operation.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous Stop operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);
  }

namespace Microsoft.Extensions.Hosting
{
  /// <summary>
  /// Defines methods that are run before or after
  /// <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StartAsync(System.Threading.CancellationToken)" /> and
  /// <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" />.
  /// </summary>
  public interface IHostedLifecycleService : IHostedService
  {
    /// <summary>
    /// Triggered before <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StartAsync(System.Threading.CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    Task StartingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered after <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StartAsync(System.Threading.CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    Task StartedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered before <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    Task StoppingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered after <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the stop process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    Task StoppedAsync(CancellationToken cancellationToken);
  }
}
```

## 如何确保你的 " 临时服务 " 是一个单一的例子

### 接口方法

接口方法可能比较简单 取决于您的情况。 在这里您会添加一个界面, 从此继承 `IHostedService` 然后在该界面中添加一个方法,您可以从控制器中调用该方法。

```csharp
    public interface IEmailSenderHostedService : IHostedService
    {
        Task SendEmailAsync(BaseEmailModel message);
        void Dispose();
    }
```

我们只需要将它登记为单吨 然后在我们的控制器中使用它

```csharp
        services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
```

ASP.NET 将看到此接口装饰正确, 并将使用此注册来运行 `IHostedService`.

### 工厂方法方法方法

另一个能确保 `IHostedService` 是一个单例,即使用 `AddSingleton` 用于注册您的服务,然后通过 `IHostedService` 登记为“设施方法”。 这将确保您在整个申请期间只提供一次服务,并使用一次服务。

* A A A *工厂工厂* 方法只是一种巧妙的表达方式 一种创造对象实例的方法。

```csharp
        services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

如你所见,我首先登记 `IHostedService` (或) `IHostedLifeCycleService`)作为单顿,然后我用 `AddHostedService` 将服务登记为工厂方法的方法。 这将确保在整个申请期间只建立和使用一个服务实例。

## 在结论结论中

和往常一样 有几种方法可以剥猫皮 接口法可能是最容易确保 `IHostedService` 是一个实例。 但是,工厂方法方法也是确保您的服务仅举一例的好办法。 由你决定你走哪条路 我希望这篇文章帮助你了解如何确保 `IHostedService` 是一个实例。