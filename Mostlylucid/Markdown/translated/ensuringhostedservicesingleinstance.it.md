# Garantire il vostro IHostedService (o IHostedLifecycleService) è una singola istanza

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-22T16:08</datetime>

## Introduzione

Questo è un piccolo articolo stupido perché ero un po 'confusa su come garantire che il mio `IHostedService` era un unico esempio. Pensavo fosse un po' piu' complicato di quanto non fosse in realta'. Così ho pensato di scrivere un piccolo articolo su di esso. Nel caso qualcun altro fosse confuso.

Nella [articolo precedente](/blog/addingasyncsendingforemails), abbiamo coperto come creare un servizio di background utilizzando il `IHostedService` interfaccia per l'invio di email. Questo articolo coprirà come garantire che il vostro `IHostedService` è un'unica istanza.
Questo potrebbe essere ovvio per alcuni, ma non è per altri (e non è stato immediatamente per me!).

[TOC]

## Perché questo è un problema?

Beh, è un problema come la maggior parte degli articoli fuori questi come utilizzare un `IHostedService` ma non coprono come assicurarsi che il servizio sia una singola istanza. Questo è importante perché non si vogliono più istanze del servizio in esecuzione allo stesso tempo.

Cosa voglio dire? Bene in ASP.NET il modo per registrare un IHostedService o IHostedlifeCycleService (fondamentalmente lo stesso con più sovrascritture per la gestione del ciclo di vita) si utilizza questo

```csharp
  services.AddHostedService(EmailSenderHostedService);
```

Ciò che fa è chiamare in questo codice di backend:

```csharp
public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

```

Che va bene e dandy, ma cosa succede se si desidera pubblicare un nuovo messaggio direttamente a questo servizio da dire un `Controller` Azione?

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

O è necessario creare un'interfaccia che si implementa `IHostedService` poi chiamare il metodo su questo o è necessario assicurarsi che il servizio è una singola istanza. Quest'ultimo è il modo più semplice per farlo (dipende dal vostro scenario però, per testare il metodo Interface potrebbe essere preferito).

### IHostedService

Qui noterete che registra il servizio come un `IHostedService`, questo ha a che fare con la gestione del ciclo di vita di questo servizio come il framework ASP.NET userà questa registrazione per accendere gli eventi di questo servizio (`StartAsync` e `StopAsync` per IHostedService). Vedi sotto, `IHostedlifeCycleService` è solo una versione più dettagliata di IHostedService.

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

## Come garantire che il vostro IHostedService sia una singola istanza

### Approccio all'interfaccia

L'approccio Interface potrebbe essere più semplice a seconda dello scenario. Qui si aggiunge un'interfaccia che eredita da `IHostedService` e poi aggiungere un metodo a quell'interfaccia che è possibile chiamare dal vostro controller.

```csharp
    public interface IEmailSenderHostedService : IHostedService
    {
        Task SendEmailAsync(BaseEmailModel message);
        void Dispose();
    }
```

Tutto quello che dobbiamo fare è registrarlo come singoloton e poi usarlo nel nostro controller.

```csharp
        services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
```

ASP.NET vedrà che questo ha l'interfaccia corretta decorata e userà questa registrazione per eseguire il `IHostedService`.

### Approccio del metodo di fabbrica

Un altro per garantire che il vostro `IHostedService` è una singola istanza è quella di utilizzare il `AddSingleton` metodo per registrare il vostro servizio poi passare il `IHostedService` registrazione come "metodo di fabbrica." Ciò garantirà che venga creata e utilizzata una sola istanza del servizio durante tutta la durata dell'applicazione.

* A *fabbrica* metodo è solo un modo elegante di dire un metodo che crea un'istanza di un oggetto.

```csharp
        services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Quindi, come vedete qui, prima registro il mio `IHostedService` (o `IHostedLifeCycleService`) come singleton e poi uso il `AddHostedService` metodo per registrare il servizio come metodo di fabbrica. Ciò garantirà la creazione e l'utilizzo di un'unica istanza del servizio durante tutta la durata dell'applicazione.

## In conclusione

Come al solito ci sono un paio di modi per scuoiare un gatto. L'approccio interfaccia è probabilmente il modo più semplice per garantire che il vostro `IHostedService` è un'unica istanza. Ma l'approccio metodo di fabbrica è anche un buon modo per garantire che il vostro servizio è una singola istanza. Dipende da te quale approccio scegliere. Spero che questo articolo vi ha aiutato a capire come garantire che il vostro `IHostedService` è un'unica istanza.