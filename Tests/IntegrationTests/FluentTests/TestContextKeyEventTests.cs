using AppTestHelpers;
using AppTestHelpers.XunitHelpers;

namespace IntegrationTests;

/// <summary>
///     Integration tests for AppTestHelper keyboard event handling (InjectKeyEvent).
/// </summary>
public class TestContextKeyEventTests (ITestOutputHelper outputHelper) : TestsAllDrivers
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void QuitKey_ViaApplication_Stops (string d)
    {
        IRunnable? top = null;

        using AppTestHelper helper = With.A<Window> (40, 10, d)
                                               .Then (app =>
                                               {
                                                   top = app!.TopRunnable;
                                                   app.Keyboard.RaiseKeyDownEvent (Application.GetDefaultKey (Command.Quit));
                                               });

        Assert.True (
            SpinWait.SpinUntil (() => top is { IsRunning: false }, TimeSpan.FromSeconds (5)),
            "TopRunnable did not stop within timeout.");
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void QuitKey_ViaStops (string d)
    {
        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out);
        Assert.True (helper.App?.TopRunnable!.IsRunning);

        IRunnable? top = helper.App?.TopRunnable;
        helper.KeyDown (Application.GetDefaultKey (Command.Quit));
        helper.App?.Dispose ();

        Assert.False (top!.IsRunning);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void AfterResizeConsole_StillWorks (string d)
    {
        var keyReceived = false;
        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keyReceived = true;

        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out)
                                              .Add (view)
                                              .Then ((_) => view.SetFocus ())
                                              .ResizeConsole (50, 20)
                                              .KeyDown (Key.A);

        Assert.True (keyReceived);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Backspace_DeletesCharacter (string d)
    {
        var textField = new TextField { Text = "TEST", Width = 20 };

        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out)
                                              .Add (textField)
                                              .Focus (textField)
                                              .Then ((_) => textField.MoveEnd ())
                                              .KeyDown (Key.Backspace)
                                              .KeyDown (Key.Backspace);

        Assert.Equal ("TE", textField.Text);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void ChainedWithOtherOperations_WorksCorrectly (string d)
    {
        var textField = new TextField { Width = 20 };
        var clickedCount = 0;
        var button = new Button { Text = "Click Me" };
        button.Accepting += (s, e) => clickedCount++;

        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out)
                                              .Add (textField)
                                              .Add (button)
                                              .Then ((_) => textField.SetFocus ())
                                              .KeyDown (Key.T.WithShift)
                                              .KeyDown (Key.E)
                                              .KeyDown (Key.S)
                                              .KeyDown (Key.T)
                                              .AssertEqual ("Test", textField.Text)
                                              .KeyDown (Key.Tab)
                                              .Then ((_) => Assert.True (button.HasFocus))
                                              .KeyDown (Key.Enter)
                                              .AssertEqual (1, clickedCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void EnqueuesKeyAndProcessesIt (string d)
    {
        var keyReceived = false;
        var receivedKey = Key.Empty;

        var view = new View { CanFocus = true };

        view.KeyDown += (s, e) =>
        {
            keyReceived = true;
            receivedKey = e;
        };

        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out)
                                              .Add (view)
                                              .Then ((_) => view.SetFocus ())
                                              .KeyDown (Key.A);

        Assert.True (keyReceived, "Key was not received by the view");
        Assert.Equal (Key.A, receivedKey);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void FunctionKeys_ProcessesCorrectly (string d)
    {
        List<Key> keysReceived = [];

        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keysReceived.Add (e);

        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out)
                                              .Add (view)
                                              .Then ((_) => view.SetFocus ())
                                              .KeyDown (Key.F1)
                                              .KeyDown (Key.F5)
                                              .KeyDown (Key.F12);

        Assert.Equal (3, keysReceived.Count);
        Assert.Equal (Key.F1, keysReceived [0]);
        Assert.Equal (Key.F5, keysReceived [1]);
        Assert.Equal (Key.F12, keysReceived [2]);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void MultipleKeys_ProcessesInOrder (string d)
    {
        List<Key> keysReceived = [];

        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keysReceived.Add (e);

        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out)
                                              .Add (view)
                                              .Then ((_) => view.SetFocus ())
                                              .KeyDown (Key.A)
                                              .KeyDown (Key.B)
                                              .KeyDown (Key.C);

        Assert.Equal (3, keysReceived.Count);
        Assert.Equal (Key.A, keysReceived [0]);
        Assert.Equal (Key.B, keysReceived [1]);
        Assert.Equal (Key.C, keysReceived [2]);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void NavigationKeys_ChangeFocus (string d)
    {
        var view1 = new View { Id = "view1", CanFocus = true };
        var view2 = new View { Id = "view2", CanFocus = true };

        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out)
                                              .Add (view1)
                                              .Add (view2)
                                              .Then ((_) => view1.SetFocus ())
                                              .AssertTrue (view1.HasFocus)
                                              .AssertFalse (view2.HasFocus)
                                              .KeyDown (Key.Tab)
                                              .AssertFalse (view1.HasFocus)
                                              .AssertTrue (view2.HasFocus)
                                              .KeyDown (Key.Tab.WithShift)
                                              .AssertTrue (view1.HasFocus)
                                              .AssertFalse (view2.HasFocus);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void NumericKeys_ProcessesCorrectly (string d)
    {
        var textField = new TextField { Width = 20 };

        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out)
                                              .Add (textField)
                                              .Then ((_) => textField.SetFocus ())
                                              .KeyDown (Key.D1)
                                              .KeyDown (Key.D2)
                                              .KeyDown (Key.D3)
                                              .KeyDown (Key.D4)
                                              .KeyDown (Key.D5);

        Assert.Equal ("12345", textField.Text);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void RapidSequence_ProcessesAllKeys (string d)
    {
        List<Key> keysReceived = [];
        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keysReceived.Add (e);

        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out)
                                              .Add (view)
                                              .Then ((_) => view.SetFocus ());

        // Send 10 keys rapidly
        for (var i = 0; i < 10; i++)
        {
            helper.KeyDown ((Key)(Key.A.KeyCode + (uint)i));
        }

        Assert.Equal (10, keysReceived.Count);

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal ((Key)(Key.A.KeyCode + (uint)i), keysReceived [i]);
        }
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void SpecialKeys_ProcessesCorrectly (string d)
    {
        List<Key> keysReceived = [];

        var view = new View { CanFocus = true };
        view.KeyDown += (s, e) => keysReceived.Add (e);

        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out)
                                              .Add (view)
                                              .Then ((_) => view.SetFocus ())
                                              .KeyDown (Key.Enter)
                                              .KeyDown (Key.Tab)
                                              .KeyDown (Key.CursorUp)
                                              .KeyDown (Key.CursorDown)
                                              .KeyDown (Key.Esc);

        Assert.Equal (5, keysReceived.Count);
        Assert.Equal (Key.Enter, keysReceived [0]);
        Assert.Equal (Key.Tab, keysReceived [1]);
        Assert.Equal (Key.CursorUp, keysReceived [2]);
        Assert.Equal (Key.CursorDown, keysReceived [3]);
        Assert.Equal (Key.Esc, keysReceived [4]);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void WithListView_NavigatesItems (string d)
    {
        var listView = new ListView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        listView.SetSource (["Item1", "Item2", "Item3", "Item4", "Item5"]);
        listView.SelectedItem = 0;

        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out)
                                              .Add (listView)
                                              .Then ((_) => listView.SetFocus ())
                                              .AssertEqual (0, listView.SelectedItem)
                                              .KeyDown (Key.CursorDown)
                                              .AssertEqual (1, listView.SelectedItem)
                                              .KeyDown (Key.CursorDown)
                                              .AssertEqual (2, listView.SelectedItem)
                                              .KeyDown (Key.CursorUp)
                                              .AssertEqual (1, listView.SelectedItem);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void WithModifiers_ProcessesCorrectly (string d)
    {
        var keyReceived = false;
        var receivedKey = Key.Empty;

        var view = new View { CanFocus = true };

        view.KeyDown += (s, e) =>
        {
            keyReceived = true;
            receivedKey = e;
        };

        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out)
                                              .Add (view)
                                              .Then ((_) => view.SetFocus ())
                                              .KeyDown (Key.A.WithCtrl);

        Assert.True (keyReceived);
        Assert.Equal (Key.A.WithCtrl, receivedKey);
        Assert.True (receivedKey.IsCtrl);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void WithTextField_UpdatesText (string d)
    {
        var textField = new TextField { Width = 20 };

        using AppTestHelper helper = With.A<Window> (40, 10, d,  _out)
                                              .Add (textField)
                                              .KeyDown (Key.H.WithShift)
                                              .KeyDown (Key.E)
                                              .KeyDown (Key.L)
                                              .KeyDown (Key.L)
                                              .KeyDown (Key.O);

        //Assert.Equal ("Hello", textField.Text);
    }
}
