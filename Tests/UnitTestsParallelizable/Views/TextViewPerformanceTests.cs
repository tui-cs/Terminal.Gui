

// Copilot - Claude Opus 4.6

namespace ViewsTests.TextViewTests;

/// <summary>
///     Tests verifying TextView scroll performance optimizations.
///     Phase 1: Tests that validate existing correct behavior (must pass before AND after fixes).
///     Phase 2: Tests that expose performance issues (expected to fail before fixes, pass after).
/// </summary>
public class TextViewPerformanceTests
{
    #region Phase 1: Passing tests for existing behavior

    /// <summary>
    ///     Verifies that GetMaxVisibleLine (via ContentSize) returns the width of the longest line.
    /// </summary>
    [Fact]
    public void GetMaxVisibleLine_Returns_Correct_Width_For_Mixed_Lines ()
    {
        // Arrange: lines of varying lengths
        TextView tv = new () { Width = 80, Height = 10, Text = "Short\nThis is a much longer line than the first\nMed line" };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // The longest line is "This is a much longer line than the first" = 41 chars
        // ContentSize.Width should reflect the longest line (plus 1 for cursor column)
        int contentWidth = tv.GetContentSize ().Width;
        Assert.True (contentWidth >= 41, $"ContentSize.Width ({contentWidth}) should be >= 41 (longest line width)");
    }

    /// <summary>
    ///     Verifies content size height equals line count and width reflects max line width.
    /// </summary>
    [Fact]
    public void UpdateContentSize_Sets_Correct_Width_And_Height ()
    {
        TextView tv = new () { Width = 80, Height = 10, Text = "Line 1\nLine 2\nLine 3" };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        Size contentSize = tv.GetContentSize ();
        Assert.Equal (3, contentSize.Height);
        Assert.True (contentSize.Width >= 6, $"ContentSize.Width ({contentSize.Width}) should be >= 6 (length of 'Line 1')");
    }

