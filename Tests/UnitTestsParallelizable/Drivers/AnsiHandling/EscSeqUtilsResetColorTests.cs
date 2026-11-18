using System.Text;

namespace DriverTests.AnsiHandling;

[Collection ("Driver Tests")]
public class EscSeqUtilsResetColorTests
{
    [Fact]
    public void CSI_AppendResetForegroundColor_Appends_CSI_39m ()
    {
        StringBuilder builder = new ();

        EscSeqUtils.CSI_AppendResetForegroundColor (builder);

        Assert.Equal ("\u001b[39m", builder.ToString ());
    }

    [Fact]
    public void CSI_AppendResetBackgroundColor_Appends_CSI_49m ()
    {
        StringBuilder builder = new ();

        EscSeqUtils.CSI_AppendResetBackgroundColor (builder);

        Assert.Equal ("\u001b[49m", builder.ToString ());
    }

    [Fact]
    public void CSI_AppendResetForegroundColor_Appends_To_Existing_Content ()
    {
        StringBuilder builder = new ("existing");

        EscSeqUtils.CSI_AppendResetForegroundColor (builder);

        Assert.Equal ("existing\u001b[39m", builder.ToString ());
    }

    [Fact]
    public void CSI_AppendResetBackgroundColor_Appends_To_Existing_Content ()
    {
        StringBuilder builder = new ("existing");

        EscSeqUtils.CSI_AppendResetBackgroundColor (builder);

        Assert.Equal ("existing\u001b[49m", builder.ToString ());
    }

    [Fact]
    public void Both_Reset_Methods_Can_Be_Called_Sequentially ()
    {
        StringBuilder builder = new ();

        EscSeqUtils.CSI_AppendResetForegroundColor (builder);
        EscSeqUtils.CSI_AppendResetBackgroundColor (builder);

        Assert.Equal ("\u001b[39m\u001b[49m", builder.ToString ());
    }
}
