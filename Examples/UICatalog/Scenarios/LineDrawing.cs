using System.Text;

namespace UICatalog.Scenarios;

public interface ITool
{
    void OnMouseEvent (DrawingArea area, Mouse mouse);
}

internal class DrawLineTool : ITool
{
    private StraightLine _currentLine;
    public LineStyle LineStyle { get; set; } = LineStyle.Single;

    /// <inheritdoc/>
    public void OnMouseEvent (DrawingArea area, Mouse mouse)
    {
        if (mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed))
        {
            if (_currentLine == null)
            {
                // MouseEventArgs pressed down
                _currentLine = new StraightLine (mouse.Position!.Value, 0, Orientation.Vertical, LineStyle, area.CurrentAttribute);

                area.CurrentLayer.AddLine (_currentLine);
            }
            else
            {
                // MouseEventArgs dragged
                Point start = _currentLine.Start;
                Point end = mouse.Position!.Value;
                var orientation = Orientation.Vertical;
                int length = end.Y - start.Y;

                // if line is wider than it is tall switch to horizontal
                if (Math.Abs (start.X - end.X) > Math.Abs (start.Y - end.Y))
                {
                    orientation = Orientation.Horizontal;
                    length = end.X - start.X;
                }

                if (length > 0)
                {
                    length++;
                }
                else
                {
                    length--;
                }

                _currentLine.Length = length;
                _currentLine.Orientation = orientation;
                area.CurrentLayer.ClearCache ();
                area.SetNeedsDraw ();
            }
        }
        else
        {
            // MouseEventArgs released
            if (_currentLine != null)
            {
                if (_currentLine.Length == 0)
                {
                    _currentLine.Length = 1;
                }

                if (_currentLine.Style == LineStyle.None)
                {
                    // Treat none as eraser
                    int idx = area.Layers.IndexOf (area.CurrentLayer);
                    area.Layers.Remove (area.CurrentLayer);

                    area.CurrentLayer = new LineCanvas (area.CurrentLayer.Lines.Exclude (_currentLine.Start, _currentLine.Length, _currentLine.Orientation));

                    area.Layers.Insert (idx, area.CurrentLayer);
                }

                _currentLine = null;
                area.ClearUndo ();
                area.SetNeedsDraw ();
            }
        }

        mouse.Handled = true;
    }
}

[ScenarioMetadata ("Line Drawing", "Demonstrates LineCanvas.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Drawing")]
public class LineDrawing : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new () { Title = GetQuitKeyAndName () };
        DrawingArea canvas = new () { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

        ToolsView tools = new () { Title = "Tools", X = Pos.Right (canvas) - 20, Y = 2 };

        tools.ColorChanged += (s, e) => canvas.SetCurrentAttribute (e);
        tools.SetStyle += b => canvas.CurrentTool = new DrawLineTool { LineStyle = b };
        tools.AddLayer += () => canvas.AddLayer ();

        win.Add (canvas);
        win.Add (tools);
        tools.CurrentColor = canvas.GetAttributeForRole (VisualRole.Normal);
        canvas.CurrentAttribute = tools.CurrentColor;

        win.KeyDown += (s, e) => { e.Handled = canvas.NewKeyDownEvent (e); };

        app.Run (win);
    }

    public static bool PromptForColor (IApplication app, string title, Color current, out Color newColor)
    {
        var accept = false;

        Dialog d = new () { Title = title, Buttons = [new Button { Title = Strings.btnCancel }, new Button { Title = Strings.btnOk }] };

        View cp;

        if (app.Driver!.Force16Colors)
        {
            cp = new ColorPicker16 { SelectedColor = current.GetClosestNamedColor16 (), Width = Dim.Fill () };
        }
        else
        {
            cp = new ColorPicker
            {
                SelectedColor = current, Width = Dim.Fill (0, 50), Style = new ColorPickerStyle { ShowColorName = true, ShowTextFields = true }
            };
            ((ColorPicker)cp).ApplyStyleChanges ();
        }

        d.Add (cp);

        app.Run (d);
        accept = d.Result == 1;
        d.Dispose ();
        newColor = app.Driver!.Force16Colors ? ((ColorPicker16)cp).SelectedColor : ((ColorPicker)cp).SelectedColor;

        return accept;
    }
}

public class ToolsView : Window
{
    private Button _addLayerBtn;
    private readonly AttributeView _colors;
    private OptionSelector<LineStyle> _stylePicker;

    public Attribute CurrentColor { get => _colors.Value; set => _colors.Value = value; }

    public ToolsView ()
    {
        BorderStyle = LineStyle.Dotted;
        Border.Thickness = new Thickness (1, 2, 1, 1);
        Initialized += ToolsView_Initialized;
        _colors = new AttributeView ();
    }

    public event Action AddLayer;

    public override void BeginInit ()
    {
        base.BeginInit ();

        _colors.ValueChanged += (s, e) => ColorChanged?.Invoke (this, e);

        _stylePicker = new OptionSelector<LineStyle> { X = 0, Y = Pos.Bottom (_colors), AssignHotKeys = true };

        _stylePicker.ValueChanged += (s, a) =>
                                     {
                                         if (a.Value is { })
                                         {
                                             SetStyle?.Invoke ((LineStyle)a.Value);
                                         }
                                     };
        _stylePicker.Value = LineStyle.Single;

        _addLayerBtn = new Button { Text = "New Layer", X = Pos.Center (), Y = Pos.Bottom (_stylePicker) };

        _addLayerBtn.Accepting += (s, a) => AddLayer?.Invoke ();
        Add (_colors, _stylePicker, _addLayerBtn);
    }

