using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;
using Reflect.ProviderDashboard.Web.Controllers;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.Integration;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Patients;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Reports;
using Reflect.ProviderDashboard.Web.Services;

static void Check(bool condition, string description)
{
    if (!condition) throw new Exception(description);
    Console.WriteLine($"PASS: {description}");
}

InMemoryDataStore.Seed();
var currentUser = new TestCurrentUserService(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
InMemoryDataStore.AssignPatientsToProvider(currentUser.UserId!.Value.ToString());
var patients = new InMemoryPatientRepository(currentUser);
var sessions = new InMemoryTherapySessionRepository(patients);
var ct = CancellationToken.None;
var first = await patients.SearchAsync(null, null, 1, 2, ct);
var second = await patients.SearchAsync(null, null, 2, 2, ct);
Check(first.Items.Count == 2 && second.Items.Count == 2 && !first.Items.Select(p => p.Id).Intersect(second.Items.Select(p => p.Id)).Any(), "Pages do not overlap");
var last = await patients.SearchAsync(null, null, int.MaxValue, 2, ct);
Check(last.Page == (int)Math.Ceiling(last.TotalCount / 2d), "Out-of-range pages clamp to last page");
var search = await patients.SearchAsync("  sAM  ", EngagementTrend.Declining, 1, 10, ct);
Check(search.TotalCount == 1 && search.Items[0].DisplayName == "Sam Patel", "Search trims and ignores case while combining filters");
var external = await patients.SearchAsync(search.Items[0].ExternalPatientId, null, 1, 10, ct);
Check(external.TotalCount == 1, "External patient IDs are searchable");
var empty = await patients.SearchAsync("no such patient", null, -1, 10, ct);
Check(empty.Items.Count == 0 && empty.Page == 1, "Empty results have a valid page");
try
{
    await patients.SearchAsync(null, null, 1, 10, new CancellationToken(true));
    throw new Exception("Cancellation was ignored");
}
catch (OperationCanceledException) { Console.WriteLine("PASS: Search honors cancellation"); }
var devices = new InMemoryCareDeviceRepository(patients);
var controller = new PatientsController(patients, sessions, devices);
Check(await controller.Details(Guid.NewGuid(), ct) is NotFoundResult, "Unknown patient returns 404");
Check(await controller.Details(Guid.Empty, ct) is NotFoundResult, "Missing patient ID returns 404");
controller.ModelState.AddModelError("id", "Invalid GUID");
Check(await controller.Details(Guid.Empty, ct) is NotFoundResult, "Malformed patient ID returns 404");
controller.ModelState.Clear();
var patient = search.Items[0];
patient.IsDeleted = true;
Check(await controller.Details(patient.Id, ct) is NotFoundResult, "Deleted patient returns 404");
Check((await patients.SearchAsync("Sam", null, 1, 10, ct)).TotalCount == 0, "Deleted patients are excluded from search");
patient.IsDeleted = false;
Check(await controller.Details(patient.Id, ct) is ViewResult, "Existing patient still renders");
Check(await controller.Index(ct, trend: (EngagementTrend)999) is BadRequestResult, "Undefined trend is rejected");
var vm = new PatientIndexViewModel { Search = "Sam" };
await vm.PrepareData(patients, sessions, ct);
Check(vm.Rows.Count == 1 && vm.Rows[0].TotalSessions == (await sessions.GetByPatientIdAsync(patient.Id, ct)).Count, "Filtered rows retain session totals");

var otherProvider = new InMemoryPatientRepository(
    new TestCurrentUserService(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")));
Check((await otherProvider.SearchAsync(null, null, 1, 10, ct)).TotalCount == 0,
    "Providers cannot list another provider's patients");
try
{
    await otherProvider.GetByIdAsync(patient.Id, ct);
    throw new Exception("Unassigned patient access was allowed");
}
catch (EntityNotFoundException)
{
    Console.WriteLine("PASS: Unassigned patient access is denied");
}

var graph = new Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Graph.KnowledgeGraphViewModel();
await graph.PrepareData(patients, sessions, devices, ct);
Check(graph.PatientCount == 5 && graph.Nodes.Count(node => node.Kind == "patient") == 5,
    "Knowledge graph includes every assigned patient");
Check(graph.Edges.Any(edge => edge.To == "attention" || edge.Label.Contains("quieter", StringComparison.OrdinalIgnoreCase)),
    "Knowledge graph links quieter patients to the attention signal");
Check(graph.Svg.Contains("Caseload activity knowledge graph", StringComparison.Ordinal),
    "Knowledge graph renders an accessible SVG");

var dashboard = new Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Dashboard.ProviderDashboardViewModel();
await dashboard.PrepareData(patients, sessions, ct);
Check(dashboard.NeedsAttention.Count == 1 && dashboard.NeedsAttention[0].PatientName == "Sam Patel"
    && dashboard.NeedsAttention[0].CurrentSessions == 0 && dashboard.NeedsAttention[0].PreviousSessions > 0,
    "Attention queue reflects a real decrease in the seeded session windows");
Check(dashboard.AffectDeviations.Any(row => row.PatientName == "Casey Nguyen" && row.LatestScore < row.PersonalBaseline),
    "Affect flags use each patient's own baseline rather than a caseload average");

var casey = (await patients.SearchAsync("Casey", null, 1, 10, ct)).Items[0];
var caseySessions = await sessions.GetByPatientIdAsync(casey.Id, ct);
var caseyTrajectory = Reflect.ProviderDashboard.Domain.Services.EmotionalTrajectoryCalculator.Build(caseySessions);
Check(caseyTrajectory.LatestDeviatesFromSelf && caseyTrajectory.Points.Count(point => point.DeviatesBelowSelf) >= 1,
    "Casey Nguyen's latest affect is below their personal rolling baseline");

var otherPatientMean = caseySessions.Average(session => session.AffectScore!.Value);
var taylor = (await patients.SearchAsync("Taylor", null, 1, 10, ct)).Items[0];
var taylorTrajectory = Reflect.ProviderDashboard.Domain.Services.EmotionalTrajectoryCalculator.Build(
    await sessions.GetByPatientIdAsync(taylor.Id, ct));
Check(!taylorTrajectory.LatestDeviatesFromSelf,
    "A high personal baseline does not flag a patient whose latest score stays near their own range");
Check(taylorTrajectory.LatestScore != otherPatientMean,
    "Trajectory calculation does not substitute another patient's mean");

var alex = (await patients.SearchAsync("Alex", null, 1, 10, ct)).Items[0];
var unassignedHeadset = InMemoryDataStore.Devices.Single(device => device.Id == InMemoryDataStore.HeadsetUnassignedId);
Check(unassignedHeadset.CurrentPatientId is null, "XR-090 remains a device after leaving Alex's assignment");
await devices.ReassignAsync(unassignedHeadset.Id, casey.Id, ct);
Check(unassignedHeadset.CurrentPatientId == casey.Id, "A device can move to another patient without being recreated");
Check((await sessions.GetByPatientIdAsync(alex.Id, ct)).Any(session => session.DeviceId == InMemoryDataStore.HeadsetUnassignedId),
    "Past sessions keep the original device reference after reassignment");
Check((await devices.GetHistoryAsync(unassignedHeadset.Id, ct)).Count >= 2,
    "Reassignment appends history instead of binding the device to one patient lifecycle");
await devices.ReassignAsync(unassignedHeadset.Id, null, ct);

var otherSessions = new InMemoryTherapySessionRepository(otherProvider);
Check((await otherSessions.GetAllAsync(ct)).Count == 0 && await otherSessions.CountSinceAsync(DateTimeOffset.MinValue, ct) == 0,
    "Unassigned providers cannot read sessions or aggregate counts");
try
{
    await otherSessions.GetByPatientIdAsync(patient.Id, ct);
    throw new Exception("Unassigned session access was allowed");
}
catch (EntityNotFoundException) { Console.WriteLine("PASS: Unassigned patient session access is denied"); }

var clock = new CompanyClock("America/New_York", "ET");
var todayEt = DateOnly.FromDateTime(clock.ToCompany(DateTimeOffset.UtcNow).DateTime);
var reports = new ReportsController(patients, sessions, clock);
var report = new ClinicalReportViewModel();
await report.PrepareData(patients, sessions, clock, ct, taylor.Id, todayEt.AddDays(-14), todayEt, true, true, true, "Dr. Demo");
Check(report.PatientName == "Taylor Brooks" && report.ExternalPatientId == "PT-10074",
    "Report preview names the selected patient");
Check(report.ReportingPeriod.Contains(todayEt.ToString("M/d/yyyy"), StringComparison.Ordinal)
    && report.MainModule == "Flow",
    "Report preview includes the reporting period and the patient's main module");
Check(report.SessionCount == 4 && report.Sessions.Count == 4,
    "Report session history stays inside the selected date range");
var emptyReport = new ClinicalReportViewModel();
await emptyReport.PrepareData(patients, sessions, clock, ct, taylor.Id, todayEt.AddDays(-400), todayEt.AddDays(-370), true, true, true, "Dr. Demo");
Check(emptyReport.SessionCount == 0 && emptyReport.MainModule == "No sessions in range",
    "An empty date range does not invent sessions");
Check(await reports.Index(ct, from: todayEt, to: todayEt.AddDays(-1)) is BadRequestResult,
    "A reversed report date range is rejected");
Check(await reports.Index(ct, patientId: Guid.NewGuid()) is NotFoundResult,
    "Unknown patients cannot be reported");
var otherReports = new ReportsController(otherProvider, otherSessions, clock);
Check(await otherReports.Index(ct, patientId: taylor.Id) is NotFoundResult,
    "Providers cannot report on another provider's patient");
var pdf = await reports.Pdf(ct, patientId: taylor.Id, from: todayEt.AddDays(-14), to: todayEt) as FileContentResult;
Check(pdf is { ContentType: "application/pdf", FileDownloadName: not null }
    && pdf.FileContents.AsSpan(0, 5).SequenceEqual("%PDF-"u8.ToArray()),
    "Report export is a downloadable PDF");
var pdfText = System.Text.Encoding.Latin1.GetString(pdf!.FileContents);
Check(pdfText.Contains("Taylor Brooks", StringComparison.Ordinal)
    && pdfText.Contains("Window:", StringComparison.Ordinal)
    && pdfText.Contains("Main module", StringComparison.Ordinal)
    && pdfText.Contains("Flow", StringComparison.Ordinal),
    "PDF includes patient name, reporting period, and main module");

var anonymousWithClaim = new InMemoryPatientRepository(new TestCurrentUserService(currentUser.UserId.Value, false));
Check((await anonymousWithClaim.GetAllAsync(ct)).Count == 0,
    "An unauthenticated identity cannot use an assigned provider ID claim");

Check((await new InMemoryPatientRepository(new TestCurrentUserService(null)).GetAllAsync(ct)).Count == 0,
    "A missing provider ID fails closed");

var offsetStamp = DateTimeOffset.Parse("2026-11-06T16:00:00-05:00");
Check(clock.ToUtc(offsetStamp).Offset == TimeSpan.Zero
    && clock.ToUtc(offsetStamp).UtcDateTime == new DateTime(2026, 11, 6, 21, 0, 0, DateTimeKind.Utc),
    "Ingest timestamps are normalized to UTC for RDS timestamptz");
Check(clock.FormatDateTime(offsetStamp).Contains("4:00 PM", StringComparison.Ordinal)
    && clock.Abbreviation == "ET",
    "Company display uses the configured IANA zone rather than the browser clock");

Check(!IngestController.KeyMatches("", "secret") && !IngestController.KeyMatches("secret", "secre"),
    "Integration keys reject empty and length-mismatched values");
Check(IngestController.KeyMatches("reflect-demo-ingest-key", "reflect-demo-ingest-key"),
    "A matching integration key is accepted");

var ingest = new SessionIngestService(clock);
var beforeCount = InMemoryDataStore.Sessions.Count;
var ingested = ingest.Ingest(new SessionIngestRequest
{
    ExternalPatientId = "PT-10085",
    StartedAt = offsetStamp,
    DurationMinutes = 12,
    Platform = "Mobile",
    ModuleName = "Ingest Check",
    ReflectionSummary = "UTC normalize",
    AffectScore = 6,
    HardwareId = "MOB-7740"
});
Check(ingested.StartedAtUtc.Offset == TimeSpan.Zero && ingested.StartedAtUtc.Hour == 21,
    "Accepted ingest rows are stored in UTC");
Check(InMemoryDataStore.Sessions.Count == beforeCount + 1, "Ingest appends a session without rewriting history");
try
{
    ingest.Ingest(new SessionIngestRequest
    {
        ExternalPatientId = "NO-SUCH",
        StartedAt = offsetStamp,
        DurationMinutes = 10,
        Platform = "Mobile",
        ModuleName = "Missing"
    });
    throw new Exception("Unknown patient ingest was allowed");
}
catch (EntityNotFoundException) { Console.WriteLine("PASS: Unknown patient ingest is rejected"); }
Check((await new InMemoryPatientRepository(new TestCurrentUserService(currentUser.UserId, true, false)).GetAllAsync(ct)).Count == 0,
    "An authenticated identity without the provider role fails closed");

using (var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()))
{
    var store = new InMemoryTicketStore(cache, TimeProvider.System);
    var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(
        new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity("test")),
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10) }, "test");
    var firstKey = await store.StoreAsync(ticket);
    var secondKey = await store.StoreAsync(ticket);
    Check(firstKey != secondKey, "Independent logins have independent session keys");
    await store.RemoveAsync(firstKey);
    Check(await store.RetrieveAsync(firstKey) is null && await store.RetrieveAsync(secondKey) is not null,
        "Logout revokes only the selected session");
    await store.RenewAsync(firstKey, ticket);
    Check(await store.RetrieveAsync(firstKey) is null, "A late renewal cannot resurrect a revoked session");
    ticket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddHours(5);
    await store.RenewAsync(secondKey, ticket);
    Check((await store.RetrieveAsync(secondKey))!.Properties.ExpiresUtc < DateTimeOffset.UtcNow.AddMinutes(11),
        "Ticket renewal preserves the original absolute expiry");
    ticket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
    Check(await store.RetrieveAsync(await store.StoreAsync(ticket)) is null, "Expired tickets cannot authenticate");
}

await SecurityChecks.RunAsync();
await Reflect.ProviderDashboard.Checks.TimelineChecks.RunAsync();
await Reflect.ProviderDashboard.Checks.ChatAndTimeChecks.RunAsync();
await Reflect.ProviderDashboard.Checks.ClinicalStoreChecks.RunAsync();

sealed class TestCurrentUserService(Guid? userId, bool authenticated = true, bool provider = true) : ICurrentUserService
{
    public Guid? UserId { get; } = userId;
    public string EntraObjectId => string.Empty;
    public string Email => string.Empty;
    public bool IsAuthenticated => authenticated;
    public bool IsInRole(string role) => provider;
}
