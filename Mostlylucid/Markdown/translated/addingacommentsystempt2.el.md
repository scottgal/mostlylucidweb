# Προσθήκη ενός συστήματος σχολίων μέρος 2 - Αποταμίευση Σχόλια

<!--category-- ASP.NET, Alpine.js, HTMX  -->
<datetime class="hidden">2024-08-31T09:00</datetime>

# Εισαγωγή

Κατά την piροηγούενη piροηγούενη [μέρος αυτής της σειράς](/blog/addingacommentsystempt1), έφτιαξα τη βάση δεδομένων για το σύστημα σχολίων. Σε αυτή τη δημοσίευση, θα καλύψω το πώς η εξοικονόμηση των σχολίων διαχειρίζεται την πλευρά του πελάτη και στο ASP.NET Core.

[TOC]

## Προσθήκη νέου σχολίου

### `_CommentForm.cshtml`

Αυτή είναι μια μερική άποψη Razor που περιέχει τη μορφή για την προσθήκη ενός νέου σχολίου. Μπορείς να δεις με το πρώτο φορτίο που καλεί `window.mostlylucid.comments.setup()` που αρχικoπoιεί τoν εκδότη. Αυτή είναι μια απλή περιοχή κειμένου που χρησιμοποιεί το `SimpleMDE` επεξεργαστής για να επιτρέψει την επεξεργασία πλούσιων κειμένων.

```razor
@model Mostlylucid.Models.Comments.CommentInputModel

 
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
    <section id="commentsection" ></section>
    
    <input type="hidden" asp-for="BlogPostId" />
    <div asp-validation-summary="All" class="text-red-500" id="validationsummary"></div>
    <p class="font-body text-lg font-medium text-primary dark:text-white pb-8">Welcome @Model.Name please comment below.</p>
    
    <div asp-validation-summary="All" class="text-red-500" id="validationsummary"></div>
    <!-- Username Input -->
    <div class="flex space-x-4"> <!-- Flexbox to keep Name and Email on the same line -->

        <!-- Username Input -->
        <label class="input input-bordered flex items-center gap-2 mb-2 dark:bg-custom-dark-bg bg-white w-1/2">
            <i class='bx bx-user'></i>
            <input type="text" class="grow text-black dark:text-white bg-transparent border-0"
                   asp-for="Name" placeholder="Name (required)" />
        </label>

        <!-- Email Input -->
        <label class="input input-bordered flex items-center gap-2 mb-2 dark:bg-custom-dark-bg bg-white w-1/2">
            <i class='bx bx-envelope'></i>
            <input type="email" class="grow text-black dark:text-white bg-transparent border-0"
                   asp-for="Email" placeholder="Email (optional)" />
        </label>

    </div>

    <textarea id="commenteditor" class="hidden w-full h-44 dark:bg-custom-dark-bg bg-white text-black dark:text-white rounded-2xl"></textarea>

    <input type="hidden" asp-for="ParentId"></input>
    <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
</div>
```

Εδώ χρησιμοποιούμε τα Alpine.js `x-init` Τηλεφώνημα για την αρχικοποίηση του εκδότη. Αυτή είναι μια απλή περιοχή κειμένου που χρησιμοποιεί το `SimpleMDE` επεξεργαστής για να επιτρέψει την επεξεργασία πλούσιων κειμένων (γιατί όχι :)).

```html
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
```

#### `window.mostlylucid.comments.setup()`

Αυτό ζει στο... `comment.js` και είναι υπεύθυνος για την αρχικοποίηση του απλού επεξεργαστή MDE.

```javascript
    function setup ()  {
    if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
        mostlylucid.simplemde.initialize('commenteditor', true);
    } else {
        console.error("simplemde is not initialized correctly.");
    }
};
```

Αυτό είναι μια απλή λειτουργία που ελέγχει αν `simplemde` το αντικείμενο είναι αρχικοποιημένο και αν ναι καλεί το `initialize` Πήγαινε πάνω του.

## Αποθήκευση του σχολίου

Για να αποθηκεύσετε το σχόλιο χρησιμοποιούμε HTMX για να κάνουμε μια POST στο `CommentController` που στη συνέχεια αποθηκεύει το σχόλιο στη βάση δεδομένων.

