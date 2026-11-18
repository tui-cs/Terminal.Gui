#nullable enable
using System.Text;
namespace UICatalog.Scenarios;

// ReSharper disable AccessToDisposedClosure

/// <summary>
///     Demonstrates creating and drawing regions through mouse dragging.
/// </summary>
[ScenarioMetadata ("Regions", "Region Tester")]
[ScenarioCategory ("Mouse and Keyboard")]
[ScenarioCategory ("Drawing")]
public class RegionScenario : Scenario
{
    private readonly Region _region = new ();
    private Point? _dragStart;
    private bool _isDragging;

    private readonly Rune? _previewFillRune = Glyphs.Stipple;

    private RegionDrawStyles _drawStyle;
    private RegionOp _regionOp;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.TabStop = TabBehavior.TabGroup;
        appWindow.Padding.Thickness = new (1);

        RegionToolsView tools = new () { Title = "Tools", X = Pos.AnchorEnd (), Y = 2 };

        tools.CurrentAttribute = appWindow.GetAttributeForRole (VisualRole.HotNormal);

        tools.SetStyle += b =>
                          {
                              _drawStyle = b;
                              appWindow.SetNeedsDraw ();
                          };

        tools.RegionOpChanged += (_, e) => { _regionOp = e; };

        //tools.AddLayer += () => canvas.AddLayer ();

        appWindow.Add (tools);

        // Add drag handling to window
        appWindow.MouseEvent += (_, e) =>
                                {
                                    if (e.Flags.HasFlag (MouseFlags.LeftButtonPressed))
                                    {
                                        if (!e.Flags.HasFlag (MouseFlags.PositionReport))
                                        { // Start drag
                                            _dragStart = e.ScreenPosition;
                                            _isDragging = true;
                                        }
                                        else
                                        {
                                            // Drag
                                            if (_isDragging && _dragStart.HasValue)
                                            {
                                                appWindow.SetNeedsDraw ();
                                            }
                                        }
                                    }

                                    if (e.Flags.HasFlag (MouseFlags.LeftButtonReleased))
                                    {
                                        if (_isDragging && _dragStart.HasValue)
                                        {
                                            // Add the new region
                                            AddRectangleFromPoints (_dragStart.Value, e.ScreenPosition, _regionOp);
                                            _isDragging = false;
                                            _dragStart = null;
                                        }

                                        appWindow.SetNeedsDraw ();
                                    }
                                };

        // Draw the regions
        appWindow.DrawingContent += (s, _) =>
                                    {
                                        if (s is not View sendingView)
                                        {
                                            return;
                                        }

                                        // Draw all regions with single line style
                                        //_region.FillRectangles (_attribute.Value, _fillRune);
                                        switch (_drawStyle)
                                        {
                                            case RegionDrawStyles.FillOnly:
                                                _region.FillRectangles (sendingView.App?.Driver, tools.CurrentAttribute!.Value, _previewFillRune);

                                                break;

                                            case RegionDrawStyles.InnerBoundaries:
                                                _region.DrawBoundaries (sendingView.LineCanvas, LineStyle.Single, tools.CurrentAttribute);
                                                _region.FillRectangles (sendingView.App?.Driver, tools.CurrentAttribute!.Value, (Rune)' ');

                                                break;

                                            case RegionDrawStyles.OuterBoundary:
                                                // Demo simplification: iterate the region's rectangles and add the four edges of each
                                                // to the LineCanvas. Shared edges between adjacent rectangles will overlap; LineCanvas's
                                                // line-style joining handles the visual result, matching how Border/Adornment draw.
                                                foreach (Rectangle rect in _region.GetRectangles ())
                                                {
                                                    if (rect.IsEmpty || rect.Width <= 0 || rect.Height <= 0)
                                                    {
                                                        continue;
                                                    }

                                                    sendingView.LineCanvas.AddLine (
                                                                                    new (rect.Left, rect.Top),
                                                                                    rect.Width,
                                                                                    Orientation.Horizontal,
                                                                                    LineStyle.Single,
                                                                                    tools.CurrentAttribute);

                                                    sendingView.LineCanvas.AddLine (
                                                                                    new (rect.Left, rect.Bottom - 1),
                                                                                    rect.Width,
                                                                                    Orientation.Horizontal,
                                                                                    LineStyle.Single,
                                                                                    tools.CurrentAttribute);

                                                    sendingView.LineCanvas.AddLine (
                                                                                    new (rect.Left, rect.Top),
                                                                                    rect.Height,
                                                                                    Orientation.Vertical,
                                                                                    LineStyle.Single,
                                                                                    tools.CurrentAttribute);

                                                    sendingView.LineCanvas.AddLine (
                                                                                    new (rect.Right - 1, rect.Top),
                                                                                    rect.Height,
                                                                                    Orientation.Vertical,
                                                                                    LineStyle.Single,
                                                                                    tools.CurrentAttribute);
                                                }

                                                _region.FillRectangles (sendingView.App?.Driver, tools.CurrentAttribute!.Value, (Rune)' ');

                                                break;
                                        }

                                        // If currently dragging, draw preview rectangle
                                        if (_isDragging && _dragStart.HasValue)
                                        {
                                            Point currentMousePos = sendingView.App!.Mouse.LastMousePosition!.Value;
                                            Rectangle previewRect = GetRectFromPoints (_dragStart.Value, currentMousePos);
                                            Region previewRegion = new (previewRect);

                                            previewRegion.FillRectangles (sendingView.App.Driver, tools.CurrentAttribute!.Value, (Rune)' ');

                                            previewRegion.DrawBoundaries (
                                                                          sendingView.LineCanvas,
                                                                          LineStyle.Dashed,
                                                                          new (
                                                                               tools.CurrentAttribute!.Value.Foreground.GetBrighterColor (),
                                                                               tools.CurrentAttribute!.Value.Background));
                                        }
                                    };

