# إطار الهيئة المضافة للوظائف المدرجة في القائمة (الجزء 1، إنشاء قاعدة البيانات)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-11TT04: 53</datetime>

إربطوا لأن هذا سيكون طويلاً جداً!

يمكنك أن ترى الأجزاء 2 و3 [هنا هنا](/blog/addingentityframeworkforblogpostspt2) وقد عقد مؤتمراً بشأن [هنا هنا](/blog/addingentityframeworkforblogpostspt3).

## أولاً

بينما كنت سعيداً بنهجي القائم على ملفي في التدوين، كخصوصية قررت الانتقال إلى استخدام Postgres لتخزين مقالات وتعليقات المدونات. في هذا المقال سأعرض كيف يتم ذلك جنبا إلى جنب مع بعض النصائح والحيل لقد التقطت على طول الطريق.

[رابعاً -

## إنشاء قاعدة البيانات

قاعدة البيانات هي قاعدة بيانات مجانية مع بعض الميزات العظيمة. أنا مستخدم خادم SQL منذ وقت طويل (حتى أنني أجريت حلقات عمل الأداء في مايكروسوفت قبل بضع سنوات) ولكن Postgres هو بديل كبير. وهي حرة، مفتوحة المصدر، ولها مجتمع كبير؛ وPGadmin، للأداة لإدارتها هو الرأس والكتف فوق SQL استوديو إدارة الخوادم.

للبدء، ستحتاج لتثبيت PGADmin و PGADMIN. يمكنك أن تجعله إما كخدمة نوافذ أو يمكنك استخدام (دوكر) كما عرضته في ورقة سابقة على [مُصد مُصد مُصد مُكر](/blog/dockercomposedevdeps).

## النفقات الممولة من الموارد الأساسية

في هذا المقال سأستخدم الرمز الأول في EF Coure، وبهذه الطريقة يمكنك إدارة قاعدة بياناتك بالكامل من خلال الرمز. يمكنك بطبيعة الحال إنشاء قاعدة البيانات يدوياً واستخدام EF Corre لتنزيل النماذج. أو بطبيعة الحال استخدام Daper أو أداة أخرى و كتابة SQL الخاص بك عن طريق اليد (أو مع نهج MicroORM).

أول شيء عليك القيام به هو تثبيت حزم EF الأساسية NuGet. هنا أنا استخدام:

- ميكروسوفت ميكروسوفت. EntityFrameframeframedor core - الحزمة الأساسية EF
- تصميم - هذه ضرورية لأدوات EF الأساسية للعمل
- Npgsql. EentityFrameworkCore. PoststgreSQL - المزوّد Postgrs لـ EF

يمكنك تثبيت هذه الحزم باستخدام مدير حزمة نوت أو Dotnet CLI.

بعد ذلك نحن بحاجة إلى التفكير في نماذج لأجسام قاعدة البيانات؛ هذه منفصلة عن قوالب العرض التي تستخدم لنقل البيانات إلى وجهات النظر. سأستخدم نموذجاً بسيطاً لكتابات وتعليقات المدونات.

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

ملاحظة أنني قمت بتزيين هذه مع بعض الخصائص

```csharp
 [Key]
 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
```

هؤلاء يَتْركونَ EF أساسي يَعْرفونَ بأنّ حقلَ Id هو المفتاح الرئيسي وأنّه يَجِبُ أَنْ يُولّدَ مِن قِبل قاعدةِ البيانات.

لدي أيضاً الفئة

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

لغة

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

والتعليقات والتعليقات

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

سوف ترون أني أشير إلى المدونات في التعليقات، والجمع بين التعليقات والفئات في B;ogPost. وهذه خصائص ملاحية، وهي الطريقة التي يعرف بها EF Cour كيفية الانضمام إلى الجداول معا.

## إعداد النص DbCon contr

في فئة الـ DbConcing ستحتاج إلى تعريف الجداول والعلاقات. هذا هو اللغم:

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
في طريقة التهيئة الآلية، أقوم بتعريف العلاقات بين الجداول. لقد استخدمت المؤشر القياسي الفائق لتحديد العلاقات بين الجداول. هذا أكثر فضولاً قليلاً من استخدام البيانات الشروح ولكن أجده أكثر قابلية للقراءة.

يمكنك أن ترى أنني وضعت بعض الفهارس على طاولة بليوست. هذا للمساعدة في الأداء عند الاستفسار عن قاعدة البيانات، يجب عليك اختيار المؤشرات بناءً على كيفية إشارتك إلى البيانات. في هذه الحالة الحشيش، والرصاصة، والتاريخ المنشور واللغة هي كل المجالات التي سأتساءل عنها.

### إنشاء

الآن لدينا نماذجنا و DbConstructure نَحتاجُ لصَرْقه في DB. ممارستي المعتادة هي إضافة طرق التمديد، وهذا يساعد على إبقاء كل شيء أكثر تنظيماً:

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

هنا أقوم بإنشاء قاعدة البيانات المتصلة ومن ثم إدارة الهجرة. أنا أيضاً أتصل بطريقة لكتابة قاعدة البيانات (في حالتي أنا ما زلت أستخدم النهج القائم على الملفات لذا أحتاج إلى ملء قاعدة البيانات مع الوظائف الموجودة).

مُسند الـ مُسند نص نص إلى شيء مثل هذا:

```json
 "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Mostlylucid;port=5432;Username=postgres;Password=<PASSWORD>;"
  },
```

استخدام نهج التمديد يعني أن ملف برنامجي لطيف ونظيف:

```csharp
services.SetupEntityFramework(config.GetConnectionString("DefaultConnection") ??
                              throw new Exception("No Connection String"));

//Then later in the app section

await app.InitializeDatabase();
```

والفرع الوارد أدناه مسؤول عن إدارة الهجرة وعن إنشاء قاعدة البيانات بالفعل. الـ `MigrateAsync` ستنشئ الطريقة قاعدة البيانات إذا لم تكن موجودة و تدير أي هجرة مطلوبة. هذه طريقة عظيمة لإبقاء قاعدة بياناتك متزامنة مع نماذجك

```csharp
     await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
```

## 

بمجرد أن يكون لديك كل هذا الإعداد تحتاج إلى خلق الهجرة الأولية الخاصة بك. هذه لمحة عن الحالة الراهنة لنماذجك وسوف تستخدم لإنشاء قاعدة البيانات. يمكنك القيام بهذا باستخدام CLI (انظر [هنا هنا](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) لـ تفاصيل عن تثبيت dotnet ef أداة إذا لزم الأمر:

```bash
dotnet ef migrations add InitialCreate
```

سينشئ هذا المجلد في مشروعك مع ملفات الإزاحة. ثم يمكنك تطبيق الانتقال إلى قاعدة البيانات باستخدام:

```bash
dotnet ef database update
```

هذا سينشئ قاعدة البيانات والجداول لك.