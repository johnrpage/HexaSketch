using System.Text.RegularExpressions;
using HexaSketch.Domain;

namespace HexaSketch.Application.Plan;

/// <summary>
/// The opinions, hard-coded: the core (Domain and UseCases) shares the solution's namespace with the
/// port interfaces; each port's real and simulated adapters share one project under Adapters/; the
/// User port's one runnable adapter is the App; fakes share one project, and every driving port's
/// fakes are the Specs; every port gets a fake.
/// </summary>
public static partial class SolutionPlanner
{
    public const string TargetFramework = "net10.0";

    /// <summary>The xunit template's packages (coverlet.collector included) plus the assertion library.</summary>
    public static readonly IReadOnlyList<string> SpecPackages =
        ["Microsoft.NET.Test.Sdk", "xunit", "xunit.runner.visualstudio", "coverlet.collector", "AwesomeAssertions"];

    /// <summary>
    /// Ports can't take these names: each is a project or a namespace beside the port interfaces, and
    /// "Application" would hide the UI framework's Application class from the App.
    /// </summary>
    public static readonly IReadOnlyList<string> ReservedNames =
        ["Domain", "UseCases", "Application", "App", "Adapters", "Fakes", "Specs"];

    public static SolutionPlan Plan(Sketch sketch)
    {
        var problems = new List<string>();
        var s = CodeName.From(sketch.System.Name);
        if (!CodeName.IsValid(s))
            problems.Add($"The system name '{sketch.System.Name}' doesn't make a usable solution name.");

        var ports = sketch.Ports.OrderBy(p => p.Side).ToList();
        var runnable = ports.Where(p => p.IsUser).SelectMany(p => p.Adapters).Count(a => a.Kind != AdapterKind.Fake);
        var planned = ports.ToDictionary(p => p, p => AdaptersFor(sketch, p, s, runnable, problems));

        var domain = new PlannedProject($"{s}.Domain", $"{s}/{s}.Domain", "classlib", s, ProjectRole.Domain, [], []);
        var useCases = new PlannedProject($"{s}.UseCases", $"{s}/{s}.UseCases", "classlib", s, ProjectRole.UseCases, [domain.Name], []);
        var projects = new List<PlannedProject> { domain, useCases };

        foreach (var (port, adapters) in planned)
        {
            var plugged = port.Adapters.Zip(adapters).Where(x => x.First.Kind != AdapterKind.Fake).ToList();
            if (port.IsUser)
            {
                projects.AddRange(plugged.Select(x => new PlannedProject(
                    x.Second.Project, x.Second.Project, TemplateFor(Side.Driving, x.First.Technology),
                    x.Second.Project, ProjectRole.App, [useCases.Name], [])));
            }
            else if (plugged.Count > 0)
            {
                var templates = plugged.Select(x => TemplateFor(port.Side, x.First.Technology)).Distinct().ToList();
                if (templates.Count > 1)
                    problems.Add($"The adapters on port '{port.Name}' share a project but need different project templates ({string.Join(", ", templates)}).");
                var name = plugged[0].Second.Project;
                projects.Add(new(name, $"Adapters/{name}", templates[0], $"{s}.Adapters", ProjectRole.Adapters, [useCases.Name], []));
            }
        }

        var specsReferences = new List<string> { useCases.Name };
        if (ports.Any(p => p.Side == Side.Driven))
        {
            projects.Add(new($"{s}.Fakes", $"Checks/{s}.Fakes", "classlib", $"{s}.Fakes", ProjectRole.Fakes, [useCases.Name], []));
            specsReferences.Add($"{s}.Fakes");
        }
        projects.Add(new($"{s}.Specs", $"Checks/{s}.Specs", "xunit", $"{s}.Specs", ProjectRole.Specs, specsReferences, SpecPackages));

        var appNames = projects.Where(p => p.Role == ProjectRole.App && runnable > 1).Select(p => p.Name[(s.Length + 1)..]);
        CheckNames(s, ports, planned, [.. ReservedNames, .. appNames, s], problems);
        foreach (var clash in projects.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            problems.Add($"More than one project would be called '{clash.Key}'. Rename a port or adapter.");

        var plannedPorts = ports.Select(p => new PlannedPort(
            p.Name, p.Description, p.Side, p.IsUser, InterfacesFor(p), p.IsUser ? "" : "Ports", planned[p])).ToList();

        return new SolutionPlan(
            s, sketch.System.Name, sketch.System.Description, sketch.System.Technology,
            projects, plannedPorts, sketch.UnplacedElements, problems);
    }

    /// <summary>The User port is two interfaces (tasks and queries); every other port is one.</summary>
    public static IReadOnlyList<string> InterfacesFor(Port port) =>
        port.IsUser ? ["UserTasks", "UserQueries"] : [CodeName.From(port.Name)];

    /// <summary>
    /// Implementations carry the meaningful extra, taken from the adapter's name ("Azure DevOps" on port
    /// "Tracker" is AzureDevOpsTracker). When the name only repeats the port's, or is empty, the
    /// technology stands in ("File System (JSON)" on port "Sketches" is FileSystemSketches), and
    /// failing that the kind (FakeSketches). Null when a real adapter has neither.
    /// </summary>
    public static string? ClassFor(string portCode, string adapterCode, string? technology, AdapterKind kind)
    {
        var tech = CodeName.From(Brackets().Replace(technology ?? "", " "));
        var prefix =
            CodeName.IsValid(adapterCode) && !adapterCode.Equals(portCode, StringComparison.OrdinalIgnoreCase) ? adapterCode
            : CodeName.IsValid(tech) ? tech
            : kind != AdapterKind.Real ? kind.ToString()
            : null;
        return prefix is null ? null
            : prefix.EndsWith(portCode, StringComparison.Ordinal) ? prefix
            : prefix + portCode;
    }

    private static List<PlannedAdapter> AdaptersFor(Sketch sketch, Port port, string s, int runnable, List<string> problems)
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
            var cls = port.Side == Side.Driven ? ClassFor(portCode, code, adapter.Technology, adapter.Kind) : null;
            if (port.Side == Side.Driven && cls is null)
                problems.Add($"The adapter '{adapter.Name}' on port '{port.Name}' needs a name of its own or a technology, to name its class.");

            var (project, folder) = adapter.Kind switch
            {
                AdapterKind.Fake when port.Side == Side.Driving => ($"{s}.Specs", ""),
                AdapterKind.Fake => ($"{s}.Fakes", portCode),
                _ when port.IsUser => (runnable == 1 ? $"{s}.App" : $"{s}.{code}", ""),
                _ => ($"{s}.{portCode}", ""),
            };
            result.Add(new PlannedAdapter(
                adapter.Name, adapter.Kind, adapter.Technology, adapter.Description,
                project, cls, folder, source, AutoAdded: false));
        }

