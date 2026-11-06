using Microsoft.AspNetCore.Http;
using Reflect.ProviderDashboard.Web.Infrastructure;
using Reflect.ProviderDashboard.Web.Extensions;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Patients;
using Reflect.ProviderDashboard.Domain.Entities;
using Reflect.ProviderDashboard.Domain.Enums;

public static class SecurityChecks
{
    public static Task RunAsync()
    {
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            Console.WriteLine("PASS: " + message);
        }
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.0.2.1");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.2";
        Check(AccountRateLimit.ClientKey(context) == "192.0.2.1", "Raw forwarding headers cannot select a rate-limit bucket");
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("::ffff:192.0.2.1");
        Check(AccountRateLimit.ClientKey(context) == "192.0.2.1", "Mapped IPv4 addresses share the same rate-limit bucket");
        context.Request.Method = "POST";
        Check(!AccountRateLimit.IsCredentialAttempt(context), "Unmarked POST actions such as logout bypass credential limits");
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
            new EndpointMetadataCollection(new Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute("account")), "login"));
        Check(AccountRateLimit.IsCredentialAttempt(context), "Credential endpoint metadata selects request limits");
        context.Request.Method = "GET";
        Check(!AccountRateLimit.IsCredentialAttempt(context), "Sign-in pages do not consume credential budgets");
        var patient = new Patient { DisplayName = "Test", ExternalPatientId = "SYN-TEST" };
        var row = PatientRowViewModel.FromEntity(patient, new[] {
            new TherapySession { StartedAtUtc = DateTimeOffset.UtcNow.AddDays(1), Platform = TherapyPlatform.Vr }
        });
        Check(row.TotalSessions == 0 && row.LastSessionDisplay == "—", "Future sessions do not change caseload totals or last activity");
        Check(DateTimeOffset.Parse("2026-03-08T06:59:00Z").ToUsDateTimeString() == "3/8/2026 1:59 AM EST"
            && DateTimeOffset.Parse("2026-03-08T07:00:00Z").ToUsDateTimeString() == "3/8/2026 3:00 AM EDT",
            "Eastern display skips the spring DST gap correctly");
        Check(DateTimeOffset.Parse("2026-11-01T05:30:00Z").ToUsDateTimeString() == "11/1/2026 1:30 AM EDT"
            && DateTimeOffset.Parse("2026-11-01T06:30:00Z").ToUsDateTimeString() == "11/1/2026 1:30 AM EST",
            "Eastern fall DST overlap is disambiguated by zone label");
        return Task.CompletedTask;
    }
}
