# 完整文本搜索 (Pt 1)

<!--category-- Postgres, Entity Framework -->
<datetime class="hidden">2024-008-20T12:40</datetime>

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

搜索内容是任何内容重的网站的关键部分。 它提高了可发现性和用户经验。 本文将报导我如何加入搜寻网站全文的全文,

[技选委

# 方法方法

有很多方法可以进行全文搜索 包括

1. 仅仅搜索记忆中的数据结构( 如列表), 这比较容易执行, 但规模不适当 。 此外,它不支持复杂的查询 没有大量的工作。
2. 使用 SQL 服务器或 Postgres 等数据库 。 虽然这确实有效,而且得到几乎所有类型数据库的支持,但它并不总是更复杂的数据结构或复杂查询的最佳解决办法;然而,这是本条将涵盖的内容。
3. 使用轻量级搜索技术 [卢塞](https://lucenenet.apache.org/) 或 SQLITE FTS 。 这是上述两种解决办法之间的中间立场。 它比搜索列表复杂得多 但比完整的数据库解决方案复杂多了 然而,执行(特别是获取数据)仍然相当复杂, 并且没有规模和完整的搜索解决方案。 事实上,许多其他搜索技术 [用Lucene在引擎盖下 ](https://www.elastic.co/search-labs/blog/elasticsearch-lucene-vector-database-gains) 这是惊人的矢量搜索能力。
4. 使用Elastic Search、OpenSearch或Azure搜索等搜索引擎。 这是最复杂和资源密集的解决办法,但也是最强大的解决办法。 它也是最可扩缩的 并且可以轻松地处理复杂的查询 我会在下周深入到 令人痛苦的深度 如何自我主机,配置和使用 OpenSearch from C#

# 用 Postgres 搜索全文本数据库

我最近改用Postgres作为数据库。 Postgres 具有全文搜索功能,功能非常强大,而且(有些)易于使用。 它也非常快,可以轻松地处理复杂的查询。

当建造高温时 `DbContext` 您可以指定哪些字段已启用全文搜索功能。

Postgres利用搜索矢量的概念实现快速、高效的全文本搜索。 搜索矢量是一个包含文档中的单词及其位置的数据结构。 基本上预先计算数据库中每行的搜索矢量,使Postgres能够非常迅速地搜索文档中的单词。
它为此使用两种特殊数据类型:

- TSVector: 一个特殊的 PostgreSQL 数据类型, 存储一个词汇列表( 把它视为文字矢量 ) 。 它是用于快速搜索的文档索引版。
- TS查询:存储搜索查询的另一个特殊数据类型,包括搜索条件和逻辑运算符(如和、或、或、非)。

此外,它还提供排序功能,使您能够根据结果与搜索查询的匹配程度排列结果。 这是非常强大的, 并允许你按相关性来决定结果。
PostgreSQL根据相关性对结果进行排名。 相关性是通过考虑检索术语相互接近等因素以及这些术语在文件中的出现频率来计算的。
函数 ts_ rank 或 ts_ rk_ cd 用于计算此排序 。

您可以阅读更多关于 Postgres 全文搜索功能的更多信息 [在这里](https://www.postgresql.org/docs/current/textsearch.html)

## 实体框架

Postgres实体框架一揽子计划 [在这里](https://www.npgsql.org/efcore/mapping/full-text-search.html?tabs=pg12%2Cv5) 为全文搜索提供强有力的支持。 它允许您指定哪些字段为完整文本索引,以及如何查询它们。

为此,我们为我们的实体增加具体的索引类型,其定义如下: `DbContext`:

```csharp
   modelBuilder.Entity<BlogPostEntity>(entity =>
        {
            entity.HasIndex(x => new { x.Slug, x.LanguageId });
            entity.HasIndex(x => x.ContentHash).IsUnique();
            entity.HasIndex(x => x.PublishedDate);

                entity.HasIndex(b => new { b.Title, b.PlainTextContent})
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("english");
  ...
```

我们在此添加一个全文索引 `Title` 和 `PlainTextContent` 区域中的 `BlogPostEntity`.. 我们还在具体说明索引应该使用 `GIN` 索引类型和 `english` 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 语言 这一点很重要,因为它告诉Postgres如何编制数据索引,用什么语言来阻止和阻止言词。

这显然是我们博客的一个问题,因为我们有多种语言。 不幸的是,现在我只是在使用 `english` 用于所有职位的语文。 这是我将来需要处理的事情 但现在已经足够有效了

我们还将增加一个索引。 `Category` 实体:

```csharp
     modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasIndex(b => b.Name).HasMethod("GIN").IsTsVectorExpressionIndex("english");;
...

```

通过此 Postgres 生成数据库中每行的搜索矢量 。 此矢量包含在 `Title` 和 `PlainTextContent` 字段。 然后我们可以使用此矢量搜索文档中的单词 。

这将转换为 SQL 中生成行搜索矢量的 to_tsverctor 函数。 然后我们可以使用 ts_ rank 函数来根据关联性排列结果的排序 。

```postgresql
SELECT to_tsvector('english', 'a fat  cat sat on a mat - it ate a fat rats');
to_tsvector
-----------------------------------------------------
'ate':9 'cat':3 'fat':2,11 'mat':7 'rat':12 'sat':4
```

应用它作为向数据库的迁移, 我们准备开始搜索。

# 搜索

## Ts Victor 指数

要搜索, 我们使用将使用 `EF.Functions.ToTsVector` 和 `EF.Functions.WebSearchToTsQuery` 函数创建搜索矢量和查询。 然后我们就可以使用 `Matches` 函数在搜索矢量中查找查询。

```csharp
  var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Matches(EF.Functions.WebSearchToTsQuery("english", query)) // Search in title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", query))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Rank(EF.Functions.WebSearchToTsQuery("english", query))) // Rank by relevance
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
       
```

EF. Functions. WebSearchToTs 查询函数生成基于通用 Web 搜索引擎语法的行查询 。

```postgresql
SELECT websearch_to_tsquery('english', '"sad cat" or "fat rat"');
       websearch_to_tsquery
-----------------------------------
 'sad' <-> 'cat' | 'fat' <-> 'rat'
```

在此示例中, 您可以看到此选项生成一个查询, 以查找文档中的“ sad cat” 或“ fat rat” 等词 。 这是一个强有力的特点,使我们能够轻松地查询复杂的问题。

如前所述,这些方法同时产生行的搜索矢量和查询。 然后我们使用 `Matches` 函数在搜索矢量中查找查询。 我们也可以使用 `Rank` 函数按关联性排列结果的排序。

正如你所看到的,这不是一个简单的查询, 但它非常强大, 使我们能够在 `Title`, `PlainTextContent` 和 `Category` 区域中的 `BlogPostEntity` 并按相关性排列这些要素的等级。

## WebAPI 网络

要使用这些( 未来) 我们可以创建一个简单的 WebAPI 端点, 以查询并返回结果 。 这是一个简单的控制器, 进行查询并返回结果 :

```csharp
[ApiController]
[Route("api/[controller]")]
public class SearchApi(MostlylucidDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<JsonHttpResult<List<SearchResults>>> Search(string query)
    {;

        var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Matches(EF.Functions.WebSearchToTsQuery("english", query)) // Search in title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", query))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Rank(EF.Functions.WebSearchToTsQuery("english", query))) // Rank by relevance
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
        
        var output = posts.Select(x => new SearchResults(x.Title.Trim(), x.Slug)).ToList();
        
        return TypedResults.Json(output);
    }

```

## 生成的列和类型

使用这些“ 简单” TsVector Indices 的另一种办法,是使用生成的柱子存储搜索矢量,然后用它进行搜索。 这是一种更为复杂的办法,但可以提高业绩。
我们在这里修改 `BlogPostEntity` 添加特殊类型的列 :

```csharp
   [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public NpgsqlTsVector SearchVector { get; set; }
```

这是生成行的搜索矢量的计算列。 然后,我们可以使用这一栏来搜索文件中的字词。

我们随后在实体定义内设置了这个索引(尚有待确认),但这也可能允许我们使用多种语文,为每个职位指定一个语文栏目。

```csharp
   entity.Property(b => b.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))", stored: true);
```

你会看到我们在这里使用 `HasComputedColumnSql` 以明确指定 PostGreSQL 函数来生成搜索矢量。 我们还具体说明,该栏目储存在数据库中。 这一点很重要,因为它告诉 Postgres 将搜索矢量存储在数据库中 。 这样我们就可以使用搜索矢量搜索文档中的单词 。

在数据库中, 这是为每行生成的, 它们是文档中的“ lexemes” 及其位置 :

```csharp
"'1992':464 '1996':468 '20':480 '200':115 '2007':426 '2009':428 '2012':88 '2015':397 '2018':370 '2020':372 '2021':288,327,329,399 '2022':196,243,245,290 '2024':156,158,198 '25':21,477,486,522 '3d':346 '6':203,256 '8':179,485 '90':120,566 'ab':282 'access':221 'accomplish':14 'achiev':118 'across':60 'adapt':579 'advanc':134 'applic':168,316,526 'apr':155,197 'architect':83,97,159 'architectur':307,337 ...
```

### 搜索

然后,我们可以使用这一栏来搜索文件中的字词。 我们可以使用 `Matches` 函数在搜索矢量中查找查询。 我们也可以使用 `Rank` 函数按关联性排列结果的排序。

```csharp
       var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                // Search using the precomputed SearchVector
                x.SearchVector.Matches(EF.Functions.ToTsQuery("english", query + ":*")) // Use precomputed SearchVector for title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                // Rank based on the precomputed SearchVector
                x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*"))) // Use precomputed SearchVector for ranking
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
```

你看这里,我们用不同的查询构建器 `EF.Functions.ToTsQuery("english", query + ":*")`  它使我们能够提供一种“头型”功能(我们可以在其中输入,例如: "猫"和得到"猫","猫","猫","猫毛虫"等等。

此外,它让我们简化主要博客主博客的海报查询,以便搜索该页面中的查询。 `SearchVector` 列内。 这是一个强大的特点,使我们能够在《世界人权宣言》和《世界人权宣言》中寻找词语。 `Title`, `PlainTextContent`.. 我们仍然使用上面显示的索引 `CategoryEntity`.

```csharp
x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) 
```

然后我们使用 `Rank` 函数根据查询按相关性排列结果。

```csharp
 x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*")))
```

这让我们可以使用以下的终点, 我们可以在开头几个字中通过, 并拿回所有符合该词的文章:

您可以查看 [API 此处行动](https://www.mostlylucid.net/swagger/index.html) 查找 `/api/SearchApi`.. (注意; 我启用了这个网站的 Swagger, 这样您就可以在操作中看到 API, 但大部分时间应该保留给 `Is Development () 。 )

![API API AIPI AIPI AIPI AIPI AIPI AIPI AIPI AIPI AIPI AIPI AIPI AIPI AIPI](searchapi.png?width=900&format=webp&quality=50)

未来我会在使用此功能的网站的搜索框中 添加一个前型特征

# 在结论结论中

你可以看到,使用Postgres和实体框架 可以获得强大的搜索功能。 然而,它有复杂和局限,我们需要说明(如语言问题)。 下一部分我会用OpenSearch 来报导我们如何做这个工作—— 开放搜索系统有一大堆的装置,但更强大,更可扩缩。