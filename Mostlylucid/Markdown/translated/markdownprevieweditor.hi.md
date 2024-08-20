# एक सादाएमई मार्क पूर्वावलोकन संपादक सर्वर साइड रेंडरिंग के साथ.

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024- 0. 1418टी17: 45</datetime>

# परिचय

एक बात मैंने सोचा था कि यह जोड़ने के लिए मज़ेदार होगा...... साइट पर लेखों के लिए देख करने का एक तरीका है... / मैं यह एक सरल निशान संपादक है जो सिंदूरी तथा एक सर्वर बाज़ू का प्रयोग करता है जो कि चिह्निडी लाइब्रेरी का उपयोग करता है जिसे मैं इन ब्लॉग पोस्टों को प्रस्तुत करने के लिए प्रयोग करता हूँ.

ब्लॉग पोस्टों के शीर्षक में अब आप किसी वर्ग की सूची में देख सकते हैं 'यह' बटन
![बटन संपादित करें](editbutton.png)___ यदि आप इस पर क्लिक करते हैं तो आप एक पृष्ठ मिलेगा जिसमें निशान नीचे दिए गए मूल्य तथा चिह्न चिह्नों का पूर्वावलोकन होगा. आप चिह्न को संपादित कर सकते हैं तथा अपने वास्तविक समय में परिवर्तनों को देख सकते हैं (शिष्टि- Ctrl-R) (या Mac- Alt- R) या ताज़ा करने के लिए एंटर करें. आप भी हिट कर सकते हैं <i class="bx bx-save"></i> अपने स्थानीय मशीन को चिह्न नीचे की फ़ाइल सहेजने के लिए बटन.

CURSURS यह फ़ाइल को सर्वर में सहेज नहीं करता है, यह सिर्फ आपके स्थानीय मशीन में संचिका को डाउनलोड करता है. मैं तुम्हें अपने ब्लॉग पोस्ट संपादित करने नहीं जा रहा हूँ!

[विषय

# कोड

## मेरी बकवास आई- एम्प्लिक जावास्क्रिप्ट

जावा स्क्रिप्ट बहुत सरल है और मैं सिर्फ इसे छोड़ दिया है `scripts` खंड का खंड `Edit.cshtml` उस समय पृष्ठ.

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

के रूप में आप इस ट्रिगर पर देखेंगे `load` और मामले की तदबीर करें `keydown` सादा पाठ संपादक पर घटना. यदि कुंजी दबाया जाता है `Ctrl-Alt-R` या `Enter` तब यह संपादक की सामग्री को वेब क्यूपी अंत बिन्दु पर भेजता है जो कि चिह्न को नीचे रखता है तथा एचटीएमएल को लौटाता है. इसे तब अनुवादित किया गया है `renderedcontent` पानी ।

जैसा कि मेरा ब्लॉग पोस्ट्स एक में हैंडल किया जाता है `BlogPostViewModel` यह तब लौटा JSON की व्याख्या करता है और शीर्षक को भरता है, प्रकाशित तिथि और वर्ग. यह भी चलाता है `mermaid` और `highlight.js` किसी भी डायग्राम तथा कोड ब्लॉक रेंडर करने के लिए लाइब्रेरी.

## C# कोड

### संपादन नियंत्रक

पहली बार मैंने एक नया नियंत्रक जोड़ा जिसे कहा जाता है `EditorController` जिस (ख़ुदा) के पास एकल है (हर तरफ से) उन लोगों को जो कुछ (दुनिया में) किया जाता है `Edit` लौटाता है `Edit.cshtml` दृश्य.

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

आप यह बहुत सरल है, यह Iswidewidemid पर नए तरीकों का उपयोग करता है इस पोस्ट को ulug से प्राप्त करने के लिए और फिर लौटा देता है `Editor.cshtml` दृश्य के साथ `EditorModel` कौन सा निशान पहले से मौजूद है `BlogPostViewModel`.

वह `Editor.cshtml` दृश्य एक सरल पृष्ठ के साथ है `textarea` निशान चिह्न तथा एक के लिए `div` चूँकि क़ुरैश को जाड़े और गर्मी के सफ़र से मानूस कर दिया है यह भी एक है `button` स्थानीय मशीन को निशान नीचे बचाने के लिए.

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

यह आपके द्वारा प्रकाशित ब्लॉग पोस्ट को वास्तविक ब्लॉग पोस्ट के रूप में कॉन्फ़िगर करने की कोशिश करता है. यह भी एक है `script` नीचे का भाग जिसमें जावास्क्रिप्ट कोड मैंने पहले दिखाया है.

### वेबपीआई अंतपाइंट

इस के लिए वेबपीआई अंत बिन्दु सिर्फ निशानित सामग्री लेता है तथा अनुवाद किए गए HTML सामग्री को लौटाता है. यह काफी सरल है और बस उपयोग करता है `IMarkdownService` निशान नीचे देने के लिए.

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

यह काफी सरल है और बस लौटाता है `BlogPostViewModel` जिसे जावास्क्रिप्ट ने वर्गीकृत किया है और जिसे वह ठहरा रहा है `renderedcontent` पानी ।

# ऑन्टियम

यह देखने के लिए एक सरल तरीका है. और मुझे लगता है कि यह साइट के लिए एक अच्छा जोड़ है. मुझे यकीन है कि इस करने के लिए बेहतर तरीके हैं लेकिन यह मेरे लिए काम करता है. मुझे आशा है कि आप इसे उपयोगी पाते हैं और यदि आप सुधार के लिए कोई सुझाव है मुझे पता है.