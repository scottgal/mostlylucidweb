# الوحدة (البسيطة) اختبار الجزء الأول من القائمة - الخدمات

<datetime class="hidden">2024-2024-08-02- 25-25T 23:00</datetime>

<!--category-- xUnit, Moq, Unit Testing -->
## أولاً

في هذا المقال سأبدأ بإضافة وحدة اختبار لهذا الموقع. لن يكون هذا درسًا كاملًا عن اختبار الوحدة، لكن بالأحرى سلسلة من المقالات عن كيفية إضافة وحدة الاختبار إلى هذا الموقع.
في هذا المقال أختبر بعض الخدمات عن طريق المحاكاة DbContex؛ هذا لتجنب أي DB أي chunnanaigins محدد.

[رابعاً -

## لماذا وحدة الاختبار؟

وحدة الاختبار هي طريقة لاختبار المكونات الفردية لرمزك في عزلة. وهذا مفيد لعدة أسباب:

1. إنه يعزل كل مكون من مكونات قانونك يجعل من السهل رؤية أي قضايا في مجالات محددة.
2. إنها طريقة لتوثيق شفرتك إذا كان لديك اختبار فشل، أنت تعرف أن شيئا ما قد تغير في تلك المنطقة من الشفرة الخاصة بك.

### ما هي الأنواع الأخرى للاختبارات الموجودة هناك؟

هناك عدد من الأنواع الأخرى من الاختبارات التي يمكنك القيام بها. وفيما يلي بعض ما يلي:

1. اختبار التكامل - اختبار كيفية عمل المكونات المختلفة لرمزك معاً. في ASP.net يمكننا استخدام أدوات مثل [أولا -](https://github.com/VerifyTests/Verify) اختبار ناتج نقاط النهاية ومقارنتها بالنتائج المتوقعة. سنضيف هذا في المستقبل
2. اختبار من النهاية إلى النهاية - اختبار التطبيق الكامل من وجهة نظر المستخدم. يمكن فعل هذا بأدوات مثل [سِِِِِِِِِِِِِسِِلِِِِسِِِِِِِِِِِِِِِِِ](https://www.selenium.dev/).
3. اختبار الأداء - اختبار كيفية أداء طلبك تحت الحمل. يمكن فعل هذا بأدوات مثل [KMet](https://jmeter.apache.org/), [رجل أعمال](https://www.postman.com/)/ / / / الخيار المفضل لدي هو أداة تدعى [](https://k6.io/).
4. اختبار أمني - اختبار مدى تأمين طلبك. يمكن فعل هذا بأدوات مثل [البلد الذي يوجد فيه](https://www.zaproxy.org/), [مُحَجج مُنْج](https://portswigger.net/burp), [السلس](https://www.tenable.com/products/nessus).
5. نهاية المستخدم اختبار اختبار اختبار كيف يعمل تطبيقك لمستخدم النهاية. يمكن فعل هذا بأدوات مثل [المُتَجِبْرِيْ](https://www.usertesting.com/), [سوم](https://www.userzoom.com/), [](https://www.userlytics.com/).

## إعداد مشروع الاختبار

سأستخدم xunit لإختباراتي. ويستخدم هذا المبلغ بالخطأ في مشاريع ASP.net الأساسية. أنا أيضاً سأستخدم (موق) للسخرية من (دب) مع

- QQQQureable - هذا له امتدادات مفيدة للاستهزاء بالأشياء القابلة للقياس.
- Muq. entityFrameworkCore - هذا له امتدادات مفيدة لاستهزاء كائنات DbConting.

## تخزين النص DbCon سياق DbCon

وتحضيراً لذلك، أضفت وصلة لـ (د.ب.كون) الخاص بي. هذا هو حتى أتمكن من سخرية النص DbConve في اختباراتي. هنا هو الواجهة:

```csharp
namespace Mostlylucid.EntityFramework;

public interface IMostlylucidDBContext
{
    public DbSet<CommentEntity> Comments { get; set; }
    public DbSet<BlogPostEntity> BlogPosts { get; set; }
    public DbSet<CategoryEntity> Categories { get; set; }

    public DbSet<LanguageEntity> Languages { get; set; }
    
    public DatabaseFacade Database { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}

```

الأمر بسيط جداً، فقط نكشف عن طريقة DBSets و طريقة Save Changes Assyync.

(أ) مـا مـن مـن مـن مـن مـن مـن مـن مـن مـن مـن *لا لا لا* استخدم نمط مستودع في شفرتي ويرجع ذلك إلى أن إطار الكيانات الأساسية هو بالفعل نمط مستودع. أنا أستخدم طبقة خدمة للتفاعل مع النص DbConficing. هذا لأنني لا أريد أن أتخلص من قوة إطار الكيان الأساسي.

ثم نضيف فئة جديدة إلى `Mostlylucid.Test` (ب) مشروع ذي طريقة تمديدية لتحديد استفسارنا:

```csharp
public static class MockDbSetExtensions
{
    public static Mock<DbSet<T>> CreateDbSetMock<T>(this IEnumerable<T> sourceList) where T : class
    {
        // Use the MockQueryable.Moq extension method to create the mock
        return sourceList.AsQueryable().BuildMockDbSet();
    }

    // SetupDbSet remains the same, just uses the updated CreateDbSetMock
    public static void SetupDbSet<T>(this Mock<IMostlylucidDBContext> mockContext, IEnumerable<T> entities,
        Expression<Func<IMostlylucidDBContext, DbSet<T>>> dbSetProperty) where T : class
    {
        var dbSetMock = entities.CreateDbSetMock();
        mockContext.Setup(dbSetProperty).Returns(dbSetMock.Object);
    }
}
```

سترى أن هذا يستخدم `MockQueryable.Moq` طريقة تمديد لإنشاء السخر. الذي يُثبّتُ ثمّ فوق كائناتِنا المُقَدّرةِ و كائنات آي أسينك Quaryable.

### إعداد الاختبار

وثمة مبدأ أساسي من مبادئ اختبار الوحدة هو أن كل اختبار ينبغي أن يكون "وحدة" من العمل وألا يعتمد على نتيجة أي اختبار آخر (وهذا هو السبب في أننا نسخر من نصنا DbCont).

في حالتنا الجديدة `BlogServiceFetchTests` ورتبنا سياق اختبارنا في المنشأة:

```csharp
  public BlogServiceFetchTests()
    {
        // 1. Setup ServiceCollection for DI
        var services = new ServiceCollection();
        // 2. Create a mock of IMostlylucidDbContext
        _dbContextMock = new Mock<IMostlylucidDBContext>();
        // 3. Register the mock of IMostlylucidDbContext into the ServiceCollection
        services.AddSingleton(_dbContextMock.Object);
        // Optionally register other services
        services.AddScoped<IBlogService, EFBlogService>(); // Example service that depends on IMostlylucidDbContext
        services.AddLogging(configure => configure.AddConsole());
        services.AddScoped<MarkdownRenderingService>();
        // 4. Build the service provider
        _serviceProvider = services.BuildServiceProvider();
    }
```

لقد علقت هذا بشكل كبير جدا حتى يمكنك أن ترى ما يجري. نحن ننشئ `ServiceCollection` وهي مجموعة من الخدمات التي يمكننا استخدامها في اختباراتنا. « ثم نخلق من بعد خلقنا » خلقنا خلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا و خلقنا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا وخلقا خاصً من عبادنا وخلقا خاصً خاصً خاصً خاصً ، وخلقًا وخلقً منًا خاصً خاصً ، وخلقًا وخلقً منا وخلقً منًا خاصً ، وخلقًا خاصً ، وخلقًا خاصً ، وخلقًا وخلقً ،ً من خلقًا خاصً خاصً ،ً ،ً ، وخلقًا وخلقً ،ً منًا وخلقً منًا وخلقً منًا خاصً خاصً ،ً ،ً منًا خاصًا وخلقًا وخلقً ،ً منًا وخلقً وخلقً منً ،ً ،ً منًا وخلقًا وخلقًا وخلقًا وخلقً ،ً وخلقً ،ً ،ً وخلقً وخلقً ،ًا وخلقًا وخلقً وخلقً ،ً وخلقً ،ً وخلقً ،ً خاصً خاصً ،ًا وخلقًا وخلقً خاصً خاصً خاصً ،ًا وخلقًا وخلقًا وخلقًا وخلقًا وخلقًا وخلقًا وخلقًا وخلقًا وخلقًا وخلقًا وخلقًا وخلقً وخلقً ،ًا وخلقناًًًًًًًًًًً منً منً خاصً خاصً خاصً خاصً وخلقً وخلقً وخلقًًً وخلقً وخلقً وخلقً وخلقً وخلقً خاصً خاصً خاصً خاصً وخلقً وخلقً خاصً خاصً خاصًًًً خاصً خاصً خاصًًًً خاصً خاصً خاصًًً خاصًًًًًًً وخلقً وخلقً وخلقً وخلقًًًً وخلقًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًًً `IMostlylucidDBContext` وتسجله في الـ `ServiceCollection`/ / / / ثم نسجل أي خدمات أخرى نحتاجها لاختباراتنا. وأخيراً، نبني `ServiceProvider` التي يمكننا استخدامها للحصول على خدماتنا من.

## & & & &:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

بدأت بإضافة فئة اختبار واحدة، `BlogServiceFetchTests` -مصنفة. -مصنفة. هذه درجة اختبار للحصول على أساليبي `EFBlogService` -مصنفة. -مصنفة.

كل اختبار يستخدم استخداماً مشتركاً `SetupBlogService` للحصول على طريقة جديدة مأهولة `EFBlogService` (أ) الهدف من الهدف. وهذا هو ما يمكننا من اختبار الخدمة في عزلة.

```csharp
    private IBlogService SetupBlogService(List<BlogPostEntity>? blogPosts = null)
    {
        blogPosts ??= BlogEntityExtensions.GetBlogPostEntities(5);

        // Setup the DbSet for BlogPosts in the mock DbContext
        _dbContextMock.SetupDbSet(blogPosts, x => x.BlogPosts);

        // Resolve the IBlogService from the service provider
        return _serviceProvider.GetRequiredService<IBlogService>();
    }

```

### مُ______ مُ___ مُ__ مُ_ مُ_ مُ_ مُ_ مُ_ مُ_ مُ_ مُ_ مُ_ مُ_ مُ_ مُ_ مُ_ مُ_ مُ_ م_ مُ م

هذه فئة بسيطة للتمديد والتي تعطينا عدداً من `BlogPostEntity` أُجُلِسَتْ أُخْرُسُ أُخْرُسُ أُخْرُسُ أُخْرُسُ. هذا حتى نتمكن من اختبار الخدمة لدينا مع عدد من الأجسام المختلفة.

```csharp
 public static List<BlogPostEntity> GetBlogPostEntities(int count, string? langName = "")
    {
        var langs = LanguageExtensions.GetLanguageEntities();

        if (!string.IsNullOrEmpty(langName)) langs = new List<LanguageEntity> { langs.First(x => x.Name == langName) };

        var langCount = langs.Count;
        var categories = CategoryEntityExtensions.GetCategoryEntities();
        var entities = new List<BlogPostEntity>();

        var enLang = langs.First();
        var cat1 = categories.First();

        // Add a root post to the list to test the category filter.
        var rootPost = new BlogPostEntity
        {
            Id = 0,
            Title = "Root Post",
            Slug = "root-post",
            HtmlContent = "<p>Html Content</p>",
            PlainTextContent = "PlainTextContent",
            Markdown = "# Markdown",
            PublishedDate = DateTime.ParseExact("2025-01-01T07:01", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            UpdatedDate = DateTime.ParseExact("2025-01-01T07:01", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            LanguageEntity = enLang,
            Categories = new List<CategoryEntity> { cat1 }
        };
        entities.Add(rootPost);
        for (var i = 1; i < count; i++)
        {
            var langIndex = (i - 1) % langCount;
            var language = langs[langIndex];
            var postCategories = categories.Take(i - 1 % categories.Count).ToList();
            var dayDate = (i + 1 % 30 + 1).ToString("00");
            entities.Add(new BlogPostEntity
            {
                Id = i,
                Title = $"Title {i}",
                Slug = $"slug-{i}",
                HtmlContent = $"<p>Html Content {i}</p>",
                PlainTextContent = $"PlainTextContent {i}",
                Markdown = $"# Markdown {i}",
                PublishedDate = DateTime.ParseExact($"2025-01-{dayDate}T07:01", "yyyy-MM-ddTHH:mm",
                    CultureInfo.InvariantCulture),
                UpdatedDate = DateTime.ParseExact($"2025-01-{dayDate}T07:01", "yyyy-MM-ddTHH:mm",
                    CultureInfo.InvariantCulture),
                LanguageEntity = new LanguageEntity
                {
                    Id = language.Id,
                    Name = language.Name
                },
                Categories = postCategories
            });
        }

        return entities;
    }
```

يمكنكم أن تروا أن كل ما يفعله هذا هو إعادة عدد من مقالات المدونات باللغات والفئات. ومع ذلك فإننا نضيف دائماً غرضاً "متقطعاً" يسمح لنا بأن نكون قادرين على الاعتماد على كائن معروف في اختباراتنا.

### الإخت الإختبارات

والغرض من كل اختبار هو اختبار جانب واحد من نتائج الوظائف.

فعلى سبيل المثال، في الحالتين الواردتين أدناه نختبر ببساطة أنه يمكننا الحصول على جميع الوظائف وأنه يمكننا الحصول على وظائف حسب اللغة.

```csharp
    [Fact]
    public async Task TestBlogService_GetBlogsByLanguage_ReturnsBlogs()
    {
        var blogService = SetupBlogService();

        // Act
        var result = await blogService.GetPostsForLanguage(language: "es");

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task TestBlogService_GetAllBlogs_ReturnsBlogs()
    {
        var blogs = BlogEntityExtensions.GetBlogPostEntities(2);
        var blogService = SetupBlogService(blogs);
        // Act
        var result = await blogService.GetAllPosts();

        // Assert
        Assert.Equal(2, result.Count());
    }
```

#### فشل

مفهوم مهم في اختبار الوحدة هو "فشل اختباري" حيث تثبت أن شفرتك تفشل بالطريقة التي تتوقعها.

في الاختبارات التي تحتها نختبر أولاً أن شفرات النبذات تعمل كما هو متوقع. ثم نختبر أنه إذا طلبنا صفحات أكثر مما لدينا، نحصل على نتيجة فارغة (وليس خطأ).

```csharp
    [Fact]
    public async Task TestBlogServicePagination_GetBlogsByCategory_ReturnsBlogs()
    {
        var blogPosts = BlogEntityExtensions.GetBlogPostEntities(10, "en");
        var blogService = SetupBlogService(blogPosts);

        // Act
        var result = await blogService.GetPagedPosts(2, 5);

        // Assert
        Assert.Equal(5, result.Posts.Count);
    }

    [Fact]
    public async Task TestBlogServicePagination_GetBlogsByCategory_FailsBlogs()
    {
        var blogPosts = BlogEntityExtensions.GetBlogPostEntities(10, "en");
        var blogService = SetupBlogService(blogPosts);

        // Act
        var result = await blogService.GetPagedPosts(10, 5);

        // Assert
        Assert.Empty(result.Posts);
    }
```

## في الإستنتاج

هذه بداية بسيطة لإختبار وحدتنا في الموقع التالي سنضيف إختبارات للمزيد من الخدمات و نقاط النهاية سننظر أيضاً كيف يمكننا اختبار نقاط النهاية باستخدام اختبار التكامل.