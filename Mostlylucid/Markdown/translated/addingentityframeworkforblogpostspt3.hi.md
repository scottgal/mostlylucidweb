# ब्लॉग पोस्ट के लिए एंटिटी फ्रेमवर्क जोड़े ( पार्ट 3)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024- 0. 3131: 00</datetime>

आप ब्लॉग पोस्ट के लिए सभी स्रोत कोड पा सकते हैं [GiHh](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**भाग 1 में से% 2 को एंटिटी फ्रेमवर्क को एक परियोजना में जोड़ने पर.**

पार्ट 1 मिल सकता है [यहाँ](/blog/addingentityframeworkforblogpostspt1).

पार्ट 2 पाया जा सकता है [यहाँ](/blog/addingentityframeworkforblogpostspt2).

## परिचय

पिछले भाग में हमने डाटाबेस को और अपने ब्लॉग पोस्ट के संदर्भ को सेट किया, और सेवाओं को इस डाटाबेस के साथ बातचीत करने के लिए जोड़ा. इस पोस्ट में, हम देखेंगे कि ये सेवाएँ मौजूदा नियंत्रण और विचारों के साथ कैसे कार्य करती हैं ।

[विषय

## नियंत्रक

ब्लॉग्स के लिए नियंत्रण वास्तव में बहुत सरल है, 'Fontalals' के विरोधी पैटर्न से बचने के लिए पंक्ति में (एक तरह का नमूना जो हम गुप्त रूप से शुरू में किया गया है. TEVC दिनों में.

### मोटा नियंत्रक पैटर्न AUVC में.

मैं MVC एक अच्छा अभ्यास सेट आपके नियंत्रण तरीकों में जितना संभव हो उतना कम करने के लिए है. यह इसलिए है क्योंकि नियंत्रण निवेदन को नियंत्रित करने और प्रतिक्रिया वापस लाने के लिए ज़िम्मेदार है । यह अनुप्रयोग के व्यापार तर्क के लिए जिम्मेदार नहीं होना चाहिए. यह मॉडल की जिम्मेदारी है.

'प्रयोग' कंट्रास्ट पैटर्न है जहां नियंत्रण बहुत अधिक करता है. यह कई समस्याओं की ओर ले जा सकता है, जिनमें से एक है:

1. कितने क्रियाओं में कोड का सिंपलिंग: (t)
   एक क्रिया काम की एक ही इकाई होनी चाहिए, केवल मॉडल भरना और दृष्टिकोण वापस लाना । यदि आप अपने आप को अनेक कार्यों में कोड दोहराते हैं, तो यह एक संकेत है कि आपको इस कोड को अलग तरीके से फिर से दोहराया जाना चाहिए.
2. कोड जो जांच करना कठिन है:
   ' लाना नियंत्रण' होने के द्वारा आप कोड की जाँच करने में मुश्किल हो सकता है. कोड के माध्यम से सभी संभावित पथों का पालन करने की कोशिश करनी चाहिए, और यह कठिन हो सकता है यदि कोड अच्छी तरह से उपयोग नहीं किया गया है और एक एकल ज़िम्मेदारी पर केंद्रित है.
3. कोड जो बनाए रखना कठिन है:
   जब अनुप्रयोग निर्माण करते हैं तो स्थिर रहना एक कुंजी है. 'पीकिंग सिंक' क्रिया पद्धति में आसानी से आप और अन्य विकासकर्ता को अनुप्रयोग के अन्य भाग को तोड़ने के लिए कोड का उपयोग कर सकते हैं.
4. कोड जो समझना कठिन है:
   यह विकासकर्ता के लिए कुंजी चिंता है. यदि आप एक बड़े कोड की सच्चाई के साथ परियोजना पर काम कर रहे हैं, तो यह समझना मुश्‍किल हो सकता है कि क्या हो रहा है अगर यह बहुत अधिक कर रहा है ।

### ब्लॉग नियंत्रक

ब्लॉग नियंत्रक अति सरल है. इस में 4 मुख्य क्रियाएँ हैं (और एक नया ब्लॉग लिंक). ये हैं:

```csharp
Task<IActionResult> Index(int page = 1, int pageSize = 5)

Task<IActionResult> Show(string slug, string language = BaseService.EnglishLanguage)

Task<IActionResult> Category(string category, int page = 1, int pageSize = 5)

Task<IActionResult> Language(string slug, string language)

IActionResult Compat(string slug, string language)
```

इन क्रियाओं को चालू करें `IBlogService` डेटा वे जरूरत है पाने के लिए. वह `IBlogService` विस्तृत है [पिछला पोस्ट](/blog/addingentityframeworkforblogpostspt2).

इन कामों को बारी - बारी से आगे बढ़ाया जाता है

- सूची: यह ब्लॉग पोस्टों की सूची है (डिफ़ॉल्ट से अंग्रेज़ी भाषा में), हम इसे अनेक भाषाओं के लिए जारी कर सकते हैं. आप यह लेता देखेंगे `page` और `pageSize` पैरामीटर्स की तरह. यह paggion के लिए है. परिणाम का.
- दिखाएँ: यह ब्लॉग पोस्ट है. यह लेता है `slug` पोस्ट तथा पोस्ट का `language` पैरामीटर्स की तरह. टीआईए आप वर्तमान में इस ब्लॉग पोस्ट को पढ़ने के लिए प्रयोग कर रहे हैं.
- वर्ग: यह ब्लॉग पोस्टों की सूची है दिए गए वर्ग के लिए. यह लेता है `category`, `page` और `pageSize` पैरामीटर्स की तरह.
- भाषाः यह दी गई भाषा के लिए ब्लॉग पोस्ट दिखाता है. यह लेता है `slug` और `language` पैरामीटर्स की तरह.
- रंग: यह पुराने ब्लॉग लिंक के लिए एक सहवास्य क्रिया है. यह लेता है `slug` और `language` पैरामीटर्स की तरह.

### कैशिंग

जैसा एक व्यक्‍ति में उल्लेख किया गया है [पहले पोस्ट](https://www.mostlylucid.net/blog/aspnetcachingwithhtmx) हम लागू करें `OutputCache` और `ResponseCahce` ब्लॉग पोस्ट के परिणाम को कैश करने के लिए. यह उपयोक्ता अनुभव को बढ़ाता है तथा सर्वर पर लोड को कम करता है.

ये उचित क्रिया टालनेवाले के प्रयोग से लागू होते हैं जो कि क्रिया के लिए इस्तेमाल किए गए पैरामीटर्स को निर्धारित करते हैं (जैसे साथ ही साथ) `hx-request` HMAX निवेदन के लिए. परीक्षा के लिए `Index` हम इन्हें उल्लेखित करें:

```csharp
    [ResponseCache(Duration = 300, VaryByHeader  = "hx-request", VaryByQueryKeys = new[] {nameof(page), nameof(pageSize)}, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] {"hx-request"} ,VaryByQueryKeys = new[] { nameof(page), nameof(pageSize)})]
```

## दृश्य

ब्लॉग के लिए विचार सापेक्ष रूप से सरल हैं। ये ज़्यादातर ब्लॉग पोस्टों की सूची हैं, जिनमें प्रत्येक पोस्ट के लिए कुछ विवरण हैं. दृश्य इसमें हैं `Views/Blog` फ़ोल्डर. मुख्य दृश्य हैं:

### `_PostPartial.cshtml`

एक ब्लॉग पोस्ट के लिए यह आंशिक दृश्य है. हमारे भीतर यह प्रयोग किया गया है `Post.cshtml` दृश्य.

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
    Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

### `_BlogSummaryList.cshtml`

ब्लॉग पोस्टों की सूची के लिए यह आंशिक दृश्य है. हमारे भीतर यह प्रयोग किया गया है `Index.cshtml` के साथ साथ साथ साथ साथ ही साथ साथ घरपृष्ठ में देखें.

```razor
@model Mostlylucid.Models.Blog.PostListViewModel
<div class="pt-2" id="content">

    @if (Model.TotalItems > Model.PageSize)
    {
        <pager
            x-ref="pager"
            link-url="@Model.LinkUrl"
               hx-boost="true"
               hx-push-url="true"
               hx-target="#content"
               hx-swap="show:none"
               page="@Model.Page"
               page-size="@Model.PageSize"
               total-items="@Model.TotalItems"
            class="w-full"></pager>
    }
    @if(ViewBag.Categories != null)
{
    <div class="pb-3">
        <h4 class="font-body text-lg text-primary dark:text-white">Categories</h4>
        <div class="flex flex-wrap gap-2 pt-2">
            @foreach (var category in ViewBag.Categories)
            {
                <a hx-controller="Blog" hx-action="Category" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>
                    <span class="inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white">@category</span>
                </a>
            }
        </div>
    </div>
}
@foreach (var post in Model.Posts)
{
    <partial name="_ListPost" model="post"/>
}
</div>
```

यह उपयोग करता है `_ListPost` व्यक्तिगत ब्लॉग पोस्ट को साथ ही प्रदर्शित करने के लिए आंशिक दृश्य [एकसार टैग सहायक](/blog/addpagingwithhtmx) जो हमें ब्लॉग पोस्ट पर भेजने की अनुमति देता है.

### `_ListPost.cshtml`

वह _सूची में निजी ब्लॉग पोस्टों को प्रदर्शित करने के लिए पोस्टों का उपयोग किया जाता है. जिसमें (उन्होंने मुसलमानों के लिए) ईंधन झोंक रखा था `_BlogSummaryList` दृश्य.

```razor
@model Mostlylucid.Models.Blog.PostListModel

<div class="border-b border-grey-lighter pb-8 mb-8">
 
    <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold transition-colors hover:text-green text-blue-dark dark:text-white  dark:hover:text-secondary">@Model.Title</a>
    <div class="flex space-x-2 items-center py-4">
    @foreach (var category in Model.Categories)
    {
    <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
    }

    @{ var languageModel = (Model.Slug, Model.Languages, Model.Language); }
        <partial name="_LanguageList" model="languageModel"/>
    </div>
    <div class="block font-body text-black dark:text-white">@Model.Summary</div>
    <div class="flex items-center pt-4">
        <p class="pr-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.PublishedDate.ToString("f")
        </p>
        <span class="font-body text-grey dark:text-white">//</span>
        <p class="pl-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.ReadingTime
        </p>
    </div>
</div>
```

जैसे कि आप यहाँ देखेंगे हम व्यक्तिगत ब्लॉग पोस्ट के लिए एक लिंक है, पोस्ट के लिए वर्ग, पोस्ट के सारांश, प्रकाशित तिथि और पढ़ने के समय में उपलब्ध है।

हमारे पास वर्गों और भाषाओं के लिए HMMMX लिंक टैग भी है हमें स्थानीय पोस्टों और पोस्टों को किसी दिए वर्ग के लिए प्रदर्शित करने की अनुमति देने के लिए अनुमति देने के लिए।

हमारे पास HMSX का उपयोग करने के दो तरीके हैं, एक जो पूरा URL देता है और एक जो सिर्फ 'एचटीएमएल' है. यूआरएल नहीं है. यह इसलिए है कि हम वर्गों और भाषाओं के लिए पूरा यूआरएल उपयोग करना चाहते हैं, लेकिन हमें पूरे यूआरएल की आवश्यकता नहीं है व्यक्तिगत ब्लॉग पोस्ट के लिए.

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
```

यह ब्लॉग पोस्ट तथा उपयोग के लिए पूरा यूआरएल भरता है `hx-boost` HMAX का उपयोग करने के लिए 'बोद' निवेदन (यह सेट करता है `hx-request` शीर्षिका को `true`).

```razor
  <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
```

आम तौर पर यह तरीका HMMMX टैगों को ब्लॉग पोस्ट पाने के लिए प्रयोग करता है. यह उपयोग करता है `hx-controller`, `hx-action`, `hx-push-url`, `hx-get`, `hx-target` और `hx-route-category` ब्लॉग पोस्टों के लिए श्रेणियाँ प्राप्त करने के लिए टैग `hx-push-url` को सेट कर दिया है `true` ब्राउज़र इतिहास में यूआरएल को पुश करने के लिए.

यह हमारे भीतर भी प्रयोग किया जाता है `Index` एटीएम निवेदन के लिए क्रिया विधि.

```csharp
  public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
    {
        var posts =await  blogService.GetPagedPosts(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

जहां यह हमें या तो पूर्ण दृष्टिकोण वापस करने के लिए सक्षम करता है या HMAX निवेदन के लिए आंशिक दृष्टिकोण, अनुभव की तरह एक 'SPPAP' देने के लिए.

## पहला पन्ना

में `HomeController` हम इन ब्लॉग सेवाओं का भी उल्लेख इस घर पृष्ठ के लिए नवीनतम ब्लॉग पोस्ट प्राप्त करने के लिए करते हैं। यह किया जाता है `Index` क्रिया विधि.

```csharp
   public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPagedPosts(page, pageSize);
            posts.LinkUrl= Url.Action("Index", "Home");
            if (Request.IsHtmx())
            {
                return PartialView("_BlogSummaryList", posts);
            }
            var indexPageViewModel = new IndexPageViewModel
            {
                Posts = posts, Authenticated = authenticateResult.LoggedIn, Name = authenticateResult.Name,
                AvatarUrl = authenticateResult.AvatarUrl
            };
            
            return View(indexPageViewModel);
    }
```

जैसे आप यहाँ देखेंगे हम उपयोग करेंगे `IBlogService` घर पृष्ठ के लिए नवीनतम ब्लॉग पोस्ट प्राप्त करने के लिए. हम भी इस्तेमाल करते हैं `GetUserInfo` घर पृष्ठ के लिए उपयोक्ता जानकारी प्राप्त करने का विधि.

इस में एक बार फिर HMAX निवेदन है कि हम अपने ब्लॉग को पृष्ठ पर पोस्ट भेजने की अनुमति दें।

## ऑन्टियम

हमारे अगले भाग में हम कैसे इस्तेमाल के बारे में गहराई से विस्तार में जाना होगा `IMarkdownBlogService` इस डाटाबेस को ब्लॉग पोस्टों से भरने के लिए इस चिह्नी फ़ाइलों में से चुनें. यह अनुप्रयोग का मुख्य भाग है जहाँ यह हमें ब्लॉग पोस्टों को भरने के लिए चिह्नों का उपयोग करने की अनुमति देता है.