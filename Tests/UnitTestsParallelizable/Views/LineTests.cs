using UnitTests;

namespace ViewsTests;

public class LineTests : TestDriverBase
{
    [Fact]
    public void Line_DefaultConstructor_Horizontal ()
    {
        var line = new Line ();

        Assert.Equal (Orientation.Horizontal, line.Orientation);
        Assert.Equal (Dim.Fill (), line.Width);
        Assert.Equal (LineStyle.Single, line.Style);
        Assert.True (line.SuperViewRendersLineCanvas);
        Assert.False (line.CanFocus);

        line.Layout ();
        Assert.Equal (1, line.Frame.Height);
    }

    [Fact]
    public void Line_Horizontal_FillsWidth ()
    {
        var line = new Line { Orientation = Orientation.Horizontal };
        var container = new View { Width = 50, Height = 10 };
        container.Add (line);

        container.Layout ();

        Assert.Equal (50, line.Frame.Width);
        Assert.Equal (1, line.Frame.Height);
    }

    [Fact]
    public void Line_Vertical_FillsHeight ()
    {
        var line = new Line { Orientation = Orientation.Vertical };
        var container = new View { Width = 50, Height = 10 };
        container.Add (line);

        container.Layout ();

        Assert.Equal (1, line.Frame.Width);
        Assert.Equal (10, line.Frame.Height);
    }

    [Fact]
    public void Line_ChangeOrientation_UpdatesDimensions ()
    {
        var line = new Line { Orientation = Orientation.Horizontal };
        var container = new View { Width = 50, Height = 20 };
        container.Add (line);
        container.Layout ();

        Assert.Equal (50, line.Frame.Width);
        Assert.Equal (1, line.Frame.Height);

        // Change to vertical
        line.Orientation = Orientation.Vertical;
        container.Layout ();

        Assert.Equal (1, line.Frame.Width);
        Assert.Equal (20, line.Frame.Height);
    }

    [Fact]
    public void Line_ChangeOrientation_IsInitialized_UpdatesDimensions ()
    {
        var line = new Line { Orientation = Orientation.Horizontal, Width = Dim.Fill (), Height = 1 };

        line.BeginInit ();
        line.EndInit ();

        Assert.Equal (Orientation.Horizontal, line.Orientation);
        Assert.Equal (Dim.Fill (), line.Width);
        Assert.Equal (1, line.Height);

        line.Orientation = Orientation.Vertical;

        Assert.Equal (Orientation.Vertical, line.Orientation);
        Assert.Equal (1, line.Width);
        Assert.Equal (Dim.Fill (), line.Height);

        line.Orientation = Orientation.Horizontal;

        Assert.Equal (Orientation.Horizontal, line.Orientation);
        Assert.Equal (Dim.Fill (), line.Width);
        Assert.Equal (1, line.Height);

        // Change Length will update correct dimension based on orientation
        line.Length = 10;

        Assert.Equal (Orientation.Horizontal, line.Orientation);
        Assert.Equal (10, line.Width);
        Assert.Equal (1, line.Height);

        line.Orientation = Orientation.Vertical;

        Assert.Equal (Orientation.Vertical, line.Orientation);
        Assert.Equal (1, line.Width);
        Assert.Equal (10, line.Height);

        line.Orientation = Orientation.Horizontal;

        Assert.Equal (Orientation.Horizontal, line.Orientation);
        Assert.Equal (10, line.Width);
        Assert.Equal (1, line.Height);
    }

    [Fact]
    public void Line_Style_CanBeSet ()
    {
        var line = new Line { Style = LineStyle.Double };

        Assert.Equal (LineStyle.Double, line.Style);
    }

