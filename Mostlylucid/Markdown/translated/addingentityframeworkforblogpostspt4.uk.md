# Додавання кадрів сутності для дописів блогу (Pt. 4)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024- 08- 17T20: 00</datetime>

Див. частини [1](/blog/addingentityframeworkforblogpostspt1) і [2](/blog/addingentityframeworkforblogpostspt2) і [3](/blog/addingentityframeworkforblogpostspt3) за попередніми кроками.

# Вступ

У попередніх частинах ми обговорювали, як створити базу даних, як структуруються наші регулятори та погляди, і як працюють наші служби. У цій частині ми будемо розглядати подробиці про те, як засіяти базу даних деякими початковими даними і як працюють сервіси ОЕС.

Як завжди, ви можете бачити все джерело для цього на моєму GitHub [тут](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog), у теці calcuid/Blog.

[TOC]

# Поширення бази даних

В попередній частині ми розглянули, як ми [ініціалізація і налаштування служб](/blog/addingentityframeworkforblogpostspt2#setup). У цій частині ми опишемо, як створити базу даних деякими початковими даними. Це робиться в `EFBlogPopulator` Клас. Цей клас зареєстровано як службу `SetupBlog` метод розширення.

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

Це видно у `Populate` метод, який ми називаємо `_markdownBlogService.GetPages()` це проходить через наші файли і заповнюється купкою файлів `BlogViewModels` містить всі дописи.
Потім ми робимо те саме для мов; це дивиться на наші `translated` тека для всіх перекладених файлів, які ми створили за допомогою EasyNMT (див. [тут](/blog/autotranslatingmarkdownfiles) про те, як ми робимо цю частину).

## Додавання мов

Потім ми кличемо нас `EnsureLanguages` метод, який гарантує, що всі мови знаходяться у базі даних. Цей простий метод перевіряє існування мови і додавання її до бази даних.

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

Ви побачите, що це дуже просто і забезпечує те, що всі мови, які ми отримали з дописів, знаходяться у базі даних; і як ми вказали, що іди створені автоматично, ми повинні `SaveChanges` щоб забезпечити створення ідентифікаторів.

### Додавання категорій і дописів

Потім ми кличемо нас `EnsureCategoriesAndPosts` метод, за допомогою якого можна вказати, що всі категорії і дописи зберігаються у базі даних. Це трохи складніше, ніж потрібно для того, щоб впевнитися, що категорії є у базі даних, а потім нам потрібно переконатися, що дописи є в базі даних.

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

Ви бачите, що ми кличемо `PostsQuery` метод нашого базового класу, який є простим методом, який повертає запит з `BlogPostEntity` щоб запитати в бази даних про дописи.

```csharp
  protected IQueryable<BlogPostEntity> PostsQuery()=>Context.BlogPosts.Include(x => x.Categories)
        .Include(x => x.LanguageEntity);
   
```

#### Додавання категорій

Потім ми викликаємо `AddCategoriesToContext` метод, який гарантує, що всі категорії знаходяться у базі даних. Цей простий метод перевіряє наявність категорії і додавання її до бази даних.

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

Знову ж таки, перевіряє існування категорії і чи не додає її до бази даних.

#### Додавання дописів блогу

Потім ми викликаємо `AddBlogPostToContext` метод, який забезпечує доступ до всіх дописів до бази даних. Це трохи складніше, ніж потрібно, щоб цей допис був у базі даних і потім нам потрібно переконатися, що категорії знаходяться в базі даних.

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

Як бачите, це має значення LOT для виявлення змін, щоб переконатися, що ми не додаємо дописи, які не змінилися. Ми перевіряємо хеш змісту, категорії, дату і назву. Если что-то из них изменилось, мы обновим пост в базе данных.

Одна річ, яку варто помітити, це як дратує перевірка DateTimeoffset; нам потрібно перетворити його на UTC, а потім отримати дату, щоб порівняти його. Це тому, що `DateTimeOffset` має компонент часу і ми хочемо порівняти лише дату.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# Включення

Тепер у нас є повноцінна робоча система блогів, яка може бути заповнена файлами markdown і перекладена на файли markdown. У наступній частині ми охопимо просту Службу, яку ми використовуємо для показу дописів, збережених у базі даних.