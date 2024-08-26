# Ένας επεξεργαστής προεπισκόπησης SimpleMDE Markdown με την απόδοση πλευρά του διακομιστή.

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-18T17:45</datetime>

# Εισαγωγή

Ένα πράγμα που σκέφτηκα ότι θα ήταν διασκεδαστικό να προσθέσω είναι ένας τρόπος για να δούμε το markdown για τα άρθρα στο site με μια ζωντανή απόδοση του markdown. Αυτό είναι ένα απλό markdown επεξεργαστή που χρησιμοποιεί SimpleMDE και ένα server πλευρά απόδοση του markdown χρησιμοποιώντας τη βιβλιοθήκη markkdig που χρησιμοποιώ για να καταστήσει αυτές τις δημοσιεύσεις blog.

Στην επικεφαλίδα των αναρτήσεων blog δίπλα στη λίστα κατηγοριών θα δείτε τώρα ένα πλήκτρο 'Επεξεργασία'
![Επεξεργασία κουμπιού](editbutton.png). Αν κάνετε κλικ σε αυτό θα πάρετε μια σελίδα που έχει έναν επεξεργαστή markdown και μια προεπισκόπηση του markdown. Μπορείτε να επεξεργαστείτε το markdown και να δείτε τις αλλαγές σε πραγματικό χρόνο (χτυπήστε Ctrl-Alt-R (ή ~ Alt-R σε Mac) ή Εισάγετε για ανανέωση). Μπορείτε επίσης να χτυπήσει το <i class="bx bx-save"></i> κουμπί για να αποθηκεύσετε το αρχείο markdown στην τοπική μηχανή σας.

Σίγουρα αυτό δεν αποθηκεύει το αρχείο στο διακομιστή, απλά μεταφορτώνει το αρχείο στο τοπικό σας μηχάνημα. Δεν θα σε αφήσω να επεξεργαστείς τις αναρτήσεις μου στο blog!

[TOC]

# Ο κώδικας

## Η σκατένια μου JavaScript

Το Javascript είναι αρκετά απλοϊκό και το έχω αφήσει στο `scripts` τμήμα της `Edit.cshtml` Page προς το παρόν.

```javascript
        window.addEventListener('load', function () {
            console.log('Page loaded without refresh');

            // Trigger on change event of SimpleMDE editor
            window.simplemde.codemirror.on("keydown", function(instance, event) {
            let triggerUpdate= false;
                // Check if the Enter key is pressed
                if ((event.ctrlKey || event.metaKey) && event.altKey && event.key.toLowerCase() === "r") {
                    event.preventDefault(); // Prevent the default behavior (e.g., browser refresh)
                    triggerUpdate = true;
                    }
                    if (event.key === "Enter")
                    {
                        triggerUpdate = true;
                    }

                if (triggerUpdate) {
        
                    var content = simplemde.value();

                    // Send content to WebAPI endpoint
                    fetch('/api/editor/getcontent', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({ content: content })  // JSON object with 'content' key
                    })
                        .then(response => response.json())  // Parse the JSON response
                        .then(data => {
                            // Render the returned HTML content into the div
                            document.getElementById('renderedcontent').innerHTML = data.htmlContent;
                            document.getElementById('title').innerHTML  = data.title;// Assuming the returned JSON has an 'htmlContent' property
                            const date = new Date(data.publishedDate);

                            const formattedDate = new Intl.DateTimeFormat('en-GB', {
                                weekday: 'long',  // Full weekday name
                                day: 'numeric',   // Day of the month
                                month: 'long',    // Full month name
                                year: 'numeric'   // Full year
                            }).format(date);
                            
                            document.getElementById('publishedDate').innerHTML = formattedDate;
                           populateCategories(data.categories);
                            
                            
                            mermaid.run();
                            hljs.highlightAll();
                        })
                        .catch(error => console.error('Error:', error));
                }
            });

            function populateCategories(categories) {
                var categoriesDiv = document.getElementById('categories');
                categoriesDiv.innerHTML = ''; // Clear the div

                categories.forEach(function(category) {
                    // Create the span element
                    let span = document.createElement('span');
                    span.className = 'inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white'; // Apply the style class
                    span.textContent = category;

                    // Append the span to the categories div
                    categoriesDiv.appendChild(span);
                });
            }
        });
```

Όπως θα δείτε αυτή η σκανδάλη στο `load` event, και στη συνέχεια ακούει για το `keydown` event on the SimpleMDE editor. Αν το πλήκτρο πατηθεί είναι `Ctrl-Alt-R` ή `Enter` Στη συνέχεια στέλνει το περιεχόμενο του επεξεργαστή σε ένα τελικό σημείο WebAPI που καθιστά το σήμα down και επιστρέφει το HTML. Αυτό μεταφράζεται στη συνέχεια στο `renderedcontent` div.

Καθώς οι αναρτήσεις στο blog μου χειρίζονται σε ένα `BlogPostViewModel` Στη συνέχεια παριστάνει τον επιστρεφόμενο JSON και κοσμεί τον τίτλο, δημοσιεύεται ημερομηνία και κατηγορίες. Επίσης, διαχειρίζεται το `mermaid` και `highlight.js` βιβλιοθήκες για την απόδοση διαγραμμάτων και μπλοκ κώδικα.

## Ο κωδικός C#

### Ο επεξεργαστής ελεγκτή

