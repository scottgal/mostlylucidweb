# Απλό 'Donut Hole' Caching με HTMX

# Εισαγωγή

Donut hole caching μπορεί να είναι μια χρήσιμη τεχνική όπου θέλετε να κρύψετε ορισμένα στοιχεία μιας σελίδας, αλλά όχι όλα. Ωστόσο, μπορεί να είναι δύσκολο να εφαρμοστεί. Σε αυτό το άρθρο θα σας δείξω πώς να εφαρμόσετε μια απλή τεχνική αποθήκευσης ντόνατς χρησιμοποιώντας HTMX.

<!--category-- HTMX, Razor, ASP.NET -->
<datetime class="hidden">2024-09-12T16:00</datetime>
[TOC]

# Το Πρόβλημα

Ένα θέμα που είχα με αυτό το site είναι ότι ήθελα να χρησιμοποιήσω αντι-αξέχαστες μάρκες με τις φόρμες μου. Πρόκειται για μια καλή πρακτική ασφαλείας για την πρόληψη των επιθέσεων Cross-Site Request Forgery (CSRF). Ωστόσο, δημιουργούσε πρόβλημα με την αποθήκευση των σελίδων. Το αντι-αξέχαστο σύμβολο είναι μοναδικό σε κάθε αίτηση σελίδας, έτσι αν κρύψετε τη σελίδα, το σύμβολο θα είναι το ίδιο για όλους τους χρήστες. Αυτό σημαίνει ότι αν ένας χρήστης υποβάλει ένα έντυπο, το σύμβολο θα είναι άκυρο και η υποβολή του εντύπου θα αποτύχει. ASP.NET Core αποτρέπει αυτό με την απενεργοποίηση όλων των caching κατόπιν αιτήματος όπου χρησιμοποιείται το αντι-αξέχαστο σύμβολο. Αυτή είναι μια καλή πρακτική ασφαλείας, αλλά αυτό σημαίνει ότι η σελίδα δεν θα κρατηθεί καθόλου. Αυτό δεν είναι ιδανικό για ένα site όπως αυτό όπου το περιεχόμενο είναι κυρίως στατικό.

# Η Λύση

Ένας κοινός τρόπος γύρω από αυτό είναι το "donut hole" caching όπου μπορείτε να κρύψετε το μεγαλύτερο μέρος της σελίδας αλλά ορισμένα στοιχεία. Υπάρχουν πολλοί τρόποι για να επιτευχθεί αυτό στο ASP.NET Core χρησιμοποιώντας το μερικό πλαίσιο προβολής, ωστόσο είναι περίπλοκο να εφαρμόσει και συχνά απαιτεί συγκεκριμένα πακέτα και config. Ήθελα μια απλούστερη λύση.

Όπως ήδη χρησιμοποιώ το εξαιρετικό [HTMX](https://htmx.org/examples/lazy-load/) σε αυτό το έργο υπάρχει ένας σούπερ απλός τρόπος για να αποκτήσετε δυναμική λειτουργία 'donut hole' με δυναμική φόρτωση Μερικά με HTMX.
Έχω ήδη μπλόκαρε περίπου [χρησιμοποιώντας AntiForgeryRequest Tokens με Javascript](/blog/addingxsrfforjavascript) Ωστόσο και πάλι το θέμα ήταν ότι αυτή η αποτελεσματικά ανάπηρη αποθήκευση για τη σελίδα.

Τώρα μπορώ να επαναφέρω αυτή τη λειτουργία όταν χρησιμοποιώ HTMX για να φορτώσω δυναμικά επιμέρους.

```razor
<li class="group relative mb-1">
    <div  hx-trigger="load" hx-get="/typeahead">
    </div>
</li>
```

Απολύτως απλό, σωστά; Το μόνο που κάνει αυτό είναι να καλέσει τη μία γραμμή κώδικα στο χειριστήριο που επιστρέφει τη μερική άποψη. Αυτό σημαίνει ότι το αντι-αξέχαστο σύμβολο παράγεται στο διακομιστή και η σελίδα μπορεί να κρατηθεί ως φυσιολογικό. Η μερική θέα φορτώνεται δυναμικά έτσι ώστε το σύμβολο να είναι ακόμα μοναδικό σε κάθε αίτημα.

```csharp
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
```

Within το μερικό έχουμε ακόμα την απλή μορφή με το αντι-αξέχαστο σύμβολο.

```razor
<div x-data="window.mostlylucid.typeahead()" class="relative" id="searchelement"  x-on:click.outside="results = []">
    @Html.AntiForgeryToken()
    <label class="input input-sm dark:bg-custom-dark-bg bg-white input-bordered flex items-center gap-2">
        <input
            type="text"
            x-model="query"
            x-on:input.debounce.300ms="search"
            x-on:keydown.down.prevent="moveDown"
            x-on:keydown.up.prevent="moveUp"
            x-on:keydown.enter.prevent="selectHighlighted"
            placeholder="Search..."
            class="border-0 grow  input-sm text-black dark:text-white bg-transparent w-full"/>
        <i class="bx bx-search"></i>
    </label>
    <!-- Dropdown -->
    <ul x-show="results.length > 0"
        class="absolute z-10 my-2 w-full bg-white dark:bg-custom-dark-bg border border-1 text-black dark:text-white border-b-neutral-600 dark:border-gray-300   rounded-lg shadow-lg">
        <template x-for="(result, index) in results" :key="result.slug">
            <li
                x-on:click="selectResult(result)"
                :class="{
                    'dark:bg-blue-dark bg-blue-light': index === highlightedIndex,
                    'dark:hover:bg-blue-dark hover:bg-blue-light': true
                }"
                class="cursor-pointer text-sm p-2 m-2"
                x-text="result.title"
            ></li>
        </template>
    </ul>
</div>
```

Αυτό στη συνέχεια ενσωματώνει όλο τον κώδικα για την αναζήτηση του typenaward και όταν υποβάλλεται τραβάει το σήμα και τον προσθέτει στο αίτημα (ακριβώς όπως και πριν).

```javascript
        let token = document.querySelector('#searchelement input[name="__RequestVerificationToken"]').value;
            console.log(token);
            fetch(`/api/search/${encodeURIComponent(this.query)}`, { // Fixed the backtick and closing bracket
                method: 'GET', // or 'POST' depending on your needs
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': token // Attach the AntiForgery token in the headers
                }
            })
```

# Συμπέρασμα

Αυτός είναι ένας σούπερ απλός τρόπος για να κάνετε το 'donut hole' caching με HTMX. Είναι ένας πολύ καλός τρόπος για να πάρει τα οφέλη του caching χωρίς την πολυπλοκότητα ενός επιπλέον πακέτου. Ελπίζω να το βρεις χρήσιμο. Ενημερώστε με αν έχετε οποιεσδήποτε ερωτήσεις στα σχόλια που ακολουθούν.