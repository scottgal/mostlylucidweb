# (काम्पल) इकाई जाँच कर रही है ब्लॉग पार्ट 1 - सेवा

<datetime class="hidden">2024- 2523: 00</datetime>

<!--category-- xUnit, Moq, Unit Testing -->
## परिचय

इस पोस्ट में मैं इस साइट के लिए इकाई जाँच शुरू हो जाएगा। यह इकाई जाँच पर पूरा शिक्षण नहीं होगा, लेकिन इसके बजाय पोस्टों की एक श्रृंखला इस साइट पर कि कैसे मैं इकाई जाँच कर रहा हूँ।
इस पोस्ट में मैं डीबी संदर्भ द्वारा कुछ सेवाओं का परीक्षण करता हूँ; यह किसी भी डीबीएगन से दूर रहना है.

[विषय

## इकाई क्यों परखती है?

इकाई जाँच अकेला में अपने कोड के विभिन्न घटकों को जाँचने का एक तरीका है. कई कारणों से यह उपयोगी है:

1. यह आपके कोड के प्रत्येक अवयव को विशिष्ट क्षेत्रों में किसी भी विषय को देखने के लिए सरल बनाती है ।
2. यह अपने कोड को दस्तावेज़ का एक तरीका है. यदि आपके पास एक परीक्षा है जो असफल हो जाती है, आप जानते हैं कि आपके कोड के उस क्षेत्र में कुछ बदल गया है.

### कौन - से अन्य प्रकार की जाँच कर रहे हैं?

अन्य कई प्रकार के जाँच हैं जो आप कर सकते हैं. यहाँ कुछ हैं:

1. साथ मिलकर जाँच - जाँच कीजिए कि आपके कोड के अलग - अलग अवयव कैसे मिलकर काम करते हैं । हम जैसे औज़ार इस्तेमाल कर सकते हैं [सत्यापन](https://github.com/VerifyTests/Verify) अंत - बिन्दुओं के आउटपुट की जाँच करने और उन्हें वांछित परिणामों की तुलना करने के लिए । हम भविष्य में यह जोड़ देंगे.
2. अंत में जाँच - उपयोक्ता के दृष्टिकोण से पूरा अनुप्रयोग जाँच करें. जैसे औज़ारों के साथ किया जा सकता है [सेलेनियम](https://www.selenium.dev/).
3. परफ़ॉर्मेंस जाँच - जाँच करता है कि आपका अनुप्रयोग किस तरह लोड होता है. जैसे औज़ारों के साथ किया जा सकता है [स्ट्रांग जे मीटर](https://jmeter.apache.org/), [पोस्ट- मेन](https://www.postman.com/)___ मेरी पसंदीदा विकल्प हालांकि एक उपकरण कहलाता है [के6](https://k6.io/).
4. सुरक्षा जाँच - जाँच आपके अनुप्रयोग को कितनी सुरक्षित है. जैसे औज़ारों के साथ किया जा सकता है [OWA NNTP ZAP](https://www.zaproxy.org/), [बर्प सूट](https://portswigger.net/burp), [नेस्टससhaiti. kgm](https://www.tenable.com/products/nessus).
5. उपयोक्ता जाँच - जाँच करें कि आपका अनुप्रयोग अंत उपयोक्ता के लिए कैसे काम करता है. जैसे औज़ारों के साथ किया जा सकता है [उपयोक्ता जाँचिंग](https://www.usertesting.com/), [उपयोक्ता- ज़ूम](https://www.userzoom.com/), [उपयोक्ता नाम: (U)](https://www.userlytics.com/).

## जाँच परियोजना सेट किया जा रहा है

मैं अपने परीक्षण के लिए एक्स इकाई का उपयोग किया जा रहा हूँ. यह डिफ़ॉल्ट एकपीयू परियोजना में उपयोग में लिया जाता है. मैं भी के साथ डीबी संदर्भ ठट्ठा करने के लिए मोजे का उपयोग करने जा रहा हूँ

- MYEAVE - यह मैं मजाक करने के लिए उपयोगी एक्सटेंशन है - चीजों को बदलने योग्य है.
- MED. TED.ED.ED. Dback वस्तुओं का उपहास करने के लिए यह उपयोगी एक्सटेंशन है.

## डीब संदर्भ को धोखा देना

इस के लिए तैयारी में मैं अपने डीबी संदर्भ के लिए एक इंटरफेस जोड़ा. यह इतना है कि मैं अपनी परीक्षा में डीबी संदर्भ का उपहास कर सकते हैं. यहाँ इंटरफ़ेस है:

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

यह बहुत सरल है, बस हमारे DBobs का परदाफ़ाश और सहेजें परिवर्तनों को बचाने का तरीका.

आई *नहीं* मेरे कोड में एक भंडार पैटर्न इस्तेमाल करें. यह है क्योंकि एंटिटी फ्रेमवर्क पहले से ही भंडार पैटर्न है. मैं डीबी संदर्भ के साथ बातचीत करने के लिए एक सेवा परत का उपयोग करें. यह इसलिए है क्योंकि मैं एंटिटी फ्रेमवर्क की शक्ति कोसों दूर नहीं करना चाहता.

तब हम अपने लिए एक नई कक्षा जोड़ते हैं `Mostlylucid.Test` हमारे क्वैरी को सेट करने के लिए एक एक्सटेंशन विधि के साथ:

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

आप यह उपयोग कर रहा है कि देखेंगे `MockQueryable.Moq` उपहास करने के लिए एक्सटेंशन विधि जो तब हमारे ऊपर सेट करता है मैंQuery

### जांच सेट किया जा रहा है

इकाई जाँच का एक कोर दससेट है कि हर परीक्षा का 'एक' होना चाहिए और किसी भी अन्य परीक्षा के परिणाम पर निर्भर नहीं करना चाहिए (इस वजह से हम अपने डीबी संदर्भ का उपहास करते हैं).

हमारे नए में `BlogServiceFetchTests` वर्ग हम निर्माणकर्ता में अपनी परीक्षा संदर्भ सेट:

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

मैंने इस बहुत भारी टिप्पणी की है तो आप देख सकते हैं कि क्या हो रहा है. हम एक स्थापित कर रहे हैं `ServiceCollection` जो हम अपनी परीक्षाओं में उपयोग कर सकते हैं सेवा का एक संग्रह है. फिर (उनको) फाड़ कर जुदा कर देते हैं `IMostlylucidDBContext` और उसकी किताब (लौहे महफूज़) में (लिखा हुआ) है `ServiceCollection`___ फिर हम किसी भी अन्य सेवा को रजिस्टर करते हैं जो हमें हमारी परीक्षा के लिए चाहिए । अंत में हम निर्माण `ServiceProvider` जो हम अपनी सेवाओं से प्राप्त करने के लिए उपयोग कर सकते हैं.

## जाँच लिख रहा है

मैं एक एकल परीक्षण वर्ग जोड़ने के द्वारा शुरू की थी, नीचे `BlogServiceFetchTests` वर्ग. यह पोस्ट हो रही विधियों के लिए एक जांच वर्ग है `EFBlogService` वर्ग.

हर परीक्षा एक सामान्य प्रयोग करती है `SetupBlogService` नयी आबादी पाने का विधि `EFBlogService` वस्तु. यह इतना है कि हम अकेलेपन में सेवा की परीक्षा ले सकते हैं.

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

### ब्लॉगिटी विस्तार

यह एक सरल विस्तार वर्ग है जो हमें बहुत से परिचित कराने देता है `BlogPostEntity` वस्तुएँ. यह इतना है कि हम कई वस्तुओं के साथ अपनी सेवा की जाँच कर सकते हैं.

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

आप देख सकते हैं कि यह सब कुछ अलग भाषाओं और वर्गों के जरिए ब्लॉग पोस्टों की सेट संख्या लौटाता है. हालांकि हम हमेशा एक 'रू' वस्तु जोड़ते हैं...... हमें परीक्षा में एक ज्ञात वस्तु पर भरोसा करने के लिए सक्षम किया जा सकता है.

### परीक्षण

हर जाँच को पोस्ट के परिणामों के एक पहलू को जाँचने के लिए रचा गया है ।

उदाहरण के लिए, नीचे दिए गए दो लेखों में हम सिर्फ जाँच करते हैं कि हम सभी पोस्टों को पा सकते हैं और कि हम भाषा में पोस्ट ला सकते हैं ।

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

#### असफल होने के लिए जाँच

इकाई जाँच में एक महत्वपूर्ण धारणा 'एक असफलता' है जहां आप स्थापित करते हैं कि आपका कोड जिस तरह से आप यह उम्मीद कर रहे हैं.

नीचे की जाँच में हम पहले जाँच करते हैं कि हमारे paning कोड की तरह काम करता है। फिर हम जाँच करें कि अगर हम अधिक से पृष्ठों के लिए पूछना चाहते हैं, तो हम एक खाली परिणाम प्राप्त करें (और कोई त्रुटि नहीं)

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

## ऑन्टियम

यह हमारी इकाई जाँच करने के लिए एक सरल शुरुआत है. अगले पोस्ट में हम अधिक सेवाओं के लिए जाँच और अंत बिंदुओं के लिए परीक्षण जोड़ देंगे. हम भी इस बात पर गौर करेंगे कि हम कैसे हमारे अंत बिन्दुओं की जाँच कर सकते हैं संयोजन जाँच का उपयोग कर।