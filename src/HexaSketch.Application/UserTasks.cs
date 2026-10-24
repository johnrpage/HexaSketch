using HexaSketch.Domain;

namespace HexaSketch.Application;

/// <summary>Driving port, command half: everything the user can do to a sketch.</summary>
/// <remarks>Every editing task is one undo step. Ids are those shown by <see cref="UserQueries"/>.</remarks>
public interface UserTasks
{
    Outcome StartFrom(string contextDiagramLocation);
    Outcome Open(string sketchLocation);
    Outcome Save();
    Outcome SaveAs(string sketchLocation);
    void Close();

    Outcome ExportDiagram(string location);
    Outcome ExportPlan(string location);

    string AddPort(Side side);
    void RenamePort(string portId, string name);
    void DescribePort(string portId, string? description);
    void DeletePort(string portId);
    void MovePort(string portId, int indexOnSide);

    string Place(string elementId, string portId, int? index = null);
    string AddAdapter(string portId, AdapterKind kind = AdapterKind.Real);

    /// <summary>Makes a port named after a context diagram element and places the element on it. One undo step.</summary>
    string PlaceOnNewPort(string elementId, Side side);
    void MoveAdapter(string adapterId, string portId, int? index = null);
    void RemoveAdapter(string adapterId);
    void RenameAdapter(string adapterId, string name);
    void SetAdapterTechnology(string adapterId, string? technology);
    void DescribeAdapter(string adapterId, string? description);
    void SetAdapterKind(string adapterId, AdapterKind kind);

    void Undo();
    void Redo();
}
