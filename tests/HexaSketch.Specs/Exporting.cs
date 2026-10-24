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

    [Fact]
    public void Port_interfaces_have_plain_names_with_no_I_prefix()
    {
        SketchTheTrackerAndStorage();

        Assert.True(Tasks.ExportPlan(PlanFile).Succeeded);

        Assert.Contains("public interface Tracker", Plan);
        Assert.Contains("public interface Storage", Plan);
        Assert.Contains("public interface UserTasks", Plan);
        Assert.Contains("public interface UserQueries", Plan);
        Assert.DoesNotContain("interface ITracker", Plan);
    }

    [Fact]
    public void Implementations_carry_the_adapter_name()
    {
        SketchTheTrackerAndStorage();

        Tasks.ExportPlan(PlanFile);

        Assert.Contains("`AzureDevOpsTracker`", Plan);
        Assert.Contains("`FileSystemStorage`", Plan);
        Assert.Contains("`InMemoryStorage`", Plan);
        Assert.Contains("`CumulativeFlow.Storage.FileSystem`", Plan);
    }

    [Fact]
    public void Every_port_without_a_fake_gets_one_in_the_plan()
    {
        SketchTheTrackerAndStorage();

        Tasks.ExportPlan(PlanFile);

        Assert.Contains("`FakeTracker`", Plan);
        Assert.Contains("`CumulativeFlow.Tracker.Fake`", Plan);
        Assert.DoesNotContain("`FakeStorage`", Plan);
        Assert.Contains("`CumulativeFlow.Specs`", Plan);
    }

    [Fact]
    public void The_specs_project_references_every_fake_driven_adapter()
    {
        SketchTheTrackerAndStorage();

        Tasks.ExportPlan(PlanFile);

        Assert.Contains(
            "| `CumulativeFlow.Specs` | `CumulativeFlow.Application`, `CumulativeFlow.Tracker.Fake`, `CumulativeFlow.Storage.InMemory` |",
            Plan);
    }

    [Fact]
    public void The_plan_lists_items_that_were_not_placed()
    {
        SketchTheTrackerAndStorage();

        Tasks.ExportPlan(PlanFile);

        Assert.Contains("- Storage 1 (storage, Flat files)", Plan);
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
}
