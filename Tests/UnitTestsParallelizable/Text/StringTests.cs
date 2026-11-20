namespace TextTests;

#nullable enable

public class StringTests
{
    // Claude - Opus 5.5
    [Theory]
    [InlineData ("a", true, 1)]
    [InlineData ("你", true, 2)]
    [InlineData ("\u0301", true, 0)]
    [InlineData ("\u0007", true, 0)]
    [InlineData ("\u0007", false, -1)]
    public void TestGetColumns_SingleChar_MatchesGraphemePath (string str, bool ignoreLessThanZero, int expected)
    {
        Assert.Equal (expected, str.GetColumns (ignoreLessThanZero));
    }

    [Fact]
    public void TestGetColumns_Null ()
    {
        string? str = null;
        Assert.Equal (0, str!.GetColumns ());
    }

    [Fact]
    public void TestGetColumns_Empty ()
    {
        var str = string.Empty;
        Assert.Equal (0, str.GetColumns ());
    }

    [Fact]
    public void TestGetColumns_SingleRune ()
    {
        var str = "a";
        Assert.Equal (1, str.GetColumns ());
    }

    [Fact]
    public void TestGetColumns_Zero_Width ()
    {
        var str = "\u200D";
        Assert.Equal (0, str.GetColumns ());
    }

    [Theory]
    [InlineData ("a", 1)]
    [InlineData ("á", 1)]
    [InlineData ("ab", 2)]
    [InlineData ("áé", 2)]
    [InlineData ("abc", 3)]
    [InlineData ("áéí", 3)]
    [InlineData ("abcd", 4)]
    public void TestGetColumns_MultiRune (string str, int expected) { Assert.Equal (expected, str.GetColumns ()); }

    // Test non-BMP codepoints 
    // Face with Tears of Joy Emoji (😂), Unicode U+1F602 is 2 columns wide
    [Theory]
    [InlineData ("😂", 2)]
    [InlineData ("😂😂", 4)]
    public void TestGetColumns_MultiRune_NonBMP (string str, int expected) { Assert.Equal (expected, str.GetColumns ()); }

    // Test known wide codepoints
    [Theory]
    [InlineData ("🙂", 2, 1, 2)]
    [InlineData ("a🙂", 3, 2, 3)]
    [InlineData ("🙂a", 3, 2, 3)]
    [InlineData ("👨‍👩‍👦‍👦", 8, 1, 2)]
    [InlineData ("👨‍👩‍👦‍👦🙂", 10, 2, 4)]
    [InlineData ("👨‍👩‍👦‍👦🙂a", 11, 3, 5)]
    [InlineData ("👨‍👩‍👦‍👦a🙂", 11, 3, 5)]
    [InlineData ("👨‍👩‍👦‍👦👨‍👩‍👦‍👦", 16, 2, 4)]
    [InlineData ("าำ", 2, 1, 2)] // า U+0E32 - THAI CHARACTER SARA AA with ำ U+0E33 - THAI CHARACTER SARA AM
    [InlineData ("山", 2, 1, 2)] // The character for "mountain" in Chinese/Japanese/Korean (山), Unicode U+5C71
    [InlineData ("山🙂", 4, 2, 4)] // The character for "mountain" in Chinese/Japanese/Korean (山), Unicode U+5C71
    [InlineData ("a\ufe20e\ufe21", 2, 2, 2)] // Combining Ligature Left Half ︠ - U+fe20 -https://github.com/microsoft/terminal/blob/main/src/types/unicode_width_overrides.xml
    // Combining Ligature Right Half - U+fe21 -https://github.com/microsoft/terminal/blob/main/src/types/unicode_width_overrides.xml
    //[InlineData ("क", 1, 1, 1)] // क U+0915 Devanagari Letter Ka
    //[InlineData ("ि", 1, 1, 1)] // U+093F Devanagari Vowel Sign I ि (i-kar).
    //[InlineData ("कि", 2, 1, 2)] // "कि" is U+0915 for the base consonant "क" with U+093F for the vowel sign "ि" (i-kar).
    [InlineData ("ᄀ", 2, 1, 2)] // ᄀ U+1100 HANGUL CHOSEONG KIYEOK (consonant)
    [InlineData ("ᅡ", 0, 1, 0)] // ᅡ U+1161 HANGUL JUNGSEONG A (vowel)
    [InlineData ("가", 2, 1, 2)] // ᄀ U+1100 HANGUL CHOSEONG KIYEOK (consonant) with ᅡ U+1161 HANGUL JUNGSEONG A (vowel)
    [InlineData ("ᄒ", 2, 1, 2)] // ᄒ U+1112 Hangul Choseong Hieuh
    [InlineData ("ᅵ", 0, 1, 0)] // ᅵ U+1175 Hangul Jungseong I
    [InlineData ("ᇂ", 0, 1, 0)] // ᇂ U+11C2 Hangul Jongseong Hieuh
    [InlineData ("힣", 2, 1, 2)] // ᄒ (choseong h) + ᅵ (jungseong i) + ᇂ (jongseong h)
    [InlineData ("ힰ", 0, 1, 0)]    // U+D7B0 ힰ Hangul Jungseong O-Yeo
    [InlineData ("ᄀힰ", 2, 1, 2)]  // ᄀ U+1100 HANGUL CHOSEONG KIYEOK (consonant) with U+D7B0 ힰ Hangul Jungseong O-Yeo
    //[InlineData ("षि", 2, 1, 2)] // U+0937 ष DEVANAGARI LETTER SSA with U+093F ि COMBINING DEVANAGARI VOWEL SIGN I
    [InlineData ("🇵🇹", 2, 1, 2)] // 🇵 U+1F1F5 — REGIONAL INDICATOR SYMBOL LETTER P with 🇹 U+1F1F9 — REGIONAL INDICATOR SYMBOL LETTER T (flag of Portugal)
    public void TestGetColumns_MultiRune_WideBMP_Graphemes (string str, int expectedRunesWidth, int expectedGraphemesCount, int expectedWidth)
    {
        Assert.Equal (expectedRunesWidth, str.EnumerateRunes ().Sum (r => r.GetColumns ()));
        Assert.Equal (expectedGraphemesCount, GraphemeHelper.GetGraphemes (str).ToArray ().Length);
        Assert.Equal (expectedWidth, str.GetColumns ());
    }

