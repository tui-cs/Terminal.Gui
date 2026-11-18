using Microsoft.VisualStudio.TestPlatform.Utilities;
using UnitTests;

namespace ViewBaseTests.Adornments;

public class PaddingTests
{
    [Fact]
    public void Constructor_Defaults ()
    {
        Padding padding = new ();
        Assert.Null (padding.View);
    }

    [Fact]
    public void View_Constructor_Defaults ()
    {
        View view = new () { Height = 3, Width = 3 };
        Assert.Null (view.Padding.View);

        view.Padding.GetOrCreateView ();
        Assert.True (view.Padding.View?.CanFocus);
        Assert.Equal (TabBehavior.NoStop, view.Padding.View?.TabStop);
        Assert.Empty (view.Padding.View?.KeyBindings.GetBindings ()!);
    }

    [Fact]
    public void Thickness_Is_Empty_By_Default ()
    {
        View view = new () { Height = 3, Width = 3 };
        Assert.Equal (Thickness.Empty, view.Padding.Thickness);
    }

    [Fact]
    public void GetFrame_Without_View_Is_Parent_Offset_By_Margin_And_Border ()
    {
        Padding padding = new ();

        Assert.Equal (Rectangle.Empty, padding.GetFrame ());

        View parent = new () { Frame = new Rectangle (1, 2, 3, 4) };
        padding.Parent = parent;

        parent.Frame = new Rectangle (10, 20, 30, 40);
        Assert.Equal (new Rectangle (0, 0, 30, 40), padding.GetFrame ());

        padding.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (0, 0, 30, 40), padding.GetFrame ());

        parent.Border.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (1, 1, 28, 38), padding.GetFrame ());

        parent.Margin.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (2, 2, 26, 36), padding.GetFrame ());
    }

    [Fact]
    public void GetFrame_With_View_Tracks_View_Margin_And_Border ()
    {
        var view = new View { Id = "view" };
        view.Padding.GetOrCreateView ();
        view.Margin.View?.Id = "view.Padding.View";

        view.Frame = new Rectangle (1, 2, 3, 4);
        Assert.Equal (new Rectangle (0, 0, 3, 4), view.Padding.View?.Frame);
        Assert.Equal (view.Padding.View?.Frame, view.Padding.GetFrame ());

        view.Padding.Parent?.Frame = new Rectangle (10, 20, 30, 40);
        Assert.Equal (new Rectangle (0, 0, 30, 40), view.Padding.GetFrame ());

        view.Padding.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (0, 0, 30, 40), view.Padding.GetFrame ());

        view.Border.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (1, 1, 28, 38), view.Padding.GetFrame ());

        view.Margin.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (2, 2, 26, 36), view.Padding.GetFrame ());
    }

    [Fact]
    public void GetFrame_With_View_Is_Parent_Offset_By_Margin_And_Border ()
    {
        Padding padding = new ();

        Assert.Equal (Rectangle.Empty, padding.GetFrame ());

        padding.View = new PaddingView ();

        View parent = new () { Frame = new Rectangle (1, 2, 3, 4) };
        padding.Parent = parent;
        Assert.Equal (new Rectangle (0, 0, 3, 4), padding.GetFrame ());

        parent.Padding.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (0, 0, 3, 4), padding.GetFrame ());

        padding.Parent.Frame = new Rectangle (10, 20, 30, 40);
        Assert.Equal (new Rectangle (0, 0, 30, 40), padding.GetFrame ());

        parent.Border.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (1, 1, 28, 38), padding.GetFrame ());

        parent.Margin.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (2, 2, 26, 36), padding.GetFrame ());
    }

    [Fact]
    public void Uses_Parent_Scheme ()
    {
        View view = new () { Height = 3, Width = 3 };
        view.Padding.Thickness = new Thickness (1);
        view.Padding.GetOrCreateView ();

        view.SetScheme (new Scheme { Normal = new Attribute (Color.Red, Color.Green), Focus = new Attribute (Color.Green, Color.Red) });

        Assert.Equal (ColorName16.Red, view.Padding.View!.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (view.GetAttributeForRole (VisualRole.Normal), view.Padding.View!.GetAttributeForRole (VisualRole.Normal));
    }

    [Fact]
    public void Uses_Parent_Scheme_With_View ()
    {
        View view = new () { Height = 3, Width = 3 };
        view.Padding.GetOrCreateView ();
        view.Padding.Thickness = new Thickness (1);

        view.SetScheme (new Scheme { Normal = new Attribute (Color.Red, Color.Green), Focus = new Attribute (Color.Green, Color.Red) });

        Assert.Equal (ColorName16.Red, view.Padding.View!.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (view.GetAttributeForRole (VisualRole.Normal), view.Padding.View!.GetAttributeForRole (VisualRole.Normal));
    }
}
