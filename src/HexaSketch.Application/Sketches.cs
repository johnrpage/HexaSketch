using HexaSketch.Domain;

namespace HexaSketch.Application;

/// <summary>Driven port: where sketches are saved and loaded.</summary>
public interface Sketches
{
    void Save(string location, Sketch sketch);

    /// <summary>Reads a sketch. Throws <see cref="UnreadableException"/> if it can't be read.</summary>
    Sketch Load(string location);
}