    public event EventHandler<Attribute> ColorChanged;
    public event Action<LineStyle> SetStyle;

    private void ToolsView_Initialized (object sender, EventArgs e)
    {
        Width = Math.Max (_colors.Frame.Width, _stylePicker.Frame.Width) + GetAdornmentsThickness ().Horizontal;
        Height = _colors.Frame.Height + _stylePicker.Frame.Height + _addLayerBtn.Frame.Height + GetAdornmentsThickness ().Vertical;
    }
}

public class DrawingArea : View
{
    public readonly List<LineCanvas> Layers = new ();
    private readonly Stack<StraightLine> _undoHistory = new ();
    public Attribute CurrentAttribute { get; set; }
    public LineCanvas CurrentLayer { get; set; }

    public ITool CurrentTool { get; set; } = new DrawLineTool ();
    public DrawingArea () => AddLayer ();

    protected override bool OnDrawingContent (DrawContext context)
    {
        foreach (LineCanvas canvas in Layers)
        {
            foreach (KeyValuePair<Point, Cell?> c in canvas.GetCellMap ())
            {
                if (c.Value is { })
                {
                    SetAttribute (c.Value.Value.Attribute ?? GetAttributeForRole (VisualRole.Normal));

                    // TODO: #2616 - Support combining sequences that don't normalize
                    AddStr (c.Key.X, c.Key.Y, c.Value.Value.Grapheme);
                }
            }
        }

        return true;
    }

    //// BUGBUG: Why is this not handled by a key binding???
    protected override bool OnKeyDown (Key e)
    {
        // BUGBUG: These should be implemented with key bindings
        if (e.KeyCode == (KeyCode.Z | KeyCode.CtrlMask))
        {
            StraightLine pop = CurrentLayer.RemoveLastLine ();

            if (pop != null)
            {
                _undoHistory.Push (pop);
                SetNeedsDraw ();

                return true;
            }
        }

        if (e.KeyCode == (KeyCode.Y | KeyCode.CtrlMask))
        {
            if (_undoHistory.Any ())
            {
                StraightLine pop = _undoHistory.Pop ();
                CurrentLayer.AddLine (pop);
                SetNeedsDraw ();

                return true;
            }
        }

        return false;
    }

    protected override bool OnMouseEvent (Mouse mouse)
    {
        CurrentTool.OnMouseEvent (this, mouse);

        return mouse.Handled;
    }

    internal void AddLayer ()
    {
        CurrentLayer = new LineCanvas ();
        Layers.Add (CurrentLayer);
    }

    internal void SetCurrentAttribute (Attribute a) => CurrentAttribute = a;

    public void ClearUndo () => _undoHistory.Clear ();
}

public class AttributeView : View
{
    public event EventHandler<Attribute> ValueChanged;
    private Attribute _value;

    public Attribute Value
    {
        get => _value;
        set
        {
            _value = value;
            ValueChanged?.Invoke (this, value);
        }
    }

    private static readonly HashSet<(int, int)> ForegroundPoints = new ()
    {
        (0, 0),
        (1, 0),
        (2, 0),
        (0, 1),
        (1, 1),
        (2, 1)
    };

    private static readonly HashSet<(int, int)> BackgroundPoints = new () { (3, 1), (1, 2), (2, 2), (3, 2) };

    public AttributeView ()
    {
        Width = 4;
        Height = 3;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext context)
    {
        Color fg = Value.Foreground;
        Color bg = Value.Background;

        bool isTransparentFg = fg == GetAttributeForRole (VisualRole.Normal).Background;
        bool isTransparentBg = bg == GetAttributeForRole (VisualRole.Normal).Background;

        SetAttribute (new Attribute (fg, isTransparentFg ? Color.Gray : fg));

        // Square of foreground color
        foreach ((int, int) point in ForegroundPoints)
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

        SetAttribute (new Attribute (bg, isTransparentBg ? Color.Gray : bg));

        // Square of background color
        foreach ((int, int) point in BackgroundPoints)
        {
            // Make pattern like this when it is same color as background of control
            /*▓▒
              ▒▓*/
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

            mouse.Handled = true;
        }

        return mouse.Handled;
    }

    private bool IsForegroundPoint (int x, int y) => ForegroundPoints.Contains ((x, y));

    private bool IsBackgroundPoint (int x, int y) => BackgroundPoints.Contains ((x, y));

    private void ClickedInBackground ()
    {
        Color? result = App?.TopRunnable?.Prompt<ColorPicker, Color?> (resultExtractor: cp => cp.SelectedColor,
                                                                       beginInitHandler: prompt =>
                                                                                         {
                                                                                             prompt.Title = "Background Color";
                                                                                             prompt.GetWrappedView ().SelectedColor = Value.Background;
                                                                                         });

        if (result is { } selectedColor)
        {
            Value = new Attribute (Value.Foreground, selectedColor, Value.Style);
            SetNeedsDraw ();
        }
    }

    private void ClickedInForeground ()
    {
        Color? result = App?.TopRunnable?.Prompt<ColorPicker, Color?> (resultExtractor: cp => cp.SelectedColor,
                                                                       beginInitHandler: prompt =>
                                                                                         {
                                                                                             prompt.Title = "Foreground Color";
                                                                                             prompt.GetWrappedView ().SelectedColor = Value.Foreground;
                                                                                         });

        if (result is { } selectedColor)
        {
            Value = new Attribute (selectedColor, Value.Background);
            SetNeedsDraw ();
        }
    }
}
