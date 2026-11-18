using System.Data;
using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>Delegate for providing color to <see cref="TableView"/> cells based on the value being rendered</summary>
/// <param name="args">Contains information about the cell for which color is needed</param>
/// <returns></returns>
public delegate Scheme? CellColorGetterDelegate (CellColorGetterArgs args);

/// <summary>Delegate for providing color for a whole row of a <see cref="TableView"/></summary>
/// <param name="args"></param>
/// <returns></returns>
public delegate Scheme? RowColorGetterDelegate (RowColorGetterArgs args);

/// <summary>Delegate for providing color to <see cref="TableView"/> column headers based on contextual information.</summary>
/// <param name="args">Contains information about the column header for which color is needed.</param>
/// <returns>A <see cref="Scheme"/> to use for rendering the header, or <see langword="null"/> to use the default.</returns>
public delegate Scheme? HeaderColorGetterDelegate (HeaderColorGetterArgs args);

/// <summary>
///     Displays and enables infinite scrolling through tabular data based on a <see cref="ITableSource"/>.
///     <a href="../docs/tableview.md">See the TableView Deep Dive for more</a>.
/// </summary>
/// <remarks>
/// <img src="../images/views/TableView.gif" alt="TableView demo"/>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Left / Right</term> <description>Moves one column left or right.</description>
///         </item>
///         <item>
///             <term>Up / Down</term> <description>Moves one row up or down.</description>
///         </item>
///         <item>
///             <term>PageUp / PageDown</term> <description>Moves one page up or down.</description>
///         </item>
///         <item>
///             <term>Home / End</term> <description>Moves to the first or last column.</description>
///         </item>
///         <item>
///             <term>Ctrl+Home / Ctrl+End</term> <description>Moves to the first or last row.</description>
///         </item>
///         <item>
///             <term>Shift+&lt;movement&gt;</term>
///             <description>Extends the selection in the given direction.</description>
///         </item>
///         <item>
///             <term>Ctrl+A</term> <description>Selects all cells.</description>
///         </item>
///     </list>
///     <para>Default mouse bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Mouse Event</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Click</term> <description>Activates the clicked cell (<see cref="Command.Activate"/>).</description>
///         </item>
///     </list>
/// </remarks>
public partial class TableView : View, IValue<TableSelection?>, IDesignable
{
    /// <summary>
    ///     The default maximum cell width for <see cref="TableView.MaxCellWidth"/> and <see cref="ColumnStyle.MaxWidth"/>
    /// </summary>
    public const int DEFAULT_MAX_CELL_WIDTH = 100;

    /// <summary>
    ///     Gets or sets the default key bindings for <see cref="TableView"/>. All standard navigation and
    ///     selection-extend bindings are inherited from <see cref="View.DefaultKeyBindings"/>, so this dictionary
    ///     is empty by default.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
    {
        // Emacs navigation
        [Command.Up] = Bind.All (Key.P.WithCtrl),
        [Command.Down] = Bind.All (Key.N.WithCtrl),
        [Command.PageDown] = Bind.All (Key.V.WithCtrl),

        // Add Home/End as additional Start/End bindings (the base layer also provides Ctrl+Home/Ctrl+End)
        [Command.Start] = Bind.All (Key.Home),
        [Command.End] = Bind.All (Key.End),
        [Command.ToggleExtend] = Bind.All (Key.Space)
    };

    /// <summary>Initializes a <see cref="TableView"/> class.</summary>
    /// <param name="table">The table to display in the control</param>
    public TableView (ITableSource table) : this () => Table = table;

