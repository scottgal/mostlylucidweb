# نص كامل متصفح ( Ptt 1 1 1

<!--category-- Postgres, Entity Framework -->
<datetime class="hidden">2024-08-20T12: 40</datetime>

# أولاً

والبحث عن المحتوى هو جزء حاسم الأهمية من أي موقع على شبكة الإنترنت ثقيل المحتوى. وهو يعزز القدرة على الاكتشاف وخبرة المستعملين. في هذا الموقع سوف أغطي كيف أضفت النص الكامل للبحث عن هذا الموقع

الأجزاء التالية من هذه السلسلة:

- [مربع البحث مع الموضع](/blog/textsearchingpt11)
- [مقدمة إلى OpenSearch](/blog/textsearchingpt2)
- [ابحث مع C](/blog/textsearchingpt3)

[رابعاً -

# أولاً - النهج

هناك عدد من الطرق للقيام بنص كامل للبحث بما في ذلك

1. مجرد البحث في بنية بيانات الذاكرة (مثل القائمة)، هذا أمر بسيط نسبياً للتنفيذ لكنه لا يتدرج بشكل جيد. بالإضافة إلى أنه لا يدعم الإستفسارات المعقدة بدون الكثير من العمل.
2. استخدام قاعدة بيانات مثل SQL خادم أو Postgres. في حين أن هذا يعمل و لديه دعم من جميع أنواع قواعد البيانات تقريباً فإنه ليس دائماً أفضل حل لهياكل بيانات أكثر تعقيداً أو استفسارات معقدة
3. استخدام تقنية البحث عن التكنولوجيا مثل: [::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::](https://lucenenet.apache.org/) أو SQLite FTS. وهذه نقطة وسط بين الحلين المذكورين أعلاه. إنه أكثر تعقيداً من مجرد البحث عن قائمة لكن أقل تعقيداً من حل قاعدة بيانات كاملة ومع ذلك، فإنه لا يزال معقّداً جداً للتنفيذ (خاصة بالنسبة إلى البيانات عن ابتلاع البيانات) ولا يتسع كما أنه حل بحثي كامل. في الحقيقة العديد من تكنولوجيات البحث الأخرى [استخدام الوسين تحت غطاء المحرك من أجل: ](https://www.elastic.co/search-labs/blog/elasticsearch-lucene-vector-database-gains) إنها قدرات بحثية قوية في مجال البحث عن المتجهات.
4. باستخدام محرك بحث مثل ElasticSearch أو OpenSearch أو Azuere Search. وهذا هو الحل الأكثر تعقيداً وكثافة في استخدام الموارد، ولكنه أيضاً الحل الأكثر قوة. وهو أيضاً الأكثر قابلية للتعديل ويمكنه التعامل مع الاستفسارات المعقدة بسهولة سوف أذهب إلى العمق المؤلم في الأسبوع القادم أو نحو ذلك حول كيفية استضافة ذاتي، إعداد واستخدام OpenSearch من C#.

# نص كامل البحث مع الملصقات

في هذه المدونة انتقلت مؤخراً إلى استخدام Postgres لقاعدة بياناتي. يوجد في الموقع خاصية كاملة للبحث النصي وهي قوية جداً و(على نحو ما) سهلة الاستخدام. وهو أيضا سريع جدا ويمكن التعامل مع الاستفسارات المعقدة بسهولة.

متى بناء `DbContext` يمكنك تحديد أيّ حقول لديها كامل نص البحث المُمكّن.

تستخدم المواضع مفهوم متجهات البحث لتحقيق البحث الكامل في النصوص بسرعة وكفاءة. متجه البحث هو عبارة عن هيكل بيانات يحتوي على الكلمات في مستند ومواقعها. أساسيّاً:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: إلى:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
وهو يستخدم نوعين خاصين من البيانات لتحقيق ذلك:

- TSVEctor: نوع خاص من بيانات PostgreSQL يقوم بتخزين قائمة من المعاجم (إعتبرها متجهة للكلمات). إنها النسخة المفهرسة من الوثيقة المستخدمة للبحث السريع.
- TSQYE: نوع بيانات خاص آخر يخزن استعلام البحث، الذي يشمل شروط البحث والمشغلين المنطقيين (مثل و، أو، لا).

بالإضافة إلى ذلك a رتّب الدالة إلى ترتيب النتائج مستند إلى إلى كيف يطابق طلب البحث. هذا قوي جداً ويمكّنك من ترتيب النتائج حسب الأهمية.
PostgreSQL يعيّن ترتيباً للنتائج استناداً إلى الأهمية. وتحسب الأهمية بعوامل اعتبارية مثل قرب مصطلحات البحث من بعضها البعض ومدى تواتر ظهورها في المستند.
الدالة ts_rank أو ts_rank_cd تستخدم لحساب هذا الترتيب.

يمكنك قراءة المزيد عن خصائص البحث النصي الكامل لـ Postgress [هنا هنا](https://www.postgresql.org/docs/current/textsearch.html)

## أُكِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِِْْ

رزمة إطار عمل ما بعد الاستغريس [هنا هنا](https://www.npgsql.org/efcore/mapping/full-text-search.html?tabs=pg12%2Cv5) يُقدّم دعما قويا للبحث الكامل عن النصوص. إنه يسمح لك بتحديد أي الحقول هي كاملة النص مفهرسة وكيفية إستفسارها.

ولفعل ذلك نضيف أنواع مؤشرات محددة إلى كياناتنا كما هي معرّفة في `DbContext`:

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

هنا نحن نضيف نص كامل فهرس إلى `Title` وقد عقد مؤتمراً بشأن `PlainTextContent` من `BlogPostEntity`/ / / / نحن ايضاً نحدد ايضاً ان الفهرس يجب ان يستخدم `GIN` ألف - الرســل `english` اللغـة اللغـة اللغـة وهذا أمر هام لأنه ينبئ Postgres بكيفية فهرسة البيانات واللغات التي تستخدم في وقف الكلمات ووقفها.

من الواضح أن هذه قضية بالنسبة لمدونتنا كما لدينا لغات متعددة. لسوء الحظ في الوقت الراهن أنا فقط أستخدم `english` (بآلاف دولارات الولايات المتحدة) هذا شيء يجب أن أعالجه في المستقبل لكن الآن يعمل بشكل جيد بما فيه الكفاية.

ونضيف أيضاً رقماً قياسياً إلى `Category` الكيان:

```csharp
     modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasIndex(b => b.Name).HasMethod("GIN").IsTsVectorExpressionIndex("english");;
...

```

عن طريق القيام بهذا الـ postgres يولّد متجه بحث لكل صف في قاعدة البيانات. هذا المتجه يحتوي على عبارة في `Title` وقد عقد مؤتمراً بشأن `PlainTextContent` في مجالات متعددة. ثم يمكننا استخدام هذا المتجه للبحث عن عبارة في المستند.

هذا يُترجم إلى الدالة at_ tsvat في SQL التي تُولّد مُشتَرَكَة مُشتَرَكَة لِلتَوَجُّهِ. ثم يمكننا استخدام الدالة ts_rrank لترتيب النتائج بناءً على الأهمية.

```postgresql
SELECT to_tsvector('english', 'a fat  cat sat on a mat - it ate a fat rats');
to_tsvector
-----------------------------------------------------
'ate':9 'cat':3 'fat':2,11 'mat':7 'rat':12 'sat':4
```

تطبيق هذا كإتجاه لقاعدة بياناتنا ونحن مستعدون للبدء بالبحث.

# جاري البحث

## مؤشر TssVrect

إلى البحث سوف نستعمل `EF.Functions.ToTsVector` وقد عقد مؤتمراً بشأن `EF.Functions.WebSearchToTsQuery` إلى إ_ نشئ a بحث متجه و إستفسار. ثم يمكننا استخدام `Matches` إلى ابحث لـ الدالة في مُتَحَذِّر.

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

الدالة EF. Functions. WebSearch TotTSQuysere تولد الاستعلام لـ سطر مستندة على نمط موحد لـ Web Search translated translated translated translated by queentions.

```postgresql
SELECT websearch_to_tsquery('english', '"sad cat" or "fat rat"');
       websearch_to_tsquery
-----------------------------------
 'sad' <-> 'cat' | 'fat' <-> 'rat'
```

في هذا المثال يمكنك أن ترى أن هذا يولّد إستفسار يبحث عن عبارة "asad cat" أو "fat bat" في المستند. وهذه سمة قوية تتيح لنا البحث بسهولة عن الاستفسارات المعقدة.

كما هو مبين قبل هذه الطرائق كل من توليد متجه البحث والسؤال للصف. ثم نستخدم `Matches` إلى ابحث لـ الدالة في مُتَحَذِّر. يمكننا أيضاً استخدام `Rank` إلى ترتيب النتائج حسب الصلة.

كما ترون هذا ليس سؤالاً بسيطاً لكنه قوي جداً و يسمح لنا بالبحث عن كلمات في `Title`, `PlainTextContent` وقد عقد مؤتمراً بشأن `Category` من `BlogPostEntity` و مرتبة هذه حسب الأهمية.

## الشبكة الدولية

لاستخدام هذه (في المستقبل) يمكننا إنشاء نقطة نهاية بسيطة WebAPI التي تأخذ إستفسار وترجع النتائج. هذا هو a بسيط متحكم الذي a إقتباس و نتيج:

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

## العمود والنوع المُولِثان

نهج بديل لاستخدام هذه "البساطة" TsVector indexes هو استخدام عمود متولد لتخزين متجه البحث ومن ثم استخدام هذا للبحث. وهذا نهج أكثر تعقيدا ولكنه يسمح بأداء أفضل.
هنا نُعدّل `BlogPostEntity` إلى إضافة نوع خاص من العمود:

```csharp
   [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public NpgsqlTsVector SearchVector { get; set; }
```

هذا هو a عامود الذي يولّد متجه البحث لـ. ثم يمكننا استخدام هذا العمود للبحث عن كلمات في المستند.

ثم نضع هذا الفهرس داخل تعريف الكيان (يتعين تأكيده ولكن هذا قد يسمح لنا أيضاً بالحصول على لغات متعددة عن طريق تحديد عمود لغة لكل وظيفة).

```csharp
   entity.Property(b => b.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))", stored: true);
```

سترى هنا أننا نستخدم `HasComputedColumnSql` إلى set this this the PostGreSQL الدالة إلى توليد متجه البحث. ونحدد أيضاً أن العمود مخزن في قاعدة البيانات. وهذا مهم لأنه يُخبر Postgres بتخزين متجه البحث في قاعدة البيانات. هذا يسمح لنا بالبحث عن عبارة في المستند باستخدام متجه البحث.

في قاعدة البيانات هذا متولّد هذا لكل صف، التي هي "الليكسم" في المستند ومواقعها:

```csharp
"'1992':464 '1996':468 '20':480 '200':115 '2007':426 '2009':428 '2012':88 '2015':397 '2018':370 '2020':372 '2021':288,327,329,399 '2022':196,243,245,290 '2024':156,158,198 '25':21,477,486,522 '3d':346 '6':203,256 '8':179,485 '90':120,566 'ab':282 'access':221 'accomplish':14 'achiev':118 'across':60 'adapt':579 'advanc':134 'applic':168,316,526 'apr':155,197 'architect':83,97,159 'architectur':307,337 ...
```

### البحث البحثي (البحثي)

ثم يمكننا استخدام هذا العمود للبحث عن كلمات في المستند. يمكننا استخدام `Matches` إلى ابحث لـ الدالة في مُتَحَذِّر. يمكننا أيضاً استخدام `Rank` إلى ترتيب النتائج حسب الصلة.

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

أنت سَتَسْلمُ هنا بأنّنا نَستعملُ أيضاً أيضاً a مختلف `EF.Functions.ToTsQuery("english", query + ":*")`  التي تسمح لنا بعرض وظيفة من نوع PiteAhead (حيث يمكننا أن نطبع مثلاً). "كات" والحصول على "كات" و"كاتس" و"كاتربيلر" وما إلى ذلك.

بالإضافة إلى ذلك يمكننا تبسيط التدوينة الرئيسية لـ ابحث عن الإستفسار بوصة `SearchVector` عمودا في العمود الفقري. هذه سمة قوية تسمح لنا بالبحث عن كلمات في `Title`, `PlainTextContent`/ / / / ما زلنا نستخدم المؤشر الذي عرضناه أعلاه `CategoryEntity`.

```csharp
x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) 
```

ثم نستخدم `Rank` إلى ترتيب النتائج حسب الصلة استناداً إلى الاستعلام.

```csharp
 x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*")))
```

دعونا نستخدم نقطة النهاية على النحو التالي، حيث يمكننا المرور في الأحرف الأولى القليلة من كلمة واحدة ونسترجع كل الوظائف التي تطابق تلك الكلمة:

يمكنك أن ترى [إجراء إجراء هنا](https://www.mostlylucid.net/swagger/index.html) النظر إلى `/api/SearchApi`/ / / / (ملاحظة: لقد قمت بتمكين المراوغة لهذا الموقع حتى تتمكن من رؤية API في العمل ولكن معظم الوقت هذا ينبغي أن يكون مخصصا لـ 'IsDevelopment').

![ألف- ألفيج](searchapi.png?width=900&format=webp&quality=50)

في المستقبل سأضيف ميزة PiepAhead إلى صندوق البحث في الموقع الذي يستخدم هذه الوظيفة.

# في الإستنتاج

يمكنك أن ترى أنه من الممكن الحصول على وظيفة بحث قوية باستخدام Postgres وإطار الكيان. ومع ذلك فإن لها تعقيدات وقيوداً يجب أن نحسبها (مثل اللغة). في الجزء التالي سأقوم بتغطية كيفية القيام بهذا باستخدام OpenSearch والذي يحتوي على الكثير من الإعدادات لكنه أكثر قوة و قابلة للقياس.