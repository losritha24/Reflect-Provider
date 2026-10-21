using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Dashboard;

namespace Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderAuthorization.PolicyName)]
public class HomeController : Controller
{
    private readonly IPatientRepository _patients;
    private readonly ITherapySessionRepository _sessions;

    public HomeController(IPatientRepository patients, ITherapySessionRepository sessions)
    {
        _patients = patients;
        _sessions = sessions;
    }

    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var vm = new ProviderDashboardViewModel();
        await vm.PrepareData(_patients, _sessions, ct);
        return View(vm);
    }
}
