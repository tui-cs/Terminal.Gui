#nullable enable

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

// ReSharper disable AccessToDisposedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Images", "Demonstration of cell and raster image rendering with runtime capability detection.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
public class Images : Scenario
{
    private const string FIRE_RASTER_IMAGE_ID = "UICatalog.Images.Fire";
    private const int RASTER_PROTOCOL_AUTO = 0;
    private const int RASTER_PROTOCOL_KITTY = 1;
    private const int RASTER_PROTOCOL_SIXEL = 2;

    private IApplication _app = null!;
    private ImageView _cellImageView = null!;
    private CheckBox _cbUseRasterGraphics = null!;
    private Label _cellStatus = null!;
    private Label _driverStatus = null!;
    private DoomFire? _fire;
    private SixelEncoder _fireEncoder = new ();
    private int _fireFrameCounter;
    private Image<Rgba32>? _fullResImage;
    private bool _isDisposed;
    private Label _kittyStatus = null!;
    private KittyGraphicsSupportResult? _kittyGraphicsSupportResult;
    private OptionSelector _osDistanceAlgorithm = null!;
    private OptionSelector _osPaletteBuilder = null!;
    private OptionSelector _osRasterProtocol = null!;
    private NumericUpDown _popularityThreshold = null!;
    private NumericUpDown _pxX = null!;
    private NumericUpDown _pxY = null!;
    private ImageView _rasterImageView = null!;
    private View _rasterSettings = null!;
    private Label _selectedStatus = null!;
    private Label _sixelStatus = null!;
    private SixelSupportResult? _sixelSupportResult;
    private View _tabRaster = null!;
    private Window _win = null!;
    private Size _winSize;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();
        IDriver driver = app.Driver ?? throw new InvalidOperationException ("The application driver was not initialized.");
        _app = app;

        _win = new Window { Title = $"{Application.GetDefaultKey (Command.Quit)} to Quit - Scenario: {GetName ()}" };

        View tabCell = new () { Title = "_Cell renderer", Width = Dim.Fill (), Height = Dim.Fill () };
        _tabRaster = new View { Title = "_Raster graphics", Width = Dim.Fill (), Height = Dim.Fill () };

        FrameView capabilityMatrix = BuildCapabilityMatrix ();
        _win.Add (capabilityMatrix);

        _cbUseRasterGraphics = new CheckBox { Y = Pos.Bottom (capabilityMatrix), Value = CheckState.Checked, Text = "Use raster ImageView" };
        _cbUseRasterGraphics.ValueChanging += (_, evt) => SetUseRasterGraphics (evt.NewValue == CheckState.Checked);
        _win.Add (_cbUseRasterGraphics);

        bool canTrueColor = driver.SupportsTrueColor;

        CheckBox cbUseTrueColor = new ()
        {
            X = Pos.Right (_cbUseRasterGraphics) + 2,
            Y = Pos.Top (_cbUseRasterGraphics),
            Value = !Driver.Force16Colors ? CheckState.Checked : CheckState.UnChecked,
            Enabled = canTrueColor,
            Text = "Use true color"
        };

        cbUseTrueColor.ValueChanging += (_, evt) =>
                                        {
                                            Driver.Force16Colors = evt.NewValue == CheckState.UnChecked;
                                            UpdateRasterCapabilityMatrix ();
                                        };
        _win.Add (cbUseTrueColor);

        CheckBox cbUseBackgroundRendering = new ()
        {
            X = Pos.Right (cbUseTrueColor) + 2,
            Y = Pos.Top (cbUseTrueColor),
            Value = CheckState.Checked,
            Text = "Async rendering"
        };
        cbUseBackgroundRendering.ValueChanging += (_, evt) => SetBackgroundRendering (evt.NewValue == CheckState.Checked);
        _win.Add (cbUseBackgroundRendering);

        Label protocolLabel = new () { Y = Pos.Bottom (_cbUseRasterGraphics), Text = "Raster protocol:" };

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
        _win.Add (protocolLabel, _osRasterProtocol);

        Button btnOpenImage = new () { Y = Pos.Bottom (_osRasterProtocol), Text = "_Open Image" };
        _win.Add (btnOpenImage);

        Button btnStartFire = new () { X = Pos.Right (btnOpenImage), Y = Pos.Top (btnOpenImage), Text = "Start _Fire" };
        btnStartFire.Accepting += BtnStartFireOnAccept;
        _win.Add (btnStartFire);

        Tabs tabs = new () { Y = Pos.Bottom (btnOpenImage), Width = Dim.Fill (), Height = Dim.Fill () };
        BuildCellTab (tabCell);
        BuildRasterTab (_tabRaster);
        tabs.Add (tabCell, _tabRaster);

