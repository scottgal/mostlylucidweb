# البحث عن نص كامل (Pt 3 - فتح النظام مع ASP.net Cor)

<!--category-- OpenSearch, ASP.NET -->
<datetime class="hidden">2024-0224-08-02-04-TT06:40</datetime>

## أولاً

وفي الأجزاء السابقة من هذه السلسلة أدخلنا مفهوم البحث الكامل عن النص وكيف يمكن استخدامه للبحث عن النص داخل قاعدة بيانات. في هذا الجزء سوف نقدم كيفية استخدام OpenSearch مع ASP.net conr.

الأجزاء السابقة:

- [جاري البحث مع الموضعات](/blog/textsearchingpt1)
- [مربع البحث مع الموضع](/blog/textsearchingpt11)
- [مقدمة إلى OpenSearch](/blog/textsearchingpt3)

في هذا الجزء سوف نقوم بتغطية كيفية البدء في استخدام لكم جديدة لامعة OpenSearch حالة مع ASP.net الأساسية.

[رابعاً -

## إنشاء

بمجرد أن يكون لدينا حالة OpenSearch حتى وتشغيل يمكننا أن نبدأ للتفاعل معها. نحن سَنَكُونُ سَنَستعملُ [](https://opensearch.org/docs/latest/clients/OSC-dot-net/) عــن الميزانيــة
أولاً لقد وضعنا العميل في تمديدنا للتجهيزات

```csharp
    var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));
```

هذا يُحدّدُ الزبونَ مَع نقطةِ النهايةِ ووثائقِ التفويض. نحن أيضاً نمكّن من معالجة نمط التنقيط حتى نتمكن من رؤية ما يحدث. وعلاوة على ذلك، فإننا لا نستخدم شهادات SSL REAL نحن تعطيل شهادة التحقق (لا تفعل هذا في الإنتاج).

## 

والمفهوم الأساسي في النظام المفتوح هو المؤشر. فكّر في مؤشر مثل جدول قاعدة بيانات، حيث يتم تخزين كل بياناتك.

إلى أن نفعل هذا نحن سَنَستعملُ [](https://opensearch.org/docs/latest/clients/OSC-dot-net/) عــن الميزانيــة أنت قادر على تثبيت هذا

ستلاحظون أن هناك اثنين هناك - OpenSearch.net و Opensearch.Client. الأولى هي الأشياء ذات المستوى المنخفض مثل إدارة الاتصال، والثانية هي الأشياء ذات المستوى العالي مثل الفهرسة والبحث.

الآن بعد أن قمنا بتثبيتها يمكننا البدء بالبحث في بيانات الفهرسة.

إنشاء مؤشر هو شبه واضح إلى الأمام. أنت فقط تعرّف ما يجب أن يبدو عليه مؤشرك ومن ثمّ إبتكره.
في الرمز أدناه يمكنك أن ترى أننا 'خريطة' نموذج مؤشرنا (نسخة مبسطة من نموذج قاعدة بيانات المدونة).
لكل حقل من هذا النموذج نحدد بعد ذلك ما هو نوعه (النص، التاريخ، كلمة المفتاح، الخ) وما هو المحلل الذي سيستخدمه.

والنوع مهم لأنه يحدد كيفية تخزين البيانات وكيفية تفتيشها. فعلى سبيل المثال، يجري تحليل حقل 'النص` ورمزه، أما حقل 'الكلمة المفتاحية` فليس كذلك. لذا تتوقع أن تبحث عن حقل كلمة مفتاحية بالضبط كما هو مخزن، لكن حقل نصي يمكنك البحث عنه لأجزاء من النص.

إلى هنا إلى هنا[ولكن نوع الكلمة الرئيسية يفهم كيفية التعامل معها بشكل صحيح.

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

## إضافة بنود إلى فهرس

وبمجرد أن نضع فهرسنا لإضافة بنود إليه، نحتاج إلى إضافة عناصر إلى هذا المؤشر. هنا كما نقوم بإضافة BUNCH نحن نستخدم طريقة إدخال الجملة.

يمكنك أن ترى أننا في البداية ندعو إلى طريقة تدعى`GetExistingPosts` التي ترجع جميع الوظائف الموجودة بالفعل في الفهرس. ثم نجمع الوظائف حسب اللغة ونصفي لغة "أوك" (بما أننا لا نريد أن نرهنها كما أنها تحتاج إلى ملحق إضافي لا نملكه بعد). ثم نرشح اي وظائف موجودة بالفعل في الفهرس
نستخدم الهاش والهيد لتحديد ما إذا كانت الوظيفة موجودة بالفعل في الفهرس.

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

بمجرد أن نرشح الوظائف الموجودة و المحلل المفقود ننشئ فهرساً جديداً (على أساس الاسم، في حالتي، "معظمها لوكسيد-blog-<language>") ثم تُنشئ طلباً شاملاً. وهذا الطلب الشامل هو عبارة عن مجموعة من العمليات التي يتعين القيام بها على أساس المؤشر.
وهذا أكثر كفاءة من إضافة كل بند الواحد تلو الآخر.

سترى ذلك في `BulkRequest` نحن نحدد `Refresh` التي تحمل `true`/ / / / وهذا يعني أنه بعد اكتمال إدراج السالب يتم تحديث المؤشر. هذا ليس ضرورياً في الواقع لكنه مفيد لإزالة الإحتمالات

## جاري البحث

طريقة جيدة لإختبار رؤية ما تم صنعه هنا هو الدخول في أدوات Dev على OpenSearch Dashboards وتشغيل إستفسار البحث.

```json
GET /mostlylucid-blog-*
{}
```

هذا الاستعلام سيرجع إلينا جميع الفهاس التي تطابق نمط `mostlylucid-blog-*`/ / / / (وبالتالي جميع مؤشراتنا حتى الآن).

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

أدوات ديف في OpenSearch Dashboards هي طريقة عظيمة لاختبار إستفساراتك قبل أن تضعها في شفرتك.

![](devtools.png?width=900&quality=25)

## جاري البحث

الآن يمكننا ان نبدأ بالبحث عن الفهرس يمكننا استخدام `Search` على العميل للقيام بذلك.
هذا هو المكان الذي تأتي فيه القوة الحقيقية من OpenShearch في. لَهُ لَهُ حرفاً حرفياً [عشرات من الأنواع المختلفة للاستفسار](https://opensearch.org/docs/latest/query-dsl/) أنت استخدام إلى ابحث بيانات. كل شيء من بحث بسيط عن كلمة أساسية إلى بحث معقد عن "الناموسية".

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

### الوصف

هذه الطريقة `GetSearchResults`مصممة للاستفسار عن فهرس OpenSearch لاسترجاع مواقع المدونات. هو يَأْخذُ ثلاثة مؤكّد: `language`, `query`وبارامترات الانحلال `page` وقد عقد مؤتمراً بشأن `pageSize`/ / / / وهنا ما تقوم به:

1. **مؤشر الانتقاء**:
   
   - تقوم باسترجاع الاسم المُسند بالاسم المستخدم `GetBlogIndexName` على أساس اللغة المقدمة. ويتم اختيار المؤشر بطريقة ديناميكية حسب اللغة.

2. **هذا الأمر**:
   
   - الـ الدالة `Bool` & `Must` :: شرط ضمان تطابق النتائج مع بعض المعايير.
   - الـ داخل الـ `Must` الشرط، (أ) `MultiMatch` الاستعلام مُستخدَم إلى ابحث مرّات متعددة (% 1)`Title`, `Categories`، و ، ، ، ، ، ، ، ، ، ، ، ، ، ، `Content`).
     - **يُعزَز**الـ: `Title` (أ) إعطاء دفعة لـ `2.0`وجعله اكثر اهمية في البحث `Categories` - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - `1.5`/ / / / وهذا يعني أن المستندات التي يظهر فيها استعلام البحث في العنوان أو الفئات ستكون أعلى مرتبة.
     - ****استخدامات: `BestFields`الذي يحاول العثور على أفضل حقل مضاهاة للسؤال.
     - **أظظظظيف**الـ: `Fuzziness.Auto` تسمح البارامترات بالمطابقات التقريبية (مثلاً، مناولة المطبعة الثانوية).

3. ****:
   
   - الـ `Skip` أولاً `n` تُحسب على أساس عدد الصفحة، على النحو التالي: `(page - 1) * pageSize`/ / / / وهذا يساعد في التصفح من خلال النتائج التي يتم التوصل إليها من خلال النتائـج التناسليـة.
   - الـ `Size` عدد الوثائق المعادة إلى الدول `pageSize`.

4. ****:
   
   - إذا فشل الاستعلام، فإن خطأً ما هو مُسجّل و قائمة فارغة مُرجعة.

5. **النتيجة**:
   
   - الدالة الدالة ترجع قائمة `BlogIndexModel` مستندات مطابقة لمعايير البحث.

اذاً يمكنكم ان تروا انه يمكننا ان نكون فائقي المرونة في كيفية بحثنا عن بياناتنا يمكننا البحث عن مجالات محددة، ويمكننا تعزيز مجالات معينة، ويمكننا حتى البحث عبر مؤشرات متعددة.

وميزة واحدة من ميزات BIG هي السهولة التي يمكننا أن ندعم بها لغات متعددة. ولدينا مؤشر مختلف لكل لغة ويمكننا من البحث ضمن ذلك المؤشر. وهذا يعني أنه يمكننا استخدام المحلل الصحيح لكل لغة والحصول على أفضل النتائج.

## الواجهة الجديدة للبحث

على النقيض من البحث API الذي رأيناه في الأجزاء السابقة من هذه السلسلة، يمكننا تبسيط عملية البحث باستخدام OpenSearch. يمكننا فقط أن نضع النص على هذا السؤال ونحصل على نتائج عظيمة.

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

كما ترون لدينا كل البيانات التي نحتاجها في الفهرس لإرجاع النتائج. ثم يمكننا استخدام هذا لتوليد عنوان إلى تدوينة. هذا يأخذ التحميل من قاعدة بياناتنا ويجعل عملية البحث أسرع بكثير.

## في الإستنتاج

في هذا المقال رأينا كيفية كتابة عميل C# للتفاعل مع حالة OpenSearch.