# Envoi de courriels HTML depuis ASP.NET Core avec FluentEmail

<datetime class="hidden">2024-08-07T00:30</datetime>

<!--category-- ASP.NET, FluentEmail -->
Il s'agit d'un article assez simple, mais qui couvrira une partie de l'utilisation[Courriel fluide](https://github.com/lukencode/FluentEmail)dans ASP.NET Core pour envoyer des courriels HTML Je n'ai pas vu ailleurs.

## Le problème

Envoyer des mails HTML est lui-même assez simple avec SmtpClient, mais il n'est pas très flexible et ne supporte pas des choses comme des modèles ou des pièces jointes. FluentEmail est une excellente bibliothèque pour cela, mais il n'est pas toujours clair comment l'utiliser dans ASP.NET Core.

FluentEmail avec Razorlight (il est intégré) vous permet de modéliser vos e-mails en utilisant la syntaxe Razor. C'est génial car il vous permet d'utiliser toute la puissance de Razor pour créer vos e-mails.

## La solution

Tout d'abord, vous devez installer le FluentEmail.Core, FluentEmail.Smtp & FluentEmail.

```bash
dotnet add package FluentEmail.Core
dotnet add package FluentEmail.Smtp
dotnet add package FluentEmail.Razor
```

## Configuration FluentEmail

Pour garder les choses séparées, j'ai ensuite créé une extension IServiceCollection qui met en place les services FluentEmail :

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

Paramètres ##SMTP

Comme vous le verrez, j'ai également utilisé la méthode IConfigSection mentionnée dans mon[article précédent](blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)pour obtenir les paramètres SMTP.

```csharp
  var smtpSettings = services.ConfigurePOCO<SmtpSettings>(config.GetSection(SmtpSettings.Section));
```

Cela vient du fichier appsettings.json:

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

Note: Pour Google SMTP si vous utilisez MFA (que vous**Vraiment*au cas où vous auriez besoin de faire un[mot de passe de l'application pour votre compte](https://myaccount.google.com/apppasswords).

Pour dev local, vous pouvez ajouter ceci à votre fichier secret.json :

![secrets.png](secrets.png)

### Configuration de Docker

Pour l'utilisation de la composition de docker, vous l'incluez normalement dans un fichier.env :

```env
SMTPSETTINGS_USERNAME="scott.galloway@gmail.com"
SMTPSETTINGS_PASSWORD="<MFA PASSWORD>" -- this is the app password you created

```

Ensuite, dans le fichier de composition de docker, vous injectez ces variables sous forme de variables env :

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

Prenez note de l'espacement car cela peut vraiment vous gâcher avec Docker composer. Pour vérifier ce qui est injecté vous pouvez utiliser

```bash
docker compose config
```

Pour vous montrer à quoi ressemble le fichier avec ces injectés.

## Service de courrier électronique

Retournez au code!

Maintenant nous avons tout configuré nous pouvons ajouter le service d'email. Il s'agit d'un service simple qui prend un modèle et envoie un e-mail:

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

Comme vous pouvez le voir ici, nous avons deux méthodes, l'une pour les commentaires et l'autre pour le formulaire de contact ([Envoyez-moi un courrier!](/contact)Dans cette application, je vous fais vous connecter afin que je puisse obtenir le courrier de (et pour éviter le spam).

La plupart du travail est fait ici :

```csharp
 var template = await File.ReadAllTextAsync(templatePath);
        // Use FluentEmail to send the email
        var email = fluentEmail.UsingTemplate(template, model);
        await email.To(smtpSettings.ToMail)
            .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .Subject("New Comment")
            .SendAsync();
```

Ici, nous ouvrons un fichier modèle, ajoutons le modèle contenant le contenu de l'email, chargeons-le dans FluentEmail puis envoyons-le. Le modèle est un simple fichier Razor:

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

Ceux-ci sont stockés sous forme de fichiers.template dans le dossier Email/Templates. Vous pouvez utiliser des fichiers.cshtml mais cela cause un problème avec la balise @Raw dans le modèle (c'est une chose de rasoir).

## Le Contrôleur

Enfin nous arrivons au contrôleur; c'est vraiment assez simple

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

Ici, nous obtenons l'information utilisateur, traitons le commentaire (j'utilise un simple processeur de balisage avec Markdig pour convertir balisage en HTML) puis envoyons l'e-mail.