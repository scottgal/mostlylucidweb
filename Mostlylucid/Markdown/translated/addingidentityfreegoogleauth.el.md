# Προσθήκη Google Auth χωρίς βάση δεδομένων ταυτότητας ASP.NET

<!--category-- ASP.NET, Google Auth -->
<datetime class="hidden">2024-08-05T08:06</datetime>

## Εισαγωγή

Σε αυτή την εφαρμογή ήθελα να προσθέσω έναν αυθόρμητο μηχανισμό που επιτρέπει τη σύνδεση για να προσθέσετε σχόλια (και κάποιες διοικητικές εργασίες) στην εφαρμογή. Ήθελα να χρησιμοποιήσω το Google Auth για αυτό το σκοπό. Δεν ήθελα να χρησιμοποιήσω τη βάση δεδομένων ταυτότητας ASP.NET για το σκοπό αυτό. Ήθελα να κρατήσω την εφαρμογή όσο το δυνατόν πιο απλή για όσο το δυνατόν περισσότερο.

Οι βάσεις δεδομένων είναι ένα ισχυρό συστατικό κάθε εφαρμογής, αλλά προσθέτουν επίσης πολυπλοκότητα. Ήθελα να αποφύγω αυτή την πολυπλοκότητα μέχρι που την χρειαζόμουν πραγματικά.

[TOC]

## Βήματα

Πρώτα θα πρέπει να ρυθμίσετε το Google Auth στην κονσόλα προγραμματιστών Google. Μπορείτε να ακολουθήσετε τα βήματα σε αυτό [σύνδεσμος](https://developers.google.com/identity/gsi/web/guides/overview) για να ρυθμίσετε τα στοιχεία σας Google Client ID και μυστικό.

Μόλις έχετε το Google Client ID και το Secret, μπορείτε να τα προσθέσετε στο αρχείο σας appsettings.json.

```json
    "Auth" :{
"GoogleClientId": "",
"GoogleClientSecret": ""
}
```

ΠΩΣ ΔΕΝ πρέπει να τα ελέγχετε αυτά στο σύστημα ελέγχου της πηγής. Αντί για τοπική ανάπτυξη μπορείτε να χρησιμοποιήσετε το αρχείο Secrets:

![secrets.png](secrets.png)

Εκεί μπορείτε να προσθέσετε το Google Client ID και το Μυστικό (σημειώστε ότι ο πελάτης σας Id δεν είναι στην πραγματικότητα εμπιστευτικός, όπως θα δείτε αργότερα περιλαμβάνεται στην κλήση JS στο μπροστινό άκρο.

```json
    "Auth" :{
  "GoogleClientId": "ID",
  "GoogleClientSecret": "CLIENTSECRET"
}
```

## Ρύθμιση Google Auth με POCO

Σημείωση Χρησιμοποιώ μια τροποποιημένη έκδοση του IConfigSection του Steve Smith (πρόσφατα έγινε διάσημος από τον Phil Hack).
Αυτό είναι για να αποφευχθεί η IOptions πράγματα που βρίσκω λίγο clunky (και σπάνια χρειάζεται όπως σχεδόν ποτέ δεν αλλάζω config μετά την ανάπτυξη στα σενάρια μου).

Στο δικό μου κάνω αυτό που μου επιτρέπει να πάρω το όνομα τομέα από την ίδια την τάξη:

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
Οπότε ο Ούθ μου μοιάζει...

```csharp
public class Auth : IConfigSection
{
    public static string Section => "Auth";
    public string GoogleClientId { get; set; }
    public string GoogleClientSecret { get; set; }
    
    public string AdminUserGoogleId { get; set; }
    
}
```

Όπου χρησιμοποιώ μια στατική μέθοδο διεπαφής για να πάρω το όνομα τομέα.

Τότε στην αρχή μου μπορώ να το κάνω αυτό:

```csharp
var auth = builder.GetConfig<Auth>();
```

Τέλος πάντων, πίσω στο google!

## Ρύθμιση προγράμματος.cs

ΓΙΑ να προσθέσετε πραγματικά αυτό

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

Θα σημειώσετε ότι υπάρχουν καταχωρήσεις CRS εδώ, θα πρέπει επίσης να εγκαταστήσετε αυτά στην κονσόλα ταυτότητας google.

![googleidentity.png](googleidentity.png)

Αυτό εξασφαλίζει ότι το Google Auth μπορεί να χρησιμοποιηθεί μόνο από τους τομείς που καθορίζετε.

## Google Auth In Razor

Στο............................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................. _Διάταξη.cshtml Έχω αυτό το Javascript, εδώ είναι που στήνω το Google Κουμπιά μου και ενεργοποιώ μια κλήση που καταγράφει την εφαρμογή ASP.NET.

# Google JS

```html
<script src="https://accounts.google.com/gsi/client" async defer></script>
```

Αυτό είναι το dlow για τον παρακάτω κωδικό

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

Εδώ μπορείτε να δείτε Έχω μέχρι δύο στοιχεία div στη σελίδα με το id google_button και το google_button2. Αυτά είναι τα στοιχεία που το Google JS θα καταστήσει τα κουμπιά σε.

TIP: Αν χρησιμοποιείτε Tailwind, μπορείτε να λάμψετε το κουμπί div για να λειτουργήσει σωστά σε σκοτεινή λειτουργία (αλλιώς καθιστά ένα λευκό φόντο γύρω από το κουμπί)

```html
<div class="w-[200px] h-[39px] overflow-hidden rounded">
    <div id="google_button">
    </div>
</div>
```

Στο JavaScript παραπάνω αναρτώ αυτό πίσω σε μια δράση Controller που ονομάζεται Login. Εδώ είναι που χειρίζομαι το Google Auth.

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

## Google Auth στο χειριστήριο

Το Controller είναι εδώ' είναι αρκετά απλό παίρνει απλά το δημοσιεύτηκε JWT, αποκωδικοποιεί στη συνέχεια χρησιμοποιεί ότι για να συνδεθείτε στην εφαρμογή.

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

ΣΗΜΕΙΩΣΗ: Αυτό δεν είναι νομάρχης καθώς χρησιμοποιεί τα ονόματα διεκδικήσεων (είναι όλα χαμηλότερη περίπτωση) αλλά λειτουργεί προς το παρόν.

### Controller Base Class για την εξαγωγή των ιδιοτήτων σύνδεσης

Στο BaseController μου εξάγω τις ιδιότητες που χρειάζομαι?

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

Και αυτό ήταν! Αυτό σας επιτρέπει να χρησιμοποιήσετε το Gooogle Authentication χωρίς να χρησιμοποιείτε τη βάση δεδομένων ταυτότητας ASP.NET.