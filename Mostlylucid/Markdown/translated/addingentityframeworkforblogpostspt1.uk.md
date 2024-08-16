# Додавання блоку сутностей для дописів блогу (Part 1, налаштування бази даних)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024- 08- 11T04: 53</datetime>

Пристебнися, бо це буде довга!

Можна побачити частини 2 і 3 [тут](/blog/addingentityframeworkforblogpostspt2) і [тут](/blog/addingentityframeworkforblogpostspt3).

## Вступ

Хоча я був задоволений своїм підходом до блогу, але, як приклад, вирішив перейти до використання Postgres для зберігання дописів та коментарів у блогах. На цьому пості я покажу, як це робиться разом з кількома підказками та трюками, які я взяв по дорозі.

[TOC]

## Налаштування бази даних

Postgres - це вільна база даних з деякими чудовими можливостями. Я вже давно користувач SQL-сервера (я навіть керував майстернями виступів у Microsoft кілька років тому), але Postgres - це чудова альтернатива. Він вільний, відкритий і має велику спільноту; і PGAdmin, для інструмента для його виконання, це голова і плечі над Studio керування сервером SQL.

Для початку вам доведеться встановити Postgres і PGAdmin. Ви можете налаштувати його або як службу для вікон, або скористатися Docker, як я показував у попередньому дописі [Панель](/blog/dockercomposedevdeps).

## Корінь EF

В этом посте я буду использовать код первое в ЭФ-корне, таким образом вы можете управлять вашей базой данных полностью кодом. Ви, звичайно ж, можете налаштувати базу даних вручну і скористатися EF Core, щоб відшліфувати моделі. Або, звичайно ж, скористайтеся Dapper або іншим інструментом і вручну запишіть ваш SQL (або з підходом MicroORM).

Перше, що вам потрібно зробити, це встановити пакунки EF Core NuGe. Тут я користуюся:

- Microsoft. EntityFrameworkCore - пакунок core EF
- Microsoft. EntityFrameworkCore. Design - Це потрібно для роботи інструментів EF Core
- Npgsql.EntityFrameworkCore.PostgreSQL - постачальник Postgres для EF Core

Ви можете встановити ці пакунки за допомогою інструменту керування пакунками NuGet або CLI dotnet.

Далі нам слід подумати про моделі об' єктів бази даних; вони відрізняються від ViewModels, які використовуються для передавання даних об' єктам. Я використовуватиму просту модель для блошиних дописів і коментарів.

```csharp
public class BlogPost
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public string Title { get; set; }
    public string Slug { get; set; }
    public string HtmlContent { get; set; }
    public string PlainTextContent { get; set; }
    public string ContentHash { get; set; }

    
    public int WordCount { get; set; }
    
    public int LanguageId { get; set; }
    public Language Language { get; set; }
    public ICollection<Comments> Comments { get; set; }
    public ICollection<Category> Categories { get; set; }
    
    public DateTimeOffset PublishedDate { get; set; }
    
}
```

Зверніть увагу, що я прикрасив їх кількома атрибутами.

```csharp
 [Key]
 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
```

Це дає змогу EF Core знати, що поле Id є основним ключем і що його має бути створено базою даних.

У мене також є категорія

```csharp
public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<BlogPost> BlogPosts { get; set; }
}
```

Мови

```csharp
public class Language
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<BlogPost> BlogPosts { get; set; }
}
```

І коментарі

```csharp
public class Comments
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Comment { get; set; }
    public string Slug { get; set; }
    public int BlogPostId { get; set; }
    public BlogPost BlogPost { get; set; } 
}
```

Ви побачите, що я посилаюсь на BlogPost в Коментарі, і ICollections of Коментарі і Категорії у B; ogPost. Це навігаційні властивості, і саме так EF Core знає, як з'єднати таблиці разом.

## Налаштування DbContext

У класі "DbContext" вам слід визначити таблиці та відносини. Ось мій.

