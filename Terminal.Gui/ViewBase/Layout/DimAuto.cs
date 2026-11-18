namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a dimension that automatically sizes the view to fit all the view's Content, SubViews, and/or Text.
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
///         SubViews that use <see cref="DimFill"/> do not contribute to the auto-sizing calculation unless
///         <see cref="DimFill.MinimumContentDim"/> or <see cref="DimFill.To"/> is specified. Without either, a
///         <see cref="DimFill"/> SubView will receive a size of 0 because the SuperView has no content-based size
///         to fill against. Use <see cref="Dim.Fill(Dim, Dim?)"/> with a <c>minimumContentDim</c> parameter or
///         <see cref="Dim.Fill(View)"/> with a <c>to</c> parameter to ensure the SubView contributes to the
///         auto-sizing calculation.
///     </para>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
///     </para>
/// </remarks>
/// <param name="MaximumContentDim">The maximum dimension the View's ContentSize will be fit to.</param>
/// <param name="MinimumContentDim">The minimum dimension the View's ContentSize will be constrained to.</param>
/// <param name="Style">The <see cref="DimAutoStyle"/> of the <see cref="DimAuto"/>.</param>
public record DimAuto (Dim? MaximumContentDim, Dim? MinimumContentDim, DimAutoStyle Style) : Dim
{
    /// <inheritdoc/>
    public override string ToString () => $"Auto({Style},{MinimumContentDim},{MaximumContentDim})";

    /// <inheritdoc/>
    internal override int GetAnchor (int size) => 0;

    /// <inheritdoc/>
    internal override bool IsFixed => true;

    /// <summary>
    ///     Holds categorized views for single-pass processing.
    ///     Phase 1 and 2 Performance Optimization: Reduces iterations and allocations.
    /// </summary>
    private readonly struct ViewCategories
    {
        public List<View> NotDependent { get; init; }
        public List<View> Centered { get; init; }
        public List<View> Anchored { get; init; }
        public List<View> PosViewBased { get; init; }
        public List<View> DimViewBased { get; init; }
        public List<View> DimAutoBased { get; init; }
        public List<View> DimFillBased { get; init; }
        public List<int> AlignGroupIds { get; init; }
    }

    /// <summary>
    ///     Categorizes views in a single pass to reduce iterations and allocations.
    ///     Phase 1 and 2 Performance Optimization.
    /// </summary>
    private static ViewCategories CategorizeViews (IList<View> subViews, Dimension dimension)
    {
        ViewCategories categories = new ()
        {
            NotDependent = [],
            Centered = [],
            Anchored = [],
            PosViewBased = [],
            DimViewBased = [],
            DimAutoBased = [],
            DimFillBased = [],
            AlignGroupIds = []
        };

        HashSet<int> seenAlignGroupIds = new ();

        foreach (View v in subViews)
        {
            Pos pos = dimension == Dimension.Width ? v.X : v.Y;
            Dim dim = dimension == Dimension.Width ? v.Width : v.Height;

            // Check for not dependent views first (most common case)
            if ((pos.IsFixed || dim.IsFixed) && !pos.DependsOnSuperViewContentSize && !dim.DependsOnSuperViewContentSize)
            {
                categories.NotDependent.Add (v);
            }

            // Check for centered views
            if (pos.Has<PosCenter> (out _))
            {
                categories.Centered.Add (v);
            }

            // Check for anchored views
            if (pos.Has<PosAnchorEnd> (out _))
            {
                categories.Anchored.Add (v);
            }

            // Check for PosView based views
            if (pos.Has<PosView> (out _))
            {
                categories.PosViewBased.Add (v);
            }

            // Check for DimView based views
            if (dim.Has<DimView> (out _))
            {
                categories.DimViewBased.Add (v);
            }

            // Check for DimAuto based views
            if (dim.Has<DimAuto> (out _))
            {
                categories.DimAutoBased.Add (v);
            }

            // Check for DimFill based views that can contribute
            if (dim.Has<DimFill> (out _) && dim.CanContributeToAutoSizing)
            {
                categories.DimFillBased.Add (v);
            }

            // Collect align group IDs
            if (!pos.Has (out PosAlign posAlign))
            {
                continue;
            }

            if (seenAlignGroupIds.Add (posAlign.GroupId))
            {
                categories.AlignGroupIds.Add (posAlign.GroupId);
            }
        }

        return categories;
    }

