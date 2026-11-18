using UnitTests;

namespace ViewsTests;

/// <summary>
///     Comprehensive tests for all 4 combinations of ShowMarks and MarkMultiple.
/// </summary>
public class ListViewTests_Combinations
{
    #region Combination 1: ShowMarks=false, MarkMultiple=false (Standard Selection)

    // Claude - Opus 4.5
    [Fact]
    public void Combination1_StandardSelection_NoMarkingAllowed ()
    {
        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4"]),
            ShowMarks = false,
            MarkMultiple = false,
            Height = 4,
            Width = 10
        };

        lv.BeginInit ();
        lv.EndInit ();

        // MarkUnmarkSelectedItem should return false (marking not allowed)
        lv.SelectedItem = 0;
        Assert.False (lv.MarkUnmarkSelectedItem ());
        Assert.False (lv.Source!.IsMarked (0));

        // SetSelection with extend should not create marks
        lv.SetSelection (1, true);
        Assert.Empty (lv.GetAllMarkedItems ());

        // MarkAll should return false
        Assert.False (lv.MarkAll (true));
        Assert.Empty (lv.GetAllMarkedItems ());
    }

    #endregion

    #region Combination 2: ShowMarks=false, MarkMultiple=true (Hidden Marks with Visual Roles)

    // Claude - Opus 4.5
    [Fact]
    public void Combination2_HiddenMarks_MarkingAllowed ()
    {
        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4"]),
            ShowMarks = false,
            MarkMultiple = true,
            Height = 4,
            Width = 10
        };

        lv.BeginInit ();
        lv.EndInit ();

        // MarkUnmarkSelectedItem should work (marking allowed even though ShowMarks=false)
        lv.SelectedItem = 0;
        Assert.True (lv.MarkUnmarkSelectedItem ());
        Assert.True (lv.Source!.IsMarked (0));

        // MarkAll should work
        Assert.True (lv.MarkAll (true));
        Assert.Equal (4, lv.GetAllMarkedItems ().Count ());

        // SetSelection with extend should create range marks
        lv.UnmarkAll ();
        lv.SetSelection (0, false);
        lv.SetSelection (3, true); // Extend from 0 to 3
        Assert.True (lv.Source!.IsMarked (0));
        Assert.True (lv.Source!.IsMarked (1));
        Assert.True (lv.Source!.IsMarked (2));
        Assert.True (lv.Source!.IsMarked (3));
    }

    #endregion

    #region Combination 3: ShowMarks=true, MarkMultiple=false (Radio Button)

    // Claude - Opus 4.5
    [Fact]
    public void Combination3_RadioButton_SingleMarkOnly ()
    {
        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4"]),
            ShowMarks = true,
            MarkMultiple = false,
            Height = 4,
            Width = 10
        };

        lv.BeginInit ();
        lv.EndInit ();

        // SetSelection should mark only the selected item (radio button behavior)
        lv.SetSelection (0, false);
        Assert.True (lv.Source!.IsMarked (0));

        // Selecting another item should clear previous mark
        lv.SetSelection (2, false);
        Assert.False (lv.Source!.IsMarked (0));
        Assert.True (lv.Source!.IsMarked (2));

        // SetSelection with extend should not create range (radio button mode)
        lv.SetSelection (0, true);
        Assert.Single (lv.GetAllMarkedItems ());
        Assert.True (lv.Source!.IsMarked (0));
        Assert.False (lv.Source!.IsMarked (1));
        Assert.False (lv.Source!.IsMarked (2));

        // MarkAll should return false (not allowed in single-mark mode)
        Assert.False (lv.MarkAll (true));
    }

    // Claude - Opus 4.5
    [Fact]
    public void Combination3_RadioButton_MouseClick_MarksClickedItem ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3"]),
            ShowMarks = true,
            MarkMultiple = false, // Radio button mode
            Height = 3,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Click on item 0 - should select and mark it
        // x=2 to account for mark width (2 characters for radio button glyphs)
        app.Mouse.RaiseMouseEvent (new () { ScreenPosition = new (2, 0), Flags = MouseFlags.LeftButtonPressed });
        app.Mouse.RaiseMouseEvent (new () { ScreenPosition = new (2, 0), Flags = MouseFlags.LeftButtonReleased });
        app.Mouse.RaiseMouseEvent (new () { ScreenPosition = new (2, 0), Flags = MouseFlags.LeftButtonClicked });

        Assert.Equal (0, lv.SelectedItem);
        Assert.True (lv.Source!.IsMarked (0));
        Assert.False (lv.Source!.IsMarked (1));
        Assert.False (lv.Source!.IsMarked (2));

        // Click on item 2 - should unmark item 0 and mark item 2 (radio button: only one at a time)
        app.Mouse.RaiseMouseEvent (new () { ScreenPosition = new (2, 2), Flags = MouseFlags.LeftButtonPressed });
        app.Mouse.RaiseMouseEvent (new () { ScreenPosition = new (2, 2), Flags = MouseFlags.LeftButtonReleased });
        app.Mouse.RaiseMouseEvent (new () { ScreenPosition = new (2, 2), Flags = MouseFlags.LeftButtonClicked });

        Assert.Equal (2, lv.SelectedItem);
        Assert.False (lv.Source!.IsMarked (0)); // Unmarked
        Assert.False (lv.Source!.IsMarked (1));
        Assert.True (lv.Source!.IsMarked (2)); // Now marked

        top.Dispose ();
        app.Dispose ();
    }

    #endregion

    #region Combination 4: ShowMarks=true, MarkMultiple=true (Checkbox)

    // Claude - Opus 4.5
    [Fact]
    public void Combination4_Checkbox_MultipleMarksAllowed ()
    {
        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4"]),
            ShowMarks = true,
            MarkMultiple = true,
            Height = 4,
            Width = 10
        };

        lv.BeginInit ();
        lv.EndInit ();

        // MarkAll should work
        Assert.True (lv.MarkAll (true));
        Assert.Equal (4, lv.GetAllMarkedItems ().Count ());

        // SetSelection with extend should create range marks
        lv.UnmarkAll ();
        lv.SetSelection (0, false);
        lv.SetSelection (2, true); // Extend from 0 to 2
        Assert.True (lv.Source!.IsMarked (0));
        Assert.True (lv.Source!.IsMarked (1));
        Assert.True (lv.Source!.IsMarked (2));

        // Individual marking should work
        lv.UnmarkAll ();
        lv.SelectedItem = 0;
        lv.MarkUnmarkSelectedItem ();
        lv.SelectedItem = 2;
        lv.MarkUnmarkSelectedItem ();
        Assert.True (lv.Source!.IsMarked (0));
        Assert.False (lv.Source!.IsMarked (1));
        Assert.True (lv.Source!.IsMarked (2));
    }

    #endregion

    #region Edge Cases and Transitions

    // Claude - Opus 4.5
    [Fact]
    public void MarkMultiple_SetToFalse_ClearsAllButSelected ()
    {
        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3", "4"]),
            ShowMarks = true,
            MarkMultiple = true,
            Height = 4,
            Width = 10
        };

        lv.BeginInit ();
        lv.EndInit ();

        // Mark multiple items
        lv.SelectedItem = 1;
        lv.Source!.SetMark (0, true);
        lv.Source!.SetMark (1, true);
        lv.Source!.SetMark (2, true);
        Assert.Equal (3, lv.GetAllMarkedItems ().Count ());

        // Set MarkMultiple to false - should clear all except SelectedItem
        lv.MarkMultiple = false;
        Assert.Single (lv.GetAllMarkedItems ());
        Assert.True (lv.Source!.IsMarked (1)); // SelectedItem still marked
    }

    // Claude - Opus 4.5
    [Fact]
    public void Combination2_SpaceKey_WorksWithHiddenMarks ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        ListView lv = new ()
        {
            Source = new ListWrapper<string> (["1", "2", "3"]),
            ShowMarks = false,
            MarkMultiple = true,
            Height = 3,
            Width = 10
        };

        Runnable top = new ();
        top.Add (lv);
        lv.SetFocus ();
        app.Begin (top);

        // Select item 0
        lv.SelectedItem = 0;

        // Press Space - should mark item 0 even though ShowMarks=false
        lv.NewKeyDownEvent (Key.Space);
        Assert.True (lv.Source!.IsMarked (0));

        // Press Space again - should unmark item 0
        lv.NewKeyDownEvent (Key.Space);
        Assert.False (lv.Source!.IsMarked (0));

        top.Dispose ();
        app.Dispose ();
    }

    #endregion
}
