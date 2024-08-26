# Att säkerställa din IHostedService (eller IHostedLifecycleService) är en enda instans

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-22T16:08</datetime>

## Inledning

Detta är en dum liten artikel eftersom jag var lite förvirrad om hur man kan se till att min `IHostedService` var ett enda exempel. Jag tyckte det var lite mer komplicerat än det faktiskt var. Så jag tänkte skriva en liten artikel om det. Bara ifall någon annan var förvirrad om det.

I och med att [tidigare artikel](/blog/addingasyncsendingforemails), vi behandlade hur man skapar en bakgrundstjänst med hjälp av `IHostedService` gränssnitt för att skicka e-post. Den här artikeln kommer att behandla hur du kan se till att din `IHostedService` är en enda instans.
Detta kan vara uppenbart för vissa, men det är inte för andra (och var inte omedelbart för mig!)..............................................................................................

[TOC]

## Varför är detta ett problem?

Jo det är ett nummer som de flesta av dessa artiklar täcker hur man använder en `IHostedService` Men de täcker inte hur man ser till att tjänsten är en enda instans. Detta är viktigt eftersom du inte vill att flera instanser av tjänsten körs samtidigt.

Vad menar jag? Väl i ASP.NET sättet att registrera en IHostedService eller IHostedlifeCycleService (i grund och botten samma med fler företräden för livscykelhantering) du använder detta

```csharp
  services.AddHostedService(EmailSenderHostedService);
```

Vad det gör är att kalla in denna gränssnittskod:

```csharp
public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

```

Vilket är fint och fint men tänk om du vill posta ett nytt meddelande direkt till denna tjänst från säga en `Controller` Börja?

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

Antingen måste du skapa ett gränssnitt som själv implementerar `IHostedService` sedan kalla in metoden på det eller du måste se till att tjänsten är en enda instans. Det senare är det enklaste sättet att göra detta (beroende på ditt scenario dock, för att testa gränssnittsmetoden kan vara att föredra).

### IHostedService

Du kommer att notera här att det registrerar tjänsten som en `IHostedService`, Detta har att göra med livscykelhanteringen av denna tjänst som ASP.NET ramverket kommer att använda denna registrering för att avfyra händelserna i denna tjänst (`StartAsync` och `StopAsync` för IHostedService). Se nedan. `IHostedlifeCycleService` är bara en mer detaljerad version av IHostedService.

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

## Hur ser du till att din IHostedService är en enda instans

### Gränssnittsinflygning

Gränssnittet kan vara enklare beroende på ditt scenario. Här skulle du lägga till ett gränssnitt som ärver från `IHostedService` och sedan lägga till en metod till det gränssnittet som du kan ringa från din controller.

**OBS: Du behöver fortfarande lägga till det som en HostedService i ASP.NET för tjänsten att faktiskt köra.**

```csharp
    public interface IEmailSenderHostedService : IHostedService, IDisposable
    {
        Task SendEmailAsync(BaseEmailModel message);
    }
```

Allt vi sedan behöver göra är att registrera detta som en singleton och sedan använda detta i vår kontrollant.

```csharp
             services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
        services.AddHostedService<IEmailSenderHostedService>(provider => provider.GetRequiredService<IEmailSenderHostedService>());
        
```

ASP.NET kommer att se att detta har rätt gränssnitt dekorerade och kommer att använda denna registrering för att köra `IHostedService`.

### Tillverkningsmetod

En annan för att se till att din `IHostedService` är en enda instans är att använda `AddSingleton` metod för att registrera din tjänst sedan passera `IHostedService` Registrering som "fabriksmetod". Detta kommer att säkerställa att endast en instans av din tjänst skapas och används under hela applikationens livstid.

* En förteckning över de behöriga myndigheter som avses i punkt 1 i denna artikel ska upprättas i enlighet med det förfarande som avses i artikel 4 i förordning (EU) nr 952/2013. *fabrik* metod är bara ett fint sätt att säga en metod som skapar en instans av ett objekt.

```csharp
        services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Så som ni ser här registrerar jag först min `IHostedService` (eller `IHostedLifeCycleService`) som en singelton och sedan använder jag `AddHostedService` Metod för att registrera tjänsten som fabriksmetod. Detta kommer att säkerställa att endast en instans av tjänsten skapas och används under hela applikationens livstid.

## Slutsatser

Som vanligt finns det ett par sätt att flå en katt.  Fabrikens metod är också ett bra sätt att se till att din tjänst är en enda instans. Det är upp till dig vilket tillvägagångssätt du väljer. Jag hoppas att den här artikeln har hjälpt dig att förstå hur du kan se till att din `IHostedService` är en enda instans.