using Mostlylucid.Shared.Models.Email;
using Mostlylucid.Shared.Models.EmailSubscription;

namespace Mostlylucid.Services.Email;

public interface IEmailService
{
    Task SendCommentEmail(CommentEmailModel commentModel);
    Task SendContactEmail(ContactEmailModel contactModel);
    Task SendConfirmationEmail(ConfirmEmailModel confirmEmailModel);
    Task SendNewsletterEmail(EmailTemplateModel newsletterEmailModel);
}