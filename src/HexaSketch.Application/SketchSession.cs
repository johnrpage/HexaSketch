using HexaSketch.Application.Diagram;
using HexaSketch.Application.Plan;
using HexaSketch.Domain;

namespace HexaSketch.Application;

/// <summary>The application: implements the User port on top of the domain and the driven ports.</summary>
/// <remarks>
/// Undo works on whole-sketch snapshots: every edit clones the sketch, changes the clone and keeps
/// the old one. Sketches are small, so this is simpler and safer than reversible commands.
/// </remarks>
public sealed class SketchSession(ContextDiagrams contextDiagrams, Sketches sketches, Exports exports)
    : UserTasks, UserQueries
{
    private readonly Stack<(Sketch Sketch, int Version)> undo = new();
    private readonly Stack<(Sketch Sketch, int Version)> redo = new();
    private Sketch? sketch;
    private int version;
    private int savedVersion;
    private int lastVersion;
    private (int Version, SketchView Sketch, DiagramView Diagram)? views;

    public event EventHandler? Changed;

    public string? Location { get; private set; }

    public bool HasUnsavedChanges => sketch is not null && version != savedVersion;
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;

    public SketchView? Sketch => sketch is null ? null : Views().Sketch;
    public DiagramView? Diagram => sketch is null ? null : Views().Diagram;

    public Outcome StartFrom(string contextDiagramLocation)
    {
        try
        {
            var diagram = contextDiagrams.Load(contextDiagramLocation);
            var problems = diagram.Problems();
            if (problems.Count > 0) return Outcome.Failed(problems);
            Begin(Domain.Sketch.StartFrom(diagram), location: null);
            return Outcome.Ok;
        }
        catch (UnreadableException e)
        {
            return Outcome.Failed(e.Problems);
        }
    }

    public Outcome Open(string sketchLocation)
    {
        try
        {
            Begin(sketches.Load(sketchLocation), sketchLocation);
            return Outcome.Ok;
        }
        catch (UnreadableException e)
        {
            return Outcome.Failed(e.Problems);
        }
        catch (SketchRuleException e)
        {
            return Outcome.Failed(e.Message.Split(Environment.NewLine));
        }
    }

    public Outcome Save() =>
        Location is null ? Outcome.Failed("The sketch hasn't been saved before; choose where to save it.") : SaveAs(Location);

    public Outcome SaveAs(string sketchLocation)
    {
        var current = Current();
        return Attempt(() =>
        {
            sketches.Save(sketchLocation, current);
            Location = sketchLocation;
            savedVersion = version;
            Raise();
        });
    }

    public void Close()
    {
        sketch = null;
        Location = null;
        undo.Clear();
        redo.Clear();
        Raise();
    }

    public Outcome ExportDiagram(string location)
    {
        var svg = SvgDiagram.Render(DiagramLayout.Of(Current()));
        return Attempt(() => exports.WriteDiagram(location, svg));
    }

    public Outcome ExportPlan(string location)
    {
        var current = Current();
        var plan = SolutionPlanner.Plan(current);
        if (plan.Problems.Count > 0) return Outcome.Failed(plan.Problems);
        var markdown = PlanMarkdown.Write(plan, current.Title);
        return Attempt(() => exports.WritePlan(location, markdown));
    }

    public ExportCheck CheckExport()
    {
        var current = Current();
        return new ExportCheck(
            current.UnplacedElements.Select(e => e.Name).ToList(),
            SolutionPlanner.Plan(current).Problems);
    }

    public string AddPort(Side side) => Edit(s => s.AddPort(side).Id);
    public void RenamePort(string portId, string name) => Edit(s => s.RenamePort(portId, name));
    public void DescribePort(string portId, string? description) => Edit(s => s.DescribePort(portId, description));
    public void DeletePort(string portId) => Edit(s => s.DeletePort(portId));
    public void MovePort(string portId, int indexOnSide) => Edit(s => s.MovePort(portId, indexOnSide));

    public string Place(string elementId, string portId, int? index = null) => Edit(s => s.Place(elementId, portId, index).Id);
    public string AddAdapter(string portId) => Edit(s => s.AddAdapter(portId).Id);
    public void MoveAdapter(string adapterId, string portId, int? index = null) => Edit(s => s.MoveAdapter(adapterId, portId, index));
    public void RemoveAdapter(string adapterId) => Edit(s => s.RemoveAdapter(adapterId));
    public void RenameAdapter(string adapterId, string name) => Edit(s => s.RenameAdapter(adapterId, name));
    public void SetAdapterTechnology(string adapterId, string? technology) => Edit(s => s.SetAdapterTechnology(adapterId, technology));
    public void DescribeAdapter(string adapterId, string? description) => Edit(s => s.DescribeAdapter(adapterId, description));
    public void SetAdapterKind(string adapterId, AdapterKind kind) => Edit(s => s.SetAdapterKind(adapterId, kind));

    public void Undo()
    {
        if (sketch is null || undo.Count == 0) return;
        redo.Push((sketch, version));
        (sketch, version) = undo.Pop();
        Raise();
    }

    public void Redo()
    {
        if (sketch is null || redo.Count == 0) return;
        undo.Push((sketch, version));
        (sketch, version) = redo.Pop();
        Raise();
    }

    private void Begin(Sketch started, string? location)
    {
        sketch = started;
        Location = location;
        undo.Clear();
        redo.Clear();
        version = savedVersion = ++lastVersion;
        Raise();
    }

    private void Edit(Action<Sketch> change) => Edit(s => { change(s); return 0; });

    /// <summary>Applies a change to a copy, so a change that breaks a rule leaves the sketch untouched.</summary>
    private T Edit<T>(Func<Sketch, T> change)
    {
        var current = Current();
        var next = current.Clone();
        var result = change(next);
        undo.Push((current, version));
        redo.Clear();
        sketch = next;
        version = ++lastVersion;
        Raise();
        return result;
    }

    private Sketch Current() => sketch ?? throw new InvalidOperationException("There is no sketch open.");

    private static Outcome Attempt(Action action)
    {
        try
        {
            action();
            return Outcome.Ok;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or UnreadableException)
        {
            return Outcome.Failed(e.Message);
        }
    }

    private void Raise() => Changed?.Invoke(this, EventArgs.Empty);

    private (int Version, SketchView Sketch, DiagramView Diagram) Views()
    {
        var current = Current();
        if (views is { } cached && cached.Version == version) return cached;
        views = (version, ViewOf(current), DiagramLayout.Of(current));
        return views.Value;
    }

    private static SketchView ViewOf(Sketch s) => new(
        s.Title,
        s.System.Name,
        s.System.Description,
        s.System.Technology,
        s.Ports.Select(p => new PortView(
            p.Id, p.Name, p.Description, p.Side, p.IsUser, CodeName.From(p.Name),
            p.Adapters.Select(a => new AdapterView(
                a.Id, p.Id, a.Name, a.Technology, a.Description, a.Kind, CodeName.From(a.Name),
                a.SourceElementId is { } id && s.ContextDiagram.Find(id) is { } e
                    ? new SourceView(e.Id, e.Name, e.Type)
                    : null)).ToList())).ToList(),
        s.UnplacedElements.Select(e => new UnplacedItemView(e.Id, e.Name, e.Type, e.Technology, e.Description)).ToList());
}
