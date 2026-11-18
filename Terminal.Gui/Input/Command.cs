// These classes use a keybinding system based on the design implemented in Scintilla.Net which is an
// MIT licensed open source project https://github.com/jacobslusser/ScintillaNET/blob/master/src/ScintillaNET/Command.cs


namespace Terminal.Gui.Input;

/// <summary>
///     Actions which can be performed by a <see cref="View"/>.
/// </summary>
/// <seealso cref="View.KeyBindings"/>
/// <seealso cref="View.MouseBindings"/>
/// <seealso cref="Application.KeyBindings"/>
/// <remarks>
///     <para>
///         <see cref="Application"/> supports a subset of these commands by default, which can be overriden via <see cref="Application.KeyBindings"/>.
///     </para>
///     <para>
///         See the Commands Deep Dive for more information: <see href="https://tui-cs.github.io/Terminal.Gui/docs/command.html"/>.
///     </para>
///     <para>
///         <b>Every member has an explicit, frozen integer value.</b> These values are an ABI contract:
///         separately-compiled assemblies (e.g. the <c>Terminal.Gui.Editor</c> package) bake the integer
///         of each <see cref="Command"/> into their key bindings. Inserting or reordering members would
///         silently change those integers and re-map already-compiled bindings to the wrong commands —
///         e.g. Backspace invoking <see cref="SelectAll"/> (tui-cs/Editor#241). <b>When adding a command,
///         append it with the next unused number; never insert, reorder, or renumber existing members.</b>
///     </para>
/// </remarks>
/// <seealso cref="KeyBinding"/>
/// <seealso cref="MouseBinding"/>
/// <seealso cref="ICommandBinding"/>
/// <seealso cref="View.InvokeCommand(Command)"/>
/// <seealso cref="CommandContext"/>

public enum Command
{
    /// <summary>
    ///     Indicates the command is not bound or invalid. Will call <see cref="View.RaiseCommandNotBound"/>.
    /// </summary>
    NotBound = 0,

    #region Base View Commands

    /// <summary>
    ///     Accepts the current state of the View (e.g. list selection, button press, checkbox state, etc.).
    ///     <para>
    ///         The default implementation in <see cref="View"/> raises the <see cref="View.Accepting"/> and
    ///         <see cref="View.Accepting"/> events.
    ///     </para>
    /// </summary>
    Accept = 1,

    /// <summary>
    ///     Performs a hot key action (e.g. setting focus, accepting, and/or moving focus to the next View).
    ///     <para>
    ///         The default implementation in <see cref="View"/> raises the <see cref="View.HandlingHotKey"/> event
    ///         and if that is not handled, invokes <see cref="Command.Activate"/>.
    ///     </para>
    /// </summary>
    HotKey = 2,

    /// <summary>
    ///     Activates the View or an item in the View, changing its state or preparing it for interaction
    ///     (e.g. toggling a checkbox, selecting a list item, focusing a button, navigating a menu item) without necessarily accepting it.
    ///     <para>
    ///         The default implementation in <see cref="View"/> raises the <see cref="View.Activating"/> event. If  <see cref="View.Activating"/> is not
    ///         handled, <see cref="View.SetFocus"/> will be called and the <see cref="View.Activated"/> event will be raised.
    ///     </para>
    /// </summary>
    Activate = 3,

    #endregion

    #region Movement Commands

    /// <summary>Moves up one (cell, line, etc...).</summary>
    Up = 4,

    /// <summary>Moves down one item (cell, line, etc...).</summary>
    Down = 5,

    /// <summary>
    ///     Moves left one (cell, line, etc...).
    /// </summary>
    Left = 6,

    /// <summary>
    ///     Moves right one (cell, line, etc...).
    /// </summary>
    Right = 7,

    /// <summary>Move one page up.</summary>
    PageUp = 8,

    /// <summary>Move one page down.</summary>
    PageDown = 9,

    /// <summary>Moves to the left page.</summary>
    PageLeft = 10,

    /// <summary>Moves to the right page.</summary>
    PageRight = 11,

    /// <summary>Moves to the top of page.</summary>
    StartOfPage = 12,

    /// <summary>Moves to the bottom of page.</summary>
    EndOfPage = 13,

    /// <summary>Moves to the start (e.g. the top or home).</summary>
    Start = 14,

    /// <summary>Moves or resets to the home position.</summary>
    Home = 15,

