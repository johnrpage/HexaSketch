using HexaSketch.Application.Plan;
using HexaSketch.Domain;

namespace HexaSketch.Specs;

public class Exporting : Sketching
{
    private const string PlanFile = "plan.md";
    private const string DiagramFile = "diagram.svg";

    private string Plan => Exports.Plans[PlanFile];

    private void SketchTheTrackerAndStorage()
    {
        Start();
        var tracker = Tasks.AddPort(Side.Driven);
        Tasks.RenamePort(tracker, "Tracker");
        Tasks.Place(Item("Azure DevOps"), tracker);
        Tasks.Place(Item("Jira"), tracker);
        var storage = Tasks.AddPort(Side.Driven);
        Tasks.RenamePort(storage, "Storage");
        var files = Tasks.AddAdapter(storage);
        Tasks.RenameAdapter(files, "File system");
        var memory = Tasks.AddAdapter(storage);
        Tasks.RenameAdapter(memory, "In-memory");
        Tasks.SetAdapterKind(memory, AdapterKind.Fake);
    }

    private string ExportedPlan()
    {
        Assert.True(Tasks.ExportPlan(PlanFile).Succeeded, string.Join(" ", Queries.CheckExport().PlanProblems));
        return Plan;
    }

    private void RemoveTheAgent() => Tasks.RemoveAdapter(Adapter("Agent").Id);

    [Fact]
    public void Port_interfaces_have_plain_names_with_no_I_prefix()
    {
        SketchTheTrackerAndStorage();

        var plan = ExportedPlan();

        Assert.Contains("public interface Tracker", plan);
        Assert.Contains("public interface Storage", plan);
        Assert.Contains("public interface UserTasks", plan);
        Assert.Contains("public interface UserQueries", plan);
        Assert.DoesNotContain("interface ITracker", plan);
    }

    [Fact]
    public void The_core_shares_the_solution_namespace_and_driven_ports_sit_in_a_Ports_folder()
    {
        SketchTheTrackerAndStorage();

        var plan = ExportedPlan();

        Assert.Contains("| `CumulativeFlow.Domain` | `CumulativeFlow/CumulativeFlow.Domain/` | `classlib` | `CumulativeFlow` |", plan);
        Assert.Contains("| `CumulativeFlow.UseCases` | `CumulativeFlow/CumulativeFlow.UseCases/` | `classlib` | `CumulativeFlow` |", plan);
        Assert.Contains("`Ports/Tracker.cs`", plan);
        Assert.Contains("`UserTasks.cs`", plan);
        Assert.DoesNotContain("src/CumulativeFlow", plan);
    }

    [Fact]
    public void Implementations_carry_the_adapter_name()
    {
        SketchTheTrackerAndStorage();

        var plan = ExportedPlan();

        Assert.Contains("`AzureDevOpsTracker`", plan);
        Assert.Contains("`JiraTracker`", plan);
        Assert.Contains("`FileSystemStorage`", plan);
        Assert.Contains("`InMemoryStorage`", plan);
    }

    [Fact]
    public void A_real_adapter_named_like_its_port_takes_its_class_name_from_its_technology()
    {
        Start();
        var storage = Tasks.AddPort(Side.Driven);
        Tasks.RenamePort(storage, "Storage");
        var adapter = Tasks.AddAdapter(storage);
        Tasks.RenameAdapter(adapter, "Storage");
        Tasks.SetAdapterTechnology(adapter, "File System (JSON)");

        var plan = ExportedPlan();

        Assert.Contains("`FileSystemStorage`", plan);
        Assert.Contains("public sealed class FileSystemStorage : Storage", plan);
    }

    [Fact]
    public void A_real_adapter_with_no_name_of_its_own_and_no_technology_blocks_the_plan()
    {
        Start();
        var storage = Tasks.AddPort(Side.Driven);
        Tasks.RenamePort(storage, "Storage");
        Tasks.RenameAdapter(Tasks.AddAdapter(storage), "Storage");

        Assert.False(Tasks.ExportPlan(PlanFile).Succeeded);
        Assert.Contains(Queries.CheckExport().PlanProblems, p => p.Contains("technology"));
    }

    [Fact]
    public void Each_port_gets_one_adapters_project_holding_its_real_and_simulated_adapters()
    {
        SketchTheTrackerAndStorage();
        var simulated = Tasks.AddAdapter(Port("Storage").Id, AdapterKind.Simulated);
        Tasks.RenameAdapter(simulated, "Local");

        var plan = ExportedPlan();

        Assert.Contains("| `CumulativeFlow.Tracker` | `Adapters/CumulativeFlow.Tracker/` | `classlib` | `CumulativeFlow.Adapters` |", plan);
        Assert.Contains("| Storage | Local | Simulated | - | `CumulativeFlow.Storage` | `LocalStorage` |", plan);
        Assert.Contains("| Tracker | Jira | Real | HTTPS | `CumulativeFlow.Tracker` | `JiraTracker` |", plan);
    }

    [Fact]
    public void Fakes_share_one_project_with_a_folder_per_port()
    {
        SketchTheTrackerAndStorage();

        var plan = ExportedPlan();

        Assert.Contains("| Tracker | Fake | Fake | - | `CumulativeFlow.Fakes` (`Tracker/`) | `FakeTracker` |", plan);
        Assert.Contains("| Storage | In-memory | Fake | - | `CumulativeFlow.Fakes` (`Storage/`) | `InMemoryStorage` |", plan);
        Assert.Contains("| `CumulativeFlow.Fakes` | `Checks/CumulativeFlow.Fakes/` | `classlib` | `CumulativeFlow.Fakes` |", plan);
    }

