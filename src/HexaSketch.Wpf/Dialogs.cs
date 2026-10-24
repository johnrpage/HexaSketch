using System.Windows;
using Microsoft.Win32;

namespace HexaSketch.Wpf;

/// <summary>File pickers and message boxes, kept out of the view model so it reads as plain decisions.</summary>
public sealed class Dialogs(Window owner)
{
    public const string ContextDiagramFilter = "Context Sketch diagrams (*.contextsketch.json)|*.contextsketch.json|JSON files (*.json)|*.json|All files (*.*)|*.*";
    public const string SketchFilter = "HexaSketch sketches (*.hexasketch.json)|*.hexasketch.json|All files (*.*)|*.*";
    public const string SvgFilter = "SVG images (*.svg)|*.svg";
    public const string MarkdownFilter = "Markdown files (*.md)|*.md";

    public string? OpenFile(string title, string filter)
    {
        var dialog = new OpenFileDialog { Title = title, Filter = filter };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public string? SaveFile(string title, string filter, string suggestedName)
    {
        var dialog = new SaveFileDialog { Title = title, Filter = filter, FileName = suggestedName, AddExtension = false };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public bool Confirm(string message, string title) =>
        MessageBox.Show(owner, message, title, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;

    /// <summary>Asks whether to save unsaved changes: true to save, false to discard, null to cancel.</summary>
    public bool? AskToSave() =>
        MessageBox.Show(owner, "Save changes to this sketch first?", "HexaSketch",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Question) switch
        {
            MessageBoxResult.Yes => true,
            MessageBoxResult.No => false,
            _ => null,
        };

    public void ShowProblems(string title, IEnumerable<string> problems) =>
        MessageBox.Show(owner, string.Join(Environment.NewLine, problems.Select(p => "• " + p)), title,
            MessageBoxButton.OK, MessageBoxImage.Warning);

    public void Warn(string message) =>
        MessageBox.Show(owner, message, "HexaSketch", MessageBoxButton.OK, MessageBoxImage.Information);
}
