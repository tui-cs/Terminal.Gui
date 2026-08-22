namespace Terminal.Gui.Views;

/// <summary>Specifies the style that a <see cref="ProgressBar"/> uses to indicate the progress of an operation.</summary>
public enum ProgressBarStyle
{
    /// <summary>Indicates progress by increasing the number of segmented blocks in a <see cref="ProgressBar"/>.</summary>
    Blocks,

    /// <summary>Indicates progress by increasing the size of a smooth, continuous bar in a <see cref="ProgressBar"/>.</summary>
    Continuous,

    /// <summary>Indicates progress by continuously scrolling a block across a <see cref="ProgressBar"/> in a marquee fashion.</summary>
    MarqueeBlocks,

    /// <summary>Indicates progress by continuously scrolling a block across a <see cref="ProgressBar"/> in a marquee fashion.</summary>
    MarqueeContinuous,

    /// <summary>Indicates progress by filling the progress area with a sixel-rendered Doom fire effect when available.</summary>
    Fire
}

/// <summary>Specifies the format that a <see cref="ProgressBar"/> uses to indicate the visual presentation.</summary>
public enum ProgressBarFormat
{
    /// <summary>A simple visual presentation showing only the progress bar.</summary>
    Simple,

    /// <summary>A simple visual presentation showing the progress bar overlaid with the percentage.</summary>
    SimplePlusPercentage
}

/// <summary>A Progress Bar view that can indicate progress of an activity visually.</summary>
/// <remarks>
///     <img src="../images/views/ProgressBar.gif" alt="ProgressBar demo"/>
///     <para>
///         <see cref="ProgressBar"/> can operate in two modes, percentage mode, or activity mode. The progress bar
///         starts in percentage mode and setting the Fraction property will reflect on the UI the progress made so far.
///         Activity mode is used when the application has no way of knowing how much time is left, and is started when the
///         <see cref="Pulse"/> method is called. Call <see cref="Pulse"/> repeatedly as progress is made.
///     </para>
/// </remarks>
public class ProgressBar : View, IDesignable
{
    private static readonly ProgressBarStyle [] _progressBarStyles =
    [
        ProgressBarStyle.Blocks, ProgressBarStyle.Continuous, ProgressBarStyle.MarqueeBlocks, ProgressBarStyle.MarqueeContinuous, ProgressBarStyle.Fire
    ];

    private static readonly Color [] _firePalette =
    [
        new (7, 7, 7),
        new (31, 7, 7),
        new (47, 15, 7),
        new (71, 15, 7),
        new (87, 23, 7),
        new (103, 31, 7),
        new (119, 31, 7),
        new (143, 39, 7),
        new (159, 47, 7),
        new (175, 63, 7),
        new (191, 71, 7),
        new (199, 71, 7),
        new (223, 79, 7),
        new (223, 87, 7),
        new (223, 87, 7),
        new (215, 95, 7),
        new (215, 95, 7),
        new (215, 103, 15),
        new (207, 111, 15),
        new (207, 119, 15),
        new (207, 127, 15),
        new (207, 135, 23),
        new (199, 135, 23),
        new (199, 143, 23),
        new (199, 151, 31),
        new (191, 159, 31),
        new (191, 159, 31),
        new (191, 167, 39),
        new (191, 167, 39),
        new (191, 175, 47),
        new (183, 175, 47),
        new (183, 183, 47),
        new (183, 183, 55),
        new (207, 207, 111),
        new (223, 223, 159),
        new (239, 239, 199),
        new (255, 255, 255)
    ];

