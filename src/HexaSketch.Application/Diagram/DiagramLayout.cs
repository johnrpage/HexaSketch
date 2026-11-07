using HexaSketch.Domain;

namespace HexaSketch.Application.Diagram;

/// <summary>
/// Lays a sketch out: a pointy-topped hexagon at the origin, ports placed radially around it on
/// stalks (driving on the left with the User port at nine o'clock, driven on the right), and each
/// port's adapters in a column beside it. The same sketch always gives the same layout.
/// </summary>
public static class DiagramLayout
{
    public const double HexagonRadius = 170;
    public const double DomainRadius = 72;
    public const double PortRadius = 20;
    public const double UserPortRadius = 24;
    public const double StalkLength = 55;
    public const double AdapterHeight = 50;
    public const double AdapterGap = 10;
    public const double MaxSpread = 75;
    public const double PortSpacing = 50;

    private const double TasksFrom = 130, Divider = 180, QueriesTo = 230;
    private static readonly double Apothem = HexagonRadius * Math.Cos(Radians(30));

    public static DiagramView Of(Sketch sketch)
    {
        var hexagon = Hexagon();
        var ports = new List<PortShape>();
        foreach (var (port, angle) in Angles(sketch))
            ports.Add(PortAt(port, angle));
        foreach (var side in new[] { Side.Driving, Side.Driven })
            SeparateColumns(ports, side);

        var content = Bound(hexagon.Corners
            .Concat(ports.SelectMany(PortExtent)));
        var titleSpace = 40;
        var bounds = new DiagramRect(content.X - 20, content.Y - titleSpace - 20, content.Width + 40, content.Height + titleSpace + 40);
        var centreLine = new DiagramLine(new(0, bounds.Y), new(0, bounds.Bottom));

        return new DiagramView(sketch.System.Name, hexagon, centreLine, ports, bounds);
    }

    /// <summary>Estimated width of a line of text at the diagram's font size, used for boxes and bounds.</summary>
    public static double TextWidth(string text, double fontSize) => text.Length * fontSize * 0.56;

    public static double AdapterWidth(string name, string? caption) =>
        Math.Clamp(Math.Max(TextWidth(name, 13), TextWidth(caption ?? "", 11)) + 32, 100, 230);

    public static string? CaptionFor(Adapter adapter) => adapter.Kind switch
    {
        AdapterKind.Real => adapter.Technology,
        _ when adapter.Technology is null => adapter.Kind.ToString(),
        _ => $"{adapter.Kind} · {adapter.Technology}",
    };

    private static HexagonShape Hexagon()
    {
        var corners = Enumerable.Range(0, 6).Select(i => Polar(90 + 60 * i, HexagonRadius)).ToList();
        var tasks = Region(TasksFrom, Divider);
        var queries = Region(Divider, QueriesTo);
        var middle = (DomainRadius + Apothem) / 2;
        return new HexagonShape(
            corners,
            new(0, 0),
            DomainRadius,
            tasks,
            queries,
            Polar((TasksFrom + Divider) / 2 + 3, middle),
            Polar((Divider + QueriesTo) / 2 - 3, middle),
            new DiagramLine(Polar(Divider, DomainRadius), Polar(Divider, EdgeDistance(Divider))));
    }

    /// <summary>The ring segment between the Domain circle and the hexagon's edge, between two angles.</summary>
    private static List<DiagramPoint> Region(double from, double to)
    {
        var points = new List<DiagramPoint> { Polar(from, EdgeDistance(from)) };
        for (var corner = 30.0; corner < 390; corner += 60)
            if (corner > from && corner < to) points.Add(Polar(corner, HexagonRadius));
        points.Add(Polar(to, EdgeDistance(to)));
        for (var a = to; a >= from; a -= 5)
            points.Add(Polar(a, DomainRadius));
        return points;
    }

    private static IEnumerable<(Port Port, double Angle)> Angles(Sketch sketch)
    {
        // Driving: the User port at nine o'clock, any others alternating above and below it.
        yield return (sketch.UserPort, 180);
        var others = sketch.PortsOn(Side.Driving).Where(p => !p.IsUser).ToList();
        var above = (others.Count + 1) / 2;
        var drivingStep = above == 0 ? 0 : Math.Min(PortSpacing, MaxSpread / above);
        for (var j = 0; j < others.Count; j++)
        {
            var k = j / 2 + 1;
            yield return (others[j], j % 2 == 0 ? 180 - k * drivingStep : 180 + k * drivingStep);
        }

        // Driven: spread evenly across the right, top to bottom, centred on three o'clock.
        var driven = sketch.PortsOn(Side.Driven).ToList();
        var step = driven.Count > 1 ? Math.Min(PortSpacing, 2 * MaxSpread / (driven.Count - 1)) : 0;
        for (var i = 0; i < driven.Count; i++)
            yield return (driven[i], step * (driven.Count - 1) / 2 - i * step);
    }

    private static PortShape PortAt(Port port, double angle)
    {
        var radius = port.IsUser ? UserPortRadius : PortRadius;
        var edge = EdgeDistance(angle);
        var centre = Polar(angle, edge + StalkLength + radius);
        var stalk = new DiagramLine(Polar(angle, edge), Polar(angle, edge + StalkLength));

        var right = port.Side == Side.Driven;
        var outward = Polar(angle, radius + 30);
        var anchor = new DiagramPoint(centre.X + outward.X, centre.Y + outward.Y);
        var count = port.Adapters.Count;
        var columnHeight = count * AdapterHeight + Math.Max(0, count - 1) * AdapterGap;

        var adapters = new List<AdapterShape>();
        for (var i = 0; i < count; i++)
        {
            var adapter = port.Adapters[i];
            var caption = CaptionFor(adapter);
            var width = AdapterWidth(adapter.Name, caption);
            var y = anchor.Y - columnHeight / 2 + i * (AdapterHeight + AdapterGap);
            var x = right ? anchor.X + 10 : anchor.X - 10 - width;
            var box = new DiagramRect(x, y, width, AdapterHeight);

            var target = new DiagramPoint(right ? box.X : box.Right, box.Centre.Y);
            adapters.Add(new AdapterShape(adapter.Id, adapter.Name, caption, adapter.Kind, box,
                new DiagramLine(TowardsEdge(centre, target, radius), target)));
        }

        var (label, align) = LabelFor(angle, centre, radius, right);
        return new PortShape(port.Id, port.Name, port.Side, port.IsUser, centre, radius, stalk, label, align, adapters);
    }

