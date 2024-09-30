using Mostlylucid.Shared.Models.Email;
using Mostlylucid.Shared.Models.EmailSubscription;

namespace Mostlylucid.Services.Email;

public interface IEmailService
{
    Task<bool> SendCommentEmail(CommentEmailModel commentModel);
    Task<bool> SendContactEmail(ContactEmailModel contactModel);
    Task<bool> SendConfirmationEmail(ConfirmEmailModel confirmEmailModel);
    Task<bool> SendNewsletterEmail(EmailTemplateModel newsletterEmailModel);
}