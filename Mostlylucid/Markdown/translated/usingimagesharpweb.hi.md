# छवि को तीव्र उपयोग में लें. UNAT कोर के साथ वेब पर.

<datetime class="hidden">2024- 0. 1313T14: 16</datetime>

<!--category-- ASP.NET, ImageSharp -->
## परिचय

[छवि सुस्पष्ट](https://docs.sixlabors.com/index.html)एक शक्तिशाली छवि प्रक्रिया लाइब्रेरी है जो आपको अनेक तरीकों से छवियों को रूपांतरित करने की अनुमति देता है. चित्रों का एक विस्तार है जो आपको छवि सुस्पष्टता से काम करने के लिए अतिरिक्त प्रकार्य प्रदान करता है जो कि एनईटीटीटीसी अनुप्रयोगों में काम करने के लिए. इस शिक्षण अनुप्रयोग में, हम चित्र को पुनः उपयोग करने के लिए कैसे इस्तेमाल करेंगे.

[विषय

## छवि सुस्पष्ट. वेब संस्थापन

छवि सुस्पष्टता से प्रारंभ करने के लिए. वेब, आपको निम्न एनयूएओ पैकेज संस्थापित करने की आवश्यकता होगी:

```bash
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Web
```

## छवि को सुस्पष्ट करें. वेब कॉन्फ़िगरेशन

हमारे प्रोग्राम में फिर हम छवि को सुस्पष्ट सेट. वेब पर सेट करें. हमारे मामले में हम एक फ़ोल्डर में अपनी छवियों का उल्लेख कर रहे हैं जो हमारी परियोजना के www.dririririririoc. हम फिर छवि सेट करें इस फ़ोल्डर का उपयोग करने के लिए हमारे छवियों के स्रोत के रूप में।

छवि को सुस्पष्ट करें. वेब पर फ़ाइलों को भंडारित करने के लिए 'केश' फ़ोल्डर का भी उपयोग करता है (यह प्रत्येक बार फ़ाइलों को फिर से चालू करने से रोकता है).

```csharp
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

ये फ़ोल्डर www.jw.org से संबंधित हैं इसलिए हमारे पास निम्न संरचना है:

![फ़ोल्डर स्ट्रक्चर](/cachefolder.png)

छवि सुस्पष्ट. वेब पर बहुत सारे विकल्प हैं जहाँ आप अपनी फ़ाइलों तथा कलिंगिंग को जमा करते हैं (सभी विवरण के लिए यहाँ देखें):[https://probs.com/aribs. arbs/images.hml?](https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a))

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

अब कि हम यह सेट किया है यह वास्तव में आसान है हमारे अनुप्रयोग के अंदर इसका उपयोग करने के लिए. उदाहरण के लिए यदि हम एक नया छवि सेवा करना चाहते हैं हम या तो इस्तेमाल कर सकते हैं[टैग मदद करनेवाला](https://sixlabors.com/posts/announcing-imagesharp-web-300/#imagetaghelper)यूआरएल को सीधे उल्लेखित करें या उल्लेखित करें.

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

इस app में हम सरल तरीके से जाना और सिर्फ क्वैरी पैरामीटरों का उपयोग करें. चिह्न के लिए हम एक विस्तार का उपयोग करें जो हमें छवि आकार और प्रारूप को निर्दिष्ट करने की अनुमति देता है.

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

यह छवि कहाँ से आएगी`wwwroot/articleimages/image.jpg`और 50% गुणवत्ता और वेब फ़ॉर्मेट में नया आकार बदलें.

या हम छवि का प्रयोग बतौर है का उपयोग कर सकते हैं और इसे रिसाइज किया जाएगा जैसा कि क्वेरी स्ट्रिंग में निर्दिष्ट किया गया है.

## कंटेनमेंट

जैसा कि आपने छवि को सुस्पष्ट देखा. वेब पर हमें एक बहुत बड़ी क्षमता प्रदान करती है कि हम आकार प्राप्त करें और छवियों को हमारे अप्रयोग अनुप्रयोग में फ़ॉर्मेट करें. यह सेट और उपयोग करने के लिए आसान है और प्रदान करता है कि हम कैसे हमारे अनुप्रयोगों में छवियों में हेरफेर कर सकते हैं.