    /// <summary>
    /// Ports near three or nine o'clock have their name centred above them. Diagonal ports have it on
    /// the side away from both the stalk (towards the hexagon) and the adapters (outwards): above for
    /// upper ports, below for lower ones, and reaching back towards the centre line.
    /// </summary>
    private static (DiagramPoint, LabelAlign) LabelFor(double angle, DiagramPoint centre, double radius, bool right)
    {
        var tilt = right ? angle : 180 - angle;
        const double gap = 16;
        if (Math.Abs(tilt) <= 10)
            return (new(centre.X, centre.Y - radius - gap), LabelAlign.Middle);
        var y = tilt > 0 ? centre.Y - radius - gap : centre.Y + radius + gap;
        return (new(centre.X, y), right ? LabelAlign.End : LabelAlign.Start);
    }

    /// <summary>
    /// Busy neighbouring ports can have overlapping adapter columns. Working outwards from the port
    /// nearest the middle of the side, push each column up or down until it clears its neighbour and
    /// that neighbour's label.
    /// </summary>
    private static void SeparateColumns(List<PortShape> ports, Side side)
    {
        const double gap = 16;
        var onSide = ports.Where(p => p.Side == side).OrderBy(p => p.Centre.Y).ToList();
        if (onSide.Count < 2) return;
        var pivot = onSide.IndexOf(onSide.MinBy(p => Math.Abs(p.Centre.Y))!);

        for (var i = pivot - 1; i >= 0; i--)
        {
            var limit = Top(onSide[i + 1]) - gap;
            var bottom = Bottom(onSide[i]);
            if (bottom > limit) onSide[i] = Shift(onSide[i], limit - bottom);
        }
        for (var i = pivot + 1; i < onSide.Count; i++)
        {
            var limit = Bottom(onSide[i - 1]) + gap;
            var top = Top(onSide[i]);
            if (top < limit) onSide[i] = Shift(onSide[i], limit - top);
        }
        foreach (var shifted in onSide)
            ports[ports.FindIndex(p => p.Id == shifted.Id)] = shifted;

        static double Top(PortShape p) =>
            Math.Min(p.Adapters.Count > 0 ? p.Adapters[0].Box.Y : double.MaxValue, p.Centre.Y - p.Radius - 26);

        static double Bottom(PortShape p) =>
            Math.Max(p.Adapters.Count > 0 ? p.Adapters[^1].Box.Bottom : double.MinValue, p.Centre.Y + p.Radius + 26);
    }

    /// <summary>Moves a port's adapter column up or down, re-aiming the links from the port circle.</summary>
    private static PortShape Shift(PortShape port, double dy) => port with
    {
        Adapters = port.Adapters.Select(a =>
        {
            var box = a.Box with { Y = a.Box.Y + dy };
            var target = a.Link.To with { Y = a.Link.To.Y + dy };
            return a with { Box = box, Link = new DiagramLine(TowardsEdge(port.Centre, target, port.Radius), target) };
        }).ToList(),
    };

    private static IEnumerable<DiagramPoint> PortExtent(PortShape port)
    {
        var r = port.Radius;
        yield return new(port.Centre.X - r, port.Centre.Y - r);
        yield return new(port.Centre.X + r, port.Centre.Y + r);
        var width = TextWidth(port.Name, 14);
        var left = port.LabelAlign switch
        {
            LabelAlign.Start => port.Label.X,
            LabelAlign.End => port.Label.X - width,
            _ => port.Label.X - width / 2,
        };
        yield return new(left, port.Label.Y - 10);
        yield return new(left + width, port.Label.Y + 10);
        foreach (var a in port.Adapters)
        {
            yield return new(a.Box.X, a.Box.Y);
            yield return new(a.Box.Right, a.Box.Bottom);
        }
    }

    private static DiagramRect Bound(IEnumerable<DiagramPoint> points)
    {
        var list = points.ToList();
        var minX = list.Min(p => p.X);
        var minY = list.Min(p => p.Y);
        return new DiagramRect(minX, minY, list.Max(p => p.X) - minX, list.Max(p => p.Y) - minY);
    }

    /// <summary>How far from the centre the hexagon's edge is, in a given direction.</summary>
    private static double EdgeDistance(double angle)
    {
        var normal = Math.Round(angle / 60) * 60;
        return Apothem / Math.Cos(Radians(angle - normal));
    }

    private static DiagramPoint TowardsEdge(DiagramPoint centre, DiagramPoint target, double radius)
    {
        var dx = target.X - centre.X;
        var dy = target.Y - centre.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        return length == 0 ? centre : new(centre.X + dx / length * radius, centre.Y + dy / length * radius);
    }

    /// <summary>Angle in degrees, anticlockwise from three o'clock; screen y points down.</summary>
    private static DiagramPoint Polar(double angle, double distance) =>
        new(distance * Math.Cos(Radians(angle)), -distance * Math.Sin(Radians(angle)));

    private static double Radians(double degrees) => degrees * Math.PI / 180;
}
