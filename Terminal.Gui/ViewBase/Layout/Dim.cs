using System.Numerics;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     Describes a declarative, responsive dimension for a <see cref="View"/>.
///     <see cref="Dim"/> is the type of <see cref="View.Width"/> and <see cref="View.Height"/>.
///     Integer values are implicitly convertible to an absolute <see cref="Dim"/>.
/// </summary>
/// <remarks>
///     <para>
///         To make a view's size respond to the available space, its content, or the size and position of other views
///         rather than using a fixed number of cells, use <see cref="Dim"/>.
///     </para>
///     <para>
///         To define responsive sizing in Terminal.Gui, describe the relationship that should be maintained and let the
///         layout engine resolve the final <see cref="View.Frame"/> whenever layout runs.
///     </para>
///     <para>
///         <list type="table">
///             <listheader>
///                 <term>Dim Object</term> <description>Description</description>
///             </listheader>
///             <item>
///                 <term>
///                     <see cref="Dim.Absolute"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> that is a fixed size.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Dim.Auto"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> object that automatically sizes the view to fit
///                     the view's Text, SubViews, or ContentArea.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Func"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> object that computes the dimension by executing the provided
///                     function. The function will be called every time the dimension is needed.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Dim.Percent(int, DimPercentMode)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> object that is a percentage of the width or height of the
///                     SuperView.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Dim.Fill(Dim)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> object that fills the dimension from the View's X position
///                     to the end of the super view's width, leaving the specified number of columns for a margin.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Dim.Width(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> object that tracks the Width of the specified
///                     <see cref="View"/>.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Dim.Height(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> object that tracks the Height of the specified
///                     <see cref="View"/>.
///                 </description>
///             </item>
///         </list>
///     </para>
///     <para></para>
/// </remarks>
public abstract record Dim : IEqualityOperators<Dim, Dim, bool>
{
    #region static Dim creation methods

