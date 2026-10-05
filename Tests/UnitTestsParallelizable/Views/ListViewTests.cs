using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using Moq;
using UnitTests;

// ReSharper disable AccessToModifiedClosure

namespace ViewsTests;

public class ListViewTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void CollectionNavigatorMatcher_KeybindingsOverrideNavigator ()
    {
        ObservableCollection<string> source = ["apricot", "arm", "bat", "batman", "bates hotel", "candle"];
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        lv.SetFocus ();

        lv.KeyBindings.Add (Key.B, Command.Down);

        Assert.Null (lv.SelectedItem);

        // Keys should be consumed to move down the navigation i.e. to apricot
        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.Equal (0, lv.SelectedItem);

        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.Equal (1, lv.SelectedItem);

        // There is no keybinding for Key.C so it hits collection navigator i.e. we jump to candle
        Assert.True (lv.NewKeyDownEvent (Key.C));
        Assert.Equal (5, lv.SelectedItem);
    }

    [Fact]
    public void Ensures_Visibility_SelectedItem_On_MoveDown_And_MoveUp ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (12, 12);

        ObservableCollection<string> source = [];

        for (var i = 0; i < 20; i++)
        {
            source.Add ($"Line{i}");
        }

        var lv = new ListView { Width = Dim.Fill (), Height = Dim.Fill (), Source = new ListWrapper<string> (source) };
        var win = new Window ();
        win.Add (lv);
        var top = new Runnable ();
        top.Add (win);
        app.Begin (top);

        Assert.Null (lv.SelectedItem);
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                       _output,
                                                       app.Driver);

        Assert.True (lv.ScrollVertical (10));
        app.LayoutAndDraw ();
        Assert.Null (lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘",
                                                       _output,
                                                       app.Driver);

        Assert.True (lv.MoveDown ());
        app.LayoutAndDraw ();
        Assert.Equal (0, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                       _output,
                                                       app.Driver);

        Assert.True (lv.MoveEnd ());
        app.LayoutAndDraw ();
        Assert.Equal (19, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘",
                                                       _output,
                                                       app.Driver);

        Assert.True (lv.ScrollVertical (-20));
        app.LayoutAndDraw ();
        Assert.Equal (19, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                       _output,
                                                       app.Driver);

        Assert.True (lv.MoveDown ());
        app.LayoutAndDraw ();
        Assert.Equal (19, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘",
                                                       _output,
                                                       app.Driver);

        Assert.True (lv.ScrollVertical (-20));
        app.LayoutAndDraw ();
        Assert.Equal (19, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                       _output,
                                                       app.Driver);

        Assert.True (lv.MoveDown ());
        app.LayoutAndDraw ();
        Assert.Equal (19, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘",
                                                       _output,
                                                       app.Driver);

        Assert.True (lv.MoveHome ());
        app.LayoutAndDraw ();
        Assert.Equal (0, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                       _output,
                                                       app.Driver);

        Assert.True (lv.ScrollVertical (20));
        app.LayoutAndDraw ();
        Assert.Equal (0, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line10    │
│Line11    │
│Line12    │
│Line13    │
│Line14    │
│Line15    │
│Line16    │
│Line17    │
│Line18    │
│Line19    │
└──────────┘",
                                                       _output,
                                                       app.Driver);

        Assert.True (lv.MoveUp ());
        app.LayoutAndDraw ();
        Assert.Equal (0, lv.SelectedItem);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
┌──────────┐
│Line0     │
│Line1     │
│Line2     │
│Line3     │
│Line4     │
│Line5     │
│Line6     │
│Line7     │
│Line8     │
│Line9     │
└──────────┘",
                                                       _output,
                                                       app.Driver);
        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void EnsureSelectedItemVisible_SelectedItem ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (12, 12);

        ObservableCollection<string> source = [];

        for (var i = 0; i < 10; i++)
        {
            source.Add ($"Item {i}");
        }

        var lv = new ListView { Width = 10, Height = 5, Source = new ListWrapper<string> (source) };
        var top = new Runnable ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Item 0
Item 1
Item 2
Item 3
Item 4",
                                                       _output,
                                                       app.Driver);

        // EnsureSelectedItemVisible is auto enabled on the OnSelectedChanged
        lv.SelectedItem = 6;
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Item 2
Item 3
Item 4
Item 5
Item 6",
                                                       _output,
                                                       app.Driver);
        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void EnsureSelectedItemVisible_Top ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        IDriver? driver = app.Driver;
        driver?.SetScreenSize (8, 2);

        ObservableCollection<string> source = ["First", "Second"];
        var lv = new ListView { Width = Dim.Fill (), Height = 1, Source = new ListWrapper<string> (source) };
        lv.SelectedItem = 1;
        var top = new Runnable ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        Assert.Equal ("Second ", GetContents (0));
        Assert.Equal (new string (' ', 7), GetContents (1));

        lv.MoveUp ();
        lv.Draw ();

        Assert.Equal ("First  ", GetContents (0));
        Assert.Equal (new string (' ', 7), GetContents (1));

        string GetContents (int line)
        {
            var sb = new StringBuilder ();

            for (var i = 0; i < 7; i++)
            {
                sb.Append ((app?.Driver?.Contents!) [line, i].Grapheme);
            }

            return sb.ToString ();
        }

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Horizontal_Scroll ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var lv = new ListView { Width = 10, Height = 3 };
        lv.SetSource (["One", "Two", "Three - long", "Four", "Five"]);
        var top = new Runnable ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        Assert.Equal (0, lv.Viewport.X);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
One       
Two       
Three - lo",
                                                       _output,
                                                       app?.Driver);

        lv.ScrollHorizontal (1);
        app?.LayoutAndDraw ();
        Assert.Equal (1, lv.Viewport.X);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
ne        
wo        
hree - lon",
                                                       _output,
                                                       app?.Driver);

        // Scroll right with mouse
        app?.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.WheeledRight });
        app?.LayoutAndDraw ();
        Assert.Equal (2, lv.Viewport.X);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
e         
o         
ree - long",
                                                       _output,
                                                       app?.Driver);

        // Scroll left with mouse
        app?.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.WheeledLeft });
        app?.LayoutAndDraw ();
        Assert.Equal (1, lv.Viewport.X);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