    /// <summary>
    ///     Documents that <c>EnumerateRunes().Sum(GetColumns)</c> produces inflated widths for multi-rune grapheme
    ///     clusters (e.g., ZWJ emoji), while <c>string.GetColumns()</c> correctly clamps each cluster to max 2 columns.
    ///     This is the core issue fixed by replacing rune-sum width measurement with grapheme-aware
    ///     <c>string.GetColumns()</c> in widget code (TableView, TabView, TreeView, FileDialog, ColorBar,
    ///     AppendAutocomplete).
    /// </summary>
    [Theory]
    [InlineData ("\U0001F468\u200D\U0001F469\u200D\U0001F466\u200D\U0001F466", 8, 2)] // ZWJ family 👨‍👩‍👦‍👦: 4 × width-2 emoji + 3 × ZWJ(0) = 8 vs grapheme = 2
    [InlineData ("\U0001F469\u200D\U0001F4BB", 4, 2)] // Woman technologist 👩‍💻: 2 × width-2 emoji + ZWJ(0) = 4 vs grapheme = 2
    [InlineData ("\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466", 8, 2)] // ZWJ family variant 👨‍👩‍👧‍👦
    [InlineData ("\U0001F642", 2, 2)] // Single emoji 🙂: rune width = grapheme width (no difference)
    [InlineData ("abc", 3, 3)] // ASCII: rune width = grapheme width (no difference)
    [InlineData ("a\u0301", 1, 1)] // a + combining acute: both give 1 (combining mark is width 0)
    [InlineData ("\u5C71", 2, 2)] // CJK 山: both give 2 (single wide rune)
    public void TestGetColumns_RuneSum_Inflated_For_GraphemeClusters (string str, int expectedRuneSum, int expectedGetColumns)
    {
        // setup
        int runeSum = str.EnumerateRunes ().Sum (r => r.GetColumns ());
        int graphemeWidth = str.GetColumns ();

        // verify — shows the discrepancy that caused bugs in widget width calculations
        Assert.Equal (expectedRuneSum, runeSum);
        Assert.Equal (expectedGetColumns, graphemeWidth);
        Assert.True (graphemeWidth <= runeSum, "GetColumns should never exceed rune sum");
    }

    [Theory]
    [InlineData (null)]
    [InlineData ("")]
    public void TestGetColumns_Does_Not_Throws_With_Null_And_Empty_String (string? text)
    {
        // ReSharper disable once InvokeAsExtensionMethod
        Assert.Equal (0, StringExtensions.GetColumns (text!));
    }

    public class ReadOnlySpanExtensionsTests
    {
        [Theory]
        [InlineData ("12345", true)] // all ASCII digits
        [InlineData ("0", true)] // single ASCII digit
        [InlineData ("", false)] // empty span
        [InlineData ("12a45", false)] // contains a letter
        [InlineData ("１２３", false)] // full-width Unicode digits (not ASCII)
        [InlineData ("12 34", false)] // contains space
        [InlineData ("١٢٣", false)] // Arabic-Indic digits
        public void IsAllAsciiDigits_WorksAsExpected (string input, bool expected)
        {
            // Arrange
            ReadOnlySpan<char> span = input.AsSpan ();

            // Act
            bool result = span.IsAllAsciiDigits ();

            // Assert
            Assert.Equal (expected, result);
        }
    }

