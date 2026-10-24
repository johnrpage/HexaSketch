namespace HexaSketch.Domain;

/// <summary>Which side of the hexagon a port is on. Left is driving, right is driven.</summary>
public enum Side { Driving, Driven }

/// <summary>A port: a named role the application plays or needs. Adapters plug into it.</summary>
public sealed class Port
{
    private readonly List<Adapter> adapters = [];

    internal Port(string id, string name, Side side, bool isUser)
    {
        Id = id;
        Name = name;
        Side = side;
        IsUser = isUser;
    }

    public string Id { get; }
    public string Name { get; internal set; }
    public string? Description { get; internal set; }
    public Side Side { get; }

    /// <summary>The User port: always present, always driving, never deleted.</summary>
    public bool IsUser { get; }

    public IReadOnlyList<Adapter> Adapters => adapters;

    internal void Insert(Adapter adapter, int index) =>
        adapters.Insert(Math.Clamp(index, 0, adapters.Count), adapter);

    internal bool Remove(Adapter adapter) => adapters.Remove(adapter);

    internal Port Clone()
    {
        var copy = new Port(Id, Name, Side, IsUser) { Description = Description };
        copy.adapters.AddRange(adapters.Select(a => a.Clone()));
        return copy;
    }
}
