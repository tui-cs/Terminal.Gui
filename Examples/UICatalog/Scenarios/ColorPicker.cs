#nullable enable
namespace UICatalog.Scenarios;

[ScenarioMetadata ("ColorPicker", "Color Picker and TrueColor demonstration.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Controls")]
public class ColorPickers : Scenario
{
    /// <summary>Background color Label.</summary>
    private Label? _backgroundColorLabel;

    /// <summary>Demo label.</summary>
    private View? _demoView;

    /// <summary>Foreground color label.</summary>
    private Label? _foregroundColorLabel;

    /// <summary>Background ColorPicker.</summary>
    private ColorPicker? _backgroundColorPicker;

    /// <summary>Foreground ColorPicker.</summary>
    private ColorPicker? _foregroundColorPicker;

    /// <summary>Background ColorPicker.</summary>
    private ColorPicker16? _backgroundColorPicker16;

    /// <summary>Foreground ColorPicker.</summary>
    private ColorPicker16? _foregroundColorPicker16;

    /// <summary>Set up the scenario.</summary>
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new ();
        window.Title = GetQuitKeyAndName ();

        ///////////////////////////////////////
        // True Color Pickers
        ///////////////////////////////////////

        // Foreground ColorPicker.
        _foregroundColorPicker = new ColorPicker { Title = "_Foreground Color", BorderStyle = LineStyle.Single, Width = Dim.Percent (50) };
        _foregroundColorPicker.ValueChanged += ForegroundColor_ColorChanged;
        window.Add (_foregroundColorPicker);

        _foregroundColorLabel = new Label { X = Pos.Left (_foregroundColorPicker), Y = Pos.Bottom (_foregroundColorPicker) + 1 };
        window.Add (_foregroundColorLabel);

        // Background ColorPicker.
        _backgroundColorPicker = new ColorPicker
        {
            Title = "_Background Color", X = Pos.AnchorEnd (), Width = Dim.Percent (50), BorderStyle = LineStyle.Single
        };

        _backgroundColorPicker.ValueChanged += BackgroundColor_ColorChanged;
        window.Add (_backgroundColorPicker);

        _backgroundColorLabel = new Label { X = Pos.AnchorEnd (), Y = Pos.Bottom (_backgroundColorPicker) + 1 };

        window.Add (_backgroundColorLabel);

        ///////////////////////////////////////
        // 16 Color Pickers
        ///////////////////////////////////////

        // Foreground ColorPicker 16.
        _foregroundColorPicker16 = new ColorPicker16
        {
            Title = "_Foreground Color", BorderStyle = LineStyle.Single, Width = Dim.Percent (50), Visible = false // We default to HSV so hide old one
        };
        _foregroundColorPicker16.ValueChanged += ForegroundColor_ColorChanged16;
        window.Add (_foregroundColorPicker16);

        // Background ColorPicker 16.
        _backgroundColorPicker16 = new ColorPicker16
        {
            Title = "_Background Color",
            X = Pos.AnchorEnd (),
            Width = Dim.Percent (50),
            BorderStyle = LineStyle.Single,
            Visible = false // We default to HSV so hide old one
        };

        _backgroundColorPicker16.ValueChanged += BackgroundColor_ColorChanged16;
        window.Add (_backgroundColorPicker16);

        // Demo Label.
        _demoView = new View
        {
            Title = "Color Sample",
            Text = "Lorem Ipsum",
            TextAlignment = Alignment.Center,
            VerticalTextAlignment = Alignment.Center,
            BorderStyle = LineStyle.Heavy,
            X = Pos.Center (),
            Y = Pos.Center (),
            Height = 5,
            Width = 20
        };
        window.Add (_demoView);

        var osColorModel = new OptionSelector
        {
            Y = Pos.Bottom (_demoView),
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Labels = ["_RGB", "_HSV", "H_SL", "_16 Colors"],
            Value = (int)_foregroundColorPicker.Style.ColorModel
        };

        osColorModel.ValueChanged += OnOsColorModelOnValueChanged;

