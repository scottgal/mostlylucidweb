# 从 ASP.NET 核心发送 HTML 电子邮件, 带有流利电子邮件

<datetime class="hidden">2024-08-007:00:30</datetime>

<!--category-- ASP.NET, FluentEmail -->
这是一项相当简单的条款,但将涵盖使用中的某些现象。 [流流电子邮件](https://github.com/lukencode/FluentEmail) 在ASP.NET核心 发送 HTML 电子邮件,我还没有看到。

## 问题

发送 HTML 邮件本身在 SmtpClient 上相当简单, 但并不灵活, 不支持模板或附件等内容 。 流利电子邮件是一个很好的图书馆, 但它并不总是很清楚如何在 ASP. NET Core中使用它。

Razorlight (它所建) 的流利电子邮件允许您使用 Razor 语法输入邮件模板 。 这是伟大的,因为它允许你 充分利用Razor的力量 创建您的电子邮件。

## 解决方案

首先, 您需要安装流利电子邮件. core、 流利电子邮件. Smtp 和流利电子邮件. Razor 库 :

```bash
dotnet add package FluentEmail.Core
dotnet add package FluentEmail.Smtp
dotnet add package FluentEmail.Razor
```

## 设置流利电子邮件

建立流利电子邮件服务:

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

#### SMTP 设置

正如你将看到,我还使用 ICAFIG 控制方法,我提到, [前一条文前一条文](blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco) 以获取 SMTP 设置。

```csharp
  var smtpSettings = services.ConfigurePOCO<SmtpSettings>(config.GetSection(SmtpSettings.Section));
```

这是来自 appings.json 文件 :

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

## GMAAIL/谷歌 SMTP

注:对于 Google SMTP 来说,如果使用MFA(即您使用MFA) **真的* 如果你需要做一个 [您账户的密码](https://myaccount.google.com/apppasswords).

对于本地的 Dev, 您可以将此添加到您的机密文件 。 Json 文件 :

![secrets.png](secrets.png)

### doccc 设置

对于 docker 拼写使用, 您通常会在. env 文件中包含此内容 :

```env
SMTPSETTINGS_USERNAME="scott.galloway@gmail.com"
SMTPSETTINGS_PASSWORD="<MFA PASSWORD>" -- this is the app password you created

```

然后,在 docker 拼写文件中, 您输入这些变量作为 env 变量 :

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

注意一下间距,因为这会真的 弄乱你和Docker作曲。 检查你注射了什么可以用

```bash
docker compose config
```

让你看看这些注射的档案长什么样

## 流流电子邮件

流利电子邮件的一个问题是 您需要将此添加到您的 csproj 中

```xml
  <PropertyGroup>
    <PreserveCompilationContext>true</PreserveCompilationContext>
  </PropertyGroup>
```

这是因为流利电子邮件使用 RazorLight 来操作它。

对于模板文件,您可以将文件作为内容文件列入您的工程中,也可以如同我在 docker 容器中所做的那样,将文件复制到最终图像中

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

## 电子邮件服务

OK回到代码!

现在我们都安排好了 我们可以加上电子邮件服务了 这是一个简单的服务, 使用模板发送电子邮件 :

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

从这里可以看到,我们有两种方法,一种是“评论”方法,一种是“联系表”(Contact form)方法。[寄邮件给我!](/contact) ) ) 在此应用程序中, 我让你登录, 这样我就可以从( 并避免垃圾邮件) 获得邮件 。

大部分工作都是在这里完成的:

```csharp
 var template = await File.ReadAllTextAsync(templatePath);
        // Use FluentEmail to send the email
        var email = fluentEmail.UsingTemplate(template, model);
        await email.To(smtpSettings.ToMail)
            .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .Subject("New Comment")
            .SendAsync();
```

在此我们打开一个模板文件, 添加包含电子邮件内容的模型, 将其装入流利电子邮件, 然后发送 。 模板是一个简单的 Razor 文件 :

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

这些文件作为.template 文件存储在电子邮件/templates 文件夹中。 您可以使用.cshtml 文件, 但它会给模板中的 @Raw 标记造成问题( 这是剃须刀)。

## 主计长

我们终于找到控制器了,这非常直截了当

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

我们在这里获取用户信息, 处理注释( 我使用一个简单的标记分解处理器, 与 Markdig 将标记分解转换为 HTML), 然后发送电子邮件 。