using HexaSketch.Domain;

namespace HexaSketch.Application;

/// <summary>Driven port: where context diagrams (the starting point of a sketch) come from.</summary>
public interface ContextDiagrams
{
    /// <summary>Reads a context diagram. Throws <see cref="UnreadableException"/> if it can't be read.</summary>
    ContextDiagram Load(string location);
}
