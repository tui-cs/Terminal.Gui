namespace Terminal.Gui.Views;

/// <summary>
///     <see cref="ITableSource"/> for a <see cref="TableView"/> which adds a checkbox column as an additional column
///     in the table.
/// </summary>
/// <remarks>
///     This class wraps another <see cref="ITableSource"/> and dynamically serves its rows/cols plus an extra column.
///     Data in the wrapped source can be dynamic (change over time).
/// </remarks>
public abstract class CheckBoxTableSourceWrapperBase : ITableSource
{
    private readonly TableView _tableView;

    /// <summary>
    ///     Creates a new instance of the class presenting the data in <paramref name="toWrap"/> plus an additional
    ///     checkbox column.
    /// </summary>
    /// <param name="tableView">
    ///     The <see cref="TableView"/> this source will be used with. This is required for event
    ///     registration.
    /// </param>
    /// <param name="toWrap">The original data source of the <see cref="TableView"/> that you want to add checkboxes to.</param>
    protected CheckBoxTableSourceWrapperBase (TableView tableView, ITableSource toWrap)
    {
        Wrapping = toWrap;
        _tableView = tableView;

        _tableView.KeyBindings.ReplaceCommands (Key.Space, Command.ToggleExtend);

        // Intercept ToggleExtend before it reaches the default handler
        _tableView.KeyDown += HandleSpaceKeyDown;
        _tableView.Activating += TableView_Activating;
    }

    /// <summary>
    ///     Gets or sets the character to use for checked entries. Defaults to <see cref="Glyphs.CheckStateChecked"/>
    /// </summary>
    public Rune CheckedRune { get; set; } = Glyphs.CheckStateChecked;

    /// <summary>
    ///     Gets or sets the character to use for checked entry when <see cref="UseRadioButtons"/> is true. Defaults to
    ///     <see cref="Glyphs.Selected"/>
    /// </summary>
    public Rune RadioCheckedRune { get; set; } = Glyphs.Selected;

    /// <summary>
    ///     Gets or sets the character to use for unchecked entries when <see cref="UseRadioButtons"/> is true. Defaults
    ///     to <see cref="Glyphs.UnSelected"/>
    /// </summary>
    public Rune RadioUnCheckedRune { get; set; } = Glyphs.UnSelected;

    /// <summary>
    ///     Gets or sets the character to use for UnChecked entries. Defaults to <see cref="Glyphs.CheckStateUnChecked"/>
    /// </summary>
    public Rune UnCheckedRune { get; set; } = Glyphs.CheckStateUnChecked;

    /// <summary>Gets or sets whether to only allow a single row to be toggled at once (Radio button).</summary>
    public bool UseRadioButtons { get; set; }

    /// <summary>Gets the <see cref="ITableSource"/> that this instance is wrapping.</summary>
    public ITableSource Wrapping { get; }

    /// <inheritdoc/>
    public object this [int row, int col]
    {
        get
        {
            if (col != 0)
            {
                return Wrapping [row, col - 1];
            }

            if (UseRadioButtons)
            {
                return IsChecked (row) ? RadioCheckedRune : RadioUnCheckedRune;
            }

            return IsChecked (row) ? CheckedRune : UnCheckedRune;
        }
    }

    /// <inheritdoc/>
    public int Rows => Wrapping.Rows;

    /// <inheritdoc/>
    public int Columns => Wrapping.Columns + 1;

    /// <inheritdoc/>
    public string [] ColumnNames
    {
        get
        {
            List<string> toReturn = Wrapping.ColumnNames.ToList ();
            toReturn.Insert (0, " ");

            return toReturn.ToArray ();
        }
    }

    /// <summary>Clears the toggled state of all rows.</summary>
    protected abstract void ClearAllToggles ();

    /// <summary>Returns true if <paramref name="row"/> is checked.</summary>
    /// <param name="row"></param>
    /// <returns></returns>
    protected abstract bool IsChecked (int row);

    /// <summary>
    ///     Called when the 'toggled all' action is performed. This should change state from 'some selected' to 'all
    ///     selected' or clear selection if all area already selected.
    /// </summary>
    protected abstract void ToggleAllRows ();

    /// <summary>Flips the checked state of the given <paramref name="row"/>.</summary>
    /// <param name="row"></param>
    protected abstract void ToggleRow (int row);

    /// <summary>
    ///     Flips the checked state for a collection of rows. If some (but not all) are selected they should flip to all
    ///     selected.
    /// </summary>
    /// <param name="range"></param>
    protected abstract void ToggleRows (int [] range);

    private void HandleSpaceKeyDown (object? sender, Key e)
    {
        if (e != Key.Space)
        {
            return;
        }

        int [] range = _tableView.GetAllSelectedCells ().Select (c => c.Y).Distinct ().ToArray ();

        if (UseRadioButtons)
        {
            if (range.Length != 1)
            {
                e.Handled = true;

                return;
            }

            ClearAllToggles ();
            ToggleRow (range.Single ());
        }
        else
        {
            ToggleRows (range);
        }

        e.Handled = true;
        _tableView.SetNeedsDraw ();
    }

    private void TableView_Activating (object? sender, CommandEventArgs e)
    {
        // Only handle mouse clicks, not keyboard selections
        if (e.Context?.Binding is not MouseBinding { MouseEvent: { } mouse })
        {
            return;
        }

        // we only care about clicks (not movements)
        if (!mouse.Flags.FastHasFlags (MouseFlags.LeftButtonClicked))
        {
            return;
        }

        Point? hit = _tableView.ScreenToCell (mouse.Position!.Value.X, mouse.Position!.Value.Y, out int? headerIfAny);

        if (headerIfAny is 0)
        {
            // clicking in header with radio buttons does nothing
            if (UseRadioButtons)
            {
                return;
            }

            // otherwise it ticks all rows
            ToggleAllRows ();
            e.Handled = true;
            _tableView.SetNeedsDraw ();
        }
        else if (hit is { X: 0 })
        {
            if (UseRadioButtons)
            {
                ClearAllToggles ();
                ToggleRow (hit.Value.Y);
            }
            else
            {
                ToggleRow (hit.Value.Y);
            }

            e.Handled = true;
            _tableView.SetNeedsDraw ();
        }
    }
}
