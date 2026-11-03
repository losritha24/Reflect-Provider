using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Devices;

namespace Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderAuthorization.PolicyName)]
public class DevicesController : Controller
{
    private readonly ICareDeviceRepository _devices;
    private readonly IPatientRepository _patients;

    public DevicesController(ICareDeviceRepository devices, IPatientRepository patients)
    {
        _devices = devices;
        _patients = patients;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new DeviceIndexViewModel();
        await vm.PrepareData(_devices, _patients, ct);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Reassign(Guid id, Guid? patientId, CancellationToken ct)
    {
        if (id == Guid.Empty)
            return NotFound();

        try
        {
            await _devices.ReassignAsync(id, patientId == Guid.Empty ? null : patientId, ct);
            return RedirectToAction(nameof(Index));
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
    }
}
