using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Digest;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderAuthorization.PolicyName)]
public class DigestController : Controller
{
    private readonly IPatientRepository _patients;
    private readonly ITherapySessionRepository _sessions;
    private readonly FollowUpService _followUps;

    public DigestController(IPatientRepository patients, ITherapySessionRepository sessions, FollowUpService followUps)
    {
        _patients = patients;
        _sessions = sessions;
        _followUps = followUps;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new DigestViewModel();
        await vm.PrepareData(_patients, _sessions, _followUps, ct);
        return View(vm);
    }
}
