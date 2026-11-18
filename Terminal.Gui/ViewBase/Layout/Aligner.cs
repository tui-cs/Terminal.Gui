using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     Aligns items within a container based on the specified <see cref="Alignment"/>. Both horizontal and vertical
///     alignments are supported.
/// </summary>
public class Aligner : INotifyPropertyChanged
{
    /// <summary>
    ///     Gets or sets how the <see cref="Aligner"/> aligns items within a container.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="AlignmentModes"/> provides additional options for aligning items in a container.
    ///     </para>
    /// </remarks>
    public Alignment Alignment
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (nameof (Alignment)));
        }
    }

    /// <summary>
    ///     Gets or sets the modes controlling <see cref="Alignment"/>.
    /// </summary>
    public AlignmentModes AlignmentModes
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (nameof (AlignmentModes)));
        }
    } = AlignmentModes.StartToEnd;

    /// <summary>
    ///     The size of the container.
    /// </summary>
    public int ContainerSize
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (nameof (ContainerSize)));
        }
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///     Takes a list of item sizes and returns a list of the positions of those items when aligned within
    ///     <see name="ContainerSize"/>
    ///     using the <see cref="Alignment"/> and <see cref="AlignmentModes"/> settings.
    /// </summary>
    /// <param name="sizes">The sizes of the items to align.</param>
    /// <returns>The locations of the items, from left/top to right/bottom.</returns>
    public int [] Align (int [] sizes) => Align (Alignment, AlignmentModes, ContainerSize, sizes);

    /// <summary>
    ///     Takes a list of item sizes and returns a list of the  positions of those items when aligned within
    ///     <paramref name="containerSize"/>
    ///     using specified parameters.
    /// </summary>
    /// <param name="alignment">Specifies how the items will be aligned.</param>
    /// <param name="alignmentMode"></param>
    /// <param name="containerSize">The size of the container.</param>
    /// <param name="sizes">The sizes of the items to align.</param>
    /// <returns>The positions of the items, from left/top to right/bottom.</returns>
    public static int [] Align (in Alignment alignment, in AlignmentModes alignmentMode, in int containerSize, in int [] sizes)
    {
        if (sizes.Length == 0)
        {
            return [];
        }

        int [] sizesCopy = sizes;

        if (alignmentMode.FastHasFlags (AlignmentModes.EndToStart))
        {
            sizesCopy = sizes.Reverse ().ToArray ();
        }

        int maxSpaceBetweenItems = alignmentMode.FastHasFlags (AlignmentModes.AddSpaceBetweenItems) ? 1 : 0;
        int totalItemsSize = sizes.Sum ();
        int totalGaps = sizes.Length - 1; // total gaps between items
        int totalItemsAndSpaces = totalItemsSize + totalGaps * maxSpaceBetweenItems; // total size of items and spacesToGive if we had enough room
        int spacesToGive = totalGaps * maxSpaceBetweenItems; // We'll decrement this below to place one space between each item until we run out

        if (totalItemsSize >= containerSize)
        {
            spacesToGive = 0;
        }
        else if (totalItemsAndSpaces > containerSize)
        {
            spacesToGive = containerSize - totalItemsSize;
        }

        AlignmentModes mode = alignmentMode & ~AlignmentModes.AddSpaceBetweenItems; // copy to avoid modifying the original

        switch (alignment)
        {
            case Alignment.Start:
                switch (mode)
                {
                    case AlignmentModes.StartToEnd:
                        return Start (in sizesCopy, maxSpaceBetweenItems, spacesToGive);

                    case AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast:
                        return IgnoreLast (in sizesCopy, containerSize, totalItemsSize, maxSpaceBetweenItems, spacesToGive);

                    case AlignmentModes.EndToStart:
                        return End (in sizesCopy, containerSize, totalItemsSize, maxSpaceBetweenItems, spacesToGive).Reverse ().ToArray ();

                    case AlignmentModes.EndToStart | AlignmentModes.IgnoreFirstOrLast:
                        return IgnoreFirst (in sizesCopy, containerSize, totalItemsSize, maxSpaceBetweenItems, spacesToGive).Reverse ().ToArray ();
                }

                break;

            case Alignment.End:
                switch (mode)
                {
                    case AlignmentModes.StartToEnd:
                        return End (in sizesCopy, containerSize, totalItemsSize, maxSpaceBetweenItems, spacesToGive);

                    case AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast:
                        return IgnoreFirst (in sizesCopy, containerSize, totalItemsSize, maxSpaceBetweenItems, spacesToGive);

                    case AlignmentModes.EndToStart:
                        return Start (in sizesCopy, maxSpaceBetweenItems, spacesToGive).Reverse ().ToArray ();

                    case AlignmentModes.EndToStart | AlignmentModes.IgnoreFirstOrLast:
                        return IgnoreLast (in sizesCopy, containerSize, totalItemsSize, maxSpaceBetweenItems, spacesToGive).Reverse ().ToArray ();
                }

                break;

            case Alignment.Center:
                mode &= ~AlignmentModes.IgnoreFirstOrLast;

                switch (mode)
                {
                    case AlignmentModes.StartToEnd:
                        return Center (in sizesCopy, containerSize, totalItemsSize, maxSpaceBetweenItems, spacesToGive);

                    case AlignmentModes.EndToStart:
                        return Center (in sizesCopy, containerSize, totalItemsSize, maxSpaceBetweenItems, spacesToGive).Reverse ().ToArray ();
                }

                break;

            case Alignment.Fill:
                mode &= ~AlignmentModes.IgnoreFirstOrLast;

                switch (mode)
                {
                    case AlignmentModes.StartToEnd:
                        return Fill (in sizesCopy, containerSize, totalItemsSize);

                    case AlignmentModes.EndToStart:
                        return Fill (in sizesCopy, containerSize, totalItemsSize).Reverse ().ToArray ();
                }

                break;

            default:
                throw new ArgumentOutOfRangeException (nameof (alignment), alignment, null);
        }

        return [];
    }

    internal static int [] Start (ref readonly int [] sizes, int maxSpaceBetweenItems, int spacesToGive)
    {
        var positions = new int [sizes.Length]; // positions of the items. the return value.

        for (var i = 0; i < sizes.Length; i++)
        {
            CheckSizeCannotBeNegative (i, in sizes);

            if (i == 0)
            {
                positions [0] = 0; // first item position

                continue;
            }

            int spaceBefore = spacesToGive-- > 0 ? maxSpaceBetweenItems : 0;

            // subsequent items are placed one space after the previous item
            positions [i] = positions [i - 1] + sizes [i - 1] + spaceBefore;
        }

        return positions;
    }

    internal static int [] IgnoreFirst (ref readonly int [] sizes, int containerSize, int totalItemsSize, int maxSpaceBetweenItems, int spacesToGive)
    {
        var positions = new int [sizes.Length]; // positions of the items. the return value.

        switch (sizes.Length)
        {
            case > 1:
                {
                    var currentPosition = 0;
                    positions [0] = currentPosition; // first item is flush left

                    for (int i = sizes.Length - 1; i >= 0; i--)
                    {
                        CheckSizeCannotBeNegative (i, in sizes);

                        if (i == sizes.Length - 1)
                        {
                            // start at right
                            currentPosition = Math.Max (totalItemsSize, containerSize) - sizes [i];
                            positions [i] = currentPosition;
                        }

                        if (i >= sizes.Length - 1 || i <= 0)
                        {
                            continue;
                        }
                        int spaceBefore = spacesToGive-- > 0 ? maxSpaceBetweenItems : 0;

                        positions [i] = currentPosition - sizes [i] - spaceBefore;
                        currentPosition = positions [i];
                    }

                    break;
                }

            case 1:
                CheckSizeCannotBeNegative (0, in sizes);
                positions [0] = 0; // single item is flush left

                break;
        }

        return positions;
    }

    internal static int [] IgnoreLast (ref readonly int [] sizes, int containerSize, int totalItemsSize, int maxSpaceBetweenItems, int spacesToGive)
    {
        var positions = new int [sizes.Length]; // positions of the items. the return value.

        if (sizes.Length > 1)
        {
            var currentPosition = 0;

            if (totalItemsSize > containerSize)
            {
                // Don't allow negative positions
                currentPosition = int.Max (0, containerSize - totalItemsSize - spacesToGive);
            }

            for (var i = 0; i < sizes.Length; i++)
            {
                CheckSizeCannotBeNegative (i, in sizes);

                if (i >= sizes.Length - 1)
                {
                    continue;
                }
                int spaceBefore = spacesToGive-- > 0 ? maxSpaceBetweenItems : 0;

                positions [i] = currentPosition;
                currentPosition += sizes [i] + spaceBefore;
            }

            positions [sizes.Length - 1] = containerSize - sizes [^1];
        }
        else if (sizes.Length == 1)
        {
            CheckSizeCannotBeNegative (0, in sizes);

            positions [0] = containerSize - sizes [0]; // single item is flush right
        }

        return positions;
    }

    internal static int [] Fill (ref readonly int [] sizes, int containerSize, int totalItemsSize)
    {
        var positions = new int [sizes.Length]; // positions of the items. the return value.

        int spaceBetween = sizes.Length > 1 ? (containerSize - totalItemsSize) / (sizes.Length - 1) : 0;
        int remainder = sizes.Length > 1 ? (containerSize - totalItemsSize) % (sizes.Length - 1) : 0;
        var currentPosition = 0;

        for (var i = 0; i < sizes.Length; i++)
        {
            CheckSizeCannotBeNegative (i, in sizes);
            positions [i] = currentPosition;
            int extraSpace = i < remainder ? 1 : 0;
            currentPosition += sizes [i] + spaceBetween + extraSpace;
        }

        return positions;
    }

    internal static int [] Center (ref readonly int [] sizes, int containerSize, int totalItemsSize, int maxSpaceBetweenItems, int spacesToGive)
    {
        var positions = new int [sizes.Length]; // positions of the items. the return value.

        if (sizes.Length > 1)
        {
            // remaining space to be distributed before first and after the items
            int remainingSpace = containerSize - totalItemsSize - spacesToGive;

            for (var i = 0; i < sizes.Length; i++)
            {
                CheckSizeCannotBeNegative (i, in sizes);

                if (i == 0)
                {
                    positions [i] = remainingSpace / 2; // first item position

                    continue;
                }

                int spaceBefore = spacesToGive-- > 0 ? maxSpaceBetweenItems : 0;

                // subsequent items are placed one space after the previous item
                positions [i] = positions [i - 1] + sizes [i - 1] + spaceBefore;
            }
        }
        else if (sizes.Length == 1)
        {
            CheckSizeCannotBeNegative (0, in sizes);
            positions [0] = (containerSize - sizes [0]) / 2; // single item is centered
        }

        return positions;
    }

    internal static int [] End (ref readonly int [] sizes, int containerSize, int totalItemsSize, int maxSpaceBetweenItems, int spacesToGive)
    {
        var positions = new int [sizes.Length]; // positions of the items. the return value.
        int currentPosition = containerSize - totalItemsSize - spacesToGive;

        for (var i = 0; i < sizes.Length; i++)
        {
            CheckSizeCannotBeNegative (i, in sizes);
            int spaceBefore = spacesToGive-- > 0 ? maxSpaceBetweenItems : 0;

            positions [i] = currentPosition;
            currentPosition += sizes [i] + spaceBefore;
        }

        return positions;
    }

    private static void CheckSizeCannotBeNegative (int i, ref readonly int [] sizes)
    {
        if (sizes [i] < 0)
        {
            throw new ArgumentException ("The size of an item cannot be negative.");
        }
    }
}