        window.Add (osColorModel);

        // Checkbox for switching show text fields on and off
        var cbShowTextFields = new CheckBox
        {
            Text = "Show _Text Fields",
            Y = Pos.Bottom (osColorModel) + 1,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Value = _foregroundColorPicker.Style.ShowTextFields ? CheckState.Checked : CheckState.UnChecked
        };

        cbShowTextFields.ValueChanging += OnCbShowTextFieldsOnValueChanging;
        window.Add (cbShowTextFields);

        // Checkbox for switching show text fields on and off
        var cbShowName = new CheckBox
        {
            Text = "Show Color _Name",
            Y = Pos.Bottom (cbShowTextFields) + 1,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Value = _foregroundColorPicker.Style.ShowColorName ? CheckState.Checked : CheckState.UnChecked
        };

        cbShowName.ValueChanging += OnCbShowColorNameOnValueChanging;

        window.Add (cbShowName);

        var lblDriverName = new Label { Y = Pos.Bottom (cbShowName) + 1, Text = $"Driver is `{app.Driver?.GetName ()}`:" };
        bool canTrueColor = app.Driver?.SupportsTrueColor ?? false;

        var cbSupportsTrueColor = new CheckBox
        {
            X = Pos.Right (lblDriverName) + 1,
            Y = Pos.Top (lblDriverName),
            Value = canTrueColor ? CheckState.Checked : CheckState.UnChecked,
            CanFocus = false,
            Enabled = false,
            Text = "SupportsTrueColor"
        };
        window.Add (cbSupportsTrueColor);

        var cbUseTrueColor = new CheckBox
        {
            X = Pos.Right (cbSupportsTrueColor) + 1,
            Y = Pos.Top (lblDriverName),
            Value = app.Driver!.Force16Colors ? CheckState.Checked : CheckState.UnChecked,
            Enabled = canTrueColor,
            Text = "Force16Colors"
        };
        cbUseTrueColor.ValueChanging += (_, evt) => { app.Driver!.Force16Colors = evt.NewValue == CheckState.Checked; };
        window.Add (lblDriverName, cbSupportsTrueColor, cbUseTrueColor);

        // Set default colors.
        _foregroundColorPicker.Value = _demoView.SuperView!.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ();
        _backgroundColorPicker.Value = _demoView.SuperView.GetAttributeForRole (VisualRole.Normal).Background.GetClosestNamedColor16 ();

        app.Run (window);

        return;

        void OnCbShowTextFieldsOnValueChanging (object? _, ValueChangingEventArgs<CheckState> e)
        {
            _foregroundColorPicker.Style.ShowTextFields = e.NewValue == CheckState.Checked;
            _foregroundColorPicker.ApplyStyleChanges ();
            _backgroundColorPicker.Style.ShowTextFields = e.NewValue == CheckState.Checked;
            _backgroundColorPicker.ApplyStyleChanges ();
        }


        void OnCbShowColorNameOnValueChanging (object? _, ValueChangingEventArgs<CheckState> e)
        {
            _foregroundColorPicker.Style.ShowColorName = e.NewValue == CheckState.Checked;
            _foregroundColorPicker.ApplyStyleChanges ();
            _backgroundColorPicker.Style.ShowColorName = e.NewValue == CheckState.Checked;
            _backgroundColorPicker.ApplyStyleChanges ();
        }

