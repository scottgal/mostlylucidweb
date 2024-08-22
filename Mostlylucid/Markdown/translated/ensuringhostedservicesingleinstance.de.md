# Sicherstellen, dass Ihr IHostedService (oder IHostedLifecycleService) eine einzige Instanz ist

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-21T16:08</datetime>

## Einleitung

Dies ist ein dummer kleiner Artikel, weil ich war ein wenig verwirrt darüber, wie zu gewährleisten, dass meine `IHostedService` war eine einzige Instanz. Ich dachte, es wäre etwas komplizierter, als es tatsächlich war. Also dachte ich, ich schreibe einen kleinen Artikel darüber. Nur für den Fall, dass jemand anderes darüber verwirrt war.

In der [vorheriger Artikel](/blog/addingasyncsendingforemails), wir behandelt, wie man einen Hintergrund-Service mit dem erstellen `IHostedService` Schnittstelle zum Senden von E-Mails. Dieser Artikel behandelt, wie Sie sicherstellen, dass Ihre `IHostedService` ist eine einzige Instanz.
Dies mag für einige offensichtlich sein, aber es ist nicht für andere (und war nicht sofort für mich!)== Einzelnachweise ==

[TOC]

## Warum ist das ein Problem?

Nun, es ist ein Problem, da die meisten der Artikel, die diese umfassen, wie man eine `IHostedService` Aber sie decken nicht ab, wie man sicherstellt, dass der Dienst eine einzige Instanz ist. Dies ist wichtig, da Sie nicht möchten, dass mehrere Instanzen des Dienstes gleichzeitig ausgeführt werden.

Was soll ich sagen? Gut in ASP.NET die Art und Weise, einen IHostedService oder IHostedlifeCycleService zu registieren (basisch die gleiche mit mehr Überbrückungen für das Lebenszyklusmanagement) Sie verwenden diese

```csharp
  services.AddHostedService(EmailSenderHostedService);
```

Was das tut, ist in diesen Backend-Code zu rufen:

```csharp
public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

```

Was ist in Ordnung und Dandy, aber was, wenn Sie eine neue Nachricht direkt an diesen Dienst posten von sagen ein `Controller` Aktion?

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

Entweder müssen Sie eine Schnittstelle, die selbst implementiert erstellen `IHostedService` Dann rufen Sie die Methode auf, oder Sie müssen sicherstellen, dass der Dienst eine einzige Instanz ist. Letzteres ist der einfachste Weg, dies zu tun (abhängig von Ihrem Szenario, aber für das Testen der Interface-Methode könnte bevorzugt werden).

### IHostedService

Sie werden hier feststellen, dass es registriert den Service als `IHostedService`, dies hat mit dem Lebenszyklus-Management dieses Dienstes zu tun, da das ASP.NET-Framework diese Registrierung verwenden wird, um die Ereignisse dieses Dienstes abzufeuern (`StartAsync` und `StopAsync` für IHostedService). Siehe unten. `IHostedlifeCycleService` ist nur eine detailliertere Version von IHostedService.

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

## Wie Sie sicherstellen können, dass Ihr IHostedService eine einzige Instanz ist

### Schnittstellenansatz

Der Interface-Ansatz könnte je nach Szenario einfacher sein. Hier würden Sie eine Schnittstelle hinzufügen, die von erbt `IHostedService` und fügen Sie dann eine Methode zu dieser Schnittstelle hinzu, die Sie von Ihrem Controller aufrufen können.

```csharp
    public interface IEmailSenderHostedService : IHostedService
    {
        Task SendEmailAsync(BaseEmailModel message);
        void Dispose();
    }
```

Alles was wir dann tun müssen, ist dies als Singleton zu registrieren und dann in unserem Controller zu verwenden.

```csharp
        services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
```

ASP.NET wird sehen, dass dies die richtige Schnittstelle dekoriert hat und wird diese Registrierung verwenden, um die `IHostedService`.

### Methodenansatz für die Fabrik

Eine andere, um sicherzustellen, dass Ihre `IHostedService` ist eine einzige Instanz ist, um die `AddSingleton` Methode, um Ihren Service zu registrieren, dann übergeben Sie die `IHostedService` Registrierung als 'Fabrikmethode'. Dadurch wird sichergestellt, dass nur eine Instanz Ihres Dienstes während der gesamten Lebensdauer der Anwendung erstellt und verwendet wird.

* A..............................................................................................................................................................................................................................................................  *Fabrik* Methode ist nur eine schicke Art, eine Methode zu sagen, die eine Instanz eines Objekts erzeugt.

```csharp
        services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

So wie Sie hier sehen, registriere ich zuerst meine `IHostedService` (oder `IHostedLifeCycleService`) als Singleton und dann verwende ich die `AddHostedService` Methode, um den Service als Fabrikmethode zu registrieren. Dadurch wird sichergestellt, dass während der gesamten Laufzeit der Anwendung nur eine Instanz des Dienstes erstellt und genutzt wird.

## Schlussfolgerung

Wie immer gibt es ein paar Möglichkeiten, eine Katze zu häuten. Die Schnittstelle Ansatz ist wahrscheinlich der einfachste Weg, um sicherzustellen, dass Ihre `IHostedService` ist eine einzige Instanz. Aber die Fabrik Methode Ansatz ist auch ein guter Weg, um sicherzustellen, dass Ihr Service ist eine einzige Instanz. Es liegt an Ihnen, welche Annäherung Sie nehmen. Ich hoffe, dieser Artikel hat Ihnen geholfen zu verstehen, wie Sie sicherstellen, dass Ihre `IHostedService` ist eine einzige Instanz.