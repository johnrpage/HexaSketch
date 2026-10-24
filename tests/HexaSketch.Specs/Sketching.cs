using HexaSketch.Application;
using HexaSketch.ContextDiagrams.Fake;
using HexaSketch.Domain;
using HexaSketch.Exports.Fake;
using HexaSketch.Sketches.Fake;

namespace HexaSketch.Specs;

/// <summary>Wires the application to fake adapters by hand, and offers an example context diagram.</summary>
public abstract class Sketching
{
    protected const string CumulativeFlow = "cumulative-flow.contextsketch.json";

    protected readonly FakeContextDiagrams ContextDiagrams = new();
    protected readonly FakeSketches Sketches = new();
    protected readonly FakeExports Exports = new();
    protected readonly UserTasks Tasks;
    protected readonly UserQueries Queries;

    protected Sketching()
    {
        var session = new SketchSession(ContextDiagrams, Sketches, Exports);
        Tasks = session;
        Queries = session;
        ContextDiagrams.Add(CumulativeFlow, CumulativeFlowDiagram());
    }

    protected SketchView Sketch => Queries.Sketch ?? throw new InvalidOperationException("No sketch is open.");

    protected PortView Port(string name) => Sketch.Ports.Single(p => p.Name == name);

    protected PortView UserPort => Sketch.Ports.Single(p => p.IsUser);

    protected AdapterView Adapter(string name) => Sketch.Ports.SelectMany(p => p.Adapters).Single(a => a.Name == name);

    protected string Item(string name) => Sketch.Unplaced.Single(i => i.Name == name).ElementId;

    protected void Start() => Assert.True(Tasks.StartFrom(CumulativeFlow).Succeeded);

    /// <summary>The Cumulative Flow context diagram from the original whiteboard discussion.</summary>
    protected static ContextDiagram CumulativeFlowDiagram() => new(
        "Cumulative Flow - Context Diagram",
        [
            new("sys", ElementType.System, "Cumulative Flow", "Tracks work items and projects progress", ".NET Desktop App", 0, 0, false),
            new("viewer", ElementType.PrimaryActor, "Viewer", "Views stuff", "User", -345, 0, false),
            new("agent", ElementType.PrimaryActor, "Agent", "Requests reports", "Claude", -345, 120, false),
            new("devops", ElementType.ExternalSystem, "Azure DevOps", null, "REST", 345, 0, false),
            new("jira", ElementType.ExternalSystem, "Jira", null, null, 345, 100, false),
            new("store", ElementType.Storage, "Storage 1", null, "Flat files", 345, 200, false),
        ],
        [
            new("r1", "viewer", "sys", "Views", "WPF", Direction.TwoWay),
            new("r2", "agent", "sys", "Requests report", "CLI", Direction.OneWay),
            new("r3", "sys", "devops", "Reads work item history", "HTTPS", Direction.OneWay),
            new("r4", "sys", "jira", "Reads issue history", "HTTPS", Direction.OneWay),
            new("r5", "sys", "store", "Saves config and results", null, Direction.TwoWay),
        ]);
}
