using HexaSketch.Domain;

namespace HexaSketch.Application.Plan;

/// <summary>
/// Core is Domain and UseCases; App is a runnable User port adapter at the top level; Adapters is one
/// project per port under Adapters/; Fakes and Specs sit under Checks/.
/// </summary>
public enum ProjectRole { Domain, UseCases, App, Adapters, Fakes, Specs }

/// <summary>A project. <see cref="Folder"/> is relative to the solution; its first part, if any, is the solution folder.</summary>
public sealed record PlannedProject(
    string Name,
    string Folder,
    string Template,
    string Namespace,
    ProjectRole Role,
    IReadOnlyList<string> References,
    IReadOnlyList<string> Packages)
{
    public string? SolutionFolder => Folder.Contains('/') ? Folder[..Folder.IndexOf('/')] : null;
}

/// <summary>A port. <see cref="Folder"/> is where its interface files go in UseCases ("" for the top level).</summary>
public sealed record PlannedPort(
    string Name,
    string? Description,
    Side Side,
    bool IsUser,
    IReadOnlyList<string> Interfaces,
    string Folder,
    IReadOnlyList<PlannedAdapter> Adapters);

/// <summary>
/// An adapter. <see cref="Class"/> is null for driving adapters, which keep their template's files.
/// <see cref="Folder"/> is where its class file goes inside its project ("" for the top level).
/// </summary>
public sealed record PlannedAdapter(
    string Name,
    AdapterKind Kind,
    string? Technology,
    string? Description,
    string Project,
    string? Class,
    string Folder,
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
    IReadOnlyList<string> Problems)
{
    public PlannedProject Project(ProjectRole role) => Projects.First(p => p.Role == role);
}
