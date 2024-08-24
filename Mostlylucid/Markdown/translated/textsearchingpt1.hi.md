# पूरा पाठ ढूंढा जा रहा है (टटी 1)

<!--category-- Postgres, Entity Framework -->
<datetime class="hidden">2024- 0. 1420टी2: 40</datetime>

# परिचय

सामग्री के लिए खोज किसी भी भारी वेबसाइट का महत्वपूर्ण हिस्सा है. यह गुण और उपयोगकर्ता अनुभव को बढ़ाता है. इस पोस्ट में मैं कवर होगा कि कैसे मैंने इस साइट के लिए पूरा पाठ खोज जोड़ा

इस क्रम में अगले भाग में:

- [पोस्ट- वाक्यांशों के साथ बॉक्स खोजें](/blog/textsearchingpt11)
- [ढूंढने के लिए परिचय](/blog/textsearchingpt2)
- [सी# के साथ खोज खोलें](/blog/textsearchingpt3)

[विषय

# आने वाले

पूर्ण पाठ खोजने के लिए कई तरीके हैं

1. स्मृति डाटा संरचना में सिर्फ खोज रहे हैं (जैसे सूची, यह लागू करने के लिए सरल है लेकिन अच्छी तरह से स्केल नहीं करता है). इसके अलावा यह कई काम के बिना जटिल कब्रों का समर्थन नहीं करता.
2. एसक्यूएल सर्वर या पोस्ट- व्यू जैसे डाटाबेस का उपयोग किया जा रहा है. जबकि यह काम करता है और लगभग सभी डाटाबेस प्रकार से समर्थन है...... यह हमेशा अधिक जटिल डेटा या जटिल इमारतों के लिए सबसे अच्छा समाधान नहीं है, हालांकि यह क्या इस लेख को कवर करेगा.
3. जटिल खोज तकनीक की तरह इस्तेमाल किया जा रहा है [लुक- लाइन](https://lucenenet.apache.org/) या॰ सीन॰ FTS. यह दो ऊपर के समाधानों के बीच एक मध्य भूमि है. यह सिर्फ एक सूची खोजने से अधिक जटिल है लेकिन पूर्ण डाटाबेस समाधान से कम जटिल है. हालांकि, यह अभी भी लागू करने के लिए बहुत जटिल है (सामान्य रूप से डेटा के लिए) और साथ ही एक पूर्ण खोज समाधान. वास्तव में कई अन्य खोज तकनीकों में [हुड के नीचे लूला का प्रयोग करें ](https://www.elastic.co/search-labs/blog/elasticsearch-lucene-vector-database-gains) यह अद्भुत वेक्टर खोज क्षमता है.
4. एल. आई. वी.) यह सबसे जटिल संसाधन है ठोस समाधान, लेकिन सबसे शक्तिशाली भी. यह भी सबसे कठिन है और आसान के साथ जटिल कब्र संभाल सकते हैं. मैं अगले सप्ताह गहराई में जाना होगा या कैसे स्व- होस्ट के लिए, कॉन्फ़िगर करें और सी# से खोलें खोज का उपयोग करें.

# डाटाबेस पूरा पाठ पोस्ट- आरों के साथ ढूंढा जा रहा है

इस ब्लॉग में मैं हाल ही में अपने डाटाबेस के लिए पोस्ट आइग्रियों का उपयोग करने के लिए प्रेरित हुआ है. पोस्टर को एक संपूर्ण पाठ खोज विशेषता है जो बहुत शक्‍तिशाली है और (कुछ आसान) प्रयोग करने के लिए बहुत ही आसान है । यह भी बहुत तेजी से है और आराम के साथ जटिल आराम के साथ संभाल सकते हैं.

निर्माण करते समय `DbContext` आप उल्लेखित कर सकते हैं कि कौन से क्षेत्र में पूर्ण पाठ खोज क्रिया सक्षम है.

पोस्टग्रेस तेजी से प्राप्त करने के लिए खोज सदिशों की धारणा का प्रयोग करता है, कुशल पूर्ण पाठ खोज में. खोज वेक्टर एक डाटा स्ट्रक्चर है जिसमें दस्तावेज़ तथा उनके पदों में शब्द हैं. डाटाबेस में प्रत्येक पंक्ति के लिए खोज सदिश को आवश्यक रूप से अलग करने से पोस्ट- व्यू को दस्तावेज़ में बहुत जल्दी शब्दों को खोजने देता है.
यह इस प्राप्त करने के लिए दो विशेष डाटा क़िस्म उपयोग करता है:

- TSGRE: एक विशेष एसक्यूएल डाटा क़िस्म जो लेक्सस्‌ की एक सूची जमा करता है (कौटे के एक सदिश के रूप में इसके बारे में सोचा जाता है). यह दस्तावेज़ का सूचीबद्ध संस्करण है जो तेजी से खोज करने के लिए प्रयुक्त है.
- TSQuery: एक और विशेष डाटा क़िस्म जो सर्च क्वैरी को भंडारित करता है, जिसमें सर्च शर्तों और तार्किक ऑपरेटर (जैसे, या, नहीं) शामिल हैं.

इसके अतिरिक्‍त, यह एक ऊँचे पद प्रदान करता है जो आपको उन परिणामों को निर्धारित करने देता है जो वे खोज क्वेरी से मेल खाते हैं । यह बहुत ही शक्‍तिशाली है और आपको इसके परिणाम को ठीक करने की अनुमति देता है ।
उपयोग किए गए परिणामों के लिए केआईओस्लेव एक सूचक प्रदान करता है. एक - दूसरे की खोज के लिए जो शब्द इस्तेमाल किया जाता है, उसका हिसाब लगाया जाता है और वे कितनी बार दस्तावेज़ में आते हैं ।
प्रतिबन्ध या Tr_ Case_und फंक्शन इस छोटा की गणना करने के लिए प्रयोग किया जाता है.

आप पोस्टग्रेस की संपूर्ण पाठ खोज विशेषता के बारे में अधिक पढ़ सकते हैं [यहाँ](https://www.postgresql.org/docs/current/textsearch.html)

## एंटिटी फ्रेमवर्क

पोस्टग्रेस एंटिटी फ्रेमवर्क पैकेज [यहाँ](https://www.npgsql.org/efcore/mapping/full-text-search.html?tabs=pg12%2Cv5) पूर्ण पाठ खोजने के लिए शक्‍तिशाली समर्थन देता है । यह आपको निर्धारित करने देता है कि कौन से क्षेत्र पूरे पाठ सूचीबद्ध हैं तथा उन्हें कैसे क्वैरी करें.

ऐसा करने के लिए हम विशिष्ट निर्देशिका क़िस्मों को जोड़ने के लिए जैसा कि इन में पारिभाषित है `DbContext`:

```csharp
   modelBuilder.Entity<BlogPostEntity>(entity =>
        {
            entity.HasIndex(x => new { x.Slug, x.LanguageId });
            entity.HasIndex(x => x.ContentHash).IsUnique();
            entity.HasIndex(x => x.PublishedDate);

                entity.HasIndex(b => new { b.Title, b.PlainTextContent})
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("english");
  ...
```

यहाँ हम एक पूरा पाठ निर्देशिका जोड़ रहे हैं `Title` और `PlainTextContent` हमारे खेत `BlogPostEntity`___ हम भी निर्दिष्ट कर रहे हैं कि निर्देशिका का उपयोग करना चाहिए `GIN` निर्देशिका क़िस्म और `english` भाषा. यह इतना महत्त्वपूर्ण है कि यह पोस्ट मुख्‌स को बताता है कि कैसे डेटा और किस भाषा में रचना करने और शब्दों को बंद करने के लिए प्रयोग किया जाता है ।

यह स्पष्ट है कि हमारे ब्लॉग के लिए एक मुद्दा है हमारे पास बहुत सी भाषाओं है। दुर्भाग्य से अब मैं सिर्फ उपयोग कर रहा हूँ `english` सभी पोस्टों के लिए भाषा. यह कुछ मैं भविष्य में पता करने की जरूरत होगी लेकिन अब यह काफी अच्छी तरह काम करता है.

हम भी हमारे लिए एक सूची जोड़ `Category` एंटिटी:

```csharp
     modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasIndex(b => b.Name).HasMethod("GIN").IsTsVectorExpressionIndex("english");;
...

```

इस पोस्ट युक्तियों को करने से इस डाटाबेस में प्रत्येक पंक्ति के लिए खोज सदिश उत्पन्न करता है. इस सदिश में शब्द हैं `Title` और `PlainTextContent` क्षेत्र. तब हम इस सदिश का उपयोग दस्तावेज़ में शब्दों को ढूंढने के लिए कर सकते हैं.

यह एसक्यूएल में_टाइजर फ़ंक्शन के लिए अनुवाद करता है जो पंक्ति के लिए सर्च सदिश बनाता है. तब हम Tss_ प्रक्षेपन समारोह का उपयोग कर सकते हैं जो परिणाम के आधार पर होता है.

```postgresql
SELECT to_tsvector('english', 'a fat  cat sat on a mat - it ate a fat rats');
to_tsvector
-----------------------------------------------------
'ate':9 'cat':3 'fat':2,11 'mat':7 'rat':12 'sat':4
```

यह हमारे डाटाबेस के लिए एक उत्प्रवासन के रूप में लागू करें और हम खोजने के लिए तैयार हैं.

# खोज रहा है

## टी. वी.

खोज करने के लिए हम इस्तेमाल करेंगे `EF.Functions.ToTsVector` और `EF.Functions.WebSearchToTsQuery` खोज सदिश तथा क्वैरी बनाने के लिए फंक्शन. तब हम इस्तेमाल कर सकते हैं `Matches` खोज सदिश में क्वैरी को ढूंढने के लिए फंक्शन.

```csharp
  var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Matches(EF.Functions.WebSearchToTsQuery("english", query)) // Search in title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", query))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Rank(EF.Functions.WebSearchToTsQuery("english", query))) // Rank by relevance
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
       
```

ईएफ.FEND. वेब सर्चQuery फ़ंक्शन सामान्य वेब सर्च इंजिन पर आधारित पंक्ति के लिए क्वैरी बनाता है.

```postgresql
SELECT websearch_to_tsquery('english', '"sad cat" or "fat rat"');
       websearch_to_tsquery
-----------------------------------
 'sad' <-> 'cat' | 'fat' <-> 'rat'
```

इस उदाहरण में, आप देख सकते हैं कि यह एक क्वैरी बनाता है जो दस्तावेज़ में शब्दों के लिए खोज करता है "एली बिल्ली" या "फाइट"। यह एक शक्‍तिशाली विशेषता है जो हमें सहज - बुद्धि के साथ जटिल खोज करने की अनुमति देती है ।

जैसा कहा गया है, इन तरीकों से खोज वेक्टर तथा क्वैरी दोनों पंक्ति के लिए खोज सदिश तथा क्वैरी तैयार होते हैं. तब हम इस्तेमाल करते हैं `Matches` खोज सदिश में क्वैरी को ढूंढने के लिए फंक्शन. हम भी इसका इस्तेमाल कर सकते हैं `Rank` परिणाम को दृढ़तापूर्वक रैंकित करने के लिए फ़ंक्शन.

जैसा कि आप देख सकते हैं यह एक सरल प्रश्न नहीं है लेकिन यह बहुत शक्तिशाली है और हमें अंदर शब्दों की खोज करने की अनुमति देता है `Title`, `PlainTextContent` और `Category` हमारे खेत `BlogPostEntity` और उनकी क़सम जो (आसमान ज़मीन के दरमियान) पैरते फिरते हैं

## वेबपीआई

इन (भविष्य में) का उपयोग करने के लिए हम एक सरल वेब-विधक अंत बिन्दु बना सकते हैं जो एक प्रश्न लेता है और परिणाम लौटाता है । यह एक सादा नियंत्रक है जो क्वैरी करता है और परिणाम बताता है:

```csharp
[ApiController]
[Route("api/[controller]")]
public class SearchApi(MostlylucidDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<JsonHttpResult<List<SearchResults>>> Search(string query)
    {;

        var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Matches(EF.Functions.WebSearchToTsQuery("english", query)) // Search in title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", query))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Rank(EF.Functions.WebSearchToTsQuery("english", query))) // Rank by relevance
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
        
        var output = posts.Select(x => new SearchResults(x.Title.Trim(), x.Slug)).ToList();
        
        return TypedResults.Json(output);
    }

```

## ्ड स्तम्भ तथा टाइप Aven तैयार करें

इन'तुच्छ' Tseviceeices का उपयोग करने के लिए एक वैकल्पिक तरीका है खोज सदिश को जमा करने के लिए एक उत्पन्न स्तंभ का उपयोग करने के लिए और फिर इस तरह खोज करने के लिए इसका उपयोग करें. यह एक और जटिल दृष्टिकोण है लेकिन बेहतर प्रदर्शन की अनुमति देता है ।
यहाँ हम अपने रूपांतरण `BlogPostEntity` विशेष स्तम्भ जोड़ने के लिए:

```csharp
   [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public NpgsqlTsVector SearchVector { get; set; }
```

यह एक गणनात्मक स्तम्भ है जो पंक्ति के लिए खोज सदिश उत्पन्न करता है. तब हम इस स्तम्भ का उपयोग दस्तावेज़ में शब्दों को ढूंढने के लिए कर सकते हैं.

फिर हम इस सूची को अपनी एंटिटी परिभाषा के अंदर स्थापित करते हैं (अभी पुष्टि करने के लिए लेकिन यह हमें हर पोस्ट के लिए भाषा स्तम्भ निर्दिष्ट करने के लिए एक भाषा स्तम्भ को निर्दिष्ट करने के लिए भी कई भाषाओं को रखने की अनुमति दे सकता है.

```csharp
   entity.Property(b => b.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))", stored: true);
```

आप यहाँ देखेंगे कि हम इस्तेमाल करते हैं `HasComputedColumnSql` सर्च सदिश तैयार करने के लिए पोस्ट- एसक्यूएल फ़ंक्शन को सुस्पष्ट रूप से उल्लेखित करें. हम यह भी निर्दिष्ट करते हैं कि स्तम्भ डाटाबेस में भंडारित है. यह महत्वपूर्ण है जैसे यह पोस्टग्रेस को डाटाबेस में खोज सदिश को भंडारित करने के लिए बताता है. यह हमें खोज सदिश के उपयोग से दस्तावेज़ में शब्दों के लिए खोज करने देता है.

डाटाबेस में यह प्रत्येक पंक्ति के लिए तैयार किया गया है, जो कि दस्तावेज़ तथा उनके पदों में 'xeeepes' हैं:

```csharp
"'1992':464 '1996':468 '20':480 '200':115 '2007':426 '2009':428 '2012':88 '2015':397 '2018':370 '2020':372 '2021':288,327,329,399 '2022':196,243,245,290 '2024':156,158,198 '25':21,477,486,522 '3d':346 '6':203,256 '8':179,485 '90':120,566 'ab':282 'access':221 'accomplish':14 'achiev':118 'across':60 'adapt':579 'advanc':134 'applic':168,316,526 'apr':155,197 'architect':83,97,159 'architectur':307,337 ...
```

### खोज का तरीका

तब हम इस स्तम्भ का उपयोग दस्तावेज़ में शब्दों को ढूंढने के लिए कर सकते हैं. हम इसका इस्तेमाल कर सकते हैं `Matches` खोज सदिश में क्वैरी को ढूंढने के लिए फंक्शन. हम भी इसका इस्तेमाल कर सकते हैं `Rank` परिणाम को दृढ़तापूर्वक रैंकित करने के लिए फ़ंक्शन.

```csharp
       var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                // Search using the precomputed SearchVector
                x.SearchVector.Matches(EF.Functions.ToTsQuery("english", query + ":*")) // Use precomputed SearchVector for title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                // Rank based on the precomputed SearchVector
                x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*"))) // Use precomputed SearchVector for ranking
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
```

आप यहाँ देख सकते हैं कि हम एक अलग प्रश्नर का उपयोग भी करते हैं `EF.Functions.ToTsQuery("english", query + ":*")`  जो हमें एक टाइप BAS प्रकार कार्य पेश करने की अनुमति देता है (जहाँ हम टाइप कर सकते हैं). 'बैट' और 'काब, 'बक', 'काप्टर' मिलता है.

इसके अतिरिक्त यह हमें मुख्य ब्लॉग पोस्ट क्वैरी को सरल बनाने देता है सिर्फ क्वैरी को ढूंढने के लिए `SearchVector` स्तंभ। यह एक शक्‍तिशाली गुण है जो हमें शब्दों की खोज करने की अनुमति देता है `Title`, `PlainTextContent`___ हम अभी भी ऊपर दिखाया गया निर्देशिका का उपयोग करें `CategoryEntity`.

```csharp
x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) 
```

तब हम इस्तेमाल करते हैं `Rank` क्वैरी पर आधारित परिणाम बताता है.

```csharp
 x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*")))
```

यह हमें अंत - बिन्दु का उपयोग करने देता है, जहाँ हम एक शब्द के पहले कुछ पत्रों में जा सकते हैं और उन सभी पोस्टों को वापस ले सकते हैं जो शब्द से मेल खाते हैं:

आप देख सकते हैं [यहाँ क्रिया में एपीआई](https://www.mostlylucid.net/swagger/index.html) के लिए देखें `/api/SearchApi`___ (नहीं; मैं इस साइट के लिए Swawowing सक्षम किया है इसलिए आप कार्रवाई में एपीआई देख सकते हैं लेकिन ज़्यादातर समय यह 'Wetion) के लिए रखा जाना चाहिए.

![एपीआई](searchapi.png?width=900&format=webp&quality=50)

भविष्य में मैं इस कार्य का उपयोग इस साइट पर खोज बक्से के लिए एक प्रकार का सिर विशेषता जोड़ूंगा.

# ऑन्टियम

आप देख सकते हैं कि यह Trids और एंटिटी फ्रेमवर्क के प्रयोग से शक्तिशाली खोज प्रकार्य पाने के लिए संभव है। लेकिन इसमें पेचीदा और सीमित सीमाएँ हैं, जैसे कि भाषा के लिए हमें ध्यान देना ज़रूरी है । अगले भाग में मैं यह कवर होगा कैसे हम खोलें खोज - जो एक अधिक सेटअप है लेकिन अधिक शक्तिशाली है और अधिक शक्तिशाली है.