# उममी ट्रेकिंग क्लाएंट Nuget पैकेज जोड़े

<!--category-- ASP.NET, Umami, Nuget -->
<datetime class="hidden">2024- 022: 00</datetime>

# परिचय

अब मेरे पास उममी क्लाएंट है, मुझे इसे पैकेज करने की जरूरत है और इसे एक Nuget पैकेज के रूप में उपलब्ध बनाने के लिए. यह एक बहुत ही सरल प्रक्रिया है लेकिन कुछ बातें पता करने के लिए हैं.

[विषय

# नुचिट पैकेज बनाया जा रहा है

## संस्करण

मैंने उनकी नकल करने का फैसला किया [खाल](@khalidabuhakmeh@mastodon.social) और बेहतरीन मिचवर पैकेज का उपयोग मेरे Nuget पैकेज को संस्करण के लिए करें. यह एक सरल पैकेज है जो संस्करण संख्या निर्धारित करने के लिए gvit संस्करण टैग प्रयोग करता है.

इसे प्रयोग करने के लिए मैं ने बस मेरे लिए निम्नलिखित जोड़ दिया `Umami.Net.csproj` फ़ाइल:

```xml
    <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```

जिस तरह से मैं अपने संस्करण को एक साथ टैग कर सकते हैं `v` और पैकेज ठीक से संस्करण किया जाएगा.

```bash
 git tag v0.0.8       
 git push origin v0.0.8

```

इस टैग को धक्का देंगे, तो मेरे पास एक Git क्रिया सेटअप है उस टैग के लिए इंतजार करने के लिए और Nuget पैकेज बनाने के लिए.

## नुरू पैकेज का निर्माण करना

मेरे पास एक Githrate है जो Nuget पैकेज को बनाता है और इसे Githb पैकेज भंडार में धक्का देता है. यह एक सादा प्रक्रिया है जो प्रयोग करती है `dotnet pack` पैकेज को निर्माण करने का कमांड तथा तब `dotnet nuget push` Exget भंडार में इसे पुश करने के लिए कमांड.

```yaml
name: Publish Umami.NET
on:
  push:
    tags:
      - 'v*.*.*'  # This triggers the action for any tag that matches the pattern v1.0.0, v2.1.3, etc.

jobs:
  publish:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.x' # Specify the .NET version you need

    - name: Restore dependencies
      run: dotnet restore ./Umami.Net/Umami.Net.csproj

    - name: Build project
      run: dotnet build --configuration Release ./Umami.Net/Umami.Net.csproj --no-restore

    - name: Pack project
      run: dotnet pack --configuration Release ./Umami.Net/Umami.Net.csproj --no-build --output ./nupkg

    - name: Publish to NuGet
      run: dotnet nuget push ./nupkg/*.nupkg --source https://api.nuget.org/v3/index.json --api-key ${{ secrets.UMAMI_NUGET_API_KEY }}
      env:
        NUGET_API_KEY: ${{ secrets.UMAMI_NUGET_API_KEY }}
```

### पढ़ना तथा प्रतीक जोड़ना

यह बहुत सरल है, मैं एक जोड़ें `README.md` परियोजना और एक रूट के लिए फ़ाइल `icon.png` परियोजना की रूट में फ़ाइल. वह `README.md` पैकेज और इसके विवरण के रूप में फ़ाइल प्रयोग में लिया गया है `icon.png` पैकेज के प्रतीक के रूप में फ़ाइल उपयोग में लिया जाएगा.

```xml
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <LangVersion>12</LangVersion>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>

        <IsPackable>true</IsPackable>
        <PackageId>Umami.Net</PackageId>
        <Authors>Scott Galloway</Authors>
        <PackageIcon>icon.png</PackageIcon>
        <RepositoryUrl>https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net</RepositoryUrl>
        <PackageLicenseExpression>MIT</PackageLicenseExpression>
        <PackageTags>web</PackageTags>
        <PackageReadmeFile>README.md</PackageReadmeFile>
        <Description>
           Adds a simple Umami endpoint to your ASP.NET Core application.
        </Description>
    </PropertyGroup>
```

मेरे पढ़ने में.md फ़ाइल मेरे पास Gimb भंडार में लिंक है और पैकेज का वर्णन है.

नीचे दिए गए उत्पादनों को पुन: व्यवस्थित करें:

# उममी.Net

यह उममी ट्रैक एपीआई के लिए.नेट कोर ग्राहक है.
यह उममी नोड ग्राहक पर आधारित है, जो मिल सकता है [यहाँ](https://github.com/umami-software/node).

आप देख सकते हैं कि कैसे उमा को एक डाकर पात्र के रूप में सेट किया जा सकता है [यहाँ](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics).
आप इस बारे में अधिक विस्तृत पढ़ सकते हैं मेरे ब्लॉग पर बनाने के बारे में [यहाँ](https://www.mostlylucid.net/blog/addingumamitrackingclientfollowup).

इस क्लाएंट का उपयोग करने के लिए आपको निम्न एग्रेसन कॉन्फ़िगरेशन की आवश्यकता है:

```json
{
  "Analytics":{
    "UmamiPath" : "https://umamilocal.mostlylucid.net",
    "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  },
}
```

कहाँ `UmamiPath` अपने उममी उदाहरण और का पथ है `WebsiteId` वेबसाइट का आईडी है जिसे आप ट्रैक करना चाहते हैं.

क्लाइंट का प्रयोग करने के लिए आपको निम्नलिखित जोड़ने की जरूरत है `Program.cs`:

```csharp
using Umami.Net;

services.SetupUmamiClient(builder.Configuration);
```

यह सेवा संग्रह में उममी क्लाएंट जोड़ेगा.

तो आप दो तरीकों से क्लाएंट का उपयोग कर सकते हैं:

1. इन्हें बाहर करें `UmamiClient` अपनी क्लास में और फोन करने के लिए `Track` विधि:

```csharp
 // Inject UmamiClient umamiClient
 await umamiClient.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

2. उपयोग `UmamiBackgroundSender` पृष्ठभूमि में घटनाओं को ट्रैक करने के लिए (यह किसी को प्रयोग करता है) `IHostedService` पृष्ठभूमि में घटनाओं को भेजने के लिए:

```csharp
 // Inject UmamiBackgroundSender umamiBackgroundSender
await umamiBackgroundSender.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

क्लाएंट घटना को उममी एपीआई में भेज देगा और यह जमा किया जाएगा.

वह `UmamiEventData` कुंजी मूल्य जोड़े का शब्दकोश है जो कि घटना डाटा के रूप में उममी एपीआई में भेजा जाएगा.

इसके अतिरिक्‍त कम स्तर ऐसे तरीक़े हैं जिन्हें उममी एपीआई में घटनाओं को भेजने के लिए प्रयोग किया जा सकता है ।

दोनों पर `UmamiClient` और `UmamiBackgroundSender` आप निम्न विधि कॉल कर सकते हैं.

```csharp


 Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

आप एक में पास नहीं है `UmamiPayload` वस्तु, ग्राहक आप के लिए उपयोग कर एक होगा `WebsiteId` ए.एस.सन से।

```csharp
    public  UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
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
           Hostname = request?.Host.Host,
        };
        
        return payload;
    }

```

आप देख सकते हैं कि यह भरें `UmamiPayload` वस्तु के साथ `WebsiteId` Aagss.jon से, `Url`, `IpAddress`, `UserAgent`, `Referrer` और `Hostname` से `HttpContext`.

नोट: घटना क़िस्म केवल "मिनट" हो सकता है या "अनुष्ट" को प्रति उममी एपीआई में.

# ऑन्टियम

तो यह है कि आप अब एमुमा स्थापित कर सकते हैं. Nuget से और इसे अपने unuck में उपयोग करें. Nuc.NT कोर अनुप्रयोग में. मुझे आशा है कि आप इसे उपयोगी पाते हैं. मैं आगे की पोस्टों में जाँच जारी रखेंगे.