namespace ViewsTests.TextViewTests;

public class TextViewDeletionTests
{
    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void Delete_Key_Removes_Character_At_Cursor ()
    {
        // Test that Delete key removes the character at cursor position
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Delete should remove 'T' at position 0
        Assert.True (tv.NewKeyDownEvent (Key.Delete));
        Assert.Equal ($"his is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsSelecting);

        // Delete again should remove 'h'
        Assert.True (tv.NewKeyDownEvent (Key.Delete));
        Assert.Equal ($"is is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlD_Deletes_Character_At_Cursor ()
    {
        // Test that Ctrl+D deletes character at cursor (same as Delete)
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"is is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Ctrl+D should remove 'i'
        Assert.True (tv.NewKeyDownEvent (Key.D.WithCtrl));
        Assert.Equal ($"s is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void Backspace_Multiple_Characters ()
    {
        // Test backspace at end of line removing multiple characters
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"is is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first"
        };

        runnable.Add (tv);
        app.Begin (runnable);

        // Navigate to end of first line
        app.Keyboard.RaiseKeyDownEvent (Key.End);
        Assert.Equal (new (21, 0), tv.InsertionPoint);

        // Backspace should delete the '.',
        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ($"is is the first line{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (new (20, 0), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);

        // Backspace again should delete 'e'
        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ($"is is the first lin{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.first", tv.Text);
        Assert.Equal (new (19, 0), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void Delete_With_Selection_Removes_Selection ()
    {
        // Test that Delete with selection removes the selected text
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            Width = 10,
            Height = 2,
            Text = $"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line."
        };

        runnable.Add (tv);
        app.Begin (runnable);

        // Select some text
        app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithShift);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithShift);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithShift);
        app.Keyboard.RaiseKeyDownEvent (Key.CursorRight.WithShift);

        Assert.Equal (4, tv.SelectedLength);
        Assert.Equal ("This", tv.SelectedText);
        Assert.True (tv.IsSelecting);

        // Delete should remove the selection
        Assert.True (tv.NewKeyDownEvent (Key.Delete));

        Assert.Equal ($" is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.False (tv.IsSelecting);
    }
}