    /// <summary>
    ///     Calculates maximum size from a pre-categorized list of views.
    ///     Phase 1 and 2 Performance Optimization: Avoids redundant filtering.
    /// </summary>
    private static int CalculateMaxSizeFromList (List<View> views, int max, Dimension dimension)
    {
        foreach (View v in views)
        {
            int newMax = dimension == Dimension.Width
                             ? v.Frame.X + v.Width.Calculate (0, max, v, dimension)
                             : v.Frame.Y + v.Height.Calculate (0, max, v, dimension);

            if (newMax > max)
            {
                max = newMax;
            }
        }

        return max;
    }

    /// <inheritdoc/>
    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        var textSize = 0;
        var maxCalculatedSize = 0;

        // 2048 x 2048 supports unit testing where no App is running.
        Size screenSize = us.App?.Screen.Size ?? new Size (2048, 2048);
        int autoMin = MinimumContentDim?.GetAnchor (superviewContentSize) ?? 0;
        int autoMax = MaximumContentDim?.GetAnchor (superviewContentSize) ?? int.MaxValue;

        if (Style.FastHasFlags (DimAutoStyle.Text))
        {
            if (dimension == Dimension.Width)
            {
                if (us.TextFormatter.ConstrainToWidth is null)
                {
                    // Set BOTH width and height (by setting Size). We do this because we will be called again, next
                    // for Dimension.Height. We need to know the width to calculate the height.
                    us.TextFormatter.ConstrainToSize = us.TextFormatter.FormatAndGetSize (new Size (autoMax, int.MaxValue));
                }

                textSize = us.TextFormatter.ConstrainToWidth ?? 0;
            }
            else
            {
                // For height, we need to make sure width has been calculated.
                if (us.TextFormatter.ConstrainToHeight is null)
                {
                    int width = int.Min (MaximumContentDim?.GetAnchor (superviewContentSize) ?? int.MaxValue, screenSize.Width * 4);

                    if (us.TextFormatter.ConstrainToWidth is null)
                    {
                        // Use Viewport.Width if available; fall back to the max-based width when
                        // the view hasn't been laid out yet (Viewport.Width == 0) to avoid
                        // constraining the text to zero width which produces height = 0.
                        int constrainWidth = us.Viewport.Width > 0 ? us.Viewport.Width : width;
                        width = us.TextFormatter.FormatAndGetSize (new Size (constrainWidth, int.MaxValue)).Width;
                    }

                    textSize = us.TextFormatter.FormatAndGetSize (new Size (us.TextFormatter.ConstrainToWidth ?? width, int.MaxValue)).Height;
                    us.TextFormatter.ConstrainToHeight = textSize;
                }
                else
                {
                    textSize = us.TextFormatter.ConstrainToHeight.Value;
                }
            }
        }

