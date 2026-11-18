namespace ViewsTests.TextViewTests;

public class TextViewInputTests
{
    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CanFocus_False_Blocks_Key_Events ()
    {
        // When CanFocus is false, key events should be handled (return true) and not processed
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2, Text = "This is the first line.\nThis is the second line.\nThis is the third line." };

        runnable.Add (tv);
        app.Begin (runnable);

        Assert.True (tv.CanFocus);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // When CanFocus is false, key events should be blocked (return true)
        tv.CanFocus = false;
        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft));
        Assert.False (tv.IsSelecting);
        Assert.Equal (Point.Empty, tv.InsertionPoint); // Position should not change

        // When CanFocus is true, CursorLeft at start position returns false (no movement possible)
        tv.CanFocus = true;
        Assert.False (tv.NewKeyDownEvent (Key.CursorLeft));
        Assert.False (tv.IsSelecting);
        Assert.Equal (Point.Empty, tv.InsertionPoint); // Still at start
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CursorRight_Moves_Insertion_Point_Forward ()
    {
        // Test that CursorRight moves the insertion point forward by one character
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2, Text = "This is the first line.\nThis is the second line.\nThis is the third line." };

        runnable.Add (tv);
        app.Begin (runnable);

        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsSelecting);

        // CursorRight should move from (0,0) to (1,0)
        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlEnd_Navigates_To_End_Of_Document ()
    {
        // Test that Ctrl+End moves to the end of the document
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2, Text = "This is the first line.\nThis is the second line.\nThis is the third line." };

        runnable.Add (tv);
        app.Begin (runnable);

        // Move right one character first
        app.Keyboard.RaiseKeyDownEvent (Key.CursorRight);
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);

        // Ctrl+End should move to end of document
        Assert.True (tv.NewKeyDownEvent (Key.End.WithCtrl));
        Assert.Equal (2, tv.CurrentRow);
        Assert.Equal (23, tv.CurrentColumn);
        Assert.Equal (tv.CurrentColumn, tv.GetCurrentLine ().Count);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CursorRight_At_End_Of_Document_Returns_False ()
    {
        // Test that CursorRight at end of document returns false (no further movement)
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2, Text = "This is the first line.\nThis is the second line.\nThis is the third line." };

        runnable.Add (tv);
        app.Begin (runnable);

        // Navigate to end of document
        app.Keyboard.RaiseKeyDownEvent (Key.End.WithCtrl);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);

        // CursorRight at end should return false
        Assert.False (tv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void Typing_Character_Updates_Text ()
    {
        // Test that typing a character updates the text and moves insertion point
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2, Text = "This is the first line.\nThis is the second line.\nThis is the third line." };

        runnable.Add (tv);
        app.Begin (runnable);

        // Navigate to end and type 'F'
        app.Keyboard.RaiseKeyDownEvent (Key.End.WithCtrl);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.F.WithShift));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.F", tv.Text);
        Assert.Equal (new Point (24, 2), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlZ_Undo_Restores_Previous_Text ()
    {
        // Test that Ctrl+Z undoes the last text change
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2, Text = "This is the first line.\nThis is the second line.\nThis is the third line." };

        runnable.Add (tv);
        app.Begin (runnable);

        // Navigate to end, type 'F', then undo
        app.Keyboard.RaiseKeyDownEvent (Key.End.WithCtrl);
        app.Keyboard.RaiseKeyDownEvent (Key.F.WithShift);

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.F", tv.Text);
        Assert.Equal (new Point (24, 2), tv.InsertionPoint);

        // Undo should restore original text
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void CtrlY_Redo_Reapplies_Undone_Change ()
    {
        // Test that Ctrl+Y redoes the last undone change
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2, Text = "This is the first line.\nThis is the second line.\nThis is the third line." };

        runnable.Add (tv);
        app.Begin (runnable);

        // Navigate to end, type 'F', undo, then redo
        app.Keyboard.RaiseKeyDownEvent (Key.End.WithCtrl);
        app.Keyboard.RaiseKeyDownEvent (Key.F.WithShift);
        app.Keyboard.RaiseKeyDownEvent (Key.Z.WithCtrl);

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);

        // Redo should reapply the 'F' character
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.F", tv.Text);
        Assert.Equal (new Point (24, 2), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    [Fact]
    public void KittyAssociatedText_ShiftedPrintableKey_InsertsAssociatedText ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2 };

        runnable.Add (tv);
        app.Begin (runnable);

        Key kittyKey = new ('!') { AssociatedText = "!" };

        Assert.True (tv.NewKeyDownEvent (kittyKey));
        Assert.Equal ("!", tv.Text);
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);
    }

    [Fact]
    public void KittyAltGrModifierOnly_DoesNotInsertPrivateUseRune ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2 };

        runnable.Add (tv);
        app.Begin (runnable);

        Key kittyKey = new () { ModifierKey = ModifierKey.AltGr };

        Assert.False (tv.NewKeyDownEvent (kittyKey));
        Assert.Equal (string.Empty, tv.Text);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
    }

    [Fact]
    public void KittyAltGr5_InsertsEuroSymbol ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2 };

        runnable.Add (tv);
        app.Begin (runnable);

        Key? key = new KittyKeyboardPattern ().GetKey ("\u001b[8364;1:1u");

        Assert.NotNull (key);
        Assert.True (tv.NewKeyDownEvent (key));
        Assert.Equal ("€", tv.Text);
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);
    }

    [Fact]
    public void KittyAltGrE_InsertsEuroSymbol ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2 };

        runnable.Add (tv);
        app.Begin (runnable);

        Key? key = new KittyKeyboardPattern ().GetKey ("\u001b[8364;1:1u");

        Assert.NotNull (key);
        Assert.True (tv.NewKeyDownEvent (key));
        Assert.Equal ("€", tv.Text);
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);
    }

    [Fact]
    public void KittyAltGr2_InsertsAtSign ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2 };

        runnable.Add (tv);
        app.Begin (runnable);

        Key? key = new KittyKeyboardPattern ().GetKey ("\u001b[64::50;;64u");

        Assert.NotNull (key);
        Assert.True (tv.NewKeyDownEvent (key));
        Assert.Equal ("@", tv.Text);
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);
    }

    [Fact]
    public void InsertText_GraphemeSequence_PreservesSingleGrapheme ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2 };

        runnable.Add (tv);
        app.Begin (runnable);

        tv.InsertText ("👨‍👩‍👧‍👦");

        Assert.Equal ("👨‍👩‍👧‍👦", tv.Text);
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);
    }

    // CoPilot - decomposed from KeyBindings_Command test
    [Fact]
    public void Backspace_Deletes_Previous_Character ()
    {
        // Test that Backspace deletes the character before the cursor
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 10, Height = 2, Text = "This is the first line.\nThis is the second line.\nThis is the third line.F" };

        runnable.Add (tv);
        app.Begin (runnable);

        // Navigate to end
        app.Keyboard.RaiseKeyDownEvent (Key.End.WithCtrl);
        Assert.Equal (new Point (24, 2), tv.InsertionPoint);

        // Backspace should delete the 'F'
        Assert.True (tv.NewKeyDownEvent (Key.Backspace));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
    }

    // Existing tests...
    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void EnterKeyAddsLine_Adds_Line_Or_Fires_Accepted (bool enterKeyAddsLine)
    {
        TextView tv = new () { EnterKeyAddsLine = enterKeyAddsLine };
        var accepted = false;
        tv.Accepting += (_, _) => accepted = true;

        tv.NewKeyDownEvent (Key.Enter);

        if (enterKeyAddsLine)
        {
            Assert.Equal (Environment.NewLine, tv.Text);
            Assert.False (accepted);
        }
        else
        {
            Assert.Equal ("", tv.Text);
            Assert.True (accepted);
        }
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void TabKeyAddsTab_Adds_Tab_Or_Moves_Focus (bool tabKeyAddsTab)
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();
        runnable.CanFocus = true;

        TextView tv = new () { TabKeyAddsTab = tabKeyAddsTab };
        View otherView = new () { CanFocus = true };
        runnable.Add (tv, otherView);

        app.Begin (runnable);

        Assert.True (tv.CanFocus);
        Assert.True (tv.HasFocus);

        app.Keyboard.RaiseKeyDownEvent (Key.Tab);

        if (tabKeyAddsTab)
        {
            Assert.Equal ("\t", tv.Text);
            Assert.True (tv.HasFocus);
        }
        else
        {
            Assert.Equal ("", tv.Text);
            Assert.False (tv.HasFocus);
            Assert.True (otherView.HasFocus);
        }
    }

    [Fact]
    public void Typing_Text_With_Tab_Positions_Cursor_Correctly ()
    {
        TextView tv = new ()
        {
            TabKeyAddsTab = true,
            TabWidth = 4,
            Width = 20,
            Height = 3,
            Text = "a\t1"
        };

        tv.InsertionPoint = new Point (3, 0);
        var expectedVisualColumn = 5;
        int fixedVisualColumn = 1 + (4 - 1 % 4) + 1;

        Assert.Equal (expectedVisualColumn, fixedVisualColumn);
    }

    [Fact]
    public void Typing_Text_With_Tab_And_Wide_Characters_Positions_Cursor_Correctly ()
    {
        TextView tv = new ()
        {
            TabKeyAddsTab = true,
            TabWidth = 4,
            Width = 20,
            Height = 3,
            Text = "??\t1"
        };

        tv.InsertionPoint = new Point (3, 0);
        var expectedVisualColumn = 5;
        int fixedVisualColumn = 2 + (4 - 2 % 4) + 1;

        Assert.Equal (expectedVisualColumn, fixedVisualColumn);
    }

    [Fact]
    public void Typing_Text_With_Tab_And_Multiple_Wide_Characters_Positions_Cursor_Correctly ()
    {
        TextView tv = new ()
        {
            TabKeyAddsTab = true,
            TabWidth = 8,
            Width = 40,
            Height = 3,
            Text = "a??\t?\tX"
        };

        tv.InsertionPoint = new Point (6, 0);
        var expectedVisualColumn = 17;
        int fixedVisualColumn = 1 + 2 + (8 - 3 % 8) + 2 + (8 - 10 % 8) + 1;

        Assert.Equal (expectedVisualColumn, fixedVisualColumn);
    }

    [Theory]
    [InlineData ("abc\t1", 4, 5)]
    [InlineData ("abcd\t1", 4, 9)]
    [InlineData ("a\t1", 4, 5)]
    [InlineData ("ab\t1", 4, 5)]
    public void Tab_Advances_Correctly_To_Next_Tab_Stop (string text, int tabWidth, int expectedColumn)
    {
        TextView tv = new ()
        {
            TabKeyAddsTab = true,
            TabWidth = tabWidth,
            Width = 20,
            Height = 3,
            Text = text
        };

        tv.InsertionPoint = new Point (text.Length, 0);

        var visualColumn = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text [i] == '\t')
            {
                visualColumn += tabWidth - visualColumn % tabWidth;
            }
            else
            {
                visualColumn += text [i].ToString ().GetColumns ();
            }
        }

        Assert.Equal (expectedColumn, visualColumn);
    }

    [Fact]
    public void Typing_Tab_At_End_Of_Line_With_Wrap_Disabled_Should_Scroll_Horizontally_And_Update_Content_Size ()
    {
        TextView tv = new ()
        {
            Width = 5,
            Height = 2,
            Text = "Line1\nLine2\nLine3"
        };
        tv.BeginInit ();
        tv.EndInit ();

        tv.InsertionPoint = new Point (6, 0);
        Assert.Equal (new Point (1, 0), tv.Viewport.Location);
        Assert.Equal (new Point (5, 0), tv.InsertionPoint);
        Assert.Equal (new Size (6, 3), tv.GetContentSize ());

        tv.NewKeyDownEvent (Key.Tab);

        Assert.Equal (new Point (4, 0), tv.Viewport.Location);
        Assert.Equal (new Point (6, 0), tv.InsertionPoint);
        Assert.Equal (new Size (9, 3), tv.GetContentSize ());
    }

    [Fact]
    public void Typing_ShiftTab_At_End_Of_Line_With_Wrap_Disabled_Should_Scroll_Horizontally_And_Update_Content_Size ()
    {
        TextView tv = new ()
        {
            Width = 5,
            Height = 2,
            Text = "Line1\t\nLine2\nLine3"
        };
        tv.BeginInit ();
        tv.EndInit ();

        tv.InsertionPoint = new Point (9, 0);
        Assert.Equal (new Point (4, 0), tv.Viewport.Location);
        Assert.Equal (new Point (6, 0), tv.InsertionPoint);
        Assert.Equal (new Size (9, 3), tv.GetContentSize ());

        tv.NewKeyDownEvent (Key.Tab.WithShift);

        Assert.Equal (new Point (1, 0), tv.Viewport.Location);
        Assert.Equal (new Point (5, 0), tv.InsertionPoint);
        Assert.Equal (new Size (6, 3), tv.GetContentSize ());
    }

    [Fact]
    public void EnterKeyAddsLine_Setter_Should_Not_Scroll_View ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new ()
        {
            X = 1,
            Height = Dim.Fill (2),
            Width = Dim.Fill (2),
            Text = "Heya",
            TabKeyAddsTab = false
        };

        runnable.Add (tv);
        app.Begin (runnable);

        tv.SelectAll ();
        int leftColumnBefore = tv.Viewport.X;

        tv.EnterKeyAddsLine = false;

        Assert.Equal (leftColumnBefore, tv.Viewport.X);
    }

    [Fact]
    public void EnterKeyAddsLine_Toggling_Does_Not_Reset_Cursor_Position ()
    {
        using IApplication app = Application.Create ();
        using Runnable<bool> runnable = new ();

        TextView tv = new () { Width = 30, Height = 5, Text = $"This is the second line.{Environment.NewLine}This is the third " };

        runnable.Add (tv);
        app.Begin (runnable);

        app.Keyboard.RaiseKeyDownEvent (Key.End.WithCtrl);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.Equal ($"This is the second line.{Environment.NewLine}This is the third ", tv.Text);

        tv.EnterKeyAddsLine = false;
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsSelecting);
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.False (tv.EnterKeyAddsLine);
        Assert.False (tv.Multiline);

        // Pressing Enter should fire Accepted event, not add a new line
        app.Keyboard.RaiseKeyDownEvent (Key.Enter);
        Assert.Equal ($"This is the second line.{Environment.NewLine}This is the third ", tv.Text);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.False (tv.IsSelecting);

        tv.EnterKeyAddsLine = true;
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.True (tv.EnterKeyAddsLine);
        Assert.True (tv.Multiline);

        app.Keyboard.RaiseKeyDownEvent (Key.Enter);
        Assert.Equal ($"This is the second line.{Environment.NewLine}This is the third {Environment.NewLine}", tv.Text);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.False (tv.IsSelecting);
        Assert.True (tv.EnterKeyAddsLine);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that common printable characters including space, letters, digits,
    ///     and punctuation are correctly inserted as text input.
    /// </summary>
    [Fact]
    public void CommonInput_AllPrintableCharacters_InsertedAsText ()
    {
        Runnable top = new ();
        TextView tv = new () { Width = 40, Height = 2 };
        top.Add (tv);
        tv.SetFocus ();

        // Type a string with letters, digits, space, and punctuation
        foreach (char c in "Hello World 123!@#")
        {
            top.NewKeyDownEvent (c);
        }

        Assert.Equal ("Hello World 123!@#", tv.Text);

        top.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that the space key is not consumed by the default View Command.Activate binding.
    ///     TextView removes the Key.Space binding so space can be typed as text.
    /// </summary>
    [Fact]
    public void Space_IsInsertedAsText_NotConsumedByActivate ()
    {
        Runnable top = new ();
        TextView tv = new () { Width = 20, Height = 2 };
        top.Add (tv);
        tv.SetFocus ();

        top.NewKeyDownEvent ((Key)'a');
        top.NewKeyDownEvent (Key.Space);
        top.NewKeyDownEvent ((Key)'b');

        Assert.Equal ("a b", tv.Text);

        top.Dispose ();
    }

    // Note: TextView disables HotKeySpecifier (sets it to '\xffff') in its constructor,
    // so unlike TextField, it does not need a HotKey_WhenFocused_InsertsText test.
    // The HotKey-eating-characters scenario only applies to TextField (e.g. FileDialog "_Enter Path").
}
