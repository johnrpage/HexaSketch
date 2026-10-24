using HexaSketch.Domain;

namespace HexaSketch.Application.Plan;

public enum ProjectRole { Domain, Application, DrivingAdapter, DrivenAdapter, Specs }

public sealed record PlannedProject(
    string Name,
    string Folder,
    string Template,
    ProjectRole Role,
    IReadOnlyList<string> References,
    IReadOnlyList<string> Packages);

public sealed record PlannedPort(
    string Name,
    string? Description,
    Side Side,
    bool IsUser,
    IReadOnlyList<string> Interfaces,
    IReadOnlyList<PlannedAdapter> Adapters);

public sealed record PlannedAdapter(
    string Name,
    AdapterKind Kind,
    string? Technology,
    string? Description,
    string Project,
    string? Class,
    string Source,
    bool AutoAdded);

/// <summary>Everything the plan says, worked out from a sketch, before it is written as Markdown.</summary>
public sealed record SolutionPlan(
    string Solution,
    string SystemName,
    string? SystemDescription,
    string? SystemTechnology,
    IReadOnlyList<PlannedProject> Projects,
    IReadOnlyList<PlannedPort> Ports,
    IReadOnlyList<ContextElement> NotPlaced,
    IReadOnlyList<string> Problems);
