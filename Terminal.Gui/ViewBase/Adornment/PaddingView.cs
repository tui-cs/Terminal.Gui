namespace Terminal.Gui.ViewBase;

/// <summary>
///     The View-backed rendering layer for the Padding adornment.
///     Created lazily by <see cref="Padding"/> (via <see cref="AdornmentImpl.GetOrCreateView"/>)
///     when SubViews are added or other View-level functionality is needed.
/// </summary>
/// <remarks>
///     <para>See the <see cref="AdornmentView"/> class.</para>
/// </remarks>
public class PaddingView : AdornmentView
{
    /// <inheritdoc/>
    public PaddingView ()
    {
        /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <inheritdoc/>
    public PaddingView (Padding padding) : base (padding)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (padding == null)
        {
            // Supports AllViews_Tests_All_Constructors which uses reflection
            return;
        }

        CanFocus = true;
        TabStop = TabBehavior.NoStop;

        if (padding.Parent is { })
        {
            Frame = padding.Parent.Border.Thickness.GetInside (padding.Parent.Border.GetFrame ());
        }
        padding.ThicknessChanged += OnThicknessChanged;
        padding.Parent?.Margin.ThicknessChanged += OnThicknessChanged;
        padding.Parent?.Border.ThicknessChanged += OnThicknessChanged;
    }

    /// <inheritdoc/>
    public override void OnParentFrameChanged (Rectangle newParentFrame)
    {
        if (Adornment?.Parent is { })
        {
            Frame = Adornment.Parent.Border.Thickness.GetInside (Adornment.Parent.Border.GetFrame ());
        }
    }

    // TODO: Move DrawIndicator out of Border and into View
    private void OnThicknessChanged (object? sender, EventArgs e)
    {
        if (Adornment is { Parent: { } })
        {
            OnParentFrameChanged (Adornment.Parent.Frame);
        }
    }

    /// <summary>Called when a mouse event occurs within the Padding.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    ///     <para>
    ///         A mouse click on the Padding will cause the Parent to focus.
    ///     </para>
    /// </remarks>
    /// <param name="mouse"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (Adornment?.Parent is null)
        {
            return false;
        }

        if (!mouse.Flags.FastHasFlags (MouseFlags.LeftButtonClicked))
        {
            return false;
        }

        if (!Adornment.Parent.CanFocus || Adornment.Parent.HasFocus)
        {
            return false;
        }
        Adornment.Parent.SetFocus ();
        Adornment.Parent.SetNeedsDraw ();

        return mouse.Handled = true;
    }

    /// <summary>
    ///     Gets all SubViews of this Padding, optionally including SubViews of the Padding's Parent.
    /// </summary>
    /// <param name="includeMargin">
    ///     Ignored.
    /// </param>
    /// <param name="includeBorder">
    ///     Ignored.
    /// </param>
    /// <param name="includePadding">
    ///     If <see langword="true"/>, includes SubViews from <see cref="PaddingView"/>. If <see langword="false"/> (default),
    ///     returns only the direct SubViews
    ///     of this Padding.
    /// </param>
    /// <returns>
    ///     A read-only collection containing all SubViews. If <paramref name="includePadding"/> is
    ///     <see langword="true"/>, the collection includes SubViews from this Padding's direct SubViews as well
    ///     as SubViews from the Padding's Parent.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method returns a snapshot of the SubViews at the time of the call. The collection is
    ///         safe to iterate even if SubViews are added or removed during iteration.
    ///     </para>
    ///     <para>
    ///         The order of SubViews in the returned collection is:
    ///         <list type="number">
    ///             <item>Direct SubViews of this Padding</item>
    ///             <item>SubViews of Parent (if <paramref name="includePadding"/> is <see langword="true"/>)</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public override IReadOnlyCollection<View> GetSubViews (bool includeMargin = false, bool includeBorder = false, bool includePadding = false)
    {
        List<View> subViewsOfThisAdornment = new (base.GetSubViews (false, false, includePadding));

        if (includePadding && Adornment?.Parent is { })
        {
            // Include SubViews from Parent. Since we are a Padding of Parent do not
            // request Adornments again to avoid infinite recursion.
            subViewsOfThisAdornment.AddRange (Adornment.Parent.GetSubViews ());
        }

        return subViewsOfThisAdornment;
    }
}
