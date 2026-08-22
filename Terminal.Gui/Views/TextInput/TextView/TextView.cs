using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>Fully featured multi-line text editor.</summary>
/// <remarks>
/// <img src="../images/views/TextView.gif" alt="TextView demo"/>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Left, Ctrl+B</term> <description>Moves the editing point left.</description>
///         </item>
///         <item>
///             <term>Right, Ctrl+F</term> <description>Moves the editing point right.</description>
///         </item>
///         <item>
///             <term>Up, Ctrl+P</term> <description>Moves the editing point one line up.</description>
///         </item>
///         <item>
///             <term>Down, Ctrl+N</term> <description>Moves the editing point one line down.</description>
///         </item>
///         <item>
///             <term>Home</term> <description>Moves the cursor to the beginning of the line.</description>
///         </item>
///         <item>
///             <term>End, Ctrl+E</term> <description>Moves the cursor to the end of the line.</description>
///         </item>
///         <item>
///             <term>Ctrl+Home</term> <description>Moves to the first line and first column.</description>
///         </item>
///         <item>
///             <term>Ctrl+End</term> <description>Moves to the last line and last column.</description>
///         </item>
///         <item>
///             <term>Ctrl+Left</term> <description>Moves one word left.</description>
///         </item>
///         <item>
///             <term>Ctrl+Right</term> <description>Moves one word right.</description>
///         </item>
///         <item>
///             <term>PageUp / PageDown</term> <description>Moves one page up or down.</description>
///         </item>
///         <item>
///             <term>Shift+&lt;movement&gt;</term>
///             <description>Extends the selection in the given direction.</description>
///         </item>
///         <item>
///             <term>Delete, Ctrl+D</term> <description>Deletes the character in front of the cursor.</description>
///         </item>
///         <item>
///             <term>Backspace</term> <description>Deletes the character behind the cursor.</description>
///         </item>
///         <item>
///             <term>Ctrl+K</term>
///             <description>Cuts text from the cursor to the end of the line (kill-to-end).</description>
///         </item>
///         <item>
///             <term>Ctrl+Shift+Backspace</term>
///             <description>Cuts text from the cursor to the start of the line (kill-to-start).</description>
///         </item>
///         <item>
///             <term>Ctrl+Delete</term> <description>Deletes the word to the right of the cursor.</description>
///         </item>
///         <item>
///             <term>Ctrl+Backspace</term> <description>Deletes the word to the left of the cursor.</description>
///         </item>
///         <item>
///             <term>Ctrl+C</term> <description>Copies the selected text to the clipboard.</description>
///         </item>
///         <item>
///             <term>Ctrl+X, Ctrl+W</term> <description>Cuts the selected text to the clipboard.</description>
///         </item>
///         <item>
///             <term>Ctrl+V</term> <description>Pastes text from the clipboard.</description>
///         </item>
///         <item>
///             <term>Ctrl+A</term> <description>Selects all text.</description>
///         </item>
///         <item>
///             <term>Ctrl+Shift+Delete</term> <description>Deletes all text.</description>
///         </item>
///         <item>
///             <term>Ctrl+Z</term> <description>Undoes the last change.</description>
///         </item>
///         <item>
///             <term>Ctrl+Y</term> <description>Redoes the last undone change.</description>
///         </item>
///         <item>
///             <term>Ctrl+Space</term> <description>Toggles selection mode.</description>
///         </item>
///         <item>
///             <term>Insert</term> <description>Toggles overwrite mode.</description>
///         </item>
///     </list>
/// </remarks>
[Obsolete ("TextView is superseded by tui-cs/Editor's EditorView, which provides a rope-backed document model, "
           + "cell-aware rendering, multi-caret editing, undo, syntax highlighting, folding, find/replace, and soft wrap. "
           + "See https://github.com/tui-cs/Editor for details.", error: false)]
public partial class TextView : View, IDesignable
{
    /// <summary>
    ///     Gets or sets the default cursor style.
    /// </summary>
    public static CursorStyle DefaultCursorStyle => TextViewSettings.Current.DefaultCursorStyle;

    private CultureInfo? _currentCulture;

    //private Dim? _savedHeight;

    /// <summary>
    ///     Initializes a <see cref="TextView"/> on the specified area, with dimensions controlled with the X, Y, Width
    ///     and Height properties.
    /// </summary>
    public TextView ()
    {
        CanFocus = true;
        Used = true;

        ViewportSettings |= ViewportSettingsFlags.AllowLocationPlusSizeGreaterThanContentSize;

        // By default, disable hotkeys (in case someone sets Title)
        base.HotKeySpecifier = new Rune ('\xffff');

        _model.LinesLoaded += Model_LinesLoaded!;
        _historyText.ChangeText += HistoryText_ChangeText;

        // Initialize the model with at least one empty line
        _model.LoadString (string.Empty);
        _historyText.Clear (_model.GetAllLines ());

        CreateCommandsAndBindings ();

        _currentCulture = Thread.CurrentThread.CurrentUICulture;

        Cursor = new Cursor { Style = DefaultCursorStyle };
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        Autocomplete.HostControl ??= this;

        ContextMenu = CreateContextMenu ();

        UpdateScrollBars ();
        UpdateContentSize ();
        PositionCursor ();

        if (!HasFocus)
        {
            return;
        }
        App?.Popovers?.Register (ContextMenu);

        if (ContextMenu?.Key is { } key && !KeyBindings.TryGet (key, out _))
        {
            KeyBindings.Add (key, Command.Context);
        }
    }

