namespace Terminal.Gui.Views;

public partial class Markdown
{
    /// <summary>A contiguous link span on a single rendered line, built during layout.</summary>
    internal sealed class MarkdownLinkRegion
    {
        public int Line { get; init; }
        public int StartX { get; init; }
        public int EndXExclusive { get; set; }
        public string Url { get; init; } = "";
    }

    private void SetupBindingsAndCommands ()
    {
        // Navigation commands — keys are bound via ApplyKeyBindings from DefaultKeyBindings
        AddCommand (Command.Up, () => ScrollVertical (-1));
        AddCommand (Command.Down, () => ScrollVertical (1));
        AddCommand (Command.Left, () => ScrollHorizontal (-1));
        AddCommand (Command.Right, () => ScrollHorizontal (1));
        AddCommand (Command.PageUp, () => ScrollVertical (-Math.Max (Viewport.Height - 1, 1)));
        AddCommand (Command.PageDown, () => ScrollVertical (Math.Max (Viewport.Height - 1, 1)));
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));
        AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));

        AddCommand (Command.Start,
                    () =>
                    {
                        Viewport = Viewport with { Y = 0 };

                        return true;
                    });

        // Home (without Ctrl) also scrolls to top — this is a read-only view with no cursor
        AddCommand (Command.LeftStart,
                    () =>
                    {
                        Viewport = Viewport with { Y = 0 };

                        return true;
                    });

        AddCommand (Command.End,
                    () =>
                    {
                        Viewport = Viewport with { Y = Math.Max (GetContentHeight () - Viewport.Height, 0) };

                        return true;
                    });

        // End (without Ctrl) also scrolls to bottom — this is a read-only view with no cursor
        AddCommand (Command.RightEnd,
                    () =>
                    {
                        Viewport = Viewport with { Y = Math.Max (GetContentHeight () - Viewport.Height, 0) };

                        return true;
                    });

        AddCommand (Command.Accept, () => ActivateCurrentLink ());

        // Selection and clipboard commands
        AddCommand (Command.SelectAll, () => SelectAll ());
        AddCommand (Command.Copy, () => Copy ());
        AddCommand (Command.Context, () => ShowContextMenu ());

        // Apply default key bindings (maps CursorUp→Up, CursorDown→Down, etc.)
        ApplyKeyBindings (DefaultKeyBindings, DefaultKeyBindings);

        // Mouse wheel and click bindings
        MouseBindings.ReplaceCommands (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledRight, Command.ScrollRight);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledLeft, Command.ScrollLeft);

        // The base class binds LeftButtonReleased → Activate; remove that so Activate
        // fires only on LeftButtonClicked (not twice per click which would clear selection).
        // Also remove the base class Ctrl+LeftButtonReleased → Context binding so that
        // Ctrl+Click can follow links without triggering the context menu popover.
        MouseBindings.Remove (MouseFlags.LeftButtonReleased);
        MouseBindings.Remove (MouseFlags.LeftButtonReleased | MouseFlags.Ctrl);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked, Command.Activate);

        // Right-click is handled directly in OnMouseEvent so that the view can be focused
        // and the context menu created before trying to show it, even when not yet focused.

        // Press anchors the drag-selection; drag extends it — both routed through OnActivated.
        MouseBindings.Add (MouseFlags.LeftButtonPressed, Command.Activate);
        MouseBindings.Add (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, Command.Activate);
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        // Right-click: focus the view first (which creates ContextMenu) then show the menu at
        // the click's screen position. Handled here rather than via a Command binding so that
        // focus and menu creation are guaranteed even when the view is not yet focused.
        if (mouse.Flags.FastHasFlags (MouseFlags.RightButtonClicked))
        {
            if (!HasFocus && CanFocus)
            {
                SetFocus ();
            }

            ShowContextMenu (mouse.ScreenPosition);

            return true;
        }

        if (!mouse.Flags.FastHasFlags (MouseFlags.LeftButtonReleased))
        {
            return false;
        }

        App?.Mouse.UngrabMouse ();

        return false;
    }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        if (newHasFocus)
        {
            CreateContextMenu ();

            if (ContextMenu?.Key is { })
            {
                KeyBindings.Add (ContextMenu.Key, Command.Context);
            }
        }
        else
        {
            if (ContextMenu?.Key is { })
            {
                KeyBindings.Remove (ContextMenu.Key);
            }

            DisposeContextMenu ();
            _activeLinkIndex = -1;
            SetNeedsDraw ();
        }

        base.OnHasFocusChanged (newHasFocus, previousFocusedView, focusedView);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Cycles through link regions on Tab / Shift+Tab. Returns <see langword="false"/>
    ///     when there are no more links in that direction, allowing focus to leave the view.
    /// </remarks>
    protected override bool OnAdvancingFocus (NavigationDirection direction, TabBehavior? behavior)
    {
        if (Viewport.Width < MIN_WRAP_WIDTH)
        {
            return false;
        }

        // Cancel auto-advance (behavior==null, used by SetHasFocusTrue) so that gaining
        // focus doesn't automatically drill into a table SubView or link region.
        // Only explicit Tab navigation (behavior==TabStop) should cycle through links.
        if (behavior is null)
        {
            return true;
        }

        if (behavior != TabStop)
        {
            return false;
        }

        // Do NOT do layout here — SubView Add/Remove re-enters focus navigation.
        // _linkRegions is populated during OnSubViewLayout and is safe to read here.

        if (_linkRegions.Count == 0)
        {
            return false;
        }

        int delta = direction == NavigationDirection.Forward ? 1 : -1;

        if (_activeLinkIndex < 0)
        {
            // First entry — select first or last link
            _activeLinkIndex = delta > 0 ? 0 : _linkRegions.Count - 1;
            ScrollToLinkRegion (_linkRegions [_activeLinkIndex]);
            SetNeedsDraw ();

            return true;
        }

        int next = _activeLinkIndex + delta;

        // If we've gone past either end, clear selection and let focus leave
        if (next < 0 || next >= _linkRegions.Count)
        {
            _activeLinkIndex = -1;
            SetNeedsDraw ();

            return false;
        }

        _activeLinkIndex = next;
        ScrollToLinkRegion (_linkRegions [_activeLinkIndex]);
        SetNeedsDraw ();

        return true;
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        // Only process mouse input — keyboard activation is handled via Command.Accept
        if (ctx?.Binding is not MouseBinding { MouseEvent: { } mouse, MouseEvent.Position: { } pos })
        {
            return;
        }

        // Button-down: anchor the drag-selection start
        if (mouse.Flags.FastHasFlags (MouseFlags.LeftButtonPressed) && !mouse.Flags.HasFlag (MouseFlags.PositionReport))
        {
            int contentX = Viewport.X + pos.X;
            int contentY = Math.Min (Viewport.Y + pos.Y, Math.Max (_renderedLines.Count - 1, 0));
            _selectionAnchor = new Point (contentX, contentY);
            _selectionCurrent = _selectionAnchor;
            _isDragging = false;

            if (App is { } && !App.Mouse.IsGrabbed (this))
            {
                App.Mouse.GrabMouse (this);
            }

            if (!HasFocus && CanFocus)
            {
                SetFocus ();
            }

            return;
        }

        // Drag: extend selection and auto-scroll when the pointer leaves the viewport.
        if (mouse.Flags.FastHasFlags (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport))
        {
            // Auto-scroll: if the pointer has left the top or bottom edge, scroll one line
            // in that direction so the user can extend the selection beyond the visible area.
            if (pos.Y < 0)
            {
                ScrollVertical (-1);
            }
            else if (pos.Y >= Viewport.Height)
            {
                ScrollVertical (1);
            }

            // Clamp both axes to the actual content bounds to prevent negative indices or
            // indices beyond the last rendered line (possible when the mouse is grabbed and
            // moves outside the view's frame).
            int maxLine = Math.Max (_renderedLines.Count - 1, 0);
            int contentX = Math.Max (Viewport.X + pos.X, 0);
            int contentY = Math.Clamp (Viewport.Y + pos.Y, 0, maxLine);
            _selectionCurrent = new Point (contentX, contentY);
            _isDragging = true;
            _isSelecting = true;
            SetNeedsDraw ();

            return;
        }

        // LeftButtonClicked: a drag ended — the click fires after release, but the user was
        // selecting text, so don't activate a link.
        if (_isDragging)
        {
            _isDragging = false;

            return;
        }

        // Plain click clears any existing text selection.
        ClearSelection ();

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        int clickX = Viewport.X + pos.X;
        int clickY = Viewport.Y + pos.Y;

        for (var i = 0; i < _linkRegions.Count; i++)
        {
            MarkdownLinkRegion region = _linkRegions [i];

            if (region.Line != clickY)
            {
                continue;
            }

            if (clickX < region.StartX || clickX >= region.EndXExclusive)
            {
                continue;
            }

            _activeLinkIndex = i;
            ActivateLink (region);

            return;
        }
    }

    /// <summary>
    ///     Returns the URL of the link region at content coordinates (<paramref name="contentX"/>,
    ///     <paramref name="contentY"/>), or <see langword="null"/> if no link covers that position.
    /// </summary>
    private string? FindLinkUrlAt (int contentX, int contentY)
    {
        if (contentY >= 0 && contentY < _renderedLines.Count && _renderedLines [contentY].TableView is { } table)
        {
            RenderedLine line = _renderedLines [contentY];
            int link = table.HitTestLinkIndex (contentX - table.Frame.X - table.Viewport.X, line.TableRow - table.Viewport.Y);

            return link >= 0 ? table.TableLayout.Links [link].Url : null;
        }

        foreach (MarkdownLinkRegion region in _linkRegions)
        {
            if (region.Line != contentY)
            {
                continue;
            }

            if (contentX >= region.StartX && contentX < region.EndXExclusive)
            {
                return region.Url;
            }
        }

        return null;
    }

    /// <summary>
    ///     Builds the deduplicated list of link regions by scanning rendered lines.
    ///     Called at the end of <see cref="BuildRenderedLines"/>.
    /// </summary>
    private void BuildLinkRegions ()
    {
        _linkRegions.Clear ();
        _activeLinkIndex = -1;

        for (var lineIdx = 0; lineIdx < _renderedLines.Count; lineIdx++)
        {
            RenderedLine line = _renderedLines [lineIdx];
            var x = 0;
            string? currentUrl = null;
            MarkdownLinkRegion? currentRegion = null;

            foreach (StyledSegment segment in line.Segments)
            {
                int segWidth = segment.Text.GetColumns ();

                if (!string.IsNullOrWhiteSpace (segment.Url))
                {
                    if (currentUrl == segment.Url && currentRegion is { })
                    {
                        currentRegion.EndXExclusive = x + segWidth;
                    }
                    else
                    {
                        currentRegion = new MarkdownLinkRegion { Line = lineIdx, StartX = x, EndXExclusive = x + segWidth, Url = segment.Url! };

                        _linkRegions.Add (currentRegion);
                        currentUrl = segment.Url;
                    }
                }
                else
                {
                    currentUrl = null;
                    currentRegion = null;
                }

                x += segWidth;
            }
        }
    }

    /// <summary>Activates the currently highlighted link (Enter key).</summary>
    private bool ActivateCurrentLink ()
    {
        if (_activeLinkIndex < 0 || _activeLinkIndex >= _linkRegions.Count)
        {
            return false;
        }

        ActivateLink (_linkRegions [_activeLinkIndex]);

        return true;
    }

    /// <summary>Activates a link region: scrolls for anchors, opens URL otherwise.</summary>
    private void ActivateLink (MarkdownLinkRegion region)
    {
        if (region.Url.StartsWith ('#'))
        {
            ScrollToAnchor (region.Url);
            RaiseLinkClicked (region.Url);

            return;
        }

        bool handled = RaiseLinkClicked (region.Url);

        if (!handled)
        {
            Link.OpenUrl (region.Url);
        }
    }

    /// <summary>Scrolls the viewport so that the given link region is visible.</summary>
    private void ScrollToLinkRegion (MarkdownLinkRegion region)
    {
        int lineY = region.Line;

        if (lineY < Viewport.Y)
        {
            Viewport = Viewport with { Y = lineY };
        }
        else if (lineY >= Viewport.Y + Viewport.Height)
        {
            Viewport = Viewport with { Y = lineY - Viewport.Height + 1 };
        }
    }

    /// <summary>
    ///     Returns <see langword="true"/> if the segment at position (<paramref name="contentX"/>,
    ///     <paramref name="lineIdx"/>) belongs to the currently active (focused) link.
    /// </summary>
    internal bool IsActiveLinkAt (int lineIdx, int contentX)
    {
        if (_activeLinkIndex < 0 || _activeLinkIndex >= _linkRegions.Count)
        {
            return false;
        }

        MarkdownLinkRegion active = _linkRegions [_activeLinkIndex];

        return active.Line == lineIdx && contentX >= active.StartX && contentX < active.EndXExclusive;
    }
}
