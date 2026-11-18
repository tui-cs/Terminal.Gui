#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Mandelbrot", "Demonstrates Sixel/Kitty Graphics Support with live settings and an overlay dialog.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
public class Mandelbrot : Scenario
{
    private const int SETTING_LABEL_WIDTH = 11;
    private const int CENTER_X_LABEL_GROUP_ID = 1;
    private const int CENTER_Y_LABEL_GROUP_ID = 2;
    private const int SPAN_LABEL_GROUP_ID = 3;
    private const int ITERATIONS_LABEL_GROUP_ID = 4;
    private const int RESET_GROUP_ID = 5;
    private const int NOTE_GROUP_ID = 6;
    private const int FIRE_LABEL_GROUP_ID = 7;
    private const int FIRE_PROGRESS_GROUP_ID = 8;
    private const int RASTER_PROTOCOL_AUTO = 0;
    private const int RASTER_PROTOCOL_KITTY = 1;
    private const int RASTER_PROTOCOL_SIXEL = 2;

    private IApplication _app = null!;
    private NumericUpDown<double> _centerX = null!;
    private NumericUpDown<double> _centerY = null!;
    private ProgressBar _fireProgress = null!;
    private object? _fireProgressTimeout;
    private NumericUpDown<int> _iterations = null!;
    private MandelbrotImageView _mandelbrotView = null!;
    private NumericUpDown<double> _span = null!;
    private Label _status = null!;
    private Window _window = null!;
    private SixelSupportResult _sixelSupportResult = new ();
    private KittyGraphicsSupportResult? _kittyGraphicsSupportResult;
    private OptionSelector _osRasterProtocol = null!;
    private Label _driverStatus = null!;
    private Label _cellStatus = null!;
    private Label _kittyStatus = null!;
    private Label _sixelStatus = null!;
    private Label _selectedStatus = null!;

    public override void Main ()
    {
        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        _app = app;
        _app.Driver!.SixelSupportChanged += OnSixelSupportChanged;
        _app.Driver.KittyGraphicsSupportChanged += OnKittyGraphicsSupportChanged;

        _window = new Window { Title = $"{Application.GetDefaultKey (Command.Quit)} to Quit - Scenario: {GetName ()}" };

        FrameView capabilityMatrix = BuildCapabilityMatrix ();

        Label protocolLabel = new () { Y = Pos.Bottom (capabilityMatrix), Text = "Raster protocol:" };

        _osRasterProtocol = new OptionSelector
        {
            X = Pos.Right (protocolLabel) + 1,
            Y = Pos.Top (protocolLabel),
            Orientation = Orientation.Horizontal,
            Labels = ["Auto", "Kitty", "Sixel"],
            Values = [RASTER_PROTOCOL_AUTO, RASTER_PROTOCOL_KITTY, RASTER_PROTOCOL_SIXEL],
            Value = RASTER_PROTOCOL_AUTO
        };
        _osRasterProtocol.ValueChanged += (_, _) => ApplyRasterProtocolSelection ();

        FrameView settings = new () { Title = "Settings", Y = Pos.Bottom (protocolLabel), Width = 34, Height = Dim.Fill () };

        View display = new ()
        {
            X = Pos.Right (settings),
            Y = Pos.Bottom (protocolLabel),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            CanFocus = true
        };

        _status = new Label { X = 0, Y = Pos.AnchorEnd (1), Width = Dim.Fill (), Height = 1 };

        _mandelbrotView = new MandelbrotImageView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        _mandelbrotView.ImageRendered += (_, _) => UpdateMandelbrotStatus ();
        _mandelbrotView.MandelbrotSettingsChanged += (_, _) => UpdateSettingsFromMandelbrotView ();

        BuildSettings (settings);

        display.Add (_status, _mandelbrotView);
        _window.Add (capabilityMatrix, protocolLabel, _osRasterProtocol, settings, display);

        _window.Initialized += (_, _) =>
                               {
                                   UpdateRasterSupportState (_app.Driver?.SixelSupport, _app.Driver?.KittyGraphicsSupport);
                                   StartFireProgress ();

                                   _app.AddTimeout (TimeSpan.FromMilliseconds (100),
                                                    () =>
                                                    {
                                                        _mandelbrotView.SetFocus ();

                                                        return false;
                                                    });
                               };

        try
        {
            app.Run (_window);
        }
        finally
        {
            StopFireProgress ();
            _app.Driver!.SixelSupportChanged -= OnSixelSupportChanged;
            _app.Driver.KittyGraphicsSupportChanged -= OnKittyGraphicsSupportChanged;
            _window.Dispose ();
        }
    }

