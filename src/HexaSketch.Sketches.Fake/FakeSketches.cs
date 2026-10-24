using HexaSketch.Application;
using HexaSketch.Domain;

namespace HexaSketch.Sketches.Fake;

/// <summary>Sketches adapter. In memory, for fast specs. Keeps copies, so later edits don't leak into saved sketches.</summary>
public sealed class FakeSketches : HexaSketch.Application.Sketches
{
    private readonly Dictionary<string, Sketch> saved = [];

    public IReadOnlyCollection<string> Locations => saved.Keys;

    public void Save(string location, Sketch sketch) => saved[location] = sketch.Clone();

    public Sketch Load(string location) =>
        saved.TryGetValue(location, out var sketch)
            ? sketch.Clone()
            : throw new UnreadableException($"There is no sketch at '{location}'.");
}
