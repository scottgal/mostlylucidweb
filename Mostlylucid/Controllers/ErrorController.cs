using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Services;

namespace Mostlylucid.Controllers;

public class ErrorController(BaseControllerService baseControllerService, ILogger<ErrorController> logger) : BaseController(baseControllerService, logger)
{
    [Route("/error/{statusCode}")]
    [HttpGet]
    public IActionResult HandleError(int statusCode)
    {
        // Retrieve the original request information
        var statusCodeReExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

        if (statusCodeReExecuteFeature != null)
        {
            // Access the original path and query string that caused the error
            var originalPath = statusCodeReExecuteFeature.OriginalPath;
            var originalQueryString = statusCodeReExecuteFeature.OriginalQueryString;

            // Optionally log the original URL or pass it to the view
            ViewData["OriginalUrl"] = $"{originalPath}{originalQueryString}";
            ViewData["StatusCode"] = statusCode;
        }

        // Handle specific status codes and return corresponding views
        switch (statusCode)
        {
            case 404:
                return View("NotFound");
            case 500:
                return View("ServerError");
            default:
                return View("Error");
        }
    }
}