# ब्लॉग पोस्ट के लिए एंटिटी फ्रेमवर्क जोड़े ( पार्ट 2)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024- 0. 1515टी18: 00</datetime>

आप ब्लॉग पोस्ट के लिए सभी स्रोत कोड पा सकते हैं [GiHh](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**एंटिटी फ्रेमवर्क को एक परियोजना में जोड़ने पर श्रृंखला का भाग 2**
पार्ट 1 मिल सकता है [यहाँ](/blog/addingentityframeworkforblogpostspt1).

# परिचय

पिछले पोस्ट में, हमने डाटाबेस तथा अपने ब्लॉग पोस्ट के संदर्भ को स्थापित किया. इस पोस्ट में, हम सेवाओं को जोड़ने के लिए डेटाबेस के साथ संलग्न करेंगे.

अगले पोस्ट में हम देखेंगे कि ये सेवाएँ मौजूदा नियंत्रण और विचारों के साथ कैसे कार्य करती हैं ।

[विषय

### सेटअप

अब हम एक ब्लॉग-आवर विस्तार क्लास है जो इन सेवाओं को सेट करती है। यह हम में क्या किया से एक विस्तार है [पार्ट 1](/blog/addingentityframeworkforblogpostspt1), जहां हम डाटाबेस और संदर्भ सेट.

```csharp
  public static void SetupBlog(this IServiceCollection services, IConfiguration configuration)
    {
        var config = services.ConfigurePOCO<BlogConfig>(configuration.GetSection(BlogConfig.Section));
       services.ConfigurePOCO<MarkdownConfig>(configuration.GetSection(MarkdownConfig.Section));
        switch (config.Mode)
        {
            case BlogMode.File:
                services.AddScoped<IBlogService, MarkdownBlogService>();
                services.AddScoped<IBlogPopulator, MarkdownBlogPopulator>();
                break;
            case BlogMode.Database:
                services.AddDbContext<MostlylucidDbContext>(options =>
                {
                    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
                });
                services.AddScoped<IBlogService, EFBlogService>();
                services.AddScoped<IMarkdownBlogService, MarkdownBlogPopulator>();
                services.AddScoped<IBlogPopulator, EFBlogPopulator>();
                break;
        }
    }
```

यह साधारण उपयोग करता है `BlogConfig` वर्ग जिस मोड में हम हैं उसे पारिभाषित करने के लिए, या तो `File` या `Database`___ इस पर आधारित, हम उन सेवाओं का रजिस्टर करते हैं जिनकी हमें ज़रूरत है ।

```json
  "Blog": {
    "Mode": "File"
  }
```

```csharp
public class BlogConfig : IConfigSection
{
    public static string Section => "Blog";
    
    public BlogMode Mode { get; set; }
}

public enum BlogMode
{
    File,
    Database
}
```

## इंटरफेसेस

जैसे मैं इस अनुप्रयोग में फ़ाइल तथा डाटाबेस दोनों का समर्थन करना चाहता हूँ (क्योंकि क्यों नहीं!) मैंने एक इंटरफेस आधारित दृष्टिकोण का इस्तेमाल किया है उन्हें कॉन्फ़िग पर आधारित होने की अनुमति देने के लिए।

हम तीन नई इंटरफेस है, `IBlogService`, `IMarkdownBlogService` और `IBlogPopulator`.

#### ब्लॉग सर्विस

यह ब्लॉग सेवा के लिए मुख्य इंटरफेस है. इसमें पोस्ट, श्रेणी और प्रत्येक पोस्ट प्राप्त करने के तरीके हैं ।

```csharp
public interface IBlogService
{
   Task<List<string>> GetCategories();
    Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "");
    
    Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10, string language = BaseService.EnglishLanguage);
    
    Task<BlogPostViewModel?> GetPost(string slug, string language = "");
    
    Task<PostListViewModel> GetPagedPosts(int page = 1, int pageSize = 10, string language = BaseService.EnglishLanguage);
    
    Task<List<PostListModel>> GetPostsForLanguage(DateTime? startDate = null, string category = "", string language = BaseService.EnglishLanguage);
}
```

#### ब्लॉग सर्विसेस नीचे हस्ताक्षरित करें

यह सेवा इस सेवा के द्वारा प्रयोग में है `EFlogPopulatorService` पहले चलाने पर इस डाटाबेस को चिह्नित किए गए फ़ाइलों से पोस्टों को भरने के लिए चलाएँ.

```csharp
public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();
}
```

जैसा कि आप देख सकते हैं यह बहुत सरल है और बस दो तरीकों से है, `GetPages` और `LanguageList`___ ये चयनित फ़ाइलों को चिह्नित करने के लिए प्रयुक्त हैं तथा भाषाओं की सूची को प्राप्त करने के लिए.

#### ब्लॉगर

ब्लॉगPoperers वर्तमान सेटअप विधि में प्रयोग किया जाता है कि पोस्ट के जरिए डाटाबेस या स्थिर कैश ऑब्जेक्ट (फ़ाइल आधारित सिस्टम के लिए).

```csharp
  public static async Task PopulateBlog(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var config = scope.ServiceProvider.GetRequiredService<BlogConfig>();
        if(config.Mode == BlogMode.Database)
        {
           var blogContext = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
           await blogContext.Database.MigrateAsync();
        }
    
        var context = scope.ServiceProvider.GetRequiredService<IBlogPopulator>();
        await context.Populate();
    }
```

आप देख सकते हैं कि यह एक एक्सटेंशन है `WebApplication` कॉन्फ़िग के साथ डाटाबेस माइग्रेशन चलाने की अनुमति देता है यदि आवश्यक हो तो चलाने के लिए (जो भी डाटाबेस बना देता है). तब यह कॉल विन्यस्त किया गया `IBlogPopulator` डाटाबेस को भरने के लिए सेवा.

यह उस सेवा के लिए इंटरफेस है.

```csharp
public interface IBlogPopulator
{
    Task Populate();
}
```

### कार्यान्वयन

बहुत सरल है? यह दोनों में लागू है `MarkdownBlogPopulator` और `EFBlogPopulator` क्लास ।

- नीचे - यहाँ हम में फोन `GetPages` विधि और कैश को भरें.

```csharp
  /// <summary>
    ///     The method to preload the cache with pages and Languages.
    /// </summary>
    public async Task Populate()
    {
        await PopulatePages();
    }

    private async Task PopulatePages()
    {
        if (GetPageCache() is { Count: > 0 }) return;
        Dictionary<(string slug, string lang), BlogPostViewModel> pageCache = new();
        var pages = await GetPages();
        foreach (var page in pages) pageCache.TryAdd((page.Slug, page.Language), page);
        SetPageCache(pageCache);
    }
```

- EF - यहाँ हम में फोन `IMarkdownBlogService` पृष्ठ प्राप्त करने के लिए और फिर डाटाबेस को भरें.

```csharp
    public async Task Populate()
    {
        var posts = await markdownBlogService.GetPages();
        var languages = markdownBlogService.LanguageList();

        var languageEntities = await EnsureLanguages(languages);
        await EnsureCategoriesAndPosts(posts, languageEntities);

        await context.SaveChangesAsync();
    }

```

हमने इस कार्य को इंटरफेस में विभाजित किया है कोड को अधिक सरल बनाने के लिए और 'एनआईडी सिद्धांतों में' (जैसे कि). यह हमें कॉन्फ़िगरेशन पर आधारित सेवाओं को आसानी से बदलने देता है.

# ऑन्टियम

अगले पोस्ट में, हम इन सेवाओं का उपयोग करने के लिए नियंत्रक और दृश्य के कार्यान्वयन पर अधिक विस्तार से देखेंगे ।