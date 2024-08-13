# मार्क डाउन के साथ एक सुपर सरल टिप्पणी तंत्र

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024- 0. 3206T18: 50</datetime>

विचार कीजिए: सा. यु.

मैं अपने ब्लॉग के लिए एक सरल टिप्पणी तंत्र देख रहा हूँ जो मार्क नीचे प्रयोग करता है। मैं एक है कि मुझे पसंद नहीं मिला, तो मैंने अपना खुद लिखने का फैसला किया. यह एक सरल टिप्पणी तंत्र है जो मार्क इन फ़ॉर्मेटिंग के लिए प्रयोग करता है. इस तंत्र का दूसरा भाग ईमेल सूचना जोड़ेगा जो मुझे इस टिप्पणी को लिंक के साथ ई- मेल भेज देगा, मुझे 'सही' को बदलने की अनुमति देगा इससे पहले कि यह साइट पर प्रदर्शित हो.

उत्पादन प्रणाली के लिए फिर से एक डेटाबेस का उपयोग करता है, लेकिन इस उदाहरण के लिए मैं सिर्फ निशान नीचे का उपयोग करने के लिए जा रहा हूँ।

## टिप्पणी तंत्र

टिप्पणी तंत्र निश्‍चित ही सरल है । मैं सिर्फ उपयोक्ता नाम, ई- मेल तथा टिप्पणी के लिए एक चिह्न नीचे फ़ाइल सहेजा जा रहा है. उस समय टिप्पणी पृष्ठ पर प्रदर्शित की गयी हैं, जिस क्रम में उन्हें प्राप्त किया गया ।

टिप्पणी को प्रविष्ट करने के लिए मैं सादाएमई का उपयोग करता हूँ, एक जावा- स्क्रिप्ट जो मार्क डाउन संपादक है.
यह मेरे में शामिल है _अभिन्यास. cltml जैसा कि इसके बाद:

```html
<!-- Include the SimpleMDE CSS, here I use a dark theme -->
  <link rel="stylesheet" href="https://cdn.rawgit.com/xcatliu/simplemde-theme-dark/master/dist/simplemde-theme-dark.min.css">

<!--Later in the page include the JS for SimpleMDE -->
<script src="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.js"></script>

```

मैं दोनों पृष्ठ लोड और HMMAX लोड पर सादा संपादक प्रारंभ करता हूं:

```javascript
    var simplemde;
    document.addEventListener('DOMContentLoaded', function () {
    
        if (document.getElementById("comment") != null)
        {
        
       simplemde = new SimpleMDE({ element: document.getElementById("comment") });
       }
        
    });
    document.body.addEventListener('htmx:afterSwap', function(evt) {
        if (document.getElementById("comment") != null)
        {
        simplemde = new SimpleMDE({ element: document.getElementById("comment") });
        
        }
    });
```

यहाँ मैं निर्धारित करता हूँ कि मेरी टिप्पणी का पाठ है 'सर्जन' कहा जाता है और केवल एक बार यह पता चला है. यहाँ मैं फ़ॉर्म को 'यस्तित' (जो मैं दृश्य मोड में जाता हूँ) में लपेटता हूँ. इसका अर्थ है कि मैं सुनिश्चित कर सकता हूँ कि सिर्फ उन्हीं में जिन्होंने गूगल के साथ लॉगइन किए हैं, जवाब जोड़ सकते हैं.

```razor
@if (Model.Authenticated)
    {
        
  
        <div class=" max-w-none border-b border-grey-lighter py-8 dark:prose-dark sm:py-12">
            <p class="font-body text-lg font-medium text-primary dark:text-white">Welcome @Model.Name please comment below.</p>
            <textarea id="comment"></textarea>
       <button class="btn btn-primary" hx-action="Comment" hx-controller="Blog" hx-post hx-vals="js:{comment: simplemde.value()}" hx-route-slug="@Model.Slug" hx-swap="outerHTML" hx-target="#comment" onclick="prepareForSubmission()">Comment</button>
        </div>
    }
    else
    {
       ...
    }
```

आप भी नोट कर सकते हैं कि मैं टिप्पणी प्रेषण के लिए यहाँ HMMMX का उपयोग करते हैं. जहाँ मैं hx-REGEes गुण तथा एक जेएस कॉल का उपयोग टिप्पणी के लिए मूल्य प्राप्त करने के लिए करता हूँ. यह तब ब्लॉग नियंत्रक पर हस्ताक्षर किया गया है 'Comment' क्रिया के साथ. तब यह नई टिप्पणी के साथ बदल गया है.

```csharp
    [HttpPost]
    [Route("comment")]
    [Authorize]
    public async Task<IActionResult> Comment(string slug, string comment)
    {
        var principal = HttpContext.User;
        principal.Claims.ToList().ForEach(c => logger.LogInformation($"{c.Type} : {c.Value}"));
        var nameIdentifier = principal.FindFirst("sub");
        var userInformation = GetUserInfo();
       await commentService.AddComment(slug, userInformation, comment, nameIdentifier.Value);
        return RedirectToAction(nameof(Show), new { slug });
    }

```