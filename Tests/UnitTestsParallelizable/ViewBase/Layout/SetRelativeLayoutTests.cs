using static Terminal.Gui.ViewBase.Dim;

namespace ViewBaseTests.Layout;

public class SetRelativeLayoutTests
{
    [Fact]
    public void AbsolutePosDim_DontChange ()
    {
        var screen = new Size (10, 15);

        var view = new View
        {
            X = 1, // outside of screen +10
            Y = 2, // outside of screen -10
            Width = 3,
            Height = 4
        };

        // Layout is Absolute. So the X and Y are not changed.
        view.SetRelativeLayout (screen);
        Assert.Equal (1, view.Frame.X);
        Assert.Equal (2, view.Frame.Y);
        Assert.Equal (3, view.Frame.Width);
        Assert.Equal (4, view.Frame.Height);
    }

    [Fact]
    public void ComputedPosDim_StayComputed ()
    {
        var screen = new Size (10, 15);
        var view = new View { X = 1, Y = 2, Width = Dim.Fill (), Height = Dim.Fill () };

        Assert.Equal ("Absolute(1)", view.X.ToString ());
        Assert.Equal ("Absolute(2)", view.Y.ToString ());
        Assert.Equal ("Fill(Absolute(0))", view.Width.ToString ());
        Assert.Equal ("Fill(Absolute(0))", view.Height.ToString ());
        view.SetRelativeLayout (screen);
        Assert.Equal ("Fill(Absolute(0))", view.Width.ToString ());
        Assert.Equal ("Fill(Absolute(0))", view.Height.ToString ());
    }

    [Fact]
    public void DimFill_Is_Honored ()
    {
        var view = new View { X = 1, Y = 1, Width = Dim.Fill (), Height = Dim.Fill () };

        view.SetRelativeLayout (new Size (80, 25));
        Assert.Equal ("Fill(Absolute(0))", view.Width.ToString ());
        Assert.Equal (1, view.Frame.X);
        Assert.Equal (1, view.Frame.Y);
        Assert.Equal (79, view.Frame.Width);
        Assert.Equal (24, view.Frame.Height);
        Assert.Equal (0, view.Viewport.X);
        Assert.Equal (0, view.Viewport.Y);
        Assert.Equal (79, view.Viewport.Width);
        Assert.Equal (24, view.Viewport.Height);

        view.X = 0;
        view.Y = 0;
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        Assert.Equal ("Fill(Absolute(0))", view.Width.ToString ());
        view.SetRelativeLayout (new Size (80, 25));
        Assert.Equal (0, view.Frame.X);
        Assert.Equal (0, view.Frame.Y);
        Assert.Equal (80, view.Frame.Width);
        Assert.Equal (25, view.Frame.Height);
        Assert.Equal (0, view.Viewport.X);
        Assert.Equal (0, view.Viewport.Y);
        Assert.Equal (80, view.Viewport.Width);
        Assert.Equal (25, view.Viewport.Height);
    }