    [Theory]
    [InlineData ("0", true)]
    [InlineData ("9", true)]
    [InlineData ("A", true)]
    [InlineData ("F", true)]
    [InlineData ("a", true)]
    [InlineData ("f", true)]
    [InlineData ("123ABC", true)]
    [InlineData ("abcdef", true)]
    [InlineData ("G", false)]        // 'G' not hex
    [InlineData ("Z9", false)]       // 'Z' not hex
    [InlineData ("12 34", false)]    // space not hex
    [InlineData ("", false)]         // empty string
    [InlineData ("１２３", false)]    // full-width digits, not ASCII
    [InlineData ("0xFF", false)]     // includes 'x'
    public void IsAllAsciiHexDigits_ReturnsExpected (string input, bool expected)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan ();

        // Act
        bool result = span.IsAllAsciiHexDigits ();

        // Assert
        Assert.Equal (expected, result);
    }

    [Theory]
    [MemberData (nameof (GetStringConcatCases))]
    public void ToString_ReturnsExpected (IEnumerable<string> input, string expected)
    {
        // Act
        string result = StringExtensions.ToString (input);

        // Assert
        Assert.Equal (expected, result);
    }

    public static IEnumerable<object []> GetStringConcatCases ()
    {
        yield return [new string [] { }, string.Empty]; // Empty sequence
        yield return [new [] { "" }, string.Empty]; // Single empty string
        yield return [new [] { "A" }, "A"]; // Single element
        yield return [new [] { "A", "B" }, "AB"]; // Simple concatenation
        yield return [new [] { "Hello", " ", "World" }, "Hello World"]; // Multiple parts
        yield return [new [] { "123", "456", "789" }, "123456789"]; // Numeric strings
        yield return [new [] { "👩‍", "🧒" }, "👩‍🧒"]; // Grapheme sequence
        yield return [new [] { "α", "β", "γ" }, "αβγ"]; // Unicode letters
        yield return [new [] { "A", null, "B" }, "AB"]; // Null ignored by string.Concat
        yield return [new [] { "🇵", "🇹" }, "🇵🇹"]; // Grapheme sequence
    }

    [Theory]
    [InlineData ("", false)]                                // Empty string
    [InlineData ("A", false)]                               // Single BMP character
    [InlineData ("AB", false)]                              // Two BMP chars, not a surrogate pair
    [InlineData ("👩", true)]                               // Single emoji surrogate pair (U+1F469)
    [InlineData ("🧒", true)]                               // Another emoji surrogate pair (U+1F9D2)
    [InlineData ("𐍈", true)]                               // Gothic letter hwair (U+10348)
    [InlineData ("A👩", false)]                             // One BMP + one surrogate half
    [InlineData ("👩‍", false)]                              // Surrogate pair + ZWJ (length != 2)
    public void IsSurrogatePair_ReturnsExpected (string input, bool expected)
    {
        // Act
        bool result = input.IsSurrogatePair ();

        // Assert
        Assert.Equal (expected, result);
    }

    [Theory]
    // Control characters (should be replaced with the "Control Pictures" block via +U+2400 offset)
    [InlineData ("\u0000", "\u2400")]  // NULL → ␀
    [InlineData ("\u0009", "\u2409")]  // TAB → ␉
    [InlineData ("\u000A", "\u240A")]  // LF → ␊
    [InlineData ("\u000D", "\u240D")]  // CR → ␍
    [InlineData ("\u001B", "\u241B")]  // ESC → ␛

    // C1 controls (mapped via +U+2400 offset for distinct visuals)
    [InlineData ("\u007F", "\u247F")]  // DEL → Control Picture
    [InlineData ("\u0080", "\u2480")]  // C1 control → distinct visual
    [InlineData ("\u009F", "\u249F")]  // C1 control → distinct visual

    // Printable characters (should remain unchanged)
    [InlineData ("A", "A")]
    [InlineData (" ", " ")]
    [InlineData ("~", "~")]

    // Multi-character strings (control at start → space, no controls → unchanged)
    [InlineData ("AB", "AB")]
    [InlineData ("Hello", "Hello")]

    // Copilot - Security fix: multi-char graphemes starting with control chars are sanitized
    [InlineData ("\u001BA", " ")]       // ESC + A → space (unsafe control at start)
    [InlineData ("\u0009A", " ")]       // TAB + A → space (control at start of multi-char)
    public void MakePrintable_ReturnsExpected (string input, string expected)
    {
        // Act
        string result = input.MakePrintable ();

        // Assert
        Assert.Equal (expected, result);
    }
}
