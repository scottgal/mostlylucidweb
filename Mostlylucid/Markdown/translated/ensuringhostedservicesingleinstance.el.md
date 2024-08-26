# Η διασφάλιση του IHostedService (ή IHostedLifecycleService) είναι μια μοναδική περίπτωση

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-22T16:08</datetime>

## Εισαγωγή

Αυτό είναι ένα ηλίθιο μικρό άρθρο γιατί ήμουν λίγο μπερδεμένος σχετικά με το πώς να εξασφαλίσει ότι `IHostedService` ήταν μια μοναδική περίπτωση. Νόμιζα ότι ήταν λίγο πιο περίπλοκο απ' ό,τι ήταν. Σκέφτηκα να γράψω ένα άρθρο γι' αυτό. Σε περίπτωση που κάποιος άλλος ήταν μπερδεμένος με αυτό.

Στην [προηγούμενο άρθρο](/blog/addingasyncsendingforemails), καλύψαμε πώς να δημιουργήσετε μια υπηρεσία υποβάθρου χρησιμοποιώντας το `IHostedService` interface για την αποστολή μηνυμάτων ηλεκτρονικού ταχυδρομείου. Το άρθρο αυτό θα καλύψει τον τρόπο με τον οποίο θα διασφαλιστεί ότι η `IHostedService` είναι μια μοναδική περίπτωση.
Αυτό μπορεί να είναι προφανές σε κάποιους, αλλά δεν είναι για άλλους (και δεν ήταν αμέσως για μένα!).

[TOC]

## Γιατί είναι θέμα αυτό;

Είναι ένα ζήτημα όπως τα περισσότερα από τα άρθρα αυτά καλύπτουν πώς να χρησιμοποιήσετε ένα `IHostedService` Αλλά δεν καλύπτουν πώς να διασφαλίσουν ότι η υπηρεσία είναι μια ενιαία περίπτωση. Αυτό είναι σημαντικό καθώς δεν θέλετε πολλαπλές περιπτώσεις λειτουργίας της υπηρεσίας ταυτόχρονα.

Τι εννοώ; Λοιπόν στο ASP.NET ο τρόπος για να καταχωρήσετε ένα IHostedService ή IHostedlifeCycleService (βασικά το ίδιο με περισσότερες υπερβάσεις για τη διαχείριση κύκλου ζωής) που χρησιμοποιείτε αυτό

```csharp
  services.AddHostedService(EmailSenderHostedService);
```

Αυτό που κάνει είναι να καλεί σε αυτόν τον κώδικα υποστήριξης:

```csharp
public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

```

Το οποίο είναι ωραίο και δαντελωτό αλλά τι γίνεται αν θέλετε να δημοσιεύσετε ένα νέο μήνυμα απευθείας σε αυτή την υπηρεσία από ας πούμε `Controller` Δράση;

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

Είτε θα πρέπει να δημιουργήσετε μια διεπαφή που η ίδια εφαρμόζει `IHostedService` στη συνέχεια καλέστε τη μέθοδο σχετικά με αυτό ή θα πρέπει να διασφαλίσετε ότι η υπηρεσία είναι μια ενιαία περίπτωση. Ο τελευταίος είναι ο ευκολότερος τρόπος για να γίνει αυτό (ανάλογα με το σενάριο σας, όμως, για τη δοκιμή της μεθόδου Διεπαφή μπορεί να προτιμάται).

### IHostedService

Θα σημειώσετε εδώ ότι καταγράφει την υπηρεσία ως `IHostedService`, αυτό έχει να κάνει με τη διαχείριση του κύκλου ζωής αυτής της υπηρεσίας, καθώς το πλαίσιο ASP.NET θα χρησιμοποιήσει αυτή την εγγραφή για να πυροδοτήσει τα γεγονότα αυτής της υπηρεσίας (`StartAsync` και `StopAsync` για το IHostedService). Βλέπε παρακάτω, `IHostedlifeCycleService` είναι απλά μια πιο λεπτομερής έκδοση του IHostedService.

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

## Πώς να διασφαλίσετε ότι το IHostedService σας είναι μια ενιαία περίπτωση

### Προσέγγιση διεπαφής

Η προσέγγιση Interface μπορεί να είναι απλούστερη ανάλογα με το σενάριο σας. Εδώ θα προσθέσετε μια διεπαφή που κληρονομεί από `IHostedService` και στη συνέχεια να προσθέσετε μια μέθοδο σε αυτή τη διεπαφή που μπορείτε να καλέσετε από το χειριστήριο σας.

**ΣΗΜΕΙΩΣΗ: Θα πρέπει ακόμα να το προσθέσετε ως HostedService στο ASP.NET για την υπηρεσία να τρέξει πραγματικά.**

```csharp
    public interface IEmailSenderHostedService : IHostedService, IDisposable
    {
        Task SendEmailAsync(BaseEmailModel message);
    }
```

Το μόνο που χρειαζόμαστε είναι να το καταγράψουμε ως μονότονο και στη συνέχεια να το χρησιμοποιήσουμε στο χειριστήριο μας.

```csharp
             services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
        services.AddHostedService<IEmailSenderHostedService>(provider => provider.GetRequiredService<IEmailSenderHostedService>());
        
```

ASP.NET θα δείτε ότι αυτό έχει τη σωστή διεπαφή διακοσμημένο και θα χρησιμοποιήσετε αυτή την εγγραφή για να εκτελέσετε το `IHostedService`.

### Προσέγγιση της μεθόδου του εργοστασίου

Μια άλλη για να εξασφαλίσει ότι `IHostedService` είναι μία μόνο περίπτωση είναι να χρησιμοποιήσετε το `AddSingleton` μέθοδος για την εγγραφή της υπηρεσίας σας στη συνέχεια περάστε το `IHostedService` καταχώριση ως "εφάπαξ μέθοδος." Αυτό θα διασφαλίσει ότι μόνο μία περίπτωση της υπηρεσίας σας δημιουργείται και χρησιμοποιείται καθ' όλη τη διάρκεια ζωής της εφαρμογής.

* Α *εργοστάσιο* Η μέθοδος είναι απλά ένας φανταχτερός τρόπος να πεις μια μέθοδο που δημιουργεί μια περίπτωση ενός αντικειμένου.

```csharp
        services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

Έτσι όπως βλέπετε εδώ... καταγράφω για πρώτη φορά... `IHostedService` (ή `IHostedLifeCycleService`) ως singleton και στη συνέχεια χρησιμοποιώ το `AddHostedService` μέθοδος εγγραφής της υπηρεσίας ως μεθόδου εργοστασίου. Αυτό θα διασφαλίσει ότι μόνο μία περίπτωση της υπηρεσίας δημιουργείται και χρησιμοποιείται καθ' όλη τη διάρκεια ζωής της εφαρμογής.

## Συμπέρασμα

Ως συνήθως, υπάρχουν δυο τρόποι να γδάρεις μια γάτα.  Η προσέγγιση της μεθόδου του εργοστασίου είναι επίσης ένας καλός τρόπος για να διασφαλιστεί ότι η υπηρεσία σας είναι μια ενιαία περίπτωση. Εξαρτάται από σένα ποια προσέγγιση παίρνεις. Ελπίζω ότι αυτό το άρθρο σας βοήθησε να καταλάβετε πώς να διασφαλίσετε ότι `IHostedService` είναι μια μοναδική περίπτωση.