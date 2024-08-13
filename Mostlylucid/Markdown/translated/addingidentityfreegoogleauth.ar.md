# اض إضافة جو جو جو جو جو جو جوجل ASP. net الهوية قاعدة بيانات

<!--category-- ASP.NET, Google Auth -->
<datetime class="hidden">2024-08-08-05-TT08: 06</datetime>

## أولاً

في هذا التطبيق أردت أن أضيف آلية مماثلة للسماح بالدخول لإضافة التعليقات (وبعض مهام الإدارة) إلى التطبيق. أردت استخدام جوجل Auth لهذا الغرض. لم أكن أريد استخدام قاعدة بيانات هوية ASP.NET لهذا الغرض. أردت أن أبقي التطبيق بسيطاً قدر الإمكان لأطول فترة ممكنة

وتشكل قواعد البيانات عنصراً قوياً في أي تطبيق ولكنها تزيد أيضاً من التعقيد. أردت أن أتجنب ذلك التعقيد حتى أحتاجه حقاً

[رابعاً -

## )أ))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))

أولاً تحتاج إلى إنشاء جوجل Auth في مطور جوجل Console. يمكنك متابعة الخطوات في هذا [](https://developers.google.com/identity/gsi/web/guides/overview) للحصول على التفاصيل الخاصة بك إعداد حتى بالنسبة لك جوجل العميل الهوية والسرية.

عندما يكون لديك هوية عميل جوجل و سرك، يمكنك إضافتهما إلى ملفك. Jesson.

```json
    "Auth" :{
"GoogleClientId": "",
"GoogleClientSecret": ""
}
```

على أي حال لا يجب عليك التحقق من هذه إلى مصدر السيطرة. بدلاً من التنمية المحلية يمكنك استخدام ملف الأسرار:

![secrets.png](secrets.png)

في الداخل يمكنك إضافة الهوية والسرية الخاصة بعميلك في جوجل (لاحظ أن هوية عميلك ليست سرية في الواقع، كما سترى لاحقاً

```json
    "Auth" :{
  "GoogleClientId": "ID",
  "GoogleClientSecret": "CLIENTSECRET"
}
```

## يجري تنفيذ حوار جو جوجل Auth مع OP CO

ملاحظة أستخدم نسخة معدلة من IConfigs section لستيف سميث (الذي أصبح مشهوراً مؤخراً من قبل فيل هاك).
هذا هو لتجنّب المادة IOVES التي أجد قليلا clunky (ونادرا ما تحتاج لأنني تقريبا لا تغيير التهيئة بعد النشر في سيناريوهاتي).

في الألغام أنا أفعل هذا الذي يسمح لي للحصول على اسم القسم من الفئة نفسها:

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
اذاً Auth تبدو مثل

```csharp
public class Auth : IConfigSection
{
    public static string Section => "Auth";
    public string GoogleClientId { get; set; }
    public string GoogleClientSecret { get; set; }
    
    public string AdminUserGoogleId { get; set; }
    
}
```

حيث أستخدم طريقة الواجهة الساكنة للحصول على اسم القسم.

ثم في بدايتي يمكنني أن أفعل هذا:

```csharp
var auth = builder.GetConfig<Auth>();
```

على أي حال العودة إلى الاشياء غوغل!

## إعداد البرامج

إلى هذا

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

أنت سَتُلاحظُ هناك خاناتَ CORS هنا، أنت أيضاً تَحتاجُ إلى وَضْع هذه في لوحةِ هوية جوجل.

![googleidentity.png](googleidentity.png)

هذا يضمن أن جوجل Auth يمكن استخدامه فقط من المجالات التي تحددها.

## جو جو جو جو جو جو جو جو جو جوس بوصة

(ب) في _البرمجية. cshtml لدي هذا جافاستورج، هنا حيث قمت بوضع زر جوجل واقوم بتحريك نداء يقوم بتسجيل التطبيق ASP.net.

# قوقل GGS

```html
<script src="https://accounts.google.com/gsi/client" async defer></script>
```

هذا هو dlow لـ رمز أسفل

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

يمكنك أن ترى هنا أن لدي ما يصل إلى اثنين من عناصر div في الصفحة مع Id Google_Button و Google_Button 2. هذه هي العناصر التي ستعرض عليها جوجل بورز الأزرار.

TIP: إذا كنت تستخدم تايلwind، فيمكنك أن تُشَكِّل هذا الزر إلى العمل بشكل صحيح في النمط الداكن (غير ذلك من الناحية العملية، فإنّه يعطي خلفية بيضاء حول هذا الزر).

```html
<div class="w-[200px] h-[39px] overflow-hidden rounded">
    <div id="google_button">
    </div>
</div>
```

في مخطوط جافاسكربت أعلاه أنا أضع هذا مرة أخرى إلى إجراء المراقب المالي يسمى الولوج. هذا هو المكان الذي أتعامل فيه مع جوجل Auth.

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

## جو جو جو جو جو جوقل بوصة

المراقب المالي هنا انه بسيط جداً انه يأخذ موقع JWT، ويفك شفراته ثم يستخدم ذلك للدخول الى التطبيق.

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

ملاحظة: هذا ليس محافظاً لأنه يبسط أسماء المطالبين (جميعهم قضايا أقل) لكنه يعمل الآن.

### الفئة الدنيا من الفئة الأساسية لاستخلاص خواص الولوج

في قاعدتي أنا أستخرج الممتلكات التي أحتاجها

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

وهذا هو عليه! يسمح لك هذا إلى استخدام Gooogle موثّق غير مستخدم ASP. net الهوية قاعدة بيانات.