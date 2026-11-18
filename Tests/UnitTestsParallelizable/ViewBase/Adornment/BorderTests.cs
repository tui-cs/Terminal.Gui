using UnitTests;

namespace ViewBaseTests.Adornments;

public class BorderTests : TestDriverBase
{
    [Fact]
    public void Constructor_Defaults ()
    {
        Border border = new ();
        Assert.Null (border.View);
        Assert.Null (border.LineStyle);
        Assert.Equal (BorderSettings.Title, border.Settings);
    }

    [Fact]
    public void View_Constructor_Defaults ()
    {
        View view = new () { Height = 3, Width = 3 };
        Assert.Null (view.Border.View);

        view.Border.GetOrCreateView ();
        Assert.NotNull (view.Border.View);
        Assert.False (view.Border.View?.CanFocus);
        Assert.Equal (TabBehavior.TabStop, view.Border.View?.TabStop);
        Assert.Empty (view.Border.View?.KeyBindings.GetBindings ()!);
        Assert.Null (view.Border.View?.ShadowStyle);
    }

    [Fact]
    public void LineStyle_Set_EnsuresView ()
    {
        Border border = new ();
        Assert.Null (border.View);

        border.LineStyle = null;
        Assert.Null (border.View);

        border.LineStyle = LineStyle.None;
        Assert.NotNull (border.View);

        border.LineStyle = LineStyle.Single;
        Assert.NotNull (border.View);
    }

    [Fact]
    public void WithView_NeedsDraw ()
    {
        var view = new View
        {
            X = 0,
            Y = 0,
            Width = 4,
            Height = 4,
            Driver = CreateTestDriver ()
        };
        view.Border.Thickness = new Thickness (1);

        Assert.Null (view.Border.View);

        view.Border.LineStyle = LineStyle.Dashed;
        Assert.NotNull (view.Border.View);
        Assert.True (view.Border.View?.NeedsDraw);

        view.Draw ();

        Assert.False (view.Border.View?.NeedsDraw);
    }

    [Fact]
    public void WithView_Thickness_Is_Empty_By_Default ()
    {
        View view = new () { Height = 3, Width = 3 };
        Assert.Equal (Thickness.Empty, view.Border.Thickness);
    }

    [Fact]
    public void View_Viewport_Location_Always_Empty_Size_Correct ()
    {
        var view = new View { Frame = new Rectangle (1, 2, 20, 20) };

        Assert.Equal (new Rectangle (1, 2, 20, 20), view.Frame);
        Assert.Equal (new Rectangle (0, 0, 20, 20), view.Viewport);
        view.Border.GetOrCreateView ();
        Assert.Equal (new Rectangle (0, 0, 20, 20), view.Border.View?.Viewport);

        view.Border.Thickness = new Thickness (1);

        Assert.Equal (new Rectangle (0, 0, 18, 18), view.Viewport);
        Assert.Equal (new Rectangle (0, 0, 20, 20), view.Border.View?.Viewport);

        view.Margin.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (0, 0, 16, 16), view.Viewport);
        Assert.Equal (new Rectangle (0, 0, 18, 18), view.Border.View?.Viewport);
    }

    [Fact]
    public void GetFrame_Without_View_Is_Parent_Offset_By_Margin ()
    {
        Border border = new ();

        Assert.Equal (Rectangle.Empty, border.GetFrame ());

        border.Parent = new View { Frame = new Rectangle (1, 2, 3, 4) };

        Assert.Equal (new Rectangle (0, 0, 3, 4), border.GetFrame ());

        border.Parent.Frame = new Rectangle (10, 20, 30, 40);
        Assert.Equal (new Rectangle (0, 0, 30, 40), border.GetFrame ());

        border.Parent.Margin.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (1, 1, 28, 38), border.GetFrame ());
    }

    [Fact]
    public void GetFrame_With_View_Tracks_View_Frame ()
    {
        Border border = new ();

        Assert.Equal (Rectangle.Empty, border.GetFrame ());

        border.View = new MarginView { Id = "border.View" };

        View parent = new () { Id = "border.Parent", Frame = new Rectangle (1, 2, 3, 4) };

        border.Parent = parent;
        Assert.Equal (border.View.Frame with { Location = Point.Empty }, border.GetFrame ());

        border.Parent.Frame = new Rectangle (10, 20, 30, 40);
        Assert.Equal (new Rectangle (0, 0, 30, 40), border.GetFrame ());

        border.Parent.Margin.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (1, 1, 28, 38), border.GetFrame ());
    }

    [Fact]
    public void GetFrame_With_View_Is_Parent_Offset_By_Margin ()
    {
        Border border = new ();

        Assert.Equal (Rectangle.Empty, border.GetFrame ());

        border.View = new BorderView ();

        View parent = new () { Frame = new Rectangle (1, 2, 3, 4) };

        border.Parent = parent;

        Assert.Equal (new Rectangle (0, 0, 3, 4), border.GetFrame ());

        border.Parent.Frame = new Rectangle (10, 20, 30, 40);
        Assert.Equal (new Rectangle (0, 0, 30, 40), border.GetFrame ());

        border.Parent.Margin.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (1, 1, 28, 38), border.GetFrame ());
    }
}
