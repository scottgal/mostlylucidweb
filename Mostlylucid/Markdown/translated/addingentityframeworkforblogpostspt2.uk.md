# Додавання блоку сутностей для дописів блогу (Part 2)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024- 08- 15T18: 00</datetime>

Ви можете знайти всі вихідні коди дописів блогу [GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Частина 2 серії про додавання фреймів сутностей до проекту.NET Core.**
Частину 1 можна знайти [тут](/blog/addingentityframeworkforblogpostspt1).

# Вступ

На попередньому дописі ми встановили базу даних та контекст наших дописів у блогі. У цьому полі ми додамо служби для взаємодії з базою даних.

У наступній статті ми поговоримо про те, як ці служби працюють з існуючими контролерами та поглядами.

[TOC]

### Налаштування

Тепер у нас є клас розширення BlogSetup, який створює ці послуги. Це продовження від того, що ми зробили в [Частина 1](/blog/addingentityframeworkforblogpostspt1)Там, де ми встановимо базу даних і контекст.

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

This use the simple `BlogConfig` Клас, щоб визначити, в якому режимі ми знаходимося, або `File` або `Database`. На основі цього ми реєструємо потрібні послуги.

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

## Інтерфейси

Оскільки я хочу підтримувати як файл, так і базу даних у цій програмі (оскільки ні! Я використав інтерфейсний підхід, який дозволяє поміняти їх на конфігурацію.

У нас є три нові інтерфейси. `IBlogService`, `IMarkdownBlogService` і `IBlogPopulator`.

#### IBlogService

Це основний інтерфейс служби блогу. У ньому містяться методи отримання дописів, категорій та окремих дописів.

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

#### IMarkdownBlogService

Ця служба використовується `EFlogPopulatorService` після першого запуску, щоб залити базу даних дописами з файлів markdown.

```csharp
public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();
}
```

Як ви можете бачити, це досить просто і просто має два способи, `GetPages` і `LanguageList`. Ці файли використовуються для обробки файлів Markdown і отримання списку мов.

#### IBlogPopulator

Populators блогу використовуються у нашому методі налаштування, наведеному вище, щоб заповнювати базу даних або об' єкт статичного кешу (для системи, що працює з файлами) дописами.

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

Ви можете бачити, що це розширення до `WebApplication` за допомогою налаштувань, за потреби, можна запускати міграцію баз даних (яка також створює базу даних, якщо такої бази даних не існує). Після цього буде викликано налаштовані `IBlogPopulator` Служба для заповнення бази даних.

Це інтерфейс для цієї служби.

```csharp
public interface IBlogPopulator
{
    Task Populate();
}
```

### Впровадження

Досить просто, правильно? Це реалізовано в обох випадках `MarkdownBlogPopulator` і `EFBlogPopulator` Класи.

- Markdown - тут ми викликаємо `GetPages` метод і заповнення кешу.

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

- ЕФ - ось ми викликаємо `IMarkdownBlogService` щоб отримати сторінки і потім заповнити базу даних.

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

Ми розділили цю функціональність на інтерфейси, щоб зробити код більш зрозумілим і "регульованим" (як у принципах SOLID). Це дозволяє нам легко обмінюватися послугами на основі конфігурації.

# Включення

В наступному повідомленні ми розглянемо детальніше реалізація регуляторів та переглядів для використання цих послуг.