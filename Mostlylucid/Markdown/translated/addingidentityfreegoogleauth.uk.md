# Додавання auth Google без бази даних профілю ASP. NET

<!--category-- ASP.NET, Google Auth -->
<datetime class="hidden">2024- 08- 05T08: 06</datetime>

## Вступ

У цій програмі я хотів додати гнучкий механізм, за допомогою якого можна додавати коментарі (і деякі адміністративні завдання) до програми. Я хотів використати Google Auth з цією метою. Я не хотів використовувати базу даних профілю ASP.NET з цією метою. Я хотів зробити додаток якомога простішим.

Бази даних є потужним компонентом будь-якої програми, але вони також додають складності. Я хотіла уникнути такої складності, поки вона мені дійсно не була потрібна.

[TOC]

## Кроки

Спочатку вам слід налаштувати Google Auth в Консолі розробників Google. Ви можете зробити кроки в цьому. [посилання](https://developers.google.com/identity/gsi/web/guides/overview) щоб налаштувати ваші подробиці для вас, ідентифікатори і секрети Google-клієнта.

Якщо у вас є ідентифікатор і секретний клієнт Google, ви можете додати їх до вашого файла appsettings.json.

```json
    "Auth" :{
"GoogleClientId": "",
"GoogleClientSecret": ""
}
```

ЯК НІКОЛИ не слід перевіряти ці дані на панелі керування кодами. Замість цього для локальної розробки ви можете скористатися файлом Секрети:

![secrets.png](secrets.png)

Тут ви можете додати ідентифікатор вашого клієнта Google і Секретний (зауважте, що ваш клієнт Id насправді не є конфіденційним, як ви побачите пізніше, він включений у виклик JS на початку.

```json
    "Auth" :{
  "GoogleClientId": "ID",
  "GoogleClientSecret": "CLIENTSECRET"
}
```

## Налаштування розпізнавання Google за допомогою POCO

Зверніть увагу, що я використовую модифіковану версію IConfigSection Стіва Сміта (насправді вона стала відомою Філом Хаком).
Це для того, щоб уникнути речей IOptions, які я вважаю дещо незграбними (і рідко потрібно, оскільки я майже ніколи не змінюю налаштування після активації у моїх сценаріях).

У моїй я роблю це, що надає мені змогу отримати назву розділу від самого класу:

<details>
<summary>Click to expand</summary>
```csharp


namespace Mostlylucid.Config;

public static class ConfigExtensions {
    public static TConfig ConfigurePOCO<TConfig>(this IServiceCollection services, IConfiguration configuration)
        where TConfig : class, new() {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        
        var config = new TConfig();
        configuration.Bind(config);
        services.AddSingleton(config);
        return config;
    }
    
    public static TConfig Configure<TConfig>(this WebApplicationBuilder builder)
        where TConfig : class, IConfigSection, new() {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var sectionName = TConfig.Section;
        return services.ConfigurePOCO<TConfig>(configuration.GetSection(sectionName));
    }
    

    public static TConfig GetConfig<TConfig>(this WebApplicationBuilder builder)
        where TConfig : class, IConfigSection, new() {
        var configuration = builder.Configuration;
        var sectionName = TConfig.Section;
        var section = configuration.GetSection(sectionName).Get<TConfig>();
        return section;
        
    }
    
    public static Dictionary<string, object> GetConfigSection(this IConfiguration configuration, string sectionName) {
        var section = configuration.GetSection(sectionName);
        var result = new Dictionary<string, object>();
        foreach (var child in section.GetChildren()) {
            var key = child.Key;
            var value = child.Value;
            result.Add(key, value);
        }
        
        return result;
    }
    
    public static Dictionary<string, object> GetConfigSection<TConfig>(this WebApplicationBuilder builder)
        where TConfig : class, IConfigSection, new() {
        var configuration = builder.Configuration;
        var sectionName = TConfig.Section;
        return configuration.GetConfigSection(sectionName);
    }
}

public interface IConfigSection {
    public static abstract string Section { get; }
}
```

</details>
Так, мій Авдій, схоже, сказав:

```csharp
public class Auth : IConfigSection
{
    public static string Section => "Auth";
    public string GoogleClientId { get; set; }
    public string GoogleClientSecret { get; set; }
    
    public string AdminUserGoogleId { get; set; }
    
}
```

Тут я використовую метод статичного інтерфейсу, щоб отримати назву розділу.

Тоді в моєму стартапі я зможу зробити це:

```csharp
var auth = builder.GetConfig<Auth>();
```

КОГДА-НИБУДЬ ВЕРНУТСЯ В КУПИ!

## Налаштування програм. cs

СПРАВДІ додати це

```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowMostlylucid",
        builder =>
        {
            builder.WithOrigins("https://www.mostlylucid.net")
                .WithOrigins("https://mostlylucid.net")
                .WithOrigins("https://localhost:7240")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
   
      
    })
    .AddCookie()
    .AddGoogle(options =>
    {
        options.ClientId = auth.GoogleClientId;
        options.ClientSecret = auth.GoogleClientSecret;
    });
```

Ви помітите, що тут є записи CORS, вам також потрібно встановити їх в Консоль особистості google.

![googleidentity.png](googleidentity.png)

За допомогою цього пункту можна зробити так, щоб автентифікацію Google можна було використовувати лише з визначених вами доменів.

## Google Auth In Razor

В моєму _Компонування.cshtml Я маю цей Javascript, тут я налаштовую свої кнопки Google і вмикаю виклик, який реєструє програму ASP.NET.

# Google JS

```html
<script src="https://accounts.google.com/gsi/client" async defer></script>
```

Це двокрапка для коду нижче

```javascript


        
        function renderButton(element)
        {
            google.accounts.id.renderButton(
                element,
                {
                    type: "standard",
                    size: "large",
                    width: 200,
                    theme: "filled_black",
                    text: "sign_in_with",
                    shape: "rectangular",
                    logo_alignment: "left"
                }
            );
        }
        function initGoogleSignIn() {
            google.accounts.id.initialize({
                client_id: "839055275161-u7dqn2oco2729n6i5mk0fe7gap0bmg6g.apps.googleusercontent.com",
                callback: handleCredentialResponse
            });
            const element = document.getElementById('google_button');
            if (element) {
                renderButton(element);
            }
            const secondElement = document.getElementById('google_button2');
            if (secondElement) {
                renderButton(secondElement);
            }
           
        }

        function handleCredentialResponse(response) {
            if (response.credential) {
                const xhr = new XMLHttpRequest();
                xhr.open('POST', '/login', true);
                xhr.setRequestHeader('Content-Type', 'application/json');
                xhr.onload = function () {
                    if (xhr.status === 200) {
                        window.location.reload();
                    } else {
                        console.error('Failed to log in.');
                    }
                };
                xhr.send(JSON.stringify({ idToken: response.credential }));
            } else {
                console.error('No credential in response.');
            }
        }

        window.onload = initGoogleSignIn;

```

Тут ви можете бачити, що на сторінці є до двох розв' язаних елементів, за допомогою кнопок id google_ і google_ button2. Це елементи, на які JS Google буде перетворювати кнопки.

TIP: якщо ви використовуєте Tailwind, ви можете відштовхнути кнопку div до правильної роботи у темному режимі (у іншому випадку вона відображає білий тло навколо кнопки)

```html
<div class="w-[200px] h-[39px] overflow-hidden rounded">
    <div id="google_button">
    </div>
</div>
```

У JavaScript, наведеному вище, я відсилаю це назад до дії Контролера, яка називається Ім' я користувача. Ось де я працюю з автентифікацією Google.

```javascript
      const xhr = new XMLHttpRequest();
                xhr.open('POST', '/login', true);
                xhr.setRequestHeader('Content-Type', 'application/json');
                xhr.onload = function () {
                    if (xhr.status === 200) {
                        window.location.reload();
                    } else {
                        console.error('Failed to log in.');
                    }
                };
                xhr.send(JSON.stringify({ idToken: response.credential }));
```

## Автентифікація Google у контролері

Диспетчер тут, це досить просто він просто бере виписаний JWT, декодує його потім, щоб увійти в програму.

```csharp
    [Route("login")]
        [HttpPost]
        public async Task<IActionResult> HandleGoogleCallback([FromBody] GoogleLoginRequest request)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(request.IdToken) as JwtSecurityToken;

            if (jsonToken == null)
            {
                return BadRequest("Invalid token");
            }

            var claimsIdentity = new ClaimsIdentity(
                jsonToken.Claims,
                GoogleDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return Ok();
        }
    }
```

ЗАУВАЖЕННЯ: цей випадок не є префектом, бо він висуває так звані твердження (вони всі є малими літерами), але тепер це працює.

### Базовий клас контролера для видобування властивостей входу до системи

У моєму BaseController я витягаю властивості, які мені потрібні;

```csharp
      public record LoginData(bool loggedIn, string? name, string? avatarUrl, string? identifier);
    
    protected LoginData GetUserInfo()
    {
        var authenticateResult = HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme).Result;
        if (authenticateResult.Succeeded)
        {
            var principal = authenticateResult.Principal;
            if(principal == null)
            {
                return new LoginData(false, null, null, null);
            }
            var name = principal.FindFirst("name").Value;
            var avatarUrl =principal.FindFirst("picture").Value;
            var nameIdentifier = principal.FindFirst("sub");
            return new LoginData(true, name, avatarUrl, nameIdentifier?.Value);
        }
        return new LoginData(false,null,null,null);
    }
```

І все! За допомогою цього пункту ви можете скористатися пунктом Розпізнавання Gooogle без використання бази даних профілю ASP. NET.