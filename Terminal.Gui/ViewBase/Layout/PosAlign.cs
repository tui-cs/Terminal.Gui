using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     Enables alignment of a set of views.
/// </summary>
/// <remarks>
///     <para>
///         Updating the properties of <see cref="Aligner"/> is supported, but will not automatically cause re-layout to
///         happen. <see cref="View.Layout()"/>
///         must be called on the SuperView.
///     </para>
///     <para>
///         Views that should be aligned together must have a distinct <see cref="GroupId"/>. When only a single
///         set of views is aligned within a SuperView, setting <see cref="GroupId"/> is optional because it defaults to 0.
///     </para>
///     <para>
///         The first view added to the Superview with a given <see cref="GroupId"/> is used to determine the alignment of
///         the group.
///         The alignment is applied to all views with the same <see cref="GroupId"/>.
///     </para>
/// </remarks>
public record PosAlign : Pos
{
    /// <summary>
    ///     The cached location. Used to store the calculated location to minimize recalculating it.
    /// </summary>
    internal int? _cachedLocation;

    private readonly Aligner? _aligner;

    /// <summary>
    ///     Gets the alignment settings.
    /// </summary>
    public required Aligner Aligner
    {
        get => _aligner!;
        init
        {
            if (_aligner is { })
            {
                _aligner.PropertyChanged -= Aligner_PropertyChanged;
            }

            _aligner = value;
            _aligner.PropertyChanged += Aligner_PropertyChanged;
        }
    }

    // TODO: PosAlign.CalculateMinDimension is a hack. Need to figure out a better way of doing this.
    /// <summary>
    ///     Returns the minimum size a group of views with the same <paramref name="groupId"/> can be.
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="views"></param>
    /// <param name="dimension"></param>
    /// <returns></returns>
    public static int CalculateMinDimension (int groupId, IReadOnlyCollection<View> views, Dimension dimension)
    {
        var dimensionsSum = 0;

        foreach (View view in views)
        {
            if (!HasGroupId (view, dimension, groupId))
            {
                continue;
            }

            PosAlign? posAlign = dimension == Dimension.Width ? view.X as PosAlign : view.Y as PosAlign;

            if (posAlign is { })
            {
                dimensionsSum += dimension == Dimension.Width ? view.Frame.Width : view.Frame.Height;
            }
        }

        // Align
        return dimensionsSum;
    }

    internal static bool HasGroupId (View v, Dimension dimension, int groupId) =>
        dimension switch
        {
            Dimension.Width when v.X.Has (out PosAlign pos) => pos.GroupId == groupId,
            Dimension.Height when v.Y.Has (out PosAlign pos) => pos.GroupId == groupId,
            _ => false
        };

    /// <summary>
    ///     Gets the identifier of a set of views that should be aligned together. When only a single
    ///     set of views in a SuperView is aligned, setting <see cref="GroupId"/> is not needed because it defaults to 0.
    /// </summary>
    public int GroupId { get; init; }

    /// <inheritdoc/>
    public override string ToString () => $"Align(alignment={Aligner.Alignment},modes={Aligner.AlignmentModes},groupId={GroupId})";

    internal override int Calculate (int superviewDimension, Dim dim, View us, Dimension dimension)
    {
        if (_cachedLocation.HasValue && Aligner.ContainerSize == superviewDimension && !us.NeedsLayout)
        {
            return _cachedLocation.Value;
        }

        List<View> groupViews = us.SuperView is null ? [us] : us.SuperView!.SubViews.Snapshot ().Where (v => HasGroupId (v, dimension, GroupId)).ToList ();

        AlignAndUpdateGroup (GroupId, groupViews, dimension, superviewDimension);

        if (_cachedLocation.HasValue)
        {
            return _cachedLocation.Value;
        }

        return 0;
    }

    internal override int GetAnchor (int width) => _cachedLocation ?? 0 - width;

    /// <summary>
    ///     Aligns the views in <paramref name="views"/> that have the same group ID as <paramref name="groupId"/>.
    ///     Updates each view's cached _location.
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="views"></param>
    /// <param name="dimension"></param>
    /// <param name="size"></param>
    private static void AlignAndUpdateGroup (int groupId, IList<View> views, Dimension dimension, int size)
    {
        List<int> dimensionsList = new ();

        // PERF: If this proves a perf issue, consider caching a ref to this list in each item
        List<PosAlign?> posAligns = views.Where (v => HasGroupId (v, dimension, groupId))
                                         .Select (v => dimension == Dimension.Width ? v.X as PosAlign : v.Y as PosAlign)
                                         .ToList ();

        // PERF: We iterate over viewsInGroup multiple times here.

        Aligner? firstInGroup = null;

        // Update the dimensionList with the sizes of the views
        for (var index = 0; index < posAligns.Count; index++)
        {
            if (posAligns [index] is null)
            {
                continue;
            }
            firstInGroup ??= posAligns [index]!.Aligner;

            dimensionsList.Add (dimension == Dimension.Width
                                    ? views [index].Width.Calculate (0, size, views [index], dimension)
                                    : views [index].Height.Calculate (0, size, views [index], dimension));
        }

        if (firstInGroup is null)
        {
            return;
        }

        // Update the first item in the group with the new container size.
        firstInGroup.ContainerSize = size;

        // Align
        int [] locations = firstInGroup.Align (dimensionsList.ToArray ());

        // Update the cached location for each item
        for (int posIndex = 0, locIndex = 0; posIndex < posAligns.Count; posIndex++)
        {
            if (posAligns [posIndex] is { })
            {
                posAligns [posIndex]!._cachedLocation = locations [locIndex++];
            }
        }
    }

    internal override bool ReferencesOtherViews () => true;

    /// <inheritdoc/>
    internal override bool DependsOnSuperViewContentSize => true;

    private void Aligner_PropertyChanged (object? sender, PropertyChangedEventArgs e) => _cachedLocation = null;
}
