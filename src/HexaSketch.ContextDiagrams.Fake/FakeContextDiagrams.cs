using HexaSketch.Application;
using HexaSketch.Domain;

namespace HexaSketch.ContextDiagrams.Fake;

/// <summary>ContextDiagrams adapter. In memory, for fast specs: add diagrams, then load them by location.</summary>
public sealed class FakeContextDiagrams : HexaSketch.Application.ContextDiagrams
{
    private readonly Dictionary<string, ContextDiagram> diagrams = [];

    public void Add(string location, ContextDiagram diagram) => diagrams[location] = diagram;

    public ContextDiagram Load(string location) =>
        diagrams.TryGetValue(location, out var diagram)
            ? diagram
            : throw new UnreadableException($"There is no context diagram at '{location}'.");
}
