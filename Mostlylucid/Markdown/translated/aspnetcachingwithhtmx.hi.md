# प्रमाणीकरण विधि

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024- 0. 121200: 50</datetime>

## परिचय

कैशिंग एक अहम तरीका है जिससे उपयोक्ता का अनुभव बेहतर हो सकता है सामग्री को तेजी से लोड करके तथा आपके सर्वर पर लोड करने के लिए. इस लेख में मैं आपको दिखाता हूँ कि Ablicks की निर्माण विशेषता उपयोग करने के लिए कैसे। HMMAX के साथ MAT को लोड करने के लिए ग्राहक के पक्ष में।

[विषय

## सेटअप

एनईएसई में.नेटटी कोर में दो प्रकार की कलिंगियाँ दी जाती हैं

- कैश - यह डाटा है जो ग्राहक पर या एकीकरण सर्वर (या दोनों) पर कैश किया जाता है और निवेदन के लिए पूरी प्रतिक्रिया को कम करने के लिए प्रयोग किया जाता है.
- आउटपुट कैश - यह डाटा है जो सर्वर पर कैश्ड है और नियंत्रण क्रिया के आउटपुट को कैश करने में प्रयोग में लिया जाता है.

इन गन में सेट करने के लिए आप अपने में कुछ सेवा के एक जोड़े जोड़ने की जरूरत है `Program.cs` फ़ाइल

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

जबकि आपके में प्रतिक्रिया कैश करना संभव है `Program.cs` यह अक्सर एक छोटा सा निदान होता है (सामान्य रूप से जब HMMMX निवेदन का प्रयोग किया जाता है मैं मिल गया है). आप अपने नियंत्रण कार्यों में प्रतिक्रिया कैशिंग सेट कर सकते हैं इस्तेमाल करके `ResponseCache` गुण.

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

यह 300 सेकंड के लिए अनुक्रिया को कैश करेगा और कैश को अलग करेगा `hx-request` शीर्षिका और `page` और `pageSize` क्वैरी पैरामीटर्स. हम भी स्थापित कर रहे हैं `Location` को `Any` इसका अर्थ है कि प्रतिक्रिया क्लाएंट पर, एकीकरण प्रॉक्सी सर्वर पर, या दोनों पर कैश किया जा सकता है.

यहाँ `hx-request` शीर्षिका है कि HMAX प्रत्येक निवेदन के साथ भेजा जाता है. यह महत्वपूर्ण है क्योंकि यह आपको एक HMMMX निवेदन या एक सामान्य निवेदन पर आधारित प्रतिक्रिया को अलग तरह से कैश करने की अनुमति देता है.

यह हमारा वर्तमान है `Index` क्रिया विधि. यो यूकेन देखते हैं कि हम पृष्ठ और पृष्ठ आकार पैरामीटर यहाँ स्वीकार करते हैं और हम इन को अलग-अलग क्वेरी कुंजी के रूप में जोड़ा `ResponseCache` गुण. मतलब है कि जवाब 'इन कुंजियों द्वारा 'पुष्टि' कर रहे हैं और इन पर आधारित विविध सामग्री जमा कर रहे हैं.

काम करते वक्‍त हमारे पास भी है `if(Request.IsHtmx())` यह इस पर आधारित है [HMAX.Net पैकेज](https://github.com/khalidabuhakmeh/Htmx.Net)  और अनिवार्यतः एक ही के लिए जाँच `hx-request` शीर्ष है कि हम कैश अलग करने के लिए उपयोग कर रहे हैं। यहाँ हम एक आंशिक दृष्टिकोण वापस आते हैं अगर निवेदन HMAX से है।

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

सर्वर कैशिंग अनुक्रिया कैश के समतुल्य है. यह नियंत्रण क्रिया का आउटपुट कैश करता है. वेब सर्वर के सारांश में वेब सर्वर निवेदन के परिणाम को समाहित करता है और इसके बाद के निवेदन के लिए काम करता है.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

यहाँ हम 3600 सेकंड के लिए नियंत्रण क्रिया के आउटपुट को कवर कर रहे हैं और कैश को अलग कर रहे हैं `hx-request` शीर्षिका और `page` और `pageSize` क्वैरी पैरामीटर्स.
के रूप में हम एक महत्वपूर्ण समय के लिए डेटा सर्वर की ओर जमा कर रहे हैं (सिर्फ पोस्ट ही एक डॉकrrerroging के साथ अद्यतन किया जाता है) यह अनुक्रिया कैश से अधिक लंबे समय के लिए सेट किया जाता है, यह वास्तव में हमारे मामले में अनंत हो सकता है लेकिन 3600 सेकंड एक अच्छा समझौता है.

अनुक्रिया कैश के साथ के रूप में हम इस्तेमाल कर रहे हैं `hx-request` कैश को अलग करने के लिए शीर्षक यह निवेदन है कि HMAX से क्या निवेदन किया जाना है या नहीं.

## स्थिर फ़ाइलें

एनईएसई. नेटटी कोर ने भी Cliglig फ़ाइलों के लिए समर्थन बनाया है. यह सेटिंग द्वारा किया जाता है `Cache-Control` जवाब में शीर्षिका. आप इसे अपने में स्थापित कर सकते हैं `Program.cs` फ़ाइल.
टीप लें कि अनुक्रम यहाँ महत्वपूर्ण है, यदि आपका स्थिर फ़ाइल समर्थन आपको चाल चलना चाहिए `UseAuthorization` के पहले मध्य भाग में `UseStaticFiles` बीच में। TERSRERSRRRE बीच के बीच में भी होना चाहिए यदि आप इस सुविधा पर भरोसा करते हैं.

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

अपने अनुप्रयोग के प्रदर्शन को बेहतर बनाने के लिए कैशिंग एक शक्तिशाली औज़ार है. वीईसी का उपयोग करके आप क्लाएंट या सर्वर की तरफ आसानी से कैश कर सकते हैं. HMMX का उपयोग करके आप क्लाएंट के पक्ष में सामग्री लौटा सकते हैं और उपयोगकर्ता अनुभव को सुधारने के लिए आंशिक दृष्टिकोण का समर्थन कर सकते हैं.