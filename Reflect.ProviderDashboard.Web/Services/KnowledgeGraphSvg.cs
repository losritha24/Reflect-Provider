using System.Globalization;
using System.Net;
using System.Text;
using Reflect.ProviderDashboard.Web.Models.ViewModels.Provider.Graph;

namespace Reflect.ProviderDashboard.Web.Services;

public static class KnowledgeGraphSvg
{
    public static string From(
        IReadOnlyList<KnowledgeGraphViewModel.GraphNode> nodes,
        IReadOnlyList<KnowledgeGraphViewModel.GraphEdge> edges,
        double width,
        double height)
    {
        var lookup = nodes.ToDictionary(node => node.Id);
        var hub = nodes.First(node => node.Kind == "hub");
        var xml = new StringBuilder();
        xml.Append("<svg class=\"knowledge-graph-svg\" viewBox=\"0 0 ")
            .Append(Inv(width)).Append(' ').Append(Inv(height))
            .Append("\" role=\"img\" aria-labelledby=\"kg-title kg-desc\">")
            .Append("<title id=\"kg-title\">Caseload activity knowledge graph</title>")
            .Append("<desc id=\"kg-desc\">Patients connect to Reflect XR, Reflect Mobile, assigned devices, and follow-up signals. Coral edges mark a change worth reviewing.</desc>");

        foreach (var edge in edges)
        {
            if (!lookup.TryGetValue(edge.From, out var from) || !lookup.TryGetValue(edge.To, out var to))
                continue;
            xml.Append("<g class=\"kg-edge").Append(edge.Alert ? " is-alert" : "").Append("\">")
                .Append("<line x1=\"").Append(Inv(from.X)).Append("\" y1=\"").Append(Inv(from.Y))
                .Append("\" x2=\"").Append(Inv(to.X)).Append("\" y2=\"").Append(Inv(to.Y)).Append("\" />")
                .Append("</g>");
        }

        foreach (var node in nodes)
        {
            var radius = Radius(node.Kind);
            var inner = node.Href.Length == 0
                ? $"<g class=\"kg-node kg-{Encode(node.Kind)}{(node.Alert ? " is-alert" : "")}\">"
                : $"<a class=\"kg-node kg-{Encode(node.Kind)}{(node.Alert ? " is-alert" : "")}\" href=\"{Encode(node.Href)}\">";
            xml.Append(inner)
                .Append("<circle cx=\"").Append(Inv(node.X)).Append("\" cy=\"").Append(Inv(node.Y))
                .Append("\" r=\"").Append(Inv(radius)).Append("\" />");

            var caption = CaptionPoint(node, hub.X, hub.Y, radius);
            xml.Append("<text class=\"kg-label\" text-anchor=\"").Append(caption.Anchor)
                .Append("\" x=\"").Append(Inv(caption.LabelX)).Append("\" y=\"")
                .Append(Inv(caption.LabelY)).Append("\">").Append(Encode(node.Label)).Append("</text>");
            if (ShowSubtitle(node))
            {
                xml.Append("<text class=\"kg-sub\" text-anchor=\"").Append(caption.Anchor)
                    .Append("\" x=\"").Append(Inv(caption.LabelX)).Append("\" y=\"")
                    .Append(Inv(caption.SubY)).Append("\">").Append(Encode(node.Subtitle)).Append("</text>");
            }
            xml.Append(node.Href.Length == 0 ? "</g>" : "</a>");
        }

        xml.Append("</svg>");
        return xml.ToString();
    }

    private static bool ShowSubtitle(KnowledgeGraphViewModel.GraphNode node) =>
        node.Kind != "device" && !string.IsNullOrWhiteSpace(node.Subtitle);

    private static double Radius(string kind) => kind switch
    {
        "hub" => 62,
        "patient" => 34,
        "device" => 16,
        _ => 28
    };

    private static (double LabelX, double LabelY, double SubY, string Anchor) CaptionPoint(
        KnowledgeGraphViewModel.GraphNode node,
        double hubX,
        double hubY,
        double radius)
    {
        if (node.Kind == "hub")
            return (node.X, node.Y - 4, node.Y + 16, "middle");

        var dx = node.X - hubX;
        var dy = node.Y - hubY;
        var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        var ux = dx / length;
        var uy = dy / length;

        if (node.Kind is "platform" or "signal")
        {
            var y = node.Y + radius + 20;
            return node.X < hubX
                ? (node.X + radius + 10, y, y + 15, "start")
                : (node.X - radius - 10, y, y + 15, "end");
        }

        if (node.Kind == "device")
        {
            var gap = radius + 16;
            if (Math.Abs(ux) >= Math.Abs(uy))
            {
                var anchor = ux < 0 ? "end" : "start";
                var labelX = node.X + ux * gap;
                return (labelX, node.Y + 4, node.Y + 20, anchor);
            }

            var labelY = node.Y + uy * (radius + 18);
            return (node.X, labelY, labelY + 16, "middle");
        }

        if (Math.Abs(uy) >= Math.Abs(ux))
        {
            var y = node.Y - uy * (radius + 26);
            return (node.X, y, y + 16, "middle");
        }

        var stackedY = node.Y - radius - 40;
        return (node.X, stackedY, stackedY + 16, "middle");
    }

    private static string Inv(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
