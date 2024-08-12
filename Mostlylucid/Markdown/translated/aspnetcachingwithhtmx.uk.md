# ASP. NET Core Caching з HTMX

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024- 08- 12T00: 50</datetime>

## Вступ

Качування є важливою технікою, яка покращує досвід обох користувачів: пришвидшує завантаження вмісту і зменшує навантаження на ваш сервер. У цій статті я покажу вам, як використовувати вбудовані можливості кешування ядра ASP. NET з HTMX для кешування вмісту на стороні клієнта.

[TOC]

## Налаштування

У ядрах ASP.NET передбачено два види Caching.

- Кеш repons - це дані, що зберігаються на клієнтських або проміжних серверах (або обидва сервери), які використовуються для кешування всієї відповіді на запит.
- Кеш виводу - це дані, які кешуються на сервері, і які використовуються для кешування виводу дії контролера.

Щоб встановити їх у ядрі ASP.NET, вам потрібно додати декілька послуг до вашої системи.`Program.cs`файл

### Кечування відповідей

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### Кечування виводу

```csharp
services.AddOutputCache();
app.UseOutputCache();
```

## Кечування відповідей

У той час як у вашій системі є можливість створити відповідь`Program.cs`часто це трохи нерівномірно (особливо, якщо ви використовуєте запити HTMX, як я це виявив). Ви можете налаштувати Качування відповідей у діях вашого контролера за допомогою команди`ResponseCache`атрибут.

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

За допомогою цього пункту можна виконати кешування відповіді протягом 300 секунд і змінити розмір кешу за допомогою`hx-request`заголовок і назва`page`і`pageSize`Параметри запиту. Ми також встановлюємо параметри`Location`до`Any`це означає, що відповідь може бути кешовано на клієнті, на проксі- серверах посередників або на обох серверах.

Ось, будь ласка.`hx-request`шапка - це заголовок, який HTMX надсилає з кожним запитом. Це важливо, оскільки вона надає вам змогу кешувати відповідь інакше, на основі запиту HTMX чи звичайного запиту.

Це наш струм.`Index`метод дій. Yo ucan бачить, що ми приймаємо параметр page і pageSize тут, а ми додали їх як ключі запиту rangeby`ResponseCache`attribute. means that answers are 'indexed' by this keys and keep other method, based on its.

У дії ми також маємо`if(Request.IsHtmx())`це базується на[Пакунок HTMX.Net](https://github.com/khalidabuhakmeh/Htmx.Net)і, по суті, перевірка на те саме`hx-request`заголовок, який ми використовуємо для зміни об' єму кешу. Тут ми повертаємо частковий перегляд, якщо запит належить до GTMX.

```csharp
    public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPosts(page, pageSize);
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

## Кечування виводу

Кечування виводу - це сторона сервера, що відповідає за кешування відповідей. Програма кешує вивід дії контролера. По суті, веб- сервер зберігає результат запиту і обслуговує його для наступних запитів.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

Тут ми кешуємо вивід дії контролера протягом 3600 секунд і змінюємо кеш на`hx-request`заголовок і назва`page`і`pageSize`Параметри запиту.
Оскільки ми зберігаємо сервер даних протягом значного часу (шпи оновлюються лише з натисненням докерів) це означає довше, ніж Кеш реагування; насправді, він може бути нескінченним у нашому випадку, але 3600 секунд це хороший компроміс.

Як і з Кеш реагування ми використовуємо`hx-request`заголовок, який слід змінити у кеші, залежно від того, чи належить запит до HTMX, чи ні.

## Статичні файли

Ядро ASP. NET також має вбудовану підтримку статичних файлів, що кешують. Це можна зробити встановленням значення`Cache-Control`заголовок у відповіді. Ви можете налаштувати його у вашій системі`Program.cs`файл.
Зауважте, що порядок " i " важливий тут, якщо ваші статичні файли потребують підтримки уповноваження, вам слід пересунути`UseAuthorization`middleware before the`UseStaticFiles`middleware. TH UseHttpsReпрямування middleware також має бути до посередньої програми UseStatticFiles, якщо вам потрібна ця можливість.

```csharp
app.UseHttpsRedirection();
var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
            "Cache-Control", $"public, max-age={cacheMaxAgeOneWeek}");
    }
});
app.UseRouting();
app.UseCors("AllowMostlylucid");
app.UseAuthentication();
app.UseAuthorization();
```

## Висновки

Caching - це потужний інструмент для покращення швидкодії вашої програми. За допомогою вбудованих можливостей кешування ядра ASP. NET ви легко можете кешувати вміст на стороні клієнта або сервера. За допомогою HTMX ви можете кешувати вміст на стороні клієнта, а також створювати часткові перегляди, щоб покращити досвід користувача.