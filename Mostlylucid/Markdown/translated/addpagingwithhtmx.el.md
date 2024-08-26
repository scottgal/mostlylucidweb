# Προσθήκη κλήσης με HTMX και ASP.NET πυρήνα με TagHelper

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-09T12:50</datetime>

## Εισαγωγή

Τώρα που έχω ένα μάτσο αναρτήσεις blog η αρχική σελίδα είχε πάρει μάλλον μήκος, έτσι αποφάσισα να προσθέσω ένα μηχανισμό κλήσης για τις δημοσιεύσεις blog.

Αυτό συμβαδίζει με την προσθήκη πλήρους caching για τις δημοσιεύσεις blog για να γίνει αυτό ένα γρήγορο και αποτελεσματικό site.

Δείτε το [Πηγή υπηρεσίας blog](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/Markdown/MarkdownBlogService.cs) για το πώς αυτό εφαρμόζεται? είναι πραγματικά αρκετά απλό χρησιμοποιώντας το IMemoryCache.

[TOC]

### TagHelperName

Αποφάσισα να χρησιμοποιήσω ένα TagHelper για την εφαρμογή του μηχανισμού τηλεειδοποίησης. Αυτός είναι ένας πολύ καλός τρόπος για να ενσαρκώσουμε τη λογική του τηλεειδοποίησης και να την κάνουμε να ξαναχρησιμοποιηθεί.
Αυτό χρησιμοποιεί το [pagination taghelper από τον Darrel O'Neil ](https://github.com/darrel-oneil/PaginationTagHelper) Αυτό περιλαμβάνεται στο έργο ως πακέτο Nuget.

Αυτό προστίθεται στη συνέχεια στο _Δείτε το αρχείο Imports.cshtml έτσι ώστε να είναι διαθέσιμο σε όλες τις απόψεις.

```razor
@addTagHelper *,PaginationTagHelper.AspNetCore
```

### Το TagHelper

Στην _BlogSummaryList.cshtml μερική άποψη Πρόσθεσα τον ακόλουθο κωδικό για να καταστεί ο μηχανισμός τηλεειδοποίησης.

```razor
<pager link-url="@Model.LinkUrl"
       hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
       page="@Model.Page"
       page-size="@Model.PageSize"
       total-items="@Model.TotalItems" ></pager>
```

Μερικά αξιοσημείωτα πράγματα εδώ:

1. `link-url` Αυτό επιτρέπει στο taghelper να δημιουργήσει το σωστό url για τους συνδέσμους τηλεειδοποίησης. Στη μέθοδο HomeController Index, αυτό ορίζεται σε αυτή τη δράση.

```csharp
   var posts = blogService.GetPostsForFiles(page, pageSize);
   posts.LinkUrl= Url.Action("Index", "Home");
   if (Request.IsHtmx())
   {
      return PartialView("_BlogSummaryList", posts);
   }
```

Και στο χειριστήριο του Blog

```csharp
    public IActionResult Index(int page = 1, int pageSize = 5)
    {
        var posts = blogService.GetPostsForFiles(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

Αυτό είναι ρυθμισμένο σε αυτό το URL. Αυτό εξασφαλίζει ότι ο βοηθός επιγραφής μπορεί να λειτουργήσει για οποιαδήποτε από τις δύο κορυφαίες μεθόδους.

### Ιδιότητες HTMX

`hx-boost`, `hx-push-url`, `hx-target`, `hx-swap` αυτές είναι όλες οι ιδιότητες HTMX που επιτρέπουν στην επιγραφή να λειτουργήσει με HTMX.

```razor
     hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
```

Εδώ χρησιμοποιούμε `hx-boost="true"` Αυτό επιτρέπει στο pagination tag helper να μην χρειάζεται καμία τροποποίηση υποκλέπτοντας την κανονική γενιά URL και χρησιμοποιώντας την τρέχουσα URL.

`hx-push-url="true"` για να εξασφαλιστεί η ανταλλαγή του URL στο ιστορικό URL του προγράμματος περιήγησης (το οποίο επιτρέπει την απευθείας σύνδεση με τις σελίδες).

`hx-target="#content"` Αυτός είναι ο στόχος div που θα αντικατασταθεί με το νέο περιεχόμενο.

`hx-swap="show:none"` Αυτό είναι το αποτέλεσμα ανταλλαγής που θα χρησιμοποιηθεί όταν αντικατασταθεί το περιεχόμενο. Στην περίπτωση αυτή αποτρέπει την κανονική επίδραση άλματος που χρησιμοποιεί η HTMX για την ανταλλαγή περιεχομένου.

#### CSS

Επίσης πρόσθεσα στυλ στην κύρια.css στον κατάλογο /src μου επιτρέπει να χρησιμοποιήσω τα μαθήματα CSS Tailwind για τους συνδέσμους pagination.

```css
.pagination {
    @apply py-2 flex list-none p-0 m-0 justify-center items-center;
}

.page-item {
    @apply mx-1 text-black  dark:text-white rounded;
}

.page-item a {
    @apply block rounded-md transition duration-300 ease-in-out;
}

.page-item a:hover {
    @apply bg-blue-dark text-white;
}

.page-item.disabled a {
    @apply text-blue-dark pointer-events-none cursor-not-allowed;
}

```

### Ελεγκτής

`page`, `page-size`, `total-items` είναι οι ιδιότητες που το pagination taghelper χρησιμοποιεί για να δημιουργήσει τους συνδέσμους τηλεειδοποίησης.
Αυτά περνάνε στη μερική θέα από το χειριστήριο.

```csharp
 public IActionResult Index(int page = 1, int pageSize = 5)
```

### Υπηρεσία Blog

Εδώ η σελίδα και η σελίδαΜέγεθος περνούν από το URL και τα συνολικά στοιχεία υπολογίζονται από την υπηρεσία blog.

```csharp
    public PostListViewModel GetPostsForFiles(int page=1, int pageSize=10)
    {
        var model = new PostListViewModel();
        var posts = GetPageCache().Values.Select(GetListModel).ToList();
        model.Posts = posts.OrderByDescending(x => x.PublishedDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        model.TotalItems = posts.Count();
        model.PageSize = pageSize;
        model.Page = page;
        return model;
    }
```

Εδώ απλά παίρνουμε τις αναρτήσεις από την κρύπτη, τις παραγγέλνουμε με ημερομηνία και στη συνέχεια φεύγουμε και παίρνουμε το σωστό αριθμό θέσεων για τη σελίδα.

### Συμπέρασμα

Αυτή ήταν μια απλή προσθήκη στο site, αλλά το κάνει πολύ πιο χρήσιμο. Η ενσωμάτωση HTMX κάνει το site να αισθάνεται πιο ανταποκρίνεται ενώ δεν προσθέτει περισσότερα JavaScript στο site.