    [Fact]
    public void Fill_And_PosCenter ()
    {
        var screen = new Size (80, 25);
        var view = new View { X = Pos.Center (), Y = Pos.Center (), Width = Dim.Fill (), Height = Dim.Fill () };

        view.SetRelativeLayout (screen);
        Assert.Equal (0, view.Frame.X);
        Assert.Equal (0, view.Frame.Y);
        Assert.Equal (80, view.Frame.Width);
        Assert.Equal (25, view.Frame.Height);

        view.X = Pos.Center () + 1;
        view.SetRelativeLayout (screen);
        Assert.Equal (1, view.Frame.X);
        Assert.Equal (0, view.Frame.Y);
        Assert.Equal (79, view.Frame.Width);
        Assert.Equal (25, view.Frame.Height);

        view.X = Pos.Center () + 79;
        view.SetRelativeLayout (screen);
        Assert.Equal (79, view.Frame.X);
        Assert.Equal (0, view.Frame.Y);
        Assert.Equal (1, view.Frame.Width);
        Assert.Equal (25, view.Frame.Height);

        view.X = Pos.Center () + 80;
        view.SetRelativeLayout (screen);
        Assert.Equal (80, view.Frame.X);
        Assert.Equal (0, view.Frame.Y);
        Assert.Equal (0, view.Frame.Width);
        Assert.Equal (25, view.Frame.Height);

        view.X = Pos.Center () - 1;
        view.SetRelativeLayout (screen);
        Assert.Equal (-1, view.Frame.X);
        Assert.Equal (0, view.Frame.Y);
        Assert.Equal (81, view.Frame.Width);
        Assert.Equal (25, view.Frame.Height);

        view.X = Pos.Center () - 2; // Fill means all the way to right. So width will be 82. (dim gets calc'd before pos).
        view.SetRelativeLayout (screen);
        Assert.Equal (-2, view.Frame.X);
        Assert.Equal (0, view.Frame.Y);
        Assert.Equal (82, view.Frame.Width);
        Assert.Equal (25, view.Frame.Height);

        view.X = Pos.Center () - 3; // Fill means all the way to right. So width will be 83. (dim gets calc'd before pos).
        view.SetRelativeLayout (screen);
        Assert.Equal (-3, view.Frame.X);
        Assert.Equal (0, view.Frame.Y);
        Assert.Equal (83, view.Frame.Width);
        Assert.Equal (25, view.Frame.Height);

        view.X = Pos.Center () - 41; // Fill means all the way to right. So width will be . (dim gets calc'd before pos).
        view.SetRelativeLayout (screen);
        Assert.Equal (-41, view.Frame.X);
        Assert.Equal (0, view.Frame.Y);
        Assert.Equal (121, view.Frame.Width);
        Assert.Equal (25, view.Frame.Height);
    }

    [Fact]
    public void Fill_Pos_Outside_Bounds ()
    {
        var screen = new Size (80, 25);

        var view = new View
        {
            X = 90, // outside of screen +10
            Y = -10, // outside of screen -10
            Width = 15,
            Height = 15
        };

        // Layout is Absolute. So the X and Y are not changed.
        view.SetRelativeLayout (screen);
        Assert.Equal (90, view.Frame.X);
        Assert.Equal (-10, view.Frame.Y);
        Assert.Equal (15, view.Frame.Width);
        Assert.Equal (15, view.Frame.Height);

        // prove Width=Height= same as screen size
        view.Width = 80;
        view.Height = 25;
        view.SetRelativeLayout (screen);
        Assert.Equal (90, view.Frame.X);
        Assert.Equal (-10, view.Frame.Y);
        Assert.Equal (80, view.Frame.Width);
        Assert.Equal (25, view.Frame.Height);

        view.Width = Dim.Fill ();
        view.Height = Dim.Fill ();
        view.SetRelativeLayout (screen);
        Assert.Equal (90, view.Frame.X);
        Assert.Equal (-10, view.Frame.Y);

        Assert.Equal (
                      0,
                      view.Frame
                          .Width
                     ); // proof: 15x15 view is placed beyond right side of screen, so fill width is 0

        Assert.Equal (
                      35,
                      view.Frame
                          .Height
                     ); // proof: 15x15 view is placed beyond top of screen 10 rows, screen is 25 rows. so fill height is 25 + 10 = 35
    }

