using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

public partial class View // Text Property APIs
{
    private string _text = string.Empty;

    /// <summary>
    ///     Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved
    ///     or not when <see cref="Text.TextFormatter.WordWrap"/> is enabled.
    ///     If <see langword="true"/> trailing spaces at the end of wrapped lines will be removed when
    ///     <see cref="Text"/> is formatted for display. The default is <see langword="false"/>.
    /// </summary>
    public bool PreserveTrailingSpaces
    {
        get => TextFormatter.PreserveTrailingSpaces;
        set
        {
            if (TextFormatter.PreserveTrailingSpaces == value)
            {
                return;
            }
            TextFormatter.PreserveTrailingSpaces = value;
            TextFormatter.NeedsFormat = true;
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     The text displayed by the <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The text will be drawn before any subviews are drawn.
    ///     </para>
    ///     <para>
    ///         The text will be drawn starting at the view origin (0, 0) and will be formatted according
    ///         to <see cref="TextAlignment"/> and <see cref="TextDirection"/>.
    ///     </para>
    ///     <para>
    ///         The text will word-wrap to additional lines if it does not fit horizontally. If <see cref="GetContentSize ()"/>
    ///         's height
    ///         is 1, the text will be clipped.
    ///     </para>
    ///     <para>
    ///         If <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="DimAutoStyle.Text"/>,
    ///         the <see cref="GetContentSize ()"/> will be adjusted to fit the text.
    ///     </para>
    ///     <para>
    ///         Setting <see cref="Text"/> to the same value as the current value is a no-op; neither
    ///         <see cref="TextChanging"/> nor <see cref="TextChanged"/> will be raised.
    ///     </para>
    ///     <para>
    ///         Before the text is changed, the <see cref="TextChanging"/> CWP hook is invoked. If cancelled,
    ///         the text remains unchanged and <see cref="TextChanged"/> is not raised.
    ///     </para>
    ///     <para>
    ///         After the text is changed, the <see cref="TextChanged"/> event is raised.
    ///     </para>
    /// </remarks>
    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            if (OnTextChanging (value))
            {
                return;
            }

            _text = value;
            UpdateTextFormatterText ();

            if (TryRedrawWithoutLayout ())
            {
                RaiseTextChanged ();

                return;
            }

            SetNeedsLayout ();
            RaiseTextChanged ();
        }
    }

    /// <summary>
    ///     Sets the <see cref="Text"/> backing field directly without raising <see cref="TextChanging"/>
    ///     or <see cref="TextChanged"/> events and without invoking the <see cref="OnTextChanging"/>
    ///     or <see cref="OnTextChanged"/> virtual methods.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use this method in derived views that maintain an internal text model (e.g., an editor
    ///         buffer) and need to keep <see cref="Text"/> in sync after internal edits without
    ///         re-entering the CWP flow.
    ///     </para>
    /// </remarks>
    /// <param name="value">The new text value to store.</param>
    protected void SetTextDirect (string value)
    {
        _text = value;
        UpdateTextFormatterText ();
        SetNeedsLayout ();
    }

    /// <summary>
    ///     Called before the <see cref="Text"/> changes. Invokes the <see cref="TextChanging"/> event, which can
    ///     be cancelled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The base implementation raises the <see cref="TextChanging"/> event. Override in derived views
    ///         to perform validation or fire control-specific pre-change events.
    ///     </para>
    /// </remarks>
    /// <param name="newText">The proposed new text value.</param>
    /// <returns><see langword="true"/> if the text change should be cancelled; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnTextChanging (string newText)
    {
        CancelEventArgs args = new ();
        TextChanging?.Invoke (this, args);

        return args.Cancel;
    }

    /// <summary>
    ///     Raised when the <see cref="Text"/> is about to change. Set <see cref="CancelEventArgs.Cancel"/> to
    ///     <see langword="true"/> to prevent the change.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a signal-only notification at the <see cref="View"/> level. It does not carry old or new
    ///         text values. Derived controls that need richer text-edit semantics may expose their own specific events.
    ///     </para>
    /// </remarks>
    public event EventHandler<CancelEventArgs>? TextChanging;

    /// <summary>
    ///     Called after the <see cref="Text"/> has been changed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Override in derived views to react to text changes. The base implementation is empty.
    ///         The <see cref="TextChanged"/> event is raised by the caller after this method returns.
    ///     </para>
    /// </remarks>
    protected virtual void OnTextChanged () { }

