# ضمانك للخدمة المُقَرَّنة (أو خدمة خدمة دورة حياة مُقَنَنة) هو حالة واحدة

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-21TT16: 08</datetime>

## أولاً

هذه مقاله صغيره غبيه لأنني كنت مرتبكه قليلاً حول كيفية ضمان `IHostedService` في حالة واحدة. ظننت أن الأمر أكثر تعقيداً مما هو عليه في الواقع لذا فكرت أن أكتب مقالاً صغيراً عن ذلك. فقط في حالة أي شخص آخر كان مرتبك حول ذلك.

في الـ [قبل سنة](/blog/addingasyncsendingforemails)، قمنا بتغطية كيفية إنشاء خدمة خلفية `IHostedService` واجهة لـ إرسال بريد إلكتروني. ستغطي هذه المقالة كيفية ضمان `IHostedService` هو مثال واحد.
قد يكون هذا واضحا للبعض، لكنه ليس للآخرين (ولم يكن مباشرة بالنسبة لي!)ع(

[رابعاً -

## لماذا هذه قضية؟

حسناً، إنها قضية مثل معظم المواد خارج هذه تغطي كيفية استخدام `IHostedService` لكنها لا تغطي كيفية ضمان أن الخدمة هي حالة واحدة. هذا مهم كما أنك لا تريد حالات متعددة من الخدمة تعمل في نفس الوقت.

ما الذي أعنيه؟ حسناً في ASP.net الطريقة التي يمكن بها تسجيل خدمة IHOstedSerfice أو خدمة CCSERS (أو أساساً نفس الشيء مع المزيد من التجاوزات لإدارة دورة دورة دورة الحياة) يمكنك استخدام هذا

```csharp
  services.AddHostedService(EmailSenderHostedService);
```

ما يقوم به هو نداءات في هذا الرمز الخلفي:

```csharp
public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

```

و الذي هو جيد و رائع لكن ماذا لو أردت أن تنشر رسالة جديدة مباشرة إلى هذه الخدمة `Controller` (أ) هل يمكن أن يكون هذا الفعل فعلاً أم فعلاً؟

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

لا تحتاج إلى إنشاء واجهة تقوم هي نفسها بتنفيذ `IHostedService` ثم اتصل بالطريق على ذلك أو عليك أن تتأكد من أن الخدمة هي حالة واحدة. وهذه هي الطريقة الأسهل للقيام بذلك (على الرغم من أنه قد يكون من الأفضل الاعتماد على سيناريوك لاختبار طريقة الواجهة).

### الخدمة المعتمدة

أنت سَتُلاحظُ هنا بأنّه يُسجّلُ الخدمة كa `IHostedService`هذا يتعلق بإدارة دورة حياة هذه الخدمة حيث أن إطار ASP.NET سوف يستخدم هذا التسجيل لتحريض أحداث هذه الخدمة (ASP.NET)`StartAsync` وقد عقد مؤتمراً بشأن `StopAsync` (الخدمات المخصصة) (الخدمات المخصصة) انظر أدناه، `IHostedlifeCycleService` هو مجرد نسخة أكثر تفصيلاً من الخدمة المُخصصة.

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

## كيف تتأكد من ان عملك المعتمد هو مثال واحد

### نهج الواجه

منهج الواجهة قد يكون أسهل اعتماداً على سيناريوك. هنا يمكنك إضافة واجهة ترث من `IHostedService` ومن ثم اضف طريقة لذلك الواجهة التي يمكنك الاتصال بها من متحكمك.

```csharp
    public interface IEmailSenderHostedService : IHostedService
    {
        Task SendEmailAsync(BaseEmailModel message);
        void Dispose();
    }
```

كل ما نحتاجه بعد ذلك هو تسجيل هذا كأفردتون ومن ثم استخدام هذا في متحكمنا.

```csharp
        services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
```

SP.net سوف ترى أن هذا يحتوي على الوصلة البينية الصحيحة مزينة وسوف تستخدم هذا التسجيل لتشغيل `IHostedService`.

### النهج المتبع في

معلومات أخرى لضمان أن `IHostedService` هو a حالة منفردة هو إلى استخدام `AddSingleton` إلى تسجيل الخدمة الخاصة بك ثم تمر `IHostedService` كـ "طريقة اعتبارية". هذا سيضمن أن حالة واحدة فقط من الخدمة الخاصة بك يتم إنشاؤها واستخدامها طوال عمر الطلب.

* A A ** هي عبارة عن طريقة مبهرجة لقول طريقة تخلق حالة جسم ما.

```csharp
        services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

كما ترون هنا أنا أولاً أسجل `IHostedService` (في `IHostedLifeCycleService`(كأحادي طن واحد ومن ثم أستخدم `AddHostedService` طريقة تسجيل الخدمة كطريقة تصنيع. وسيكفل ذلك إنشاء واستخدام حالة واحدة فقط من الخدمة طوال مدة تقديم الطلب.

## في الإستنتاج

كالعادة هناك طريقتان لسلخ قطة نهج الواجهة هو على الأرجح أسهل طريقة لضمان أن `IHostedService` هو مثال واحد. ولكن نهج طريقة المصنع هو أيضاً طريقة جيدة للتأكد من أن الخدمة الخاصة بك هي حالة واحدة. الأمر عائد لكِ منهجكِ الذي تأخذينه. أرجو أن تكون هذه المادة قد ساعدتك على فهم كيفية ضمان `IHostedService` هو مثال واحد.