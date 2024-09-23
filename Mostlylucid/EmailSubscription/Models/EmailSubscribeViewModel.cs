using System.ComponentModel.DataAnnotations;
using Mostlylucid.Models;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared;

namespace Mostlylucid.EmailSubscription.Models;

public class EmailSubscribeViewModel : BaseViewModel
{
    public  EmailSubscriptionModel ToModel( string token)
    {
        var model= new EmailSubscriptionModel
        {
            Token = token,
            Email = Email,
            Categories = SelectedCategories,
            SubscriptionType = SubscriptionType,
            Language = Language,
            EmailConfirmed =EmailConfirmed,
            DayOfMonth = DayOfMonth,
            Day = Day
            
        };
        switch (SubscriptionType)
        {
            case SubscriptionType.Weekly:
                model.DayOfMonth = null;
                break;
            case SubscriptionType.Monthly:
               model.Day = null;
                break;
            case SubscriptionType.EveryPost:
                model.Day = null;
                model.DayOfMonth = null;
                break;
        }
        return model;
    }
    
    public int? Id { get; set; }
    public string? Token { get; set; }
    
    [Required(ErrorMessage = "Email is required", AllowEmptyStrings = false)]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [MaxLength(100, ErrorMessage = "Email is too long")]
    public new string Email { get; set; } = string.Empty;
    
    public bool AllCategories { get; set; } = true;
    public  List<string> Categories { get; set; } = new List<string>();
    
    public List<string> SelectedCategories { get; set; } = new List<string>();
    public SubscriptionType SubscriptionType { get; set; } = SubscriptionType.Weekly;
    
    public string Language { get; set; } = MarkdownBaseService.EnglishLanguage;
    
    public string? Day { get; set; } = "Monday";
    
    public int? DayOfMonth { get; set; } = 1;

    public bool EmailConfirmed { get; set; } = false;
    
    public List<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek>();
    
    public bool IsManageSubscription { get; set; } = false;
    
    public PageType PageType { get; set; } = PageType.Subscribe;
}

public enum PageType
{
    Subscribe,
    Manage
}