        if (!port.Adapters.Any(a => a.Kind == AdapterKind.Fake))
        {
            var driving = port.Side == Side.Driving;
            result.Add(new PlannedAdapter(
                driving ? "Specs" : "Fake", AdapterKind.Fake, null,
                driving
                    ? $"Specs that drive the application through the {port.Name} port, using the fake driven adapters."
                    : $"In-memory stand-in for the {port.Name} port, for fast specs.",
                driving ? $"{s}.Specs" : $"{s}.Fakes",
                driving ? null : ClassFor(portCode, "Fake", null, AdapterKind.Fake),
                driving ? "" : portCode,
                "Added by the plan: every port gets a fake", AutoAdded: true));
        }
        return result;
    }

    private static void CheckNames(
        string s, List<Port> ports, Dictionary<Port, List<PlannedAdapter>> planned,
        IReadOnlyList<string> reserved, List<string> problems)
    {
        foreach (var port in ports.Where(p => !CodeName.IsValid(CodeName.From(p.Name))))
            problems.Add($"The port name '{port.Name}' doesn't make a usable code name.");

        var interfaces = ports.SelectMany(InterfacesFor).Where(n => n.Length > 0).Append($"{s}Application").ToList();
        foreach (var clash in interfaces.GroupBy(n => n, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            problems.Add($"More than one port would make an interface called '{clash.Key}'.");

        foreach (var port in ports.Where(p => !p.IsUser && reserved.Contains(CodeName.From(p.Name), StringComparer.OrdinalIgnoreCase)))
            problems.Add($"A port can't be called '{port.Name}': that name is taken by a project or namespace in the plan.");

        var classes = planned.Values.SelectMany(a => a).Where(a => a.Class is not null).ToList();
        foreach (var clash in classes.GroupBy(a => (a.Project == $"{s}.Fakes", a.Class), (key, g) => (key.Class, Count: g.Count()))
                     .Where(g => g.Count > 1))
            problems.Add($"More than one adapter would make a class called '{clash.Class}'. Rename an adapter.");

        foreach (var adapter in classes.Where(a => interfaces.Contains(a.Class!, StringComparer.OrdinalIgnoreCase)))
            problems.Add($"The adapter '{adapter.Name}' would make a class called '{adapter.Class}', the same as a port interface. Rename it or give it a technology.");
    }

    private static string TemplateFor(Side side, string? technology)
    {
        var tech = technology?.ToLowerInvariant() ?? "";
        if (side == Side.Driving && tech.Contains("wpf")) return "wpf";
        if (side == Side.Driving && (tech.Contains("cli") || tech.Contains("console"))) return "console";
        if (side == Side.Driving && (tech.Contains("web") || tech.Contains("http"))) return "web";
        return "classlib";
    }

    [GeneratedRegex(@"\([^)]*\)")]
    private static partial Regex Brackets();
}
