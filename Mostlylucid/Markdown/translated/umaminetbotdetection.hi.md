# उममी.नेट तथा बोट पता लगाएँ

# परिचय

तो मेरे पास है [आर. ए.](/blog/category/Umami) अतीत में एक स्वयं के वातावरण के लिए उममी का उपयोग करने पर और यहाँ तक कि प्रकाशित [उममी. नॉर्बोच](https://www.nuget.org/packages/Umami.Net/)___ लेकिन मैं अपने RSS फ़ीड के उपयोक्ताों को ट्रैक करना चाहता था जहाँ मैं चाहता था. इस पोस्ट में जाता है क्यों और कैसे मैंने इसे हल किया.

[TOC]

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-1214:50</datetime>

# समस्या

समस्या यह है कि RSS फीड रीडर पास करने की कोशिश कर रहा है *उपयोगी* जब फीड अनुरोध किया जाता है उपयोक्ता एजेंट. यह अनुमति देता है **आग्रह** उपयोक्ताओं की संख्या और जो उपयोक्ता फीड को भस्म कर रहे हैं, उनकी प्रकार प्रदान करें. लेकिन इसका मतलब यह भी है कि उममी इन अनुरोधों को पहचान लेगी *बॉट* निवेदन. यह मेरे प्रयोग के लिए एक मसला है क्योंकि इसका परिणाम नज़रअंदाज़ किया जा रहा है और नहीं की गई जाँच की जा रही है.

फीडिन उपयोक्ता एजेंट इस तरह दिखता है:

```plaintext
Feedbin feed-id:1234 - 21 subscribers
```

तो बहुत उपयोगी है, यह आपके फ़ीड आईडी क्या है के बारे में कुछ उपयोगी विवरण देता है, उपयोक्ताओं की संख्या और उपयोक्ता एजेंट की संख्या. हालांकि यह भी एक समस्या है के रूप में यह भी मतलब है कि उममी अनुरोध को नज़रअंदाज़ करेगा, वास्तव में यह एक 200 स्थिति B लेकिन सामग्री रखता है `{"beep": "boop"}` इसका अर्थ है कि इसे एक बॉटव निवेदन के रूप में पहचाना जाता है । यह परेशान है क्योंकि मैं सामान्य त्रुटि संभाल नहीं कर सकते (यह 200 है, एक 403 आदि नहीं).

# हल

तो इसका हल क्या है? मैं इन सभी अनुरोधों को दस्ती रूप से पार्स नहीं कर सकता और पता लगा सकता है कि क्या उममी उन्हें एक बोरी के रूप में पता चल जाएगी; यह http://s.netms.com/package/ssssse.com/sshhhks) का उपयोग करता है यदि कोई अनुरोध है या नहीं. कोई C# बराबर है और यह एक बदलने की सूची है तो मैं भी उस सूची का उपयोग नहीं कर सकते (अब मैं चतुर हो सकता है और सूची का उपयोग कर सकते हैं पता लगाने के लिए पता लगाने के लिए सूची का उपयोग करें यदि कोई अनुरोध है या नहीं.
तो मुझे इस अनुरोध को सुलझाने की जरूरत है इससे पहले कि यह उममी को मिलता है और उपयोक्ता एजेंट को कुछ करने के लिए बदल देता है कि उममी विशिष्ट अनुरोधों के लिए स्वीकार करेंगे।

तो अब मैं उममी में अपने ट्रैक के तरीकों के लिए कुछ अतिरिक्त पैरामीटर्स जोड़े. ये आपको नए 'डिफ़ॉल्ट उपयोक्ता एजेंट' को निर्दिष्ट करने की अनुमति देंगे बजाए मूल उपयोक्ता एजेंट के. यह मुझे उल्लेखित करने देता है कि उपयोक्ता एजेंट विशिष्ट मान में बदला जाए.

## विधि

मेरे पर `UmamiBackgroundSender` मैंने निम्नलिखित जोड़ दिया:

```csharp
   public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
```

यह वहाँ सभी ट्रैकिंग तरीकों पर मौजूद है और सिर्फ एक पैरामीटर सेट करता है `UmamiPayload` वस्तु.

पर `UmamiClient` इन्हें बाद में नियत किया जा सकता है:

```csharp
    [Fact]
    public async Task TrackPageView_WithUrl()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.TrackPageViewAndDecode("https://example.com", "Example Page",
            new UmamiPayload { UseDefaultUserAgent = true });
        Assert.NotNull(response);
        Assert.Equal(UmamiDataResponse.ResponseStatus.Success, response.Status);
    }
```

इस जाँच में मैं नया प्रयोग करता हूँ `TrackPageViewAndDecode` विधि जो एक लौटाता है `UmamiDataResponse` वस्तु. इस वस्तु में जेएचओटी टोकन है (जो अवैध है यदि यह एक बॉट है तो यह उपयोगी है) और निवेदन की स्थिति.

## `PayloadService`

यह सभी हैंडल किया जाता है `Payload` सेवा जो लोड वस्तु को भरने के लिए जिम्मेदार है। यह कहाँ है `UseDefaultUserAgent` सेट है.

डिफ़ॉल्ट से मैं भुगतान से भरता है `HttpContext` तो आप आमतौर पर यह सेट ठीक से मिलता है; मैं बाद में दिखाता हूँ कि यह उममी से वापस खींच लिया जाता है।

```csharp
    private UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var request = httpContext?.Request;

        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Data = data,
            Url = url ?? httpContext?.Request?.Path.Value,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = request?.Headers["User-Agent"].FirstOrDefault(),
            Referrer = request?.Headers["Referer"].FirstOrDefault(),
            Hostname = request?.Host.Host
        };

        return payload;
    }
```

मैं फोन कोड का एक टुकड़ा है `PopulateFromPayload` जहाँ निवेदन वस्तु यह डाटा सेट हो जाता है:

```csharp
    public static string DefaultUserAgent =>
        $"Mozilla/5.0 (Windows 11)  Umami.Net/{Assembly.GetAssembly(typeof(UmamiClient))!.GetName().Version}";

    public UmamiPayload PopulateFromPayload(UmamiPayload? payload, UmamiEventData? data)
    {
        var newPayload = GetPayload(data: data);
        ...
        
        newPayload.UserAgent = payload.UserAgent ?? DefaultUserAgent;

        if (payload.UseDefaultUserAgent)
        {
            var userData = newPayload.Data ?? new UmamiEventData();
            userData.TryAdd("OriginalUserAgent", newPayload.UserAgent ?? "");
            newPayload.UserAgent = DefaultUserAgent;
            newPayload.Data = userData;
        }


        logger.LogInformation("Using UserAgent: {UserAgent}", newPayload.UserAgent);
     }        
        
```

आप देख सकते हैं कि यह फ़ाइल के शीर्ष पर एक नया उपयोगमयकरण परिभाषित करता है (जिसे मैंने पुष्टि किया है) *वर्तमान* एक बॉट के रूप में पता चला. फिर विधि में यह पता चलता है कि क्या उपयोक्ता एजेंट बेकार है (जो कि नहीं होना चाहिए) या यदि यह कोई  दिल के बिना कोड से बुलाया जाता है या अगर `UseDefaultUserAgent` सेट है. यदि यह तब उपयोक्ता एजेंट को डिफ़ॉल्ट में सेट करता है तथा मूल उपयोक्ता एजेंट को डाटा ऑब्जेक्ट में जोड़ता है.

यह तब लॉग किया जा रहा है ताकि आप देख सकते हैं कि उपयोक्ता एजेंट क्या इस्तेमाल किया जा रहा है.

## जवाब देना.

उममी 0 में.N. 0 मैं एक नए 'और Deev' तरीकों की संख्या जोड़ा जो एक वापसी है `UmamiDataResponse` वस्तु. इस वस्तु में DTCT टोकन है.

```csharp
    public async Task<UmamiDataResponse?> TrackPageViewAndDecode(
        string? url = "",
        string? title = "",
        UmamiPayload? payload = null,
        UmamiEventData? eventData = null)
    {
        var response = await TrackPageView(url, title, payload, eventData);
        return await DecodeResponse(response);
    }
    
        private async Task<UmamiDataResponse?> DecodeResponse(HttpResponseMessage responseMessage)
    {
        var responseString = await responseMessage.Content.ReadAsStringAsync();

        switch (responseMessage.IsSuccessStatusCode)
        {
            case false:
                return new UmamiDataResponse(UmamiDataResponse.ResponseStatus.Failed);
            case true when responseString.Contains("beep") && responseString.Contains("boop"):
                logger.LogWarning("Bot detected data not stored in Umami");
                return new UmamiDataResponse(UmamiDataResponse.ResponseStatus.BotDetected);

            case true:
                var decoded = await jwtDecoder.DecodeResponse(responseString);
                if (decoded == null)
                {
                    logger.LogError("Failed to decode response from Umami");
                    return null;
                }

                var payload = UmamiDataResponse.Decode(decoded);

                return payload;
        }
    }
```

आप देख सकते हैं कि यह कॉल सामान्य में `TrackPageView` विधि तब बुलाया जाता है `DecodeResponse` जो प्रतिक्रिया जांचता है `beep` और `boop` वाक्यांश (कंड जांच के लिए) और जब वह उनके सामने एक डरानेवाला पहुँचे तो उससे कहा, "यह तो बस एक चेतावनी है। फिर क्या देखते है कि वह उनके लिए होश में आता है और उनके पास कोई डरानेवाला आ जाता है? `BotDetected` स्थिति. यदि यह उन्हें नहीं मिलता है तो यह JT टोकन बजता है और भुगतान लोड करता है.

UNT टोकन स्वयं एक बेस64 पीसित स्ट्रिंग है जिसमें उममी जमा है. यह डिकोड्ड है और एक के रूप में वापस आ गया है `UmamiDataResponse` वस्तु.

इस के लिए पूरा स्रोत नीचे है:

<details>
<summary>Response Decoder</summary>

```csharp
using System.IdentityModel.Tokens.Jwt;

namespace Umami.Net.Models;

public class UmamiDataResponse
{
    public enum ResponseStatus
    {
        Failed,
        BotDetected,
        Success
    }

    public UmamiDataResponse(ResponseStatus status)
    {
        Status = status;
    }

    public ResponseStatus Status { get; set; }

    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public string? Hostname { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Screen { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? Subdivision1 { get; set; }
    public string? Subdivision2 { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public long Iat { get; set; }

    public static UmamiDataResponse Decode(JwtPayload? payload)
    {
        if (payload == null) return new UmamiDataResponse(ResponseStatus.Failed);
        payload.TryGetValue("visitId", out var visitIdObj);
        payload.TryGetValue("iat", out var iatObj);
        //This should only happen then the payload is dummy.
        if (payload.Count == 2)
        {
            var visitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty;
            var iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0;

            return new UmamiDataResponse(ResponseStatus.Success)
            {
                VisitId = visitId,
                Iat = iat
            };
        }

        payload.TryGetValue("id", out var idObj);
        payload.TryGetValue("websiteId", out var websiteIdObj);
        payload.TryGetValue("hostname", out var hostnameObj);
        payload.TryGetValue("browser", out var browserObj);
        payload.TryGetValue("os", out var osObj);
        payload.TryGetValue("device", out var deviceObj);
        payload.TryGetValue("screen", out var screenObj);
        payload.TryGetValue("language", out var languageObj);
        payload.TryGetValue("country", out var countryObj);
        payload.TryGetValue("subdivision1", out var subdivision1Obj);
        payload.TryGetValue("subdivision2", out var subdivision2Obj);
        payload.TryGetValue("city", out var cityObj);
        payload.TryGetValue("createdAt", out var createdAtObj);

        return new UmamiDataResponse(ResponseStatus.Success)
        {
            Id = idObj != null ? Guid.Parse(idObj.ToString()!) : Guid.Empty,
            WebsiteId = websiteIdObj != null ? Guid.Parse(websiteIdObj.ToString()!) : Guid.Empty,
            Hostname = hostnameObj?.ToString(),
            Browser = browserObj?.ToString(),
            Os = osObj?.ToString(),
            Device = deviceObj?.ToString(),
            Screen = screenObj?.ToString(),
            Language = languageObj?.ToString(),
            Country = countryObj?.ToString(),
            Subdivision1 = subdivision1Obj?.ToString(),
            Subdivision2 = subdivision2Obj?.ToString(),
            City = cityObj?.ToString(),
            CreatedAt = createdAtObj != null ? DateTime.Parse(createdAtObj.ToString()!) : DateTime.MinValue,
            VisitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty,
            Iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0
        };
    }
}
```

</details>
आप देख सकते हैं कि इस अनुरोध के बारे में उपयोगी जानकारी है कि उममी में भंडारित है. यदि आप लोकेल, भाषा, ब्राउज़र पर आधारित विभिन्न सामग्री दिखाने के लिए चाहते हैं तो यह आपको यह करने देता है.

```csharp
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public string? Hostname { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Screen { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? Subdivision1 { get; set; }
    public string? Subdivision2 { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public long Iat { get; set; }
```

# ऑन्टियम

तो सिर्फ एक छोटा सा पोस्ट कवर उमम में कुछ नया कार्य कवर.Net 0. 0 जो आपको विशिष्ट निवेदन के लिए डिफ़ॉल्ट उपयोक्ता एजेंट उल्लेखित करने की अनुमति देता है. यह अनुरोधों को ट्रैक करने के लिए उपयोगी है कि उममी अन्यथा नज़रअंदाज़ की जाएगी.