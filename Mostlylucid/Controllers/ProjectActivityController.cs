using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.Services;

namespace Mostlylucid.Controllers;

public class ProjectActivityController(GitHubActivityService activity) : Controller
{
    [HttpGet("projects/activity")]
    [OutputCache(NoStore = true)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        PartialView("_RepositoryActivity", await activity.GetAsync(cancellationToken));
}
