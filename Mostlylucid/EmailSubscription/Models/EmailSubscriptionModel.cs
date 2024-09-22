using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared;
using Mostlylucid.Shared.Entities;

namespace Mostlylucid.EmailSubscription.Models;

public class EmailSubscriptionModel
{
    public static EmailSubscriptionModel FromEntity(EmailSubscriptionEntity entity)
    {
        return new EmailSubscriptionModel
        {
            Id = entity.Id,
            Token = entity.Token,
            SubscriptionType = entity.SubscriptionType,
            Language = entity.Language,
            Email = entity.Email,
            CreatedDate = entity.CreatedDate,
            Day = entity.Day,
            DayOfMonth = entity.DayOfMonth,
            LastSent = entity.LastSent,
            Categories = entity.Categories?.Select(c => c.Name).ToList(),
            EmailConfirmed = entity.EmailConfirmed
        };
    }
    
    public static EmailSubscriptionEntity ToEntity(EmailSubscriptionModel model)
    {
        return new EmailSubscriptionEntity
        {
            Id = model.Id,
            Token = model.Token,
            SubscriptionType = model.SubscriptionType,
            Language = model.Language,
            Email = model.Email,
            CreatedDate = model.CreatedDate,
            LastSent = model.LastSent,
            Categories = model.Categories?.Select(c => new CategoryEntity { Name = c }).ToList(),
            EmailConfirmed = model.EmailConfirmed,
            Day = model.Day
        };
    }
    
    public int Id { get; set; }
    
    public required string Token { get; set; }
    
    public SubscriptionType SubscriptionType { get; set; }

    public string Language { get; set; } = MarkdownBaseService.EnglishLanguage;
    
    public required string Email { get; set; } 
    
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.Now;
    
    public DateTimeOffset? LastSent { get; set; }
    
    public List<string>? Categories { get; set; }
    
    public int? DayOfMonth { get; set; }
    
    public string?  Day { get; set; } 
    
    public bool EmailConfirmed { get; set; } = false;
}