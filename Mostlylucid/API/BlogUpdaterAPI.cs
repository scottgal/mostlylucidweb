using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Services.Blog;

namespace Mostlylucid.API;

[Route("api/blog")]
[ApiController]
public class BlogUpdaterAPI(BlogUpdater blogBlogUpdater, ILogger<BlogUpdaterAPI> logger) : ControllerBase
{
    
    [HttpGet]
    [Route("update")]
    public async Task<Results<Ok,StatusCodeHttpResult >> Update(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Triggering EF Blog Updater");
            await blogBlogUpdater.TriggerUpdate(cancellationToken);
            return TypedResults.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error triggering EF Blog Updater");
            return TypedResults.StatusCode(500);
        }
     
    }
}