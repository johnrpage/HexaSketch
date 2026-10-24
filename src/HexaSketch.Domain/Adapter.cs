namespace HexaSketch.Domain;

/// <summary>Real adapters are the production ones; Fake and Simulated stand in for them.</summary>
public enum AdapterKind { Real, Fake, Simulated }

/// <summary>An adapter: one concrete way of plugging something into a port.</summary>
public sealed class Adapter
{
    internal Adapter(string id, string name, AdapterKind kind, string? sourceElementId)
    {
        Id = id;
        Name = name;
        Kind = kind;
        SourceElementId = sourceElementId;
    }

    public string Id { get; }
    public string Name { get; internal set; }
    public string? Technology { get; internal set; }
    public string? Description { get; internal set; }
    public AdapterKind Kind { get; internal set; }

    /// <summary>The context diagram element this adapter came from, or null if it was added by hand.</summary>
    public string? SourceElementId { get; }

    internal Adapter Clone() => new(Id, Name, Kind, SourceElementId)
    {
        Technology = Technology,
        Description = Description,
    };
}
