# Envío de correos HTML desde el núcleo de ASP.NET con FluentEmail

<datetime class="hidden">2024-08-07T00:30</datetime>

<!--category-- ASP.NET, FluentEmail -->
Este es un artículo bastante simple, pero cubrirá algo de la odness de usar[FluentEmail](https://github.com/lukencode/FluentEmail)en ASP.NET Core para enviar correos HTML que no he visto en otra parte.

## El problema

Enviar correos HTML es en sí mismo un poco simple con SmtpClient, pero no es muy flexible y no soporta cosas como plantillas o adjuntos. FluentEmail es una gran biblioteca para esto, pero no siempre está claro cómo usarlo en ASP.NET Core.

FluentEmail con Razorlight (está integrado) te permite plantillar tus emails usando la sintaxis de Razor. Esto es genial ya que te permite usar todo el poder de Razor para crear tus emails.

## La solución

En primer lugar, es necesario instalar las bibliotecas FluentEmail.Core, FluentEmail.Smtp & FluentEmail.Razor:

```bash
dotnet add package FluentEmail.Core
dotnet add package FluentEmail.Smtp
dotnet add package FluentEmail.Razor
```

## Configuración de FluentEmail

Para mantener las cosas separadas, creé una extensión de IServiceCollection que establece los servicios de FluentEmail:

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

Configuración ##SMTP

Como verás, también utilicé el método IConfigSection mencionado en mi[Artículo anterior](blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)para obtener la configuración SMTP.

```csharp
  var smtpSettings = services.ConfigurePOCO<SmtpSettings>(config.GetSection(SmtpSettings.Section));
```

Esto viene del archivo appsettings.json:

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

Nota: Para Google SMTP si utiliza MFA (que usted**¿En serio?*Si necesitas hacer un[contraseña de aplicación para tu cuenta](https://myaccount.google.com/apppasswords).

Para dev local, puede añadir esto a su archivo secrets.json:

![secrets.png](secrets.png)

### Configuración de Docker

Para el uso del docker composite normalmente lo incluirías en un archivo.env:

```env
SMTPSETTINGS_USERNAME="scott.galloway@gmail.com"
SMTPSETTINGS_PASSWORD="<MFA PASSWORD>" -- this is the app password you created

```

Luego en el archivo de composición docker se inyectan estas como variables env:

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

Tome una nota del espaciado, ya que esto puede realmente lío con docker componer. Para comprobar lo que se inyecta se puede utilizar

```bash
docker compose config
```

Para mostrarte cómo se ve el archivo con estos inyectados.

## Servicio de correo electrónico

¡De acuerdo, de vuelta al código!

Ahora lo tenemos todo configurado podemos agregar el Servicio de Email. Este es un servicio simple que toma una plantilla y envía un correo electrónico:

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

Como se puede ver aquí tenemos dos métodos, uno para los comentarios y otro para el formulario de contacto ([¡Envíame un correo!](/contact)En esta aplicación te hago iniciar sesión para que pueda obtener el correo del que es (y para evitar el spam).

Realmente la mayor parte del trabajo se hace aquí:

```csharp
 var template = await File.ReadAllTextAsync(templatePath);
        // Use FluentEmail to send the email
        var email = fluentEmail.UsingTemplate(template, model);
        await email.To(smtpSettings.ToMail)
            .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .Subject("New Comment")
            .SendAsync();
```

Aquí abrimos un archivo de plantilla, añadimos el modelo que contiene el contenido del correo electrónico, lo cargamos en FluentEmail y luego lo enviamos. La plantilla es un archivo Razor simple:

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

Estos se almacenan como archivos.template en la carpeta Email/Templates. Puede utilizar archivos.cshtml pero causa un problema con la etiqueta @Raw en la plantilla (es una cosa de luz de afeitar).

## El Contralor

Finalmente llegamos al controlador; es realmente bastante sencillo

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

Aquí obtenemos la información del usuario, procesar el comentario (Utilizo un simple procesador Markdown con Markdig para convertir Markdown a HTML) y luego enviar el correo electrónico.