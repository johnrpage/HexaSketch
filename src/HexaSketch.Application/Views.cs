using HexaSketch.Domain;

namespace HexaSketch.Application;

public sealed record SketchView(
    string Title,
    string SystemName,
    string? SystemDescription,
    string? SystemTechnology,
    IReadOnlyList<PortView> Ports,
    IReadOnlyList<UnplacedItemView> Unplaced);

public sealed record PortView(
    string Id,
    string Name,
    string? Description,
    Side Side,
    bool IsUser,
    string CodeName,
    IReadOnlyList<AdapterView> Adapters);

public sealed record AdapterView(
    string Id,
    string PortId,
    string Name,
    string? Technology,
    string? Description,
    AdapterKind Kind,
    string CodeName,
    SourceView? Source);

/// <summary>The context diagram element an adapter came from.</summary>
public sealed record SourceView(string ElementId, string Name, ElementType Type);

/// <summary>A context diagram element still waiting in the list.</summary>
public sealed record UnplacedItemView(
    string ElementId, string Name, ElementType Type, string? Technology, string? Description);

public sealed record ExportCheck(IReadOnlyList<string> Unplaced, IReadOnlyList<string> PlanProblems);