<details>
<summary>Expand to see the full code</summary>
```csharp
public class MostlylucidDbContext : DbContext
{
    public MostlylucidDbContext(DbContextOptions<MostlylucidDbContext> contextOptions) : base(contextOptions)
    {
    }

    public DbSet<Comments> Comments { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<Category> Categories { get; set; }

    public DbSet<Language> Languages { get; set; }


    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasIndex(x => new { x.Slug, x.LanguageId });
            entity.HasIndex(x => x.ContentHash).IsUnique();
            entity.HasIndex(x => x.PublishedDate);

            entity.HasMany(b => b.Comments)
                .WithOne(c => c.BlogPost)
                .HasForeignKey(c => c.BlogPostId);

            entity.HasOne(b => b.Language)
                .WithMany(l => l.BlogPosts).HasForeignKey(x => x.LanguageId);

            entity.HasMany(b => b.Categories)
                .WithMany(c => c.BlogPosts)
                .UsingEntity<Dictionary<string, object>>(
                    "BlogPostCategory",
                    c => c.HasOne<Category>().WithMany().HasForeignKey("CategoryId"),
                    b => b.HasOne<BlogPost>().WithMany().HasForeignKey("BlogPostId")
                );
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasMany(l => l.BlogPosts)
                .WithOne(b => b.Language);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id); // Assuming Category has a primary key named Id

            entity.HasMany(c => c.BlogPosts)
                .WithMany(b => b.Categories)
                .UsingEntity<Dictionary<string, object>>(
                    "BlogPostCategory",
                    b => b.HasOne<BlogPost>().WithMany().HasForeignKey("BlogPostId"),
                    c => c.HasOne<Category>().WithMany().HasForeignKey("CategoryId")
                );
        });
    }
}
```

</details>
У методі "ОnModelCreating" я визначаю взаємозв'язки між таблицями. Я использовал API Fluent, чтобы определить отношения между столами. Це трохи більш докладне, ніж використання анотацій даних, але я вважаю, що це більш придатно для читання.

Як бачите, я встановив кілька індексів на столі Blog Post. Цей пункт допоможе з швидкодією під час опитування бази даних. Вам слід обрати індекси на основі способу опитування даних. У цьому випадку хеш, слимак, друкована дата і мова це всі поля, про які я буду розпитувати.

### Налаштування

Тепер у нас є наші моделі і DbContext, нам потрібно підключити їх до DB. Звичайна практика полягає у додаванні методів розширення, це допомагає зберігати все більш організованим:

```csharp
public static class Setup
{
    public static void SetupEntityFramework(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MostlylucidDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    public static async Task InitializeDatabase(this WebApplication app)
    {
        try
        {
            await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
            
            var blogService = scope.ServiceProvider.GetRequiredService<IBlogService>();
            await blogService.Populate();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Failed to migrate database");
        }        
    }
}
```

Тут я заснував зв'язок з базою даних, а потім запустив міграцію. Я також дзвоню до методу заповнення бази даних (у моєму випадку я все ще використовую підхід, оснований на файлах, тому мені потрібно заселити базу даних на існуючі дописи).

Ваш рядок з' єднання виглядатиме приблизно так:

```json
 "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Mostlylucid;port=5432;Username=postgres;Password=<PASSWORD>;"
  },
```

Використання підходу до суфіксів означає, що мій файл Program. cs є чудовим і чистим:

```csharp
services.SetupEntityFramework(config.GetConnectionString("DefaultConnection") ??
                              throw new Exception("No Connection String"));

//Then later in the app section

await app.InitializeDatabase();
```

Розділ, розташований нижче, відповідає за виконання міграції і налаштування бази даних. The `MigrateAsync` метод створить базу даних, якщо такої бази даних не існує, і запустить потрібні міграції. Це чудовий спосіб підтримувати синхронізацію вашої бази даних з вашими моделями.

```csharp
     await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
```

## Міграції

Після того, як ви все це налаштуєте, вам слід створити початкову міграцію. Це знімок поточного стану ваших моделей і буде використано для створення бази даних. Ви можете зробити це за допомогою CLI dotnet (див. [тут](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) для подробиць щодо встановлення інструменту з' єднання з dotnet за потреби):

```bash
dotnet ef migrations add InitialCreate
```

За допомогою цього пункту можна створити теку у вашому проекті з файлами міграції. Після цього ви можете застосувати міграцію до бази даних за допомогою:

```bash
dotnet ef database update
```

Це створить для вас базу даних і таблиці.