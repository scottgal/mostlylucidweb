# Ένα σούπερ απλό σύστημα σχολίων με Markdown

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-06T18:50</datetime>

ΣΗΜΕΙΩΣΗ: ΕΡΓΑΣΙΑ ΣΕ ΠΡΟΟΔΟ

Έψαχνα για ένα απλό σύστημα σχολίων για το blog μου που χρησιμοποιεί Markdown. Δεν μπορούσα να βρω ένα που μου άρεσε, έτσι αποφάσισα να γράψω το δικό μου. Αυτό είναι ένα απλό σύστημα σχολίων που χρησιμοποιεί Markdown για τη μορφοποίηση. Το δεύτερο μέρος αυτού θα προσθέσει ειδοποιήσεις ηλεκτρονικού ταχυδρομείου στο σύστημα το οποίο θα μου στείλει ένα email με ένα σύνδεσμο με το σχόλιο, επιτρέποντάς μου να το "εγκρίνω" πριν εμφανιστεί στην ιστοσελίδα.

Και πάλι για ένα σύστημα παραγωγής αυτό θα χρησιμοποιούσε κανονικά μια βάση δεδομένων, αλλά για αυτό το παράδειγμα απλά θα χρησιμοποιήσω το σήμα.

## Το Σύστημα Σχόλιας

Το σύστημα σχολίων είναι απίστευτα απλό. Έχω μόνο ένα αρχείο markdown που αποθηκεύονται για κάθε σχόλιο με το όνομα, το email και το σχόλιο του χρήστη. Τα σχόλια στη συνέχεια εμφανίζονται στη σελίδα με τη σειρά που παραλήφθηκαν.

Για να εισάγετε το σχόλιο Χρησιμοποιώ το SimpleMDE, ένα επεξεργαστή Javascript βασισμένο Markdown.
Αυτό περιλαμβάνεται στο... _Διάταξη.cshtml ως εξής:

```html
<!-- Include the SimpleMDE CSS, here I use a dark theme -->
  <link rel="stylesheet" href="https://cdn.rawgit.com/xcatliu/simplemde-theme-dark/master/dist/simplemde-theme-dark.min.css">

<!--Later in the page include the JS for SimpleMDE -->
<script src="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.js"></script>

```

Στη συνέχεια αρχικοποιώ τον επεξεργαστή SimpleMDE τόσο στο φορτίο σελίδων όσο και στο φορτίο HTMX:

```javascript
    var simplemde;
    document.addEventListener('DOMContentLoaded', function () {
    
        if (document.getElementById("comment") != null)
        {
        
       simplemde = new SimpleMDE({ element: document.getElementById("comment") });
       }
        
    });
    document.body.addEventListener('htmx:afterSwap', function(evt) {
        if (document.getElementById("comment") != null)
        {
        simplemde = new SimpleMDE({ element: document.getElementById("comment") });
        
        }
    });
```

Εδώ διευκρινίζω ότι η περιοχή του σχολίου μου ονομάζεται "σχόλιο" και αρχικοποιείται μόνο όταν ανιχνεύεται. Εδώ τυλίγω τη φόρμα σε 'IsAuthenticated' (την οποία περνάω στο ViewModel). Αυτό σημαίνει ότι μπορώ να διασφαλίσω ότι μόνο εκείνοι που έχουν συνδεθεί (προς το παρόν με την Google) μπορούν να προσθέσουν σχόλια.

```razor
@if (Model.Authenticated)
    {
        
  
        <div class=" max-w-none border-b border-grey-lighter py-8 dark:prose-dark sm:py-12">
            <p class="font-body text-lg font-medium text-primary dark:text-white">Welcome @Model.Name please comment below.</p>
            <textarea id="comment"></textarea>
       <button class="btn btn-primary" hx-action="Comment" hx-controller="Blog" hx-post hx-vals="js:{comment: simplemde.value()}" hx-route-slug="@Model.Slug" hx-swap="outerHTML" hx-target="#comment" onclick="prepareForSubmission()">Comment</button>
        </div>
    }
    else
    {
       ...
    }
```

Θα παρατηρήσετε επίσης ότι χρησιμοποιώ HTMX εδώ για την ανάρτηση σχολίων. Όπου χρησιμοποιώ το χαρακτηριστικό hx-vals και μια κλήση JS για να πάρει την τιμή για το σχόλιο. Αυτό στη συνέχεια αναρτάται στο χειριστήριο του Blog με τη δράση 'Σχόλιο'. Αυτό στη συνέχεια αλλάζει με το νέο σχόλιο.

```csharp
    [HttpPost]
    [Route("comment")]
    [Authorize]
    public async Task<IActionResult> Comment(string slug, string comment)
    {
        var principal = HttpContext.User;
        principal.Claims.ToList().ForEach(c => logger.LogInformation($"{c.Type} : {c.Value}"));
        var nameIdentifier = principal.FindFirst("sub");
        var userInformation = GetUserInfo();
       await commentService.AddComment(slug, userInformation, comment, nameIdentifier.Value);
        return RedirectToAction(nameof(Show), new { slug });
    }

```