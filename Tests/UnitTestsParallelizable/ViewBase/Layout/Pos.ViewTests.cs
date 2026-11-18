using static Terminal.Gui.ViewBase.Pos;

namespace ViewBaseTests.Layout;

public class PosViewTests
{
    [Fact]
    public void PosView_Equal ()
    {
        var view1 = new View ();
        var view2 = new View ();

        Pos pos1 = Left (view1);
        Pos pos2 = Left (view1);
        Assert.Equal (pos1, pos2);

        pos2 = Left (view2);
        Assert.NotEqual (pos1, pos2);

        pos2 = Right (view1);
        Assert.NotEqual (pos1, pos2);
    }

    // TODO: Test Left, Top, Right bottom Equal

    /// <summary>Tests Pos.Left, Pos.X, Pos.Top, Pos.Y, Pos.Right, and Pos.Bottom set operations</summary>
    [Fact]
    public void PosView_Side_SetsValue ()
    {
        var testRect = Rectangle.Empty;
        var testInt = 0;
        Pos pos;

        // Pos.Left
        testInt = 0;
        testRect = Rectangle.Empty;
        pos = Left (new ());
        Assert.Equal (testInt, pos.GetAnchor (0));

        pos = Left (new () { Frame = testRect });
        Assert.Equal (testRect.Left, pos.GetAnchor (0));

        testRect = new (1, 2, 3, 4);
        pos = Left (new () { Frame = testRect });
        Assert.Equal (testRect.Left, pos.GetAnchor (0));

        // Pos.Left(win) + 0
        pos = Left (new () { Frame = testRect }) + testInt;
        Assert.Equal (testRect.Left, pos.GetAnchor (0));

        testInt = 1;

        // Pos.Left(win) +1
        pos = Left (new () { Frame = testRect }) + testInt;
        Assert.Equal (testRect.Left + testInt, pos.GetAnchor (0));

        testInt = -1;

        // Pos.Left(win) -1
        pos = Left (new () { Frame = testRect }) - testInt;
        Assert.Equal (testRect.Left - testInt, pos.GetAnchor (0));

        // Pos.X
        testInt = 0;
        testRect = Rectangle.Empty;
        pos = X (new ());
        Assert.Equal (testRect.X + testInt, pos.GetAnchor (0));

        pos = X (new () { Frame = testRect });
        Assert.Equal (testRect.X, pos.GetAnchor (0));

        testRect = new (1, 2, 3, 4);
        pos = X (new () { Frame = testRect });
        Assert.Equal (testRect.X, pos.GetAnchor (0));

        // Pos.X(win) + 0
        pos = X (new () { Frame = testRect }) + testInt;
        Assert.Equal (testRect.X, pos.GetAnchor (0));

        testInt = 1;

        // Pos.X(win) +1
        pos = X (new () { Frame = testRect }) + testInt;
        Assert.Equal (testRect.X + testInt, pos.GetAnchor (0));

        testInt = -1;

        // Pos.X(win) -1
        pos = X (new () { Frame = testRect }) - testInt;
        Assert.Equal (testRect.X - testInt, pos.GetAnchor (0));

        // Pos.Top
        testInt = 0;
        testRect = Rectangle.Empty;
        pos = Top (new ());
        Assert.Equal (testRect.Top, pos.GetAnchor (0));

        pos = Top (new () { Frame = testRect });
        Assert.Equal (testRect.Top + testInt, pos.GetAnchor (0));

        testRect = new (1, 2, 3, 4);
        pos = Top (new () { Frame = testRect });
        Assert.Equal (testRect.Top, pos.GetAnchor (0));

        // Pos.Top(win) + 0
        pos = Top (new () { Frame = testRect }) + testInt;
        Assert.Equal (testRect.Top, pos.GetAnchor (0));

        testInt = 1;

        // Pos.Top(win) +1
        pos = Top (new () { Frame = testRect }) + testInt;
        Assert.Equal (testRect.Top + testInt, pos.GetAnchor (0));

        testInt = -1;

        // Pos.Top(win) -1
        pos = Top (new () { Frame = testRect }) - testInt;
        Assert.Equal (testRect.Top - testInt, pos.GetAnchor (0));

        // Pos.Y
        testInt = 0;
        testRect = Rectangle.Empty;
        pos = Y (new ());
        Assert.Equal (testRect.Y, pos.GetAnchor (0));

        pos = Y (new () { Frame = testRect });
        Assert.Equal (testRect.Y, pos.GetAnchor (0));

        testRect = new (1, 2, 3, 4);
        pos = Y (new () { Frame = testRect });
        Assert.Equal (testRect.Y, pos.GetAnchor (0));

        // Pos.Y(win) + 0
        pos = Y (new () { Frame = testRect }) + testInt;
        Assert.Equal (testRect.Y, pos.GetAnchor (0));

        testInt = 1;

        // Pos.Y(win) +1
        pos = Y (new () { Frame = testRect }) + testInt;
        Assert.Equal (testRect.Y + testInt, pos.GetAnchor (0));

        testInt = -1;

        // Pos.Y(win) -1
        pos = Y (new () { Frame = testRect }) - testInt;
        Assert.Equal (testRect.Y - testInt, pos.GetAnchor (0));

        // Pos.Bottom
        testRect = Rectangle.Empty;
        testInt = 0;
        pos = Bottom (new ());
        Assert.Equal (testRect.Bottom, pos.GetAnchor (0));

        pos = Bottom (new () { Frame = testRect });
        Assert.Equal (testRect.Bottom, pos.GetAnchor (0));

        testRect = new (1, 2, 3, 4);
        pos = Bottom (new () { Frame = testRect });
        Assert.Equal (testRect.Bottom, pos.GetAnchor (0));

        // Pos.Bottom(win) + 0
        pos = Bottom (new () { Frame = testRect }) + testInt;
        Assert.Equal (testRect.Bottom, pos.GetAnchor (0));

        testInt = 1;

        // Pos.Bottom(win) +1
        pos = Bottom (new () { Frame = testRect }) + testInt;
        Assert.Equal (testRect.Bottom + testInt, pos.GetAnchor (0));

        testInt = -1;

        // Pos.Bottom(win) -1
        pos = Bottom (new () { Frame = testRect }) - testInt;
        Assert.Equal (testRect.Bottom - testInt, pos.GetAnchor (0));
    }
}
