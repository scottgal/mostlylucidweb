# Het verzenden van HTML e-mails van ASP.NET Core met FluentEmail

<datetime class="hidden">2024-08-07T00:30</datetime>

<!--category-- ASP.NET, FluentEmail -->
Dit is een vrij eenvoudig artikel, maar zal betrekking hebben op een deel van de ziekte van het gebruik[FluentEmail](https://github.com/lukencode/FluentEmail)in ASP.NET Core om HTML e-mails te versturen die ik elders niet heb gezien.

## Het probleem

Het versturen van HTML mails is zelf een beetje eenvoudig met SmtpClient, maar het is niet erg flexibel en ondersteunt geen dingen zoals sjablonen of bijlagen. FluentEmail is hiervoor een geweldige bibliotheek, maar het is niet altijd duidelijk hoe het te gebruiken in ASP.NET Core.

FluentEmail met Razorlight (het is ingebouwd) kunt u sjabloon uw e-mails met behulp van Razor syntax. Dit is geweldig als het kunt u de volledige kracht van Razor gebruiken om uw e-mails te maken.

## De oplossing

Ten eerste moet u de FluentEmail.Core, FluentEmail.Smtp & FluentEmail.Razor bibliotheken installeren:

```bash
dotnet add package FluentEmail.Core
dotnet add package FluentEmail.Smtp
dotnet add package FluentEmail.Razor
```

## FluentEmail instellen

Om dingen gescheiden te houden creëerde ik vervolgens een IServiceCollection extensie die de FluentEmail diensten opzet:

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

##SMTP Instellingen

Zoals u zult zien heb ik ook de IConfigSectie methode gebruikt die in mijn[vorig artikel](blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)om de SMTP-instellingen te krijgen.

```csharp
  var smtpSettings = services.ConfigurePOCO<SmtpSettings>(config.GetSection(SmtpSettings.Section));
```

Dit komt uit het appsettings.json bestand:

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

Opmerking: Voor Google SMTP als u gebruik maakt van MFA (die u**Echt waar.*moet je een[app-wachtwoord voor uw account](https://myaccount.google.com/apppasswords).

Voor lokale dev kunt u dit toevoegen aan uw secrets.json bestand:

![secrets.png](secrets.png)

### Instellen van de docker

Voor docker componeren gebruik je normaal zou opnemen dit in een.env bestand:

```env
SMTPSETTINGS_USERNAME="scott.galloway@gmail.com"
SMTPSETTINGS_PASSWORD="<MFA PASSWORD>" -- this is the app password you created

```

Dan in de docker componeren bestand u injecteert deze als env variabelen:

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

Neem een notitie van de afstand, want dit kan je echt verknoeien met docker componeren. Om te controleren wat geïnjecteerd u kunt gebruiken

```bash
docker compose config
```

Om je te laten zien hoe het bestand eruit ziet met deze geïnjecteerde.

## VloeiendE-mail Vervelingen

Een probleem met Fluent Email is dat je dit moet toevoegen aan je csproj

```xml
  <PropertyGroup>
    <PreserveCompilationContext>true</PreserveCompilationContext>
  </PropertyGroup>
```

Dit komt omdat FluentEmail gebruik maakt van RazorLight die dit nodig heeft om te werken.

Voor de sjabloon bestanden, kunt u ze opnemen in uw project als Content bestanden of zoals ik doe in de docker container, kopieer de bestanden naar de uiteindelijke afbeelding

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

## E-maildienst

Oké, terug naar de code.

Nu hebben we het allemaal opgezet kunnen we de e-maildienst toevoegen. Dit is een eenvoudige dienst die een sjabloon neemt en een e-mail stuurt:

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

Zoals u hier kunt zien hebben we twee methoden, een voor Reacties en een voor het Contactformulier ([Stuur me een mail!](/contact)). In deze app laat ik je inloggen zodat ik de mail kan krijgen waar het vandaan komt (en om spam te vermijden).

Echt het grootste deel van het werk wordt hier gedaan:

```csharp
 var template = await File.ReadAllTextAsync(templatePath);
        // Use FluentEmail to send the email
        var email = fluentEmail.UsingTemplate(template, model);
        await email.To(smtpSettings.ToMail)
            .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .Subject("New Comment")
            .SendAsync();
```

Hier openen we een sjabloonbestand, voegen het model met de inhoud voor de e-mail toe, laden het in FluentEmail en versturen het vervolgens. Het sjabloon is een eenvoudig Razor bestand:

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

Deze worden opgeslagen als.template bestanden in de map E-mail/Templates. U kunt.cshtml bestanden gebruiken, maar het veroorzaakt een probleem met de @Raw tag in het sjabloon (het is een scheermes ding).

## De controller

Eindelijk komen we bij de controller; het is echt vrij eenvoudig

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

Hier krijgen we de gebruikersinfo, verwerken we het commentaar (ik gebruik een eenvoudige markdown processor met Markdig om markdown te converteren naar HTML) en versturen we de e-mail.