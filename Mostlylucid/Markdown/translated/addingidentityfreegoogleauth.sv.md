# Lägga till Google Auth utan ASP.NET Identitetsdatabas

<!--category-- ASP.NET, Google Auth -->
<datetime class="hidden">2024-08-05T08:06..........................................................................................................</datetime>

## Inledning

I den här appen ville jag lägga till en enkel mekanism för att tillåta inloggning att lägga till kommentarer (och vissa administratörsuppgifter) till appen. Jag ville använda Google Auth för detta ändamål. Jag ville inte använda databasen ASP.NET Identitet för detta ändamål. Jag ville hålla appen så enkel som möjligt så länge som möjligt.

Databaser är en kraftfull komponent i alla program men de ökar också komplexiteten. Jag ville undvika den komplexiteten tills jag verkligen behövde den.

[TOC]

## Steg

Först måste du konfigurera Google Auth i Google Developer Console. Du kan följa stegen i detta [länk](https://developers.google.com/identity/gsi/web/guides/overview) för att få dina uppgifter konfigurerade för dig Google Client ID och Secret.

När du har ditt Google Client ID och Hemlighet, kan du lägga till dem i din appsettings.json-fil.

```json
    "Auth" :{
"GoogleClientId": "",
"GoogleClientSecret": ""
}
```

Du bör dock inte checka in dessa till källkodskontrollen. Istället för lokal utveckling kan du använda Secrets-filen:

![secrets.png](secrets.png)

Där kan du lägga till ditt Google Client ID och Hemlighet (observera att din klient Id faktiskt inte är konfidentiell, som du kommer att se senare det ingår i JS samtalet på framsidan.

```json
    "Auth" :{
  "GoogleClientId": "ID",
  "GoogleClientSecret": "CLIENTSECRET"
}
```

## Anpassa Google Auth med POCO

Observera att jag använder en modifierad version av Steve Smiths IConfigsektion (nyligen känd av Phil Haack).
Detta för att undvika IOptions grejer som jag tycker är lite klumpiga (och sällan behöver eftersom jag nästan aldrig ändrar konfiguration efter installation i mina scenarier).

I min gör jag detta som gör att jag kan få sektionsnamnet från klassen själv:

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
Så min Auth ser ut som

```csharp
public class Auth : IConfigSection
{
    public static string Section => "Auth";
    public string GoogleClientId { get; set; }
    public string GoogleClientSecret { get; set; }
    
    public string AdminUserGoogleId { get; set; }
    
}
```

Där jag använder en statisk gränssnittsmetod för att få sektionsnamnet.

Sen i min start kan jag göra detta:

```csharp
var auth = builder.GetConfig<Auth>();
```

Tillbaka till googlegrejerna!

## Program.cs Ställ in

Att faktiskt lägga till detta

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

Du kommer att märka att det finns Cors poster här, du måste också ställa in dessa i google identitetskonsolen.

![googleidentity.png](googleidentity.png)

Detta säkerställer att Google Auth endast kan användas från de domäner du anger.

## Google Auth i Razor

I min _Layout.cshtml Jag har denna Javascript, det är där jag konfigurerar mina Google Knappar och trigga en callback som loggar ASP.NET app.

# Google JS

```html
<script src="https://accounts.google.com/gsi/client" async defer></script>
```

Detta är dlowen för koden nedan

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

Här kan du se att jag har upp till två div element på sidan med id google_knappen och google_knappen2. Dessa är de element som Google JS kommer att göra knapparna i.

TIPS: Om du använder Tailwind kan du shink knappen div att fungera korrekt i mörkt läge (annars gör det en vit bakgrund runt knappen)

```html
<div class="w-[200px] h-[39px] overflow-hidden rounded">
    <div id="google_button">
    </div>
</div>
```

I JavaScript ovan lägger jag tillbaka detta till en controller-åtgärd som heter Logga in. Det är här jag hanterar Google Auth.

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

## Google Auth i styrenheten

Controllern är här "det är ganska enkelt att det bara tar den postade JWT, avkodar den sedan använder den för att logga in på appen.

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

NOTERA: Detta är inte prefekt eftersom det esses upp påståenden namn (de är alla lägre fall) men det fungerar för tillfället.

### Controller Base Class för att extrahera inloggningsegenskaperna

I min BaseController jag extrahera de egenskaper jag behöver;

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

Och det är allt! Detta gör att du kan använda Gooogle Authentication utan att använda databasen ASP.NET Identitet.