    [Fact]
    public void Fill_Pos_Within_Bounds ()
    {
        var screen = new Size (80, 25);
        var view = new View { X = 1, Y = 1, Width = 5, Height = 4 };

        view.SetRelativeLayout (screen);
        Assert.Equal (1, view.Frame.X);
        Assert.Equal (1, view.Frame.Y);
        Assert.Equal (5, view.Frame.Width);
        Assert.Equal (4, view.Frame.Height);

        view.Width = 80;
        view.Height = 25;
        view.SetRelativeLayout (screen);
        Assert.Equal (1, view.Frame.X);
        Assert.Equal (1, view.Frame.Y);
        Assert.Equal (80, view.Frame.Width);
        Assert.Equal (25, view.Frame.Height);

        view.Width = Dim.Fill ();
        view.Height = Dim.Fill ();
        view.SetRelativeLayout (screen);
        Assert.Equal (1, view.Frame.X);
        Assert.Equal (1, view.Frame.Y);
        Assert.Equal (79, view.Frame.Width); // proof (80 - 1)
        Assert.Equal (24, view.Frame.Height); // proof (25 - 1)

        view.X = 79;
        view.Width = Dim.Fill ();
        view.Height = Dim.Fill ();
        view.SetRelativeLayout (screen);
        Assert.Equal (79, view.Frame.X);
        Assert.Equal (1, view.Frame.Y);
        Assert.Equal (1, view.Frame.Width); // proof (80 - 79)
        Assert.Equal (24, view.Frame.Height);

        view.X = 80;
        view.Width = Dim.Fill ();
        view.Height = Dim.Fill ();
        view.SetRelativeLayout (screen);
        Assert.Equal (80, view.Frame.X);
        Assert.Equal (1, view.Frame.Y);
        Assert.Equal (0, view.Frame.Width); // proof (80 - 80)
        Assert.Equal (24, view.Frame.Height);
    }

    [Fact]
    public void PosCombine_Plus_Absolute ()
    {
        var superView = new View { Width = 10, Height = 10 };

        var testView = new View
        {
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = 1,
            Height = 1
        };
        superView.Add (testView);
        superView.Layout ();
        testView.SetRelativeLayout (superView.Frame.Size);
        Assert.Equal (4, testView.Frame.X);
        Assert.Equal (4, testView.Frame.Y);

        testView = new ()
        {
            X = Pos.Center () + 1, // ((10 / 2) - (1 / 2)) + 1 = 5 - 1 + 1 = 5
            Y = Pos.Center () + 1,
            Width = 1,
            Height = 1
        };
        superView.Add (testView);
        testView.SetRelativeLayout (superView.Frame.Size);
        Assert.Equal (5, testView.Frame.X);
        Assert.Equal (5, testView.Frame.Y);

        testView = new ()
        {
            X = 1 + Pos.Center (),
            Y = 1 + Pos.Center (),
            Width = 1,
            Height = 1
        };
        superView.Add (testView);
        testView.SetRelativeLayout (superView.Frame.Size);
        Assert.Equal (5, testView.Frame.X);
        Assert.Equal (5, testView.Frame.Y);

        testView = new ()
        {
            X = 1 + Pos.Percent (50),
            Y = Pos.Percent (50) + 1,
            Width = 1,
            Height = 1
        };
        superView.Add (testView);
        testView.SetRelativeLayout (superView.Frame.Size);
        Assert.Equal (6, testView.Frame.X);
        Assert.Equal (6, testView.Frame.Y);

        testView = new ()
        {
            X = Pos.Percent (10) + Pos.Percent (40),
            Y = Pos.Percent (10) + Pos.Percent (40),
            Width = 1,
            Height = 1
        };
        superView.Add (testView);
        testView.SetRelativeLayout (superView.Frame.Size);
        Assert.Equal (5, testView.Frame.X);
        Assert.Equal (5, testView.Frame.Y);

        testView = new ()
        {
            X = 1 + Pos.Percent (10) + Pos.Percent (40) - 1,
            Y = 5 + Pos.Percent (10) + Pos.Percent (40) - 5,
            Width = 1,
            Height = 1
        };
        superView.Add (testView);
        testView.SetRelativeLayout (superView.Frame.Size);
        Assert.Equal (5, testView.Frame.X);
        Assert.Equal (5, testView.Frame.Y);

        testView = new ()
        {
            X = Pos.Left (testView),
            Y = Pos.Left (testView),
            Width = 1,
            Height = 1
        };
        superView.Add (testView);
        testView.SetRelativeLayout (superView.Frame.Size);
        Assert.Equal (5, testView.Frame.X);
        Assert.Equal (5, testView.Frame.Y);

        testView = new ()
        {
            X = 1 + Pos.Left (testView),
            Y = Pos.Top (testView) + 1,
            Width = 1,
            Height = 1
        };
        superView.Add (testView);
        testView.SetRelativeLayout (superView.Frame.Size);
        Assert.Equal (6, testView.Frame.X);
        Assert.Equal (6, testView.Frame.Y);

        superView.Dispose ();
    }

