# A SimpleMDE Markdown Preview Editor with server side rendering.

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-18T17:45</datetime>

# Introduction
One thing I thought it would be fun to add is a way to look at the markdown for the articles on the site with a live rendering of the markdown. This is a simple markdown editor that uses SimpleMDE and a server side rendering of the markdown using the markkdig library which I use to render these blog posts.

In the heading of blog posts next to the categories list you'll now see an 'edit' button
![Edit Button](editbutton.png). If you click this you'll get a page which has a markdown editor and a preview of the markdown. You can edit the markdown and see the changes in real time (hit Ctrl-Alt-R (or ⌘-Alt-R on Mac) or Enter to refresh). You can also hit the <i class="bx bx-save"></i> button to save the markdown file to your local machine.

OF COURSE this doesn't save the file to the server, it just downloads the file to your local machine. I'm not going to let you edit my blog posts!

[TOC]

# The Code

## My crappy JavaScript
The Javascript is pretty simplistic and I've just left it in the `scripts` section of the `Edit.cshtml` page at the moment. 

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
As you'll see this triggers on the `load` event, and then listens for the `keydown` event on the SimpleMDE editor. If the key pressed is `Ctrl-Alt-R` or `Enter` then it sends the content of the editor to a WebAPI endpoint which renders the markdown and returns the HTML. This is then rendered into the `renderedcontent` div.

As my blog posts are handled in a `BlogPostViewModel` it then parses the returned JSON and populates the title, published date and categories. It also runs the `mermaid` and `highlight.js` libraries to render any diagrams and code blocks.

## The C# Code

### The Edit Controller
Firstly I added a new controller called `EditorController` which has a single action called `Edit` which returns the `Edit.cshtml` view.

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
You'll see this is pretty simple, it uses new methods on the IMarkdownBlogService to get the blog post from the slug and then returns the `Editor.cshtml` view with the `EditorModel` which contains the markdown and the `BlogPostViewModel`.

The `Editor.cshtml` view is a simple page with a `textarea` for the markdown and a `div` for the rendered markdown. It also has a `button` to save the markdown to the local machine.

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
This tries to make the rendered Blog post look as close to the actual blog post as possible. It also has a `script` section at the bottom which contains the JavaScript code I showed earlier.


### The WebAPI Endpoint
The WebAPI endpoint for this just takes the markdown content and returns the rendered HTML content. It's pretty simple and just uses the `IMarkdownService` to render the markdown.

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
This is pretty simple and just returns the `BlogPostViewModel` which is then parsed by the JavaScript and rendered into the `renderedcontent` div.

# In Conclusion
This is a simple way to preview markdown content and I think it's a nice addition to the site. I'm sure there are better ways to do this but this works for me. I hope you find it useful and if you have any suggestions for improvements please let me know.