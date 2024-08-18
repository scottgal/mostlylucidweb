# A بسيط MEDE علامة معاينة محرّر مع خادم جانب.

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-08-18-TT17: 45</datetime>

# أولاً

شيء واحد اعتقدت انه سيكون من الممتع ان اضيفه هو طريقة للنظر الى الهدف النهائي للمقالات في الموقع هذا محرّر بسيط يستخدم SlimeMDE و جانب خادم من رمز إلى أسفل باستخدام مكتبة ماركديج التي أستخدمها لترجمة هذه المدوّنات.

في عنوان التدوينات بجانب قائمة الفئات سترى الآن زر "edit"
![حرر هذا الأمر](editbutton.png)/ / / / إذا ضغطت على هذا ستحصل على صفحة تحتوي على محرّر و معاينة للعلامة. يمكنك تحرير العلامة التنازلية ورؤية التغييرات في الوقت الحقيقي (Het Ctrl-Alt-R (أو o-Alt-R على ماك) أو إدخال إلى التنشيط). يمكنك أيضاً ضرب <i class="bx bx-save"></i> زر إلى حفظ ملفّ إلى محليّ آلة.

بالطبع هذا لا يحفظ الملف للخادم، بل يقوم فقط بتنزيل الملف إلى آلتك المحلية. لن أسمح لك بتحرير مدوّنتي!

[رابعاً -

# ألف - القانون

## سكري جافا سكريبتي

المخطوطة مبسطة جداً وقد تركتها للتو في `scripts` من `Edit.cshtml` في هذه اللحظة.

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

كما سترى هذا الزناد على `load` و من ثمّ يُستمعُ لـ `keydown` حدث خاص بمحرّر النظام المبسّط للمحرّر. إذا كان المفتاح مضغوط هو: `Ctrl-Alt-R` أو `Enter` ثم يرسل محتوى المحرر إلى نقطة نهاية WebAPI التي تجعل العلامة أسفل وترجع HTML. بعد ذلك يُحوّل هذا إلى `renderedcontent` )٤(

كما يتم التعامل مع مقالات مدونتي في `BlogPostViewModel` ثم يتنازل عن JSON المعاد ويصف العنوان، تاريخ النشر والفئات. كما أنها تدير أيضاً `mermaid` وقد عقد مؤتمراً بشأن `highlight.js` المكتبات لترجمة أي رسوم بيانية أو كتلات رمزية.

## رمز الرمز C

### 

أولاً أضفت متحكماً جديداً يدعى `EditorController` التي لها عمل واحد ، يُدَّعَى `Edit` الـ الدالة الـ `Edit.cshtml` وجهة نظر.

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

سترون أن هذا بسيط جداً، إنه يستخدم أساليب جديدة على خدمة IMarkdown BlogServ `Editor.cshtml` إلى الأمين العام مـن `EditorModel` التي تحتوي على العلامة التنازلية و `BlogPostViewModel`.

الـ `Editor.cshtml` هو a بسيط صفحة مع a `textarea` دال - التدابيـر والخطـوء `div` من أجل الهدف المُتَخَلِّص. كما أن لها أيضاً `button` -لإنقاذ الهدف للآلة المحلية.

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

هذا المحاول لجعل مدوّنة المدوّنة المُقدّمة تبدو أقرب إلى المدوّنة الفعلية قدر الإمكان. كما أن لها أيضاً `script` في الجزء السفلي الذي يحتوي على شفرة جافاسكربت التي عرضتها سابقاً.

### نقطة النهاية في ويپپاپاپاپي

نقطة نهاية WebAPI لهذا فقط يأخذ محتوى العلامة السفلية ويرجع محتوى HTML. انه بسيط جداً وفقط يستخدم `IMarkdownService` -لإبراز الهدف.

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

هذا بسيط جداً وفقط ترجع `BlogPostViewModel` ثم يُقَسَّر بالكتاب المُجَوَّل والمُنَجَّه إلى `renderedcontent` )٤(

# في الإستنتاج

هذه طريقة بسيطة لمعاينة محتوى العلامة التنازلية وأعتقد أنها إضافة جميلة للموقع. أنا متأكد من أن هناك طرق أفضل للقيام بذلك ولكن هذا يعمل بالنسبة لي. وآمل أن تجدوا ذلك مفيداً، وإذا كان لديكم أي اقتراحات لإدخال تحسينات، فاسمحوا لي أن أعلمكم.