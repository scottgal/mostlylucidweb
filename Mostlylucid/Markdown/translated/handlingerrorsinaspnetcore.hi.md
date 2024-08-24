# अनियंत्रित हैंडल (अनियंत्रित) त्रुटियाँ अप्रयोगियों में. एनईटी कोर

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024- 0. 1417टी0: 00</datetime>

## परिचय

किसी भी वेब अनुप्रयोग में यह गलतियों को हल करने के लिए महत्वपूर्ण है. यह ख़ासकर एक उत्पादन वातावरण में सच है जहाँ आप एक अच्छे उपयोगकर्ता का अनुभव करना चाहते हैं और किसी संवेदनशील जानकारी का परदाफ़ाश नहीं करना चाहते । इस लेख में हम देखेंगे कि कैसे गलतियों को एक अनजानी में संभाल सकते हैं. कोर अनुप्रयोग.

[विषय

## समस्या

जब कोई अनियंत्रित अपवाद उत्पन्न होता है. अनियंत्रित कोरल अनुप्रयोग, डिफ़ॉल्ट बर्ताव 500 की स्थिति कोड के साथ जेनेरिक त्रुटि पृष्ठ लौटाना है. यह कई कारणों के लिए आदर्श नहीं है:

1. यह बदसूरत है और एक अच्छा उपयोगकर्ता अनुभव प्रदान नहीं करता है.
2. यह उपयोक्ता को कोई उपयोगी सूचना नहीं प्रदान करता है.
3. यह अकसर समस्या को डीबग करने के लिए कठिन है क्योंकि त्रुटि संदेश इतना जेनेरिक है.
4. यह बदसूरत है; जेनेरिक ब्राउज़र त्रुटि पृष्ठ कुछ पाठ के साथ सिर्फ एक ग्रे स्क्रीन है.

## हल

एक अतिरिक्त में। वहाँ एक साफ सुविधा निर्माण है जिसमें हमें इन गलतियों को नियंत्रित करने की अनुमति मिलती है।

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

हम इसे अपने में डाल दिया `Program.cs` अतुल्यकालिक स्थिति में आरंभिक फ़ाइल. यह किसी भी स्थिति कोड को पकड़ने के लिए है जो कि 200 और पुनर्निदेशित नहीं है `/error` एक पैरामीटर के रूप में स्थिति कोड के साथ मार्ग.

हमारे त्रुटि नियंत्रण कुछ इस तरह दिखेगा:

```csharp
    [Route("/error/{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        // Retrieve the original request information
        var statusCodeReExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        
        if (statusCodeReExecuteFeature != null)
        {
            // Access the original path and query string that caused the error
            var originalPath = statusCodeReExecuteFeature.OriginalPath;
            var originalQueryString = statusCodeReExecuteFeature.OriginalQueryString;

            
            // Optionally log the original URL or pass it to the view
            ViewData["OriginalUrl"] = $"{originalPath}{originalQueryString}";
        }

        // Handle specific status codes and return corresponding views
        switch (statusCode)
        {
            case 404:
            return View("NotFound");
            case 500:
            return View("ServerError");
            default:
            return View("Error");
        }
    }
```

यह नियंत्रण त्रुटि नियंत्रण करेगा तथा स्थिति कोड पर आधारित मनपसंद दृश्य लौटाता है. हम मूल यूआरएल का लॉग कर सकते हैं जो त्रुटि पैदा करता है और इसे दृश्य में पास करता है.
अगर हम एक केंद्रीय लॉगिंग / aslys सेवा था हम इस सेवा के लिए इस त्रुटि का लॉग कर सकते हैं।

हमारा दृश्य इस तरह है:

```razor
<h1>404 - Page not found</h1>

<p>Sorry that Url doesn't look valid</p>
@section Scripts {
    <script>
            document.addEventListener('DOMContentLoaded', function () {
                if (!window.hasTracked) {
                    umami.track('404', { page:'@ViewData["OriginalUrl"]'});
                    window.hasTracked = true;
                }
            });

    </script>
}
```

बहुत सरल है? हम अनुप्रयोग इंसाइट ऑन द स्क्रिप्चर्स की तरह लॉगिंग सेवा करने में भी त्रुटि का लॉग कर सकते हैं. इस तरह हम उन्हें त्रुटियों का ट्रैक रख सकते हैं और एक समस्या बनने से पहले ठीक कर सकते हैं.
हमारे मामले में हम यह एक घटना के रूप में अपने उममी सेवा के लिए। इस तरह हम इस बात का ध्यान रख सकते हैं कि हमारे पास कितनी 404 त्रुटियाँ हैं और वे कहाँ से आ रहे हैं.

यह आपके चुने गए खाका और डिजाइन के अनुरूप भी रहता है ।

![404 पृष्ठ](new404.png)

## ऑन्टियम

यह एक आसान तरीका है जिसमें त्रुटियाँ हों. NEACKT अनुप्रयोग में त्रुटिओं को नियंत्रित करने का. यह एक अच्छा उपयोगकर्ता अनुभव प्रदान करता है और हमें त्रुटियों का ट्रैक बनाए रखने देता है । यह एक लॉगिंग सेवा में गलतियों को लॉग करने के लिए एक अच्छा विचार है ताकि आप उनका ट्रैक रख सकते हैं और उन्हें एक समस्या बनने से पहले ठीक कर सकते हैं.