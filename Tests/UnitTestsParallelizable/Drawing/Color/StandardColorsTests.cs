#pragma warning disable xUnit1031

namespace DrawingTests.ColorTests;

public class StandardColorsTests
{
    [Fact]
    public void GetArgb_HandlesAllStandardColorValues ()
    {
        foreach (StandardColor sc in Enum.GetValues<StandardColor> ())
        {
            uint argb = StandardColors.GetArgb (sc);

            // Verify alpha is always 0xFF (fully opaque)
            var alpha = (byte)((argb >> 24) & 0xFF);
            Assert.Equal (255, alpha);

            // Verify the RGB components match the enum value
            var enumRgb = (int)sc;
            uint expectedArgb = (uint)enumRgb | 0xFF000000;
            Assert.Equal (expectedArgb, argb);
        }
    }

    [Theory]
    [InlineData (StandardColor.Red, 255, 0, 0)]
    [InlineData (StandardColor.Green, 0, 128, 0)]
    [InlineData (StandardColor.Blue, 0, 0, 255)]
    [InlineData (StandardColor.White, 255, 255, 255)]
    [InlineData (StandardColor.Black, 0, 0, 0)]
    [InlineData (StandardColor.AliceBlue, 240, 248, 255)]
    [InlineData (StandardColor.YellowGreen, 154, 205, 50)]
    public void GetArgb_ReturnsCorrectArgbWithFullAlpha (StandardColor standardColor, byte r, byte g, byte b)
    {
        uint argb = StandardColors.GetArgb (standardColor);

        var actualA = (byte)((argb >> 24) & 0xFF);
        var actualR = (byte)((argb >> 16) & 0xFF);
        var actualG = (byte)((argb >> 8) & 0xFF);
        var actualB = (byte)(argb & 0xFF);

        Assert.Equal (255, actualA);
        Assert.Equal (r, actualR);
        Assert.Equal (g, actualG);
        Assert.Equal (b, actualB);
    }

    [Fact]
    public void GetColorNames_ContainsAllStandardColorEnumValues ()
    {
        string [] enumNames = Enum.GetNames<StandardColor> ().Order ().ToArray ();
        IReadOnlyList<string> colorNames = StandardColors.GetColorNames ();

        Assert.Equal (enumNames.Length, colorNames.Count);
        Assert.Equal (enumNames, colorNames);
    }

    [Fact]
    public void GetColorNames_IsAlphabeticallySorted ()
    {
        IReadOnlyList<string> colorNames = StandardColors.GetColorNames ();
        string [] sortedNames = colorNames.OrderBy (n => n).ToArray ();

        Assert.Equal (sortedNames, colorNames);
    }

    [Fact]
    public void LazyInitialization_IsThreadSafe ()
    {
        // Force initialization by calling GetColorNames multiple times in parallel
        Task [] tasks = new Task [10];

        for (var i = 0; i < tasks.Length; i++)
        {
            tasks [i] = Task.Run (() =>
                                  {
                                      IReadOnlyList<string> names = StandardColors.GetColorNames ();
                                      Assert.NotNull (names);
                                      Assert.NotEmpty (names);
                                  },
                                  TestContext.Current.CancellationToken);
        }

        Task.WaitAll (tasks, TestContext.Current.CancellationToken);
    }

    [Fact]
    public void MapValueFactory_CreatesConsistentMapping ()
    {
        // Call TryNameColor multiple times for the same color
        var testColor = new Color (255, 0);

        bool result1 = StandardColors.TryNameColor (testColor, out string? name1);
        bool result2 = StandardColors.TryNameColor (testColor, out string? name2);

        Assert.True (result1);
        Assert.True (result2);
        Assert.Equal (name1, name2);
    }

    [Fact]
    public void ToString_G_Prints_Opaque_ARGB_For_StandardColor_CadetBlue ()
    {
        // Without the fix, A=0x00, so "G" prints "#005F9EA0" instead of "#FF5F9EA0".
        var c = new Color (StandardColor.CadetBlue);

        // Expected: #AARRGGBB with A=FF (opaque)
        Assert.Equal ("#FF5F9EA0", c.ToString ("G"));
    }

    [Fact]
    public void ToString_Returns_Standard_Name_For_StandardColor_CadetBlue ()
    {
        // Without the fix, this uses Color(in StandardColor) -> this((int)colorName),
        // which sets A=0x00 and prevents name resolution (expects A=0xFF).
        var c = new Color (StandardColor.CadetBlue);

        // Expected: named color
        Assert.Equal ("CadetBlue", c.ToString ());
    }

    [Fact]
    public void TryNameColor_IgnoresAlphaChannel ()
    {
        var opaqueRed = new Color (255, 0, 0, 255);
        var transparentRed = new Color (255, 0, 0, 128);

        Assert.True (StandardColors.TryNameColor (opaqueRed, out string? name1));
        Assert.True (StandardColors.TryNameColor (transparentRed, out string? name2));

        Assert.Equal (name1, name2);
        Assert.Equal ("Red", name1);
    }

