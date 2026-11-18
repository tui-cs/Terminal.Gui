namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="Button"/> used to scroll content forward or backward. It enables mouse hold-repeat for continuous
///     scrolling when the mouse button is held down.
///     The button displays an arrow glyph determined by the combination of <see cref="Direction"/> and
///     <see cref="Orientation"/>:
///     <list type="table">
///         <listheader>
///             <term>Orientation</term><term>Direction</term><term>Glyph</term>
///         </listheader>
///         <item>
///             <term>Horizontal</term><term>Backward</term>
///             <term>
///                 <see cref="Glyphs.LeftArrow"/>
///             </term>
///         </item>
///         <item>
///             <term>Horizontal</term><term>Forward</term>
///             <term>
///                 <see cref="Glyphs.RightArrow"/>
///             </term>
///         </item>
///         <item>
///             <term>Vertical</term><term>Backward</term>
///             <term>
///                 <see cref="Glyphs.UpArrow"/>
///             </term>
///         </item>
///         <item>
///             <term>Vertical</term><term>Forward</term>
///             <term>
///                 <see cref="Glyphs.DownArrow"/>
///             </term>
///         </item>
///     </list>
/// </summary>
/// <remarks>
///     <para>
///         By default, <see cref="ScrollButton"/> cannot receive focus and does not participate in keyboard navigation;
///         override this by setting <see cref="View.CanFocus"/> to <see langword="true"/> if desired.
///     </para>
/// </remarks>
public class ScrollButton : Button, IOrientation
{
    private readonly OrientationHelper _orientationHelper;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ScrollButton"/> class.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The button defaults to <see cref="Orientation.Horizontal"/> orientation and no
    ///         <see cref="NavigationDirection"/> (glyph will be updated once both
    ///         <see cref="Orientation"/> and <see cref="Direction"/> are set).
    ///     </para>
    /// </remarks>
    public ScrollButton ()
    {
        CanFocus = false;
        NoDecorations = true;
        NoPadding = true;
        MouseHoldRepeat = MouseFlags.LeftButtonReleased;

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new OrientationHelper (this);
        SetGlyph ();
    }

    /// <inheritdoc/>
    /// <remarks>Sets <see cref="ValueChangingEventArgs{T}.NewValue"/> to <see langword="null"/> so that no shadow infrastructure is allocated by default for scroll buttons.</remarks>
    protected override void OnInitializingShadowStyle (ValueChangingEventArgs<ShadowStyles?> args) => args.NewValue = null;

    /// <summary>
    ///     Gets or sets the direction this <see cref="ScrollButton"/> scrolls.
    /// </summary>
    /// <value>
    ///     <see cref="NavigationDirection.Backward"/> renders an up-arrow (vertical) or left-arrow (horizontal).
    ///     <see cref="NavigationDirection.Forward"/> renders a down-arrow (vertical) or right-arrow (horizontal).
    /// </value>
    /// <remarks>
    ///     <para>
    ///         Changing this property automatically updates the button's glyph via <see cref="Orientation"/>.
    ///     </para>
    /// </remarks>
    public NavigationDirection Direction
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }
            field = value;
            SetGlyph ();
        }
    }

    private void SetGlyph ()
    {
        if (Orientation == Orientation.Horizontal)
        {
            Title = Direction switch
            {
                NavigationDirection.Backward => Glyphs.LeftArrow.ToString (),
                NavigationDirection.Forward => Glyphs.RightArrow.ToString (),
                _ => Title
            };
        }
        else
        {
            Title = Direction switch
            {
                NavigationDirection.Backward => Glyphs.UpArrow.ToString (),
                NavigationDirection.Forward => Glyphs.DownArrow.ToString (),
                _ => Title
            };
        }
    }

    #region IOrientation members

    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/> for this <see cref="ScrollButton"/>. The default is
    ///     <see cref="Orientation.Horizontal"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Changing <see cref="Orientation"/> automatically updates the button's glyph to match the new
    ///         orientation and the current <see cref="Direction"/>.
    ///     </para>
    /// </remarks>
    public Orientation Orientation { get => _orientationHelper.Orientation; set => _orientationHelper.Orientation = value; }

#pragma warning disable CS0067 // The event is never used
    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;
#pragma warning restore CS0067 // The event is never used

    /// <summary>Called when <see cref="Orientation"/> has changed.</summary>
    /// <param name="newOrientation">The new <see cref="Orientation"/> value.</param>
    public void OnOrientationChanged (Orientation newOrientation) => SetGlyph ();

    #endregion
}
