using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HexaSketch.Application;

namespace HexaSketch.Wpf;

/// <summary>The WPF UI: the real adapter on the User port.</summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;
    private Point? itemPressedAt;

    public MainWindow(UserTasks tasks, UserQueries queries)
    {
        InitializeComponent();
        viewModel = new MainViewModel(tasks, queries, new Dialogs(this));
        DataContext = viewModel;

        viewModel.RenameRequested += (_, _) => Dispatcher.BeginInvoke(FocusName);
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.HasSketch)) ShowHint();
        };
        ShowHint();

        PreviewKeyDown += OnPreviewKeyDown;
        Surface.MouseRightButtonUp += Surface_RightClick;
    }

    public void OpenFromCommandLine(string path) => viewModel.OpenFromCommandLine(path);

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!viewModel.ConfirmDiscard()) e.Cancel = true;
        base.OnClosing(e);
    }

    private void ShowHint() => EmptyHint.Visibility = viewModel.HasSketch ? Visibility.Collapsed : Visibility.Visible;

    private void FocusName()
    {
        var box = viewModel.IsPortSelected ? PortNameBox : viewModel.IsAdapterSelected ? AdapterNameBox : null;
        if (box is null) return;
        box.Focus();
        box.SelectAll();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Enter commits a single-line edit; Delete deletes the selection unless a text box is busy.
        if (e.Key == Key.Enter && e.OriginalSource is TextBox { AcceptsReturn: false } box)
        {
            box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            Surface.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete && e.OriginalSource is not TextBox && viewModel.DeleteCommand.CanExecute(null))
        {
            viewModel.DeleteCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Surface_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (!viewModel.IsPortSelected && !viewModel.IsAdapterSelected) return;
        var menu = new ContextMenu();
        if (viewModel.SelectedPort is { } port)
        {
            menu.Items.Add(Item("Add adapter", () => viewModel.AddAdapterTo(port.Id)));
            menu.Items.Add(Item("Rename", viewModel.RequestRename));
            if (!port.IsUser) menu.Items.Add(Item("Delete port", () => viewModel.DeleteCommand.Execute(null)));
        }
        else
        {
            menu.Items.Add(Item("Rename", viewModel.RequestRename));
            menu.Items.Add(Item("Delete adapter", () => viewModel.DeleteCommand.Execute(null)));
        }
        menu.IsOpen = true;
        e.Handled = true;

        static MenuItem Item(string header, Action action)
        {
            var item = new MenuItem { Header = header };
            item.Click += (_, _) => action();
            return item;
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void ItemList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        itemPressedAt = e.GetPosition(ItemList);

    private void ItemList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (itemPressedAt is not { } from || e.LeftButton != MouseButtonState.Pressed) return;
        var at = e.GetPosition(ItemList);
        if (Math.Abs(at.X - from.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(at.Y - from.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        itemPressedAt = null;
        if (ItemUnder(e.OriginalSource as DependencyObject) is { } item)
            DragDrop.DoDragDrop(ItemList, new DataObject(DragFormats.Item, item.ElementId), DragDropEffects.Move);
    }

    private void ItemList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DragFormats.Adapter) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void ItemList_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DragFormats.Adapter) is string adapterId) viewModel.RemoveAdapter(adapterId);
        e.Handled = true;
    }

    private static UnplacedItemView? ItemUnder(DependencyObject? source)
    {
        for (var node = source; node is not null; node = VisualTreeHelper.GetParent(node))
            if (node is ListBoxItem { DataContext: UnplacedItemView item }) return item;
        return null;
    }
}
