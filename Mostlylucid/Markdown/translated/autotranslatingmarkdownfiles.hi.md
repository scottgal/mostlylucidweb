# आसानएनएमटी के साथ मार्क नीचे की टोकरी को स्वचालित प्रतिस्थापित किया जा रहा है

## परिचय

आसानNMT एक स्थानीय संस्थापित सेवा है जो मशीन अनुवाद सेवाओं के लिए एक सरल इंटरफेस प्रदान करता है. इस शिक्षण में, हम स्वचलित रूप से अंग्रेज़ी से अनेक भाषाओं में मार्क फ़ाइल का अनुवाद करने के लिए उपयोग करेंगे.

इस शिक्षण पाठ के लिए सभी फ़ाइलों को आप ढूंढ सकते हैं.[Git भंडार](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator)इस परियोजना के लिए.

ध्यान दीजिए: यह अभी भी बहुत कठिन है, मैं इसे शुद्ध करते रहेंगे के रूप में मैं जा रहा हूँ.

मैंने केवल इस फ़ाइल का ही अनुवाद किया है (सही है और यह)[मेरे बारे में](/blog/aboutme)जैसे - जैसे मैं इस विधि को शुद्ध करता हूँ; अनुवाद के साथ कुछ मसले भी होते हैं जिसे पूरा करने की मुझे ज़रूरत है ।

[विषय

## पूर्वपाराईज़

आसान एनएनएमटी का संस्थापन इस शिक्षण पाठ का पालन करने के लिए आवश्यक है. मैं इसे आम तौर पर एक डॉकर सेवा के रूप में चलाते हैं. आप संस्थापना निर्देश पा सकते हैं.[यहाँ](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md)जो उसे एक डॉकer सेवा के रूप में चलाने के लिए कैसे कवर करता है।

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

या फिर अगर आपके पास कोई नीकडिया जीयू उपलब्ध हो:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

MAX_ WORCENT_BAR_FORSCTE(FRTE) वातावरण चर ने उन कर्मचारियों की संख्या निर्धारित की जो आसानMT का उपयोग करेंगे _BAR_ आप इन्हें अपने मशीन को सूट करने के लिए समायोजित कर सकते हैं _BAR_

टिप्पणी: आसान नहीं है SMOMOT सेवा चलाने के लिए नहीं है, लेकिन यह सबसे अच्छा है मैं इस उद्देश्य के लिए मिल गया है. यह एक बिट है इनपुट स्ट्रिंग के बारे में यह पारित किया गया है के बारे में एक बिट है, तो आप अपने पाठ के कुछ पूर्व प्रक्रिया करने की जरूरत हो सकता है इसे आसान करने के लिए.

## बाल - श्रम को लोड करने के लिए अयोग्य

आसान NMT एक प्यासी जानवर है जब यह संसाधनों के लिए आता है, तो मेरे मार्क इटेलर सर्विसेशन में मेरे पास एक बहुत ही साधारण बेतरतीब बेतरतीब बेतरतीब IP चयनक है कि सिर्फ मेरे पास मशीनों की सूची से एक बेतरतीब IP लेता है. यह एक छोटा सा बेवकूफ है और एक अधिक जटिल भार एल्गोरिथ्म का उपयोग करने के द्वारा बेहतर किया जा सकता है.

```csharp
    private string[] IPs = new[] { "http://192.168.0.30:24080", "http://localhost:24080", "http://192.168.0.74:24080" };

     var ip = IPs[random.Next(IPs.Length)];
     logger.LogInformation("Sendign request to {IP}", ip);
     var response = await client.PostAsJsonAsync($"{ip}/translate", postObject, cancellationToken);

```

## चिह्न नीचे किया जा रहा है@ info: whatsthis

यह वह कोड है जिसके लिए मैं मार्क इग्निटर सर्विस में काम करता हूँ. यह एक सरल सेवा है जो एक चिह्न वाक्यांश और एक लक्ष्य भाषा लेता है तथा अनुवाद किए गए चिह्न वाक्यांश को बताता है.

```csharp
    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken)
    {
        var document = Markdig.Markdown.Parse(markdown);
        var textStrings = ExtractTextStrings(document);
        var batchSize = 50;
        var stringLength = textStrings.Count;
        List<string> translatedStrings = new();
        for (int i = 0; i < stringLength; i += batchSize)
        {
            var batch = textStrings.Skip(i).Take(batchSize).ToArray();
            translatedStrings.AddRange(await Post(batch, targetLang, cancellationToken));
        }


        ReinsertTranslatedStrings(document, translatedStrings.ToArray());
        return document.ToMarkdownString();
    }
```

जैसे - जैसे आप देख सकते हैं कि उसमें कई कदम हैं:

1. `  var document = Markdig.Markdown.Parse(markdown);`- यह निशान चिह्न को दस्तावेज़ में रखता है.
2. `  var textStrings = ExtractTextStrings(document);`- यह दस्तावेज़ से पाठ वाक्यांश निकालता है.
3. `  var batchSize = 50;`- यह अनुवाद सेवा के लिए बैच आकार सेट करता है. आसानMMT के अक्षरों की संख्या पर एक जाना में यह अनुवाद कर सकते हैं.
4. `csharp await Post(batch, targetLang, cancellationToken)`
   यह उस विधि में आता है जो तब हटाया जाता है आसानएमटी सेवा में.

```csharp
    private async Task<string[]> Post(string[] elements, string targetLang, CancellationToken cancellationToken)
    {
        try
        {
            var postObject = new PostRecord(targetLang, elements);
            var response = await client.PostAsJsonAsync("/translate", postObject, cancellationToken);

            var phrase = response.ReasonPhrase;
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PostResponse>(cancellationToken: cancellationToken);

            return result.translated;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error translating markdown: {Message} for strings {Strings}", e.Message, string.Concat( elements, Environment.NewLine));
            throw;
        }
    }
```

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());`- यह अनुवाद किए गए वाक्यांश को फिर से दस्तावेज़ में बदल देता है. मार्कडिग की क्षमता का उपयोग करके दस्तावेज़ को चला जाता है तथा पाठ वाक्यांश बदल देता है.

## होस्ट सेवा

यह सब चलाने के लिए मैं एक IHobed जीवन सेवा सेवा का उपयोग करने के लिए जो प्रोग्राम में प्रारंभ किया गया है. यह सेवा एक चिह्न फ़ाइल में पढ़ता है, यह कई भाषाओं में अनुवाद करता है और इस अनुवाद में डिस्क में अनुवाद किया गया फ़ाइल लिखते हैं.

```csharp
    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles("Markdown", "*.md");

        var outDir = "Markdown/translated";

        var languages = new[] { "es", "fr", "de", "it", "jap", "uk", "zh" };
        foreach (var language in languages)
        {
            foreach (var file in files)
            {
                var fileChanged = await file.IsFileChanged(outDir);
                var outName = Path.GetFileNameWithoutExtension(file);

                var outFileName = $"{outDir}/{outName}.{language}.md";
                if (File.Exists(outFileName) && !fileChanged)
                {
                    continue;
                }

                var text = await File.ReadAllTextAsync(file, cancellationToken);
                try
                {
                    logger.LogInformation("Translating {File} to {Language}", file, language);
                    var translatedMarkdown = await blogService.TranslateMarkdown(text, language, cancellationToken);
                    await File.WriteAllTextAsync(outFileName, translatedMarkdown, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error translating {File} to {Language}", file, language);
                }
            }
        }

        logger.LogInformation("Background translation service started");
    }
