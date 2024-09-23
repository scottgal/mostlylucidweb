using Mostlylucid.Shared;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.EmailSubscription.Models;

public class EmailRenderingModel
{
    public List<EmailPostModel> Posts { get; set; } = new List<EmailPostModel>();
    
    
    public SubscriptionType SubscriptionType { get; set; }
    public string UnsubscribeUrl { get; set; } = string.Empty;
    
    public string ManageSubscriptionUrl { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string Language { get; set; } = string.Empty;
    
}

public class EmailPostModel : BlogPostDto
{
public string Url { get; set; } = string.Empty;
}