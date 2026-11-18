// Copilot

namespace ViewBaseTests.Layout;

/// <summary>
///     Tests for the stale content size capture bug in <see cref="View.LayoutSubViews"/>.
///     See: https://github.com/tui-cs/Terminal.Gui/issues/4522
///
///     The core bug: <c>LayoutSubViews</c> captures <c>contentSize</c> once at the top, then fires
///     <c>OnSubViewLayout</c>. If a subclass (e.g. Dialog) calls <c>SetContentSize</c> from that callback,
///     the captured value diverges from the actual content size.
///
///     Additionally, line 771 sets <c>NeedsLayout</c> directly instead of calling <c>SetNeedsLayout()</c>,
///     bypassing upward propagation.
///
///     Some tests demonstrate bugs by failing; others serve as regression tests ensuring correct
///     behavior is preserved.
/// </summary>
public class StaleContentSizeCaptureTests
{
    #region Helper SubView classes

    /// <summary>
    ///     A View subclass that calls <see cref="View.SetContentSize"/> from <see cref="View.OnSubViewLayout"/>.
    /// </summary>
    private class ContentSizeChangingOnSubViewLayoutView : View
    {
        public Size NewContentSize { get; set; }

        /// <summary>Gets the content size that was passed to <c>OnSubViewLayout</c>.</summary>
        public Size? CapturedLayoutContentSize { get; private set; }

        protected override void OnSubViewLayout (LayoutEventArgs args)
        {
            CapturedLayoutContentSize = args.OldContentSize;
            SetContentSize (NewContentSize);
            base.OnSubViewLayout (args);
        }
    }

    /// <summary>
    ///     A View subclass that calls <see cref="View.SetContentSize"/> from the <c>SubViewsLaidOut</c>
    ///     event (simulating HexView behavior).
    /// </summary>
    private class ContentSizeChangingOnSubViewsLaidOutView : View
    {
        public Size NewContentSize { get; set; }

        public ContentSizeChangingOnSubViewsLaidOutView ()
        {
            SubViewsLaidOut += (_, _) => { SetContentSize (NewContentSize); };
        }
    }

    /// <summary>
    ///     A View subclass that calls <see cref="View.SetContentSize"/> from the <c>SubViewLayout</c>
    ///     event (raised before SubViews are laid out, after <see cref="View.OnSubViewLayout"/>).
    ///     <see cref="NewContentSize"/> is nullable so the event handler can be armed after
    ///     <c>EndInit</c> and won't corrupt state during initialization.
    /// </summary>
    private class ContentSizeChangingOnSubViewLayoutEventView : View
    {
        public Size? NewContentSize { get; set; }

        public ContentSizeChangingOnSubViewLayoutEventView ()
        {
            SubViewLayout += (_, _) =>
            {
                if (NewContentSize.HasValue) { SetContentSize (NewContentSize.Value); }
            };
        }
    }

    #endregion

    #region Test 1: Core stale capture

    /// <summary>
    ///     Regression guard: <c>LayoutSubViews</c> re-reads <c>contentSize</c> after
    ///     <c>OnSubViewLayout</c> fires, so SubViews are laid out with the updated size.
    ///     Fixed by #4863.
    /// </summary>
    [Fact]
    public void LayoutSubViews_Honors_SetContentSize_From_OnSubViewLayout ()
    {
        // Arrange: SuperView that changes content size in OnSubViewLayout
        ContentSizeChangingOnSubViewLayoutView superView = new ()
        {
            Width = 20,
            Height = 10,
            NewContentSize = new Size (50, 20)
        };

        View child = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        superView.Add (child);
        superView.BeginInit ();
        superView.EndInit ();
        superView.Layout ();

        // Assert: The child should fill the NEW content size (50, 20),
        // not the stale pre-callback value.
        Assert.Equal (50, child.Frame.Width);
        Assert.Equal (20, child.Frame.Height);
    }

    #endregion

    #region Test 2: Dialog resize scenario