        if (Style.FastHasFlags (DimAutoStyle.Content))
        {
            maxCalculatedSize = textSize;

            if (us is { ContentSizeTracksViewport: false, InternalSubViews.Count: 0 })
            {
                // ContentSize was explicitly set. Use `us.ContentSize` to determine size.
                maxCalculatedSize = dimension == Dimension.Width ? us.GetContentWidth () : us.GetContentHeight ();
            }
            else
            {
                // Single-pass categorization to reduce iterations and allocations
                // Work directly with the collection to avoid unnecessary ToList() allocation

                // Categorize views in a single pass
                ViewCategories categories = CategorizeViews (us.InternalSubViews, dimension);

                // Process not-dependent views
                foreach (View notDependentSubView in categories.NotDependent)
                {
                    notDependentSubView.SetRelativeLayout (us.GetContentSize ());

                    int size = dimension == Dimension.Width
                                   ? notDependentSubView.X.GetAnchor (0)
                                     + notDependentSubView.Width.Calculate (0, superviewContentSize, notDependentSubView, dimension)
                                   : notDependentSubView.Y.GetAnchor (0)
                                     + notDependentSubView.Height.Calculate (0, superviewContentSize, notDependentSubView, dimension);

                    if (size > maxCalculatedSize)
                    {
                        maxCalculatedSize = size;
                    }
                }

                // Process centered views
                var maxCentered = 0;

                foreach (View v in categories.Centered)
                {
                    maxCentered = dimension == Dimension.Width
                                      ? v.X.GetAnchor (0) + v.Width.Calculate (0, int.MaxValue, v, dimension)
                                      : v.Y.GetAnchor (0) + v.Height.Calculate (0, int.MaxValue, v, dimension);
                }

                maxCalculatedSize = int.Max (maxCalculatedSize, maxCentered);

                // Process aligned views
                var maxAlign = 0;

                foreach (int groupId in categories.AlignGroupIds)
                {
                    // Convert to IReadOnlyCollection for PosAlign API
                    maxAlign = PosAlign.CalculateMinDimension (groupId, us.InternalSubViews.ToArray (), dimension);
                }

                maxCalculatedSize = int.Max (maxCalculatedSize, maxAlign);

                // Process anchored views
                var maxAnchorEnd = 0;

                foreach (View anchoredSubView in categories.Anchored)
                {
                    // Need to set the relative layout for PosAnchorEnd subviews to calculate the size
                    anchoredSubView.SetRelativeLayout (dimension == Dimension.Width
                                                           ? new Size (maxCalculatedSize, int.MaxValue)
                                                           : new Size (int.MaxValue, maxCalculatedSize));

                    maxAnchorEnd = dimension == Dimension.Width
                                       ? anchoredSubView.X.GetAnchor (maxCalculatedSize + anchoredSubView.Frame.Width)
                                       : anchoredSubView.Y.GetAnchor (maxCalculatedSize + anchoredSubView.Frame.Height);
                }

                maxCalculatedSize = Math.Max (maxCalculatedSize, maxAnchorEnd);

                // Process PosView, DimView, and DimAuto based views
                maxCalculatedSize = CalculateMaxSizeFromList (categories.PosViewBased, maxCalculatedSize, dimension);
                maxCalculatedSize = CalculateMaxSizeFromList (categories.DimViewBased, maxCalculatedSize, dimension);
                maxCalculatedSize = CalculateMaxSizeFromList (categories.DimAutoBased, maxCalculatedSize, dimension);

                // Process DimFill views that can contribute
                foreach (View dimFillSubView in categories.DimFillBased)
                {
                    Dim dimFill = dimension == Dimension.Width ? dimFillSubView.Width : dimFillSubView.Height;

                    // Get the minimum contribution from the Dim itself
                    int minContribution = dimFill.GetMinimumContribution (0, maxCalculatedSize, dimFillSubView, dimension);

                    if (minContribution > 0)
                    {
                        // Add position offset to get total size needed
                        int positionOffset = dimension == Dimension.Width ? dimFillSubView.Frame.X : dimFillSubView.Frame.Y;
                        int totalSize = positionOffset + minContribution;

                        if (totalSize > maxCalculatedSize)
                        {
                            maxCalculatedSize = totalSize;
                        }
                    }

                    // Handle special case for DimFill with To (still needs type-specific logic)
                    if (dimFill is not DimFill dimFillTyped || dimFillTyped.To is null)
                    {
                        continue;
                    }

                    // The SuperView needs to be large enough to contain both the dimFillSubView and the To view
                    int dimFillPos = dimension == Dimension.Width ? dimFillSubView.Frame.X : dimFillSubView.Frame.Y;
                    int toViewPos = dimension == Dimension.Width ? dimFillTyped.To.Frame.X : dimFillTyped.To.Frame.Y;
                    int toViewSize = dimension == Dimension.Width ? dimFillTyped.To.Frame.Width : dimFillTyped.To.Frame.Height;
                    int totalSizeTo = int.Max (dimFillPos, toViewPos + toViewSize);

                    if (totalSizeTo > maxCalculatedSize)
                    {
                        maxCalculatedSize = totalSizeTo;
                    }
                }
            }
        }

        // All sizes here are content-relative; ignoring adornments.
        // We take the largest of text and content.
        int max = int.Max (textSize, maxCalculatedSize);

        // And, if min: is set, it wins if larger
        max = int.Max (max, autoMin);

        // And, if max: is set, it wins if smaller
        max = int.Min (max, autoMax);

        Thickness thickness = us.GetAdornmentsThickness ();

        int adornmentThickness = dimension switch
        {
            Dimension.Width => thickness.Horizontal,
            Dimension.Height => thickness.Vertical,
            Dimension.None => 0,
            _ => throw new ArgumentOutOfRangeException (nameof (dimension), dimension, null)
        };

        max += adornmentThickness;

        return max;
    }

    /// <inheritdoc/>
    protected override bool HasInner<TDim> (out TDim dim)
    {
        if (MinimumContentDim is { } && MinimumContentDim.Has (out dim))
        {
            return true;
        }

        if (MaximumContentDim is { } && MaximumContentDim.Has (out dim))
        {
            return true;
        }

        dim = null!;

        return false;
    }
}
