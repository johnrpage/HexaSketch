using System.Windows;
using HexaSketch.Application;
using HexaSketch.ContextDiagrams.FileSystem;
using HexaSketch.Exports.FileSystem;
using HexaSketch.Sketches.FileSystem;

namespace HexaSketch.Wpf;

/// <summary>Composition root: chooses the real adapters and hands them to the application.</summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var session = new SketchSession(
            new FileSystemContextDiagrams(),
            new FileSystemSketches(),
            new FileSystemExports());
        new MainWindow(session, session).Show();
    }
}
