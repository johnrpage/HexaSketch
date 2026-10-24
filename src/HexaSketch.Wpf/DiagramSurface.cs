using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using HexaSketch.Application.Diagram;
using HexaSketch.Domain;

namespace HexaSketch.Wpf;

/// <summary>What a drag carries: a list item, an adapter or a port.</summary>
public static class DragFormats
{
    public const string Item = "HexaSketch.Item";
    public const string Adapter = "HexaSketch.Adapter";
    public const string Port = "HexaSketch.Port";
}

/// <summary>
/// Draws the laid-out diagram and turns mouse work into view model calls: click to select,
/// double-click to rename, drag adapters and ports about, drop list items on ports (or on the
/// hexagon, to make a new port), drag the background to pan and use the wheel to zoom.
/// </summary>
public sealed class DiagramSurface : Border
{
    private sealed record Hit(string Kind, string Id, string PortId, int Index);

    private const string HexagonKind = "Hexagon";
    private static readonly Hit HexagonHit = new(HexagonKind, HexagonKind, "", -1);

    private static readonly Brush Ink = Brush(SvgDiagram.Ink);
    private static readonly Brush HexagonFill = Brush(SvgDiagram.HexagonFill);
    private static readonly Brush CoreFill = Brush(SvgDiagram.CoreFill);
    private static readonly Brush UserFill = Brush(SvgDiagram.UserFill);
    private static readonly Brush UserInk = Brush(SvgDiagram.UserInk);
    private static readonly Brush PortInk = Brush(SvgDiagram.PortInk);
    private static readonly Brush RealFill = Brush(SvgDiagram.RealFill);
    private static readonly Brush StandInInk = Brush(SvgDiagram.StandInInk);
    private static readonly Brush Muted = Brush(SvgDiagram.Muted);
    private static readonly Brush Highlight = Brush("#f39c12");
    private static readonly FontFamily Font = new("Segoe UI");

    private readonly Canvas content = new();
    private readonly ScaleTransform scale = new(1, 1);
    private readonly TranslateTransform offset = new();
    private readonly Dictionary<string, Shape> dropTargets = [];
    private Point? panFrom;
    private (Hit Hit, Point At)? pressed;
    private Shape? hovered;
    private TextBlock? dropHint;
    private bool autoFit = true;

