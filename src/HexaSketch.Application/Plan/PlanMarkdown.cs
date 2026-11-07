using System.Text;
using HexaSketch.Domain;

namespace HexaSketch.Application.Plan;

/// <summary>Writes a <see cref="SolutionPlan"/> as one Markdown file with fixed headings, for a coding agent.</summary>
public static class PlanMarkdown
{
    public static string Write(SolutionPlan plan, string sketchTitle)
    {
        var md = new StringBuilder();
        var s = plan.Solution;
        var useCases = plan.Project(ProjectRole.UseCases).Name;
        var projects = Ordered(plan).ToList();

        md.AppendLine($"# {plan.SystemName}: solution plan");
        md.AppendLine();
        md.AppendLine($"*Exported by HexaSketch from the sketch \"{sketchTitle}\". Stubs only: no functionality.*");
        md.AppendLine();

        md.AppendLine("## 1. Overview");
        md.AppendLine();
        md.AppendLine($"- **System**: {plan.SystemName}{Suffix(plan.SystemTechnology)}");
        if (!string.IsNullOrWhiteSpace(plan.SystemDescription))
            md.AppendLine($"- **Purpose**: {plan.SystemDescription}");
        md.AppendLine($"- **Solution**: `{s}.sln`, targeting `{SolutionPlanner.TargetFramework}` (`{SolutionPlanner.TargetFramework}-windows` for WPF projects)");
        md.AppendLine("- **Architecture**: ports and adapters. The application core (Domain and UseCases) knows nothing about the outside world; every external concern sits behind a port and is reached through an adapter.");
        md.AppendLine($"- **Ports**: {Count(plan.Ports, Side.Driving)} driving, {Count(plan.Ports, Side.Driven)} driven");
        md.AppendLine();

        md.AppendLine("## 2. Solution and project layout");
        md.AppendLine();
        md.AppendLine("Projects are grouped by role, and the solution folders in the `.sln` mirror the folders on disk. There are no `src/` or `tests/` folders.");
        md.AppendLine();
        md.AppendLine("```");
        md.AppendLine($"{s}.sln");
        string? folder = null;
        foreach (var project in projects)
        {
            if (project.SolutionFolder is { } f && f != folder)
                md.AppendLine($"  {f}/");
            folder = project.SolutionFolder;
            md.AppendLine(folder is null ? $"  {project.Name}/" : $"    {project.Name}/");
        }
        md.AppendLine("```");
        md.AppendLine();
        md.AppendLine("| Project | Folder | Template | Namespace | Role |");
        md.AppendLine("|---|---|---|---|---|");
        foreach (var project in projects)
            md.AppendLine($"| `{project.Name}` | `{project.Folder}/` | `{project.Template}` | `{project.Namespace}` | {RoleText(plan, project)} |");
        md.AppendLine();

        md.AppendLine("## 3. Project references (dependency rules)");
        md.AppendLine();
        md.AppendLine("| Project | References |");
        md.AppendLine("|---|---|");
        foreach (var project in projects)
            md.AppendLine($"| `{project.Name}` | {(project.References.Count == 0 ? "nothing" : string.Join(", ", project.References.Select(r => $"`{r}`")))} |");
        md.AppendLine();
        md.AppendLine("Rules:");
        md.AppendLine();
        md.AppendLine($"- `{s}.Domain` references nothing.");
        md.AppendLine($"- `{useCases}` references `{s}.Domain` only.");
        md.AppendLine($"- Every adapter project references `{useCases}` only. **Adapters never reference each other.**");
        md.AppendLine("- The specs project may also reference the fakes it wires up by hand.");
        md.AppendLine("- These references are the dependency rule. Don't add others.");
        md.AppendLine();

        md.AppendLine("## 4. Namespaces");
        md.AppendLine();
        md.AppendLine($"- `{s}.Domain` and `{useCases}` both use the plain `{s}` namespace, so the port interfaces are `{s}.{plan.Ports[0].Interfaces[0]}` and so on.");
        md.AppendLine("- Every other project uses the namespace in section 2. Real and simulated adapters share one namespace, and so do the fakes.");
        md.AppendLine("- Folders inside a project don't change its namespace.");
        md.AppendLine("- Set `RootNamespace` in each `.csproj` to its namespace, so new files created in an IDE get it.");
        md.AppendLine($"- Every namespace sits inside `{s}`, so adapters, fakes and specs see the port interfaces without a `using`.");
        md.AppendLine();

        md.AppendLine("## 5. Ports");
        md.AppendLine();
        md.AppendLine($"All port interfaces live in `{useCases}`, namespace `{s}`, one file each. Interfaces take the plain name of the port, **with no `I` prefix**. Leave them empty: their members are design work still to do.");
        md.AppendLine();
        md.AppendLine("```");
        md.AppendLine($"{useCases}/");
        foreach (var name in plan.Ports.Where(p => p.Folder == "").SelectMany(p => p.Interfaces))
            md.AppendLine($"  {name}.cs");
        md.AppendLine($"  {s}Application.cs");
        foreach (var group in plan.Ports.Where(p => p.Folder != "").GroupBy(p => p.Folder))
        {
            md.AppendLine($"  {group.Key}/");
            foreach (var name in group.SelectMany(p => p.Interfaces))
                md.AppendLine($"    {name}.cs");
        }
        md.AppendLine("```");
        md.AppendLine();
        foreach (var port in plan.Ports)
        {
            md.AppendLine($"### {port.Name} ({(port.Side == Side.Driving ? "driving" : "driven")})");
            md.AppendLine();
            if (port.IsUser)
                md.AppendLine("The User port: how people (and anything acting for them) use the application. It has two halves, commands and queries.");
            if (!string.IsNullOrWhiteSpace(port.Description))
                md.AppendLine(port.Description);
            if (port.IsUser || !string.IsNullOrWhiteSpace(port.Description))
                md.AppendLine();
            foreach (var name in port.Interfaces)
            {
                md.AppendLine($"`{(port.Folder == "" ? "" : port.Folder + "/")}{name}.cs`:");
                md.AppendLine();
                md.AppendLine("```csharp");
                md.AppendLine($"namespace {s};");
                md.AppendLine();
                md.AppendLine($"public interface {name}");
                md.AppendLine("{");
                md.AppendLine("}");
                md.AppendLine("```");
                md.AppendLine();
            }
        }
        var drivingInterfaces = plan.Ports.Where(p => p.Side == Side.Driving).SelectMany(p => p.Interfaces).ToList();
        md.AppendLine($"Also add one stub class, `{s}Application` in `{useCases}` (namespace `{s}`), implementing the driving interfaces ({string.Join(", ", drivingInterfaces.Select(i => $"`{i}`"))}). It is the application itself; its constructor will later take the driven ports.");
        md.AppendLine();

        md.AppendLine("## 6. Adapters");
        md.AppendLine();
        md.AppendLine("Kinds: **Real** is the production adapter; **Fake** is a simple in-process stand-in for fast specs; **Simulated** is a higher-fidelity stand-in for people running the whole app locally.");
        md.AppendLine();
        md.AppendLine("| Port | Adapter | Kind | Technology | Project | Class | Source |");
        md.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var port in plan.Ports)
            foreach (var a in port.Adapters)
                md.AppendLine($"| {port.Name} | {a.Name} | {a.Kind} | {a.Technology ?? "-"} | `{a.Project}`{(a.Folder == "" ? "" : $" (`{a.Folder}/`)")} | {(a.Class is null ? "-" : $"`{a.Class}`")} | {a.Source} |");
        md.AppendLine();
        md.AppendLine("For each driven adapter, add the stub class named above implementing its port's interface, in its project's namespace and in the folder shown. For example:");
        md.AppendLine();
        var example = plan.Ports.Where(p => p.Side == Side.Driven)
            .SelectMany(p => p.Adapters.Select(a => (Port: p, Adapter: a)))
            .OrderBy(x => x.Adapter.Kind != AdapterKind.Real)
            .FirstOrDefault(x => x.Adapter.Class is not null);
        md.AppendLine("```csharp");
        if (example.Adapter is { } adapter)
        {
            md.AppendLine($"namespace {plan.Projects.First(p => p.Name == adapter.Project).Namespace};");
            md.AppendLine();
            md.AppendLine($"public sealed class {adapter.Class} : {example.Port.Interfaces[0]}");
        }
        else
        {
            md.AppendLine($"namespace {s}.Adapters;");
            md.AppendLine();
            md.AppendLine("public sealed class FileSystemStorage : Storage");
        }
        md.AppendLine("{");
        md.AppendLine("}");
        md.AppendLine("```");
        md.AppendLine();
        md.AppendLine("Driving adapters (the app among them) keep their project template's default files. Fakes on driving ports are the specs (section 7).");
        md.AppendLine();
        var descriptions = plan.Ports.SelectMany(p => p.Adapters).Where(a => !string.IsNullOrWhiteSpace(a.Description)).ToList();
        if (descriptions.Count > 0)
        {
            md.AppendLine("Adapter notes:");
            md.AppendLine();
            foreach (var a in descriptions)
                md.AppendLine($"- **{a.Name}** (`{a.Project}`): {a.Description}");
            md.AppendLine();
        }

