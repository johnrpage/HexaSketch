namespace HexaSketch.Application;

/// <summary>The application: implements the User port on top of the domain and the driven ports.</summary>
public sealed class SketchSession(ContextDiagrams contextDiagrams, Sketches sketches, Exports exports)
    : UserTasks, UserQueries
{
    private readonly ContextDiagrams contextDiagrams = contextDiagrams;
    private readonly Sketches sketches = sketches;
    private readonly Exports exports = exports;
}
