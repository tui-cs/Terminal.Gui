namespace Terminal.Gui.Views;

/// <summary>Displays a spinning glyph or combinations of glyphs to indicate progress or activity</summary>
/// <remarks>
/// <img src="../images/views/SpinnerView.gif" alt="SpinnerView demo"/>
///     By default, animation only occurs when you call <see cref="SpinnerView.AdvanceAnimation(bool)"/>. Use
///     <see cref="AutoSpin"/> to make the automate calls to <see cref="SpinnerView.AdvanceAnimation(bool)"/>.
/// </remarks>
public class SpinnerView : View, IDesignable
{
    private const int DEFAULT_DELAY = 130;
    private static readonly SpinnerStyle _defaultStyle = new SpinnerStyle.Line ();

    private bool _autoSpin;
    private bool _bounce;
    private bool _bounceReverse;
    private int _currentIdx;
    private int _delay;
    private DateTime _lastRender = DateTime.MinValue;
    private string [] _sequence = _defaultStyle.Sequence;
    private SpinnerStyle _style = _defaultStyle;
    private object? _timeout;
    private bool _syncWithTerminal;

    /// <summary>Creates a new instance of the <see cref="SpinnerView"/> class.</summary>
    public SpinnerView ()
    {
        Width = Dim.Auto (minimumContentDim: 1);
        Height = Dim.Auto (minimumContentDim: 1);
        _delay = DEFAULT_DELAY;
        _bounce = false;
        SpinReverse = false;
        SetStyle (_defaultStyle);

        AdvanceAnimation ();
    }

    /// <summary>
    ///     Gets or sets whether spinning should occur automatically or be manually triggered (e.g. from a background
    ///     task).
    /// </summary>
    public bool AutoSpin
    {
        get => _autoSpin;
        set
        {
            _autoSpin = value;

            if (value)
            {
                AddAutoSpinTimeout ();
            }
            else
            {
                RemoveAutoSpinTimeout ();
            }

            SyncProgressIndicator ();
        }
    }

    /// <summary>
    ///     Gets or sets whether <see cref="SpinnerView"/> should mirror <see cref="AutoSpin"/> to the driver's terminal
    ///     <see cref="ProgressIndicator"/>.
    /// </summary>
    public bool SyncWithTerminal
    {
        get => _syncWithTerminal;
        set
        {
            if (_syncWithTerminal == value)
            {
                return;
            }

            if (!value)
            {
                ClearProgressIndicator ();
                _syncWithTerminal = value;

                return;
            }

            _syncWithTerminal = value;
            SyncProgressIndicator ();
        }
    }

    /// <summary>
    ///     Gets whether the current spinner style contains emoji or other special characters. Does not check Custom
    ///     sequences.
    /// </summary>
    public bool HasSpecialCharacters => _style.HasSpecialCharacters;

    /// <summary>Gets whether the current spinner style contains only ASCII characters.  Also checks Custom sequences.</summary>
    public bool IsAsciiOnly => GetIsAsciiOnly ();

    /// <summary>Gets or sets the animation frames used to animate the spinner.</summary>
    public string [] Sequence { get => _sequence; set => SetSequence (value); }

    /// <summary>
    ///     Gets or sets whether spinner should go back and forth through the frames rather than going to the end and
    ///     starting again at the beginning.
    /// </summary>
    public bool SpinBounce { get => _bounce; set => SetBounce (value); }

    /// <summary>Gets or sets the number of milliseconds to wait between characters in the animation.</summary>
    /// <remarks>
    ///     This is the maximum speed the spinner will rotate at.  You still need to call
    ///     <see cref="SpinnerView.AdvanceAnimation(bool)"/> or <see cref="SpinnerView.AutoSpin"/> to advance/start animation.
    /// </remarks>
    public int SpinDelay { get => _delay; set => SetDelay (value); }

    /// <summary>
    ///     Gets or sets whether spinner should go through the frames in reverse order. If SpinBounce is true, this sets
    ///     the starting order.
    /// </summary>
    public bool SpinReverse { get; set; }

    /// <summary>Gets or sets the Style used to animate the spinner.</summary>
    public SpinnerStyle Style { get => _style; set => SetStyle (value); }

