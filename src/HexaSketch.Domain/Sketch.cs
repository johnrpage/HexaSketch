namespace HexaSketch.Domain;

public sealed record AdapterRecord(
    string Id, string Name, string? Technology, string? Description, AdapterKind Kind, string? SourceElementId);

public sealed record PortRecord(
    string Id, string Name, string? Description, Side Side, bool IsUser, IReadOnlyList<AdapterRecord> Adapters);

/// <summary>A ports-and-adapters sketch of one system, grown from a context diagram.</summary>
/// <remarks>
/// The hexagon (Domain, Tasks, Queries) is a given. The sketch is the ports around it and the
/// adapters on them. A context diagram element is "placed" when an adapter came from it; every
/// other non-system element is waiting in the list.
/// </remarks>
public sealed class Sketch
{
    public const string UserPortName = "User";

    private readonly List<Port> ports = [];
    private readonly Random random;

    private Sketch(string title, ContextDiagram contextDiagram, Random random)
    {
        Title = title;
        ContextDiagram = contextDiagram;
        this.random = random;
    }

    public string Title { get; private set; }
    public ContextDiagram ContextDiagram { get; }
    public ContextElement System => ContextDiagram.System;
    public IReadOnlyList<Port> Ports => ports;
    public Port UserPort => ports.Single(p => p.IsUser);

    public IEnumerable<Port> PortsOn(Side side) => ports.Where(p => p.Side == side);

    public IEnumerable<Adapter> Adapters => ports.SelectMany(p => p.Adapters);

    /// <summary>Context diagram elements that no adapter has come from yet, in diagram order.</summary>
    public IReadOnlyList<ContextElement> UnplacedElements
    {
        get
        {
            var placed = Adapters.Select(a => a.SourceElementId).OfType<string>().ToHashSet();
            return ContextDiagram.Elements
                .Where(e => e.Type != ElementType.System && !placed.Contains(e.Id))
                .ToList();
        }
    }

    /// <summary>Starts a sketch: the User port, with one adapter for each primary actor.</summary>
    public static Sketch StartFrom(ContextDiagram contextDiagram, Random? random = null)
    {
        var problems = contextDiagram.Problems();
        if (problems.Count > 0)
            throw new SketchRuleException(string.Join(Environment.NewLine, problems));

        var sketch = new Sketch(contextDiagram.Title, contextDiagram, random ?? Random.Shared);
        var user = new Port(sketch.NewId(), UserPortName, Side.Driving, isUser: true);
        sketch.ports.Add(user);
        foreach (var actor in contextDiagram.Elements.Where(e => e.Type == ElementType.PrimaryActor))
            user.Insert(sketch.AdapterFrom(actor), user.Adapters.Count);
        return sketch;
    }

    /// <summary>Rebuilds a saved sketch, checking it still makes sense.</summary>
    public static Sketch Restore(
        string title, ContextDiagram contextDiagram, IEnumerable<PortRecord> portRecords, Random? random = null)
    {
        var problems = contextDiagram.Problems().ToList();
        var sketch = new Sketch(title, contextDiagram, random ?? Random.Shared);
        var records = portRecords.ToList();

        var userPorts = records.Count(p => p.IsUser);
        if (userPorts != 1)
            problems.Add($"There must be exactly one User port, but there are {userPorts}.");
        if (records.Any(p => p.IsUser && p.Side != Side.Driving))
            problems.Add("The User port must be on the driving side.");

        var ids = records.Select(p => p.Id).Concat(records.SelectMany(p => p.Adapters).Select(a => a.Id));
        foreach (var duplicate in ids.GroupBy(id => id).Where(g => g.Count() > 1))
            problems.Add($"Id '{duplicate.Key}' is used more than once.");

        var sources = records.SelectMany(p => p.Adapters).Select(a => a.SourceElementId).OfType<string>().ToList();
        foreach (var source in sources.Distinct())
        {
            var element = contextDiagram.Find(source);
            if (element is null || element.Type == ElementType.System)
                problems.Add($"An adapter comes from '{source}', which is not an element of the context diagram.");
            else if (sources.Count(s => s == source) > 1)
                problems.Add($"More than one adapter comes from '{element.Name}'.");
        }

        if (problems.Count > 0)
            throw new SketchRuleException(string.Join(Environment.NewLine, problems));

        foreach (var record in records)
        {
            var port = new Port(record.Id, record.Name, record.Side, record.IsUser) { Description = record.Description };
            foreach (var a in record.Adapters)
            {
                var adapter = new Adapter(a.Id, a.Name, a.Kind, a.SourceElementId)
                {
                    Technology = a.Technology,
                    Description = a.Description,
                };
                port.Insert(adapter, port.Adapters.Count);
            }
            sketch.ports.Add(port);
        }
        return sketch;
    }

    public Sketch Clone()
    {
        var copy = new Sketch(Title, ContextDiagram, random);
        copy.ports.AddRange(ports.Select(p => p.Clone()));
        return copy;
    }

    public Port FindPort(string portId) =>
        ports.FirstOrDefault(p => p.Id == portId) ?? throw new SketchRuleException($"There is no port '{portId}'.");

    public Adapter FindAdapter(string adapterId) =>
        Adapters.FirstOrDefault(a => a.Id == adapterId) ?? throw new SketchRuleException($"There is no adapter '{adapterId}'.");

    public Port PortOf(string adapterId) =>
        ports.FirstOrDefault(p => p.Adapters.Any(a => a.Id == adapterId))
        ?? throw new SketchRuleException($"There is no adapter '{adapterId}'.");

    public void Rename(string title) => Title = title.Trim();

