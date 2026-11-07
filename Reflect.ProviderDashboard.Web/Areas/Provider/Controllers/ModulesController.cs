using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Catalog;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Web.Infrastructure;

namespace Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderAuthorization.PolicyName)]
[Route("Provider/Modules")]
public class ModulesController : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        return View(ReflectModuleCatalog.All);
    }
}
