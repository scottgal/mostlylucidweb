# ASP. NET Core Caching з HTMX

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024- 08- 12T00: 50</datetime>

## Вступ

Качування є важливою технікою для того, щоб покращити досвід користувача, завантажуючи вміст швидше і щоб зменшити навантаження на вашому сервері. У цій статті я покажу вам, як використовувати вбудовані можливості кешування ASP. NET Core з HTMX для кешування вмісту на стороні клієнта.

[TOC]

## Налаштування

У ядрах ASP.NET передбачено два види Caching.

- Кеш repons - це дані, що зберігаються на клієнтських або проміжних серверах (або обидва сервери), які використовуються для кешування всієї відповіді на запит.
- Кеш виводу - це дані, які кешуються на сервері, і які використовуються для кешування виводу дії контролера.

Щоб встановити їх у ядрі ASP.NET, вам потрібно додати декілька послуг до вашої системи. `Program.cs` файл

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

У той час як у вашій системі є можливість створити відповідь `Program.cs` часто це трохи нерівномірно (особливо, використовуючи запити HTMX, як я виявив). Ви можете встановити Caching відповідь у ваших діях контролера за допомогою `ResponseCache` атрибут.

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

За допомогою цього пункту можна виконати кешування відповіді протягом 300 секунд і змінити розмір кешу за допомогою `hx-request` заголовок і назва `page` і `pageSize` Параметри запиту. Ми також встановлюємо `Location` до `Any` це означає, що відповідь може бути кешовано на клієнті, на проксі- серверах посередників або на обох серверах.

Ось, будь ласка. `hx-request` заголовок - це заголовок, який HTMX надсилає з кожним запитом. Ця дія є важливою, оскільки надає вам змогу кешувати відповідь на неї, залежно від того, чи вона є запитом HTMX, чи звичайним запитом.

Це наш струм. `Index` метод дій. Yo ucan бачить, що ми приймаємо параметр page і pageSize тут і ми додали їх як ключі verby query в `ResponseCache` атрибут. Це означає, що відповіді "взяті" цими ключами і зберігають різний вміст на основі них.

У дії ми також маємо `if(Request.IsHtmx())` це базується на [Пакунок HTMX.Net](https://github.com/khalidabuhakmeh/Htmx.Net)  і, по суті, перевірка на те саме `hx-request` заголовок, який ми використовуємо, щоб змінити розмір кешу. Тут ми повертаємо частковий перегляд, якщо запит походить від HTMX.

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

Кечування виводу є стороною сервера, що відповідає Caching у відповідь. Він кешує вивід дії контролера. По суті, веб- сервер зберігає результат запиту і обслуговує його для наступних запитів.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

Тут ми кешуємо вивід дії контролера протягом 3600 секунд і змінюємо кеш на `hx-request` заголовок і назва `page` і `pageSize` Параметри запиту.
Оскільки ми зберігаємо сервер даних протягом значного часу (шпи оновлюються лише з натисненням докерів) це означає довше, ніж Кеш реагування; насправді, він може бути нескінченним у нашому випадку, але 3600 секунд це хороший компроміс.

Як і з Кеш реагування ми використовуємо `hx-request` заголовок, який слід змінити у кеші, залежно від того, чи належить запит до HTMX, чи ні.

## Статичні файли

Ядро ASP. NET також має вбудовану підтримку статичних файлів, що кешують. Зробити це можна за допомогою встановлення значення `Cache-Control` заголовок у відповіді. Ви можете встановити це у вашій `Program.cs` файл.
Зауважте, що порядок " i " важливий тут, якщо ваші статичні файли потребують підтримки уповноваження, вам слід пересунути `UseAuthorization` middleware before the `UseStaticFiles` middleware. TH UseHttpsReпрямування Посередніх програм також слід використовувати до програмного забезпечення UseStatticFiles, якщо ви маєте змогу скористатися цією можливістю.

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

Качування - це потужний інструмент для покращення швидкодії вашої програми. За допомогою вбудованих можливостей кешування ядра ASP. NET ви легко можете кешувати вміст на стороні клієнта або сервера. За допомогою HTMX ви можете кешувати вміст на стороні клієнта, а також створювати часткові перегляди, щоб покращити досвід користувача.