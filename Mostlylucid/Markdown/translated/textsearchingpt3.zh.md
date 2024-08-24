# 全文搜索( Pt 3 - 使用 ASP. NET 核心打开搜索)

<!--category-- OpenSearch, ASP.NET -->
<datetime class="hidden">2024-08-24T06:40</datetime>

## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

在本系列的前面部分,我们介绍了全文搜索的概念,以及如何利用它来在数据库中搜索文本。 本部分将介绍如何使用ASP.NET核心的 OpenSearch。

前几部分 :

- [使用 Postgres 搜索完整文本](/blog/textsearchingpt1)
- [带有海报的搜索框](/blog/textsearchingpt11)
- [OpenSearch 介绍介绍](/blog/textsearchingpt2)

在这部分,我们将报道如何开始使用你 与ASP.NET核心的新光亮的 OpenSearch 实例。

[技选委

## 设置设置设置设置设置设置设置

一旦我们建立并运行了开放搜索实验 我们就可以开始与它互动 我们将使用 [开放搜索客户端](https://opensearch.org/docs/latest/clients/OSC-dot-net/) 用于.NET。
我们先在设置延长期中 设置客户

```csharp
    var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));
```

这为客户设置了终点和证书。 我们还启用调试模式 这样我们就能看清情况了 此外,由于我们没有使用真实的SSL证书,我们禁用证书验证(在生产时不要这样做)。

## 索引编制数据

OpenSearch的核心概念是索引。 将索引想象成数据库表格; 它就是存储您所有数据的地方。

为了做到这一点,我们将使用 [开放搜索客户端](https://opensearch.org/docs/latest/clients/OSC-dot-net/) 用于.NET。 您可以通过 NuGet 安装此项 :

你会注意到那里有两个 - Opensearch.Net 和 Opensearch. Client. 第一个是连接管理等低层次的东西,第二个是高层次的东西,比如索引和搜索。

现在我们已经安装了它, 我们可以开始看索引数据。

创建索引是半向向偏移的 。 你只要定义你的索引应该看起来像什么 然后创造它。
在下面的代码中,我们可以看到我们的索引模型(该博客数据库模型的简化版)。
然后,我们对这一模型的每个领域确定它是什么类型(文字、日期、关键字等)以及使用什么分析器。

该类型很重要,因为它界定了数据如何储存和如何搜索。 例如,对“文本”字段进行了分析和象征性化,“关键字”字段不是。 所以,您会期望查找一个关键字字段, 精确地说, 因为它是存储的, 但是一个文本字段, 您可以搜索文本的部分 。

在此在此分类实际上是一个字符串[但关键字类型能理解如何正确处理它们 。

```csharp
   public async Task CreateIndex(string language)
    {
        var languageName = language.ConvertCodeToLanguageName();
        var indexName = GetBlogIndexName(language);

      var response =  await client.Indices.CreateAsync(indexName, c => c
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(1)
            )
            .Map<BlogIndexModel>(m => m
                .Properties(p => p
                    .Text(t => t
                        .Name(n => n.Title)
                        .Analyzer(languageName)
                    )
                    .Text(t => t
                        .Name(n => n.Content)
                        .Analyzer(languageName)
                    )
                    .Text(t => t
                        .Name(n => n.Language)
                    )
                    .Date(t => t
                        .Name(n => n.LastUpdated)
                    )
                    .Date(t => t
                        .Name(n => n.Published)
                    )
                    .Date(t => t
                        .Name(n => n.LastUpdated)
                    )
                    .Keyword(t => t
                        .Name(n => n.Id)
                    )
                    .Keyword(t=>t
                        .Name(n=>n.Slug)
                    )
                    .Keyword(t=>t
                        .Name(n=>n.Hash)
                    )
                    .Keyword(t => t
                        .Name(n => n.Categories)
                    )
                )
            )
        );
        
        if (!response.IsValid)
        {
           logger.LogError("Failed to create index {IndexName}: {Error}", indexName, response.DebugInformation);
        }
    }
```

## 向索引添加项目

一旦我们建立了我们的索引来增加其项目,我们需要在这个索引中增加项目。 当我们在这里添加一个BUNCH时, 我们使用一个大容量插入的方法。

你可以看到,我们最初使用的方法 被称为`GetExistingPosts` 返回已经在索引中的所有员额。 然后我们按语言分组文章, 并过滤“ uk” 语言(因为我们不想索引, 因为它需要额外的插件, 然后,我们筛选出指数中已经列出的任何职位。
我们用散列和代号来确认某个职位是否已经被列入索引。

```csharp
    public async Task AddPostsToIndex(IEnumerable<BlogIndexModel> posts)
    {
        var existingPosts = await GetExistingPosts();
        var langPosts = posts.GroupBy(p => p.Language);
        langPosts=langPosts.Where(p => p.Key!="uk");
        langPosts = langPosts.Where(p =>
            p.Any(post => !existingPosts.Any(existing => existing.Id == post.Id && existing.Hash == post.Hash)));
        
        foreach (var blogIndexModels in langPosts)
        {
            
            var language = blogIndexModels.Key;
            var indexName = GetBlogIndexName(language);
            if(!await IndexExists(language))
            {
                await CreateIndex(language);
            }
            
            var bulkRequest = new BulkRequest(indexName)
            {
                Operations = new BulkOperationsCollection<IBulkOperation>(blogIndexModels.ToList()
                    .Select(p => new BulkIndexOperation<BlogIndexModel>(p))
                    .ToList()),
                Refresh = Refresh.True,
                ErrorTrace = true,
                RequestConfiguration = new RequestConfiguration
                {
                    MaxRetries = 3
                }
            };

            var bulkResponse = await client.BulkAsync(bulkRequest);
            if (!bulkResponse.IsValid)
            {
                logger.LogError("Failed to add posts to index {IndexName}: {Error}", indexName, bulkResponse.DebugInformation);
            }
            
        }
    }
```

一旦我们过滤了现有的文章 和我们缺失的分析器 我们就可以创建一个新的索引(根据名字, 在我的例子中,"大多是Lulucid-blog-blog-<language>" ),然后提出批量请求。 这一大宗请求是按指数进行的业务汇编。
这比每个项目一个一个地加一个更有效率。

你会看到,在 `BulkRequest` 我们设置了 `Refresh` 财产至 `true`.. 这意味着在批量插入完整之后,索引要刷新。 这不是真的有必要的,但它对调试有用。

## 搜索索引

测试这里实际所创造的东西的一个好方法 就是进入OpenSearch Dashboards的Dev工具 并运行搜索查询。

```json
GET /mostlylucid-blog-*
{}
```

此查询将返回所有与模式匹配的索引 `mostlylucid-blog-*`.. (因此到目前为止我们所有的指数)

```json
{
  "mostlylucid-blog-ar": {
    "aliases": {},
    "mappings": {
      "properties": {
        "categories": {
          "type": "keyword"
        },
        "content": {
          "type": "text",
          "analyzer": "arabic"
        },
        "hash": {
          "type": "keyword"
        },
        "id": {
          "type": "keyword"
        },
        "language": {
          "type": "text"
        },
        "lastUpdated": {
          "type": "date"
        },
        "published": {
          "type": "date"
        },
        "slug": {
          "type": "keyword"
        },
        "title": {
          "type": "text",
          "analyzer": "arabic"
        }
      }
    },
    "settings": {
      "index": {
        "replication": {
          "type": "DOCUMENT"
..MANY MORE
```

在 OpenSearch Dashboards 中的 Dev 工具是测试您询问的好方法, 在您将其输入代码之前。

![Dev 工具](devtools.png?width=900&quality=25)

## 搜索索引

现在我们可以开始搜索索引了。 我们可以使用 `Search` 使用客户端方法进行此操作 。
这是OpenSearch的真正力量出现的地方 它的字面上 [数十种不同种类的查询](https://opensearch.org/docs/latest/query-dsl/) 您可以搜索您的数据。 从简单的关键字搜索到复杂的“神经”搜索

```csharp
    public async Task<List<BlogIndexModel>> GetSearchResults(string language, string query, int page = 1, int pageSize = 10)
    {
        var indexName = GetBlogIndexName(language);
        var searchResponse = await client.SearchAsync<BlogIndexModel>(s => s
                .Index(indexName)  // Match index pattern
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .MultiMatch(mm => mm
                                .Query(query)
                                .Fields(f => f
                                    .Field(p => p.Title, boost: 2.0) 
                                    .Field(p => p.Categories, boost: 1.5) 
                                    .Field(p => p.Content)
                                )
                                .Type(TextQueryType.BestFields)
                                .Fuzziness(Fuzziness.Auto)
                            )
                        )
                    )
                )
                .Skip((page -1) * pageSize)  // Skip the first n results (adjust as needed)
                .Size(pageSize)  // Limit the number of results (adjust as needed)
        );

        if(!searchResponse.IsValid)
        {
            logger.LogError("Failed to search index {IndexName}: {Error}", indexName, searchResponse.DebugInformation);
            return new List<BlogIndexModel>();
        }
        return searchResponse.Documents.ToList();
    }

```

### 查询描述

此方法, `GetSearchResults`用于查询特定的 OpenSearch 索引以检索博客文章。 这需要三个参数: `language`, `query`和页码参数 `page` 和 `pageSize`.. 这就是它的作用:

1. **指数选择**:
   
   - 它使用 `GetBlogIndexName` 基于所提供语言的方法。 索引是按语言动态选择的。

2. **搜索查询**:
   
   - 查询使用 a `Bool` 与 `Must` 条款,以确保结果符合某些标准。
   - 内心深处 `Must` 条款,a `MultiMatch` 查询用于在多个字段中搜索(`Title`, `Categories`, 和 `Content`).
     - **推 推 推 动**: 变化: `Title` 字段的加速度 `2.0`使它在搜索中更加重要, `Categories`  `1.5`.. 这意味着查找查询出现在标题或类别中的文档排位将更高。
     - **查询类型**:它使用 `BestFields`,试图为查询找到最佳匹配字段。
     - **模糊**: 变化: `Fuzziness.Auto` 参数允许近似匹配( 例如, 处理小打字) 。

3. **铺铺垫**:
   
   - 缩略 `Skip` 方法跳过第一个 `n` 取决于页数,根据页数计算结果 `(page - 1) * pageSize`.. 这有助于通过了解的结果。
   - 缩略 `Size` 方法限制返回指定文件的数量 `pageSize`.

4. **错误处理错误处理**:
   
   - 如果查询失败,会登录错误并返回空列表。

5. **结果成果成果成果成果成果成果成果成果成果成果**:
   
   - 方法返回列表 `BlogIndexModel` 符合搜索条件的文档。

所以,你可以看到,我们可以在如何搜索数据上 采取超级灵活的方式。 我们可以搜索特定的字段, 我们可以提升某些字段, 甚至可以搜索多个索引 。

BIG的一个优势是,我们可以支持多种语言的轻松度。 我们为每种语言制定了不同的索引,并允许在该索引中进行搜索。 这意味着我们可以对每种语言使用正确的分析器,取得最佳结果。

## 新建搜索 API

与我们在本系列前半部分所看到的搜索API相比,我们可以通过使用 OpenSearch 大大简化搜索过程。 我们可以把文字输入到这个查询 并获得伟大的结果回来。

```csharp
   [HttpGet]
    [Route("osearch/{query}")]
   [ValidateAntiForgeryToken]
    public async Task<JsonHttpResult<List<SearchResults>>> OpenSearch(string query, string language = MarkdownBaseService.EnglishLanguage)
    {
        var results = await indexService.GetSearchResults(language, query);
        
        var host = Request.Host.Value;
        var output = results.Select(x => new SearchResults(x.Title.Trim(), x.Slug, @Url.ActionLink("Show", "Blog", new{ x.Slug}, protocol:"https", host:host) )).ToList();
        return TypedResults.Json(output);
    }
```

正如你可以看到的,我们拥有索引中所需的全部数据 来回报结果。 我们可以用这个来生成博客文章的 URL 。 这样就把数据库的负荷卸下来了 搜索过程就快得多了

## 在结论结论中

以与我们的OpenSearch事件互动。