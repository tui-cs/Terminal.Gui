namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <summary>
    ///     Gets or sets the default key bindings for <see cref="ListView"/>. These are layered on top of
    ///     <see cref="View.DefaultKeyBindings"/> when the view is created.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Only single-command bindings are included here. Multi-command bindings (e.g., Shift+Space for
    ///         Activate+Down) and bindings with data payloads are added directly in the constructor.
    ///     </para>
    /// </remarks>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
    {
        // Emacs navigation
        [Command.Up] = Bind.All (Key.P.WithCtrl),
        [Command.Down] = Bind.All (Key.N.WithCtrl),
        [Command.PageDown] = Bind.All (Key.V.WithCtrl),

        // ListView adds Home/End as additional Start/End bindings (the base layer also provides Ctrl+Home/Ctrl+End)
        [Command.Start] = Bind.All (Key.Home),
        [Command.End] = Bind.All (Key.End),

        // Emacs extend
        [Command.UpExtend] = Bind.All (Key.P.WithCtrl.WithShift),
        [Command.DownExtend] = Bind.All (Key.N.WithCtrl.WithShift),

        // ListView uses Shift+Home/End (not Ctrl+Shift+Home/End like the base layer)
        [Command.StartExtend] = Bind.All (Key.Home.WithShift),
        [Command.EndExtend] = Bind.All (Key.End.WithShift),
    };

    private void SetupBindingsAndCommands ()
    {
        // Things this view knows how to do
        //
        AddCommand (Command.Up, ctx => RaiseActivating (ctx) == true || MoveUp ());
        AddCommand (Command.Down, ctx => RaiseActivating (ctx) == true || MoveDown ());

        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        AddCommand (Command.PageUp, () => MovePageUp ());
        AddCommand (Command.PageDown, () => MovePageDown ());
        AddCommand (Command.Start, () => MoveHome ());
        AddCommand (Command.End, () => MoveEnd ());
        AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));
        AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));

        // Extend commands for multi-selection
        AddCommand (Command.UpExtend, ctx => RaiseActivating (ctx) == true || MoveUp (true));
        AddCommand (Command.DownExtend, ctx => RaiseActivating (ctx) == true || MoveDown (true));
        AddCommand (Command.PageUpExtend, () => MovePageUp (true));
        AddCommand (Command.PageDownExtend, () => MovePageDown (true));
        AddCommand (Command.StartExtend, () => MoveHome (true));
        AddCommand (Command.EndExtend, () => MoveEnd (true));

        AddCommand (Command.SelectAll, HandleSelectAll);

        // Apply configurable key bindings (base layer + ListView-specific layer)
        ApplyKeyBindings (DefaultKeyBindings, View.DefaultKeyBindings);

        // Multi-command binding: activate then move down (can't be expressed in single-command dict)
        KeyBindings.Add (Key.Space.WithShift, Command.Activate, Command.Down);

        // Use the form of Add that lets us pass data with the binding.
        // Remove Ctrl+A first since ApplyKeyBindings may have already bound it (without data).
        KeyBindings.Remove (Key.A.WithCtrl);
        KeyBindings.Add (Key.A.WithCtrl, new KeyBinding ([Command.SelectAll], true));
        KeyBindings.Add (Key.U.WithCtrl, new KeyBinding ([Command.SelectAll], false));

        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonDoubleClicked, Command.Accept);

        // Mouse click bindings for selection and marking
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked, Command.Activate); // Normal click
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked | MouseFlags.Shift, Command.Activate); // Shift+Click

        MouseBindings.ReplaceCommands (MouseFlags.RightButtonClicked | MouseFlags.Ctrl,
                                       Command.Activate); // Alternative to Shift+Click for terminals like WT that us Shift-Click for selection
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked | MouseFlags.Ctrl, Command.Activate); // Ctrl+Click

        MouseBindings.ReplaceCommands (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledRight, Command.ScrollRight);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledLeft, Command.ScrollLeft);
    }

    /// <inheritdoc />
    protected override void OnHotKeyCommand (ICommandContext? ctx)
    {
        base.OnHotKeyCommand (ctx);

        SelectedItem ??= 0;
    }

    /// <inheritdoc />
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);

        // Handle keyboard (Space key) - mark item when marking is enabled
        if (ctx?.Binding is not MouseBinding { MouseEvent: { } mouse })
        {
            // Allow marking if: ShowMarks=true OR MarkMultiple=true
            // Only disallow if both are false (Combination 1: standard selection mode)
            if ((ShowMarks || MarkMultiple) && SelectedItem.HasValue)
            {
                MarkUnmarkSelectedItem ();
            }

            return;
        }

        // Handle mouse clicks
        Point position = mouse.Position!.Value;
        int row = position.Y;

        if (IsPinnedFooterRow (row))
        {
            return;
        }

        int headerRows = HeaderRowCount;

        if (row < headerRows)
        {
            return;
        }

        row -= headerRows;

        if (IsTrailingFooterRow (row))
        {
            return;
        }

        if (ItemAutoHeight)
        {
            PrepareItemHeights ();
        }

        int index = ItemAutoHeight ? ItemIndexAtRow (row) : Viewport.Y + row / Math.Max (ItemHeight, 1);

        if (Source is null || index >= Source.Count)
        {
            return;
        }
        bool shift = mouse.Flags.FastHasFlags (MouseFlags.Shift);
        bool ctrl = mouse.Flags.FastHasFlags (MouseFlags.Ctrl);
        bool leftButton = mouse.Flags.FastHasFlags (MouseFlags.LeftButtonClicked);
        bool rightButton = mouse.Flags.FastHasFlags (MouseFlags.RightButtonClicked);

        // Allow marking if: ShowMarks=true OR MarkMultiple=true
        bool allowMarking = ShowMarks || MarkMultiple;

        if (ctrl && leftButton && MarkMultiple && allowMarking)
        {
            // Ctrl+LeftClick: Toggle mark state directly
            Source.SetMark (index, !Source.IsMarked (index));

            // Update SelectedItem to clicked item
            SelectedItem = index;
            SetNeedsDraw ();
        }
        else if ((shift || (ctrl && rightButton)) && MarkMultiple && allowMarking)
        {
            // Shift+Click or Ctrl+RightClick: Extend marking from anchor
            SetSelection (index, true);
        }
        else
        {
            // Normal click: Select item (SetSelection handles marking in radio button mode)
            SetSelection (index, false);

            // In checkbox/hidden marks mode, toggle the mark for the clicked item
            // In radio button mode, SetSelection already marked it, so skip MarkUnmarkSelectedItem
            // Mark item only on Clicked (not Pressed) to avoid double-toggle
            if (allowMarking && MarkMultiple && mouse.Flags.FastHasFlags (MouseFlags.LeftButtonClicked))
            {
                MarkUnmarkSelectedItem ();
            }
        }
    }
}
