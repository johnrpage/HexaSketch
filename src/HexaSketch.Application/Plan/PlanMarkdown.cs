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
        md.AppendLine("- **Architecture**: ports and adapters. The application core (Domain and Application) knows nothing about the outside world; every external concern sits behind a port and is reached through an adapter.");
        md.AppendLine($"- **Ports**: {Count(plan.Ports, Side.Driving)} driving, {Count(plan.Ports, Side.Driven)} driven");
        md.AppendLine();

        md.AppendLine("## 2. Solution and project layout");
        md.AppendLine();
        md.AppendLine("```");
        md.AppendLine($"{s}.sln");
        foreach (var project in plan.Projects)
            md.AppendLine($"  {project.Folder}/");
        md.AppendLine("```");
        md.AppendLine();
        md.AppendLine("| Project | Template | Role |");
        md.AppendLine("|---|---|---|");
        foreach (var project in plan.Projects)
            md.AppendLine($"| `{project.Name}` | `{project.Template}` | {RoleText(project.Role)} |");
        md.AppendLine();

        md.AppendLine("## 3. Project references (dependency rules)");
        md.AppendLine();
        md.AppendLine("| Project | References |");
        md.AppendLine("|---|---|");
        foreach (var project in plan.Projects)
            md.AppendLine($"| `{project.Name}` | {(project.References.Count == 0 ? "nothing" : string.Join(", ", project.References.Select(r => $"`{r}`")))} |");
        md.AppendLine();
        md.AppendLine("Rules:");
        md.AppendLine();
        md.AppendLine($"- `{s}.Domain` references nothing.");
        md.AppendLine($"- `{s}.Application` references `{s}.Domain` only.");
        md.AppendLine($"- Every adapter project references `{s}.Application` only. **Adapters never reference each other.**");
        md.AppendLine("- Spec projects may also reference the fake driven adapters they wire up by hand.");
        md.AppendLine("- These references are the dependency rule. Don't add others.");
        md.AppendLine();

        md.AppendLine("## 4. Ports");
        md.AppendLine();
        md.AppendLine($"All port interfaces live in `{s}.Application`, namespace `{s}.Application`. Interfaces take the plain name of the port, **with no `I` prefix**. Leave them empty: their members are design work still to do.");
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
            md.AppendLine("```csharp");
            md.AppendLine($"namespace {s}.Application;");
            foreach (var name in port.Interfaces)
            {
                md.AppendLine();
                md.AppendLine($"public interface {name}");
                md.AppendLine("{");
                md.AppendLine("}");
            }
            md.AppendLine("```");
            md.AppendLine();
        }
        var drivingInterfaces = plan.Ports.Where(p => p.Side == Side.Driving).SelectMany(p => p.Interfaces).ToList();
        md.AppendLine($"Also add one stub class, `{s}Application` in `{s}.Application`, implementing the driving interfaces ({string.Join(", ", drivingInterfaces.Select(i => $"`{i}`"))}). It is the application itself; its constructor will later take the driven ports.");
        md.AppendLine();

        md.AppendLine("## 5. Adapters");
        md.AppendLine();
        md.AppendLine("Kinds: **Real** is the production adapter; **Fake** is a simple in-process stand-in for fast specs; **Simulated** is a higher-fidelity stand-in for people running the whole app locally.");
        md.AppendLine();
        md.AppendLine("| Port | Adapter | Kind | Technology | Project | Class | Source |");
        md.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var port in plan.Ports)
            foreach (var a in port.Adapters)
                md.AppendLine($"| {port.Name} | {a.Name} | {a.Kind} | {a.Technology ?? "-"} | `{a.Project}` | {(a.Class is null ? "-" : $"`{a.Class}`")} | {a.Source} |");
        md.AppendLine();
        md.AppendLine("For each driven adapter, add the stub class named above implementing its port's interface, in the namespace matching its project. For example:");
        md.AppendLine();
        var example = plan.Ports.Where(p => p.Side == Side.Driven).SelectMany(p => p.Adapters.Select(a => (p, a))).FirstOrDefault();
        md.AppendLine("```csharp");
        if (example.a is not null)
        {
            md.AppendLine($"namespace {example.a.Project};");
            md.AppendLine();
            md.AppendLine($"using {s}.Application;");
            md.AppendLine();
            md.AppendLine($"public sealed class {example.a.Class} : {example.p.Interfaces[0]}");
        }
        else
        {
            md.AppendLine($"namespace {s}.Storage.FileSystem;");
            md.AppendLine();
            md.AppendLine($"public sealed class FileSystemStorage : Storage");
        }
        md.AppendLine("{");
        md.AppendLine("}");
        md.AppendLine("```");
        md.AppendLine();
        md.AppendLine("Driving adapters keep their project template's default files. Fakes on driving ports are spec projects (next section).");
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

        md.AppendLine("## 6. Test projects and packages");
        md.AppendLine();
        var specs = plan.Projects.Where(p => p.Role == ProjectRole.Specs).ToList();
        foreach (var project in specs)
        {
            md.AppendLine($"- `{project.Name}` (`{project.Folder}`): xUnit. Packages: {string.Join(", ", project.Packages.Select(p => $"`{p}`"))}.");
            md.AppendLine($"  Add one placeholder spec class with a single passing `[Fact]`, so `dotnet test` runs. Specs wire the fake adapters up by hand; there is no composition root yet.");
        }
        md.AppendLine("- Use the latest stable package versions. Prefer Shouldly assertions in specs.");
        md.AppendLine();

        md.AppendLine("## 7. Not placed");
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

        md.AppendLine("## 8. Build this");
        md.AppendLine();
        md.AppendLine("Instructions for the coding agent:");
        md.AppendLine();
        md.AppendLine($"1. Create `{s}.sln` (classic `.sln` format: `dotnet new sln --format sln`).");
        md.AppendLine("2. Create every project in section 2 with its template and folder, and add it to the solution. Delete template placeholder files (`Class1.cs`, `UnitTest1.cs`).");
        md.AppendLine("3. Add exactly the project references in section 3, and the packages in section 6.");
        md.AppendLine("4. Add the port interfaces and the application stub from section 4, and the adapter stub classes from section 5.");
        md.AppendLine("5. Add the placeholder specs from section 6.");
        md.AppendLine("6. Check that `dotnet build` and `dotnet test` both succeed.");
        md.AppendLine();
        md.AppendLine("Ground rules:");
        md.AppendLine();
        md.AppendLine("- **Stubs only.** No functionality, no interface members, no wiring.");
        md.AppendLine("- **No composition root** (no host project, no dependency injection setup) in this version.");
        md.AppendLine("- **No `I` prefix** on interfaces. Implementations carry the meaningful extra (`FileSystemStorage`), never `Impl`.");
        md.AppendLine("- Don't add projects, references or packages the plan doesn't list.");

        return md.ToString();
    }

    private static int Count(IEnumerable<PlannedPort> ports, Side side) => ports.Count(p => p.Side == side);

    private static string Suffix(string? text, string separator = " ") =>
        string.IsNullOrWhiteSpace(text) ? "" : separator == " " ? $" ({text})" : $"{separator}{text}";

    private static string RoleText(ProjectRole role) => role switch
    {
        ProjectRole.Domain => "Domain-specific business rules",
        ProjectRole.Application => "Application rules (use cases) and all port interfaces",
        ProjectRole.DrivingAdapter => "Driving adapter",
        ProjectRole.DrivenAdapter => "Driven adapter",
        _ => "Specs",
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
