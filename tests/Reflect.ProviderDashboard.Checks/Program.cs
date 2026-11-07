using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;
using Reflect.ProviderDashboard.Web.Controllers;
using Reflect.ProviderDashboard.Web.Data;
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
Check(graph.PatientCount == InMemoryDataStore.Patients.Count
    && graph.Nodes.Count(node => node.Kind == "patient") == InMemoryDataStore.Patients.Count,
    "Knowledge graph includes every assigned patient");
Check(graph.Edges.Any(edge => edge.To == "attention" || edge.Label.Contains("quieter", StringComparison.OrdinalIgnoreCase)),
    "Knowledge graph links quieter patients to the attention signal");
Check(graph.Svg.Contains("Caseload activity knowledge graph", StringComparison.Ordinal),
    "Knowledge graph renders an accessible SVG");

Check(controller.Create() is ViewResult, "Add-patient form renders");
controller.ModelState.AddModelError("DisplayName", "Required");
Check(await controller.Create(new CreatePatientViewModel
{
    DisplayName = "A",
    ExternalPatientId = "PT-19999",
    DateOfBirth = new DateOnly(1990, 1, 1)
}, ct) is ViewResult, "Invalid add-patient posts redisplay the form");
controller.ModelState.Clear();
var created = await patients.AddAsync("Riley Chen", "PT-10991", new DateOnly(1992, 4, 8), "Synthetic intake note.", ct);
Check(created.AssignedProviderId == currentUser.UserId!.Value.ToString()
    && created.Trend == EngagementTrend.Stable
    && (await patients.GetByIdAsync(created.Id, ct)).DisplayName == "Riley Chen",
    "Added patients belong to the signed-in provider");
Check(await controller.Create(new CreatePatientViewModel
{
    DisplayName = "Riley Duplicate",
    ExternalPatientId = "PT-10991",
    DateOfBirth = new DateOnly(1992, 4, 8)
}, ct) is ViewResult, "Duplicate patient IDs stay on the form");
try
{
    await patients.AddAsync("Riley Chen", "PT-10991", new DateOnly(1992, 4, 8), "", ct);
    throw new Exception("Duplicate patient ID was allowed");
}
catch (DuplicateExternalPatientIdException) { Console.WriteLine("PASS: Duplicate caseload patient IDs are rejected"); }
Check((await otherProvider.GetAllAsync(ct)).Count == 0, "Another provider cannot see a newly added patient");
try
{
    await new InMemoryPatientRepository(new TestCurrentUserService(currentUser.UserId, true, false))
        .AddAsync("No Role", "PT-00000", new DateOnly(1990, 1, 1), "", ct);
    throw new Exception("A non-provider was allowed to add a patient");
}
catch (InvalidOperationException) { Console.WriteLine("PASS: Non-providers cannot add patients"); }

Check(await controller.Remove(ct) is ViewResult, "Remove-patient form renders");
Check(await controller.Remove(new RemovePatientViewModel { PatientId = created.Id, Confirm = false }, ct) is ViewResult,
    "Removal without confirmation redisplays the form");
try
{
    await otherProvider.RemoveAsync(created.Id, ct);
    throw new Exception("Another provider was allowed to remove a caseload member");
}
catch (EntityNotFoundException) { Console.WriteLine("PASS: Providers cannot remove another provider's patient"); }
await patients.RemoveAsync(created.Id, ct);
try
{
    await patients.GetByIdAsync(created.Id, ct);
    throw new Exception("Removed patient remained visible");
}
catch (EntityNotFoundException) { Console.WriteLine("PASS: Removed patients leave the caseload"); }
Check((await patients.SearchAsync("Riley Chen", null, 1, 10, ct)).TotalCount == 0,
    "Removed patients are excluded from search");

var labeledPdf = IntakeLabeledPdf.Create("Morgan Lee", "PT-11002", "1987-06-15", "Prefers evening mobile sessions.");
var draft = IntakePdfReader.Read(new MemoryStream(labeledPdf));
Check(draft.DisplayName == "Morgan Lee" && draft.ExternalPatientId == "PT-11002"
    && draft.DateOfBirth == new DateOnly(1987, 6, 15)
    && draft.NotesSummary.Contains("evening mobile", StringComparison.Ordinal),
    "A labeled intake PDF fills name, ID, date of birth, and notes");
var pdfFile = new FormFile(new MemoryStream(labeledPdf), 0, labeledPdf.Length, "document", "intake.pdf")
{
    Headers = new HeaderDictionary(),
    ContentType = "application/pdf"
};
var imported = await Task.FromResult(controller.FromDocument(pdfFile)) as ViewResult;
Check(imported?.Model is CreatePatientViewModel filled
    && filled.DisplayName == "Morgan Lee"
    && filled.ExternalPatientId == "PT-11002"
    && filled.DateOfBirth == new DateOnly(1987, 6, 15),
    "Read PDF prefills the add-patient form");
