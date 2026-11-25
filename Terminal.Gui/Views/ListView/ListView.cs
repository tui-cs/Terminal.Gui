using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides a scrollable list of data where each item can be activated to perform an
///     action.
/// </summary>
/// <remarks>
/// <img src="../images/views/ListView.gif" alt="ListView demo"/>
///     <para>
///         The <see cref="ListView"/> displays lists of data and allows the user to scroll through the data. Items in
///         the can be activated firing an event (with the ENTER key or a mouse double-click). If the
///         <see cref="ShowMarks"/> property is true, elements of the list can be marked by the user.
///     </para>
///     <para>
///         By default <see cref="ListView"/> uses <see cref="object.ToString"/> to render the items of any
///         <see cref="ObservableCollection{T}"/> object (e.g. arrays, <see cref="List{T}"/>, and other collections).
///         Alternatively, an
///         object that implements <see cref="IListDataSource"/> can be provided giving full control of what is rendered.
///     </para>
///     <para>
///         <see cref="ListView"/> can display any object that implements the <see cref="IList"/> interface.
///         <see cref="string "/> values are converted into <see cref="string "/> values before rendering, and other values
///         are converted into <see cref="string "/> by calling <see cref="object.ToString"/> and then converting to
///         <see cref="string "/> .
///     </para>
///     <para>
///         To change the contents of the ListView, set the <see cref="Source"/> property (when providing custom
///         rendering via <see cref="IListDataSource"/>) or call <see cref="SetSource{T}"/> an <see cref="IList"/> is being
///         used.
///     </para>
///     <para>
///         When <see cref="ShowMarks"/> is set to true the rendering will prefix the rendered items with [x] or [ ]
///         and bind the SPACE key to toggle the selection. To implement a different marking style set
///         <see cref="ShowMarks"/> to false and implement custom rendering.
///     </para>
///     <para>
///         Searching the ListView with the keyboard is supported. Users type the first characters of an item, and the
///         first item that starts with what the user types will be selected.
///     </para>
///     <para>
///         By default each item is one row tall. Set <see cref="ItemHeight"/> to a larger value to give every item
///         that many rows. Set <see cref="ItemAutoHeight"/> to size each visible item from its own lines instead.
///         Each of those items is at least one row, and <see cref="ItemHeight"/> is not used.
///         Without column headers, each newline starts a new row, and a line wider than the width
///         wraps onto the following rows. <see cref="ItemsVerticalAlignment"/> places those lines inside the item.
///         A line break inside a column stays in that column. Columns stay side by side.
///         A list or array item places element <c>i</c> in column <c>i</c>.
///         Set <see cref="ColumnHeaders"/> to name those columns, including properties of arbitrary item types.
///         <see cref="ShowColumnHeaders"/> pins a header band above the items. <see cref="ColumnHeader.Height"/> sets
///         how many rows that column's header uses, and the band is as tall as the tallest header. A line break in a
///         header starts another row of that header. A line wider than the column wraps when that header is taller
///         than one row. <see cref="ColumnHeader.HeaderVerticalAlignment"/> places those lines inside the header.
///     </para>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Up, Ctrl+P</term> <description>Moves up one item.</description>
///         </item>
///         <item>
///             <term>Down, Ctrl+N</term> <description>Moves down one item.</description>
///         </item>
///         <item>
///             <term>PageUp / PageDown</term> <description>Moves one page up or down.</description>
///         </item>
///         <item>
///             <term>Home / End</term> <description>Moves to the first or last item.</description>
///         </item>
///         <item>
///             <term>Shift+&lt;movement&gt;</term>
///             <description>Extends the selection in the given direction.</description>
///         </item>
///         <item>
///             <term>Ctrl+A</term> <description>Selects all items.</description>
///         </item>
///         <item>
///             <term>Ctrl+U</term> <description>Deselects all items.</description>
///         </item>
///     </list>
///     <para>Default mouse bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Mouse Event</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Click</term> <description>Activates (selects) the clicked item.</description>
///         </item>
///         <item>
///             <term>Double-Click</term>
///             <description>Accepts the clicked item (<see cref="Command.Accept"/>).</description>
///         </item>
///         <item>
///             <term>Wheel Up / Down</term> <description>Scrolls the list.</description>
///         </item>
///     </list>
/// </remarks>
public partial class ListView : View, IDesignable, IValue<int?>
{
    /// <summary>
    ///     Initializes a new instance of <see cref="ListView"/>. Set the <see cref="Source"/> property to display
    ///     something.
    /// </summary>
    public ListView ()
    {
        CanFocus = true;

        SetupBindingsAndCommands ();
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        ListWrapper<string> source = new (["List Item 1", "List Item two", "List Item 3", "List Item Quattro", "Last List Item"]);
        Source = source;

        return true;
    }

