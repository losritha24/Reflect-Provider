using Microsoft.AspNetCore.Mvc;

namespace Reflect.ProviderDashboard.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Dashboard", "Home", new { area = "Provider" });
    }

    public IActionResult Error()
    {
        return View();
    }
}
