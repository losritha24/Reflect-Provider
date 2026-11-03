using System.Globalization;
using System.Text;
using Reflect.ProviderDashboard.Domain.Services;

namespace Reflect.ProviderDashboard.Web.Services;

public static class AffectChartSvg
{
    public static string From(EmotionalTrajectory trajectory)
    {
        if (trajectory.Points.Count == 0)
            return string.Empty;

        const double width = 640;
        const double height = 220;
        const double padX = 36;
        const double padY = 24;
        var plotW = width - padX * 2;
        var plotH = height - padY * 2;
        var points = trajectory.Points;
        double X(int i) => padX + (points.Count == 1 ? plotW / 2 : plotW * i / (points.Count - 1));
        double Y(double score) => padY + plotH * (1 - (score - 1) / 9.0);

        var line = new StringBuilder();
        for (var i = 0; i < points.Count; i++)
        {
            line.Append(i == 0 ? "M" : " L");
            line.Append(Inv(X(i))).Append(' ').Append(Inv(Y(points[i].Score)));
        }

        var baseline = new StringBuilder();
        var hasBaseline = false;
        for (var i = 0; i < points.Count; i++)
        {
            if (points[i].RollingBaseline is not double value)
                continue;
            baseline.Append(hasBaseline ? " L" : "M");
            baseline.Append(Inv(X(i))).Append(' ').Append(Inv(Y(value)));
            hasBaseline = true;
        }

        var svg = new StringBuilder();
        svg.Append($"<svg class=\"affect-chart\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-hidden=\"true\" focusable=\"false\">");
        svg.Append($"<line class=\"affect-grid\" x1=\"{Inv(padX)}\" y1=\"{Inv(Y(10))}\" x2=\"{Inv(width - padX)}\" y2=\"{Inv(Y(10))}\" />");
        svg.Append($"<line class=\"affect-grid\" x1=\"{Inv(padX)}\" y1=\"{Inv(Y(5))}\" x2=\"{Inv(width - padX)}\" y2=\"{Inv(Y(5))}\" />");
        svg.Append($"<line class=\"affect-grid\" x1=\"{Inv(padX)}\" y1=\"{Inv(Y(1))}\" x2=\"{Inv(width - padX)}\" y2=\"{Inv(Y(1))}\" />");
        svg.Append($"<text class=\"affect-axis\" x=\"8\" y=\"{Inv(Y(10) + 4)}\">10</text>");
        svg.Append($"<text class=\"affect-axis\" x=\"8\" y=\"{Inv(Y(5) + 4)}\">5</text>");
        svg.Append($"<text class=\"affect-axis\" x=\"8\" y=\"{Inv(Y(1) + 4)}\">1</text>");
        if (hasBaseline)
            svg.Append($"<path class=\"affect-baseline\" d=\"{baseline}\" fill=\"none\" />");
        svg.Append($"<path class=\"affect-line\" d=\"{line}\" fill=\"none\" />");
        for (var i = 0; i < points.Count; i++)
        {
            var markerClass = points[i].DeviatesBelowSelf ? "affect-point is-deviation" : "affect-point";
            svg.Append($"<circle class=\"{markerClass}\" cx=\"{Inv(X(i))}\" cy=\"{Inv(Y(points[i].Score))}\" r=\"5\" />");
        }
        svg.Append("</svg>");
        return svg.ToString();
    }

    private static string Inv(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
