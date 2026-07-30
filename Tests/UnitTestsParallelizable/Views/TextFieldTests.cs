using System.Text;
using UnitTests;

namespace ViewsTests;

public class TextFieldTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Cancel_TextChanging_ThenBackspace ()
    {
        var tf = new TextField ();
        tf.SetFocus ();
        tf.NewKeyDownEvent (Key.A.WithShift);
        Assert.Equal ("A", tf.Text);

        // cancel the next keystroke
        tf.TextChanging += (s, e) => e.Handled = e.Result == "AB";
        tf.NewKeyDownEvent (Key.B.WithShift);

        // B was canceled so should just be A
        Assert.Equal ("A", tf.Text);

        // now delete the A
        tf.NewKeyDownEvent (Key.Backspace);

        Assert.Equal ("", tf.Text);
    }

    [Fact]
    public void HistoryText_IsDirty_ClearHistoryChanges ()
    {
        var text = "Testing";
        var tf = new TextField { Text = text };
        tf.BeginInit ();
        tf.EndInit ();

        Assert.Equal (text, tf.Text);
        tf.ClearHistoryChanges ();
        Assert.False (tf.IsDirty);

        Assert.True (tf.NewKeyDownEvent (Key.A.WithShift));
        Assert.Equal ($"{text}A", tf.Text);
        Assert.True (tf.IsDirty);
    }

    [Fact]
    public void Space_Does_Not_Raise_Selected ()
    {
        TextField tf = new ();

        tf.Activating += (sender, args) => Assert.Fail ("Activating should not be raised.");

        Runnable top = new ();
        top.Add (tf);
        tf.SetFocus ();
        top.NewKeyDownEvent (Key.Space);

        top.Dispose ();
    }

    [Fact]
    public void Enter_Does_Not_Raise_Selected ()
    {
        TextField tf = new ();

        var activatingCount = 0;
        tf.Activating += (sender, args) => activatingCount++;

        Runnable top = new ();
        top.Add (tf);
        tf.SetFocus ();
        top.NewKeyDownEvent (Key.Enter);

        Assert.Equal (0, activatingCount);

        top.Dispose ();
    }

    [Fact]
    public void Enter_Raises_Accepted ()
    {
        TextField tf = new ();

        var acceptedCount = 0;
        tf.Accepting += (sender, args) => acceptedCount++;

        Runnable top = new ();
        top.Add (tf);
        tf.SetFocus ();
        top.NewKeyDownEvent (Key.Enter);

        Assert.Equal (1, acceptedCount);

        top.Dispose ();
    }

    [Fact]
    public void HotKey_Command_SetsFocus ()
    {
        var view = new TextField ();

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.InvokeCommand (Command.HotKey);
        Assert.True (view.HasFocus);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     When a TextField has a HotKey matching a typed character (e.g. 'E' from Title "_Enter Path"),
    ///     pressing that character while the TextField is focused should insert it as text, not activate the hotkey.
    /// </summary>
    [Fact]
    public void HotKey_WhenFocused_InsertsText_DoesNotActivate ()
    {
        Runnable top = new ();
        TextField tf = new () { Title = "_Enter Path", Width = 30 };
        top.Add (tf);
        tf.SetFocus ();
        Assert.True (tf.HasFocus);

        // HotKey should be 'E' from the title
        Assert.Equal (Key.E, tf.HotKey);

        // Clear any selection from focus
        tf.ClearAllSelection ();
        tf.InsertionPoint = 0;

        // Type "hello" which contains 'e' (the HotKey character)
        foreach (char c in "hello")
        {
            top.NewKeyDownEvent (c);
        }

        Assert.Equal ("hello", tf.Text);

        top.Dispose ();
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var view = new TextField ();
        var accepted = false;
        view.Accepting += OnAccept;
        view.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void OnAccept (object? sender, CommandEventArgs e) => accepted = true;
    }

    [Fact]
    public void Accept_Command_Fires_Accepting ()
    {
        var view = new TextField ();

        var accepting = false;
        view.Accepting += Accepting;
        view.InvokeCommand (Command.Accept);
        Assert.True (accepting);

        return;

        void Accepting (object? sender, CommandEventArgs e) => accepting = true;
    }

    [Fact]
    public void Enter_Enables_Default_Button_Accept ()
    {
        var superView = new Window { Id = "superView" };
        var tf = new TextField { Id = "tf" };

        var button = new Button { Id = "button", IsDefault = true };

        superView.Add (tf, button);

        var buttonAccepting = 0;
        button.Accepting += ButtonAccepting;

        tf.SetFocus ();
        Assert.True (tf.HasFocus);

        superView.NewKeyDownEvent (Key.Enter);
        Assert.Equal (1, buttonAccepting);

        button.SetFocus ();
        superView.NewKeyDownEvent (Key.Enter);
        Assert.Equal (2, buttonAccepting);

        return;

        void ButtonAccepting (object? sender, CommandEventArgs e) => buttonAccepting++;
    }

    [Fact]
    public void Accept_Command_Handles_Properly ()
    {
        var view = new TextField ();

        var tfAcceptingInvoked = false;
        var handle = false;
        view.Accepting += TextViewAccepting;

        view.InvokeCommand (Command.Accept);
        Assert.True (tfAcceptingInvoked);

        tfAcceptingInvoked = false;
        handle = true;
        view.Accepting += TextViewAccepting;
        view.InvokeCommand (Command.Accept);
        Assert.True (tfAcceptingInvoked);

        return;

        void TextViewAccepting (object? sender, CommandEventArgs e)
        {
            tfAcceptingInvoked = true;
            e.Handled = handle;
        }
    }

    [Fact]
    public void OnEnter_Does_Not_Throw_If_Not_IsInitialized_SetCursorVisibility ()
    {
        var top = new Runnable ();
        var tf = new TextField { Width = 10 };
        top.Add (tf);

        Exception? exception = Record.Exception (() => tf.SetFocus ());
        Assert.Null (exception);
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
        TextField tf = new () { Width = 40 };
        top.Add (tf);
        tf.SetFocus ();
        tf.ClearAllSelection ();
        tf.InsertionPoint = 0;

        // Type a string with letters, digits, space, and punctuation
        foreach (char c in "Hello World 123!@#")
        {
            top.NewKeyDownEvent (c);
        }

        Assert.Equal ("Hello World 123!@#", tf.Text);

        top.Dispose ();
    }

    [Fact]
    public void KittyAssociatedText_ShiftedPrintableKey_InsertsAssociatedText ()
    {
        Runnable top = new ();
        TextField tf = new () { Width = 10 };
        top.Add (tf);
        tf.SetFocus ();
        tf.ClearAllSelection ();
        tf.InsertionPoint = 0;

        Key kittyKey = new ('!') { AssociatedText = "!" };

        Assert.True (top.NewKeyDownEvent (kittyKey));
        Assert.Equal ("!", tf.Text);

        top.Dispose ();
    }

    [Fact]
    public void KittyAltGrModifierOnly_DoesNotInsertPrivateUseRune ()
    {
        Runnable top = new ();
        TextField tf = new () { Width = 10 };
        top.Add (tf);
        tf.SetFocus ();
        tf.ClearAllSelection ();
        tf.InsertionPoint = 0;

        Key kittyKey = new () { ModifierKey = ModifierKey.AltGr };

        Assert.False (top.NewKeyDownEvent (kittyKey));
        Assert.Equal (string.Empty, tf.Text);

        top.Dispose ();
    }

    [Fact]
    public void KittyAltGr5_InsertsEuroSymbol ()
    {
        Runnable top = new ();
        TextField tf = new () { Width = 10 };
        top.Add (tf);
        tf.SetFocus ();
        tf.ClearAllSelection ();
        tf.InsertionPoint = 0;

        Key? key = new KittyKeyboardPattern ().GetKey ("\u001b[8364;1:1u");

        Assert.NotNull (key);
        Assert.True (top.NewKeyDownEvent (key));
        Assert.Equal ("€", tf.Text);
        Assert.Equal (1, tf.InsertionPoint);

        top.Dispose ();
    }

    [Fact]
    public void KittyAltGrE_InsertsEuroSymbol ()
    {
        Runnable top = new ();
        TextField tf = new () { Width = 10 };
        top.Add (tf);
        tf.SetFocus ();
        tf.ClearAllSelection ();
        tf.InsertionPoint = 0;

        Key? key = new KittyKeyboardPattern ().GetKey ("\u001b[8364;1:1u");

        Assert.NotNull (key);
        Assert.True (top.NewKeyDownEvent (key));
        Assert.Equal ("€", tf.Text);
        Assert.Equal (1, tf.InsertionPoint);

        top.Dispose ();
    }

    [Fact]
    public void KittyAltGr2_InsertsAtSign ()
    {
        Runnable top = new ();
        TextField tf = new () { Width = 10 };
        top.Add (tf);
        tf.SetFocus ();
        tf.ClearAllSelection ();
        tf.InsertionPoint = 0;

        Key? key = new KittyKeyboardPattern ().GetKey ("\u001b[64::50;;64u");

        Assert.NotNull (key);
        Assert.True (top.NewKeyDownEvent (key));
        Assert.Equal ("@", tf.Text);
        Assert.Equal (1, tf.InsertionPoint);

        top.Dispose ();
    }

    [Fact]
    public void KittyAltModifiedPrintableKey_DoesNotInsertAssociatedText ()
    {
        Runnable top = new ();
        TextField tf = new () { Width = 10 };
        top.Add (tf);
        tf.SetFocus ();
        tf.ClearAllSelection ();
        tf.InsertionPoint = 0;

        Key? key = new KittyKeyboardPattern ().GetKey ("\u001b[116;3;116u");

        Assert.NotNull (key);
        Assert.False (top.NewKeyDownEvent (key));
        Assert.Equal (string.Empty, tf.Text);

        top.Dispose ();
    }

    [Fact]
    public void ShiftedDigitKey_WithoutKittyMetadata_InsertsBaseDigit ()
    {
        Runnable top = new ();
        TextField tf = new () { Width = 10 };
        top.Add (tf);
        tf.SetFocus ();
        tf.ClearAllSelection ();
        tf.InsertionPoint = 0;

        Assert.True (top.NewKeyDownEvent (Key.D1.WithShift));
        Assert.Equal ("1", tf.Text);

        top.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that the space key is not consumed by the default View Command.Activate binding.
    ///     TextField removes the Key.Space binding so space can be typed as text.
    /// </summary>
    [Fact]
    public void Space_IsInsertedAsText_NotConsumedByActivate ()
    {
        Runnable top = new ();
        TextField tf = new () { Width = 20 };
        top.Add (tf);
        tf.SetFocus ();
        tf.ClearAllSelection ();

        top.NewKeyDownEvent ((Key)'a');
        top.NewKeyDownEvent (Key.Space);
        top.NewKeyDownEvent ((Key)'b');

        Assert.Equal ("a b", tf.Text);

        top.Dispose ();
    }

    [Fact]
    public void Backspace_From_End ()
    {
        var tf = new TextField { Text = "ABC" };
        tf.SetFocus ();
        Assert.Equal ("ABC", tf.Text);
        tf.BeginInit ();
        tf.EndInit ();

        // Clear the automatic selection from focus
        tf.ClearAllSelection ();

        Assert.Equal (3, tf.InsertionPoint);

        // now delete the C
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("AB", tf.Text);
        Assert.Equal (2, tf.InsertionPoint);

        // then delete the B
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("A", tf.Text);
        Assert.Equal (1, tf.InsertionPoint);

        // then delete the A
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("", tf.Text);
        Assert.Equal (0, tf.InsertionPoint);
    }

    [Fact]
    public void Backspace_From_Middle ()
    {
        var tf = new TextField { Text = "ABC" };
        tf.SetFocus ();

        // Clear the automatic selection from focus
        tf.ClearAllSelection ();

        tf.InsertionPoint = 2;
        Assert.Equal ("ABC", tf.Text);

        // now delete the B
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("AC", tf.Text);

        // then delete the A
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("C", tf.Text);

        // then delete nothing
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("C", tf.Text);

        // now delete the C
        tf.InsertionPoint = 1;
        tf.NewKeyDownEvent (Key.Backspace);
        Assert.Equal ("", tf.Text);
    }

    [Fact]
    public void KeyDown_Handled_Prevents_Input ()
    {
        var tf = new TextField ();
        tf.KeyDown += HandleJKey;

        tf.NewKeyDownEvent (Key.A);
        Assert.Equal ("a", tf.Text);

        // SuppressKey suppresses the 'j' key
        tf.NewKeyDownEvent (Key.J);
        Assert.Equal ("a", tf.Text);

        tf.KeyDown -= HandleJKey;

        // Now that the delegate has been removed we can type j again
        tf.NewKeyDownEvent (Key.J);
        Assert.Equal ("aj", tf.Text);

        return;

        void HandleJKey (object? s, Key arg)
        {
            if (arg.AsRune == new Rune ('j'))
            {
                arg.Handled = true;
            }
        }
    }

    [InlineData ("a")] // Lower than selection
    [InlineData ("aaaaaaaaaaa")] // Greater than selection
    [InlineData ("aaaa")] // Equal than selection
    [Theory]
    public void SetTextAndMoveCursorToEnd_WhenExistingSelection (string newText)
    {
        var tf = new TextField ();
        tf.Text = "fish";
        tf.InsertionPoint = tf.Text.Length;

        tf.NewKeyDownEvent (Key.CursorLeft);

        tf.NewKeyDownEvent (Key.CursorLeft.WithShift);
        tf.NewKeyDownEvent (Key.CursorLeft.WithShift);

        Assert.Equal (1, tf.InsertionPoint);
        Assert.Equal (2, tf.SelectedLength);
        Assert.Equal ("is", tf.SelectedText);

        tf.Text = newText;
        tf.InsertionPoint = tf.Text.Length;

        Assert.Equal (newText.Length, tf.InsertionPoint);
        Assert.Equal (0, tf.SelectedLength);
        Assert.Null (tf.SelectedText);
    }

    [Fact]
    public void SpaceHandling ()
    {
        var tf = new TextField { Width = 10, Text = " " };

        var ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonDoubleClicked };

        tf.NewMouseEvent (ev);
        Assert.Equal (1, tf.SelectedLength);

        ev = new Mouse { Position = new Point (1, 0), Flags = MouseFlags.LeftButtonDoubleClicked };

        tf.NewMouseEvent (ev);
        Assert.Equal (1, tf.SelectedLength);
    }

    [Fact]
    public void WordBackward_WordForward_Mixed ()
    {
        var tf = new TextField { Width = 30, Text = "Test with0. and!.?;-@+" };
        tf.BeginInit ();
        tf.EndInit ();

        Assert.False (tf.UseSameRuneTypeForWords);
        Assert.Equal (22, tf.InsertionPoint);

        tf.NewKeyDownEvent (Key.CursorLeft.WithCtrl);
        Assert.Equal (15, tf.InsertionPoint);
        tf.NewKeyDownEvent (Key.CursorLeft.WithCtrl);
        Assert.Equal (12, tf.InsertionPoint);
        tf.NewKeyDownEvent (Key.CursorLeft.WithCtrl);
        Assert.Equal (10, tf.InsertionPoint);
        tf.NewKeyDownEvent (Key.CursorLeft.WithCtrl);
        Assert.Equal (5, tf.InsertionPoint);
        tf.NewKeyDownEvent (Key.CursorLeft.WithCtrl);
        Assert.Equal (0, tf.InsertionPoint);

        tf.NewKeyDownEvent (Key.CursorRight.WithCtrl);
        Assert.Equal (5, tf.InsertionPoint);
        tf.NewKeyDownEvent (Key.CursorRight.WithCtrl);
        Assert.Equal (10, tf.InsertionPoint);
        tf.NewKeyDownEvent (Key.CursorRight.WithCtrl);
        Assert.Equal (12, tf.InsertionPoint);
        tf.NewKeyDownEvent (Key.CursorRight.WithCtrl);
        Assert.Equal (15, tf.InsertionPoint);
        tf.NewKeyDownEvent (Key.CursorRight.WithCtrl);
        Assert.Equal (22, tf.InsertionPoint);
    }

    [Fact]
    public void WordBackward_WordForward_SelectedText_With_Accent ()
    {
        var text = "Les Misérables movie.";
        var tf = new TextField { Width = 30, Text = text };
        tf.BeginInit ();
        tf.EndInit ();

        Assert.Equal (21, text.Length);
        Assert.Equal (21, tf.Text.GetRuneCount ());
        Assert.Equal (21, tf.Text.GetColumns ());
        Assert.Equal (21, GraphemeHelper.GetGraphemeCount (tf.Text));

        List<Rune> runes = tf.Text.ToRuneList ();
        List<string> graphemes = GraphemeHelper.GetGraphemes (tf.Text).ToList ();
        Assert.Equal (21, runes.Count);
        Assert.Equal (21, tf.Text.Length);
        Assert.Equal (21, graphemes.Count);

        for (var i = 0; i < runes.Count; i++)
        {
            char c = text [i];
            var rune = (char)runes [i].Value;
            Assert.Equal (c, rune);
        }

        for (var i = 0; i < graphemes.Count; i++)
        {
            string grapheme = graphemes [i];
            var rune = runes [i].ToString ();
            Assert.Equal (grapheme, rune);
        }

        var idx = 15;
        Assert.Equal ('m', text [idx]);
        Assert.Equal ('m', (char)runes [idx].Value);
        Assert.Equal ("m", runes [idx].ToString ());
        Assert.Equal ("m", graphemes [idx]);

        Assert.True (tf.NewMouseEvent (new Mouse { Position = new Point (idx, 1), Flags = MouseFlags.LeftButtonDoubleClicked, View = tf }));
        Assert.Equal ("movie", tf.SelectedText);

        Assert.True (tf.NewMouseEvent (new Mouse { Position = new Point (idx + 1, 1), Flags = MouseFlags.LeftButtonDoubleClicked, View = tf }));
        Assert.Equal ("movie", tf.SelectedText);
    }

    [Fact]
    public void WordBackward_WordForward_SelectedText_With_SurrogatePair ()
    {
        var text = "Les Mis🍎rables movie.";
        var tf = new TextField { Width = 30, Text = text };
        tf.BeginInit ();
        tf.EndInit ();

        Assert.Equal (22, text.Length);
        Assert.Equal (21, tf.Text.GetRuneCount ());
        Assert.Equal (22, tf.Text.GetColumns ());
        Assert.Equal (21, GraphemeHelper.GetGraphemeCount (tf.Text));

        List<Rune> runes = tf.Text.ToRuneList ();
        List<string> graphemes = GraphemeHelper.GetGraphemes (tf.Text).ToList ();
        Assert.Equal (21, runes.Count);
        Assert.Equal (22, tf.Text.Length);
        Assert.Equal (21, graphemes.Count);

        Exception? exception = Record.Exception (() =>
                                                 {
                                                     for (var i = 0; i < runes.Count; i++)
                                                     {
                                                         char c = text [i];
                                                         var rune = (char)runes [i].Value;
                                                         Assert.Equal (c, rune);
                                                     }
                                                 });
        Assert.NotNull (exception);

        for (var i = 0; i < graphemes.Count; i++)
        {
            string grapheme = graphemes [i];
            var rune = runes [i].ToString ();
            Assert.Equal (grapheme, rune);
        }

        var idx = 15;
        Assert.Equal (' ', text [idx]);
        Assert.Equal ('m', (char)runes [idx].Value);
        Assert.Equal ("m", runes [idx].ToString ());
        Assert.Equal ("m", graphemes [idx]);

        // There is a wide glyph, so it's needed to add +1 to the index to select the word with double click
        Assert.True (tf.NewMouseEvent (new Mouse { Position = new Point (idx + 1, 1), Flags = MouseFlags.LeftButtonDoubleClicked, View = tf }));
        Assert.Equal ("movie", tf.SelectedText);

        Assert.True (tf.NewMouseEvent (new Mouse { Position = new Point (idx + 2, 1), Flags = MouseFlags.LeftButtonDoubleClicked, View = tf }));
        Assert.Equal ("movie", tf.SelectedText);
    }

    [Fact]
    public void WordBackward_WordForward_SelectedText_With_SurrogatePairsAndZWJ ()
    {
        var text = "Les Mis👨‍👩‍👧rables movie.";
        var tf = new TextField { Width = 30, Text = text };
        tf.BeginInit ();
        tf.EndInit ();

        Assert.Equal (28, text.Length);
        Assert.Equal (25, tf.Text.GetRuneCount ());
        Assert.Equal (22, tf.Text.GetColumns ());
        Assert.Equal (21, GraphemeHelper.GetGraphemeCount (tf.Text));

        List<Rune> runes = tf.Text.ToRuneList ();
        List<string> graphemes = GraphemeHelper.GetGraphemes (tf.Text).ToList ();
        Assert.Equal (25, runes.Count);
        Assert.Equal (28, tf.Text.Length);
        Assert.Equal (21, graphemes.Count);

        Exception? exception = Record.Exception (() =>
                                                 {
                                                     for (var i = 0; i < runes.Count; i++)
                                                     {
                                                         char c = text [i];
                                                         var rune = (char)runes [i].Value;
                                                         Assert.Equal (c, rune);
                                                     }
                                                 });
        Assert.NotNull (exception);

        exception = Record.Exception (() =>
                                      {
                                          for (var i = 0; i < graphemes.Count; i++)
                                          {
                                              string grapheme = graphemes [i];
                                              var rune = runes [i].ToString ();
                                              Assert.Equal (grapheme, rune);
                                          }
                                      });
        Assert.NotNull (exception);

        var idx = 15;
        Assert.Equal ('r', text [idx]);
        Assert.Equal ('l', (char)runes [idx].Value);
        Assert.Equal ("l", runes [idx].ToString ());
        Assert.Equal ("m", graphemes [idx]);

        // There is a wide glyph, so it's needed to add +1 to the index to select the word with double click
        Assert.True (tf.NewMouseEvent (new Mouse { Position = new Point (idx + 1, 1), Flags = MouseFlags.LeftButtonDoubleClicked, View = tf }));
        Assert.Equal ("movie", tf.SelectedText);

        Assert.True (tf.NewMouseEvent (new Mouse { Position = new Point (idx + 2, 1), Flags = MouseFlags.LeftButtonDoubleClicked, View = tf }));
        Assert.Equal ("movie", tf.SelectedText);
    }

    [Fact]
    public void Autocomplete_Popup_Added_To_SuperView_On_Init ()
    {
        View superView = new () { CanFocus = true };

        TextField t = new ();

        superView.Add (t);
        Assert.Single (superView.SubViews);

        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (2, superView.SubViews.Count);
    }

    [Fact]
    public void Autocomplete__Added_To_SuperView_On_Add ()
    {
        View superView = new () { CanFocus = true, Id = "superView" };

        superView.BeginInit ();
        superView.EndInit ();
        Assert.Empty (superView.SubViews);

        TextField t = new () { Id = "t" };

        superView.Add (t);

        Assert.Equal (2, superView.SubViews.Count);
    }

    [Fact]
    public void Right_CursorAtEnd_WithSelection_ShouldClearSelection ()
    {
        var tf = new TextField { Text = "Hello" };
        tf.SetFocus ();
        tf.SelectAll ();
        tf.InsertionPoint = 5;

        // When there is selected text and the cursor is at the end of the text field
        Assert.Equal ("Hello", tf.SelectedText);

        // Pressing right should not move focus, instead it should clear selection
        Assert.True (tf.NewKeyDownEvent (Key.CursorRight));
        Assert.Null (tf.SelectedText);

        // Now that the selection is cleared another right keypress should move focus
        Assert.False (tf.NewKeyDownEvent (Key.CursorRight));
    }

    [Fact]
    public void Left_CursorAtStart_WithSelection_ShouldClearSelection ()
    {
        var tf = new TextField { Text = "Hello" };
        tf.SetFocus ();

        // Clear the automatic selection from focus
        tf.ClearAllSelection ();

        tf.InsertionPoint = 2;
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft.WithShift));
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft.WithShift));

        // When there is selected text and the cursor is at the start of the text field
        Assert.Equal ("He", tf.SelectedText);

        // Pressing left should not move focus, instead it should clear selection
        Assert.True (tf.NewKeyDownEvent (Key.CursorLeft));
        Assert.Null (tf.SelectedText);

        // When clearing selected text with left the cursor should be at the start of the selection
        Assert.Equal (0, tf.InsertionPoint);

        // Now that the selection is cleared another left keypress should move focus
        Assert.False (tf.NewKeyDownEvent (Key.CursorLeft));
    }

    [Fact]
    public void Autocomplete_Visible_False_By_Default ()
    {
        View superView = new () { CanFocus = true };

        TextField t = new ();

        superView.Add (t);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (2, superView.SubViews.Count);

        Assert.True (t.Visible);
        Assert.False (t.Autocomplete?.Visible);
    }

    [Fact]
    public void InsertText_Bmp_SurrogatePair_Non_Bmp_Invalid_SurrogatePair ()
    {
        var tf = new TextField ();

        //📄 == \ud83d\udcc4 == \U0001F4C4
        // � == Rune.ReplacementChar
        tf.InsertText ("aA,;\ud83d\udcc4\U0001F4C4\udcc4\ud83d");
        Assert.Equal ("aA,;📄📄��", tf.Text);
    }

    [Fact]
    public void PositionCursor_Respect_GetColumns ()
    {
        var tf = new TextField { Width = 5 };
        tf.BeginInit ();
        tf.EndInit ();
        tf.SetFocus ();

        tf.NewKeyDownEvent (new Key ("📄"));
        Assert.Equal (1, tf.InsertionPoint);
        Assert.Equal (new Point (2, 0), tf.Cursor.Position);
        Assert.Equal ("📄", tf.Text);

        tf.NewKeyDownEvent (new Key (KeyCode.A));
        Assert.Equal (2, tf.InsertionPoint);
        Assert.Equal (new Point (3, 0), tf.Cursor.Position);
        Assert.Equal ("📄a", tf.Text);
    }

    [Fact]
    public void InsertText_GraphemeSequence_PreservesSingleGrapheme ()
    {
        TextField tf = new ();

        tf.InsertText ("👨‍👩‍👧‍👦");

        Assert.Equal ("👨‍👩‍👧‍👦", tf.Text);
        Assert.Equal (1, tf.InsertionPoint);
    }

    [Fact]
    public void Accented_Letter_With_Three_Combining_Unicode_Chars ()
    {
        IDriver driver = CreateTestDriver ();

        var tf = new TextField { Width = 3, Text = "ắ" };
        tf.Driver = driver;
        tf.Layout ();
        tf.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
ắ",
                                                       output,
                                                       driver);

        tf.Text = "\u1eaf";
        tf.Layout ();
        tf.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
