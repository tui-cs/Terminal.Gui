#nullable enable
using System.Text;
using UnitTests;

// Alias Console to MockConsole so we don't accidentally use Console

namespace TextTests;

public class TextFormatterDrawTests (ITestOutputHelper output) : TestDriverBase
{
    public static IEnumerable<object []> CMGlyphs =>
        new List<object []> { new object [] { $"{Glyphs.LeftBracket} Say Hello 你 {Glyphs.RightBracket}", 16, 15 } };

    [Theory]
    [InlineData ("A", 1, 0, "")]
    [InlineData ("A", 0, 1, "")]
    [InlineData ("AB1 2", 2, 1, "2")]
    [InlineData ("AB12", 5, 1, "21BA")]
    [InlineData ("AB\n12", 5, 2, "21\nBA")]
    [InlineData ("ABC 123 456", 7, 2, "CBA    \n654 321")]
    [InlineData ("こんにちは", 1, 1, "")]
    [InlineData ("こんにちは", 2, 1, "は")]
    [InlineData ("こんにちは", 5, 1, "はち")]
    [InlineData ("こんにちは", 10, 1, "はちにんこ")]
    [InlineData ("こんにちは\nAB\n12", 10, 3, "21        \nBA        \nはちにんこ")]
    public void Draw_Horizontal_RightLeft_BottomTop (string text, int width, int height, string expectedText)
    {
        IDriver driver = CreateTestDriver ();

        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.RightLeft_BottomTop
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;
        tf.Draw (driver: driver, screen: new (0, 0, width, height), normalColor: Attribute.Default, hotColor: Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, output, driver);
    }

    [Theory]
    [InlineData ("A", 1, 0, "")]
    [InlineData ("A", 0, 1, "")]
    [InlineData ("AB1 2", 2, 1, "2")]
    [InlineData ("AB12", 5, 1, "21BA")]
    [InlineData ("AB\n12", 5, 2, "BA\n21")]
    [InlineData ("ABC 123 456", 7, 2, "654 321\nCBA    ")]
    [InlineData ("こんにちは", 1, 1, "")]
    [InlineData ("こんにちは", 2, 1, "は")]
    [InlineData ("こんにちは", 5, 1, "はち")]
    [InlineData ("こんにちは", 10, 1, "はちにんこ")]
    [InlineData ("こんにちは\nAB\n12", 10, 3, "はちにんこ\nBA        \n21        ")]
    public void Draw_Horizontal_RightLeft_TopBottom (string text, int width, int height, string expectedText)
    {
        IDriver driver = CreateTestDriver ();

        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.RightLeft_TopBottom
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;
        tf.Draw (driver: driver, screen: new (0, 0, width, height), normalColor: Attribute.Default, hotColor: Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, output, driver);
    }

