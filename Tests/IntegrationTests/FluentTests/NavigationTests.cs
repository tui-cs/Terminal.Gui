#nullable enable
using AppTestHelpers;

namespace IntegrationTests;

public class NavigationTests (ITestOutputHelper outputHelper) : TestsAllDrivers
{
    private readonly TextWriter? _out = new TestOutputWriter (outputHelper);

    [Fact]
    public void Runnable_TabGroup_Forward_Backward ()
    {
        var v1 = new View { Id = "v1", CanFocus = true };
        var v2 = new View { Id = "v2", CanFocus = true };
        var v3 = new View { Id = "v3", CanFocus = true };
        var v4 = new View { Id = "v4", CanFocus = true };
        var v5 = new View { Id = "v5", CanFocus = true };
        var v6 = new View { Id = "v6", CanFocus = true };

        using AppTestHelper c = With.A<Window> (50, 20, "ansi", _out)
                                        .Then (app =>
                                               {
                                                   var w1 = new Window { Id = "w1" };
                                                   w1.Add (v1, v2);
                                                   var w2 = new Window { Id = "w2" };
                                                   w2.Add (v3, v4);
                                                   var w3 = new Window { Id = "w3" };
                                                   w3.Add (v5, v6);
                                                   app?.TopRunnableView!.Add (w1, w2, w3);
                                               })
                                        .WaitUntil (() => v5.HasFocus)
                                        .KeyDown (Key.F6)
                                        .WaitUntil (() => v1.HasFocus)
                                        .KeyDown (Key.F6)
                                        .WaitUntil (() => v3.HasFocus)
                                        .KeyDown (Key.F6.WithShift)
                                        .WaitUntil (() => v1.HasFocus)
                                        .KeyDown (Key.F6.WithShift)
                                        .WaitUntil (() => v5.HasFocus)
                                        .KeyDown (Key.F6.WithShift)
                                        .WaitUntil (() => v3.HasFocus)
                                        .KeyDown (Key.F6)
                                        .WaitUntil (() => v5.HasFocus)
                                        .KeyDown (Key.F6)
                                        .WaitUntil (() => v1.HasFocus)
                                        .KeyDown (Key.F6)
                                        .WaitUntil (() => v3.HasFocus)
                                        .KeyDown (Key.F6.WithShift)
                                        .WaitUntil (() => v1.HasFocus)
                                        .KeyDown (Key.F6.WithShift)
                                        .WaitUntil (() => v5.HasFocus)
                                        .KeyDown (Key.F6.WithShift)
                                        .WaitUntil (() => v3.HasFocus)
                                        .KeyDown (Key.Tab)
                                        .WaitUntil (() => v4.HasFocus)
                                        .KeyDown (Key.F6)
                                        .WaitUntil (() => v5.HasFocus)
                                        .KeyDown (Key.F6)
                                        .WaitUntil (() => v1.HasFocus)
                                        .KeyDown (Key.F6.WithShift)
                                        .WaitUntil (() => v5.HasFocus)
                                        .KeyDown (Key.Tab)
                                        .WaitUntil (() => v6.HasFocus)
                                        .KeyDown (Key.F6.WithShift)
                                        .WaitUntil (() => v4.HasFocus)
                                        .KeyDown (Key.F6)
                                        .WaitUntil (() => v6.HasFocus);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        Assert.False (v4.HasFocus);
        Assert.False (v5.HasFocus);
    }
}
