# असंनेट कोर के साथ म्यूक्स

<datetime class="hidden">2024- 0. 01013: 4</datetime>

<!--category-- ASP.NET, HTMX -->
## परिचय

एचएसईएसए.नेटसी का उपयोग करने का एक महान तरीका है अत्यंत जावास्क्रिप्ट अनुप्रयोगों के साथ गतिशील वेब अनुप्रयोगों को बनाने का. HMX आपको आपके पृष्ठ के भाग को पुनः लोड करने की अनुमति देता है बिना पूर्ण पृष्ठ पुनः लोड किए, और आपके अनुप्रयोग को अधिक प्रतिक्रिया और संवाद को महसूस करने देता है.

यह मैं क्या कह रहा था 'धा' वेब डिजाइन जहां आप पृष्ठ को पूरी तरह से उपयोग करते हैं सर्वर- बाजू कोड का उपयोग करते हैं और फिर HMMMX का उपयोग पृष्ठ गतिशील रूप से अद्यतन करने के लिए करता हूँ।

इस लेख में, मैं आपको दिखाएगा कि कैसे HMMMX के साथ शुरू हो रही है एक सीपी.NT अनुप्रयोग.

[विषय

## पूर्वपाराईज़

HMX - Htmx एक जावास्क्रिप्ट पैकेज है जिसमें इसे शामिल करना आपके परियोजना में एक सीडी एन का प्रयोग करना है. ( लेख की शुरूआत में दी तसवीर देखिए ।) [यहाँ](https://htmx.org/docs/#installing) )

```html
<script src="https://unpkg.com/htmx.org@2.0.1" integrity="sha384-QWGpdj554B4ETpJJC9z+ZHJcA/i59TyjxEPXiiUgN2WmTyV5OEZWCD6gQhgkdpB/" crossorigin="anonymous"></script>
```

आप निश्चित रूप से एक प्रति डाउनलोड कर सकते हैं और इसमें शामिल कर सकते हैं 'या लिबमेन या nm' (या उपयोग करें).

## कनेक्शन (n)

मैं यह भी सुझाव देता हूँ कि Htmx टैग सहायक से काम करें [यहाँ](https://github.com/khalidabuhakmeh/Htmx.Net)

ये दोनों अद्‌भुत कार्य कर रहे हैं [खाल अबुदान
](https://mastodon.social/@khalidabuhakmeh@mastodon.social)

```shell
dotnet add package Htmx.TagHelpers
```

और Htmx Nuget पैकेज से [यहाँ](https://www.nuget.org/packages/Htmx/)

```shell
 dotnet add package Htmx
```

टैग सहायक आपको यह करने देता है:

```razor
    <a hx-controller="Blog" hx-action="Show" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-slug="@Model.Slug"
        class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### वैकल्पिक दृष्टिकोण.

**नोट: यह तरीका एक प्रमुख ड्राफ़्ट है; यह पोस्ट लिंक के लिए एक अस्पष्ट नहीं बनाता. यह सीओ और पहुँच के लिए एक समस्या है. इसका मतलब यह भी है कि इन कड़ियों को विफल होगा अगर HMAX कुछ कारण के लिए लोड नहीं होता (Cass नीचे जाते हैं).**

एक आसान तरीका है इस्तेमाल करना ` hx-boost="true"` गुण तथा सामान्य रूप से कोर टैग सहायक. देखें  [यहाँ](https://htmx.org/docs/#hx-boost) hx-babooobooo पर अधिक जानकारी के लिए (हालाँकि कुछ वक्त में डॉटs एक बिट का उपयोग कर रहे हैं).
यह सामान्य डायजेस्ट आउटपुट करेगा लेकिन HMMX द्वारा लिया जाएगा तथा सामग्री को गतिशील रूप से लोड किया जाएगा.

सो जैसा है, वैसा ही हो:

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### आंशिक

एटीएमएक्स आंशिक दृष्टिकोणों के साथ अच्छी तरह से काम करता है. आप अपने पेज पर एक संक्षिप्त दृष्टिकोण लोड करने के लिए HMMX इस्तेमाल कर सकते हैं. यह आपके पृष्ठ के भागों को बिना पूर्ण पृष्ठ को पुनः लोड करने के लिए महान है.

इस एप्पल में हमारे पास खाका में एक कंटेनर है.प्रशएचटीएमएल फ़ाइल है कि हम एक आंशिक दृष्टिकोण को लोड करना चाहते हैं.

```razor
    <div class="container mx-auto" id="contentcontainer">
   @RenderBody()

    </div>
```

सामान्यतः यह सर्वर साइड सामग्री का अनुवाद करता है परंतु आपके बारे में HMMAX टैग सहायक का उपयोग करके हम लक्ष्य देख सकते हैं `hx-target="#contentcontainer"` जो आंशिक रूप से दृश्य को संग्राहक में लोड करेगा.

हमारी परियोजना में हम ब्लॉग दृश्य आंशिक दृष्टिकोण है कि हम बर्तन में लोड करना चाहते हैं.

![img.png](project.png)

फिर ब्लॉग नियंत्रक में हमारे पास है

```csharp
    [Route("{slug}")]
    [OutputCache(Duration = 3600)]
    public IActionResult Show(string slug)
    {
       var post =  blogService.GetPost(slug);
       if(Request.IsHtmx())
       {
              return PartialView("_PostPartial", post);
       }
       return View("Post", post);
    }
```

आप यहाँ देख सकते हैं कि HMAX निवेदन है. हैHTmx () विधि, यह सही होगा यदि निवेदन है HMMX निवेदन है. यदि हम पक्षपाती दृष्टिकोण वापस कर रहे हैं, तो अगर हम पूर्ण दृष्टिकोण वापस नहीं लौटे ।

यह हम निश्‍चित कर सकते हैं कि हम सीधे - सीधे पूछताछ करने का भी समर्थन करते हैं ।

इस मामले में हमारा पूरा दृष्टिकोण इस पक्षपाती को सूचित करता है:

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
ViewBag.Title = "title";
Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

और इसलिए अब हमारे पास एक बहुत ही सरल तरीका है आंशिक दृष्टिकोण को हमारे पृष्ठ में HMMX का उपयोग करने के लिए।