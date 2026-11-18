#nullable disable
using static Terminal.Gui.ViewBase.Dim;

namespace ViewBaseTests.Layout;

public class DimFuncTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void DimFunc_Equal ()
    {
        Func<View, int> f1 = _ => 0;
        Func<View, int> f2 = _ => 0;

        Dim dim1 = Func (f1);
        Dim dim2 = Func (f1);
        Assert.Equal (dim1, dim2);

        dim2 = Func (f2);
        Assert.NotEqual (dim1, dim2);

        f2 = _ => 1;
        dim2 = Func (f2);
        Assert.NotEqual (dim1, dim2);
    }

    [Fact]
    public void DimFunc_SetsValue ()
    {
        var text = "Test";
        Dim dim = Func (_ => text.Length);
        Assert.Equal ("DimFunc(4)", dim.ToString ());

        text = "New Test";
        Assert.Equal ("DimFunc(8)", dim.ToString ());

        text = "";
        Assert.Equal ("DimFunc(0)", dim.ToString ());
    }

    [Fact]
    public void DimFunc_Calculate_ReturnsCorrectValue ()
    {
        var dim = new DimFunc (_ => 10);
        int result = dim.Calculate (0, 100, null, Dimension.None);
        Assert.Equal (10, result);
    }

    [Fact]
    public void DimFunc_View_Equal ()
    {
        Func<View, int> f1 = v => v.Frame.Width;
        Func<View, int> f2 = v => v.Frame.Width;
        View view1 = new ();
        View view2 = new ();

        Dim dim1 = Func (f1, view1);
        Dim dim2 = Func (f1, view1);
        Assert.Equal (dim1, dim2);

        dim2 = Func (f2, view2);
        Assert.NotEqual (dim1, dim2);

        view2.Width = 1;
        Assert.NotEqual (dim1, dim2);
        Assert.Equal (1, f2 (view2));
    }

    [Fact]
    public void DimFunc_View_SetsValue ()
    {
        View view = new () { Text = "Test" };
        Dim dim = Func (v => v.Text.Length, view);
        Assert.Equal ("DimFunc(4)", dim.ToString ());

        view.Text = "New Test";
        Assert.Equal ("DimFunc(8)", dim.ToString ());

        view.Text = "";
        Assert.Equal ("DimFunc(0)", dim.ToString ());
    }

    [Fact]
    public void DimFunc_View_Calculate_ReturnsCorrectValue ()
    {
        View view = new () { Width = 10 };
        var dim = new DimFunc (v => v.Frame.Width, view);
        int result = dim.Calculate (0, 100, view, Dimension.None);
        Assert.Equal (10, result);
    }
}
