using System.Net.Mail;
using System.Reflection;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging;
using Mostlylucid.Shared.Config;
using Mostlylucid.Shared.Models.Email;
using Mostlylucid.Shared.Models.EmailSubscription;
using Serilog;
using Serilog.Events;
using SerilogTracing;

namespace Mostlylucid.Services.Email;

public class EmailService(SmtpSettings smtpSettings, IFluentEmail fluentEmail, ILogger<EmailService> logger)
    : IEmailService
{
    private readonly string _nameSpace = typeof(EmailService).Namespace! + ".Templates.";

    public async Task<bool> SendCommentEmail(CommentEmailModel commentModel)
    {
        // Load the template
        var templatePath = _nameSpace + "CommentMailTemplate.cshtml";
      var response=  await SendMail(commentModel, templatePath);
        return response?.Successful ?? false;
    }

    public async Task<bool> SendContactEmail(ContactEmailModel contactModel)
    {
        var templatePath = _nameSpace + "ContactEmailModel.cshtml";

       var response = await SendMail(contactModel, templatePath);
        return response?.Successful ?? false;
    }

    public async Task<bool> SendConfirmationEmail(ConfirmEmailModel confirmEmailModel)
    {
        var templatePath = _nameSpace + "ConfirmationMailTemplate.cshtml";

        var response =await SendMail(confirmEmailModel, templatePath, confirmEmailModel.ToEmail);
        return response?.Successful ?? false;
    }

    public async Task<bool> SendNewsletterEmail(EmailTemplateModel newsletterEmailModel)
    {
        var templatePath = _nameSpace + "NewsletterTemplate.cshtml";

        var response = await SendMail(newsletterEmailModel, templatePath, newsletterEmailModel.ToEmail);
        return response?.Successful ?? false;
    }

    private async Task<SendResponse?> SendMail(BaseEmailModel model, string template, string? toEmail = null)
    {
        using var activity = Log.Logger.StartActivity("SendMail");
        try
        {
            activity.AddProperty("ToEmail", toEmail);
            activity.AddProperty("Template", template);
            activity.AddProperty("Model", model);
            var assembly = Assembly.GetAssembly(typeof(EmailService));

            // Use FluentEmail to send the email
            var email = fluentEmail.UsingTemplateFromEmbedded(template, model, assembly);
            var response = await email.To(toEmail ?? smtpSettings.ToMail)
                .SetFrom(smtpSettings.SenderEmail, smtpSettings.SenderName)
                .Subject("New Comment")
                .SendAsync();
            if (response.Successful)
            {
                logger.LogInformation("Email sent to {ToEmail}", toEmail);
            }
            else
            {
                activity.Complete(LogEventLevel.Error);
                logger.LogError("Email failed to send to {ToEmail}, {ErrorMessages}", toEmail, response.ErrorMessages);
            }

            return response;
        }
        catch (SmtpException se)
        {
            activity.Complete(LogEventLevel.Error, se);
            logger.LogError(se, "Error sending email");
            throw;
        }
        catch (Exception e)
        {
            activity.Complete(LogEventLevel.Error, e);
            logger.LogError(e, "Error sending email");
            return null;
        }
    }
}