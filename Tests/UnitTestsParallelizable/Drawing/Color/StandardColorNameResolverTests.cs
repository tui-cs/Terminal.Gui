namespace DrawingTests.ColorTests;

public class StandardColorNameResolverTests (ITestOutputHelper output)
{
    private readonly StandardColorsNameResolver _candidate = new();

    [Fact]
    public void GetColorNames_NamesAreInAlphabeticalOrder ()
    {
        string[] alphabeticallyOrderedNames = Enum.GetNames<StandardColor> ().Order ().ToArray ();

        Assert.Equal (alphabeticallyOrderedNames, _candidate.GetColorNames ());
    }

    [Fact]
    public void TryParseColor_Resolves_All_StandardColor_Enum_Values ()
    {
        foreach (StandardColor sc in Enum.GetValues<StandardColor> ())
        {
            bool success = _candidate.TryParseColor (sc.ToString (), out Color actual);
            Assert.True (success, $"Expected to parse StandardColor.{sc}");
            Color expected = new ((int)sc);
            Assert.Equal (expected.R, actual.R);
            Assert.Equal (expected.G, actual.G);
            Assert.Equal (expected.B, actual.B);
        }
    }

    [Fact]
    public void TryNameColor_Resolves_FirstName_For_UniqueArgbValues ()
    {
        Dictionary<uint, string> seen = new ();

        foreach (StandardColor sc in Enum.GetValues<StandardColor> ())
        {
            uint argb = StandardColors.GetArgb (sc);
            if (seen.ContainsKey (argb))
            {
                continue;
            }

            Color color = new (argb);
            bool success = _candidate.TryNameColor (color, out string? resolved);

            Assert.True (success, $"Expected name resolution for {sc} -> ARGB #{argb:X8}");
            Assert.NotNull (resolved);

            List<string> expectedNames = new ();
            foreach (string name in Enum.GetNames<StandardColor> ())
            {
                StandardColor parsed = Enum.Parse<StandardColor> (name);
                if (StandardColors.GetArgb (parsed) == argb)
                {
                    expectedNames.Add (name);
                }
            }

            Assert.Contains (resolved, expectedNames);
            seen [argb] = resolved!;
        }
    }


    [Fact]
    public void TryNameColor_Logs_StandardColor_Aliases ()
    {
        var map = new Dictionary<uint, List<string>> ();

        foreach (StandardColor sc in Enum.GetValues<StandardColor> ())
        {
            var color = new Color ((int)sc);
            if (!map.TryGetValue (color.Argb, out var list))
            {
                list = new List<string> ();
                map [color.Argb] = list;
            }
            list.Add (sc.ToString ());
        }

        foreach (var kvp in map.Where (kvp => kvp.Value.Count > 1))
        {
            output.WriteLine ($"ARGB #{kvp.Key:X8} maps to: {string.Join (", ", kvp.Value)}");
        }
    }


    [Theory]
    [InlineData (nameof (StandardColor.Aqua))]
    [InlineData (nameof (StandardColor.Cyan))]
    [InlineData (nameof (StandardColor.DarkGray))]
    [InlineData (nameof (StandardColor.DarkGrey))]
    [InlineData (nameof (StandardColor.DarkSlateGray))]
    [InlineData (nameof (StandardColor.DarkSlateGrey))]
    [InlineData (nameof (StandardColor.DimGray))]
    [InlineData (nameof (StandardColor.DimGrey))]
    [InlineData (nameof (StandardColor.Fuchsia))]
    [InlineData (nameof (StandardColor.LightGray))]
    [InlineData (nameof (StandardColor.LightGrey))]
    [InlineData (nameof (StandardColor.LightSlateGray))]
    [InlineData (nameof (StandardColor.LightSlateGrey))]
    [InlineData (nameof (StandardColor.Magenta))]
    [InlineData (nameof (StandardColor.SlateGray))]
    [InlineData (nameof (StandardColor.SlateGrey))]
    public void GetColorNames_IncludesNamesWithSameValues (string name)
    {
        string[] names = _candidate.GetColorNames ().ToArray();

        Assert.True (names.Contains (name), $"W3C color names is missing '{name}'.");
    }