```razor
  <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
```

Αυτό χρησιμοποιεί το [Βοηθός ετικετών HTMX](https://www.nuget.org/packages/Htmx.TagHelpers) για να στείλετε πίσω στο `CommentController` και στη συνέχεια να ανταλλάξει το έντυπο με το νέο σχόλιο.

Στη συνέχεια, μπορούμε να συνδεθούμε με το `mostlylucid.comments.setValues($event)` που χρησιμοποιούμε για να κατοικήσουμε το `hx-values` atribution (αυτό είναι απαραίτητο μόνο εφόσον το simplemde πρέπει να ενημερώνεται χειροκίνητα).

```javascript
    function setValues (evt)  {
    const button = evt.currentTarget;
    const element = mostlylucid.simplemde.getinstance('commenteditor');
    const content = element.value();
    const email = document.getElementById("Email");
    const name = document.getElementById("Name");
    const blogPostId = document.getElementById("BlogPostId");

    const parentId = document.getElementById("ParentId")
    const values = {
        content: content,
        email: email.value,
        name: name.value,
        blogPostId: blogPostId.value,
        parentId: parentId.value
    };

    button.setAttribute('hx-vals', JSON.stringify(values));
};
}
```

### Σχόλιο Controller

Ο ελεγκτής σχολίων `save-comment` η δράση είναι υπεύθυνη για την αποθήκευση του σχολίου στη βάση δεδομένων. Στέλνει επίσης ένα email στον ιδιοκτήτη του blog (εγώ) όταν προστίθεται ένα σχόλιο.

```csharp
    [HttpPost]
    [Route("save-comment")]
    public async Task<IActionResult> Comment([Bind(Prefix = "")] CommentInputModel model )
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_CommentForm", model);
        }
        var postId = model.BlogPostId;
        ;
        var name = model.Name ?? "Anonymous";
        var email = model.Email ?? "Anonymous";
        var comment = model.Content;

        var parentCommentId = model.ParentId;
        
      var htmlContent=  await commentService.Add(postId, parentCommentId, name, comment);
      if (string.IsNullOrEmpty(htmlContent))
      {
          ModelState.AddModelError("Content", "Comment could not be saved");
          return PartialView("_CommentForm", model);
      }
        var slug = await blogService.GetSlug(postId);
        var url = Url.Action("Show", "Blog", new {slug }, Request.Scheme);
        var commentModel = new CommentEmailModel
        {
            SenderEmail = email ?? "",
            Comment = htmlContent,
            PostUrl = url??string.Empty,
        };
        await sender.SendEmailAsync(commentModel);
        model.Content = htmlContent;
        return PartialView("_CommentResponse", model);
    }
```

Θα δείτε ότι αυτό κάνει μερικά πράγματα:

1. Προσθέτει το σχόλιο στο DB (αυτό κάνει επίσης μια μετατροπή MarkDig για να μετατρέψει markdown σε HTML).
2. Αν υπάρχει λάθος, επιστρέφει τη φόρμα με το λάθος. (Σημείωση Έχω επίσης τώρα μια δραστηριότητα εντοπισμού που καταγράφει το σφάλμα σε Seq).
3. Αν το σχόλιο είναι αποθηκευμένο, στέλνει ένα email σε μένα με το σχόλιο και το post URL.

This post URL then let me click the post, if I'm logged in as me (using) [το Google Auth πράγμα μου](/blog/addingidentityfreegoogleauth)). Αυτό απλά ελέγχει για το Google ID μου στη συνέχεια θέτει το 'IsAdmin' ιδιοκτησία που μου επιτρέπει να δω τα σχόλια και να τα διαγράψω αν είναι απαραίτητο.

# Συμπέρασμα

Αυτό είναι το δεύτερο μέρος, πώς θα σώσω τα σχόλια. Λείπει ακόμα ένα ζευγάρι κομμάτια; κλωστή (ώστε να μπορείτε να απαντήσετε σε ένα σχόλιο), στην λίστα των δικών σας σχολίων και να διαγράψετε τα σχόλια. Θα τα καλύψω στην επόμενη θέση.