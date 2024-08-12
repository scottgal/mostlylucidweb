# Agregar Google Auth sin ASP.NET Identity Database

<!--category-- ASP.NET, Google Auth -->
<datetime class="hidden">2024-08-05T08:06</datetime>

## Introducción

En esta aplicación quería añadir un simple mecanismo de permitir que el inicio de sesión agregara comentarios (y algunas tareas de administración) a la aplicación. Quería usar Google Auth para este propósito. No quería usar la base de datos de identidad ASP.NET para este propósito. Quería mantener la aplicación lo más simple posible durante el mayor tiempo posible.

Las bases de datos son un componente poderoso de cualquier aplicación, pero también añaden complejidad. Quería evitar esa complejidad hasta que realmente lo necesitaba.

[TOC]

## Pasos

Primero tienes que configurar Google Auth en la Consola de Desarrolladores de Google. Puedes seguir los pasos en esta[enlace](https://developers.google.com/identity/gsi/web/guides/overview)para obtener sus datos configurados para usted Google Client ID y Secret.

Una vez que tengas tu Google Client ID y Secret, puedes añadirlos a tu archivo appsettings.json.

```json
    "Auth" :{
"GoogleClientId": "",
"GoogleClientSecret": ""
}
```

Sin embargo, no debe registrarse para el control de origen. En lugar de ello, para el desarrollo local puede utilizar el archivo Secrets:

![secrets.png](secrets.png)

Allí puedes agregar tu Google Client ID y Secret (nota que tu cliente Id no es en realidad confidencial, como verás más adelante se incluye en la llamada de JS en la parte delantera.

```json
    "Auth" :{
  "GoogleClientId": "ID",
  "GoogleClientSecret": "CLIENTSECRET"
}
```

## Configuración de Google Auth con POCO

Nota Yo uso una versión modificada de la IConfigSection de Steve Smith (recientemente hecha famosa por Phil Haack).
Esto es para evitar las cosas de IOptions que encuentro un poco torpe (y rara vez necesito ya que casi nunca cambio la configuración después de la implementación en mis escenarios).

En el mío hago esto que me permite obtener el nombre de la sección de la propia clase:

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
Así que mi Auth parece

```csharp
public class Auth : IConfigSection
{
    public static string Section => "Auth";
    public string GoogleClientId { get; set; }
    public string GoogleClientSecret { get; set; }
    
    public string AdminUserGoogleId { get; set; }
    
}
```

Donde uso un método de interfaz estática para obtener el nombre de la sección.

Entonces en mi startup puedo hacer esto:

```csharp
var auth = builder.GetConfig<Auth>();
```

¡De todos modos de vuelta a las cosas de Google!

## Program.cs Setup

Para agregar esto realmente

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

Notarás que hay entradas de CORS aquí, también necesitas configurarlas en la consola de identidad de Google.

![googleidentity.png](googleidentity.png)

Esto garantiza que la Auth de Google sólo se puede utilizar desde los dominios que especifique.

## Google Auth In Razor

En mi_Layout.cshtml Tengo este Javascript, aquí es donde configuré mis botones de Google y desencadené un callback que registra la aplicación ASP.NET.

# Google JS

```html
<script src="https://accounts.google.com/gsi/client" async defer></script>
```

Este es el dlow para el código de abajo

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

Aquí puedes ver que tengo hasta dos elementos div en la página con el botón id google_button y el botón google_button2. Estos son los elementos en los que la JS de Google mostrará los botones.

SUGERENCIA: Si está usando Tailwind, puede hacer brillar el botón div para que funcione correctamente en modo oscuro (de lo contrario, representa un fondo blanco alrededor del botón)

```html
<div class="w-[200px] h-[39px] overflow-hidden rounded">
    <div id="google_button">
    </div>
</div>
```

En el JavaScript de arriba lo devuelvo a una acción de Controlador llamada Login. Aquí es donde manejo la Auth de Google.

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

## Google Auth en el controlador

El controlador está aquí' es bastante simple que sólo toma el JWT publicado, decodifica que luego utiliza para iniciar sesión en la aplicación.

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

NOTA: Esto no es prefecto ya que esse hasta los nombres de la reclamación (todos son minúsculas) pero funciona por ahora.

### Controlador Clase Base para extraer las propiedades de inicio de sesión

En mi BaseController extraigo las propiedades que necesito;

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

¡Y eso es todo! Esto te permite usar la Autenticación de Gooogle sin usar la base de datos de identidad ASP.NET.