    /// <summary>Creates an Absolute <see cref="Dim"/> from the specified integer value.</summary>
    /// <returns>The Absolute <see cref="Dim"/>.</returns>
    /// <param name="size">The value to convert to the <see cref="Dim"/>.</param>
    public static Dim Absolute (int size) => new DimAbsolute (size);

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that automatically sizes the view to fit all the view's Content, SubViews,
    ///     and/or Text.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See <see cref="DimAutoStyle"/>.
    ///     </para>
    ///     <para>
    ///         See the <a href="../docs/dimauto.md">Dim.Auto Deep Dive</a> for comprehensive documentation including
    ///         non-trivial usage patterns.
    ///     </para>
    ///     <para>
    ///         SubViews that use <see cref="Dim.Fill()"/> do not contribute to the auto-sizing calculation unless
    ///         <see cref="DimFill.MinimumContentDim"/> is specified. Without it, a <see cref="DimFill"/> SubView will
    ///         receive a size of 0. To ensure the SubView contributes a minimum size, use
    ///         <see cref="Dim.Fill(Dim, Dim?)"/> with a <c>minimumContentDim</c>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     This initializes a <see cref="View"/> with two SubViews. The view will be automatically sized to fit the two
    ///     SubViews.
    ///     <code>
    /// var button = new Button () { Text = "Click Me!", X = 1, Y = 1, Width = 10, Height = 1 };
    /// var textField = new TextField { Text = "Type here", X = 1, Y = 2, Width = 20, Height = 1 };
    /// var view = new Window () { Title = "MyWindow", X = 0, Y = 0, Width = Dim.Auto (), Height = Dim.Auto () };
    /// view.Add (button, textField);
    /// </code>
    /// </example>
    /// <returns>The <see cref="Dim"/> object.</returns>
    /// <param name="style">
    ///     Specifies how <see cref="Dim.Auto"/> will compute the dimension. The default is <see cref="DimAutoStyle.Auto"/>.
    /// </param>
    /// <param name="minimumContentDim">The minimum dimension the View's ContentSize will be constrained to.</param>
    /// <param name="maximumContentDim">The maximum dimension the View's ContentSize will be fit to.</param>
    public static Dim Auto (DimAutoStyle style = DimAutoStyle.Auto, Dim? minimumContentDim = null, Dim? maximumContentDim = null) =>
        new DimAuto (MinimumContentDim: minimumContentDim, MaximumContentDim: maximumContentDim, Style: style);

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that fills the dimension, leaving no margin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The view will fill from its position to the end of the SuperView's content area.
    ///     </para>
    ///     <para>
    ///         If the SuperView uses <see cref="Dim.Auto"/>, a <see cref="DimFill"/> SubView does <b>not</b>
    ///         contribute to the auto-sizing calculation and will receive a size of 0. To make the SubView contribute to
    ///         auto-sizing, use <see cref="Fill(Dim, Dim?)"/> with a <c>minimumContentDim</c> or <see cref="Fill(View)"/>
    ///         with a <c>to</c> parameter.
    ///         See the <a href="../docs/dimauto.md">Dim.Auto Deep Dive</a> for details.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var view = new View { X = 5, Y = 0, Width = Dim.Fill(), Height = 1 };
    /// // If SuperView width is 80, view width will be 75 (80 - 5)
    /// </code>
    /// </example>
    /// <returns>The Fill dimension.</returns>
    public static Dim Fill () => new DimFill (0);

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that fills the dimension, leaving the specified margin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the SuperView uses <see cref="Dim.Auto"/>, a <see cref="DimFill"/> SubView does <b>not</b>
    ///         contribute to the auto-sizing calculation and will receive a size of 0. To make the SubView contribute to
    ///         auto-sizing, use <see cref="Fill(Dim, Dim?)"/> with a <c>minimumContentDim</c> or <see cref="Fill(View)"/>
    ///         with a <c>to</c> parameter.
    ///         See the <a href="../docs/dimauto.md">Dim.Auto Deep Dive</a> for details.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var view = new View { X = 0, Y = 0, Width = Dim.Fill(2), Height = 1 };
    /// // If SuperView width is 80, view width will be 78 (80 - 2)
    /// </code>
    /// </example>
    /// <returns>The Fill dimension.</returns>
    /// <param name="margin">Margin to use.</param>
    public static Dim Fill (Dim margin) => new DimFill (margin);

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that fills the dimension, leaving the specified margin and respecting
    ///     the specified minimum dimension.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When the SuperView uses <see cref="Dim.Auto"/>, a <see cref="DimFill"/> SubView does <b>not</b>
    ///         contribute to the auto-sizing calculation by default. The <paramref name="minimumContentDim"/> parameter
    ///         resolves this: it contributes a floor to the auto-sizing calculation, ensuring the SuperView is at least
    ///         large enough to accommodate the minimum. Without it (or without using <see cref="Fill(View)"/> with
    ///         a <c>to</c> parameter), the SubView will receive a size of 0.
    ///     </para>
    ///     <para>
    ///         See the <a href="../docs/dimauto.md">Dim.Auto Deep Dive</a> for details.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Fill with minimum width of 40
    /// var view = new View { X = 0, Y = 0, Width = Dim.Fill(margin: 0, minimumContentDim: 40), Height = 1 };
    /// // If SuperView has Dim.Auto() width, it will be at least 40 wide
    /// // If SuperView is 80 wide, view will be 80 wide
    /// // If SuperView is 30 wide, view will still be 40 wide (minimum)
    /// </code>
    /// </example>
    /// <returns>The Fill dimension.</returns>
    /// <param name="margin">Margin to use.</param>
    /// <param name="minimumContentDim">
    ///     The minimum dimension. If <see langword="null"/>, no minimum is enforced.
    /// </param>
    public static Dim Fill (Dim margin, Dim? minimumContentDim) => new DimFill (margin, minimumContentDim);

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that fills the dimension up to the position of another view.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The view will fill from its position up to (but not including) the position of the <paramref name="to"/> view.
    ///         For Width, this means filling up to the X coordinate of the <paramref name="to"/> view.
    ///         For Height, this means filling up to the Y coordinate of the <paramref name="to"/> view.
    ///     </para>
    ///     <para>
    ///         When the SuperView uses <see cref="Dim.Auto"/>, this <see cref="DimFill"/> with <c>to</c> parameter
    ///         <b>does</b> contribute to the auto-sizing calculation by ensuring the SuperView is large enough to
    ///         accommodate both this view and the <paramref name="to"/> view.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var label = new Label { X = 0, Y = 0, Width = 10, Text = "Name:" };
    /// var btn = new Button { X = Pos.AnchorEnd(), Text = "OK" };
    /// var textField = new TextField { X = Pos.Right(label) + 1, Y = 0, Width = Dim.Fill(to: btn) };
    /// // textField will fill the space between the label and the button
    /// </code>
    /// </example>
    /// <returns>The Fill dimension.</returns>
    /// <param name="to">The view to fill up to.</param>
    public static Dim Fill (View to) => new DimFill (Absolute (0), null, to);

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that fills the dimension up to the position of another view, with a margin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The view will fill from its position up to (but not including) the position of the <paramref name="to"/> view,
    ///         minus the specified margin.
    ///     </para>
    ///     <para>
    ///         When the SuperView uses <see cref="Dim.Auto"/>, this <see cref="DimFill"/> with <c>to</c> parameter
    ///         <b>does</b> contribute to the auto-sizing calculation by ensuring the SuperView is large enough to
    ///         accommodate both this view and the <paramref name="to"/> view.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var btn = new Button { X = Pos.AnchorEnd(), Text = "OK" };
    /// var textField = new TextField { X = 0, Y = 0, Width = Dim.Fill(margin: 2, to: btn) };
    /// // textField will fill the space up to 2 columns before the button
    /// </code>
    /// </example>
    /// <returns>The Fill dimension.</returns>
    /// <param name="margin">Margin to use.</param>
    /// <param name="to">The view to fill up to.</param>
    public static Dim Fill (Dim margin, View to) => new DimFill (margin, null, to);

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that fills the dimension up to the position of another view,
    ///     with a margin and minimum dimension.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The view will fill from its position up to (but not including) the position of the <paramref name="to"/> view,
    ///         minus the specified margin, while respecting the minimum dimension.
    ///     </para>
    ///     <para>
    ///         When the SuperView uses <see cref="Dim.Auto"/>, this <see cref="DimFill"/> with <c>to</c> parameter
    ///         <b>does</b> contribute to the auto-sizing calculation by ensuring the SuperView is large enough to
    ///         accommodate both this view and the <paramref name="to"/> view. If <paramref name="minimumContentDim"/>
    ///         is also specified, both constraints contribute to the calculation.
    ///     </para>
    /// </remarks>
    /// <returns>The Fill dimension.</returns>
    /// <param name="margin">Margin to use.</param>
    /// <param name="minimumContentDim">
    ///     The minimum dimension. If <see langword="null"/>, no minimum is enforced.
    /// </param>
    /// <param name="to">The view to fill up to.</param>
    public static Dim Fill (Dim margin, Dim? minimumContentDim, View to) => new DimFill (margin, minimumContentDim, to);

