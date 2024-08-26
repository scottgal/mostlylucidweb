# Πλήρης αναζήτηση κειμένου (Pt 3 - OpenSearch με πυρήνα ASP.NET)

<!--category-- OpenSearch, ASP.NET -->
<datetime class="hidden">2024-08-24T06:40</datetime>

## Εισαγωγή

Στα προηγούμενα μέρη αυτής της σειράς εισάγαμε την έννοια της πλήρους αναζήτησης κειμένου και πώς μπορεί να χρησιμοποιηθεί για την αναζήτηση κειμένου μέσα σε μια βάση δεδομένων. Σε αυτό το μέρος θα εισαγάγει πώς να χρησιμοποιήσετε OpenSearch με ASP.NET Core.

Προηγούμενα μέρη:

- [Πλήρης αναζήτηση κειμένου με Postgres](/blog/textsearchingpt1)
- [Κουτί αναζήτησης με Postgres](/blog/textsearchingpt11)
- [Εισαγωγή στο OpenSearch](/blog/textsearchingpt2)

Σε αυτό το μέρος θα καλύψουμε πώς να αρχίσουμε να χρησιμοποιούμε το νέο γυαλιστερό παράδειγμα OpenSearch με το ASP.NET Core.

[TOC]

## Ρύθμιση

