using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Dashboard;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Graph;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Trust;

namespace Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderAuthorization.PolicyName)]
public class HomeController : Controller
{
    private readonly IPatientRepository _patients;
    private readonly ITherapySessionRepository _sessions;
    private readonly ICareDeviceRepository _devices;
    private readonly ICompanyClock _clock;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public HomeController(
        IPatientRepository patients,
        ITherapySessionRepository sessions,
        ICareDeviceRepository devices,
        ICompanyClock clock,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _patients = patients;
        _sessions = sessions;
        _devices = devices;
        _clock = clock;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task<IActionResult> Graph(CancellationToken ct)
    {
        var vm = new KnowledgeGraphViewModel();
        await vm.PrepareData(_patients, _sessions, _devices, ct);
        return View(vm);
    }

    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var vm = new ProviderDashboardViewModel();
        await vm.PrepareData(_patients, _sessions, ct);
        return View(vm);
    }

    public IActionResult Trust()
    {
        return View(new TrustViewModel
        {
            HttpsActive = Request.IsHttps,
            HstsEnabled = !_environment.IsDevelopment(),
            TimeZoneIana = _clock.IanaId,
            TimeZoneLabel = _clock.Abbreviation,
            IntegrationEnabled = _configuration.GetValue<bool>("Integration:Enabled")
        });
    }
}
