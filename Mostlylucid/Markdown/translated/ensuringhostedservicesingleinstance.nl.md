# Ervoor zorgen dat uw IHostedService (of IHostedLifecycleService) één instantie is

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-21T16:08</datetime>

## Inleiding

Dit is een dom artikel omdat ik een beetje in de war was over hoe ervoor te zorgen dat mijn `IHostedService` was een enkel geval. Ik dacht dat het iets ingewikkelder was dan het eigenlijk was. Dus ik wilde er een artikel over schrijven. Voor het geval iemand anders er in de war over was.

In de [voorgaand artikel](/blog/addingasyncsendingforemails), we bespraken hoe een background service te creëren met behulp van de `IHostedService` interface voor het versturen van e-mails. Dit artikel zal betrekking hebben op hoe ervoor te zorgen dat uw `IHostedService` is een enkele instantie.
Dit kan voor sommigen duidelijk zijn, maar het is niet voor anderen (en was niet meteen voor mij!).

[TOC]

## Waarom is dit een probleem?

Nou, het is een kwestie als de meeste artikelen uit deze hebben betrekking op hoe een `IHostedService` maar ze hebben geen betrekking op hoe ervoor te zorgen dat de dienst is een enkele instantie. Dit is belangrijk omdat u niet wilt dat meerdere instanties van de dienst draaien op hetzelfde moment.

Wat bedoel ik? Goed in ASP.NET de manier om een IHostedService of IHostedlifeCycleService (in principe hetzelfde met meer overrides voor lifecycle management) u deze gebruiken

```csharp
  services.AddHostedService(EmailSenderHostedService);
```

Wat dat doet is oproepen in deze backend code:

```csharp
public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

```

Dat is prima en dandy, maar wat als je wilt een nieuw bericht direct te plaatsen naar deze dienst van zeggen een `Controller` Actie?

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

Of je moet een interface maken die zelf implementeert `IHostedService` dan bel de methode op dat of je moet ervoor zorgen dat de dienst is een enkele instantie. Dit laatste is de makkelijkste manier om dit te doen (afhankelijk van uw scenario, maar voor het testen van de Interface methode zou de voorkeur kunnen worden gegeven).

### IHostedService

U zult hier merken dat het registreert de dienst als een `IHostedService`, dit heeft te maken met het levenscyclusbeheer van deze dienst, aangezien het ASP.NET-kader deze registratie zal gebruiken om de gebeurtenissen van deze dienst aan te wakkeren (`StartAsync` en `StopAsync` voor IHostedService). Zie hieronder, `IHostedlifeCycleService` is gewoon een meer gedetailleerde versie van IHostedService.

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

## Hoe ervoor te zorgen dat uw IHostedService één instantie is

### Interface-aanpak

De Interface-benadering kan eenvoudiger zijn afhankelijk van uw scenario. Hier voeg je een interface toe die erft van `IHostedService` en voeg dan een methode toe aan die interface die je vanuit je controller kunt bellen.

```csharp
    public interface IEmailSenderHostedService : IHostedService
    {
        Task SendEmailAsync(BaseEmailModel message);
        void Dispose();
    }
```

Alles wat we dan hoeven te doen is dit registreren als een singleton en dan gebruik maken van dit in onze controller.

```csharp
        services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
```

ASP.NET zal zien dat dit de juiste interface gedecoreerd heeft en zal deze registratie gebruiken om de `IHostedService`.

### Fabrieksmethodebenadering

Een andere om ervoor te zorgen dat uw `IHostedService` is een enkele instantie is om de `AddSingleton` methode om uw service te registreren en vervolgens de `IHostedService` registratie als "fabrieksmethode." Dit zal ervoor zorgen dat slechts één instantie van uw dienst wordt gemaakt en gebruikt gedurende de hele levensduur van de toepassing.

* A *fabriek* methode is gewoon een chique manier om een methode te zeggen die een instantie van een object creëert.

```csharp
        services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Dus zoals je hier ziet, registreer ik eerst mijn `IHostedService` (of `IHostedLifeCycleService`) als singleton en dan gebruik ik de `AddHostedService` methode om de dienst te registreren als een fabrieksmethode. Dit zal ervoor zorgen dat slechts één instantie van de dienst wordt gecreëerd en gebruikt gedurende de gehele levensduur van de toepassing.

## Conclusie

Zoals gewoonlijk zijn er een paar manieren om een kat te villen. De interface benadering is waarschijnlijk de gemakkelijkste manier om ervoor te zorgen dat uw `IHostedService` is een enkele instantie. Maar de fabriek methode aanpak is ook een goede manier om ervoor te zorgen dat uw service is een enkele instantie. Het is aan jou welke benadering je neemt. Ik hoop dat dit artikel u heeft geholpen te begrijpen hoe ervoor te zorgen dat uw `IHostedService` is een enkele instantie.