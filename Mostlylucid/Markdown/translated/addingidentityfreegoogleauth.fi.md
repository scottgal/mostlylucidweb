# Google Authin lisääminen ilman ASP.NET-identiteettitietokantaa

<!--category-- ASP.NET, Google Auth -->
<datetime class="hidden">2024-08-05T08:06</datetime>

## Johdanto

Tässä sovelluksessa halusin lisätä yksinkertaisen mekanismin, jonka avulla sisäänkirjautuminen voi lisätä kommentteja (ja joitakin admin-tehtäviä) sovellukseen. Halusin käyttää Google Authia tähän tarkoitukseen. En halunnut käyttää ASP.NET-identiteettitietokantaa tähän tarkoitukseen. Halusin pitää sovelluksen mahdollisimman yksinkertaisena mahdollisimman pitkään.

Tietokannat ovat tehokas osa kaikkia sovelluksia, mutta ne lisäävät myös monimutkaisuutta. Halusin välttää sen monimutkaisuuden, kunnes todella tarvitsin sitä.

[TÄYTÄNTÖÖNPANO

## Vaiheet

Ensin Google Auth on perustettava Google Developer Consoleen. Voit seurata askeleita tässä [linkki](https://developers.google.com/identity/gsi/web/guides/overview) Saadaksesi tietosi valmiiksi sinulle Google Client ID ja Secret.

Kun sinulla on Google Client ID ja Secret, voit lisätä ne asetuksiin.json-tiedostoosi.

```json
    "Auth" :{
"GoogleClientId": "",
"GoogleClientSecret": ""
}
```

ÄLÄ tsekkaa näitä lähdeohjaukseen. Paikallisen kehityksen sijaan voit käyttää Secrets-tiedostoa:

![secrets.png](secrets.png)

Siellä voit lisätä Google Client ID:si ja salaisuutesi (huomatkaa, että asiakkaasi henkilötunnus ei ole luottamuksellinen, kuten näette myöhemmin, että se on mukana JS:n puhelussa etupäässä.

```json
    "Auth" :{
  "GoogleClientId": "ID",
  "GoogleClientSecret": "CLIENTSECRET"
}
```

## Google Authin konfigurointi POCO:n kanssa

Huomaa, että käytän muokattua versiota Steve Smithin IConfig-osiosta (Phil Haackin äskettäin tekemä).
Näin vältytään Ioptions-jutuilta, joita pidän hieman kömpelöinä (ja harvoin kaipaan, koska en juuri koskaan muuta konfiguraatiota käyttöönoton jälkeen skenaarioissani).

Omassa työssäni teen tämän, jonka avulla saan osaston nimen itse luokalta:

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
Joten minun Authini näyttää siltä, että

```csharp
public class Auth : IConfigSection
{
    public static string Section => "Auth";
    public string GoogleClientId { get; set; }
    public string GoogleClientSecret { get; set; }
    
    public string AdminUserGoogleId { get; set; }
    
}
```

Jossa käytän staattista rajapintamenetelmää saadakseni osion nimen.

Sitten startup-yrityksessäni voin tehdä tämän:

```csharp
var auth = builder.GetConfig<Auth>();
```

Takaisin google-juttuihin!

## Ohjelma.cs-asetus

Lisää tämä oikeasti

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

Huomaat, että täällä on CORS-merkintöjä, sinun täytyy myös asettaa ne google-identiteettikonsoliin.

![googleidentity.png](googleidentity.png)

Näin varmistetaan, että Google Authia voidaan käyttää vain määrittelemiltäsi verkkoalueilta.

## Google Auth Razorissa

Omassa _Layout.cshtml Minulla on tämä Javascript, täällä pystytän Google-nappini ja käynnistän takaisinkutsun, joka kirjautuu ASP.NET-sovellukseen.

# Google JS

```html
<script src="https://accounts.google.com/gsi/client" async defer></script>
```

Tämä on alla olevan koodin hitaus

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

Tässä näet, että sivulla on jopa kaksi div-elementtiä, joissa on id-google_painike ja google_painike2. Nämä ovat elementtejä, joihin Google JS tekee painikkeet.

TIP: Jos käytät Tailwindiä, voit shinkata painikkeen div toimimaan oikein pimeässä tilassa (muuten se kääntää valkoisen taustan painikkeen ympärille)

```html
<div class="w-[200px] h-[39px] overflow-hidden rounded">
    <div id="google_button">
    </div>
</div>
```

Yllä olevassa JavaScript-julkaisussa lähetän tämän takaisin rekisterinpitäjän toimintaan nimeltä Login. Täällä minä hoidan Google Authin.

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

## Google Auth ohjaimessa

Hallinta on tässä", on aika yksinkertaista, että se vain vie lähetetyn JWT:n, purkaa sen ja käyttää sitä kirjautuakseen sovellukseen.

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

HUOMAUTUS: Tämä ei ole prefekti, koska se sotkee valtausnimiä (kaikki ne ovat pienempiä), mutta se toimii toistaiseksi.

### Hallinta Base Class kirjautumisominaisuuksien poistoon

BaseControllerissani poistan tarvitsemani ominaisuudet.

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

Siinä kaikki! Näin voit käyttää Gooogle Authenticationia ilman ASP.NET-identiteettitietokantaa.