using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Reports;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderAuthorization.PolicyName)]
public class ReportsController : Controller
{
    public const int MaxRangeDays = 366;

    private readonly IPatientRepository _patients;
    private readonly ITherapySessionRepository _sessions;
    private readonly ICompanyClock _clock;
    private readonly Services.AuditLog _audit;

    public ReportsController(
        IPatientRepository patients,
        ITherapySessionRepository sessions,
        ICompanyClock clock,
        Services.AuditLog audit = null)
    {
        _audit = audit;
        _patients = patients;
        _sessions = sessions;
        _clock = clock;
    }

    public async Task<IActionResult> Index(
        CancellationToken ct,
        Guid? patientId = null,
        DateOnly? from = null,
        DateOnly? to = null,
        bool? summary = null,
        bool? history = null,
        bool? split = null,
        bool preview = false)
    {
        if (!TryResolveRange(from, to, out var rangeFrom, out var rangeTo))
            return BadRequest();

        try
        {
            var vm = await BuildAsync(
                patientId,
                rangeFrom,
                rangeTo,
                Include(preview, summary),
                Include(preview, history),
                Include(preview, split),
                ct);
            return View(vm);
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Pdf(
        CancellationToken ct,
        Guid? patientId = null,
        DateOnly? from = null,
        DateOnly? to = null,
        bool? summary = null,
        bool? history = null,
        bool? split = null,
        bool preview = false)
    {
        if (!TryResolveRange(from, to, out var rangeFrom, out var rangeTo))
            return BadRequest();

        try
        {
            var vm = await BuildAsync(
                patientId,
                rangeFrom,
                rangeTo,
                Include(preview, summary),
                Include(preview, history),
                Include(preview, split),
                ct);
            if (!vm.HasCaseload)
                return NotFound();
            if (_audit is not null && User?.Identity?.IsAuthenticated == true)
                await _audit.RecordAsync(User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "provider", "report-exported", $"{rangeFrom:yyyy-MM-dd} to {rangeTo:yyyy-MM-dd}", ct);
            var bytes = ClinicalReportPdf.From(vm);
            var fileName = $"reflect-report-{Slug(vm.PatientName)}-{rangeFrom:yyyyMMdd}-{rangeTo:yyyyMMdd}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
    }

    private async Task<ClinicalReportViewModel> BuildAsync(
        Guid? patientId,
        DateOnly from,
        DateOnly to,
        bool summary,
        bool history,
        bool split,
        CancellationToken ct)
    {
        var vm = new ClinicalReportViewModel();
        await vm.PrepareData(
            _patients,
            _sessions,
            _clock,
            ct,
            patientId,
            from,
            to,
            summary,
            history,
            split,
            HttpContext?.User?.FindFirst("display_name")?.Value
            ?? HttpContext?.User?.Identity?.Name
            ?? "Assigned clinician");
        return vm;
    }

    private static bool Include(bool preview, bool? value) => preview ? value == true : value ?? true;

    private bool TryResolveRange(DateOnly? from, DateOnly? to, out DateOnly rangeFrom, out DateOnly rangeTo)
    {
        var today = DateOnly.FromDateTime(_clock.ToCompany(DateTimeOffset.UtcNow).DateTime);
        rangeFrom = from ?? today.AddDays(-29);
        rangeTo = to ?? today;
        if (!ModelState.IsValid)
            return false;
        if (rangeFrom > rangeTo)
            return false;
        return rangeTo.DayNumber - rangeFrom.DayNumber <= MaxRangeDays;
    }

    private static string Slug(string value)
    {
        var chars = (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var slug = new string(chars).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        return string.IsNullOrEmpty(slug) ? "patient" : slug;
    }
}
