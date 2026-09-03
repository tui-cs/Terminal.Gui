using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Provides the main implementation of the driver abstraction layer for Terminal.Gui.
///     This implementation of <see cref="IDriver"/> coordinates the interaction between input processing, output
///     rendering,
///     screen size monitoring, and ANSI escape sequence handling.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="DriverImpl"/> implements <see cref="IDriver"/>,
///         serving as the central coordination point for console I/O operations. It delegates functionality
///         to specialized components:
///     </para>
///     <list type="bullet">
///         <item><see cref="IInputProcessor"/> - Processes keyboard and mouse input</item>
///         <item><see cref="IOutputBuffer"/> - Manages the screen buffer state</item>
///         <item><see cref="IOutput"/> - Handles actual console output rendering</item>
///         <item><see cref="AnsiRequestScheduler"/> - Manages ANSI escape sequence requests</item>
///         <item><see cref="ISizeMonitor"/> - Monitors terminal size changes</item>
///     </list>
///     <para>
///         This class is internal and should not be used directly by application code.
///         Applications interact with drivers through the <see cref="Application"/> class.
///     </para>
/// </remarks>
internal class DriverImpl : IDriver
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DriverImpl"/> class.
    /// </summary>
    /// <param name="componentFactory">The component factory that created the driver components.</param>
    /// <param name="inputProcessor">The input processor for handling keyboard and mouse events.</param>
    /// <param name="outputBuffer">The output buffer for managing screen state.</param>
    /// <param name="output">The output interface for rendering to the console.</param>
    /// <param name="ansiRequestScheduler">The scheduler for managing ANSI escape sequence requests.</param>
    /// <param name="sizeMonitor">The monitor for tracking terminal size changes.</param>
    public DriverImpl (IComponentFactory componentFactory,
                       IInputProcessor inputProcessor,
                       IOutputBuffer outputBuffer,
                       IOutput output,
                       AnsiRequestScheduler ansiRequestScheduler,
                       ISizeMonitor sizeMonitor) : this (componentFactory, inputProcessor, outputBuffer, output, ansiRequestScheduler, sizeMonitor, null)
    { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DriverImpl"/> class.
    /// </summary>
    /// <param name="componentFactory">The component factory that created the driver components.</param>
    /// <param name="inputProcessor">The input processor for handling keyboard and mouse events.</param>
    /// <param name="outputBuffer">The output buffer for managing screen state.</param>
    /// <param name="output">The output interface for rendering to the console.</param>
    /// <param name="ansiRequestScheduler">The scheduler for managing ANSI escape sequence requests.</param>
    /// <param name="sizeMonitor">The monitor for tracking terminal size changes.</param>
    /// <param name="ansiStartupGate">Optional startup-readiness gate for ANSI capability queries.</param>
    public DriverImpl (IComponentFactory componentFactory,
                       IInputProcessor inputProcessor,
                       IOutputBuffer outputBuffer,
                       IOutput output,
                       AnsiRequestScheduler ansiRequestScheduler,
                       ISizeMonitor sizeMonitor,
                       IAnsiStartupGate? ansiStartupGate)
    {
        _componentFactory = componentFactory;
        _inputProcessor = inputProcessor;
        _inputProcessor.KeyDown += (s, e) => KeyDown?.Invoke (s, e);
        _inputProcessor.KeyUp += (s, e) => KeyUp?.Invoke (s, e);
        _inputProcessor.SyntheticMouseEvent += (s, e) => MouseEvent?.Invoke (s, e);
        _inputProcessor.Paste += (s, e) => Paste?.Invoke (s, e);
        _outputBuffer = outputBuffer;
        _output = output;
        _ansiRequestScheduler = ansiRequestScheduler;
        _sizeMonitor = sizeMonitor;
        _sizeMonitor.SizeChanged += OnSizeMonitorOnSizeChanged;
        _terminalSize = new Size (outputBuffer.Cols, outputBuffer.Rows);
        AnsiStartupGate = ansiStartupGate;

        Clipboard = CreateClipboard ();

        Driver.Force16ColorsChanged += OnDriverOnForce16ColorsChanged;
    }

    #region Driver Lifecycle

    /// <inheritdoc/>
    public void Init () => throw new NotSupportedException ();

    /// <inheritdoc/>
    public void Refresh ()
    {
        // Hide cursor during rendering to prevent flicker
        Cursor cursor = _output.GetCursor ();

        if (cursor.IsVisible)
        {
            Cursor hiddenCursor = cursor with { Position = null, Style = cursor.Style };
            _output.SetCursor (hiddenCursor);
            SetCursorNeedsUpdate (true);
        }
        _output.Write (_outputBuffer);

        // Cursor visibility restored by ApplicationMainLoop to reduce flicker
    }

    /// <inheritdoc/>
    public string? GetName () => _componentFactory.GetDriverName ();

    /// <inheritdoc/>
    public void Suspend ()
    {
        try
        {
            _output.Suspend ();
        }
        catch (Exception ex)
        {
            Logging.Error ($"Error suspending terminal: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public bool IsLegacyConsole { get => _output.IsLegacyConsole; set => _output.IsLegacyConsole = value; }

    /// <inheritdoc/>
    public void Dispose ()
    {
        _sizeMonitor.SizeChanged -= OnSizeMonitorOnSizeChanged;
        Driver.Force16ColorsChanged -= OnDriverOnForce16ColorsChanged;
    }

    #endregion Driver Lifecycle

    #region Driver Components

    private readonly IComponentFactory _componentFactory;

    /// <inheritdoc/>
    public IOutputBuffer GetOutputBuffer () => _outputBuffer;

    private readonly IOutput _output;

    public IOutput GetOutput () => _output;

    private readonly IInputProcessor _inputProcessor;

    /// <inheritdoc/>
    public IInputProcessor GetInputProcessor () => _inputProcessor;

    private readonly IOutputBuffer _outputBuffer;

    private readonly ISizeMonitor _sizeMonitor;

    /// <inheritdoc/>
    public IAnsiStartupGate? AnsiStartupGate { get; }

    /// <inheritdoc/>
    public IClipboard? Clipboard { get; set; }

    /// <inheritdoc/>
    public ProgressIndicator? ProgressIndicator { get; internal set; }

    internal static IClipboard CreateClipboard (
        Func<bool> isWindows,
        Func<bool> isMac,
        Func<bool> isWsl,
        Func<bool> isLinux,
        Func<IClipboard> createWindowsClipboard,
        Func<IClipboard> createMacClipboard,
        Func<IClipboard> createWslClipboard,
        Func<IClipboard> createUnixClipboard,
        IClipboard fallbackClipboard
    )
    {
        if (isWindows ())
        {
            return createWindowsClipboard ();
        }

        if (isMac ())
        {
            return createMacClipboard ();
        }

        if (isWsl ())
        {
            return createWslClipboard ();
        }

        if (!isLinux ())
        {
            return fallbackClipboard;
        }

        IClipboard unixClipboard = createUnixClipboard ();

        return !unixClipboard.IsSupported ? fallbackClipboard : unixClipboard;
    }

    private static IClipboard CreateClipboard ()
    {
        PlatformID p = Environment.OSVersion.Platform;

        return CreateClipboard (() => p is PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows,
                                OperatingSystem.IsMacOS,
                                PlatformDetection.IsWSL,
                                OperatingSystem.IsLinux,
                                () => new WindowsClipboard (),
                                () => new MacOSXClipboard (),
                                () => new WSLClipboard (),
                                () => new UnixClipboard (),
                                new FakeClipboard ());
    }

    internal void InitializeProgressIndicator ()
    {
        if (IsLegacyConsole)
        {
            ProgressIndicator = null;

            return;
        }

        Driver.IsAttachedToTerminal (out _, out bool outputAttached);

        if (!ProgressIndicator.IsSupportedOutput (outputAttached, Console.IsOutputRedirected, Environment.GetEnvironmentVariable ("TERM")))
        {
            ProgressIndicator = null;

            return;
        }

        ProgressIndicator = new ProgressIndicator (this);
    }

    #endregion Driver Components

    #region Screen and Display

    private Size _terminalSize;

    /// <inheritdoc/>
    public Rectangle Screen => new (0, 0, _terminalSize.Width, _terminalSize.Height);

    /// <inheritdoc/>
    public virtual void SetScreenSize (int width, int height)
    {
        Trace.Lifecycle (nameof (DriverImpl), "SetScreenSize", $"{width}×{height}");
        _terminalSize = new Size (width, height);

        // Always update the output's knowledge of terminal size
        _output.SetSize (width, height);

        // In inline mode, the output buffer is sized to App.Screen (the inline region),
        // not the full terminal. Only resize the buffer in fullscreen mode.
        if (AppModel != AppModel.Inline)
        {
            _outputBuffer.SetSize (width, height);
        }

        SizeChanged?.Invoke (this, new SizeChangedEventArgs (new Size (width, height)));
    }

    /// <summary>
    ///     Resizes the output buffer independently of the terminal size.
    ///     Used in inline mode where the buffer represents the inline rendering region
    ///     (<see cref="IApplication.Screen"/>), not the full terminal.
    /// </summary>
    internal void ResizeOutputBuffer (int width, int height)
    {
        Trace.Lifecycle (nameof (DriverImpl), "ResizeOutputBuffer", $"{width}×{height}");
        _outputBuffer.SetSize (width, height);
    }

    /// <inheritdoc/>
    public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    private void OnSizeMonitorOnSizeChanged (object? _, SizeChangedEventArgs e) =>

        // Trace.Lifecycle (nameof (DriverImpl), "OnSizeMonitorOnSizeChanged", $"{e.Size?.Width}×{e.Size?.Height}");
        SetScreenSize (e.Size!.Value.Width, e.Size.Value.Height);

    /// <inheritdoc/>
    public int Cols { get => _outputBuffer.Cols; set => _outputBuffer.Cols = value; }

    /// <inheritdoc/>
    public int Rows { get => _outputBuffer.Rows; set => _outputBuffer.Rows = value; }

    /// <inheritdoc/>
    public int Left { get => _outputBuffer.Left; set => _outputBuffer.Left = value; }

    /// <inheritdoc/>
    public int Top { get => _outputBuffer.Top; set => _outputBuffer.Top = value; }

    /// <inheritdoc/>
    public Point InlinePosition { get; set; }

    /// <inheritdoc/>
    public AppModel AppModel { get; set; }

    #endregion Screen and Display

    #region Color Support

    /// <inheritdoc/>
    public SixelSupportResult? SixelSupport { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<SixelSupportResult?>>? SixelSupportChanged;

    /// <inheritdoc/>
    public void SetSixelSupport (SixelSupportResult result)
    {
        SixelSupportResult? old = SixelSupport;
        SixelSupport = result;
        SixelSupportChanged?.Invoke (this, new ValueChangedEventArgs<SixelSupportResult?> (old, result));
    }

    /// <inheritdoc/>
    public KittyGraphicsSupportResult? KittyGraphicsSupport { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<KittyGraphicsSupportResult?>>? KittyGraphicsSupportChanged;

    /// <inheritdoc/>
    public void SetKittyGraphicsSupport (KittyGraphicsSupportResult result)
    {
        KittyGraphicsSupportResult? old = KittyGraphicsSupport;
        KittyGraphicsSupport = result;

        // Kitty takes priority when supported.  Sixel is the fallback for terminals that lack
        // Kitty support (e.g. Windows Terminal).
        _output.UseKittyGraphics = result.IsSupported;

        KittyGraphicsSupportChanged?.Invoke (this, new ValueChangedEventArgs<KittyGraphicsSupportResult?> (old, result));
    }

    /// <inheritdoc/>
    public bool SupportsTrueColor => !IsLegacyConsole;

    /// <inheritdoc/>
    public bool Force16Colors { get => _output.Force16Colors; set => _output.Force16Colors = value; }

    /// <inheritdoc/>
    public Attribute? DefaultAttribute { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<Attribute?>>? DefaultAttributeChanged;

    /// <summary>
    ///     Sets the terminal's default attribute (queried via OSC 10/11).
    /// </summary>
    internal void SetDefaultAttribute (Attribute attr)
    {
        Attribute? old = DefaultAttribute;
        DefaultAttribute = attr;
        DefaultAttributeChanged?.Invoke (this, new ValueChangedEventArgs<Attribute?> (old, attr));
    }

    /// <inheritdoc/>
    public TerminalColorCapabilities? ColorCapabilities { get; private set; }

    /// <summary>
    ///     Sets the terminal's color capabilities (detected from environment variables).
    /// </summary>
    internal void SetColorCapabilities (TerminalColorCapabilities caps) => ColorCapabilities = caps;

    private void OnDriverOnForce16ColorsChanged (object? _, ValueChangedEventArgs<bool> e) => Force16Colors = e.NewValue;

    #endregion Color Support

    #region Content Buffer

    /// <inheritdoc/>
    public Cell [,]? Contents { get => _outputBuffer.Contents; set => _outputBuffer.Contents = value; }

    /// <inheritdoc/>
    public Region? Clip { get => _outputBuffer.Clip; set => _outputBuffer.Clip = value; }

    /// <inheritdoc/>
    public string? CurrentUrl { get => _outputBuffer.CurrentUrl; set => _outputBuffer.CurrentUrl = value; }

    /// <summary>Clears the <see cref="IDriver.Contents"/> of the driver.</summary>
    public void ClearContents ()
    {
        _outputBuffer.ClearContents ();
        ClearedContents?.Invoke (this, EventArgs.Empty);
        CurrentUrl = null;
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs>? ClearedContents;

    #endregion Content Buffer

    #region Drawing and Rendering

    /// <inheritdoc/>
    public int Col => _outputBuffer.Col;

    /// <inheritdoc/>
    public int Row => _outputBuffer.Row;

    /// <inheritdoc/>
    public Attribute CurrentAttribute { get => _outputBuffer.CurrentAttribute; set => _outputBuffer.CurrentAttribute = value; }

    /// <inheritdoc/>
    public void Move (int col, int row) => _outputBuffer.Move (col, row);

    /// <inheritdoc/>
    public bool IsRuneSupported (Rune rune) => Rune.IsValid (rune.Value);

    /// <summary>Tests whether the specified coordinate are valid for drawing the specified Text.</summary>
    /// <param name="text">Used to determine if one or two columns are required.</param>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <returns>
    ///     <see langword="false"/> if the coordinate is outside the screen bounds or outside of
    ///     <see cref="IDriver.Clip"/>.
    ///     <see langword="true"/> otherwise.
    /// </returns>
    public bool IsValidLocation (string text, int col, int row) => _outputBuffer.IsValidLocation (text, col, row);

    /// <inheritdoc/>
    public void AddRune (Rune rune) => _outputBuffer.AddRune (rune);

    /// <inheritdoc/>
    public void AddRune (char c) => _outputBuffer.AddRune (c);

    /// <inheritdoc/>
    public void AddStr (string str) => _outputBuffer.AddStr (str);

    /// <inheritdoc/>
    public void FillRect (Rectangle rect, Rune rune = default) => _outputBuffer.FillRect (rect, rune);

    /// <inheritdoc/>
    public Attribute SetAttribute (Attribute newAttribute)
    {
        Attribute currentAttribute = _outputBuffer.CurrentAttribute;
        _outputBuffer.CurrentAttribute = newAttribute;

        return currentAttribute;
    }

    /// <inheritdoc/>
    public Attribute GetAttribute () => _outputBuffer.CurrentAttribute;

    /// <inheritdoc/>
    public void WriteRaw (string ansi) => _output.Write (ansi);

    /// <inheritdoc/>
    public void SetTerminalTitle (string title, int mode = 0)
    {
        if (IsLegacyConsole)
        {
            return;
        }

        WriteRaw (EscSeqUtils.OSC_SetWindowTitle (title, mode));
    }

    /// <inheritdoc/>
    public ConcurrentQueue<SixelToRender> GetSixels () => _output.GetSixels ();

    /// <inheritdoc/>
    public new string ToString ()
    {
        StringBuilder sb = new ();

        Cell [,] contents = Contents!;

        for (var r = 0; r < Rows; r++)
        {
            for (var c = 0; c < Cols; c++)
            {
                string text = contents [r, c].Grapheme;

                sb.Append (text);

                if (text.GetColumns () > 1)
                {
                    c++;
                }
            }

            sb.AppendLine ();
        }

        return sb.ToString ();
    }

    /// <inheritdoc/>
    public string ToAnsi () => _output.ToAnsi (_outputBuffer);

    #endregion Drawing and Rendering

    #region Cursor

    /// <inheritdoc/>
    public void SetCursor (Cursor cursor) => _output.SetCursor (cursor);

    /// <inheritdoc/>
    public Cursor GetCursor () => _output.GetCursor ();

    // Cursor caching fields
    private bool _cursorNeedsUpdate;

    /// <inheritdoc/>
    public bool GetCursorNeedsUpdate () => _cursorNeedsUpdate;

    /// <param name="needsUpdate"></param>
    /// <inheritdoc/>
    public void SetCursorNeedsUpdate (bool needsUpdate) => _cursorNeedsUpdate = needsUpdate;

    #endregion Cursor

    #region Input Events

    /// <inheritdoc/>
    public KittyKeyboardCapabilities? KittyKeyboardCapabilities { get; private set; }

    /// <summary>
    ///     Stores the detected kitty keyboard protocol capabilities.
    /// </summary>
    /// <param name="capabilities">The detected kitty keyboard capabilities.</param>
    internal void SetKittyKeyboardCapabilities (KittyKeyboardCapabilities capabilities) => KittyKeyboardCapabilities = capabilities;

    /// <summary>Event fired when a key is pressed down.</summary>
    public event EventHandler<Key>? KeyDown;

    /// <summary>Event fired when a key is released.</summary>
    public event EventHandler<Key>? KeyUp;

    /// <summary>Event fired when a mouse event occurs.</summary>
    public event EventHandler<Mouse>? MouseEvent;

    /// <summary>Event fired when a bracketed paste is received from the terminal.</summary>
    public event EventHandler<string>? Paste;

    #endregion Input Events

    #region ANSI Escape Sequences

    private readonly AnsiRequestScheduler _ansiRequestScheduler;

    /// <inheritdoc/>
    public virtual void QueueAnsiRequest (AnsiEscapeSequenceRequest request) => _ansiRequestScheduler.SendOrSchedule (this, request);

    #endregion ANSI Escape Sequences
}