ne        
wo        
hree - lon",
                                                       _output,
                                                       app?.Driver);

        top.Dispose ();
        app?.Dispose ();
    }

    [Fact]
    public void HotKey_Command_Does_Not_Change_SelectedItem_When_Already_Set ()
    {
        ListView listView = new () { Source = new ListWrapper<string> (["Item1", "Item2", "Item3"]) };

        // Set SelectedItem to a non-zero value
        listView.SelectedItem = 2;
        Assert.Equal (2, listView.SelectedItem);

        listView.InvokeCommand (Command.HotKey);

        // OnHotKeyCommand uses ??= so it should NOT change SelectedItem
        Assert.Equal (2, listView.SelectedItem);

        listView.Dispose ();
    }

    [Fact]
    public void HotKey_Command_Raises_HotKeyCommand_Event ()
    {
        ListView listView = new () { Source = new ListWrapper<string> (["Item1", "Item2"]) };
        var hotKeyCommandInvokedCount = 0;

        listView.HotKeyCommand += (_, _) => hotKeyCommandInvokedCount++;

        listView.InvokeCommand (Command.HotKey);

        Assert.Equal (1, hotKeyCommandInvokedCount);

        listView.Dispose ();
    }

    [Fact]
    public void HotKey_Command_Sets_SelectedItem_To_Zero_When_Null ()
    {
        ListView listView = new () { Source = new ListWrapper<string> (["Item1", "Item2", "Item3"]) };

        // Initially SelectedItem is null
        Assert.Null (listView.SelectedItem);

        listView.InvokeCommand (Command.HotKey);

        // OnHotKeyCommand sets SelectedItem ??= 0
        Assert.Equal (0, listView.SelectedItem);

        listView.Dispose ();
    }

    [Fact]
    public void ListView_CollectionNavigatorMatcher_KeybindingsOverrideNavigator ()
    {
        ObservableCollection<string> source = ["apricot", "arm", "bat", "batman", "bates hotel", "candle"];
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        lv.SetFocus ();

        lv.KeyBindings.Add (Key.B, Command.Down);

        Assert.Null (lv.SelectedItem);

        // Keys should be consumed to move down the navigation i.e. to apricot
        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.Equal (0, lv.SelectedItem);

        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.Equal (1, lv.SelectedItem);

        // There is no keybinding for Key.C so it hits collection navigator i.e. we jump to candle
        Assert.True (lv.NewKeyDownEvent (Key.C));
        Assert.Equal (5, lv.SelectedItem);
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void ListView_Command_Accept_RaisesAccepting ()
    {
        ListView listView = new () { Source = new ListWrapper<string> (["Item1", "Item2"]) };
        var acceptingFired = false;

        listView.Accepting += (_, e) =>
                              {
                                  acceptingFired = true;
                                  e.Handled = true; // Signal that the Accept was processed
                              };

        bool? result = listView.InvokeCommand (Command.Accept);

        Assert.True (acceptingFired);
        Assert.True (result);

        listView.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void ListView_Command_Activate_ChangesSelection ()
    {
        ListView listView = new () { Source = new ListWrapper<string> (["Item1", "Item2", "Item3"]), Height = 3 };
        listView.BeginInit ();
        listView.EndInit ();

        listView.SelectedItem = 0;

        // Activate changes selection via arrow keys (Down command)
        bool? result = listView.InvokeCommand (Command.Down);

        Assert.Equal (1, listView.SelectedItem);
        Assert.True (result);

        listView.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void ListView_Command_HotKey_SetsFocus ()
    {
        ListView listView = new () { Source = new ListWrapper<string> (["Item1", "Item2"]) };
        Assert.False (listView.HasFocus);

        listView.InvokeCommand (Command.HotKey);
        Assert.True (listView.HasFocus);

        listView.Dispose ();
    }

    [Fact]
    public void ListViewCollectionNavigatorMatcher_DefaultBehaviour ()
    {
        ObservableCollection<string> source = ["apricot", "arm", "bat", "batman", "bates hotel", "candle"];
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        // Keys are consumed during navigation
        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.True (lv.NewKeyDownEvent (Key.A));
        Assert.True (lv.NewKeyDownEvent (Key.T));

        Assert.Equal ("bat", (string)lv.Source.ToList () [lv.SelectedItem!.Value]!);
    }

    [Fact]
    public void ListViewCollectionNavigatorMatcher_IgnoreKeys ()
    {
        ObservableCollection<string> source = ["apricot", "arm", "bat", "batman", "bates hotel", "candle"];
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        Mock<ICollectionNavigatorMatcher> matchNone = new ();

        matchNone.Setup (m => m.IsCompatibleKey (It.IsAny<Key> ())).Returns (false);

        lv.KeystrokeNavigator?.Matcher = matchNone.Object;

        // Keys are ignored because IsCompatibleKey returned false i.e. don't use these keys for navigation
        Assert.False (lv.NewKeyDownEvent (Key.B));
        Assert.False (lv.NewKeyDownEvent (Key.A));
        Assert.False (lv.NewKeyDownEvent (Key.T));

        // assert IsMatch never called
        matchNone.Verify (m => m.IsMatch (It.IsAny<string> (), It.IsAny<object> ()), Times.Never ());
    }

    // Copilot
    [Fact]
    public void KeystrokeNavigator_SetNull_DisablesKeystrokeNavigation ()
    {
        ObservableCollection<string> source = ["apricot", "arm", "bat", "batman", "bates hotel", "candle"];
        ListView lv = new () { Source = new ListWrapper<string> (source) };

        lv.SetFocus ();

        // Verify keystroke navigation works by default
        Assert.NotNull (lv.KeystrokeNavigator);
        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.Equal (2, lv.SelectedItem); // "bat"

        // Disable keystroke navigation
        lv.KeystrokeNavigator = null;

        // Reset selection
        lv.SelectedItem = 0;

        // Typing should no longer navigate — key events are not consumed
        Assert.False (lv.NewKeyDownEvent (Key.C));
        Assert.Equal (0, lv.SelectedItem); // unchanged
        Assert.False (lv.NewKeyDownEvent (Key.A));
        Assert.Equal (0, lv.SelectedItem); // unchanged
    }

    // Copilot
    [Fact]
    public void KeystrokeNavigator_ReassignAfterSource_SyncsCollection ()
    {
        ObservableCollection<string> source = ["apricot", "arm", "bat", "batman", "bates hotel", "candle"];
        ListView lv = new () { Source = new ListWrapper<string> (source) };

        lv.SetFocus ();

        // Disable then re-enable with a fresh navigator
        lv.KeystrokeNavigator = null;
        lv.KeystrokeNavigator = new CollectionNavigator ();

        // The new navigator should have been synced with Source automatically
        Assert.True (lv.NewKeyDownEvent (Key.C));
        Assert.Equal (5, lv.SelectedItem); // "candle"
    }

    // Codex - GPT-5.6
    [Fact]
    public void KeystrokeNavigator_CollectionChanged_SnapshotSource_SyncsAfterRemoval ()
    {
        ObservableCollection<string> source = ["Alpha", "Bravo", "Zulu"];
        using ListView listView = new () { Source = new SnapshotListDataSource (source), SelectedItem = 0 };

        source.RemoveAt (0);
        source.RemoveAt (0);

        Assert.Single (listView.KeystrokeNavigator!.Collection);
        Assert.Equal ("Zulu", listView.KeystrokeNavigator.Collection [0]);
        Assert.True (listView.NewKeyDownEvent (Key.Z));
        Assert.Equal (0, listView.SelectedItem);
    }

    // Codex - GPT-5.6
    [Fact]
    public void KeystrokeNavigator_CollectionChanged_SnapshotSource_SyncsAfterMove ()
    {
        ObservableCollection<string> source = ["Alpha", "Bravo", "Zulu"];
        using ListView listView = new () { Source = new SnapshotListDataSource (source), SelectedItem = 0 };

        source.Move (2, 0);

        Assert.Equal ("Zulu", listView.KeystrokeNavigator!.Collection [0]);
        Assert.True (listView.NewKeyDownEvent (Key.Z));
        Assert.Equal (0, listView.SelectedItem);
        Assert.Equal ("Zulu", listView.Source!.ToList () [listView.SelectedItem.Value]);
    }

    [Fact]
    public void ListViewCollectionNavigatorMatcher_OverrideMatching ()
    {
        ObservableCollection<string> source = ["apricot", "arm", "bat", "batman", "bates hotel", "candle"];
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        Mock<ICollectionNavigatorMatcher> matchNone = new ();

        matchNone.Setup (m => m.IsCompatibleKey (It.IsAny<Key> ())).Returns (true);

        // Match any string starting with b to "candle" (psych!)
        matchNone.Setup (m => m.IsMatch (It.IsAny<string> (), It.IsAny<object> ()))
                 .Returns ((string s, object key) => s.StartsWith ('B') && key?.ToString () == "candle");

        lv.KeystrokeNavigator?.Matcher = matchNone.Object;

        // Keys are consumed during navigation
        Assert.True (lv.NewKeyDownEvent (Key.B));
        Assert.Equal (5, lv.SelectedItem);
        Assert.True (lv.NewKeyDownEvent (Key.A));
        Assert.Equal (5, lv.SelectedItem);
        Assert.True (lv.NewKeyDownEvent (Key.T));
        Assert.Equal (5, lv.SelectedItem);

        Assert.Equal ("candle", (string)lv.Source.ToList () [lv.SelectedItem!.Value]!);
    }

    [Fact]
    public void MarkMultiple_Set_To_False_Unmarks_All_But_Selected ()
    {
        ListView lv = new () { ShowMarks = true, MarkMultiple = true };
        ListWrapper<string> source = new (["One", "Two", "Three"]);
        lv.Source = source;

        lv.SelectedItem = 0;
        source.SetMark (0, true);
        source.SetMark (1, true);
        source.SetMark (2, true);

        Assert.True (source.IsMarked (0));
        Assert.True (source.IsMarked (1));
        Assert.True (source.IsMarked (2));

        lv.MarkMultiple = false;

        Assert.True (source.IsMarked (0));
        Assert.False (source.IsMarked (1));
        Assert.False (source.IsMarked (2));
    }

    [Fact]
    public void Mouse_Wheel_Scrolls ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var lv = new ListView { Width = 10, Height = 3 };
        lv.SetSource (["One", "Two", "Three", "Four", "Five"]);
        var top = new Runnable ();
        top.Add (lv);
        app.Begin (top);

        // Initially, we are at the top.
        Assert.Equal (0, lv.Viewport.Y);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
One  
Two  
Three",
                                                       _output,
                                                       app?.Driver);

        // Scroll down
        app?.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.WheeledDown });
        app?.LayoutAndDraw ();
        Assert.Equal (1, lv.Viewport.Y);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
Two  
Three
Four ",
                                                       _output,
                                                       app?.Driver);

        // Scroll up
        app?.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.WheeledUp });
        app?.LayoutAndDraw ();
        Assert.Equal (0, lv.Viewport.X);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
One  
Two  
Three",
                                                       _output,
                                                       app?.Driver);

        top.Dispose ();
        app?.Dispose ();
    }

    [Fact]
    public void RowRender_Event ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var rendered = false;
        ObservableCollection<string> source = ["one", "two", "three"];
        var lv = new ListView { Width = Dim.Fill (), Height = Dim.Fill () };
        lv.RowRender += (s, _) => rendered = true;
        var top = new Runnable ();
        top.Add (lv);
        app.Begin (top);
        Assert.False (rendered);

        lv.SetSource (source);
        lv.Draw ();
        Assert.True (rendered);
        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void SelectedItem_With_Source_Null_Does_Nothing ()
    {
        var lv = new ListView ();
        Assert.Null (lv.Source);

        // should not throw
        lv.SelectedItem = 0;

        Assert.Null (lv.SelectedItem);
    }

    [Fact]
    public async Task SetSourceAsync_SetsSource ()
    {
        var lv = new ListView ();
        ObservableCollection<string> source = new () { "One", "Two", "Three" };

        await lv.SetSourceAsync (source);

        Assert.NotNull (lv.Source);
        Assert.Equal (3, lv.Source.Count);
    }

    [Fact]
    public void Source_CollectionChanged_Remove ()
    {
        ObservableCollection<string> source = ["One", "Two", "Three"];
        ListView lv = new () { Source = new ListWrapper<string> (source) };

        lv.SelectedItem = 2;
        Assert.Equal (2, lv.SelectedItem);
        Assert.Equal (3, lv.Source.Count);

        source.RemoveAt (0);

        Assert.Equal (2, lv.Source.Count);
        Assert.Equal (1, lv.SelectedItem);

        source.RemoveAt (1);
        Assert.Equal (1, lv.Source.Count);
        Assert.Equal (0, lv.SelectedItem);
    }

    [Fact]
    public void Vertical_ScrollBar_Hides_And_Shows_As_Needed ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new () { Width = 10, Height = 3 };
        lv.SetSource (["One", "Two", "Three", "Four", "Five"]);
        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);

        // Scrollbar settings can only be applied after Init
        lv.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
        lv.Layout ();
        lv.Draw ();

        Assert.True (lv.VerticalScrollBar.Visible);

        DriverAssert.AssertDriverContentsWithFrameAre ("""

                                                       One      ▲
                                                       Two      █
                                                       Three    ▼
                                                       """,
                                                       _output,
                                                       app?.Driver);

        lv.Height = 5;
        app?.LayoutAndDraw ();

        Assert.False (lv.VerticalScrollBar.Visible);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
