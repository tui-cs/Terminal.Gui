#nullable disable

namespace ViewBaseTests.Layout;

public class PosAbsoluteTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void PosAbsolute_Equal ()
    {
        Pos pos1 = Pos.Absolute (1);
        Pos pos2 = Pos.Absolute (1);
        Assert.Equal (pos1, pos2);

        pos2 = Pos.Absolute (2);
        Assert.NotEqual (pos1, pos2);
    }

    [Fact]
    public void PosAbsolute_Calculate_ReturnsExpectedValue ()
    {
        var posAbsolute = new PosAbsolute (5);
        int result = posAbsolute.Calculate (10, new DimAbsolute (2), null, Dimension.None);
        Assert.Equal (5, result);
    }

    [Theory]
    [InlineData (-1)]
    [InlineData (0)]
    [InlineData (1)]
    public void PosAbsolute_SetsPosition (int position)
    {
        var pos = Pos.Absolute (position) as PosAbsolute;
        Assert.Equal (position, pos!.Position);
    }
}
