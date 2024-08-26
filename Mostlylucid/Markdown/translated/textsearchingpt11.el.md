# Πλήρης αναζήτηση κειμένου (Pt 1.1)

<!--category-- Postgres, Alpine -->
<datetime class="hidden">2024-08-21T20:30</datetime>

## Εισαγωγή

Στην [τελευταίο άρθρο](/blog/textsearchingpt1) Σας έδειξα πώς να δημιουργήσετε μια πλήρη αναζήτηση κειμένου χρησιμοποιώντας την ενσωματωμένη σε πλήρεις δυνατότητες αναζήτησης κειμένου του Postgres. Ενώ ξεσκέπασα έναν έιπι που ψάχνει, δεν είχα τρόπο να το χρησιμοποιήσω, οπότε... ήταν λίγο πειραχτήρι. Σε αυτό το άρθρο θα σας δείξω πώς να χρησιμοποιήσετε την αναζήτηση api για να αναζητήσετε κείμενο στη βάση δεδομένων σας.

Προηγούμενα μέρη σε αυτή τη σειρά:

- [Πλήρης αναζήτηση κειμένου με Postgres](/blog/textsearchingpt1)

Επόμενα μέρη σε αυτή τη σειρά:

- [Εισαγωγή στο OpenSearch](/blog/textsearchingpt2)
- [Opensearch με C#](/blog/textsearchingpt3)

Αυτό θα προσθέσει ένα μικρό πλαίσιο αναζήτησης στην κεφαλίδα της ιστοσελίδας που θα επιτρέψει στους χρήστες να αναζητήσουν το κείμενο στις αναρτήσεις blog.

![Αναζήτηση](searchbox.png?format=webp&quality=25)

**Σημείωση: Ο ελέφαντας στο δωμάτιο είναι ότι δεν θεωρώ τον καλύτερο τρόπο για να το κάνουμε αυτό. Για να υποστηρίξω την πολυγλωσσική γλώσσα είναι σούπερ σύνθετο (θα χρειαστώ μια διαφορετική στήλη ανά γλώσσα) και θα πρέπει να χειριστώ τα αντανακλαστικά και άλλα συγκεκριμένα γλωσσικά πράγματα. Θα το αγνοήσω προς το παρόν και θα επικεντρωθώ στα Αγγλικά. Αργότερα θα δείξουμε πώς θα το χειριστούμε στο OpenSearch.**

[TOC]

## Αναζήτηση κειμένου

Για να προσθέσω μια δυνατότητα αναζήτησης έπρεπε να κάνω κάποιες αλλαγές στο πεδίο αναζήτησης. Πρόσθεσα χειρισμό για φράσεις με τη χρήση του `EF.Functions.WebSearchToTsQuery("english", processedQuery)`

```csharp
    private async Task<List<(string Title, string Slug)>> GetSearchResultForQuery(string query)
    {
        var processedQuery = query;
        var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                // Search using the precomputed SearchVector
                (x.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", processedQuery)) // Use precomputed SearchVector for title and content
                || x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", processedQuery)))) // Search in categories
                && x.LanguageEntity.Name == "en")// Filter by language
            
            .OrderByDescending(x =>
                // Rank based on the precomputed SearchVector
                x.SearchVector.Rank(EF.Functions.WebSearchToTsQuery("english", processedQuery))) // Use precomputed SearchVector for ranking
            .Select(x => new { x.Title, x.Slug,  })
            .Take(5)
            .ToListAsync();
        return posts.Select(x=> (x.Title, x.Slug)).ToList();
    }
```

Αυτό χρησιμοποιείται προαιρετικά όταν υπάρχει χώρος στο ερώτημα

```csharp
    if (!query.Contains(" "))
        {
            posts = await GetSearchResultForComplete(query);
        }
        else
        {
            posts = await GetSearchResultForQuery(query);
        }
```

Αλλιώς θα χρησιμοποιήσω την υπάρχουσα μέθοδο αναζήτησης που προσθέτει τον χαρακτήρα πρόθεμα.

```csharp
EF.Functions.ToTsQuery("english", query + ":*")

```

## Έλεγχος αναζήτησης

Χρήση [Alpine.js](https://alpinejs.dev/) Έκανα ένα απλό Μερικό έλεγχο που παρέχει ένα σούπερ απλό κουτί αναζήτησης.

```razor
<div x-data="window.mostlylucid.typeahead()" class="relative"    x-on:click.outside="results = []">

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

Αυτό έχει ένα μάτσο τάξεις CSS για να καταστήσει σωστά είτε για τη σκοτεινή ή φωτεινή λειτουργία. Ο κώδικας Alpine.js είναι πολύ απλός. Είναι ένας απλός έλεγχος από την αρχή της ώρας που καλεί την αναζήτηση api όταν ο χρήστης πληκτρολογεί στο πλαίσιο αναζήτησης.
Έχουμε επίσης ένα μικρό κώδικα για να χειριστεί χωρίς εστίαση για να κλείσει τα αποτελέσματα αναζήτησης.

```html
   x-on:click.outside="results = []"
```

Σημειώστε ότι έχουμε μια αποβολή εδώ για να αποφύγουμε να σφυροκοπήσουμε τον διακομιστή με αιτήματα.

## Το πρότυπο JSName

Αυτό καλεί στη λειτουργία JS μας (καθορισμένο σε `src/js/main.js`)

```javascript
window.mostlylucid = window.mostlylucid || {};

window.mostlylucid.typeahead = function () {
    return {
        query: '',
        results: [],
        highlightedIndex: -1, // Tracks the currently highlighted index

        search() {
            if (this.query.length < 2) {
                this.results = [];
                this.highlightedIndex = -1;
                return;
            }

            fetch(`/api/search/${encodeURIComponent(this.query)}`)
                .then(response => response.json())
                .then(data => {
                    this.results = data;
                    this.highlightedIndex = -1; // Reset index on new search
                });
        },

        moveDown() {
            if (this.highlightedIndex < this.results.length - 1) {
                this.highlightedIndex++;
            }
        },

        moveUp() {
            if (this.highlightedIndex > 0) {
                this.highlightedIndex--;
            }
        },

        selectHighlighted() {
            if (this.highlightedIndex >= 0 && this.highlightedIndex < this.results.length) {
                this.selectResult(this.results[this.highlightedIndex]);
            }
        },

        selectResult(result) {
           window.location.href = result.url;
            this.results = []; // Clear the results
            this.highlightedIndex = -1; // Reset the highlighted index
        }
    }
}
```

Όπως μπορείτε να δείτε αυτό είναι αρκετά απλό (μεγάλο μέρος της πολυπλοκότητας χειρίζεται τα πάνω και κάτω κλειδιά για να επιλέξετε τα αποτελέσματα).
Αυτό αναρτά στην `SearchApi`
Όταν επιλεγεί ένα αποτέλεσμα, πλοηγούμαστε στο url του αποτελέσματος.

```javascript
     search() {
            if (this.query.length < 2) {
                this.results = [];
                this.highlightedIndex = -1;
                return;
            }

            fetch(`/api/search/${encodeURIComponent(this.query)}`)
                .then(response => response.json())
                .then(data => {
                    this.results = data;
                    this.highlightedIndex = -1; // Reset index on new search
                });
        },
```

### HTMX

Άλλαξα επίσης το φέρετρο για να συνεργαστεί με HTMX, αυτό απλά αλλάζει το `search` μέθοδος για τη χρήση ανανέωσης HTMX:

```javascript
    selectResult(result) {
    htmx.ajax('get', result.url, {
        target: '#contentcontainer',  // The container to update
        swap: 'innerHTML', // Replace the content inside the target
    }).then(function() {
        history.pushState(null, '', result.url); // Push the new url to the history
    });

    this.results = []; // Clear the results
    this.highlightedIndex = -1; // Reset the highlighted index
    this.query = ''; // Clear the query
}
```

Σημειώστε ότι ανταλλάσσουμε το εσωτερικό HTML του `contentcontainer` με το αποτέλεσμα της έρευνας. Αυτός είναι ένας απλός τρόπος για να ενημερώσετε το περιεχόμενο της σελίδας με το αποτέλεσμα αναζήτησης χωρίς μια σελίδα ανανέωση.
Αλλάζουμε επίσης το url στην ιστορία με το νέο url.

## Συμπέρασμα

Αυτό προσθέτει μια ισχυρή αλλά απλή δυνατότητα αναζήτησης στην ιστοσελίδα. Είναι ένας καλός τρόπος για να βοηθήσει τους χρήστες να βρουν αυτό που ψάχνουν.
Δίνει σε αυτό το site μια πιο επαγγελματική αίσθηση και καθιστά ευκολότερη την πλοήγηση.