var notPdf = new FormFile(new MemoryStream("not a pdf"u8.ToArray()), 0, 9, "document", "notes.txt")
{
    Headers = new HeaderDictionary(),
    ContentType = "text/plain"
};
Check(controller.FromDocument(notPdf) is ViewResult rejected
    && rejected.Model is CreatePatientViewModel notice
    && notice.ImportNotice.Contains("not a PDF", StringComparison.OrdinalIgnoreCase),
    "Non-PDF uploads do not prefill the form");

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
await devices.ReassignAsync(unassignedHeadset.Id, casey.Id, ct);
var leaving = await patients.AddAsync("Lea Offboard", "PT-18888", new DateOnly(1991, 2, 2), "", ct);
await devices.ReassignAsync(unassignedHeadset.Id, leaving.Id, ct);
Check(unassignedHeadset.CurrentPatientId == leaving.Id, "A leaving patient can hold a device before removal");
controller.ModelState.Clear();
Check(await controller.Remove(new RemovePatientViewModel { PatientId = leaving.Id, Confirm = true }, ct) is RedirectToActionResult,
    "Confirmed removal redirects to the caseload");
Check(unassignedHeadset.CurrentPatientId is null, "Removing a patient returns assigned devices to storage");
try
{
    await patients.GetByIdAsync(leaving.Id, ct);
    throw new Exception("Removed patient remained on the caseload");
}
catch (EntityNotFoundException) { Console.WriteLine("PASS: Confirmed removal hides the patient"); }
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

await using var clinicalConnection = new SqliteConnection("Data Source=:memory:");
await clinicalConnection.OpenAsync();
var clinicalOptions = new DbContextOptionsBuilder<ClinicalDbContext>().UseSqlite(clinicalConnection).Options;
await using var clinicalDb = new ClinicalDbContext(clinicalOptions);
await ClinicalSeeder.EnsureAsync(clinicalDb, currentUser.UserId!.Value.ToString(), reseed: true);
var ingest = new SessionIngestService(clinicalDb, clock);
var beforeCount = await clinicalDb.Sessions.CountAsync();
var ingested = await ingest.IngestAsync(new SessionIngestRequest
{
    ExternalPatientId = "PT-10085",
    StartedAt = offsetStamp,
    DurationMinutes = 12,
    Platform = "Mobile",
    ModuleName = "emotions",
    ReflectionSummary = "UTC normalize",
    AffectScore = 6,
    HardwareId = "MOB-7740"
});
Check(ingested.ModuleName == "Emotions", "Ingest canonicalizes module names to the catalog");
Check(ingested.StartedAtUtc.Offset == TimeSpan.Zero && ingested.StartedAtUtc.Hour == 21,
    "Accepted ingest rows are stored in UTC");
Check(await clinicalDb.Sessions.CountAsync() == beforeCount + 1, "Ingest appends a session without rewriting history");
var caseyId = InMemoryDataStore.Patients.Single(p => p.ExternalPatientId == "PT-10085").Id;
var ingestSessions = new EfTherapySessionRepository(clinicalDb, new EfPatientRepository(clinicalDb, currentUser));
Check((await ingestSessions.GetByPatientIdAsync(caseyId, ct)).Any(s => s.Id == ingested.Id),
    "Ingested sessions are visible through the clinical repository");
try
{
    await ingest.IngestAsync(new SessionIngestRequest
    {
        ExternalPatientId = "NO-SUCH",
        StartedAt = offsetStamp,
        DurationMinutes = 10,
        Platform = "Mobile",
        ModuleName = "Emotions"
    });
    throw new Exception("Unknown patient ingest was allowed");
}
catch (EntityNotFoundException) { Console.WriteLine("PASS: Unknown patient ingest is rejected"); }
foreach (var (platform, module) in new[] { ("Mobile", "Hope Tree"), ("Vr", "Emotions"), ("Mobile", "Made Up") })
{
    try
    {
        await ingest.IngestAsync(new SessionIngestRequest { ExternalPatientId = "PT-10085", StartedAt = offsetStamp, DurationMinutes = 10, Platform = platform, ModuleName = module });
        throw new Exception($"{platform}/{module} was accepted");
    }
    catch (ArgumentException) { Console.WriteLine($"PASS: {platform}/{module} is not a catalog module"); }
}
Check(Reflect.ProviderDashboard.Domain.Catalog.ReflectModuleCatalog.All.Count(m => m.Platform == TherapyPlatform.Mobile) == 14
    && Reflect.ProviderDashboard.Domain.Catalog.ReflectModuleCatalog.All.Count(m => m.Platform == TherapyPlatform.Vr) == 17,
    "Catalog matches the official 14 mobile and 17 XR modules");
Check(InMemoryDataStore.Sessions.All(s => Reflect.ProviderDashboard.Domain.Catalog.ReflectModuleCatalog.Find(s.Platform, s.ModuleName) is not null),
    "Every seeded session uses a catalog module for its platform");
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
await Reflect.ProviderDashboard.Checks.TeamChecks.RunAsync();
await Reflect.ProviderDashboard.Checks.OpenRouterChecks.RunAsync();

sealed class TestCurrentUserService(Guid? userId, bool authenticated = true, bool provider = true) : ICurrentUserService
{
    public Guid? UserId { get; } = userId;
    public string EntraObjectId => string.Empty;
    public string Email => string.Empty;
    public bool IsAuthenticated => authenticated;
    public bool IsInRole(string role) => provider;
}
