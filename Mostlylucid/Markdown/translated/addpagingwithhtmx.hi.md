# हाइममएक्स तथा आउचक के साथ पृष्ठ जोड़ने के लिए. टैग- मददर के साथ नीटी कोर

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024- 0. 0809टी12: 50</datetime>

## परिचय

अब जबकि मेरे पास गृह पृष्ठ पोस्टों का एक गुच्छा हो रहा था......तो मैंने ब्लॉग पोस्टों के लिए एक paning सुविधा जोड़ने का निर्णय किया.

इस ब्लॉग पोस्टों को एक त्वरित तथा कुशल साइट बनाने के लिए पूर्ण कैशिंग जोड़ने के साथ यह जाता है.

देखें [ब्लॉग सेवा स्रोत:](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/Markdown/MarkdownBlogService.cs) कैसे यह लागू किया जाता है के लिए, यह वास्तव में आसान है मैं मेमोरी कीश का उपयोग करना.

[विषय

### टैग मददर

मैंने फैसला किया कि मैं एक टैग मददकर्ता का इस्तेमाल करूँगा ताकि वह इन चीज़ों को सही तरीके से इस्तेमाल कर सके । यह paning तर्क को दोहराने का एक महान तरीका है और इसे फिर से बनाने का.
यह उपयोग करता है [डारेल ओ 'ललललल से paradation टैग सहायक ](https://github.com/darrel-oneil/PaginationTagHelper) यह परियोजना में एक परमाणु पैकेज के रूप में शामिल है.

तब यह जोड़ा जाता है _दृश्य आयात करता है.

```razor
@addTagHelper *,PaginationTagHelper.AspNetCore
```

### टैग मदद करनेवाला

में _ब्लॉगिंग सूची. sclutml आंशिक दृश्य मैं ने निम्न कोड जोड़ दिया कि paning सुविधा देने के लिए.

```razor
<pager link-url="@Model.LinkUrl"
       hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
       page="@Model.Page"
       page-size="@Model.PageSize"
       total-items="@Model.TotalItems" ></pager>
```

यहाँ कुछ उल्लेखनीय बातें:

1. `link-url` यह टैग सहायक को स्विचिंग कड़ियों के लिए सही यूआरएल बनाने देता है. घर नियंत्रण इंडेक्स पद्धति में यह उस कार्य के लिए सेट किया जाता है ।

```csharp
   var posts = blogService.GetPostsForFiles(page, pageSize);
   posts.LinkUrl= Url.Action("Index", "Home");
   if (Request.IsHtmx())
   {
      return PartialView("_BlogSummaryList", posts);
   }
```

और ब्लॉग नियंत्रक में

```csharp
    public IActionResult Index(int page = 1, int pageSize = 5)
    {
        var posts = blogService.GetPostsForFiles(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

यह है कि नीचे सेट कर दिया जाता है. यह निश्‍चित करता है कि भेंटन सहायक या तो ऊपरी स्तर विधि के लिए काम कर सकता है ।

### एचएसएमएक्स गुण

`hx-boost`, `hx-push-url`, `hx-target`, `hx-swap` ये सभी HMAX गुण हैं जो HMMX के साथ काम करने की अनुमति देते हैं.

```razor
     hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
```

यहाँ हम उपयोग `hx-boost="true"` यह प्रोग्रेसन टैग सहायक को स्वीकार करने की अनुमति देता है जिसमें यह सामान्य यूआरएल पीढ़ी है तथा मौजूदा यूआरएल के प्रयोग से किसी भी संशोधन की आवश्यकता नहीं है.

`hx-push-url="true"` यह सुनिश्चित करने के लिए कि यूआरएल ब्राउज़र के इतिहास में बदला गया है (जो कि पृष्ठों पर सीधे लिंक की अनुमति देता है).

`hx-target="#content"` यह वह लक्ष्य है जो कि नई सामग्री से बदल दिया जाएगा.

`hx-swap="show:none"` यहाँ पर छवि के संतृप्ति समायोजन को सेट करें. इस मामले में यह सामान्य 'पाम्प' प्रभाव को रोक देता है जिसे HMMAX सामग्री पर उपयोग करता है.

#### सीएसएस

मैंने मुख्य शैली भी जोड़े अपने /scss डिरेक्ट्री में मुझे playssssss का उपयोग करने की अनुमति दे रही है prols लिंक के लिए scents.

```css
.pagination {
    @apply py-2 flex list-none p-0 m-0 justify-center items-center;
}

.page-item {
    @apply mx-1 text-black  dark:text-white rounded;
}

.page-item a {
    @apply block rounded-md transition duration-300 ease-in-out;
}

.page-item a:hover {
    @apply bg-blue-dark text-white;
}

.page-item.disabled a {
    @apply text-blue-dark pointer-events-none cursor-not-allowed;
}

```

### नियंत्रक

`page`, `page-size`, `total-items` सजावटी टैग सहायकों का उपयोग panceing लिंक तैयार करने के लिए किया जाता है.
इन्हें नियंत्रण से आंशिक दृष्टिकोण में भेज दिया गया है ।

```csharp
 public IActionResult Index(int page = 1, int pageSize = 5)
```

### ब्लॉग सेवा

यहाँ पृष्ठ तथा पृष्ठ आकार यूआरएल से पास किए गए हैं तथा ब्लॉग सेवा से कुल वस्तुओं की गणना की जाती है.

```csharp
    public PostListViewModel GetPostsForFiles(int page=1, int pageSize=10)
    {
        var model = new PostListViewModel();
        var posts = GetPageCache().Values.Select(GetListModel).ToList();
        model.Posts = posts.OrderByDescending(x => x.PublishedDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        model.TotalItems = posts.Count();
        model.PageSize = pageSize;
        model.Page = page;
        return model;
    }
```

यहाँ हम कैश से पोस्ट प्राप्त करते हैं, उन्हें तिथि से आदेश देते हैं और फिर पृष्ठ के लिए पोस्ट की सही संख्या लेते हैं ।

### कंटेनमेंट

यह साइट के लिए एक सरल अतिरिक्त था लेकिन यह इसे और अधिक प्रयोग करता है । इस साइट में और ज़्यादा जावास्क्रिप्ट जोड़ने के दौरान, HMX संयोजन इस साइट को अधिक प्रतिक्रियाशील महसूस करता है.