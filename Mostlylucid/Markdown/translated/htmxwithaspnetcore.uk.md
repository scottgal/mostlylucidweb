# Htmx з ядром Asp.Net

<datetime class="hidden">2024- 08- 01T03: 42</datetime>

<!--category-- ASP.NET, HTMX -->
## Вступ

Використання HTMX з ядром ASP. NET є чудовим способом побудови динамічних веб- програм з мінімальним JavaScript. HTMX надає вам змогу оновлювати частини вашої сторінки без перезавантаження повної сторінки, що робить вашу програму більш швидкою і інтерактивною.

Це те, що я називав "гібридним" веб-дизайном, де ви повністю використовуєте код з сервера, а потім використовуєте HTMX для динамічного оновлення частин сторінки.

У цій статті я покажу вам, як починати з HTMX у програмі для роботи з ядром ASP. NET.

[TOC]

## Передумови

HTMX - Htmx - це пакунок JavaScript, яким можна включити його до вашого проекту за допомогою CDN. (Дивіться [тут](https://htmx.org/docs/#installing) )

```html
<script src="https://unpkg.com/htmx.org@2.0.1" integrity="sha384-QWGpdj554B4ETpJJC9z+ZHJcA/i59TyjxEPXiiUgN2WmTyV5OEZWCD6gQhgkdpB/" crossorigin="anonymous"></script>
```

Крім того, ви можете звантажити копію і включити її " вручну " (або скористатися LibMan або npm).

## ASP. NET Bits

Я також рекомендую встановити Помічник міток Htmx з [тут](https://github.com/khalidabuhakmeh/Htmx.Net)

Вони обидва від прекрасного. [Халід Абгакмеlithuania_ municipalities. kgm
](https://mastodon.social/@khalidabuhakmeh@mastodon.social)

```shell
dotnet add package Htmx.TagHelpers
```

І пакунок Htmx Nuget з [тут](https://www.nuget.org/packages/Htmx/)

```shell
 dotnet add package Htmx
```

Помічник теґів надає вам змогу зробити це:

```razor
    <a hx-controller="Blog" hx-action="Show" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-slug="@Model.Slug"
        class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Альтернативний підхід.

**ЗАУВАЖЕННЯ: такий підхід має одну головну ваду, він не створює згину для повідомлення. Це проблема SEO та доступності. Це також означає, що ці посилання зазнають невдачі, якщо HTMX з певних причин не завантажиться (CDN DO зменшиться).**

Альтернативний підхід - це використання ` hx-boost="true"` атрибут і звичайні помічники теґів Ap.net. Див.  [тут](https://htmx.org/docs/#hx-boost) щоб дізнатися більше про hx- boust (хоча документи дещо розсіяні).
За допомогою цього пункту можна вивести звичайний herf, але його перехопить HTMX і вміст, завантажений динамічно.

Ось так:

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Частки

HTMX добре працює з частковими переглядами. Ви можете скористатися HTMX для завантаження часткового перегляду у контейнер на вашій сторінці. Це чудово для динамічного завантаження частин вашої сторінки без повного перезавантаження сторінки.

У цьому додатку ми маємо контейнер у файлі Design. cshtml, до якого ми хочемо завантажити частковий перегляд.

```razor
    <div class="container mx-auto" id="contentcontainer">
   @RenderBody()

    </div>
```

Зазвичай, він відображає зміст сторони сервера, але використання допоміжного теґу HTMX, який ви бачите, ми маємо намір `hx-target="#contentcontainer"` який завантажить частковий перегляд до контейнера.

У нашому проекті ми маємо частковий перегляд BlogView, який ми хочемо завантажити у контейнер.

![img.png](project.png)

Потім у Контроллері блогів ми маємо

```csharp
    [Route("{slug}")]
    [OutputCache(Duration = 3600)]
    public IActionResult Show(string slug)
    {
       var post =  blogService.GetPost(slug);
       if(Request.IsHtmx())
       {
              return PartialView("_PostPartial", post);
       }
       return View("Post", post);
    }
```

Тут ви бачите, що ми маємо метод HTMX Protocol.IsHtmx}, цей метод повернеться, якщо запит є HTMX. Якщо так, то ми повертаємо частковий погляд, якщо не повністю зосередимося на поглядах.

Використовуючи це, ми можемо переконатися, що також підтримуємо прямі запити з незначними зусиллями.

У цьому випадку наш повний погляд стосується цієї частини:

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
ViewBag.Title = "title";
Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

І тепер ми маємо дуже простий спосіб завантажити частковий перегляд на нашу сторінку за допомогою HTMX.