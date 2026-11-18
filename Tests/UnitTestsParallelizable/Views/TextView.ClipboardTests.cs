namespace ViewsTests.TextViewTests;

public class TextViewClipboardTests
{
    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlK_Kill_Line_To_Clipboard ()
    {
        // Test that Ctrl+K cuts from cursor to end of line and copies to clipboard
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Ctrl+K should kill the rest of the line
        Assert.True (tv.NewKeyDownEvent (Key.K.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal ("is is the first lin", app.Clipboard!.GetClipboardData ());
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlV_Paste_From_Clipboard ()
    {
        // Test that Ctrl+V pastes from clipboard
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        app.Clipboard!.SetClipboardData ("is is the first lin");
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Ctrl+V should paste from clipboard
        Assert.True (tv.NewKeyDownEvent (Key.V.WithCtrl));
        Assert.Equal ($"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (new (19, 0), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlV_Respects_ReadOnly ()
    {
        // Test that Ctrl+V does not paste when ReadOnly is true
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first",
            ReadOnly = true
        };

        runnable.Add (tv);
        app.Begin (runnable);

        app.Clipboard!.SetClipboardData ("is is the first lin");
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Ctrl+V should not paste when ReadOnly
        Assert.True (tv.NewKeyDownEvent (Key.V.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlC_Copy_Selection_To_Clipboard ()
    {
        // Test that Ctrl+C copies selected text to clipboard
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        // Select text using Shift+End from start
        app.Keyboard.RaiseKeyDownEvent (Key.End.WithShift);

        Assert.Equal (new (19, 0), tv.InsertionPoint);
        Assert.Equal (19, tv.SelectedLength);
        Assert.Equal ("is is the first lin", tv.SelectedText);
        Assert.True (tv.IsSelecting);

        // Ctrl+C should copy to clipboard
        Assert.True (tv.NewKeyDownEvent (Key.C.WithCtrl));

        // Text should be unchanged
        Assert.Equal ($"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (new (19, 0), tv.InsertionPoint);
        Assert.Equal (19, tv.SelectedLength);
        Assert.Equal ("is is the first lin", tv.SelectedText);
        Assert.True (tv.IsSelecting);
        Assert.Equal ("is is the first lin", app.Clipboard.GetClipboardData ());
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlX_Cut_Selection_To_Clipboard ()
    {
        // Test that Ctrl+X cuts selected text to clipboard
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        // Select text using Shift+End from start
        app.Keyboard.RaiseKeyDownEvent (Key.End.WithShift);

        Assert.Equal (new (19, 0), tv.InsertionPoint);
        Assert.Equal (19, tv.SelectedLength);
        Assert.Equal ("is is the first lin", tv.SelectedText);
        Assert.True (tv.IsSelecting);

        // Ctrl+X should cut to clipboard
        Assert.True (tv.NewKeyDownEvent (Key.X.WithCtrl));

        // Text should be cut
        Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.False (tv.IsSelecting);
        Assert.Equal ("is is the first lin", app.Clipboard!.GetClipboardData ());
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlW_Clear_Clipboard ()
    {
        // Test that Ctrl+W clears the clipboard
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        app.Clipboard!.SetClipboardData ("is is the first lin");
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Ctrl+W should clear clipboard
        Assert.True (tv.NewKeyDownEvent (Key.W.WithCtrl));

        Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal ("", app.Clipboard!.GetClipboardData ());
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlX_With_No_Selection_Does_Nothing ()
    {
        // Test that Ctrl+X without selection does nothing
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        app.Clipboard!.SetClipboardData ("");
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Ctrl+X with no selection should do nothing
        Assert.True (tv.NewKeyDownEvent (Key.X.WithCtrl));

        Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal ("", app.Clipboard!.GetClipboardData ());
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlShiftDelete_Deletes_All_Text ()
    {
        // Test that Ctrl+Shift+Delete deletes all text (DeleteAll command)
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Ctrl+Shift+Delete should delete all text
        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl.WithShift));
        Assert.Equal ("", tv.Text);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlShiftBackspace_Kill_Line_Backward_To_Clipboard ()
    {
        // Test that Ctrl+Shift+Backspace kills from start of line to cursor
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        // Navigate to end of first line
        app.Keyboard.RaiseKeyDownEvent (Key.End);
        Assert.Equal (new (19, 0), tv.InsertionPoint);

        // Ctrl+Shift+Backspace should kill from start to cursor
        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl.WithShift));
        Assert.Equal ($"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal ("is is the first lin", app.Clipboard!.GetClipboardData ());
    }
}
