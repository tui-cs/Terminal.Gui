using System.Text;
using UnitTests;

namespace ViewsTests;

public class LinearRangeOptionTests : TestDriverBase
{
    [Fact]
    public void LinearRange_Option_Default_Constructor ()
    {
        LinearRangeOption<int> o = new ();
        Assert.Null (o.Legend);
        Assert.Equal (default (Rune), o.LegendAbbr);
        Assert.Equal (0, o.Data);
    }

    [Fact]
    public void LinearRange_Option_Values_Constructor ()
    {
        LinearRangeOption<int> o = new ("1 thousand", new Rune ('y'), 1000);
        Assert.Equal ("1 thousand", o.Legend);
        Assert.Equal (new Rune ('y'), o.LegendAbbr);
        Assert.Equal (1000, o.Data);
    }

    [Fact]
    public void LinearRangeOption_ToString_WhenEmpty ()
    {
        LinearRangeOption<object> sliderOption = new ();
        Assert.Equal ("{Legend=, LegendAbbr=\0, Data=}", sliderOption.ToString ());
    }

    [Fact]
    public void LinearRangeOption_ToString_WhenPopulated_WithInt ()
    {
        LinearRangeOption<int> sliderOption = new () { Legend = "Lord flibble", LegendAbbr = new Rune ('l'), Data = 1 };

        Assert.Equal ("{Legend=Lord flibble, LegendAbbr=l, Data=1}", sliderOption.ToString ());
    }

    [Fact]
    public void LinearRangeOption_ToString_WhenPopulated_WithSizeF ()
    {
        LinearRangeOption<SizeF> sliderOption = new () { Legend = "Lord flibble", LegendAbbr = new Rune ('l'), Data = new SizeF (32, 11) };

        Assert.Equal ("{Legend=Lord flibble, LegendAbbr=l, Data={Width=32, Height=11}}", sliderOption.ToString ());
    }

    [Fact]
    public void OnChanged_Should_Raise_ChangedEvent ()
    {
        LinearRangeOption<int> sliderOption = new ();
        var eventRaised = false;
        sliderOption.Changed += (sender, args) => eventRaised = true;

        sliderOption.OnChanged (true);

        Assert.True (eventRaised);
    }

    [Fact]
    public void OnSet_Should_Raise_SetEvent ()
    {
        LinearRangeOption<int> sliderOption = new ();
        var eventRaised = false;
        sliderOption.Set += (sender, args) => eventRaised = true;

        sliderOption.OnSet ();

        Assert.True (eventRaised);
    }

    [Fact]
    public void OnUnSet_Should_Raise_UnSetEvent ()
    {
        LinearRangeOption<int> sliderOption = new ();
        var eventRaised = false;
        sliderOption.UnSet += (sender, args) => eventRaised = true;

        sliderOption.OnUnSet ();

        Assert.True (eventRaised);
    }
}

public class LinearRangeEventArgsTests : TestDriverBase
{
    [Fact]
    public void Constructor_Sets_Cancel_Default_To_False ()
    {
        Dictionary<int, LinearRangeOption<int>> options = new ();
        var focused = 42;

        LinearRangeEventArgs<int> sliderEventArgs = new (options, focused);

        Assert.False (sliderEventArgs.Cancel);
    }

    [Fact]
    public void Constructor_Sets_Focused ()
    {
        Dictionary<int, LinearRangeOption<int>> options = new ();
        var focused = 42;

        LinearRangeEventArgs<int> sliderEventArgs = new (options, focused);

        Assert.Equal (focused, sliderEventArgs.Focused);
    }

    [Fact]
    public void Constructor_Sets_Options ()
    {
        Dictionary<int, LinearRangeOption<int>> options = new ();

        LinearRangeEventArgs<int> sliderEventArgs = new (options);

        Assert.Equal (options, sliderEventArgs.Options);
    }
}
