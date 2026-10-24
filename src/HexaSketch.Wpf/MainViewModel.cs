using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HexaSketch.Application;
using HexaSketch.Application.Diagram;
using HexaSketch.Domain;

namespace HexaSketch.Wpf;

/// <summary>Turns clicks, drags and menu choices into User tasks, and shows what the User queries say.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private const string SketchExtension = ".hexasketch.json";

    private readonly UserTasks tasks;
    private readonly UserQueries queries;
    private readonly Dialogs dialogs;
    private bool loading;

    public MainViewModel(UserTasks tasks, UserQueries queries, Dialogs dialogs)
    {
        this.tasks = tasks;
        this.queries = queries;
        this.dialogs = dialogs;
        queries.Changed += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>A new or opened sketch has arrived: the canvas should fit it to the view.</summary>
    public event EventHandler? SketchLoaded;

    /// <summary>The user wants to rename the selection: the window should focus its name box.</summary>
    public event EventHandler? RenameRequested;

    public ObservableCollection<UnplacedItemView> Unplaced { get; } = [];

    public IReadOnlyList<AdapterKind> Kinds { get; } = Enum.GetValues<AdapterKind>();

    [ObservableProperty] private DiagramView? diagram;
    [ObservableProperty] private string windowTitle = "HexaSketch";
    [ObservableProperty] private string status = "";
    [ObservableProperty] private string? selectedId;
    [ObservableProperty] private PortView? selectedPort;
    [ObservableProperty] private AdapterView? selectedAdapter;
    [ObservableProperty] private string portName = "";
    [ObservableProperty] private string? portDescription;
    [ObservableProperty] private string adapterName = "";
    [ObservableProperty] private string? adapterTechnology;
    [ObservableProperty] private string? adapterDescription;
    [ObservableProperty] private AdapterKind adapterKind;

    public bool HasSketch => queries.Sketch is not null;
    public bool IsPortSelected => SelectedPort is not null;
    public bool IsAdapterSelected => SelectedAdapter is not null;
    public bool IsNothingSelected => HasSketch && !IsPortSelected && !IsAdapterSelected;

    public string PortDetails => SelectedPort is not { } p ? "" :
        $"{(p.Side == Side.Driving ? "Driving" : "Driven")} port{(p.IsUser ? " (the User port: always present)" : "")}" +
        $"{Environment.NewLine}Interface: {(p.IsUser ? "UserTasks, UserQueries" : Code(p.CodeName))}";

    public string AdapterDetails => SelectedAdapter is not { } a ? "" :
        (a.Source is { } s ? $"From the context diagram: {s.Name} ({TypeText(s.Type)})" : "Added in the sketch") +
        $"{Environment.NewLine}Code name: {Code(a.CodeName)}";

    public string SystemDetails => queries.Sketch is not { } s ? "" :
        $"{s.SystemName}{(s.SystemTechnology is null ? "" : $" ({s.SystemTechnology})")}" +
        (s.SystemDescription is null ? "" : $"{Environment.NewLine}{Environment.NewLine}{s.SystemDescription}");

    public void Select(string? id)
    {
        SelectedId = id;
        Refresh();
    }

    public void RequestRename()
    {
        if (IsPortSelected || IsAdapterSelected) RenameRequested?.Invoke(this, EventArgs.Empty);
    }

    public void PlaceItem(string elementId, string portId, int? index) =>
        Try(() => Select(tasks.Place(elementId, portId, index)));

    public void MoveAdapter(string adapterId, string portId, int? index) =>
        Try(() => tasks.MoveAdapter(adapterId, portId, index));

    public void RemoveAdapter(string adapterId) => Try(() => tasks.RemoveAdapter(adapterId));

    /// <summary>Moves a port to where another port on the same side is.</summary>
    public void MovePortTo(string portId, string targetPortId)
    {
        if (queries.Sketch is not { } sketch || portId == targetPortId) return;
        var port = sketch.Ports.Single(p => p.Id == portId);
        var sameSide = sketch.Ports.Where(p => p.Side == port.Side).ToList();
        var index = sameSide.FindIndex(p => p.Id == targetPortId);
        if (index >= 0) Try(() => tasks.MovePort(portId, index));
    }

    public void AddAdapterTo(string portId) => Try(() =>
    {
        Select(tasks.AddAdapter(portId));
        RequestRename();
    });

    /// <summary>Asks about unsaved work. False means the user cancelled whatever they were about to do.</summary>
    public bool ConfirmDiscard()
    {
        if (!queries.HasUnsavedChanges) return true;
        switch (dialogs.AskToSave())
        {
            case null: return false;
            case false: return true;
            default:
                Save();
                return !queries.HasUnsavedChanges;
        }
    }

    /// <summary>Opens a file named on the command line: a context diagram starts a sketch, anything else is a sketch.</summary>
    public void OpenFromCommandLine(string path)
    {
        if (path.EndsWith(".contextsketch.json", StringComparison.OrdinalIgnoreCase))
            Loaded(tasks.StartFrom(path), "Can't start from this context diagram");
        else
            Loaded(tasks.Open(path), "Can't open this sketch");
    }

    [RelayCommand]
    private void NewFromContextDiagram()
    {
        if (!ConfirmDiscard()) return;
        var path = dialogs.OpenFile("Start a sketch from a context diagram", Dialogs.ContextDiagramFilter);
        if (path is null) return;
        Loaded(tasks.StartFrom(path), "Can't start from this context diagram");
    }

    [RelayCommand]
    private void Open()
    {
        if (!ConfirmDiscard()) return;
        var path = dialogs.OpenFile("Open a sketch", Dialogs.SketchFilter);
        if (path is null) return;
        Loaded(tasks.Open(path), "Can't open this sketch");
    }

    [RelayCommand(CanExecute = nameof(HasSketch))]
    private void Save()
    {
        if (queries.Location is null) SaveAs();
        else Report(tasks.Save(), "Can't save the sketch");
    }

    [RelayCommand(CanExecute = nameof(HasSketch))]
    private void SaveAs()
    {
        var path = dialogs.SaveFile("Save the sketch", Dialogs.SketchFilter, FileStem() + SketchExtension);
        if (path is null) return;
        if (!path.EndsWith(SketchExtension, StringComparison.OrdinalIgnoreCase))
            path = (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? path[..^5] : path) + SketchExtension;
        Report(tasks.SaveAs(path), "Can't save the sketch");
    }

    [RelayCommand(CanExecute = nameof(HasSketch))]
    private void ExportDiagram()
    {
        if (!AcceptUnplaced(queries.CheckExport())) return;
        var path = dialogs.SaveFile("Export the diagram", Dialogs.SvgFilter, FileStem() + ".svg");
        if (path is null) return;
        if (Report(tasks.ExportDiagram(path), "Can't export the diagram")) Status = $"Diagram exported to {path}";
    }

    [RelayCommand(CanExecute = nameof(HasSketch))]
    private void ExportPlan()
    {
        var check = queries.CheckExport();
        if (check.PlanProblems.Count > 0)
        {
            dialogs.ShowProblems("The plan can't be exported yet", check.PlanProblems);
            return;
        }
        if (!AcceptUnplaced(check)) return;
        var path = dialogs.SaveFile("Export the plan", Dialogs.MarkdownFilter, FileStem() + " - plan.md");
        if (path is null) return;
        if (Report(tasks.ExportPlan(path), "Can't export the plan")) Status = $"Plan exported to {path}";
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo() => tasks.Undo();

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo() => tasks.Redo();

    [RelayCommand(CanExecute = nameof(HasSketch))]
    private void AddDrivingPort() => AddPort(Side.Driving);

    [RelayCommand(CanExecute = nameof(HasSketch))]
    private void AddDrivenPort() => AddPort(Side.Driven);

    [RelayCommand(CanExecute = nameof(CanAddAdapter))]
    private void AddAdapter()
    {
        var portId = SelectedPort?.Id ?? SelectedAdapter?.PortId;
        if (portId is not null) AddAdapterTo(portId);
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        if (SelectedAdapter is { } adapter)
        {
            RemoveAdapter(adapter.Id);
            return;
        }
        if (SelectedPort is not { } port) return;
        if (port.IsUser)
        {
            dialogs.Warn("The User port is always there, so it can't be deleted.");
            return;
        }
        if (port.Adapters.Count > 0 && !dialogs.Confirm(
                $"Delete the port '{port.Name}' and its {port.Adapters.Count} adapter(s)?{Environment.NewLine}{Environment.NewLine}" +
                "Adapters from the context diagram go back to the list; adapters added by hand are deleted.",
                "Delete port"))
            return;
        Try(() => tasks.DeletePort(port.Id));
    }

    private bool CanUndo() => queries.CanUndo;
    private bool CanRedo() => queries.CanRedo;
    private bool CanAddAdapter() => IsPortSelected || IsAdapterSelected;
    private bool CanDelete() => IsAdapterSelected || (SelectedPort is { IsUser: false });

    private void AddPort(Side side) => Try(() =>
    {
        Select(tasks.AddPort(side));
        RequestRename();
    });

    partial void OnPortNameChanged(string value) =>
        Edit(SelectedPort, p => value.Trim() != p.Name, p =>
        {
            if (string.IsNullOrWhiteSpace(value)) Refresh();
            else tasks.RenamePort(p.Id, value);
        });

    partial void OnPortDescriptionChanged(string? value) =>
        Edit(SelectedPort, p => Tidy(value) != p.Description, p => tasks.DescribePort(p.Id, value));

    partial void OnAdapterNameChanged(string value) =>
        Edit(SelectedAdapter, a => value.Trim() != a.Name, a =>
        {
            if (string.IsNullOrWhiteSpace(value)) Refresh();
            else tasks.RenameAdapter(a.Id, value);
        });

    partial void OnAdapterTechnologyChanged(string? value) =>
        Edit(SelectedAdapter, a => Tidy(value) != a.Technology, a => tasks.SetAdapterTechnology(a.Id, value));

    partial void OnAdapterDescriptionChanged(string? value) =>
        Edit(SelectedAdapter, a => Tidy(value) != a.Description, a => tasks.DescribeAdapter(a.Id, value));

    partial void OnAdapterKindChanged(AdapterKind value) =>
        Edit(SelectedAdapter, a => value != a.Kind, a => tasks.SetAdapterKind(a.Id, value));

    private void Edit<T>(T? selected, Func<T, bool> changed, Action<T> apply) where T : class
    {
        if (loading || selected is null || !changed(selected)) return;
        Try(() => apply(selected));
    }

    private void Refresh()
    {
        var sketch = queries.Sketch;
        Diagram = queries.Diagram;

        Unplaced.Clear();
        foreach (var item in sketch?.Unplaced ?? []) Unplaced.Add(item);

        SelectedPort = sketch?.Ports.FirstOrDefault(p => p.Id == SelectedId);
        SelectedAdapter = sketch?.Ports.SelectMany(p => p.Adapters).FirstOrDefault(a => a.Id == SelectedId);
        if (SelectedPort is null && SelectedAdapter is null) SelectedId = null;

        loading = true;
        PortName = SelectedPort?.Name ?? "";
        PortDescription = SelectedPort?.Description;
        AdapterName = SelectedAdapter?.Name ?? "";
        AdapterTechnology = SelectedAdapter?.Technology;
        AdapterDescription = SelectedAdapter?.Description;
        AdapterKind = SelectedAdapter?.Kind ?? AdapterKind.Real;
        loading = false;

        var name = queries.Location is { } location ? Path.GetFileName(location) : sketch?.Title;
        WindowTitle = sketch is null ? "HexaSketch" : $"HexaSketch - {name}{(queries.HasUnsavedChanges ? " *" : "")}";
        Status = sketch is null ? "Start from a context diagram: File > New from context diagram." : StatusOf(queries.CheckExport());

        OnPropertyChanged(nameof(HasSketch));
        OnPropertyChanged(nameof(IsPortSelected));
        OnPropertyChanged(nameof(IsAdapterSelected));
        OnPropertyChanged(nameof(IsNothingSelected));
        OnPropertyChanged(nameof(PortDetails));
        OnPropertyChanged(nameof(AdapterDetails));
        OnPropertyChanged(nameof(SystemDetails));
        foreach (var command in new IRelayCommand[]
                 {
                     SaveCommand, SaveAsCommand, ExportDiagramCommand, ExportPlanCommand, UndoCommand, RedoCommand,
                     AddDrivingPortCommand, AddDrivenPortCommand, AddAdapterCommand, DeleteCommand,
                 })
            command.NotifyCanExecuteChanged();
    }

    private static string StatusOf(ExportCheck check)
    {
        var unplaced = check.Unplaced.Count switch
        {
            0 => "Every context diagram item is placed",
            1 => "1 item not placed",
            var n => $"{n} items not placed",
        };
        var plan = check.PlanProblems.Count switch
        {
            0 => "plan ready to export",
            1 => "plan has 1 problem: " + check.PlanProblems[0],
            var n => $"plan has {n} problems, first: {check.PlanProblems[0]}",
        };
        return $"{unplaced} · {plan}";
    }

    private void Loaded(Outcome outcome, string failure)
    {
        if (!Report(outcome, failure)) return;
        SelectedId = null;
        Refresh();
        SketchLoaded?.Invoke(this, EventArgs.Empty);
    }

    private bool Report(Outcome outcome, string failure)
    {
        if (!outcome.Succeeded) dialogs.ShowProblems(failure, outcome.Problems);
        return outcome.Succeeded;
    }

    private bool AcceptUnplaced(ExportCheck check) =>
        check.Unplaced.Count == 0 || dialogs.Confirm(
            $"{check.Unplaced.Count} context diagram item(s) aren't on the sketch:{Environment.NewLine}" +
            string.Join(Environment.NewLine, check.Unplaced.Select(n => "• " + n)) +
            $"{Environment.NewLine}{Environment.NewLine}Export anyway? The plan lists them as not placed.",
            "Items not placed");

    private void Try(Action action)
    {
        try
        {
            action();
        }
        catch (SketchRuleException e)
        {
            dialogs.Warn(e.Message);
            Refresh();
        }
    }

    private string FileStem()
    {
        if (queries.Location is { } location)
        {
            var file = Path.GetFileName(location);
            return file.EndsWith(SketchExtension, StringComparison.OrdinalIgnoreCase) ? file[..^SketchExtension.Length] : Path.GetFileNameWithoutExtension(file);
        }
        var title = queries.Sketch?.SystemName ?? "Sketch";
        return string.Concat(title.Split(Path.GetInvalidFileNameChars()));
    }

    private static string Code(string codeName) => codeName.Length == 0 ? "(none: needs a name)" : codeName;

    private static string? Tidy(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static string TypeText(ElementType type) => type switch
    {
        ElementType.PrimaryActor => "primary actor",
        ElementType.SecondaryActor => "secondary actor",
        ElementType.ExternalSystem => "external system",
        ElementType.Storage => "storage",
        _ => "system",
    };
}
