# छवि को तीव्र उपयोग में लें. UNAT कोर के साथ वेब पर.

<datetime class="hidden">2024- 0. 1313T14: 16</datetime>

<!--category-- ASP.NET, ImageSharp -->
## परिचय

[छवि सुस्पष्ट](https://docs.sixlabors.com/index.html) यह एक शक्‍तिशाली छवि है जो आपको विभिन्‍न तरीक़ों से छवि करने की अनुमति देती है । छवि को त्वरित करें. वेब पर तीव्र विस्तार है जो कि छवि के साथ काम करने के लिए अतिरिक्त कार्य देता है.CUNK अनुप्रयोगों के साथ काम करने के लिए. इस शिक्षण पाठ में, हम छवि को सुस्पष्ट करने के लिए कैसे इस्तेमाल करेंगे. वेब- आकार को नया बनाने, काटने, तथा इस अनुप्रयोग में छवि फ़ॉर्मेट करने के लिए वेब साइटों का उपयोग करें.

[विषय

## छवि सुस्पष्ट. वेब संस्थापन

छवि सुस्पष्टता से प्रारंभ करने के लिए. वेब, आपको निम्न एनयूएओ पैकेज संस्थापित करने की आवश्यकता होगी:

```bash
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Web
```

## छवि को सुस्पष्ट करें. वेब कॉन्फ़िगरेशन

हमारे प्रोग्राम में.css फ़ाइल हम फिर छवि सुस्पष्ट सेट। वेब। हमारे मामले में हम एक फ़ोल्डर में अपनी छवियों को जमा कर रहे हैं जो हमारे परियोजना के www.example.com में कहा जाता है। इस फ़ोल्डर का उपयोग करने के लिए इस फ़ोल्डर को अपनी छवियों के स्रोत के रूप में करें।

छवि को सुस्पष्ट करें. वेब पर फ़ाइलों को भंडारित करने के लिए 'केश' फ़ोल्डर का भी उपयोग करता है (यह प्रत्येक बार फ़ाइलों को फिर से चालू करने से रोकता है).

```csharp
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

ये फ़ोल्डर www.jw.org से संबंधित हैं इसलिए हमारे पास निम्न संरचना है:

![फ़ोल्डर स्ट्रक्चर](/cachefolder.png)

छवि सुस्पष्ट. वेब पर बहुत सारे विकल्प हैं जहाँ आप अपनी फ़ाइलों तथा कलिंगिंग को जमा करते हैं (सभी विवरण के लिए यहाँ देखें): [https://probs.com/aribs. arbs/images.hml?](https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a))

उदाहरण के लिए कि अपनी छवियों को क्वीप पात्र में जमा करने के लिए (अप्रैल्स के लिए) आप Ablphbibibibs विकल्प का उपयोग करेंगे:

```bash
dotnet add SixLabors.ImageSharp.Web.Providers.Azure
```

```csharp
// Configure and register the containers.  
// Alteratively use `appsettings.json` to represent the class and bind those settings.
.Configure<AzureBlobStorageImageProviderOptions>(options =>
{
    // The "BlobContainers" collection allows registration of multiple containers.
    options.BlobContainers.Add(new AzureBlobContainerClientOptions
    {
        ConnectionString = {AZURE_CONNECTION_STRING},
        ContainerName = {AZURE_CONTAINER_NAME}
    });
})
.AddProvider<AzureBlobStorageImageProvider>()
```

## छवि सुस्पष्ट. वेब उपयोग

अब हम यह सेट किया है कि यह वास्तव में आसान है हमारे अनुप्रयोग के अंदर इसका उपयोग करने के लिए। उदाहरण के लिए यदि हम एक आकारित छवि की सेवा करना चाहते हैं तो हम या तो इस्तेमाल कर सकते हैं [टैग मदद करनेवाला](https://sixlabors.com/posts/announcing-imagesharp-web-300/#imagetaghelper) यूआरएल को सीधे उल्लेखित करें या उल्लेखित करें.

टैग मददर:

```razor
<img
    src="sixlabors.imagesharp.web.png"
    imagesharp-width="300"
    imagesharp-height="200"
    imagesharp-rmode="ResizeMode.Pad"
    imagesharp-rcolor="Color.LimeGreen" />

```

ध्यान दीजिए कि इस के साथ हम छवि को नया आकार दे रहे हैं, चौड़ाई और ऊँचाई सेट, और भी नया आकार मॉडल बनाने और छवि को फिर से रंग देते हैं।

इस app में हम सरल तरीके से जाना और सिर्फ क्वैरी पैरामीटरों का प्रयोग करें. चिह्नन के लिए हम एक विस्तार का उपयोग करते हैं जो हमें छवि आकार और प्रारूप निर्दिष्ट करने की अनुमति देता है.

```csharp
    public void ChangeImgPath(MarkdownDocument document)
    {
        foreach (var link in document.Descendants<LinkInline>())
            if (link.IsImage)
            {
                if(link.Url.StartsWith("http")) continue;
                
                if (!link.Url.Contains("?"))
                {
                   link.Url += "?format=webp&quality=50";
                }

                link.Url = "/articleimages/" + link.Url;
            }
               
    }
```

यह हमें इन पोस्टों में इन को निर्दिष्ट करने की कमी देता है

```markdown
![image](/image.jpg?format=webp&quality=50)
```

यह छवि कहाँ से आएगी `wwwroot/articleimages/image.jpg` और 50% गुणवत्ता और वेब फ़ॉर्मेट में नया आकार बदलें.

या हम छवि का प्रयोग बतौर है का उपयोग कर सकते हैं और इसे रिसाइज किया जाएगा जैसा कि क्वेरी स्ट्रिंग में निर्दिष्ट किया गया है.

## डॉकर

टिप्पणी `cache` मैं ऊपर इस्तेमाल किया है अनुप्रयोग द्वारा लिखने की जरूरत है. यदि आप डॉकर का उपयोग कर रहे हैं आप सुनिश्चित करने के लिए की आवश्यकता होगी कि यह मामले है.
देखें [मेरा पहले पोस्ट](/blog/imagesharpwithdocker) कैसे के लिए मैं इसे एक मैप वॉल्यूम का उपयोग कर सकते हैं.

## कंटेनमेंट

जैसा कि आपने छवि को सुस्पष्ट देखा है. वेब हमें एक महान क्षमता प्रदान करता है आकार का आकार और हमारे आन्तरिक अनुप्रयोगों में छवियों को फ़ॉर्मेट करने के लिए। यह सेट करने और उपयोग करने के लिए आसान है और प्रदान करता है कि कैसे हम हमारे अनुप्रयोगों में छवि चल सकते हैं।