    [Fact]
    public void Every_port_without_a_fake_gets_one_in_the_plan()
    {
        SketchTheTrackerAndStorage();

        var plan = ExportedPlan();

        Assert.Contains("`FakeTracker`", plan);
        Assert.DoesNotContain("`FakeStorage`", plan);
        Assert.Contains("| User | Specs | Fake | - | `CumulativeFlow.Specs` | - |", plan);
    }

    [Fact]
    public void The_specs_project_references_the_use_cases_and_the_fakes()
    {
        SketchTheTrackerAndStorage();

        var plan = ExportedPlan();

        Assert.Contains("| `CumulativeFlow.Specs` | `CumulativeFlow.UseCases`, `CumulativeFlow.Fakes` |", plan);
        Assert.Contains("`AwesomeAssertions`", plan);
        Assert.Contains("`coverlet.collector`", plan);
    }

    [Fact]
    public void The_one_runnable_adapter_on_the_User_port_is_the_App()
    {
        SketchTheTrackerAndStorage();
        RemoveTheAgent();

        var plan = ExportedPlan();

        Assert.Contains("| `CumulativeFlow.App` | `CumulativeFlow.App/` | `wpf` | `CumulativeFlow.App` |", plan);
        Assert.DoesNotContain("CumulativeFlow.Viewer", plan);
    }

    [Fact]
    public void Several_runnable_adapters_on_the_User_port_are_named_after_themselves()
    {
        SketchTheTrackerAndStorage();

        var plan = ExportedPlan();

        Assert.Contains("| `CumulativeFlow.Viewer` | `CumulativeFlow.Viewer/` | `wpf` | `CumulativeFlow.Viewer` |", plan);
        Assert.Contains("| `CumulativeFlow.Agent` | `CumulativeFlow.Agent/` | `console` | `CumulativeFlow.Agent` |", plan);
        Assert.DoesNotContain("CumulativeFlow.App", plan);
    }

    [Fact]
    public void Adapters_on_other_driving_ports_go_in_their_ports_adapters_project()
    {
        SketchTheTrackerAndStorage();
        var api = Tasks.AddPort(Side.Driving);
        Tasks.RenamePort(api, "Reports");
        Tasks.RenameAdapter(Tasks.AddAdapter(api), "Http");

        var plan = ExportedPlan();

        Assert.Contains("| Reports | Http | Real | - | `CumulativeFlow.Reports` | - |", plan);
        Assert.Contains("| Reports | Specs | Fake | - | `CumulativeFlow.Specs` | - |", plan);
        Assert.Contains("`Adapters/CumulativeFlow.Reports/`", plan);
    }

    [Fact]
    public void The_plan_lists_items_that_were_not_placed()
    {
        SketchTheTrackerAndStorage();

        var plan = ExportedPlan();

        Assert.Contains("- Storage 1 (storage, Flat files)", plan);
        Assert.Equal(["Storage 1"], Queries.CheckExport().Unplaced);
    }

    [Fact]
    public void Names_that_would_clash_in_code_block_the_plan()
    {
        Start();
        Tasks.RenamePort(Tasks.AddPort(Side.Driven), "Work items");
        Tasks.RenamePort(Tasks.AddPort(Side.Driven), "work-items");

        var outcome = Tasks.ExportPlan(PlanFile);

        Assert.False(outcome.Succeeded);
        Assert.NotEmpty(Queries.CheckExport().PlanProblems);
        Assert.Empty(Exports.Plans);
    }

    [Theory]
    [InlineData("Fakes")]
    [InlineData("Adapters")]
    [InlineData("Use cases")]
    [InlineData("Cumulative flow")]
    public void A_port_named_like_a_project_or_namespace_blocks_the_plan(string name)
    {
        Start();
        Tasks.RenamePort(Tasks.AddPort(Side.Driven), name);

        Assert.False(Tasks.ExportPlan(PlanFile).Succeeded);
    }

    [Fact]
    public void A_port_named_like_a_runnable_adapter_blocks_the_plan()
    {
        Start();
        Tasks.RenamePort(Tasks.AddPort(Side.Driven), "Viewer");

        Assert.False(Tasks.ExportPlan(PlanFile).Succeeded);
    }

    [Fact]
    public void The_diagram_shows_the_hexagon_ports_and_adapters()
    {
        SketchTheTrackerAndStorage();

        Assert.True(Tasks.ExportDiagram(DiagramFile).Succeeded);

        var svg = Exports.Diagrams[DiagramFile];
        Assert.StartsWith("<?xml", svg);
        foreach (var text in new[] { "Domain", "Tasks", "Queries", "User", "Tracker", "Jira", "In-memory" })
            Assert.Contains($">{text}<", svg);
    }

    [Theory]
    [InlineData("In-memory store", "InMemoryStore")]
    [InlineData("WPF UI", "WpfUI")]
    [InlineData("Azure DevOps", "AzureDevOps")]
    [InlineData("file system", "FileSystem")]
    [InlineData("  ", "")]
    public void Display_names_become_PascalCase_code_names(string name, string code) =>
        Assert.Equal(code, CodeName.From(name));

    [Theory]
    [InlineData("Azure DevOps", "REST", AdapterKind.Real, "AzureDevOpsTracker")]
    [InlineData("Fake tracker", null, AdapterKind.Fake, "FakeTracker")]
    [InlineData("Tracker", "File System (JSON)", AdapterKind.Real, "FileSystemTracker")]
    [InlineData("Tracker", null, AdapterKind.Simulated, "SimulatedTracker")]
    [InlineData("Tracker", null, AdapterKind.Real, null)]
    public void Class_names_come_from_the_adapter_then_the_technology_then_the_kind(
        string adapter, string? technology, AdapterKind kind, string? expected) =>
        Assert.Equal(expected, SolutionPlanner.ClassFor("Tracker", CodeName.From(adapter), technology, kind));
}
