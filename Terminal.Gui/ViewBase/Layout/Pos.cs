namespace Terminal.Gui.ViewBase;

/// <summary>
///     Describes a declarative, responsive position for a <see cref="View"/>.
///     A <see cref="Pos"/> can be absolute, percentage-based, centered, anchored to an edge, or relative to another
///     <see cref="View"/>. Integer values are implicitly convertible to an absolute <see cref="Pos"/>.
/// </summary>
/// <remarks>
///     <para>
///         To describe where a view should be, relative to its <see cref="View.SuperView"/>, its available space, or its
///         sibling views, use <see cref="Pos"/> with <see cref="View.X"/> and <see cref="View.Y"/>.
///     </para>
///     <para>
///         To define responsive horizontal and vertical placement in Terminal.Gui, describe the relationship that should
///         be maintained and let the layout engine resolve the final <see cref="View.Frame"/> whenever layout runs.
///     </para>
///     <para>
///         <see cref="Pos"/> values can be combined to produce more useful layouts, such as
///         <c>Pos.Center () - 3</c>, which shifts the resolved position of the <see cref="View"/> 3 cells earlier on
///         the axis after centering.
///     </para>
///     <para>
///         To reference coordinates of another view, use <see cref="Left(View)"/>, <see cref="Right(View)"/>,
///         <see cref="Bottom(View)"/>, and <see cref="Top(View)"/>. <see cref="X(View)"/> and <see cref="Y(View)"/> are
///         aliases to <see cref="Left(View)"/> and <see cref="Top(View)"/> respectively.
///     </para>
///     <para>
///         <list type="table">
///             <listheader>
///                 <term>Pos Object</term> <description>Description</description>
///             </listheader>
///             <item>
///                 <term>
///                     <see cref="Pos.Align"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that aligns a set of views.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Func"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that computes the position by executing the provided
///                     function. The function will be called every time the position is needed.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Percent(int)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that is a percentage of the width or height of the
///                     SuperView.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.AnchorEnd()"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that is anchored to the end (right side or bottom) of
///                     the dimension, useful to flush the layout from the right or bottom.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Center"/>
///                 </term>
///                 <description>Creates a <see cref="Pos"/> object that can be used to center the <see cref="View"/>.</description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Absolute"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that is an absolute position based on the specified
///                     integer value.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Left"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that tracks the Left (X) position of the specified
///                     <see cref="View"/>.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.X(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that tracks the Left (X) position of the specified
///                     <see cref="View"/>.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Top(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that tracks the Top (Y) position of the specified
///                     <see cref="View"/>.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Y(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that tracks the Top (Y) position of the specified
///                     <see cref="View"/>.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Right(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that tracks the Right (X+Width) coordinate of the
///                     specified <see cref="View"/>.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Bottom(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that tracks the Bottom (Y+Height) coordinate of the
///                     specified <see cref="View"/>
///                 </description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public abstract record Pos
{
    #region static Pos creation methods

    /// <summary>Creates a <see cref="Pos"/> object that is an absolute position based on the specified integer value.</summary>
    /// <returns>The Absolute <see cref="Pos"/>.</returns>
    /// <param name="position">The value to convert to the <see cref="Pos"/>.</param>
    public static Pos Absolute (int position) => new PosAbsolute (position);

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that aligns a set of views according to the specified <see cref="Alignment"/>
    ///     and <see cref="AlignmentModes"/>.
    /// </summary>
    /// <param name="alignment">The alignment. The default includes <see cref="AlignmentModes.AddSpaceBetweenItems"/>.</param>
    /// <param name="modes">The optional alignment modes.</param>
    /// <param name="groupId">
    ///     The optional identifier of a set of views that should be aligned together. When only a single
    ///     set of views in a SuperView is aligned, this parameter is optional.
    /// </param>
    /// <returns></returns>
    public static Pos Align (Alignment alignment, AlignmentModes modes = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems, int groupId = 0) =>
        new PosAlign { Aligner = new () { Alignment = alignment, AlignmentModes = modes }, GroupId = groupId };

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that is anchored to the end (right side or bottom) of the SuperView's
    ///     content area. The view will be positioned so its right/bottom edge aligns with the SuperView's right/bottom edge.
    /// </summary>
    /// <returns>The <see cref="Pos"/> object anchored to the end (the bottom or the right side).</returns>
    /// <remarks>
    ///     <para>
    ///         When used within a SuperView that has <see cref="Dim.Auto"/>, <see cref="Pos.AnchorEnd()"/> actively
    ///         contributes to determining the SuperView's content size, ensuring the SuperView is large enough to
    ///         accommodate the anchored view at the end position.
    ///     </para>
    ///     <para>
    ///         This is equivalent to using <see cref="Pos.AnchorEnd(int)"/> with an offset equal to the view's
    ///         width (for X) or height (for Y).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     This sample shows how to align a <see cref="Button"/> to the bottom-right of the SuperView.
    ///     <code>
    ///     anchorButton.X = Pos.AnchorEnd ();
    ///     anchorButton.Y = Pos.AnchorEnd ();
    ///     </code>
    /// </example>
    /// <example>
    ///     This sample shows how <see cref="Pos.AnchorEnd()"/> affects <see cref="Dim.Auto"/>:
    ///     <code>
    ///     View superView = new () { Width = Dim.Auto () };
    ///     
    ///     // Label contributes ~10 to width
    ///     Label label = new () { Text = "Name:" };
    ///     superView.Add (label);
    ///     
    ///     // Button forces SuperView to be wide enough to fit it at the end
    ///     Button button = new ()
    ///     {
    ///         Text = "OK",
    ///         X = Pos.AnchorEnd ()  // SuperView width = button position + button width
    ///     };
    ///     superView.Add (button);
    ///     </code>
    /// </example>
    public static Pos AnchorEnd () => new PosAnchorEnd ();

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that is anchored to the end (right side or bottom) of the SuperView's
    ///     content area, with a specified offset from the edge.
    /// </summary>
    /// <returns>The <see cref="Pos"/> object anchored to the end (the bottom or the right side).</returns>
    /// <param name="offset">
    ///     The number of columns (for X) or rows (for Y) from the right/bottom edge. The view will be positioned
    ///     this many cells away from the edge, towards the start.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         When used within a SuperView that has <see cref="Dim.Auto"/>, <see cref="Pos.AnchorEnd(int)"/> actively
    ///         contributes to determining the SuperView's content size, ensuring the SuperView is large enough to
    ///         accommodate the anchored view at the calculated position.
    ///     </para>
    ///     <para>
    ///         See also <see cref="Pos.AnchorEnd()"/>, which uses the view's dimension to ensure the view is
    ///         fully visible with its right/bottom edge aligned to the SuperView's edge.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     This sample shows how to position a <see cref="Button"/> 2 columns from the right edge.
    ///     <code>
    ///     // If SuperView width is 80, button will be at X = 80 - 2 = 78
    ///     button.X = Pos.AnchorEnd (2);
    ///     button.Y = 1;
    ///     </code>
    /// </example>
    /// <example>
    ///     This sample shows how <see cref="Pos.AnchorEnd(int)"/> affects <see cref="Dim.Auto"/>:
    ///     <code>
    ///     View superView = new () { Width = Dim.Auto () };
    ///     
    ///     // Button forces SuperView to be at least 12 columns wide (10 offset + 2 for margin)
    ///     Button button = new ()
    ///     {
    ///         Text = "OK",
    ///         Width = 2,
    ///         X = Pos.AnchorEnd (10)  // SuperView width = 10 + some additional space
    ///     };
    ///     superView.Add (button);
    ///     </code>
    /// </example>
    public static Pos AnchorEnd (int offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative (offset);

        return new PosAnchorEnd (offset);
    }

    /// <summary>Creates a <see cref="Pos"/> object that can be used to center the <see cref="View"/>.</summary>
    /// <returns>The center Pos.</returns>
    /// <example>
    ///     This creates a <see cref="TextField"/> centered horizontally, is 50% of the way down, is 30% the height, and
    ///     is 80% the width of the <see cref="View"/> it added to.
    ///     <code>
    ///  var textField = new TextField () {
    ///     X = Pos.Center (),
    ///     Y = Pos.Percent (50),
    ///     Width = Dim.Percent (80),
    ///     Height = Dim.Percent (30),
    ///  };
    ///  </code>
    /// </example>
    public static Pos Center () => new PosCenter ();

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that computes the position based on the passed view and by executing the
    ///     provided function.
    ///     The function will be called every time the position is needed.
    /// </summary>
    /// <param name="function">The function to be executed.</param>
    /// <param name="view">The view where the data will be retrieved.</param>
    /// <returns>The <see cref="Pos"/> returned from the function.</returns>
    public static Pos Func (Func<View?, int> function, View? view = null) => new PosFunc (function, view);

    /// <summary>Creates a percentage <see cref="Pos"/> object</summary>
    /// <returns>The percent <see cref="Pos"/> object.</returns>
    /// <param name="percent">A value between 0 and 100 representing the percentage.</param>
    /// <example>
    ///     This creates a <see cref="TextField"/> centered horizontally, is 50% of the way down, is 30% the height, and
    ///     is 80% the width of the <see cref="View"/> it added to.
    ///     <code>
    ///  var textView = new TextField {
    ///      X = Pos.Center (),
    ///      Y = Pos.Percent (50),
    ///      Width = Dim.Percent (80),
    ///      Height = Dim.Percent (30),
    ///  };
    ///  </code>
    /// </example>
    public static Pos Percent (int percent)
    {
        ArgumentOutOfRangeException.ThrowIfNegative (percent);

        return new PosPercent (percent);
    }

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Top (Y) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Top (View view) => new PosView (view, Side.Top);

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Top (Y) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Y (View view) => new PosView (view, Side.Top);

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Left (X) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Left (View view) => new PosView (view, Side.Left);

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Left (X) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos X (View view) => new PosView (view, Side.Left);

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that tracks the Bottom (Y+Height) coordinate of the specified
    ///     <see cref="View"/>
    /// </summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Bottom (View view) => new PosView (view, Side.Bottom);

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that tracks the Right (X+Width) coordinate of the specified
    ///     <see cref="View"/>.
    /// </summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Right (View view) => new PosView (view, Side.Right);

    #endregion static Pos creation methods

    #region virtual methods

    /// <summary>
    ///     Gets the starting point of an element based on the size of the parent element (typically
    ///     <c>Superview.GetContentSize ()</c>).
    ///     This method is meant to be overridden by subclasses to provide different ways of calculating the starting point.
    ///     This method is used
    ///     internally by the layout system to determine where a View should be positioned.
    /// </summary>
    /// <param name="size">The size of the parent element (typically <c>Superview.GetContentSize ()</c>).</param>
    /// <returns>
    ///     An integer representing the calculated position. The way this position is calculated depends on the specific
    ///     subclass of Pos that is used. For example, PosAbsolute returns a fixed position, PosAnchorEnd returns a
    ///     position that is anchored to the end of the layout, and so on.
    /// </returns>
    internal virtual int GetAnchor (int size) => 0;

    /// <summary>
    ///     Calculates and returns the final position of a <see cref="View"/> object. It takes into account the dimension of
    ///     the
    ///     superview and the dimension of the view itself.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="superviewDimension">
    ///     The dimension of the superview. This could be the width for x-coordinate calculation or the
    ///     height for y-coordinate calculation.
    /// </param>
    /// <param name="dim">The dimension of the View. It could be the current width or height.</param>
    /// <param name="us">The View that holds this Pos object.</param>
    /// <param name="dimension">Width or Height</param>
    /// <returns>
    ///     The calculated position of the View. The way this position is calculated depends on the specific subclass of Pos
    ///     that
    ///     is used.
    /// </returns>
    internal virtual int Calculate (int superviewDimension, Dim dim, View us, Dimension dimension) => GetAnchor (superviewDimension);

    /// <summary>
    ///     Returns <see langword="true"/> if this Pos object references other views.
    /// </summary>
    /// <remarks>
    ///     The default implementation uses <see cref="GetReferencedViews"/>. Override for optimization
    ///     in types that can determine this without allocating an iterator.
    /// </remarks>
    /// <returns><see langword="true"/> if this Pos depends on other views for layout.</returns>
    internal virtual bool ReferencesOtherViews () => GetReferencedViews ().Any ();

    /// <summary>
    ///     Returns the views that this Pos depends on for layout calculations.
    ///     Used by the layout system to determine the order in which views should be laid out.
    /// </summary>
    /// <remarks>
    ///     Override in subclasses that reference other views (e.g., <see cref="PosView"/>).
    ///     Composite types like <see cref="PosCombine"/> should aggregate results from their children.
    /// </remarks>
    /// <returns>An enumerable of views that this Pos depends on.</returns>
    internal virtual IEnumerable<View> GetReferencedViews () { yield break; }

    /// <summary>
    ///     Indicates whether this Pos depends on the SuperView's content size for its calculation.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is used by <see cref="DimAuto"/> to categorize subviews during auto-sizing calculations
    ///         without needing to perform type checking.
    ///     </para>
    ///     <para>
    ///         Types that depend on and actively contribute to SuperView content size determination include
    ///         <see cref="PosAnchorEnd"/> and <see cref="PosAlign"/>. These types require special handling during
    ///         auto-sizing because they affect the minimum content size needed.
    ///     </para>
    ///     <para>
    ///         Types like <see cref="PosCenter"/> and <see cref="PosPercent"/> also use the SuperView's content size
    ///         for positioning, but they do NOT actively contribute to determining that size, so this property
    ///         returns <see langword="false"/> for them.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if this Pos actively contributes to determining the SuperView's content size;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    internal virtual bool DependsOnSuperViewContentSize => false;

    /// <summary>
    ///     Indicates whether this Pos has a fixed value that doesn't depend on layout calculations.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is used by <see cref="DimAuto"/> to identify positions that can be
    ///         determined without performing layout calculations on other views.
    ///     </para>
    ///     <para>
    ///         Fixed positions include <see cref="PosAbsolute"/> and positions calculated by
    ///         <see cref="PosFunc"/> that don't depend on other views' layouts.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if this Pos has a fixed value independent of layout;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    internal virtual bool IsFixed => false;

    /// <summary>
    ///     Indicates whether this Pos requires the target view to be laid out before it can be calculated.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is used by <see cref="DimAuto"/> to identify positions that depend on
    ///         another view's layout being completed first.
    ///     </para>
    ///     <para>
    ///         Positions that require target layout include <see cref="PosView"/> which depends on
    ///         the target view's calculated position.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if this Pos requires the target view's layout to be calculated first;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    internal virtual bool RequiresTargetLayout => false;

    /// <summary>
    ///     Indicates whether the specified type <typeparamref name="TPos"/> is in the hierarchy of this Pos object.
    /// </summary>
    /// <param name="pos">
    ///     When this method returns, contains the first instance of type <typeparamref name="TPos"/> found,
    ///     or <see langword="null"/> if no instance was found.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if this Pos or any nested Pos is of type <typeparamref name="TPos"/>;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    public bool Has<TPos> (out TPos pos) where TPos : Pos
    {
        pos = (this as TPos)!;

        if (this is TPos)
        {
            return true;
        }

        return HasInner (out pos);
    }

    /// <summary>
    ///     Searches nested Pos objects for the specified type. Override in subclasses that contain
    ///     other Pos objects to enable <see cref="Has{TPos}"/> to find nested types.
    /// </summary>
    /// <param name="pos">
    ///     When this method returns, contains the first instance of type <typeparamref name="TPos"/> found,
    ///     or <see langword="null"/> if no instance was found.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if any nested Pos is of type <typeparamref name="TPos"/>;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    protected virtual bool HasInner<TPos> (out TPos pos) where TPos : Pos
    {
        pos = null!;

        return false;
    }

    #endregion virtual methods

    #region operators

    /// <summary>Adds a <see cref="Pos"/> to a <see cref="Pos"/>, yielding a new <see cref="Pos"/>.</summary>
    /// <param name="left">The first <see cref="Pos"/> to add.</param>
    /// <param name="right">The second <see cref="Pos"/> to add.</param>
    /// <returns>The <see cref="Pos"/> that is the sum of the values of <c>left</c> and <c>right</c>.</returns>
    public static Pos operator + (Pos left, Pos right)
    {
        if (left is PosAbsolute && right is PosAbsolute)
        {
            return new PosAbsolute (left.GetAnchor (0) + right.GetAnchor (0));
        }

        PosCombine newPos = new (AddOrSubtract.Add, left, right);

        // QUESTION: This seems like a hack. Is it really needed?
        if (left is PosView view)
        {
            view.Target.SetNeedsLayout ();
        }

        return newPos;
    }

    /// <summary>Creates an Absolute <see cref="Pos"/> from the specified integer value.</summary>
    /// <returns>The Absolute <see cref="Pos"/>.</returns>
    /// <param name="n">The value to convert to the <see cref="Pos"/> .</param>
    public static implicit operator Pos (int n) => new PosAbsolute (n);

    /// <summary>
    ///     Subtracts a <see cref="Pos"/> from a <see cref="Pos"/>, yielding a new
    ///     <see cref="Pos"/>.
    /// </summary>
    /// <param name="left">The <see cref="Pos"/> to subtract from (the minuend).</param>
    /// <param name="right">The <see cref="Pos"/> to subtract (the subtrahend).</param>
    /// <returns>The <see cref="Pos"/> that is the <c>left</c> minus <c>right</c>.</returns>
    public static Pos operator - (Pos left, Pos right)
    {
        if (left is PosAbsolute && right is PosAbsolute)
        {
            return new PosAbsolute (left.GetAnchor (0) - right.GetAnchor (0));
        }

        PosCombine newPos = new (AddOrSubtract.Subtract, left, right);

        // QUESTION: This seems like a hack. Is it really needed?
        if (left is PosView view)
        {
            view.Target.SetNeedsLayout ();
        }

        return newPos;
    }

    #endregion operators
}
