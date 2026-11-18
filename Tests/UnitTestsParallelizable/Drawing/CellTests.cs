using System.Text;

namespace DrawingTests;

public class CellTests
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var c = new Cell ();
        Assert.True (c is { });
        Assert.Empty (c.Runes);
        Assert.Null (c.Attribute);
        Assert.False (c.IsDirty);
        Assert.Null (c.Grapheme);
    }

    [Theory]
    [InlineData (null, new uint [] { })]
    [InlineData ("", new uint [] { })]
    [InlineData ("a", new uint [] { 0x0061 })]
    [InlineData ("👩‍❤️‍💋‍👨", new uint [] { 0x1F469, 0x200D, 0x2764, 0xFE0F, 0x200D, 0x1F48B, 0x200D, 0x1F468 })]
    [InlineData ("æ", new uint [] { 0x00E6 })]
    [InlineData ("a︠", new uint [] { 0x0061, 0xFE20 })]
    [InlineData ("e︡", new uint [] { 0x0065, 0xFE21 })]
    [InlineData ("🇵🇹", new uint [] { 0x1F1F5, 0x1F1F9 })]
    public void Runes_From_Grapheme (string? grapheme, uint [] expected)
    {
        // Arrange
        var c = new Cell { Grapheme = grapheme! };

        // Act
        Rune [] runes = expected.Select (u => new Rune (u)).ToArray ();

        // Assert
        Assert.Equal (grapheme, c.Grapheme);
        Assert.Equal (runes, c.Runes);
    }

    [Fact]
    public void Equals_False ()
    {
        var c1 = new Cell ();

        var c2 = new Cell
        {
            Grapheme = "a", Attribute = new (Color.Red)
        };
        Assert.False (c1.Equals (c2));
        Assert.False (c2.Equals (c1));

        c1.Grapheme = "a";
        c1.Attribute = new ();
        Assert.Equal (c1.Grapheme, c2.Grapheme);
        Assert.False (c1.Equals (c2));
        Assert.False (c2.Equals (c1));
    }

    [Fact]
    public void Set_Text_With_Invalid_Grapheme_Throws ()
    {
        Assert.Throws<InvalidOperationException> (() => new Cell { Grapheme = "ab" });
        Assert.Throws<InvalidOperationException> (() => new Cell { Grapheme = "\u0061\u0062" }); // ab
    }

    [Theory]
    [MemberData (nameof (ToStringTestData))]
    public void ToString_Override (string text, Attribute? attribute, string expected)
    {
        var c = new Cell (attribute, true, text);
        string result = c.ToString ();

        Assert.Equal (expected, result);
    }

    public static IEnumerable<object? []> ToStringTestData ()
    {
        yield return ["", null, "[\"\":]"];
        yield return ["a", null, "[\"a\":]"];
        yield return ["\t", null, "[\"\\t\":]"];
        yield return ["\r", null, "[\"\\r\":]"];
        yield return ["\n", null, "[\"\\n\":]"];
        yield return ["\r\n", null, "[\"\\r\\n\":]"];
        yield return ["\f", null, "[\"\\f\":]"];
        yield return ["\v", null, "[\"\\v\":]"];
        yield return ["\x1B", null, "[\"\\u001B\":]"];
        yield return ["\\", new Attribute (Color.Blue), "[\"\\\":[Blue,Blue,None]]"];
        yield return ["😀", null, "[\"😀\":]"];
        yield return ["👨‍👩‍👦‍👦", null, "[\"👨‍👩‍👦‍👦\":]"];
        yield return ["A", new Attribute (Color.Red) { Style = TextStyle.Blink }, "[\"A\":[Red,Red,Blink]]"];
        yield return ["\U0001F469\u200D\u2764\uFE0F\u200D\U0001F48B\u200D\U0001F468", null, "[\"👩‍❤️‍💋‍👨\":]"];
        yield return ["\uD83C\uDDF5\uD83C\uDDF9", null, "[\"🇵🇹\":]"];
    }

    [Fact]
    public void Graphemes_Decomposed_Normalize ()
    {
        Cell c1 = new ()
        {
            // 'e' + '◌́' COMBINING ACUTE ACCENT (U+0301)
            Grapheme = "e\u0301" // visually "é"
        };

        Cell c2 = new ()
        {
            // NFC single code point (U+00E9)
            Grapheme = "é"
        };

        // Validation
        Assert.Equal ("é", c1.Grapheme); // Proper normalized grapheme
        Assert.Equal (c1.Grapheme, c2.Grapheme);
        Assert.Equal (c1.Runes.Count, c2.Runes.Count);
        Assert.Equal (new (0x00E9), c2.Runes [0]);
    }

    [Fact]
    public void Cell_IsDirty_Flag_Works ()
    {
        var c = new Cell ();
        Assert.False (c.IsDirty);
        c.IsDirty = true;
        Assert.True (c.IsDirty);
        c.IsDirty = false;
        Assert.False (c.IsDirty);
    }

    [Theory]
    [InlineData ("\uFDD0", false)]
    [InlineData ("\uFDEF", false)]
    [InlineData ("\uFFFE", true)]
    [InlineData ("\uFFFF", false)]
    [InlineData ("\U0001FFFE", false)]
    [InlineData ("\U0001FFFF", false)]
    [InlineData ("\U0010FFFE", false)]
    [InlineData ("\U0010FFFF", false)]
    public void IsNormalized_ArgumentException (string text, bool throws)
    {
        try
        {
            bool normalized = text.IsNormalized (NormalizationForm.FormC);

            Assert.True (normalized);
            Assert.False (throws);
        }
        catch (ArgumentException)
        {
            Assert.True (throws);
        }

        Assert.Null (Record.Exception (() => new Cell { Grapheme = text }));
    }

    [Fact]
    public void Surrogate_Normalize_Throws_And_Cell_Setter_Throws ()
    {
        // Create the lone high surrogate at runtime (safe)
        string s = new string ((char)0xD800, 1);

        // Confirm the runtime string actually contains the surrogate
        Assert.Equal (0xD800, s [0]);

        // Normalize should throw
        Assert.Throws<ArgumentException> (() => s.Normalize (NormalizationForm.FormC));

        // And if your Grapheme setter normalizes, assignment should throw as well
        Assert.Throws<ArgumentException> (() => new Cell () { Grapheme = s });

        // Create the lone low surrogate at runtime (safe)
        s = new string ((char)0xDC00, 1);

        // Confirm the runtime string actually contains the surrogate
        Assert.Equal (0xDC00, s [0]);

        // Normalize should throw
        Assert.Throws<ArgumentException> (() => s.Normalize (NormalizationForm.FormC));

        // And if your Grapheme setter normalizes, assignment should throw as well
        Assert.Throws<ArgumentException> (() => new Cell () { Grapheme = s });
    }
}
