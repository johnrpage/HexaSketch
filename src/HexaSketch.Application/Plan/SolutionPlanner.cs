using HexaSketch.Domain;

namespace HexaSketch.Application.Plan;

/// <summary>
/// The opinions, hard-coded for v1: one project per adapter, port interfaces in Application,
/// fakes on driving ports are xUnit spec projects, and every port gets a fake.
/// </summary>
public static class SolutionPlanner
{
    public const string TargetFramework = "net10.0";

    private static readonly string[] TestPackages =
        ["Microsoft.NET.Test.Sdk", "xunit", "xunit.runner.visualstudio", "Shouldly"];

    public static SolutionPlan Plan(Sketch sketch)
    {
        var problems = new List<string>();
        var solution = CodeName.From(sketch.System.Name);
        if (!CodeName.IsValid(solution))
            problems.Add($"The system name '{sketch.System.Name}' doesn't make a usable solution name.");

        var domain = $"{solution}.Domain";
        var application = $"{solution}.Application";

        var ports = sketch.Ports.OrderBy(p => p.Side).ToList();
        CheckPortNames(ports, problems);

        // Work out every adapter first: driving fakes (specs) need to know every driven fake.
        var planned = ports.ToDictionary(p => p, p => AdaptersFor(sketch, p, solution, problems));
        var drivenFakes = planned
            .Where(kv => kv.Key.Side == Side.Driven)
            .Select(kv => kv.Value.First(a => a.Kind == AdapterKind.Fake).Project)
            .ToList();

        var projects = new List<PlannedProject>
        {
            new(domain, $"src/{domain}", "classlib", ProjectRole.Domain, [], []),
            new(application, $"src/{application}", "classlib", ProjectRole.Application, [domain], []),
        };
        foreach (var (port, adapters) in planned)
        {
            foreach (var adapter in adapters)
            {
                var isSpecs = port.Side == Side.Driving && adapter.Kind == AdapterKind.Fake;
                projects.Add(isSpecs
                    ? new(adapter.Project, $"tests/{adapter.Project}", "xunit", ProjectRole.Specs,
                        [application, .. drivenFakes], TestPackages)
                    : new(adapter.Project, $"src/{adapter.Project}", TemplateFor(port.Side, adapter.Technology),
                        port.Side == Side.Driving ? ProjectRole.DrivingAdapter : ProjectRole.DrivenAdapter,
                        [application], []));
            }
        }

        foreach (var clash in projects.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            problems.Add($"More than one project would be called '{clash.Key}'. Rename a port or adapter.");

        var plannedPorts = ports.Select(p => new PlannedPort(
            p.Name, p.Description, p.Side, p.IsUser, InterfacesFor(p), planned[p])).ToList();

        return new SolutionPlan(
            solution, sketch.System.Name, sketch.System.Description, sketch.System.Technology,
            projects, plannedPorts, sketch.UnplacedElements, problems);
    }

    /// <summary>The User port is two interfaces (tasks and queries); every other port is one.</summary>
    public static IReadOnlyList<string> InterfacesFor(Port port) =>
        port.IsUser ? ["UserTasks", "UserQueries"] : [CodeName.From(port.Name)];

    /// <summary>
    /// Implementations carry the meaningful extra: adapter "File system" on port "Storage" is
    /// FileSystemStorage, and adapter "Fake tracker" on port "Tracker" is FakeTracker.
    /// </summary>
    public static string ClassFor(string portCode, string adapterCode) =>
        adapterCode.EndsWith(portCode, StringComparison.Ordinal) ? adapterCode : adapterCode + portCode;

    /// <summary>User port adapters are top-level (HexaSketch.Wpf); others sit under their port (HexaSketch.Storage.FileSystem).</summary>
    public static string ProjectFor(string solution, Port port, string adapterCode) =>
        port.IsUser ? $"{solution}.{adapterCode}" : $"{solution}.{CodeName.From(port.Name)}.{adapterCode}";

    private static List<PlannedAdapter> AdaptersFor(Sketch sketch, Port port, string solution, List<string> problems)
    {
        var portCode = CodeName.From(port.Name);
        var result = new List<PlannedAdapter>();

        foreach (var adapter in port.Adapters)
        {
            var code = CodeName.From(adapter.Name);
            if (!CodeName.IsValid(code))
                problems.Add($"The adapter name '{adapter.Name}' on port '{port.Name}' doesn't make a usable code name.");

            var source = adapter.SourceElementId is { } id && sketch.ContextDiagram.Find(id) is { } element
                ? $"Context diagram: {element.Name}"
                : "Added in the sketch";
            result.Add(new PlannedAdapter(
                adapter.Name, adapter.Kind, adapter.Technology, adapter.Description,
                ProjectFor(solution, port, code),
                port.Side == Side.Driven ? ClassFor(portCode, code) : null,
                source, AutoAdded: false));
        }

        foreach (var clash in port.Adapters.GroupBy(a => CodeName.From(a.Name), StringComparer.OrdinalIgnoreCase)
                     .Where(g => g.Key.Length > 0 && g.Count() > 1))
            problems.Add($"Port '{port.Name}' has more than one adapter called '{clash.Key}'.");

        if (!port.Adapters.Any(a => a.Kind == AdapterKind.Fake))
        {
            var name = port.Side == Side.Driving ? "Specs" : "Fake";
            result.Add(new PlannedAdapter(
                name, AdapterKind.Fake, null,
                port.Side == Side.Driving
                    ? $"Specs that drive the application through the {port.Name} port, using the fake driven adapters."
                    : $"In-memory stand-in for the {port.Name} port, for fast specs.",
                ProjectFor(solution, port, name),
                port.Side == Side.Driven ? ClassFor(portCode, name) : null,
                "Added by the plan: every port gets a fake", AutoAdded: true));
        }
        return result;
    }

    private static void CheckPortNames(List<Port> ports, List<string> problems)
    {
        foreach (var port in ports.Where(p => !CodeName.IsValid(CodeName.From(p.Name))))
            problems.Add($"The port name '{port.Name}' doesn't make a usable code name.");

        var interfaces = ports.SelectMany(InterfacesFor).Where(n => n.Length > 0);
        foreach (var clash in interfaces.GroupBy(n => n, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            problems.Add($"More than one port would make an interface called '{clash.Key}'.");

        foreach (var port in ports.Where(p => !p.IsUser && CodeName.From(p.Name) is "Domain" or "Application"))
            problems.Add($"A port can't be called '{port.Name}': that name is taken by a project.");
    }

    private static string TemplateFor(Side side, string? technology)
    {
        var tech = technology?.ToLowerInvariant() ?? "";
        if (side == Side.Driving && tech.Contains("wpf")) return "wpf";
        if (side == Side.Driving && (tech.Contains("cli") || tech.Contains("console"))) return "console";
        if (side == Side.Driving && (tech.Contains("web") || tech.Contains("http"))) return "web";
        return "classlib";
    }
}
