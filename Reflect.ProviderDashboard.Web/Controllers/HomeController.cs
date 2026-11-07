using Microsoft.AspNetCore.Mvc;

namespace Reflect.ProviderDashboard.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return User.Identity?.IsAuthenticated == true
            ? RedirectToAction("Dashboard", "Home", new { area = "Provider" })
            : View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
