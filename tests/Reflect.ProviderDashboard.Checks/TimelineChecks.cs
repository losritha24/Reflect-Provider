using Microsoft.AspNetCore.Mvc;
using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Exceptions;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Web.Areas.Provider.Controllers;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Patients;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Checks;

public static class TimelineChecks
{
    public static async Task RunAsync()
    {
        InMemoryDataStore.Seed();
        var user = new FakeCurrentUser(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        InMemoryDataStore.AssignPatientsToProvider(user.UserId!.Value.ToString());
        var patients = new InMemoryPatientRepository(user);
        var patient = (await patients.GetAllAsync(CancellationToken.None)).First();
        var sessions = new InMemoryTherapySessionRepository(patients);
        var devices = new InMemoryCareDeviceRepository(patients);
        var controller = new PatientsController(patients, sessions, devices);

        var all = await controller.Details(patient.Id, CancellationToken.None, tab: "timeline");
        var allModel = ((ViewResult)all).Model as PatientDetailsViewModel
            ?? throw new Exception("Timeline did not render.");
        Check(allModel.FilteredSessionCount == allModel.TotalSessions, "Empty filters show all past sessions");

        var vr = (ViewResult)await controller.Details(patient.Id, CancellationToken.None, TherapyPlatform.Vr, 7, tab: "timeline");
        var vrModel = (PatientDetailsViewModel)vr.Model!;
        Check(vrModel.Sessions.All(s => s.PlatformLabel.Contains("VR")
                && s.StartedAtUtc >= vrModel.AsOfUtc.AddDays(-7)
                && s.StartedAtUtc <= vrModel.AsOfUtc)
            && vrModel.Sessions.Count <= allModel.Sessions.Count,
            "Platform and days filters intersect");

        Check(await controller.Details(patient.Id, CancellationToken.None, (TherapyPlatform)999, tab: "timeline") is BadRequestResult,
            "Invalid platform is rejected");
        Check(await controller.Details(patient.Id, CancellationToken.None, days: 14, tab: "timeline") is BadRequestResult,
            "Invalid days are rejected");
        var malformed = new PatientsController(patients, sessions, devices);
        malformed.ModelState.AddModelError("platform", "Invalid platform.");
        Check(await malformed.Details(patient.Id, CancellationToken.None, tab: "timeline") is BadRequestResult,
            "Malformed filter binding is rejected");

        var futureRepository = new FutureSessionRepository(sessions, patient.Id);
        var futureController = new PatientsController(patients, futureRepository, devices);
        var future = (PatientDetailsViewModel)((ViewResult)await futureController.Details(patient.Id, CancellationToken.None, tab: "timeline")).Model!;
        Check(future.Sessions.All(s => s.StartedAtUtc <= future.AsOfUtc) && future.TotalSessions == allModel.TotalSessions,
            "Future sessions are excluded from totals and timeline");

        var cc = (PatientDetailsViewModel)((ViewResult)await controller.Details(patient.Id, CancellationToken.None, tab: "timeline", program: "Creative Calm")).Model!;
        Check(cc.Sessions.All(s => s.ModuleGroup == "Creative Calm") && cc.Program == "Creative Calm" && cc.TotalSessions == allModel.TotalSessions,
            "Program filter narrows the timeline without changing totals");
        Check(await controller.Details(patient.Id, CancellationToken.None, tab: "timeline", program: "Not A Program") is BadRequestResult,
            "An unknown program is rejected");
        Check(allModel.ProgramUsage.All(u => u.Last30Days <= u.Sessions) && allModel.NotTried.All(g => allModel.ProgramUsage.All(u => u.Group != g)),
            "Program usage reports recent counts and programs not yet tried");

        Check(await controller.Details(Guid.Empty, CancellationToken.None) is NotFoundResult,
            "Missing patient ID remains 404");
        var strangerPatients = new InMemoryPatientRepository(new FakeCurrentUser(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")));
        Check(await new PatientsController(strangerPatients, sessions, new InMemoryCareDeviceRepository(strangerPatients)).Details(patient.Id, CancellationToken.None) is NotFoundResult,
            "Unauthorized patient remains 404");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Console.WriteLine("PASS: " + message);
    }

    private sealed class FakeCurrentUser(Guid? userId) : ICurrentUserService
    {
        public Guid? UserId { get; } = userId;
        public string EntraObjectId => string.Empty;
        public string Email => string.Empty;
        public bool IsAuthenticated => UserId.HasValue;
        public bool IsInRole(string role) => true;
    }

    private sealed class FutureSessionRepository(ITherapySessionRepository inner, Guid patientId) : ITherapySessionRepository
    {
        public Task<IReadOnlyList<TherapySession>> GetAllAsync(CancellationToken ct) => inner.GetAllAsync(ct);
        public async Task<IReadOnlyList<TherapySession>> GetByPatientIdAsync(Guid id, CancellationToken ct)
        {
            var sessions = (await inner.GetByPatientIdAsync(id, ct)).ToList();
            if (id == patientId)
                sessions.Add(new TherapySession { PatientId = id, StartedAtUtc = DateTimeOffset.UtcNow.AddDays(1), Platform = TherapyPlatform.Vr });
            return sessions;
        }
        public Task<int> CountSinceAsync(DateTimeOffset sinceUtc, CancellationToken ct) => inner.CountSinceAsync(sinceUtc, ct);
        public Task<int> CountByPlatformSinceAsync(TherapyPlatform platform, DateTimeOffset sinceUtc, CancellationToken ct) => inner.CountByPlatformSinceAsync(platform, sinceUtc, ct);
    }
}