        md.AppendLine("## 7. Fakes and specs");
        md.AppendLine();
        if (plan.Projects.FirstOrDefault(p => p.Role == ProjectRole.Fakes) is { } fakes)
        {
            var folders = plan.Ports.SelectMany(p => p.Adapters).Where(a => a.Project == fakes.Name).Select(a => $"`{a.Folder}/`").Distinct();
            md.AppendLine($"- `{fakes.Name}` (`{fakes.Folder}/`): every fake driven adapter, with a folder per port ({string.Join(", ", folders)}). The namespace is flat: `{fakes.Namespace}`.");
        }
        var specs = plan.Project(ProjectRole.Specs);
        md.AppendLine($"- `{specs.Name}` (`{specs.Folder}/`): xUnit. Packages: {string.Join(", ", specs.Packages.Select(p => $"`{p}`"))}. Keep the template's packages and add `AwesomeAssertions`.");
        md.AppendLine("  Add one placeholder spec class with a single passing `[Fact]`, so `dotnet test` runs. Specs wire the fake adapters up by hand; there is no composition root yet.");
        md.AppendLine("- Use the latest stable package versions. Use AwesomeAssertions (`.Should()` style) in specs.");
        md.AppendLine();

        md.AppendLine("## 8. Not placed");
        md.AppendLine();
        if (plan.NotPlaced.Count == 0)
            md.AppendLine("Every element of the context diagram is on the sketch.");
        else
        {
            md.AppendLine("These context diagram elements were deliberately left off the sketch. Don't build anything for them.");
            md.AppendLine();
            foreach (var element in plan.NotPlaced)
                md.AppendLine($"- {element.Name} ({TypeText(element.Type)}{Suffix(element.Technology, ", ")})");
        }
        md.AppendLine();

