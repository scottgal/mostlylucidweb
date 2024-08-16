# 添加博客文章实体框架(第2部分)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-15T18:00</datetime>

您可以在博客文章中找到所有源代码 [吉特胡布](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**关于将实体框架加入.NET核心项目的系列第二部分。**
第一部分可以找到 [在这里](/blog/addingentityframeworkforblogpostspt1).

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

在上篇文章中, 我们建立了数据库, 以及博客文章的背景。 在此员额中,我们将增加与数据库互动的服务。

在下一篇文章中,我们将详细说明这些服务目前如何与现有的控制者和观点合作。

[技选委

### 设置设置设置设置设置设置设置

现在我们有一个BlogSetup推广班来提供这些服务。 这是对我们所作所为的延伸 [第一部分 第一部分](/blog/addingentityframeworkforblogpostspt1),我们建立数据库和上下文。

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

此选项使用简单 `BlogConfig` 类来定义我们处于哪种模式,或者 `File` 或 `Database`.. 基于这一点,我们登记我们需要的服务。

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

## 接口

我想在这个应用程序中同时支持文件和数据库( 因为为什么不! 我采用了基于界面的方法 允许根据配置来交换这些

我们有三个新界面 `IBlogService`, `IMarkdownBlogService` 和 `IBlogPopulator`.

#### IB服务

这是博客服务的主要界面 。 它载有获得员额、职类和个人员额的方法。

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

#### IMarkdown 服务( ImarkdownBlog service)

此服务由以下用户使用: `EFlogPopulatorService` 首运行以从标记文件填充文章来填充数据库 。 @ info: whatsthis

```csharp
public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();
}
```

正如你可以看到的,它非常简单 并且只是有两种方法, `GetPages` 和 `LanguageList`.. 这些用于处理 Markdown 文件并获取语言列表 。

#### IBlogPoppuputor

BlogPopulators 用于上述设置方法, 以填充数据库或静态缓存对象( 以文件为基础的系统),

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

您可以看到,这是 `WebApplication` 配置允许根据需要运行数据库迁移( 如果数据库不存在, 也会创建数据库 ) 。 然后它调用配置 `IBlogPopulator` 输入数据库的服务。

这是该服务的界面 。

```csharp
public interface IBlogPopulator
{
    Task Populate();
}
```

### 执行 执行情况 执行

很简单吧? 这在以下两个方面都得到了实施: `MarkdownBlogPopulator` 和 `EFBlogPopulator` 班级。

- Markdown - 在这里,我们呼叫 `GetPages` 方法并填充缓存 。

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

- EF - 我们在这里呼叫 `IMarkdownBlogService` 以获取页面,然后填充数据库。

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

我们已经将这个功能分成接口, 使代码更容易理解, 并且“分离”(如SOLID原则) 。 这使得我们可以轻松地根据配置换掉服务。

# 在结论结论中

在下一个职位上,我们将更详细地研究主计长和看法的执行情况,以便利用这些服务。