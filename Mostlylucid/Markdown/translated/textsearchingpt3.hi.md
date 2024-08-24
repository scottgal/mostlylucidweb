# पूर्ण पाठ खोज (पीट 3 - ओपन सर्च सर्च सक्से के साथ. neck)

<!--category-- OpenSearch, ASP.NET -->
<datetime class="hidden">2024- 2424टी06: 40</datetime>

## परिचय

इस श्रृंखला के पिछले भाग में हमने पूरा पाठ खोज की धारणा शुरू की और यह कैसे एक डाटाबेस के भीतर पाठ को खोजने के लिए प्रयोग किया जा सकता है. इस भाग में हम ओपन सर्च का उपयोग करने के लिए कैसे उपयोग करेंगे. NEENT कोर के साथ.

पिछला हिस्सा:

- [पोस्ट- धर्म के साथ पूरा पाठ खोज रहा है](/blog/textsearchingpt1)
- [पोस्ट- वाक्यांशों के साथ बॉक्स खोजें](/blog/textsearchingpt11)
- [ढूंढने के लिए परिचय](/blog/textsearchingpt2)

इस भाग में हम आप के उपयोग करने के लिए कैसे कवर करेंगे नई चमकदार खोज उदाहरण का उपयोग करने के लिए ACUNT कोर के साथ।

