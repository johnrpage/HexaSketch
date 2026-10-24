using HexaSketch.Application;

namespace HexaSketch.Exports.Fake;

/// <summary>Exports adapter. In memory, for fast specs: what was written, by location.</summary>
public sealed class FakeExports : HexaSketch.Application.Exports
{
    public Dictionary<string, string> Diagrams { get; } = [];
    public Dictionary<string, string> Plans { get; } = [];

    public void WriteDiagram(string location, string svg) => Diagrams[location] = svg;

    public void WritePlan(string location, string markdown) => Plans[location] = markdown;
}