    /// <summary>
    ///     Creates a function <see cref="Dim"/> object that computes the dimension based on the passed view and by executing
    ///     the provided function.
    ///     The function will be called every time the dimension is needed.
    /// </summary>
    /// <param name="function">The function to be executed.</param>
    /// <param name="view">The view where the data will be retrieved.</param>
    /// <returns>The <see cref="Dim"/> returned from the function based on the passed view.</returns>
    public static Dim Func (Func<View?, int> function, View? view = null) => new DimFunc (function, view);

    /// <summary>Creates a <see cref="Dim"/> object that tracks the Height of the specified <see cref="View"/>.</summary>
    /// <returns>The height <see cref="Dim"/> of the other <see cref="View"/>.</returns>
    /// <param name="view">The view that will be tracked.</param>
    public static Dim Height (View? view) => new DimView (view, Dimension.Height);

    /// <summary>Creates a percentage <see cref="Dim"/> object that is a percentage of the width or height of the SuperView.</summary>
    /// <returns>The percent <see cref="Dim"/> object.</returns>
    /// <param name="percent">A value between 0 and 100 representing the percentage.</param>
    /// <param name="mode">the mode. Defaults to <see cref="DimPercentMode.ContentSize"/>.</param>
    /// <example>
    ///     This initializes a <see cref="TextField"/> that will be centered horizontally, is 50% of the way down, is 30% the
    ///     height,
    ///     and is 80% the width of the SuperView.
    ///     <code>
    ///  var textField = new TextField {
    ///     X = Pos.Center (),
    ///     Y = Pos.Percent (50),
    ///     Width = Dim.Percent (80),
    ///     Height = Dim.Percent (30),
    ///  };
    ///  </code>
    /// </example>
    public static Dim Percent (int percent, DimPercentMode mode = DimPercentMode.ContentSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative (percent);

        return new DimPercent (percent, mode);
    }

    /// <summary>Creates a <see cref="Dim"/> object that tracks the Width of the specified <see cref="View"/>.</summary>
    /// <returns>The width <see cref="Dim"/> of the other <see cref="View"/>.</returns>
    /// <param name="view">The view that will be tracked.</param>
    public static Dim Width (View? view) => new DimView (view, Dimension.Width);

    #endregion static Dim creation methods

