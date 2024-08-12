# 添加博客文章实体框架(第一部分,建立数据库)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-111T04:53</datetime>

困在里面,因为这将是一个很长的!

## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

虽然我对基于文件的博客方法很满意, 我决定改用Postgres来储存博客文章和评论。

[技选委

## 建立数据库

Postgres 是一个免费数据库, 具有一些伟大的功能。 我是一个长期的 SQL 服务器用户( 我甚至几年前在微软公司举办了性能讲习班 ), 但 Postgres 是一个很棒的替代方案。 它是一个自由、 开放的源码, 拥有一个伟大的社区; PGAdmin 管理它的工具是超越 SQL 服务器管理工作室的正面和肩膀。

启动前, 您需要安装 Postgres 和 PGAdmin 。 您可以设置它作为窗口服务, 或者使用 Docker, 正如我在前一篇文章中介绍的那样 。[嵌嵌入器](/blog/dockercomposedevdeps).

## EF 核心核心

在此文章中, 我将使用 EF Core 中的代码第一, 这样您就可以完全通过代码管理您的数据库。 您当然可以手动建立数据库, 并使用 EF Core 来筛选模型。 或者使用 Dapper 或其他工具, 并用手写 SQL (或使用 MicroORM 方法) 。

首先你要做的是安装 EF Core Nuget 软件包。这里我使用:

- 微软. 实体FrameworkCore - 核心EF软件包
- 微软. Entity FrameworkCorore. 设计 - EF核心工具有效运行需要此功能
- Npgsql.EF核心邮政gres提供商 Npgsql. Entity FrameworkCore.PostgreSQL

您可以使用 NuGet 软件包管理器或点网 CLI 安装这些软件包 。

接下来我们需要考虑数据库对象的模型; 这些与用于将数据传送到视图的 View 模型不同。 我将使用一个简单的博客文章和评论模式 。

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

请注意,我已经装饰了这些 与几个属性

```csharp
 [Key]
 [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
```

让EF核心知道,ID字段是主要关键,应当由数据库生成。

我也有类别

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

语言语言语言语言语言

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

以及评论和评论

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

您会看到我指的是“评论中的BlogPost”和“B:ogPost”中的评论和分类集。这些是导航属性,EF Core知道如何将表格合并在一起。

## 设置 DbContext

在DbContext课上 你需要定义表格和关系

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
在 OnModelCrection 方法中, 我定义了表格之间的关系。 我使用流畅的 API 来定义表格之间的关系。 这比使用数据说明要多一点动词, 但我发现它更容易读取 。

您可以看到我在 BlogPost 表格上设置了几个索引。 这是用来帮助查询数据库时的性能; 您应该根据您如何查询数据来选择指数。 在这种情况下, 散列、 散列、 发布日期和语言都是我将要查询的字段 。

### 设置设置设置设置设置设置设置

现在,我们有了模型和DbContext, 我们需要把它连接到 DB。 我通常的做法就是添加扩展方法, 这有助于让一切更有条理:

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

在此我设置了数据库连接, 然后运行迁移 。 同时我呼吁一种输入数据库的方法( 在我的案例中, 我仍在使用基于文件的方法, 所以我需要将数据库以现有位置填充 ) 。

您的连接字符串将看起来类似 :

```json
 "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=Mostlylucid;port=5432;Username=postgres;Password=<PASSWORD>;"
  },
```

使用扩展方法意味着我的程序. cs 文件是干净干净的:

```csharp
services.SetupEntityFramework(config.GetConnectionString("DefaultConnection") ??
                              throw new Exception("No Connection String"));

//Then later in the app section

await app.InitializeDatabase();
```

下一节负责管理迁移并实际建立数据库。`MigrateAsync`如果数据库不存在, 将会创建数据库, 并运行任何需要的迁移 。 这是将数据库与模型同步的好方法 。

```csharp
     await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
```

## 移徙移民

一旦您设置了所有这些设置, 您需要创建您初始的迁移 。 这是您模型当前状态的快照, 并将用于创建数据库 。 您可以使用点网 CLI( 参见) 来创建数据库 。[在这里](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)需要时安装 dotnet ef 工具的详细信息 :

```bash
dotnet ef migrations add InitialCreate
```

这将在您的项目中创建一个文件夹, 包含迁移文件。 然后您可以使用 :

```bash
dotnet ef database update
```

这将为您创建数据库和表格 。