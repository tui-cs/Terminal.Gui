// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls
// by phillip.piper@gmail.com). Phillip has explicitly granted permission for his design
// and code to be used in this library under the MIT license.

namespace Terminal.Gui.Views;

public partial class TreeView<T>
{
    /// <summary>
    ///     Called once for each visible row during rendering.  Can be used to make last minute changes to color or text
    ///     rendered
    /// </summary>

    // TODO: Refactor to use CWP
    public event EventHandler<DrawTreeViewLineEventArgs<T>>? DrawLine;

    ///<inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (Roots is null)
        {
            return true;
        }

        if (TreeBuilder is null)
        {
            Move (0, 0);
            AddStr (NO_BUILDER_ERROR);

            return true;
        }

        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        for (var line = 0; line < Viewport.Height; line++)
        {
            int idxToRender = ScrollOffsetVertical + line;

            // Is there part of the tree view to render?
            if (idxToRender < map.Count)
            {
                // Render the line
                map.ElementAt (idxToRender).Draw (line, Viewport.Width);
            }
            else
            {
                // Else clear the line to prevent stale symbols due to scrolling etc
                Move (0, line);
                SetAttribute (GetAttributeForRole (VisualRole.Normal));
                AddStr (new string (' ', Viewport.Width));
            }
        }

        return true;
    }

    ///<inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? currentFocused, View? newFocused)
    {
        if (!newHasFocus)
        {
            return;
        }

        // If there is no selected object and there are objects in the tree, select the first one
        if (SelectedObject is null && Objects is { } && Objects.Any ())
        {
            SelectedObject = Objects.First ();
        }
    }

    /// <summary>Returns the maximum width line in the tree including prefix and expansion symbols.</summary>
    /// <param name="visible">
    ///     True to consider only rows currently visible (based on window bounds and
    ///     <see cref="ScrollOffsetVertical"/>). False to calculate the width of every exposed branch in the tree.
    /// </param>
    /// <returns></returns>
    public int GetContentWidth (bool visible)
    {
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        if (map.Count == 0)
        {
            return 0;
        }

        if (!visible)
        {
            return map.Max (b => b.GetWidth ());
        }

        //Somehow we managed to scroll off the end of the control
        if (ScrollOffsetVertical >= map.Count)
        {
            return 0;
        }

        // If control has no height to it then there is no visible area for content
        if (Viewport.Height == 0)
        {
            return 0;
        }

        return map.Skip (ScrollOffsetVertical).Take (Viewport.Height).Max (b => b.GetWidth ());
    }

    /// <summary>
    ///     Returns the object in the tree list that is currently visible at the provided row. Returns null if no object
    ///     is at that location.
    /// </summary>
    /// <remarks>
    ///     If you have screen coordinates then use <see cref="View.ScreenToFrame"/> to translate these into the client area of
    ///     the <see cref="TreeView{T}"/>.
    /// </remarks>
    /// <param name="row">The row of the <see cref="View.Viewport"/> of the <see cref="TreeView{T}"/>.</param>
    /// <returns>The object currently displayed on this row or null.</returns>
    public T? GetObjectOnRow (int row) => HitTest (row)?.Model;

    /// <summary>
    ///     <para>
    ///         Returns the Y coordinate within the <see cref="View.Viewport"/> of the tree at which <paramref name="toFind"/>
    ///         would be displayed or null if it is not currently exposed (e.g. its parent is collapsed).
    ///     </para>
    ///     <para>
    ///         Note that the returned value can be negative if the TreeView is scrolled down and the
    ///         <paramref name="toFind"/> object is off the top of the view.
    ///     </para>
    /// </summary>
    /// <param name="toFind"></param>
    /// <returns></returns>
    public int? GetObjectRow (T toFind)
    {
        int idx = BuildLineMap ().IndexOf (o => o.Model.Equals (toFind));

        if (idx == -1)
        {
            return null;
        }

        return idx - ScrollOffsetVertical;
    }

    /// <summary>Raises the DrawLine event</summary>
    /// <param name="e"></param>

    // TODO: Refactor to use CWP
    internal void OnDrawLine (DrawTreeViewLineEventArgs<T> e) => DrawLine?.Invoke (this, e);

    private void UpdateCursor ()
    {
        if (!CanFocus || !HasFocus || !Visible || SelectedObject is null || !Cursor.IsVisible)
        {
            return;
        }

        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();
        int idx = map.IndexOf (b => b.Model.Equals (SelectedObject));

        // if currently selected line is visible
        if (idx - ScrollOffsetVertical < 0 || idx - ScrollOffsetVertical >= Viewport.Height)
        {
            return;
        }

        Branch<T> branch = map.ElementAt (idx);
        int indent = branch.Depth + 2 + (branch.Parent?.Depth ?? 1);
        Cursor = Cursor with { Position = ViewportToScreen (new Point (indent - ScrollOffsetHorizontal, idx - ScrollOffsetVertical)) };
    }
}