    [Theory]
    [InlineData ("A", 0, 1, "", 0)]
    [InlineData ("A", 1, 1, "A", 0)]
    [InlineData ("A", 2, 2, " A", 1)]
    [InlineData ("AB", 1, 1, "B", 0)]
    [InlineData ("AB", 2, 2, " A\n B", 0)]
    [InlineData ("ABC", 3, 2, "  B\n  C", 0)]
    [InlineData ("ABC", 4, 2, "   B\n   C", 0)]
    [InlineData ("ABC", 6, 2, "     B\n     C", 0)]
    [InlineData ("こんにちは", 0, 1, "", 0)]
    [InlineData ("こんにちは", 1, 0, "", 0)]
    [InlineData ("こんにちは", 1, 1, "", 0)]
    [InlineData ("こんにちは", 2, 1, "は", 0)]
    [InlineData ("こんにちは", 2, 2, "ち\nは", 0)]
    [InlineData ("こんにちは", 2, 3, "に\nち\nは", 0)]
    [InlineData ("こんにちは", 2, 4, "ん\nに\nち\nは", 0)]
    [InlineData ("こんにちは", 2, 5, "こ\nん\nに\nち\nは", 0)]
    [InlineData ("こんにちは", 2, 6, "こ\nん\nに\nち\nは", 1)]
    [InlineData ("ABCD\nこんにちは", 4, 7, "  こ\n Aん\n Bに\n Cち\n Dは", 2)]
    [InlineData ("こんにちは\nABCD", 3, 7, "こ \nんA\nにB\nちC\nはD", 2)]
    public void Draw_Vertical_Bottom_Horizontal_Right (string text, int width, int height, string expectedText, int expectedY)
    {
        IDriver driver = CreateTestDriver ();

        TextFormatter tf = new ()
        {
            Text = text,
            Alignment = Alignment.End,
            Direction = TextDirection.TopBottom_LeftRight,
            VerticalAlignment = Alignment.End
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;

        tf.Draw (driver: driver, screen: new (Point.Empty, new (width, height)), normalColor: Attribute.Default, hotColor: Attribute.Default);
        Rectangle rect = DriverAssert.AssertDriverContentsWithFrameAre (expectedText, output, driver);
        Assert.Equal (expectedY, rect.Y);
    }

    [Theory]
    [InlineData ("A", 1, 0, "")]
    [InlineData ("A", 0, 1, "")]
    [InlineData ("AB1 2", 1, 2, "2")]
    [InlineData ("AB12", 1, 5, "2\n1\nB\nA")]
    [InlineData ("AB\n12", 2, 5, "B2\nA1")]
    [InlineData ("ABC 123 456", 2, 7, "6C\n5B\n4A\n  \n3 \n2 \n1 ")]
    [InlineData ("こんにちは", 1, 1, "")]
    [InlineData ("こんにちは", 2, 1, "は")]
    [InlineData ("こんにちは", 2, 5, "は\nち\nに\nん\nこ")]
    [InlineData ("こんにちは", 2, 10, "は\nち\nに\nん\nこ")]
    [InlineData ("こんにちは\nAB\n12", 4, 10, "はB2\nちA1\nに  \nん  \nこ  ")]
    public void Draw_Vertical_BottomTop_LeftRight (string text, int width, int height, string expectedText)
    {
        IDriver driver = CreateTestDriver ();

        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.BottomTop_LeftRight
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;
        tf.Draw (driver: driver, screen: new (0, 0, width, height), normalColor: Attribute.Default, hotColor: Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, output, driver);
    }

    [Theory]
    [InlineData ("A", 1, 0, "")]
    [InlineData ("A", 0, 1, "")]
    [InlineData ("AB1 2", 1, 2, "2")]
    [InlineData ("AB12", 1, 5, "2\n1\nB\nA")]
    [InlineData ("AB\n12", 2, 5, "2B\n1A")]
    [InlineData ("ABC 123 456", 2, 7, "C6\nB5\nA4\n  \n 3\n 2\n 1")]
    [InlineData ("こんにちは", 1, 1, "")]
    [InlineData ("こんにちは", 2, 1, "は")]
    [InlineData ("こんにちは", 2, 5, "は\nち\nに\nん\nこ")]
    [InlineData ("こんにちは", 2, 10, "は\nち\nに\nん\nこ")]
    [InlineData ("こんにちは\nAB\n12", 4, 10, "2Bは\n1Aち\n  に\n  ん\n  こ")]
    public void Draw_Vertical_BottomTop_RightLeft (string text, int width, int height, string expectedText)
    {
        IDriver driver = CreateTestDriver ();

        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.BottomTop_RightLeft
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;
        tf.Draw (driver: driver, screen: new (0, 0, width, height), normalColor: Attribute.Default, hotColor: Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, output, driver);
    }

    [Theory]
    [InlineData ("A", 5, 5, "A")]
    [InlineData (
                    "AB12",
                    5,
                    5,
                    @"
A
B
1
2")]
    [InlineData (
                    "AB\n12",
                    5,
                    5,
                    @"
A1
B2")]
    [InlineData ("", 5, 1, "")]
    [InlineData (
                    "Hello Worlds",
                    1,
                    12,
                    @"
H
e
l
l
o
 
W
o
r
l
d
s")]
    [InlineData ("Hello Worlds", 12, 1, @"HelloWorlds")]
    public void Draw_Vertical_TopBottom_LeftRight (string text, int width, int height, string expectedText)
    {
        IDriver driver = CreateTestDriver ();

        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.TopBottom_LeftRight
        };

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;
        tf.Draw (driver: driver, screen: new (0, 0, 20, 20), normalColor: Attribute.Default, hotColor: Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, output, driver);
    }

    [Theory]

    // The expectedY param is to probe that the expectedText param start at that Y coordinate
    [InlineData ("A", 0, "", 0)]
    [InlineData ("A", 1, "A", 0)]
    [InlineData ("A", 2, "A", 0)]
    [InlineData ("A", 3, "A", 1)]
    [InlineData ("AB", 1, "A", 0)]
    [InlineData ("AB", 2, "A\nB", 0)]
    [InlineData ("ABC", 2, "A\nB", 0)]
    [InlineData ("ABC", 3, "A\nB\nC", 0)]
    [InlineData ("ABC", 4, "A\nB\nC", 0)]
    [InlineData ("ABC", 5, "A\nB\nC", 1)]
    [InlineData ("ABC", 6, "A\nB\nC", 1)]
    [InlineData ("ABC", 9, "A\nB\nC", 3)]
    [InlineData ("ABCD", 2, "B\nC", 0)]
    [InlineData ("こんにちは", 0, "", 0)]
    [InlineData ("こんにちは", 1, "に", 0)]
    [InlineData ("こんにちは", 2, "ん\nに", 0)]
    [InlineData ("こんにちは", 3, "ん\nに\nち", 0)]
    [InlineData ("こんにちは", 4, "こ\nん\nに\nち", 0)]
    [InlineData ("こんにちは", 5, "こ\nん\nに\nち\nは", 0)]
    [InlineData ("こんにちは", 6, "こ\nん\nに\nち\nは", 0)]
    [InlineData ("ABCD\nこんにちは", 7, "Aこ\nBん\nCに\nDち\n は", 1)]
    [InlineData ("こんにちは\nABCD", 7, "こA\nんB\nにC\nちD\nは ", 1)]
    public void Draw_Vertical_TopBottom_LeftRight_Middle (string text, int height, string expectedText, int expectedY)
    {
        IDriver driver = CreateTestDriver ();

        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.TopBottom_LeftRight,
            VerticalAlignment = Alignment.Center
        };

        int width = text.ToRunes ().Max (r => r.GetColumns ());

        if (text.Contains ("\n"))
        {
            width++;
        }

        tf.ConstrainToWidth = width;
        tf.ConstrainToHeight = height;
        tf.Draw (driver: driver, screen: new (0, 0, 5, height), normalColor: Attribute.Default, hotColor: Attribute.Default);

        Rectangle rect = DriverAssert.AssertDriverContentsWithFrameAre (expectedText, output, driver);
        Assert.Equal (expectedY, rect.Y);
    }

    [Theory]
    [InlineData ("A", 5, "A")]
    [InlineData (
                    "AB12",
                    5,
                    @"
A
B
1
2")]
    [InlineData (
                    "AB\n12",
                    5,
                    @"
A1
B2")]
    [InlineData ("", 1, "")]
    [InlineData (
                    "AB1 2",
                    2,
                    @"
A12
B  ")]
    [InlineData (
                    "こんにちは",
                    1,
                    @"
こん")]
    [InlineData (
                    "こんにちは",
                    2,
                    @"
こに
んち")]
    [InlineData (
                    "こんにちは",
                    5,
                    @"
こ
ん
に
ち
は")]
    public void Draw_Vertical_TopBottom_LeftRight_Top (string text, int height, string expectedText)
    {
        IDriver driver = CreateTestDriver ();

        TextFormatter tf = new ()
        {
            Text = text,
            Direction = TextDirection.TopBottom_LeftRight
        };

        tf.ConstrainToWidth = 5;
        tf.ConstrainToHeight = height;
        tf.Draw (driver: driver, screen: new (0, 0, 5, height), normalColor: Attribute.Default, hotColor: Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, output, driver);
    }

    [Fact]
    public void FillRemaining_True_False ()
    {
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (22, 5);

        Attribute [] attrs =
        {
            Attribute.Default, new (ColorName16.Green, ColorName16.BrightMagenta),
            new (ColorName16.Blue, ColorName16.Cyan)
        };
        var tf = new TextFormatter { ConstrainToSize = new (14, 3), Text = "Test\nTest long\nTest long long\n", MultiLine = true };

        tf.Draw (driver: driver, screen: new (1, 1, 19, 3), normalColor: attrs [1], hotColor: attrs [2]);

        Assert.False (tf.FillRemaining);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 Test          
 Test long     
 Test long long",
                                                       output,
                                                       driver);

        DriverAssert.AssertDriverAttributesAre (
                                                @"
000000000000000000000
011110000000000000000
011111111100000000000
011111111111111000000
000000000000000000000",
                                                output,
                                                driver,
                                                attrs);

        tf.FillRemaining = true;

        tf.Draw (driver: driver, screen: new (1, 1, 19, 3), normalColor: attrs [1], hotColor: attrs [2]);

        DriverAssert.AssertDriverAttributesAre (
                                                @"
000000000000000000000
011111111111111111110
011111111111111111110
011111111111111111110
000000000000000000000",
                                                output,
                                                driver,
                                                attrs);
    }

    [Theory]
    [InlineData ("Hello World", 15, 1, "Hello     World")]
    [InlineData (
                    "Well Done\nNice Work",
                    15,
                    2,
                    @"
Well       Done
Nice       Work")]
    [InlineData ("你好 世界", 15, 1, "你好       世界")]
    [InlineData (
                    "做 得好\n幹 得好",
                    15,
                    2,
                    @"
做         得好
幹         得好")]
    public void Justify_Horizontal (string text, int width, int height, string expectedText)
    {
        IDriver driver = CreateTestDriver ();

        TextFormatter tf = new ()
        {
            Text = text,
            Alignment = Alignment.Fill,
            ConstrainToSize = new Size (width, height),
            MultiLine = true
        };

        tf.Draw (driver: driver, screen: new (0, 0, width, height), normalColor: Attribute.Default, hotColor: Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, output, driver);
    }

    #region FormatAndGetSizeTests

    // TODO: Add multi-line examples
    // TODO: Add other TextDirection examples

    [Theory]
    [InlineData ("界1234", 10, 10, TextDirection.LeftRight_TopBottom, 6, 1, @"界1234")]
    [InlineData ("01234", 10, 10, TextDirection.LeftRight_TopBottom, 5, 1, @"01234")]
    [InlineData (
                    "界1234",
                    10,
                    10,
                    TextDirection.TopBottom_LeftRight,
                    2,
                    5,
                    """
                    界
                    1 
                    2 
                    3 
                    4 
                    """)]
    [InlineData (
                    "01234",
                    10,
                    10,
                    TextDirection.TopBottom_LeftRight,
                    1,
                    5,
                    """
                    0
                    1
                    2
                    3
                    4
                    """)]
    [InlineData (
                    "界1234",
                    3,
                    3,
                    TextDirection.LeftRight_TopBottom,
                    3,
                    2,
                    """
                    界1
                    234
                    """)]
    [InlineData (
                    "01234",
                    3,
                    3,
                    TextDirection.LeftRight_TopBottom,
                    3,
                    2,
                    """
                    012
                    34 
                    """)]
    [InlineData (
                    "界1234",
                    3,
                    3,
                    TextDirection.TopBottom_LeftRight,
                    3,
                    3,
                    """
                    界3
                    1 4
                    2  
                    """)]
    [InlineData (
                    "01234",
                    3,
                    3,
                    TextDirection.TopBottom_LeftRight,
                    2,
                    3,
                    """
                    03
                    14
                    2 
                    """)]
    [InlineData ("01234", 2, 1, TextDirection.LeftRight_TopBottom, 2, 1, @"01")]
    public void FormatAndGetSize_Returns_Correct_Size (
        string text,
        int width,
        int height,
        TextDirection direction,
        int expectedWidth,
        int expectedHeight,
        string expectedDraw
    )
    {
        IDriver driver = CreateTestDriver ();

        TextFormatter tf = new ()
        {
            Direction = direction,
            ConstrainToWidth = width,
            ConstrainToHeight = height,
            Text = text
        };
        Assert.True (tf.WordWrap);
        Size size = tf.FormatAndGetSize ();
        Assert.Equal (new (expectedWidth, expectedHeight), size);

        tf.Draw (driver: driver, screen: new (0, 0, width, height), normalColor: Attribute.Default, hotColor: Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedDraw, output, driver);
    }

    [Theory]
    [InlineData ("界1234", 10, 10, TextDirection.LeftRight_TopBottom, 6, 1, @"界1234")]
    [InlineData ("01234", 10, 10, TextDirection.LeftRight_TopBottom, 5, 1, @"01234")]
    [InlineData (
                    "界1234",
                    10,
                    10,
                    TextDirection.TopBottom_LeftRight,
                    2,
                    5,
                    """
                    界
                    1 
                    2 
                    3 
                    4 
                    """)]
    [InlineData (
                    "01234",
                    10,
                    10,
                    TextDirection.TopBottom_LeftRight,
                    1,
                    5,
                    """
                    0
                    1
                    2
                    3
                    4
                    """)]
    [InlineData ("界1234", 3, 3, TextDirection.LeftRight_TopBottom, 3, 1, @"界1")]
    [InlineData ("01234", 3, 3, TextDirection.LeftRight_TopBottom, 3, 1, @"012")]
    [InlineData (
                    "界1234",
                    3,
                    3,
                    TextDirection.TopBottom_LeftRight,
                    2,
                    3,
                    """
                    界
                    1 
                    2 
                    """)]
    [InlineData (
                    "01234",
                    3,
                    3,
                    TextDirection.TopBottom_LeftRight,
                    1,
                    3,
                    """
                    0
                    1
                    2
                    """)]
    public void FormatAndGetSize_WordWrap_False_Returns_Correct_Size (
        string text,
        int width,
        int height,
        TextDirection direction,
        int expectedWidth,
        int expectedHeight,
        string expectedDraw
    )
    {
        IDriver driver = CreateTestDriver ();

        TextFormatter tf = new ()
        {
            Direction = direction,
            ConstrainToSize = new (width, height),
            Text = text,
            WordWrap = false
        };
        Assert.False (tf.WordWrap);
        Size size = tf.FormatAndGetSize ();
        Assert.Equal (new (expectedWidth, expectedHeight), size);

        tf.Draw (driver: driver, screen: new (0, 0, width, height), normalColor: Attribute.Default, hotColor: Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedDraw, output, driver);
    }

    [Theory]
    [InlineData ("\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466", 2, 1, TextDirection.LeftRight_TopBottom, "👨‍👩‍👧‍👦")]
    [InlineData ("\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466", 2, 1, TextDirection.TopBottom_LeftRight, "👨‍👩‍👧‍👦")]
    public void Draw_Emojis_With_Zero_Width_Joiner (
        string text,
        int width,
        int height,
        TextDirection direction,
        string expectedDraw
    )
    {
        IDriver driver = CreateTestDriver ();

        TextFormatter tf = new ()
        {
            Direction = direction,
            ConstrainToSize = new (width, height),
            Text = text,
            WordWrap = false
        };
        Assert.Equal (width, text.GetColumns ());

        tf.Draw (driver, new (0, 0, width, height), Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedDraw, output, driver);
    }

    #endregion
}