    private int []? _activityPos;
    private int _delta;
    private SixelEncoder? _fireEncoder;
    private int _fireEncoderMaxColors;
    private int _fireFrame;
    private readonly string _fireRasterImageId = $"{nameof (ProgressBar)}.{Guid.NewGuid ():N}.Fire";
    private bool _fireRasterImageActive;
    private float _fraction;
    private bool _isActivity;
    private bool _syncWithTerminal;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProgressBar"/> class, starts in percentage mode and uses relative
    ///     layout.
    /// </summary>
    public ProgressBar ()
    {
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content, 1);
        CanFocus = false;
        _fraction = 0;
        SetTextDirect ("0%");
    }

    /// <summary>
    ///     Specifies if the <see cref="ProgressBarStyle.MarqueeBlocks"/> or the
    ///     <see cref="ProgressBarStyle.MarqueeContinuous"/> styles is unidirectional or bidirectional.
    /// </summary>
    public bool BidirectionalMarquee { get; set; } = true;

    /// <summary>Gets or sets the <see cref="ProgressBar"/> fraction to display, must be a value between 0 and 1.</summary>
    /// <value>The fraction representing the progress.</value>
    public float Fraction
    {
        get => _fraction;
        set
        {
            _fraction = Math.Min (value, 1);
            _isActivity = false;
            SetTextDirect ($"{_fraction * 100:F0}%");
            UpdateTerminalProgress ();
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this <see cref="ProgressBar"/> uses the driver's terminal
    ///     <see cref="ProgressIndicator"/> to emit OSC 9;4 progress sequences when supported.
    /// </summary>
    /// <remarks>
    ///     This is independent of <see cref="View.Visible"/> so the terminal progress indicator can continue to be used
    ///     even when the on-screen <see cref="ProgressBar"/> is hidden.
    /// </remarks>
    public bool SyncWithTerminal
    {
        get => _syncWithTerminal;
        set
        {
            if (_syncWithTerminal == value)
            {
                return;
            }

            _syncWithTerminal = value;

            if (value)
            {
                UpdateTerminalProgress ();
            }
            else
            {
                ClearTerminalProgress ();
            }
        }
    }

    /// <summary>Specifies the format that a <see cref="ProgressBar"/> uses to indicate the visual presentation.</summary>
    public ProgressBarFormat ProgressBarFormat { get; set; } = ProgressBarFormat.Simple;

    /// <summary>Gets/Sets the progress bar style based on the <see cref="Views.ProgressBarStyle"/></summary>
    public ProgressBarStyle ProgressBarStyle
    {
        get;
        set
        {
            switch (value)
            {
                case ProgressBarStyle.Blocks:
                    SegmentCharacter = Glyphs.BlocksMeterSegment;

                    break;

                case ProgressBarStyle.Continuous:
                    SegmentCharacter = Glyphs.ContinuousMeterSegment;

                    break;

                case ProgressBarStyle.MarqueeBlocks:
                    SegmentCharacter = Glyphs.BlocksMeterSegment;

                    break;

                case ProgressBarStyle.MarqueeContinuous:
                    SegmentCharacter = Glyphs.ContinuousMeterSegment;

                    break;

                case ProgressBarStyle.Fire:
                    SegmentCharacter = Glyphs.ContinuousMeterSegment;

                    break;
            }

            if (field == ProgressBarStyle.Fire && value != ProgressBarStyle.Fire)
            {
                RemoveFireRasterImage ();
            }

            field = value;
            SetNeedsDraw ();
        }
    } = ProgressBarStyle.Blocks;

    /// <summary>Segment indicator for meter views.</summary>
    public Rune SegmentCharacter { get; set; } = Glyphs.BlocksMeterSegment;

    /// <summary>
    ///     Gets or sets the text displayed on the progress bar. If set to an empty string and
    ///     <see cref="ProgressBarFormat"/> is <see cref="ProgressBarFormat.SimplePlusPercentage"/> the percentage will be
    ///     displayed. If <see cref="ProgressBarStyle"/> is a marquee style, the text will be displayed.
    /// </summary>
    protected override bool OnTextChanging (string newText)
    {
        // Only allow text to be set externally in marquee mode
        if (ProgressBarStyle is not (ProgressBarStyle.MarqueeBlocks or ProgressBarStyle.MarqueeContinuous or ProgressBarStyle.Fire))
        {
            return true;
        }

        return base.OnTextChanging (newText);
    }

    ///<inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        SetAttribute (GetAttributeForRole (VisualRole.Normal));

        Move (0, 0);

        if (_isActivity)
        {
            RemoveFireRasterImage ();

            for (var i = 0; i < Viewport.Width; i++)
            {
                if (Array.IndexOf (_activityPos!, i) != -1)
                {
                    AddRune (SegmentCharacter);
                }
                else
                {
                    AddRune ((Rune)' ');
                }
            }
        }
        else
        {
            var mid = (int)(_fraction * Viewport.Width);

            if (ProgressBarStyle == ProgressBarStyle.Fire && DrawFireProgress (mid))
            {
                Move (Math.Clamp (mid, 0, Viewport.Width), 0);

                for (int i = Math.Clamp (mid, 0, Viewport.Width); i < Viewport.Width; i++)
                {
                    AddRune ((Rune)' ');
                }
            }
            else
            {
                RemoveFireRasterImage ();

                int i;

                for (i = 0; i < Math.Clamp (mid, 0, Viewport.Width); i++)
                {
                    AddRune (SegmentCharacter);
                }

                for (; i < Viewport.Width; i++)
                {
                    AddRune ((Rune)' ');
                }
            }
        }

        if (ProgressBarFormat == ProgressBarFormat.Simple || _isActivity)
        {
            return true;
        }

        TextFormatter tf = new () { Alignment = Alignment.Center, Text = Text };

        Attribute attr = new (GetAttributeForRole (VisualRole.Normal).Foreground,
                              GetAttributeForRole (VisualRole.Normal).Background,
                              GetAttributeForRole (VisualRole.Normal).Style);

        if (_fraction > .5)
        {
            attr = new Attribute (GetAttributeForRole (VisualRole.Normal).Background,
                                  GetAttributeForRole (VisualRole.Normal).Foreground,
                                  GetAttributeForRole (VisualRole.Normal).Style);
        }

        tf.Draw (Driver,
                 ViewportToScreen (Viewport),
                 attr,
                 GetAttributeForRole (VisualRole.Normal),
                 SuperView?.ViewportToScreen (SuperView.Viewport) ?? default (Rectangle));

        return true;
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? commandContext)
    {
        base.OnActivated (commandContext);

        if (!CanFocus)
        {
            return;
        }

        CycleProgressBarStyle ();
    }

    /// <summary>Notifies the <see cref="ProgressBar"/> that some progress has taken place.</summary>
    /// <remarks>
    ///     If the <see cref="ProgressBar"/> is percentage mode, it switches to activity mode. If is in activity mode, the
    ///     marker is moved.
    /// </remarks>
    public void Pulse ()
    {
        if (_activityPos is null || _activityPos.Length == 0)
        {
            PopulateActivityPos ();
        }

        if (_activityPos!.Length == 0)
        {
            return;
        }

        if (!_isActivity)
        {
            _isActivity = true;
            _delta = 1;
        }
        else
        {
            for (var i = 0; i < _activityPos.Length; i++)
            {
                _activityPos [i] += _delta;
            }

            if (_activityPos [^1] < 0)
            {
                for (var i = 0; i < _activityPos.Length; i++)
                {
                    _activityPos [i] = i - _activityPos.Length + 2;
                }

                _delta = 1;
            }
            else if (_activityPos [0] >= Viewport.Width)
            {
                if (BidirectionalMarquee)
                {
                    for (var i = 0; i < _activityPos.Length; i++)
                    {
                        _activityPos [i] = Viewport.Width + i - 2;
                    }

                    _delta = -1;
                }
                else
                {
                    PopulateActivityPos ();
                }
            }
        }

        UpdateTerminalProgress ();
        SetNeedsDraw ();
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing && _syncWithTerminal)
        {
            ClearTerminalProgress ();
        }

        if (disposing)
        {
            RemoveFireRasterImage ();
        }

        base.Dispose (disposing);
    }

    private void ClearTerminalProgress () => Driver?.ProgressIndicator?.Clear ();

    private void CycleProgressBarStyle ()
    {
        int current = Array.IndexOf (_progressBarStyles, ProgressBarStyle);

        if (current < 0)
        {
            ProgressBarStyle = ProgressBarStyle.Blocks;

            return;
        }

        ProgressBarStyle = _progressBarStyles [(current + 1) % _progressBarStyles.Length];
    }

    private bool DrawFireProgress (int filledCells)
    {
        filledCells = Math.Clamp (filledCells, 0, Viewport.Width);

        if (filledCells <= 0 || Viewport.Height <= 0 || Driver is not { } driver)
        {
            return false;
        }

        // The fire is a raster image, so it works under either raster protocol. Prefer Kitty when it
        // is the active output (e.g. Kitty/Ghostty report no Sixel support), otherwise fall back to
        // Sixel. Without this the fire silently disappears on Kitty-only terminals.
        bool kittyActive = driver.KittyGraphicsSupport is { IsSupported: true } && driver.GetOutput ().UseKittyGraphics;
        SixelSupportResult? sixel = driver.SixelSupport;

        if (!kittyActive && sixel is not { IsSupported: true })
        {
            return false;
        }

        Size resolution = kittyActive ? driver.KittyGraphicsSupport!.Resolution : sixel!.Resolution;
        int cellWidthPixels = Math.Max (1, resolution.Width);
        int cellHeightPixels = Math.Max (1, resolution.Height);
        int pixelWidth = Math.Max (1, filledCells * cellWidthPixels);
        int pixelHeight = Math.Max (1, Viewport.Height * cellHeightPixels);

        RasterImageCommand command = new ()
        {
            Id = _fireRasterImageId,
            Pixels = CreateFirePixels (pixelWidth, pixelHeight, _fireFrame++),
            DestinationCells = ViewportToScreen (new Rectangle (0, 0, filledCells, Viewport.Height)),

            // The Sixel encoder is ignored when Kitty output is active; only build it for the Sixel path.
            Encoder = kittyActive ? null : GetFireEncoder (sixel!)
        };

        driver.GetOutputBuffer ().AddRasterImage (command);
        _fireRasterImageActive = true;

        return true;
    }

    private SixelEncoder GetFireEncoder (SixelSupportResult support)
    {
        int maxColors = Math.Min (support.MaxPaletteColors, _firePalette.Length);

        if (_fireEncoder is { } encoder && _fireEncoderMaxColors == maxColors)
        {
            return encoder;
        }

        _fireEncoderMaxColors = maxColors;
        _fireEncoder = new ()
        {
            Quantizer =
            {
                MaxColors = maxColors,
                PaletteBuildingAlgorithm = new FirePaletteBuilder ()
            }
        };

        return _fireEncoder;
    }

    private static Color [,] CreateFirePixels (int width, int height, int frame)
    {
        Color [,] pixels = new Color [width, height];
        int maxIntensity = _firePalette.Length - 1;

        for (var y = 0; y < height; y++)
        {
            double vertical = 1d - (double)y / Math.Max (1, height - 1);

            for (var x = 0; x < width; x++)
            {
                double wave = (Math.Sin ((x + frame * 3) * 0.18d) + Math.Sin (x * 0.11d - frame * 0.27d)) * 0.08d;
                double flicker = (((x * 17 + y * 31 + frame * 13) & 15) - 7) / 110d;
                double heat = Math.Clamp (vertical + wave + flicker, 0, 1);
                int intensity = Math.Clamp ((int)Math.Round (heat * maxIntensity), 0, maxIntensity);
                pixels [x, y] = _firePalette [intensity];
            }
        }

        return pixels;
    }

    private int GetProgressPercentage () => Math.Clamp ((int)Math.Round (_fraction * 100), 0, 100);

    private sealed class FirePaletteBuilder : IStaticPaletteBuilder
    {
        public List<Color> BuildPalette (List<Color> colors, int maxColors) => BuildPalette (maxColors);

        public List<Color> BuildPalette (int maxColors) => [.. _firePalette.Take (maxColors)];
    }

    private void RemoveFireRasterImage ()
    {
        _fireRasterImageActive = false;
        Driver?.GetOutputBuffer ().RemoveRasterImage (_fireRasterImageId);
    }

    /// <inheritdoc/>
    internal override void CollectActiveRasterImageIds (HashSet<string> ids)
    {
        if (!Visible)
        {
            return;
        }

        base.CollectActiveRasterImageIds (ids);

        if (_fireRasterImageActive)
        {
            ids.Add (_fireRasterImageId);
        }
    }

    private void UpdateTerminalProgress ()
    {
        if (!_syncWithTerminal || Driver?.ProgressIndicator is not { } progressIndicator)
        {
            return;
        }

        if (_isActivity)
        {
            progressIndicator.SetIndeterminate ();

            return;
        }

        progressIndicator.SetValue (GetProgressPercentage ());
    }

    private void PopulateActivityPos ()
    {
        _activityPos = new int [Math.Min (Frame.Width / 3, 5)];

        for (var i = 0; i < _activityPos.Length; i++)
        {
            _activityPos [i] = i - _activityPos.Length + 1;
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Width = Dim.Fill ();
        Height = Dim.Auto (DimAutoStyle.Text, 1);
        CanFocus = true;
        Fraction = 0.75f;

        return true;
    }

    /// <inheritdoc/>
    public string GetDemoKeyStrokes () => "wait:2000";
}
