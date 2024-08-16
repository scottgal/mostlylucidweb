# 添加博客文章实体框架(第一部分,建立数据库)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-111T04:53</datetime>

困在里面,因为这将是一个很长的!

可见第2和第3部分 [在这里](/blog/addingentityframeworkforblogpostspt2) 和 [在这里](/blog/addingentityframeworkforblogpostspt3).

## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

我决定改用Postgres来储存博客文章与评论。 我将在这篇文章中展示我是如何完成这些的, 以及我一路学到的一些小把戏和小把戏。

[技选委

## 建立数据库

Postgres是一个免费的数据库,具有一些伟大的特点。 我是一个长期的 SQL 服务器用户(我甚至几年前在微软公司举办了性能讲习班), 但Postgres是一个很好的替代方案。 它是自由的开放源码,并且拥有一个伟大的社区; PGADmin, 用来管理它的工具是 SQL 服务器管理工作室之上的头部和肩膀。

要开始,你需要安装 Postgres和PGADmin。 您可以设置它作为窗口服务, 或者使用 Docker, 正如我在前一篇文章中展示的 。 [嵌嵌入器](/blog/dockercomposedevdeps).

## EF 核心核心

在这个职位上,我将使用EF核心的代码一, 这样你就可以完全通过代码管理你的数据库。 当然,您可以手动建立数据库,并使用 EF Core 来支撑模型。 或者当然使用 Dapper 或其他工具, 并用手写您的 SQL (或使用 MicroORM 方法) 。

你要做的第一件事就是安装 EF Core Nuget 软件包。 我在这里使用:

- 微软. 实体FrameworkCore - 核心EF软件包
- 微软. Entity FrameworkCorore. 设计 - EF核心工具有效运行需要此功能
- Npgsql.EF核心邮政gres提供商 Npgsql. Entity FrameworkCore.PostgreSQL

您可以使用 NuGet 软件包管理器或点网 CLI 安装这些软件包 。

接下来,我们需要考虑数据库对象的模型;这些模型不同于用于将数据传送到视图的ViewModels。 我将使用一个简单的博客文章和评论模式。

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

你会看到我指的是评论中的博客Post, 以及B;ogPost中的评论和分类集。 这些是导航特性,是EF核心如何知道如何将表格合在一起的。

## 设置 DbContext

在DbContext类中 你需要定义表格 和关系。 这是我的:

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
在“OnModelCreate”方法中,我定义了各表格之间的关系。 我用流利的API来定义表格之间的关系 这比使用数据说明要多一点,但我发现它更易读。

你可以看到我在BlogPost桌上设置了几张索引。 这是用来帮助查询数据库时的性能; 您应该根据您如何查询数据来选择指数 。 在此情况下,哈希、鼻涕虫、公布日期和语言都是我将要查询的领域。

### 设置设置设置设置设置设置设置

现在我们有我们的模型和DbContext 设置,我们需要把它连接到 DB。 我通常的做法是增加推广方法, 这有助于让一切组织得更井井有条:

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

在这里,我设置了数据库连接 并运行迁移。 我也呼吁一种方法来填充数据库(我的情况是,我仍在使用基于文件的方法,所以我需要用现有职位填充数据库)。

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

下一节负责管理移徙工作,并实际建立数据库。 缩略 `MigrateAsync` 方法将创建数据库, 如果它不存在, 并运行任何所需的迁移 。 这是保持数据库与模型同步的好方法。

```csharp
     await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
```

## 移徙移民

一旦您有了所有这些设置, 您需要创建您的初始迁移 。 这是您模型当前状态的快照, 将用于创建数据库 。 您可以使用 dotnet CTL( 参见 [在这里](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) 需要时安装 dotnet ef 工具的详细信息 :

```bash
dotnet ef migrations add InitialCreate
```

这将在您的工程中创建一个文件夹, 并使用迁移文件 。 然后,您可以使用下列方法将迁移应用到数据库:

```bash
dotnet ef database update
```

这将为您创建数据库和表格 。