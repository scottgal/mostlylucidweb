# Lähetetään HTML-viestejä ASP.NET Coresta FluentEmaililla

<datetime class="hidden">2024-08-07T00:30</datetime>

<!--category-- ASP.NET, FluentEmail -->
Tämä on melko yksinkertainen artikkeli, mutta se kattaa osan käytöstä. [FluentEmail](https://github.com/lukencode/FluentEmail) ASP.NET Coressa lähetetään HTML-viestejä, joita en ole nähnyt muualla.

## Ongelma

HTML-viestien lähettäminen on SmtpClientin kanssa yksinkertaista, mutta se ei ole kovin joustavaa eikä tue esimerkiksi malleja tai liitetiedostoja. FluentEmail on hyvä kirjasto tähän, mutta aina ei ole selvää, miten sitä käytetään ASP.NET Coressa.

FluentEmail Razorlight (se on sisäänrakennettu) mahdollistaa sähköpostien mallintamisen Razor syntaksin avulla. Tämä on hienoa, koska sen avulla voit käyttää Razorin täyttä voimaa sähköpostien luomiseen.

## Ratkaisu

Ensinnäkin sinun täytyy asentaa FluentEmail.Core, FluentEmail.Smtp & FluentEmail.Razor-kirjastot:

```bash
dotnet add package FluentEmail.Core
dotnet add package FluentEmail.Smtp
dotnet add package FluentEmail.Razor
```

## FluentEmailin käyttöönotto

Jotta asiat pysyisivät erillään, loin sitten IServiceCollection -laajennuksen, joka perustaa FluentEmail -palvelut:

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

SMTP-asetukset

Kuten näette, käytin myös IconfigSection -menetelmää, joka on mainittu [aiempi artikkeli](blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco) SMTP-asetusten saamiseksi.

```csharp
  var smtpSettings = services.ConfigurePOCO<SmtpSettings>(config.GetSection(SmtpSettings.Section));
```

Tämä tulee asetuksista.json-tiedostosta:

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

Huomautus: Google SMTP:lle, jos käytät MFA:ta (jota käytät **todella* Jos sinun täytyy tehdä [sovellussalasana tilillesi](https://myaccount.google.com/apppasswords).

Paikalliselle deville voit lisätä tämän salaisuuteesi.json-tiedosto:

![secrets.png](secrets.png)

### Dockerin asettelu

Docker compose -muodossa tämä normaalisti lisätään.env-tiedostoon:

```env
SMTPSETTINGS_USERNAME="scott.galloway@gmail.com"
SMTPSETTINGS_PASSWORD="<MFA PASSWORD>" -- this is the app password you created

```

Sitten pistät docker compose -tiedostossa nämä env-muuttujat:

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

Ota huomioon välit, sillä tämä voi todella sotkea dockerin sävellyksen. Tarkistaaksesi, mitä injisoit, voit käyttää

```bash
docker compose config
```

Näyttääksesi, miltä tiedosto näyttää näillä pistoksilla.

## FluentEmail Agoyances

Yksi ongelma Fluent Email on, että sinun täytyy lisätä tämä csproj

```xml
  <PropertyGroup>
    <PreserveCompilationContext>true</PreserveCompilationContext>
  </PropertyGroup>
```

Tämä johtuu siitä, että FluentEmail käyttää RazorLightia, joka tarvitsee tätä toimiakseen.

Mallitiedostoja varten voit joko liittää ne projektiisi Content-tiedostoina tai minä kopioida tiedostot lopulliseen kuvaan.

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

## Sähköpostipalvelu

Takaisin koodiin!

Nyt meillä on kaikki valmiina, voimme lisätä sähköpostipalvelun. Tämä on yksinkertainen palvelu, joka ottaa mallin ja lähettää sähköpostia:

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

Kuten näette, meillä on kaksi menetelmää, toinen kommenttien ja toinen yhteydenottolomakkeen ([Lähetä minulle postia!](/contact) ). Tässä sovelluksessa laitan sinut kirjautumaan sisään, jotta voin saada sähköpostin, joka on peräisin (ja välttää roskapostia).

Todella suurin osa työstä tehdään täällä:

```csharp
 var template = await File.ReadAllTextAsync(templatePath);
        // Use FluentEmail to send the email
        var email = fluentEmail.UsingTemplate(template, model);
        await email.To(smtpSettings.ToMail)
            .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .Subject("New Comment")
            .SendAsync();
```

Tässä avaamme mallitiedoston, lisäämme sähköpostin sisällön sisältävän mallin, lataamme sen FluentEmailiin ja lähetämme sen sitten. Malli on yksinkertainen Razor-tiedosto:

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

Nämä on tallennettu.template-tiedostoina sähköposti-/templates-kansioon. Voit käyttää.cshtml-tiedostoja, mutta se aiheuttaa ongelmia mallin @Raw-tunnuksen kanssa (se on partakoneenvalojuttu).

## Valvoja

Lopulta pääsemme ohjaimeen, se on aika suoraviivaista

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

Täältä saamme käyttäjätiedot, käsittelemme kommentin (käytän Markdigin kanssa yksinkertaista markown-prosessoria muuntaakseni markownin HTML:ksi) ja lähetämme sähköpostin.