    /// <inheritdoc/>
    public string? GetDemoKeyStrokes () => "wait:500,CursorDown,wait:400,CursorDown,wait:400,CursorDown,wait:400,CursorUp,wait:800";

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            DetachColumnHeaders ();
        }

        Source?.Dispose ();
        base.Dispose (disposing);
    }

    #region IListDataSource

    /// <summary>
    ///     Use <see cref="IListDataSource.MaxItemLength"/> to get the maximum item length. Note that the behavior is dependent
    ///     on the data source.
    /// </summary>
    public int MaxItemLength => Source?.MaxItemLength ?? 0;

    /// <summary>
    ///     Sets the items from an <see cref="ObservableCollection{T}"/> using the default
    ///     <see cref="IListDataSource"/> for collections.
    /// </summary>
    /// <param name="source">
    ///     The collection to display, or <see langword="null"/> to clear items through the default
    ///     collection data-source path (see remarks).
    /// </param>
    /// <remarks>
    ///     <para>
    ///         When <paramref name="source"/> is non-<see langword="null"/>, <see cref="Source"/> is set to
    ///         an <see cref="IListDataSource"/> that wraps that collection (the built-in implementation is
    ///         <see cref="ListWrapper{T}"/>).
    ///     </para>
    ///     <para>
    ///         When <paramref name="source"/> is <see langword="null"/>:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     If <see cref="Source"/> is already the default collection data source for
    ///                     <typeparamref name="T"/>, it is replaced with an empty default data source
    ///                     (<see cref="IListDataSource.Count"/> is 0). <see cref="Source"/> stays non-<see langword="null"/>.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Otherwise (no source, or a custom <see cref="IListDataSource"/>),
    ///                     <see cref="Source"/> is set to <see langword="null"/>.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         To always detach the data source regardless of its type, assign <c>Source = null</c>.
    ///     </para>
    /// </remarks>
    public void SetSource<T> (ObservableCollection<T>? source)
    {
        if (source is null && Source is not ListWrapper<T>)
        {
            Source = null;
        }
        else
        {
            Source = new ListWrapper<T> (source);
        }
    }

    /// <summary>
    ///     Sets the items from an <see cref="ObservableCollection{T}"/> using the default
    ///     <see cref="IListDataSource"/> for collections.
    /// </summary>
    /// <param name="source">
    ///     The collection to display, or <see langword="null"/> to clear items through the default
    ///     collection data-source path (see remarks).
    /// </param>
    /// <remarks>
    ///     <para>
    ///         When <paramref name="source"/> is non-<see langword="null"/>, <see cref="Source"/> is set to
    ///         an <see cref="IListDataSource"/> that wraps that collection (the built-in implementation is
    ///         <see cref="ListWrapper{T}"/>).
    ///     </para>
    ///     <para>
    ///         When <paramref name="source"/> is <see langword="null"/>:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     If <see cref="Source"/> is already the default collection data source for
    ///                     <typeparamref name="T"/>, it is replaced with an empty default data source
    ///                     (<see cref="IListDataSource.Count"/> is 0). <see cref="Source"/> stays non-<see langword="null"/>.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Otherwise (no source, or a custom <see cref="IListDataSource"/>),
    ///                     <see cref="Source"/> is set to <see langword="null"/>.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         To always detach the data source regardless of its type, assign <c>Source = null</c>.
    ///     </para>
    /// </remarks>
    public Task SetSourceAsync<T> (ObservableCollection<T>? source) =>
        Task.Factory.StartNew (() =>
                               {
                                   if (source is null && Source is not ListWrapper<T>)
                                   {
                                       Source = null;
                                   }
                                   else
                                   {
                                       Source = new ListWrapper<T> (source);
                                   }

                                   return source;
                               },
                               CancellationToken.None,
                               TaskCreationOptions.DenyChildAttach,
                               TaskScheduler.Default);

    /// <summary>
    ///     Gets or sets the <see cref="IListDataSource"/> that provides items for this <see cref="ListView"/>.
    /// </summary>
    /// <value>
    ///     The current data source, or <see langword="null"/> if none is attached.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         Assign <see langword="null"/> to detach the data source completely.
    ///     </para>
    ///     <para>
    ///         For an <see cref="ObservableCollection{T}"/>, <see cref="SetSource{T}"/> and
    ///         <see cref="SetSourceAsync{T}"/> install a default <see cref="IListDataSource"/> implementation.
    ///         Use this property directly when supplying a custom <see cref="IListDataSource"/>.
    ///     </para>
    ///     <para>
    ///         Clearing via <see cref="SetSource{T}"/> with a <see langword="null"/> collection is not always
    ///         the same as assigning <see langword="null"/> here; see those methods' remarks.
    ///     </para>
    /// </remarks>
    public IListDataSource? Source
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            // Clear selection while Source is still the old non-null value (if any),
            // so SelectedItem's setter can run; also force-clear after for the null case.
            SelectedItem = null;
            _lastSelectedItem = null;

            field?.Dispose ();
            field = value;

            if (field is { })
            {
                field.CollectionChanged += SourceOnCollectionChanged;
                UpdateContentSize ();
                KeystrokeNavigator?.Collection = field?.ToList ();
            }
            else
            {
                // No source: empty content; ensure selection stays null even if setter was picky.
                // (SelectedItem setter fix should make SelectedItem = null above sufficient;
                //  setting field = null here is belt-and-braces if you assign via a private path.)
                UpdateContentSize ();
                KeystrokeNavigator?.Collection = null;
            }

            // Reset scroll so a shorter/empty list is not drawn off-screen.
            if (Viewport.Y != 0)
            {
                Viewport = Viewport with { Y = 0 };
            }

            OnSourceChanged ();
            SourceChanged?.Invoke (this, EventArgs.Empty);
            SetNeedsDraw ();
        }
    }

    // TODO: Make these match the pattern in IListDataSource where it's just a property.
    /// <summary>
    ///     Allow suspending the <see cref="CollectionChanged"/> event from being invoked,
    /// </summary>
    public void SuspendCollectionChangedEvent () => Source?.SuspendCollectionChangedEvent = true;

    /// <summary>
    ///     Allow resume the <see cref="CollectionChanged"/> event from being invoked,
    /// </summary>
    public void ResumeSuspendCollectionChangedEvent () => Source?.SuspendCollectionChangedEvent = false;

    // TODO: Make CollectionChange follow the CWP
    private void SourceOnCollectionChanged (object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateContentSize ();
        KeystrokeNavigator?.Collection = Source?.ToList ();

        if (Source is { Count: > 0 } && SelectedItem.HasValue && SelectedItem > Source.Count - 1)
        {
            SelectedItem = Source.Count - 1;
        }
        else if (Source is null || Source.Count == 0)
        {
            SelectedItem = null;
        }

        SetNeedsDraw ();
        OnCollectionChanged (e);
    }

    /// <summary>
    ///     Called when the <see cref="Source"/> property is changed. Invokes the <see cref="SourceChanged"/> event.
    /// </summary>
    protected virtual void OnSourceChanged () { }

    /// <summary>
    ///     Event to raise when the <see cref="Source"/> data is changed.
    /// </summary>
    public event EventHandler? SourceChanged;

    /// <summary>
    ///     Call the event to raises the <see cref="CollectionChanged"/>.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnCollectionChanged (NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke (this, e);

    /// <summary>
    ///     Event to raise when an item is added, removed, or moved, or the entire list is refreshed.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    #endregion IListDataSource

    #region Item height and columns

    /// <summary>
    ///     Gets or sets how many rows each item occupies when <see cref="ItemAutoHeight"/> is <see langword="false"/>.
    ///     The default is one.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than one.</exception>
    public int ItemHeight
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan (value, 1);

            if (field == value)
            {
                return;
            }

            field = value;
            UpdateContentSize ();
            EnsureSelectedItemVisible ();
            SetNeedsDraw ();
        }
    } = 1;

    /// <summary>
    ///     Gets or sets whether each visible item grows to fit its lines. The default is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When <see langword="false"/>, every item occupies <see cref="ItemHeight"/> rows.
    ///         When <see langword="true"/>, a visible item is as tall as its wrapped lines and is at least one row.
    ///         <see cref="ItemHeight"/> does not change that height.
    ///     </para>
    ///     <para>
    ///         Heights are measured for the items in the viewport, and for the trailing page that keeps the last item
    ///         reachable. The rest of the list is not measured.
    ///     </para>
    /// </remarks>
    public bool ItemAutoHeight
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            UpdateContentSize ();
            EnsureSelectedItemVisible ();
            SetNeedsDraw ();
        }
    }

    /// <summary>Gets the number of items that fit in the viewport.</summary>
    public int VisibleItems => !ItemAutoHeight && ItemHeight <= 1 && !ShowColumnHeaders ? Viewport.Height : VisibleItemCount;

    /// <summary>Gets or sets whether a header row is pinned above the items.</summary>
    public bool ShowColumnHeaders
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            UpdateContentSize ();
            EnsureSelectedItemVisible ();
            SetNeedsDraw ();
        }
    }

    /// <summary>Gets or sets the number of columns between columns. The default is 2.</summary>
    public int ColumnMargin
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            SetNeedsDraw ();
        }
    } = 2;

    /// <summary>
    ///     Gets or sets the horizontal header alignment used when a <see cref="ColumnHeader"/> is not set.
    /// </summary>
    public Alignment HeadersAlignment { get; set; } = Alignment.Start;

    /// <summary>
    ///     Gets or sets how header lines are placed when a <see cref="ColumnHeader"/> is not set.
    /// </summary>
    /// <remarks>
    ///     Placement matches <see cref="ItemsVerticalAlignment"/>. A header band with no <see cref="ColumnHeaders"/> is
    ///     one row tall and shows <c>Col0</c>.
    /// </remarks>
    public Alignment HeadersVerticalAlignment { get; set; } = Alignment.Start;

    /// <summary>
    ///     Gets or sets the horizontal cell alignment used when a <see cref="ColumnHeader"/> is not set.
    /// </summary>
    /// <remarks>
    ///     When a <see cref="ColumnHeader"/> is set, <see cref="ColumnHeader.MemberHorizontalAlignment"/> is used instead.
    /// </remarks>
    public Alignment ItemsHorizontalAlignment { get; set; } = Alignment.Start;

    /// <summary>Gets or sets how a cell's lines are placed inside <see cref="ItemHeight"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="Alignment.Start"/> keeps the first line at the top.
    ///         <see cref="Alignment.End"/> keeps the last line at the bottom and clips lines that do not fit from the top.
    ///         <see cref="Alignment.Center"/> centers the lines, and keeps the first lines when they do not fit.
    ///         <see cref="Alignment.Fill"/> spreads the lines across the item and leaves the extra rows between them.
    ///         A single line stays at the top. Any remainder after an even split is placed in the later gaps.
    ///     </para>
    ///     <para>
    ///         The alignment applies when <see cref="ItemHeight"/> is greater than one. Each column uses its own lines.
    ///         When a <see cref="ColumnHeader"/> is set, <see cref="ColumnHeader.MemberVerticalAlignment"/> is used instead.
    ///     </para>
    /// </remarks>
    public Alignment ItemsVerticalAlignment { get; set; } = Alignment.Start;

    /// <summary>
    ///     Gets or sets the columns used to render items. A list or array item places element <c>i</c> in column
    ///     <c>i</c>. A line break in a column value starts another row of that column. Other items are read through
    ///     each header's <see cref="ColumnHeader.MemberName"/>.
    /// </summary>
    public List<ColumnHeader>? ColumnHeaders
    {
        get;
        set
        {
            DetachColumnHeaders ();
            field = value;
            AttachColumnHeaders ();
            UpdateContentSize ();
            SetNeedsDraw ();
        }
    }

    /// <summary>Rows occupied by the pinned header band. Zero when headers are hidden.</summary>
    internal int HeaderRowCount
    {
        get
        {
            if (!ShowColumnHeaders)
            {
                return 0;
            }

            if (ColumnHeaders is not { Count: > 0 } headers)
            {
                return 1;
            }

            int height = 1;

            foreach (ColumnHeader header in headers)
            {
                height = Math.Max (height, header.Height);
            }

            return height;
        }
    }

    private void AttachColumnHeaders ()
    {
        if (ColumnHeaders is not { } headers)
        {
            return;
        }

        foreach (ColumnHeader header in headers)
        {
            header.HeightChanged += OnColumnHeaderHeightChanged;
        }
    }

    private void DetachColumnHeaders ()
    {
        if (ColumnHeaders is not { } headers)
        {
            return;
        }

        foreach (ColumnHeader header in headers)
        {
            header.HeightChanged -= OnColumnHeaderHeightChanged;
        }
    }

    private void OnColumnHeaderHeightChanged (object? sender, EventArgs args)
    {
        UpdateContentSize ();
        EnsureSelectedItemVisible ();
        SetNeedsDraw ();
    }

    #endregion Item height and columns

    #region Viewport/ContentSize Management

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e) => UpdateContentSize ();

    /// <summary>
    ///     INTERNAL: Gets the columns reserved for mark rendering (mark glyph + separator column). Wide glyphs may
    ///     consume the separator column.
    /// </summary>
    private int MarkWidth => ShowMarks ? 2 : 0;

    /// <summary>INTERNAL: Gets the effective content width including mark columns when <see cref="ShowMarks"/> is true.</summary>
    private int EffectiveMaxItemLength => MaxItemLength + MarkWidth;

    /// <summary>Rows available for items after a pinned header.</summary>
    private int VisibleItemCount
    {
        get
        {
            int rows = Math.Max (0, Viewport.Height - HeaderRowCount);

            if (!ItemAutoHeight)
            {
                if (ItemHeight <= 1)
                {
                    return rows;
                }

                return rows / ItemHeight;
            }

            int total = Source?.Count ?? 0;

            if (rows == 0 || total == 0)
            {
                return 0;
            }

            PrepareItemHeights ();
            int index = Math.Max (0, Viewport.Y);
            int used = 0;
            int count = 0;

            while (index < total)
            {
                int height = RowsForItem (index);

                if (used + height > rows)
                {
                    if (count == 0)
                    {
                        return 1;
                    }

                    break;
                }

                used += height;
                count++;
                index++;
            }

            return count;
        }
    }

    /// <summary>
    ///     Number of items treated as one page. Equals <see cref="View.Viewport"/> height when every item is one row and
    ///     no header row is pinned.
    /// </summary>
    private int PageSize
    {
        get
        {
            if (!ItemAutoHeight && ItemHeight <= 1 && !ShowColumnHeaders)
            {
                return Viewport.Height;
            }

            return Math.Max (VisibleItemCount, 1);
        }
    }

    private int CalculateContentHeight ()
    {
        int count = Source?.Count ?? 0;

        if (!ItemAutoHeight)
        {
            if (count == 0 || (ItemHeight <= 1 && !ShowColumnHeaders))
            {
                return count;
            }

            return Math.Max (count, count - VisibleItemCount + Viewport.Height);
        }

        if (count == 0)
        {
            return 0;
        }

        // Viewport.Y is an item index. The trailing page is measured so the last item can sit on screen.
        return Math.Max (count, LastPageTop () + Math.Max (Viewport.Height, 1));
    }

    /// <summary>Measures the layout used by per-item heights. Safe to call more than once for the same width.</summary>
    private void PrepareItemHeights ()
    {
        if (Source is not IListViewLayout layout)
        {
            return;
        }

        layout.PrepareRender (this, Math.Max (0, Viewport.Width - MarkWidth));
    }

    /// <summary>Rows occupied by one item. <see cref="ItemHeight"/> when automatic height is off.</summary>
    private int RowsForItem (int item)
    {
        if (!ItemAutoHeight)
        {
            return Math.Max (ItemHeight, 1);
        }

        if (Source is null || item < 0 || item >= Source.Count)
        {
            return 1;
        }

        if (Source is not IListViewLayout layout)
        {
            return Math.Max (ItemHeight, 1);
        }

        return Math.Max (1, layout.MeasureItemHeight (this, item));
    }

    /// <summary>Item index at a content row below the header, measured from <see cref="View.Viewport"/>.</summary>
    private int ItemIndexAtRow (int row)
    {
        int index = Math.Max (0, Viewport.Y);
        int remaining = Math.Max (0, row);
        int count = Source?.Count ?? 0;

        while (index < count)
        {
            int height = RowsForItem (index);

            if (remaining < height)
            {
                return index;
            }

            remaining -= height;
            index++;
        }

        return index;
    }

    /// <summary>First item of the page that still includes the last item.</summary>
    private int LastPageTop ()
    {
        int count = Source?.Count ?? 0;
        int rows = Math.Max (0, Viewport.Height - HeaderRowCount);

        if (count == 0 || rows <= 0)
        {
            return 0;
        }

        PrepareItemHeights ();
        int used = 0;
        int top = count;

        while (top > 0)
        {
            int height = RowsForItem (top - 1);

            if (used > 0 && used + height > rows)
            {
                break;
            }

            used += height;
            top--;

            if (used >= rows)
            {
                break;
            }
        }

        return top;
    }

    /// <summary>Scroll origin that keeps <paramref name="item"/> on screen.</summary>
    private int TopIndexShowing (int item)
    {
        int rows = Math.Max (0, Viewport.Height - HeaderRowCount);

        if (rows <= 0)
        {
            return item;
        }

        PrepareItemHeights ();
        int used = 0;
        int top = item + 1;

        while (top > 0)
        {
            int height = RowsForItem (top - 1);

            if (used > 0 && used + height > rows)
            {
                break;
            }

            used += height;
            top--;

            if (used >= rows)
            {
                break;
            }
        }

        return top;
    }

    private void UpdateContentSize ()
    {
        if (Source is null)
        {
            SetContentSize (new Size (0, Viewport.Height));

            return;
        }

        SetContentSize (new Size (EffectiveMaxItemLength, CalculateContentHeight ()));
    }

    #endregion Viewport/ContentSize Management

    #region Keystroke Navigation

    private IListCollectionNavigator? _keystrokeNavigator = new CollectionNavigator ();

    /// <summary>
    ///     Gets or sets the <see cref="CollectionNavigator"/> that searches the <see cref="ListView.Source"/> collection as
    ///     the user types. The default implementation is a <see cref="CollectionNavigator"/> that uses the string
    ///     representation of the items in the collection. Set to <see langword="null"/> to disable keystroke navigation.
    /// </summary>
    /// <remarks>
    ///     When a new navigator is assigned, its <see cref="IListCollectionNavigator.Collection"/> is automatically
    ///     synchronized with the current <see cref="Source"/>.
    /// </remarks>
    public IListCollectionNavigator? KeystrokeNavigator
    {
        get => _keystrokeNavigator;
        set
        {
            _keystrokeNavigator = value;

            if (_keystrokeNavigator is { } && Source is { })
            {
                _keystrokeNavigator.Collection = Source.ToList ();
            }
        }
    }

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key key)
    {
        // If the key was bound to key command, let normal KeyDown processing happen. This enables overriding the default handling.
        // See: https://github.com/tui-cs/Terminal.Gui/issues/3950#issuecomment-2807350939
        if (KeyBindings.TryGet (key, out _))
        {
            return false;
        }

        // Enable user to find & select an item by typing text
        if (KeystrokeNavigator is null || !KeystrokeNavigator.Matcher.IsCompatibleKey (key))
        {
            return false;
        }

        int? newItem = KeystrokeNavigator.GetNextMatchingItem (SelectedItem ?? null, (char)key);

        if (newItem is null or -1)
        {
            return false;
        }

        SelectedItem = (int)newItem;
        EnsureSelectedItemVisible ();
        SetNeedsDraw ();

        return true;
    }

    #endregion Keystroke Navigation
}
