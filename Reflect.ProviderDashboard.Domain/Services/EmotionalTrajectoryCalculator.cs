using Reflect.ProviderDashboard.Domain.Entities;

namespace Reflect.ProviderDashboard.Domain.Services;

public sealed record AffectPoint(
    Guid SessionId,
    DateTimeOffset StartedAtUtc,
    int Score,
    double? RollingBaseline,
    double? UsualLow,
    double? UsualHigh,
    bool DeviatesBelowSelf);

public sealed record EmotionalTrajectory(
    IReadOnlyList<AffectPoint> Points,
    bool LatestDeviatesFromSelf,
    double? LatestPersonalBaseline,
    int? LatestScore);

public static class EmotionalTrajectoryCalculator
{
    public const int MinPriorSessions = 3;
    public const int RollingWindowSize = 5;
    public const int RollingWindowDays = 28;
    public const double DeviationPoints = 2.0;

    public static EmotionalTrajectory Build(IEnumerable<TherapySession> sessions)
    {
        var ordered = sessions
            .Where(session => !session.IsDeleted && session.AffectScore is >= 1 and <= 10)
            .OrderBy(session => session.StartedAtUtc)
            .ThenBy(session => session.Id)
            .ToList();

        var points = new List<AffectPoint>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var current = ordered[i];
            var windowStart = current.StartedAtUtc.AddDays(-RollingWindowDays);
            var prior = ordered.Take(i)
                .Where(session => session.StartedAtUtc >= windowStart)
                .TakeLast(RollingWindowSize)
                .ToList();

            double? baseline = null;
            double? low = null;
            double? high = null;
            var deviates = false;

            if (prior.Count >= MinPriorSessions)
            {
                var scores = prior.Select(session => (double)session.AffectScore!.Value).ToList();
                baseline = scores.Average();
                var variance = scores.Average(value => Math.Pow(value - baseline.Value, 2));
                var band = Math.Max(1.0, Math.Sqrt(variance));
                low = baseline - band;
                high = baseline + band;
                var score = current.AffectScore!.Value;
                deviates = score <= baseline.Value - DeviationPoints && score < low.Value;
            }

            points.Add(new AffectPoint(
                current.Id,
                current.StartedAtUtc,
                current.AffectScore!.Value,
                baseline,
                low,
                high,
                deviates));
        }

        var latest = points.Count == 0 ? null : points[^1];
        return new EmotionalTrajectory(
            points,
            latest?.DeviatesBelowSelf == true,
            latest?.RollingBaseline,
            latest?.Score);
    }
}
