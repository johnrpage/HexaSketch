using HexaSketch.Application;
using HexaSketch.ContextDiagrams.FileSystem;
using HexaSketch.Domain;
using HexaSketch.Exports.FileSystem;
using HexaSketch.Sketches.FileSystem;

namespace HexaSketch.FileSystem.Specs;

public sealed class FileSystemAdapters : IDisposable
{
    private readonly string folder = Directory.CreateTempSubdirectory("hexasketch-specs-").FullName;
    private readonly SketchSession session = new(new FileSystemContextDiagrams(), new FileSystemSketches(), new FileSystemExports());

    public void Dispose() => Directory.Delete(folder, recursive: true);

    private static string HexaSketchContextDiagram()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var file = Path.Combine(dir.FullName, "HexaSketch - Context Diagram.contextsketch.json");
            if (File.Exists(file)) return file;
        }
        throw new FileNotFoundException("Can't find the HexaSketch context diagram above the test folder.");
    }

    [Fact]
    public void Reads_a_context_diagram_exported_by_Context_Sketch()
    {
        Assert.True(session.StartFrom(HexaSketchContextDiagram()).Succeeded);

        var sketch = session.Sketch!;
        Assert.Equal("HexaSketch", sketch.SystemName);
        Assert.Equal(["Developer"], sketch.Ports.Single().Adapters.Select(a => a.Name));
        Assert.Equal("WPF", sketch.Ports.Single().Adapters.Single().Technology);
        Assert.Equal(["Context Diagrams", "Sketches", "Exports"], sketch.Unplaced.Select(i => i.Name));
    }

    [Fact]
    public void An_older_context_diagram_with_technology_on_its_arrows_still_opens_and_uses_the_actors_own()
    {
        var file = Path.Combine(folder, "older.contextsketch.json");
        File.WriteAllText(file, """
            { "schemaVersion": 1, "title": "Older", "elements": [
                { "id": "s", "type": "system", "name": "Shop" },
                { "id": "u", "type": "primaryActor", "name": "Customer", "technology": "Web browser" } ],
              "relationships": [ { "id": "r", "from": "u", "to": "s", "label": "Buys", "technology": "HTTPS", "direction": "oneWay" } ] }
            """);

        Assert.True(session.StartFrom(file).Succeeded);

        Assert.Equal("Web browser", session.Sketch!.Ports.Single().Adapters.Single().Technology);
    }

    [Fact]
    public void Reports_a_file_that_is_not_a_context_diagram()
    {
        var file = Path.Combine(folder, "nonsense.json");
        File.WriteAllText(file, "{ not json");

        var outcome = session.StartFrom(file);

        Assert.False(outcome.Succeeded);
        Assert.NotEmpty(outcome.Problems);
    }

    [Fact]
    public void Saves_and_opens_a_sketch_with_everything_in_it()
    {
        session.StartFrom(HexaSketchContextDiagram());
        var port = session.AddPort(Side.Driven);
        session.RenamePort(port, "Sketches");
        session.DescribePort(port, "Saved sketches");
        var placed = session.Place(session.Sketch!.Unplaced.Single(i => i.Name == "Sketches").ElementId, port);
        session.RenameAdapter(placed, "File system");
        var fake = session.AddAdapter(port);
        session.SetAdapterKind(fake, AdapterKind.Simulated);
        session.SetAdapterTechnology(fake, "In memory");
        var file = Path.Combine(folder, "sketch.hexasketch.json");

        Assert.True(session.SaveAs(file).Succeeded);
        var saved = session.Sketch!;
        session.Close();
        Assert.True(session.Open(file).Succeeded);

        Assert.Equivalent(saved, session.Sketch, strict: true);
    }

    [Fact]
    public void Writes_the_diagram_and_plan()
    {
        session.StartFrom(HexaSketchContextDiagram());
        var svg = Path.Combine(folder, "diagram.svg");
        var plan = Path.Combine(folder, "plan.md");

        Assert.True(session.ExportDiagram(svg).Succeeded);
        Assert.True(session.ExportPlan(plan).Succeeded);

        Assert.StartsWith("<?xml", File.ReadAllText(svg));
        Assert.StartsWith("# HexaSketch: solution plan", File.ReadAllText(plan));
    }
}