    /// <summary>Moves to the end (e.g. the bottom).</summary>
    End = 16,

    /// <summary>Moves left to the start on the current row/line.</summary>
    LeftStart = 17,

    /// <summary>Moves right to the end on the current row/line.</summary>
    RightEnd = 18,

    /// <summary>Moves to the start of the previous word.</summary>
    WordLeft = 19,

    /// <summary>Moves the start of the next word.</summary>
    WordRight = 20,

    #endregion

    #region Movement With Extension Commands

    /// <summary>Extends the selection up one item (cell, line, etc...).</summary>
    UpExtend = 21,

    /// <summary>Extends the selection down one (cell, line, etc...).</summary>
    DownExtend = 22,

    /// <summary>
    ///     Extends the selection left one item (cell, line, etc...)
    /// </summary>
    LeftExtend = 23,

    /// <summary>
    ///     Extends the selection right one item (cell, line, etc...)
    /// </summary>
    RightExtend = 24,

    /// <summary>Extends the selection to the start of the previous word.</summary>
    WordLeftExtend = 25,

    /// <summary>Extends the selection to the start of the next word.</summary>
    WordRightExtend = 26,

    /// <summary>Move one page down extending the selection to cover revealed objects/characters.</summary>
    PageDownExtend = 27,

    /// <summary>Move one page up extending the selection to cover revealed objects/characters.</summary>
    PageUpExtend = 28,

    /// <summary>Extends the selection to start (e.g. home or top).</summary>
    StartExtend = 29,

    /// <summary>Extends the selection to end (e.g. bottom).</summary>
    EndExtend = 30,

    /// <summary>Extends the selection to the start on the current row/line.</summary>
    LeftStartExtend = 31,

    /// <summary>Extends the selection to the right on the current row/line.</summary>
    RightEndExtend = 32,

    /// <summary>Toggles the selection (or a specific element of the selection).</summary>
    ToggleExtend = 33,

    #endregion

    #region Editing Commands

    /// <summary>Deletes the word to the right of the cursor.</summary>
    KillWordRight = 34,

    /// <summary>Deletes the word to left to the cursor.</summary>
    KillWordLeft = 35,

    /// <summary>
    ///     Toggles overwrite mode such that newly typed text overwrites the text that is already there (typically
    ///     associated with the Insert key).
    /// </summary>
    ToggleOverwrite = 36,

    // QUESTION: What is the difference between EnableOverwrite and ToggleOverwrite?

    /// <summary>
    ///     Enables overwrite mode such that newly typed text overwrites the text that is already there (typically
    ///     associated with the Insert key).
    /// </summary>
    EnableOverwrite = 37,

    /// <summary>
    ///     Inserts a character.
    /// </summary>
    Insert = 38,

    /// <summary>Disables overwrite mode (<see cref="EnableOverwrite"/>)</summary>
    DisableOverwrite = 39,

    /// <summary>Deletes the character on the right.</summary>
    DeleteCharRight = 40,

    /// <summary>Deletes the character on the left.</summary>
    DeleteCharLeft = 41,

    /// <summary>Selects all objects.</summary>
    SelectAll = 42,

    /// <summary>Deletes all objects.</summary>
    DeleteAll = 43,

    /// <summary>Inserts a new item.</summary>
    NewLine = 44,

    /// <summary>Unix emulation.</summary>
    UnixEmulation = 45,

    /// <summary>Inserts a tab character or spaces at the cursor or selection.</summary>
    InsertTab = 46,

    /// <summary>Removes one level of indentation from the current line or selection.</summary>
    Unindent = 47,

    #endregion

    #region Search Commands

    /// <summary>Opens or activates a find/search UI.</summary>
    Find = 48,

    /// <summary>Finds the next match.</summary>
    FindNext = 49,

    /// <summary>Finds the previous match.</summary>
    FindPrevious = 50,

    /// <summary>Opens or activates a find-and-replace UI.</summary>
    Replace = 51,

    #endregion

    #region Tree Commands

    /// <summary>Moves down to the last child node of the branch that holds the current selection.</summary>
    LineDownToLastBranch = 52,

    /// <summary>Moves up to the first child node of the branch that holds the current selection.</summary>
    LineUpToFirstBranch = 53,

    #endregion

    #region Scroll Commands

    /// <summary>Scrolls down one (cell, line, etc...).</summary>
    ScrollDown = 54,

    /// <summary>Scrolls up one item (cell, line, etc...).</summary>
    ScrollUp = 55,

