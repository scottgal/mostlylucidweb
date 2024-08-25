using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.MarkdownTranslator.Models;

namespace Mostlylucid.MarkdownTranslator;

public class TranslateCacheService(IMemoryCache memoryCache)
{
    public List<TranslateTask> GetTasks(string userId)
    {
        if (memoryCache.TryGetValue(userId, out CachedTasks? tasks))
            return tasks?.Tasks ?? new List<TranslateTask>();
        return new List<TranslateTask>();
        
    }

    public void AddTask(string userId, TranslateTask task)
    {
        CachedTasks CachedTasks() => new()
        {
            Tasks = new List<TranslateTask> { task },
            AbsoluteExpiration = DateTime.Now.AddHours(6)
        };
        
        if (memoryCache.TryGetValue(userId, out CachedTasks? tasks))
        {
          tasks ??= CachedTasks();

         var currentTasks = tasks.Tasks;
             
             currentTasks= currentTasks.OrderByDescending(x => x.StartTime).ToList();
          if (currentTasks.Count >= 5)
          {
              var lastTask = currentTasks.Last();
              currentTasks.Remove(lastTask);
          }

          currentTasks.Add(task);
          currentTasks= currentTasks.OrderByDescending(x => x.StartTime).ToList();
            tasks.Tasks = currentTasks;
            memoryCache.Set(userId, tasks, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = tasks.AbsoluteExpiration,
                SlidingExpiration = TimeSpan.FromHours(1)
            });
        }
        else
        {
            var absoluteExpiration = DateTime.Now.AddHours(6);
            var cachedTasks = CachedTasks();
            memoryCache.Set(userId, cachedTasks, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration,
                SlidingExpiration = TimeSpan.FromHours(1)
            });
        }
    }
    private class CachedTasks
    {
        public List<TranslateTask> Tasks { get; set; } = new ();
        public DateTime AbsoluteExpiration { get; set; }
    }
}