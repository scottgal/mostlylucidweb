# Skicka HTML-mail från ASP.NET Core med FluentEmail

<datetime class="hidden">2024-08-07T00:30</datetime>

<!--category-- ASP.NET, FluentEmail -->
Detta är en ganska enkel artikel men kommer att täcka några av de odness av att använda [FluentEmail](https://github.com/lukencode/FluentEmail) i ASP.NET Core för att skicka HTML-mail jag inte har sett någon annanstans.

## Problemet

Att skicka HTML-meddelanden är i sig ganska enkelt med SmtpClient, men det är inte särskilt flexibelt och stöder inte saker som mallar eller bilagor. FluentEmail är ett bra bibliotek för detta, men det är inte alltid klart hur man använder det i ASP.NET Core.

FluentEmail med Razorlight (det är inbyggt) låter dig mallera dina e-postmeddelanden med hjälp av Razor syntax. Detta är bra eftersom det tillåter dig att använda den fulla kraften i Razor för att skapa dina e-postmeddelanden.

## Lösningen

För det första måste du installera FluentEmail.Core, FluentEmail.Smtp & FluentEmail.Razor bibliotek:

```bash
dotnet add package FluentEmail.Core
dotnet add package FluentEmail.Smtp
dotnet add package FluentEmail.Razor
```

## Ställa in FluentEmail

För att hålla saker och ting åtskilda skapade jag sedan ett tillägg till IServiceCollection som sätter upp FluentEmail-tjänsterna:

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

## SMTP- inställningar

Som ni ser använde jag även IConfigsektionsmetoden som nämns i min [tidigare artikel](blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco) för att få SMTP-inställningarna.

```csharp
  var smtpSettings = services.ConfigurePOCO<SmtpSettings>(config.GetSection(SmtpSettings.Section));
```

Detta kommer från appsettings.json-filen:

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

Obs: För Google SMTP om du använder MFA (som du **verkligen* om du behöver göra en [app-lösenord för ditt konto](https://myaccount.google.com/apppasswords).

För lokal dev, kan du lägga till detta till din chemles.json fil:

![secrets.png](secrets.png)

### Dockningsinställning

För Docker komponera användning skulle du normalt inkludera detta i en.env-fil:

```env
SMTPSETTINGS_USERNAME="scott.galloway@gmail.com"
SMTPSETTINGS_PASSWORD="<MFA PASSWORD>" -- this is the app password you created

```

Sedan i Docker komponera fil du injicerar dessa som env variabler:

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

Lägg märke till avståndet eftersom detta verkligen kan förstöra dig med Docker komponera. För att kontrollera vad som injiceras kan du använda

```bash
docker compose config
```

För att visa dig hur filen ser ut med dessa injicerade.

## FluentEmail Oanständigheter

Ett problem med Fluent e-post är att du måste lägga till detta till din csproj

```xml
  <PropertyGroup>
    <PreserveCompilationContext>true</PreserveCompilationContext>
  </PropertyGroup>
```

Detta beror på att FluentEmail använder RazorLight som behöver detta för att fungera.

För mallfilerna kan du antingen inkludera dem i ditt projekt som Innehållsfiler eller som jag gör i Docker-behållaren, kopiera filerna till den slutliga bilden

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

## E-posttjänst

Tillbaka till koden!

Nu har vi allt inställt vi kan lägga till e-posttjänsten. Detta är en enkel tjänst som tar en mall och skickar ett e-postmeddelande:

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

Som ni kan se här har vi två metoder, en för Kommentarer och en för kontaktformuläret ([Skicka mig en post!](/contact) ).............................................................................................. I den här appen får jag dig att logga in så att jag kan få den e-post den är från (och för att undvika spam).

Det mesta av arbetet görs här:

```csharp
 var template = await File.ReadAllTextAsync(templatePath);
        // Use FluentEmail to send the email
        var email = fluentEmail.UsingTemplate(template, model);
        await email.To(smtpSettings.ToMail)
            .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .Subject("New Comment")
            .SendAsync();
```

Här öppnar vi en mallfil, lägger till modellen som innehåller innehållet för e-postmeddelandet, laddar in den i FluentEmail och skickar den sedan. Mallen är en enkel Razor-fil:

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

Dessa lagras som.template-filer i mappen Email/Templates. Du kan använda.cshtml-filer men det orsakar ett problem med @Raw taggen i mallen (det är en rakhyvelljus sak).

## Kontrollanten

Slutligen kommer vi till regulatorn; det är verkligen ganska enkelt

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

Här får vi användarinformation, bearbeta kommentaren (Jag använder en enkel markdown-processor med Markdig för att konvertera markdown till HTML) och sedan skicka e-post.