# Додавання коду за допомогою HTMX і ASP. NET з інструментом довідки TagName

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024- 08- 09T12: 50</datetime>

## Вступ

Тепер, коли я маю купу блогів, домашня сторінка ставала досить довгою, тому я вирішив додати механізм розсилки для блогів.

Це супроводжується тим, що ми додаємо повний кеш для блогових дописів, щоб зробити це швидким і ефективним сайтом.

Видите [Джерело служби блогу](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/Markdown/MarkdownBlogService.cs) Для того, як це впроваджено; це дійсно дуже просто, використовуючи IMemoryCache.

[TOC]

### Допомога TagHelp

Я вирішив використати TagHelper, щоб впровадити цей механізм. Це чудовий спосіб перебудувати логіку і зробити її реанімацією.
This using the [pagination tagheler з Darrel O' Neil ](https://github.com/darrel-oneil/PaginationTagHelper) цей пакунок включено до проекту як пакунок nuget.

Це буде додано до _ViewImports. cshtml файл, отже він доступний для всіх переглядів.

```razor
@addTagHelper *,PaginationTagHelper.AspNetCore
```

### Інструмент довідки мітками

У _BlogSummaryList.cshtml частковий перегляд Я додав наступний код для відтворення механізму.

```razor
<pager link-url="@Model.LinkUrl"
       hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
       page="@Model.Page"
       page-size="@Model.PageSize"
       total-items="@Model.TotalItems" ></pager>
```

Декілька визначних речей тут:

1. `link-url` за допомогою цього пункту можна створити чинну адресу адреси URL для пошуків посилань. У методі індексу HomeController цю дію встановлено.

```csharp
   var posts = blogService.GetPostsForFiles(page, pageSize);
   posts.LinkUrl= Url.Action("Index", "Home");
   if (Request.IsHtmx())
   {
      return PartialView("_BlogSummaryList", posts);
   }
```

І в контролері блогу

```csharp
    public IActionResult Index(int page = 1, int pageSize = 5)
    {
        var posts = blogService.GetPostsForFiles(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

Це дорівнює URl. Це забезпечить допоміжну пару парування, яка може працювати для обох методів найвищого рівня.

### Властивості HTMX

`hx-boost`, `hx-push-url`, `hx-target`, `hx-swap` Це всі властивості HTMX, які дають змогу використовувати HTMX.

```razor
     hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
```

Тут ми використовуємо `hx-boost="true"` За допомогою цього пункту можна зробити так, щоб у допоміжному режимі pagination не було внесено жодних змін, перехоплюючи його звичайне створення адрес URL і використовуючи поточну адресу URL.

`hx-push-url="true"` щоб переконатися, що адресу URL було змінено у журналі адрес URL навігатора (за допомогою цього журналу можна безпосередньо пов' язати зі сторінками).

`hx-target="#content"` це div цілі, який буде замінено новим вмістом.

`hx-swap="show:none"` це ефект свопінгу, який буде використано під час заміни вмісту. У цьому випадку він запобігає звичайному ефекту " jump," який HTMX використовує для заміни вмісту.

#### CSS

Я також додав стилі до main.css в моєму каталозі / src, що дозволяє мені використовувати класи CSS Tailwin для pagination посилань.

```css
.pagination {
    @apply py-2 flex list-none p-0 m-0 justify-center items-center;
}

.page-item {
    @apply mx-1 text-black  dark:text-white rounded;
}

.page-item a {
    @apply block rounded-md transition duration-300 ease-in-out;
}

.page-item a:hover {
    @apply bg-blue-dark text-white;
}

.page-item.disabled a {
    @apply text-blue-dark pointer-events-none cursor-not-allowed;
}

```

### Контролер

`page`, `page-size`, `total-items` є властивостями, які використовує програма для створення пінгівських посилань для роботи з мітками (Pagination taghler).
Вони передаються в частковий вигляд контролера.

```csharp
 public IActionResult Index(int page = 1, int pageSize = 5)
```

### Служба блогу

За допомогою цієї сторінки і сторінки Розмір буде передано за адресою URL, а всі пункти буде обчислено за допомогою служби блогу.

```csharp
    public PostListViewModel GetPostsForFiles(int page=1, int pageSize=10)
    {
        var model = new PostListViewModel();
        var posts = GetPageCache().Values.Select(GetListModel).ToList();
        model.Posts = posts.OrderByDescending(x => x.PublishedDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        model.TotalItems = posts.Count();
        model.PageSize = pageSize;
        model.Page = page;
        return model;
    }
```

Тут ми просто отримуємо дописи з кешу, впорядковуємо їх за датою, а потім пропускаємо і беремо відповідну кількість дописів для сторінки.

### Висновки

Це був простий додаток до сайту, але він робить його набагато придатнішим для використання. Інтеграція з HTMX робить сайт чутливішим і не додає більшого JavaScript до сайта.