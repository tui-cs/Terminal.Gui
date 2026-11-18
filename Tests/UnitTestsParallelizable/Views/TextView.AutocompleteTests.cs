using UnitTests;

namespace ViewsTests.TextViewTests;

public class TextViewAutocompleteTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Autocomplete_Popup_Suggests_And_Accepts ()
    {
        using IApplication testApp = RunTestApplication (20, 5, DoTest, true, output);

        return;

        void DoTest (object? _, EventArgs<IApplication?> args)
        {
            const string TEXT = "1 2 ";

            IApplication app = args.Value!;

            TextView tv = new () { Width = Dim.Fill (), Height = Dim.Fill () };
            tv.Text = TEXT;
            (app.TopRunnable as View)!.Add (tv);

            var g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;
            g.AllSuggestions = ["three"];

            tv.InsertionPoint = new (TEXT.Length, 0);
            app.InjectKey (Key.T);
            Assert.Equal ("1 2 t", tv.Text);
            Assert.Equal (new (5, 0), tv.InsertionPoint);
            Assert.Single (tv.Autocomplete.Suggestions);
            Assert.Equal ("three", tv.Autocomplete.Suggestions [0].Replacement);

            app.LayoutAndDraw ();

            DriverAssert.AssertDriverContentsWithFrameAre (
                                                           """
                                                           ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                                           ┊1 2 t             ┊
                                                           ┊    three         ┊
                                                           ┊                  ┊
                                                           └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                                           """,
                                                           output,
                                                           app.Driver);

            app.InjectKey (Key.Enter);
            Assert.Equal ("1 2 three", tv.Text);
            Assert.Equal (new (9, 0), tv.InsertionPoint);
            Assert.Empty (tv.Autocomplete.Suggestions);
            Assert.False (tv.Autocomplete.Visible);
        }
    }

    [Fact]
    public void Autocomplete_Popup_Open_And_Select_By_Mouse_Click_And_Close ()
    {
        using IApplication testApp = RunTestApplication (20, 5, DoTest, true, output);

        return;

        void DoTest (object? _, EventArgs<IApplication?> args)
        {
            const string TEXT = "1 2 ";

            IApplication app = args.Value!;

            TextView tv = new () { Width = Dim.Fill (), Height = Dim.Fill () };
            tv.Text = TEXT;
            (app.TopRunnable as View)!.Add (tv);

            var g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;
            g.AllSuggestions = ["three"];

            tv.InsertionPoint = new (TEXT.Length, 0);
            app.InjectKey (Key.T);
            Assert.Equal ("1 2 t", tv.Text);
            Assert.Equal (new (5, 0), tv.InsertionPoint);
            Assert.Single (tv.Autocomplete.Suggestions);
            Assert.Equal ("three", tv.Autocomplete.Suggestions [0].Replacement);

            app.LayoutAndDraw ();

            DriverAssert.AssertDriverContentsWithFrameAre (
                                                           """
                                                           ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                                           ┊1 2 t             ┊
                                                           ┊    three         ┊
                                                           ┊                  ┊
                                                           └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                                           """,
                                                           output,
                                                           app.Driver);

            app.InjectMouse (new () { ScreenPosition = new (6, 2), Flags = MouseFlags.LeftButtonClicked });
            Assert.Equal ("1 2 three", tv.Text);
            Assert.Equal (new (9, 0), tv.InsertionPoint);
            Assert.Empty (tv.Autocomplete.Suggestions);
            Assert.False (tv.Autocomplete.Visible);
        }
    }

    [Fact]
    public void Autocomplete_Popup_Open_And_Select_By_Mouse_Click_To_Replace_Text ()
    {
        using IApplication testApp = RunTestApplication (20, 5, DoTest, true, output);

        return;

        void DoTest (object? _, EventArgs<IApplication?> args)
        {
            const string TEXT = "1 2 three";

            IApplication app = args.Value!;

            TextView tv = new () { Width = Dim.Fill (), Height = Dim.Fill () };
            tv.Text = TEXT;
            (app.TopRunnable as View)!.Add (tv);

            var g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;
            g.AllSuggestions = ["two"];

            tv.SelectionStartColumn = 4;
            tv.SelectionStartRow = 0;
            tv.InsertionPoint = new (9, 0); // select "three"

            app.InjectKey (Key.T);
            Assert.Equal ("1 2 t", tv.Text);
            Assert.True (tv.Autocomplete.Visible);

            app.LayoutAndDraw ();

            DriverAssert.AssertDriverContentsWithFrameAre (
                                                           """
                                                           ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                                           ┊1 2 t             ┊
                                                           ┊    two           ┊
                                                           ┊                  ┊
                                                           └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                                           """,
                                                           output,
                                                           app.Driver);

            app.InjectMouse (new () { ScreenPosition = new (6, 2), Flags = MouseFlags.LeftButtonClicked });

            Assert.Equal ("1 2 two", tv.Text);
            Assert.Equal (new (7, 0), tv.InsertionPoint);
            Assert.Empty (tv.Autocomplete.Suggestions);
            Assert.False (tv.Autocomplete.Visible);
        }
    }

    [Fact]
    public void Autocomplete_Popup_Open_And_Close_With_ESC_Key ()
    {
        using IApplication testApp = RunTestApplication (20, 5, DoTest, true, output);

        return;

        void DoTest (object? _, EventArgs<IApplication?> args)
        {
            const string TEXT = "1 2 ";

            IApplication app = args.Value!;

            TextView tv = new () { Width = Dim.Fill (), Height = Dim.Fill () };
            tv.Text = TEXT;
            (app.TopRunnable as View)!.Add (tv);

            var g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;
            g.AllSuggestions = ["three"];

            tv.InsertionPoint = new (TEXT.Length, 0);
            app.InjectKey (Key.T);
            Assert.Equal ("1 2 t", tv.Text);
            Assert.True (tv.Autocomplete.Visible);

            app.InjectKey (Key.Esc);
            Assert.False (tv.Autocomplete.Visible);
            Assert.Equal ("1 2 t", tv.Text);
            Assert.Equal (new (5, 0), tv.InsertionPoint);
        }
    }

    // CoPilot - Rewrote to use RunTestApplication pattern
    [Fact]
    public void Autocomplete_Popup_Open_And_Select_With_Down_And_Up_Arrows_Keys_And_Close_With_Enter_Key ()
    {
        using IApplication testApp = RunTestApplication (20, 10, DoTest, true, output);

        return;

        void DoTest (object? _, EventArgs<IApplication?> args)
        {
            IApplication app = args.Value!;

            TextView tv = new () { Width = Dim.Fill (), Height = Dim.Fill () };
            (app.TopRunnable as View)!.Add (tv);

            var g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;
            g.AllSuggestions = ["first", "friend", "fabulous"];

            tv.InsertionPoint = new (0, 0);
            app.InjectKey (Key.F);

            Assert.Equal ("f", tv.Text);
            Assert.True (tv.Autocomplete.Visible);
            Assert.Equal (3, tv.Autocomplete.Suggestions.Count);
            Assert.Equal (0, tv.Autocomplete.SelectedIdx);

            app.InjectKey (Key.CursorDown);
            Assert.Equal (1, tv.Autocomplete.SelectedIdx);

            app.InjectKey (Key.CursorDown);
            Assert.Equal (2, tv.Autocomplete.SelectedIdx);

            app.InjectKey (Key.CursorUp);
            Assert.Equal (1, tv.Autocomplete.SelectedIdx);

            app.InjectKey (Key.Enter);
            Assert.Equal ("friend", tv.Text);
            Assert.False (tv.Autocomplete.Visible);
        }
    }

    // CoPilot - Rewrote to use RunTestApplication pattern
    [Fact]
    public void Autocomplete_Popup_Open_And_Typing_With_And_Without_Selection ()
    {
        using IApplication testApp = RunTestApplication (50, 10, DoTest, true, output);

        return;

        void DoTest (object? _, EventArgs<IApplication?> args)
        {
            IApplication app = args.Value!;

            TextView tv = new () { Width = Dim.Fill (), Height = Dim.Fill () };
            (app.TopRunnable as View)!.Add (tv);

            var g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;
            g.AllSuggestions = ["first", "friend", "fabulous"];

            // Typing without selection
            app.InjectKey (Key.F.WithShift);
            Assert.Equal ("F", tv.Text);
            Assert.True (tv.Autocomplete.Visible);
            Assert.Equal (3, tv.Autocomplete.Suggestions.Count);

            app.InjectKey (Key.R);
            Assert.Equal ("Fr", tv.Text);
            Assert.Single (tv.Autocomplete.Suggestions);
            Assert.Equal ("friend", tv.Autocomplete.Suggestions [0].Replacement);

            // Typing with selection
            tv.Text = "some text";
            tv.SelectAll ();

            app.InjectKey (Key.F.WithShift);
            Assert.Equal ("F", tv.Text);
            Assert.True (tv.Autocomplete.Visible);
            Assert.Equal (3, tv.Autocomplete.Suggestions.Count);
        }
    }

    // CoPilot - Rewrote to use RunTestApplication pattern
    [Fact]
    public void Autocomplete_Popup_Open_And_Select_With_Home_And_End_Keys_And_Close_With_Enter_Key ()
    {
        using IApplication testApp = RunTestApplication (50, 10, DoTest, true, output);

        return;

        void DoTest (object? _, EventArgs<IApplication?> args)
        {
            IApplication app = args.Value!;

            TextView tv = new () { Width = Dim.Fill (), Height = Dim.Fill () };
            (app.TopRunnable as View)!.Add (tv);

            var g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;
            g.AllSuggestions = ["item0", "item1", "item2", "item3", "item4"];

            app.InjectKey (Key.I);
            Assert.Equal ("i", tv.Text);
            Assert.True (tv.Autocomplete.Visible);
            Assert.Equal (5, tv.Autocomplete.Suggestions.Count);
            Assert.Equal (0, tv.Autocomplete.SelectedIdx);

            // Navigate down to last item
            for (var i = 0; i < 4; i++)
            {
                app.InjectKey (Key.CursorDown);
            }

            Assert.Equal (4, tv.Autocomplete.SelectedIdx);

            // Navigate back up to first item
            for (var i = 0; i < 4; i++)
            {
                app.InjectKey (Key.CursorUp);
            }

            Assert.Equal (0, tv.Autocomplete.SelectedIdx);

            app.InjectKey (Key.Enter);
            Assert.Equal ("item0", tv.Text);
            Assert.False (tv.Autocomplete.Visible);
        }
    }

    [Fact]
    public void Autocomplete_Popup_Show_Then_LostFocus_Should_Hide ()
    {
        var iterations = 0;
        TextView tv = new () { Width = Dim.Fill (), Height = 10 };
        View otherView = new () { CanFocus = true, Y = 1, Width = 1, Height = 1, X = 1 };
        using IApplication testApp = RunTestApplication (50, 15, DoTest, false, output);

        return;

        void DoTest (object? _, EventArgs<IApplication?> args)
        {
            IApplication app = args.Value!;

            switch (iterations++)
            {
                case 0:
                    (app.TopRunnable as View)!.Add (tv, otherView);

                    var g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;
                    g.AllSuggestions = ["item"];

                    tv.SetFocus ();

                    app.InjectKey (Key.I);
                    Assert.Equal ("i", tv.Text);
                    Assert.True (tv.Autocomplete.Visible);

                    // Lose focus
                    otherView.SetFocus ();

                    break;

                case 1:

                    Assert.True (otherView.HasFocus);
                    Assert.False (tv.Autocomplete.Visible);
                    app.RequestStop ();

                    break;
            }
        }
    }

    [Fact]
    public void Autocomplete_Popup_Sets_Visible_To_False_When_Clicked_Outside_Of_Popup ()
    {
        TextView tv = new () { Width = Dim.Fill (), Height = 10, Text = " some text" };
        using IApplication testApp = RunTestApplication (50, 15, DoTest, false, output);

        return;

        void DoTest (object? _, EventArgs<IApplication?> args)
        {
            IApplication app = args.Value!;

            (app.TopRunnable as View)!.Add (tv);

            SingleWordSuggestionGenerator g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;
            g.AllSuggestions = ["item"];

            tv.SetFocus ();

            app.InjectKey (Key.I);
            Assert.Equal ("i some text", tv.Text);
            Assert.True (tv.Autocomplete.Visible);

            // Click outside of popup
            app.InjectMouse (new Mouse { ScreenPosition = new Point (5, 1), Flags = MouseFlags.LeftButtonClicked });

            Assert.False (tv.Autocomplete.Visible);
            app.RequestStop ();
        }
    }
}
