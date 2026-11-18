using System.Drawing;
using AppTestHelpers;
using AppTestHelpers.XunitHelpers;

namespace IntegrationTests;

/// <summary>
///     Integration tests for AppTestHelper mouse event handling (LeftClick, RightClick).
/// </summary>
public class TestContextMouseEventTests (ITestOutputHelper outputHelper) : TestsAllDrivers
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Click_RaisesAccepting (string d)
    {
        var clickedCount = 0;

        Button button = new () { X = 5, Y = 5, Text = "Click Me" };
        button.Accepting += (_, _) => clickedCount++;

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out)
                                              .Add (button)
                                              .LeftClick (6, 6) // Click inside button (accounting for Window's border)
                                              .AssertEqual (1, clickedCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Click_TView_RaisesAccepting (string d)
    {
        var clickedCount = 0;

        Button button = new () { X = 5, Y = 5, Text = "Click Me" };
        button.Accepting += (_, _) => clickedCount++;

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out).Add (button).LeftClick<Button> (b => b.Text == "Click Me").AssertEqual (1, clickedCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Click_OnView_RaisesMouseEvent (string d)
    {
        var mouseReceived = false;
        var receivedPosition = Point.Empty;

        View view = new () { X = 10, Y = 5, Width = 20, Height = 5 };

        view.MouseEvent += (_, mouse) =>
                           {
                               mouseReceived = true;
                               receivedPosition = mouse.Position!.Value;
                           };

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out).Add (view).LeftClick (15, 7).AssertTrue (mouseReceived);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void MultipleClicks_ProcessesInOrder (string d)
    {
        var clickCount = 0;

        Button button = new () { X = 5, Y = 5, Text = "Click Me" };
        button.Accepting += (_, _) => clickCount++;

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out)
                                              .Add (button)
                                              .LeftClick (6, 6)
                                              .LeftClick (6, 6)
                                              .LeftClick (6, 6)
                                              .AssertEqual (3, clickCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void RightClick_RaisesCorrectEvent (string d)
    {
        var rightClickCount = 0;

        View view = new () { X = 10, Y = 5, Width = 20, Height = 5 };

        view.MouseEvent += (_, e) =>
                           {
                               if (e.Flags.HasFlag (MouseFlags.RightButtonClicked))
                               {
                                   rightClickCount++;
                               }
                           };

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out).Add (view).RightClick (15, 7).AssertEqual (1, rightClickCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Click_SetsFocusOnView (string d)
    {
        View view1 = new ()
        {
            Id = "view1",
            X = 5,
            Y = 5,
            Width = 10,
            Height = 5,
            CanFocus = true
        };

        View view2 = new ()
        {
            Id = "view2",
            X = 20,
            Y = 5,
            Width = 10,
            Height = 5,
            CanFocus = true
        };

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out)
                                              .Add (view1)
                                              .Add (view2)
                                              .Then (_ => view1.SetFocus ())
                                              .AssertTrue (view1.HasFocus)
                                              .LeftClick (25, 7) // Click on view2
                                              .AssertFalse (view1.HasFocus)
                                              .AssertTrue (view2.HasFocus);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void ChainedWithKeyboard_WorksCorrectly (string d)
    {
        var clickCount = 0;

        Button button = new () { X = 5, Y = 5, Text = "Click Me" };
        button.Accepting += (_, _) => clickCount++;

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out)
                                              .Add (button)
                                              .LeftClick (6, 6) // Click button to focus it
                                              .AssertEqual (1, clickCount)
                                              .AssertTrue (button.HasFocus)
                                              .KeyDown (Key.Enter) // Press Enter
                                              .AssertEqual (2, clickCount); // Should trigger button again
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Click_OnTextField_SetsCaretPosition (string d)
    {
        TextField textField = new () { X = 5, Y = 5, Width = 20, Text = "Hello World" };

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out)
                                              .Add (textField)
                                              .LeftClick (11, 6) // Click in middle of text (accounting for border)
                                              .AssertTrue (textField.HasFocus);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void RapidClicks_AllProcessed (string d)
    {
        var clickCount = 0;

        View view = new () { X = 10, Y = 5, Width = 20, Height = 5 };

        // Only count Clicked events, not all mouse events (Pressed, Released, Clicked)
        view.MouseEvent += (_, e) =>
                           {
                               if (e.Flags.HasFlag (MouseFlags.LeftButtonClicked))
                               {
                                   clickCount++;
                               }
                           };

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out).Add (view);

        // Rapid fire 10 clicks
        for (var i = 0; i < 10; i++)
        {
            helper.LeftClick (15, 7);
        }

        helper.AssertEqual (10, clickCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Click_OutsideView_DoesNotRaiseEvent (string d)
    {
        var clickCount = 0;

        View view = new () { X = 10, Y = 5, Width = 10, Height = 5 };

        view.MouseEvent += (_, _) => clickCount++;

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out)
                                              .Add (view)
                                              .LeftClick (5, 5) // Click outside view
                                              .AssertEqual (0, clickCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void ClickOnDisabledView_DoesNotTrigger (string d)
    {
        var clickCount = 0;

        Button button = new () { X = 5, Y = 5, Text = "Click Me", Enabled = false };
        button.Accepting += (_, _) => clickCount++;

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out)
                                              .Add (button)
                                              .LeftClick (6, 6)
                                              .AssertEqual (0, clickCount); // Should not increment because button is disabled
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void AfterResize_StillWorks (string d)
    {
        var clickCount = 0;

        Button button = new () { X = 5, Y = 5, Text = "Click Me" };
        button.Accepting += (_, _) => clickCount++;

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out).Add (button).ResizeConsole (50, 20).LeftClick (6, 6).AssertEqual (1, clickCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void WithCheckBox_TogglesState (string d)
    {
        CheckBox checkBox = new () { X = 5, Y = 5, Text = "Check Me" };

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out)
                                              .Add (checkBox)
                                              .AssertEqual (CheckState.UnChecked, checkBox.Value)
                                              .LeftClick (6, 6) // Click checkbox
                                              .AssertEqual (CheckState.Checked, checkBox.Value)
                                              .LeftClick (6, 6) // Click again
                                              .AssertEqual (CheckState.UnChecked, checkBox.Value);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void WithListView_SelectsItem (string d)
    {
        ListView listView = new () { X = 5, Y = 5, Width = 20, Height = 10 };
        listView.SetSource (["Item1", "Item2", "Item3", "Item4", "Item5"]);
        listView.SelectedItem = 0;

        using AppTestHelper helper = With.A<Window> (40, 20, d, _out)
                                              .Add (listView)
                                              .AssertEqual (0, listView.SelectedItem)
                                              .LeftClick (6, 7) // Click on Item2 (accounting for header/border)
                                              .AssertEqual (1, listView.SelectedItem);
    }
}
