# Htmx με Asp.Net Core

<datetime class="hidden">2024-08-01T03:42</datetime>

<!--category-- ASP.NET, HTMX -->
## Εισαγωγή

Χρησιμοποιώντας HTMX με ASP.NET Core είναι ένας εξαιρετικός τρόπος για να οικοδομήσουμε δυναμικές εφαρμογές ιστού με minimal JavaScript. HTMX σας επιτρέπει να ενημερώσετε τα μέρη της σελίδας σας χωρίς πλήρη επαναφόρτωση σελίδας, κάνοντας την εφαρμογή σας να αισθάνεται πιο αποτελεσματική και διαδραστική.

Είναι αυτό που συνήθιζα να αποκαλώ 'hybrid' web design όπου κάνετε τη σελίδα πλήρως χρησιμοποιώντας τον κωδικό server-side και στη συνέχεια να χρησιμοποιήσετε HTMX για να ενημερώσετε τα μέρη της σελίδας δυναμικά.

Σε αυτό το άρθρο, θα σας δείξω πώς να ξεκινήσετε με HTMX σε μια εφαρμογή ASP.NET Core.

[TOC]

## Προαπαιτούμενα

HTMX - Htmx είναι ένα πακέτο JavaScript ο ευκολότερος τρόπος για να το συμπεριλάβετε στο έργο σας είναι να χρησιμοποιήσετε ένα CDN. (Βλέπε [Ορίστε.](https://htmx.org/docs/#installing) )

```html
<script src="https://unpkg.com/htmx.org@2.0.1" integrity="sha384-QWGpdj554B4ETpJJC9z+ZHJcA/i59TyjxEPXiiUgN2WmTyV5OEZWCD6gQhgkdpB/" crossorigin="anonymous"></script>
```

Μπορείτε φυσικά να κατεβάσετε ένα αντίγραφο και να το συμπεριλάβετε "χειροκίνητα" (ή να χρησιμοποιήσετε LibMan ή npm).

## ASP.NET Bits

Συνιστώ επίσης την εγκατάσταση του Htmx Tag Helper από [Ορίστε.](https://github.com/khalidabuhakmeh/Htmx.Net)

Αυτά είναι και τα δύο από την υπέροχη [Khalid Abuhakmeh
](https://mastodon.social/@khalidabuhakmeh@mastodon.social)

```shell
dotnet add package Htmx.TagHelpers
```

Και το Htmx Nuget πακέτο από [Ορίστε.](https://www.nuget.org/packages/Htmx/)

```shell
 dotnet add package Htmx
```

Ο βοηθός ετικετών σάς επιτρέπει να το κάνετε αυτό:

```razor
    <a hx-controller="Blog" hx-action="Show" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-slug="@Model.Slug"
        class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Εναλλακτική προσέγγιση.

**ΣΗΜΕΙΩΣΗ: Αυτή η προσέγγιση έχει ένα σημαντικό μειονέκτημα· δεν παράγει ένα href για τη σύνδεση μετά τη σύνδεση. Αυτό είναι ένα πρόβλημα για SEO και προσβασιμότητα. Σημαίνει επίσης ότι αυτοί οι σύνδεσμοι θα αποτύχουν αν το HTMX για κάποιο λόγο δεν φορτώσει (CDN DO πηγαίνει προς τα κάτω).**

Μια εναλλακτική προσέγγιση είναι η χρήση του ` hx-boost="true"` χαρακτηριστικό και κανονική asp.net πυρήνα βοηθούς ετικέτας. Βλέπεις;  [Ορίστε.](https://htmx.org/docs/#hx-boost) για περισσότερες πληροφορίες σχετικά με hx-boost (αν και τα έγγραφα είναι λίγο αραιά).
Αυτό θα παράγει ένα κανονικό href αλλά θα υποκλέψει από HTMX και το περιεχόμενο φορτώθηκε δυναμικά.

Οπότε ως εξής:

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Μερικά

HTMX λειτουργεί καλά με μερική θέα. Μπορείτε να χρησιμοποιήσετε HTMX για να φορτώσετε μια μερική προβολή σε ένα δοχείο στη σελίδα σας. Αυτό είναι υπέροχο για τη φόρτωση τμημάτων της σελίδας σας δυναμικά χωρίς μια πλήρη reload σελίδα.

Σε αυτή την εφαρμογή έχουμε ένα δοχείο στο αρχείο Layout.cshtml που θέλουμε να φορτώσουμε μια μερική προβολή.

```razor
    <div class="container mx-auto" id="contentcontainer">
   @RenderBody()

    </div>
```

Κανονικά καθιστά το περιεχόμενο πλευρά του διακομιστή, αλλά χρησιμοποιώντας τον βοηθό ετικέτας HTMX σχετικά με μπορείτε να δείτε στοχεύουμε `hx-target="#contentcontainer"` η οποία θα φορτώσει τη μερική θέα στο δοχείο.

Στο έργο μας έχουμε το BlogView μερική άποψη ότι θέλουμε να φορτώσουμε στο δοχείο.

![img.png](project.png)

Στη συνέχεια, στο Blog Controller έχουμε

```csharp
    [Route("{slug}")]
    [OutputCache(Duration = 3600)]
    public IActionResult Show(string slug)
    {
       var post =  blogService.GetPost(slug);
       if(Request.IsHtmx())
       {
              return PartialView("_PostPartial", post);
       }
       return View("Post", post);
    }
```

Μπορείτε να δείτε εδώ έχουμε τη μέθοδο HTMX Request.IsHtmx(), αυτό θα επιστρέψει αληθινό εάν το αίτημα είναι αίτημα HTMX. Αν είναι να επιστρέψουμε τη μερική θέα, αν όχι θα επιστρέψουμε την πλήρη θέα.

Χρησιμοποιώντας αυτό, μπορούμε να διασφαλίσουμε ότι θα υποστηρίξουμε επίσης την άμεση εξέταση με λίγη πραγματική προσπάθεια.

Σε αυτή την περίπτωση η πλήρης άποψή μας αναφέρεται σε αυτό το μέρος:

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
ViewBag.Title = "title";
Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

Και τώρα έχουμε έναν πολύ απλό τρόπο να φορτώσουμε μερικές απόψεις στη σελίδα μας χρησιμοποιώντας HTMX.