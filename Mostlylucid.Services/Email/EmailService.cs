using System.Reflection;
using FluentEmail.Core;
using Mostlylucid.Shared.Config;
using Mostlylucid.Shared.Models.Email;
using Mostlylucid.Shared.Models.EmailSubscription;

namespace Mostlylucid.Services.Email;

public class EmailService(SmtpSettings smtpSettings, IFluentEmail fluentEmail)
{
    private readonly string _nameSpace= typeof(EmailService).Namespace! + ".Templates.";

    public async Task SendCommentEmail(CommentEmailModel commentModel)
    {
        // Load the template
        var templatePath = _nameSpace + "CommentMailTemplate.cshtml";
        await SendMail(commentModel, templatePath);
    }

    public async Task SendContactEmail(ContactEmailModel contactModel)
    {
        var templatePath = _nameSpace +"ContactEmailModel.cshtml";

        await SendMail(contactModel, templatePath);
    }

    public async Task SendConfirmationEmail(ConfirmEmailModel confirmEmailModel)
    {
        var templatePath = _nameSpace +"ConfirmationMailTemplate.cshtml";

        await SendMail(confirmEmailModel, templatePath, confirmEmailModel.ToEmail);
    }
    
    public async Task SendNewsletterEmail(EmailTemplateModel newsletterEmailModel)
    {
        var templatePath = _nameSpace +"NewsletterTemplate.cshtml";

        await SendMail(newsletterEmailModel, templatePath, newsletterEmailModel.ToEmail);
    }

    private async Task SendMail(BaseEmailModel model, string template, string? toEmail = null)
    {
        var assembly = Assembly.GetAssembly(typeof(EmailService));

        // Use FluentEmail to send the email
        var email = fluentEmail.UsingTemplateFromEmbedded(template, model, assembly);
        await email.To(toEmail?? smtpSettings.ToMail)
            .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
            .Subject("New Comment")
            .SendAsync();
    }
}