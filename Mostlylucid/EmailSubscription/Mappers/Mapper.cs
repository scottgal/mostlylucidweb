using Mostlylucid.EmailSubscription.Models;
using Mostlylucid.Shared.Entities;

namespace Mostlylucid.EmailSubscription.Mappers;

public static class Mapper
{
    
    public static EmailSubscribeViewModel MapToEmailSubscribeViewModel(this EmailSubscriptionModel model)
    {
        return new EmailSubscribeViewModel
        {
            Id = model.Id,
            Email = model.Email,
            Language = model.Language,
            Token = model.Token,
            SubscriptionType = model.SubscriptionType,
            Day = model.Day,
            DayOfMonth = model.DayOfMonth,
            EmailConfirmed = model.EmailConfirmed,
            SelectedCategories = model.Categories ?? new List<string>(),
        };
    }
}