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
        Source?.Dispose ();
        base.Dispose (disposing);
    }

    #region IListDataSource

    /// <summary>
    ///     Use <see cref="IListDataSource.MaxItemLength"/> to get the maximum item length. Note that the behavior is dependent
    ///     on the data source.
    /// </summary>
    public int MaxItemLength => Source?.MaxItemLength ?? 0;

    /// <summary>Sets the source of the <see cref="ListView"/> to an <see cref="IList"/>.</summary>
    /// <value>An object implementing the IList interface.</value>
    /// <remarks>
    ///     Use the <see cref="Source"/> property to set a new <see cref="IListDataSource"/> source and use custom
    ///     rendering.
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

    /// <summary>Sets the source to an <see cref="IList"/> value asynchronously.</summary>
    /// <value>An item implementing the IList interface.</value>
    /// <remarks>
    ///     Use the <see cref="Source"/> property to set a new <see cref="IListDataSource"/> source and use custom
    ///     rendering.
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

    /// <summary>Gets or sets the <see cref="IListDataSource"/> backing this <see cref="ListView"/>, enabling custom rendering.</summary>
    /// <value>The source.</value>
    /// <remarks>Use <see cref="SetSource{T}"/> to set a new <see cref="IList"/> source.</remarks>
    public IListDataSource? Source
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field?.Dispose ();
            field = value;

            if (field is { })
            {
                field.CollectionChanged += SourceOnCollectionChanged;
                SetContentSize (new Size (EffectiveMaxItemLength, field?.Count ?? Viewport.Height));
                KeystrokeNavigator?.Collection = field?.ToList ();
            }

            SelectedItem = null;
            _lastSelectedItem = null;
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
        SetContentSize (new Size (EffectiveMaxItemLength, Source?.Count ?? Viewport.Height));
        KeystrokeNavigator?.Collection = Source?.ToList ();

        if (Source is { Count: > 0 } && SelectedItem.HasValue && SelectedItem > Source.Count - 1)
        {
            SelectedItem = Source.Count - 1;
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

    #region Viewport/ContentSize Management

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e) => SetContentSize (new Size (EffectiveMaxItemLength, Source?.Count ?? Viewport.Height));

    /// <summary>
    ///     INTERNAL: Gets the columns reserved for mark rendering (mark glyph + separator column). Wide glyphs may
    ///     consume the separator column.
    /// </summary>
    private int MarkWidth => ShowMarks ? 2 : 0;

    /// <summary>INTERNAL: Gets the effective content width including mark columns when <see cref="ShowMarks"/> is true.</summary>
    private int EffectiveMaxItemLength => MaxItemLength + MarkWidth;

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
