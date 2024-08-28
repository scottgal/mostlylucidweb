# Προσθήκη πλαισίου οντοτήτων για δημοσιεύσεις blog (μέρος 3)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-16T18:00</datetime>

Μπορείτε να βρείτε όλο τον πηγαίο κώδικα για τις δημοσιεύσεις blog στο [GitHubCity name (optional, probably does not need a translation)](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Μέρος 1 & 2 της σειράς σχετικά με την προσθήκη πλαισίου οντότητας σε έργο πυρήνα του NET.**

Μέρος 1 μπορεί να βρεθεί [Ορίστε.](/blog/addingentityframeworkforblogpostspt1).

Μέρος 2 μπορεί να βρεθεί [Ορίστε.](/blog/addingentityframeworkforblogpostspt2).

## Εισαγωγή

Στα προηγούμενα μέρη δημιουργήσαμε τη βάση δεδομένων και το πλαίσιο για τις δημοσιεύσεις μας στο blog και προσθέσαμε τις υπηρεσίες για να αλληλεπιδράσουμε με τη βάση δεδομένων. Σε αυτή τη θέση, θα εξετάσουμε λεπτομερώς πώς αυτές οι υπηρεσίες λειτουργούν τώρα με τους υφιστάμενους ελεγκτές και απόψεις.

[TOC]

## Ελεγκτές

Out ελεγκτές για Blogs είναι πραγματικά αρκετά απλό; σύμφωνα με την αποφυγή του αντιπατητή 'Fat Controller' (ένα μοτίβο που εικονίσαμε νωρίς στις ημέρες ASP.NET MVC).

### Το μοτίβο Fat Controller σε ASP.NET MVC

I MVC πλαίσια μια καλή πρακτική είναι να κάνετε όσο το δυνατόν λιγότερα στις μεθόδους ελέγχου σας. Αυτό οφείλεται στο γεγονός ότι ο υπεύθυνος επεξεργασίας είναι υπεύθυνος για τον χειρισμό του αιτήματος και την απάντηση. Δεν θα πρέπει να είναι υπεύθυνη για την επιχειρηματική λογική της εφαρμογής. Αυτή είναι η ευθύνη του μοντέλου.

Το αντιπάτερν του "Fat Controller" είναι το σημείο όπου ο ελεγκτής κάνει πάρα πολλά. Αυτό μπορεί να οδηγήσει σε ορισμένα προβλήματα, μεταξύ των οποίων:

1. Διαγραφή κώδικα σε πολλαπλές δράσεις:
   Μια δράση θα πρέπει να είναι μια ενιαία μονάδα εργασίας, απλά να κατοικήσει το μοντέλο και να επιστρέψει την άποψη. Εάν βρεθείτε να επαναλαμβάνετε τον κώδικα σε πολλαπλές ενέργειες, είναι ένα σημάδι ότι θα πρέπει να αναπαραστήσετε αυτόν τον κώδικα σε μια ξεχωριστή μέθοδο.
2. Κωδικός που είναι δύσκολο να δοκιμαστεί:
   Με το να έχετε "χοντρά χειριστήρια" μπορεί να δυσκολεύεστε να ελέγξετε τον κώδικα. Η δοκιμή θα πρέπει να προσπαθήσει να ακολουθήσει όλα τα πιθανά μονοπάτια μέσω του κώδικα, και αυτό μπορεί να είναι δύσκολο εάν ο κώδικας δεν είναι καλά δομημένος και επικεντρώνεται σε μια ενιαία ευθύνη.
3. Κωδικός που είναι δύσκολο να διατηρηθεί:
   Η διατήρηση αποτελεί βασικό μέλημα κατά την κατασκευή εφαρμογών. Έχοντας μεθόδους δράσης "νιπτήρα κουζίνας" μπορεί εύκολα να οδηγήσει σε εσάς καθώς και άλλους προγραμματιστές χρησιμοποιώντας τον κώδικα για να κάνουν αλλαγές που σπάνε άλλα μέρη της εφαρμογής.
4. Κωδικός που είναι δύσκολο να κατανοηθεί:
   Αυτό αποτελεί βασικό μέλημα για τους προγραμματιστές. Εάν εργάζεστε πάνω σε ένα έργο με μια μεγάλη βάση κώδικα, μπορεί να είναι δύσκολο να κατανοήσετε τι συμβαίνει σε μια ενέργεια ελεγκτή εάν κάνει πάρα πολλά.

### The Blog Controller

Ο ελεγκτής blog είναι σχετικά απλός. Διαθέτει 4 κύριες δράσεις (και μία "σύνθετη δράση" για τους παλιούς συνδέσμους blog). Αυτές είναι:

```csharp
Task<IActionResult> Index(int page = 1, int pageSize = 5)

Task<IActionResult> Show(string slug, string language = BaseService.EnglishLanguage)

Task<IActionResult> Category(string category, int page = 1, int pageSize = 5)

Task<IActionResult> Language(string slug, string language)

IActionResult Compat(string slug, string language)
```

Με τη σειρά τους, αυτές οι ενέργειες αποκαλούν `IBlogService` για να πάρουν τα δεδομένα που χρειάζονται. Η `IBlogService` είναι λεπτομερής στην [προηγούμενη θέση](/blog/addingentityframeworkforblogpostspt2).

Με τη σειρά τους, οι ενέργειες αυτές έχουν ως εξής:

- Δείκτης: Αυτή είναι η λίστα των αναρτήσεων blog (προκαταβολές στην αγγλική γλώσσα; μπορούμε να το επεκτείνουμε αργότερα ώστε να επιτρέψουμε για πολλαπλές γλώσσες). Θα δεις ότι χρειάζεται. `page` και `pageSize` ως παράμετροι. Αυτό είναι για σάλτσα. των αποτελεσμάτων.
- Εμφάνιση: Αυτό είναι το ατομικό blog post. Παίρνει το... `slug` της θέσης και της `language` ως παράμετροι. THis είναι η μέθοδος που χρησιμοποιείτε αυτή τη στιγμή για να διαβάσετε αυτό το blog post.
- Κατηγορία: Αυτή είναι η λίστα των αναρτήσεων blog για μια δεδομένη κατηγορία. Παίρνει το... `category`, `page` και `pageSize` ως παράμετροι.
- Γλώσσα: Αυτό δείχνει ένα blog post για μια δεδομένη γλώσσα. Παίρνει το... `slug` και `language` ως παράμετροι.
- Compat: Πρόκειται για μια δράση ευσυνειδησίας για τους παλιούς συνδέσμους blog. Παίρνει το... `slug` και `language` ως παράμετροι.

### Φύλαξη

Όπως αναφέρεται σε μια [παλαιότερη θέση](https://www.mostlylucid.net/blog/aspnetcachingwithhtmx) Εφαρμόζουμε `OutputCache` και `ResponseCahce` για να κρύψει τα αποτελέσματα των αναρτήσεων στο blog. Αυτό βελτιώνει την εμπειρία του χρήστη και μειώνει το φορτίο στο διακομιστή.

Αυτές εφαρμόζονται χρησιμοποιώντας τους κατάλληλους διακοσμητές δράσης που καθορίζουν τις παραμέτρους που χρησιμοποιούνται για τη δράση (καθώς και `hx-request` για αιτήσεις HTMX). Για exampel με `Index` τα διασαφηνίζουμε αυτά:

```csharp
    [ResponseCache(Duration = 300, VaryByHeader  = "hx-request", VaryByQueryKeys = new[] {nameof(page), nameof(pageSize)}, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] {"hx-request"} ,VaryByQueryKeys = new[] { nameof(page), nameof(pageSize)})]
```

## Προβολές

Οι απόψεις για το blog είναι σχετικά απλές. Είναι ως επί το πλείστον απλά μια λίστα των αναρτήσεων blog, με μερικές λεπτομέρειες για κάθε δημοσίευση. Οι απόψεις είναι οι εξής: `Views/Blog` Φάκελος. Οι κύριες απόψεις είναι:

### `_PostPartial.cshtml`

Αυτή είναι η μερική προβολή για ένα μόνο blog post. Χρησιμοποιείται μέσα μας. `Post.cshtml` Θέα.

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
    Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

### `_BlogSummaryList.cshtml`

Αυτή είναι η μερική άποψη για μια λίστα των αναρτήσεων blog. Χρησιμοποιείται μέσα μας. `Index.cshtml` προβολή καθώς και στην αρχική σελίδα.

```razor
@model Mostlylucid.Models.Blog.PostListViewModel
<div class="pt-2" id="content">

    @if (Model.TotalItems > Model.PageSize)
    {
        <pager
            x-ref="pager"
            link-url="@Model.LinkUrl"
               hx-boost="true"
               hx-push-url="true"
               hx-target="#content"
               hx-swap="show:none"
               page="@Model.Page"
               page-size="@Model.PageSize"
               total-items="@Model.TotalItems"
            class="w-full"></pager>
    }
    @if(ViewBag.Categories != null)
{
    <div class="pb-3">
        <h4 class="font-body text-lg text-primary dark:text-white">Categories</h4>
        <div class="flex flex-wrap gap-2 pt-2">
            @foreach (var category in ViewBag.Categories)
            {
                <a hx-controller="Blog" hx-action="Category" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>
                    <span class="inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white">@category</span>
                </a>
            }
        </div>
    </div>
}
@foreach (var post in Model.Posts)
{
    <partial name="_ListPost" model="post"/>
}
</div>
```

Αυτό χρησιμοποιεί το `_ListPost` μερική προβολή για την εμφάνιση των μεμονωμένων αναρτήσεων blog μαζί με το [paging tag helper](/blog/addpagingwithhtmx) που μας επιτρέπει να καλέσουμε τις αναρτήσεις στο blog.

### `_ListPost.cshtml`

Η _Listpost μερική προβολή χρησιμοποιείται για την εμφάνιση των μεμονωμένων αναρτήσεων blog στη λίστα. Χρησιμοποιείται εντός του `_BlogSummaryList` Θέα.

```razor
@model Mostlylucid.Models.Blog.PostListModel

<div class="border-b border-grey-lighter pb-8 mb-8">
 
    <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold transition-colors hover:text-green text-blue-dark dark:text-white  dark:hover:text-secondary">@Model.Title</a>
    <div class="flex space-x-2 items-center py-4">
    @foreach (var category in Model.Categories)
    {
    <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
    }

    @{ var languageModel = (Model.Slug, Model.Languages, Model.Language); }
        <partial name="_LanguageList" model="languageModel"/>
    </div>
    <div class="block font-body text-black dark:text-white">@Model.Summary</div>
    <div class="flex items-center pt-4">
        <p class="pr-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.PublishedDate.ToString("f")
        </p>
        <span class="font-body text-grey dark:text-white">//</span>
        <p class="pl-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.ReadingTime
        </p>
    </div>
</div>
```

Όπως θα δείτε εδώ, έχουμε ένα σύνδεσμο με το ατομικό blog post, τις κατηγορίες για την ανάρτηση, τις γλώσσες που είναι διαθέσιμη η ανάρτηση, την περίληψη της ανάρτησης, την δημοσιευμένη ημερομηνία και την ώρα ανάγνωσης.

Έχουμε επίσης ετικέτες σύνδεσης HTMX για τις κατηγορίες και τις γλώσσες για να μας επιτρέψει να εμφανίσει τις τοπικές θέσεις και τις θέσεις για μια δεδομένη κατηγορία.

Έχουμε δύο τρόπους χρήσης HTMX εδώ, ένας τρόπος που δίνει το πλήρες URL και ένας που είναι "μόνο HTML" (δηλαδή. Δεν URL). Αυτό συμβαίνει επειδή θέλουμε να χρησιμοποιήσουμε το πλήρες URL για τις κατηγορίες και τις γλώσσες, αλλά δεν χρειαζόμαστε το πλήρες URL για το ατομικό blog post.

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
```

Αυτή η προσέγγιση κοσμεί ένα πλήρες URL για το ατομικό blog post και χρησιμοποιεί `hx-boost` να "ενισχύσει" το αίτημα για χρήση HTMX (αυτό ορίζει την `hx-request` κεφαλίδα προς `true`).

```razor
  <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
```

Εναλλακτικά αυτή η προσέγγιση χρησιμοποιεί τις ετικέτες HTMX για να πάρει τις κατηγορίες για τις δημοσιεύσεις blog. Αυτό χρησιμοποιεί το `hx-controller`, `hx-action`, `hx-push-url`, `hx-get`, `hx-target` και `hx-route-category` ετικέτες για να πάρετε τις κατηγορίες για τις δημοσιεύσεις blog, ενώ `hx-push-url` έχει οριστεί για `true` για να ωθήσει το URL στο ιστορικό του προγράμματος περιήγησης.

Χρησιμοποιείται επίσης μέσα μας `Index` Μέθοδος δράσης για τις αιτήσεις HTMX.

```csharp
  public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
    {
        var posts =await  blogService.GetPagedPosts(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

Όπου μας επιτρέπει είτε να επιστρέψουμε την πλήρη εικόνα είτε μόνο τη μερική άποψη για τα αιτήματα HTMX, δίνοντας μια "SPA" όπως εμπειρία.

## Αρχική σελίδα

Στην `HomeController` αναφερόμαστε επίσης σε αυτές τις υπηρεσίες blog για να πάρετε τις τελευταίες δημοσιεύσεις blog για την αρχική σελίδα. Αυτό γίνεται στο `Index` μέθοδος δράσης.

```csharp
   public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPagedPosts(page, pageSize);
            posts.LinkUrl= Url.Action("Index", "Home");
            if (Request.IsHtmx())
            {
                return PartialView("_BlogSummaryList", posts);
            }
            var indexPageViewModel = new IndexPageViewModel
            {
                Posts = posts, Authenticated = authenticateResult.LoggedIn, Name = authenticateResult.Name,
                AvatarUrl = authenticateResult.AvatarUrl
            };
            
            return View(indexPageViewModel);
    }
```

Όπως θα δείτε εδώ, χρησιμοποιούμε το `IBlogService` για να πάρετε τις τελευταίες δημοσιεύσεις blog για την αρχική σελίδα. Χρησιμοποιούμε επίσης το `GetUserInfo` μέθοδος για να πάρετε τις πληροφορίες του χρήστη για την αρχική σελίδα.

Και πάλι αυτό έχει ένα αίτημα HTMX για να επιστρέψει τη μερική άποψη για τις δημοσιεύσεις blog για να μας επιτρέψει να σελίδα τις δημοσιεύσεις blog στην αρχική σελίδα.

## Συμπέρασμα

Στο επόμενο μέρος μας θα πάμε σε βασανιστική λεπτομέρεια του πώς χρησιμοποιούμε το `IMarkdownBlogService` να κατοικήσει τη βάση δεδομένων με τις δημοσιεύσεις blog από τα αρχεία markdown. Αυτό είναι ένα βασικό μέρος της εφαρμογής, καθώς μας επιτρέπει να χρησιμοποιήσουμε τα αρχεία markdown για να κατοικήσουμε τη βάση δεδομένων με τις δημοσιεύσεις blog.