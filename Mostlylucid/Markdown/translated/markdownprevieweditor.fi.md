# SimpleMDE Markdown Esikatselueditori, jossa on palvelimen sivurenderointi.

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-18T17:45</datetime>

# Johdanto

Yksi asia, jonka ajattelin, että olisi hauska lisätä, on tapa katsoa kohteen artikkelien maaliviivaa suoralla renderöinnillä maaliviivasta. Tämä on yksinkertainen markdown-editori, joka käyttää simppeliä MDE:tä ja palvelimen sivurenderointia markownista käyttäen Markkdig-kirjastoa, jonka avulla teen nämä blogikirjoitukset.

Blogikirjoitusten otsikossa kategorialuettelon vieressä näet nyt edit-painikkeen
![Muokkaa painiketta](editbutton.png)...................................................................................................................................... Jos klikkaat tätä, saat sivun, jossa on markdown-editori ja esikatselu markdownista. Voit muokata markdownia ja nähdä reaaliajassa tapahtuneet muutokset (lyö Ctrl-Alt-R (tai Mac-Alt-R) tai Enter to virkistä). Voit myös osua <i class="bx bx-save"></i> painike tallentaa markown-tiedoston paikalliselle koneellesi.

Tämä ei tietenkään tallenna tiedostoa palvelimelle, se vain lataa tiedoston paikalliselle koneellesi. En anna sinun editoida blogikirjoituksiani!

[TÄYTÄNTÖÖNPANO

# Koodi

## Minun surkea JavaScriptini

Javascript on aika yksioikoinen, ja olen juuri jättänyt sen `scripts` Otsikko on jaettu seuraavasti: `Edit.cshtml` sivu tällä hetkellä.

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

Kuten näette, tämä laukaisin on `load` Tapahtuma, ja sitten kuunnellaan `keydown` tapahtuma SimpleMDE-editorilla. Jos näppäintä painetaan `Ctrl-Alt-R` tai `Enter` Sitten se lähettää editorin sisällön WebAPI:n päätetapahtumaan, joka palauttaa markan ja palauttaa HTML:n. Tämä käännetään sitten osaksi `renderedcontent` div.

Kuten blogikirjoituksiani käsitellään `BlogPostViewModel` sen jälkeen se parsii palautettua JSONia ja kansoittaa otsikon, julkaistut päivämäärät ja kategoriat. Se myös pyörittää `mermaid` sekä `highlight.js` Kirjastot tekevät kaavioita ja koodilohkoja.

## C#-koodi

### Muokkaa ohjainta

Ensinnäkin lisäsin uuden ohjaimen nimeltä `EditorController` jolla on yksi ainoa toimi nimeltään `Edit` joka palauttaa `Edit.cshtml` näkymä.

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

Huomaat, että tämä on melko yksinkertaista, se käyttää uusia menetelmiä IMarkdownBlogService saada blogikirjoituksen etana ja sitten palauttaa `Editor.cshtml` näköyhteys `EditorModel` joka sisältää marketdown ja `BlogPostViewModel`.

Erytropoietiini `Editor.cshtml` näkymä on yksinkertainen sivu, jossa on `textarea` markkuun ja a `div` Renderöidylle maalille. Sillä on myös `button` Tallentaa markdown paikalliselle koneelle.

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

Tämä pyrkii tekemään renderoidusta blogikirjoituksesta mahdollisimman lähellä varsinaista blogikirjoitusta. Sillä on myös `script` alla kohta, joka sisältää aiemmin näyttämäni JavaScript-koodin.

### WebAPIn päätepiste

WebAPI:n päätepiste tässä vain ottaa maaliviivan sisällön ja palauttaa renderoidun HTML-sisällön. Se on aika yksinkertainen ja käyttää vain `IMarkdownService` maalintekoon.

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

Tämä on aika yksinkertaista ja palauttaa vain `BlogPostViewModel` joka sitten jäsennetään JavaScript ja renderoidaan `renderedcontent` div.

# Johtopäätöksenä

Tämä on yksinkertainen tapa esikatsella marketdown-sisältöä ja mielestäni se on mukava lisä sivustoon. Olen varma, että tähän on parempiakin tapoja, mutta tämä toimii minulle. Toivon, että pidät sitä hyödyllisenä ja jos sinulla on parannusehdotuksia, ilmoita minulle.