    /// <summary>
    ///     Advances the animation frame and notifies main loop that repainting needs to happen. Repeated calls are
    ///     ignored based on <see cref="SpinDelay"/>.
    /// </summary>
    /// <remarks>
    ///     Ensure this method is called on the main UI thread e.g. via <see cref="IApplication.Invoke(Action)"/>
    /// </remarks>
    public void AdvanceAnimation (bool setNeedsDraw = true)
    {
        if (DateTime.Now - _lastRender > TimeSpan.FromMilliseconds (SpinDelay))
        {
            if (Sequence is { Length: > 1 })
            {
                var d = 1;

                if ((_bounceReverse && !SpinReverse) || (!_bounceReverse && SpinReverse))
                {
                    d = -1;
                }

                _currentIdx += d;

                if (_currentIdx >= Sequence.Length)
                {
                    if (SpinBounce)
                    {
                        _bounceReverse = !SpinReverse;

                        _currentIdx = Sequence.Length - 1;
                    }
                    else
                    {
                        _currentIdx = 0;
                    }
                }

                if (_currentIdx < 0)
                {
                    if (SpinBounce)
                    {
                        _bounceReverse = SpinReverse;

                        _currentIdx = 1;
                    }
                    else
                    {
                        _currentIdx = Sequence.Length - 1;
                    }
                }
            }

            _lastRender = DateTime.Now;
        }

        if (setNeedsDraw)
        {
            SetNeedsDraw ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnClearingViewport () => true;

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        Render ();

        return true;
    }

    /// <summary>
    ///     Renders the current frame of the spinner.
    /// </summary>
    public void Render ()
    {
        if (Sequence is not { Length: > 0 } || _currentIdx >= Sequence.Length)
        {
            return;
        }
        Move (Viewport.X, Viewport.Y);
        AddStr (Sequence [_currentIdx]);
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        RemoveAutoSpinTimeout ();

        if (disposing)
        {
            ClearProgressIndicator ();
        }

        base.Dispose (disposing);
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        if (_autoSpin)
        {
            AddAutoSpinTimeout ();
        }

        SyncProgressIndicator ();
    }

    private void AddAutoSpinTimeout ()
    {
        // Only add timeout if we are initialized and not already spinning
        if (App is { } && (_timeout is { } || !App.Initialized))
        {
            return;
        }

        _timeout = App?.AddTimeout (TimeSpan.FromMilliseconds (SpinDelay),
                                    () =>
                                    {
                                        App?.Invoke (_ => AdvanceAnimation ());

                                        return true;
                                    });
    }

    private bool GetIsAsciiOnly ()
    {
        if (HasSpecialCharacters)
        {
            return false;
        }

        return _sequence is not { Length: > 0 } || _sequence.SelectMany (frame => frame).All (char.IsAscii);
    }

    private int GetSpinnerWidth ()
    {
        var max = 0;

        if (_sequence is not { Length: > 0 })
        {
            return max;
        }

        foreach (string frame in _sequence)
        {
            if (frame.Length > max)
            {
                max = frame.Length;
            }
        }

        return max;
    }

    private void RemoveAutoSpinTimeout ()
    {
        if (_timeout is null)
        {
            return;
        }
        App?.RemoveTimeout (_timeout);
        _timeout = null;
    }

    private void ClearProgressIndicator ()
    {
        if (!_syncWithTerminal)
        {
            return;
        }

        Driver?.ProgressIndicator?.Clear ();
    }

    private void SetBounce (bool bounce) => _bounce = bounce;

    private void SetDelay (int delay)
    {
        if (delay > -1)
        {
            _delay = delay;
        }
    }

    private void SetSequence (string [] frames)
    {
        if (frames is not { Length: > 0 })
        {
            return;
        }
        _style = new SpinnerStyle.Custom ();
        _sequence = frames;
        Width = GetSpinnerWidth ();
    }

    private void SetStyle (SpinnerStyle? style)
    {
        if (style is null)
        {
            return;
        }
        _style = style;
        _sequence = style.Sequence;
        _delay = style.SpinDelay;
        _bounce = style.SpinBounce;
        Width = GetSpinnerWidth ();
    }

    private void SyncProgressIndicator ()
    {
        if (!_syncWithTerminal)
        {
            return;
        }

        if (_autoSpin)
        {
            Driver?.ProgressIndicator?.SetIndeterminate ();

            return;
        }

        Driver?.ProgressIndicator?.Clear ();
    }

    bool IDesignable.EnableForDesign ()
    {
        Style = new SpinnerStyle.Dots2 ();
        AutoSpin = true;

        return true;
    }

    /// <inheritdoc/>
    string? IDesignable.GetDemoKeyStrokes () => "wait:3000";
}
