# إطار الهيئة المضاف للوظائف المشغولة (Pt. )٤(

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-00/17</datetime>

انظر الأجزاء [1](/blog/addingentityframeworkforblogpostspt1) وقد عقد مؤتمراً بشأن [2](/blog/addingentityframeworkforblogpostspt2) وقد عقد مؤتمراً بشأن [3](/blog/addingentityframeworkforblogpostspt3) عن الخطوات السابقة.

# أولاً

وفي أجزاء سابقة غطينا كيفية إنشاء قاعدة البيانات، وكيفية تنظيم ضوابطنا وآرائنا، وكيفية عمل خدماتنا. في هذا الجزء سوف نغطي تفاصيل عن كيفية بذر قاعدة البيانات مع بعض البيانات الأولية وكيف تعمل خدمات قاعدة EF.

كالمعتاد يمكنك أن ترى كل مصدر لهذا على بلدي جيت هوب [هنا هنا](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)، في مجلد Plisuslucid/Blog.

[رابعاً -

# تزور قاعدة البيانات

وفي الجزء السابق تناولنا كيف [:: تأسيس وإنشاء الخدمات](/blog/addingentityframeworkforblogpostspt2#setup)/ / / / في هذا الجزء سوف نغطي كيفية بذر قاعدة البيانات ببعض البيانات الأولية. هذا ما تم القيام به في `EFBlogPopulator` -مصنفة. -مصنفة. هذه الفئة مسجلة كخدمة في `SetupBlog` طريقة التمديد.

```csharp
    public async Task Populate()
    {
        var posts = await _markdownBlogService.GetPages();
        var languages = _markdownBlogService.LanguageList();

        var languageEntities = await EnsureLanguages(languages);
        await EnsureCategoriesAndPosts(posts, languageEntities);

        await Context.SaveChangesAsync();
    }
```

يمكنك أن ترى ذلك في `Populate` نُدَعَى في `_markdownBlogService.GetPages()` هذا يجري من خلال ملفاتنا الماكردونية و يُعبّر عن مجموعة من `BlogViewModels` يتضمن جميع الوظائف.
ثم نقوم بالمثل بالنسبة للغات، هذا ينظر الى `translated` لـ لـ الكل مجلد لـ الكل المُر المُر المُرْجَجَجّل الملفات التي نُنشئها باستخدام hieNMT (انظر: [هنا هنا](/blog/autotranslatingmarkdownfiles) لكيفية القيام بذلك الجزء).

## سابعاً - ما ما يُضاف من لغات

ثمّ نُدّعي إلى `EnsureLanguages` (ب) ضمان وجود جميع اللغات في قاعدة البيانات. وهذه طريقة بسيطة تتحقق من وجود اللغة، وإذا لم تكن تضيفها إلى قاعدة البيانات.

```csharp
  private async Task<List<LanguageEntity>> EnsureLanguages(Dictionary<string, List<string>> languages)
    {
        var languageList = languages.SelectMany(x => x.Value).ToList();
        var currentLanguages = await Context.Languages.Select(x => x.Name).ToListAsync();

        var languageEntities = new List<LanguageEntity>();
        var enLang = new LanguageEntity { Name =MarkdownBaseService.EnglishLanguage };

        if (!currentLanguages.Contains(MarkdownBaseService.EnglishLanguage)) Context.Languages.Add(enLang);
        languageEntities.Add(enLang);

        foreach (var language in languageList)
        {
            if (languageEntities.Any(x => x.Name == language)) continue;

            var langItem = new LanguageEntity { Name = language };

            if (!currentLanguages.Contains(language)) Context.Languages.Add(langItem);

            languageEntities.Add(langItem);
        }

        await Context.SaveChangesAsync(); // Save the languages first so we can reference them in the blog posts
        return languageEntities;
    }
```

سترون أن هذا بسيط و بسيط جداً و يضمن أن جميع اللغات التي حصلنا عليها من العلامات التنازلية موجودة في قاعدة البيانات، و كما حددنا أن هذه الهويات يتم توليدها آلياً نحن بحاجة إلى `SaveChanges` لضمان أن يتم توليد الهويات.

### إضافة الفئات والمناصب

ثمّ نُدّعي إلى `EnsureCategoriesAndPosts` (ب) ضمان إدراج جميع الفئات والوظائف في قاعدة البيانات. وهذا أكثر تعقيداً قليلاً لأننا بحاجة إلى ضمان أن تكون الفئات في قاعدة البيانات، ومن ثم نحتاج إلى ضمان أن تكون الوظائف في قاعدة البيانات.

```csharp
    private async Task EnsureCategoriesAndPosts(
        IEnumerable<BlogPostViewModel> posts,
        List<LanguageEntity> languageEntities)
    {
        var existingCategories = await Context.Categories.Select(x => x.Name).ToListAsync();

        var languages = languageEntities.ToDictionary(x => x.Name, x => x);
        var categories = new List<CategoryEntity>();

        foreach (var post in posts)
        {
            var currentPost =
                await PostsQuery().FirstOrDefaultAsync(x => x.Slug == post.Slug && x.LanguageEntity.Name == post.Language);
            await AddCategoriesToContext(post.Categories, existingCategories, categories);
            await AddBlogPostToContext(post, languages[post.Language], categories, currentPost);
        }
    }
```

يمكنك أن ترى أننا ندعو إلى `PostsQuery` و هي طريقة بسيطة ترجع إلى `BlogPostEntity` حتى نتمكن من استعلام قاعدة بيانات الوظائف.

```csharp
  protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);
   
```

#### جاري إضافة المصنفات

ثمّ نَدّي إلى `AddCategoriesToContext` (ب) ضمان إدراج جميع الفئات في قاعدة البيانات. وهذه طريقة بسيطة تتحقق مما إذا كانت الفئة موجودة وما إذا لم تكن تضيفها إلى قاعدة البيانات.

```csharp
    private async Task AddCategoriesToContext(
        IEnumerable<string> categoryList,
        List<string> existingCategories,
        List<CategoryEntity> categories)
    {
        foreach (var category in categoryList)
        {
            if (categories.Any(x => x.Name == category)) continue;

            var cat = new CategoryEntity { Name = category };

            if (!existingCategories.Contains(category)) await Context.Categories.AddAsync(cat);

            categories.Add(cat);
        }
    }

```

ومرة أخرى، يتحقق هذا الأمر مما إذا كانت الفئة موجودة وما إذا لم تكن تضيفها إلى قاعدة البيانات.

#### إضافة الوظائف القائمة

ثمّ نَدّي إلى `AddBlogPostToContext` (ب) ضمان أن تكون جميع الوظائف مدرجة في قاعدة البيانات. وهذا أمر أكثر تعقيدا إلى حد ما لأننا بحاجة إلى ضمان أن تكون الوظيفة في قاعدة البيانات، ومن ثم نحتاج إلى ضمان أن تكون الفئات في قاعدة البيانات.

```csharp
  private async Task AddBlogPostToContext(
        BlogPostViewModel post,
        LanguageEntity postLanguageEntity,
        List<CategoryEntity> categories,
        BlogPostEntity? currentPost)
    {
        try
        {
            var hash = post.HtmlContent.ContentHash();
            var currentCategoryNames = currentPost?.Categories.Select(x => x.Name).ToArray() ?? Array.Empty<string>();
            var categoriesChanged = false;
            if (!currentCategoryNames.All(post.Categories.Contains) ||
                !post.Categories.All(currentCategoryNames.Contains))
            {
                categoriesChanged = true;
                _logger.LogInformation("Categories have changed for post {Post}", post.Slug);
            }

            var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
            var titleChanged = currentPost?.Title != post.Title;
            if (!titleChanged && !dateChanged && hash == currentPost?.ContentHash && !categoriesChanged)
            {
                _logger.LogInformation("Post {Post} has not changed", post.Slug);
                return;
            }

            var blogPost = new BlogPostEntity
            {
                Title = post.Title,
                Slug = post.Slug,
                HtmlContent = post.HtmlContent,
                PlainTextContent = post.PlainTextContent,
                ContentHash = hash,
                PublishedDate = post.PublishedDate,
                LanguageEntity = postLanguageEntity,
                LanguageId = postLanguageEntity.Id,
                Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList()
            };


            if (currentPost != null)
            {
                _logger.LogInformation("Updating post {Post}", post.Slug);
                Context.BlogPosts.Update(blogPost);
            }
            else
            {
                _logger.LogInformation("Adding post {Post}", post.Slug);
                await Context.BlogPosts.AddAsync(blogPost);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error adding post {Post}", post.Slug);
        }
    }

```

كما ترون هذا يحتوي على كمية كبيرة من الكشف عن التغيير لضمان أننا لا نقوم بإعادة إضواء المواقع التي لم تتغير. نتحقق من حجم المحتوى، الفئات، التاريخ والعنوان. إذا كان أي من هذه قد تغير نحن تحديث وظيفة في قاعدة البيانات.

شيء واحد لملاحظة هو كيف مزعج التحقق من التاريخ timeOffset هو؛ لدينا لتحويله إلى UTC ومن ثم الحصول على التاريخ لمقارنته. هذا هو السبب في أن `DateTimeOffset` لدينا مكون زمني ونريد أن نقارن التاريخ فقط

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# في الإستنتاج

الآن لدينا نظام مدوّنات يعمل بشكل كامل والذي يمكن أن يكون مأهولاً من ملفات العلامات التنازلية والملفات المترجمة المدوّنة. في الجزء التالي سنغطي الخدمة البسيطة التي نستخدمها لعرض الوظائف المخزنة في قاعدة البيانات.