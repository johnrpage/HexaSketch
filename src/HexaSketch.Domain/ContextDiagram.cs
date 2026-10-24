namespace HexaSketch.Domain;

public enum ElementType { System, PrimaryActor, SecondaryActor, ExternalSystem, Storage }

public enum Direction { OneWay, TwoWay }

public sealed record ContextElement(
    string Id,
    ElementType Type,
    string Name,
    string? Description,
    string? Technology,
    double? X,
    double? Y,
    bool Pinned);

public sealed record ContextRelationship(
    string Id,
    string From,
    string To,
    string Label,
    string? Technology,
    Direction Direction);

/// <summary>A C4 system context diagram, as drawn in Context Sketch. Read-only once imported.</summary>
public sealed record ContextDiagram(
    string Title,
    IReadOnlyList<ContextElement> Elements,
    IReadOnlyList<ContextRelationship> Relationships)
{
    public ContextElement System => Elements.Single(e => e.Type == ElementType.System);

    public ContextElement? Find(string elementId) => Elements.FirstOrDefault(e => e.Id == elementId);

    /// <summary>The relationship linking an element to the system, if there is one.</summary>
    public ContextRelationship? RelationshipOf(string elementId) =>
        Relationships.FirstOrDefault(r => r.From == elementId || r.To == elementId);

    /// <summary>Context Sketch's own load rules. An empty list means the diagram is usable.</summary>
    public IReadOnlyList<string> Problems()
    {
        var problems = new List<string>();

        var ids = Elements.Select(e => e.Id).Concat(Relationships.Select(r => r.Id));
        foreach (var duplicate in ids.GroupBy(id => id).Where(g => g.Count() > 1))
            problems.Add($"Id '{duplicate.Key}' is used more than once.");

        var systems = Elements.Count(e => e.Type == ElementType.System);
        if (systems != 1)
            problems.Add($"There must be exactly one system, but there are {systems}.");

        foreach (var element in Elements.Where(e => string.IsNullOrWhiteSpace(e.Name)))
            problems.Add($"Element '{element.Id}' has no name.");

        var elementIds = Elements.Select(e => e.Id).ToHashSet();
        var systemIds = Elements.Where(e => e.Type == ElementType.System).Select(e => e.Id).ToHashSet();
        foreach (var r in Relationships)
        {
            if (!elementIds.Contains(r.From) || !elementIds.Contains(r.To))
                problems.Add($"Relationship '{r.Id}' refers to an element that does not exist.");
            else if (r.From == r.To)
                problems.Add($"Relationship '{r.Id}' links an element to itself.");
            else if (!systemIds.Contains(r.From) && !systemIds.Contains(r.To))
                problems.Add($"Relationship '{r.Id}' does not have the system at one end.");
        }

        foreach (var element in Elements.Where(e => e.Type != ElementType.System))
        {
            var count = Relationships.Count(r => r.From == element.Id || r.To == element.Id);
            if (count != 1)
                problems.Add($"Element '{element.Name}' must have exactly one relationship, but has {count}.");
        }

        return problems;
    }
}