    /// <summary>
    ///     Indicates whether the specified type <typeparamref name="TDim"/> is in the hierarchy of this Dim object.
    /// </summary>
    /// <param name="dim">
    ///     When this method returns, contains the first instance of type <typeparamref name="TDim"/> found,
    ///     or <see langword="null"/> if no instance was found.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if this Dim or any nested Dim is of type <typeparamref name="TDim"/>;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    public bool Has<TDim> (out TDim dim) where TDim : Dim
    {
        dim = (this as TDim)!;

        return this is TDim || HasInner (out dim);
    }

    /// <summary>
    ///     Searches nested Dim objects for the specified type. Override in subclasses that contain
    ///     other Dim objects to enable <see cref="Has{TDim}"/> to find nested types.
    /// </summary>
    /// <param name="dim">
    ///     When this method returns, contains the first instance of type <typeparamref name="TDim"/> found,
    ///     or <see langword="null"/> if no instance was found.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if any nested Dim is of type <typeparamref name="TDim"/>;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    protected virtual bool HasInner<TDim> (out TDim dim) where TDim : Dim
    {
        dim = null!;

        return false;
    }

    #region virtual methods

    /// <summary>
    ///     Gets a dimension that is anchored to a certain point in the layout.
    ///     This method is typically used internally by the layout system to determine the size of a View.
    /// </summary>
    /// <param name="size">The width of the area where the View is being sized (Superview.GetContentSize ()).</param>
    /// <returns>
    ///     An integer representing the calculated dimension. The way this dimension is calculated depends on the specific
    ///     subclass of Dim that is used. For example, DimAbsolute returns a fixed dimension, DimFactor returns a
    ///     dimension that is a certain percentage of the super view's size, and so on.
    /// </returns>
    internal abstract int GetAnchor (int size);

    /// <summary>
    ///     Calculates and returns the dimension of a <see cref="View"/> object. It takes into account the location of the
    ///     <see cref="View"/>, it's SuperView's ContentSize, and whether it should automatically adjust its size based on its
    ///     content.
    /// </summary>
    /// <param name="location">
    ///     The starting point from where the size calculation begins. It could be the left edge for width calculation or the
    ///     top edge for height calculation.
    /// </param>
    /// <param name="superviewContentSize">The size of the SuperView's content. It could be width or height.</param>
    /// <param name="us">The View that holds this Pos object.</param>
    /// <param name="dimension">Width or Height</param>
    /// <returns>
    ///     The calculated size of the View. The way this size is calculated depends on the specific subclass of Dim that
    ///     is used.
    /// </returns>
    internal virtual int Calculate (int location, int superviewContentSize, View us, Dimension dimension) =>
        Math.Clamp (GetAnchor (superviewContentSize - location), 0, short.MaxValue);

    /// <summary>
    ///     Returns <see langword="true"/> if this Dim object references other views.
    /// </summary>
    /// <remarks>
    ///     The default implementation uses <see cref="GetReferencedViews"/>. Override for optimization
    ///     in types that can determine this without allocating an iterator.
    /// </remarks>
    /// <returns><see langword="true"/> if this Dim depends on other views for layout.</returns>
    internal virtual bool ReferencesOtherViews () => GetReferencedViews ().Any ();

    /// <summary>
    ///     Returns the views that this Dim depends on for layout calculations.
    ///     Used by the layout system to determine the order in which views should be laid out.
    /// </summary>
    /// <remarks>
    ///     Override in subclasses that reference other views (e.g., <see cref="DimView"/>, <see cref="DimFill"/>).
    ///     Composite types like <see cref="DimCombine"/> should aggregate results from their children.
    /// </remarks>
    /// <returns>An enumerable of views that this Dim depends on.</returns>
    internal virtual IEnumerable<View> GetReferencedViews () { yield break; }

    /// <summary>
    ///     Indicates whether this Dim depends on the SuperView's content size for its calculation.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is used by <see cref="DimAuto"/> to categorize subviews during auto-sizing calculations
    ///         without needing to perform type checking.
    ///     </para>
    ///     <para>
    ///         Types that depend on SuperView content size include <see cref="DimPercent"/> and <see cref="DimFill"/>
    ///         (unless it has a <see cref="DimFill.MinimumContentDim"/> without a <see cref="DimFill.To"/>).
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if this Dim's calculation depends on the SuperView's content size;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    internal virtual bool DependsOnSuperViewContentSize => false;

    /// <summary>
    ///     Indicates whether this Dim can contribute to auto-sizing calculations.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is used by <see cref="DimAuto"/> to determine if a Dim should be considered
    ///         when calculating the minimum content size of a SuperView.
    ///     </para>
    ///     <para>
    ///         Types that cannot contribute include <see cref="DimPercent"/> (would be 0 without existing content)
    ///         and <see cref="DimFill"/> without <see cref="DimFill.MinimumContentDim"/> or <see cref="DimFill.To"/>
    ///         (would be 0 without content to fill against).
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if this Dim can contribute to determining the SuperView's auto-size;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    internal virtual bool CanContributeToAutoSizing => true;