    public DiagramSurface()
    {
        Background = Brushes.White;
        ClipToBounds = true;
        Focusable = true;
        FocusVisualStyle = null;
        AllowDrop = true;
        content.RenderTransform = new TransformGroup { Children = { scale, offset } };
        Child = content;

        DataContextChanged += (_, _) => Attach();
        SizeChanged += (_, _) => { if (autoFit) Fit(); };
        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += (_, _) => { panFrom = null; pressed = null; ReleaseMouseCapture(); };
        MouseRightButtonDown += (_, e) =>
            ViewModel?.Select(HitAt(e.GetPosition(this)) is { Kind: not HexagonKind } hit ? hit.Id : null);
        MouseWheel += OnWheel;
        DragOver += OnDragOver;
        DragLeave += (_, _) => { Hover(null); ShowDropHint(null); };
        Drop += OnDrop;
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    /// <summary>Fits the diagram to the window, and keeps it fitted as it changes until the user pans or zooms.</summary>
    public void FitToWindow()
    {
        autoFit = true;
        Fit();
    }

    private void Fit()
    {
        if (ViewModel?.Diagram is not { } view || ActualWidth < 1 || ActualHeight < 1) return;
        var b = view.Bounds;
        var s = Math.Min(1.2, Math.Min(ActualWidth / b.Width, ActualHeight / b.Height) * 0.95);
        scale.ScaleX = scale.ScaleY = s;
        offset.X = ActualWidth / 2 - b.Centre.X * s;
        offset.Y = ActualHeight / 2 - b.Centre.Y * s;
    }

    private void Attach()
    {
        if (ViewModel is not { } vm) return;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainViewModel.Diagram) or nameof(MainViewModel.SelectedId))
                Dispatcher.BeginInvoke(Rebuild);
        };
        vm.SketchLoaded += (_, _) => Dispatcher.BeginInvoke(FitToWindow);
        Rebuild();
    }

    private void Rebuild()
    {
        content.Children.Clear();
        dropTargets.Clear();
        hovered = null;
        if (ViewModel?.Diagram is not { } view) return;
        var selected = ViewModel.SelectedId;

        Add(Line(view.CentreLine, Muted, 1, dashed: true));
        DrawHexagon(view.Hexagon);
        foreach (var port in view.Ports) DrawPort(port, selected);
        Add(Text(view.Title, 18, Ink, bold: true), view.Bounds.X + 20, view.Bounds.Y + 30, LabelAlign.Start);
        if (autoFit) Fit();
    }

    private void DrawHexagon(HexagonShape h)
    {
        // An invisible wide outline makes the edge easy to drop on; the hexagon itself takes drops too.
        var edge = Polygon(h.Corners, Brushes.Transparent, Brushes.Transparent, 36);
        edge.IsHitTestVisible = true;
        edge.Tag = HexagonHit;
        Add(edge);
        var hexagon = Polygon(h.Corners, HexagonFill, Ink, 1.5);
        hexagon.IsHitTestVisible = true;
        hexagon.Tag = HexagonHit;
        Add(hexagon);
        dropTargets[HexagonKind] = hexagon;
        Add(Polygon(h.TasksRegion, UserFill, UserInk, 1));
        Add(Polygon(h.QueriesRegion, UserFill, UserInk, 1));
        Add(Line(h.TasksQueriesDivider, UserInk, 1, dashed: true));
        Add(Text("Tasks", 13, UserInk), h.TasksLabel.X, h.TasksLabel.Y);
        Add(Text("Queries", 13, UserInk), h.QueriesLabel.X, h.QueriesLabel.Y);
        var domain = new Ellipse
        {
            Width = h.DomainRadius * 2, Height = h.DomainRadius * 2,
            Fill = CoreFill, Stroke = Ink, StrokeThickness = 1.5, IsHitTestVisible = false,
        };
        Canvas.SetLeft(domain, h.DomainCentre.X - h.DomainRadius);
        Canvas.SetTop(domain, h.DomainCentre.Y - h.DomainRadius);
        Add(domain);
        Add(Text("Domain", 16, Ink, bold: true), h.DomainCentre.X, h.DomainCentre.Y);
    }

    private void DrawPort(PortShape port, string? selected)
    {
        var weight = port.IsUser ? 4 : 2;
        Add(Line(port.Stalk, Ink, weight));

        for (var i = 0; i < port.Adapters.Count; i++)
        {
            var a = port.Adapters[i];
            Add(Line(a.Link, Ink, 1));
            var real = a.Kind == AdapterKind.Real;
            var box = new Rectangle
            {
                Width = a.Box.Width, Height = a.Box.Height, RadiusX = 4, RadiusY = 4,
                Fill = real ? RealFill : Brushes.White,
                Stroke = a.Id == selected ? Highlight : real ? PortInk : StandInInk,
                StrokeThickness = a.Id == selected ? 3 : 1.2,
                StrokeDashArray = real ? null : [4, 2.5],
                Tag = new Hit(DragFormats.Adapter, a.Id, port.Id, i),
                Cursor = Cursors.Hand,
                ToolTip = a.Caption is null ? a.Name : $"{a.Name}\n{a.Caption}",
            };
            Canvas.SetLeft(box, a.Box.X);
            Canvas.SetTop(box, a.Box.Y);
            Add(box);
            dropTargets[a.Id] = box;

            var c = a.Box.Centre;
            var name = Text(a.Name, 13, Ink, bold: true, maxWidth: a.Box.Width - 12);
            if (a.Caption is null) Add(name, c.X, c.Y);
            else
            {
                Add(name, c.X, c.Y - 7);
                Add(Text(a.Caption, 11, StandInInk, maxWidth: a.Box.Width - 12), c.X, c.Y + 10);
            }
        }

        var circle = new Ellipse
        {
            Width = port.Radius * 2, Height = port.Radius * 2,
            Fill = Brushes.White,
            Stroke = port.Id == selected ? Highlight : PortInk,
            StrokeThickness = port.Id == selected ? weight + 2 : weight,
            Tag = new Hit(DragFormats.Port, port.Id, port.Id, -1),
            Cursor = Cursors.Hand,
            ToolTip = $"{port.Name} ({(port.Side == Side.Driving ? "driving" : "driven")} port)\nDrop items or adapters here",
        };
        Canvas.SetLeft(circle, port.Centre.X - port.Radius);
        Canvas.SetTop(circle, port.Centre.Y - port.Radius);
        Add(circle);
        dropTargets[port.Id] = circle;
        Add(Text(port.Name, 14, PortInk, bold: true), port.Label.X, port.Label.Y, port.LabelAlign);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        if (ViewModel is not { } vm) return;
        if (HitAt(e.GetPosition(this)) is { Kind: not HexagonKind } hit)
        {
            vm.Select(hit.Id);
            if (e.ClickCount == 2) vm.RequestRename();
            else pressed = (hit, e.GetPosition(this));
        }
        else
        {
            vm.Select(null);
            panFrom = e.GetPosition(this);
            CaptureMouse();
        }
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var at = e.GetPosition(this);
        if (panFrom is { } from && e.LeftButton == MouseButtonState.Pressed)
        {
            autoFit = false;
            offset.X += at.X - from.X;
            offset.Y += at.Y - from.Y;
            panFrom = at;
            return;
        }
        if (pressed is not { } press || e.LeftButton != MouseButtonState.Pressed) return;
        if (Math.Abs(at.X - press.At.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(at.Y - press.At.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        pressed = null;
        DragDrop.DoDragDrop(this, new DataObject(press.Hit.Kind, press.Hit.Id), DragDropEffects.Move);
        Hover(null);
    }

    private void OnWheel(object sender, MouseWheelEventArgs e)
    {
        var at = e.GetPosition(this);
        var factor = e.Delta > 0 ? 1.1 : 1 / 1.1;
        autoFit = false;
        var next = Math.Clamp(scale.ScaleX * factor, 0.2, 4);
        factor = next / scale.ScaleX;
        scale.ScaleX = scale.ScaleY = next;
        offset.X = at.X - (at.X - offset.X) * factor;
        offset.Y = at.Y - (at.Y - offset.Y) * factor;
        e.Handled = true;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        var target = Accepts(e.Data, HitAt(e.GetPosition(this)));
        e.Effects = target is null ? DragDropEffects.None : DragDropEffects.Move;
        Hover(target is null ? null : dropTargets.GetValueOrDefault(target.Id));
        ShowDropHint(target?.Kind == HexagonKind ? e.GetPosition(content) : null);
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        Hover(null);
        ShowDropHint(null);
        if (ViewModel is not { } vm || Accepts(e.Data, HitAt(e.GetPosition(this))) is not { } target) return;
        int? index = target.Index >= 0 ? target.Index : null;

        if (e.Data.GetData(DragFormats.Item) is string elementId && target.Kind == HexagonKind)
            vm.PlaceOnNewPort(elementId, SideAt(e.GetPosition(content)));
        else if (e.Data.GetData(DragFormats.Item) is string itemId)
            vm.PlaceItem(itemId, target.PortId, index);
        else if (e.Data.GetData(DragFormats.Adapter) is string adapterId)
            vm.MoveAdapter(adapterId, target.PortId, index);
        else if (e.Data.GetData(DragFormats.Port) is string portId)
            vm.MovePortTo(portId, target.PortId);
        e.Handled = true;
    }

    /// <summary>The left half of the hexagon is driving, the right half driven.</summary>
    private static Side SideAt(Point inDiagram) => inDiagram.X < 0 ? Side.Driving : Side.Driven;

    /// <summary>While an item is over the hexagon, says what dropping it there will make.</summary>
    private void ShowDropHint(Point? at)
    {
        if (at is not { } point)
        {
            if (dropHint is not null) content.Children.Remove(dropHint);
            dropHint = null;
            return;
        }
        if (dropHint is null)
        {
            dropHint = Text("", 13, Highlight, bold: true);
            content.Children.Add(dropHint);
        }
        // Above the cursor, reaching in towards the hexagon so it never runs off the canvas edge.
        var driving = SideAt(point) == Side.Driving;
        dropHint.Text = driving ? "+ new driving port" : "+ new driven port";
        dropHint.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(dropHint, driving ? point.X + 4 : point.X - 4 - dropHint.DesiredSize.Width);
        Canvas.SetTop(dropHint, point.Y - 8 - dropHint.DesiredSize.Height);
    }

    /// <summary>
    /// Items drop on ports or adapters (to go at that position) or on the hexagon (to make a new
    /// port); adapters drop on ports or adapters; ports drop on ports.
    /// </summary>
    private static Hit? Accepts(IDataObject data, Hit? target)
    {
        if (target is null) return null;
        if (data.GetDataPresent(DragFormats.Item)) return target;
        if (target.Kind == HexagonKind) return null;
        if (data.GetData(DragFormats.Adapter) is string adapterId) return adapterId == target.Id ? null : target;
        if (data.GetData(DragFormats.Port) is string portId)
            return target.Kind == DragFormats.Port && portId != target.Id ? target : null;
        return null;
    }

    private Hit? HitAt(Point at)
    {
        Hit? hit = null;
        VisualTreeHelper.HitTest(this, null, result =>
        {
            if (result.VisualHit is FrameworkElement { Tag: Hit h })
            {
                hit = h;
                return HitTestResultBehavior.Stop;
            }
            return HitTestResultBehavior.Continue;
        }, new PointHitTestParameters(at));
        return hit;
    }

    private void Hover(Shape? shape)
    {
        if (hovered == shape) return;
        if (hovered is not null) hovered.Effect = null;
        hovered = shape;
        if (shape is not null)
            shape.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Orange, ShadowDepth = 0, BlurRadius = 14, Opacity = 1,
            };
    }

    private void Add(UIElement element) => content.Children.Add(element);

    private void Add(TextBlock text, double x, double y, LabelAlign align = LabelAlign.Middle)
    {
        text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = text.DesiredSize;
        Canvas.SetLeft(text, align switch
        {
            LabelAlign.Start => x,
            LabelAlign.End => x - size.Width,
            _ => x - size.Width / 2,
        });
        Canvas.SetTop(text, y - size.Height / 2);
        content.Children.Add(text);
    }

    private static TextBlock Text(string text, double size, Brush brush, bool bold = false, double maxWidth = double.PositiveInfinity) => new()
    {
        Text = text,
        FontFamily = Font,
        FontSize = size,
        FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
        Foreground = brush,
        MaxWidth = maxWidth,
        TextTrimming = TextTrimming.CharacterEllipsis,
        IsHitTestVisible = false,
    };

    private static Line Line(DiagramLine line, Brush brush, double thickness, bool dashed = false) => new()
    {
        X1 = line.From.X, Y1 = line.From.Y, X2 = line.To.X, Y2 = line.To.Y,
        Stroke = brush,
        StrokeThickness = thickness,
        StrokeDashArray = dashed ? [5, 4] : null,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
        IsHitTestVisible = false,
    };

    private static Polygon Polygon(IEnumerable<DiagramPoint> points, Brush fill, Brush stroke, double thickness) => new()
    {
        Points = new PointCollection(points.Select(p => new Point(p.X, p.Y))),
        Fill = fill,
        Stroke = stroke,
        StrokeThickness = thickness,
        StrokeLineJoin = PenLineJoin.Round,
        IsHitTestVisible = false,
    };

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
