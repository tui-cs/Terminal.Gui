// Copilot - Claude Sonnet 4.6

namespace DrawingTests;

public class KittyGraphicsSupportResultTests
{
    [Fact]
    public void DefaultValues_AreCorrect ()
    {
        KittyGraphicsSupportResult result = new ();

        Assert.False (result.IsSupported);
        Assert.Equal (new Size (10, 20), result.Resolution);
    }

    [Fact]
    public void IsSupported_CanBeSetToTrue ()
    {
        KittyGraphicsSupportResult result = new () { IsSupported = true };

        Assert.True (result.IsSupported);
    }

    [Fact]
    public void Resolution_CanBeCustomized ()
    {
        KittyGraphicsSupportResult result = new () { Resolution = new Size (8, 16) };

        Assert.Equal (new Size (8, 16), result.Resolution);
    }
}