Μόλις έχουμε την περίπτωση OpenSearch επάνω και τρέχει μπορούμε να αρχίσουμε να αλληλεπιδρούμε με αυτό. Θα χρησιμοποιήσουμε το... [Πελάτης OpenSearch](https://opensearch.org/docs/latest/clients/OSC-dot-net/) για το.NET.
Πρώτα στήσαμε τον πελάτη στην επέκταση της εγκατάστασης μας

```csharp
    var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));
```

Αυτό δημιουργεί τον πελάτη με το τελικό σημείο και τα διαπιστευτήρια. Επίσης ενεργοποιούμε τη λειτουργία αποσφαλμάτωσης για να δούμε τι συμβαίνει. Επιπλέον, καθώς δεν χρησιμοποιούμε πιστοποιητικά REAL SSL απενεργοποιούμε την επικύρωση πιστοποιητικού (μην το κάνετε αυτό στην παραγωγή).

## Στοιχεία ευρετηρίου

Η βασική έννοια στο OpenSearch είναι ο Δείκτης. Σκεφτείτε ένα ευρετήριο σαν έναν πίνακα βάσης δεδομένων: είναι όπου όλα τα δεδομένα σας αποθηκεύονται.

Για να το κάνουμε αυτό θα χρησιμοποιήσουμε το [Πελάτης OpenSearch](https://opensearch.org/docs/latest/clients/OSC-dot-net/) για το.NET. Μπορείτε να εγκαταστήσετε αυτό μέσω NuGet:

Θα παρατηρήσετε ότι υπάρχουν δύο εκεί - Opensearch.Net και Opensearch.Client. Το πρώτο είναι το χαμηλό επίπεδο πράγμα, όπως η διαχείριση σύνδεσης, το δεύτερο είναι το υψηλό επίπεδο πράγματα όπως η ευρετηρίαση και η αναζήτηση.

Τώρα που το έχουμε εγκαταστήσει μπορούμε να αρχίσουμε να εξετάζουμε τα δεδομένα ευρετηρίου.

Η δημιουργία ενός ευρετηρίου είναι ημι-ευθεία μπροστά. Απλά ορίζεις πώς πρέπει να είναι ο δείκτης σου και μετά τον δημιουργείς.
Στον παρακάτω κώδικα μπορείτε να δείτε το'map' μας Index Model (μια απλοποιημένη έκδοση του μοντέλου βάσης δεδομένων του blog).
Για κάθε πεδίο αυτού του μοντέλου ορίζουμε στη συνέχεια τι τύπο είναι (κείμενο, ημερομηνία, λέξη-κλειδί κ.λπ.) και τι αναλυτής να χρησιμοποιήσει.

Ο τύπος είναι σημαντικός καθώς καθορίζει πώς αποθηκεύονται τα δεδομένα και πώς μπορούν να αναζητηθούν. Για παράδειγμα, ένα πεδίο "κείμενο" αναλύεται και επισημαίνεται, ένα πεδίο "κλειδί" δεν είναι. Έτσι θα περιμένατε να αναζητήσετε ένα πεδίο λέξεων-κλειδιών ακριβώς όπως είναι αποθηκευμένο, αλλά ένα πεδίο κειμένου που μπορείτε να αναζητήσετε μέρη του κειμένου.

Επίσης εδώ Κατηγορίες είναι στην πραγματικότητα μια χορδή[] αλλά ο τύπος λέξης-κλειδί καταλαβαίνει πώς να τα χειριστεί σωστά.

```csharp
   public async Task CreateIndex(string language)
    {
        var languageName = language.ConvertCodeToLanguageName();
        var indexName = GetBlogIndexName(language);

      var response =  await client.Indices.CreateAsync(indexName, c => c
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(1)
            )
            .Map<BlogIndexModel>(m => m
                .Properties(p => p
                    .Text(t => t
                        .Name(n => n.Title)
                        .Analyzer(languageName)
                    )
                    .Text(t => t
                        .Name(n => n.Content)
                        .Analyzer(languageName)
                    )
                    .Text(t => t
                        .Name(n => n.Language)
                    )
                    .Date(t => t
                        .Name(n => n.LastUpdated)
                    )
                    .Date(t => t
                        .Name(n => n.Published)
                    )
                    .Date(t => t
                        .Name(n => n.LastUpdated)
                    )
                    .Keyword(t => t
                        .Name(n => n.Id)
                    )
                    .Keyword(t=>t
                        .Name(n=>n.Slug)
                    )
                    .Keyword(t=>t
                        .Name(n=>n.Hash)
                    )
                    .Keyword(t => t
                        .Name(n => n.Categories)
                    )
                )
            )
        );
        
        if (!response.IsValid)
        {
           logger.LogError("Failed to create index {IndexName}: {Error}", indexName, response.DebugInformation);
        }
    }
```

## Προσθήκη αντικειμένων στο ευρετήριο

Μόλις φτιάξουμε τον δείκτη μας για να προσθέσουμε στοιχεία σε αυτόν, πρέπει να προσθέσουμε στοιχεία σε αυτόν τον δείκτη. Εδώ, καθώς προσθέτουμε ένα BUNCH χρησιμοποιούμε μια μέθοδο μαζικής εισαγωγής.

Μπορείτε να δείτε ότι αρχικά καλούμε σε μια μέθοδο που ονομάζεται`GetExistingPosts` που επιστρέφει όλες τις θέσεις που είναι ήδη στο ευρετήριο. Στη συνέχεια, ομαδοποιούμε τις θέσεις ανά γλώσσα και φιλτράρουμε τη γλώσσα 'uk' (καθώς δεν θέλουμε να καταγράψουμε ότι καθώς χρειάζεται ένα επιπλέον plugin που δεν έχουμε ακόμα). Στη συνέχεια φιλτράρουμε όλες τις θέσεις που βρίσκονται ήδη στο ευρετήριο.
Χρησιμοποιούμε το χασίς και την ταυτότητα για να αναγνωρίσουμε αν μια θέση είναι ήδη στο ευρετήριο.

```csharp
    public async Task AddPostsToIndex(IEnumerable<BlogIndexModel> posts)
    {
        var existingPosts = await GetExistingPosts();
        var langPosts = posts.GroupBy(p => p.Language);
        langPosts=langPosts.Where(p => p.Key!="uk");
        langPosts = langPosts.Where(p =>
            p.Any(post => !existingPosts.Any(existing => existing.Id == post.Id && existing.Hash == post.Hash)));
        
        foreach (var blogIndexModels in langPosts)
        {
            
            var language = blogIndexModels.Key;
            var indexName = GetBlogIndexName(language);
            if(!await IndexExists(language))
            {
                await CreateIndex(language);
            }
            
            var bulkRequest = new BulkRequest(indexName)
            {
                Operations = new BulkOperationsCollection<IBulkOperation>(blogIndexModels.ToList()
                    .Select(p => new BulkIndexOperation<BlogIndexModel>(p))
                    .ToList()),
                Refresh = Refresh.True,
                ErrorTrace = true,
                RequestConfiguration = new RequestConfiguration
                {
                    MaxRetries = 3
                }
            };

            var bulkResponse = await client.BulkAsync(bulkRequest);
            if (!bulkResponse.IsValid)
            {
                logger.LogError("Failed to add posts to index {IndexName}: {Error}", indexName, bulkResponse.DebugInformation);
            }
            
        }
    }
```

Μόλις φιλτράρουμε τις υπάρχουσες θέσεις και τον αγνοούμενο αναλυτή μας δημιουργούμε ένα νέο ευρετήριο (βάσει του ονόματος, στην περίπτωσή μου "κυρίως διαυγή-blog-<language>") και στη συνέχεια να δημιουργήσει ένα μεγάλο αίτημα. Αυτό το μαζικό αίτημα είναι μια συλλογή εργασιών για την εκτέλεση του ευρετηρίου.
Αυτό είναι πιο αποτελεσματικό από την προσθήκη κάθε αντικειμένου ένα προς ένα.

Θα το δεις αυτό στο... `BulkRequest` εμείς ορίσαμε το `Refresh` περιουσιακό στοιχείο προς `true`. Αυτό σημαίνει ότι μετά την ολοκλήρωση του μαζικού ένθετου ο δείκτης αναζωογονείται. Αυτό δεν είναι πραγματικά απαραίτητο, αλλά είναι χρήσιμο για αποσφαλμάτωση.

## Αναζήτηση του ευρετηρίου

Ένας καλός τρόπος για να δοκιμάσετε να δείτε τι πραγματικά δημιουργήθηκε εδώ είναι να πάτε στο Dev Tools στο OpenSearch Dashboards και να εκτελέσετε μια έρευνα.

```json
GET /mostlylucid-blog-*
{}
```

Αυτό το ερώτημα θα μας επιστρέψει όλους τους δείκτες που ταιριάζουν με το μοτίβο `mostlylucid-blog-*`. (όπως όλα τα ευρετήρια μας μέχρι στιγμής).

```json
{
  "mostlylucid-blog-ar": {
    "aliases": {},
    "mappings": {
      "properties": {
        "categories": {
          "type": "keyword"
        },
        "content": {
          "type": "text",
          "analyzer": "arabic"
        },
        "hash": {
          "type": "keyword"
        },
        "id": {
          "type": "keyword"
        },
        "language": {
          "type": "text"
        },
        "lastUpdated": {
          "type": "date"
        },
        "published": {
          "type": "date"
        },
        "slug": {
          "type": "keyword"
        },
        "title": {
          "type": "text",
          "analyzer": "arabic"
        }
      }
    },
    "settings": {
      "index": {
        "replication": {
          "type": "DOCUMENT"
..MANY MORE
```

Dev Tools in OpenSearch Dashboards is a great way to test your questions before you put them into your code.

![Εργαλεία Dev](devtools.png?width=900&quality=25)

## Αναζήτηση του ευρετηρίου

Τώρα μπορούμε να αρχίσουμε να ψάχνουμε το ευρετήριο. Μπορούμε να χρησιμοποιήσουμε το `Search` μέθοδος για τον πελάτη για να το κάνει αυτό.
Εδώ είναι που έρχεται η πραγματική δύναμη του OpenSearch. Έχει κυριολεκτικά [Δεκάδες διαφορετικά είδη ερωτήσεων](https://opensearch.org/docs/latest/query-dsl/) Μπορείτε να χρησιμοποιήσετε για να αναζητήσετε τα δεδομένα σας. Τα πάντα από μια απλή αναζήτηση λέξεων-κλειδιών μέχρι μια πολύπλοκη "νευρική" αναζήτηση.

```csharp
    public async Task<List<BlogIndexModel>> GetSearchResults(string language, string query, int page = 1, int pageSize = 10)
    {
        var indexName = GetBlogIndexName(language);
        var searchResponse = await client.SearchAsync<BlogIndexModel>(s => s
                .Index(indexName)  // Match index pattern
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .MultiMatch(mm => mm
                                .Query(query)
                                .Fields(f => f
                                    .Field(p => p.Title, boost: 2.0) 
                                    .Field(p => p.Categories, boost: 1.5) 
                                    .Field(p => p.Content)
                                )
                                .Type(TextQueryType.BestFields)
                                .Fuzziness(Fuzziness.Auto)
                            )
                        )
                    )
                )
                .Skip((page -1) * pageSize)  // Skip the first n results (adjust as needed)
                .Size(pageSize)  // Limit the number of results (adjust as needed)
        );

        if(!searchResponse.IsValid)
        {
            logger.LogError("Failed to search index {IndexName}: {Error}", indexName, searchResponse.DebugInformation);
            return new List<BlogIndexModel>();
        }
        return searchResponse.Documents.ToList();
    }

```

### Περιγραφή της ερώτησης

Αυτή η μέθοδος, `GetSearchResults`, έχει σχεδιαστεί για να ερευνήσει ένα συγκεκριμένο ευρετήριο OpenSearch για την ανάκτηση των αναρτήσεων blog. Χρειάζονται τρεις παράμετροι: `language`, `query`και παράμετροι επικόλλησης `page` και `pageSize`. Άκου τι κάνει:

1. **Επιλογή ευρετηρίου**:
   
   - Ανακτά το όνομα του ευρετηρίου χρησιμοποιώντας το `GetBlogIndexName` μέθοδος με βάση τη γλώσσα που παρέχεται. Ο δείκτης επιλέγεται δυναμικά σύμφωνα με τη γλώσσα.

2. **Αναζήτηση ερωτημάτων**:
   
   - Η ερώτηση χρησιμοποιεί ένα `Bool` Ερωτηματολόγιο με `Must` ρήτρα διασφάλισης ότι τα αποτελέσματα ανταποκρίνονται σε ορισμένα κριτήρια.
   - Μέσα στο `Must` ρήτρα, α) `MultiMatch` το ερώτημα χρησιμοποιείται για την αναζήτηση σε πολλαπλά πεδία (`Title`, `Categories`, και `Content`).
     - **Ενίσχυση**: Η `Title` το πεδίο έχει δοθεί μια ώθηση της `2.0`, καθιστώντας το πιο σημαντικό στην αναζήτηση, και `Categories` έχει μια ώθηση της `1.5`. Αυτό σημαίνει έγγραφα όπου το ερώτημα αναζήτησης εμφανίζεται στον τίτλο ή τις κατηγορίες θα είναι υψηλότερη.
     - **Τύπος ερώτησης**: Χρησιμοποιεί `BestFields`, το οποίο προσπαθεί να βρει το καλύτερο πεδίο αντιστοίχισης για το ερώτημα.
     - **Αφθονία**: Η `Fuzziness.Auto` παράμετρος επιτρέπει την προσέγγιση των αγώνων (π.χ. χειρισμό μικροτυφώνων).

3. **Παιχνιδισμός**:
   
   - Η `Skip` μέθοδος παραλείπει την πρώτη `n` αποτελέσματα ανάλογα με τον αριθμό της σελίδας, υπολογισμένα ως `(page - 1) * pageSize`. Αυτό βοηθά στην περιήγηση μέσω των επικολλημένων αποτελεσμάτων.
   - Η `Size` η μέθοδος περιορίζει τον αριθμό των εγγράφων που επιστρέφονται στο καθορισμένο `pageSize`.

4. **Χειρισμός λάθους**:
   
   - Εάν το ερώτημα αποτύχει, ένα σφάλμα καταγράφεται και επιστρέφεται μια κενή λίστα.

5. **Αποτέλεσμα**:
   
   - Η μέθοδος επιστρέφει μια λίστα των `BlogIndexModel` έγγραφα που ταιριάζουν με τα κριτήρια αναζήτησης.

Έτσι μπορείτε να δείτε ότι μπορούμε να είμαστε εξαιρετικά ευέλικτοι σχετικά με το πώς ψάχνουμε τα δεδομένα μας. Μπορούμε να ψάξουμε για συγκεκριμένα πεδία, μπορούμε να ενισχύσουμε ορισμένα πεδία, μπορούμε ακόμη και να ψάξουμε σε πολλαπλά ευρετήρια.

Ένα BIG πλεονέκτημα είναι η ευκολία qith την οποία μπορούμε να υποστηρίξουμε πολλές γλώσσες. Έχουμε ένα διαφορετικό ευρετήριο για κάθε γλώσσα και μπορούμε να ψάξουμε στο πλαίσιο αυτού του ευρετηρίου. Αυτό σημαίνει ότι μπορούμε να χρησιμοποιήσουμε τον σωστό αναλυτή για κάθε γλώσσα και να έχουμε τα καλύτερα αποτελέσματα.

## Η νέα αναζήτηση API

Σε αντίθεση με το API αναζήτησης που είδαμε στα προηγούμενα μέρη αυτής της σειράς, μπορούμε να απλοποιήσουμε σε μεγάλο βαθμό τη διαδικασία αναζήτησης χρησιμοποιώντας το OpenSearch. Μπορούμε απλά να στείλουμε μήνυμα σε αυτό το ερώτημα και να πάρουμε τα καλά αποτελέσματα πίσω.

```csharp
   [HttpGet]
    [Route("osearch/{query}")]
   [ValidateAntiForgeryToken]
    public async Task<JsonHttpResult<List<SearchResults>>> OpenSearch(string query, string language = MarkdownBaseService.EnglishLanguage)
    {
        var results = await indexService.GetSearchResults(language, query);
        
        var host = Request.Host.Value;
        var output = results.Select(x => new SearchResults(x.Title.Trim(), x.Slug, @Url.ActionLink("Show", "Blog", new{ x.Slug}, protocol:"https", host:host) )).ToList();
        return TypedResults.Json(output);
    }
```

Όπως μπορείτε να δείτε, έχουμε όλα τα δεδομένα που χρειαζόμαστε στο ευρετήριο για να επιστρέψουμε τα αποτελέσματα. Στη συνέχεια μπορούμε να χρησιμοποιήσουμε αυτό για να δημιουργήσουμε ένα URL στο blog post. Αυτό αφαιρεί το φορτίο από τη βάση δεδομένων μας και κάνει τη διαδικασία αναζήτησης πολύ γρηγορότερα.

## Συμπέρασμα

Σε αυτή τη δημοσίευση είδαμε πώς να γράψουμε έναν πελάτη C# για να αλληλεπιδράσει με την περίπτωσή μας OpenSearch.