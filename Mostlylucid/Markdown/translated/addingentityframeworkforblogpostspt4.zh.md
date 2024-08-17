# 添加博客文章实体框架( Pt. (4) 4)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-17T20:00</datetime>

见各部分 [1](/blog/addingentityframeworkforblogpostspt1) 和 [2](/blog/addingentityframeworkforblogpostspt2) 和 [3](/blog/addingentityframeworkforblogpostspt3) 用于前几个步骤。

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

在前几部分中,我们讨论了如何建立数据库,我们的控制器和观点是如何结构的,以及我们的服务是如何运作的。 在这一部分,我们将详细介绍如何利用一些初步数据播种数据库,以及基于EF的服务如何运作。

和往常一样,你可以在我的GitHub上看到所有源头 [在这里](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)中,在 Mostlylucid/Blog 文件夹中。

[技选委

# 种子数据库

在前一部分中,我们讨论了我们如何 [初始化和设置服务](/blog/addingentityframeworkforblogpostspt2#setup).. 在这一部分,我们将使用一些初步数据来报道如何为数据库播种。 这是在 `EFBlogPopulator` 类。 此类的注册服务 `SetupBlog` 扩展方法 。

```csharp
    public async Task Populate()
    {
        var posts = await _markdownBlogService.GetPages();
        var languages = _markdownBlogService.LanguageList();

        var languageEntities = await EnsureLanguages(languages);
        await EnsureCategoriesAndPosts(posts, languageEntities);

        await Context.SaveChangesAsync();
    }
```

你可以看到,在 `Populate` 方法,我们拨入 `_markdownBlogService.GetPages()` 它通过我们的Makrdown文件运行 并弹出一堆 `BlogViewModels` 包含所有职位 。
然后,我们用同样的语言来对待语言。 `translated` 用于所有我们使用 EasyNMT 生成的翻译的标记文件的文件夹(见 [在这里](/blog/autotranslatingmarkdownfiles) 我们如何做这部分工作)。

## 添加语言

然后,我们呼唤我们, `EnsureLanguages` 确保所有语文都输入数据库的方法。 这是一个简单的方法,可以检查语言是否存在,如果不将其添加到数据库,也可以检查语言是否存在。

```csharp
  private async Task<List<LanguageEntity>> EnsureLanguages(Dictionary<string, List<string>> languages)
    {
        var languageList = languages.SelectMany(x => x.Value).ToList();
        var currentLanguages = await Context.Languages.Select(x => x.Name).ToListAsync();

        var languageEntities = new List<LanguageEntity>();
        var enLang = new LanguageEntity { Name =MarkdownBaseService.EnglishLanguage };

        if (!currentLanguages.Contains(MarkdownBaseService.EnglishLanguage)) Context.Languages.Add(enLang);
        languageEntities.Add(enLang);

        foreach (var language in languageList)
        {
            if (languageEntities.Any(x => x.Name == language)) continue;

            var langItem = new LanguageEntity { Name = language };

            if (!currentLanguages.Contains(language)) Context.Languages.Add(langItem);

            languageEntities.Add(langItem);
        }

        await Context.SaveChangesAsync(); // Save the languages first so we can reference them in the blog posts
        return languageEntities;
    }
```

你会看到,这是 ppretty 简单简单,只是确保 我们从标记站得到的所有语言 都在数据库里; 而正如我们具体指出, ID是自动生成的, `SaveChanges` 以确保生成代号。

### 增加类别和员额

然后,我们呼唤我们, `EnsureCategoriesAndPosts` 确保数据库中包括所有职类和员额的方法。 这要复杂一些,因为我们需要确保这些类别在数据库中,然后我们需要确保这些员额在数据库中。

```csharp
    private async Task EnsureCategoriesAndPosts(
        IEnumerable<BlogPostViewModel> posts,
        List<LanguageEntity> languageEntities)
    {
        var existingCategories = await Context.Categories.Select(x => x.Name).ToListAsync();

        var languages = languageEntities.ToDictionary(x => x.Name, x => x);
        var categories = new List<CategoryEntity>();

        foreach (var post in posts)
        {
            var currentPost =
                await PostsQuery().FirstOrDefaultAsync(x => x.Slug == post.Slug && x.LanguageEntity.Name == post.Language);
            await AddCategoriesToContext(post.Categories, existingCategories, categories);
            await AddBlogPostToContext(post, languages[post.Language], categories, currentPost);
        }
    }
```

你们可以看到,我们呼吁 `PostsQuery` 我们的 Base 类的基类方法, 这是一种简单的方法, 返回可查询的 `BlogPostEntity` 这样我们就可以查询这些职位的数据库。

```csharp
  protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);
   
```

#### 添加类别

然后,我们使你们进入火狱, `AddCategoriesToContext` 确保所有类别都输入数据库的方法。 这是一个简单的方法,可以检查该类别是否存在,如果不将其添加到数据库,则进行检查。

```csharp
    private async Task AddCategoriesToContext(
        IEnumerable<string> categoryList,
        List<string> existingCategories,
        List<CategoryEntity> categories)
    {
        foreach (var category in categoryList)
        {
            if (categories.Any(x => x.Name == category)) continue;

            var cat = new CategoryEntity { Name = category };

            if (!existingCategories.Contains(category)) await Context.Categories.AddAsync(cat);

            categories.Add(cat);
        }
    }

```

此选项再次检查该类别是否存在, 如果没有将其添加到数据库中 。

#### 添加博客文章

然后,我们使你们进入火狱, `AddBlogPostToContext` 确保所有员额都输入数据库的方法。 这有点复杂,因为我们需要确保该员额在数据库中,然后我们需要确保这些类别在数据库中。

```csharp
  private async Task AddBlogPostToContext(
        BlogPostViewModel post,
        LanguageEntity postLanguageEntity,
        List<CategoryEntity> categories,
        BlogPostEntity? currentPost)
    {
        try
        {
            var hash = post.HtmlContent.ContentHash();
            var currentCategoryNames = currentPost?.Categories.Select(x => x.Name).ToArray() ?? Array.Empty<string>();
            var categoriesChanged = false;
            if (!currentCategoryNames.All(post.Categories.Contains) ||
                !post.Categories.All(currentCategoryNames.Contains))
            {
                categoriesChanged = true;
                _logger.LogInformation("Categories have changed for post {Post}", post.Slug);
            }

            var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
            var titleChanged = currentPost?.Title != post.Title;
            if (!titleChanged && !dateChanged && hash == currentPost?.ContentHash && !categoriesChanged)
            {
                _logger.LogInformation("Post {Post} has not changed", post.Slug);
                return;
            }

            var blogPost = new BlogPostEntity
            {
                Title = post.Title,
                Slug = post.Slug,
                HtmlContent = post.HtmlContent,
                PlainTextContent = post.PlainTextContent,
                ContentHash = hash,
                PublishedDate = post.PublishedDate,
                LanguageEntity = postLanguageEntity,
                LanguageId = postLanguageEntity.Id,
                Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList()
            };


            if (currentPost != null)
            {
                _logger.LogInformation("Updating post {Post}", post.Slug);
                Context.BlogPosts.Update(blogPost);
            }
            else
            {
                _logger.LogInformation("Adding post {Post}", post.Slug);
                await Context.BlogPosts.AddAsync(blogPost);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error adding post {Post}", post.Slug);
        }
    }

```

以确保我们不会再增加没有改变的帖子。 我们检查内容、分类、日期和标题的散列。 如果有任何改动,我们在数据库中更新该职位。

需要注意的一件事是,检查“日期时间交易”是多么令人烦恼;我们必须将其转换为世界协调时,然后获得比较日期。 这是因为 `DateTimeOffset` 我们想比较一下日期。

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# 在结论结论中

现在,我们有一个完全正常运行的博客系统, 可以通过标记文件或翻译标记文件来输入。 在下一部分,我们将覆盖一个简单的服务, 我们用来显示存储在数据库中的位置。