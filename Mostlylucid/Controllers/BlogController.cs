using Microsoft.AspNetCore.Mvc;

namespace Mostlylucidblog.Controllers;

public class BlogController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}