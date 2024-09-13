using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Mostlylucid.API;

[Route("api/blog")]
[ApiController]
public class BlogUpdaterAPI(EFBlogUpdater efBlogEfBlogUpdater, ILogger<BlogUpdaterAPI> logger) : ControllerBase
{
    
    [HttpGet]
    [Route("update")]
    public async Task<Results<Ok,StatusCodeHttpResult >> Update(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Triggering EF Blog Updater");
            await efBlogEfBlogUpdater.TriggerUpdate(cancellationToken);
            return TypedResults.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error triggering EF Blog Updater");
            return TypedResults.StatusCode(500);
        }
     
    }
}