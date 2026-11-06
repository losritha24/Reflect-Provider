using Reflect.ProviderDashboard.Domain.Enums;
using Reflect.ProviderDashboard.Domain.Interfaces;
using Reflect.ProviderDashboard.Domain.Services;
using Reflect.ProviderDashboard.Web.Services;

namespace Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Graph;

public sealed class KnowledgeGraphViewModel
{
    public List<GraphNode> Nodes { get; private set; } = new();
    public List<GraphEdge> Edges { get; private set; } = new();
    public string Svg { get; private set; } = string.Empty;
    public int PatientCount { get; private set; }
    public int SessionCount { get; private set; }
    public int DeviceCount { get; private set; }

    public async Task PrepareData(
        IPatientRepository patients,
        ITherapySessionRepository sessions,
        ICareDeviceRepository devices,
        CancellationToken ct)
    {
        var allPatients = (await patients.GetAllAsync(ct)).OrderBy(patient => patient.DisplayName).ToList();
        var allSessions = await sessions.GetAllAsync(ct);
        var allDevices = await devices.GetAllAsync(ct);
        PatientCount = allPatients.Count;
        SessionCount = allSessions.Count;
        DeviceCount = allDevices.Count(device => device.CurrentPatientId is not null);

        const double width = 960;
        const double height = 720;
        const double cx = 480;
        const double cy = 330;
        const double patientRadius = 168;
        const double deviceRadius = 268;

        Nodes.Add(new GraphNode("hub", "hub", "Caseload", $"{PatientCount} people · {SessionCount} sessions", "", cx, cy, false));
        Nodes.Add(new GraphNode("xr", "platform", "Reflect XR", "Immersive", "", 170, 88, false));
        Nodes.Add(new GraphNode("mobile", "platform", "Reflect Mobile", "Phone / tablet", "", 790, 88, false));
        Nodes.Add(new GraphNode("attention", "signal", "Needs attention", "Fewer sessions", "/Provider/Home/Dashboard", 170, 548, false));
        Nodes.Add(new GraphNode("affect", "signal", "Below self", "Affect vs baseline", "/Provider/Home/Dashboard", 790, 548, false));

        var since = DateTimeOffset.UtcNow.AddDays(-7);
        for (var i = 0; i < allPatients.Count; i++)
        {
            var patient = allPatients[i];
            var angle = -Math.PI / 2 + 2 * Math.PI * i / Math.Max(1, allPatients.Count);
            var x = cx + patientRadius * Math.Cos(angle);
            var y = cy + patientRadius * Math.Sin(angle);
            var patientSessions = allSessions.Where(session => session.PatientId == patient.Id).ToList();
            var xrCount = patientSessions.Count(session => session.Platform == TherapyPlatform.Vr);
            var mobileCount = patientSessions.Count(session => session.Platform == TherapyPlatform.Mobile);
            var current = patientSessions.Count(session => session.StartedAtUtc >= since);
            var previous = patientSessions.Count(session =>
                session.StartedAtUtc >= since.AddDays(-7) && session.StartedAtUtc < since);
            var quieter = current < previous;
            var trajectory = EmotionalTrajectoryCalculator.Build(patientSessions);
            var belowSelf = trajectory.LatestDeviatesFromSelf;
            var id = $"patient-{patient.Id:N}";
            Nodes.Add(new GraphNode(
                id,
                "patient",
                patient.DisplayName,
                $"{patientSessions.Count} sessions",
                $"/Provider/Patients/Details/{patient.Id}",
                x,
                y,
                quieter || belowSelf));
            Edges.Add(new GraphEdge("hub", id, "assigned", quieter || belowSelf));
            if (xrCount > 0)
                Edges.Add(new GraphEdge(id, "xr", $"{xrCount} XR", false));
            if (mobileCount > 0)
                Edges.Add(new GraphEdge(id, "mobile", $"{mobileCount} mobile", false));
            if (quieter)
                Edges.Add(new GraphEdge(id, "attention", "quieter this week", true));
            if (belowSelf)
                Edges.Add(new GraphEdge(id, "affect", "below personal baseline", true));

            var assigned = allDevices.Where(device => device.CurrentPatientId == patient.Id).ToList();
            for (var d = 0; d < assigned.Count; d++)
            {
                var device = assigned[d];
                var spread = assigned.Count == 1 ? 0 : (d - (assigned.Count - 1) / 2.0) * 0.18;
                var dx = cx + deviceRadius * Math.Cos(angle + spread);
                var dy = cy + deviceRadius * Math.Sin(angle + spread);
                var deviceId = $"device-{device.Id:N}";
                Nodes.Add(new GraphNode(
                    deviceId,
                    "device",
                    device.HardwareId,
                    device.Kind == DeviceKind.XrHeadset ? "Headset" : "Mobile install",
                    "/Provider/Devices/Index",
                    dx,
                    dy,
                    false));
                Edges.Add(new GraphEdge(id, deviceId, "using", false));
            }
        }

        Svg = KnowledgeGraphSvg.From(Nodes, Edges, width, height);
    }

    public sealed record GraphNode(
        string Id,
        string Kind,
        string Label,
        string Subtitle,
        string Href,
        double X,
        double Y,
        bool Alert);

    public sealed record GraphEdge(string From, string To, string Label, bool Alert);
}