    [Fact]
    public void PosCombine_PosCenter_Minus_Absolute ()
    {
        // This test used to be in ViewTests.cs Internal_Tests. It was moved here because it is testing
        // SetRelativeLayout. In addition, the old test was bogus because it was testing the wrong thing (and 
        // because in v1 Pos.Center was broken in this regard!

        var screen = new Size (80, 25);

        var view = new View
        {
            X = Pos.Center () - 41, // -2 off left edge of screen
            Y = Pos.Center () - 13, // -1 off top edge of screen
            Width = 1,
            Height = 1
        };

        view.SetRelativeLayout (screen);
        Assert.Equal (-2, view.Frame.X); // proof: 1x1 view centered in 80x25 screen has x of 39, so -41 is -2
        Assert.Equal (-1, view.Frame.Y); // proof: 1x1 view centered in 80x25 screen has y of 12, so -13 is -1

        view.Width = 80;
        view.Height = 25;
        view.SetRelativeLayout (screen);
        Assert.Equal (-41, view.Frame.X);
        Assert.Equal (-13, view.Frame.Y);
        Assert.Equal (80, view.Frame.Width);
        Assert.Equal (25, view.Frame.Height);

        view.Width = Dim.Fill ();
        view.Height = Dim.Fill ();
        view.SetRelativeLayout (screen);
        Assert.Equal (-41, view.Frame.X);
        Assert.Equal (-13, view.Frame.Y);
        Assert.Equal (121, view.Frame.Width); // 121 = screen.Width - (-Center - 41)
        Assert.Equal (38, view.Frame.Height);
    }

    [Fact]
    public void PosCombine_PosCenter_Plus_Absolute ()
    {
        var screen = new Size (80, 25);

        var view = new View
        {
            X = Pos.Center () + 41, // ((80 / 2) - (5 / 2)) + 41 = (40 - 3 + 41) = 78
            Y = Pos.Center () + 13, // ((25 / 2) - (4 / 2)) + 13 = (12 - 2 + 13) = 23
            Width = 5,
            Height = 4
        };

        view.SetRelativeLayout (screen);
        Assert.Equal (78, view.Frame.X);
        Assert.Equal (23, view.Frame.Y);
    }

    [Fact]
    public void PosDimFunc ()
    {
        var screen = new Size (30, 1);
        var view = new View
        {
            Text = "abc",
            Width = Auto (DimAutoStyle.Text),
            Height = Auto (DimAutoStyle.Text)
        };
        view.X = Pos.AnchorEnd (0) - Pos.Func (GetViewWidth);

        int GetViewWidth (View? _) { return view.Frame.Width; }

        // view will be 3 chars wide. It's X will be 27 (30 - 3).
        // BUGBUG: IsInitialized need to be true before calculate
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (screen);
        Assert.Equal (27, view.Frame.X);
        Assert.Equal (0, view.Frame.Y);
        Assert.Equal (3, view.Frame.Width);
        Assert.Equal (1, view.Frame.Height);

        var tf = new TextField { Text = "01234567890123456789" };
        tf.Width = Dim.Fill (1) - Dim.Func (GetViewWidth);

        // tf will fill the screen minus 1 minus the width of view (3).
        // so it's width will be 26 (30 - 1 - 3).
        tf.SetRelativeLayout (screen);
        Assert.Equal (0, tf.Frame.X);
        Assert.Equal (0, tf.Frame.Y);
        Assert.Equal (26, tf.Frame.Width);
        Assert.Equal (1, tf.Frame.Height);
    }



}