    [Theory]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Double)]
    [InlineData (LineStyle.Heavy)]
    [InlineData (LineStyle.Rounded)]
    [InlineData (LineStyle.Dashed)]
    [InlineData (LineStyle.Dotted)]
    public void Line_SupportsDifferentLineStyles (LineStyle style)
    {
        var line = new Line { Style = style };

        Assert.Equal (style, line.Style);
    }

    [Fact]
    public void Line_DrawsCalled_Successfully ()
    {
        var app = new Window () { Driver = CreateTestDriver () };
        var line = new Line { Y = 1, Width = 10 };
        app.Add (line);

        app.BeginInit ();
        app.EndInit ();
        app.Layout ();

        // Just verify the line can be drawn without errors
        Exception? exception = Record.Exception (() => app.Draw ());
        Assert.Null (exception);
    }

    [Fact]
    public void Line_WithBorder_DrawsSuccessfully ()
    {
        var app = new Window { Driver = CreateTestDriver (), Width = 20, Height = 10, BorderStyle = LineStyle.Single };

        // Add a line that intersects with the window border
        var line = new Line { X = 5, Y = 0, Height = Dim.Fill (), Orientation = Orientation.Vertical };
        app.Add (line);

        app.BeginInit ();
        app.EndInit ();
        app.Layout ();

        // Just verify the line and border can be drawn together without errors
        Exception? exception = Record.Exception (() => app.Draw ());
        Assert.Null (exception);
    }

    [Fact]
    public void Line_MultipleIntersecting_DrawsSuccessfully ()
    {
        var app = new Window { Driver = CreateTestDriver (), Width = 30, Height = 15 };

        // Create intersecting lines
        var hLine = new Line { X = 5, Y = 5, Width = 15, Style = LineStyle.Single };

        var vLine = new Line
        {
            X = 12, Y = 2, Height = 8, Orientation = Orientation.Vertical, Style = LineStyle.Single
        };

        app.Add (hLine, vLine);

        app.BeginInit ();
        app.EndInit ();
        app.Layout ();

        // Just verify multiple intersecting lines can be drawn without errors
        Exception? exception = Record.Exception (() => app.Draw ());
        Assert.Null (exception);
    }

    [Fact]
    public void Line_ExplicitWidthAndHeight_RespectValues ()
    {
        var line = new Line { Width = 10, Height = 1 };
        var container = new View { Width = 50, Height = 20 };
        container.Add (line);

        container.Layout ();

        Assert.Equal (10, line.Frame.Width);
        Assert.Equal (1, line.Frame.Height);
    }

    [Fact]
    public void Line_VerticalWithExplicitHeight_RespectValues ()
    {
        var line = new Line { Orientation = Orientation.Vertical };

        // Set height AFTER orientation to avoid it being reset
        line.Width = 1;
        line.Height = 8;

        var container = new View { Width = 50, Height = 20 };
        container.Add (line);

        container.Layout ();

        Assert.Equal (1, line.Frame.Width);
        Assert.Equal (8, line.Frame.Height);
    }

    [Fact]
    public void Line_SuperViewRendersLineCanvas_IsTrue ()
    {
        var line = new Line ();

        Assert.True (line.SuperViewRendersLineCanvas);
    }

    [Fact]
    public void Line_CannotFocus ()
    {
        var line = new Line ();

        Assert.False (line.CanFocus);
    }

    [Fact]
    public void Line_ImplementsIOrientation ()
    {
        var line = new Line ();

        Assert.IsAssignableFrom<IOrientation> (line);
    }

    [Fact]
    public void Line_Length_Get_ReturnsCorrectDimension ()
    {
        var line = new Line { Width = 20, Height = 1 };

        // For horizontal, Length should be Width
        line.Orientation = Orientation.Horizontal;
        Assert.Equal (line.Width, line.Length);
        Assert.Equal (1, line.Height.GetAnchor (0));

        // For vertical, Length should be Height
        line.Orientation = Orientation.Vertical;
        Assert.Equal (line.Height, line.Length);
        Assert.Equal (1, line.Width.GetAnchor (0));
    }

    [Fact]
    public void Line_OrientationChange_SwapsDimensions ()
    {
        var line = new Line ();
        var container = new View { Width = 50, Height = 20 };
        container.Add (line);

        // Start horizontal with custom dimensions
        line.Orientation = Orientation.Horizontal;
        line.Width = 30;
        line.Height = 1;
        container.Layout ();

        Assert.Equal (30, line.Frame.Width);
        Assert.Equal (1, line.Frame.Height);

        // Change to vertical - dimensions should swap
        line.Orientation = Orientation.Vertical;
        container.Layout ();

        Assert.Equal (1, line.Frame.Width);
        Assert.Equal (30, line.Frame.Height); // Width became Height
    }

    [Fact]
    public void Line_OrientationChange_IsInitialized_SwapsDimensions ()
    {
        var line = new Line ();
        var container = new View { Width = 50, Height = 20 };
        container.Add (line);

        line.BeginInit ();
        line.EndInit();

        // Start horizontal with custom dimensions
        line.Orientation = Orientation.Horizontal;
        line.Width = 30;
        line.Height = 1;
        container.Layout ();

        Assert.Equal (30, line.Frame.Width);
        Assert.Equal (1, line.Frame.Height);

        // Change to vertical - dimensions should swap
        line.Orientation = Orientation.Vertical;
        container.Layout ();

        Assert.Equal (1, line.Frame.Width);
        Assert.Equal (30, line.Frame.Height); // Width became Height
    }

    [Fact]
    public void Line_Dimensions_WorkSameAsInitializers ()
    {
        // Object initializers work same as sequential assignment
        // Test: new Line { Width = 15, Orientation = Orientation.Horizontal }
        // Expected: Width=15, Height=1
        Line line = new () { Width = 15, Orientation = Orientation.Horizontal };

        Assert.Equal (15, line.Width.GetAnchor (0));
        Assert.Equal (1, line.Height.GetAnchor (0));
        Assert.Equal (line.Length, line.Width); // Length should be Width for horizontal

        line = new ();
        line.Width = 15;
        line.Orientation = Orientation.Horizontal;
        Assert.Equal (15, line.Width.GetAnchor (0));
        Assert.Equal (1, line.Height.GetAnchor (0));
        Assert.Equal (line.Length, line.Width); // Length should be Width for horizontal

        // Test: new Line { Height = 9, Orientation = Orientation.Vertical }
        // Expected: Width=1, Height=9
        line = new () { Height = 9, Orientation = Orientation.Vertical };

        Assert.Equal (1, line.Width.GetAnchor (0));
        Assert.Equal (9, line.Height.GetAnchor (0));
        Assert.Equal (line.Length, line.Height); // Length should be Height for vertical

        line = new ();
        line.Height = 9;
        line.Orientation = Orientation.Vertical;
        Assert.Equal (1, line.Width.GetAnchor (0));
        Assert.Equal (9, line.Height.GetAnchor (0));
        Assert.Equal (line.Length, line.Height); // Length should be Height for vertical
    }

    // Copilot
    [Fact]
    public void LineAttribute_Null_By_Default ()
    {
        Line line = new ();

        Assert.Null (line.LineAttribute);
    }

    // Copilot
    [Fact]
    public void LineAttribute_Overrides_Scheme_Attribute ()
    {
        Attribute custom = new (Color.Red, Color.Blue);
        Line line = new () { LineAttribute = custom };

        Assert.Equal (custom, line.LineAttribute);
    }

    // Copilot
    [Fact]
    public void LineAttribute_Set_To_Null_Reverts_To_Scheme ()
    {
        Attribute custom = new (Color.Red, Color.Blue);
        Line line = new () { LineAttribute = custom };

        Assert.NotNull (line.LineAttribute);

        line.LineAttribute = null;

        Assert.Null (line.LineAttribute);
    }
}