[विषय

## सेटअप

एक बार हम खोलें खोज उदाहरण है और हम इसके साथ बातचीत शुरू कर सकते हैं। हम उपयोग हो जाएगा [खोज क्लाएंट खोलें](https://opensearch.org/docs/latest/clients/OSC-dot-net/) .नेट के लिए.
पहले हम अपने सेटअप विस्तार में ग्राहक सेट

```csharp
    var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));
```

यह अंत बिन्दु तथा महत्व के साथ क्लाएंट को सेट करता है. हम डिबग मोड भी सक्षम करें ताकि हम देख सकें कि क्या चल रहा है. इसके अलावा हम RALS SSL प्रमाणपत्रों का उपयोग नहीं कर रहे हैं हम प्रमाणपत्र वैधीकरण (यह उत्पादन में नहीं है).

## डाटा सूचीबद्ध किया जा रहा है

खोलें खोज में मुख्य संकल्प निर्देशिका है. एक डाटाबेस तालिका की तरह निर्देशिका के बारे में सोचो, यह है जहां आपके सभी डाटा जमा है.

ऐसा करने के लिए हम इस्तेमाल करेंगे [खोज क्लाएंट खोलें](https://opensearch.org/docs/latest/clients/OSC-dot-net/) .नेट के लिए. आप इसे नुतो के द्वारा संस्थापित कर सकते हैं:

आप वहाँ दो वहाँ पर ध्यान देंगे - खोलें.Net और ढूंढना.CLICANent. पहला स्तर कनेक्शन प्रबंधन की तरह कम स्तर सामग्री है, दूसरा स्तर सामग्री के रूप में इंडेक्सिंग और खोज के रूप में उच्च स्तर सामग्री है.

अब कि हम यह संस्थापित किया है हम सूची डाटा को देख शुरू कर सकते हैं.

एक निर्देशिका बनाया जा रहा है अर्ध-पूर्व आगे. आप सिर्फ परिभाषित करते हैं कि आपकी निर्देशिका की तरह दिखने और फिर इसे बनाने के लिए क्या करना चाहिए।
नीचे दिए गए कोड में हम 'मैप' निर्देशिका मॉडल ( ब्लॉग के डाटाबेस मॉडल का सरल संस्करण) देख सकते हैं.
हर क्षेत्र के लिए इस मॉडल के लिए हम तब परिभाषित करते हैं कि यह किस प्रकार है (text, तिथि, आदि) और क्या हिसाब करने के लिए.

टाइप करना महत्वपूर्ण है क्योंकि यह परिभाषित करता है कि डेटा किस प्रकार भंडारित है और कैसे इसे खोजा जा सकता है. उदाहरण के लिए, एक 'text' क्षेत्र विश्लेषण व सूचित किया जाता है, एक'sub' क्षेत्र नहीं है. तो आप एक कीवर्ड क्षेत्र के लिए खोज करने की उम्मीद करेंगे ठीक जैसे यह जमा है, लेकिन एक पाठ क्षेत्र आप पाठ के भागों के लिए खोज कर सकते हैं।

यहाँ भी वर्ग वास्तव में एक स्ट्रिंग है[] लेकिन बीजशब्द प्रकार उन्हें सही तरीके से संभालने के लिए कैसे पता है.

```csharp
   public async Task CreateIndex(string language)
    {
        var languageName = language.ConvertCodeToLanguageName();
        var indexName = GetBlogIndexName(language);

      var response =  await client.Indices.CreateAsync(indexName, c => c
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(1)
            )
            .Map<BlogIndexModel>(m => m
                .Properties(p => p
                    .Text(t => t
                        .Name(n => n.Title)
                        .Analyzer(languageName)
                    )
                    .Text(t => t
                        .Name(n => n.Content)
                        .Analyzer(languageName)
                    )
                    .Text(t => t
                        .Name(n => n.Language)
                    )
                    .Date(t => t
                        .Name(n => n.LastUpdated)
                    )
                    .Date(t => t
                        .Name(n => n.Published)
                    )
                    .Date(t => t
                        .Name(n => n.LastUpdated)
                    )
                    .Keyword(t => t
                        .Name(n => n.Id)
                    )
                    .Keyword(t=>t
                        .Name(n=>n.Slug)
                    )
                    .Keyword(t=>t
                        .Name(n=>n.Hash)
                    )
                    .Keyword(t => t
                        .Name(n => n.Categories)
                    )
                )
            )
        );
        
        if (!response.IsValid)
        {
           logger.LogError("Failed to create index {IndexName}: {Error}", indexName, response.DebugInformation);
        }
    }
```

## निर्देशिका में वस्तुओं को जोड़ा जा रहा है

एक बार जब हमारे पास हमारी निर्देशिका सेट वस्तुओं को जोड़ने के लिए सेट है हम इस सूची में वस्तुओं को जोड़ने की जरूरत है. हम एक बड़े प्रवेश विधि का उपयोग कर रहे हैं के रूप में यहाँ हम एक BUNCH जोड़ रहे हैं.

आप देख सकते हैं कि शुरू - शुरू में हम एक विधि में कॉल करते हैं`GetExistingPosts` जो सभी पोस्टों को बताता है कि पहले से निर्देशिका में हैं. तब हम भाषा द्वारा पोस्टों को समूहबद्ध करें और 'k' भाषा बाहर फ़िल्टर करें (के रूप में हम निर्देशिका के रूप में नहीं करना चाहते हैं के रूप में यह एक अतिरिक्त प्लगइन की जरूरत नहीं है के रूप में हम अभी तक नहीं है). फिर हम उन पोस्टों को फ़िल्टर करते हैं जो पहले से सूची में हैं.
हम हैश तथा आईडी का उपयोग करें यदि एक पोस्ट पहले से ही निर्देशिका में है.

```csharp
    public async Task AddPostsToIndex(IEnumerable<BlogIndexModel> posts)
    {
        var existingPosts = await GetExistingPosts();
        var langPosts = posts.GroupBy(p => p.Language);
        langPosts=langPosts.Where(p => p.Key!="uk");
        langPosts = langPosts.Where(p =>
            p.Any(post => !existingPosts.Any(existing => existing.Id == post.Id && existing.Hash == post.Hash)));
        
        foreach (var blogIndexModels in langPosts)
        {
            
            var language = blogIndexModels.Key;
            var indexName = GetBlogIndexName(language);
            if(!await IndexExists(language))
            {
                await CreateIndex(language);
            }
            
            var bulkRequest = new BulkRequest(indexName)
            {
                Operations = new BulkOperationsCollection<IBulkOperation>(blogIndexModels.ToList()
                    .Select(p => new BulkIndexOperation<BlogIndexModel>(p))
                    .ToList()),
                Refresh = Refresh.True,
                ErrorTrace = true,
                RequestConfiguration = new RequestConfiguration
                {
                    MaxRetries = 3
                }
            };

            var bulkResponse = await client.BulkAsync(bulkRequest);
            if (!bulkResponse.IsValid)
            {
                logger.LogError("Failed to add posts to index {IndexName}: {Error}", indexName, bulkResponse.DebugInformation);
            }
            
        }
    }
```

एक बार जब हम मौजूदा पोस्टों को फिल्टर कर चुके हैं और हमारी गुमता हम एक नई निर्देशिका (नाम पर आधारित) बनाने हैं, मेरे मामले में "बहुत से अधिक से अधिक नवीकृत वेब- पृष्ठों पर"<language>") और फिर एक बड़ी मांग बना। यह बड़ी बिनती सूची पर कार्यान्वित करने के लिए क्रिया का संग्रह है.
यह प्रत्येक वस्तु को एक - एक करके जोड़ने से ज़्यादा कुशल है ।

तुम देखोगे कि में `BulkRequest` हम सेट `Refresh` गुण `true`___ इसका अर्थ है कि बड़ी मात्रा में प्रविष्ट होने के बाद सूची को ताज़ा किया जाता है । यह सच में आवश्यक नहीं है लेकिन यह डिबगिंग के लिए उपयोगी है.

## निर्देशिका ढूंढा जा रहा है

यह देखने के लिए एक अच्छा तरीका है कि वास्तव में यहाँ बनाया गया है क्या वास्तव में बनाया गया है देखने के लिए...... डीवबोर्ड्स पर जाने के लिए और एक खोज क्वेरी पर।

```json
GET /mostlylucid-blog-*
{}
```

यह प्रश्न हमें सभी निर्देशिकाओं को लौटा देगा जो पैटर्न से मेल खाते हैं `mostlylucid-blog-*`___ (अब तक हमारी सभी इंडेक्स्स.

```json
{
  "mostlylucid-blog-ar": {
    "aliases": {},
    "mappings": {
      "properties": {
        "categories": {
          "type": "keyword"
        },
        "content": {
          "type": "text",
          "analyzer": "arabic"
        },
        "hash": {
          "type": "keyword"
        },
        "id": {
          "type": "keyword"
        },
        "language": {
          "type": "text"
        },
        "lastUpdated": {
          "type": "date"
        },
        "published": {
          "type": "date"
        },
        "slug": {
          "type": "keyword"
        },
        "title": {
          "type": "text",
          "analyzer": "arabic"
        }
      }
    },
    "settings": {
      "index": {
        "replication": {
          "type": "DOCUMENT"
..MANY MORE
```

savs खोलने में औज़ार एक महान तरीका है अपनी जांच करने के लिए आप पहले उन्हें अपने कोड में डाल दिया।

![डेव औज़ार](devtools.png?width=900&quality=25)

## निर्देशिका ढूंढा जा रहा है

अब हम निर्देशिका खोज शुरू कर सकते हैं. हम इसका इस्तेमाल कर सकते हैं `Search` यह करने के लिए ग्राहक पर विधि.
यह है कि खोलने की असली शक्ति अंदर आती है। इसका शाब्दिक रूप से है [क्वैरी के दर्जनों भिन्न प्रकार](https://opensearch.org/docs/latest/query-dsl/) आप अपने डाटा को खोजने के लिए उपयोग कर सकते हैं. एक सरल बीजशब्द से सब कुछ एक जटिल 'नील' खोज के लिए.

```csharp
    public async Task<List<BlogIndexModel>> GetSearchResults(string language, string query, int page = 1, int pageSize = 10)
    {
        var indexName = GetBlogIndexName(language);
        var searchResponse = await client.SearchAsync<BlogIndexModel>(s => s
                .Index(indexName)  // Match index pattern
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .MultiMatch(mm => mm
                                .Query(query)
                                .Fields(f => f
                                    .Field(p => p.Title, boost: 2.0) 
                                    .Field(p => p.Categories, boost: 1.5) 
                                    .Field(p => p.Content)
                                )
                                .Type(TextQueryType.BestFields)
                                .Fuzziness(Fuzziness.Auto)
                            )
                        )
                    )
                )
                .Skip((page -1) * pageSize)  // Skip the first n results (adjust as needed)
                .Size(pageSize)  // Limit the number of results (adjust as needed)
        );

        if(!searchResponse.IsValid)
        {
            logger.LogError("Failed to search index {IndexName}: {Error}", indexName, searchResponse.DebugInformation);
            return new List<BlogIndexModel>();
        }
        return searchResponse.Documents.ToList();
    }

```

### प्रश्न विवरण

इस विधि, `GetSearchResults`ब्लॉग पोस्ट को प्राप्त करने के लिए विशिष्ट ओपन सर्च इंडेक्स को क्वैरी करने के लिए बनाया गया है. यह तीन पैरामीटर्स लेता है: `language`, `query`, और पारिभाषन पैरामीटर `page` और `pageSize`___ यहाँ यह क्या करता है:

1. **निर्देशिका चयन**:
   
   - इससे निर्देशिका नाम प्राप्त होता है `GetBlogIndexName` दिए गए भाषा पर आधारित विधि । निर्देशिका भाषा के अनुसार गतिशील रूप से चयनित है.

2. **खोज प्रश्न**:
   
   - क्वैरी एक का उपयोग करता है `Bool` क्वैरी एक के साथ `Must` सुनिश्चित करने के लिए सुनिश्चित करें कि कुछ मापदण्डों से मेल खाता है.
   - भीतर `Must` अंतराल, एक `MultiMatch` अनेक क्षेत्रों में ढूंढने के लिए क्वेरी का प्रयोग किया जाता है (या जाता है)`Title`, `Categories`, और `Content`).
     - **बूलिंग**: `Title` क्षेत्र दिया गया है `2.0`, खोज में इसे अधिक महत्वपूर्ण बनाने, और `Categories` इसका बड़ा प्रभाव है `1.5`___ इसका अर्थ है कि जहाँ सर्च क्वैरी शीर्षक या वर्गों में प्रकट होता है वहाँ दस्तावेज़ों का अर्थ है.
     - **क्वैरी क़िस्म**: इसका इस्तेमाल `BestFields`क्वैरी के लिए सबसे अच्छा मेल खाने वाले फील्ड को ढूंढने का प्रयास करें.
     - **छायादारनेस**: `Fuzziness.Auto` पैरामीटर लगभग मैच के लिए अनुमति देता है (उदा. play. g.)

3. **पैसिवेशन**:
   
   - वह `Skip` पहला चरण भटकता है `n` पृष्ठ संख्या के आधार पर परिणाम देता है, हिसाब के रूप में गणना करता है `(page - 1) * pageSize`___ यह परिष्कृत परिणामों से निपटने में मदद करता है ।
   - वह `Size` विधि सीमा निर्धारित किए गए दस्तावेज़ों की संख्या को लौटाता है `pageSize`.

4. **सिंकिंग में त्रुटि**:
   
   - यदि क्वैरी असफल तो, एक त्रुटि लॉग और एक खाली सूची लौटाया गया है.

5. **परिणाम**:
   
   - विधि का परिणाम होगा सूची `BlogIndexModel` दस्तावेज़ खोज मापदण्ड से मेल खाते हैं.

तो आप देख सकते हैं कि हम अपने डेटा की खोज कैसे के बारे में सुपरप्रयोग किया जा सकता है। हम विशेष क्षेत्रों की खोज कर सकते हैं, हम कुछ क्षेत्रों को बढ़ा सकते हैं, और हम अनेक इंडेक्सों पर भी खोज कर सकते हैं ।

एक श्रेष्ठ लाभ है आसान शब्द जो हम अनेक भाषाओं का समर्थन कर सकते हैं. हम प्रत्येक भाषा के लिए एक अलग निर्देशिका है और उस सूची के भीतर खोज सक्षम कर सकते हैं. इसका मतलब है कि हम हर भाषा के लिए सही शब्द इस्तेमाल कर सकते हैं और अच्छे नतीजे पा सकते हैं ।

## नया खोज पट्टी

इस श्रृंखला के पिछले भाग में जो खोज हमने देखी उसके विपरीत, हम खोलें खोज के प्रयोग से खोज प्रक्रिया को पूरी तरह सरल कर सकते हैं । हम सिर्फ इस प्रश्न के लिए पाठ में फेंक सकते हैं और बड़े परिणाम वापस प्राप्त कर सकते हैं.

```csharp
   [HttpGet]
    [Route("osearch/{query}")]
   [ValidateAntiForgeryToken]
    public async Task<JsonHttpResult<List<SearchResults>>> OpenSearch(string query, string language = MarkdownBaseService.EnglishLanguage)
    {
        var results = await indexService.GetSearchResults(language, query);
        
        var host = Request.Host.Value;
        var output = results.Select(x => new SearchResults(x.Title.Trim(), x.Slug, @Url.ActionLink("Show", "Blog", new{ x.Slug}, protocol:"https", host:host) )).ToList();
        return TypedResults.Json(output);
    }
```

जैसा कि आप देख सकते हैं हम सभी डेटा है हम परिणाम वापस करने के लिए निर्देशिका में जरूरत है. तब हम इस का उपयोग ब्लॉग पोस्ट में यूआरएल तैयार करने के लिए कर सकते हैं. यह हमारे डाटाबेस से लोड लेता है और खोज प्रक्रिया अधिक तेजी से बनाता है.

## ऑन्टियम

इस पोस्ट में हमने देखा कि कैसे एक C# ग्राहक हमारे खुले खोज उदाहरण के साथ बातचीत करने के लिए।