    /// <inheritdoc/>
    protected override void OnSubViewsLaidOut (LayoutEventArgs args)
    {
        base.OnSubViewsLaidOut (args);

        // Only rewrap when viewport width actually changes to avoid O(N×L) rescans on every layout pass.
        if (_wordWrap && Viewport.Width != _lastWrapWidth)
        {
            WrapTextModel ();
            _lastWrapWidth = Viewport.Width;
        }

        // Don't call AdjustViewport() here - it resets viewport to cursor position,
        // undoing any user scrolling via scrollbar. AdjustViewport() is called when
        // cursor actually moves (InsertionPoint setter, movement commands, etc.)
        UpdateContentSize ();
    }

    // TODO: Upgrade TextView events to use CWP
    /// <summary>Raised when the contents of the <see cref="TextView"/> are changed.</summary>
    /// <remarks>
    ///     Unlike the <see cref="View.TextChanged"/> event, this event is raised whenever the user types or otherwise changes
    ///     the contents of the <see cref="TextView"/>.
    /// </remarks>
    public event EventHandler<ContentsChangedEventArgs>? ContentsChanged;

    /// <summary>
    ///     Called when the contents of the TextView change. E.g. when the user types text or deletes text. Raises the
    ///     <see cref="ContentsChanged"/> event.
    /// </summary>
    public virtual void OnContentsChanged ()
    {
        _model.InvalidateMaxWidthCache ();

        ContentsChanged?.Invoke (this, new ContentsChangedEventArgs (CurrentRow, CurrentColumn));

        ProcessInheritsPreviousScheme (CurrentRow, CurrentColumn);
        ProcessAutocomplete ();
    }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? view)
    {
        if (App is { } && App.Mouse.IsGrabbed (this))
        {
            App.Mouse.UngrabMouse ();
        }

        if (newHasFocus)
        {
            PositionCursor ();

            App?.Popovers?.Register (ContextMenu);

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
            App?.Popovers?.DeRegister (ContextMenu);
        }
    }

    /// <summary>Positions the cursor on the current row and column</summary>
    public void PositionCursor ()
    {
        if (!CanFocus || !Enabled || ReadOnly || Driver is null)
        {
            Cursor = Cursor with { Position = null };

            return;
        }

        List<Cell> line = _model.GetLine (CurrentRow);

        // Calculate absolute cursor position and store each glyph width
        _ = TextModel.CursorColumn (TextModel.CellsToStringList (line), CurrentColumn, TabWidth, out List<int> glyphWidths, out _);
        var colsWidth = 0;

        if (glyphWidths.Count > 0)
        {
            for (var i = 0; i < Viewport.X; i++)
            {
                if (i == glyphWidths.Count)
                {
                    break;
                }
                colsWidth += glyphWidths [i];
            }
        }

        if (line.Count > 0)
        {
            if (line.Count > Viewport.X)
            {
                for (int idx = Viewport.X; idx < line.Count; idx++)
                {
                    if (idx >= CurrentColumn)
                    {
                        break;
                    }

                    int cols = glyphWidths [idx];

                    if (TextModel.SetCol (ref colsWidth, Viewport.Width, cols, Viewport.X))
                    {
                        continue;
                    }

                    break;
                }
            }
        }

        int posX = colsWidth - Viewport.X;
        int posY = CurrentRow - Viewport.Y;

        if (posX > -1 && colsWidth >= posX && posX < Viewport.Width && Viewport.Y <= CurrentRow && posY < Viewport.Height)
        {
            Cursor = Cursor with { Position = ViewportToScreen (new Point (colsWidth - Viewport.X, CurrentRow - Viewport.Y)) };
        }
        else
        {
            Cursor = Cursor with { Position = null };
        }
    }

    private PopoverMenu CreateContextMenu ()
    {
        PopoverMenu menu = new (new List<View>
        {
            new MenuItem (this, Command.SelectAll),
            new MenuItem (this, Command.DeleteAll),
            new MenuItem (this, Command.Copy),
            new MenuItem (this, Command.Cut),
            new MenuItem (this, Command.Paste),
            new MenuItem (this, Command.Undo),
            new MenuItem (this, Command.Redo)
        })
        {
#if DEBUG
            Id = "textViewContextMenu"
#endif
        };

        menu.KeyChanged += ContextMenu_KeyChanged;

        return menu;
    }

    //
    // Clears the contents of the selected region
    //

    private void ContextMenu_KeyChanged (object? sender, KeyChangedEventArgs e) => KeyBindings.Replace (e.OldKey, e.NewKey);

    /// <summary>Get the Context Menu.</summary>
    public PopoverMenu? ContextMenu { get; private set; }

    /// <inheritdoc/>
    public string? GetDemoKeyStrokes () => "wait:500,End,wait:300,` Editing`,wait:400,` text!`,wait:800";

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Text = """
               TextView provides a fully featured multi-line text editor.
               It supports word wrap and history for undo.
               """;

        // This enables AllViews_HasFocus_Changed_Event to pass since it requires
        // tab navigation to work
        TabKeyAddsTab = false;

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing && ContextMenu is { })
        {
            ContextMenu.Visible = false;
            ContextMenu.Dispose ();
            ContextMenu = null;
        }

        base.Dispose (disposing);
    }
}