    /// <summary>
    ///     Initializes a <see cref="TableView"/> class. Set the
    ///     <see cref="Table"/> property to begin editing
    /// </summary>
    public TableView ()
    {
        CanFocus = true;
        CollectionNavigator = new TableCollectionNavigator (this);

        // Things this view knows how to do
        AddCommand (Command.Right, HandleRight);
        AddCommand (Command.Left, (ctx) => MoveCursorByOffsetWithReturn (-1, 0, ctx));
        AddCommand (Command.Up, HandleUp);
        AddCommand (Command.Down, HandleDown);
        AddCommand (Command.PageUp, ctx => PageUp (false, ctx));
        AddCommand (Command.PageDown, ctx => PageDown (false, ctx));
        AddCommand (Command.LeftStart, ctx => MoveCursorToStartOfRow (false, ctx));
        AddCommand (Command.RightEnd, ctx => MoveCursorToEndOfRow (false, ctx));
        AddCommand (Command.Start, ctx => MoveCursorToStartOfTable (false, ctx));
        AddCommand (Command.End, ctx => MoveCursorToEndOfTable (false, ctx));
        AddCommand (Command.RightExtend, ctx => MoveCursorByOffset (1, 0, true, ctx));
        AddCommand (Command.LeftExtend, ctx => MoveCursorByOffset (-1, 0, true, ctx));
        AddCommand (Command.UpExtend, ctx => MoveCursorByOffset (0, -1, true, ctx));
        AddCommand (Command.DownExtend, ctx => MoveCursorByOffset (0, 1, true, ctx));
        AddCommand (Command.PageUpExtend, ctx => PageUp (true, ctx));
        AddCommand (Command.PageDownExtend, ctx => PageDown (true, ctx));
        AddCommand (Command.LeftStartExtend, ctx => MoveCursorToStartOfRow (true, ctx));
        AddCommand (Command.RightEndExtend, ctx => MoveCursorToEndOfRow (true, ctx));
        AddCommand (Command.StartExtend, ctx => MoveCursorToStartOfTable (true, ctx));
        AddCommand (Command.EndExtend, ctx => MoveCursorToEndOfTable (true, ctx));
        AddCommand (Command.ToggleExtend, ToggleExtend);
        AddCommand (Command.SelectAll, _ => SelectAll ());

        // Apply configurable key bindings (base View layer + TableView-specific layer)
        ApplyKeyBindings (View.DefaultKeyBindings, DefaultKeyBindings);

        MouseBindings.ReplaceCommands (MouseFlags.WheeledRight, Command.Right);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledLeft, Command.Left);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledDown, Command.Down);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledUp, Command.Up);

        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked, Command.Activate);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked | MouseFlags.Ctrl, Command.ToggleExtend);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked | MouseFlags.Alt, Command.ToggleExtend);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonDoubleClicked, Command.Accept);
    }

    private ITableSource? _table;

    /// <summary>The data table to render in the view.  Setting this property automatically updates and redraws the control.</summary>
    public ITableSource? Table
    {
        get => _table;
        set
        {
            _table = value;

            if (_table is null || _table.Columns <= 0 || _table.Rows <= 0)
            {
                Value = null;
            }
            else
            {
                SetSelection (0, 0, false);
            }

            RefreshContentSize ();
            Update ();
        }
    }

    /// <summary>
    ///     Navigator for cycling the selected row in the table by typing. Set to <see langword="null"/> to disable
    ///     type-to-search navigation; printable keys not otherwise bound will then bubble through normal key handling
    ///     instead of being consumed for incremental row navigation.
    /// </summary>
    public ICollectionNavigator? CollectionNavigator { get; set; }

    /// <summary>
    ///     The maximum number of characters to render in any given column.  This prevents one long column from pushing
    ///     out all the others
    /// </summary>
    public int MaxCellWidth { get; set; } = DEFAULT_MAX_CELL_WIDTH;

    /// <summary>The minimum number of characters to render in any given column.</summary>
    public int MinCellWidth { get; set; }

    /// <summary>The text representation that should be rendered for cells with the value <see cref="DBNull.Value"/></summary>
    public string NullSymbol { get; set; } = "-";

    /// <summary>
    ///     The symbol to add after each cell value and header value to visually separate values (if not using vertical
    ///     gridlines)
    /// </summary>
    public char SeparatorSymbol { get; set; } = ' ';

    private TableStyle _style = new ();

    /// <summary>Contains options for changing how the table is rendered</summary>
    public TableStyle Style
    {
        get => _style;
        set
        {
            _style = value;
            Update ();
        }
    }

    /// <summary>
    ///     Updates the view to reflect changes to <see cref="Table"/> and to (<see cref="ColumnOffset"/> /
    ///     <see cref="RowOffset"/>) etc.
    /// </summary>
    /// <remarks>This always calls <see cref="View.SetNeedsDraw()"/></remarks>
    public void Update ()
    {
        _columnsToRenderCache = null; // this will trigger a recalculation of the size and the columns when needed

        if (!IsInitialized || TableIsNullOrInvisible ())
        {
            SetNeedsDraw ();

            return;
        }

        EnsureValidScrollOffsets ();
        EnsureValidSelection ();
        EnsureCursorIsVisible ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Returns true if the given <paramref name="columnIndex"/> indexes a visible column otherwise false.  Returns
    ///     false for indexes that are out of bounds.
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <returns></returns>
    private bool IsColumnVisible (int columnIndex)
    {
        // if the column index provided is out of bounds
        if (_table is null || columnIndex < 0 || columnIndex >= _table.Columns)
        {
            return false;
        }

        return Style.GetColumnStyleIfAny (columnIndex)?.Visible ?? true;
    }

    /// <inheritdoc />
    protected override bool OnActivating (CommandEventArgs args)
    {
        // Cancel Activate when a mouse click lands outside any cell (e.g. below the
        // last row or in the header area). Without this, the Activating event would
        // fire even though no cell was actually selected, surprising subscribers.
        if (!TryGetMouseCellHit (args.Context, out Point? hit))
        {
            return false;
        }

        if (hit is null)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    protected override void OnActivated (ICommandContext? ctx)
    {
        if (ctx?.Binding is KeyBinding { Key: { } } keyBinding && keyBinding.Key == Key.Space)
        {
            ToggleExtend (ctx);

            return;
        }

        if (!TryGetMouseCellHit (ctx, out Point? hit) || hit is null)
        {
            return;
        }

        MouseBinding mouseBinding = (MouseBinding)ctx!.Binding!;
        SetSelection (hit.Value.X, hit.Value.Y, mouseBinding.MouseEvent!.Flags.FastHasFlags (MouseFlags.Shift));

        Update ();
    }

    // Returns true when the binding represents a left-click on this TableView; in that case
    // hit is the cell that was clicked, or null if the click was outside any cell (e.g. header,
    // below the last row, or right of the last rendered column). Returns false for non-mouse or
    // non-left-click bindings.
    private bool TryGetMouseCellHit (ICommandContext? ctx, out Point? hit)
    {
        hit = null;

        if (ctx?.Binding is not MouseBinding mouseBinding || mouseBinding.MouseEvent is null)
        {
            return false;
        }

        if (!mouseBinding.MouseEvent.Flags.FastHasFlags (MouseFlags.LeftButtonClicked))
        {
            return false;
        }

        int clientX = mouseBinding.MouseEvent.Position!.Value.X;
        int clientY = mouseBinding.MouseEvent.Position!.Value.Y;

        hit = ScreenToCell (clientX, clientY);

        // ScreenToCell maps far-right X positions onto the last column (its column lookup picks
        // the largest X with X <= clientX, with no upper-bound check). When ExpandLastColumn is
        // false there is visible whitespace to the right of the last rendered column; reject
        // hits that fall there so clicks in that whitespace don't raise Activating.
        if (hit is not null && !IsXWithinAnyRenderedColumn (clientX))
        {
            hit = null;
        }

        return true;
    }

    private bool IsXWithinAnyRenderedColumn (int clientX)
    {
        int viewportX = clientX + Viewport.X;

        foreach (ColumnToRender col in NonHiddenCellInfos ())
        {
            if (viewportX >= col.X && viewportX < col.X + col.Width)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key key)
    {
        if (TableIsNullOrInvisible ())
        {
            return false;
        }

        if (key == HotKey)
        {
            return CycleToNextTableEntryBeginningWith (key);
        }

        return false;
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key key)
    {
        if (key.AsRune is var rune && rune != default (Rune) && Rune.IsControl (rune))
        {
            return false;
        }

        if (key.IsAlt || key.IsCtrl)
        {
            // Never insert modified keys
            return false;
        }

        // Ignore other control characters.
        if (string.IsNullOrEmpty (key.AsGrapheme) && key is { IsKeyCodeAtoZ: false, KeyCode: < KeyCode.Space or > KeyCode.CharMask })
        {
            return false;
        }

        if (HasFocus && Table?.Rows != 0)
        {
            return CycleToNextTableEntryBeginningWith (key);
        }

        return true;
    }

    /// <summary>
    ///     Generates a new demo <see cref="DataTable"/> with the given number of <paramref name="cols"/> (min 5) and
    ///     <paramref name="rows"/>
    /// </summary>
    /// <param name="cols"></param>
    /// <param name="rows"></param>
    /// <returns></returns>
    public static DataTable BuildDemoDataTable (int cols, int rows)
    {
        var dt = new DataTable ();
        var explicitCols = 6;
        dt.Columns.Add (new DataColumn ("StrCol", typeof (string)));
        dt.Columns.Add (new DataColumn ("DateCol", typeof (DateTime)));
        dt.Columns.Add (new DataColumn ("IntCol", typeof (int)));
        dt.Columns.Add (new DataColumn ("DoubleCol", typeof (double)));
        dt.Columns.Add (new DataColumn ("NullsCol", typeof (string)));
        dt.Columns.Add (new DataColumn ("Unicode", typeof (string)));
        dt.Columns.Add (new DataColumn ("VarLength", typeof (string))); //ColIdx = 6

        for (var i = 0; i < cols - explicitCols; i++)
        {
            dt.Columns.Add ("Column" + (i + explicitCols));
        }

        var r = new Random (100);

        string numberText = NumberText (rows);

        for (var i = 0; i < rows; i++)
        {
            List<object> row =
            [
                $"Demo text in row {i}",
                new DateTime (2000 + i, 12, 25),
                r.Next (i),
                r.NextDouble () * i - 0.5 /*add some negatives to demo styles*/,
                DBNull.Value,
                "Les Mise" + char.ConvertFromUtf32 (int.Parse ("0301", NumberStyles.HexNumber)) + "rables",
                numberText [..i]
            ];

            for (var j = 0; j < cols - explicitCols; j++)
            {
                row.Add ("SomeValue" + r.Next (100));
            }

            dt.Rows.Add (row.ToArray ());
        }

        return dt;

        static string NumberText (int len)
        {
            var result = string.Empty;

            for (var i = 1; i <= len; i++)
            {
                result += i % 10;
            }

            return result;
        }
    }

    /// <inheritdoc/>
    string? IDesignable.GetDemoKeyStrokes () => "wait:500,CursorDown,wait:400,CursorRight,wait:400,CursorDown,wait:400,CursorRight,wait:800";

    bool IDesignable.EnableForDesign ()
    {
        DataTable dt = BuildDemoDataTable (5, 5);
        Table = new DataTableSource (dt);

        return true;
    }
}