ắ",
                                                       output,
                                                       driver);

        tf.Text = "\u0103\u0301";
        tf.Layout ();
        tf.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
ắ",
                                                       output,
                                                       driver);

        tf.Text = "\u0061\u0306\u0301";
        tf.Layout ();
        tf.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
ắ",
                                                       output,
                                                       driver);
    }

    [Fact]
    public void Adjust_First ()
    {
        IDriver driver = CreateTestDriver ();

        var tf = new TextField { Width = Dim.Fill (), Text = "This is a test." };
        tf.Driver = driver;
        tf.SetRelativeLayout (new Size (20, 20));
        tf.Draw ();

        Assert.Equal ("This is a test. ", GetContents ());

        string GetContents ()
        {
            var sb = new StringBuilder ();

            for (var i = 0; i < 16; i++)
            {
                sb.Append (driver.Contents! [0, i]!.Grapheme);
            }

            return sb.ToString ();
        }
    }

    [Fact]
    public void PositionCursor_Treat_Zero_Width_As_One_Column ()
    {
        IDriver driver = CreateTestDriver ();

        TextField tf = new () { Width = 10, Text = "\u001B[" };
        tf.Driver = driver;
        tf.SetRelativeLayout (new Size (10, 1));
        tf.SetFocus ();

        // Clear the automatic selection from focus
        tf.ClearAllSelection ();
        tf.InsertionPoint = 0;

        Assert.Equal (0, tf.InsertionPoint);

        tf.InsertionPoint = 1;
        Assert.Equal (new Point (1, 0), tf.Cursor.Position);

        tf.InsertionPoint = 2;
        Assert.Equal (new Point (2, 0), tf.Cursor.Position);
    }

    [Fact]
    public void ScrollOffset_Treat_Negative_Width_Glyph_As_One_Column ()
    {
        View view = new () { Width = 10, Height = 1 };
        TextField tf = new () { Width = 2, Text = "\u001B[" };
        view.Add (tf);
        tf.SetRelativeLayout (new Size (10, 1));

        Assert.Equal (0, tf.ScrollOffset);
        Assert.Equal (0, tf.InsertionPoint);

        Assert.True (tf.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (0, tf.ScrollOffset);
        Assert.Equal (1, tf.InsertionPoint);

        Assert.True (tf.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (1, tf.ScrollOffset);
        Assert.Equal (2, tf.InsertionPoint);

        Assert.False (tf.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (1, tf.ScrollOffset);
        Assert.Equal (2, tf.InsertionPoint);
    }

    [Fact]
    public void Focus_Via_Keyboard_Selects_All_Text ()
    {
        // Create a TextField with some text
        TextField tf = new () { Text = "Hello World" };
        tf.BeginInit ();
        tf.EndInit ();

        // Verify no selection initially
        Assert.Null (tf.SelectedText);
        Assert.Equal (0, tf.SelectedLength);

        // Focus the TextField (simulating keyboard focus like Tab)
        tf.SetFocus ();

        // Verify all text is selected
        Assert.Equal ("Hello World", tf.SelectedText);
        Assert.Equal (11, tf.SelectedLength);
        Assert.Equal (0, tf.SelectedStart);
    }

    [Fact]
    public void Focus_Via_Keyboard_With_Empty_Text_Does_Not_Select ()
    {
        // Create a TextField with no text
        TextField tf = new () { Text = "" };
        tf.BeginInit ();
        tf.EndInit ();

        // Focus the TextField
        tf.SetFocus ();

        // Verify no selection (since there's no text)
        Assert.Null (tf.SelectedText);
        Assert.Equal (0, tf.SelectedLength);
    }

    [Fact]
    public void Focus_Via_Mouse_Does_Not_Select_All_Text ()
    {
        // Create a TextField with text
        TextField tf = new () { Text = "Hello World", Width = 20, Height = 1 };
        tf.BeginInit ();
        tf.EndInit ();

        // Simulate mouse click to focus
        Mouse ev = new () { Position = new Point (5, 0), Flags = MouseFlags.LeftButtonPressed };
        tf.NewMouseEvent (ev);

        // Verify text is NOT selected
        Assert.Null (tf.SelectedText);
        Assert.Equal (0, tf.SelectedLength);

        // Verify cursor is positioned at click location
        Assert.Equal (5, tf.InsertionPoint);
    }

    [Fact]
    public void Focus_Via_Mouse_Clears_Existing_Selection ()
    {
        // Create a TextField with text
        TextField tf = new () { Text = "Hello World", Width = 20, Height = 1 };
        tf.BeginInit ();
        tf.EndInit ();

        // Select all text first
        tf.SelectAll ();
        Assert.Equal ("Hello World", tf.SelectedText);
        Assert.Equal (11, tf.SelectedLength);

        // Simulate mouse click
        Mouse ev = new () { Position = new Point (5, 0), Flags = MouseFlags.LeftButtonPressed };
        tf.NewMouseEvent (ev);

        // Verify selection is cleared
        Assert.Null (tf.SelectedText);
        Assert.Equal (0, tf.SelectedLength);
        Assert.Equal (5, tf.InsertionPoint);
    }

    [Fact]
    public void Focus_Twice_Via_Keyboard_Selects_All_Text_Each_Time ()
    {
        // Create two TextFields
        TextField tf1 = new () { Text = "First", Width = 10, Height = 1 };
        TextField tf2 = new () { Text = "Second", Width = 10, Height = 1 };

        Runnable container = new ();
        container.Add (tf1, tf2);

        tf1.BeginInit ();
        tf1.EndInit ();
        tf2.BeginInit ();
        tf2.EndInit ();

        // Focus first field
        tf1.SetFocus ();
        Assert.Equal ("First", tf1.SelectedText);

        // Focus second field
        tf2.SetFocus ();
        Assert.Equal ("Second", tf2.SelectedText);

        // Focus first field again
        tf1.SetFocus ();
        Assert.Equal ("First", tf1.SelectedText);

        container.Dispose ();
    }

    [Fact]
    public void Mouse_Double_Click_Selects_Word_Not_All ()
    {
        // Create a TextField with text
        TextField tf = new () { Text = "Hello World Test", Width = 20, Height = 1 };
        tf.BeginInit ();
        tf.EndInit ();

        // Double click on "World"
        Mouse ev = new () { Position = new Point (7, 0), Flags = MouseFlags.LeftButtonDoubleClicked };
        tf.NewMouseEvent (ev);

        // Should select the word (with trailing space by default), not all text
        Assert.Equal ("World ", tf.SelectedText);
        Assert.NotEqual ("Hello World Test", tf.SelectedText);
    }

    [Fact]
    public void Mouse_Triple_Click_Selects_All ()
    {
        // Create a TextField with text
        TextField tf = new () { Text = "Hello World", Width = 20, Height = 1 };
        tf.BeginInit ();
        tf.EndInit ();

        // Triple click
        Mouse ev = new () { Position = new Point (5, 0), Flags = MouseFlags.LeftButtonTripleClicked };
        tf.NewMouseEvent (ev);

        // Should select all text
        Assert.Equal ("Hello World", tf.SelectedText);
        Assert.Equal (11, tf.SelectedLength);
    }

    [Fact]
    public void Mouse_Right_Click_Open_ContextMenuAndKeepTextSelected ()
    {
        using IApplication app = Application.Create ().Init ();

        // Create a TextField with text
        TextField tf = new () { Text = "Hello World", Width = 20, Height = 1 };
        tf.App = app;
        tf.BeginInit ();
        tf.EndInit ();

        // Select all text
        tf.SelectAll ();
        Assert.Equal ("Hello World", tf.SelectedText);

        // Right click
        Mouse ev = new () { Position = new Point (5, 0), Flags = MouseFlags.RightButtonClicked };
        tf.NewMouseEvent (ev);

        // Should open context menu and keep text selected
        Assert.True (tf.ContextMenu?.Visible);
        Assert.Equal (11, tf.SelectedLength);
    }

    [Fact]
    public void Mouse_Left_Clicked_At_Start_And_End_Viewport_Change_ScrollOffset ()
    {
        // Create a TextField with text
        TextField tf = new () { Text = "Hello World", Width = 5, Height = 1 };
        tf.BeginInit ();
        tf.EndInit ();

        // Verify initial state with text longer than width
        Assert.Equal (11, tf.InsertionPoint);
        Assert.Equal (7, tf.ScrollOffset);

        // Mouse button clicked at the start of the viewport should move the cursor and adjust decreasing ScrollOffset
        Mouse ev = new () { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked };
        tf.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (7, tf.InsertionPoint);
        Assert.Equal (6, tf.ScrollOffset);

        // Mouse button clicked at the end of the viewport should move the cursor and adjust incrementing ScrollOffset
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonClicked };
        tf.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (10, tf.InsertionPoint);
        Assert.Equal (7, tf.ScrollOffset);
    }

    [Fact]
    public void Mouse_Left_Clicked_At_Start_And_End_Viewport_Change_ScrollOffset_Wide_Glyphs ()
    {
        // Create a TextField with text
        TextField tf = new () { Text = "Hello World👨‍👩‍👧", Width = 5, Height = 1 };
        tf.BeginInit ();
        tf.EndInit ();

        // Verify initial state with text longer than width
        Assert.Equal (12, tf.InsertionPoint);
        Assert.Equal (9, tf.ScrollOffset);

        // Mouse button clicked at the start of the viewport should move the cursor and adjust decreasing ScrollOffset
        Mouse ev = new () { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked };
        tf.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (9, tf.InsertionPoint);
        Assert.Equal (8, tf.ScrollOffset);

        // Mouse button clicked at the end of the viewport should move the cursor and adjust incrementing ScrollOffset
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonClicked };
        tf.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (12, tf.InsertionPoint);
        Assert.Equal (9, tf.ScrollOffset);
    }

    [Fact]
    public void Mouse_Selecting_From_Start_And_From_End_Viewport_SelectsCorrectly ()
    {
        // Create a TextField with text
        TextField tf = new () { Text = "Hello World", Width = 5, Height = 1 };
        tf.BeginInit ();
        tf.EndInit ();

        // Verify initial state with text longer than width
        Assert.Equal (11, tf.InsertionPoint);
        Assert.Equal (7, tf.ScrollOffset);

        // Mouse button pressed at the start of the viewport should move the cursor and adjust decreasing ScrollOffset
        Mouse ev = new () { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed };
        tf.NewMouseEvent (ev);

        // Start selecting text by moving mouse to the right while pressing
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tf.NewMouseEvent (ev);

        // Mouse move to right while pressing should select text and adjust ScrollOffset
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tf.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (11, tf.InsertionPoint);
        Assert.Equal (7, tf.ScrollOffset);
        Assert.Equal ("orld", tf.SelectedText);

        // Release mouse button
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonReleased };
        tf.NewMouseEvent (ev);

        // Mouse button pressed at the end of the viewport should move the cursor and adjust incrementing ScrollOffset
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonPressed };
        tf.NewMouseEvent (ev);

        // Start selecting text by moving mouse to the left while pressing
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tf.NewMouseEvent (ev);

        // Continue selecting text by moving mouse to the left while pressing forcing SelectedLength to be greater than 0
        ev = new Mouse { Position = new Point (3, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tf.NewMouseEvent (ev);

        // Mouse move to left while pressing should select text and adjust ScrollOffset
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tf.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (0, tf.InsertionPoint);
        Assert.Equal (0, tf.ScrollOffset);
        Assert.Equal ("Hello World", tf.SelectedText);
    }

    [Fact]
    public void Mouse_Selecting_From_Start_And_From_End_Viewport_SelectsCorrectly_Wide_Glyphs ()
    {
        // Create a TextField with text
        TextField tf = new () { Text = "Hello World👨‍👩‍👧", Width = 5, Height = 1 };
        tf.BeginInit ();
        tf.EndInit ();

        // Verify initial state with text longer than width
        Assert.Equal (12, tf.InsertionPoint);
        Assert.Equal (9, tf.ScrollOffset);

        // Mouse button pressed at the start of the viewport should move the cursor and adjust decreasing ScrollOffset
        Mouse ev = new () { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed };
        tf.NewMouseEvent (ev);

        // Start selecting text by moving mouse to the right while pressing
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tf.NewMouseEvent (ev);

        // Mouse move to right while pressing should select text and adjust ScrollOffset
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tf.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (12, tf.InsertionPoint);
        Assert.Equal (9, tf.ScrollOffset);
        Assert.Equal ("ld👨‍👩‍👧", tf.SelectedText);

        // Release mouse button
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonReleased };
        tf.NewMouseEvent (ev);

        // Mouse button pressed at the end of the viewport should move the cursor and adjust incrementing ScrollOffset
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonPressed };
        tf.NewMouseEvent (ev);

        // Start selecting text by moving mouse to the left while pressing
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tf.NewMouseEvent (ev);

        // Continue selecting text by moving mouse to the left while pressing forcing SelectedLength to be greater than 0
        ev = new Mouse { Position = new Point (3, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tf.NewMouseEvent (ev);

        // Mouse move to left while pressing should select text and adjust ScrollOffset
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tf.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (0, tf.InsertionPoint);
        Assert.Equal (0, tf.ScrollOffset);
        Assert.Equal ("Hello World👨‍👩‍👧", tf.SelectedText);
    }

    [Fact]
    public void Mouse_Click_At_Position_GreaterThanZero_On_Empty_TextField_Should_Set_ScrollOffset_To_Zero ()
    {
        // Create a TextField with empty text
        TextField tf = new () { Width = 5, Height = 1 };
        tf.BeginInit ();
        tf.EndInit ();

        // Verify initial state
        Assert.Equal (0, tf.InsertionPoint);
        Assert.Equal (0, tf.ScrollOffset);

        // Simulate mouse click
        Mouse ev = new () { Position = new Point (1, 0), Flags = MouseFlags.LeftButtonClicked };
        tf.NewMouseEvent (ev);

        // Verify ScrollOffset remains at zero
        Assert.Equal (0, tf.InsertionPoint);
        Assert.Equal (0, tf.ScrollOffset);
    }

    // Claude - Opus 4.5
    [Fact]
    public void Text_Polymorphism_Works ()
    {
        // Test that TextField.Text works correctly when accessed via View base class
        TextField tf = new () { Text = "Test" };
        Assert.Equal ("Test", tf.Text);
        Assert.Equal ("Test", tf.Text); // Should be same due to polymorphism
    }

    [Fact]
    public void Command_Activate_SetsFocus ()
    {
        TextField textField = new () { Text = "Test", Width = 10 };
        textField.BeginInit ();
        textField.EndInit ();
        Assert.False (textField.HasFocus);

        textField.InvokeCommand (Command.Activate);

        Assert.True (textField.HasFocus);

        textField.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void Command_Accept_RaisesAccepting ()
    {
        TextField textField = new () { Text = "Test" };
        var acceptingFired = false;

        textField.Accepting += (_, e) =>
                               {
                                   acceptingFired = true;
                                   e.Handled = true; // Signal that the Accept was processed
                               };

        bool? result = textField.InvokeCommand (Command.Accept);

        Assert.True (acceptingFired);
        Assert.True (result);

        textField.Dispose ();
    }

    [Fact]
    public void Command_HotKey_SetsFocus ()
    {
        TextField textField = new () { Text = "Test" };
        textField.BeginInit ();
        textField.EndInit ();
        Assert.False (textField.HasFocus);

        textField.InvokeCommand (Command.HotKey);

        Assert.True (textField.HasFocus);

        textField.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void Enter_RaisesAccepting ()
    {
        TextField textField = new () { Text = "Test" };
        var acceptingFired = false;

        textField.Accepting += (_, e) =>
                               {
                                   acceptingFired = true;
                                   e.Handled = true;
                               };

        // Enter should raise Accepting
        bool? result = textField.NewKeyDownEvent (Key.Enter);

        Assert.True (acceptingFired);
        Assert.True (result);

        textField.Dispose ();
    }

    // Copilot
    [Fact]
    public void UnifiedKeyBindings_Undo_Redo_Paste_DeleteAll ()
    {
        // Arrange
        TextField tf = new () { Width = 40, Text = "hello" };
        tf.BeginInit ();
        tf.EndInit ();
        tf.InsertionPoint = tf.Text.Length;

        // Ctrl+Z → Undo
        tf.NewKeyDownEvent (Key.Backspace); // delete a char so undo has something to do
        Assert.Equal ("hell", tf.Text);
        Assert.True (tf.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("hello", tf.Text);

        // Ctrl+Y → Redo
        Assert.True (tf.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("hell", tf.Text);
        Assert.True (tf.NewKeyDownEvent (Key.Z.WithCtrl)); // undo again to restore
        Assert.Equal ("hello", tf.Text);

        // Ctrl+V → Paste (Ctrl+R must no longer be DeleteAll)
        Assert.False (tf.KeyBindings.TryGet (Key.R.WithCtrl, out _));

        // Ctrl+Shift+Delete → DeleteAll (and NOT Ctrl+R)
        Assert.True (tf.NewKeyDownEvent (Key.Delete.WithCtrl.WithShift));
        Assert.Equal ("", tf.Text);
    }

    // Copilot
    [Fact]
    public void Paste_Uses_Clipboard_Text ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.Clipboard = new FakeClipboard ();
        using Runnable runnable = new ();

        TextField tf = new () { Width = 40, Text = string.Empty };
        runnable.Add (tf);
        app.Begin (runnable);

        app.Driver.Clipboard!.SetClipboardData ("Hello ");

        bool result = tf.Paste ();

        Assert.True (result);
        Assert.Equal ("Hello ", tf.Text);
    }

    // Copilot
    [Fact]
    public void CtrlV_Pastes_From_Clipboard ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.Clipboard = new FakeClipboard ();
        using Runnable runnable = new ();

        TextField tf = new () { Width = 40, Text = string.Empty };
        runnable.Add (tf);
        app.Begin (runnable);
        tf.SetFocus ();

        app.Driver.Clipboard!.SetClipboardData ("Hello ");

        bool? result = tf.NewKeyDownEvent (Key.V.WithCtrl);

        Assert.True (result);
        Assert.Equal ("Hello ", tf.Text);
    }

    // Copilot
    [Fact]
    public void Paste_CancelledByValueChanging_DoesNotRaisePasted_OrMoveCursor ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.Clipboard = new FakeClipboard ();
        using Runnable runnable = new ();

        TextField tf = new () { Width = 40, Text = "abc" };
        app.Begin (runnable);
        runnable.Add (tf);
        tf.InsertionPoint = 1;
        tf.SelectedStart = -1;

        int pastedCount = 0;
        tf.Pasted += (_, _) => pastedCount++;
        tf.ValueChanging += (_, e) => e.Handled = true;

        app.Driver.Clipboard!.SetClipboardData ("ZZ");

        bool result = tf.Paste ();

        Assert.True (result);
        Assert.Equal ("abc", tf.Text);
        Assert.Equal (1, tf.InsertionPoint);
        Assert.Equal (0, pastedCount);
    }

    // Copilot
    [Fact]
    public void Paste_RewrittenByTextChanging_Raises_Pasted_With_ActualInsertedText ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.Clipboard = new FakeClipboard ();
        using Runnable runnable = new ();

        TextField tf = new () { Width = 40, Text = string.Empty };
        runnable.Add (tf);
        app.Begin (runnable);

        string? pastedText = null;
        tf.Pasted += (_, e) => pastedText = e.Text;
        tf.TextChanging += (_, e) => e.Result = e.Result!.ToUpperInvariant ();

        app.Driver.Clipboard!.SetClipboardData ("Hello");

        bool result = tf.Paste ();

        Assert.True (result);
        Assert.Equal ("HELLO", tf.Text);
        Assert.Equal ("HELLO", pastedText);
        Assert.Equal (5, tf.InsertionPoint);
    }

    // Copilot
    [Fact]
    public void Paste_RewrittenByTextChanging_With_Existing_Text_Raises_Only_Inserted_Text ()
    {
        TextField tf = new () { Width = 40, Text = "abc" };
        tf.InsertionPoint = 1;
        tf.SelectedStart = -1;

        string? pastedText = null;
        tf.Pasted += (_, e) => pastedText = e.Text;
        tf.TextChanging += (_, e) => e.Result = e.Result!.ToUpperInvariant ();

        CommandContext ctx = new (Command.Paste, new WeakReference<View> (tf), binding: null)
        {
            Routing = CommandRouting.BubblingUp
        };

        bool? result = tf.InvokeCommand (Command.Paste, ctx.WithValue (new PastePayload ("zz")));

        Assert.True (result);
        Assert.Equal ("AZZBC", tf.Text);
        Assert.Equal ("ZZ", pastedText);
        Assert.Equal (3, tf.InsertionPoint);
    }

    // Copilot
    [Fact]
    public void Paste_RewrittenByTextChanging_With_AddedPrefixAndSuffix_Raises_Only_Inserted_Text ()
    {
        TextField tf = new () { Width = 40, Text = "abcd" };
        tf.InsertionPoint = 2;
        tf.SelectedStart = -1;

        string? pastedText = null;
        tf.Pasted += (_, e) => pastedText = e.Text;
        tf.TextChanging += (_, e) => e.Result = $"[{e.Result}]";

        CommandContext ctx = new (Command.Paste, new WeakReference<View> (tf), binding: null)
        {
            Routing = CommandRouting.BubblingUp
        };

        bool? result = tf.InvokeCommand (Command.Paste, ctx.WithValue (new PastePayload ("X")));

        Assert.True (result);
        Assert.Equal ("[abXcd]", tf.Text);
        Assert.Equal ("X", pastedText);
        Assert.Equal (4, tf.InsertionPoint);
    }

    // Copilot
    [Fact]
    public void Paste_RewrittenByTextChanging_In_Empty_Field_Excludes_Boundary_Wrappers ()
    {
        TextField tf = new () { Width = 40, Text = string.Empty };
        tf.SelectedStart = -1;

        string? pastedText = null;
        tf.Pasted += (_, e) => pastedText = e.Text;
        tf.TextChanging += (_, e) => e.Result = $"[{e.Result}]";

        CommandContext ctx = new (Command.Paste, new WeakReference<View> (tf), binding: null)
        {
            Routing = CommandRouting.BubblingUp
        };

        bool? result = tf.InvokeCommand (Command.Paste, ctx.WithValue (new PastePayload ("Hello")));

        Assert.True (result);
        Assert.Equal ("[Hello]", tf.Text);
        Assert.Equal ("Hello", pastedText);
        Assert.Equal (6, tf.InsertionPoint);
    }

    // Copilot
    [Fact]
    public void Paste_Replacing_Selection_With_No_Inserted_Text_Moves_Cursor_To_Selection_Start ()
    {
        TextField tf = new () { Width = 40, Text = "abcd" };
        tf.InsertionPoint = 4;
        tf.SelectedStart = 2;

        int pastedCount = 0;
        tf.Pasted += (_, _) => pastedCount++;
        tf.TextChanging += (_, e) => e.Result = "ab";

        CommandContext ctx = new (Command.Paste, new WeakReference<View> (tf), binding: null)
        {
            Routing = CommandRouting.BubblingUp
        };

        bool? result = tf.InvokeCommand (Command.Paste, ctx.WithValue (new PastePayload ("X")));

        Assert.True (result);
        Assert.Equal ("ab", tf.Text);
        Assert.Equal (2, tf.InsertionPoint);
        Assert.Equal (0, pastedCount);
    }

    // Copilot
    [Fact]
    public void Paste_RewrittenWithoutLengthChange_Does_Not_Allocate_Quadratic_Memory ()
    {
        const int pasteLength = 3000;
        const long allocationBudget = 10_000_000;

        TextField tf = new () { Width = 4000, Text = string.Empty };
        string pasted = new ('a', pasteLength);
        tf.TextChanging += (_, e) => e.Result = e.Result!.ToUpperInvariant ();

        CommandContext ctx = new (Command.Paste, new WeakReference<View> (tf), binding: null)
        {
            Routing = CommandRouting.BubblingUp
        };

        GC.Collect ();
        GC.WaitForPendingFinalizers ();
        GC.Collect ();

        long before = GC.GetAllocatedBytesForCurrentThread ();

        bool? result = tf.InvokeCommand (Command.Paste, ctx.WithValue (new PastePayload (pasted)));

        long allocated = GC.GetAllocatedBytesForCurrentThread () - before;

        Assert.True (result);
        Assert.Equal (pasted.ToUpperInvariant (), tf.Text);
        Assert.Equal (pasteLength, tf.InsertionPoint);
        Assert.True (allocated < allocationBudget, $"Allocated {allocated:N0} bytes.");
    }

    // Copilot
    [Fact]
    public void Paste_Pasting_Event_Text_Matches_TextField_Insertable_Text ()
    {
        TextField tf = new () { Width = 40, Text = string.Empty };

        string? pastingText = null;
        string? pastedText = null;
        tf.Pasting += (_, e) => pastingText = e.Text;
        tf.Pasted += (_, e) => pastedText = e.Text;

        CommandContext ctx = new (Command.Paste, new WeakReference<View> (tf), binding: null)
        {
            Routing = CommandRouting.BubblingUp
        };

        bool? result = tf.InvokeCommand (Command.Paste, ctx.WithValue (new PastePayload ("A\tB")));

        Assert.True (result);
        Assert.Equal ("AB", tf.Text);
        Assert.Equal ("AB", pastingText);
        Assert.Equal ("AB", pastedText);
    }

    // Copilot
    [Fact]
    public void Paste_Replacing_Selection_With_Identical_Text_Clears_Selection_And_Raises_Pasted ()
    {
        TextField tf = new () { Width = 40, Text = "abcd" };
        tf.InsertionPoint = 3;
        tf.SelectedStart = 1;

        string? pastedText = null;
        tf.Pasted += (_, e) => pastedText = e.Text;

        CommandContext ctx = new (Command.Paste, new WeakReference<View> (tf), binding: null)
        {
            Routing = CommandRouting.BubblingUp
        };

        bool? result = tf.InvokeCommand (Command.Paste, ctx.WithValue (new PastePayload ("bc")));

        Assert.True (result);
        Assert.Equal ("abcd", tf.Text);
        Assert.Equal (3, tf.InsertionPoint);
        Assert.Equal (0, tf.SelectedLength);
        Assert.Null (tf.SelectedText);
        Assert.Equal ("bc", pastedText);
    }

    // Copilot
    [Fact]
    public void UnifiedKeyBindings_NonWindows_Undo_Redo ()
    {
        if (OperatingSystem.IsWindows ())
        {
            return; // non-Windows-only bindings are not added on Windows
        }

        TextField tf = new () { Width = 40, Text = "hello" };
        tf.BeginInit ();
        tf.EndInit ();
        tf.InsertionPoint = tf.Text.Length;

        // Ctrl+/ → Undo
        tf.NewKeyDownEvent (Key.Backspace); // delete so undo has something
        Assert.Equal ("hell", tf.Text);
        Assert.True (tf.NewKeyDownEvent (new Key ('/').WithCtrl));
        Assert.Equal ("hello", tf.Text);

        // Ctrl+Shift+Z → Redo
        Assert.True (tf.NewKeyDownEvent (Key.Z.WithCtrl.WithShift));
        Assert.Equal ("hell", tf.Text);
    }

    [Fact]
    public void Text_Shorter_Than_Width_Should_Not_Scroll ()
    {
        using IApplication app = Application.Create ().Init ();
        Runnable container = new () { Id = "container", Width = 20, Height = 1 };
        container.App = app;
        TextField tf = new () { Width = Dim.Percent (50), Text = "hello world" };
        container.Add (tf);
        container.BeginInit ();
        container.EndInit ();

        tf.SetFocus ();
        Assert.True (tf.HasFocus);
        Assert.Equal ("hello world", tf.SelectedText);
        Assert.Equal (2, tf.ScrollOffset);
        Assert.Equal (11, tf.Text.GetColumns ());
        Assert.Equal (10, tf.Viewport.Width);
;
        // Resize container to be greater than text length
        container.Width = 30;
        Assert.Equal (0, tf.ScrollOffset);
        Assert.Equal (15, tf.Viewport.Width);
    }

    /// <summary>
    ///     Regression test for https://github.com/tui-cs/Terminal.Gui/issues/4963
    ///     When Kitty keyboard protocol sets AssociatedText on Alt+letter keys,
    ///     TextField must not insert the text. Alt-modified keys are never text input.
    /// </summary>
    [Fact]
    public void AltKey_With_AssociatedText_Does_Not_Insert_Into_TextField ()
    {
        // Copilot
        TextField tf = new () { Width = 20 };
        tf.SetFocus ();

        // Simulate Alt+T with AssociatedText set by Kitty keyboard protocol which in a real scenario would be set AssociatedText as empty string
        // for Alt+letter keys, with the exception of AltGr+key which would set AssociatedText to "key" (e.g. "€" for AltGr+E)
        Key altT = new (Key.T.WithAlt) { AssociatedText = "" };
        tf.NewKeyDownEvent (altT);

        Assert.Equal ("", tf.Text);
    }

    /// <summary>
    ///     Regression test for https://github.com/tui-cs/Terminal.Gui/issues/4963
    ///     Ctrl-modified keys with AssociatedText must not be inserted as text.
    /// </summary>
    [Fact]
    public void CtrlKey_With_AssociatedText_Does_Not_Insert_Into_TextField ()
    {
        // Copilot
        TextField tf = new () { Width = 20 };
        tf.SetFocus ();

        Key ctrlT = new (Key.T.WithCtrl) { AssociatedText = "t" };
        tf.NewKeyDownEvent (ctrlT);

        Assert.Equal ("", tf.Text);
    }

    [Fact]
    public void ReadOnly_ShouldNotAllowAutomaticallyScrolling_AndMustSetInsertionPointToZeroAtInitialization ()
    {
        TextField tf = new () { Width = 5, ReadOnly = true, Text = "hello world" };
        tf.BeginInit ();
        tf.EndInit ();

        Assert.Equal (0, tf.InsertionPoint);
        Assert.Equal (0, tf.ScrollOffset);
        Assert.Null (tf.SelectedText);
    }

    [Fact]
    public void ReadOnly_ShouldNotSelectAllText_OnFocus ()
    {
        TextField tf = new () { Width = 5, ReadOnly = true, Text = "hello world" };
        tf.BeginInit ();
        tf.EndInit ();

        tf.SetFocus ();

        Assert.Equal (0, tf.InsertionPoint);
        Assert.Equal (0, tf.ScrollOffset);
        Assert.Null (tf.SelectedText);
    }

     [Fact]
     public void ReadOnly_ShouldSetInsertionPointAndScrollOffsetToZero_OnFocus ()
     {
         TextField tf = new () { Width = 5, ReadOnly = true, Text = "hello world" };
         tf.BeginInit ();
         tf.EndInit ();

         // Move insertion point to end of text
         tf.InsertionPoint = tf.Text.Length;
         Assert.Equal (11, tf.InsertionPoint);
         Assert.Equal (7, tf.ScrollOffset);

         // Set focus and verify insertion point is still at zero
         tf.SetFocus ();
         Assert.Equal (0, tf.InsertionPoint);
         Assert.Equal (0, tf.ScrollOffset);
     }

     [Fact]
     public void ReadOnly_ShouldSetInsertionPointAndScrollOffsetToZero_LeavingFocus ()
     {
         TextField tf = new () { Width = 5, ReadOnly = true, Text = "hello world" };

         // Create another view to take focus away
         View otherView = new () { CanFocus = true };

         // Create a container to hold both views
         Runnable container = new () { Id = "container", Width = 20, Height = 5 };
         container.Add (tf, otherView);
         container.BeginInit ();
         container.EndInit ();

         // Set focus to TextField and verify insertion point and scroll offset are at zero
         tf.SetFocus ();
         Assert.Equal (0, tf.InsertionPoint);
         Assert.Equal (0, tf.ScrollOffset);

         // Move insertion point to end of text and then move focus away to verify insertion point is still at zero
         tf.InsertionPoint = tf.Text.Length;
         Assert.Equal (11, tf.InsertionPoint);
         Assert.Equal (7, tf.ScrollOffset);

         otherView.SetFocus ();
         Assert.Equal (0, tf.InsertionPoint);
         Assert.Equal (0, tf.ScrollOffset);
     }

     [Fact]
     public void ReadOnly_ShouldSetInsertionPointAndScrollOffsetToZero_AfterCopyingText ()
     {
         TextField tf = new () { Width = 5, ReadOnly = true, Text = "hello world" };
         tf.BeginInit ();
         tf.EndInit ();

         // Select all text
         tf.SelectAll ();
         Assert.Equal (11, tf.InsertionPoint);
         Assert.Equal (7, tf.ScrollOffset);
         Assert.Equal ("hello world", tf.SelectedText);

         // Copy text and verify insertion point and scroll offset are still at zero
         tf.Copy ();
         Assert.Equal (0, tf.InsertionPoint);
         Assert.Equal (0, tf.ScrollOffset);
         Assert.Null (tf.SelectedText);
     }

     [Fact]
     public void ReadOnly_ShouldSetInsertionPointAndScrollOffsetToZeroAndClearAllSelection_OnLeavingFocus ()
     {
         TextField tf = new () { Width = 5, ReadOnly = true, Text = "hello world" };

         // Create another view to take focus away
         View otherView = new () { CanFocus = true };

         // Create a container to hold both views
         Runnable container = new () { Id = "container", Width = 20, Height = 5 };
         container.Add (tf, otherView);
         container.BeginInit ();
         container.EndInit ();

         // Set focus to TextField and select all text
         tf.SetFocus ();
         tf.SelectAll ();
         Assert.Equal (11, tf.InsertionPoint);
         Assert.Equal (7, tf.ScrollOffset);
         Assert.Equal ("hello world", tf.SelectedText);

         // Move focus away and verify insertion point and scroll offset are at zero and selection is cleared
         otherView.SetFocus ();
         Assert.Equal (0, tf.InsertionPoint);
         Assert.Equal (0, tf.ScrollOffset);
         Assert.Null (tf.SelectedText);
     }
}
