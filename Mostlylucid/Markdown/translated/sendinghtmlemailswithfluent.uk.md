# Надсилання HTML- листів з ядра ASP. NET з FluentEmail

<datetime class="hidden">2024- 08- 07T00: 30</datetime>

<!--category-- ASP.NET, FluentEmail -->
Це досить проста стаття, але вона покриє деякі з нерозсудливих можливостей використання [FluentEmail](https://github.com/lukencode/FluentEmail) у Core ASP.NET для надсилання HTML- листів, яких я не бачив десь інше.

## Проблема

Надсилання HTML-пошти само по собі досить просто з SmtpClient, але воно не дуже гнучке і не підтримує такі речі, як шаблони або долучення. FluentEmail - це чудова бібліотека для цього, але не завжди зрозуміло, як нею користуватися у ядрах ASP. NET.

FluentEmail з параметром Razorlight (його вбудовано) надає вам змогу шаблонувати ваші листи електронною поштою за допомогою синтаксису Razor. Цей пункт є чудовим, оскільки ви можете використовувати всю потужність Razor для створення ваших листів електронної пошти.

## Розв'язання

Спочатку вам слід встановити бібліотеки FluentEmail. Corre, FluentEmail. Smtp і FluentEmail. Razor:

```bash
dotnet add package FluentEmail.Core
dotnet add package FluentEmail.Smtp
dotnet add package FluentEmail.Razor
```

## Налаштування fluentEmail

Щоб все було окремо, я створив додаток IserviceCollection, за допомогою якого можна налаштувати служби FluentEmail:

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

Параметри # # # SMTP

Як бачите, я також використовував метод IConfigSection, згаданий у моєму [Попередня стаття](blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco) щоб отримати параметри SMTP.

```csharp
  var smtpSettings = services.ConfigurePOCO<SmtpSettings>(config.GetSection(SmtpSettings.Section));
```

Цей пункт можна знайти у файлі app settingss. json:

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

## GMAIL / Google SMTP

Зауваження: для Google SMTP, якщо ви використовуєте MFA (яку ви використовуєте) **дійсно* якщо тобі треба буде зробити [Пароль програми для вашого облікового запису](https://myaccount.google.com/apppasswords).

Для локального dev ви можете додати цей файл до вашого файла конфіденційності. json:

![secrets.png](secrets.png)

### Налаштування панелі

Для використання у Docker Складання ви, зазвичай, включаєте це у файл. env:

```env
SMTPSETTINGS_USERNAME="scott.galloway@gmail.com"
SMTPSETTINGS_PASSWORD="<MFA PASSWORD>" -- this is the app password you created

```

Після цього у файлі набору docker ви ввели такі змінні як env:

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

Зверніть увагу на інтервали, оскільки це може дійсно вас заплутати з комбінацією докерів. Щоб перевірити, що ін'єкції ви можете використовувати

```bash
docker compose config
```

Щоб показати вам як виглядає файл за допомогою цих ін'єкцій.

## Енергії FluentEmail

Одна з проблем з електронною поштою Fluent полягає у тому, що вам слід додати це до вашого csproj

```xml
  <PropertyGroup>
    <PreserveCompilationContext>true</PreserveCompilationContext>
  </PropertyGroup>
```

Це тому, що FluentEmail використовує Razor Light, щоб це спрацювало.

Для файлів шаблонів ви можете або включити їх до вашого проекту як файли вмісту, або як це робиться у контейнері docker, скопіювати файли до остаточного зображення

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

## Служба ел. пошти

Повернемося до коду!

Тепер у нас є все, що налаштовано, ми можемо додати Служба пошти. Це проста служба, яка отримує шаблон і надсилає повідомлення електронної пошти:

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

Як ви бачите, існує два способи: одне для коментарів, а інше для форми контакту ([Відправте мені листа!](/contact) ). За допомогою цієї програми ви можете увійти до системи, щоб отримати повідомлення з (і уникнути спаму).

Насправді, більшість роботи виконується тут:

```csharp
 var template = await File.ReadAllTextAsync(templatePath);
        // Use FluentEmail to send the email
        var email = fluentEmail.UsingTemplate(template, model);
        await email.To(smtpSettings.ToMail)
            .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .Subject("New Comment")
            .SendAsync();
```

Тут ми відкриваємо файл шаблону, додаємо модель, що містить вміст повідомлення електронної пошти, завантажуємо його до FluentEmail, а потім надсилаємо його. Шаблон - це простий файл Razor:

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

Ці файли зберігаються як файли. template у теці email/ Templates. Ви можете використовувати файли. cshtml, але причиною цього є мітка @ paw у шаблоні (це інструмент для роботи з бритвою).

## Контроль

Нарешті, ми дісталися до контролера; це дуже просто.

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

Тут ми отримуємо інформацію про користувача, обробимо коментар (використовую простий процесор markdown з Markdig для перетворення markdown до HTML), а потім надсилання повідомлення електронної пошти.