    public Port AddPort(Side side)
    {
        var port = new Port(NewId(), NextName("Port", ports.Select(p => p.Name)), side, isUser: false);
        ports.Add(port);
        return port;
    }

    public void RenamePort(string portId, string name) => FindPort(portId).Name = name.Trim();

    public void DescribePort(string portId, string? description) => FindPort(portId).Description = Tidy(description);

    /// <summary>Deletes a port. Its adapters go with it, so elements they came from return to the list.</summary>
    public void DeletePort(string portId)
    {
        var port = FindPort(portId);
        if (port.IsUser)
            throw new SketchRuleException("The User port cannot be deleted.");
        ports.Remove(port);
    }

    /// <summary>Moves a port to a new position among the ports on its own side.</summary>
    public void MovePort(string portId, int indexOnSide)
    {
        var port = FindPort(portId);
        var sameSide = PortsOn(port.Side).Where(p => p != port).ToList();
        sameSide.Insert(Math.Clamp(indexOnSide, 0, sameSide.Count), port);
        var otherSide = ports.Where(p => p.Side != port.Side).ToList();
        ports.Clear();
        ports.AddRange(port.Side == Side.Driving ? sameSide.Concat(otherSide) : otherSide.Concat(sameSide));
    }

    /// <summary>Turns a context diagram element into an adapter on a port.</summary>
    public Adapter Place(string elementId, string portId, int? index = null)
    {
        var element = Placeable(elementId);
        var port = FindPort(portId);
        var adapter = AdapterFrom(element);
        port.Insert(adapter, index ?? port.Adapters.Count);
        return adapter;
    }

    /// <summary>Adds an adapter that does not come from the context diagram, such as a fake.</summary>
    public Adapter AddAdapter(string portId, AdapterKind kind = AdapterKind.Real)
    {
        var port = FindPort(portId);
        var name = kind == AdapterKind.Real
            ? NextName("Adapter", Adapters.Select(a => a.Name))
            : UniqueName(kind.ToString(), port.Adapters.Select(a => a.Name));
        var adapter = new Adapter(NewId(), name, kind, null);
        port.Insert(adapter, port.Adapters.Count);
        return adapter;
    }

    /// <summary>Makes a new port named after a context diagram element, and places the element on it.</summary>
    public Adapter PlaceOnNewPort(string elementId, Side side)
    {
        var element = Placeable(elementId);
        var port = new Port(NewId(), UniqueName(element.Name, ports.Select(p => p.Name)), side, isUser: false);
        ports.Add(port);
        return Place(elementId, port.Id);
    }

    /// <summary>Moves an adapter to a port (its own or another) at the given position.</summary>
    public void MoveAdapter(string adapterId, string portId, int? index = null)
    {
        var adapter = FindAdapter(adapterId);
        var target = FindPort(portId);
        PortOf(adapterId).Remove(adapter);
        target.Insert(adapter, index ?? target.Adapters.Count);
    }

    /// <summary>Removes an adapter. If it came from the context diagram, that element returns to the list.</summary>
    public void RemoveAdapter(string adapterId) => PortOf(adapterId).Remove(FindAdapter(adapterId));

    public void RenameAdapter(string adapterId, string name) => FindAdapter(adapterId).Name = name.Trim();

    public void SetAdapterTechnology(string adapterId, string? technology) =>
        FindAdapter(adapterId).Technology = Tidy(technology);

    public void DescribeAdapter(string adapterId, string? description) =>
        FindAdapter(adapterId).Description = Tidy(description);

    public void SetAdapterKind(string adapterId, AdapterKind kind) => FindAdapter(adapterId).Kind = kind;

    private Adapter AdapterFrom(ContextElement element)
    {
        // A primary actor's technology is the person's ("User"); the relationship says how they get in ("WPF").
        var relationship = ContextDiagram.RelationshipOf(element.Id)?.Technology;
        var technology = element.Type == ElementType.PrimaryActor
            ? relationship ?? element.Technology
            : element.Technology ?? relationship;

        return new Adapter(NewId(), element.Name, AdapterKind.Real, element.Id)
        {
            Technology = Tidy(technology),
            Description = Tidy(element.Description),
        };
    }

    private string NewId()
    {
        var used = ContextDiagram.Elements.Select(e => e.Id)
            .Concat(ContextDiagram.Relationships.Select(r => r.Id))
            .Concat(ports.Select(p => p.Id))
            .Concat(Adapters.Select(a => a.Id))
            .ToHashSet();
        string id;
        do id = random.Next().ToString("x8"); while (used.Contains(id));
        return id;
    }

    /// <summary>The element, if it can become an adapter: it exists, isn't the system, and isn't placed yet.</summary>
    private ContextElement Placeable(string elementId)
    {
        var element = ContextDiagram.Find(elementId)
            ?? throw new SketchRuleException($"There is no context diagram element '{elementId}'.");
        if (element.Type == ElementType.System)
            throw new SketchRuleException("The system is the hexagon; it cannot become an adapter.");
        if (Adapters.Any(a => a.SourceElementId == elementId))
            throw new SketchRuleException($"'{element.Name}' is already on the diagram.");
        return element;
    }

    /// <summary>The name itself if it's free, otherwise "name 2", "name 3" and so on.</summary>
    private static string UniqueName(string name, IEnumerable<string> taken)
    {
        var names = taken.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!names.Contains(name)) return name;
        var n = 2;
        while (names.Contains($"{name} {n}")) n++;
        return $"{name} {n}";
    }

    private static string NextName(string stem, IEnumerable<string> taken)
    {
        var names = taken.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var n = 1;
        while (names.Contains($"{stem} {n}")) n++;
        return $"{stem} {n}";
    }

    private static string? Tidy(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
