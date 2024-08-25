# Перевірка одиниць блогу Частина 1 - служби

<datetime class="hidden">2024- 08- 25T23: 00</datetime>

<!--category-- xUnit, Moq, Unit Testing -->
## Вступ

У цьому полі я почну додавати перевірку одиниць для цього сайта. Це не буде повним підручником з перевірки одиниць, а радше серією дописів про те, як я додаю перевірку одиниць до цього сайту.
У цьому пості я перевіряю деякі послуги, глузуючи з DbContext; це для того, щоб уникнути будь-яких специфічних DB шеньніганів.

[TOC]

## Чому слід перевіряти одиниці?

Перевірка модулів - це спосіб тестування окремих компонентів вашого коду у ізоляції. Це корисно з декількох причин:

1. Вона відокремлює кожен компонент вашого коду і робить його простим, щоб бачити будь-які проблеми в певних ділянках.
2. Це спосіб документувати ваш код. Якщо у вас є тест, який зазнає невдачі, ви знаєте, що щось змінилося у цій частині вашого коду.

### Які є інші види тестування?

Існує багато інших видів тестування, які ви можете зробити. Ось кілька з них:

1. Перевірка інтеграції - перевірка того, як різні компоненти вашого коду працюють разом. У ASP.NET ми можемо використовувати такі інструменти, як [Перевірити](https://github.com/VerifyTests/Verify) для перевірки виводу кінцевих точок і порівняння їх з очікуваними результатами. Ми додамо це в майбутньому.
2. Перевірка на кінець- до кінця - перевірка всієї програми з перспективи користувача. Це можна зробити за допомогою інструментів на зразок [Селен](https://www.selenium.dev/).
3. Перевірка швидкодії - перевірка того, як працює програма під навантаженням. Це можна зробити за допомогою інструментів на зразок [Apache JMedia](https://jmeter.apache.org/), [PostMan](https://www.postman.com/). Але мій вибір - це інструмент з назвою [к6](https://k6.io/).
4. Перевірка безпеки - перевірка безпеки вашої програми. Це можна зробити за допомогою інструментів на зразок [OWASP ZAP](https://www.zaproxy.org/), [Burp Suite](https://portswigger.net/burp), [Нессусgreece_ prefectures. kgm](https://www.tenable.com/products/nessus).
5. Завершити перевірку користувача - перевірка того, як працює ваша програма для кінцевого користувача. Це можна зробити за допомогою інструментів на зразок [Перевірка користувача](https://www.usertesting.com/), [UserZom](https://www.userzoom.com/), [Користувачі](https://www.userlytics.com/).

## Налаштування тестового проекту

Я збираюся використовувати xUnit для моїх тестів. Типово цей пункт використовується у проектах ядра ASP. NET. Я також буду використовувати Moq для висміювання DbContext разом з

- Moq Quoryable - Це корисно для глузування з можливих об'єктів.
- Moq.EntityFrameworkCore - Тут ви знайдете корисні розширення для висміювання об' єктів DbContext.

## Насміхаючись з DBContext

Приготовляючись до цього, я додав інтерфейс до мого DbContext. Це для того, щоб я міг висміювати DbContext у своїх тестах. Ось інтерфейс:

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

Це досить просто, просто викриваючи наші DBSets і SaveChangesAync метод.

I *не* використовувати шаблон сховища у моєму коді. Це тому, що ядро блокування сутностей вже є шаблоном сховища. Я використовую шар сервісу для взаємодії з DbContext. Це тому, що я не хочу абстрагувати силу блокажу сутності.

Потім ми додаємо новий клас до нашого `Mostlylucid.Test` project з методом розширення для налаштування нашого запиту:

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

Ви побачите, що це використовує `MockQueryable.Moq` метод додавання для створення висмішки. Що потім створює наші об'єкти, яких можна зрозуміти, та об'єкти, які можна синхронізувати.

### Як встановити перевірку

Основним принципом тестування одиниць є те, що кожен тест повинен бути " об' єднаною " роботою, а не залежати від результату будь- якого іншого тесту (це те, чому ми висміюємо наш DbContext).

В наших нових `BlogServiceFetchTests` клас ми встановили наш тестовий контекст у конструкторі:

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

Я досить сильно прокоментував це, щоб ви могли побачити, що відбувається. Ми створюємо a `ServiceCollection` Це збірка послуг, які ми можемо використовувати у випробуваннях. Потім ми створюємо насмішку `IMostlylucidDBContext` і зареєструвати його в `ServiceCollection`. Потім ми реєструємо інші послуги, необхідні для наших тестів. Нарешті ми побудуємо `ServiceProvider` які ми можемо використати для отримання наших послуг.

## Написання тесту

Я почав з додавання одного тестового класу, вищезазначеного `BlogServiceFetchTests` Клас. Це тестовий клас для отримання методів Post `EFBlogService` Клас.

Кожен тест використовує спільний `SetupBlogService` метод отримання нових населених місць `EFBlogService` об'єкт. Це для того, щоб ми могли випробувати служіння в ізоляції.

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

### Середовища BlogEntityExtensions

Це простий клас розширення, який надає нам кількість вулудильців. `BlogPostEntity` об'єкти. Це для того, щоб ми могли перевірити нашу службу за допомогою багатьох різних об'єктів.

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

Ви можете бачити, що все це повертає певну кількість дописів блогів з мовами і категоріями. Проте ми завжди додаємо до нього кореневий об'єкт, що дозволяє нам покладатися на відомий об'єкт у наших тестах.

### Тести

Кожна перевірка розрахована на те, щоб перевірити один аспект повідомлення.

Наприклад, у двох подробицях ми просто перевіряємо, чи можемо отримати всі дописи за допомогою мови.

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

#### Тест для невдач

Важливою концепцією у тестуванні одиниць є "тестивальна невдача," де ви встановите, що ваш код зазнає невдачі у тому, чого ви очікуєте від нього.

В тестах нижче ми спочатку перевіряємо, чи працює наш код, як і очікувалося. Потім ми перевіряємо, що якщо ми просимо більше сторінок, ніж маємо, то отримуємо порожній результат (а не помилку).

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

## Включення

Це простий початок нашого тестування одиниць. В наступному полі ми додамо тестування для більшої кількості послуг і кінцевих пунктів. Ми також розглянемо, як ми можемо перевірити кінцеві точки за допомогою тестування інтеграції.