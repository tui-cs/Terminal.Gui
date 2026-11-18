// Claude - Opus 4.6

namespace ViewBaseTests.Adornments;

/// <summary>
///     Tests that adornment Views are created lazily and integrate correctly with the layout system.
/// </summary>
public class AdornmentLayoutTests
{
    #region Lazy View Creation

    [Fact]
    public void Margin_View_Is_Null_By_Default ()
    {
        View view = new ();
        Assert.Null (view.Margin.View);
    }

    [Fact]
    public void Border_View_Is_Null_By_Default ()
    {
        View view = new ();
        Assert.Null (view.Border.View);
    }

    [Fact]
    public void Padding_View_Is_Null_By_Default ()
    {
        View view = new ();
        Assert.Null (view.Padding.View);
    }

    [Fact]
    public void Margin_View_Not_Created_When_Thickness_Set ()
    {
        View view = new ();
        view.Margin.Thickness = new Thickness (1);
        Assert.Null (view.Margin.View);
    }

    [Fact]
    public void Margin_View_Not_Created_When_Thickness_Empty ()
    {
        View view = new ();
        view.Margin.Thickness = Thickness.Empty;
        Assert.Null (view.Margin.View);
    }

    [Fact]
    public void Margin_View_Created_When_ShadowStyle_Set_NonNull ()
    {
        View view = new () { ShadowStyle = ShadowStyles.Opaque };
        Assert.NotNull (view.Margin.View);
    }

    [Fact]
    public void Margin_View_Not_Created_When_ShadowStyle_Null ()
    {
        View view = new () { ShadowStyle = null };
        Assert.Null (view.Margin.View);
    }

    #endregion

    #region ShadowStyle null vs None semantics

    [Fact]
    public void ShadowStyle_Null_Means_No_Thickness ()
    {
        View view = new () { ShadowStyle = ShadowStyles.Opaque };
        Assert.Equal (new Thickness (0, 0, 1, 1), view.Margin.Thickness);

        view.ShadowStyle = null;
        Assert.Equal (Thickness.Empty, view.Margin.Thickness);
    }

    [Fact]
    public void ShadowStyle_None_Preserves_Thickness ()
    {
        View view = new () { ShadowStyle = ShadowStyles.Transparent };
        (view.Margin.View as MarginView)!.ShadowSize = new Size (2, 2);
        Assert.Equal (new Thickness (0, 0, 2, 2), view.Margin.Thickness);

        view.ShadowStyle = ShadowStyles.None;
        Assert.Equal (new Thickness (0, 0, 2, 2), view.Margin.Thickness);
    }

    [Fact]
    public void ShadowStyle_Null_Does_Not_Add_Frame_Overhead ()
    {
        View superView = new () { Width = 20, Height = 10 };
        View view = new () { Width = 5, Height = 3, ShadowStyle = null };
        superView.Add (view);
        superView.LayoutSubViews ();

        Assert.Equal (new Rectangle (0, 0, 5, 3), view.Frame);
    }

    [Fact]
    public void ShadowStyle_Opaque_Adds_Frame_Overhead ()
    {
        View superView = new () { Width = 20, Height = 10 };
        View view = new () { Width = Dim.Auto (), Height = Dim.Auto (), Text = "Hi", ShadowStyle = ShadowStyles.Opaque };
        superView.Add (view);
        superView.LayoutSubViews ();

        // Opaque shadow adds 1 to right and bottom via Margin thickness
        Assert.Equal (new Thickness (0, 0, 1, 1), view.Margin.Thickness);
    }

    #endregion

    #region Layout with Adornments

    [Fact]
    public void Margin_Frame_Matches_Parent_Frame_With_Empty_Location ()
    {
        View view = new () { Frame = new Rectangle (5, 10, 20, 15) };
        view.Margin.Thickness = new Thickness (1);

        Assert.Equal (new Rectangle (0, 0, 20, 15), view.Margin.GetFrame ());
    }

    [Fact]
    public void Margin_FrameToScreen_Includes_Parent_Position ()
    {
        View view = new () { Frame = new Rectangle (5, 10, 20, 15) };
        view.Margin.Thickness = new Thickness (1);

        Rectangle screen = view.Margin.FrameToScreen ();
        Assert.Equal (5, screen.X);
        Assert.Equal (10, screen.Y);
    }

    [Fact]
    public void Padding_Frame_Is_Inside_Border ()
    {
        View view = new () { Frame = new Rectangle (0, 0, 10, 10) };
        view.Border.Thickness = new Thickness (1);
        view.Padding.Thickness = new Thickness (1);

        Rectangle paddingFrame = view.Padding.GetFrame ();

        // Padding is inside Border, so offset by border thickness
        Assert.Equal (1, paddingFrame.X);
        Assert.Equal (1, paddingFrame.Y);
        Assert.Equal (8, paddingFrame.Width);
        Assert.Equal (8, paddingFrame.Height);
    }

    [Fact]
    public void Border_Frame_Is_Inside_Margin ()
    {
        View view = new () { Frame = new Rectangle (0, 0, 10, 10) };
        view.Margin.Thickness = new Thickness (1);

        Rectangle borderFrame = view.Border.GetFrame ();

        // Border is inside Margin, so offset by margin thickness
        Assert.Equal (1, borderFrame.X);
        Assert.Equal (1, borderFrame.Y);
        Assert.Equal (8, borderFrame.Width);
        Assert.Equal (8, borderFrame.Height);
    }