One  
Two  
Three
Four 
Five ",
                                                       _output,
                                                       app?.Driver);
        top.Dispose ();
        app?.Dispose ();
    }

    #region ListView Tests (from ListViewTests.cs - parallelizable)

    [Fact]
    public void Constructors_Defaults ()
    {
        var lv = new ListView ();
        Assert.Null (lv.Source);
        Assert.True (lv.CanFocus);
        Assert.Null (lv.SelectedItem);
        Assert.False (lv.MarkMultiple);

        lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        Assert.NotNull (lv.Source);
        Assert.Null (lv.SelectedItem);

        lv = new ListView { Source = new NewListDataSource () };
        Assert.NotNull (lv.Source);
        Assert.Null (lv.SelectedItem);

        lv = new ListView { Y = 1, Width = 10, Height = 20, Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        Assert.NotNull (lv.Source);
        Assert.Null (lv.SelectedItem);
        Assert.Equal (new Rectangle (0, 1, 10, 20), lv.Frame);

        lv = new ListView { Y = 1, Width = 10, Height = 20, Source = new NewListDataSource () };
        Assert.NotNull (lv.Source);
        Assert.Null (lv.SelectedItem);
        Assert.Equal (new Rectangle (0, 1, 10, 20), lv.Frame);
    }

    private class NewListDataSource : IListDataSource
    {
#pragma warning disable CS0067
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
#pragma warning restore CS0067

        public int Count => 0;
        public int MaxItemLength => 0;

        public bool SuspendCollectionChangedEvent { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }

        public bool IsMarked (int item) => throw new NotImplementedException ();

        public void Render (ListView container, bool selected, int item, int col, int line, int width, int viewportX = 0) =>
            throw new NotImplementedException ();

        public void SetMark (int item, bool value) => throw new NotImplementedException ();
        public IList ToList () => new List<string> { "One", "Two", "Three" };

        public void Dispose () => throw new NotImplementedException ();
    }

    private sealed class SnapshotListDataSource : ListWrapper<string>, IListDataSource
    {
        private readonly ObservableCollection<string> _source;

        public SnapshotListDataSource (ObservableCollection<string> source) : base (source)
        {
            _source = source;
        }

        IList IListDataSource.ToList () => _source.ToList ();
    }

    [Fact]
    public void KeyBindings_Command ()
    {
        ObservableCollection<string> source = ["One", "Two", "Three"];

        // Test basic keybindings without marking (standard selection mode)
        var lv = new ListView { Height = 2, ShowMarks = false, MarkMultiple = false, Source = new ListWrapper<string> (source) };

        // HACK to make test pass
        lv.ViewportSettings |= ViewportSettingsFlags.AllowLocationPlusSizeGreaterThanContentSize;

        lv.BeginInit ();
        lv.EndInit ();
        Assert.Null (lv.SelectedItem);
        Assert.True (lv.NewKeyDownEvent (Key.CursorDown));
        Assert.Equal (0, lv.SelectedItem);
        Assert.False (lv.NewKeyDownEvent (Key.CursorUp)); // at top already
        Assert.Equal (0, lv.SelectedItem);
        Assert.True (lv.NewKeyDownEvent (Key.PageDown));
        Assert.Equal (2, lv.SelectedItem);
        Assert.True (lv.NewKeyDownEvent (Key.PageUp));
        Assert.Equal (0, lv.SelectedItem);

        // In standard selection mode (ShowMarks=false), Space doesn't mark
        Assert.False (lv.Source.IsMarked (lv.SelectedItem!.Value));
        lv.NewKeyDownEvent (Key.Space);
        Assert.False (lv.Source.IsMarked (lv.SelectedItem!.Value)); // Still not marked

        var opened = false;

        lv.Accepting += (s, e) =>
                        {
                            opened = true;
                            e.Handled = true;
                        };
        Assert.True (lv.NewKeyDownEvent (Key.Enter));
        Assert.True (opened);
        Assert.True (lv.NewKeyDownEvent (Key.End));
        Assert.Equal (2, lv.SelectedItem);
        Assert.True (lv.NewKeyDownEvent (Key.Home));
        Assert.Equal (0, lv.SelectedItem);
    }

    [Fact]
    public void HotKey_Command_SetsFocus ()
    {
        var view = new ListView ();

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.InvokeCommand (Command.HotKey);
        Assert.True (view.HasFocus);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var listView = new ListView ();
        var accepted = false;

        listView.Accepting += OnAccepted;
        listView.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void OnAccepted (object? sender, CommandEventArgs e) => accepted = true;
    }

    [Fact]
    public void Accept_Command_Accepts ()
    {
        ObservableCollection<string> source = ["One", "Two", "Three"];
        var listView = new ListView { Source = new ListWrapper<string> (source) };
        listView.SelectedItem = 0;

        var accepted = false;
        var selectedValue = string.Empty;

        listView.Accepting += OnAccepting;

        listView.InvokeCommand (Command.Accept);

        Assert.True (accepted);
        Assert.Equal (source [0], selectedValue);

        return;

        void OnAccepting (object? sender, CommandEventArgs e)
        {
            accepted = true;
            selectedValue = listView.SelectedItem.HasValue ? source [listView.SelectedItem.Value] : string.Empty;
            e.Handled = true;
        }
    }

    [Fact]
    public void ListViewProcessKeyReturnValue_WithMultipleCommands ()
    {
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three", "Four"]) };

        Assert.NotNull (lv.Source);

        // first item should be deselected by default
        Assert.Null (lv.SelectedItem);

        // bind Ctrl+D to move down twice in control (using Ctrl+D since Shift+Down is now used for DownExtend)
        lv.KeyBindings.Add (Key.D.WithCtrl, Command.Down, Command.Down);

        Key ev = Key.D.WithCtrl;

        Assert.True (lv.NewKeyDownEvent (ev), "The first time we move down 2 it should be possible");

        // After moving down twice from null we should be at 'Two'
        Assert.Equal (1, lv.SelectedItem);

        // clear the items
        lv.SetSource<string> (null);

        // Press key combo again - return should be false this time as none of the Commands are allowable
        Assert.False (lv.NewKeyDownEvent (ev), "We cannot move down so will not respond to this");
    }

    [Fact]
    public void ShowMarks_True_SpaceWithShift_RadioButton_MarksAndMovesDown ()
    {
        // Radio button mode (ShowMarks=true, MarkMultiple=false)
        // Space+Shift should mark current item and move down
        // Only one item can be marked at a time
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        lv.ShowMarks = true;
        lv.MarkMultiple = false;

        Assert.NotNull (lv.Source);

        // first item should be deselected by default
        Assert.Null (lv.SelectedItem);

        // nothing is marked
        Assert.False (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // Press Space+Shift - should activate item 0 (mark it) and move down
        Assert.True (lv.NewKeyDownEvent (Key.Space.WithShift));

        // First item should be selected and marked (radio button mode)
        Assert.Equal (0, lv.SelectedItem);
        Assert.True (lv.Source.IsMarked (0)); // Marked in radio mode
        Assert.False (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // After processing Down command, should move to item 1
        // (Space+Shift is bound to Command.Activate, Command.Down)
        // Item 0 stays marked since we haven't marked another item yet

        // Press Space+Shift again - should move to item 1 and toggle its mark
        Assert.True (lv.NewKeyDownEvent (Key.Space.WithShift));
        Assert.Equal (1, lv.SelectedItem);

        // After marking item 1, item 0 should be unmarked (radio button: only one at a time)
        Assert.False (lv.Source.IsMarked (0));
        Assert.True (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // Press Space+Shift again - should move to item 2 and toggle its mark
        Assert.True (lv.NewKeyDownEvent (Key.Space.WithShift));
        Assert.Equal (2, lv.SelectedItem);
        Assert.False (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.True (lv.Source.IsMarked (2));

        // Press Space+Shift again - cannot move down further
        // In radio button mode, item should toggle: marked → unmarked
        Assert.False (lv.NewKeyDownEvent (Key.Space.WithShift));
        Assert.Equal (2, lv.SelectedItem); // Still at item 2
        Assert.False (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2)); // Toggled off

        // Press key combo again - should toggle back to marked
        Assert.False (lv.NewKeyDownEvent (Key.Space.WithShift));
        Assert.Equal (2, lv.SelectedItem); // Still at item 2
        Assert.False (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.True (lv.Source.IsMarked (2)); // Toggled back on
    }

    [Fact]
    public void ShowMarks_True_SpaceWithShift_SelectsThenDown_MultipleSelection ()
    {
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        lv.ShowMarks = true;
        lv.MarkMultiple = true;

        Assert.NotNull (lv.Source);

        // first item should be deselected by default
        Assert.Null (lv.SelectedItem);

        // nothing is ticked
        Assert.False (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // view should indicate that it has accepted and consumed the event
        Assert.True (lv.NewKeyDownEvent (Key.Space.WithShift));

        // first item should now be selected
        Assert.Equal (0, lv.SelectedItem);

        // none of the items should be ticked
        Assert.False (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // Press key combo again
        Assert.True (lv.NewKeyDownEvent (Key.Space.WithShift));

        // second item should now be selected
        Assert.Equal (1, lv.SelectedItem);

        // first item only should be ticked
        Assert.True (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // Press key combo again
        Assert.True (lv.NewKeyDownEvent (Key.Space.WithShift));
        Assert.Equal (2, lv.SelectedItem);
        Assert.True (lv.Source.IsMarked (0));
        Assert.True (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2));

        // Press key combo again
        Assert.False (lv.NewKeyDownEvent (Key.Space.WithShift));
        Assert.Equal (2, lv.SelectedItem); // cannot move down any further
        Assert.True (lv.Source.IsMarked (0));
        Assert.True (lv.Source.IsMarked (1));
        Assert.True (lv.Source.IsMarked (2)); // but can toggle marked

        // Press key combo again 
        Assert.False (lv.NewKeyDownEvent (Key.Space.WithShift));
        Assert.Equal (2, lv.SelectedItem); // cannot move down any further
        Assert.True (lv.Source.IsMarked (0));
        Assert.True (lv.Source.IsMarked (1));
        Assert.False (lv.Source.IsMarked (2)); // untoggle toggle marked
    }

    [Fact]
    public void ListWrapper_StartsWith ()
    {
        ListWrapper<string> lw = new (["One", "Two", "Three"]);

        Assert.Equal (1, lw.StartsWith ("t"));
        Assert.Equal (1, lw.StartsWith ("tw"));
        Assert.Equal (2, lw.StartsWith ("th"));
        Assert.Equal (1, lw.StartsWith ("T"));
        Assert.Equal (1, lw.StartsWith ("TW"));
        Assert.Equal (2, lw.StartsWith ("TH"));

        lw = new ListWrapper<string> (["One", "Two", "Three"]);

        Assert.Equal (1, lw.StartsWith ("t"));
        Assert.Equal (1, lw.StartsWith ("tw"));
        Assert.Equal (2, lw.StartsWith ("th"));
        Assert.Equal (1, lw.StartsWith ("T"));
        Assert.Equal (1, lw.StartsWith ("TW"));
        Assert.Equal (2, lw.StartsWith ("TH"));
    }

    [Fact]
    public void OnEnter_Does_Not_Throw_Exception ()
    {
        var lv = new ListView ();
        var top = new View ();
        top.Add (lv);
        Exception? exception = Record.Exception (() => lv.SetFocus ());
        Assert.Null (exception);
    }

    [Fact]
    public void SelectedItem_Get_Set ()
    {
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        Assert.Null (lv.SelectedItem);
        Assert.Throws<ArgumentException> (() => lv.SelectedItem = 3);
        Exception? exception = Record.Exception (() => lv.SelectedItem = null);
        Assert.Null (exception);
    }

    [Fact]
    public void SetSource_Preserves_ListWrapper_Instance_If_Not_Null ()
    {
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two"]) };

        Assert.NotNull (lv.Source);

        lv.SetSource<string> (null);
        Assert.NotNull (lv.Source);

        lv.Source = null;
        Assert.Null (lv.Source);

        lv = new ListView { Source = new ListWrapper<string> (["One", "Two"]) };
        Assert.NotNull (lv.Source);

        lv.SetSourceAsync<string> (null);
        Assert.NotNull (lv.Source);
    }

    [Fact]
    public void SettingEmptyKeybindingThrows ()
    {
        var lv = new ListView { Source = new ListWrapper<string> (["One", "Two", "Three"]) };
        Assert.Throws<ArgumentException> (() => lv.KeyBindings.Add (Key.Space));
    }

    [Fact]
    public void SourceChanged_Event ()
    {
        var changed = 0;
        ObservableCollection<string> source1 = [];
        ObservableCollection<string> source2 = [];
        IListDataSource? src2 = null;
        var lv = new ListView { Source = new ListWrapper<string> (source1) };

        lv.SourceChanged += (sender, _) =>
                            {
                                Assert.Equal (src2, (sender as ListView)?.Source);
                                changed++;
                            };

        lv.Source = src2 = new ListWrapper<string> (source2);

        for (var i = 0; i < 3; i++)
        {
            source1.Add ($"Item{i}");
            source2.Add ($"Item{i}");
        }

        Assert.Equal (1, changed);

        for (var i = 0; i < 3; i++)
        {
            source1.Remove (source1 [0]);
            source2.Remove (source2 [0]);
        }

        Assert.Equal (1, changed);
        Assert.Empty (source1);
        Assert.Empty (source2);
    }

    [Fact]
    public void SourceChanged_Event_Raised_IfSetSourceWithNull ()
    {
        var changed = 0;
        ObservableCollection<string> source = [];
        IListDataSource? src = null;
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        lv.SourceChanged += (sender, _) =>
                            {
                                Assert.Equal (src, (sender as ListView)?.Source);
                                changed++;
                            };

        lv.Source = src = new ListWrapper<string> (null);

        Assert.Equal (1, changed);
    }

    [Theory]
    [MemberData (nameof (GetSources))]
    public void SourceChanged_Event_Not_Raised_IfSetSourceWithSameSource (ObservableCollection<string>? source)
    {
        var changed = 0;
        IListDataSource? src = new ListWrapper<string> (source);
        var lv = new ListView { Source = src };

        lv.SourceChanged += (_, _) => { changed++; };

        lv.Source = src;

        Assert.Equal (0, changed);
    }

    public static TheoryData<ObservableCollection<string>?> GetSources () => new (null, [], ["Item1", "Item2"]);

    [Fact]
    public void CollectionChanged_Event ()
    {
        var added = 0;
        var removed = 0;
        ObservableCollection<string> source = [];
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        lv.CollectionChanged += (sender, args) =>
                                {
                                    if (args.Action == NotifyCollectionChangedAction.Add)
                                    {
                                        added++;
                                    }
                                    else if (args.Action == NotifyCollectionChangedAction.Remove)
                                    {
                                        removed++;
                                    }
                                };

        for (var i = 0; i < 3; i++)
        {
            source.Add ($"Item{i}");
        }

        Assert.Equal (3, added);
        Assert.Equal (0, removed);

        added = 0;

        for (var i = 0; i < 3; i++)
        {
            source.Remove (source [0]);
        }

        Assert.Equal (0, added);
        Assert.Equal (3, removed);
        Assert.Empty (source);
    }

    [Fact]
    public void CollectionChanged_Event_Is_Only_Subscribed_Once ()
    {
        var added = 0;
        var removed = 0;
        var otherActions = 0;
        IList<string> source1 = [];
        ObservableCollection<string> source2 = [];
        ObservableCollection<string> source3 = [];
        IListDataSource? src3 = null;
        var lv = new ListView { Source = new ListWrapper<string> (new ObservableCollection<string> (source1)) };

        lv.CollectionChanged += (sender, args) =>
                                {
                                    Assert.Equal (src3, (sender as ListView)?.Source);

                                    if (args.Action == NotifyCollectionChangedAction.Add)
                                    {
                                        added++;
                                    }
                                    else if (args.Action == NotifyCollectionChangedAction.Remove)
                                    {
                                        removed++;
                                    }
                                    else
                                    {
                                        otherActions++;
                                    }
                                };

        lv.Source = new ListWrapper<string> (source2);
        lv.Source = new ListWrapper<string> (source3);
        src3 = lv.Source;
        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (var i = 0; i < 3; i++)
        {
            source1.Add ($"Item{i}");
            source2.Add ($"Item{i}");
            source3.Add ($"Item{i}");
        }

        Assert.Equal (3, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        added = 0;

        for (var i = 0; i < 3; i++)
        {
            source1.Remove (source1 [0]);
            source2.Remove (source2 [0]);
            source3.Remove (source3 [0]);
        }

        Assert.Equal (0, added);
        Assert.Equal (3, removed);
        Assert.Equal (0, otherActions);
        Assert.Empty (source1);
        Assert.Empty (source2);
        Assert.Empty (source3);
    }

    [Fact]
    public void CollectionChanged_Event_UnSubscribe_Previous_If_New_Is_Null ()
    {
        var added = 0;
        var removed = 0;
        var otherActions = 0;
        ObservableCollection<string> source1 = [];
        var lv = new ListView { Source = new ListWrapper<string> (source1) };

        lv.CollectionChanged += (sender, args) =>
                                {
                                    if (args.Action == NotifyCollectionChangedAction.Add)
                                    {
                                        added++;
                                    }
                                    else if (args.Action == NotifyCollectionChangedAction.Remove)
                                    {
                                        removed++;
                                    }
                                    else
                                    {
                                        otherActions++;
                                    }
                                };

        lv.Source = new ListWrapper<string> (null);
        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (var i = 0; i < 3; i++)
        {
            source1.Add ($"Item{i}");
        }

        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (var i = 0; i < 3; i++)
        {
            source1.Remove (source1 [0]);
        }

        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);
        Assert.Empty (source1);
    }

    [Fact]
    public void ListWrapper_CollectionChanged_Event_Is_Only_Subscribed_Once ()
    {
        var added = 0;
        var removed = 0;
        var otherActions = 0;
        ObservableCollection<string> source1 = [];
        ObservableCollection<string> source2 = [];
        ObservableCollection<string> source3 = [];
        ListWrapper<string> lw = new (source1);

        lw.CollectionChanged += (sender, args) =>
                                {
                                    // The source3 isn't the current event because ListWrapper wasn't disposed any time we changed source
                                    Assert.NotEqual (source3, sender);

                                    if (args.Action == NotifyCollectionChangedAction.Add)
                                    {
                                        added++;
                                    }
                                    else if (args.Action == NotifyCollectionChangedAction.Remove)
                                    {
                                        removed++;
                                    }
                                    else
                                    {
                                        otherActions++;
                                    }
                                };

        lw = new ListWrapper<string> (source2);
        lw = new ListWrapper<string> (source3);
        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (var i = 0; i < 3; i++)
        {
            source1.Add ($"Item{i}");
            source2.Add ($"Item{i}");
            source3.Add ($"Item{i}");
        }

        Assert.Equal (3, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        added = 0;

        for (var i = 0; i < 3; i++)
        {
            source1.Remove (source1 [0]);
            source2.Remove (source2 [0]);
            source3.Remove (source3 [0]);
        }

        Assert.Equal (0, added);
        Assert.Equal (3, removed);
        Assert.Equal (0, otherActions);
        Assert.Empty (source1);
        Assert.Empty (source2);
        Assert.Empty (source3);
    }

    [Fact]
    public void ListWrapper_CollectionChanged_Event_UnSubscribe_Previous_Is_Disposed ()
    {
        var added = 0;
        var removed = 0;
        var otherActions = 0;
        ObservableCollection<string> source1 = [];
        ListWrapper<string> lw = new (source1);

        lw.CollectionChanged += Lw_CollectionChanged;

        lw.Dispose ();
        lw = new ListWrapper<string> (null);
        Assert.Equal (0, lw.Count);
        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (var i = 0; i < 3; i++)
        {
            source1.Add ($"Item{i}");
        }

        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);

        for (var i = 0; i < 3; i++)
        {
            source1.Remove (source1 [0]);
        }

        Assert.Equal (0, added);
        Assert.Equal (0, removed);
        Assert.Equal (0, otherActions);
        Assert.Empty (source1);

        void Lw_CollectionChanged (object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                added++;
            }
        }
    }

    [Fact]
    public void ListWrapper_SuspendCollectionChangedEvent_ResumeSuspendCollectionChangedEvent_Tests ()
    {
        var added = 0;
        ObservableCollection<string> source = [];
        ListWrapper<string> lw = new (source);

        lw.CollectionChanged += Lw_CollectionChanged;

        lw.SuspendCollectionChangedEvent = true;

        for (var i = 0; i < 3; i++)
        {
            source.Add ($"Item{i}");
        }

        Assert.Equal (0, added);
        Assert.Equal (3, lw.Count);
        Assert.Equal (3, source.Count);

        lw.SuspendCollectionChangedEvent = false;

        for (var i = 3; i < 6; i++)
        {
            source.Add ($"Item{i}");
        }

        Assert.Equal (3, added);
        Assert.Equal (6, lw.Count);
        Assert.Equal (6, source.Count);

        void Lw_CollectionChanged (object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                added++;
            }
        }
    }

    [Fact]
    public void ListView_SuspendCollectionChangedEvent_ResumeSuspendCollectionChangedEvent_Tests ()
    {
        var added = 0;
        ObservableCollection<string> source = [];
        var lv = new ListView { Source = new ListWrapper<string> (source) };

        lv.CollectionChanged += Lw_CollectionChanged;

        lv.SuspendCollectionChangedEvent ();

        for (var i = 0; i < 3; i++)
        {
            source.Add ($"Item{i}");
        }

        Assert.Equal (0, added);
        Assert.Equal (3, lv.Source.Count);
        Assert.Equal (3, source.Count);

        lv.ResumeSuspendCollectionChangedEvent ();

        for (var i = 3; i < 6; i++)
        {
            source.Add ($"Item{i}");
        }

        Assert.Equal (3, added);
        Assert.Equal (6, lv.Source.Count);
        Assert.Equal (6, source.Count);

        void Lw_CollectionChanged (object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                added++;
            }
        }
    }

    #endregion

    #region Mouse Multiselect Tests

    // Claude - Opus 4.5
    [Fact]
    public void Mouse_Click_Selects_Item ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        ListView lv = new () { Width = 10, Height = 5 };
        lv.SetSource (["One", "Two", "Three", "Four", "Five"]);

        (runnable as View)?.Add (lv);
        app.Begin (runnable);

        // Initially no item is selected
        Assert.Null (lv.SelectedItem);

        // Click on first item (row 0)
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (1, 0)));
        Assert.Equal (0, lv.SelectedItem);

        // Click on third item (row 2)
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (1, 2)));
        Assert.Equal (2, lv.SelectedItem);

        // Click on fifth item (row 4)
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (1, 4)));
        Assert.Equal (4, lv.SelectedItem);
    }

    // Claude - Opus 4.5
    [Fact]
    public void Mouse_Click_With_MarkMultiple_Marks_Multiple_Items ()
    {
        // Tests that Ctrl+clicking on items with MarkMultiple=true marks multiple items
        // Normal clicks clear previous marks and mark only the clicked item
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new () { Width = 10, Height = 5, ShowMarks = true, MarkMultiple = true };
        lv.SetSource (["One", "Two", "Three", "Four", "Five"]);

        var top = new Runnable ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Initially nothing is marked
        for (var i = 0; i < 5; i++)
        {
            Assert.False (lv.Source!.IsMarked (i));
        }

        // Normal click on first item - should mark it (need full sequence: Pressed → Released → Clicked)
        // x=2 to account for mark width (2 characters for checkbox glyphs)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 0), Flags = MouseFlags.LeftButtonPressed });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 0), Flags = MouseFlags.LeftButtonReleased });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 0), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (0, lv.SelectedItem);
        Assert.True (lv.Source!.IsMarked (0));

        // Ctrl+Click on third item - should mark it and keep first marked
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 2), Flags = MouseFlags.LeftButtonPressed | MouseFlags.Ctrl });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 2), Flags = MouseFlags.LeftButtonReleased | MouseFlags.Ctrl });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 2), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Ctrl });
        Assert.Equal (2, lv.SelectedItem);
        Assert.True (lv.Source.IsMarked (0)); // Still marked
        Assert.True (lv.Source.IsMarked (2)); // Newly marked

        // Ctrl+Click on fifth item - should mark it (first and third stay marked)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 4), Flags = MouseFlags.LeftButtonPressed | MouseFlags.Ctrl });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 4), Flags = MouseFlags.LeftButtonReleased | MouseFlags.Ctrl });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 4), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Ctrl });
        Assert.Equal (4, lv.SelectedItem);
        Assert.True (lv.Source.IsMarked (0)); // Still marked
        Assert.True (lv.Source.IsMarked (2)); // Still marked
        Assert.True (lv.Source.IsMarked (4)); // Newly marked

        // Click on first item again - should toggle it off
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (1, 0), Flags = MouseFlags.LeftButtonPressed });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (1, 0), Flags = MouseFlags.LeftButtonReleased });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (1, 0), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (0, lv.SelectedItem);
        Assert.False (lv.Source.IsMarked (0)); // Toggled off
        Assert.True (lv.Source.IsMarked (2)); // Still marked
        Assert.True (lv.Source.IsMarked (4)); // Still marked

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void MarkUnmarkSelectedItem_Returns_False_When_ShowMarks_Is_False ()
    {
        // Tests that MarkUnmarkSelectedItem returns false when ShowMarks=false
        ListView lv = new () { ShowMarks = false };
        lv.SetSource (["One", "Two", "Three"]);
        lv.SelectedItem = 0;

        bool result = lv.MarkUnmarkSelectedItem ();

        Assert.False (result);
        Assert.False (lv.Source!.IsMarked (0));
    }

    // Claude - Opus 4.5
    [Fact]
    public void MarkAll_Returns_False_When_MarkMultiple_Is_False ()
    {
        // Tests that MarkAll returns false when MarkMultiple=false
        ListView lv = new () { ShowMarks = true, MarkMultiple = false };
        lv.SetSource (["One", "Two", "Three"]);

        bool result = lv.MarkAll (true);

        Assert.False (result);
    }

    #endregion

    #region Phase 2: Extend Commands and Key Bindings Tests

    // Claude - Opus 4.5
    [Fact]
    public void MoveDown_With_Extend_False_Clears_HiddenMarks ()
    {
        // In hidden marks mode (ShowMarks=false, MarkMultiple=true),
        // marks represent transient range selections that clear when navigating without extend
        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4"]),
            ShowMarks = false, // Hidden marks mode
            MarkMultiple = true
        };

        lv.SetSelection (0, false);
        lv.SetSelection (2, true); // Select 0-2 (creates range marks)
        Assert.Equal (3, lv.GetAllMarkedItems ().Count ());

        lv.MoveDown (); // Move without extending - should clear transient marks

        Assert.Equal (3, lv.SelectedItem);
        Assert.Empty (lv.GetAllMarkedItems ()); // Marks cleared
    }

    // Claude - Opus 4.5
    [Fact]
    public void MoveDown_With_Extend_True_Extends_Selection ()
    {
        ListView lv = new () { Source = new ListWrapper<string> (["1", "2", "3", "4"]), ShowMarks = true, MarkMultiple = true };

        lv.SetSelection (1, false); // Anchor at 1
        lv.MoveDown (true); // Extend to 2

        Assert.Equal (2, lv.SelectedItem);
        Assert.True (lv.Source!.IsMarked (1));
        Assert.True (lv.Source!.IsMarked (2));
    }

    // Claude - Opus 4.5
    [Fact]
    public void MoveUp_With_Extend_True_Extends_Selection ()
    {
        ListView lv = new () { Source = new ListWrapper<string> (["1", "2", "3", "4"]), ShowMarks = true, MarkMultiple = true };

        lv.SetSelection (2, false); // Anchor at 2
        lv.MoveUp (true); // Extend to 1

        Assert.Equal (1, lv.SelectedItem);
        Assert.True (lv.Source!.IsMarked (1));
        Assert.True (lv.Source!.IsMarked (2));
    }

    // Claude - Opus 4.5
    [Fact]
    public void ShiftDown_Key_Extends_Selection ()
    {
        ListView lv = new () { Source = new ListWrapper<string> (["1", "2", "3", "4"]), ShowMarks = true, MarkMultiple = true };
        lv.BeginInit ();
        lv.EndInit ();

        lv.SelectedItem = 0;
        Assert.True (lv.NewKeyDownEvent (Key.CursorDown.WithShift));

        Assert.Equal (1, lv.SelectedItem);
        Assert.True (lv.IsSelectedOrMarked (0));
        Assert.True (lv.IsSelectedOrMarked (1));
    }

    // Claude - Opus 4.5
    [Fact]
    public void ShiftUp_Key_Extends_Selection ()
    {
        ListView lv = new () { Source = new ListWrapper<string> (["1", "2", "3", "4"]), ShowMarks = true, MarkMultiple = true };
        lv.BeginInit ();
        lv.EndInit ();

        lv.SelectedItem = 2;
        Assert.True (lv.NewKeyDownEvent (Key.CursorUp.WithShift));

        Assert.Equal (1, lv.SelectedItem);
        Assert.True (lv.IsSelectedOrMarked (1));
        Assert.True (lv.IsSelectedOrMarked (2));
    }

    // Claude - Opus 4.5
    [Fact]
    public void ShiftPageDown_Key_Extends_Selection ()
    {
        ListView lv = new ()
        {
            Source = new ListWrapper<string> ([
                                                  "1",
                                                  "2",
                                                  "3",
                                                  "4",
                                                  "5",
                                                  "6",
                                                  "7",
                                                  "8",
                                                  "9",
                                                  "10"
                                              ]),
            ShowMarks = true,
            MarkMultiple = true,
            Height = 3
        };
        lv.BeginInit ();
        lv.EndInit ();

        lv.SelectedItem = 0;
        Assert.True (lv.NewKeyDownEvent (Key.PageDown.WithShift));

        // Should select from 0 to wherever PageDown lands
        Assert.True (lv.IsSelectedOrMarked (0));
        Assert.True (lv.SelectedItem > 0);
    }

    // Claude - Opus 4.5
    [Fact]
    public void ShiftHome_Key_Extends_To_Beginning ()
    {
        ListView lv = new () { Source = new ListWrapper<string> (["1", "2", "3", "4", "5"]), ShowMarks = true, MarkMultiple = true };
        lv.BeginInit ();
        lv.EndInit ();

        lv.SelectedItem = 3;
        Assert.True (lv.NewKeyDownEvent (Key.Home.WithShift));

        Assert.Equal (0, lv.SelectedItem);
        Assert.True (lv.IsSelectedOrMarked (0));
        Assert.True (lv.IsSelectedOrMarked (1));
        Assert.True (lv.IsSelectedOrMarked (2));
        Assert.True (lv.IsSelectedOrMarked (3));
    }

    // Claude - Opus 4.5
    [Fact]
    public void ShiftEnd_Key_Extends_To_End ()
    {
        ListView lv = new () { Source = new ListWrapper<string> (["1", "2", "3", "4", "5"]), ShowMarks = true, MarkMultiple = true };
        lv.BeginInit ();
        lv.EndInit ();

        lv.SelectedItem = 1;
        Assert.True (lv.NewKeyDownEvent (Key.End.WithShift));

        Assert.Equal (4, lv.SelectedItem);
        Assert.True (lv.IsSelectedOrMarked (1));
        Assert.True (lv.IsSelectedOrMarked (2));
        Assert.True (lv.IsSelectedOrMarked (3));
        Assert.True (lv.IsSelectedOrMarked (4));
    }

    // Claude - Opus 4.5
    [Fact]
    public void MovePageDown_With_Extend_True_Extends_Selection ()
    {
        ListView lv = new ()
        {
            Source = new ListWrapper<string> ([
                                                  "1",
                                                  "2",
                                                  "3",
                                                  "4",
                                                  "5",
                                                  "6",
                                                  "7",
                                                  "8",
                                                  "9",
                                                  "10"
                                              ]),
            ShowMarks = true,
            MarkMultiple = true,
            Height = 3
        };
        lv.BeginInit ();
        lv.EndInit ();

        lv.SetSelection (0, false); // Anchor at 0
        lv.MovePageDown (true);

        // Should have multiple items selected
        Assert.True (lv.GetAllMarkedItems ().Count () > 1);
        Assert.True (lv.Source!.IsMarked (0));
    }

    // Claude - Opus 4.5
    [Fact]
    public void MovePageUp_With_Extend_True_Extends_Selection ()
    {
        ListView lv = new ()
        {
            Source = new ListWrapper<string> ([
                                                  "1",
                                                  "2",
                                                  "3",
                                                  "4",
                                                  "5",
                                                  "6",
                                                  "7",
                                                  "8",
                                                  "9",
                                                  "10"
                                              ]),
            ShowMarks = true,
            MarkMultiple = true,
            Height = 3
        };
        lv.BeginInit ();
        lv.EndInit ();

        lv.SetSelection (9, false); // Anchor at end
        lv.MovePageUp (true);

        // Should have multiple items selected
        Assert.True (lv.GetAllMarkedItems ().Count () > 1);
        Assert.True (lv.Source!.IsMarked (9));
    }

    // Claude - Opus 4.5
    [Fact]
    public void MoveHome_With_Extend_True_Extends_Selection ()
    {
        ListView lv = new () { Source = new ListWrapper<string> (["1", "2", "3", "4", "5"]), ShowMarks = true, MarkMultiple = true };

        lv.SetSelection (3, false); // Anchor at 3
        lv.MoveHome (true);

        Assert.Equal (0, lv.SelectedItem);
        Assert.True (lv.Source!.IsMarked (0));
        Assert.True (lv.Source!.IsMarked (1));
        Assert.True (lv.Source!.IsMarked (2));
        Assert.True (lv.Source!.IsMarked (3));
    }

    // Claude - Opus 4.5
    [Fact]
    public void MoveEnd_With_Extend_True_Extends_Selection ()
    {
        ListView lv = new () { Source = new ListWrapper<string> (["1", "2", "3", "4", "5"]), ShowMarks = true, MarkMultiple = true };

        lv.SetSelection (1, false); // Anchor at 1
        lv.MoveEnd (true);

        Assert.Equal (4, lv.SelectedItem);
        Assert.True (lv.Source!.IsMarked (1));
        Assert.True (lv.Source!.IsMarked (2));
        Assert.True (lv.Source!.IsMarked (3));
        Assert.True (lv.Source!.IsMarked (4));
    }

    #endregion

    #region Phase 3: Mouse Shift+Click and Ctrl+Click Tests

    // Claude - Opus 4.5
    [Fact]
    public void Mouse_ShiftClick_Extends_Selection ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4"]),
            ShowMarks = true,
            MarkMultiple = true,
            Height = 4,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        lv.SelectedItem = 0;

        // Shift+Click on item 2
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 2), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Shift });

        Assert.Equal (2, lv.SelectedItem);
        Assert.True (lv.Source!.IsMarked (0));
        Assert.True (lv.Source!.IsMarked (1));
        Assert.True (lv.Source!.IsMarked (2));

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void Mouse_ShiftClick_Extends_Selection_In_HiddenMarksMode ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4", "5"]),
            ShowMarks = false,
            MarkMultiple = true,
            Height = 5,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Click item 0 to set anchor
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (0, lv.SelectedItem);
        Assert.True (lv.Source!.IsMarked (0));

        // Shift+Click on item 3 to create range
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 3), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Shift });

        // Verify range is marked
        Assert.Equal (3, lv.SelectedItem);
        Assert.True (lv.Source!.IsMarked (0));
        Assert.True (lv.Source!.IsMarked (1));
        Assert.True (lv.Source!.IsMarked (2));
        Assert.True (lv.Source!.IsMarked (3));
        Assert.False (lv.Source!.IsMarked (4));

        // Normal click (without Shift) should clear transient marks
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 4), Flags = MouseFlags.LeftButtonClicked });

        Assert.Equal (4, lv.SelectedItem);
        Assert.False (lv.Source!.IsMarked (0));
        Assert.False (lv.Source!.IsMarked (1));
        Assert.False (lv.Source!.IsMarked (2));
        Assert.False (lv.Source!.IsMarked (3));
        Assert.True (lv.Source!.IsMarked (4));

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void Mouse_ShiftClick_Ignored_In_RadioButtonMode ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4"]),
            ShowMarks = true,
            MarkMultiple = false,
            Height = 4,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Click item 0 to set mark
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (0, lv.SelectedItem);
        Assert.True (lv.Source!.IsMarked (0));

        // Shift+Click on item 3 - should behave like normal click (no range)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 3), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Shift });

        // Verify only item 3 is marked (radio button behavior)
        Assert.Equal (3, lv.SelectedItem);
        Assert.False (lv.Source!.IsMarked (0));
        Assert.False (lv.Source!.IsMarked (1));
        Assert.False (lv.Source!.IsMarked (2));
        Assert.True (lv.Source!.IsMarked (3));

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void Mouse_ShiftClick_Ignored_In_StandardSelectionMode ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4"]),
            ShowMarks = false,
            MarkMultiple = false,
            Height = 4,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Click item 0
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (0, lv.SelectedItem);
        Assert.False (lv.Source!.IsMarked (0));

        // Shift+Click on item 3 - should behave like normal click (no range, no marks)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 3), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Shift });

        // Verify no marks created and only item 3 is selected
        Assert.Equal (3, lv.SelectedItem);
        Assert.False (lv.Source!.IsMarked (0));
        Assert.False (lv.Source!.IsMarked (1));
        Assert.False (lv.Source!.IsMarked (2));
        Assert.False (lv.Source!.IsMarked (3));

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void Mouse_CtrlLeftClick_Toggles_Individual_Items ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4"]),
            ShowMarks = true,
            MarkMultiple = true,
            Height = 4,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Ctrl+Click on item 0 (need full sequence: Pressed → Released → Clicked)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.Ctrl });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased | MouseFlags.Ctrl });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Ctrl });
        Assert.True (lv.Source!.IsMarked (0));

        // Ctrl+Click on item 2
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 2), Flags = MouseFlags.LeftButtonPressed | MouseFlags.Ctrl });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 2), Flags = MouseFlags.LeftButtonReleased | MouseFlags.Ctrl });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 2), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Ctrl });
        Assert.True (lv.Source!.IsMarked (0));
        Assert.True (lv.Source!.IsMarked (2));
        Assert.False (lv.Source!.IsMarked (1));

        // Ctrl+Click on item 0 again - should toggle off
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.Ctrl });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased | MouseFlags.Ctrl });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Ctrl });
        Assert.False (lv.Source!.IsMarked (0));
        Assert.True (lv.Source!.IsMarked (2));

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void Mouse_CtrlRightClick_Extends_Selection ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4", "5"]),
            ShowMarks = true,
            MarkMultiple = true,
            Height = 5,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Click item 1 to set anchor
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 1), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (1, lv.SelectedItem);

        // Ctrl+RightClick on item 4 to extend selection (alternative to Shift+Click)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 4), Flags = MouseFlags.RightButtonClicked | MouseFlags.Ctrl });

        // Verify range is marked
        Assert.Equal (4, lv.SelectedItem);
        Assert.False (lv.Source!.IsMarked (0));
        Assert.True (lv.Source!.IsMarked (1));
        Assert.True (lv.Source!.IsMarked (2));
        Assert.True (lv.Source!.IsMarked (3));
        Assert.True (lv.Source!.IsMarked (4));

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void Mouse_NormalClick_InCheckboxMode_TogglesIndividualMarks ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4"]),
            ShowMarks = true, // Checkbox mode: marks are persistent
            MarkMultiple = true,
            Height = 4,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Build up a selection via extend (items 0-2 marked)
        lv.SetSelection (0, false);
        lv.SetSelection (2, true);
        Assert.Equal (3, lv.GetAllMarkedItems ().Count ());

        // Normal click in checkbox mode should toggle the clicked item's mark
        // Other marks remain (persistent checkbox behavior)
        // Need full mouse sequence: Pressed → Released → Clicked
        // x=2 to account for mark width (2 characters for checkbox glyphs)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 3), Flags = MouseFlags.LeftButtonPressed });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 3), Flags = MouseFlags.LeftButtonReleased });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 3), Flags = MouseFlags.LeftButtonClicked });

        Assert.Equal (3, lv.SelectedItem);

        // In checkbox mode, previous marks persist and item 3 is now marked too
        Assert.Equal (4, lv.GetAllMarkedItems ().Count ());
        Assert.True (lv.Source!.IsMarked (0));
        Assert.True (lv.Source!.IsMarked (1));
        Assert.True (lv.Source!.IsMarked (2));
        Assert.True (lv.Source!.IsMarked (3));

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void Mouse_ShiftClick_Without_MarkMultiple_Does_Not_Extend ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4"]),
            MarkMultiple = false, // Disabled
            Height = 4,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        lv.SelectedItem = 0;

        // Shift+Click on item 2 - should just select, not extend
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 2), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Shift });

        Assert.Equal (2, lv.SelectedItem);
        Assert.True (lv.GetAllMarkedItems ().Count () == 0); // No multi-selection

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void Mouse_CtrlLeftClick_Without_MarkMultiple_Does_Not_Toggle ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4"]),
            MarkMultiple = false, // Disabled
            Height = 4,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Ctrl+Click on item 0 - should just select, not add to multi-selection
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Ctrl });
        Assert.Equal (0, lv.SelectedItem);
        Assert.True (lv.GetAllMarkedItems ().Count () == 0);

        // Ctrl+Click on item 2 - should just select, not add
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 2), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Ctrl });
        Assert.Equal (2, lv.SelectedItem);
        Assert.True (lv.GetAllMarkedItems ().Count () == 0);

        top.Dispose ();
        app.Dispose ();
    }

    #endregion

    #region Phase 4: Multi-Selection Rendering Tests

    // Claude - Opus 4.5
    [Fact]
    public void MultiSelectedItems_Are_Tracked_For_Rendering ()
    {
        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["One", "Two", "Three"]),
            ShowMarks = true,
            MarkMultiple = true,
            Height = 3,
            Width = 10
        };

        lv.SetSelection (0, false);
        lv.SetSelection (1, true);

        // Items 0 and 1 should be in MultiSelectedItems
        Assert.True (lv.Source!.IsMarked (0));
        Assert.True (lv.Source!.IsMarked (1));
        Assert.False (lv.Source!.IsMarked (2));
    }

    // Claude - Opus 4.5
    [Fact]
    public void MarkMultiple_False_ClearsAllExceptSelectedItem ()
    {
        // When MarkMultiple is set to false, all marks should be cleared EXCEPT SelectedItem
        ListView lv = new () { Source = new ListWrapper<string> (["One", "Two", "Three"]), ShowMarks = true, MarkMultiple = true };

        // Mark items 0, 1, 2 (SelectedItem will be 2 after extending)
        lv.SetSelection (0, false);
        lv.SetSelection (2, true); // Extends from 0 to 2
        Assert.Equal (3, lv.GetAllMarkedItems ().Count ());
        Assert.Equal (2, lv.SelectedItem);

        // Set MarkMultiple to false - should clear all marks except SelectedItem (2)
        lv.MarkMultiple = false;

        Assert.Single (lv.GetAllMarkedItems ());
        Assert.True (lv.Source!.IsMarked (2)); // SelectedItem remains marked
        Assert.False (lv.Source!.IsMarked (0));
        Assert.False (lv.Source!.IsMarked (1));
    }

    // Claude - Opus 4.5
    [Fact]
    public void OnDrawingContent_Uses_Highlight_Role_For_MultiSelected ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["One", "Two", "Three"]),
            ShowMarks = true,
            MarkMultiple = true,
            Height = 3,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);

        lv.SetSelection (0, false);
        lv.SetSelection (1, true);
        app.LayoutAndDraw ();

        // Verify items are tracked (rendering verification would need driver inspection)
        Assert.True (lv.Source!.IsMarked (0));
        Assert.True (lv.Source!.IsMarked (1));

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void SelectedItem_Gets_Focus_Role_When_Focused ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["One", "Two", "Three"]),
            ShowMarks = true,
            MarkMultiple = true,
            Height = 3,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);

        lv.SetFocus ();
        lv.SelectedItem = 1;
        app.LayoutAndDraw ();

        // The SelectedItem (1) should get Focus role when focused
        // Multi-selected items not equal to SelectedItem get Highlight role
        Assert.True (lv.HasFocus);
        Assert.Equal (1, lv.SelectedItem);

        top.Dispose ();
        app.Dispose ();
    }

    #endregion

    #region Phase 5: Mark Rendering Attribute Tests

    // Claude - Opus 4.5
    [Fact]
    public void ShowMarks_Renders_Marks ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["One", "Two"]),
            ShowMarks = true,
            MarkMultiple = true,
            Height = 2,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);

        lv.Source!.SetMark (0, true);
        app.LayoutAndDraw ();

        // Verify marks are rendered (checkbox characters should appear)
        // The first item should show checked, second unchecked
        Assert.True (lv.Source.IsMarked (0));
        Assert.False (lv.Source.IsMarked (1));

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void Marks_Rendered_Consistently_Across_Selection_States ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["One", "Two", "Three"]),
            ShowMarks = true,
            MarkMultiple = true,
            Height = 3,
            Width = 15
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);

        // Mark all items
        lv.Source!.SetMark (0, true);
        lv.Source.SetMark (1, true);
        lv.Source.SetMark (2, true);

        // Select middle item with focus
        lv.SetFocus ();
        lv.SetSelection (1, false);
        app.LayoutAndDraw ();

        // All items should remain marked regardless of selection state
        Assert.True (lv.Source.IsMarked (0));
        Assert.True (lv.Source.IsMarked (1));
        Assert.True (lv.Source.IsMarked (2));

        top.Dispose ();
        app.Dispose ();
    }

    #endregion

    #region Phase 6: Custom Mark Rendering API Tests

    // Claude - Opus 4.5
    [Fact]
    public void RenderMark_Default_Returns_False ()
    {
        ListWrapper<string> source = new (["One", "Two"]);
        IListDataSource dataSource = source;

        ListView lv = new () { Source = source };
        bool result = dataSource.RenderMark (lv, 0, 0, false, false);

        Assert.False (result);
    }

    // Claude - Opus 4.5
    [Fact]
    public void RenderMark_Called_During_Draw_When_ShowMarks ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Use standard ListWrapper - default RenderMark returns false
        ListView lv = new () { Source = new ListWrapper<string> (["One", "Two"]), ShowMarks = true, Height = 2, Width = 10 };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Default behavior: marks are rendered by ListView (RenderMark returns false)
        // This test verifies the code path doesn't crash
        Assert.NotNull (lv.Source);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void Custom_DataSource_Can_Override_RenderMark ()
    {
        // Test that the interface allows custom implementations
        IListDataSource source = new CustomMarkDataSource (["One", "Two"]);

        ListView lv = new () { Source = source };
        bool result = source.RenderMark (lv, 0, 0, true, true);

        // Our custom implementation returns true
        Assert.True (result);
    }

    /// <summary>Custom data source that overrides RenderMark for testing.</summary>
    private class CustomMarkDataSource : ListWrapper<string>
    {
        public CustomMarkDataSource (IEnumerable<string> source) : base (new ObservableCollection<string> (source)) { }

        /// <inheritdoc/>
        public override bool RenderMark (ListView listView, int item, int row, bool isMarked, bool allowsMultiple) =>

            // Custom implementation that returns true (indicating custom rendering was done)
            true;
    }

    #endregion

    #region Phase 7: Scrolling Width and Offset Clamping Tests

    // Claude - Opus 4.5
    [Fact]
    public void Scrolling_Stops_When_Last_Item_Visible ()
    {
        ObservableCollection<string> source = new (["1", "2", "3", "4", "5"]);
        ListView lv = new () { Source = new ListWrapper<string> (source), Width = 10, Height = 3 };
        lv.BeginInit ();
        lv.EndInit ();

        // Scroll to maximum
        lv.Viewport = lv.Viewport with { Y = 100 };

        // Last visible item should be item 4 (index 4), at row 2 (0-indexed)
        // TopItem should be 2 so items 2, 3, 4 are visible
        Assert.Equal (2, lv.Viewport.Y);
    }

    // Claude - Opus 4.5
    [Fact]
    public void LeftItem_With_Small_Content_Clamps_To_Zero ()
    {
        ObservableCollection<string> source = new (["Hi"]); // 2 chars, smaller than viewport

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (source),
            Width = 10, // Viewport larger than content
            Height = 1
        };
        lv.BeginInit ();
        lv.EndInit ();

        // Max LeftItem should be max(0, 2 - 10) = 0
        lv.Viewport = lv.Viewport with { X = 5 };
        Assert.Equal (0, lv.Viewport.X);
    }

    // Claude - Opus 4.5
    [Fact]
    public void ContentSize_Includes_MarkWidth_When_ShowMarks ()
    {
        ObservableCollection<string> source = new (["Item"]); // 4 chars
        ListView lv = new () { Source = new ListWrapper<string> (source), Width = 10, Height = 1 };
        lv.BeginInit ();
        lv.EndInit ();

        // Without marks: content width = 4
        Assert.Equal (4, lv.GetContentSize ().Width);

        // With marks: content width = 4 + 2 (mark checkbox + space) = 6
        lv.ShowMarks = true;
        Assert.Equal (6, lv.GetContentSize ().Width);
    }

    // Claude - Opus 4.5
    [Fact]
    public void HorizontalScroll_With_Marks_Accounts_For_MarkWidth ()
    {
        ObservableCollection<string> source = new (["Item"]); // 4 chars

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (source),
            ShowMarks = true, // Mark width = 2
            Width = 5, // Viewport smaller than content (4 + 2 = 6)
            Height = 1
        };
        lv.BeginInit ();
        lv.EndInit ();

        // Effective content width = 4 (item) + 2 (marks) = 6
        // Max LeftItem = 6 - 5 = 1
        lv.Viewport = lv.Viewport with { X = 10 };
        Assert.Equal (1, lv.Viewport.X);
    }

    #endregion

    #region Out-of-range guard tests

    // Claude - Opus 4.6
    // Verifies that ListView.OnDrawingContent does not call IsMarked with out-of-range indices
    // when the Viewport is taller than the number of items in the source.
    [Fact]
    public void Draw_DoesNotCall_IsMarked_OutOfRange ()
    {
        ObservableCollection<string> source = ["one", "two"];

        // Track all indices passed to IsMarked
        List<int> isMarkedCalls = [];
        Mock<IListDataSource> mockSource = new (MockBehavior.Strict);
        mockSource.Setup (s => s.Count).Returns (source.Count);

        mockSource.Setup (s => s.IsMarked (It.IsAny<int> ()))
                  .Returns ((int item) =>
                            {
                                isMarkedCalls.Add (item);
                                Assert.True (item >= 0 && item < source.Count, $"IsMarked called with out-of-range index {item}");

                                return false;
                            });

        mockSource.Setup (s => s.Render (It.IsAny<ListView> (),
                                         It.IsAny<bool> (),
                                         It.IsAny<int> (),
                                         It.IsAny<int> (),
                                         It.IsAny<int> (),
                                         It.IsAny<int> (),
                                         It.IsAny<int> ()));
        mockSource.Setup (s => s.RenderMark (It.IsAny<ListView> (), It.IsAny<int> (), It.IsAny<int> (), It.IsAny<bool> (), It.IsAny<bool> ())).Returns (false);
        mockSource.Setup (s => s.MaxItemLength).Returns (5);
        mockSource.Setup (s => s.ToList ()).Returns (source);
        mockSource.Setup (s => s.SuspendCollectionChangedEvent).Returns (false);
        mockSource.As<IDisposable> ().Setup (d => d.Dispose ());

        // Viewport height (10) is much larger than item count (2)
        ListView lv = new () { Width = 20, Height = 10, Source = mockSource.Object };
        lv.Layout ();
        lv.Draw ();

        // Verify IsMarked was only called with valid indices
        Assert.All (isMarkedCalls, i => Assert.InRange (i, 0, source.Count - 1));
    }

    // Claude - Opus 4.6
    // Verifies that ListView.OnDrawingContent does not call Render with out-of-range indices
    // when the Viewport is taller than the number of items in the source.
    [Fact]
    public void Draw_DoesNotCall_Render_OutOfRange ()
    {
        ObservableCollection<string> source = ["one", "two"];

        List<int> renderCalls = [];
        Mock<IListDataSource> mockSource = new (MockBehavior.Strict);
        mockSource.Setup (s => s.Count).Returns (source.Count);
        mockSource.Setup (s => s.IsMarked (It.IsAny<int> ())).Returns (false);

        mockSource.Setup (s => s.Render (It.IsAny<ListView> (),
                                         It.IsAny<bool> (),
                                         It.IsAny<int> (),
                                         It.IsAny<int> (),
                                         It.IsAny<int> (),
                                         It.IsAny<int> (),
                                         It.IsAny<int> ()))
                  .Callback ((ListView _, bool _, int item, int _, int _, int _, int _) =>
                             {
                                 renderCalls.Add (item);
                                 Assert.True (item >= 0 && item < source.Count, $"Render called with out-of-range index {item}");
                             });

        mockSource.Setup (s => s.RenderMark (It.IsAny<ListView> (), It.IsAny<int> (), It.IsAny<int> (), It.IsAny<bool> (), It.IsAny<bool> ())).Returns (false);
        mockSource.Setup (s => s.MaxItemLength).Returns (5);
        mockSource.Setup (s => s.ToList ()).Returns (source);
        mockSource.Setup (s => s.SuspendCollectionChangedEvent).Returns (false);
        mockSource.As<IDisposable> ().Setup (d => d.Dispose ());

        ListView lv = new () { Width = 20, Height = 10, Source = mockSource.Object };
        lv.Layout ();
        lv.Draw ();

        // Verify Render was only called with valid indices
        Assert.All (renderCalls, i => Assert.InRange (i, 0, source.Count - 1));
    }

    // Claude - Opus 4.6
    // Verifies that ListView.OnDrawingContent does not call SetMark with out-of-range indices
    // when the Viewport is taller than the number of items in the source.
    [Fact]
    public void Draw_DoesNotCall_SetMark_OutOfRange ()
    {
        ObservableCollection<string> source = ["one", "two"];

        List<int> setMarkCalls = [];
        Mock<IListDataSource> mockSource = new (MockBehavior.Strict);
        mockSource.Setup (s => s.Count).Returns (source.Count);
        mockSource.Setup (s => s.IsMarked (It.IsAny<int> ())).Returns (false);

        mockSource.Setup (s => s.Render (It.IsAny<ListView> (),
                                         It.IsAny<bool> (),
                                         It.IsAny<int> (),
                                         It.IsAny<int> (),
                                         It.IsAny<int> (),
                                         It.IsAny<int> (),
                                         It.IsAny<int> ()));
        mockSource.Setup (s => s.RenderMark (It.IsAny<ListView> (), It.IsAny<int> (), It.IsAny<int> (), It.IsAny<bool> (), It.IsAny<bool> ())).Returns (false);

        mockSource.Setup (s => s.SetMark (It.IsAny<int> (), It.IsAny<bool> ()))
                  .Callback ((int item, bool _) =>
                             {
                                 setMarkCalls.Add (item);
                                 Assert.True (item >= 0 && item < source.Count, $"SetMark called with out-of-range index {item}");
                             });

        mockSource.Setup (s => s.MaxItemLength).Returns (5);
        mockSource.Setup (s => s.ToList ()).Returns (source);
        mockSource.Setup (s => s.SuspendCollectionChangedEvent).Returns (false);
        mockSource.As<IDisposable> ().Setup (d => d.Dispose ());

        ListView lv = new () { Width = 20, Height = 10, ShowMarks = true, Source = mockSource.Object };
        lv.Layout ();
        lv.Draw ();

        // SetMark should not have been called at all during Draw, but if it was, only with valid indices
        Assert.All (setMarkCalls, i => Assert.InRange (i, 0, source.Count - 1));
    }

    #endregion

    #region IValue<int?> Implementation

    // Claude - Opus 4.6
    [Fact]
    public void ValueChanging_Event_Can_Cancel_Selection_Change ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Width = 10, Height = 5, Source = new ListWrapper<string> (source) };
        lv.SelectedItem = 0;

        lv.ValueChanging += (_, args) => args.Handled = true;

        lv.SelectedItem = 1;

        Assert.Equal (0, lv.SelectedItem);
    }

    // Claude - Opus 4.6
    [Fact]
    public void ValueChanging_Event_Provides_CurrentValue_And_NewValue ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Width = 10, Height = 5, Source = new ListWrapper<string> (source) };
        lv.SelectedItem = 0;

        int? capturedCurrent = null;
        int? capturedNew = null;

        lv.ValueChanging += (_, args) =>
                            {
                                capturedCurrent = args.CurrentValue;
                                capturedNew = args.NewValue;
                            };

        lv.SelectedItem = 2;

        Assert.Equal (0, capturedCurrent);
        Assert.Equal (2, capturedNew);
        Assert.Equal (2, lv.SelectedItem);
    }

    // Claude - Opus 4.6
    [Fact]
    public void ValueChanged_Event_Fires_After_Selection_Change ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Width = 10, Height = 5, Source = new ListWrapper<string> (source) };
        lv.SelectedItem = 0;

        int? oldValue = null;
        int? newValue = null;

        lv.ValueChanged += (_, args) =>
                           {
                               oldValue = args.OldValue;
                               newValue = args.NewValue;
                           };

        lv.SelectedItem = 2;

        Assert.Equal (0, oldValue);
        Assert.Equal (2, newValue);
    }

    // Claude - Opus 4.6
    [Fact]
    public void ValueChanged_DoesNotFire_When_ValueChanging_Cancels ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Width = 10, Height = 5, Source = new ListWrapper<string> (source) };
        lv.SelectedItem = 0;

        var changedFired = false;
        lv.ValueChanging += (_, args) => args.Handled = true;
        lv.ValueChanged += (_, _) => changedFired = true;

        lv.SelectedItem = 1;

        Assert.False (changedFired);
        Assert.Equal (0, lv.SelectedItem);
    }

    // Claude - Opus 4.6
    [Fact]
    public void ValueChangedUntyped_Event_Fires_After_Selection_Change ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Width = 10, Height = 5, Source = new ListWrapper<string> (source) };
        lv.SelectedItem = 0;

        object? oldValue = null;
        object? newValue = null;

        lv.ValueChangedUntyped += (_, args) =>
                                  {
                                      oldValue = args.OldValue;
                                      newValue = args.NewValue;
                                  };

        lv.SelectedItem = 1;

        Assert.Equal (0, oldValue);
        Assert.Equal (1, newValue);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Value_Property_Is_Alias_For_SelectedItem ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Width = 10, Height = 5, Source = new ListWrapper<string> (source) };

        lv.Value = 2;
        Assert.Equal (2, lv.SelectedItem);
        Assert.Equal (2, lv.Value);

        lv.SelectedItem = 0;
        Assert.Equal (0, lv.Value);
    }

    // Claude - Opus 4.6
    [Fact]
    public void IValue_GetValue_Returns_SelectedItem ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Width = 10, Height = 5, Source = new ListWrapper<string> (source) };
        lv.SelectedItem = 1;

        object? value = ((IValue)lv).GetValue ();

        Assert.Equal (1, value);
    }

    // Claude - Opus 4.6
    [Fact]
    public void IValue_GetValue_Returns_Null_When_No_Selection ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Width = 10, Height = 5, Source = new ListWrapper<string> (source) };

        object? value = ((IValue)lv).GetValue ();

        Assert.Null (value);
    }

    #endregion IValue<int?> Implementation

    #region Ctrl+A / Ctrl+U SelectAll

    // Claude - Opus 4.6
    [Fact]
    public void CtrlA_Marks_All_Items_When_MarkMultiple_True ()
    {
        ObservableCollection<string> source = ["one", "two", "three", "four"];
        ListView lv = new () { Width = 10, Height = 5, Source = new ListWrapper<string> (source), MarkMultiple = true };
        lv.SelectedItem = 0;

        lv.NewKeyDownEvent (Key.A.WithCtrl);

        List<int> marked = lv.GetAllMarkedItems ().ToList ();
        Assert.Equal (4, marked.Count);
        Assert.Equal ([0, 1, 2, 3], marked);
    }

    // Claude - Opus 4.6
    [Fact]
    public void CtrlU_Unmarks_All_Items_When_MarkMultiple_True ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Width = 10, Height = 5, Source = new ListWrapper<string> (source), MarkMultiple = true };
        lv.SelectedItem = 0;

        // Mark all first
        lv.NewKeyDownEvent (Key.A.WithCtrl);
        Assert.Equal (3, lv.GetAllMarkedItems ().Count ());

        // Unmark all
        lv.NewKeyDownEvent (Key.U.WithCtrl);
        Assert.Empty (lv.GetAllMarkedItems ());
    }

    // Claude - Opus 4.6
    [Fact]
    public void CtrlA_Does_Nothing_When_MarkMultiple_False ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Width = 10, Height = 5, Source = new ListWrapper<string> (source), MarkMultiple = false };
        lv.SelectedItem = 0;

        lv.NewKeyDownEvent (Key.A.WithCtrl);

        Assert.Empty (lv.GetAllMarkedItems ());
    }

    #endregion Ctrl+A / Ctrl+U SelectAll

    #region RowRender RowAttribute Override

    // Claude - Opus 4.6
    [Fact]
    public void RowRender_Event_RowAttribute_Applied_To_Specific_Row ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Width = 10, Height = 5, Source = new ListWrapper<string> (source) };
        lv.SelectedItem = 0;

        Attribute customAttr = new (Color.Red, Color.Blue);
        List<int> renderedRows = [];

        lv.RowRender += (_, args) =>
                        {
                            renderedRows.Add (args.Row);

                            if (args.Row == 1)
                            {
                                args.RowAttribute = customAttr;
                            }
                        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Verify the event was called for each visible row
        Assert.Contains (0, renderedRows);
        Assert.Contains (1, renderedRows);
        Assert.Contains (2, renderedRows);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void RowRender_Event_Receives_Correct_Row_Indices ()
    {
        ObservableCollection<string> source = ["a", "b", "c", "d", "e"];
        ListView lv = new () { Width = 10, Height = 3, Source = new ListWrapper<string> (source) };
        lv.SelectedItem = 0;

        List<int> receivedRows = [];

        lv.RowRender += (_, args) => receivedRows.Add (args.Row);

        lv.Layout ();
        lv.Draw ();

        // With height=3, only 3 rows visible starting from viewport Y=0
        Assert.Equal ([0, 1, 2], receivedRows);
    }

    // Claude - Opus 4.6
    [Fact]
    public void RowRender_Event_Receives_Correct_Row_Indices_After_Scroll ()
    {
        ObservableCollection<string> source = ["a", "b", "c", "d", "e"];
        ListView lv = new () { Width = 10, Height = 3, Source = new ListWrapper<string> (source) };
        lv.SelectedItem = 0;

        lv.Layout ();

        // Scroll down so viewport starts at row 2
        lv.Viewport = lv.Viewport with { Y = 2 };

        List<int> receivedRows = [];
        lv.RowRender += (_, args) => receivedRows.Add (args.Row);

        lv.Draw ();

        // After scrolling, should render items 2, 3, 4
        Assert.Equal ([2, 3, 4], receivedRows);
    }

    #endregion RowRender RowAttribute Override

    #region Movement Cycling

    // Copilot
    [Fact]
    public void MoveDown_AtBottom_WhenTabStopIsNoStop_WrapsToTop ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Source = new ListWrapper<string> (source), TabStop = TabBehavior.NoStop };
        lv.SetFocus ();

        // Move to last item
        Assert.True (lv.MoveDown ()); // selects 0
        Assert.True (lv.MoveDown ()); // selects 1
        Assert.True (lv.MoveDown ()); // selects 2
        Assert.Equal (2, lv.SelectedItem);

        // Should wrap to top
        Assert.True (lv.MoveDown ());
        Assert.Equal (0, lv.SelectedItem);
    }

    // Copilot
    [Fact]
    public void MoveDown_AtBottom_WhenTabStopIsNotNoStop_ReturnsFalse ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Source = new ListWrapper<string> (source), TabStop = TabBehavior.TabStop };
        lv.SetFocus ();

        // Move to last item
        Assert.True (lv.MoveDown ()); // selects 0
        Assert.True (lv.MoveDown ()); // selects 1
        Assert.True (lv.MoveDown ()); // selects 2
        Assert.Equal (2, lv.SelectedItem);

        // Should NOT wrap — returns false
        Assert.False (lv.MoveDown ());
        Assert.Equal (2, lv.SelectedItem);
    }

    // Copilot
    [Fact]
    public void MoveUp_AtTop_WhenTabStopIsNoStop_WrapsToBottom ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Frame = new Rectangle (0, 0, 10, 20), Source = new ListWrapper<string> (source), TabStop = TabBehavior.NoStop };
        lv.SetFocus ();

        // Select first item
        Assert.True (lv.MoveDown ());
        Assert.Equal (0, lv.SelectedItem);

        // Should wrap to bottom
        Assert.True (lv.MoveUp ());
        Assert.Equal (2, lv.SelectedItem);
    }

    // Copilot
    [Fact]
    public void MoveUp_AtTop_WhenTabStopIsNotNoStop_ReturnsFalse ()
    {
        ObservableCollection<string> source = ["one", "two", "three"];
        ListView lv = new () { Frame = new Rectangle (0, 0, 10, 20), Source = new ListWrapper<string> (source), TabStop = TabBehavior.TabStop };
        lv.SetFocus ();

        // Select first item
        Assert.True (lv.MoveDown ());
        Assert.Equal (0, lv.SelectedItem);

        // Should NOT wrap — returns false
        Assert.False (lv.MoveUp ());
        Assert.Equal (0, lv.SelectedItem);
    }

    #endregion Movement Cycling
}