        app.Run (appWindow);
    }

    private void AddRectangleFromPoints (Point start, Point end, RegionOp op)
    {
        Rectangle rect = GetRectFromPoints (start, end);
        Region region = new (rect);
        _region.Combine (region, op); // Or RegionOp.MinimalUnion if you want minimal rectangles
    }

    private Rectangle GetRectFromPoints (Point start, Point end)
    {
        int left = Math.Min (start.X, end.X);
        int top = Math.Min (start.Y, end.Y);
        int right = Math.Max (start.X, end.X);
        int bottom = Math.Max (start.Y, end.Y);

        // Ensure minimum width and height of 1
        int width = Math.Max (1, right - left + 1);
        int height = Math.Max (1, bottom - top + 1);

        return new (left, top, width, height);
    }
}

internal enum RegionDrawStyles
{
    FillOnly = 0,

    InnerBoundaries = 1,

    OuterBoundary = 2
}

internal class RegionToolsView : Window
{
    //private Button _addLayerBtn;
    private readonly RegionAttributeView _attributeView = new ();
    private OptionSelector<RegionDrawStyles>? _stylePicker;
    private OptionSelector<RegionOp>? _regionOpSelector;

    public Attribute? CurrentAttribute
    {
        get => _attributeView.Value;
        set => _attributeView.Value = value;
    }

    public RegionToolsView ()
    {
        BorderStyle = LineStyle.Dotted;
        Border.Thickness = new (1, 2, 1, 1);
        Height = Dim.Auto ();
        Width = Dim.Auto ();
    }

    public override void BeginInit ()
    {
        base.BeginInit ();

        _attributeView.ValueChanged += (_, e) => AttributeChanged?.Invoke (this, e);

        _stylePicker = new ()
        {
            Width = Dim.Fill (),
            X = 0, Y = Pos.Bottom (_attributeView) + 1,
            AssignHotKeys = true
        };
        _stylePicker.BorderStyle = LineStyle.Single;
        _stylePicker.Border.Thickness = new (0, 1, 0, 0);
        _stylePicker.Title = "Draw Style";

        _stylePicker.ValueChanged += (_, a) => { SetStyle?.Invoke ((RegionDrawStyles)a.Value!); };
        _stylePicker.Value = RegionDrawStyles.FillOnly;

        _regionOpSelector = new ()
        {
            X = 0,
            Y = Pos.Bottom (_stylePicker) + 1,
            AssignHotKeys = true
        };

        _regionOpSelector.ValueChanged += (_, a) =>
                                          {
                                              if (a.Value is not null)
                                              {
                                                  RegionOpChanged?.Invoke (this, (RegionOp)a.Value);
                                              }
                                          };
        _regionOpSelector.Value = RegionOp.MinimalUnion;

        Add (_attributeView, _stylePicker, _regionOpSelector); //, _addLayerBtn);
    }