    private bool AdvanceFireProgress ()
    {
        if (_fireProgressTimeout is null)
        {
            return false;
        }

        float nextFraction = _fireProgress.Fraction + 0.02f;
        _fireProgress.Fraction = nextFraction >= 1 ? 0 : nextFraction;

        return true;
    }

    private void BuildSettings (View settings)
    {
        Label centerXLabel = CreateSettingLabel ("Center X:", CENTER_X_LABEL_GROUP_ID);
        Label centerYLabel = CreateSettingLabel ("Center Y:", CENTER_Y_LABEL_GROUP_ID, centerXLabel);
        Label spanLabel = CreateSettingLabel ("Span:", SPAN_LABEL_GROUP_ID, centerYLabel);
        Label iterationsLabel = CreateSettingLabel ("Iterations:", ITERATIONS_LABEL_GROUP_ID, spanLabel);

        _centerX = new NumericUpDown<double>
        {
            X = Pos.Right (centerXLabel) + 1,
            Y = Pos.Top (centerXLabel),
            Width = Dim.Fill (),
            Value = -0.5,
            Increment = 0.05,
            Format = "{0:0.000}"
        };

        _centerY = new NumericUpDown<double>
        {
            X = Pos.Right (centerYLabel) + 1,
            Y = Pos.Top (centerYLabel),
            Width = Dim.Fill (),
            Value = 0,
            Increment = 0.05,
            Format = "{0:0.000}"
        };

        _span = new NumericUpDown<double>
        {
            X = Pos.Right (spanLabel) + 1,
            Y = Pos.Top (spanLabel),
            Width = Dim.Fill (),
            Value = 3,
            Increment = 0.1,
            Format = "{0:0.000}"
        };

        _iterations = new NumericUpDown<int>
        {
            X = Pos.Right (iterationsLabel) + 1,
            Y = Pos.Top (iterationsLabel),
            Width = Dim.Fill (),
            Value = 80,
            Increment = 10
        };

        _span.ValueChanging += (_, args) =>
                               {
                                   if (args.NewValue < MandelbrotImageView.MinimumSpan)
                                   {
                                       args.Handled = true;
                                   }
                               };

        _iterations.ValueChanging += (_, args) =>
                                     {
                                         if (args.NewValue < MandelbrotImageView.MinimumIterations)
                                         {
                                             args.Handled = true;
                                         }
                                     };

        _centerX.ValueChanged += (_, _) => UpdateMandelbrotSettings ();
        _centerY.ValueChanged += (_, _) => UpdateMandelbrotSettings ();
        _span.ValueChanged += (_, _) => UpdateMandelbrotSettings ();
        _iterations.ValueChanged += (_, _) => UpdateMandelbrotSettings ();

        Button reset = new () { X = Pos.Align (Alignment.Start, groupId: RESET_GROUP_ID), Y = Pos.Bottom (iterationsLabel) + 2, Text = "_Reset" };

        reset.Accepted += (_, _) => ResetSettings ();

        Button overlay = new () { X = Pos.Right (reset) + 1, Y = Pos.Top (reset), Text = "_Overlay" };

        overlay.Accepted += (_, _) => ShowOverlay ();

        Label note = new ()
        {
            X = Pos.Align (Alignment.Start, groupId: NOTE_GROUP_ID),
            Y = Pos.Bottom (reset) + 2,
            Width = Dim.Fill (),
            Height = Dim.Auto (),
            Text = "The image supports\nImageView input: mouse\nwheel or +/- zooms,\narrow keys pan, and\nHome or 0 resets."
        };

        Label fireLabel = new () { X = Pos.Align (Alignment.Start, groupId: FIRE_LABEL_GROUP_ID), Y = Pos.AnchorEnd (2), Text = "Fire progress:" };

        _fireProgress = new ProgressBar
        {
            X = Pos.Align (Alignment.Start, groupId: FIRE_PROGRESS_GROUP_ID),
            Y = Pos.Bottom (fireLabel),
            Width = Dim.Fill (),
            ProgressBarStyle = ProgressBarStyle.Fire,
            ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage,
            CanFocus = true
        };

        settings.Add (centerXLabel,
                      centerYLabel,
                      spanLabel,
                      iterationsLabel,
                      _centerX,
                      _centerY,
                      _span,
                      _iterations,
                      reset,
                      overlay,
                      note,
                      fireLabel,
                      _fireProgress);
    }

