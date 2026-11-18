namespace ViewsTests.TextViewTests;

/// <summary>Tests for public command methods in TextView.Commands.cs.</summary>
public class TextViewCommandTests
{
    // Claude - Opus 4.5

    #region DeleteAll

    [Fact]
    public void DeleteAll_Clears_All_Text ()
    {
        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };

        bool result = tv.DeleteAll ();

        Assert.True (result);
        Assert.Equal ("", tv.Text);
        Assert.Equal (0, tv.CurrentColumn);
        Assert.Equal (0, tv.CurrentRow);
    }

    [Fact]
    public void DeleteAll_Clears_Multiline_Text ()
    {
        TextView tv = new () { Width = 40, Height = 10, Text = $"Line 1{Environment.NewLine}Line 2{Environment.NewLine}Line 3" };

        bool result = tv.DeleteAll ();

        Assert.True (result);
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
    }

    [Fact]
    public void DeleteAll_On_Empty_Text_Returns_True ()
    {
        TextView tv = new () { Width = 40, Height = 10, Text = "" };

        bool result = tv.DeleteAll ();

        Assert.True (result);
        Assert.Equal ("", tv.Text);
    }

    [Fact]
    public void DeleteAll_Respects_ReadOnly ()
    {
        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World", ReadOnly = true };

        bool result = tv.DeleteAll ();

        Assert.True (result);
        Assert.Equal ("Hello World", tv.Text);
    }

    [Fact]
    public void DeleteAll_Via_KeyBinding ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl.WithShift));

        Assert.Equal ("", tv.Text);
    }

    [Fact]
    public void DeleteAll_Via_KeyBinding_Respects_ReadOnly ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World", ReadOnly = true };
        runnable.Add (tv);
        app.Begin (runnable);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl.WithShift));

        Assert.Equal ("Hello World", tv.Text);
    }

    [Fact]
    public void DeleteAll_With_Cursor_In_Middle ()
    {
        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        tv.InsertionPoint = new Point (5, 0);

        bool result = tv.DeleteAll ();

        Assert.True (result);
        Assert.Equal ("", tv.Text);
    }

    #endregion

    #region SelectAll

    [Fact]
    public void SelectAll_Selects_All_SingleLine_Text ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        bool result = tv.SelectAll ();

        Assert.True (result);
        Assert.True (tv.IsSelecting);
        Assert.Equal ("Hello World", tv.SelectedText);
        Assert.Equal (11, tv.SelectedLength);
    }

    [Fact]
    public void SelectAll_Selects_All_Multiline_Text ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = $"Line 1{Environment.NewLine}Line 2{Environment.NewLine}Line 3" };
        runnable.Add (tv);
        app.Begin (runnable);

        bool result = tv.SelectAll ();

        Assert.True (result);
        Assert.True (tv.IsSelecting);
        Assert.Equal ($"Line 1{Environment.NewLine}Line 2{Environment.NewLine}Line 3", tv.SelectedText);
    }

    [Fact]
    public void SelectAll_On_Empty_Text_Returns_True ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "" };
        runnable.Add (tv);
        app.Begin (runnable);

        bool result = tv.SelectAll ();

        Assert.True (result);
    }

    [Fact]
    public void SelectAll_Via_KeyBinding ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        Assert.True (tv.NewKeyDownEvent (Key.A.WithCtrl));

        Assert.True (tv.IsSelecting);
        Assert.Equal ("Hello World", tv.SelectedText);
    }

    [Fact]
    public void SelectAll_Moves_Cursor_To_End ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = $"Line 1{Environment.NewLine}Line 2" };
        runnable.Add (tv);
        app.Begin (runnable);

        tv.SelectAll ();

        Assert.Equal (1, tv.CurrentRow);
        Assert.Equal (6, tv.CurrentColumn);
    }

    #endregion

    #region DeleteCharLeft

    [Fact]
    public void DeleteCharLeft_Removes_Character_Before_Cursor ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (5, 0);

        bool result = tv.DeleteCharLeft ();

        Assert.True (result);
        Assert.Equal ("Hell", tv.Text);
        Assert.Equal (4, tv.CurrentColumn);
    }

    [Fact]
    public void DeleteCharLeft_At_Beginning_Of_Line_Merges_With_Previous ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = $"Line 1{Environment.NewLine}Line 2" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (0, 1);

        bool result = tv.DeleteCharLeft ();

        Assert.True (result);
        Assert.Equal ("Line 1Line 2", tv.Text);
        Assert.Equal (6, tv.CurrentColumn);
        Assert.Equal (0, tv.CurrentRow);
    }

    [Fact]
    public void DeleteCharLeft_At_Start_Of_First_Line_Does_Nothing ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (0, 0);

        bool result = tv.DeleteCharLeft ();

        Assert.True (result);
        Assert.Equal ("Hello", tv.Text);
    }

    [Fact]
    public void DeleteCharLeft_Respects_ReadOnly ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello", ReadOnly = true };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (5, 0);

        bool result = tv.DeleteCharLeft ();

        Assert.True (result);
        Assert.Equal ("Hello", tv.Text);
    }

    [Fact]
    public void DeleteCharLeft_With_Selection_Removes_Selected_Text ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        // Select "Hello" using Shift+Right
        for (int i = 0; i < 5; i++)
        {
            app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithShift);
        }

        Assert.Equal ("Hello", tv.SelectedText);

        bool result = tv.DeleteCharLeft ();

        Assert.True (result);
        Assert.Equal (" World", tv.Text);
        Assert.False (tv.IsSelecting);
    }

    [Fact]
    public void DeleteCharLeft_Only_Raises_ContentsChanged_Once ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (5, 0);

        int contentsChangedCount = 0;
        tv.ContentsChanged += (_, _) => contentsChangedCount++;

        bool result = tv.DeleteCharLeft ();

        Assert.True (result);
        Assert.Equal ("Hell", tv.Text);
        Assert.Equal (1, contentsChangedCount);
    }

    [Fact]
    public void DeleteCharLeft_With_WordWarp_True_And_Cursor_At_Start_Of_SecondLine_Wont_Return_Negative_LineIndex ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 3, Text = "One line test", WordWrap = true };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (0, 1);

        Assert.True (tv.WordWrap);
        Assert.Equal (2, tv.Lines);
        string line = tv.GetLine (0).Select (cell => cell.Grapheme).Aggregate (string.Empty, (current, next) => current + next);
        Assert.Equal ("One line ", line);
        line = tv.GetLine (1).Select (cell => cell.Grapheme).Aggregate (string.Empty, (current, next) => current + next);
        Assert.Equal ("test", line);

        tv.NewKeyDownEvent (Key.Backspace);
        Assert.Equal (new Point (9, 0), tv.InsertionPoint);
        Assert.Equal (2, tv.Lines);
        line = tv.GetLine (0).Select (cell => cell.Grapheme).Aggregate (string.Empty, (current, next) => current + next);
        Assert.Equal ("One line ", line);
        line = tv.GetLine (1).Select (cell => cell.Grapheme).Aggregate (string.Empty, (current, next) => current + next);
        Assert.Equal ("test", line);
    }

    #endregion

    #region DeleteCharRight

    [Fact]
    public void DeleteCharRight_Removes_Character_At_Cursor ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (0, 0);

        bool result = tv.DeleteCharRight ();

        Assert.True (result);
        Assert.Equal ("ello", tv.Text);
        Assert.Equal (0, tv.CurrentColumn);
    }

    [Fact]
    public void DeleteCharRight_At_End_Of_Line_Merges_With_Next ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = $"Line 1{Environment.NewLine}Line 2" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (6, 0);

        bool result = tv.DeleteCharRight ();

        Assert.True (result);
        Assert.Equal ("Line 1Line 2", tv.Text);
        Assert.Equal (6, tv.CurrentColumn);
        Assert.Equal (0, tv.CurrentRow);
    }

    [Fact]
    public void DeleteCharRight_At_End_Of_Last_Line_Does_Nothing ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (5, 0);

        bool result = tv.DeleteCharRight ();

        Assert.True (result);
        Assert.Equal ("Hello", tv.Text);
    }

    [Fact]
    public void DeleteCharRight_Respects_ReadOnly ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello", ReadOnly = true };
        runnable.Add (tv);
        app.Begin (runnable);

        bool result = tv.DeleteCharRight ();

        Assert.True (result);
        Assert.Equal ("Hello", tv.Text);
    }

    [Fact]
    public void DeleteCharRight_With_Selection_Removes_Selected_Text ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        // Select "Hello" using Shift+Right
        for (int i = 0; i < 5; i++)
        {
            app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithShift);
        }

        Assert.Equal ("Hello", tv.SelectedText);

        bool result = tv.DeleteCharRight ();

        Assert.True (result);
        Assert.Equal (" World", tv.Text);
        Assert.False (tv.IsSelecting);
    }

    [Fact]
    public void DeleteCharRight_Only_Raises_ContentsChanged_Once ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (0, 0);

        int contentsChangedCount = 0;
        tv.ContentsChanged += (_, _) => contentsChangedCount++;

        bool result = tv.DeleteCharRight ();

        Assert.True (result);
        Assert.Equal ("ello", tv.Text);
        Assert.Equal (1, contentsChangedCount);
    }

    #endregion

    #region Copy

    [Fact]
    public void Copy_With_Selection_Copies_To_Internal_State ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        // Select "Hello"
        for (int i = 0; i < 5; i++)
        {
            app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithShift);
        }

        bool result = tv.Copy ();

        Assert.True (result);
        Assert.Equal ("Hello World", tv.Text);
        Assert.Equal ("Hello", app.Clipboard!.GetClipboardData ());
    }

    [Fact]
    public void Copy_Without_Selection_Copies_Current_Line ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        bool result = tv.Copy ();

        Assert.True (result);
        Assert.Equal ("Hello World", tv.Text);
        Assert.Equal ("Hello World", app.Clipboard!.GetClipboardData ());
    }

    [Fact]
    public void Copy_Preserves_Text ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        // Select "World"
        tv.InsertionPoint = new Point (6, 0);

        for (int i = 0; i < 5; i++)
        {
            app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithShift);
        }

        tv.Copy ();

        Assert.Equal ("Hello World", tv.Text);
        Assert.Equal ("World", app.Clipboard!.GetClipboardData ());
    }

    #endregion

    #region Cut

    [Fact]
    public void Cut_With_Selection_Removes_And_Copies ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        // Select "Hello"
        for (int i = 0; i < 5; i++)
        {
            app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithShift);
        }

        bool result = tv.Cut ();

        Assert.True (result);
        Assert.Equal (" World", tv.Text);
        Assert.Equal ("Hello", app.Clipboard!.GetClipboardData ());
        Assert.False (tv.IsSelecting);
    }

    [Fact]
    public void Cut_Respects_ReadOnly ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World", ReadOnly = true };
        runnable.Add (tv);
        app.Begin (runnable);

        // Select "Hello"
        for (int i = 0; i < 5; i++)
        {
            app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithShift);
        }

        tv.Cut ();

        Assert.Equal ("Hello World", tv.Text);
    }

    [Fact]
    public void Cut_Without_Selection_Copies_Empty ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        tv.Cut ();

        // Without selection, GetRegion returns empty, text unchanged
        Assert.Equal ("Hello World", tv.Text);
        Assert.Equal ("", app.Clipboard!.GetClipboardData ());
    }

    #endregion

    #region Paste

    [Fact]
    public void Paste_Inserts_Clipboard_Text ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "World" };
        runnable.Add (tv);
        app.Begin (runnable);

        app.Clipboard!.SetClipboardData ("Hello ");

        bool result = tv.Paste ();

        Assert.True (result);
        Assert.Equal ("Hello World", tv.Text);
        Assert.False (tv.IsSelecting);
    }

    [Fact]
    public void Paste_Respects_ReadOnly ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "World", ReadOnly = true };
        runnable.Add (tv);
        app.Begin (runnable);

        app.Clipboard!.SetClipboardData ("Hello ");

        bool result = tv.Paste ();

        Assert.True (result);
        Assert.Equal ("World", tv.Text);
    }

    [Fact]
    public void Paste_Replaces_Selection ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        // Select "Hello"
        for (int i = 0; i < 5; i++)
        {
            app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithShift);
        }

        app.Clipboard!.SetClipboardData ("Goodbye");

        tv.Paste ();

        Assert.Equal ("Goodbye World", tv.Text);
        Assert.False (tv.IsSelecting);
    }

    #endregion

    #region Undo

    [Fact]
    public void Undo_Reverts_Last_Change ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (5, 0);

        // Delete a character
        tv.DeleteCharLeft ();
        Assert.Equal ("Hell", tv.Text);

        // Undo
        bool result = tv.Undo ();

        Assert.True (result);
        Assert.Equal ("Hello", tv.Text);
    }

    [Fact]
    public void Undo_Respects_ReadOnly ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (5, 0);

        // Delete a character
        tv.DeleteCharLeft ();
        Assert.Equal ("Hell", tv.Text);

        // Set read-only, then undo should be blocked
        tv.ReadOnly = true;
        bool result = tv.Undo ();

        Assert.True (result);
        Assert.Equal ("Hell", tv.Text);
    }

    [Fact]
    public void Undo_On_Unmodified_Text_Does_Nothing ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);

        bool result = tv.Undo ();

        Assert.True (result);
        Assert.Equal ("Hello", tv.Text);
    }

    #endregion

    #region Redo

    [Fact]
    public void Redo_Reapplies_Undone_Change ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (5, 0);

        // Delete, undo, then redo
        tv.DeleteCharLeft ();
        Assert.Equal ("Hell", tv.Text);

        tv.Undo ();
        Assert.Equal ("Hello", tv.Text);

        bool result = tv.Redo ();

        Assert.True (result);
        Assert.Equal ("Hell", tv.Text);
    }

    [Fact]
    public void Redo_Respects_ReadOnly ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (5, 0);

        tv.DeleteCharLeft ();
        tv.Undo ();
        Assert.Equal ("Hello", tv.Text);

        tv.ReadOnly = true;
        bool result = tv.Redo ();

        Assert.True (result);
        Assert.Equal ("Hello", tv.Text);
    }

    [Fact]
    public void Redo_Without_Prior_Undo_Does_Nothing ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);

        bool result = tv.Redo ();

        Assert.True (result);
        Assert.Equal ("Hello", tv.Text);
    }

    #endregion

    #region DeleteAll + Undo integration

    [Fact]
    public void DeleteAll_Then_Undo_Restores_Text ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        tv.DeleteAll ();
        Assert.Equal ("", tv.Text);

        tv.Undo ();

        Assert.Equal ("Hello World", tv.Text);
    }

    [Fact]
    public void DeleteAll_Multiline_Then_Undo_Restores ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = $"Line 1{Environment.NewLine}Line 2{Environment.NewLine}Line 3" };
        runnable.Add (tv);
        app.Begin (runnable);

        tv.DeleteAll ();
        Assert.Equal ("", tv.Text);

        tv.Undo ();

        Assert.Equal ($"Line 1{Environment.NewLine}Line 2{Environment.NewLine}Line 3", tv.Text);
    }

    #endregion

    #region ContentsChanged event

    [Fact]
    public void DeleteAll_Raises_ContentsChanged ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);

        bool eventRaised = false;
        tv.ContentsChanged += (_, _) => eventRaised = true;

        tv.DeleteAll ();

        Assert.True (eventRaised);
    }

    [Fact]
    public void DeleteCharLeft_Raises_ContentsChanged ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (5, 0);

        bool eventRaised = false;
        tv.ContentsChanged += (_, _) => eventRaised = true;

        tv.DeleteCharLeft ();

        Assert.True (eventRaised);
    }

    [Fact]
    public void DeleteCharRight_Raises_ContentsChanged ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);

        bool eventRaised = false;
        tv.ContentsChanged += (_, _) => eventRaised = true;

        tv.DeleteCharRight ();

        Assert.True (eventRaised);
    }

    [Fact]
    public void Cut_Raises_ContentsChanged ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        // Select "Hello"
        for (int i = 0; i < 5; i++)
        {
            app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithShift);
        }

        bool eventRaised = false;
        tv.ContentsChanged += (_, _) => eventRaised = true;

        tv.Cut ();

        Assert.True (eventRaised);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void DeleteCharLeft_Middle_Of_Line ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (3, 0);

        tv.DeleteCharLeft ();

        Assert.Equal ("Helo", tv.Text);
        Assert.Equal (2, tv.CurrentColumn);
    }

    [Fact]
    public void DeleteCharRight_Middle_Of_Line ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (2, 0);

        tv.DeleteCharRight ();

        Assert.Equal ("Helo", tv.Text);
        Assert.Equal (2, tv.CurrentColumn);
    }

    [Fact]
    public void SelectAll_Then_DeleteCharRight_Clears_Text ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello World" };
        runnable.Add (tv);
        app.Begin (runnable);

        tv.SelectAll ();
        tv.DeleteCharRight ();

        Assert.Equal ("", tv.Text);
    }

    [Fact]
    public void Copy_Then_Paste_Appends_Text ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "Hello" };
        runnable.Add (tv);
        app.Begin (runnable);

        // Select all text and copy
        tv.SelectAll ();
        tv.Copy ();

        // Deselect and move to end
        tv.IsSelecting = false;
        tv.InsertionPoint = new Point (5, 0);

        tv.Paste ();

        Assert.Equal ("HelloHello", tv.Text);
    }

    [Fact]
    public void Multiple_Undo_Redo_Cycles ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 40, Height = 10, Text = "ABC" };
        runnable.Add (tv);
        app.Begin (runnable);
        tv.InsertionPoint = new Point (3, 0);

        // Delete C
        tv.DeleteCharLeft ();
        Assert.Equal ("AB", tv.Text);

        // Delete B
        tv.DeleteCharLeft ();
        Assert.Equal ("A", tv.Text);

        // Undo delete B
        tv.Undo ();
        Assert.Equal ("AB", tv.Text);

        // Undo delete C
        tv.Undo ();
        Assert.Equal ("ABC", tv.Text);

        // Redo delete C
        tv.Redo ();
        Assert.Equal ("AB", tv.Text);
    }

    #endregion
}
