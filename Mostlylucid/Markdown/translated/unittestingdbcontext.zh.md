# (简单) 单位测试博客第1部分 - 服务

<datetime class="hidden">2024-008-25T23:00</datetime>

<!--category-- xUnit, Moq, Unit Testing -->
## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

在这个职位上,我将开始为这个网站增加单位测试。 这不会是关于单位测试的全套辅导, 而是一系列关于我如何将单位测试添加到这个网站的文章。
在文章中, 我通过嘲弄 DbContext 来测试某些服务; 这是为了避免 DB 中出现任何特定的 Shennananigan 。

[技选委

## 为什么是单位测试?

单位测试是单独测试你代码各部分的方法 这一点之所以有用,原因如下:

1. 它隔离了你们守则的每个组成部分,使得看到特定领域的任何问题变得简单易懂。
2. 这是记录你代码的方法 如果你有一个测试失败了, 你知道,在你的代码的那一方面, 发生了一些变化。

### 还有什么其他类型的测试?

您还可以做一些其它类型的测试。 以下是几个:

1. 整合测试 - 测试你代码的不同部分是如何合作的。 在ASP.NET中,我们可以使用一些工具,例如: [核查核查](https://github.com/VerifyTests/Verify) 测试端点的输出,并将其与预期结果进行比较。 我们以后再加这个
2. 端到端测试 - 从用户的角度测试整个应用程序。 可以用一些工具来做到这一点,例如: [](https://www.selenium.dev/).
3. 性能测试 - 测试您的应用程序如何在负载下运行 。 可以用一些工具来做到这一点,例如: [阿帕奇·哈利梅特](https://jmeter.apache.org/), [邮邮工](https://www.postman.com/).. 然而,我更喜欢的备选办法是一个工具,称为: [k6 k6](https://k6.io/).
4. 安全测试 - 测试您的应用程序有多安全 可以用一些工具来做到这一点,例如: [OWASP 扎帕](https://www.zaproxy.org/), [包包套件套件套件](https://portswigger.net/burp), [内索](https://www.tenable.com/products/nessus).
5. 终端用户测试 - 测试您的应用程序如何为终端用户工作 。 可以用一些工具来做到这一点,例如: [用户测试](https://www.usertesting.com/), [用户缩放](https://www.userzoom.com/), [用户解语](https://www.userlytics.com/).

## 建立测试项目

我要用x单位做测试 这在 ASP.NET 核心项目中默认使用。 我也会用莫克来嘲笑DbContext和

- moqQueryable - 此扩展功能可用于模拟 I 查询对象 。
- Moq. Entity FrameworkCore - 用于模拟 DbContext 对象的有用扩展 。

## 模拟 DbContext

为了准备这次行动,我为我的DbContext增添了一个接口。 这样我就能在我的测试中 嘲笑DbContext了 以下是接口 :

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

很简单,只是暴露了我们的DBSets 和"拯救变化Async"的方法。

一一 *不要* 使用我代码中的仓库模式 。 这是因为实体框架核心已经是一个存储模式。 我用一个服务层来与DbContext互动。 这是因为我不想抽象 实体框架核心的力量。

然后我们再增加一个新的班级 `Mostlylucid.Test` 带有扩展方法以设置查询的工程 :

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

你会看到,这是使用 `MockQueryable.Moq` 创建模拟的扩展方法 。 然后设置我们的 I 查询对象和 IAsync 查询对象 。

### 设置测试

单位测试的核心信条是,每个测试应该是“单位”的工作,而不是取决于任何其他测试的结果(这就是为什么我们嘲笑我们的DbContext)。

我们的新 `BlogServiceFetchTests` 我们在构建器中设置了我们的测试上下文 :

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

我已经很认真地评论了这一点,所以你可以看到发生了什么事情。 我们正在设置一个 `ServiceCollection` 这是一系列服务, 我们可以用来测试。 然后,我以我们的(偶像)做一个模样, `IMostlylucidDBContext` 并登记在 `ServiceCollection`.. 然后登记测试所需的其他服务 最终,我们建设了 `ServiceProvider` 我们可以从中获取服务。

## 写作测试

我首先增加了一个测试类,即上面提到的 `BlogServiceFetchTests` 类。 这是"邮报" 获取我的方法的测试课程 `EFBlogService` 类。

每次测试使用通用 `SetupBlogService` 获取新人口组成方式的方法 `EFBlogService` 对象。 这样我们就可以单独测试这项服务。

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

### 博客内容扩展

这是一个简单的扩展类, 给了我们一些预言 `BlogPostEntity` 对象。 这样我们可以用不同的物体来测试我们的服务。

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

您可以看到,所有这一切都是返回一系列带有语言和分类的博客文章。 然而我们总是添加一个“ root” 对象, 使我们能够在测试中依赖已知的物体。

### 测试

每项测试旨在测试员额结果的一个方面。

例如,在下面两个方面,我们只是测试我们能够获得所有职位,而且我们能够按语言获得职位。

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

#### 测试失败测试

单位测试中的一个重要概念是“测试失败” 。 在测试失败时,您可以确定您的代码在您预期的方式上失败了 。

在下面的测试中,我们第一次测试 我们的传呼码是否和预期的一样有效 然后我们测试,如果我们要求的页数比我们多,结果就会是空的(而不是错误的)。

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

## 在结论结论中

这是一个简单的开始 我们的单位测试。 在下一篇文章中,我们将添加更多服务测试和终点测试。 我们还将研究如何通过整合测试测试我们的终点。