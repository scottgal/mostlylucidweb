# Ajout de Google Auth sans base de données d'identité ASP.NET

<!--category-- ASP.NET, Google Auth -->
<datetime class="hidden">2024-08-05T08:06</datetime>

## Présentation

Dans cette application, je voulais ajouter un mécanisme simple permettant de connecter pour ajouter des commentaires (et quelques tâches d'administration) à l'application. Je voulais utiliser Google Auth à cette fin. Je ne voulais pas utiliser la base de données ASP.NET Identité à cette fin. Je voulais garder l'application aussi simple que possible pour le plus longtemps possible.

Les bases de données sont un composant puissant de toute application, mais elles ajoutent aussi de la complexité. Je voulais éviter cette complexité jusqu'à ce que j'en ai vraiment besoin.

[TOC]

## Étapes

Vous devez d'abord configurer Google Auth dans la console Google Developer. Vous pouvez suivre les étapes de cette[lien](https://developers.google.com/identity/gsi/web/guides/overview)pour obtenir vos détails configurés pour vous Google Client ID et Secret.

Une fois que vous avez votre identifiant Google Client et Secret, vous pouvez les ajouter à votre fichier appsettings.json.

```json
    "Auth" :{
"GoogleClientId": "",
"GoogleClientSecret": ""
}
```

HOWEVER vous ne devriez pas les vérifier dans le contrôle source. Au lieu de cela, pour le développement local, vous pouvez utiliser le fichier Secrets:

![secrets.png](secrets.png)

Vous pouvez y ajouter votre identifiant Google Client et Secret (notez que votre identifiant client n'est pas réellement confidentiel, comme vous le verrez plus tard, il est inclus dans l'appel JS sur le front.

```json
    "Auth" :{
  "GoogleClientId": "ID",
  "GoogleClientSecret": "CLIENTSECRET"
}
```

## Configuration de Google Auth avec POCO

Note J'utilise une version modifiée de IConfigSection de Steve Smith (récemment rendue célèbre par Phil Haack).
C'est pour éviter les trucs IOptions que je trouve un peu maladroits (et dont j'ai rarement besoin car je ne change presque jamais de configuration après le déploiement dans mes scénarios).

Dans le mien, je fais ceci qui me permet d'obtenir le nom de la section de la classe elle-même:

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
Alors mon Auth ressemble à

```csharp
public class Auth : IConfigSection
{
    public static string Section => "Auth";
    public string GoogleClientId { get; set; }
    public string GoogleClientSecret { get; set; }
    
    public string AdminUserGoogleId { get; set; }
    
}
```

Où j'utilise une méthode d'interface statique pour obtenir le nom de la section.

Puis dans ma startup je peux faire ceci:

```csharp
var auth = builder.GetConfig<Auth>();
```

De toute façon, retournez aux trucs de google!

## Configuration du programme.cs

Pour ajouter cela

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

Vous noterez qu'il y a des entrées CORS ici, vous devez aussi les configurer dans la console d'identité google.

![googleidentity.png](googleidentity.png)

Cela garantit que Google Auth ne peut être utilisé que depuis les domaines que vous spécifiez.

## Google Auth In Razor

Dans mon_Layout.cshtml J'ai ce Javascript, c'est là que j'ai configuré mes boutons Google et déclenché un callback qui enregistre l'application ASP.NET.

# Google JS

```html
<script src="https://accounts.google.com/gsi/client" async defer></script>
```

C'est la lunette pour le code ci-dessous

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

Ici vous pouvez voir que j'ai jusqu'à deux éléments de div dans la page avec l'id google_button et google_button2. Ce sont les éléments dans lesquels Google JS va rendre les boutons.

CONSEIL: Si vous utilisez Tailwind, vous pouvez basculer le bouton div pour fonctionner correctement en mode sombre (autrement, il rend un fond blanc autour du bouton)

```html
<div class="w-[200px] h-[39px] overflow-hidden rounded">
    <div id="google_button">
    </div>
</div>
```

Dans le JavaScript ci-dessus, je l'affiche à une action de contrôleur appelée Login. C'est là que je gère Google Auth.

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

## Google Auth dans le contrôleur

Le contrôleur est ici' c'est assez simple il prend juste le JWT posté, décode il utilise ensuite cela pour se connecter à l'application.

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

REMARQUE: Ce n'est pas préfet car il esse les noms de revendication (ils sont tous des cas inférieurs) mais il fonctionne pour l'instant.

### Controller Base Class pour extraire les propriétés de connexion

Dans mon BaseController, j'extrais les propriétés dont j'ai besoin;

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

Et c'est tout! Cela vous permet d'utiliser l'authentification Gooogle sans utiliser la base de données ASP.NET Identity.