    [Fact]
    public void Viewport_Shrinks_By_All_Adornment_Thicknesses ()
    {
        View view = new ()
        {
            Frame = new Rectangle (0, 0, 20, 10)
        };
        view.Margin.Thickness = new Thickness (1);
        view.Border.Thickness = new Thickness (1);
        view.Padding.Thickness = new Thickness (1);
        view.BeginInit ();
        view.EndInit ();

        // Viewport = Frame minus all adornments (1+1+1 = 3 on each side)
        Assert.Equal (14, view.Viewport.Width);
        Assert.Equal (4, view.Viewport.Height);
    }

    [Fact]
    public void Sibling_Views_Layout_Correctly_With_Margin ()
    {
        View superView = new () { Width = 20, Height = 10 };
        View view1 = new () { Width = 5, Height = 3 };
        view1.Margin.Thickness = new Thickness (1);
        View view2 = new () { X = Pos.Right (view1), Width = 5, Height = 3 };
        view2.Margin.Thickness = new Thickness (1);
        superView.Add (view1, view2);
        superView.LayoutSubViews ();

        Assert.Equal (0, view1.Frame.X);
        Assert.Equal (5, view2.Frame.X);
    }

    [Fact]
    public void View_Below_Sibling_Layout_Correctly_With_Margin ()
    {
        View superView = new () { Width = 20, Height = 10 };
        View view1 = new () { Width = 5, Height = 3 };
        view1.Margin.Thickness = new Thickness (1);
        View view2 = new () { Y = Pos.Bottom (view1), Width = 5, Height = 3 };
        view2.Margin.Thickness = new Thickness (1);
        superView.Add (view1, view2);
        superView.LayoutSubViews ();

        Assert.Equal (0, view1.Frame.Y);
        Assert.Equal (3, view2.Frame.Y);
    }

    #endregion

    #region Contains and Hit Testing

    [Fact]
    public void Margin_Contains_Works_Without_View ()
    {
        View view = new () { Frame = new Rectangle (0, 0, 10, 10) };

        // No thickness → no margin area
        Assert.Null (view.Margin.View);
        Assert.False (view.Margin.Contains (new Point (0, 0)));
    }

    [Fact]
    public void Margin_Contains_Works_With_Thickness ()
    {
        View view = new () { Frame = new Rectangle (0, 0, 10, 10) };
        view.Margin.Thickness = new Thickness (1);

        // Point in the top-left margin area (within the 1-pixel border)
        Assert.True (view.Margin.Contains (new Point (0, 0)));

        // Point inside the view (past the margin)
        Assert.False (view.Margin.Contains (new Point (5, 5)));
    }

    [Fact]
    public void Padding_Contains_Works_Without_View ()
    {
        View view = new () { Frame = new Rectangle (0, 0, 10, 10) };

        Assert.Null (view.Padding.View);
        Assert.False (view.Padding.Contains (new Point (0, 0)));
    }

    #endregion

    #region Padding View Creation via ScrollBar

    [Fact]
    public void Padding_View_Not_Created_When_Only_Thickness_Set ()
    {
        View view = new ();
        view.Padding.Thickness = new Thickness (1);

        // PaddingView should NOT exist — only Padding.Add triggers GetOrCreateView
        Assert.Null (view.Padding.View);
    }

    [Fact]
    public void Padding_View_Created_When_ScrollBar_Accessed ()
    {
        // Replicates what happens in TextView: accessing a ScrollBar triggers
        // Padding.Add(scrollBar) which calls GetOrCreateView, creating the PaddingView.
        View view = new () { Width = 10, Height = 5 };
        view.BeginInit ();
        view.EndInit ();

        // Access the vertical scrollbar — this triggers lazy creation via Padding.Add
        ScrollBar vbar = view.VerticalScrollBar;
        Assert.NotNull (vbar);
        Assert.NotNull (view.Padding.View);
    }

    [Fact]
    public void ScrollBar_In_Padding_Is_Initialized_After_Parent_Init ()
    {
        // The real scenario: a View accesses its ScrollBar during EndInit
        // (like TextView does). The PaddingView must be properly initialized.
        View view = new () { Width = 10, Height = 5 };
        view.BeginInit ();
        view.EndInit ();

        // Force scrollbar creation
        ScrollBar vbar = view.VerticalScrollBar;

        // The PaddingView should be initialized
        Assert.True (view.Padding.View!.IsInitialized,
                      "PaddingView should be initialized after parent init");

        // The scrollbar should be a subview of the PaddingView
        Assert.Contains (vbar, view.Padding.View.SubViews);
    }

    [Fact]
    public void Padding_With_ScrollBar_Found_By_GetViewsAtLocation ()
    {
        // When a View has a scrollbar, the PaddingView exists and has non-empty
        // thickness (configured by OnScrollBarInitialized). Hit testing should find it.
        View view = new () { Width = 10, Height = 5 };
        //view.BeginInit ();
        //view.EndInit ();

        // Force scrollbar creation and make it visible
        view.VerticalScrollBar.Visible = true;
        Assert.NotNull (view.Padding.View);

        // After scrollbar is visible, Padding.Thickness.Right should be > 0
        Assert.True (view.Padding.Thickness.Right > 0,
                      "Padding should have right thickness for vertical scrollbar");

        view.SetRelativeLayout (new Size (20, 20));

        List<View?> result = View.GetViewsAtLocation (view, new Point (9, 2));

        Assert.Contains (result, v => v == view.Padding.View);
    }

    #endregion
}
