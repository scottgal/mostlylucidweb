# إطار الهيئة المضاف لقائمة الوظائف الجزء 2

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">الساعة24/2024- 0224- 08- 15-2015 الساعة 00/18</datetime>

يمكنك أن تجد كل رموز البيانات الخاصة بكتابات المدونات على: [لا يُحَجْجَه](https://github.com/scottgal/mostlylucidweb/tree/local/Mostlylucid/Blog)

**الجزء 2 من السلسلة المتعلقة بإضافة إطار الكيان إلى مشروع أساسي من مشاريع الشبكة.**
يمكن العثور على جزء من الجزء الأول [هنا هنا](/addingentityframeworkforblogpostspt1).

# أولاً

في المقال السابق، أنشأنا قاعدة البيانات والسياق لمدوناتنا. وفي هذه الوظيفة، سنضيف الخدمات للتفاعل مع قاعدة البيانات.

وفي الوظيفة التالية، سنفصّل كيفية عمل هذه الخدمات الآن مع أجهزة المراقبة والآراء القائمة.

[رابعاً -

### إنشاء

لدينا الآن فئة تمديد BlogSetup التي تنشئ هذه الخدمات.

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

هذا استخدام `BlogConfig` لتحديد النمط الذي نحن فيه، إما `File` أو `Database`/ / / / وبناء على ذلك، نسجل الخدمات التي نحتاجها.

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

## الواجهات

كما أريد دعم كل من الملف وقاعدة البيانات في هذا التطبيق (لماذا لا)! لقد استخدمت أسلوباً قائماً على الواجهة يسمح لهم بالمبادلة على أساس التهيئة.

لدينا ثلاثة واجهات جديدة، `IBlogService`, `IMarkdownBlogService` وقد عقد مؤتمراً بشأن `IBlogPopulator`.

#### IBB وظيفة

هذا هو الواجهة الرئيسية لخدمة المدونين. وهو يتضمن أساليب للحصول على الوظائف والفئات وفرادى الوظائف.

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

#### & مُفكّر داون المُشغّل خادم

هذه الخدمة مستخدمة من قِبَل `EFlogPopulatorService` في الجولة الأولى لكتابة قاعدة البيانات مع بيانات من ملفات الهدف.

```csharp
public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();
}
```

كما يمكنك أن ترى أنه بسيط جداً وفقط لديه طريقتين، `GetPages` وقد عقد مؤتمراً بشأن `LanguageList`/ / / / تستخدم هذه لمعالجة ملفات العلامة و الحصول على قائمة اللغات.

يُنفَّذ هذا في `MarkdownBlogPopulator` -مصنفة. -مصنفة.

```csharp
public class MarkdownBlogPopulator : MarkdownBaseService, IBlogPopulator, IMarkdownBlogService
{
    //Extra methods removed for brevity
     public async Task<List<BlogPostViewModel>> GetPages()
    {
        var pageList = new ConcurrentBag<BlogPostViewModel>();
        var languages = LanguageList();
        var pages = await GetLanguagePages(EnglishLanguage);
        foreach (var page in pages) pageList.Add(page);
        var pageLanguages = languages.Values.SelectMany(x => x).Distinct().ToList();
        await Parallel.ForEachAsync(pageLanguages, ParallelOptions, async (pageLang, ct) =>
        {
            var langPages = await GetLanguagePages(pageLang);
            if (langPages is { Count: > 0 })
                foreach (var page in langPages)
                    pageList.Add(page);
        });
        foreach (var page in pageList)
        {
            var currentPagelangs = languages.Where(x => x.Key == page.Slug).SelectMany(x => x.Value)?.ToList();
            var listLangs = currentPagelangs ?? new List<string>();
            listLangs.Add(EnglishLanguage);
            page.Languages = listLangs.OrderBy(x => x).ToArray();
        }

        return pageList.ToList();
    }
    
     public  Dictionary<string, List<string>> LanguageList()
    {
        var pages = Directory.GetFiles(_markdownConfig.MarkdownTranslatedPath, "*.md");
        Dictionary<string, List<string>> languageList = new();
        foreach (var page in pages)
        {
            var pageName = Path.GetFileNameWithoutExtension(page);
            var languageCode = pageName.LastIndexOf(".", StringComparison.Ordinal) + 1;
            var language = pageName.Substring(languageCode);
            var originPage = pageName.Substring(0, languageCode - 1);
            if (languageList.TryGetValue(originPage, out var languages))
            {
                languages.Add(language);
                languageList[originPage] = languages;
            }
            else
            {
                languageList[originPage] = new List<string> { language };
            }
        }
        return languageList;
    }
 
}
```

#### مُنْبِل IB

تُستخدَم المدوّنات المُستخدِمة في طريقة إعدادنا أعلاه لحشو قاعدة البيانات أو كائنات المخابئ الساكنة (للنظام القائم على الملفات) بوظائف.

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

يمكنك أن ترى أن هذا امتداد إلى `WebApplication` مع تشكيل يسمح لقاعدة البيانات الهجرة إلى التشغيل عند الحاجة (الذي ينشئ قاعدة البيانات أيضاً إذا لم تكن موجودة). ثم يدعو إلى المُعَد `IBlogPopulator` (ب) الخدمات التي تقدم إلى الشبكة من أجل ملء قاعدة البيانات.

هذه هي واجهة تلك الخدمة.

```csharp
public interface IBlogPopulator
{
    Task Populate();
}
```

حق بسيط جداً؟ يجري تنفيذ هذا في كل من `MarkdownBlogPopulator` وقد عقد مؤتمراً بشأن `EFBlogPopulator` -الفصول الدراسية.

- - هنا نَدّعي في `GetPages` (ب) وضع طريقة لضبط المخبأ ووضعه في المخبأ.

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

- EF - هنا ننادي في `IMarkdownBlogService` للحصول على الصفحات ومن ثم تُشَغّل قاعدة البيانات.

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

لقد قسمنا هذه الخاصية إلى واجهات لجعل الشفرة أكثر تفهماً و "مفصلة" (كما هو الحال في مبادئ SolID). هذا يسمح لنا بمبادلة الخدمات بسهولة بناءً على التشكيل.

وفي الوظيفة التالية، سننظر بمزيد من التفصيل في تنفيذ `EFBlogService` وقد عقد مؤتمراً بشأن `MarkdownBlogService` -الفصول الدراسية.