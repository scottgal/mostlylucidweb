using System.Reflection;
using FluentEmail.Core;
using Mostlylucid.Shared.Config;
using Mostlylucid.Shared.Models.Email;
using Mostlylucid.Shared.Models.EmailSubscription;

namespace Mostlylucid.Services.Email;

public class EmailService(SmtpSettings smtpSettings, IFluentEmail fluentEmail)
{
    private readonly string NameSpace= typeof(EmailService).Namespace;
    public async Task SendCommentEmail(string commenterEmail, string commenterName, string comment)
    {
        var commentModel = new CommentEmailModel
        {
            SenderEmail = commenterEmail,
            SenderName = commenterName,
            Content = comment
        };
        await SendCommentEmail(commentModel);
    }

    public async Task SendCommentEmail(CommentEmailModel commentModel)
    {
        // Load the template
        var templatePath = NameSpace + ".Templates.CommentMailTemplate.cshtml";
        await SendMail(commentModel, templatePath);
    }

    public async Task SendContactEmail(ContactEmailModel contactModel)
    {
        var templatePath = NameSpace +".Templates.ContactEmailModel.cshtml";

        await SendMail(contactModel, templatePath);
    }

    public async Task SendConfirmationEmail(ConfirmEmailModel confirmEmailModel)
    {
        var templatePath = NameSpace +".Templates.ConfirmationMailTemplate.cshtml";

        await SendMail(confirmEmailModel, templatePath, confirmEmailModel.ToEmail);
    }
    
    public async Task SendNewsletterEmail(EmailTemplateModel newsletterEmailModel)
    {
        var templatePath = NameSpace +".Templates.NewsletterTemplate.cshtml";

        await SendMail(newsletterEmailModel, templatePath, newsletterEmailModel.ToEmail);
    }

    public async Task SendMail(BaseEmailModel model, string templatePath, string? toEmail = null)
    {
        var assembly = Assembly.GetAssembly(typeof(EmailService));
        var template = await File.ReadAllTextAsync(templatePath);
        // Use FluentEmail to send the email
        var email = fluentEmail.UsingTemplateFromEmbedded(template, model, assembly);
        await email.To(smtpSettings.ToMail)
            .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .Subject("New Comment")
            .SendAsync();
    }
}