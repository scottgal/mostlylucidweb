# Senden von HTML-Emails von ASP.NET Core mit FluentEmail

<datetime class="hidden">2024-08-07T00:30</datetime>

<!--category-- ASP.NET, FluentEmail -->
Dies ist ein ziemlich einfacher Artikel, aber wird einige der odness der Verwendung [FluentEmail](https://github.com/lukencode/FluentEmail) in ASP.NET Core, um HTML-E-Mails zu senden, die ich anderswo nicht gesehen habe.

## Das Problem

Mit SmtpClient ist das Senden von HTML-Mails selbst ziemlich einfach, aber es ist nicht sehr flexibel und unterstützt Dinge wie Vorlagen oder Anhänge nicht. FluentEmail ist eine großartige Bibliothek dafür, aber es ist nicht immer klar, wie man sie in ASP.NET Core verwendet.

FluentEmail mit Razorlight (es ist eingebaut) ermöglicht es Ihnen, Ihre E-Mails mit Hilfe der Razor-Syntax zu templatieren. Dies ist großartig, da es Ihnen erlaubt, die volle Leistung von Razor verwenden, um Ihre E-Mails zu erstellen.

## Die Lösung

Zuerst müssen Sie die FluentEmail.Core, FluentEmail.Smtp & FluentEmail.Razor Bibliotheken installieren:

```bash
dotnet add package FluentEmail.Core
dotnet add package FluentEmail.Smtp
dotnet add package FluentEmail.Razor
```

## Einrichtung von FluentEmail

Um die Dinge getrennt zu halten, habe ich dann eine IServiceCollection-Erweiterung erstellt, die die FluentEmail-Dienste aufbaut:

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

##SMTP-Einstellungen

Wie Sie sehen werden, habe ich auch die IConfigSection Methode verwendet, die in meinem [vorheriger Artikel](blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco) um die SMTP-Einstellungen zu erhalten.

```csharp
  var smtpSettings = services.ConfigurePOCO<SmtpSettings>(config.GetSection(SmtpSettings.Section));
```

Dies kommt von der Datei appsettings.json:

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

Hinweis: Für Google SMTP, wenn Sie MFA (die Sie **wirklich* wenn Sie müssen, um eine [App-Passwort für Ihr Konto](https://myaccount.google.com/apppasswords).

Für lokale dev können Sie dies Ihrer Secrets.json-Datei hinzufügen:

![secrets.png](secrets.png)

### Docker-Einrichtung

Für docker komponieren verwenden Sie normalerweise diese in einer.env-Datei:

```env
SMTPSETTINGS_USERNAME="scott.galloway@gmail.com"
SMTPSETTINGS_PASSWORD="<MFA PASSWORD>" -- this is the app password you created

```

Dann in der docker compound-Datei injizieren Sie diese als env-Variablen:

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

Nehmen Sie eine Notiz vom Abstand, da dies kann wirklich verwirren Sie mit docker komponieren. Um zu überprüfen, was injiziert wird, können Sie

```bash
docker compose config
```

Um Ihnen zu zeigen, wie die Datei mit diesen injiziert aussieht.

## FluentEmail-Anfechtungen

Ein Problem mit Fluent Email ist, dass Sie dies Ihrem csproj hinzufügen müssen

```xml
  <PropertyGroup>
    <PreserveCompilationContext>true</PreserveCompilationContext>
  </PropertyGroup>
```

Dies liegt daran, dass FluentEmail RazorLight verwendet, die dies benötigt, um zu funktionieren.

Für die Template-Dateien können Sie sie entweder als Content-Dateien in Ihr Projekt aufnehmen oder wie ich es im Docker-Container tue, die Dateien in das endgültige Bild kopieren

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

## E-Mail-Dienst

Okay, zurück zum Code!

Jetzt haben wir alles vorbereitet können wir den E-Mail-Service hinzufügen. Dies ist ein einfacher Dienst, der eine Vorlage nimmt und eine E-Mail sendet:

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

Wie Sie hier sehen können, haben wir zwei Methoden, eine für Kommentare und eine für das Kontaktformular ([Schicken Sie mir eine Post!](/contact) )== Einzelnachweise == In dieser App melde ich mich an, damit ich die Mail bekommen kann, von der es ist (und Spam zu vermeiden).

Die meiste Arbeit wird hier geleistet:

```csharp
 var template = await File.ReadAllTextAsync(templatePath);
        // Use FluentEmail to send the email
        var email = fluentEmail.UsingTemplate(template, model);
        await email.To(smtpSettings.ToMail)
            .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .Subject("New Comment")
            .SendAsync();
```

Hier öffnen wir eine Template-Datei, fügen das Modell mit dem Inhalt für die E-Mail hinzu, laden es in FluentEmail und senden es dann. Die Vorlage ist eine einfache Razor-Datei:

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

Diese werden als.template-Dateien im Ordner E-Mail/Templates gespeichert. Sie können.cshtml-Dateien verwenden, aber es verursacht ein Problem mit dem @Raw-Tag in der Vorlage (es ist eine Rasiererlicht Sache).

## Der Controller

Endlich kommen wir zum Controller; es ist wirklich ziemlich einfach

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

Hier erhalten wir die Benutzerinformationen, verarbeiten den Kommentar (ich benutze einen einfachen Markdown-Prozessor mit Markdig, um Markdown in HTML zu konvertieren) und senden dann die E-Mail.