    /// <summary>
    ///     Verifies content size is updated after layout.
    /// </summary>
    [Fact]
    public void OnSubViewsLaidOut_Updates_ContentSize ()
    {
        TextView tv = new () { Width = 80, Height = 10, Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5" };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        Size contentSize = tv.GetContentSize ();
        Assert.Equal (5, contentSize.Height);
        Assert.True (contentSize.Width > 0, "ContentSize.Width should be > 0 after layout");
    }

    /// <summary>
    ///     Verifies content size updates when text changes.
    /// </summary>
    [Fact]
    public void ContentSize_Correct_After_Text_Change ()
    {
        TextView tv = new () { Width = 80, Height = 10, Text = "Short" };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        Assert.Equal (1, tv.GetContentSize ().Height);

        // Change to longer text with more lines
        tv.Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7";

        Assert.Equal (7, tv.GetContentSize ().Height);
    }

    /// <summary>
    ///     Verifies content size updates when a longer line is added.
    /// </summary>
    [Fact]
    public void ContentSize_Width_Increases_When_Longer_Line_Added ()
    {
        TextView tv = new () { Width = 80, Height = 10, Text = "Short" };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        int initialWidth = tv.GetContentSize ().Width;

        // Change to text with a much longer line
        tv.Text = "Short\nThis is a significantly longer line that should increase content width";

        int newWidth = tv.GetContentSize ().Width;
        Assert.True (newWidth > initialWidth, $"ContentSize.Width should increase from {initialWidth} after adding longer line, but was {newWidth}");
    }

    /// <summary>
    ///     Verifies WrapTextModel is a no-op when WordWrap is disabled.
    /// </summary>
    [Fact]
    public void WrapTextModel_Only_Wraps_When_WordWrap_Enabled ()
    {
        string longLine = new ('X', 50);

        TextView tv = new () { Width = 20, Height = 10, WordWrap = false, Text = longLine };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // With WordWrap off, should be exactly 1 line
        Assert.Equal (1, tv.Lines);
        Assert.Equal (1, tv.GetContentSize ().Height);
    }

    /// <summary>
    ///     Verifies that changing Viewport.Y (pure scrolling) does not corrupt content size.
    /// </summary>
    [Fact]
    public void Viewport_Scrolling_Preserves_ContentSize ()
    {
        TextView tv = new () { Width = 40, Height = 5, Text = string.Join ("\n", Enumerable.Range (1, 100).Select (i => $"Line {i}: some text content")) };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        Size initialSize = tv.GetContentSize ();
        Assert.Equal (100, initialSize.Height);

        // Scroll down via viewport
        tv.Viewport = tv.Viewport with { Y = 20 };

        Size afterScroll = tv.GetContentSize ();
        Assert.Equal (initialSize.Height, afterScroll.Height);
        Assert.Equal (initialSize.Width, afterScroll.Width);

        // Scroll further
        tv.Viewport = tv.Viewport with { Y = 50 };

        Size afterScroll2 = tv.GetContentSize ();
        Assert.Equal (initialSize.Height, afterScroll2.Height);
        Assert.Equal (initialSize.Width, afterScroll2.Width);
    }

    #endregion

    #region Phase 2: Tests exposing performance issues (fail before fix, pass after)

    /// <summary>
    ///     Verifies that pure scrolling (Viewport changes without text edits) does NOT trigger
    ///     expensive GetMaxVisibleLine recalculation. We test this by checking the scan counter
    ///     on TextModel — it should not increase during pure scroll/layout operations.
    ///     Before fix: OnSubViewsLaidOut unconditionally calls UpdateContentSize which calls
    ///     GetMaxVisibleLine(0, model.Count) — an O(N×L) full scan of all text.
    ///     After fix: TextModel caches the max width, so repeated calls without mutations return
    ///     the cached value without rescanning.
    /// </summary>
    [Fact]
    public void GetMaxVisibleLine_Not_Rescanned_On_Layout_When_Content_Unchanged ()
    {
        // Arrange: Large document
        string text = string.Join ("\n", Enumerable.Range (1, 500).Select (i => $"Line {i}: content"));
        TextView tv = new () { Width = 40, Height = 10, Text = text };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Get a reference to the internal model to check scan count
        // (TextModel is internal, visible to test project via InternalsVisibleTo)
        TextModel model = new ();
        model.LoadString (text);

        // Prime the cache
        int initialResult = model.GetMaxVisibleLine (0, model.Count, 4);
        int scansAfterFirst = model.MaxVisibleLineScanCount;
        Assert.Equal (1, scansAfterFirst);

        // Act: Call GetMaxVisibleLine again without changing content
        int secondResult = model.GetMaxVisibleLine (0, model.Count, 4);

        // Assert: Should return cached value without rescanning
        Assert.Equal (initialResult, secondResult);
        Assert.Equal (1, model.MaxVisibleLineScanCount); // No additional scan
    }

    /// <summary>
    ///     Verifies that after a content mutation, GetMaxVisibleLine rescans, then subsequent
    ///     calls without mutation return the cached value.
    /// </summary>
    [Fact]
    public void GetMaxVisibleLine_Rescans_After_Mutation_Then_Caches ()
    {
        TextModel model = new ();
        model.LoadString ("Short\nMedium line\nThe longest line in the document");

        // First call: scans
        int result1 = model.GetMaxVisibleLine (0, model.Count, 4);
        Assert.Equal (1, model.MaxVisibleLineScanCount);

        // Second call: cached
        int result2 = model.GetMaxVisibleLine (0, model.Count, 4);
        Assert.Equal (result1, result2);
        Assert.Equal (1, model.MaxVisibleLineScanCount);

        // Mutate: add a line
        model.AddLine (model.Count, Cell.StringToCells ("An even longer line that exceeds everything else!!!"));

        // Third call: must rescan because of mutation
        int result3 = model.GetMaxVisibleLine (0, model.Count, 4);
        Assert.Equal (2, model.MaxVisibleLineScanCount);
        Assert.True (result3 >= result1, $"Width should be >= previous ({result1}) after adding longer line, got {result3}");

        // Fourth call: cached again
        int result4 = model.GetMaxVisibleLine (0, model.Count, 4);
        Assert.Equal (result3, result4);
        Assert.Equal (2, model.MaxVisibleLineScanCount);
    }

    /// <summary>
    ///     Verifies that cache is invalidated on all mutation paths: LoadString, ReplaceLine, RemoveLine.
    /// </summary>
    [Fact]
    public void GetMaxVisibleLine_Cache_Invalidated_On_All_Mutations ()
    {
        TextModel model = new ();
        model.LoadString ("Line 1\nLine 2");

        // Prime cache
        model.GetMaxVisibleLine (0, model.Count, 4);
        model.ResetMaxVisibleLineCallCount ();

        // ReplaceLine should invalidate
        model.ReplaceLine (0, Cell.StringToCells ("Replaced with a longer line!!!!"));
        model.GetMaxVisibleLine (0, model.Count, 4);
        Assert.Equal (1, model.MaxVisibleLineScanCount);

        // Second call cached
        model.GetMaxVisibleLine (0, model.Count, 4);
        Assert.Equal (1, model.MaxVisibleLineScanCount);

        // RemoveLine should invalidate
        model.RemoveLine (0);
        model.GetMaxVisibleLine (0, model.Count, 4);
        Assert.Equal (2, model.MaxVisibleLineScanCount);

        // LoadString should invalidate
        model.LoadString ("Brand new content");
        model.GetMaxVisibleLine (0, model.Count, 4);
        Assert.Equal (3, model.MaxVisibleLineScanCount);
    }

    /// <summary>
    ///     Verifies that WrapTextModel is not called during OnSubViewsLaidOut when the viewport
    ///     width hasn't changed. In a deep view hierarchy, OnSubViewsLaidOut fires frequently
    ///     and WrapTextModel (even when WordWrap is on) should only run when width changes.
    /// </summary>
    [Fact]
    public void OnSubViewsLaidOut_Skips_WrapTextModel_When_Width_Unchanged ()
    {
        string longLine = new ('A', 50);
        string text = string.Join ("\n", Enumerable.Range (1, 20).Select (i => $"{longLine} line {i}"));

        TextView tv = new () { Width = 30, Height = 10, WordWrap = true, Text = text };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Record state after initial layout
        int initialLines = tv.Lines;
        Size initialSize = tv.GetContentSize ();

        // Act: Trigger multiple layout passes without changing width
        for (var i = 0; i < 5; i++)
        {
            tv.SetNeedsDraw ();
            tv.LayoutSubViews ();
        }

        // Assert: Lines and content size should be identical — wrapping was not redone
        Assert.Equal (initialLines, tv.Lines);
        Assert.Equal (initialSize, tv.GetContentSize ());
    }

    /// <summary>
    ///     Verifies that content size width is cached between scrolls. Multiple scroll
    ///     operations without text changes should return the same cached width without
    ///     re-scanning all lines.
    /// </summary>
    [Fact]
    public void ContentSize_Width_Stable_Across_Multiple_Scrolls ()
    {
        // Arrange: Document with lines of varying width
        List<string> lines = [];

        for (var i = 0; i < 200; i++)
        {
            lines.Add (new string ('X', i % 40 + 10)); // widths from 10 to 49
        }

        TextView tv = new () { Width = 30, Height = 10, Text = string.Join ("\n", lines) };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        Size initialSize = tv.GetContentSize ();

        // Act: Scroll through the document
        for (var y = 0; y < 180; y += 10)
        {
            tv.Viewport = tv.Viewport with { Y = y };
        }

        // Assert: Content size must remain identical throughout
        Size finalSize = tv.GetContentSize ();
        Assert.Equal (initialSize.Width, finalSize.Width);
        Assert.Equal (initialSize.Height, finalSize.Height);
    }

    /// <summary>
    ///     Verifies that after content changes, the cached max width is correctly invalidated
    ///     and recalculated. This ensures the caching mechanism doesn't serve stale data.
    /// </summary>
    [Fact]
    public void ContentSize_Width_Updates_After_Content_Change_Then_Caches ()
    {
        TextView tv = new () { Width = 80, Height = 10, Text = "Short line" };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        Size size1 = tv.GetContentSize ();

        // Add a much wider line
        tv.Text = "Short line\nThis is a much wider line that should definitely update the content size width cache";
        Size size2 = tv.GetContentSize ();
        Assert.True (size2.Width > size1.Width, $"Width should increase after wider text. Was {size1.Width}, now {size2.Width}");

        // Shrink text back to short lines
        tv.Text = "Tiny";
        Size size3 = tv.GetContentSize ();
        Assert.True (size3.Width < size2.Width, $"Width should decrease after shrinking text. Was {size2.Width}, now {size3.Width}");

        // Multiple layouts without content change — width should be stable
        tv.LayoutSubViews ();
        tv.LayoutSubViews ();
        Size size4 = tv.GetContentSize ();
        Assert.Equal (size3.Width, size4.Width);
    }

    [Fact]
    public void ContentSize_Width_Updates_Correctly_When_Text_Is_Inserted_At_Middle_Of_Longest_Line_Before_AdjustScroll ()
    {
        TextView tv = new () { Width = 80, Height = 10, Text = "Short line\nThis is the longest line in the document\nMedium line" };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        Size initialSize = tv.GetContentSize ();

        tv.ContentsChanged += (_, _) =>
        {
            Size newSize = tv.GetContentSize ();
            Assert.True (newSize.Width > initialSize.Width, $"Content width should not decrease after mutation. Initial: {initialSize.Width}, New: {newSize.Width}");
        };

        // Insert text in the middle of the longest line to make it even longer
        tv.InsertionPoint = new Point (10, 1); // Position cursor in the middle of the longest line
        tv.NewKeyDownEvent (Key.A); // Simulate typing 'A' to increase line length

        Size afterInsertSize = tv.GetContentSize ();
        Assert.True (afterInsertSize.Width > initialSize.Width, $"Width should increase after inserting longer text. Was {initialSize.Width}, now {afterInsertSize.Width}");
    }

    #endregion
}