        LoadDefaultImage ();

        btnOpenImage.Accepting += OpenImage;

        _win.Add (tabs);

        _win.SubViewsLaidOut += Win_SubViewsLaidOut;
        _win.Initialized += (_, _) => UpdateRasterSupportState (driver.SixelSupport, driver.KittyGraphicsSupport);
        driver.SixelSupportChanged += Driver_SixelSupportChanged;
        driver.KittyGraphicsSupportChanged += Driver_KittyGraphicsSupportChanged;

        try
        {
            app.Run (_win);
        }
        finally
        {
            driver.SixelSupportChanged -= Driver_SixelSupportChanged;
            driver.KittyGraphicsSupportChanged -= Driver_KittyGraphicsSupportChanged;
            driver.GetOutputBuffer ().RemoveRasterImage (FIRE_RASTER_IMAGE_ID);
            _win.Dispose ();
        }
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);
        _fullResImage?.Dispose ();
        _app.Driver?.GetOutputBuffer ().RemoveRasterImage (FIRE_RASTER_IMAGE_ID);
        _isDisposed = true;
    }

    private bool AdvanceFireTimerCallback ()
    {
        if (_isDisposed)
        {
            return false;
        }

        if (_fire is not { } fire)
        {
            return false;
        }

        fire.AdvanceFrame ();
        _fireFrameCounter++;

        // Control frame rate by adjusting this. Lower number means more FPS.
        if (_fireFrameCounter % 2 != 0)
        {
            return true;
        }

        Color [,] bmp = fire.GetFirePixels ();

        RasterImageCommand command = new ()
        {
            Id = FIRE_RASTER_IMAGE_ID,
            Pixels = bmp,
            DestinationCells = GetFireDestinationCells (),
            Encoder = IsSixelRasterPath () ? _fireEncoder : null,
            AlwaysRender = true,
            RenderAfterText = true
        };

        _win.SetNeedsDraw ();
        _app.Driver?.GetOutputBuffer ().AddRasterImage (command);

        return true;
    }

    private void ApplyRasterProtocolSelection ()
    {
        if (_app.Driver?.GetOutput () is { } output)
        {
            output.UseKittyGraphics = ShouldUseKittyGraphics ();
        }

        if (_rasterImageView.Image is { } image)
        {
            UpdateRasterImage (image);
        }

        if (_fire is not null)
        {
            GenerateRasterFire (false);
        }

        UpdateRasterCapabilityMatrix ();
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

    private void BtnStartFireOnAccept (object? sender, CommandEventArgs e)
    {
        if (_fire is not null)
        {
            return;
        }

        if (!IsRasterAvailable ())
        {
            MessageBox.ErrorQuery (_app, "Raster Graphics Not Available", "This terminal did not report Kitty graphics or Sixel support.", "Ok");

            return;
        }

        if (IsSixelRasterPath () && _sixelSupportResult is not { SupportsTransparency: true })
        {
            if (MessageBox.Query (_app,
                                  "Transparency Not Supported",
                                  "It looks like your terminal does not support transparent Sixel backgrounds. Do you want to try anyway?",
                                  "Yes",
                                  "No")
                != 0)
            {
                return;
            }
        }

        _winSize = _win.Viewport.Size;

        GenerateRasterFire (true);
    }

    private void BuildCellTab (View tabCell)
    {
        _cellImageView = new ImageView
        {
            BorderStyle = LineStyle.Dotted,
            Width = 30,
            Height = 10,
            CanFocus = true,
            Arrangement = ViewArrangement.Resizable,
            UseRasterGraphics = false,
            UseBackgroundRendering = true,
            Title = "Cell ImageView"
        };

        tabCell.Add (_cellImageView);
    }

    private void BuildRasterTab (View tabRaster)
    {
        _rasterSettings = new View { X = Pos.AnchorEnd (), Width = Dim.Auto (), Height = Dim.Auto (), CanFocus = true };

        _rasterImageView = new ImageView
        {
            CanFocus = true,
            Width = 30,
            Height = 10,
            BorderStyle = LineStyle.Dotted,
            Arrangement = ViewArrangement.Resizable,
            UseRasterGraphics = true,
            UseBackgroundRendering = true,
            Title = "Raster ImageView"
        };

        Label lblPxX = new () { Y = 0, Text = "Fire pixels/Col:" };
        _pxX = new NumericUpDown { X = Pos.Right (lblPxX), Y = 0, Value = GetDefaultFirePixelsPerColumn () };

        Label lblPxY = new () { X = lblPxX.X, Y = Pos.Bottom (_pxX), Text = "Fire pixels/Row:" };
        _pxY = new NumericUpDown { X = Pos.Right (lblPxY), Y = Pos.Bottom (_pxX), Value = GetDefaultFirePixelsPerRow () };

        Label paletteLabel = new () { Text = "Sixel Palette Builder", Width = Dim.Auto (), Y = Pos.Bottom (_pxY) + 1 };
        _osPaletteBuilder = new OptionSelector { Labels = ["Popularity", "Median Cut"], Y = Pos.Bottom (paletteLabel), Value = 1 };

        _popularityThreshold = new NumericUpDown { X = Pos.Right (_osPaletteBuilder) + 1, Y = Pos.Top (_osPaletteBuilder), Value = 8 };

        Label lblPopThreshold = new () { Text = "(threshold)", X = Pos.Right (_popularityThreshold), Y = Pos.Top (_popularityThreshold) };

        Label distanceLabel = new () { Text = "Sixel Color Distance", Width = Dim.Auto (), Y = Pos.Bottom (_osPaletteBuilder) + 1 };
        _osDistanceAlgorithm = new OptionSelector { Labels = ["Euclidean", "CIE76"], Y = Pos.Bottom (distanceLabel) };

        _rasterSettings.Add (lblPxX);
        _rasterSettings.Add (_pxX);
        _rasterSettings.Add (lblPxY);
        _rasterSettings.Add (_pxY);
        _rasterSettings.Add (paletteLabel);
        _rasterSettings.Add (_osPaletteBuilder);
        _rasterSettings.Add (distanceLabel);
        _rasterSettings.Add (_osDistanceAlgorithm);
        _rasterSettings.Add (_popularityThreshold);
        _rasterSettings.Add (lblPopThreshold);

        tabRaster.Add (_rasterImageView, _rasterSettings);
    }

    private void Driver_KittyGraphicsSupportChanged (object? sender, ValueChangedEventArgs<KittyGraphicsSupportResult?> e) =>
        UpdateRasterSupportState (_app.Driver?.SixelSupport, e.NewValue);

    private void Driver_SixelSupportChanged (object? sender, ValueChangedEventArgs<SixelSupportResult?> e) =>
        UpdateRasterSupportState (e.NewValue, _app.Driver?.KittyGraphicsSupport);

    private void GenerateRasterFire (bool addTimeout)
    {
        int pixelsPerColumn = Math.Max (1, _pxX.Value);
        int pixelsPerRow = Math.Max (1, _pxY.Value);
        Size fireCellSize = GetFireTargetCellSize ();
        _fire = new DoomFire (fireCellSize.Width * pixelsPerColumn, fireCellSize.Height * pixelsPerRow);
        _fireEncoder = new SixelEncoder { AvoidBottomScroll = IsSixelRasterPath () };

        if (_sixelSupportResult is { } sixel)
        {
            _fireEncoder.Quantizer.MaxColors = Math.Min (_fireEncoder.Quantizer.MaxColors, sixel.MaxPaletteColors);
        }

        _fireEncoder.Quantizer.PaletteBuildingAlgorithm = new ConstPalette (_fire.Palette);

        _fireFrameCounter = 0;

        if (addTimeout)
        {
            _app.AddTimeout (TimeSpan.FromMilliseconds (30), AdvanceFireTimerCallback);
        }
    }

    private Size GetFireTargetCellSize ()
    {
        Size frameSize = _win.Frame.Size;

        return new Size (Math.Max (1, frameSize.Width), Math.Max (1, frameSize.Height));
    }

    private Size GetFireRenderedCellSize ()
    {
        int pixelsPerColumn = Math.Max (1, _pxX.Value);
        int pixelsPerRow = Math.Max (1, _pxY.Value);
        Size fireCellSize = GetFireTargetCellSize ();
        Size resolution = GetPreferredRasterResolution ();
        int pixelWidth = fireCellSize.Width * pixelsPerColumn;
        int pixelHeight = fireCellSize.Height * pixelsPerRow;

        if (IsSixelRasterPath ())
        {
            pixelHeight = Math.Max (1, pixelHeight - 6);
        }

        return new Size (Math.Max (1, (pixelWidth + resolution.Width - 1) / resolution.Width),
                         Math.Max (1, (pixelHeight + resolution.Height - 1) / resolution.Height));
    }

    private Rectangle GetFireDestinationCells ()
    {
        Rectangle frameScreen = _win.FrameToScreen ();
        Size fireCellSize = GetFireRenderedCellSize ();

        return new Rectangle (frameScreen.X, Math.Max (frameScreen.Y, frameScreen.Bottom - fireCellSize.Height), fireCellSize.Width, fireCellSize.Height);
    }

    private int GetDefaultFirePixelsPerColumn () => Math.Max (1, GetPreferredRasterResolution ().Width);

    private int GetDefaultFirePixelsPerRow () => Math.Max (1, GetPreferredRasterResolution ().Height);

    private IColorDistance GetDistanceAlgorithm ()
    {
        switch (_osDistanceAlgorithm.Value)
        {
            case 0: return new EuclideanColorDistance ();

            case 1: return new CIE76ColorDistance ();

            default: throw new ArgumentOutOfRangeException ();
        }
    }

    private IPaletteBuilder GetPaletteBuilder ()
    {
        switch (_osPaletteBuilder.Value)
        {
            case 0: return new PopularityPaletteWithThreshold (GetDistanceAlgorithm (), _popularityThreshold.Value);

            case 1: return new MedianCutPaletteBuilder ();

            default: throw new ArgumentOutOfRangeException ();
        }
    }

    private Size GetPreferredRasterResolution ()
    {
        if (ShouldUseKittyGraphics () && _kittyGraphicsSupportResult is { IsSupported: true } kitty)
        {
            return kitty.Resolution;
        }

        if (_sixelSupportResult is { IsSupported: true } sixel)
        {
            return sixel.Resolution;
        }

        if (_kittyGraphicsSupportResult is { } detectedKitty)
        {
            return detectedKitty.Resolution;
        }

        if (_sixelSupportResult is { } detectedSixel)
        {
            return detectedSixel.Resolution;
        }

        return new Size (10, 20);
    }

    private bool IsImageViewKittyActive (bool rasterEnabled) => rasterEnabled && ShouldUseKittyGraphics ();

    private bool IsRasterAvailable () => _kittyGraphicsSupportResult is { IsSupported: true } || _sixelSupportResult is { IsSupported: true };

    private bool IsImageViewSixelActive (bool rasterEnabled) => rasterEnabled && !ShouldUseKittyGraphics () && _sixelSupportResult is { IsSupported: true };

    private bool IsSixelRasterPath () => !ShouldUseKittyGraphics () && _sixelSupportResult is { IsSupported: true };

    private void LoadDefaultImage ()
    {
        Color [,] image = ImagesTestCard.Create (ImagesTestCard.DEFAULT_WIDTH, ImagesTestCard.DEFAULT_HEIGHT);
        _cellImageView.Image = image;
        UpdateRasterImage (image);
    }

    private void LoadImage (string path, bool showError)
    {
        Image<Rgba32> img;

        try
        {
            img = Image.Load<Rgba32> (File.ReadAllBytes (path));
        }
        catch (Exception ex)
        {
            if (showError)
            {
                MessageBox.ErrorQuery (_app, "Could not open file", ex.Message, "Ok");
            }

            return;
        }

        _fullResImage?.Dispose ();
        _fullResImage = img;
        Color [,] image = ConvertToColorArray (img);
        _cellImageView.Image = image;
        UpdateRasterImage (image);
    }

    public static Color [,] ConvertToColorArray (Image<Rgba32> image)
    {
        int width = image.Width;
        int height = image.Height;
        Color [,] colors = new Color [width, height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                Rgba32 pixel = image [x, y];
                colors [x, y] = new Color (pixel.R, pixel.G, pixel.B);
            }
        }

        return colors;
    }

    private void OpenImage (object? sender, CommandEventArgs e)
    {
        OpenDialog ofd = new () { Title = "Open Image", AllowsMultipleSelection = false };
        _app.Run (ofd);

        if (ofd.Canceled)
        {
            ofd.Dispose ();

            return;
        }

        string path = ofd.FilePaths [0];

        ofd.Dispose ();

        if (string.IsNullOrWhiteSpace (path))
        {
            return;
        }

        if (!File.Exists (path))
        {
            return;
        }

        string? directoryName = Path.GetDirectoryName (path);

        if (!string.IsNullOrEmpty (directoryName))
        {
            Directory.SetCurrentDirectory (Path.GetFullPath (directoryName));
        }

        LoadImage (path, true);
        _app.LayoutAndDraw ();
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

    private void SetBackgroundRendering (bool enabled)
    {
        _cellImageView.UseBackgroundRendering = enabled;
        _rasterImageView.UseBackgroundRendering = enabled;
    }

    private void SetUseRasterGraphics (bool enabled)
    {
        _rasterImageView.UseRasterGraphics = enabled;
        _rasterImageView.SetNeedsDraw ();

        UpdateRasterCapabilityMatrix (enabled);
    }

    private static string SizeText (Size size) => $"{size.Width}x{size.Height} px/cell";

    private void UpdateRasterCapabilityMatrix (bool? rasterEnabledOverride = null)
    {
        bool rasterEnabled = rasterEnabledOverride ?? _cbUseRasterGraphics.Value != CheckState.UnChecked;
        bool kittyActive = IsImageViewKittyActive (rasterEnabled);
        bool sixelActive = IsImageViewSixelActive (rasterEnabled);
        bool cellActive = !rasterEnabled || (!kittyActive && !sixelActive);
        string driverName = _app.Driver?.GetName () ?? "unknown";
        bool trueColor = _app.Driver?.SupportsTrueColor == true && !Driver.Force16Colors;
        bool legacy = _app.Driver?.IsLegacyConsole == true;

        _driverStatus.Text = $"Driver: {driverName}; true color: {YesNo (trueColor)}; legacy console: {YesNo (legacy)}";
        _cellStatus.Text = Row ("Cell colors", "yes", "1 cell", cellActive ? "active" : "fallback");

        bool? kittySupported = _kittyGraphicsSupportResult is null ? null : _kittyGraphicsSupportResult.IsSupported;
        string kittyResolution = _kittyGraphicsSupportResult is { } kitty ? SizeText (kitty.Resolution) : "-";
        _kittyStatus.Text = Row ("Kitty", ProtocolState (kittySupported), kittyResolution, kittyActive ? "active" : "auto-preferred");

        bool? sixelSupported = _sixelSupportResult is null ? null : _sixelSupportResult.IsSupported;
        string sixelResolution = _sixelSupportResult is { } sixel ? SizeText (sixel.Resolution) : "-";

        string sixelNotes = _sixelSupportResult is { IsSupported: true } supportedSixel
                                ? $"{supportedSixel.MaxPaletteColors} colors; alpha {YesNo (supportedSixel.SupportsTransparency)}"
                                : "fallback after Kitty";
        _sixelStatus.Text = Row ("Sixel", ProtocolState (sixelSupported), sixelResolution, sixelActive ? $"active; {sixelNotes}" : sixelNotes);

        UpdateRasterProtocolSelector ();
        _selectedStatus.Text = $"Selected raster path: {GetSelectedRendererName (rasterEnabled, kittyActive, sixelActive)}";
        _win.SetNeedsDraw ();
    }

    private void UpdateRasterImage (Color [,] image)
    {
        SixelEncoder encoder = new ();

        if (_sixelSupportResult is { } sixel)
        {
            encoder.Quantizer.MaxColors = Math.Min (encoder.Quantizer.MaxColors, sixel.MaxPaletteColors);
        }

        encoder.Quantizer.PaletteBuildingAlgorithm = GetPaletteBuilder ();
        encoder.Quantizer.DistanceAlgorithm = GetDistanceAlgorithm ();
        _rasterImageView.SixelEncoder = encoder;
        _rasterImageView.Image = image;
    }

    private void UpdateRasterSupportState (SixelSupportResult? sixelResult, KittyGraphicsSupportResult? kittyResult)
    {
        _sixelSupportResult = sixelResult;
        _kittyGraphicsSupportResult = kittyResult;
        ApplyRasterProtocolSelection ();

        _pxX.Value = GetDefaultFirePixelsPerColumn ();
        _pxY.Value = GetDefaultFirePixelsPerRow ();

        if (_rasterImageView.Image is { } image)
        {
            UpdateRasterImage (image);
        }

        UpdateRasterCapabilityMatrix ();
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

    private string GetSelectedRendererName (bool rasterEnabled, bool kittyActive, bool sixelActive)
    {
        if (!rasterEnabled)
        {
            return "Cell renderer";
        }

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

    private bool ShouldUseKittyGraphics ()
    {
        if (_kittyGraphicsSupportResult is not { IsSupported: true })
        {
            return false;
        }

        if (_osRasterProtocol.Value == RASTER_PROTOCOL_SIXEL && _sixelSupportResult is { IsSupported: true })
        {
            return false;
        }

        return true;
    }

    private static string YesNo (bool value) => value ? "yes" : "no";

    private void Win_SubViewsLaidOut (object? sender, LayoutEventArgs e)
    {
        Size currentSize = _win.Viewport.Size;

        if (_winSize == currentSize)
        {
            return;
        }

        _winSize = currentSize;

        if (_fire is not null)
        {
            GenerateRasterFire (false);
        }
    }
}
