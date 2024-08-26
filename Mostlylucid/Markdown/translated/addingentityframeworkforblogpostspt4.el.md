# Προσθήκη πλαισίου οντοτήτων για δημοσιεύσεις blog (Pt. 4)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-17T20:00</datetime>

Δείτε μέρη [1](/blog/addingentityframeworkforblogpostspt1) και [2](/blog/addingentityframeworkforblogpostspt2) και [3](/blog/addingentityframeworkforblogpostspt3) για τα προηγούμενα βήματα.

# Εισαγωγή

Σε προηγούμενα μέρη καλύψαμε πώς να δημιουργήσουμε τη βάση δεδομένων, πώς δομούνται οι ελεγκτές και οι απόψεις μας, και πώς λειτουργούσαν οι υπηρεσίες μας. Σε αυτό το μέρος θα καλύψουμε λεπτομέρειες για το πώς να σπείρουμε τη βάση δεδομένων με κάποια αρχικά δεδομένα και πώς λειτουργούν οι υπηρεσίες EF Based.

Ως συνήθως μπορείς να δεις όλη την πηγή για αυτό στο GitHub μου [Ορίστε.](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog), στον πιο διαυγή/blog φάκελο.

[TOC]

# Σπέρνοντας τη Βάση Δεδομένων

Στο προηγούμενο μέρος καλύψαμε τον τρόπο με τον οποίο [αρχικοποίηση και δημιουργία των υπηρεσιών](/blog/addingentityframeworkforblogpostspt2#setup). Σε αυτό το μέρος θα καλύψουμε το πώς να σπείρουμε τη βάση δεδομένων με κάποια αρχικά δεδομένα. Αυτό γίνεται στο `EFBlogPopulator` Μαθήματα. Η τάξη αυτή είναι καταχωρημένη ως υπηρεσία στο `SetupBlog` μέθοδος επέκτασης.

```csharp
    public async Task Populate()
    {
        var posts = await _markdownBlogService.GetPages();
        var languages = _markdownBlogService.LanguageList();

        var languageEntities = await EnsureLanguages(languages);
        await EnsureCategoriesAndPosts(posts, languageEntities);

        await Context.SaveChangesAsync();
    }
```

Μπορείς να το δεις αυτό στο... `Populate` Η μέθοδος που καλούμε για την `_markdownBlogService.GetPages()` Αυτό τρέχει μέσω των αρχείων μας makrdown και κατοικεί ένα μάτσο από `BlogViewModels` που περιέχει όλες τις θέσεις.
Στη συνέχεια, κάνουμε το ίδιο για τις γλώσσες? `translated` φάκελο για όλα τα μεταφρασμένα αρχεία markdown που δημιουργήσαμε χρησιμοποιώντας EasyNMT (δείτε [Ορίστε.](/blog/autotranslatingmarkdownfiles) για το πώς θα το κάνουμε αυτό το μέρος).

## Προσθήκη των γλωσσών

Μετά θα καλέσουμε τον δικό μας. `EnsureLanguages` μέθοδος που εξασφαλίζει ότι όλες οι γλώσσες βρίσκονται στη βάση δεδομένων. Αυτή είναι μια απλή μέθοδος που ελέγχει αν υπάρχει η γλώσσα και αν δεν την προσθέτει στη βάση δεδομένων.

```csharp
  private async Task<List<LanguageEntity>> EnsureLanguages(Dictionary<string, List<string>> languages)
    {
        var languageList = languages.SelectMany(x => x.Value).ToList();
        var currentLanguages = await Context.Languages.Select(x => x.Name).ToListAsync();

        var languageEntities = new List<LanguageEntity>();
        var enLang = new LanguageEntity { Name =MarkdownBaseService.EnglishLanguage };

        if (!currentLanguages.Contains(MarkdownBaseService.EnglishLanguage)) Context.Languages.Add(enLang);
        languageEntities.Add(enLang);

        foreach (var language in languageList)
        {
            if (languageEntities.Any(x => x.Name == language)) continue;

            var langItem = new LanguageEntity { Name = language };

            if (!currentLanguages.Contains(language)) Context.Languages.Add(langItem);

            languageEntities.Add(langItem);
        }

        await Context.SaveChangesAsync(); // Save the languages first so we can reference them in the blog posts
        return languageEntities;
    }
```

Θα δείτε ότι αυτό είναι απλό και απλά εξασφαλίζει ότι όλες οι γλώσσες που πήραμε από τις θέσεις markdown είναι στη βάση δεδομένων? `SaveChanges` για την εξασφάλιση της δημιουργίας των ταυτοτήτων.

### Προσθήκη των Κατηγοριών και Αναρτήσεων

Μετά θα καλέσουμε τον δικό μας. `EnsureCategoriesAndPosts` μέθοδος που εξασφαλίζει ότι όλες οι κατηγορίες και θέσεις βρίσκονται στη βάση δεδομένων. Αυτό είναι λίγο πιο περίπλοκο καθώς πρέπει να διασφαλίσουμε ότι οι κατηγορίες βρίσκονται στη βάση δεδομένων και στη συνέχεια πρέπει να διασφαλίσουμε ότι οι θέσεις βρίσκονται στη βάση δεδομένων.

```csharp
    private async Task EnsureCategoriesAndPosts(
        IEnumerable<BlogPostViewModel> posts,
        List<LanguageEntity> languageEntities)
    {
        var languages = languageEntities.ToDictionary(x => x.Name, x => x);
        var currentPosts = await PostsQuery().ToListAsync();
        foreach (var post in posts)
        {
            var existingCategories = Context.Categories.Local.ToList();
            var currentPost =
                currentPosts.FirstOrDefault(x => x.Slug == post.Slug && x.LanguageEntity.Name == post.Language);
            await AddCategoriesToContext(post.Categories, existingCategories);
            existingCategories = Context.Categories.Local.ToList();
            await AddBlogPostToContext(post, languages[post.Language], existingCategories, currentPost);
        }
    }
```

Εδώ χρησιμοποιούμε το Πλαίσιο.Categories.Τοπικά για να παρακολουθείτε τις κατηγορίες που έχουν προστεθεί στο Πλαίσιο (είναι αποθηκευμένες στη βάση δεδομένων κατά τη διάρκεια της `SaveAsync` Τηλεφώνημα).
Μπορείτε να δείτε ότι καλούμε στο `PostsQuery` μέθοδος μας βασική τάξη η οποία είναι μια απλή μέθοδος που επιστρέφει ένα ερωτηματικό της `BlogPostEntity` Για να ρωτήσουμε τη βάση δεδομένων για τις θέσεις.

```csharp
  protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);
   
```

#### Προσθήκη των κατηγοριών

Στη συνέχεια, καλούμε στο `AddCategoriesToContext` μέθοδος που εξασφαλίζει ότι όλες οι κατηγορίες βρίσκονται στη βάση δεδομένων. Αυτή είναι μια απλή μέθοδος που ελέγχει εάν υπάρχει η κατηγορία και αν δεν την προσθέτει στη βάση δεδομένων.

```csharp
    private async Task AddCategoriesToContext(
        IEnumerable<string> categoryList,
        List<CategoryEntity> existingCategories)
    {
        foreach (var category in categoryList)
        {
            if (existingCategories.Any(x => x.Name == category)) continue;

            var cat = new CategoryEntity { Name = category };

             await Context.Categories.AddAsync(cat);
        }
    }

```

Και πάλι αυτό ελέγχει αν υπάρχει η κατηγορία και αν δεν την προσθέτει στη βάση δεδομένων.

#### Προσθήκη των ιστοσελίδων@ info: whatsthis

Στη συνέχεια, καλούμε στο `AddBlogPostToContext` Με τη μέθοδο αυτή, η `EFBaseService` για να αποθηκεύσετε τη θέση στη βάση δεδομένων.

```csharp
    private async Task AddBlogPostToContext(
        BlogPostViewModel post,
        LanguageEntity postLanguageEntity,
        List<CategoryEntity> categories,
        BlogPostEntity? currentPost)
    {
        await SavePost(post, currentPost, categories, new List<LanguageEntity> { postLanguageEntity });
    }
```

Το κάνουμε αυτό καλώντας το `SavePost` μέθοδος η οποία είναι μια μέθοδος που αποθηκεύει τη θέση στη βάση δεδομένων. Αυτή η μέθοδος είναι λίγο περίπλοκη καθώς πρέπει να ελέγξει αν η θέση έχει αλλάξει και αν να ενημερώσει τη θέση στη βάση δεδομένων.

```csharp

   public async Task<BlogPostEntity?> SavePost(BlogPostViewModel post, BlogPostEntity? currentPost =null ,
        List<CategoryEntity>? categories = null,
        List<LanguageEntity>? languages = null)
    {
        if (languages == null)
            languages = await Context.Languages.ToListAsync();

    var postLanguageEntity = languages.FirstOrDefault(x => x.Name == post.Language);
        if (postLanguageEntity == null)
        {
            Logger.LogError("Language {Language} not found", post.Language);
            return null;
        }
        categories ??= await Context.Categories.Where(x => post.Categories.Contains(x.Name)).ToListAsync();
         currentPost ??= await PostsQuery().Where(x=>x.Slug == post.Slug).FirstOrDefaultAsync();
        try
        {
            var hash = post.HtmlContent.ContentHash();
            var currentCategoryNames = currentPost?.Categories.Select(x => x.Name).ToArray() ?? Array.Empty<string>();
            var categoriesChanged = false;
            if (!currentCategoryNames.All(post.Categories.Contains) ||
                !post.Categories.All(currentCategoryNames.Contains))
            {
                categoriesChanged = true;
                Logger.LogInformation("Categories have changed for post {Post}", post.Slug);
            }

            var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
            var titleChanged = currentPost?.Title != post.Title;
            if (!titleChanged && !dateChanged && hash == currentPost?.ContentHash && !categoriesChanged)
            {
                Logger.LogInformation("Post {Post} has not changed", post.Slug);
                return currentPost;
            }

            
            var blogPost = currentPost ?? new BlogPostEntity();
            
            blogPost.Title = post.Title;
            blogPost.Slug = post.Slug;
            blogPost.OriginalMarkdown = post.OriginalMarkdown;
            blogPost.HtmlContent = post.HtmlContent;
            blogPost.PlainTextContent = post.PlainTextContent;
            blogPost.ContentHash = hash;
            blogPost.PublishedDate = post.PublishedDate;
            blogPost.LanguageEntity = postLanguageEntity;
            blogPost.Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList();

            if (currentPost != null)
            {
                Logger.LogInformation("Updating post {Post}", post.Slug);
                Context.BlogPosts.Update(blogPost); // Update the existing post
            }
            else
            {
                Logger.LogInformation("Adding new post {Post}", post.Slug);
                Context.BlogPosts.Add(blogPost); // Add a new post
            }
            return blogPost;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error adding post {Post}", post.Slug);
        }

        return null;
    }

```

Όπως μπορείτε να δείτε αυτό έχει μια πολλή ανίχνευση αλλαγών για να εξασφαλίσει ότι δεν ξαναπροσθέτουμε θέσεις που δεν έχουν αλλάξει. Ελέγχουμε το χασίς του περιεχομένου, τις κατηγορίες, την ημερομηνία και τον τίτλο. Αν κάποιο από αυτά έχουν αλλάξει, ενημερώνουμε τη θέση στη βάση δεδομένων.

Ένα πράγμα που πρέπει να παρατηρήσετε είναι πόσο ενοχλητικός έλεγχος ενός DateTimeOffset είναι; πρέπει να το μετατρέψουμε σε UTC και στη συνέχεια να πάρει την ημερομηνία για να το συγκρίνουν. Αυτό συμβαίνει επειδή η `DateTimeOffset` έχει ένα στοιχείο του χρόνου και θέλουμε να συγκρίνουμε ακριβώς την ημερομηνία.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# Συμπέρασμα

Τώρα έχουμε ένα πλήρως λειτουργικό σύστημα blog που μπορεί να κατοικηθεί από τα αρχεία markdown και να μεταφραστεί αρχεία markdown. Στο επόμενο μέρος θα καλύψουμε την απλή υπηρεσία που χρησιμοποιούμε για την εμφάνιση θέσεων αποθηκευμένων στη βάση δεδομένων.