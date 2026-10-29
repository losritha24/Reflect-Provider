using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Patients;
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
var controller = new PatientsController(patients, sessions);
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

var dashboard = new Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Dashboard.ProviderDashboardViewModel();
await dashboard.PrepareData(patients, sessions, ct);
Check(dashboard.NeedsAttention.Count == 1 && dashboard.NeedsAttention[0].PatientName == "Sam Patel"
    && dashboard.NeedsAttention[0].CurrentSessions == 0 && dashboard.NeedsAttention[0].PreviousSessions > 0,
    "Attention queue reflects a real decrease in the seeded session windows");

var otherSessions = new InMemoryTherapySessionRepository(otherProvider);
Check((await otherSessions.GetAllAsync(ct)).Count == 0 && await otherSessions.CountSinceAsync(DateTimeOffset.MinValue, ct) == 0,
    "Unassigned providers cannot read sessions or aggregate counts");
try
{
    await otherSessions.GetByPatientIdAsync(patient.Id, ct);
    throw new Exception("Unassigned session access was allowed");
}
catch (EntityNotFoundException) { Console.WriteLine("PASS: Unassigned patient session access is denied"); }

var anonymousWithClaim = new InMemoryPatientRepository(new TestCurrentUserService(currentUser.UserId.Value, false));
Check((await anonymousWithClaim.GetAllAsync(ct)).Count == 0,
    "An unauthenticated identity cannot use an assigned provider ID claim");

Check((await new InMemoryPatientRepository(new TestCurrentUserService(null)).GetAllAsync(ct)).Count == 0,
    "A missing provider ID fails closed");
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

sealed class TestCurrentUserService(Guid? userId, bool authenticated = true, bool provider = true) : ICurrentUserService
{
    public Guid? UserId { get; } = userId;
    public string EntraObjectId => string.Empty;
    public string Email => string.Empty;
    public bool IsAuthenticated => authenticated;
    public bool IsInRole(string role) => provider;
}
