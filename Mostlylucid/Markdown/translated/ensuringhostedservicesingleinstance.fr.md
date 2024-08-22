# Assurer votre service IHostedService (ou IHostedLifecycleService) est une seule instance

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-21T16:08</datetime>

## Présentation

C'est un petit article stupide parce que j'étais un peu confus sur la façon de s'assurer que mon `IHostedService` a été un cas unique. Je pensais que c'était un peu plus compliqué qu'en fait. Alors j'ai pensé écrire un petit article à ce sujet. Juste au cas où quelqu'un d'autre serait confus à ce sujet.

Dans le [article précédent](/blog/addingasyncsendingforemails), nous avons couvert la façon de créer un service d'arrière-plan en utilisant le `IHostedService` interface pour l'envoi d'emails. Cet article traitera de la façon de s'assurer que votre `IHostedService` est une seule instance.
Cela pourrait être évident pour certains, mais ce n'est pas pour d'autres (et ce n'était pas tout de suite pour moi!).............................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................

[TOC]

## Pourquoi est-ce un problème?

Eh bien, son un problème que la plupart des articles sur ceux-ci couvrent comment utiliser un `IHostedService` mais ils ne couvrent pas comment s'assurer que le service est une seule instance. Ceci est important car vous ne voulez pas plusieurs instances du service en cours d'exécution en même temps.

Qu'est-ce que je veux dire? Eh bien dans ASP.NET la façon de register un IHostedService ou IHostedlifeCycleService (essentiellement la même avec plus de dérogations pour la gestion du cycle de vie) vous utilisez ce

```csharp
  services.AddHostedService(EmailSenderHostedService);
```

Ce que cela fait est d'appeler dans ce code de moteur:

```csharp
public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

```

Ce qui est bien et dandy mais que faire si vous voulez poster un nouveau message directement à ce service de dire un `Controller` L'action?

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

Soit vous devez créer une interface qui s'implémente elle-même `IHostedService` puis appelez la méthode sur cela ou vous devez vous assurer que le service est une seule instance. Ce dernier est le moyen le plus simple de le faire (dépend de votre scénario cependant, pour tester la méthode Interface pourrait être préféré).

### Service d'hébergement

Vous noterez ici qu'il enregistre le service comme un `IHostedService`, ceci est lié à la gestion du cycle de vie de ce service puisque le cadre ASP.NET utilisera cette inscription pour déclencher les événements de ce service (`StartAsync` et `StopAsync` pour IHostedService). Voir ci-dessous. `IHostedlifeCycleService` est juste une version plus détaillée de IHostedService.

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

## Comment s'assurer que votre service IHosted est une instance unique

### Approche de l'interface

L'approche Interface pourrait être plus simple selon votre scénario. Ici vous ajouteriez une interface qui hérite de `IHostedService` puis ajouter une méthode à cette interface que vous pouvez appeler depuis votre contrôleur.

```csharp
    public interface IEmailSenderHostedService : IHostedService
    {
        Task SendEmailAsync(BaseEmailModel message);
        void Dispose();
    }
```

Tout ce dont nous avons besoin, c'est de l'enregistrer comme un simpleton, puis de l'utiliser dans notre contrôleur.

```csharp
        services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
```

ASP.NET verra que cela a la bonne interface décorée et utilisera cette inscription pour exécuter le `IHostedService`.

### Méthode d'usine

Un autre pour s'assurer que votre `IHostedService` est une seule instance est d'utiliser le `AddSingleton` méthode d'enregistrement de votre service puis passer le `IHostedService` l'enregistrement en tant que «méthode d'usine». Cela permettra de s'assurer qu'une seule instance de votre service est créée et utilisée tout au long de la durée de vie de l'application.

* A. Le rôle de l'Organisation des Nations Unies dans le domaine de l'éducation, de la science et de la culture *usine* méthode est juste une façon fantaisiste de dire une méthode qui crée une instance d'un objet.

```csharp
        services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Donc, comme vous le voyez ici, j'inscris d'abord mon `IHostedService` (ou `IHostedLifeCycleService`) comme un simpleton et puis j'utilise le `AddHostedService` méthode d'enregistrement du service comme méthode d'usine. Cela permettra de s'assurer qu'une seule instance du service est créée et utilisée tout au long de la durée de vie de l'application.

## En conclusion

Comme d'habitude, il y a deux façons de peler un chat. L'approche d'interface est probablement la façon la plus facile de s'assurer que votre `IHostedService` est une seule instance. Mais l'approche de la méthode d'usine est aussi un bon moyen de s'assurer que votre service est une seule instance. C'est à vous de décider de l'approche que vous prenez. J'espère que cet article vous aidera à comprendre comment vous assurer que votre `IHostedService` est une seule instance.