        md.AppendLine("## 9. Build this");
        md.AppendLine();
        md.AppendLine("Instructions for the coding agent:");
        md.AppendLine();
        md.AppendLine($"1. Create `{s}.sln` (classic `.sln` format: `dotnet new sln --format sln`).");
        md.AppendLine("2. Create every project in section 2 with its template, in its folder, and add it to the solution in the matching solution folder (`dotnet sln add <project> --solution-folder <folder>`, or `--in-root` for top-level projects). Set each project's `RootNamespace`.");
        md.AppendLine($"3. Delete template placeholder files (`Class1.cs`, `UnitTest1.cs`), except `Class1.cs` in `{s}.Domain`: keep it, with its namespace changed to `{s}`, to show the namespace choice.");
        md.AppendLine("4. Add exactly the project references in section 3, and the packages in section 7.");
        md.AppendLine("5. Add a root `.editorconfig` with a naming rule that says interfaces are plain PascalCase, so the IDE doesn't ask for an `I` prefix:");
        md.AppendLine();
        md.AppendLine("   ```ini");
        foreach (var line in EditorConfig)
            md.AppendLine(line.Length == 0 ? "" : $"   {line}");
        md.AppendLine("   ```");
        md.AppendLine();
        md.AppendLine("6. Add the port interfaces and the application stub from section 5, and the adapter stub classes from section 6.");
        md.AppendLine("7. Add the placeholder spec from section 7.");
        md.AppendLine("8. Check that `dotnet build` and `dotnet test` both succeed.");
        md.AppendLine();
        md.AppendLine("Ground rules:");
        md.AppendLine();
        md.AppendLine("- **Stubs only.** No functionality, no interface members, no wiring.");
        md.AppendLine("- **No composition root** (no host project, no dependency injection setup) in this version.");
        md.AppendLine("- **No `I` prefix** on interfaces. Implementations carry the meaningful extra (`FileSystemStorage`), never `Impl`.");
        md.AppendLine("- Don't add projects, references or packages the plan doesn't list.");

        return md.ToString();
    }

    private static readonly string[] EditorConfig =
    [
        "root = true",
        "",
        "[*.cs]",
        "dotnet_naming_rule.interfaces_no_prefix.symbols = interfaces",
        "dotnet_naming_rule.interfaces_no_prefix.style = pascal_case",
        "dotnet_naming_rule.interfaces_no_prefix.severity = warning",
        "",
        "dotnet_naming_symbols.interfaces.applicable_kinds = interface",
        "dotnet_naming_symbols.interfaces.applicable_accessibilities = *",
        "",
        "dotnet_naming_style.pascal_case.capitalization = pascal_case",
    ];

    /// <summary>Top-level projects first, then each solution folder's projects together.</summary>
    private static IEnumerable<PlannedProject> Ordered(SolutionPlan plan) =>
        plan.Projects.Where(p => p.SolutionFolder is null)
            .Concat(plan.Projects.Where(p => p.SolutionFolder is not null).GroupBy(p => p.SolutionFolder).SelectMany(g => g));

    private static int Count(IEnumerable<PlannedPort> ports, Side side) => ports.Count(p => p.Side == side);

    private static string Suffix(string? text, string separator = " ") =>
        string.IsNullOrWhiteSpace(text) ? "" : separator == " " ? $" ({text})" : $"{separator}{text}";

    private static string RoleText(SolutionPlan plan, PlannedProject project) => project.Role switch
    {
        ProjectRole.Domain => "Domain-specific business rules",
        ProjectRole.UseCases => "Application rules (use cases) and all port interfaces",
        ProjectRole.App => "The app: runnable adapter on the User port",
        ProjectRole.Adapters =>
            $"Adapters on the {plan.Ports.First(p => p.Adapters.Any(a => a.Project == project.Name)).Name} port",
        ProjectRole.Fakes => "Fake driven adapters, for specs",
        _ => "Specs: the fake on every driving port",
    };

    private static string TypeText(ElementType type) => type switch
    {
        ElementType.PrimaryActor => "primary actor",
        ElementType.SecondaryActor => "secondary actor",
        ElementType.ExternalSystem => "external system",
        ElementType.Storage => "storage",
        _ => "system",
    };
}
