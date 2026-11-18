using AppTestHelpers;

namespace IntegrationTests;

public class LinearRangeFluentTests (ITestOutputHelper outputHelper) : TestsAllDrivers
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void LinearSelector_CanCreateAndRender (string d)
    {
        using AppTestHelper c = With.A<Window> (30, 10, d, _out)
                                        .Add (
                                              new LinearSelector<int> ([0, 10, 20, 30, 40, 50])
                                              {
                                                  X = 2,
                                                  Y = 2
                                              })
                                        .Focus<LinearSelector<int>> ()
                                        .WaitIteration ()
                                        .ScreenShot ("LinearSelector initial render", _out)
                                        .Stop ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void LinearSelector_CanNavigateWithArrowKeys (string d)
    {
        using AppTestHelper c = With.A<Window> (30, 10, d, _out)
                                        .Add (
                                              new LinearSelector<int> ([0, 10, 20, 30])
                                              {
                                                  X = 2,
                                                  Y = 2,
                                                  AllowEmpty = false
                                              })
                                        .Focus<LinearSelector<int>> ()
                                        .WaitIteration ()
                                        .ScreenShot ("Initial state", _out)
                                        .KeyDown (Key.CursorRight)
                                        .WaitIteration ()
                                        .ScreenShot ("After right arrow", _out)
                                        .KeyDown (Key.CursorRight)
                                        .WaitIteration ()
                                        .ScreenShot ("After second right arrow", _out)
                                        .Stop ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void LinearRange_RangeKindChange_TriggersValueChange (string d)
    {
        LinearRange<int> linearRange = new ([0, 10, 20, 30])
        {
            X = 2,
            Y = 2,
            RangeKind = LinearRangeSpanKind.Closed
        };
        linearRange.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 0, 30, 0, 3);

        var changedRaised = false;

        linearRange.ValueChanged += (_, args) =>
                                    {
                                        changedRaised = true;
                                        Assert.Equal (LinearRangeSpanKind.LeftBounded, args.NewValue.Kind);
                                    };

        // Migrate from Closed -> LeftBounded; the End is preserved.
        linearRange.RangeKind = LinearRangeSpanKind.LeftBounded;

        using AppTestHelper c = With.A<Window> (30, 10, d, _out)
                                        .Add (linearRange)
                                        .Focus<LinearRange<int>> ()
                                        .WaitIteration ()
                                        .ScreenShot ("After RangeKind change to LeftBounded", _out)
                                        .Stop ();

        Assert.True (changedRaised);
        Assert.Equal (LinearRangeSpanKind.LeftBounded, linearRange.RangeKind);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void LinearRange_Closed_CanSelectRange (string d)
    {
        using AppTestHelper c = With.A<Window> (30, 10, d, _out)
                                        .Add (
                                              new LinearRange<int> ([0, 10, 20, 30, 40])
                                              {
                                                  X = 2,
                                                  Y = 2,
                                                  RangeKind = LinearRangeSpanKind.Closed,
                                                  AllowEmpty = false
                                              })
                                        .Focus<LinearRange<int>> ()
                                        .WaitIteration ()
                                        .ScreenShot ("Range type initial", _out)
                                        .KeyDown (Key.Space)
                                        .WaitIteration ()
                                        .ScreenShot ("After first selection", _out)
                                        .KeyDown (Key.CursorRight.WithCtrl)
                                        .WaitIteration ()
                                        .KeyDown (Key.CursorRight.WithCtrl)
                                        .WaitIteration ()
                                        .ScreenShot ("After extending range", _out)
                                        .Stop ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void LinearSelector_VerticalOrientation_Renders (string d)
    {
        using AppTestHelper c = With.A<Window> (10, 15, d, _out)
                                        .Add (
                                              new LinearSelector<int> ([0, 10, 20, 30])
                                              {
                                                  X = 2,
                                                  Y = 2,
                                                  Orientation = Orientation.Vertical
                                              })
                                        .Focus<LinearSelector<int>> ()
                                        .WaitIteration ()
                                        .ScreenShot ("Vertical orientation", _out)
                                        .KeyDown (Key.CursorDown)
                                        .WaitIteration ()
                                        .ScreenShot ("After down arrow", _out)
                                        .Stop ();
    }
}
