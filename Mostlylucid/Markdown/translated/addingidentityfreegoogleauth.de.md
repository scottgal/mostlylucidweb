# Hinzufügen von Google Auth ohne ASP.NET Identity Database

<!--category-- ASP.NET, Google Auth -->
<datetime class="hidden">2024-08-05T08:06</datetime>

## Einleitung

In dieser App wollte ich einen einfachen Mechanismus hinzufügen, der es erlaubt, der App Kommentare (und einige Admin-Aufgaben) hinzuzufügen. Zu diesem Zweck wollte ich Google Auth nutzen. Zu diesem Zweck wollte ich die ASP.NET Identity-Datenbank nicht verwenden. Ich wollte die App so einfach wie möglich halten.

Datenbanken sind eine leistungsfähige Komponente jeder Anwendung, aber sie fügen auch Komplexität hinzu. Ich wollte diese Komplexität vermeiden, bis ich sie wirklich brauchte.

[TOC]

## Schritte

Zuerst müssen Sie Google Auth in der Google Developer Console einrichten. Sie können die Schritte in diesem folgen [Verknüpfung](https://developers.google.com/identity/gsi/web/guides/overview) um Ihre Daten für Sie Google Client ID und Secret einrichten zu lassen.

Sobald Sie Ihre Google Client ID und Secret haben, können Sie diese Ihrer appsettings.json-Datei hinzufügen.

```json
    "Auth" :{
"GoogleClientId": "",
"GoogleClientSecret": ""
}
```

WIE auch immer Sie sollten nicht überprüfen Sie diese in Source-Steuerung. Stattdessen können Sie für die lokale Entwicklung die Secrets-Datei verwenden:

![secrets.png](secrets.png)

Dort können Sie Ihre Google Client ID und Secret hinzufügen (beachten Sie, dass Ihre Client Id nicht wirklich vertraulich ist, wie Sie später sehen werden, dass sie im JS-Aufruf am vorderen Ende enthalten ist.

```json
    "Auth" :{
  "GoogleClientId": "ID",
  "GoogleClientSecret": "CLIENTSECRET"
}
```

## Google Auth mit POCO konfigurieren

Anmerkung Ich verwende eine modifizierte Version von Steve Smiths IConfigSection (die kürzlich von Phil Haack berühmt gemacht wurde).
Dies ist, um die IOptions Sachen zu vermeiden, die ich ein wenig klobig finde (und selten brauchen, da ich fast nie ändern Konfiguration nach Bereitstellung in meinen Szenarien).

In meinem mache ich das, was mir erlaubt, den Abschnittsnamen von der Klasse selbst zu bekommen:

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
Also sieht mein Auth aus wie

```csharp
public class Auth : IConfigSection
{
    public static string Section => "Auth";
    public string GoogleClientId { get; set; }
    public string GoogleClientSecret { get; set; }
    
    public string AdminUserGoogleId { get; set; }
    
}
```

Wo ich eine statische Interface-Methode benutze, um den Abschnittsnamen zu erhalten.

Dann kann ich in meinem Startup folgendes tun:

```csharp
var auth = builder.GetConfig<Auth>();
```

Auf jeden Fall zurück zum Google-Zeug!

## Programm.cs einrichten

UM dies tatsächlich hinzuzufügen

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

Sie werden feststellen, gibt es CORS-Einträge hier, müssen Sie auch diese in der Google-Identity-Konsole einrichten.

![googleidentity.png](googleidentity.png)

Dadurch wird sichergestellt, dass Google Auth nur von den von Ihnen angegebenen Domains genutzt werden kann.

## Google Auth In Razor

In meinem _Layout.cshtml Ich habe dieses Javascript, hier habe ich meine Google Buttons eingerichtet und einen Rückruf ausgelöst, der die ASP.NET App protokolliert.

# Google JS

```html
<script src="https://accounts.google.com/gsi/client" async defer></script>
```

Dies ist der Dlow für den Code unten

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

Hier sehen Sie, dass ich bis zu zwei div Elemente in der Seite mit dem id google_button und google_button2 habe. Dies sind die Elemente, in die die Google JS die Schaltflächen rendern wird.

TIPP: Wenn Sie Tailwind verwenden, können Sie den Button div einblenden, um im dunklen Modus korrekt zu arbeiten (sonst wird ein weißer Hintergrund um den Button gerendert)

```html
<div class="w-[200px] h-[39px] overflow-hidden rounded">
    <div id="google_button">
    </div>
</div>
```

Im obigen JavaScript poste ich dies zurück zu einer Controller-Aktion namens Login. Hier kümmere ich mich um die Google Auth.

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

## Google Auth im Controller

Der Controller ist hier' es ist ziemlich einfach, es nimmt nur die gepostete JWT, entschlüsselt es dann verwendet, dass um sich bei der App.

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

HINWEIS: Dies ist kein Präfekt, da es die Anspruchsnamen aufgibt (sie sind alle kleiner) aber es funktioniert für den Moment.

### Controller Basisklasse zum Extrahieren der Login-Eigenschaften

In meinem BaseController extrahiere ich die Eigenschaften, die ich brauche;

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

Und das war's! So können Sie Gooogle Authentication verwenden, ohne die ASP.NET Identity Datenbank zu verwenden.