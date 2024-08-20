using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.MarkdownTranslator;

public class TranslateCacheService(IMemoryCache memoryCache)
{
    public async Task<List<TranslateTask>> GetTask(Guid userId)
    {
        if (memoryCache.TryGetValue(userId, out List<TranslateTask>? task))
        {
            return task;
        }

        return new();
    }
    
    public async Task AddTask(Guid userId, TranslateTask task)
    {
        if (memoryCache.TryGetValue(userId, out List<TranslateTask>? tasks))
        {
            tasks.Add(task);
            memoryCache.Set(userId, tasks, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
        }
        else
        {
            memoryCache.Set(userId, new List<TranslateTask> { task }, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
        }
    }
}

public record TranslateTask(Guid TaskId, Task<(BlogPostViewModel? model, bool complete)> Task);