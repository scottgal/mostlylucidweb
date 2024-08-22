# Asegurar su IHostedService (o IHostedLifecycleService) es una sola instancia

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-21T16:08</datetime>

## Introducción

Este es un pequeño artículo tonto porque estaba un poco confundido acerca de cómo asegurar que mi `IHostedService` fue una sola instancia. Pensé que era un poco más complicado de lo que era en realidad. Así que pensé en escribir un pequeño artículo al respecto. Por si acaso alguien más estaba confundido al respecto.

En el [Artículo anterior](/blog/addingasyncsendingforemails), hemos cubierto cómo crear un servicio de fondo utilizando el `IHostedService` interfaz para el envío de correos electrónicos. Este artículo cubrirá cómo asegurarse de que su `IHostedService` es una sola instancia.
Esto puede ser obvio para algunos, pero no es para otros (y no fue inmediatamente para mí!).

[TOC]

## ¿Por qué es esto un problema?

Bueno es un tema como la mayoría de los artículos de estos cubren cómo utilizar un `IHostedService` pero no cubren cómo asegurarse de que el servicio es una sola instancia. Esto es importante, ya que no desea que varias instancias del servicio se ejecuten al mismo tiempo.

¿Qué quiero decir? Bueno, en ASP.NET la forma de registrar un IHostedService o IHostedlifeCycleService (básicamente el mismo con más anulaciones para la gestión del ciclo de vida) se utiliza este

```csharp
  services.AddHostedService(EmailSenderHostedService);
```

Lo que hace es llamar a este código de backend:

```csharp
public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

```

Que está bien y dandy, pero ¿qué pasa si quieres enviar un nuevo mensaje directamente a este servicio de decir un `Controller` ¿Acción?

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

O bien es necesario crear una interfaz que se implementa `IHostedService` a continuación, llame al método en que o necesita para asegurarse de que el servicio es una sola instancia. Este último es la manera más fácil de hacer esto (depende de su escenario, sin embargo, para probar el método de interfaz podría ser preferido).

### IHostedService

Usted notará aquí que registra el servicio como un `IHostedService`, esto tiene que ver con la gestión del ciclo de vida de este servicio, ya que el marco ASP.NET utilizará este registro para despedir los eventos de este servicio (`StartAsync` y `StopAsync` para IHostedService). Véase infra, `IHostedlifeCycleService` es sólo una versión más detallada de IHostedService.

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

## Cómo asegurarse de que su IHostedService es una sola instancia

### Enfoque de interfaz

El enfoque de interfaz puede ser más simple dependiendo de su escenario. Aquí añadirías una interfaz que hereda de `IHostedService` y luego añadir un método a esa interfaz que se puede llamar desde el controlador.

```csharp
    public interface IEmailSenderHostedService : IHostedService
    {
        Task SendEmailAsync(BaseEmailModel message);
        void Dispose();
    }
```

Todo lo que necesitamos hacer es registrar esto como un singleton y luego usar esto en nuestro controlador.

```csharp
        services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
```

ASP.NET verá que esta tiene la interfaz correcta decorada y usará este registro para ejecutar el `IHostedService`.

### Método de fabricación

Otro para asegurarse de que su `IHostedService` es una sola instancia es utilizar el `AddSingleton` método para registrar su servicio y luego pasar el `IHostedService` registro como «método de fábrica». Esto asegurará que sólo una instancia de su servicio sea creada y utilizada durante toda la vida útil de la aplicación.

* A *fábrica* método es sólo una manera elegante de decir un método que crea una instancia de un objeto.

```csharp
        services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Así que como ves aquí primero registro mi `IHostedService` (o `IHostedLifeCycleService`) como un singleton y luego utilizo el `AddHostedService` método para registrar el servicio como método de fábrica. Esto garantizará que sólo se cree una instancia del servicio y se utilice durante toda la vida útil de la aplicación.

## Conclusión

Como siempre hay un par de maneras de despellejar a un gato. El enfoque de interfaz es probablemente la manera más fácil de asegurar que su `IHostedService` es una sola instancia. Pero el método de fábrica enfoque también es una buena manera de asegurar que su servicio es una sola instancia. Depende de ti qué enfoque tomes. Espero que este artículo le haya ayudado a entender cómo asegurarse de que su `IHostedService` es una sola instancia.