#nullable enable
namespace ViewsTests;

/// <summary>
///     Parallelizable tests for <see cref="SpinnerStyle"/> and its concrete implementations.
///     Tests the abstract properties and behavior of all spinner style variants.
/// </summary>
public class SpinnerStyleTests
{
    #region Abstract Properties Tests

    [Fact]
    public void Custom_HasExpectedDefaults ()
    {
        SpinnerStyle style = new SpinnerStyle.Custom ();

        Assert.False (style.HasSpecialCharacters);
        Assert.Empty (style.Sequence);
        Assert.False (style.SpinBounce);
        Assert.Equal (80, style.SpinDelay);
    }

    [Fact]
    public void Dots_HasExpectedProperties ()
    {
        SpinnerStyle style = new SpinnerStyle.Dots ();

        Assert.False (style.HasSpecialCharacters);
        Assert.Equal (10, style.Sequence.Length);
        Assert.False (style.SpinBounce);
        Assert.Equal (80, style.SpinDelay);
        Assert.Equal ("⠋", style.Sequence [0]);
    }

    [Fact]
    public void Line_HasExpectedProperties ()
    {
        SpinnerStyle style = new SpinnerStyle.Line ();

        Assert.False (style.HasSpecialCharacters);
        Assert.Equal (4, style.Sequence.Length);
        Assert.False (style.SpinBounce);
        Assert.Equal (130, style.SpinDelay);
        Assert.Equal (["-", @"\", "|", "/"], style.Sequence);
    }

    #endregion

    #region SpinBounce Tests

    [Theory]
    [InlineData (typeof (SpinnerStyle.Dots4), true)]
    [InlineData (typeof (SpinnerStyle.Dots6), true)]
    [InlineData (typeof (SpinnerStyle.Dots7), true)]
    [InlineData (typeof (SpinnerStyle.GrowVertical), true)]
    [InlineData (typeof (SpinnerStyle.GrowHorizontal), true)]
    [InlineData (typeof (SpinnerStyle.Balloon2), true)]
    [InlineData (typeof (SpinnerStyle.Bounce), true)]
    [InlineData (typeof (SpinnerStyle.BouncingBar), true)]
    [InlineData (typeof (SpinnerStyle.BouncingBall), true)]
    [InlineData (typeof (SpinnerStyle.Pong), true)]
    [InlineData (typeof (SpinnerStyle.SoccerHeader), true)]
    [InlineData (typeof (SpinnerStyle.Speaker), true)]
    [InlineData (typeof (SpinnerStyle.Dots), false)]
    [InlineData (typeof (SpinnerStyle.Line), false)]
    [InlineData (typeof (SpinnerStyle.SimpleDots), false)]
    public void SpinBounce_ReturnsExpectedValue (Type styleType, bool expectedBounce)
    {
        SpinnerStyle? style = Activator.CreateInstance (styleType) as SpinnerStyle;

        Assert.NotNull (style);
        Assert.Equal (expectedBounce, style.SpinBounce);
    }

    #endregion

    #region HasSpecialCharacters Tests

    [Theory]
    [InlineData (typeof (SpinnerStyle.Arrow2), true)]
    [InlineData (typeof (SpinnerStyle.Smiley), true)]
    [InlineData (typeof (SpinnerStyle.Monkey), true)]
    [InlineData (typeof (SpinnerStyle.Hearts), true)]
    [InlineData (typeof (SpinnerStyle.Clock), true)]
    [InlineData (typeof (SpinnerStyle.Earth), true)]
    [InlineData (typeof (SpinnerStyle.Moon), true)]
    [InlineData (typeof (SpinnerStyle.Runner), true)]
    [InlineData (typeof (SpinnerStyle.Weather), true)]
    [InlineData (typeof (SpinnerStyle.Christmas), true)]
    [InlineData (typeof (SpinnerStyle.Grenade), true)]
    [InlineData (typeof (SpinnerStyle.FingerDance), true)]
    [InlineData (typeof (SpinnerStyle.FistBump), true)]
    [InlineData (typeof (SpinnerStyle.SoccerHeader), true)]
    [InlineData (typeof (SpinnerStyle.MindBlown), true)]
    [InlineData (typeof (SpinnerStyle.Speaker), true)]
    [InlineData (typeof (SpinnerStyle.OrangePulse), true)]
    [InlineData (typeof (SpinnerStyle.BluePulse), true)]
    [InlineData (typeof (SpinnerStyle.OrangeBluePulse), true)]
    [InlineData (typeof (SpinnerStyle.TimeTravelClock), true)]
    [InlineData (typeof (SpinnerStyle.Dots), false)]
    [InlineData (typeof (SpinnerStyle.Line), false)]
    [InlineData (typeof (SpinnerStyle.SimpleDots), false)]
    [InlineData (typeof (SpinnerStyle.Star), false)]
    [InlineData (typeof (SpinnerStyle.Arc), false)]
    public void HasSpecialCharacters_ReturnsExpectedValue (Type styleType, bool expectedHasSpecial)
    {
        SpinnerStyle? style = Activator.CreateInstance (styleType) as SpinnerStyle;

        Assert.NotNull (style);
        Assert.Equal (expectedHasSpecial, style.HasSpecialCharacters);
    }

    #endregion

    #region Sequence Tests

    [Fact]
    public void Sequence_AllStyles_ReturnsNonEmptyArray ()
    {
        Type [] allStyles = typeof (SpinnerStyle)
            .GetNestedTypes ()
            .Where (t => !t.IsAbstract && t.IsSubclassOf (typeof (SpinnerStyle)))
            .ToArray ();

        foreach (Type styleType in allStyles)
        {
            if (styleType == typeof (SpinnerStyle.Custom))
            {
                continue; // Custom has empty sequence by design
            }

            SpinnerStyle? style = Activator.CreateInstance (styleType) as SpinnerStyle;
            Assert.NotNull (style);
            Assert.NotEmpty (style.Sequence);
        }
    }

    [Fact]
    public void Sequence_AllStyles_ContainsOnlyNonNullStrings ()
    {
        Type [] allStyles = typeof (SpinnerStyle)
            .GetNestedTypes ()
            .Where (t => !t.IsAbstract && t.IsSubclassOf (typeof (SpinnerStyle)))
            .ToArray ();

        foreach (Type styleType in allStyles)
        {
            SpinnerStyle? style = Activator.CreateInstance (styleType) as SpinnerStyle;
            Assert.NotNull (style);
            Assert.All (style.Sequence, frame => Assert.NotNull (frame));
        }
    }

    [Theory]
    [InlineData (typeof (SpinnerStyle.Dots), 10)]
    [InlineData (typeof (SpinnerStyle.Dots2), 8)]
    [InlineData (typeof (SpinnerStyle.Line), 4)]
    [InlineData (typeof (SpinnerStyle.SimpleDots), 4)]
    [InlineData (typeof (SpinnerStyle.Star), 6)]
    [InlineData (typeof (SpinnerStyle.Toggle), 2)]
    [InlineData (typeof (SpinnerStyle.Arrow), 8)]
    [InlineData (typeof (SpinnerStyle.Smiley), 2)]
    [InlineData (typeof (SpinnerStyle.Hearts), 5)]
    [InlineData (typeof (SpinnerStyle.Clock), 12)]
    [InlineData (typeof (SpinnerStyle.Earth), 3)]
    public void Sequence_SpecificStyles_HasExpectedLength (Type styleType, int expectedLength)
    {
        SpinnerStyle? style = Activator.CreateInstance (styleType) as SpinnerStyle;

        Assert.NotNull (style);
        Assert.Equal (expectedLength, style.Sequence.Length);
    }

    [Fact]
    public void Sequence_Dots8Bit_Has256Frames ()
    {
        SpinnerStyle style = new SpinnerStyle.Dots8Bit ();

        Assert.Equal (256, style.Sequence.Length);
    }

    [Fact]
    public void Sequence_Material_HasProgressBarFrames ()
    {
        SpinnerStyle style = new SpinnerStyle.Material ();

        Assert.NotEmpty (style.Sequence);

        // Material style uses both filled (█) and empty (▁) block characters
        // to create a progress bar animation
        Assert.All (style.Sequence, frame =>
                                        Assert.True (
                                                     frame.Contains ("█") || frame.Contains ("▁"),
                                                     $"Frame should contain either filled (█) or empty (▁) blocks, but was: {frame}"
                                                    )
                   );

        // Verify that at least some frames contain the filled block
        Assert.Contains (style.Sequence, frame => frame.Contains ("█"));

        // Verify that the sequence shows progression (some frames are all empty at the end)
        Assert.Contains (style.Sequence, frame => frame == "▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁");
    }

    #endregion

    #region SpinDelay Tests

    [Theory]
    [InlineData (typeof (SpinnerStyle.Custom), 80)]
    [InlineData (typeof (SpinnerStyle.Dots), 80)]
    [InlineData (typeof (SpinnerStyle.Line), 130)]
    [InlineData (typeof (SpinnerStyle.SimpleDots), 400)]
    [InlineData (typeof (SpinnerStyle.Star), 70)]
    [InlineData (typeof (SpinnerStyle.GrowVertical), 120)]
    [InlineData (typeof (SpinnerStyle.Balloon), 140)]
    [InlineData (typeof (SpinnerStyle.Triangle), 50)]
    [InlineData (typeof (SpinnerStyle.Arc), 100)]
    [InlineData (typeof (SpinnerStyle.Material), 17)]
    public void SpinDelay_SpecificStyles_HasExpectedValue (Type styleType, int expectedDelay)
    {
        SpinnerStyle? style = Activator.CreateInstance (styleType) as SpinnerStyle;

        Assert.NotNull (style);
        Assert.Equal (expectedDelay, style.SpinDelay);
    }

    [Fact]
    public void SpinDelay_AllStyles_IsPositive ()
    {
        Type [] allStyles = typeof (SpinnerStyle)
            .GetNestedTypes ()
            .Where (t => !t.IsAbstract && t.IsSubclassOf (typeof (SpinnerStyle)))
            .ToArray ();

        foreach (Type styleType in allStyles)
        {
            SpinnerStyle? style = Activator.CreateInstance (styleType) as SpinnerStyle;
            Assert.NotNull (style);
            Assert.True (style.SpinDelay > 0, $"{styleType.Name} should have positive SpinDelay");
        }
    }

    #endregion

    #region Specific Style Behavior Tests

    [Fact]
    public void SimpleDots_SequenceStartsWithDot ()
    {
        SpinnerStyle style = new SpinnerStyle.SimpleDots ();

        Assert.StartsWith (".", style.Sequence [0]);
    }

    [Fact]
    public void SimpleDotsScrolling_SequenceShowsProgression ()
    {
        SpinnerStyle style = new SpinnerStyle.SimpleDotsScrolling ();

        Assert.Equal ([".  ", ".. ", "...", " ..", "  .", "   "], style.Sequence);
    }

    [Fact]
    public void Arrow_SequenceContainsAllDirections ()
    {
        SpinnerStyle style = new SpinnerStyle.Arrow ();

        Assert.Contains ("←", style.Sequence);
        Assert.Contains ("↑", style.Sequence);
        Assert.Contains ("→", style.Sequence);
        Assert.Contains ("↓", style.Sequence);
    }

    [Fact]
    public void BouncingBar_SequenceContainsBrackets ()
    {
        SpinnerStyle style = new SpinnerStyle.BouncingBar ();

        Assert.All (style.Sequence, frame => Assert.Contains ("[", frame));
        Assert.All (style.Sequence, frame => Assert.Contains ("]", frame));
    }

    [Fact]
    public void BouncingBall_SequenceContainsParentheses ()
    {
        SpinnerStyle style = new SpinnerStyle.BouncingBall ();

        Assert.All (style.Sequence, frame => Assert.Contains ("(", frame));
        Assert.All (style.Sequence, frame => Assert.Contains (")", frame));
        Assert.All (style.Sequence, frame => Assert.Contains ("●", frame));
    }

    [Fact]
    public void Pong_SequenceContainsVerticalBars ()
    {
        SpinnerStyle style = new SpinnerStyle.Pong ();

        Assert.All (style.Sequence, frame => Assert.StartsWith ("▐", frame));
        Assert.All (style.Sequence, frame => Assert.EndsWith ("▌", frame));
    }

    [Fact]
    public void Clock_SequenceContains12Frames ()
    {
        SpinnerStyle style = new SpinnerStyle.Clock ();

        Assert.Equal (12, style.Sequence.Length);

        // Verify it contains the 12 hour positions (on-the-hour clock faces)
        Assert.Contains ("🕛 ", style.Sequence); // 12 o'clock
        Assert.Contains ("🕐 ", style.Sequence); // 1 o'clock
        Assert.Contains ("🕑 ", style.Sequence); // 2 o'clock
        Assert.Contains ("🕒 ", style.Sequence); // 3 o'clock
        Assert.Contains ("🕓 ", style.Sequence); // 4 o'clock
        Assert.Contains ("🕔 ", style.Sequence); // 5 o'clock
        Assert.Contains ("🕕 ", style.Sequence); // 6 o'clock
        Assert.Contains ("🕖 ", style.Sequence); // 7 o'clock
        Assert.Contains ("🕗 ", style.Sequence); // 8 o'clock
        Assert.Contains ("🕘 ", style.Sequence); // 9 o'clock
        Assert.Contains ("🕙 ", style.Sequence); // 10 o'clock
        Assert.Contains ("🕚 ", style.Sequence); // 11 o'clock
    }

    [Fact]
    public void Earth_SequenceContainsGlobeEmojis ()
    {
        SpinnerStyle style = new SpinnerStyle.Earth ();

        Assert.Contains ("🌍 ", style.Sequence);
        Assert.Contains ("🌎 ", style.Sequence);
        Assert.Contains ("🌏 ", style.Sequence);
    }

    [Fact]
    public void Weather_SequenceShowsWeatherProgression ()
    {
        SpinnerStyle style = new SpinnerStyle.Weather ();

        Assert.Contains ("☀️ ", style.Sequence);
        Assert.Contains ("⛅️ ", style.Sequence);
        Assert.Contains ("🌧 ", style.Sequence);
        Assert.Contains ("⛈ ", style.Sequence);
    }

    [Fact]
    public void Shark_SequenceShowsSharkAnimation ()
    {
        SpinnerStyle style = new SpinnerStyle.Shark ();

        // Check that frames show movement
        Assert.All (style.Sequence, frame => Assert.Contains ("|", frame));
        Assert.All (style.Sequence, frame => Assert.Contains ("_", frame));
    }

    [Fact]
    public void Christmas_SequenceContainsTreeEmojis ()
    {
        SpinnerStyle style = new SpinnerStyle.Christmas ();

        Assert.Contains ("🌲", style.Sequence);
        Assert.Contains ("🎄", style.Sequence);
    }

    [Fact]
    public void MindBlown_SequenceShowsProgressiveExpression ()
    {
        SpinnerStyle style = new SpinnerStyle.MindBlown ();

        Assert.Contains ("😐 ", style.Sequence);
        Assert.Contains ("😮 ", style.Sequence);
        Assert.Contains ("🤯 ", style.Sequence);
        Assert.Contains ("💥 ", style.Sequence);
    }
    #endregion

    #region Toggle Style Tests

    [Theory]
    [InlineData (typeof (SpinnerStyle.Toggle), 2)]
    [InlineData (typeof (SpinnerStyle.Toggle2), 2)]
    [InlineData (typeof (SpinnerStyle.Toggle3), 2)]
    [InlineData (typeof (SpinnerStyle.Toggle4), 4)]
    [InlineData (typeof (SpinnerStyle.Toggle5), 2)]
    [InlineData (typeof (SpinnerStyle.Toggle6), 2)]
    [InlineData (typeof (SpinnerStyle.Toggle7), 2)]
    [InlineData (typeof (SpinnerStyle.Toggle8), 2)]
    [InlineData (typeof (SpinnerStyle.Toggle9), 2)]
    [InlineData (typeof (SpinnerStyle.Toggle10), 3)]
    [InlineData (typeof (SpinnerStyle.Toggle11), 2)]
    [InlineData (typeof (SpinnerStyle.Toggle12), 2)]
    [InlineData (typeof (SpinnerStyle.Toggle13), 3)]
    public void ToggleStyles_HaveExpectedFrameCount (Type styleType, int expectedFrames)
    {
        SpinnerStyle? style = Activator.CreateInstance (styleType) as SpinnerStyle;

        Assert.NotNull (style);
        Assert.Equal (expectedFrames, style.Sequence.Length);
    }

    #endregion

    #region Dots Style Variant Tests

    [Theory]
    [InlineData (typeof (SpinnerStyle.Dots))]
    [InlineData (typeof (SpinnerStyle.Dots2))]
    [InlineData (typeof (SpinnerStyle.Dots3))]
    [InlineData (typeof (SpinnerStyle.Dots4))]
    [InlineData (typeof (SpinnerStyle.Dots5))]
    [InlineData (typeof (SpinnerStyle.Dots6))]
    [InlineData (typeof (SpinnerStyle.Dots7))]
    [InlineData (typeof (SpinnerStyle.Dots8))]
    [InlineData (typeof (SpinnerStyle.Dots9))]
    [InlineData (typeof (SpinnerStyle.Dots10))]
    [InlineData (typeof (SpinnerStyle.Dots11))]
    [InlineData (typeof (SpinnerStyle.Dots12))]
    public void DotsStyles_DoNotHaveSpecialCharacters (Type styleType)
    {
        SpinnerStyle? style = Activator.CreateInstance (styleType) as SpinnerStyle;

        Assert.NotNull (style);
        Assert.False (style.HasSpecialCharacters);
    }

    [Fact]
    public void Dots8Bit_SequenceCoversAllBrailleCharacters ()
    {
        SpinnerStyle style = new SpinnerStyle.Dots8Bit ();

        // Braille patterns from U+2800 to U+28FF (256 characters)
        Assert.Equal (256, style.Sequence.Length);

        // Should start with blank braille
        Assert.Equal ("⠀", style.Sequence [0]);

        // Should end with full braille
        Assert.Equal ("⣿", style.Sequence [^1]);
    }

    #endregion

    #region Pulse Style Tests

    [Fact]
    public void OrangePulse_SequenceShowsPulseEffect ()
    {
        SpinnerStyle style = new SpinnerStyle.OrangePulse ();

        Assert.Contains ("🔸 ", style.Sequence);
        Assert.Contains ("🔶 ", style.Sequence);
        Assert.Contains ("🟠 ", style.Sequence);
    }

    [Fact]
    public void BluePulse_SequenceShowsPulseEffect ()
    {
        SpinnerStyle style = new SpinnerStyle.BluePulse ();

        Assert.Contains ("🔹 ", style.Sequence);
        Assert.Contains ("🔷 ", style.Sequence);
        Assert.Contains ("🔵 ", style.Sequence);
    }

    [Fact]
    public void OrangeBluePulse_CombinesBothPulses ()
    {
        SpinnerStyle style = new SpinnerStyle.OrangeBluePulse ();

        // Should contain frames from both orange and blue pulses
        Assert.Contains ("🔸 ", style.Sequence);
        Assert.Contains ("🟠 ", style.Sequence);
        Assert.Contains ("🔹 ", style.Sequence);
        Assert.Contains ("🔵 ", style.Sequence);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AllStyles_CanBeInstantiated ()
    {
        Type [] allStyles = typeof (SpinnerStyle)
            .GetNestedTypes ()
            .Where (t => !t.IsAbstract && t.IsSubclassOf (typeof (SpinnerStyle)))
            .ToArray ();

        foreach (Type styleType in allStyles)
        {
            Exception? exception = Record.Exception (() => Activator.CreateInstance (styleType));

            Assert.Null (exception);
        }
    }

    [Fact]
    public void AllStyles_PropertiesAreImmutable ()
    {
        Type [] allStyles = typeof (SpinnerStyle)
            .GetNestedTypes ()
            .Where (t => !t.IsAbstract && t.IsSubclassOf (typeof (SpinnerStyle)))
            .ToArray ();

        foreach (Type styleType in allStyles)
        {
            SpinnerStyle? style1 = Activator.CreateInstance (styleType) as SpinnerStyle;
            SpinnerStyle? style2 = Activator.CreateInstance (styleType) as SpinnerStyle;

            Assert.NotNull (style1);
            Assert.NotNull (style2);

            // Same type should have same property values
            Assert.Equal (style1.HasSpecialCharacters, style2.HasSpecialCharacters);
            Assert.Equal (style1.SpinBounce, style2.SpinBounce);
            Assert.Equal (style1.SpinDelay, style2.SpinDelay);
            Assert.Equal (style1.Sequence, style2.Sequence);
        }
    }

    #endregion
}