    private static Label CreateSettingLabel (string text, int groupId, View? previous = null) =>
        new ()
        {
            X = Pos.Align (Alignment.Start, groupId: groupId),
            Y = previous is null ? Pos.Align (Alignment.Start) : Pos.Bottom (previous) + 1,
            Width = SETTING_LABEL_WIDTH,
            TextAlignment = Alignment.End,
            Text = text
        };

    private void OnSixelSupportChanged (object? sender, ValueChangedEventArgs<SixelSupportResult?> args)
    {
        UpdateRasterSupportState (args.NewValue, _app.Driver?.KittyGraphicsSupport);
    }

    private void OnKittyGraphicsSupportChanged (object? sender, ValueChangedEventArgs<KittyGraphicsSupportResult?> args)
    {
        UpdateRasterSupportState (_app.Driver?.SixelSupport, args.NewValue);
    }

    private void UpdateRasterSupportState (SixelSupportResult? sixelResult, KittyGraphicsSupportResult? kittyResult)
    {
        _sixelSupportResult = sixelResult ?? new SixelSupportResult ();
        _kittyGraphicsSupportResult = kittyResult;
        ApplyRasterProtocolSelection ();
    }

    private void ApplyRasterProtocolSelection ()
    {
        if (_app.Driver?.GetOutput () is { } output)
        {
            output.UseKittyGraphics = ShouldUseKittyGraphics ();
        }

        _mandelbrotView.UpdateRasterSupport (_sixelSupportResult, _kittyGraphicsSupportResult);
        UpdateRasterCapabilityMatrix ();
    }

    private bool ShouldUseKittyGraphics ()
    {
        if (_kittyGraphicsSupportResult is not { IsSupported: true })
        {
            return false;
        }

        return _osRasterProtocol.Value != RASTER_PROTOCOL_SIXEL || _sixelSupportResult is not { IsSupported: true };
    }

    private FrameView BuildCapabilityMatrix ()
    {
        FrameView matrix = new () { Title = "Raster Capability Matrix", Width = Dim.Fill (), Height = 7, CanFocus = false };

        _driverStatus = new Label { Y = 0, Width = Dim.Fill () };
        Label header = new () { Y = 1, Width = Dim.Fill (), Text = "Renderer       Available   Resolution       Notes" };
        _cellStatus = new Label { Y = 2, Width = Dim.Fill () };
        _kittyStatus = new Label { Y = 3, Width = Dim.Fill () };
        _sixelStatus = new Label { Y = 4, Width = Dim.Fill () };
        _selectedStatus = new Label { Y = 5, Width = Dim.Fill () };

        matrix.Add (_driverStatus, header, _cellStatus, _kittyStatus, _sixelStatus, _selectedStatus);

        return matrix;
    }