    public event EventHandler<Attribute?>? AttributeChanged;
    public event EventHandler<RegionOp>? RegionOpChanged;
    public event Action<RegionDrawStyles>? SetStyle;
}

internal class RegionAttributeView : View
{
    public event EventHandler<Attribute?>? ValueChanged;
    private Attribute? _value;

    public Attribute? Value
    {
        get => _value;
        set
        {
            _value = value;
            ValueChanged?.Invoke (this, value);
        }
    }

    private static readonly HashSet<(int, int)> _foregroundPoints =
    [
        (0, 0), (1, 0), (2, 0),
        (0, 1), (1, 1), (2, 1)
    ];

    private static readonly HashSet<(int, int)> _backgroundPoints =
    [
        (3, 1),
        (1, 2), (2, 2), (3, 2)
    ];

    public RegionAttributeView ()
    {
        Width = Dim.Fill ();
        Height = 4;

        BorderStyle = LineStyle.Single;
        Border.Thickness = new (0, 1, 0, 0);
        Title = "Attribute";
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        Color fg = Value?.Foreground ?? Color.Black;
        Color bg = Value?.Background ?? Color.Black;

        bool isTransparentFg = fg == GetAttributeForRole (VisualRole.Normal).Background;
        bool isTransparentBg = bg == GetAttributeForRole (VisualRole.Normal).Background;

        SetAttribute (new (fg, isTransparentFg ? Color.Gray : fg));

        // Square of foreground color
        foreach ((int, int) point in _foregroundPoints)
        {
            // Make pattern like this when it is same color as background of control
            /*▓▒
              ▒▓*/
            Rune rune;

            if (isTransparentFg)
            {
                rune = (Rune)(point.Item1 % 2 == point.Item2 % 2 ? '▓' : '▒');
            }
            else
            {
                rune = (Rune)'█';
            }

            AddRune (point.Item1, point.Item2, rune);
        }

        SetAttribute (new (bg, isTransparentBg ? Color.Gray : bg));

        // Square of background color
        foreach ((int, int) point in _backgroundPoints)
        {
            Rune rune;

            if (isTransparentBg)
            {
                rune = (Rune)(point.Item1 % 2 == point.Item2 % 2 ? '▓' : '▒');
            }
            else
            {
                rune = (Rune)'█';
            }

            AddRune (point.Item1, point.Item2, rune);
        }

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (mouse.Flags.HasFlag (MouseFlags.LeftButtonClicked))
        {
            if (IsForegroundPoint (mouse.Position!.Value.X, mouse.Position!.Value.Y))
            {
                ClickedInForeground ();
            }
            else if (IsBackgroundPoint (mouse.Position!.Value.X, mouse.Position!.Value.Y))
            {
                ClickedInBackground ();
            }
        }

        mouse.Handled = true;

        return mouse.Handled;
    }

    private bool IsForegroundPoint (int x, int y) => _foregroundPoints.Contains ((x, y));

    private bool IsBackgroundPoint (int x, int y) => _backgroundPoints.Contains ((x, y));

    private void ClickedInBackground ()
    {
        Color? result = App?.TopRunnable?.Prompt<ColorPicker, Color?> (resultExtractor: cp => cp.SelectedColor,
                                                                       beginInitHandler: prompt =>
                                                                                         {
                                                                                             prompt.Title = "Background Color";
                                                                                             prompt.GetWrappedView ().SelectedColor = Value!.Value.Background;
                                                                                         });

        if (result is { } selectedColor)
        {
            Value = new Attribute (Value!.Value.Foreground, selectedColor, Value!.Value.Style);
            SetNeedsDraw ();
        }
    }

    private void ClickedInForeground ()
    {
        Color? result = App?.TopRunnable?.Prompt<ColorPicker, Color?> (resultExtractor: cp => cp.SelectedColor,
                                                                       beginInitHandler: prompt =>
                                                                                         {
                                                                                             prompt.Title = "Foreground Color";
                                                                                             prompt.GetWrappedView ().SelectedColor = Value!.Value.Foreground;
                                                                                         });

        if (result is { } selectedColor)
        {
            Value = new Attribute (selectedColor, Value!.Value.Background);
            SetNeedsDraw ();
        }
    }
}
