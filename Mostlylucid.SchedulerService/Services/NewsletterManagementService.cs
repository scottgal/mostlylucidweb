using Microsoft.EntityFrameworkCore;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Shared;
using Mostlylucid.Shared.Entities;
using Mostlylucid.Shared.Mapper;
using Mostlylucid.Shared.Models;
using Mostlylucid.Services.Blog;

namespace Mostlylucid.SchedulerService.Services;

public class NewsletterManagementService(MostlylucidDbContext dbContext, IBlogService blogService)
{
    public IQueryable<EmailSubscriptionEntity> Query(SubscriptionType subscriptionType)
    {
        return dbContext.EmailSubscriptions.Where(x=>x.SubscriptionType==subscriptionType);
    }
    
    public async Task<List<EmailSubscriptionEntity>> GetSubscriptions( SubscriptionType subscriptionType)
    {
        var date = DateTime.Now;
        var subscriptions = Query(subscriptionType);
        switch (subscriptionType)
        {
            case SubscriptionType.Daily:
                return await subscriptions.ToListAsync();
            case SubscriptionType.Weekly:
                return await subscriptions.Where(x => x.Day == date.DayOfWeek.ToString()).ToListAsync();
            case SubscriptionType.Monthly:
                return await subscriptions.Where(x => x.Day == date.Day.ToString()).ToListAsync();
            case SubscriptionType.EveryPost:
                return await subscriptions.ToListAsync();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public async Task<List<BlogPostDto>> GetPostsToSend(SubscriptionType subscriptionType)
    {
        PostListQueryModel? queryModel=null;
      if(subscriptionType==SubscriptionType.EveryPost)
      {
          var lastPost = await dbContext.BlogPosts.OrderByDescending(x => x.PublishedDate).FirstOrDefaultAsync();
          if(lastPost==null)
          {
              return new List<BlogPostDto>();
          }
          return new List<BlogPostDto>{lastPost.ToDto()};
      }
    
      switch (subscriptionType)
      {
          case SubscriptionType.Daily:
              queryModel= new PostListQueryModel(StartDate: DateTime.Now.AddDays(-1), EndDate: DateTime.Now);
                break;
            case SubscriptionType.Weekly:
                queryModel= new PostListQueryModel(StartDate: DateTime.Now.AddDays(-7), EndDate: DateTime.Now);
                break;
            case SubscriptionType.Monthly:
                queryModel= new PostListQueryModel(StartDate: DateTime.Now.AddMonths(-1), EndDate: DateTime.Now);
                break;
      }
      if(queryModel == null)
      {
          return new List<BlogPostDto>();
      }
      var posts= await blogService.Get(queryModel);
      if(posts==null)
      {
          return new List<BlogPostDto>();
      }
      return posts.Data.ToList();
    }


    public async Task UpdateLastSend(SubscriptionType subscriptionType, DateTime date)
    {
        var subscriptions = dbContext.EmailSubscriptionSendLogs.Where(x => x.SubscriptionType == subscriptionType);
      await subscriptions.ExecuteUpdateAsync(x=>x.SetProperty(y=>y.LastSent, date));
    }
    
}