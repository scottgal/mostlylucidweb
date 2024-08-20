using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.MarkdownTranslator;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.API;

[ApiController]
[Route("api/[controller]")]
public class TranslateController(BackgroundTranslateService backgroundTranslateService,IMemoryCache memoryCache) : ControllerBase
{

    
    // Dictionary to hold tasks that are triggered
    private static readonly Dictionary<Guid, Task<(BlogPostViewModel? model, bool complete)>> _translationTasks = new();



    [HttpPost("start-translation")]
    public async Task<IActionResult> StartTranslation([FromBody] PageTranslationModel model)
    {
        // Create a unique identifier for this translation task
        var taskId = Guid.NewGuid();

        // Trigger translation and store the associated task
        var translationTask = await backgroundTranslateService.Translate(model);
        _translationTasks[taskId] = translationTask;

        // Return the task ID to the client
        return Ok(new { TaskId = taskId });
    }

    [HttpGet("check-status/{taskId}")]
    public async Task<IActionResult> CheckStatus(Guid taskId)
    {
        // Check if the task exists
        if (_translationTasks.TryGetValue(taskId, out var translationTask))
        {
            // Check the status of the task
            if (translationTask.IsCompletedSuccessfully)
            {
                return Ok(new { Status = "Completed" });
            }
            else if (translationTask.IsFaulted)
            {
                return Ok(new { Status = "Failed", Error = translationTask.Exception?.Message });
            }
            else
            {
                return Ok(new { Status = "In Progress" });
            }
        }

        return NotFound("Task not found");
    }
}