    private void UpdateRasterCapabilityMatrix ()
    {
        bool kittyActive = _mandelbrotView.IsKittyRasterActive;
        bool sixelActive = _mandelbrotView.IsSixelRasterActive;
        bool cellActive = _mandelbrotView.IsCellRenderActive;
        string driverName = _app.Driver?.GetName () ?? "unknown";
        bool trueColor = _app.Driver?.SupportsTrueColor == true;
        bool legacy = _app.Driver?.IsLegacyConsole == true;

        _driverStatus.Text = $"Driver: {driverName}; true color: {YesNo (trueColor)}; legacy console: {YesNo (legacy)}";
        _cellStatus.Text = Row ("Cell colors", "yes", "1 cell", cellActive ? "active" : "fallback");

        bool? kittySupported = _kittyGraphicsSupportResult?.IsSupported;
        string kittyResolution = _kittyGraphicsSupportResult is { } kitty ? SizeText (kitty.Resolution) : "-";
        _kittyStatus.Text = Row ("Kitty", ProtocolState (kittySupported), kittyResolution, kittyActive ? "active" : "auto-preferred");

        bool? sixelSupported = _sixelSupportResult is { IsSupported: true };
        string sixelResolution = _sixelSupportResult is { IsSupported: true } ? SizeText (_sixelSupportResult.Resolution) : "-";

        string sixelNotes = _sixelSupportResult is { IsSupported: true }
                                ? $"{_sixelSupportResult.MaxPaletteColors} colors; alpha {YesNo (_sixelSupportResult.SupportsTransparency)}"
                                : "fallback after Kitty";
        _sixelStatus.Text = Row ("Sixel", ProtocolState (sixelSupported), sixelResolution, sixelActive ? $"active; {sixelNotes}" : sixelNotes);

        UpdateRasterProtocolSelector ();
        _selectedStatus.Text = $"Selected raster path: {GetSelectedRendererName (kittyActive, sixelActive)}";
        _window.SetNeedsDraw ();
    }

    private void UpdateRasterProtocolSelector ()
    {
        bool bothSupported = _kittyGraphicsSupportResult is { IsSupported: true } && _sixelSupportResult is { IsSupported: true };
        _osRasterProtocol.Enabled = bothSupported;

        if (!bothSupported && _osRasterProtocol.Value != RASTER_PROTOCOL_AUTO)
        {
            _osRasterProtocol.Value = RASTER_PROTOCOL_AUTO;
        }
    }

    private static string GetSelectedRendererName (bool kittyActive, bool sixelActive)
    {
        if (kittyActive)
        {
            return "Kitty graphics";
        }

        if (sixelActive)
        {
            return "Sixel";
        }

        return "Cell renderer fallback";
    }

    private static string ProtocolState (bool? isSupported) =>
        isSupported switch
        {
            true => "yes",
            false => "no",
            _ => "detecting"
        };

    private static string Row (string renderer, string available, string resolution, string notes) =>
        $"{renderer,-14} {available,-11} {resolution,-16} {notes}";

    private static string SizeText (Size size) => $"{size.Width}x{size.Height} px/cell";

    private static string YesNo (bool value) => value ? "yes" : "no";

    private void ResetSettings ()
    {
        _mandelbrotView.ResetMandelbrot ();
        UpdateSettingsFromMandelbrotView ();
    }

    private void ShowOverlay ()
    {
        _mandelbrotView.SetNeedsDraw ();

        Dialog dialog = new () { Title = "Overlay Runnable", Width = 38, Height = 9 };

        dialog.Add (new Label
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = "This dialog is a runnable\nshown over the sixel image\nto exercise clipping."
        });

        dialog.AddButton (new Button { Text = "_OK", IsDefault = true });
        _app.Run (dialog);
        dialog.Dispose ();

        _mandelbrotView.SetNeedsDraw ();
    }

    private void StartFireProgress () => _fireProgressTimeout ??= _app.AddTimeout (TimeSpan.FromMilliseconds (80), AdvanceFireProgress);

    private void StopFireProgress ()
    {
        if (_fireProgressTimeout is null)
        {
            return;
        }

        _app.RemoveTimeout (_fireProgressTimeout);
        _fireProgressTimeout = null;
    }

    private void UpdateMandelbrotSettings ()
    {
        _mandelbrotView.SetMandelbrot (_centerX.Value, _centerY.Value, _span.Value, _iterations.Value);
    }

    private void UpdateMandelbrotStatus ()
    {
        _status.Text =
            $"{_mandelbrotView.ActiveRenderMode}: {_mandelbrotView.LastImageColumns} x {_mandelbrotView.LastImageRows} cells, {_mandelbrotView.LastPixelWidth} x {_mandelbrotView.LastPixelHeight}px";
        _status.SetNeedsDraw ();
        UpdateRasterCapabilityMatrix ();
    }

    private void UpdateSettingsFromMandelbrotView ()
    {
        _centerX.Value = _mandelbrotView.CenterX;
        _centerY.Value = _mandelbrotView.CenterY;
        _span.Value = _mandelbrotView.Span;
        _iterations.Value = _mandelbrotView.MaxIterations;
    }
}
