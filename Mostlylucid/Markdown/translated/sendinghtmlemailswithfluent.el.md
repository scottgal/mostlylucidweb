# Αποστολή email HTML από το ASP.NET Core με FluentEmail

<datetime class="hidden">2024-08-07T00:30</datetime>

<!--category-- ASP.NET, FluentEmail -->
Αυτό είναι ένα αρκετά απλό άρθρο, αλλά θα καλύψει μερικές από τις οσμές της χρήσης [FluentEmail](https://github.com/lukencode/FluentEmail) στο ASP.NET Core για να στείλετε μηνύματα ηλεκτρονικού ταχυδρομείου HTML που δεν έχω δει αλλού.

## Το Πρόβλημα

Αποστολή μηνυμάτων HTML είναι η ίδια κάπως απλή με SmtpClient, αλλά δεν είναι πολύ ευέλικτη και δεν υποστηρίζει πράγματα όπως πρότυπα ή εξαρτήματα. FluentEmail είναι μια μεγάλη βιβλιοθήκη για αυτό, αλλά δεν είναι πάντα σαφές πώς να το χρησιμοποιήσετε στο ASP.NET Core.

FluentEmail με Razorlight (είναι ενσωματωμένο) σας επιτρέπει να τυπώσετε τα email σας χρησιμοποιώντας τη σύνταξη Razor. Αυτό είναι μεγάλο καθώς σας επιτρέπει να χρησιμοποιήσετε την πλήρη δύναμη του Razor για να δημιουργήσετε τα email σας.

## Η Λύση

Πρώτον, θα πρέπει να εγκαταστήσετε τις βιβλιοθήκες FluentEmail.Core, FluentEmail.Smtp & FluentEmail.Razor:

```bash
dotnet add package FluentEmail.Core
dotnet add package FluentEmail.Smtp
dotnet add package FluentEmail.Razor
```

## Ρύθμιση FluentEmail

Για να κρατήσω τα πράγματα ξεχωριστά Στη συνέχεια, δημιούργησα μια επέκταση IServiceCollection που δημιουργεί τις υπηρεσίες FluentEmail:

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

Ρυθμίσεις SMTP

Όπως θα δείτε, χρησιμοποίησα επίσης τη μέθοδο IConfigSection που αναφέρεται στο [προηγούμενο άρθρο](blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco) για να πάρει τις ρυθμίσεις SMTP.

```csharp
  var smtpSettings = services.ConfigurePOCO<SmtpSettings>(config.GetSection(SmtpSettings.Section));
```

Αυτό προέρχεται από το αρχείο appsettings.json:

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

Σημείωση: Για το Google SMTP εάν χρησιμοποιείτε MAX (την οποία χρησιμοποιείτε **Αλήθεια.* θα πρέπει να κάνετε μια [app κωδικός πρόσβασης για το λογαριασμό σας](https://myaccount.google.com/apppasswords).

Για την τοπική dev, μπορείτε να προσθέσετε αυτό στα μυστικά σας.json αρχείο:

![secrets.png](secrets.png)

### Ρύθμιση Docker

Για docker συνθέτουν τη χρήση κανονικά θα συμπεριλάβετε αυτό σε ένα αρχείο.env:

```env
SMTPSETTINGS_USERNAME="scott.galloway@gmail.com"
SMTPSETTINGS_PASSWORD="<MFA PASSWORD>" -- this is the app password you created

```

Στη συνέχεια, στο Docker συνθέτουν το αρχείο που εγχέετε αυτά ως μεταβλητές env:

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

Σημειώστε το διάστημα καθώς αυτό μπορεί πραγματικά να σας μπερδέψει με Docker συνθέτουν. Για να ελέγξετε τι ενίεται μπορείτε να χρησιμοποιήσετε

```bash
docker compose config
```

Για να σας δείξω πώς είναι το αρχείο με αυτά τα ενέσιμα.

## FluentEmail Ενοχλήσεις

Ένα θέμα με το Fluent Email είναι ότι θα πρέπει να προσθέσετε αυτό στο csproj σας

```xml
  <PropertyGroup>
    <PreserveCompilationContext>true</PreserveCompilationContext>
  </PropertyGroup>
```

Αυτό συμβαίνει επειδή το FluentEmail χρησιμοποιεί το RazorLight το οποίο χρειάζεται αυτό για να λειτουργήσει.

Για τα αρχεία πρότυπο, μπορείτε είτε να τα συμπεριλάβετε στο έργο σας ως αρχεία περιεχομένου ή όπως κάνω στο δοχείο docker, αντιγράψτε τα αρχεία στην τελική εικόνα

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

## Υπηρεσία ηλεκτρονικού ταχυδρομείου

Εντάξει πίσω στον κώδικα!

Τώρα τα έχουμε όλα έτοιμα μπορούμε να προσθέσουμε την υπηρεσία ηλεκτρονικού ταχυδρομείου. Αυτή είναι μια απλή υπηρεσία που παίρνει ένα πρότυπο και στέλνει ένα email:

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

Όπως μπορείτε να δείτε εδώ έχουμε δύο μεθόδους, μία για Σχόλια και μία για το έντυπο Επικοινωνίας ([Στείλε μου ένα γράμμα!](/contact) ). Σε αυτή την εφαρμογή σας κάνω να συνδεθείτε ώστε να μπορώ να πάρω την αλληλογραφία είναι από (και να αποφύγετε spam).

Πραγματικά το μεγαλύτερο μέρος της δουλειάς γίνεται εδώ:

```csharp
 var template = await File.ReadAllTextAsync(templatePath);
        // Use FluentEmail to send the email
        var email = fluentEmail.UsingTemplate(template, model);
        await email.To(smtpSettings.ToMail)
            .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .Subject("New Comment")
            .SendAsync();
```

Εδώ ανοίγουμε ένα αρχείο προτύπου, προσθέτουμε το μοντέλο που περιέχει το περιεχόμενο για το email, το φορτώνουμε στο FluentEmail και στη συνέχεια το στέλνουμε. Το πρότυπο είναι ένα απλό αρχείο Razor:

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

Αυτά αποθηκεύονται ως.template αρχεία στο φάκελο Email/Templates. Μπορείτε να χρησιμοποιήσετε.cshtml αρχεία, αλλά προκαλεί ένα πρόβλημα με την ετικέτα @Raw στο πρότυπο (είναι ένα ξυράφι πράγμα).

## Ο Ελεγκτής

Επιτέλους φτάσαμε στο χειριστήριο. Είναι πολύ απλό.

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

Εδώ παίρνουμε τις πληροφορίες χρήστη, επεξεργαζόμαστε το σχόλιο (Χρησιμοποιώ ένα απλό επεξεργαστή markdown με Markdig για να μετατρέψει το markdown σε HTML) και στη συνέχεια να στείλετε το email.