using System.Windows;
using HexaSketch.Application;

namespace HexaSketch.Wpf;

/// <summary>The WPF UI: the real adapter on the User port.</summary>
public partial class MainWindow : Window
{
    public MainWindow(UserTasks tasks, UserQueries queries)
    {
        InitializeComponent();
    }
}
