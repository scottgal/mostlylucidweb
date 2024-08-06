using FluentEmail.Core;
using Mostlylucid.Email.Models;

namespace Mostlylucid.Email;

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