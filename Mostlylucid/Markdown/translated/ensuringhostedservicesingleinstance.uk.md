# Введення вашого IHosedService (або IHocated LifeCService) - це єдиний екземпляр

<!--category-- ASP.NET -->
<datetime class="hidden">2024- 08- 22T16: 08</datetime>

## Вступ

Це дурна маленька стаття, тому що я була трохи збентежена щодо того, як це зробити. `IHostedService` був один приклад. Я думав, що це трохи складніше, ніж було насправді. Тому я вирішив написати про це маленьку статтю. На случай, если кто-то еще сомневается в этом.

У [Попередня стаття](/blog/addingasyncsendingforemails)Ми обговорювали, як створити фонову службу за допомогою `IHostedService` інтерфейс для надсилання повідомлень електронної пошти. У цій статті ми поговоримо про те, як переконатися у тому, що ви `IHostedService` є одним прикладом.
Для декого це може бути очевидним, але це не для інших (і не для мене відразу!).

[TOC]

## Чому це є проблемою?

Ну, це спірне питання, як більшість статей, що обговорюються, як використовувати `IHostedService` але вони не покривають як гарантувати, що служба є одним прикладом. Це важливо, оскільки ви не хочете мати декілька екземплярів служби, яка виконується одночасно.

Що я маю на увазі? Ну, у ASPNET спосіб зареєструвати IHosedService або IHostedSycleCycleService (зазвичай, те саме, що з більшою кількістю перевизначення для управління життєвим циклом) ви використовуєте це

```csharp
  services.AddHostedService(EmailSenderHostedService);
```

Це викликає цей серверний код:

```csharp
public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

```

Що добре і прекрасно, але що, якщо ви хочете надіслати нове повідомлення прямо до цієї служби від сказати a `Controller` Почали?

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

Або вам потрібно створити інтерфейс, який сам себе реалізує `IHostedService` Потім зателефонуйте до методу або вам потрібно переконатися, що послуга - це єдиний екземпляр. Останнє - це найпростіший спосіб виконання цієї дії (але у вашому сценарії може виникнути потреба у перевірці способу інтерфейсу).

### IHosedService

Ви помітите тут, що він реєструє службу як `IHostedService`Це пов'язано з управлінням життєвим циклом цієї служби, оскільки система ASP.NET використовуватиме цю реєстрацію для того, щоб підпалити події цієї служби (`StartAsync` і `StopAsync` Для IHosedService). Дивись нижче. `IHostedlifeCycleService` Це лише більш докладна версія IHostedService.

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

## Як переконатися, що ваш IHosedService є єдиним екземпляром

### Підхід інтерфейсу

Доступ до інтерфейсу може бути простішим, залежно від вашого сценарію. Тут ви можете додати інтерфейс, від якого успадковується інтерфейс `IHostedService` а потім додайте метод до інтерфейсу, який ви можете викликати від контролера.

```csharp
    public interface IEmailSenderHostedService : IHostedService
    {
        Task SendEmailAsync(BaseEmailModel message);
        void Dispose();
    }
```

Все, что нам нужно, это зарегистрировать это в одиночестве, а потом использовать это в инспекторе.

```csharp
        services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
```

ASP.NET побачить, що це має правильний інтерфейс і буде використовувати цю реєстрацію для запуску `IHostedService`.

### Наближається метод виробника

Ще один, щоб переконатися, що ви `IHostedService` є одним екземпляром є використання `AddSingleton` метод реєстрації вашої служби, а потім передавання `IHostedService` Реєстрація як "факторний метод." За допомогою цього пункту можна забезпечити створення і використання лише одного екземпляра вашої служби протягом всього життя програми.

* А *фабрика* метод - це просто уявний спосіб сказати метод, який створює зразок об'єкта.

```csharp
        services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Так як ви бачите тут, я вперше зареєструю мій `IHostedService` (або `IHostedLifeCycleService`) як одинокий, а потім я використовую `AddHostedService` спосіб зареєструвати службу як метод фабрики. Це забезпечить створення і використання лише одного екземпляра служби протягом життя програми.

## Включення

Як завжди, є декілька способів обдерти кішку. Доступ до інтерфейсу, ймовірно, є найпростішим способом переконатися, що ви `IHostedService` є одним прикладом. Але підхід до роботи на заводі - це також хороший спосіб переконатися, що ваша послуга - це єдиний екземпляр. Це залежить від вас, який підхід ви виберете. Надіюсь, що ця стаття допомогла вам зрозуміти, як запевнити вас у цьому. `IHostedService` є одним прикладом.