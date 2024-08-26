# Σφάλματα χειρισμού (μη χειριστεί) στο πυρήνα ASP.NET

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-17T02:00</datetime>

## Εισαγωγή

Σε κάθε web εφαρμογή είναι σημαντικό να χειριστεί τα λάθη με χάρη. Αυτό ισχύει ιδιαίτερα σε ένα περιβάλλον παραγωγής όπου θέλετε να παρέχετε μια καλή εμπειρία χρήστη και να μην εκθέσετε καμία ευαίσθητη πληροφορία. Σε αυτό το άρθρο θα δούμε πώς να χειριστεί τα λάθη σε μια εφαρμογή ASP.NET πυρήνα.

[TOC]

## Το Πρόβλημα

Όταν μια μη χειριζόμενη εξαίρεση εμφανίζεται σε μια εφαρμογή ASP.NET Core, η προεπιλεγμένη συμπεριφορά είναι να επιστρέψει μια γενική σελίδα σφάλματος με κωδικό κατάστασης 500. Αυτό δεν είναι ιδανικό για διάφορους λόγους:

1. Είναι άσχημο και δεν παρέχει μια καλή εμπειρία χρήστη.
2. Δεν παρέχει καμία χρήσιμη πληροφορία στον χρήστη.
3. Συχνά είναι δύσκολο να αποσφαλματωθεί το θέμα, επειδή το μήνυμα λάθους είναι τόσο γενικό.
4. Είναι άσχημο; η γενική σελίδα σφάλματος browser είναι απλά μια γκρι οθόνη με κάποιο κείμενο.

## Η Λύση

Στο ASP.NET Core υπάρχει ένα τακτοποιημένο feature build στο οποίο μας επιτρέπει να χειριστούμε αυτά τα λάθη.

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

Βάζουμε αυτό στο δικό μας `Program.cs` Φάκελος νωρίς στον αγωγό. Αυτό θα πιάσει κάθε κωδικό κατάστασης που δεν είναι 200 και να ανακατευθύνονται στο `/error` διαδρομή με τον κωδικό κατάστασης ως παράμετρο.

Ο ελεγκτής λάθους μας θα μοιάζει κάπως έτσι:

```csharp
    [Route("/error/{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        // Retrieve the original request information
        var statusCodeReExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        
        if (statusCodeReExecuteFeature != null)
        {
            // Access the original path and query string that caused the error
            var originalPath = statusCodeReExecuteFeature.OriginalPath;
            var originalQueryString = statusCodeReExecuteFeature.OriginalQueryString;

            
            // Optionally log the original URL or pass it to the view
            ViewData["OriginalUrl"] = $"{originalPath}{originalQueryString}";
        }

        // Handle specific status codes and return corresponding views
        switch (statusCode)
        {
            case 404:
            return View("NotFound");
            case 500:
            return View("ServerError");
            default:
            return View("Error");
        }
    }
```

Αυτός ο ελεγκτής θα χειριστεί το σφάλμα και θα επιστρέψει μια προσαρμοσμένη άποψη με βάση τον κωδικό κατάστασης. Μπορούμε επίσης να καταγράψουμε το αρχικό URL που προκάλεσε το σφάλμα και να το περάσουμε στην προβολή.
Αν είχαμε μια κεντρική υπηρεσία καταγραφής / ανάλυσης θα μπορούσαμε να καταγράψουμε αυτό το σφάλμα σε αυτή την υπηρεσία.

Οι απόψεις μας είναι οι εξής:

```razor
<h1>404 - Page not found</h1>

<p>Sorry that Url doesn't look valid</p>
@section Scripts {
    <script>
            document.addEventListener('DOMContentLoaded', function () {
                if (!window.hasTracked) {
                    umami.track('404', { page:'@ViewData["OriginalUrl"]'});
                    window.hasTracked = true;
                }
            });

    </script>
}
```

Αρκετά απλό, σωστά; Μπορούμε επίσης να καταγράψουμε το σφάλμα σε μια υπηρεσία καταγραφής όπως το Application Insights ή Serilog. Με αυτόν τον τρόπο μπορούμε να παρακολουθούμε τα λάθη και να τα φτιάχνουμε πριν γίνουν πρόβλημα.
Στην περίπτωσή μας το καταγράφουμε ως γεγονός στην υπηρεσία ανάλυσης Umami. Με αυτόν τον τρόπο μπορούμε να παρακολουθούμε πόσα 404 λάθη έχουμε και από πού προέρχονται.

Αυτό διατηρεί επίσης τη σελίδα σας σύμφωνα με την επιλεγμένη διάταξη και σχεδιασμό σας.

![404 Σελίδα](new404.png)

## Συμπέρασμα

Αυτός είναι ένας απλός τρόπος για να χειριστείτε τα λάθη σε μια εφαρμογή ASP.NET Core. Παρέχει μια καλή εμπειρία χρήστη και μας επιτρέπει να παρακολουθούμε τα λάθη. Είναι μια καλή ιδέα να συνδεθείτε λάθη σε μια υπηρεσία καταγραφής, ώστε να μπορείτε να τα παρακολουθείτε και να τα διορθώσετε πριν γίνουν πρόβλημα.