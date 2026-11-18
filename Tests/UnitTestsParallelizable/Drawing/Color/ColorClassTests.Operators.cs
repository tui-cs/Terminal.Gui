using System.Numerics;
using System.Reflection;

namespace DrawingTests.ColorTests;

public partial class ColorClassTests
{
    [Theory]
    [Trait ("Category", "Operators")]
    [CombinatorialData]
    public void ExplicitOperator_ToVector3_ReturnsCorrectValue (
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 255, 51)] byte g,
        [CombinatorialRange (0, 255, 51)] byte b,
        [CombinatorialValues (0, 255)] byte a
    )
    {
        Color color = new (r, g, b, a);

        var vector = (Vector3)color;

        Assert.Equal (color.R, vector.X);
        Assert.Equal (color.G, vector.Y);
        Assert.Equal (color.B, vector.Z);
    }

    [Theory]
    [CombinatorialData]
    public void GeneratedEqualityOperators_BehaveAsExpected (
        [CombinatorialValues (0, short.MaxValue, int.MaxValue, uint.MaxValue)]
        uint u1,
        [CombinatorialValues (0, short.MaxValue, int.MaxValue, uint.MaxValue)]
        uint u2
    )
    {
        Color color1 = u1;
        Color color2 = u2;

        if (u1 == u2)
        {
            Assert.True (color1 == color2);
            Assert.False (color1 != color2);
        }
        else
        {
            Assert.True (color1 != color2);
            Assert.False (color1 == color2);
        }
    }

    [Theory]
    [CombinatorialData]
    [Trait ("Category", "Operators")]
    public void GetHashCode_DelegatesTo_Rgba ([CombinatorialRandomData (Count = 16)] int rgba)
    {
        Color color = new (rgba);

        Assert.Equal (rgba.GetHashCode (), color.GetHashCode ());
    }
    
    [Theory]
    [CombinatorialData]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_FromInt32_ReturnsCorrectColorValue (
        [CombinatorialRandomData (Count = 16)] int expectedValue
    )
    {
        Color color = expectedValue;

        Assert.Equal (expectedValue, color.Rgba);
    }

    [Theory]
    [CombinatorialData]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_FromUInt32_ReturnsCorrectColorValue (
        [CombinatorialRandomData (Count = 16)] uint expectedValue
    )
    {
        Color color = expectedValue;

        Assert.Equal (expectedValue, color.Argb);
    }

    [Theory]
    [CombinatorialData]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_FromVector3_ReturnsCorrectColorValue (
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 255, 51)] byte g,
        [CombinatorialRange (0, 255, 51)] byte b
    )
    {
        Vector3 vector = new (r, g, b);
        Color color = vector;

        Assert.Equal (r, color.R);
        Assert.Equal (g, color.G);
        Assert.Equal (b, color.B);
        Assert.Equal (byte.MaxValue, color.A);
    }

    [Theory]
    [CombinatorialData]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_FromVector4_ReturnsCorrectColorValue (
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 255, 51)] byte g,
        [CombinatorialRange (0, 255, 51)] byte b,
        [CombinatorialValues (0, 255)] byte a
    )
    {
        Vector4 vector = new (r, g, b, a);
        Color color = vector;

        Assert.Equal (r, color.R);
        Assert.Equal (g, color.G);
        Assert.Equal (b, color.B);
        Assert.Equal (a, color.A);
    }

    [Theory]
    [CombinatorialData]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_ToInt32_ReturnsCorrectInt32Value (
        [CombinatorialRandomData (Count = 16)] int expectedValue
    )
    {
        Color color = new (expectedValue);

        int colorAsInt32 = color;

        Assert.Equal (expectedValue, colorAsInt32);
    }

    [Theory]
    [CombinatorialData]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_ToUInt32_ReturnsCorrectUInt32Value (
        [CombinatorialRandomData (Count = 16)] uint expectedValue
    )
    {
        Color color = new (expectedValue);

        uint colorAsInt32 = color;

        Assert.Equal (expectedValue, colorAsInt32);
    }

    [Theory]
    [CombinatorialData]
    [Trait ("Category", "Operators")]
    public void ImplicitOperator_ToVector4_ReturnsCorrectVector4Value (
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 255, 51)] byte g,
        [CombinatorialRange (0, 255, 51)] byte b,
        [CombinatorialValues (0, 255)] byte a
    )
    {
        Color color = new (r, g, b, a);
        Vector4 vector = color;

        Assert.Equal (r, vector.X);
        Assert.Equal (g, vector.Y);
        Assert.Equal (b, vector.Z);
        Assert.Equal (a, vector.W);
    }
}

public static partial class ColorTestsTheoryDataGenerators
{
    public static TheoryData<ColorName16, Color> ExplicitOperator_FromColorName_RoundTripsCorrectly ()
    {
        TheoryData<ColorName16, Color> data = []
            ;
        data.Add (ColorName16.Black, new Color (12, 12, 12));
        data.Add (ColorName16.Blue, new Color (0, 55, 218));
        data.Add (ColorName16.Green, new Color (19, 161, 14));
        data.Add (ColorName16.Cyan, new Color (58, 150, 221));
        data.Add (ColorName16.Red, new Color (197, 15, 31));
        data.Add (ColorName16.Magenta, new Color (136, 23, 152));
        data.Add (ColorName16.Yellow, new Color (128, 64, 32));
        data.Add (ColorName16.Gray, new Color (204, 204, 204));
        data.Add (ColorName16.DarkGray, new Color (118, 118, 118));
        data.Add (ColorName16.BrightBlue, new Color (59, 120, 255));
        data.Add (ColorName16.BrightGreen, new Color (22, 198, 12));
        data.Add (ColorName16.BrightCyan, new Color (97, 214, 214));
        data.Add (ColorName16.BrightRed, new Color (231, 72, 86));
        data.Add (ColorName16.BrightMagenta, new Color (180, 0, 158));
        data.Add (ColorName16.BrightYellow, new Color (249, 241, 165));
        data.Add (ColorName16.White, new Color (242, 242, 242));

        return data;
    }

    public static TheoryData<FieldInfo, int> Fields_At_Expected_Offsets ()
    {
        TheoryData<FieldInfo?, int> data = []
            ;

        data.Add (
                  typeof (Color).GetField (
                                           "Argb",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding
                                          ),
                  0
                 );

        data.Add (
                  typeof (Color).GetField (
                                           "Rgba",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding
                                          ),
                  0
                 );

        data.Add (
                  typeof (Color).GetField (
                                           "B",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding
                                          ),
                  0
                 );

        data.Add (
                  typeof (Color).GetField (
                                           "G",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding
                                          ),
                  1
                 );

        data.Add (
                  typeof (Color).GetField (
                                           "R",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding
                                          ),
                  2
                 );

        data.Add (
                  typeof (Color).GetField (
                                           "A",
                                           BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding
                                          ),
                  3
                 );

        return data!;
    }
}
