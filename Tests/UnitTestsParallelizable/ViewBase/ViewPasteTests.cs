namespace ViewBaseTests;

// Claude - Opus 4.7
public class ViewPasteTests
{
    private static bool? InvokePaste (View view, string payload)
    {
        CommandContext ctx = new (Command.Paste, new WeakReference<View> (view), binding: null)
        {
            Routing = CommandRouting.BubblingUp
        };

        return view.InvokeCommand (Command.Paste, ctx.WithValue (new PastePayload (payload)));
    }

    [Fact]
    public void CommandPaste_RaisesPastingAndPasted_WhenConsumed ()
    {
        TextField field = new () { Text = string.Empty };

        var raiseOrder = new List<string> ();
        field.Pasting += (_, _) => raiseOrder.Add ("pasting");
        field.Pasted += (_, _) => raiseOrder.Add ("pasted");

        bool? handled = InvokePaste (field, "hello");

        Assert.True (handled);
        Assert.Equal (["pasting", "pasted"], raiseOrder);
        Assert.Equal ("hello", field.Text);
    }

    [Fact]
    public void Pasting_Cancelled_DoesNotInsertOrRaisePasted ()
    {
        TextField field = new () { Text = "x" };

        var pastedFired = false;
        field.Pasting += (_, args) => args.Handled = true;
        field.Pasted += (_, _) => pastedFired = true;

        bool? handled = InvokePaste (field, "yz");

        Assert.True (handled);
        Assert.False (pastedFired);
        Assert.Equal ("x", field.Text);
    }

    [Fact]
    public void Pasting_RewritesText_BeforeInsertion ()
    {
        TextField field = new () { Text = string.Empty };

        field.Pasting += (_, args) => args.Text = args.Text.ToUpperInvariant ();

        InvokePaste (field, "abc");

        Assert.Equal ("ABC", field.Text);
    }

    [Fact]
    public void View_OnPaste_Default_ReturnsFalse_AndPastedNotRaised ()
    {
        View view = new ();
        var pastedFired = false;
        view.Pasted += (_, _) => pastedFired = true;

        bool? handled = InvokePaste (view, "hello");

        // Plain View's OnPaste returns false → handler returns false, Pasted not raised.
        Assert.False (handled);
        Assert.False (pastedFired);
    }

    [Fact]
    public void DisabledView_DoesNotPaste ()
    {
        TextField field = new () { Text = string.Empty, Enabled = false };

        bool? handled = InvokePaste (field, "x");

        Assert.False (handled);
        Assert.Equal (string.Empty, field.Text);
    }

    [Fact]
    public void TextField_Paste_InsertsTextAtCursor ()
    {
        TextField field = new () { Text = "abc", ReadOnly = false };
        field.MoveEnd ();

        bool? handled = InvokePaste (field, "XYZ");

        Assert.True (handled);
        Assert.Equal ("abcXYZ", field.Text);
    }

    [Fact]
    public void TextField_Paste_ReadOnly_DoesNotInsert ()
    {
        TextField field = new () { Text = "abc", ReadOnly = true };

        bool? handled = InvokePaste (field, "XYZ");

        // ReadOnly views consume the paste (return true) so Ctrl+V does not bubble, but the text
        // model is not modified.
        Assert.True (handled);
        Assert.Equal ("abc", field.Text);
    }

    [Fact]
    public void TextField_Paste_ReadOnly_DoesNotRaisePasted ()
    {
        TextField field = new () { Text = "abc", ReadOnly = true };
        var pastedFired = false;
        field.Pasted += (_, _) => pastedFired = true;

        bool? handled = InvokePaste (field, "XYZ");

        Assert.True (handled);
        Assert.False (pastedFired);
    }

    [Fact]
    public void TextField_Paste_TakesFirstLineOnly ()
    {
        TextField field = new () { Text = string.Empty };

        bool? handled = InvokePaste (field, "first\nsecond\rthird");

        Assert.True (handled);
        Assert.Equal ("first", field.Text);
    }

    [Fact]
    public void TextField_Paste_StripsControlCharsIncludingEscape ()
    {
        TextField field = new () { Text = string.Empty };

        // ESC[31m color sequence and a literal bell — both must be stripped.
        bool? handled = InvokePaste (field, "ab[31mcd");

        Assert.True (handled);
        Assert.Equal ("ab[31mcd", field.Text);
    }

    [Fact]
    public void TextField_Paste_AllControlChars_DoesNotInsert ()
    {
        TextField field = new () { Text = "x" };

        bool? handled = InvokePaste (field, "");

        Assert.False (handled);
        Assert.Equal ("x", field.Text);
    }