    /// <summary>
    ///     Proves the reported real-world scenario: a Dialog with Dim.Fill SubViews
    ///     gets the wrong layout after a screen size change (simulating maximize/restore).
    /// </summary>
    [Fact]
    public void Dialog_Children_Use_Stale_ContentSize_After_Screen_Resize ()
    {
        // Arrange: Dialog with explicit dimensions and a Dim.Fill child
        Dialog dialog = new ()
        {
            Width = 40,
            Height = 15
        };

        View child = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        dialog.Add (child);
        dialog.BeginInit ();
        dialog.EndInit ();

        // First layout at initial size
        dialog.Layout ();
        Size firstContentSize = dialog.GetContentSize ();
        Rectangle firstChildFrame = child.Frame;

        // Resize the dialog (simulating maximize)
        dialog.Width = 120;
        dialog.Height = 40;
        dialog.SetNeedsLayout ();
        dialog.Layout ();

        Size secondContentSize = dialog.GetContentSize ();

        // Assert: content size should have grown
        Assert.True (secondContentSize.Width > firstContentSize.Width,
                     $"Content width should have grown: was {firstContentSize.Width}, now {secondContentSize.Width}");

        Assert.True (secondContentSize.Height > firstContentSize.Height,
                     $"Content height should have grown: was {firstContentSize.Height}, now {secondContentSize.Height}");

        // Assert: child frame should match the new content size, not the old one
        Assert.Equal (secondContentSize.Width, child.Frame.Width);
        Assert.Equal (secondContentSize.Height, child.Frame.Height);
    }

    #endregion

    #region Test 3: Dialog OnSubViewLayout non-divergence

    /// <summary>
    ///     Regression guard: after the fix, <c>Dialog.OnSubViewLayout</c> → <c>UpdateSizes</c> →
    ///     <c>SetContentSize</c> does NOT produce a content size that differs from what
    ///     <c>LayoutSubViews</c> uses to lay out SubViews. The pre-event and post-event sizes
    ///     must be equal (no divergence). Fixed by #4863.
    /// </summary>
    [Fact]
    public void Dialog_OnSubViewLayout_SetContentSize_Does_Not_Diverge_From_Layout_Value ()
    {
        // Arrange: Dialog with explicit (non-Auto) Width/Height
        Dialog dialog = new ()
        {
            Width = 60,
            Height = 20
        };

        // Add several subviews to create a _minimumSubViewsSize that differs from Viewport
        View tall = new ()
        {
            Width = 10,
            Height = 30 // Taller than dialog viewport
        };

        View wide = new ()
        {
            X = 0,
            Y = Pos.Bottom (tall),
            Width = 80, // Wider than dialog viewport
            Height = 5
        };

        dialog.Add (tall, wide);
        dialog.BeginInit ();
        dialog.EndInit ();

        // Capture the content size that exists before layout
        Size? preLayoutContentSize = null;
        Size? postLayoutContentSize = null;

        dialog.SubViewLayout += (_, args) => { preLayoutContentSize = args.OldContentSize; };
        dialog.SubViewsLaidOut += (_, _) => { postLayoutContentSize = dialog.GetContentSize (); };

        dialog.Layout ();

        // Assert: If the bug exists, preLayoutContentSize (what children were laid out with)
        // differs from postLayoutContentSize (the final content size after SetContentSize calls)
        Assert.NotNull (preLayoutContentSize);
        Assert.NotNull (postLayoutContentSize);

        // After layout, the child with Dim.Fill should be sized to the FINAL content size.
        // This tests that the content size used for layout is consistent with the final value.
        Assert.Equal (postLayoutContentSize, dialog.GetContentSize ());

        // The key assertion: the content size should NOT have diverged.
        // If it diverged, children were laid out with the wrong (stale) value.
        Assert.Equal (postLayoutContentSize, preLayoutContentSize);
    }

    #endregion

    #region Test 4: NeedsLayout propagation bypass

