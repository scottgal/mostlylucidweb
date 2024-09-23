using Mostlylucid.Shared.Models.Email;

namespace Mostlylucid.Shared.Models.EmailSubscription;


public class EmailTemplateModel : BaseEmailModel
{
    public string ToEmail { get; set; } = string.Empty;
}