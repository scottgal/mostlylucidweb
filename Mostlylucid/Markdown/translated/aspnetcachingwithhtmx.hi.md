# प्रमाणीकरण विधि

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024- 0. 121200: 50</datetime>

## परिचय

कैशिंग एक महत्वपूर्ण तकनीक है उपयोगकर्ता के अनुभव को तेजी से लोड करने और आपके सर्वर पर लोड करने के लिए. इस लेख में मैं आपको दिखाता हूँ कि किस तरह ANAC के निर्माण प्रबंधकों का उपयोग करने के लिए. DELMMMST को क्लाएंट के पक्ष पर सामग्री को कैश करने के लिए.

[विषय

## सेटअप

एनईएसई में.नेटटी कोर में दो प्रकार की कलिंगियाँ दी जाती हैं

- कैश - यह डाटा है जो ग्राहक पर या एकीकरण सर्वर (या दोनों) पर कैश किया जाता है और निवेदन के लिए पूरी प्रतिक्रिया को कम करने के लिए प्रयोग किया जाता है.
- आउटपुट कैश - यह डाटा है जो सर्वर पर कैश्ड है और नियंत्रण क्रिया के आउटपुट को कैश करने में प्रयोग में लिया जाता है.

इन गन में सेट करने के लिए आप अपने में कुछ सेवा के एक जोड़े जोड़ने की जरूरत है`Program.cs`फ़ाइल

### अनुक्रिया कैशिंग

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### आउटपुट कैशिंग

```csharp
services.AddOutputCache();
app.UseOutputCache();
```

## अनुक्रिया कैशिंग

जबकि आपके में प्रतिक्रिया कैश करना संभव है`Program.cs`यह अक्सर एक बिट के रूप में HMMMX निवेदन का प्रयोग करते समय होता है. आप अनुक्रिया को अपने नियंत्रण में कर सकते हैं सेट कर सकते हैं के उपयोग से`ResponseCache`गुण.

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

यह 300 सेकंड के लिए अनुक्रिया को कैश करेगा और कैश को अलग करेगा`hx-request`शीर्षिका और`page`और`pageSize`प्रश्न पैरामीटर्स. हम भी स्थापित कर रहे हैं`Location`को`Any`इसका अर्थ है कि प्रतिक्रिया क्लाएंट पर, एकीकरण प्रॉक्सी सर्वर पर, या दोनों पर कैश किया जा सकता है.

यहाँ`hx-request`शीर्षिका है कि HMAX प्रत्येक निवेदन के साथ भेजा जाता है. यह महत्वपूर्ण है क्योंकि यह आपको एक अलग प्रकार से प्रतिक्रिया को कैश करने की अनुमति देता है कि क्या यह एक HMMX निवेदन या एक सामान्य निवेदन है.

यह हमारा वर्तमान है`Index`कार्रवाई विधि. uston देख सकते हैं कि हम एक पृष्ठ और पृष्ठ आकार पैरामीटर यहाँ स्वीकार करते हैं और हम इन्हें विभिन्न प्रश्न कुंजियों के रूप में जोड़ा`ResponseCache`गुण. मतलब है कि जवाब 'इन कुंजियों द्वारा' साझा कर रहे हैं और इन पर आधारित विविध सामग्री रख रहे हैं.

काम करते वक्‍त हमारे पास भी है`if(Request.IsHtmx())`यह इस पर आधारित है[HMAX.Net पैकेज](https://github.com/khalidabuhakmeh/Htmx.Net)और अनिवार्यतः एक ही के लिए जाँच`hx-request`शीर्ष है कि हम कैश अलग करने के लिए उपयोग कर रहे हैं। यहाँ हम एक आंशिक दृष्टिकोण वापस यदि निवेदन HMMX से है।

```csharp
    public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPosts(page, pageSize);
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

## आउटपुट कैशिंग

आउटपुट कैश अनुक्रिया के रूप में सर्वर कैश है. यह नियंत्रक क्रिया के आउटपुट को कैश करता है. संक्षिप्त में वेब सर्वर निवेदन का परिणाम निकालता है और इसके पश्चात निवेदन के लिए सेवा करता है.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

यहाँ हम 3600 सेकंड के लिए नियंत्रण क्रिया के आउटपुट को कवर कर रहे हैं और कैश को अलग कर रहे हैं`hx-request`शीर्षिका और`page`और`pageSize`क्वैरी पैरामीटर्स.
के रूप में हम एक महत्वपूर्ण समय के लिए डेटा सर्वर की ओर जमा कर रहे हैं (सिर्फ पोस्ट ही एक डॉकrrerroging के साथ अद्यतन किया जाता है) यह अनुक्रिया कैश से अधिक लंबे समय के लिए सेट किया जाता है, यह वास्तव में हमारे मामले में अनंत हो सकता है लेकिन 3600 सेकंड एक अच्छा समझौता है.

अनुक्रिया कैश के साथ के रूप में हम इस्तेमाल कर रहे हैं`hx-request`कैश को अलग करने के लिए शीर्षक यह निवेदन है कि HMAX से क्या निवेदन किया जाना है या नहीं.

## स्थिर फ़ाइलें

एनईएसई. नेटटी कोर समर्थन में भी बनाया गया है जो कि स्थिर फ़ाइलों के लिए बनाया गया है. यह सेटिंग स्थापित करने के द्वारा किया जाता है.`Cache-Control`जवाब में शीर्षिका. आप इसे अपने में सेट कर सकते हैं`Program.cs`फ़ाइल.
टीप लें कि अनुक्रम यहाँ महत्वपूर्ण है, यदि आपका स्थिर फ़ाइल समर्थन आपको चाल चलना चाहिए`UseAuthorization`के पहले मध्य भाग में`UseStaticFiles`बीच में. Tewase का उपयोग करें Spacution मध्य फ़ाइलें भी से पहले होना चाहिए यदि आप इस विशेषता पर भरोसा करते हैं.

```csharp
app.UseHttpsRedirection();
var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
            "Cache-Control", $"public, max-age={cacheMaxAgeOneWeek}");
    }
});
app.UseRouting();
app.UseCors("AllowMostlylucid");
app.UseAuthentication();
app.UseAuthorization();
```

## कंटेनमेंट

कैशिंग आपके अनुप्रयोग के प्रदर्शन को बेहतर बनाने के लिए एक शक्तिशाली औजार है. एनईओएई के उपयोग से आप क्लाएंट या सर्वर पर सामग्री आसानी से कैश कर सकते हैं. HMMX का उपयोग करके आप ग्राहक के पक्ष में सामग्री को कैश कर सकते हैं और सेवा कर सकते हैं उपयोगकर्ता अनुभव को बेहतर बनाने के लिए सेवा कर सकते हैं.