    /// <summary>Scrolls one item (cell, character, etc...) to the left.</summary>
    ScrollLeft = 56,

    /// <summary>Scrolls one item (cell, character, etc...) to the right.</summary>
    ScrollRight = 57,

    #endregion

    #region Clipboard Commands

    /// <summary>Undo changes.</summary>
    Undo = 58,

    /// <summary>Redo changes.</summary>
    Redo = 59,

    /// <summary>Copies the current selection.</summary>
    Copy = 60,

    /// <summary>Cuts the current selection.</summary>
    Cut = 61,

    /// <summary>Pastes the current selection.</summary>
    Paste = 62,

    /// <summary>Cuts to the clipboard the characters from the current position to the end of the line.</summary>
    CutToEndOfLine = 63,

    /// <summary>Cuts to the clipboard the characters from the current position to the start of the line.</summary>
    CutToStartOfLine = 64,

    #endregion

    #region Navigation Commands

    /// <summary>Moves focus to the next <see cref="TabBehavior.TabStop"/>.</summary>
    NextTabStop = 65,

    /// <summary>Moves focus to the previous <see cref="TabBehavior.TabStop"/>.</summary>
    PreviousTabStop = 66,

    /// <summary>Moves focus to the next <see cref="TabBehavior.TabGroup"/>.</summary>
    NextTabGroup = 67,

    /// <summary>Moves focus to the next<see cref="TabBehavior.TabGroup"/>.</summary>
    PreviousTabGroup = 68,

    /// <summary>Enables arrange mode.</summary>
    Arrange = 69,

    #endregion

    #region Action Commands

    /// <summary>Toggles something (e.g. the expanded or collapsed state of a list).</summary>
    Toggle = 70,

    /// <summary>Expands a list or item (with subitems).</summary>
    Expand = 71,

    /// <summary>Recursively Expands all child items and their child items (if any).</summary>
    ExpandAll = 72,

    /// <summary>Collapses a list or item (with subitems).</summary>
    Collapse = 73,

    /// <summary>Recursively collapses a list items of their children (if any).</summary>
    CollapseAll = 74,

    /// <summary>Cancels an action or any temporary states on the control e.g. expanding a combo list.</summary>
    Cancel = 75,

    /// <summary>Quit.</summary>
    Quit = 76,

    /// <summary>Refresh.</summary>
    Refresh = 77,

    /// <summary>Suspend an application (Only implemented in UnixDriver).</summary>
    Suspend = 78,

    /// <summary>Open the selected item or invoke a UI for opening something.</summary>
    Open = 79,

    /// <summary>Saves the current document.</summary>
    Save = 80,

    /// <summary>Saves the current document with a new name.</summary>
    SaveAs = 81,

    /// <summary>Creates a new document.</summary>
    New = 82,

    /// <summary>Shows context about the item (e.g. a context menu).</summary>
    Context = 83,

    /// <summary>
    ///     Invokes a user interface for editing or configuring something.
    /// </summary>
    Edit = 84,

    /// <summary>Centers the current item or viewport.</summary>
    Center = 85,

    /// <summary>Zooms in.</summary>
    ZoomIn = 86,

    /// <summary>Zooms out.</summary>
    ZoomOut = 87,

    #endregion

    #region Multi-Caret Commands

    /// <summary>
    ///     Adds an additional caret one line above the topmost caret (multi-caret editing),
    ///     preserving the sticky visual column. Mirrors VS Code's
    ///     <c>editor.action.insertCursorAbove</c>. Views that support multi-caret bind this
    ///     through <see cref="View.KeyBindings"/> or their configurable default key bindings.
    /// </summary>
    InsertCaretAbove = 88,

    /// <summary>
    ///     Adds an additional caret one line below the bottommost caret (multi-caret editing),
    ///     preserving the sticky visual column. Mirrors VS Code's
    ///     <c>editor.action.insertCursorBelow</c>. Views that support multi-caret bind this
    ///     through <see cref="View.KeyBindings"/> or their configurable default key bindings.
    /// </summary>
    InsertCaretBelow = 89,

    #endregion

    #region Mouse Selection Commands

    /// <summary>Starts extending a selection via pointing-device input.</summary>
    StartSelection = 90,

    /// <summary>Starts extending a rectangular selection via pointing-device input.</summary>
    StartRectangleSelection = 91,

    #endregion
}
