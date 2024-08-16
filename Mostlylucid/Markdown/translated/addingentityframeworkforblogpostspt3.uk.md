# Додавання блоку сутності для дописів блогу (частина 3)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024- 08- 16T18: 00</datetime>

Ви можете знайти всі вихідні коди дописів блогу [GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Частини 1 і 2 серії статей щодо додавання фреймів сутностей до проекту. NET Core.**

Частину 1 можна знайти [тут](/blog/addingentityframeworkforblogpostspt1).

Частину 2 можна знайти. [тут](/blog/addingentityframeworkforblogpostspt2).

## Вступ

У попередніх частинах ми встановили базу даних та контекст нашого блогу, і додали служби для взаємодії з базою даних. У цій статті ми поговоримо про те, як ці служби працюють з існуючими контролерами та поглядами.

[TOC]

## Контролер

Out controls for Blogs are really very simple; in line of the 'Fat Controller' Protection (ідентифікований шаблон, який ми ініціювали на початку днів MVC ASP. NET).

### Шаблон контролера жиру у MVC ASP.NET

IMVC визначає хорошу практику, щоб зробити якомога менше в ваших методах контролера. Це тому, що контролер відповідає за виконання запиту і повернення відповіді. Це не повинно бути відповідальне за фахову логіку цієї заяви. Це відповідальність моделі.

Шаблон антиконтроля "Fat Controller" полягає у тому, що контролер робить забагато. Це може призвести до багатьох проблем, зокрема:

1. Обчислення коду у декількох діях:
   Дія повинна бути однією одиницею роботи, просто роздути модель і повернути її погляд. Якщо ви знаходите, що повторюєте код у декількох діях, то це знак, що вам слід переробити цей код у окремий спосіб.
2. Код, який важко перевірити:
   За допомогою "жирних контролерів" вам може бути важко перевірити код. Тестування має намагатися стежити за всіма можливими шляхами коду, це може бути важко, якщо код недостатньо структурований і зосереджений на одній відповідальності.
3. Код, який важко підтримувати:
   Під час будівництва обов'язкова підтримка є головною турботою. Використання методів дій "kitchentle" може легко призвести до того, що ви, як і інші розробники, використовуватимете код для того, щоб внести зміни, які розбивають інші частини програми.
4. Код, який важко зрозуміти:
   Це є головною турботою для розробників. Якщо ви працюєте над проектом з великою базою коду, вам може бути важко зрозуміти, що відбувається з регулятором, якщо він забагато робить.

### Засіб керування блогом

Контролер блогу відносно простий. У програмі передбачено 4 основних дії (і одну " дію з порівняння " для попередніх посилань блогу). Ось деякі з них:

```csharp
Task<IActionResult> Index(int page = 1, int pageSize = 5)

Task<IActionResult> Show(string slug, string language = BaseService.EnglishLanguage)

Task<IActionResult> Category(string category, int page = 1, int pageSize = 5)

Task<IActionResult> Language(string slug, string language)

IActionResult Compat(string slug, string language)
```

У свою чергу ці дії викликають `IBlogService` аби отримати необхідні їм дані. The `IBlogService` докладно описано [попередній допис](/blog/addingentityframeworkforblogpostspt2).

У свою чергу ці дії є наступними

- Індекс: це список дописів блогів (типово, англійською мовою). Пізніше ми можемо розширити цей список, щоб надати змогу використовувати декілька мов. Ви побачите, що це потрібно `page` і `pageSize` як параметри. Це для парування. результатів.
- Показати: Це допис окремого блогу. Це забирає `slug` допису і `language` як параметри. ТВ - це спосіб, яким ви зараз читаєте цей допис блогу.
- Категорія: це список дописів блогу для вказаної категорії. Це забирає `category`, `page` і `pageSize` як параметри.
- Мова: тут буде показано допис блогу для вказаної мови. Це забирає `slug` і `language` як параметри.
- Компат: цей пункт призначено для сумісності застарілих посилань блогів. Це забирає `slug` і `language` як параметри.

### Кечінгgreat- britain_ counties. kgm

Як ми вже згадували [Попередній допис](https://www.mostlylucid.net/blog/aspnetcachingwithhtmx) ми реалізуємо `OutputCache` і `ResponseCahce` для кешування результатів дописів блогу. За допомогою цього пункту можна покращити досвід користувача і зменшити навантаження на сервер.

Ці параметри реалізовано за допомогою відповідних декорацій дій, які визначають параметри, які буде використано для дії (а також `hx-request` для запитів HTMX). Для ексампеля з `Index` ми визначаємо ось ці:

```csharp
    [ResponseCache(Duration = 300, VaryByHeader  = "hx-request", VaryByQueryKeys = new[] {nameof(page), nameof(pageSize)}, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] {"hx-request"} ,VaryByQueryKeys = new[] { nameof(page), nameof(pageSize)})]
```

## Перегляди

Перегляди блогу відносно прості. Це, в основному, лише список блогів, де є кілька подробиць для кожного допису. Погляди є в `Views/Blog` тека. Основними переглядами є:

### `_PostPartial.cshtml`

Це частковий перегляд для одного допису блогу. Воно використовується всередині нас. `Post.cshtml` вигляд.

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
    Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

### `_BlogSummaryList.cshtml`

Це частковий перегляд для списку дописів блогів. Воно використовується всередині нас. `Index.cshtml` переглядати як і домашню сторінку.

```razor
@model Mostlylucid.Models.Blog.PostListViewModel
<div class="pt-2" id="content">

    @if (Model.TotalItems > Model.PageSize)
    {
        <pager
            x-ref="pager"
            link-url="@Model.LinkUrl"
               hx-boost="true"
               hx-push-url="true"
               hx-target="#content"
               hx-swap="show:none"
               page="@Model.Page"
               page-size="@Model.PageSize"
               total-items="@Model.TotalItems"
            class="w-full"></pager>
    }
    @if(ViewBag.Categories != null)
{
    <div class="pb-3">
        <h4 class="font-body text-lg text-primary dark:text-white">Categories</h4>
        <div class="flex flex-wrap gap-2 pt-2">
            @foreach (var category in ViewBag.Categories)
            {
                <a hx-controller="Blog" hx-action="Category" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>
                    <span class="inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white">@category</span>
                </a>
            }
        </div>
    </div>
}
@foreach (var post in Model.Posts)
{
    <partial name="_ListPost" model="post"/>
}
</div>
```

This using the `_ListPost` частковий перегляд для показу окремих дописів блогу разом з [Помічник створення міток](/blog/addpagingwithhtmx) За допомогою цього пункту ми можемо надсилати повідомлення у блог.

### `_ListPost.cshtml`

The _Перегляд часткового списку використовується для показу окремих дописів блогу у списку. Воно використовується у межах `_BlogSummaryList` вигляд.

```razor
@model Mostlylucid.Models.Blog.PostListModel

<div class="border-b border-grey-lighter pb-8 mb-8">
 
    <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold transition-colors hover:text-green text-blue-dark dark:text-white  dark:hover:text-secondary">@Model.Title</a>
    <div class="flex space-x-2 items-center py-4">
    @foreach (var category in Model.Categories)
    {
    <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
    }

    @{ var languageModel = (Model.Slug, Model.Languages, Model.Language); }
        <partial name="_LanguageList" model="languageModel"/>
    </div>
    <div class="block font-body text-black dark:text-white">@Model.Summary</div>
    <div class="flex items-center pt-4">
        <p class="pr-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.PublishedDate.ToString("f")
        </p>
        <span class="font-body text-grey dark:text-white">//</span>
        <p class="pl-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.ReadingTime
        </p>
    </div>
</div>
```

Як ви побачите тут ми маємо посилання на окремі блоги, категорії допису, мови, на яких знаходиться допис, резюме допису, дату видання і час читання.

Ми також маємо посилання HTMX для категорій і мов, які дозволяють нам показувати локалізовані дописи і дописи для заданої категорії.

Ми маємо два способи використання HTMX тут, один, який дає повну адресу URL, а інший - "лише HTML" (і. е. немає адреси URL). Це тому, що ми хочемо використовувати повний URL для категорій та мов, але нам не потрібен повний URL для допису окремих блогів.

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
```

Цей підхід заповнює повну адресу URL допису окремої блогу і використовує `hx-boost` у " bost " запит на використання HTMX (це встановлює значення `hx-request` заголовок до `true`).

```razor
  <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
```

Крім того, цей підхід використовує мітки HTMX для отримання категорій дописів блогу. This using the `hx-controller`, `hx-action`, `hx-push-url`, `hx-get`, `hx-target` і `hx-route-category` теґи для отримання категорій дописів блогу `hx-push-url` встановлено до `true` щоб перевести адресу URL до журналу перегляду.

Воно також використовується всередині нас. `Index` Метод дії для запитів до HTMX.

```csharp
  public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
    {
        var posts =await  blogService.GetPagedPosts(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

Це дозволяє нам або повернути повний погляд, або лише частковий перегляд запитів HTMX, даючи такий досвід "SPA."

## Домашня сторінка

У `HomeController` Ми також звертаємося до цих блогових служб, щоб отримати останні дописи для домашньої сторінки. Це робиться в `Index` метод дій.

```csharp
   public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPagedPosts(page, pageSize);
            posts.LinkUrl= Url.Action("Index", "Home");
            if (Request.IsHtmx())
            {
                return PartialView("_BlogSummaryList", posts);
            }
            var indexPageViewModel = new IndexPageViewModel
            {
                Posts = posts, Authenticated = authenticateResult.LoggedIn, Name = authenticateResult.Name,
                AvatarUrl = authenticateResult.AvatarUrl
            };
            
            return View(indexPageViewModel);
    }
```

Як ви побачите тут ми використовуємо `IBlogService` щоб отримати останні дописи з блогу для домашньої сторінки. Ми також використовуємо `GetUserInfo` спосіб отримання відомостей про користувача для домашньої сторінки.

Знову ж таки, запит на отримання даних з HTMX, щоб повернути частковий перегляд дописів блогу, який надасть нам змогу надсилати дописи блогу на домашній сторінці.

## Включення

У наступній частині ми розглянемо нестерпні деталі того, як ми використовуємо `IMarkdownBlogService` для заповнення бази даних дописами блогу з файлів markdown. Це ключова частина програми, оскільки вона надає нам змогу використовувати файли markdown для заповнення бази даних блогами.