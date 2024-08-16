# आसानएनएमटी के साथ मार्क नीचे की टोकरी को स्वचालित प्रतिस्थापित किया जा रहा है

<datetime class="hidden">2024- 08- 0.3T13: 30</datetime>

<!--category-- EasyNMT, Markdown -->
## परिचय

आसानNMT एक स्थानीय संस्थापित सेवा है जो मशीन अनुवाद सेवा के लिए एक सरल इंटरफेस प्रदान करता है । इस शिक्षण पाठ में, हम अंग्रेज़ी से अनेक भाषाओं में एक मार्कएमसी फ़ाइल को स्वचालित अनुवादित करने के लिए आसान उपयोग करेंगे.

इस शिक्षण पाठ के लिए सभी फ़ाइलों को आप ढूंढ सकते हैं. [Git भंडार](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) इस परियोजना के लिए.

छवि फ़ाइल नाम प्रदर्शित करने के लिए यह विकल्प सेट करें. यह बहुत ही सरल तरीका है कि एक ब्लॉग पोस्ट को अनेक भाषाओं में अनुवादित किया जाए.

[अनुवादित पोस्ट्स](/translatedposts.png)

[विषय

## पूर्वपाराईज़

इस शिक्षण पाठ का पालन करने के लिए आसानएनएमएमटी का संस्थापन आवश्यक है. मैं आम तौर पर इसे एक डॉकर सेवा के रूप में चला जाता है. आपको संस्थापन निर्देश मिल सकते हैं [यहाँ](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) जो उसे एक डॉकer सेवा के रूप में चलाने के लिए कैसे कवर करता है।

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

या फिर अगर आपके पास कोई नीकडिया जीयू उपलब्ध हो:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

MAX_ WORCENT_BAR_FORSCT(FERTE) पर्यावरण चर ने उस आसानMT का उपयोग करने वाले कर्मचारियों की संख्या सेट किया है. आप इन को अपने मशीन सूट करने के लिए समायोजित कर सकते हैं.

ध्यान दें: आसानMOMOMT सेवा नहीं है चलाने के लिए, लेकिन यह सबसे अच्छा है मैं इस उद्देश्य के लिए मिल गया है. यह इनपुट स्ट्रिंग के बारे में एक बिट है यह पारित किया गया है, तो आप अपने इनपुट पाठ के कुछ पूर्व प्रक्रिया करने की आवश्यकता हो सकती है इसे आसानNMT करने से पहले.

ओह और यह भी इयू के लिए प्रस्ताव प्रस्तुत करने के बारे में कुछ बकवास 'Cacon' का अनुवाद किया.

## बाल - श्रम को लोड करने के लिए अयोग्य

आसान NMT एक प्यास वाला जानवर है जब संसाधन के लिए आता है, तो मेरे मार्क इटेलर सर्विस में मेरे पास एक बहुत ही सरल बेतरतीब बेतरतीब IP चयनक है जो सिर्फ मशीन की सूची के माध्यम से घूमता है मैं आसानNTTT चलाने के लिए इस्तेमाल.

शुरू में यह पर एक मिल जाता है `model_name` आसानएनएमटी सेवा पर विधि, यह एक त्वरित, जांच करने के लिए आसान तरीका है अगर सेवा ऊपर है. यदि यह है, यह आईपी को IP में कार्य करने की सूची में जोड़ता है. यह नहीं है, यह सूची में जोड़ नहीं है.

```csharp
    private string[] IPs = translateServiceConfig.IPs;
    public async ValueTask<bool> IsServiceUp(CancellationToken cancellationToken)
    {
        var workingIPs = new List<string>();

        try
        {
            foreach (var ip in IPs)
            {
                logger.LogInformation("Checking service status at {IP}", ip);
                var response = await client.GetAsync($"{ip}/model_name", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    workingIPs.Add(ip);
                }
            }

            IPs = workingIPs.ToArray();
            if (!IPs.Any()) return false;
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error checking service status");
            return false;
        }
    }
```

तब भीतर `Post` विधि `MarkdownTranslatorService` हम अगले एक को खोजने के लिए कार्यशील आईपी के माध्यम से घुमाया गया.

```csharp
          if(!IPs.Any())
            {
                logger.LogError("No IPs available for translation");
                throw new Exception("No IPs available for translation");
            }
            var ip = IPs[currentIPIndex];
            
            logger.LogInformation("Sending request to {IP}", ip);
        
            // Update the index for the next request
            currentIPIndex = (currentIPIndex + 1) % IPs.Length;
```

यह एक बहुत ही सरल तरीका है...... संतुलन के लिए कई मशीनों के पार अनुरोधों को लोड करने के लिए. यह सही नहीं है (यह परीक्षा के लिए एक सुपर व्यस्त मशीन के लिए खाता नहीं है, लेकिन यह मेरे उद्देश्य के लिए पर्याप्त है.

कैंची ` currentIPIndex = (currentIPIndex + 1) % IPs.Length;` सिर्फ आईपी्स की सूची में से 0 से प्रारंभ होता है और सूची की लंबाई तक जाता है.

## चिह्न नीचे किया जा रहा है@ info: whatsthis

यह वह कोड है जिसके लिए मैं चिह्नित इवेल्यूशन सर्विस में काम करता हूँ. यह एक सरल सेवा है जो एक निशान की स्ट्रिंग और एक लक्ष्य भाषा लेता है और अनुवाद चिह्न वाक्यांश लौटाता है.

```csharp
    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken)
    {
        var document = Markdig.Markdown.Parse(markdown);
        var textStrings = ExtractTextStrings(document);
        var batchSize = 10;
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

1. `  var document = Markdig.Markdown.Parse(markdown);` - यह निशान चिह्न को दस्तावेज़ में रखता है.
2. `  var textStrings = ExtractTextStrings(document);` - यह दस्तावेज़ से पाठ वाक्यांश निकालता है.
   यह विधि प्रयोग करता है

```csharp
  private bool IsWord(string text)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
        if (imageExtensions.Any(text.Contains)) return false;
        return text.Any(char.IsLetter);
    } 
```

यह जाँच करता है कि क्या'वर्ड' वास्तव में एक कार्य है; छवि नाम आसानNMT में वाक्य विभाजन को साझा कर सकते हैं.

3. `  var batchSize = 10;` - यह अनुवाद सेवा के लिए बैच आकार सेट करता है. आसानNMT शब्दों की संख्या पर सीमा है जो इसे एक जाने में अनुवादित कर सकते हैं (दूर 500, इसलिए १० लाइन आम तौर पर एक अच्छा बैच आकार है).
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

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());` - यह फिर से अनुवाद किए गए वाक्यांश को दस्तावेज़ में वापस लाने के लिए। दस्तावेज़ को चलने तथा पाठ वाक्यांशों को बदलने की मार्कडिग की क्षमता का उपयोग किया जा रहा है.

```csharp

    private void ReinsertTranslatedStrings(MarkdownDocument document, string[] translatedStrings)
    {
        int index = 0;

        foreach (var node in document.Descendants())
        {
            if (node is LiteralInline literalInline && index < translatedStrings.Length)
            {
                var content = literalInline.Content.ToString();
         
                if (!IsWord(content)) continue;
                literalInline.Content = new Markdig.Helpers.StringSlice(translatedStrings[index]);
                index++;
            }
        }
    }
```

## होस्ट सेवा

यह सब चलाने के लिए मैं एक आई. डी. डी. यह सेवा एक चिह्निक फ़ाइल में पढ़ी जाती है, इसका अनुवाद कई भाषाओं में करता है और डिस्क में अनुवादित फ़ाइलों को लिखता है.

```csharp
  public async Task StartedAsync(CancellationToken cancellationToken)
    {
        if(!await blogService.IsServiceUp(cancellationToken))
        {
            logger.LogError("Translation service is not available");
            return;
        }
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = blogService.IPCount, CancellationToken = cancellationToken};
        var files = Directory.GetFiles(markdownConfig.MarkdownPath, "*.md");

        var outDir = markdownConfig.MarkdownTranslatedPath;

        var languages = translateServiceConfig.Languages;
        foreach(var language in languages)
        {
            await Parallel.ForEachAsync(files, parallelOptions, async (file,ct) =>
            {
                var fileChanged = await file.IsFileChanged(outDir);
                var outName = Path.GetFileNameWithoutExtension(file);

                var outFileName = $"{outDir}/{outName}.{language}.md";
                if (File.Exists(outFileName) && !fileChanged)
                {
                    return;
                }

                var text = await File.ReadAllTextAsync(file, cancellationToken);
                try
                {
                    logger.LogInformation("Translating {File} to {Language}", file, language);
                    var translatedMarkdown = await blogService.TranslateMarkdown(text, language, ct);
                    await File.WriteAllTextAsync(outFileName, translatedMarkdown, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error translating {File} to {Language}", file, language);
                }
            });
        }
       
```

जैसा कि आप देख सकते हैं यह भी फ़ाइल के हैश को जाँच कर सकते हैं कि यह इसका अनुवाद करने से पहले बदल गया है या नहीं. यह उन फ़ाइलों का अनुवाद करने से दूर रहने के लिए है जो बदल नहीं है.

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
});
```

मैंने होस्ट अप अप- सेंटर (कैंट- सेंटर- सेंटर- सर्विस) और मार्क इंटरनॅशनर सर्विस के लिए giolicententents.
एक होस्ट सेवा एक लंबी यात्रा सेवा है जो पृष्ठभूमि में चलता है. यह सेवाओं को रखने के लिए एक अच्छा जगह है जो पृष्ठभूमि में लगातार भाग लेने की जरूरत है या सिर्फ पूरे करने के लिए कुछ समय ले. नए HHHobed जीवन समय प्रस्ताव, पुराने IHobed प्रेस इंटरफेस की तुलना में एक बिट अधिक नरम है और हम पूरी तरह से पुराने IHobed सेवा से अधिक आसानी से कार्य करते हैं.

यहाँ पर आप देख सकते हैं कि मैं समय निर्धारित कर रहा हूँ......... पिछले 15 मिनट के लिए. यह इसलिए हो सकता है क्योंकि आसानNMT प्रतिक्रिया दिखाने में थोड़ा धीमा हो (सामान्यतः एक भाषा मॉडल का प्रयोग करते वक़्त पहला बार) । मैं भी मशीन के IP पता के लिए आधार पता स्थापित कर रहा हूँ आसानNMT सेवा चल रहा है.

## ऑन्टियम

यह एक बहुत ही सरल तरीका है जिसके लिए बहुत सी भाषाओं में एक चिह्नित फ़ाइल का अनुवाद करना है. यह सही नहीं है, लेकिन यह एक अच्छी शुरुआत है. मैं आम तौर पर इसे हर नए ब्लॉग पोस्ट के लिए चलाता हूँ और इसमें इस्तेमाल किया जाता है `MarkdownBlogService` प्रत्येक ब्लॉग पोस्ट के लिए अनुवादित नामों को खींचने के लिए.