        void OnOsColorModelOnValueChanged (object? _, ValueChangedEventArgs<int?> e)
        {
            // 16 colors
            if (e.NewValue == 3)
            {
                _foregroundColorPicker16.Visible = true;
                _foregroundColorPicker.Visible = false;

                _backgroundColorPicker16.Visible = true;
                _backgroundColorPicker.Visible = false;

                // Switching to 16 colors
                ForegroundColor_ColorChanged16 (null, new (default, _foregroundColorPicker16.SelectedColor));
                BackgroundColor_ColorChanged16 (null, new (default, _backgroundColorPicker16.SelectedColor));
            }
            else
            {
                _foregroundColorPicker16.Visible = false;
                _foregroundColorPicker.Visible = true;

                if (e.NewValue is { })
                {
                    _foregroundColorPicker.Style.ColorModel = (ColorModel)e.NewValue;
                    _foregroundColorPicker.ApplyStyleChanges ();

                    _backgroundColorPicker16.Visible = false;
                    _backgroundColorPicker.Visible = true;
                    _backgroundColorPicker.Style.ColorModel = (ColorModel)e.NewValue;
                }

                _backgroundColorPicker.ApplyStyleChanges ();

                // Switching to true colors
                _foregroundColorPicker.Value = _foregroundColorPicker16.SelectedColor;
                _backgroundColorPicker.Value = _backgroundColorPicker16.SelectedColor;
            }
        }
    }

    /// <summary>Fired when background color is changed.</summary>
    private void BackgroundColor_ColorChanged (object? sender, ValueChangedEventArgs<Color?> e)
    {
        UpdateColorLabel (_backgroundColorLabel!, _backgroundColorPicker!.Visible ? _backgroundColorPicker.Value!.Value : _backgroundColorPicker16!.SelectedColor);
        UpdateDemoLabel ();
    }

    /// <summary>Fired when foreground color is changed.</summary>
    private void ForegroundColor_ColorChanged (object? sender, ValueChangedEventArgs<Color?> e)
    {
        UpdateColorLabel (_foregroundColorLabel!, _foregroundColorPicker!.Visible ? _foregroundColorPicker.Value!.Value : _foregroundColorPicker16!.SelectedColor);
        UpdateDemoLabel ();
    }

    /// <summary>Fired when background color is changed (16-color picker).</summary>
    private void BackgroundColor_ColorChanged16 (object? sender, ValueChangedEventArgs<ColorName16> e)
    {
        UpdateColorLabel (_backgroundColorLabel!, _backgroundColorPicker!.Visible ? _backgroundColorPicker.Value!.Value : _backgroundColorPicker16!.SelectedColor);
        UpdateDemoLabel ();
    }

    /// <summary>Fired when foreground color is changed (16-color picker).</summary>
    private void ForegroundColor_ColorChanged16 (object? sender, ValueChangedEventArgs<ColorName16> e)
    {
        UpdateColorLabel (_foregroundColorLabel!, _foregroundColorPicker!.Visible ? _foregroundColorPicker.Value!.Value : _foregroundColorPicker16!.SelectedColor);
        UpdateDemoLabel ();
    }

    /// <summary>Update a color label from his ColorPicker.</summary>
    private void UpdateColorLabel (Label label, Color color)
    {
        label.ClearViewport ();

        label.Text = $"{color} ({(int)color}) #{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>Update Demo Label.</summary>
    private void UpdateDemoLabel () =>
        _demoView?.SetScheme (new Scheme
        {
            Normal = new Attribute (_foregroundColorPicker!.Visible
                                        ? _foregroundColorPicker.Value!.Value
                                        : _foregroundColorPicker16!.SelectedColor,
                                    _backgroundColorPicker!.Visible
                                        ? _backgroundColorPicker.Value!.Value
                                        : _backgroundColorPicker16!.SelectedColor)
        });

    public override List<Key> GetDemoKeyStrokes (IApplication? app)
    {
        List<Key> keys = [Key.B.WithAlt];

        for (var i = 0; i < 200; i++)
        {
            keys.Add (Key.CursorRight);
        }

        keys.Add (Key.Tab);
        keys.Add (Key.Tab);

        for (var i = 0; i < 200; i++)
        {
            keys.Add (Key.CursorLeft);
        }

        keys.Add (Key.Tab);
        keys.Add (Key.Tab);

        for (var i = 0; i < 200; i++)
        {
            keys.Add (Key.CursorLeft);
        }

        keys.Add (Key.N.WithAlt);
        keys.Add (Key.R.WithAlt);
        keys.Add (Key.H.WithAlt);
        keys.Add (Key.S.WithAlt);
        keys.Add (Key.D1.WithAlt);

        return keys;
    }
}
