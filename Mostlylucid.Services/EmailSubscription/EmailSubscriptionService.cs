using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Shared.Models.EmailSubscription;

namespace Mostlylucid.Services.EmailSubscription;

public class EmailSubscriptionService(MostlylucidDbContext context, ILogger<EmailSubscriptionService> logger)
{
    
    public string GetToken()
    {
        return Guid.NewGuid().ToString("N");
    }
    public async Task<bool> Create(EmailSubscriptionModel model)
    {
        var entity = EmailSubscriptionModel.ToEntity(model);
        if (model.Categories?.Any() == true)
        {
            var categories = await context.Categories.Where(c => model.Categories.Contains(c.Name)).ToListAsync();
            entity.Categories = categories;
        }

        await context.EmailSubscriptions.AddAsync(entity);
        await context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> UpdateEmailConfirmed(string token)
    {
       var update=await context.EmailSubscriptions.Where(x=>x.Token == token).ExecuteUpdateAsync(e => e.SetProperty(x => x.EmailConfirmed, true));
        return update > 0;
    }
    
    public async Task<EmailSubscriptionModel?> GetByEmail(string email)
    {
        var entity = await context.EmailSubscriptions
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.Email == email);
        return entity == null ? null : EmailSubscriptionModel.FromEntity(entity);
    }
    
    public async Task<EmailSubscriptionModel?> GetByToken(string token)
    {
        var entity = await context.EmailSubscriptions
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.Token == token);
        return entity == null ? null : EmailSubscriptionModel.FromEntity(entity);
    }
    
    public async Task<bool> Update(EmailSubscriptionModel model)
    {
        var entity = EmailSubscriptionModel.ToEntity(model);
        if (model.Categories?.Any() == true)
        {
        var categories = await context.Categories.Where(c => model.Categories.Contains(c.Name)).ToListAsync();
        entity.Categories = categories;
        }
        context.EmailSubscriptions.Update(entity);
        await context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> Delete(string token)
    {
        var entity = await context.EmailSubscriptions.FirstOrDefaultAsync(e => e.Token == token);
        if (entity == null)
        {
            return false;
        }
        context.EmailSubscriptions.Remove(entity);
        await context.SaveChangesAsync();
        return true;
    }
}