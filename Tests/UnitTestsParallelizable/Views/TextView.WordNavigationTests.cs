namespace ViewsTests.TextViewTests;

public class TextViewWordNavigationTests
{
    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlLeft_Moves_To_Previous_Word ()
    {
        // Test that Ctrl+Left moves cursor to start of previous word
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        // Navigate to end of document
        app.Keyboard.RaiseKeyDownEvent (Key.End.WithCtrl);
        Assert.Equal (new (28, 2), tv.InsertionPoint);

        // Ctrl+Left should move to start of "first"
        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft.WithCtrl));
        Assert.Equal (new (23, 2), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);

        // Ctrl+Left again should move to after the period
        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft.WithCtrl));
        Assert.Equal (new (22, 2), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);

        // Ctrl+Left again should move to start of "line"
        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft.WithCtrl));
        Assert.Equal (new (18, 2), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlRight_Moves_To_Next_Word ()
    {
        // Test that Ctrl+Right moves cursor to start of next word
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        // Start at beginning, navigate to line 2, column 8 ("the third line.first")
        app.Keyboard.RaiseKeyDownEvent (Key.CursorDown);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorDown);
        app.Keyboard.RaiseKeyDownEvent (Key.End);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft.WithCtrl);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft.WithCtrl);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft.WithCtrl);
        Assert.Equal (new (18, 2), tv.InsertionPoint);

        // Ctrl+Right should move to after "line."
        Assert.True (tv.NewKeyDownEvent (Key.CursorRight.WithCtrl));
        Assert.Equal (new (22, 2), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);

        // Ctrl+Right again to start of "first"
        Assert.True (tv.NewKeyDownEvent (Key.CursorRight.WithCtrl));
        Assert.Equal (new (23, 2), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);

        // Ctrl+Right again to end
        Assert.True (tv.NewKeyDownEvent (Key.CursorRight.WithCtrl));
        Assert.Equal (new (28, 2), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlShiftLeft_Selects_To_Previous_Word ()
    {
        // Test that Ctrl+Shift+Left selects text to start of previous word
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        // Navigate to specific position (18,2) - start of "line"
        app.Keyboard.RaiseKeyDownEvent (Key.CursorDown);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorDown);
        app.Keyboard.RaiseKeyDownEvent (Key.End);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft.WithCtrl);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft.WithCtrl);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft.WithCtrl);
        Assert.Equal (new (18, 2), tv.InsertionPoint);

        // Ctrl+Shift+Left should select to previous word start (12,2)
        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft.WithShift.WithCtrl));
        Assert.Equal (new (12, 2), tv.InsertionPoint);
        Assert.Equal (6, tv.SelectedLength);
        Assert.Equal ("third ", tv.SelectedText);
        Assert.True (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlShiftRight_Selects_To_Next_Word ()
    {
        // Test that Ctrl+Shift+Right selects text to start of next word
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        // Navigate to position (12,2) - start of "third"
        app.Keyboard.RaiseKeyDownEvent (Key.CursorDown);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorDown);
        app.Keyboard.RaiseKeyDownEvent (Key.End);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft.WithCtrl);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft.WithCtrl);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft.WithCtrl);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft.WithCtrl);
        Assert.Equal (new (12, 2), tv.InsertionPoint);

        // Ctrl+Shift+Right should select to next word (18,2)
        Assert.True (tv.NewKeyDownEvent (Key.CursorRight.WithShift.WithCtrl));
        Assert.Equal (new (18, 2), tv.InsertionPoint);
        Assert.Equal (6, tv.SelectedLength);
        Assert.Equal ("third ", tv.SelectedText);
        Assert.True (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlDelete_Deletes_Next_Word ()
    {
        // Test that Ctrl+Delete deletes from cursor to end of next word
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        // Start at beginning
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Ctrl+Delete should delete "This "
        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ($"is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlBackspace_Deletes_Previous_Word ()
    {
        // Test that Ctrl+Backspace deletes from start of previous word to cursor
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"This is the second line.{Environment.NewLine}This is the third "
        };

        runnable.Add (tv);
        app.Begin (runnable);

        // Navigate to end
        app.Keyboard.RaiseKeyDownEvent (Key.End.WithCtrl);
        Assert.Equal (new (18, 1), tv.InsertionPoint);

        // Ctrl+Backspace should delete "third "
        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ($"This is the second line.{Environment.NewLine}This is the ", tv.Text);
        Assert.Equal (new (12, 1), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);

        // Ctrl+Backspace again should delete "the "
        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ($"This is the second line.{Environment.NewLine}This is ", tv.Text);
        Assert.Equal (new (8, 1), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);

        // Ctrl+Backspace again should delete "is "
        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ($"This is the second line.{Environment.NewLine}This ", tv.Text);
        Assert.Equal (new (5, 1), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }
}
