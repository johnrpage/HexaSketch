namespace HexaSketch.Application;

/// <summary>Driven port: where the exported diagram and plan are written.</summary>
public interface Exports
{
    void WriteDiagram(string location, string svg);
    void WritePlan(string location, string markdown);
}
