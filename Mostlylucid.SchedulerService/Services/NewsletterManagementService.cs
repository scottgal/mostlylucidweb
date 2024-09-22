using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Shared;
using Mostlylucid.Shared.Entities;

namespace Mostlylucid.SchedulerService.Services;

public class NewsletterManagementService(MostlylucidDbContext dbContext)
{
    public IQueryable<EmailSubscriptionEntity> GetSubscriptions(SubscriptionType subscriptionType)
    {
        return dbContext.EmailSubscriptions.Where(x=>x.SubscriptionType==subscriptionType);
    }
    
}