```

जैसा कि आप देख सकते हैं यह फ़ाइल के हैश को भी जाँच कर सकते हैं कि यह इसका अनुवाद करने से पहले बदल गया है या नहीं. यह उन फ़ाइलों से बचने के लिए है जो बदल नहीं गया है.

यह मूल चिह्न फ़ाइल की तेजी से गणना करने के द्वारा किया जाता है फिर यह देखने के लिए कि क्या उस फ़ाइल का अनुवाद करने से पहले परिवर्तन हुआ है.

```csharp
    private static async Task<string> ComputeHash(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        stream.Position = 0;
        var bytes = new byte[stream.Length];
        await stream.ReadAsync(bytes);
        stream.Position = 0;
        var hash = XxHash64.Hash(bytes);
        var hashString = Convert.ToBase64String(hash);
        hashString = InvalidCharsRegex.Replace(hashString, "_");
        return hashString;
    }
```

प्रोग्राम में सेटअप बहुत सरल है:

```csharp

    builder.Services.AddHostedService<BackgroundTranslateService>();
services.AddHttpClient<MarkdownTranslatorService>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(15);
    options.BaseAddress = new Uri("http://192.168.0.30:24080");
});
```

मैंने होस्ट अप अप- सेंटर (कैंट- सेंटर- सेंटर- सर्विस) और मार्क इंटरनॅशनर सर्विस के लिए giolicententents.
एक होस्ट सेवा एक लंबी यात्रा सेवा है जो पृष्ठभूमि में चलती है. यह एक अच्छा जगह है जो सेवाओं को जारी रखने के लिए आवश्यकता है या सिर्फ एक लंबे समय तक पूरा करने के लिए। नए HHEHERTR समय इंटरफ़ेस एक बिट के बजाय एक बिट के रूप में एक छोटा सा है।

यहाँ आप देख सकते हैं कि मैं समय निर्धारित कर रहा हूँ 15 मिनट के लिए। यह इसलिए है क्योंकि आसानNMT प्रतिक्रिया करने के लिए थोड़ा धीमा हो सकता है (सामान्य रूप से एक भाषा मॉडल का उपयोग कर पहली बार है)। मैं भी मशीन के IP पता सेट कर रहा हूँ आसानNT सेवा के लिए।

आई