    [Fact]
    public void TryNameColor_ReturnsConsistentResultsForSameArgb ()
    {
        List<Color> colors = new ();

        // Create multiple Color instances with the same ARGB values
        for (var i = 0; i < 5; i++)
        {
            colors.Add (new (255, 0));
        }

        HashSet<string?> names = new ();

        foreach (Color color in colors)
        {
            StandardColors.TryNameColor (color, out string? name);
            names.Add (name);
        }

        // All should resolve to the same name
        Assert.Single (names);
        Assert.Equal ("Red", names.First ());
    }

    [Fact]
    public void TryNameColor_ReturnsFalseForUnknownColor ()
    {
        var unknownColor = new Color (1, 2, 3);
        bool result = StandardColors.TryNameColor (unknownColor, out string? name);

        Assert.False (result);
        Assert.Null (name);
    }

    [Fact]
    public void TryNameColor_ReturnsFirstAlphabeticalNameForAliasedColors ()
    {
        // Aqua and Cyan have the same RGB values
        var aqua = new Color (0, 255, 255);
        Assert.True (StandardColors.TryNameColor (aqua, out string? name));

        // Should return the alphabetically first name
        Assert.Equal ("Aqua", name);
    }

    [Theory]
    [InlineData (nameof (StandardColor.Aqua), nameof (StandardColor.Cyan))]
    [InlineData (nameof (StandardColor.Fuchsia), nameof (StandardColor.Magenta))]
    [InlineData (nameof (StandardColor.DarkGray), nameof (StandardColor.DarkGrey))]
    [InlineData (nameof (StandardColor.DarkSlateGray), nameof (StandardColor.DarkSlateGrey))]
    [InlineData (nameof (StandardColor.DimGray), nameof (StandardColor.DimGrey))]
    [InlineData (nameof (StandardColor.Gray), nameof (StandardColor.Grey))]
    [InlineData (nameof (StandardColor.LightGray), nameof (StandardColor.LightGrey))]
    [InlineData (nameof (StandardColor.LightSlateGray), nameof (StandardColor.LightSlateGrey))]
    [InlineData (nameof (StandardColor.SlateGray), nameof (StandardColor.SlateGrey))]
    public void TryParseColor_HandlesColorAliases (string name1, string name2)
    {
        Assert.True (StandardColors.TryParseColor (name1, out Color color1));
        Assert.True (StandardColors.TryParseColor (name2, out Color color2));

        Assert.Equal (color1, color2);
    }

    [Theory]
    [InlineData (StandardColor.AmberPhosphor, 255, 191, 0)]
    [InlineData (StandardColor.GreenPhosphor, 0, 255, 102)]
    [InlineData (StandardColor.GuppieGreen, 173, 255, 47)]
    public void TryParseColor_HandlesNonW3CColors (StandardColor color, byte r, byte g, byte b)
    {
        bool result = StandardColors.TryParseColor (color.ToString (), out Color parsedColor);

        Assert.True (result);
        Assert.Equal (r, parsedColor.R);
        Assert.Equal (g, parsedColor.G);
        Assert.Equal (b, parsedColor.B);
    }

    [Fact]
    public void TryParseColor_IsCaseInsensitive ()
    {
        Assert.True (StandardColors.TryParseColor ("RED", out Color upperColor));
        Assert.True (StandardColors.TryParseColor ("red", out Color lowerColor));
        Assert.True (StandardColors.TryParseColor ("Red", out Color mixedColor));

        Assert.Equal (upperColor, lowerColor);
        Assert.Equal (upperColor, mixedColor);
        Assert.Equal (255, upperColor.R);
        Assert.Equal (0, upperColor.G);
        Assert.Equal (0, upperColor.B);
    }

    [Theory]
    [InlineData ("")]
    [InlineData ("NotAColor")]
    [InlineData ("123456")]
    [InlineData ("-1")]
    public void TryParseColor_ReturnsFalseForInvalidInput (string invalidInput)
    {
        bool result = StandardColors.TryParseColor (invalidInput, out Color color);

        Assert.False (result);
        Assert.Equal (default (Color), color);
    }

    [Fact]
    public void TryParseColor_SetsAlphaToFullyOpaque ()
    {
        Assert.True (StandardColors.TryParseColor ("Red", out Color color));

        Assert.Equal (255, color.A);
    }

    [Fact]
    public void TryParseColor_WithEmptySpan_ReturnsFalse ()
    {
        ReadOnlySpan<char> emptySpan = ReadOnlySpan<char>.Empty;
        bool result = StandardColors.TryParseColor (emptySpan, out Color color);

        Assert.False (result);
        Assert.Equal (default (Color), color);
    }

    [Fact]
    public void TryParseColor_WithReadOnlySpan_WorksCorrectly ()
    {
        ReadOnlySpan<char> span = "Red".AsSpan ();
        bool result = StandardColors.TryParseColor (span, out Color color);

        Assert.True (result);
        Assert.Equal (255, color.R);
        Assert.Equal (0, color.G);
        Assert.Equal (0, color.B);
    }
}
