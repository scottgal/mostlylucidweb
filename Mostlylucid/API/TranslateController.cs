using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Helpers;
using Mostlylucid.MarkdownTranslator;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.API;

[ApiController]
[Route("api/[controller]")]
public class TranslateController(
    BackgroundTranslateService backgroundTranslateService,
    TranslateCacheService translateCacheService) : ControllerBase
{
    [HttpPost("start-translation")]
    public async Task<IActionResult> StartTranslation([FromBody] MarkdownTranslationModel model)
    {
        // Create a unique identifier for this translation task
        var taskId = Guid.NewGuid().ToString("N");
        var userId = Request.GetUserId(Response);
        // Trigger translation and store the associated task
        var translationTask = await backgroundTranslateService.Translate(model);
    
        var translateTask = new TranslateTask(taskId, model.Language, translationTask);
        translateCacheService.AddTask(userId, translateTask);

        // Return the task ID to the client
        return Ok(new { TaskId = taskId });
    }

    [HttpGet("ping")]
    public async Task<Results<Ok<string>, BadRequest<string>>> Ping(CancellationToken cancellationToken)
    {
        if (await backgroundTranslateService.Ping(cancellationToken))
            return TypedResults.Ok<string>("Good");
        return TypedResults.BadRequest("bad");
    }

    [HttpGet("check-status/{taskId}")]
    public Results<Ok<TaskStatus>, NotFound<string>> CheckStatus(string taskId)
    {
        var userId = Request.GetUserId(Response);
        var tasks = translateCacheService.GetTasks(userId);
        var translationTask = tasks.FirstOrDefault(t => t.TaskId == taskId)?.Task;
        // Check if the task exists
        if (translationTask == null) return TypedResults.NotFound("Task not found");
        switch (translationTask.IsCompletedSuccessfully)
        {
            // Check the status of the task
            case true:
                return TypedResults.Ok(new TaskStatus("Completed"));
            case false when translationTask.IsFaulted:
                return TypedResults.Ok(new TaskStatus("Failed", translationTask.Exception?.Message));
            case false when translationTask.IsCanceled:
                return TypedResults.Ok(new TaskStatus("Canceled"));
            case false when !translationTask.IsCompleted:
                return TypedResults.Ok(new TaskStatus("In progress"));
            default: return TypedResults.NotFound("Task not found");
        }
    }
    
    [HttpGet]
    [Route("get-translation/{taskId}")]
    public Results<JsonHttpResult<TaskCompletion>, BadRequest<string>> GetTranslation(string taskId)
    {
        var userId = Request.GetUserId(Response);
        var tasks = translateCacheService.GetTasks(userId);
        var translationTask = tasks.FirstOrDefault(t => t.TaskId == taskId);
        if(translationTask?.Task?.Status != System.Threading.Tasks.TaskStatus.RanToCompletion)
        {
            return TypedResults.BadRequest<string>("Task not completed");
        }
        return TypedResults.Json(translationTask.Task.Result);
    }

}

public record TaskStatus(string Status, string? Error = "");