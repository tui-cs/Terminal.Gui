using UnitTests;

namespace ViewBaseTests.Draw;

// Claude - Opus 4.7
/// <summary>
///     Issue #5358: when a parent enters Draw() only because a child is dirty,
///     the parent must NOT clear its viewport, re-draw its text, or re-draw its
///     own content. Only adornments and subviews are allowed to run.
/// </summary>
public class SubViewOnlyRedrawTests : TestDriverBase
{
    /// <summary>
    ///     GIVEN a parent where only the child is dirty
    ///     WHEN Draw runs
    ///     THEN the parent's ClearingViewport / DrewText / DrawingContent events MUST NOT fire.
    /// </summary>
    [Fact]
    public void ChildOnlyDirty_ParentDoesNotClearOrRedrawSelf ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View parent = new () { Driver = driver, Width = 20, Height = 10, Text = "parent-text" };
        View child = new () { Width = 10, Height = 5, X = 1, Y = 1 };
        parent.Add (child);

        parent.Layout ();
        parent.Draw ();

        var cleared = 0;
        var drewText = 0;
        var drewContent = 0;
        parent.ClearedViewport += (_, _) => cleared++;
        parent.DrewText += (_, _) => drewText++;
        parent.DrawingContent += (_, _) => drewContent++;

        child.SetNeedsDraw ();

        parent.Draw ();

        Assert.Equal (0, cleared);
        Assert.Equal (0, drewText);
        Assert.Equal (0, drewContent);

        parent.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a parent dirtied directly via SetNeedsDraw()
    ///     WHEN Draw runs
    ///     THEN the parent MUST clear and redraw its text/content (regression guard:
    ///     the new gate must not break the normal full-redraw path).
    /// </summary>
    [Fact]
    public void ParentDirectlyDirty_StillClearsAndRedrawsSelf ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View parent = new () { Driver = driver, Width = 20, Height = 10, Text = "parent-text" };
        View child = new () { Width = 10, Height = 5, X = 1, Y = 1 };
        parent.Add (child);

        parent.Layout ();
        parent.Draw ();

        var cleared = 0;
        var drewText = 0;
        parent.ClearedViewport += (_, _) => cleared++;
        parent.DrewText += (_, _) => drewText++;

        parent.SetNeedsDraw ();

        parent.Draw ();

        Assert.True (cleared > 0, $"Parent should clear when it itself is dirty (got {cleared}).");
        Assert.True (drewText > 0, $"Parent should redraw text when it itself is dirty (got {drewText}).");

        parent.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a transparent parent where only the child is dirty
    ///     WHEN Draw runs
    ///     THEN the existing Transparent early-return in DoClearViewport still wins,
    ///     and the child-only path stays correct (no regression to transparency).
    /// </summary>
    [Fact]
    public void TransparentParent_ChildOnlyDirty_DoesNotClear ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View parent = new ()
        {
            Driver = driver,
            Width = 20,
            Height = 10,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };
        View child = new () { Width = 10, Height = 5, X = 1, Y = 1 };
        parent.Add (child);

        parent.Layout ();
        parent.Draw ();

        var cleared = 0;
        parent.ClearedViewport += (_, _) => cleared++;

        child.SetNeedsDraw ();
        parent.Draw ();

        Assert.Equal (0, cleared);

        parent.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a parent with a Border adornment where only the child is dirty
    ///     WHEN Draw runs
    ///     THEN the parent's viewport is not cleared and the parent's own text/content
    ///     is not redrawn. (Adornments still run independently — they always redraw
    ///     when the parent is entered; that is by design.)
    /// </summary>
    [Fact]
    public void ParentWithBorder_ChildOnlyDirty_ParentDoesNotClear ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View parent = new () { Driver = driver, Width = 20, Height = 10 };
        parent.Border.Thickness = new Thickness (1);

        View child = new () { Width = 5, Height = 3, X = 2, Y = 2 };
        parent.Add (child);

        parent.Layout ();
        parent.Draw ();

        var parentCleared = 0;
        var parentDrewText = 0;
        parent.ClearedViewport += (_, _) => parentCleared++;
        parent.DrewText += (_, _) => parentDrewText++;

        child.SetNeedsDraw ();
        parent.Draw ();

        Assert.Equal (0, parentCleared);
        Assert.Equal (0, parentDrewText);

        parent.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a parent and child rendered to a real driver / IOutput
    ///     WHEN only the child is dirtied and Draw runs
    ///     THEN IOutput must still produce non-empty output (the fix does not
    ///     accidentally suppress the child's render at the IOutput layer).
    /// </summary>
    [Fact]
    public void ChildOnlyDirty_StillProducesOutputAtIOutputLayer ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View parent = new () { Driver = driver, Width = 20, Height = 10 };
        View child = new () { Width = 10, Height = 5, X = 1, Y = 1, Text = "CHILD" };
        parent.Add (child);

        parent.Layout ();
        parent.Draw ();

        IOutput output = driver.GetOutput ();
        IOutputBuffer buffer = driver.GetOutputBuffer ();

        child.Text = "CHILDX";
        child.SetNeedsDraw ();
        parent.Draw ();

        output.Write (buffer);
        string ansi = output.GetLastOutput ();

        Assert.False (string.IsNullOrEmpty (ansi));

        parent.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Review feedback item 2: a transparent parent with TransparentMouse must keep its
    ///     CachedDrawnRegion populated across child-only redraws. Before the fix, the
    ///     unconditional `_localDrawContext = new DrawContext()` reset combined with skipping
    ///     DoDrawText / DoDrawContent on child-only passes meant DoDrawComplete wrote an
    ///     empty region into CachedDrawnRegion, breaking mouse hit-testing until the next
    ///     full self-redraw.
    /// </summary>
    [Fact]
    public void TransparentMouseParent_ChildOnlyDirty_PreservesCachedDrawnRegion ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View parent = new ()
        {
            Driver = driver,
            Width = 20,
            Height = 10,
            ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse
        };
        View child = new () { Width = 10, Height = 5, X = 1, Y = 1 };
        parent.Add (child);

        // Force the parent to actually draw something so CachedDrawnRegion is populated
        // (transparent views only cache cells they actually touched).
        parent.DrawingContent += (_, e) =>
        {
            parent.FillRect (new Rectangle (0, 0, 5, 3), new System.Text.Rune ('P'));
            e.DrawContext?.AddDrawnRectangle (parent.ViewportToScreen (new Rectangle (0, 0, 5, 3)));
        };

        parent.Layout ();
        parent.Draw ();

        Region? cachedAfterFullDraw = parent.CachedDrawnRegion;
        Assert.NotNull (cachedAfterFullDraw);
        Assert.False (cachedAfterFullDraw.GetBounds ().IsEmpty);

        // Child-only invalidation: only the child becomes dirty, parent stays clean.
        child.SetNeedsDraw ();
        Assert.False (parent.NeedsDraw);

        parent.Draw ();

        // After the child-only redraw, parent's cache must still reflect the previously-drawn
        // region. If we naively wiped it, transparent-parent mouse hit-testing would break.
        Assert.NotNull (parent.CachedDrawnRegion);
        Assert.False (parent.CachedDrawnRegion.GetBounds ().IsEmpty);

        parent.Dispose ();
        driver.Dispose ();
    }
}
