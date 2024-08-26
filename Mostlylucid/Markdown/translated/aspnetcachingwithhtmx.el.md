# ASP.NET Core Caching με HTMX

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-12T00:50</datetime>

## Εισαγωγή

Caching είναι μια σημαντική τεχνική για τη βελτίωση τόσο της εμπειρίας του χρήστη με τη φόρτωση του περιεχομένου γρηγορότερα και τη μείωση του φορτίου στον διακομιστή σας. Σε αυτό το άρθρο θα σας δείξω πώς να χρησιμοποιήσετε τα ενσωματωμένο caching χαρακτηριστικά του ASP.NET Core με HTMX για να cache περιεχόμενο στην πλευρά του πελάτη.

[TOC]

## Ρύθμιση

Στο ASP.NET Core, υπάρχουν δύο τύποι Caching που προσφέρονται

- Reponse Cache - Αυτό είναι τα δεδομένα που είναι αποθηκευμένα στον πελάτη ή σε ενδιάμεσους procy διακομιστές (ή και τα δύο) και χρησιμοποιείται για να κρύψει ολόκληρη την απάντηση για ένα αίτημα.
- Cache εξόδου - Αυτό είναι τα δεδομένα που είναι αποθηκευμένα στο διακομιστή και χρησιμοποιείται για την αποθήκευση της εξόδου μιας δράσης ελεγκτή.

Για να τα ρυθμίσετε στο ASP.NET Core θα πρέπει να προσθέσετε μερικές υπηρεσίες στο `Program.cs` αρχείο

### Απάντηση Caching

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### Αποθήκευση εξόδου

```csharp
services.AddOutputCache();
app.UseOutputCache();
```

## Απάντηση Caching

Ενώ είναι δυνατόν να ρυθμίσετε Response Caching σε σας `Program.cs` είναι συχνά λίγο άκαμπτο (ιδιαίτερα όταν χρησιμοποιείτε αιτήματα HTMX όπως ανακάλυψα). Μπορείτε να ρυθμίσετε το Response Caching στις ενέργειες του ελεγκτή σας με τη χρήση του `ResponseCache` γνώριμη ιδιότητα.

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

Αυτό θα κρύψει την απόκριση για 300 δευτερόλεπτα και να ποικίλει η κρύπτη από το `hx-request` κεφαλίδα και η `page` και `pageSize` παραμέτρους ερωτημάτων. Είμαστε επίσης που το `Location` έως `Any` που σημαίνει ότι η απάντηση μπορεί να κρατηθεί στον πελάτη, στους ενδιάμεσους διακομιστές μεσολάβησης, ή και τα δύο.

Ορίστε... `hx-request` κεφαλίδα είναι η κεφαλίδα που στέλνει η HTMX με κάθε αίτημα. Αυτό είναι σημαντικό καθώς σας επιτρέπει να κρύψετε την απάντηση διαφορετικά με βάση το αν πρόκειται για μια αίτηση HTMX ή ένα κανονικό αίτημα.

Αυτό είναι το ρεύμα μας. `Index` μέθοδος δράσης. Yo ucan see that we accept a page and pageSize parametry here and we added these as variety quesry keys in the `ResponseCache` γνώριμη ιδιότητα. Σημαίνει ότι οι απαντήσεις "δείχνονται" από αυτά τα κλειδιά και αποθηκεύουν διαφορετικό περιεχόμενο με βάση αυτά.

Σε δράση, έχουμε επίσης `if(Request.IsHtmx())` Αυτό βασίζεται στην [HTMX.Net πακέτο](https://github.com/khalidabuhakmeh/Htmx.Net)  και κατ' ουσίαν έλεγχοι για το ίδιο `hx-request` Με κεφαλίδα που χρησιμοποιούμε για να διαφοροποιήσουμε την κρύπτη. Εδώ επιστρέφουμε μια μερική άποψη αν το αίτημα είναι από HTMX.

```csharp
    public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPosts(page, pageSize);
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

## Αποθήκευση εξόδου

Output Caching είναι η πλευρά του διακομιστή ισοδύναμο της Caching απόκρισης. Κλειδώνει την έξοδο μιας δράσης ελεγκτή. Στην ουσία ο διακομιστής web αποθηκεύει το αποτέλεσμα ενός αιτήματος και το εξυπηρετεί για τα επόμενα αιτήματα.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

Εδώ κρατάμε την έξοδο της δράσης του ελεγκτή για 3600 δευτερόλεπτα και αλλάζουμε την κρύπτη με το `hx-request` κεφαλίδα και η `page` και `pageSize` παραμέτρους ερωτημάτων.
Καθώς αποθηκεύουμε την πλευρά του διακομιστή δεδομένων για ένα σημαντικό χρονικό διάστημα (οι δημοσιεύσεις ενημερώνονται μόνο με ένα πάτημα docker) αυτό έχει οριστεί σε μεγαλύτερο χρονικό διάστημα από ό, τι το Cache απάντηση? θα μπορούσε πραγματικά να είναι άπειρο στην περίπτωσή μας, αλλά 3600 δευτερόλεπτα είναι ένας καλός συμβιβασμός.

Όπως και με το Cache απάντηση χρησιμοποιούμε το `hx-request` κεφαλίδα για να ποικίλει η κρύπτη με βάση το αν η αίτηση είναι από HTMX ή όχι.

## Στατικά αρχεία

ASP.NET Core έχει επίσης ενσωματωμένη υποστήριξη για την αποθήκευση στατικών αρχείων. Αυτό γίνεται με τον καθορισμό του `Cache-Control` Επικεφαλίδα στην απάντηση. Μπορείς να το βάλεις αυτό στο δικό σου `Program.cs` Φάκελος.
Σημειώστε ότι η παραγγελία είναι σημαντική εδώ, αν στατικά αρχεία σας χρειάζονται υποστήριξη εξουσιοδότησης θα πρέπει να μετακινήσετε το `UseAuthorization` middleware πριν από το `UseStaticFiles` Μεσαίο λογισμικό. THe UseHttpsRedirection middleware θα πρέπει επίσης να είναι πριν από το UseStaticFiles intermediware εάν βασίζεστε σε αυτό το χαρακτηριστικό.

```csharp
app.UseHttpsRedirection();
var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
            "Cache-Control", $"public, max-age={cacheMaxAgeOneWeek}");
    }
});
app.UseRouting();
app.UseCors("AllowMostlylucid");
app.UseAuthentication();
app.UseAuthorization();
```

## Συμπέρασμα

Caching είναι ένα ισχυρό εργαλείο για τη βελτίωση της απόδοσης της εφαρμογής σας. Χρησιμοποιώντας τα ενσωματωμένα χαρακτηριστικά caching του ASP.NET Core μπορείτε εύκολα να cache περιεχόμενο στην πλευρά του πελάτη ή του διακομιστή. Με τη χρήση HTMX μπορείτε να κρύψετε το περιεχόμενο από την πλευρά του πελάτη και να εξυπηρετήσετε μερικές απόψεις για να βελτιώσετε την εμπειρία του χρήστη.