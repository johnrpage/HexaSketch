using HexaSketch.Application.Diagram;

namespace HexaSketch.Application;

/// <summary>Driving port, query half: everything the user can see about a sketch.</summary>
public interface UserQueries
{
    /// <summary>Raised after anything a query would answer differently.</summary>
    event EventHandler? Changed;

    /// <summary>The open sketch, or null when there isn't one.</summary>
    SketchView? Sketch { get; }

    /// <summary>The open sketch laid out for drawing, or null when there isn't one.</summary>
    DiagramView? Diagram { get; }

    /// <summary>Where the open sketch was last saved or opened from; null if never saved.</summary>
    string? Location { get; }

    bool HasUnsavedChanges { get; }
    bool CanUndo { get; }
    bool CanRedo { get; }

    /// <summary>What an export would warn about (unplaced items) or be blocked by (plan problems).</summary>
    ExportCheck CheckExport();
}
