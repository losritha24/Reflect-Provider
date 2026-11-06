using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Patients;

namespace Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderAuthorization.PolicyName)]
public class PatientsController : Controller
{
    private readonly IPatientRepository _patients;
    private readonly ITherapySessionRepository _sessions;
    private readonly ICareDeviceRepository _devices;

    public PatientsController(IPatientRepository patients, ITherapySessionRepository sessions, ICareDeviceRepository devices)
    {
        _patients = patients;
        _sessions = sessions;
        _devices = devices;
    }

    public async Task<IActionResult> Index(CancellationToken ct, string search = null, EngagementTrend? trend = null, int page = 1)
    {
        if (!ModelState.IsValid
            || (trend.HasValue && !Enum.IsDefined(trend.Value))
            || page < 1
            || search?.Length > 200)
        {
            return BadRequest();
        }
        var vm = new PatientIndexViewModel { Search = search?.Trim(), Trend = trend, Page = page };
        await vm.PrepareData(_patients, _sessions, ct);
        return View(vm);
    }

    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken ct,
        TherapyPlatform? platform = null,
        int? days = null,
        string search = null,
        EngagementTrend? trend = null,
        int page = 1,
        string tab = "overview")
    {
        if (id == Guid.Empty) return NotFound();
        if (!ModelState.IsValid
            || (platform.HasValue && !Enum.IsDefined(platform.Value))
            || (days.HasValue && days is not (7 or 30 or 90))
            || (trend.HasValue && !Enum.IsDefined(trend.Value))
            || page < 1
            || search?.Length > 200
            || (tab != "overview" && tab != "timeline"))
        {
            return BadRequest();
        }

        var vm = new PatientDetailsViewModel();
        try
        {
            await vm.PrepareData(
                id,
                _patients,
                _sessions,
                _devices,
                ct,
                DateTimeOffset.UtcNow,
                platform,
                days,
                search?.Trim(),
                trend,
                page,
                tab);
            return View(vm);
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
    }
}