    /// <summary>
    ///     Invokes the CWP post-change workflow for <see cref="Text"/>: calls <see cref="OnTextChanged"/>
    ///     then raises <see cref="TextChanged"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Derived views that bypass the base <see cref="Text"/> setter (e.g., using <c>new</c>) should
    ///         call this method after mutating text to participate in the CWP workflow.
    ///     </para>
    /// </remarks>
    protected internal void RaiseTextChanged ()
    {
        OnTextChanged ();
        TextChanged?.Invoke (this, EventArgs.Empty);
    }

    /// <summary>
    ///     Raised after the <see cref="Text"/> has been changed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a signal-only notification at the <see cref="View"/> level. It does not carry old or new
    ///         text values. Derived controls that need richer text-edit semantics may expose their own specific events.
    ///     </para>
    /// </remarks>
    public event EventHandler? TextChanged;

    /// <summary>
    ///     If the new <see cref="Text"/> resolves to the same <see cref="Frame"/> (position and size) the view has now,
    ///     handles the change with a redraw only — skipping <see cref="SetNeedsLayout"/> and the ancestor layout
    ///     propagation it triggers. See issue #5499.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The prediction reuses <see cref="TryComputeRelativeFrame"/> — the exact computation
    ///         <see cref="SetRelativeLayout"/> runs — so it can never disagree with the Frame a real layout pass would
    ///         produce, and it compares the whole <see cref="Frame"/> (a <see cref="Pos"/> can depend on
    ///         <see cref="Text"/>, so a same-size change can still move the view).
    ///     </para>
    ///     <para>
    ///         The optimization applies only when the view has already been laid out and both <see cref="Width"/> and
    ///         <see cref="Height"/> are either a fixed dimension or exactly <see cref="DimAutoStyle.Text"/>. A
    ///         <see cref="DimAuto"/> with <see cref="DimAutoStyle.Content"/> (including <see cref="DimAutoStyle.Auto"/>)
    ///         lays out subviews while calculating, so resolving it here would have side effects; those views fall back
    ///         to <see cref="SetNeedsLayout"/>.
    ///     </para>
    ///     <para>
    ///         On the fast path this mirrors the <see cref="TextFormatter"/> setup <see cref="SetRelativeLayout"/>
    ///         performs (<see cref="SetTextFormatterSize"/> plus the post-resolve constraint finalization) so wrapped or
    ///         clipped text stays correctly constrained, then marks the view for reformat and redraw.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if the change was handled by redraw only; <see langword="false"/> if a normal layout
    ///     pass is required.
    /// </returns>
    private bool TryRedrawWithoutLayout ()
    {
        if (_frame is null)
        {
            // Not yet laid out; let the normal layout pass establish the Frame.
            return false;
        }

        if (!IsEagerlyResolvable (_width) || !IsEagerlyResolvable (_height))
        {
            return false;
        }

        // Mirror SetRelativeLayout's preamble so the prediction sees the same TextFormatter state.
        SetTextFormatterSize ();

        if (!TryComputeRelativeFrame (GetContainerSize (), out Rectangle predicted) || predicted != Frame)
        {
            return false;
        }

        // The Frame is unchanged, so nothing outside this view can be affected. Mirror SetRelativeLayout's formatter
        // finalization (skipped because we bypass it) so wrapped/clipped text stays constrained, then reformat and
        // redraw without a layout pass.
        FinalizeTextFormatterConstraints ();
        TextFormatter.NeedsFormat = true;
        SetNeedsDraw ();

        return true;

        // A dimension is safe to resolve here only when doing so has no side effects beyond this view's own
        // TextFormatter. DimAbsolute is constant; a Text-only DimAuto depends solely on the TextFormatter.
        static bool IsEagerlyResolvable (Dim dim) => dim is DimAbsolute or DimAuto { Style: DimAutoStyle.Text };
    }