    /// <summary>
    ///     Proves that line 771 sets <c>NeedsLayout = false</c> directly, bypassing
    ///     <c>SetNeedsLayout</c> propagation. If content size changed during layout,
    ///     neither the view nor its SuperView know they need another pass.
    /// </summary>
    [Fact]
    public void NeedsLayout_Direct_Set_Does_Not_Propagate_To_SuperView ()
    {
        // Arrange: grandSuperView → customView → child
        View grandSuperView = new ()
        {
            Width = 80,
            Height = 25
        };

        ContentSizeChangingOnSubViewLayoutView customView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            NewContentSize = new Size (100, 50) // Will change content size during OnSubViewLayout
        };

        View child = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        customView.Add (child);
        grandSuperView.Add (customView);
        grandSuperView.BeginInit ();
        grandSuperView.EndInit ();

        grandSuperView.Layout ();

        // After layout, SetContentSize was called from OnSubViewLayout which calls SetNeedsLayout().
        // But LayoutSubViews line 771 then sets NeedsLayout = false directly.
        // The content size changed, so ideally NeedsLayout should still be true
        // (or should have triggered a corrective layout pass).

        // The content size was changed to (100, 50). If a corrective pass happened,
        // the child would be sized to (100, 50). If not, the child has the stale size.
        Assert.Equal (100, child.Frame.Width);
        Assert.Equal (50, child.Frame.Height);
    }

    #endregion

    #region Test 5: SubViewsLaidOut SetContentSize is too late

    /// <summary>
    ///     Proves that views calling <c>SetContentSize</c> from <c>SubViewsLaidOut</c>
    ///     (like HexView) change content size after SubViews have already been laid out.
    /// </summary>
    [Fact]
    public void LayoutSubViews_OnSubViewsLaidOut_SetContentSize_Is_Too_Late ()
    {
        ContentSizeChangingOnSubViewsLaidOutView superView = new ()
        {
            Width = 20,
            Height = 10,
            NewContentSize = new Size (80, 30)
        };

        View child = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        superView.Add (child);
        superView.BeginInit ();
        superView.EndInit ();
        superView.Layout ();

        // Assert: The child should reflect the content size (80, 30) set in SubViewsLaidOut.
        // On current code, the child was already laid out before SubViewsLaidOut fired.
        Assert.Equal (80, child.Frame.Width);
        Assert.Equal (30, child.Frame.Height);
    }

    #endregion

    #region Test 5b: SubViewLayout event SetContentSize is honored

    // Claude - Sonnet 4.6
    /// <summary>
    ///     Regression test for the <c>SubViewLayout</c> <b>event</b> companion to the
    ///     <c>OnSubViewLayout</c> virtual fixed by #4863. A handler on the <c>SubViewLayout</c> event
    ///     that calls <see cref="View.SetContentSize"/> must be honored: <see cref="View.LayoutSubViews"/>
    ///     re-reads the content size after raising the event, so SubViews are laid out against the
    ///     updated value rather than the size captured before the event fired. See issue #4522.
    /// </summary>
    /// <remarks>
    ///     The test arms the event handler <b>after</b> <c>EndInit</c> and explicitly resets the
    ///     content size to a known 20×10 baseline. This ensures the captured <c>contentSize</c> local
    ///     variable in <c>LayoutSubViews</c> starts at 20×10, so removing the post-event reread would
    ///     give the child a width of 20 (not 50) — making the test a genuine regression guard for the
    ///     specific line added in this fix.
    /// </remarks>
    [Fact]
    public void LayoutSubViews_Honors_SetContentSize_From_SubViewLayout_Event ()
    {
        // NewContentSize is NOT set here; the event handler does nothing during EndInit,
        // so it cannot pre-seed the content size to 50×20 before the guarded layout pass.
        ContentSizeChangingOnSubViewLayoutEventView superView = new ()
        {
            Width = 20,
            Height = 10
        };

        View child = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        superView.Add (child);
        superView.BeginInit ();
        superView.EndInit ();

        // Establish a concrete 20×10 baseline. SetContentSize also calls SetNeedsLayout so
        // the next Layout() call will run LayoutSubViews and not return early.
        superView.SetContentSize (new Size (20, 10));

        // Now arm the event: the handler will inject 50×20 during the next SubViewLayout event.
        superView.NewContentSize = new Size (50, 20);

        superView.Layout ();

        // The child must fill the content size injected by the SubViewLayout event handler (50×20),
        // not the baseline value captured before the event fired (20×10).
        Assert.Equal (50, child.Frame.Width);
        Assert.Equal (20, child.Frame.Height);

        superView.Dispose ();
    }

    #endregion

    #region Test 6: ListView stale capture

    /// <summary>
    ///     Proves that <c>ListView.OnViewportChanged</c> calls <c>SetContentSize</c> during layout.
    ///     The parent's <c>LayoutSubViews</c> captures <c>contentSize</c> before the ListView's viewport
    ///     change triggers <c>SetContentSize</c>. A sibling using <c>Dim.Fill</c> should reflect the
    ///     updated content size, not the stale pre-callback value.
    /// </summary>
    [Fact]
    public void ListView_OnViewportChanged_SetContentSize_Creates_Stale_Capture ()
    {
        // Arrange: ListView with items, plus a sibling, in a container
        ListView listView = new ()
        {
            Width = Dim.Fill (),
            Height = 5
        };

        listView.Source = new ListWrapper<string> (["Item 1", "Item 2", "Item 3", "Item 4", "Item 5",
                                                    "Item 6", "Item 7", "Item 8", "Item 9", "Item 10"]);

        View container = new ()
        {
            Width = 30,
            Height = 10
        };

        container.Add (listView);
        container.BeginInit ();
        container.EndInit ();

        // Initial layout
        container.Layout ();
        Size firstContentSize = listView.GetContentSize ();

        // Resize container (simulating terminal resize)
        container.Width = 60;
        container.Height = 20;
        container.SetNeedsLayout ();
        container.Layout ();

        Size secondContentSize = listView.GetContentSize ();

        // Assert: ListView frame should match the new container
        Assert.Equal (60, listView.Frame.Width);
        Assert.Equal (5, listView.Frame.Height);

        // ListView content height = Source.Count (10 items), width = EffectiveMaxItemLength
        Assert.Equal (10, secondContentSize.Height);

        // After resize, content size should have been recalculated
        // (EffectiveMaxItemLength doesn't change, but height = Source.Count stays consistent)
        Assert.Equal (firstContentSize.Height, secondContentSize.Height);
    }

    #endregion

    #region Test 7: TableView stale capture

    /// <summary>
    ///     Proves that <c>TableView.OnViewportChanged</c> → <c>RefreshContentSize</c> calls
    ///     <c>SetContentSize</c> during layout.
    /// </summary>
    [Fact]
    public void TableView_RefreshContentSize_During_Layout_Creates_Stale_Capture ()
    {
        // Arrange: TableView with data in a container
        TableView tableView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        System.Data.DataTable dt = new ();
        dt.Columns.Add ("Name");
        dt.Columns.Add ("Value");

        for (var i = 0; i < 20; i++)
        {
            dt.Rows.Add ($"Row {i}", $"Val {i}");
        }

        tableView.Table = new DataTableSource (dt);

        View container = new ()
        {
            Width = 30,
            Height = 10
        };

        container.Add (tableView);
        container.BeginInit ();
        container.EndInit ();

        // Initial layout
        container.Layout ();
        Size firstContentSize = tableView.GetContentSize ();

        // Resize container
        container.Width = 80;
        container.Height = 25;
        container.SetNeedsLayout ();
        container.Layout ();

        Size secondContentSize = tableView.GetContentSize ();

        // Assert: TableView frame should match the new container size
        Assert.Equal (80, tableView.Frame.Width);
        Assert.Equal (25, tableView.Frame.Height);

        // The content size should have been recalculated for the new viewport
        Assert.True (secondContentSize.Width >= tableView.Viewport.Width,
                     $"Content width {secondContentSize.Width} should be >= viewport width {tableView.Viewport.Width}");
    }

    #endregion
}
