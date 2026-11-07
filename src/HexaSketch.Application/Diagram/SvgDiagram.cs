using System.Globalization;
using System.Xml.Linq;
using HexaSketch.Domain;

namespace HexaSketch.Application.Diagram;

/// <summary>Draws a <see cref="DiagramView"/> as a standalone SVG, cropped to the content.</summary>
public static class SvgDiagram
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    // Ink and slate, matching Context Sketch; the User area stays red so it is the one thing that stands out.
    public const string Ink = "#0F172A";
    public const string HexagonFill = "#F8FAFC";
    public const string CoreFill = "#ffffff";
    public const string UserFill = "#fdecea";
    public const string UserInk = "#b03a2e";
    public const string PortInk = "#334155";
    public const string RealFill = "#64748B";
    public const string RealStroke = "#475569";
    public const string RealText = "#FFFFFF";
    public const string RealCaption = "#E2E8F0";
    public const string StandInInk = "#64748B";
    public const string Muted = "#94A3B8";
    public const string Font = "Segoe UI, Arial, sans-serif";

    public static string Render(DiagramView view)
    {
        var b = view.Bounds;
        var root = new XElement(Svg + "svg",
            new XAttribute("width", N(b.Width)),
            new XAttribute("height", N(b.Height)),
            new XAttribute("viewBox", $"{N(b.X)} {N(b.Y)} {N(b.Width)} {N(b.Height)}"),
            new XAttribute("font-family", Font),
            new XElement(Svg + "rect", new XAttribute("x", N(b.X)), new XAttribute("y", N(b.Y)),
                new XAttribute("width", N(b.Width)), new XAttribute("height", N(b.Height)), new XAttribute("fill", "#ffffff")),
            Line(view.CentreLine, Muted, 1, "6 6"),
            Hexagon(view.Hexagon),
            view.Ports.Select(Port),
            Text(new DiagramPoint(b.X + 20, b.Y + 30), view.Title, 18, Ink, "start", bold: true));

        return new XDeclaration("1.0", "utf-8", null) + Environment.NewLine + root + Environment.NewLine;
    }

    private static IEnumerable<XElement> Hexagon(HexagonShape h)
    {
        yield return Polygon(h.Corners, HexagonFill, Ink, 1.5);
        yield return Polygon(h.TasksRegion, UserFill, UserInk, 1);
        yield return Polygon(h.QueriesRegion, UserFill, UserInk, 1);
        yield return Line(h.TasksQueriesDivider, UserInk, 1, "4 3");
        yield return Text(h.TasksLabel, "Tasks", 13, UserInk);
        yield return Text(h.QueriesLabel, "Queries", 13, UserInk);
        yield return new XElement(Svg + "circle",
            new XAttribute("cx", N(h.DomainCentre.X)), new XAttribute("cy", N(h.DomainCentre.Y)),
            new XAttribute("r", N(h.DomainRadius)),
            new XAttribute("fill", CoreFill), new XAttribute("stroke", Ink), new XAttribute("stroke-width", "1.5"));
        yield return Text(h.DomainCentre, "Domain", 16, Ink, bold: true);
    }

    private static IEnumerable<XElement> Port(PortShape port)
    {
        var weight = port.IsUser ? 4 : 2;
        yield return Line(port.Stalk, Ink, weight);
        foreach (var adapter in port.Adapters)
        {
            yield return Line(adapter.Link, Ink, 1);
            foreach (var e in Adapter(adapter)) yield return e;
        }
        yield return new XElement(Svg + "circle",
            new XAttribute("cx", N(port.Centre.X)), new XAttribute("cy", N(port.Centre.Y)), new XAttribute("r", N(port.Radius)),
            new XAttribute("fill", "#ffffff"), new XAttribute("stroke", PortInk), new XAttribute("stroke-width", N(weight)));
        var anchor = port.LabelAlign switch { LabelAlign.Start => "start", LabelAlign.End => "end", _ => "middle" };
        yield return Text(port.Label, port.Name, 14, PortInk, anchor, bold: true);
    }

    private static IEnumerable<XElement> Adapter(AdapterShape a)
    {
        var real = a.Kind == AdapterKind.Real;
        var rect = new XElement(Svg + "rect",
            new XAttribute("x", N(a.Box.X)), new XAttribute("y", N(a.Box.Y)),
            new XAttribute("width", N(a.Box.Width)), new XAttribute("height", N(a.Box.Height)),
            new XAttribute("rx", "4"),
            new XAttribute("fill", real ? RealFill : "#ffffff"),
            new XAttribute("stroke", real ? RealStroke : StandInInk),
            new XAttribute("stroke-width", "1.2"));
        if (!real) rect.Add(new XAttribute("stroke-dasharray", "5 3"));
        yield return rect;

        var c = a.Box.Centre;
        if (a.Caption is null)
        {
            yield return Text(c, Fit(a.Name, a.Box.Width, 13), 13, real ? RealText : Ink, bold: true);
        }
        else
        {
            yield return Text(c with { Y = c.Y - 8 }, Fit(a.Name, a.Box.Width, 13), 13, real ? RealText : Ink, bold: true);
            yield return Text(c with { Y = c.Y + 9 }, Fit(a.Caption, a.Box.Width, 11), 11, real ? RealCaption : StandInInk);
        }
    }

    private static string Fit(string text, double width, double fontSize)
    {
        var max = (int)((width - 12) / (fontSize * 0.56));
        return text.Length <= max ? text : text[..Math.Max(1, max - 1)] + "…";
    }

    private static XElement Polygon(IEnumerable<DiagramPoint> points, string fill, string stroke, double width) =>
        new(Svg + "polygon",
            new XAttribute("points", string.Join(" ", points.Select(p => $"{N(p.X)},{N(p.Y)}"))),
            new XAttribute("fill", fill), new XAttribute("stroke", stroke), new XAttribute("stroke-width", N(width)),
            new XAttribute("stroke-linejoin", "round"));

    private static XElement Line(DiagramLine line, string stroke, double width, string? dash = null)
    {
        var e = new XElement(Svg + "line",
            new XAttribute("x1", N(line.From.X)), new XAttribute("y1", N(line.From.Y)),
            new XAttribute("x2", N(line.To.X)), new XAttribute("y2", N(line.To.Y)),
            new XAttribute("stroke", stroke), new XAttribute("stroke-width", N(width)),
            new XAttribute("stroke-linecap", "round"));
        if (dash is not null) e.Add(new XAttribute("stroke-dasharray", dash));
        return e;
    }

    private static XElement Text(DiagramPoint at, string text, double size, string fill, string anchor = "middle", bool bold = false)
    {
        var e = new XElement(Svg + "text", text,
            new XAttribute("x", N(at.X)), new XAttribute("y", N(at.Y)),
            new XAttribute("font-size", N(size)), new XAttribute("fill", fill),
            new XAttribute("text-anchor", anchor), new XAttribute("dominant-baseline", "central"));
        if (bold) e.Add(new XAttribute("font-weight", "600"));
        return e;
    }

    private static string N(double value) => Math.Round(value, 1).ToString(CultureInfo.InvariantCulture);
}