    /// <summary>
    ///     Gets or sets how the View's <see cref="Text"/> is aligned horizontally when drawn. Changing this property will
    ///     redisplay the <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="DimAutoStyle.Text"/>, the
    ///         <see cref="GetContentSize ()"/> will be adjusted to fit the text.
    ///     </para>
    /// </remarks>
    /// <value>The text alignment.</value>
    public Alignment TextAlignment
    {
        get => TextFormatter.Alignment;
        set
        {
            TextFormatter.Alignment = value;
            UpdateTextFormatterText ();
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     Gets or sets the direction of the View's <see cref="Text"/>. Changing this property will redisplay the
    ///     <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="DimAutoStyle.Text"/>, the
    ///         <see cref="GetContentSize ()"/> will be adjusted to fit the text.
    ///     </para>
    /// </remarks>
    /// <value>The text direction.</value>
    public TextDirection TextDirection { get => TextFormatter.Direction; set => UpdateTextDirection (value); }

    /// <summary>
    ///     Gets or sets the <see cref="Text.TextFormatter"/> used to format <see cref="Text"/>.
    /// </summary>
    public TextFormatter TextFormatter { get; init; } = new ();

    /// <summary>
    ///     Gets or sets how the View's <see cref="Text"/> is aligned vertically when drawn. Changing this property will
    ///     redisplay
    ///     the <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="DimAutoStyle.Text"/>, the
    ///         <see cref="GetContentSize ()"/> will be adjusted to fit the text.
    ///     </para>
    /// </remarks>
    /// <value>The vertical text alignment.</value>
    public Alignment VerticalTextAlignment
    {
        get => TextFormatter.VerticalAlignment;
        set
        {
            TextFormatter.VerticalAlignment = value;
            SetNeedsDraw ();
        }
    }

    // TODO: Add a OnUpdateTextFormatterText method that invokes UpdateTextFormatterText so that overrides don't have to call base.
    /// <summary>
    ///     Can be overridden if the <see cref="TextFormatter.Text"/> has
    ///     different format than the default.
    /// </summary>
    /// <remarks>
    ///     Overrides must call <c>base.UpdateTextFormatterText</c> before updating <see cref="TextFormatter.Text"/>.
    /// </remarks>
    protected virtual void UpdateTextFormatterText ()
    {
        TextFormatter.Text = _text;
        TextFormatter.ConstrainToWidth = null;
        TextFormatter.ConstrainToHeight = null;
    }

    /// <summary>
    ///     Internal API. Sets <see cref="TextFormatter"/>.Width/Height.
    /// </summary>
    /// <remarks>
    ///     Use this API to set <see cref="Text.TextFormatter.ConstrainToWidth"/>/Height when the view has changed such that
    ///     the
    ///     size required to fit the text has changed.
    ///     changes.
    /// </remarks>
    /// <returns></returns>
    internal void SetTextFormatterSize ()
    {
        // View subclasses can override UpdateTextFormatterText to modify the Text it holds (e.g. Checkbox and Button).
        // We need to ensure TextFormatter is accurate by calling it here.
        UpdateTextFormatterText ();

        // Use _width & _height instead of Width & Height to avoid debug spew

        if (_width.Has (out DimAuto widthAuto) && widthAuto.Style.FastHasFlags (DimAutoStyle.Text))
        {
            TextFormatter.ConstrainToWidth = null;
        }
        else
        {
            if (_contentWidth is { } w)
            {
                TextFormatter.ConstrainToWidth = w;
            }
        }

        // Use _width & _height instead of Width & Height to avoid debug spew

        if (_height.Has (out DimAuto heightAuto) && heightAuto.Style.FastHasFlags (DimAutoStyle.Text))
        {
            TextFormatter.ConstrainToHeight = null;
        }
        else
        {
            if (_contentHeight is { } h)
            {
                TextFormatter.ConstrainToHeight = h;
            }
        }
    }

    /// <summary>
    ///     Initializes the Text of the View. Called by the constructor.
    /// </summary>
    private void SetupText ()
    {
        Text = string.Empty;
        TextDirection = TextDirection.LeftRight_TopBottom;
    }

    private void UpdateTextDirection (TextDirection newDirection)
    {
        bool directionChanged = TextFormatter.IsHorizontalDirection (TextFormatter.Direction) != TextFormatter.IsHorizontalDirection (newDirection);
        TextFormatter.Direction = newDirection;

        UpdateTextFormatterText ();

        if (directionChanged)
        {
            TextFormatter.ConstrainToWidth = null;
            TextFormatter.ConstrainToHeight = null;
            SetNeedsLayout ();
        }

        SetNeedsDraw ();
    }
}
