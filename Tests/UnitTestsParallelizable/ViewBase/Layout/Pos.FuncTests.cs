#nullable disable

namespace ViewBaseTests.Layout;

public class PosFuncTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void PosFunc_Equal ()
    {
        Func<View, int> f1 = _ => 0;
        Func<View, int> f2 = _ => 0;

        Pos pos1 = Pos.Func (f1);
        Pos pos2 = Pos.Func (f1);
        Assert.Equal (pos1, pos2);

        f2 = _ => 1;
        pos2 = Pos.Func (f2);
        Assert.NotEqual (pos1, pos2);
    }

    [Fact]
    public void PosFunc_SetsValue ()
    {
        var text = "Test";
        Pos pos = Pos.Func (_ => text.Length);
        Assert.Equal ("PosFunc(4)", pos.ToString ());

        text = "New Test";
        Assert.Equal ("PosFunc(8)", pos.ToString ());

        text = "";
        Assert.Equal ("PosFunc(0)", pos.ToString ());
    }

    [Fact]
    public void PosFunc_Calculate_ReturnsCorrectValue ()
    {
        var pos = new PosFunc (_ => 10);
        int result = pos.Calculate (0, 100, null, Dimension.None);
        Assert.Equal (10, result);
    }

    [Fact]
    public void PosFunc_View_Equal ()
    {
        Func<View, int> f1 = v => v.Frame.X;
        Func<View, int> f2 = v => v.Frame.X;
        View view1 = new ();
        View view2 = new ();

        Pos pos1 = Pos.Func (f1, view1);
        Pos pos2 = Pos.Func (f1, view1);
        Assert.Equal (pos1, pos2);

        f2 = _ => 1;
        pos2 = Pos.Func (f2, view2);
        Assert.NotEqual (pos1, pos2);

        view2.X = 1;
        Assert.NotEqual (pos1, pos2);
        Assert.Equal (1, f2 (view2));
    }

    [Fact]
    public void PosFunc_View_SetsValue ()
    {
        View view = new () { Text = "Test" };
        Pos pos = Pos.Func (v => v.Text.Length, view);
        Assert.Equal ("PosFunc(4)", pos.ToString ());

        view.Text = "New Test";
        Assert.Equal ("PosFunc(8)", pos.ToString ());

        view.Text = "";
        Assert.Equal ("PosFunc(0)", pos.ToString ());
    }

    [Fact]
    public void PosFunc_View_Calculate_ReturnsCorrectValue ()
    {
        View view = new () { X = 10 };
        var pos = new PosFunc (v => v.Frame.X, view);
        int result = pos.Calculate (0, 100, view, Dimension.None);
        Assert.Equal (10, result);
    }
}
