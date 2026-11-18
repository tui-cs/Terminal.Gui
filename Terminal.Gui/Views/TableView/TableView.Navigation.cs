using System.Data;
using System.Globalization;

namespace Terminal.Gui.Views;

public partial class TableView
{
    /// <summary>The default minimum cell width for <see cref="ColumnStyle.MinAcceptableWidth"/></summary>
    public const int DEFAULT_MIN_ACCEPTABLE_WIDTH = 100;

    private bool? HandleRight (ICommandContext? ctx)
    {
        int oldCursorCol = _cursorColumn;
        int oldViewportX = Viewport.X;
        bool result = MoveCursorByOffsetWithReturn (1, 0, ctx);

        if (oldCursorCol != _cursorColumn || Viewport.X >= MaxViewPort ().X)
        {
            return result;
        }
        Point maxViewPort = MaxViewPort ();
        Viewport = Viewport with { X = Math.Min (oldViewportX + 1, maxViewPort.X) };

        return result;
    }

    private bool? HandleUp (ICommandContext? ctx)
    {
        if (_cursorRow != 0)
        {
            return MoveCursorByOffsetWithReturn (0, -1, ctx);
        }

        if (Viewport.Y <= 0)
        {
            return false;
        }
        Viewport = Viewport with { Y = Viewport.Y - 1 };

        return true;
    }

    private bool? HandleDown (ICommandContext? ctx)
    {
        if (Table == null || _cursorRow < Table.Rows - 1)
        {
            return MoveCursorByOffsetWithReturn (0, 1, ctx);
        }

        if (Viewport.Y >= GetContentHeight () - Viewport.Height)
        {
            return false;
        }
        Viewport = Viewport with { Y = Viewport.Y + 1 };

        return true;
    }

    /// <summary>Moves the cursor down by one page.</summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    /// <param name="ctx">The command context</param>
    public bool PageDown (bool extend, ICommandContext? ctx)
    {
        int oldCursorRow = _cursorRow;
        MoveCursorByOffset (0, Viewport.Height /* - CurrentHeaderHeightVisible ()*/, extend, ctx);

        //after scrolling the cells, also scroll to lower line
        int remainingJump = Viewport.Height - (_cursorRow - oldCursorRow);
        Point maxViewPort = MaxViewPort ();

        if (remainingJump > 0 && Viewport.Y < maxViewPort.Y)
        {
            Viewport = Viewport with { Y = Math.Min (Viewport.Y + remainingJump, maxViewPort.Y) };
        }

        Update ();

        return true;
    }

    /// <summary>Moves the cursor up by one page.</summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    /// <param name="ctx">The command context</param>
    public bool PageUp (bool extend, ICommandContext? ctx)
    {
        int oldCursorRow = _cursorRow;
        MoveCursorByOffset (0, -Viewport.Height /* - CurrentHeaderHeightVisible ()*/, extend, ctx);

        //after scrolling the cells, also scroll to header
        int remainingJump = Viewport.Height - (oldCursorRow - _cursorRow);

        if (remainingJump > 0 && Viewport.Y > 0)
        {
            Viewport = Viewport with { Y = Math.Max (Viewport.Y - remainingJump, 0) };
        }

        Update ();

        return true;
    }

    private bool CycleToNextTableEntryBeginningWith (Key key)
    {
        // Type-to-search disabled
        if (CollectionNavigator is null)
        {
            return false;
        }
        int row = _cursorRow;

        // There is a multi select going on and not just for the current row
        if (GetAllSelectedCells ().Any (c => c.Y != row))
        {
            return false;
        }

        // Pass null when there is no valid selection (row < 0), so the navigator starts from the beginning
        int? rowForNavigator = row < 0 ? null : row;
        int? match = CollectionNavigator.GetNextMatchingItem (rowForNavigator, (char)key);

        if (match == null)
        {
            return false;
        }

        _cursorRow = match.Value;
        CommitSelectionState ();
        EnsureValidSelection ();
        EnsureCursorIsVisible ();
        SetNeedsDraw ();

        return true;
    }

    /// <summary>
    ///     Returns true if the <see cref="Table"/> is not set or all the columns in the <see cref="Table"/> have an
    ///     explicit <see cref="ColumnStyle"/> that marks them <see cref="ColumnStyle.Visible"/> <see langword="false"/>.
    /// </summary>
    /// <returns></returns>
    private bool TableIsNullOrInvisible () =>
        Table is not { Columns: > 0 } || Enumerable.Range (0, Table.Columns).All (c => Style.GetColumnStyleIfAny (c)?.Visible is false);
}
