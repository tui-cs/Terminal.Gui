using static Terminal.Gui.ViewBase.Dim;

namespace ViewBaseTests.Layout;

// Claude - Opus 4.8
// Diagnostics for issue #5499: changing Text on a view sized solely by its Text (both Width and Height are exactly
// DimAutoStyle.Text) must skip SetNeedsLayout - and the ancestor propagation it triggers - when the new Text produces
// the same Frame size. The content still changes, so a reformat and redraw must still happen.
public partial class DimAutoTests
{
    [Fact]
    public void Text_SameSize_DoesNotSetNeedsLayout_ButStillRedraws ()
    {
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text), Text = "12:00:00" };
        view.Layout ();
        Assert.Equal (new Rectangle (0, 0, 8, 1), view.Frame);

        view.NeedsLayout = false;
        view.ClearNeedsDraw ();

        // Same width and height (8x1), different content
        view.Text = "12:00:01";

        Assert.False (view.NeedsLayout);
        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void Text_SameSize_SetsTextFormatterNeedsFormat ()
    {
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text), Text = "12:00:00" };
        view.Layout ();

        view.NeedsLayout = false;
        view.TextFormatter.NeedsFormat = false;

        view.Text = "12:00:01";

        Assert.True (view.TextFormatter.NeedsFormat);
    }

    [Fact]
    public void Text_SameSize_DoesNotPropagateNeedsLayoutToSuperView ()
    {
        // This is the key diagnostic the issue asked for: an identical-size text update must not mark the ancestor
        // chain as needing layout.
        View superView = new () { Width = 50, Height = 10 };
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text), Text = "12:00:00" };
        superView.Add (view);
        superView.Layout (new Size (50, 10));
        Assert.Equal (new Rectangle (0, 0, 8, 1), view.Frame);

        superView.NeedsLayout = false;
        view.NeedsLayout = false;

        view.Text = "12:00:01";

        Assert.False (view.NeedsLayout);
        Assert.False (superView.NeedsLayout);
    }

    [Fact]
    public void Text_DifferentSize_SetsNeedsLayout_AndPropagatesToSuperView ()
    {
        View superView = new () { Width = 50, Height = 10 };
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text), Text = "12:00:00" };
        superView.Add (view);
        superView.Layout (new Size (50, 10));

        superView.NeedsLayout = false;
        view.NeedsLayout = false;

        // Wider text changes the Frame size
        view.Text = "12:00:00 in the morning";

        Assert.True (view.NeedsLayout);
        Assert.True (superView.NeedsLayout);
    }

    [Fact]
    public void Text_SameSize_PreservesPendingLayoutRequest ()
    {
        // The optimization only avoids *setting* NeedsLayout; it never clears it. A layout request that was already
        // pending must survive an identical-size text change.
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text), Text = "12:00:00" };
        view.Layout ();

        view.SetNeedsLayout ();
        Assert.True (view.NeedsLayout);

        view.Text = "12:00:01";

        Assert.True (view.NeedsLayout);
    }

    [Fact]
    public void Text_RepeatedSameWidthUpdates_NoFrameDrift_NoAncestorLayout ()
    {
        // Clock simulation: a Text-auto label whose text changes every "tick" to the same width must never drift its
        // Frame and never mark its SuperView for layout. See issue #5499.
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text), Text = "00:00:00" };
        View superView = new () { Width = 40, Height = 5 };
        superView.Add (view);
        superView.Layout (new Size (40, 5));
        Rectangle frame0 = view.Frame;

        for (var i = 0; i < 100; i++)
        {
            superView.NeedsLayout = false;
            view.NeedsLayout = false;

            view.Text = $"{i % 24:D2}:{i % 60:D2}:{i % 60:D2}"; // always 8 wide

            Assert.False (view.NeedsLayout);
            Assert.False (superView.NeedsLayout);
            Assert.Equal (frame0, view.Frame);
        }
    }

    [Fact]
    public void Text_ReentrantTextChanged_Settles ()
    {
        // A TextChanged handler that sets Text again (to another same-width value) on the fast path must settle
        // without an infinite loop and apply the final value.
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text), Text = "aaa" };
        view.Layout ();

        var count = 0;
        view.TextChanged += (sender, _) =>
                            {
                                count++;

                                if (count == 1)
                                {
                                    ((View)sender!).Text = "bbb";
                                }
                            };

        view.NeedsLayout = false;

        view.Text = "ccc";

        Assert.Equal ("bbb", view.Text);
        Assert.Equal (new Rectangle (0, 0, 3, 1), view.Frame);
    }

    [Fact]
    public void Text_AutoStyle_BypassesOptimization ()
    {
        // DimAutoStyle.Auto (Content | Text) also depends on content/subviews, so the optimization must not apply
        // even when the text size is unchanged.
        View view = new () { Width = Auto (), Height = Auto (), Text = "12:00:00" };
        view.Layout ();

        view.NeedsLayout = false;

        view.Text = "12:00:01";

        Assert.True (view.NeedsLayout);
    }

    [Fact]
    public void Text_FixedDimensions_DoesNotSetNeedsLayout_ButStillRedraws ()
    {
        // A fixed-size view's Frame never changes on a text change, so layout is skipped and only a redraw happens -
        // even when the new text is a different length (it still fits the fixed Frame). See issue #5499 (Finding 3:
        // fixed-size text redraws should not force layout).
        View view = new () { Width = 20, Height = 3, Text = "12:00:00" };
        view.Layout ();

        view.NeedsLayout = false;
        view.ClearNeedsDraw ();

        view.Text = "a much longer string that still fits the fixed width";

        Assert.False (view.NeedsLayout);
        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void Text_FixedDimensions_WordWrap_FormatterConstraintsMatchFullLayout ()
    {
        // Regression: the same-frame fast path must mirror SetRelativeLayout's formatter finalization. Otherwise
        // UpdateTextFormatterText clears the constraints and they are never restored, so a null (= int.MaxValue) width
        // breaks wrapping/clipping for fixed-size text. See issue #5499 review.
        View view = new () { Width = 5, Height = 2 };
        view.TextFormatter.WordWrap = true;
        view.Text = "abc";
        view.Layout ();

        view.NeedsLayout = false;

        // Different content, same fixed Frame - takes the redraw-only fast path
        view.Text = "abcdefghi";

        Assert.False (view.NeedsLayout);

        // Contract: the fast path leaves TextFormatter in the same state a full layout pass would.
        int? fastWidth = view.TextFormatter.ConstrainToWidth;
        int? fastHeight = view.TextFormatter.ConstrainToHeight;
        Assert.NotNull (fastWidth);
        Assert.NotNull (fastHeight);

        view.SetNeedsLayout ();
        view.Layout ();
        Assert.Equal (view.TextFormatter.ConstrainToWidth, fastWidth);
        Assert.Equal (view.TextFormatter.ConstrainToHeight, fastHeight);
    }

    [Fact]
    public void Text_OneAxisAuto_WordWrap_FormatterConstraintsMatchFullLayout ()
    {
        // One axis Text-auto, the other fixed - same contract: fast-path constraints equal a full layout's.
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = 2 };
        view.TextFormatter.WordWrap = true;
        view.Text = "abc";
        view.Layout ();

        view.NeedsLayout = false;

        view.Text = "xyz"; // same 3x2 Frame

        Assert.False (view.NeedsLayout);

        int? fastWidth = view.TextFormatter.ConstrainToWidth;
        int? fastHeight = view.TextFormatter.ConstrainToHeight;
        Assert.NotNull (fastWidth);
        Assert.NotNull (fastHeight);

        view.SetNeedsLayout ();
        view.Layout ();
        Assert.Equal (view.TextFormatter.ConstrainToWidth, fastWidth);
        Assert.Equal (view.TextFormatter.ConstrainToHeight, fastHeight);
    }

    [Fact]
    public void Text_OneAxisAuto_SameSize_DoesNotSetNeedsLayout ()
    {
        // One axis Text-auto, the other fixed - a common label shape. Same-size text change skips layout. See issue
        // #5499 (Finding 3: one-axis auto scenarios).
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = 1, Text = "12:00:00" };
        view.Layout ();
        Assert.Equal (new Rectangle (0, 0, 8, 1), view.Frame);

        view.NeedsLayout = false;

        view.Text = "12:00:01";

        Assert.False (view.NeedsLayout);
    }

    [Fact]
    public void Text_OneAxisAuto_DifferentSize_SetsNeedsLayout ()
    {
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = 1, Text = "12:00:00" };
        view.Layout ();

        view.NeedsLayout = false;

        view.Text = "12:00:00 in the morning";

        Assert.True (view.NeedsLayout);
    }

    [Fact]
    public void Text_PositionDependsOnText_SameSize_SetsNeedsLayout ()
    {
        // Finding 1: comparing only size is wrong. A Pos can depend on Text, so a same-size text change can still move
        // the view. The full-Frame prediction must detect the position change and run layout.
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text), Text = "aa" };
        view.X = Pos.Func (v => v!.Text.StartsWith ("b") ? 5 : 0, view);
        view.Layout ();
        Assert.Equal (new Rectangle (0, 0, 2, 1), view.Frame);

        view.NeedsLayout = false;

        // Same size (2x1) but X must move from 0 to 5
        view.Text = "bb";

        Assert.True (view.NeedsLayout);
    }

    [Fact]
    public void Text_CompositeDim_BypassesOptimization ()
    {
        // Finding 2: the guard must be exact. Dim.Auto(Text) + 2 is a composite (DimCombine), not a bare Text-auto, so
        // it must not enter the size-only fast path.
        View view = new () { Width = Auto (DimAutoStyle.Text) + 2, Height = Auto (DimAutoStyle.Text), Text = "12:00:00" };
        view.Layout ();

        view.NeedsLayout = false;

        view.Text = "12:00:01";

        Assert.True (view.NeedsLayout);
    }

    [Fact]
    public void Text_NotYetLaidOut_SetsNeedsLayout ()
    {
        // _frame is null until the first layout pass; the optimization must bypass so the first layout can establish
        // the Frame.
        View view = new () { Width = Auto (DimAutoStyle.Text), Height = Auto (DimAutoStyle.Text) };

        view.Text = "12:00:00";

        Assert.True (view.NeedsLayout);
    }

    [Fact]
    public void Text_SameSize_WithMinimum_DoesNotSetNeedsLayout ()
    {
        // Both texts are narrower than the minimum, so the minimum anchor clamps the width to the same value. The
        // predictor reuses DimAuto.Calculate, so the min anchor is honored.
        View view = new ()
        {
            Width = Auto (DimAutoStyle.Text, minimumContentDim: 20),
            Height = Auto (DimAutoStyle.Text),
            Text = "ab"
        };
        view.Layout ();
        Assert.Equal (20, view.Frame.Width);

        view.NeedsLayout = false;

        view.Text = "cd";

        Assert.Equal (20, view.Frame.Width);
        Assert.False (view.NeedsLayout);
    }

    [Fact]
    public void Text_ExceedsMinimum_SetsNeedsLayout ()
    {
        // Growing past the minimum changes the resulting width, so layout must run.
        View view = new ()
        {
            Width = Auto (DimAutoStyle.Text, minimumContentDim: 20),
            Height = Auto (DimAutoStyle.Text),
            Text = "ab"
        };
        view.Layout ();
        Assert.Equal (20, view.Frame.Width);

        view.NeedsLayout = false;

        view.Text = new string ('x', 30);

        Assert.True (view.NeedsLayout);
    }

    [Fact]
    public void Text_SameSize_WithBorderAdornment_DoesNotSetNeedsLayout ()
    {
        // The adornment thickness is part of the Frame size. The predictor must account for it (it does, by reusing
        // DimAuto.Calculate which adds adornment thickness).
        View view = new ()
        {
            Width = Auto (DimAutoStyle.Text),
            Height = Auto (DimAutoStyle.Text),
            BorderStyle = LineStyle.Single, // 1-thick adornment on each side
            Text = "12:00:00"
        };
        view.Layout ();
        Assert.Equal (new Rectangle (0, 0, 10, 3), view.Frame); // 8 + 2 wide, 1 + 2 tall

        view.NeedsLayout = false;

        view.Text = "12:00:01";

        Assert.Equal (new Rectangle (0, 0, 10, 3), view.Frame);
        Assert.False (view.NeedsLayout);
    }
}
