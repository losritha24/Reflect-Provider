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
                .Append("<text x=\"").Append(Inv((from.X + to.X) / 2)).Append("\" y=\"")
                .Append(Inv((from.Y + to.Y) / 2 - 8)).Append("\">")
                .Append(Encode(edge.Label)).Append("</text></g>");
        }

        foreach (var node in nodes)
        {
            var radius = node.Kind switch
            {
                "hub" => 46,
                "patient" => 34,
                "device" => 20,
                _ => 28
            };
            var inner = node.Href.Length == 0
                ? $"<g class=\"kg-node kg-{Encode(node.Kind)}{(node.Alert ? " is-alert" : "")}\">"
                : $"<a class=\"kg-node kg-{Encode(node.Kind)}{(node.Alert ? " is-alert" : "")}\" href=\"{Encode(node.Href)}\">";
            xml.Append(inner)
                .Append("<circle cx=\"").Append(Inv(node.X)).Append("\" cy=\"").Append(Inv(node.Y))
                .Append("\" r=\"").Append(Inv(radius)).Append("\" />")
                .Append("<text class=\"kg-label\" x=\"").Append(Inv(node.X)).Append("\" y=\"")
                .Append(Inv(node.Y + radius + 16)).Append("\">").Append(Encode(node.Label)).Append("</text>")
                .Append("<text class=\"kg-sub\" x=\"").Append(Inv(node.X)).Append("\" y=\"")
                .Append(Inv(node.Y + radius + 32)).Append("\">").Append(Encode(node.Subtitle)).Append("</text>")
                .Append(node.Href.Length == 0 ? "</g>" : "</a>");
        }

        xml.Append("</svg>");
        return xml.ToString();
    }

    private static string Inv(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
