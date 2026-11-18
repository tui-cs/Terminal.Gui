using JetBrains.Annotations;

namespace ViewsTests.Markdown;

// Copilot
[TestSubject (typeof (Terminal.Gui.Views.Markdown))]
public class MarkdownViewSelectionTests
{
    /// <summary>Helper: builds and lays out a Markdown view at the given width/height.</summary>
    private static (IApplication App, Runnable Window, Terminal.Gui.Views.Markdown Mv) CreateMv (string text, int width = 40, int height = 10)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (width, height);
        app.Clipboard = new FakeClipboard ();

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        Terminal.Gui.Views.Markdown mv = new () { Text = text, Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (mv);
        app.Begin (window);
        app.LayoutAndDraw ();

        return (app, window, mv);
    }

    [Fact]
    public void SelectAll_Sets_IsSelecting_And_Returns_True ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "Hello World", Width = 40, Height = 5 };
        View host = new () { Width = 40, Height = 5 };
        host.Add (mv);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        bool result = mv.SelectAll ();

        Assert.True (result);
        Assert.True (mv.IsInSelection (0, 0));

        host.Dispose ();
    }

    [Fact]
    public void SelectAll_Empty_Content_Returns_True ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "", Width = 40, Height = 5 };
        View host = new () { Width = 40, Height = 5 };
        host.Add (mv);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        bool result = mv.SelectAll ();

        Assert.True (result);

        host.Dispose ();
    }

    [Fact]
    public void ClearSelection_Clears_IsSelecting ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "Hello", Width = 40, Height = 5 };
        View host = new () { Width = 40, Height = 5 };
        host.Add (mv);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        mv.SelectAll ();
        Assert.True (mv.IsInSelection (0, 0));

        mv.ClearSelection ();
        Assert.False (mv.IsInSelection (0, 0));

        host.Dispose ();
    }

    [Fact]
    public void IsInSelection_False_When_Not_Selecting ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "Hello", Width = 40, Height = 5 };
        View host = new () { Width = 40, Height = 5 };
        host.Add (mv);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // No SelectAll called — should return false
        Assert.False (mv.IsInSelection (0, 0));
        Assert.False (mv.IsInSelection (0, 5));

        host.Dispose ();
    }

    [Fact]
    public void Copy_Without_Selection_Copies_Markdown_Source ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("# Hello\n\nWorld");

        bool result = mv.Copy ();

        Assert.True (result);
        app.Clipboard!.TryGetClipboardData (out string clipboard);
        Assert.Equal ("# Hello\n\nWorld", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Copy_With_SelectAll_Copies_Selected_Text ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello");

        mv.SelectAll ();
        bool result = mv.Copy ();

        Assert.True (result);
        app.Clipboard!.TryGetClipboardData (out string clipboard);

        // The rendered text for "Hello" should contain "Hello"
        Assert.Contains ("Hello", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Copy_Command_Copies_Markdown_When_No_Selection ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("# Test");

        // Invoke Command.Copy directly (no selection active)
        mv.InvokeCommand (Command.Copy);

        app.Clipboard!.TryGetClipboardData (out string clipboard);
        Assert.Equal ("# Test", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void SelectAll_Command_Then_Copy_Command_Copies_All_Text ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello World");

        mv.InvokeCommand (Command.SelectAll);
        mv.InvokeCommand (Command.Copy);

        app.Clipboard!.TryGetClipboardData (out string clipboard);
        Assert.Contains ("Hello World", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Text_Change_Clears_Selection ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "Hello", Width = 40, Height = 5 };
        View host = new () { Width = 40, Height = 5 };
        host.Add (mv);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        mv.SelectAll ();
        Assert.True (mv.IsInSelection (0, 0));

        // Changing text should clear selection
        mv.Text = "World";

        Assert.False (mv.IsInSelection (0, 0));

        host.Dispose ();
    }

    [Fact]
    public void ContextMenu_Is_Null_Before_Init ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "Hello" };

        Assert.Null (mv.ContextMenu);

        mv.Dispose ();
    }

    [Fact]
    public void ContextMenu_Is_Created_On_Focus ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello");

        mv.SetFocus ();

        Assert.NotNull (mv.ContextMenu);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void ContextMenu_Is_Disposed_On_Losing_Focus ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello");

        // Add another focusable view
        Button btn = new () { Text = "OK", X = 0, Y = 5 };
        window.Add (btn);
        app.LayoutAndDraw ();

        mv.SetFocus ();
        Assert.NotNull (mv.ContextMenu);

        // Move focus away
        btn.SetFocus ();
        Assert.Null (mv.ContextMenu);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - verifies that LeftButtonPressed is bound to Command.Activate
    [Fact]
    public void MouseBindings_LeftButtonPressed_IsBoundTo_Activate ()
    {
        Terminal.Gui.Views.Markdown mv = new ();

        bool found = mv.MouseBindings.TryGet (MouseFlags.LeftButtonPressed, out MouseBinding binding);

        Assert.True (found);
        Assert.Contains (Command.Activate, binding.Commands);

        mv.Dispose ();
    }

    // Copilot - verifies that LeftButtonPressed|PositionReport is bound to Command.Activate
    [Fact]
    public void MouseBindings_LeftButtonPressedPositionReport_IsBoundTo_Activate ()
    {
        Terminal.Gui.Views.Markdown mv = new ();

        bool found = mv.MouseBindings.TryGet (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, out MouseBinding binding);

        Assert.True (found);
        Assert.Contains (Command.Activate, binding.Commands);

        mv.Dispose ();
    }

    // Copilot - Regression: #5272 — Ctrl+LeftButtonReleased must NOT be bound to Context
    // The base View class adds this binding; Markdown must remove it so Ctrl+Click can follow
    // links without triggering the context menu popover.
    [Fact]
    public void MouseBindings_CtrlLeftButtonReleased_IsNotBoundTo_Context ()
    {
        Terminal.Gui.Views.Markdown mv = new ();

        bool found = mv.MouseBindings.TryGet (MouseFlags.LeftButtonReleased | MouseFlags.Ctrl, out _);

        Assert.False (found);

        mv.Dispose ();
    }

    // Copilot - Regression: #5272 — Ctrl+Click on a link opens the link and does NOT show context menu
    [Fact]
    public void CtrlClick_On_Link_Opens_Link_And_Does_Not_Show_Context_Menu ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("[Click](https://example.com)");

        mv.SetFocus ();

        var linkClicked = false;

        mv.LinkClicked += (_, e) =>
                          {
                              linkClicked = true;
                              e.Handled = true;
                          };

        // Simulate Ctrl+Click: press, Ctrl+release, Ctrl+clicked
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased | MouseFlags.Ctrl });
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked });

        Assert.True (linkClicked);
        Assert.True (mv.ContextMenu is null || !mv.ContextMenu.Visible);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - verifies that a drag (press + position-report) activates the selection
    [Fact]
    public void Drag_Mouse_Creates_Selection ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello World");

        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (5, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        // After a drag the selection should span columns 0-4
        Assert.True (mv.IsInSelection (0, 0));
        Assert.True (mv.IsInSelection (0, 3));
        Assert.False (mv.IsInSelection (0, 6));

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - verifies that mouse release does not clear an active selection
    [Fact]
    public void Selection_Persists_After_LeftButtonReleased ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello World");

        // Simulate a drag: press, drag, release
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (5, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });
        mv.NewMouseEvent (new Mouse { Position = new Point (5, 0), Flags = MouseFlags.LeftButtonReleased });

        // Selection should survive the release
        Assert.True (mv.IsInSelection (0, 0));

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - verifies that a plain click (no drag) clears the selection
    [Fact]
    public void Plain_Click_Clears_Selection ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello World");

        mv.SelectAll ();
        Assert.True (mv.IsInSelection (0, 0));

        // Simulate a plain click (no PositionReport drag events)
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased });
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked });

        // Selection should be cleared by the click
        Assert.False (mv.IsInSelection (0, 0));

        window.Dispose ();
        app.Dispose ();
    }

    // --- Copy fidelity tests ---
    // These use content from Markdown.DefaultMarkdownSample where possible, which contains
    // Unicode characters (emoji), task-list markers, and fenced code blocks with language tags.

    // Copilot
    [Fact]
    public void SelectedText_Is_Null_When_No_Selection ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello");

        Assert.Null (mv.SelectedText);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - task-list items from DefaultMarkdownSample (includes emoji ✅ 🔧 🎉)
    [Fact]
    public void SelectAll_TaskList_SelectedText_Preserves_Markdown_List_Markers ()
    {
        // Source task-list lines taken from Markdown.DefaultMarkdownSample § Checklist
        string md = "- [x] Bold & italic ✅\n- [x] Code blocks 🔧\n- [ ] Emojis 🎉";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("- [x] Bold & italic ✅", selected);
        Assert.Contains ("- [x] Code blocks 🔧", selected);
        Assert.Contains ("- [ ] Emojis 🎉", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - plain bullet list (no task markers)
    [Fact]
    public void SelectAll_BulletList_SelectedText_Preserves_Markdown_List_Markers ()
    {
        string md = "- foo\n- bar\n- baz";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 40, height: 10);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.DoesNotContain ("•", selected);
        Assert.Contains ("- foo", selected);
        Assert.Contains ("- bar", selected);
        Assert.Contains ("- baz", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - fenced code block from DefaultMarkdownSample (csharp, contains 🌍 emoji)
    [Fact]
    public void SelectAll_FencedCodeBlock_With_Language_SelectedText_Preserves_Fences ()
    {
        // Source taken from Markdown.DefaultMarkdownSample § Code Block (csharp)
        string md = "```csharp\nConsole.WriteLine (\"Hello, Terminal.Gui! 🌍\");\nvar x = 42;\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("```csharp", selected);
        Assert.Contains ("Console.WriteLine", selected);
        Assert.Contains ("🌍", selected);
        Assert.Contains ("var x = 42;", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - fenced code block without language specifier
    [Fact]
    public void SelectAll_FencedCodeBlock_Without_Language_SelectedText_Preserves_Fences ()
    {
        string md = "```\nhello world\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 40, height: 10);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("```", selected);
        Assert.Contains ("hello world", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - Copy() after SelectAll with task list (includes emoji)
    [Fact]
    public void Copy_After_SelectAll_TaskList_Clipboard_Contains_Markdown_Markers ()
    {
        string md = "- [x] Bold & italic ✅\n- [x] Code blocks 🔧\n- [ ] Emojis 🎉";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        mv.SelectAll ();
        mv.Copy ();

        app.Clipboard!.TryGetClipboardData (out string clipboard);
        Assert.Contains ("- [x] Bold & italic ✅", clipboard);
        Assert.Contains ("- [x] Code blocks 🔧", clipboard);
        Assert.Contains ("- [ ] Emojis 🎉", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - Copy() after SelectAll with csharp code block (includes 🌍)
    [Fact]
    public void Copy_After_SelectAll_FencedCodeBlock_Clipboard_Contains_Fences ()
    {
        string md = "```csharp\nConsole.WriteLine (\"Hello, Terminal.Gui! 🌍\");\nvar x = 42;\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        mv.SelectAll ();
        mv.Copy ();

        app.Clipboard!.TryGetClipboardData (out string clipboard);
        Assert.Contains ("```csharp", clipboard);
        Assert.Contains ("🌍", clipboard);
        Assert.Contains ("var x = 42;", clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - partial drag selection spanning task-list items with emoji
    [Fact]
    public void PartialSelection_TaskList_SelectedText_Preserves_Markdown_Markers ()
    {
        string md = "- [x] Bold & italic ✅\n- [ ] Emojis 🎉";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        // Press at start of line 0, drag to column 10 of line 1
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (10, 1), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("- [x] Bold & italic ✅", selected);
        Assert.Contains ("- [ ]", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - partial drag selection spanning lines inside a csharp code block (with 🌍)
    // The selection covers all code lines but no non-code content, so no fence delimiters
    // should appear in the output (mirrors the copy-button behaviour on MarkdownCodeBlock).
    [Fact]
    public void PartialSelection_FencedCodeBlock_SelectedText_DoesNotIncludeFence ()
    {
        string md = "```csharp\nConsole.WriteLine (\"Hello, Terminal.Gui! 🌍\");\nvar x = 42;\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        // Select both code lines (rendered as lines 0 and 1 — fence lines are not in _renderedLines).
        // End at x=10 (one short of "var x = 42;" width=11) so IsFullDocumentSelected() returns false.
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (10, 1), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.DoesNotContain ("```", selected);
        Assert.Contains ("Console.WriteLine", selected);
        Assert.Contains ("🌍", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - FAILS: a table is rendered as a single placeholder RenderedLine (IsTable=true)
    // with no text content.  When the selection is partial (start.X > 0, so IsFullDocumentSelected
    // short-circuit does not fire) and covers the table rows, the pipe-row syntax is completely
    // absent from GetSelectedText().
    [Fact]
    public void PartialSelection_IncludingTable_SelectedText_Preserves_Table_Markdown ()
    {
        // Content taken from DefaultMarkdownSample § Table (uses ✅ emoji).
        // "After." is appended so the last rendered line is text, not the table placeholder,
        // which makes the drag a genuine partial selection (end.Y < lastLine is NOT required —
        // starting at col 1 is enough to skip the IsFullDocumentSelected shortcut).
        string md = "## Table\n\n| Feature | Status |\n|---|---|\n| A | ✅ |\n\nAfter.";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        // Start at column 1 — IsFullDocumentSelected() requires start==(0,0), so this forces
        // the partial-selection (display-text) code path.  Drag far right/down to cover the table.
        mv.NewMouseEvent (new Mouse { Position = new Point (1, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (100, 100), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("| Feature | Status |", selected);
        Assert.Contains ("| A | ✅ |", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - FAILS: inline formatting (**bold**, *italic*, `code`, ~~strike~~), heading
    // markers (#), tables, thematic breaks, and link syntax ([text](url)) are all lost in
    // the display representation — the selected text cannot equal the original markdown source.
    [Fact]
    public void SelectAll_DefaultMarkdownSample_SelectedText_RoundTrips ()
    {
        string md = Terminal.Gui.Views.Markdown.DefaultMarkdownSample;
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 120, height: 60);

        mv.SelectAll ();
        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Equal (md, selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - right-clicking an unfocused view should focus it and open the context menu
    [Fact]
    public void RightClick_On_Unfocused_View_Creates_And_Shows_ContextMenu ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello");

        // Add another focusable view and move focus there so the Markdown view is unfocused
        Button btn = new () { Text = "OK", X = 0, Y = 5 };
        window.Add (btn);
        app.LayoutAndDraw ();
        btn.SetFocus ();

        Assert.False (mv.HasFocus);
        Assert.Null (mv.ContextMenu);

        // Simulate a right-click while the view is not focused
        mv.NewMouseEvent (new Mouse
        {
            Position = new Point (0, 0),
            ScreenPosition = new Point (0, 0),
            Flags = MouseFlags.RightButtonClicked
        });

        // The view should now be focused and the context menu should be created
        Assert.True (mv.HasFocus);
        Assert.NotNull (mv.ContextMenu);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - right-clicking an already-focused view should still open the context menu
    [Fact]
    public void RightClick_On_Focused_View_Shows_ContextMenu ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello");

        mv.SetFocus ();
        Assert.NotNull (mv.ContextMenu);

        // Second right-click should not crash and context menu should remain
        mv.NewMouseEvent (new Mouse
        {
            Position = new Point (0, 0),
            ScreenPosition = new Point (0, 0),
            Flags = MouseFlags.RightButtonClicked
        });

        Assert.NotNull (mv.ContextMenu);

        window.Dispose ();
        app.Dispose ();
    }

    // --- Drag clamping + auto-scroll tests (comment #3183635182) ---

    // Copilot - dragging above the viewport (negative Y) must not produce a negative
    // contentY that causes an IndexOutOfRangeException in GetSelectedText().
    [Fact]
    public void Drag_AboveViewport_ClampsToFirstLine ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("Hello\nWorld");

        // Anchor at line 0 and then drag ABOVE the top of the view (pos.Y = -5).
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (3, -5), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        // Copy should not throw even though the drag went above the viewport.
        // Before the fix, contentY could be negative → IndexOutOfRangeException in GetSelectedText().
        Exception? ex = Record.Exception (() => mv.Copy ());
        Assert.Null (ex);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - dragging below the viewport must not produce a contentY beyond the last line.
    [Fact]
    public void Drag_BelowViewport_ClampsToLastLine ()
    {
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv ("line0\nline1\nline2", height: 2);

        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });

        // Drag to row 999 — well beyond the content
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 999), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        // Copy must not throw
        Exception? ex = Record.Exception (() => mv.Copy ());
        Assert.Null (ex);

        window.Dispose ();
        app.Dispose ();
    }

    // --- Table-duplication fix tests (comment #3183635237) ---

    // Copilot - a table with 2 body rows generates multiple placeholder RenderedLines;
    // GetSelectedText() must output the reconstructed table exactly once, not once per
    // placeholder row.
    [Fact]
    public void PartialSelection_TableWithMultipleRows_TableAppearsExactlyOnce ()
    {
        // 2 body rows → at least 2 (usually more) table placeholder lines
        string md = "| H1 | H2 |\n|---|---|\n| A | B |\n| C | D |";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 15);

        // Start at col 1 so IsFullDocumentSelected() returns false (forces the display-text path)
        mv.NewMouseEvent (new Mouse { Position = new Point (1, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (100, 100), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);

        // Header row should appear exactly once, not once per placeholder row.
        int count = CountOccurrences (selected, "| H1 | H2 |");
        Assert.Equal (1, count);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - two adjacent tables with the same structure should each appear exactly once.
    [Fact]
    public void PartialSelection_TwoAdjacentTables_EachTableAppearsOnce ()
    {
        // Two separate TableData instances → two independent tables
        const string TABLE = "| H |\n|---|\n| R |";
        string md = TABLE + "\n\n" + TABLE;
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 20);

        mv.NewMouseEvent (new Mouse { Position = new Point (1, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (100, 100), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);

        // The header appears in each table → should occur exactly twice in the output
        int count = CountOccurrences (selected, "| H |");
        Assert.Equal (2, count);

        window.Dispose ();
        app.Dispose ();
    }

    // --- Selection drawing over SubView rows (comment #3183635267) ---

    // Copilot - after SelectAll() on a document that contains a code block, the ANSI output
    // should contain the Focus-attribute escape codes (white-on-black) in the code-block rows,
    // proving the selection overlay is drawn on top of the MarkdownCodeBlock SubView.
    [Fact]
    public void SelectAll_WithCodeBlock_DrawingContainsFocusAttributeOnCodeRows ()
    {
        const int WIDTH = 20;

        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (WIDTH, 5);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Scheme scheme = new (new Attribute (Color.Black, Color.White));
        window.SetScheme (scheme);

        Terminal.Gui.Views.Markdown mv = new () { Text = "```\nAB\n```", Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (scheme);

        window.Add (mv);
        app.Begin (window);
        app.LayoutAndDraw ();

        mv.SelectAll ();
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        string output = app.Driver.GetOutput ().GetLastOutput ();

        // Focus attribute (Black-on-White scheme, Focus = swap → White fg = 97, Black bg = 40)
        // must appear somewhere in the rendered output now that the overlay is drawn.
        Assert.Contains ("\x1b[97m", output);
        Assert.Contains ("\x1b[40m", output);

        window.Dispose ();
        app.Dispose ();
    }

    // --- IsFullDocumentSelected with zero-width last line (comment #3183635296) ---

    // Copilot - SelectAll() on a document ending with a table (zero-width last rendered line)
    // should correctly return the full source markdown via Copy().
    [Fact]
    public void SelectAll_DocEndingWithTable_CopyReturnsFullMarkdown ()
    {
        string md = "intro\n| H | B |\n|---|---|\n| A | C |";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 15);

        mv.SelectAll ();
        mv.Copy ();

        app.Clipboard!.TryGetClipboardData (out string clipboard);
        Assert.Equal (md, clipboard);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - a partial selection on a document ending with a table (starting from col 1)
    // must NOT be treated as a full-document selection, even when the drag reaches the last row.
    [Fact]
    public void PartialSelection_DocEndingWithTable_NotTreatedAsFullDocument ()
    {
        string md = "intro\n| H | B |\n|---|---|\n| A | C |";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 15);

        // Start at col 1 (not col 0) → IsFullDocumentSelected() must return false
        mv.NewMouseEvent (new Mouse { Position = new Point (1, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (100, 100), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);

        // Should NOT equal the full source markdown because the selection started at col 1
        Assert.NotEqual (md, selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - Regression test for #5270.
    // When the Markdown view is scrolled (Viewport.Y > 0) and the selection overlay runs
    // for table rows, DrawSelectionOverlayOnSubViewRows must read from ScreenContents at
    // the CORRECT screen row.  The bug passed drawRow (viewport-relative) to ContentToScreen
    // instead of lineIdx (content-relative), causing ContentToScreen to double-subtract
    // Viewport.Y and read from the wrong row — displaying header content where body content
    // should appear.
    [Fact]
    public void SelectionOverlay_On_Table_Is_Synced_When_Scrolled ()
    {
        // Layout:
        //   row 0 : "para"  (paragraph text)
        //   row 1 : ""      (blank between paragraph and table)
        //   rows 2-6 : 5-row table  (top border, header, separator, body, bottom border)
        const int SCREEN_WIDTH = 30;
        const int SCREEN_HEIGHT = 5; // Must be less than content height (7) so scrolling is possible

        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (SCREEN_WIDTH, SCREEN_HEIGHT);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        Scheme scheme = new (new Attribute (Color.Black, Color.White));
        window.SetScheme (scheme);

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Text = "para\n\n| H | V |\n|---|---|\n| 1 | 2 |",
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        mv.SchemeName = null;
        mv.SetScheme (scheme);
        window.Add (mv);

        // Initial draw at Viewport.Y=0 to populate ScreenContents with the table rows.
        app.Begin (window);
        app.LayoutAndDraw ();

        // Scroll past "para" and the blank line so only the table is visible.
        mv.Viewport = mv.Viewport with { Y = 2 };

        // Activate a full selection, then redraw so the overlay runs while scrolled.
        mv.SelectAll ();
        app.LayoutAndDraw ();

        // The body row "│ 1 │ 2 │" must be visible.
        // With the bug, DrawSelectionOverlayOnSubViewRows passes drawRow (viewport-relative)
        // to ContentToScreen instead of lineIdx (content-relative).  ContentToScreen then
        // double-subtracts Viewport.Y=2, so the body row reads from screen row 1 (the header)
        // and overwrites "│ 1 │ 2 │" with "│ H │ V │" — making "1" and "2" disappear.
        string screen = app.Driver.ToString ();
        Assert.Contains ("1", screen);
        Assert.Contains ("2", screen);

        window.Dispose ();
        app.Dispose ();
    }

    // --- Issue #5273: partial selection inside a code block must not include fence delimiters ---

    // Copilot - Claude Sonnet 4.6
    // Selecting only middle lines of a multi-line code block should not produce fence delimiters.
    [Fact]
    public void PartialSelection_InsideCodeBlock_DoesNotIncludeFenceDelimiters ()
    {
        // 4 code lines; select only the middle two (lines 1 and 2)
        string md = "```csharp\nline A\nline B\nline C\nline D\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        // Rendered lines 0-3 are the four code lines (fence lines are stripped during parse).
        // Press on line 1, drag to end of line 2.
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 1), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (60, 2), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("line B", selected);
        Assert.Contains ("line C", selected);
        Assert.DoesNotContain ("```", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - Claude Sonnet 4.6
    // Selection starts before the code block (on a paragraph line) and ends inside it.
    // Only the opening fence should be present; the closing fence must be omitted.
    [Fact]
    public void PartialSelection_StartBeforeCodeBlock_EndInside_HasOpeningFenceOnly ()
    {
        string md = "Before\n```csharp\nline A\nline B\nline C\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        // Rendered line 0 = "Before", lines 1-3 = code lines.
        // Press on line 0, drag to line 2 (mid-block).
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (60, 2), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("Before", selected);
        Assert.Contains ("```csharp", selected);
        Assert.Contains ("line A", selected);
        Assert.Contains ("line B", selected);
        Assert.DoesNotContain ("line C", selected);

        // Opening fence present; closing fence absent because selection ends mid-block.
        int fenceCount = CountOccurrences (selected, "```");
        Assert.Equal (1, fenceCount);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - Claude Sonnet 4.6
    // Selection starts inside a code block and ends after it (on a paragraph line).
    // A closing fence is expected because the selection crosses the block's end, even
    // though no opening fence was emitted (the selection began inside the block).
    [Fact]
    public void PartialSelection_StartInsideCodeBlock_EndAfter_HasClosingFenceOnly ()
    {
        string md = "```csharp\nline A\nline B\nline C\n```\nAfter";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        // Rendered lines 0-2 = code lines, line 3 = "After".
        // Press on line 1 (mid-block), drag to line 3 (after block).
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 1), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (60, 3), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("line B", selected);
        Assert.Contains ("line C", selected);
        Assert.Contains ("After", selected);
        Assert.DoesNotContain ("line A", selected);

        // Closing fence present because selection crosses out of the code block; no opening fence.
        int fenceCount = CountOccurrences (selected, "```");
        Assert.Equal (1, fenceCount);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - Claude Sonnet 4.6
    // Regression guard: selecting all lines of a code block starting from its first line should
    // produce NO fence delimiters — the selection is entirely within the fenced region.
    [Fact]
    public void PartialSelection_AllLinesOfCodeBlock_FromFirstLine_NoFences ()
    {
        // Three code lines; select only the first two to avoid triggering IsFullDocumentSelected().
        string md = "```csharp\nline A\nline B\nline C\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 60, height: 10);

        // Press on line 0 (first code line), drag to end of line 1.
        // end.Y=1 < lastLine=2, so IsFullDocumentSelected() returns false and partial-selection runs.
        mv.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (6, 1), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        string? selected = mv.SelectedText;

        Assert.NotNull (selected);
        Assert.Contains ("line A", selected);
        Assert.Contains ("line B", selected);
        Assert.DoesNotContain ("line C", selected);
        Assert.DoesNotContain ("```", selected);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot - Regression test for partial code-block selection highlight.
    // When only part of a code-block line is selected (e.g. start or end of multi-line selection
    // falls inside the block), DrawSelectionOverlayOnSubViewRows must NOT highlight the entire
    // row — it must respect the per-column IsInSelection check, exactly as DrawRenderedLine does
    // for plain text lines.  The bug applied selAttr to every column unconditionally.
    [Fact]
    public void SelectionOverlay_On_CodeBlock_HighlightsOnlySelectedColumns ()
    {
        // Code block with two lines; select only from column 3 of line 0 to the end.
        // Line 0 columns 0-2 must NOT carry the selection background.
        string md = "```\nABCDEF\nGHIJKL\n```";
        (IApplication app, Runnable window, Terminal.Gui.Views.Markdown mv) = CreateMv (md, width: 20, height: 5);

        app.LayoutAndDraw ();

        // Anchor at col 3 of rendered line 0, drag to end of line 1.
        mv.NewMouseEvent (new Mouse { Position = new Point (3, 0), Flags = MouseFlags.LeftButtonPressed });
        mv.NewMouseEvent (new Mouse { Position = new Point (20, 1), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });
        app.LayoutAndDraw ();

        // Inspect raw screen buffer: the first 3 columns of line 0 must use Normal (not Focus) role.
        Scheme scheme = mv.GetScheme ()!;
        Attribute focus = scheme.Focus;

        Cell [,]? screen = app.Driver!.Contents;
        Assert.NotNull (screen);

        // Line 0 of the code block is screen row 0 (no preceding content).
        for (int col = 0; col < 3; col++)
        {
            Assert.NotEqual (focus, screen [0, col].Attribute);
        }

        // Column 3 onwards on line 0 must carry the selection (focus) attribute.
        for (int col = 3; col < 6; col++)
        {
            Assert.Equal (focus, screen [0, col].Attribute);
        }

        window.Dispose ();
        app.Dispose ();
    }

    private static int CountOccurrences (string text, string pattern)
    {
        int count = 0;
        int idx = 0;

        while ((idx = text.IndexOf (pattern, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }

        return count;
    }
}
