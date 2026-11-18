#nullable enable

namespace UICatalog.Scenarios;

/// <summary>
///     Displays an <see cref="Attribute"/> with color names, dark/light indication, text style,
///     and a sample rendered in that attribute.
/// </summary>
internal class AttributeViewer : View
{
    private readonly Label _fgLabel;
    private readonly Label _bgLabel;
    private readonly Label _styleLabel;
    private readonly Label _sampleLabel;

    private Attribute? _displayAttribute;
    private Attribute _resolvedAttribute;

    public AttributeViewer ()
    {
        CanFocus = false;
        Width = Dim.Auto ();
        Height = Dim.Auto ();

        _fgLabel = new Label { Width = Dim.Fill () };

        _bgLabel = new Label { Y = Pos.Bottom (_fgLabel), Width = Dim.Fill () };

        _styleLabel = new Label { Y = Pos.Bottom (_bgLabel), Width = Dim.Fill (), Visible = false };

        _sampleLabel = new Label { Y = Pos.Bottom (_styleLabel), Width = Dim.Fill (), Text = " Sample Text " };

        _sampleLabel.DrawingContent += (_, _) => { _sampleLabel.SetAttribute (_resolvedAttribute); };

        Add (_fgLabel, _bgLabel, _styleLabel, _sampleLabel);
    }

    /// <summary>
    ///     Gets or sets the <see cref="Attribute"/> to display. If <see langword="null"/>, the driver's
    ///     <see cref="IDriver.DefaultAttribute"/> is used at draw time.
    /// </summary>
    public Attribute? DisplayAttribute
    {
        get => _displayAttribute;
        set
        {
            _displayAttribute = value;
            UpdateLabels ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        // Resolve the attribute lazily (driver may set DefaultAttribute asynchronously)
        Attribute? resolved = _displayAttribute ?? App?.Driver?.DefaultAttribute;

        if (resolved is { } attr)
        {
            UpdateLabels (attr);
        }

        return false;
    }

    private void UpdateLabels () => UpdateLabels (_displayAttribute ?? App?.Driver?.DefaultAttribute);

    private void UpdateLabels (Attribute? attr)
    {
        if (attr is not { } a)
        {
            _fgLabel.Text = "Fg: (unknown)";
            _bgLabel.Text = "Bg: (unknown)";
            _styleLabel.Visible = false;
            _resolvedAttribute = Attribute.Default;

            return;
        }

        _fgLabel.Text = FormatColor ("Fg", a.Foreground);
        _bgLabel.Text = FormatColor ("Bg", a.Background);

        if (a.Style != TextStyle.None)
        {
            _styleLabel.Text = $"Style: {a.Style}";
            _styleLabel.Visible = true;
        }
        else
        {
            _styleLabel.Visible = false;
        }

        _resolvedAttribute = a;
    }

    private static string FormatColor (string label, Color color)
    {
        string name = ColorStrings.GetColorName (color) ?? color.ToString ();
        string darkness = color.IsDarkColor () ? "Dark" : "Light";

        return $"{label}: {name} ({darkness})";
    }
}
