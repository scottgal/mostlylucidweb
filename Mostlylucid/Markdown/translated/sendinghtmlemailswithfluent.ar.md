# إرسال رسائل البريد الإلكتروني HTML من ASP.net core مع FluentEmail

<datetime class="hidden">2024-08-08-07T00:30</datetime>

<!--category-- ASP.NET, FluentEmail -->
هذه مادة بسيطة إلى حد ما ولكنها ستغطي بعض من عوارض استخدام [](https://github.com/lukencode/FluentEmail) إلى إرسال رسائل البريد الإلكتروني HTML لم أرها في مكان آخر.

## المشكلة

إرسال رسائل HTML هي نفسها نوعاً ما بسيطة مع SmtpClient، لكنها ليست مرنة جداً ولا تدعم أشياء مثل قوالب أو ملحقات. FluentEmail هي مكتبة كبيرة لهذا، ولكن ليس من الواضح دائما كيفية استخدامها في ASP.NET الأساسية.

FluentEmail مع ريزورلايت (هو بوصة) يسمح لك إلى قالب بريد إلكتروني باستخدام zaur systatus. هذا عظيم حيث أنه يسمح لك باستخدام القوة الكاملة من ريزور لإنشاء البريد الإلكتروني الخاص بك.

## الإحلال

أولاً، تحتاج إلى تثبيت FluentEmail.Core, fluentEmail.Smtp & fluentEmail.Razor المكتبات:

```bash
dotnet add package FluentEmail.Core
dotnet add package FluentEmail.Smtp
dotnet add package FluentEmail.Razor
```

## إنشاء إنشاء غير صالح البريد

للحفاظ على كل شيء منفصل، قمت بعد ذلك بإنشاء امتداد ISERVE Collective الذي ينشئ خدمات FluentEmail:

```csharp
namespace Mostlylucid.Email;

public static class Setup
{
    public static void SetupEmail(this IServiceCollection services, IConfiguration config)
    {
          var smtpSettings = services.ConfigurePOCO<SmtpSettings>(config.GetSection(SmtpSettings.Section));

        services.AddFluentEmail(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .AddRazorRenderer();

        services.AddSingleton<ISender>(new SmtpSender( () => new SmtpClient()
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Host = smtpSettings.Server,
            Port = smtpSettings.Port,
            Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password),
            EnableSsl = smtpSettings.EnableSSL,
            UseDefaultCredentials = false
        }));
        services.AddSingleton<EmailService>();
        
    }

}
```

إعدادات

كما سترون لقد استخدمت أيضاً طريقة IConfeg section [(أ) كان](blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco) إلى get sMTP خصائص.

```csharp
  var smtpSettings = services.ConfigurePOCO<SmtpSettings>(config.GetSection(SmtpSettings.Section));
```

هذا مصدر من adminesings. Jisson ملفّ:

```json
"SmtpSettings":
  {
    "Server": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "Mostlylucid",
    "Username": "",
    "SenderEmail": "scott.galloway@gmail.com",
    "Password": "",
    "EnableSSL": "true",
    "EmailSendTry": 3,
    "EmailSendFailed": "true",
    "ToMail": "scott.galloway@gmail.com",
    "EmailSubject": "Mostlylucid"
    
  }
```

## GMAIL / جوجل SMTP

ملاحظة لـ لـ جو جوجل SMTP IF استخدام MF ( الذي أنت **ال______________________* يجب عليك أن تقوم بـ [كلمة سر لحسابك](https://myaccount.google.com/apppasswords).

لـ محلي dev، يمكنك إضافة هذا إلى أسرارك. json ملفّ:

![secrets.png](secrets.png)

### يجري إعداد

لـ استخدام عادة تضمين هذا في a ملفّ:

```env
SMTPSETTINGS_USERNAME="scott.galloway@gmail.com"
SMTPSETTINGS_PASSWORD="<MFA PASSWORD>" -- this is the app password you created

```

ثم في حالة doker compus file، تقوم بحقن هذه كمتغيرات مجزأة:

```yaml
services:
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    environment:
      - SmtpSettings__UserName=${SMTPSETTINGS_USERNAME}
      - SmtpSettings__Password=${SMTPSETTINGS_PASSWORD}
```

خُذ ملاحظة عن التباعد لأن هذا يُمْكِنُ أَنْ يُشوّشَك حقاً مَع ألحانِ ducrker. لتحقق ما الذي حقنته يمكنك استخدامه

```bash
docker compose config
```

لأريك كيف يبدو الملف مع هذه الحقنة.

## &:::::::::::::::::::::::::::::::::::::::::::::::

مسألة واحدة مع المحرف المحرف البريد الإلكتروني هو أنك تحتاج إلى إضافة هذا إلى csproj

```xml
  <PropertyGroup>
    <PreserveCompilationContext>true</PreserveCompilationContext>
  </PropertyGroup>
```

هذا لأن FlantEmail يستخدم RazorLight الذي يحتاج هذا للعمل.

لـ لـ قالب ملفات، يمكنك إمّا أن تدرجها في مشروعك كحافظات مضمون أو كما أفعل في وعاء Doker، انسخ الملفات إلى الصورة النهائية

```yaml
FROM build AS publish
RUN dotnet publish "Mostlylucid.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
# Copy the Markdown directory
COPY ./Mostlylucid/Markdown /app/Markdown
COPY ./Mostlylucid/Email/Templates /app/Email/Templates
# Switch to a non-root user
USER $APP_UID
```

## خدمات خدمات خدمات

حسناً، عد إلى الشفرة!

الآن لدينا كل شيء جاهز يمكننا أن نضيف خدمة البريد الإلكتروني. هذه خدمة بسيطة تأخذ القالب وترسل البريد الإلكتروني:

```csharp
public class EmailService(SmtpSettings smtpSettings, IFluentEmail fluentEmail)
{
    public async Task SendCommentEmail(string commenterEmail, string commenterName, string comment, string postSlug)
    {
        var commentModel = new CommentEmailModel
        {
            PostSlug = postSlug,
            SenderEmail = commenterEmail,
            SenderName = commenterName,
            Comment = comment
        };
        await SendCommentEmail(commentModel);
    }

    public async Task SendCommentEmail(CommentEmailModel commentModel)
    {
        // Load the template
        var templatePath = "Email/Templates/MailTemplate.template";
        await SendMail(commentModel, templatePath);
    }

    public async Task SendContactEmail(ContactEmailModel contactModel)
    {
        var templatePath = "Email/Templates/ContactEmailModel.template";

        await SendMail(contactModel, templatePath);
    }


    public async Task SendMail(BaseEmailModel model, string templatePath)
    {
        var template = await File.ReadAllTextAsync(templatePath);
        // Use FluentEmail to send the email
        var email = fluentEmail.UsingTemplate(template, model);
        await email.To(smtpSettings.ToMail)
            .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .Subject("New Comment")
            .SendAsync();
    }
}
```

كما ترون هنا، لدينا طريقتان، واحدة للتعليقات وواحدة لنموذج الاتصال.[أرسل لي بريداً!](/contact) )ع( في هذا التطبيق سأجعلك تسجل الدخول فيه حتى أتمكن من الحصول على البريد الذي هو من (ولتجنب التغريد).

معظم العمل يتم هنا:

```csharp
 var template = await File.ReadAllTextAsync(templatePath);
        // Use FluentEmail to send the email
        var email = fluentEmail.UsingTemplate(template, model);
        await email.To(smtpSettings.ToMail)
            .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .Subject("New Comment")
            .SendAsync();
```

ها نحن نفتح ملفاً، نضيف النموذج الذي يحتوي على محتوى البريد الإلكتروني، نحمله إلى FluentEmail ثم نرسله. الـ قالب هو a بسيط ميزّر ملفّ:

```razor
@model Mostlylucid.Email.Models.ContactEmailModel

<!DOCTYPE html>
<html class="dark">
<head>
    <title>Comment Email</title>
</head>
<body>
<h1>Comment Email</h1>
<p>New comment from email @Model.SenderEmail name @Model.SenderName</p>

<p>Thank you for your comment on our blog post. We appreciate your feedback.</p>
<p>Here is your comment:</p>
<div>
    @Raw( @Model.Comment)</div>
<p>Thanks,</p>
<p>The Blog Team</p>

</body>
</html>
```

هذه محفوظة كملفات طوابق في مجلد البريد الإلكتروني / Templats. يمكنك استخدام ملفات cshtml لكنها تسبب مشكلة مع بطاقة الـ @Raw في قالب (إنّه شيء مفضّل).

## المراقب المالي

في النهاية نصل الى المتحكم، انه في الواقع جداً مباشرة

```csharp
    [HttpPost]
    [Route("submit")]
    [Authorize]
    public async Task<IActionResult> Submit(string comment)
    {
        var user = GetUserInfo();
            var commentHtml = commentService.ProcessComment(comment);
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await emailService.SendContactEmail(contactModel);
            return PartialView("_Response", new ContactViewModel(){Email = user.email, Name = user.name, Comment = commentHtml, Authenticated = user.loggedIn});

        return RedirectToAction("Index", "Home");
    }
```

هنا نحصل على معلومات المستخدم، معالجة التعليق (أستخدم معالجا بسيطا مع مُشَكِّل إلى تحويل علامة أسفل إلى HTML) ومن ثم إرسال البريد الإلكتروني.