Πρώτον, πρόσθεσα ένα νέο ελεγκτή που ονομάζεται `EditorController` που έχει μια ενιαία δράση που ονομάζεται `Edit` η οποία επιστρέφει το `Edit.cshtml` Θέα.

```csharp
       [HttpGet]
    [Route("edit")]
    public async Task<IActionResult> Edit(string? slug = null, string language = "")
    {
        if (slug == null)
        {
            return View("Editor", new EditorModel());
        }

        var blogPost = await markdownBlogService.GetPageFromSlug(slug, language);
        if (blogPost == null)
        {
            return NotFound();
        }

        var model = new EditorModel { Markdown = blogPost.OriginalMarkdown, PostViewModel = blogPost };
        return View("Editor", model);
    }
```

Θα δείτε ότι αυτό είναι αρκετά απλό, χρησιμοποιεί νέες μεθόδους στο IMarkdownBlogService για να πάρει τη δημοσίευση blog από το γυμνοσάλιαγκα και στη συνέχεια επιστρέφει το `Editor.cshtml` άποψη με το `EditorModel` που περιέχει το σήμα και το `BlogPostViewModel`.

Η `Editor.cshtml` προβολή είναι μια απλή σελίδα με ένα `textarea` για το markdown και a `div` για το προσδιωρισμένο σήμα. Έχει επίσης ένα `button` για να αποθηκεύσετε το σήμα προς τα κάτω στην τοπική μηχανή.

```razor
@model Mostlylucid.Models.Editor.EditorModel

<div class="min-h-screen bg-gray-100">
    <p class="text-blue-dark dark:text-blue-light">This is a viewer only at the moment see the article <a asp-action="Show" asp-controller="Blog" asp-route-slug="markdownprevieweditor" class="text-blue-dark dark:text-blue-light">on how this works</a>.</p>
    <div class="container mx-auto p-0">
        <p class="text-blue-dark dark:text-blue-light">To update the preview hit Ctrl-Alt-R (or ⌘-Alt-R on Mac) or Enter to refresh. The Save <i class="bx bx-save"></i> icon lets you save the markdown file to disk </p>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <!-- Column 1 -->
            <div class="bg-white dark:bg-custom-dark-bg  p-0 rounded shadow-md">
                <textarea class="markdowneditor hidden" id="markdowneditor">@Model.Markdown</textarea>

            </div>

            <!-- Column 2 -->
            <div class="bg-white dark:bg-custom-dark-bg p-0 rounded shadow-md">
                <p class="text-blue-dark dark:text-blue-light">This is a preview from the server running through my markdig pipeline</p>
                <div class="border-b border-grey-lighter pb-2 pt-2 sm:pb-2" id="categories">
                    @foreach (var category in Model.PostViewModel.Categories)
                    {
                        <span
                            class="inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white">@category</span>

                    }
                </div>
                <h2 class="pb-2 block font-body text-3xl font-semibold leading-tight text-primary dark:text-white sm:text-3xl md:text-3xl" id="title">@Model.PostViewModel.Title</h2>
                <date id="publishedDate" class="py-2">@Model.PostViewModel.PublishedDate.ToString("D")</date>
                <div class="prose prose max-w-none border-b py-2 text-black dark:prose-dark sm:py-2" id="renderedcontent">
                    @Html.Raw(Model.PostViewModel.HtmlContent)
                </div>
            </div>
        </div>
    </div>
</div>
```

Αυτό προσπαθεί να κάνει το βραβευμένο Blog post να κοιτάξει όσο το δυνατόν πιο κοντά στο πραγματικό blog post. Έχει επίσης ένα `script` τμήμα στο κάτω μέρος που περιέχει τον κωδικό JavaScript που έδειξα νωρίτερα.

### Το WebAPI Endpoint

Το τελικό σημείο WebAPI για αυτό απλά παίρνει το περιεχόμενο markdown και επιστρέφει το μεταδιδόμενο περιεχόμενο HTML. Είναι πολύ απλό και απλά χρησιμοποιεί το `IMarkdownService` για να καταστρέψει το σήμα.

```csharp
 [Route("api/editor")]
[ApiController]
public class Editor(IMarkdownBlogService markdownBlogService) : ControllerBase
{
    public class ContentModel
    {
        public string Content { get; set; }
    }

    [HttpPost]
    [Route("getcontent")]
    public IActionResult GetContent([FromBody] ContentModel model)
    {
        var content =  model.Content.Replace("\n", Environment.NewLine);
        var blogPost = markdownBlogService.GetPageFromMarkdown(content, DateTime.Now, "");
        return Ok(blogPost);
    }
}
```

Αυτό είναι πολύ απλό και απλά επιστρέφει το `BlogPostViewModel` που στη συνέχεια αναπαρίσταται από το JavaScript και αποδίδεται στο `renderedcontent` div.

# Συμπέρασμα

Αυτός είναι ένας απλός τρόπος για να προεπιθεωρήσετε το περιεχόμενο markdown και νομίζω ότι είναι μια ωραία προσθήκη στην ιστοσελίδα. Είμαι σίγουρος ότι υπάρχουν καλύτεροι τρόποι για να το κάνουμε αυτό, αλλά αυτό λειτουργεί για μένα. Ελπίζω να το βρείτε χρήσιμο και αν έχετε κάποιες προτάσεις για βελτιώσεις παρακαλώ ενημερώστε με.