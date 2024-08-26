# IHostedServicen (tai IHostedLifecycleService) varmistaminen on yksi esimerkki

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-22T16:08</datetime>

## Johdanto

Tämä on typerä pikku artikkeli, koska olin hieman hämmentynyt siitä, miten varmistaa, että `IHostedService` Se oli yksittäinen tapaus. Minusta se oli hieman monimutkaisempaa kuin todellisuudessa oli. Joten ajattelin kirjoittaa siitä pienen artikkelin. Siltä varalta, että joku muu olisi hämmentynyt asiasta.

• • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • [etukäteisartikkeli](/blog/addingasyncsendingforemails)Kävimme läpi, miten luodaan taustapalvelu, jossa käytetään `IHostedService` Liitäntä sähköpostien lähettämiseen. Tässä artikkelissa kerrotaan, miten varmistetaan, että `IHostedService` Se on yksittäinen tapaus.
Tämä voi olla selvää joillekin, mutta ei muille (eikä heti minulle!).

[TÄYTÄNTÖÖNPANO

## Miksi tämä on ongelma?

No se on numero, koska suurin osa artikkeleista näistä kattaa, miten käyttää `IHostedService` Ne eivät kuitenkaan kata, miten varmistetaan, että palvelu on yksittäinen tapaus. Tämä on tärkeää, koska et halua, että palvelu toimii useita kertoja samaan aikaan.

Mitä tarkoitan? No ASP.NETissä tapa rekisteröidä IHostedService tai IHostedlifeCycleService (periaatteessa sama, kun elinkaaren hallintaan käytetään useampia ohituksia) käytät tätä

```csharp
  services.AddHostedService(EmailSenderHostedService);
```

Se vetoaa tähän backend-koodiin:

```csharp
public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

```

Mikä on hienoa ja hienoa, mutta entä jos haluat lähettää uuden viestin suoraan tälle palvelulle: `Controller` Toimintaa?

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

Joko sinun täytyy luoda käyttöliittymä, joka itse toteuttaa `IHostedService` sen jälkeen kehota menetelmään, tai sinun täytyy varmistaa, että palvelu on vain yksi instanssi. Jälkimmäinen on helpoin tapa tehdä tämä (riippuu kuitenkin skenaariostasi, sillä rajapintamenetelmän testaaminen voi olla parempi vaihtoehto).

### IHostedService

Huomaat tässä, että se rekisteröi palvelun `IHostedService`, tämä liittyy tämän palvelun elinkaaren hallintaan, koska ASP.net-järjestelmässä käytetään tätä rekisteröintiä tämän palvelun tapahtumien laukaisemiseen (`StartAsync` sekä `StopAsync` IHostedServicelle). Ks. jäljempänä `IHostedlifeCycleService` on vain yksityiskohtaisempi versio IHostedServicestä.

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

## Miten varmistat, että IHostedService on yksi ainoa tapaus?

### Liitäntälähestymistapa

Liitännäislähestymistapa voi olla yksinkertaisempi skenaariostasi riippuen. Tähän lisättäisiin rajapinta, joka periytyy `IHostedService` ja lisää sitten rajapintaan menetelmä, jonka voit soittaa ohjaimesta.

**HUOMAUTUS: Sinun täytyy vielä lisätä se HostedServiceksi ASP.NETissä, jotta palvelu todella toimii.**

```csharp
    public interface IEmailSenderHostedService : IHostedService, IDisposable
    {
        Task SendEmailAsync(BaseEmailModel message);
    }
```

Sitten meidän tarvitsee vain rekisteröidä tämä singletoniksi ja sitten käyttää tätä ohjaimessamme.

```csharp
             services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
        services.AddHostedService<IEmailSenderHostedService>(provider => provider.GetRequiredService<IEmailSenderHostedService>());
        
```

ASP.NET näkee, että tämä on oikea rajapinta koristeltu ja käyttää tätä rekisteröintiä ajaa `IHostedService`.

### Factory Method -lähestymistapa

Toinen varmistaa, että `IHostedService` äm ä ä ä ä ä ä ä ä ä ä ä ä ä ä ä ä ä ä ä ä n `AddSingleton` tapa rekisteröidä palvelusi, jonka jälkeen ohitat `IHostedService` ilmoittautuminen "tehdasmenetelmäksi". Näin varmistetaan, että vain yksi esimerkki palvelustasi luodaan ja käytetään koko sovelluksen käyttöiän ajan.

* A *tehdas* Menetelmä on vain hieno tapa sanoa menetelmä, joka luo esimerkin esineestä.

```csharp
        services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Kuten näette täällä, rekisteröin ensin omani. `IHostedService` (tai `IHostedLifeCycleService`) singleton ja sitten käytän `AddHostedService` menetelmä, jolla palvelu rekisteröidään tehdasmenetelmäksi. Näin varmistetaan, että vain yksi palveluesimerkki luodaan ja käytetään koko sovelluksen käyttöiän ajan.

## Johtopäätöksenä

Kuten tavallista, kissan voi nylkeä parilla tavalla.  Tehdasmenetelmä on myös hyvä tapa varmistaa, että palvelusi on yhtä ainoaa. Se on sinusta kiinni, minkä lähestymistavan valitset. Toivon, että tämä artikkeli on auttanut sinua ymmärtämään, miten varmistaa, että `IHostedService` Se on yksittäinen tapaus.