    /// <summary>
    ///     Gets the minimum contribution this Dim makes to auto-sizing calculations.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is used by <see cref="DimAuto"/> to determine the minimum size contribution
    ///         of a Dim during auto-sizing calculations. The default implementation returns the
    ///         result of <see cref="Calculate"/>.
    ///     </para>
    ///     <para>
    ///         Types that have special minimum contribution logic (like <see cref="DimFill"/> with
    ///         <see cref="DimFill.MinimumContentDim"/>) should override this method.
    ///     </para>
    /// </remarks>
    /// <param name="location">The starting point from where the size calculation begins.</param>
    /// <param name="superviewContentSize">The size of the SuperView's content.</param>
    /// <param name="us">The View that holds this Dim object.</param>
    /// <param name="dimension">Width or Height</param>
    /// <returns>The minimum size contribution for auto-sizing calculations.</returns>
    internal virtual int GetMinimumContribution (int location, int superviewContentSize, View us, Dimension dimension) =>
        Calculate (location, superviewContentSize, us, dimension);

    /// <summary>
    ///     Indicates whether this Dim has a fixed value that doesn't depend on layout calculations.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is used by <see cref="DimAuto"/> to identify dimensions that can be
    ///         determined without performing layout calculations on other views.
    ///     </para>
    ///     <para>
    ///         Fixed dimensions include <see cref="DimAbsolute"/> and dimensions calculated by
    ///         <see cref="DimFunc"/> that don't depend on other views' layouts.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if this Dim has a fixed value independent of layout;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    internal virtual bool IsFixed => false;

    /// <summary>
    ///     Indicates whether this Dim requires the target view to be laid out before it can be calculated.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is used by <see cref="DimAuto"/> to identify dimensions that depend on
    ///         another view's layout being completed first.
    ///     </para>
    ///     <para>
    ///         Dimensions that require target layout include <see cref="DimView"/> which depends on
    ///         the target view's calculated size.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if this Dim requires the target view's layout to be calculated first;
    ///     otherwise, <see langword="false"/>.
    /// </returns>
    internal virtual bool RequiresTargetLayout => false;

    #endregion virtual methods

    #region operators

    /// <summary>Adds a <see cref="Dim"/> to a <see cref="Dim"/>, yielding a new <see cref="Dim"/>.</summary>
    /// <param name="left">The first <see cref="Dim"/> to add.</param>
    /// <param name="right">The second <see cref="Dim"/> to add.</param>
    /// <returns>The <see cref="Dim"/> that is the sum of the values of <c>left</c> and <c>right</c>.</returns>
    public static Dim operator + (Dim left, Dim right)
    {
        if (left is DimAbsolute && right is DimAbsolute)
        {
            return new DimAbsolute (left.GetAnchor (0) + right.GetAnchor (0));
        }

        var newDim = new DimCombine (AddOrSubtract.Add, left, right);

        // QUESTION: This seems like a hack. Is it really needed?
        (left as DimView)?.Target?.SetNeedsLayout ();

        return newDim;
    }

    /// <summary>Creates an Absolute <see cref="Dim"/> from the specified integer value.</summary>
    /// <returns>The Absolute <see cref="Dim"/>.</returns>
    /// <param name="n">The value to convert to the pos.</param>
    public static implicit operator Dim (int n) => new DimAbsolute (n);

    /// <summary>
    ///     Subtracts a <see cref="Dim"/> from a <see cref="Dim"/>, yielding a new
    ///     <see cref="Dim"/>.
    /// </summary>
    /// <param name="left">The <see cref="Dim"/> to subtract from (the minuend).</param>
    /// <param name="right">The <see cref="Dim"/> to subtract (the subtrahend).</param>
    /// <returns>The <see cref="Dim"/> that is the <c>left</c> minus <c>right</c>.</returns>
    public static Dim operator - (Dim left, Dim right)
    {
        if (left is DimAbsolute && right is DimAbsolute)
        {
            return new DimAbsolute (left.GetAnchor (0) - right.GetAnchor (0));
        }

        var newDim = new DimCombine (AddOrSubtract.Subtract, left, right);

        // QUESTION: This seems like a hack. Is it really needed?
        (left as DimView)?.Target?.SetNeedsLayout ();

        return newDim;
    }

    #endregion operators
}
