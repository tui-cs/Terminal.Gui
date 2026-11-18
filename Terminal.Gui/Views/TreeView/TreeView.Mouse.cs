// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls
// by phillip.piper@gmail.com). Phillip has explicitly granted permission for his design
// and code to be used in this library under the MIT license.

namespace Terminal.Gui.Views;

public partial class TreeView<T>
{
    ///<inheritdoc/>
    protected override bool OnMouseEvent (Mouse me)
    {
        // If it is not an event we care about
        if (me is not { IsWheel: true })
        {
            // do nothing
            return false;
        }

        //if (!HasFocus && CanFocus)
        //{
        //    SetFocus ();
        //}

        if (me.Flags == MouseFlags.WheeledDown)
        {
            ScrollDown ();

            return true;
        }

        if (me.Flags == MouseFlags.WheeledUp)
        {
            ScrollUp ();

            return true;
        }

        if (me.Flags == MouseFlags.WheeledRight)
        {
            ScrollOffsetHorizontal++;
            SetNeedsDraw ();

            return true;
        }

        if (me.Flags == MouseFlags.WheeledLeft)
        {
            ScrollOffsetHorizontal--;
            SetNeedsDraw ();

            return true;
        }

        return false;
    }

    /// <summary>Returns the branch at the given <paramref name="y"/> client coordinate e.g. following a click event.</summary>
    /// <param name="y">Client Y position in the controls bounds.</param>
    /// <returns>The clicked branch or null if outside of tree region.</returns>
    private Branch<T>? HitTest (int y)
    {
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        int idx = y + ScrollOffsetVertical;

        // click is outside any visible nodes
        if (idx < 0 || idx >= map.Count)
        {
            return null;
        }

        // The line they clicked on
        return map.ElementAt (idx);
    }
}
