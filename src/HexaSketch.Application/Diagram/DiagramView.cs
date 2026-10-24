using HexaSketch.Domain;

namespace HexaSketch.Application.Diagram;

/// <summary>A point in diagram space: the hexagon's centre is (0, 0) and y grows downwards.</summary>
public readonly record struct DiagramPoint(double X, double Y);

public readonly record struct DiagramRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public DiagramPoint Centre => new(X + Width / 2, Y + Height / 2);
}

public sealed record DiagramLine(DiagramPoint From, DiagramPoint To);

/// <summary>The hexagon: Domain in the middle, Tasks and Queries between it and the User port.</summary>
public sealed record HexagonShape(
    IReadOnlyList<DiagramPoint> Corners,
    DiagramPoint DomainCentre,
    double DomainRadius,
    IReadOnlyList<DiagramPoint> TasksRegion,
    IReadOnlyList<DiagramPoint> QueriesRegion,
    DiagramPoint TasksLabel,
    DiagramPoint QueriesLabel,
    DiagramLine TasksQueriesDivider);

public sealed record AdapterShape(
    string Id,
    string Name,
    string? Caption,
    AdapterKind Kind,
    DiagramRect Box,
    DiagramLine Link);

/// <summary>Which end of the label text sits on the label point.</summary>
public enum LabelAlign { Start, Middle, End }

/// <summary>A port on its stalk. The label point is the vertical middle of the name text.</summary>
public sealed record PortShape(
    string Id,
    string Name,
    Side Side,
    bool IsUser,
    DiagramPoint Centre,
    double Radius,
    DiagramLine Stalk,
    DiagramPoint Label,
    LabelAlign LabelAlign,
    IReadOnlyList<AdapterShape> Adapters);

/// <summary>A sketch laid out for drawing. Screen and SVG both draw from this, so they always match.</summary>
public sealed record DiagramView(
    string Title,
    HexagonShape Hexagon,
    DiagramLine CentreLine,
    IReadOnlyList<PortShape> Ports,
    DiagramRect Bounds);