    [Theory]
    [InlineData (240, 248, 255, nameof (StandardColor.AliceBlue))]
    [InlineData (0, 255, 255, nameof (StandardColor.Aqua))]
    [InlineData (255, 0, 0, nameof (StandardColor.Red))]
    [InlineData (0, 128, 0, nameof (StandardColor.Green))]
    [InlineData (0, 0, 255, nameof (StandardColor.Blue))]
    [InlineData (0, 255, 0, nameof (StandardColor.Lime))]
    [InlineData (0, 0, 0, nameof (StandardColor.Black))]
    [InlineData (255, 255, 255, nameof (StandardColor.White))]
    [InlineData (154, 205, 50, nameof (StandardColor.YellowGreen))]
    public void TryNameColor_ReturnsExpectedColorName (int r, int g, int b, string expectedName)
    {
        var expected = (true, expectedName);

        Color inputColor = new(r, g, b);
        bool actualSuccess = _candidate.TryNameColor (inputColor, out string? actualName);
        var actual = (actualSuccess, actualName);

        Assert.Equal (expected, actual);
    }

    [Fact]
    public void TryNameColor_NoMatchFails ()
    {
        (bool, string?) expected = (false, null);

        bool actualSuccess = _candidate.TryNameColor (new Color (1, 2, 3), out string? actualName);
        var actual = (actualSuccess, actualName);

        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (nameof (StandardColor.AliceBlue), 240, 248, 255)]
    [InlineData (nameof (StandardColor.BlanchedAlmond), 255, 235, 205)]
    [InlineData (nameof (StandardColor.CadetBlue), 95, 158, 160)]
    [InlineData (nameof (StandardColor.DarkBlue), 0, 0, 139)]
    [InlineData (nameof (StandardColor.FireBrick), 178, 34, 34)]
    [InlineData (nameof (StandardColor.Gainsboro), 220, 220, 220)]
    [InlineData (nameof (StandardColor.HoneyDew), 240, 255, 240)]
    [InlineData (nameof (StandardColor.Indigo), 75, 0, 130)]
    [InlineData (nameof (StandardColor.Khaki), 240, 230, 140)]
    [InlineData (nameof (StandardColor.Lavender), 230, 230, 250)]
    [InlineData (nameof (StandardColor.Maroon), 128, 0, 0)]
    [InlineData (nameof (StandardColor.Navy), 0, 0, 128)]
    [InlineData (nameof (StandardColor.Olive), 128, 128, 0)]
    [InlineData (nameof (StandardColor.Plum), 221, 160, 221)]
    [InlineData (nameof (StandardColor.RoyalBlue), 65, 105, 225)]
    [InlineData (nameof (StandardColor.Silver), 192, 192, 192)]
    [InlineData (nameof (StandardColor.Tomato), 255, 99, 71)]
    [InlineData (nameof (StandardColor.Violet), 238, 130, 238)]
    [InlineData (nameof (StandardColor.WhiteSmoke), 245, 245, 245)]
    [InlineData (nameof (StandardColor.YellowGreen), 154, 205, 50)]
    // Aliases also work
    [InlineData (nameof (StandardColor.Aqua), 0, 255, 255)]
    [InlineData (nameof (StandardColor.Cyan), 0, 255, 255)]
    [InlineData (nameof (StandardColor.DarkGray), 169, 169, 169)]
    [InlineData (nameof (StandardColor.DarkGrey), 169, 169, 169)]
    // Case-insensitive
    [InlineData ("Red", 255, 0, 0)]
    [InlineData ("red", 255, 0, 0)]
    [InlineData ("RED", 255, 0, 0)]
    public void TryParseColor_ReturnsExpectedColor (string inputName, int r, int g, int b)
    {
        var expected = (true, new Color(r, g, b));

        bool actualSuccess = _candidate.TryParseColor (inputName, out Color actualColor);
        var actual = (actualSuccess, actualColor);

        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData ("16737095", 255, 99, 71)] // W3cColor.Tomato
    public void TryParseColor_ResolvesValidEnumNumber (string inputName, byte r, byte g, byte b)
    {
        var expected = (true, new Color(r, g, b));

        bool actualSuccess = _candidate.TryParseColor (inputName, out Color actualColor);
        var actual = (actualSuccess, actualColor);

        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (null)]
    [InlineData ("")]
    [InlineData ("brightlight")]
    public void TryParseColor_FailsOnInvalidColorName (string? invalidName)
    {
        var expected = (false, default(Color));

        bool actualSuccess = _candidate.TryParseColor (invalidName, out Color actualColor);
        var actual = (actualSuccess, actualColor);

        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData ("-16737095")]
    public void TryParseColor_FailsOnInvalidEnumNumber (string invalidName)
    {
        var expected = (false, default(Color));

        bool actualSuccess = _candidate.TryParseColor (invalidName, out Color actualColor);
        var actual = (actualSuccess, actualColor);

        Assert.Equal (expected, actual);
    }
}
