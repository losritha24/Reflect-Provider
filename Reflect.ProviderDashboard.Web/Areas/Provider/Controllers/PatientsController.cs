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

    public PatientsController(IPatientRepository patients, ITherapySessionRepository sessions)
    {
        _patients = patients;
        _sessions = sessions;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new PatientIndexViewModel();
        await vm.PrepareData(_patients, _sessions, ct);
        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var vm = new PatientDetailsViewModel();
        await vm.PrepareData(id, _patients, _sessions, ct);
        return View(vm);
    }
}
