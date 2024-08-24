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
        var languages = languageEntities.ToDictionary(x => x.Name, x => x);
        var currentPosts = await PostsQuery().ToListAsync();
        foreach (var post in posts)
        {
            var existingCategories = Context.Categories.Local.ToList();
            var currentPost =
                currentPosts.FirstOrDefault(x => x.Slug == post.Slug && x.LanguageEntity.Name == post.Language);
            await AddCategoriesToContext(post.Categories, existingCategories);
            existingCategories = Context.Categories.Local.ToList();
            await AddBlogPostToContext(post, languages[post.Language], existingCategories, currentPost);
        }
    }
```

Тут ми використовуємо Context. Categories. Local для стеження за категоріями, які було додано до контексту (вони зберігаються у базі даних під час `SaveAsync` виклик).
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
        List<CategoryEntity> existingCategories)
    {
        foreach (var category in categoryList)
        {
            if (existingCategories.Any(x => x.Name == category)) continue;

            var cat = new CategoryEntity { Name = category };

             await Context.Categories.AddAsync(cat);
        }
    }

```

Знову ж таки, перевіряє існування категорії і чи не додає її до бази даних.

#### Додавання дописів блогу

Потім ми викликаємо `AddBlogPostToContext` метод, який потім викликається `EFBaseService` щоб зберегти допис до бази даних.

```csharp
    private async Task AddBlogPostToContext(
        BlogPostViewModel post,
        LanguageEntity postLanguageEntity,
        List<CategoryEntity> categories,
        BlogPostEntity? currentPost)
    {
        await SavePost(post, currentPost, categories, new List<LanguageEntity> { postLanguageEntity });
    }
```

Ми робимо це, викликаючи `SavePost` метод, який є методом збереження допису до бази даних. Цей метод є трохи складним, оскільки має перевіряти, чи змінився допис і чи оновлюється такий допис у базі даних.

```csharp

   public async Task<BlogPostEntity?> SavePost(BlogPostViewModel post, BlogPostEntity? currentPost =null ,
        List<CategoryEntity>? categories = null,
        List<LanguageEntity>? languages = null)
    {
        if (languages == null)
            languages = await Context.Languages.ToListAsync();

    var postLanguageEntity = languages.FirstOrDefault(x => x.Name == post.Language);
        if (postLanguageEntity == null)
        {
            Logger.LogError("Language {Language} not found", post.Language);
            return null;
        }
        categories ??= await Context.Categories.Where(x => post.Categories.Contains(x.Name)).ToListAsync();
         currentPost ??= await PostsQuery().Where(x=>x.Slug == post.Slug).FirstOrDefaultAsync();
        try
        {
            var hash = post.HtmlContent.ContentHash();
            var currentCategoryNames = currentPost?.Categories.Select(x => x.Name).ToArray() ?? Array.Empty<string>();
            var categoriesChanged = false;
            if (!currentCategoryNames.All(post.Categories.Contains) ||
                !post.Categories.All(currentCategoryNames.Contains))
            {
                categoriesChanged = true;
                Logger.LogInformation("Categories have changed for post {Post}", post.Slug);
            }

            var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
            var titleChanged = currentPost?.Title != post.Title;
            if (!titleChanged && !dateChanged && hash == currentPost?.ContentHash && !categoriesChanged)
            {
                Logger.LogInformation("Post {Post} has not changed", post.Slug);
                return currentPost;
            }

            
            var blogPost = currentPost ?? new BlogPostEntity();
            
            blogPost.Title = post.Title;
            blogPost.Slug = post.Slug;
            blogPost.OriginalMarkdown = post.OriginalMarkdown;
            blogPost.HtmlContent = post.HtmlContent;
            blogPost.PlainTextContent = post.PlainTextContent;
            blogPost.ContentHash = hash;
            blogPost.PublishedDate = post.PublishedDate;
            blogPost.LanguageEntity = postLanguageEntity;
            blogPost.Categories = categories.Where(x => post.Categories.Contains(x.Name)).ToList();

            if (currentPost != null)
            {
                Logger.LogInformation("Updating post {Post}", post.Slug);
                Context.BlogPosts.Update(blogPost); // Update the existing post
            }
            else
            {
                Logger.LogInformation("Adding new post {Post}", post.Slug);
                Context.BlogPosts.Add(blogPost); // Add a new post
            }
            return blogPost;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error adding post {Post}", post.Slug);
        }

        return null;
    }

```

Як бачите, це має значення LOT для виявлення змін, щоб переконатися, що ми не додаємо дописи, які не змінилися. Ми перевіряємо хеш змісту, категорії, дату і назву. Если что-то из них изменилось, мы обновим пост в базе данных.

Одна річ, яку варто помітити, це як дратує перевірка DateTimeoffset; нам потрібно перетворити його на UTC, а потім отримати дату, щоб порівняти його. Це тому, що `DateTimeOffset` має компонент часу і ми хочемо порівняти лише дату.

```csharp
var dateChanged = currentPost?.PublishedDate.UtcDateTime.Date != post.PublishedDate.ToUniversalTime().Date;
```

# Включення

Тепер у нас є повноцінна робоча система блогів, яка може бути заповнена файлами markdown і перекладена на файли markdown. У наступній частині ми охопимо просту Службу, яку ми використовуємо для показу дописів, збережених у базі даних.