    [Fact]
    public void TextView_Paste_InsertsText ()
    {
        TextView textView = new () { Text = "abc", ReadOnly = false };
        textView.MoveEnd ();

        bool? handled = InvokePaste (textView, "XYZ");

        Assert.True (handled);
        Assert.Contains ("XYZ", textView.Text);
    }

    [Fact]
    public void TextView_Paste_ReadOnly_DoesNotInsert ()
    {
        TextView textView = new () { Text = "abc", ReadOnly = true };

        bool? handled = InvokePaste (textView, "XYZ");

        // ReadOnly views consume the paste (return true) so Ctrl+V does not bubble, but the text
        // model is not modified.
        Assert.True (handled);
        Assert.Equal ("abc", textView.Text);
    }

    [Fact]
    public void TextView_Paste_ReadOnly_DoesNotRaisePasted ()
    {
        TextView textView = new () { Text = "abc", ReadOnly = true };
        var pastedFired = false;
        textView.Pasted += (_, _) => pastedFired = true;

        bool? handled = InvokePaste (textView, "XYZ");

        Assert.True (handled);
        Assert.False (pastedFired);
    }

    [Fact]
    public void TextView_Paste_NormalizesCarriageReturnsToLogicalLineBreaks ()
    {
        TextView textView = new () { Text = string.Empty };

        // Mix of CR (terminal default), CRLF, and bare LF — all become logical line breaks.
        bool? handled = InvokePaste (textView, "a\rb\r\nc\nd");

        Assert.True (handled);
        Assert.Equal ($"a{Environment.NewLine}b{Environment.NewLine}c{Environment.NewLine}d", textView.Text);
    }

    [Fact]
    public void TextView_Paste_StripsEscapeAndOtherControlChars ()
    {
        TextView textView = new () { Text = string.Empty };

        bool? handled = InvokePaste (textView, "a[31mbc\td");

        Assert.True (handled);
        Assert.Equal ("a[31mbc\td", textView.Text);
    }

    [Fact]
    public void TextView_BracketedPaste_Ignores_CopyWithoutSelection_Mode ()
    {
        TextView textView = new () { Text = "abc" };
        textView.MoveEnd ();

        Assert.True (textView.Copy ());

        bool? handled = InvokePaste (textView, "Z");

        Assert.True (handled);
        Assert.Equal ("abcZ", textView.Text);
    }

    [Fact]
    public void EmptyPayload_DoesNotRaisePastingOrPasted ()
    {
        TextField field = new () { Text = string.Empty };

        var anyFired = false;
        field.Pasting += (_, _) => anyFired = true;
        field.Pasted += (_, _) => anyFired = true;

        bool? handled = InvokePaste (field, string.Empty);

        Assert.False (handled);
        Assert.False (anyFired);
    }

    // CoPilot - GPT-5
    [Fact]
    public void ApplicationPaste_BeginWithNestedFocusableView_DispatchesToMostFocusedView ()
    {
        using IApplication app = Application.Create ();
        using Runnable runnable = new ();
        View wrapper = new () { CanFocus = true };
        TextField field = new () { Text = string.Empty };
        wrapper.Add (field);
        runnable.Add (wrapper);

        app.Begin (runnable);

        Assert.Same (field, runnable.MostFocused);
        Assert.Same (field, app.Navigation!.GetFocused ());

        bool handled = app.RaisePasteEvent ("hello");

        Assert.True (handled);
        Assert.Equal ("hello", field.Text);
    }

    [Fact]
    public void ApplicationPaste_Handled_DoesNotDispatchToFocusedView ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        using Runnable runnable = new ();
        TextField field = new () { Text = string.Empty };
        var anyFired = false;
        field.Pasting += (_, _) => anyFired = true;
        field.Pasted += (_, _) => anyFired = true;
        runnable.Add (field);
        app.Begin (runnable);
        field.SetFocus ();
        app.Paste += (_, args) => args.Handled = true;

        bool handled = app.RaisePasteEvent ("hello");

        Assert.True (handled);
        Assert.Equal (string.Empty, field.Text);
        Assert.False (anyFired);
    }

    [Fact]
    public void MenuInitiatedPaste_UsesClipboardText_NotMenuTitle ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.Clipboard = new FakeClipboard ();
        using Runnable runnable = new ();
        TextView textView = new () { Text = string.Empty, Width = 20, Height = 5 };
        runnable.Add (textView);
        app.Begin (runnable);
        app.Driver.Clipboard.SetClipboardData ("Hello ");
        MenuItem menuItem = new (textView, Command.Paste);

        bool? handled = menuItem.InvokeCommand (Command.Activate);

        Assert.False (handled);
        Assert.Equal ("Hello ", textView.Text);
    }
}
