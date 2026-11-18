using UnitTests;

namespace DrawingTests;

/// <summary>
/// Pure unit tests for <see cref="Ruler"/> that don't require Application.Driver or View context.
/// These tests focus on properties and behavior that don't depend on rendering.
///
/// Note: Tests that verify rendered output (Draw methods) require Application.Driver and remain in UnitTests as integration tests.
/// </summary>
public class RulerTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var r = new Ruler ();
        Assert.Equal (0, r.Length);
        Assert.Equal (Orientation.Horizontal, r.Orientation);
    }

    [Fact]
    public void Attribute_Set ()
    {
        var newAttribute = new Attribute (Color.Red, Color.Green);

        var r = new Ruler ();
        r.Attribute = newAttribute;
        Assert.Equal (newAttribute, r.Attribute);
    }

    [Fact]
    public void Length_Set ()
    {
        var r = new Ruler ();
        Assert.Equal (0, r.Length);
        r.Length = 42;
        Assert.Equal (42, r.Length);
    }

    [Fact]
    public void Orientation_Set ()
    {
        var r = new Ruler ();
        Assert.Equal (Orientation.Horizontal, r.Orientation);
        r.Orientation = Orientation.Vertical;
        Assert.Equal (Orientation.Vertical, r.Orientation);
    }

    [Fact]
    public void Draw_Default ()
    {
        IDriver driver = CreateTestDriver ();

        var r = new Ruler ();
        r.Draw (driver: driver, location: Point.Empty);
        DriverAssert.AssertDriverContentsWithFrameAre (@"", output, driver);
    }

    [Fact]
    public void Draw_Horizontal ()
    {
        IDriver driver = CreateTestDriver ();

        var len = 15;

        var r = new Ruler ();
        Assert.Equal (Orientation.Horizontal, r.Orientation);

        r.Length = len;
        r.Draw (driver: driver, location: Point.Empty);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
|123456789|1234",
                                                       output,
                                                       driver
                                                      );

        // Postive offset
        r.Draw (driver: driver, location: new (1, 1));

        DriverAssert.AssertDriverContentsAre (
                                              @"
|123456789|1234
 |123456789|1234
",
                                              output,
                                              driver
                                             );

        // Negative offset
        r.Draw (driver: driver, location: new (-1, 3));

        DriverAssert.AssertDriverContentsAre (
                                              @"
|123456789|1234
 |123456789|1234
123456789|1234
",
                                              output,
                                              driver
                                             );
    }

    [Fact]
    public void Draw_Vertical ()
    {
        IDriver driver = CreateTestDriver ();

        var len = 15;

        var r = new Ruler ();
        r.Orientation = Orientation.Vertical;
        r.Length = len;
        r.Draw (driver: driver, location: Point.Empty);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
-
1
2
3
4
5
6
7
8
9
-
1
2
3
4",
                                                       output,
                                                       driver
                                                      );

        r.Draw (driver: driver, location: new (1, 1));

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
- 
1-
21
32
43
54
65
76
87
98
-9
1-
21
32
43
 4",
                                                       output,
                                                       driver
                                                      );

        // Negative offset
        r.Draw (driver: driver, location: new (2, -1));

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
- 1
1-2
213
324
435
546
657
768
879
98-
-91
1-2
213
324
43 
 4 ",
                                                       output,
                                                       driver
                                                      );
    }
}
