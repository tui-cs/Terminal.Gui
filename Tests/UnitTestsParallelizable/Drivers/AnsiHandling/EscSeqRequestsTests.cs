using UnitTests;

namespace DriverTests.AnsiHandling;

[Collection ("Driver Tests")]
public class EscSeqRequestsTests : TestDriverBase
{
    [Fact]
    public void Add_Tests ()
    {
        EscSeqRequests.Add ("t");
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Add ("t", 2);
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (3, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (3, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Clear ();
        EscSeqRequests.Add ("t", 2);
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (2, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (2, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Add ("t", 3);
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (5, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (5, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Clear ();
    }

    [Fact]
    public void Constructor_Defaults ()
    {
        Assert.NotNull (EscSeqRequests.Statuses);
        Assert.Empty (EscSeqRequests.Statuses);
    }

    [Fact]
    public void Remove_Tests ()
    {
        EscSeqRequests.Add ("t");
        EscSeqRequests.Remove ("t");
        Assert.Empty (EscSeqRequests.Statuses);

        EscSeqRequests.Add ("t", 2);
        EscSeqRequests.Remove ("t");
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (2, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Remove ("t");
        Assert.Empty (EscSeqRequests.Statuses);

        EscSeqRequests.Clear ();
    }

    [Fact]
    public void HasResponse_Tests ()
    {
        Assert.False (EscSeqRequests.HasResponse ("t"));

        EscSeqRequests.Add ("t");
        Assert.False (EscSeqRequests.HasResponse ("r"));
        Assert.True (EscSeqRequests.HasResponse ("t"));
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Remove ("t");
        Assert.Empty (EscSeqRequests.Statuses);
    }

    [Theory]
    [InlineData (null)]
    [InlineData ("")]
    public void Add_Null_Or_Empty_Terminator_Throws (string? terminator)
    {
        if (terminator is null)
        {
            Assert.Throws<ArgumentNullException> (() => EscSeqRequests.Add (terminator));
        }
        else
        {
            Assert.Throws<ArgumentException> (() => EscSeqRequests.Add (terminator));
        }
    }

    [Theory]
    [InlineData ("")]
    public void HasResponse_Null_Or_Empty_Terminator_Does_Not_Throws (string terminator)
    {
        EscSeqRequests.Add ("t");

        Assert.False (EscSeqRequests.HasResponse (terminator));

        EscSeqRequests.Clear ();
    }

    [Theory]
    [InlineData ("")]
    public void Remove_Null_Or_Empty_Terminator_Throws (string terminator)
    {
        EscSeqRequests.Add ("t");

        Assert.Throws<ArgumentException> (() => EscSeqRequests.Remove (terminator));

        EscSeqRequests.Clear ();
    }
}
