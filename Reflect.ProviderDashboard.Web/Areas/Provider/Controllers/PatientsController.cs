using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Patients;
using Reflect.ProviderDashboard.Web.Services;

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

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreatePatientViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePatientViewModel model, CancellationToken ct)
    {
        if (model.DisplayName?.Length > 200
            || model.ExternalPatientId?.Length > 64
            || model.NotesSummary?.Length > 2000)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var created = await _patients.AddAsync(
                model.DisplayName.Trim(),
                model.ExternalPatientId.Trim(),
                model.DateOfBirth!.Value,
                model.NotesSummary?.Trim() ?? string.Empty,
                ct);
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (DuplicateExternalPatientIdException)
        {
            ModelState.AddModelError(nameof(model.ExternalPatientId), "That patient ID is already on this caseload.");
            return View(model);
        }
        catch (InvalidOperationException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(IntakePdfReader.MaxBytes + 32_768)]
    [RequestFormLimits(MultipartBodyLengthLimit = IntakePdfReader.MaxBytes + 32_768)]
    public IActionResult FromDocument(IFormFile document)
    {
        var model = new CreatePatientViewModel();
        if (document is null || document.Length == 0)
        {
            model.ImportNotice = "Choose a PDF first.";
            return View("Create", model);
        }

        if (document.Length > IntakePdfReader.MaxBytes)
        {
            model.ImportNotice = "Use a PDF smaller than 1 MB.";
            return View("Create", model);
        }

        if (!document.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            model.ImportNotice = "That file is not a PDF.";
            return View("Create", model);
        }

        try
        {
            using var copy = new MemoryStream();
            document.CopyTo(copy);
            var bytes = copy.ToArray();
            if (bytes.Length < 5 || bytes[0] != (byte)'%' || bytes[1] != (byte)'P' || bytes[2] != (byte)'D' || bytes[3] != (byte)'F')
            {
                model.ImportNotice = "That file is not a PDF.";
                return View("Create", model);
            }

            copy.Position = 0;
            var draft = IntakePdfReader.Read(copy);
            model.DisplayName = draft.DisplayName;
            model.ExternalPatientId = draft.ExternalPatientId;
            model.DateOfBirth = draft.DateOfBirth;
            model.NotesSummary = draft.NotesSummary;
            model.ImportNotice = draft.Notice;
            return View("Create", model);
        }
        catch (InvalidDataException ex)
        {
            model.ImportNotice = ex.Message;
            return View("Create", model);
        }
        catch (InvalidOperationException)
        {
            model.ImportNotice = "The PDF could not be read. Use a text-based PDF, not a scanned photo.";
            return View("Create", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Remove(CancellationToken ct, Guid? patientId = null)
    {
        var vm = await RemoveForm(ct, patientId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(RemovePatientViewModel model, CancellationToken ct)
    {
        model.Patients = await _patients.GetAllAsync(ct);
        if (model.PatientId is null || model.PatientId == Guid.Empty)
            ModelState.AddModelError(nameof(model.PatientId), "Choose someone to remove.");
        if (!model.Confirm)
            ModelState.AddModelError(nameof(model.Confirm), "Confirm that this person is no longer using Reflect.");
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var assigned = await _devices.GetByPatientIdAsync(model.PatientId.Value, ct);
            foreach (var device in assigned)
                await _devices.ReassignAsync(device.Id, null, ct);
            await _patients.RemoveAsync(model.PatientId.Value, ct);
            return RedirectToAction(nameof(Index));
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException)
        {
            return Forbid();
        }
    }

    private async Task<RemovePatientViewModel> RemoveForm(CancellationToken ct, Guid? patientId)
    {
        var patients = await _patients.GetAllAsync(ct);
        return new RemovePatientViewModel
        {
            Patients = patients,
            PatientId = patientId is { } id && patients.Any(patient => patient.Id == id) ? id : null
        };
    }
}
