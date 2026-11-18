#nullable disable
namespace ViewBaseTests.Layout;

public class PosTests
{
    [Fact]
    public void PosCombine_Calculate_ReturnsExpectedValue ()
    {
        var posCombine = new PosCombine (AddOrSubtract.Add, new PosAbsolute (5), new PosAbsolute (3));
        int result = posCombine.Calculate (10, new DimAbsolute (2), null, Dimension.None);
        Assert.Equal (8, result);
    }

    [Fact]
    public void PosFactor_Calculate_ReturnsExpectedValue ()
    {
        var posFactor = new PosPercent (50);
        int result = posFactor.Calculate (10, new DimAbsolute (2), null, Dimension.None);
        Assert.Equal (5, result);
    }

    [Fact]
    public void PosFunc_Calculate_ReturnsExpectedValue ()
    {
        var posFunc = new PosFunc (_ => 5);
        int result = posFunc.Calculate (10, new DimAbsolute (2), null, Dimension.None);
        Assert.Equal (5, result);
    }

    [Fact]
    public void PosView_Calculate_ReturnsExpectedValue ()
    {
        var posView = new PosView (new() { Frame = new (5, 5, 10, 10) }, 0);
        int result = posView.Calculate (10, new DimAbsolute (2), null, Dimension.None);
        Assert.Equal (5, result);
    }

    [Fact]
    public void PosCombine_DoesNotReturn ()
    {
        var v = new View { Id = "V" };

        Pos pos = Pos.Left (v);

        Assert.Equal (
                      $"View(Side=Left,Target=View(V){v.Frame})",
                      pos.ToString ()
                     );

        pos = Pos.X (v);

        Assert.Equal (
                      $"View(Side=Left,Target=View(V){v.Frame})",
                      pos.ToString ()
                     );

        pos = Pos.Top (v);

        Assert.Equal (
                      $"View(Side=Top,Target=View(V){v.Frame})",
                      pos.ToString ()
                     );

        pos = Pos.Y (v);

        Assert.Equal (
                      $"View(Side=Top,Target=View(V){v.Frame})",
                      pos.ToString ()
                     );

        pos = Pos.Right (v);

        Assert.Equal (
                      $"View(Side=Right,Target=View(V){v.Frame})",
                      pos.ToString ()
                     );

        pos = Pos.Bottom (v);

        Assert.Equal (
                      $"View(Side=Bottom,Target=View(V){v.Frame})",
                      pos.ToString ()
                     );
    }

    [Fact]
    public void PosFunction_SetsValue ()
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
    public void PosPercent_Equal ()
    {
        var n1 = 0;
        var n2 = 0;
        Pos pos1 = Pos.Percent (n1);
        Pos pos2 = Pos.Percent (n2);
        Assert.Equal (pos1, pos2);

        n1 = n2 = 1;
        pos1 = Pos.Percent (n1);
        pos2 = Pos.Percent (n2);
        Assert.Equal (pos1, pos2);

        n1 = n2 = 50;
        pos1 = Pos.Percent (n1);
        pos2 = Pos.Percent (n2);
        Assert.Equal (pos1, pos2);

        n1 = n2 = 100;
        pos1 = Pos.Percent (n1);
        pos2 = Pos.Percent (n2);
        Assert.Equal (pos1, pos2);

        n1 = 0;
        n2 = 1;
        pos1 = Pos.Percent (n1);
        pos2 = Pos.Percent (n2);
        Assert.NotEqual (pos1, pos2);

        n1 = 50;
        n2 = 150;
        pos1 = Pos.Percent (n1);
        pos2 = Pos.Percent (n2);
        Assert.NotEqual (pos1, pos2);
    }
}
