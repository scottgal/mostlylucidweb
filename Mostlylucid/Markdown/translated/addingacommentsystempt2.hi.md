# एक टिप्पणी तंत्र पार्ट 2 - सहेजना टिप्पणी जोड़ा जा रहा है

<!--category-- ASP.NET, Alpine.js, HTMX  -->
<datetime class="hidden">2024- 0. 3131T09: 00</datetime>

# परिचय

पिछले में [इस श्रृंखला में भाग](/blog/addingacommentsystempt1)मैंने टिप्पणी तंत्र के लिए डाटाबेस सेट किया. इस पोस्ट में, मैं इस बात को कवर करूंगा कि कैसे टिप्पणीों को बचाना ग्राहकों के पक्ष में और AUNT को बचाने में मदद कर रहे हैं.NT कोर.

[विषय

## नया टिप्पणी जोड़ें

### `_CommentForm.cshtml`

यह एक Razrammit का नज़रिया है जिसमें एक नयी टिप्पणी जोड़ने का तरीका है । आप पहले लोड पर देख सकते हैं यह कॉल करने के लिए `window.mostlylucid.comments.setup()` जो संपादक प्रारंभ करता है. यह एक सादा पाठ है जो प्रयोग करता है `SimpleMDE` बढ़िया पाठ संपादन की अनुमति देने के लिए संपादक

```razor
@model Mostlylucid.Models.Comments.CommentInputModel

 
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
    <section id="commentsection" ></section>
    
    <input type="hidden" asp-for="BlogPostId" />
    <div asp-validation-summary="All" class="text-red-500" id="validationsummary"></div>
    <p class="font-body text-lg font-medium text-primary dark:text-white pb-8">Welcome @Model.Name please comment below.</p>
    
    <div asp-validation-summary="All" class="text-red-500" id="validationsummary"></div>
    <!-- Username Input -->
    <div class="flex space-x-4"> <!-- Flexbox to keep Name and Email on the same line -->

        <!-- Username Input -->
        <label class="input input-bordered flex items-center gap-2 mb-2 dark:bg-custom-dark-bg bg-white w-1/2">
            <i class='bx bx-user'></i>
            <input type="text" class="grow text-black dark:text-white bg-transparent border-0"
                   asp-for="Name" placeholder="Name (required)" />
        </label>

        <!-- Email Input -->
        <label class="input input-bordered flex items-center gap-2 mb-2 dark:bg-custom-dark-bg bg-white w-1/2">
            <i class='bx bx-envelope'></i>
            <input type="email" class="grow text-black dark:text-white bg-transparent border-0"
                   asp-for="Email" placeholder="Email (optional)" />
        </label>

    </div>

    <textarea id="commenteditor" class="hidden w-full h-44 dark:bg-custom-dark-bg bg-white text-black dark:text-white rounded-2xl"></textarea>

    <input type="hidden" asp-for="ParentId"></input>
    <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
</div>
```

यहाँ हम यू.js का उपयोग करते हैं `x-init` संपादक प्रारंभ करने के लिए फोन करें. यह एक सादा पाठ है जो प्रयोग करता है `SimpleMDE` बढ़िया पाठ संपादन की अनुमति देने के लिए संपादक (क्योंकि क्यों नहीं:)

```html
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
```

#### `window.mostlylucid.comments.setup()`

इस जीवन में `comment.js` आसान संपादक के लिए जिम्मेदार है.

```javascript
    function setup ()  {
    if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
        mostlylucid.simplemde.initialize('commenteditor', true);
    } else {
        console.error("simplemde is not initialized correctly.");
    }
};
```

यह एक सरल फंक्शन है जो कि जांच करता है यदि `simplemde` वस्तु आरंभीकृत है और यदि ऐसा है `initialize` इस पर फंक्शन.

## टिप्पणी सहेज रहा है

टिप्पणी को सहेजने के लिए हम HMMX का उपयोग कर एक PAX करने के लिए `CommentController` जो तब टिप्पणी को डाटाबेस में सहेजता है.

```razor
  <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
```

यह उपयोग करता है [एटीएम टैग सहायक](https://www.nuget.org/packages/Htmx.TagHelpers) पीछे ले जाने के लिए `CommentController` और फिर उस आकार को नई टिप्पणी के साथ बदलता है.

तो फिर हम में हुक `mostlylucid.comments.setValues($event)` जिसे हम भरने के लिए इस्तेमाल करते हैं `hx-values` ट्रिअर (यह केवल सरल रूप से अद्यतन किए जाने के लिए आवश्यक है).

```javascript
    function setValues (evt)  {
    const button = evt.currentTarget;
    const element = mostlylucid.simplemde.getinstance('commenteditor');
    const content = element.value();
    const email = document.getElementById("Email");
    const name = document.getElementById("Name");
    const blogPostId = document.getElementById("BlogPostId");

    const parentId = document.getElementById("ParentId")
    const values = {
        content: content,
        email: email.value,
        name: name.value,
        blogPostId: blogPostId.value,
        parentId: parentId.value
    };

    button.setAttribute('hx-vals', JSON.stringify(values));
};
}
```

### टिप्पणी नियंत्रक

संदेश नियंत्रण `save-comment` इस डाटाबेस में टिप्पणी को सहेजने के लिए क्रिया आवश्यक है. यह ब्लॉग मालिक को ई- मेल भी भेजता है जब कोई टिप्पणी जोड़ी जाती है.

```csharp
    [HttpPost]
    [Route("save-comment")]
    public async Task<IActionResult> Comment([Bind(Prefix = "")] CommentInputModel model )
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_CommentForm", model);
        }
        var postId = model.BlogPostId;
        ;
        var name = model.Name ?? "Anonymous";
        var email = model.Email ?? "Anonymous";
        var comment = model.Content;

        var parentCommentId = model.ParentId;
        
      var htmlContent=  await commentService.Add(postId, parentCommentId, name, comment);
      if (string.IsNullOrEmpty(htmlContent))
      {
          ModelState.AddModelError("Content", "Comment could not be saved");
          return PartialView("_CommentForm", model);
      }
        var slug = await blogService.GetSlug(postId);
        var url = Url.Action("Show", "Blog", new {slug }, Request.Scheme);
        var commentModel = new CommentEmailModel
        {
            SenderEmail = email ?? "",
            Comment = htmlContent,
            PostUrl = url??string.Empty,
        };
        await sender.SendEmailAsync(commentModel);
        model.Content = htmlContent;
        return PartialView("_CommentResponse", model);
    }
```

आप देखेंगे कि यह कुछ बातें करता है:

1. डीबी में टिप्पणी जोड़ता है (यह भी चिह्नित करें)
2. यदि वहाँ कोई त्रुटि है तो यह त्रुटि के साथ फ़ॉर्म लौटाता है. (न तो अब भी मेरे पास एक बढ़ईी कार्य है जो सीक्ख के लिए त्रुटि पैदा करता है).
3. यदि टिप्पणी सहेजा जाता है तो यह टिप्पणी और पोस्ट यूआरएल के साथ मुझे ईमेल भेजता है.

यह पोस्ट यूआरएल तब मुझे पोस्ट पर क्लिक करने देता है, यदि मैं मेरे रूप में लॉग कर रहा हूँ (जब मैं लॉग कर रहा हूँ) [मेरे गूगल AHARAT बात](/blog/addingidentityfreegoogleauth)___ यह सिर्फ मेरे गूगल आईडी के लिए जाँच करता है फिर 'घरा' गुण सेट करता है जो मुझे टिप्पणी देखने देता है और यदि आवश्यक हो तो उन्हें मिटा देता है.

# ऑन्टियम

तो यह हिस्सा 2, मैं कैसे टिप्पणी को बचाना है. कुछ टुकड़े अभी भी गायब हैं; थ्रेडिंग (सो आप एक टिप्पणी का जवाब दे सकते हैं), अपनी टिप्पणी की सूची और टिप्पणी रद्द कर सकते हैं. मैं अगले पोस्ट में उन लोगों को कवर करेंगे.