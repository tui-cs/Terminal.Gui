using System.Text.Json;

namespace ConfigurationTests;

public class SchemeJsonConverterTests
{
    //string json = @"
    //	{
    //	""Schemes"": {
    //		""Base"": {
    //			""normal"": {
    //				""foreground"": ""White"",
    //				""background"": ""Blue""
    //   		            },
    //			""focus"": {
    //				""foreground"": ""Black"",
    //				""background"": ""Gray""
    //			    },
    //			""hotNormal"": {
    //				""foreground"": ""BrightCyan"",
    //				""background"": ""Blue""
    //			    },
    //			""hotFocus"": {
    //				""foreground"": ""BrightBlue"",
    //				""background"": ""Gray""
    //			    },
    //			""disabled"": {
    //				""foreground"": ""DarkGray"",
    //				""background"": ""Blue""
    //			    }
    //		}
    //		}
    //	}";
    [Fact]
    public void TestSchemesSerialization_Equality ()
    {
        // Arrange
        var expectedScheme = new Scheme
        {
            Normal = new (Color.White, Color.Blue),
            Focus = new (Color.Black, Color.Gray),
            HotNormal = new (Color.BrightCyan, Color.Blue),
            HotFocus = new (Color.BrightBlue, Color.Gray),
            Active = new (Color.Gray, Color.Black),
            HotActive = new (Color.Blue, Color.Gray),
            Highlight = new (Color.Gray, Color.Black),
            Editable = new (Color.Gray, Color.Black),
            ReadOnly = new (Color.Gray, Color.Black),
            Disabled = new (Color.Gray, Color.Black),
        };

        string serializedScheme =
            JsonSerializer.Serialize (expectedScheme, TuiSerializerContext.Instance.Options);

        // Act
        var actualScheme =
            JsonSerializer.Deserialize<Scheme> (serializedScheme, TuiSerializerContext.Instance.Options);

        // Assert
        Assert.Equal (expectedScheme, actualScheme);
    }

    [Fact]
    public void TestSchemesSerialization ()
    {
        var expected = new Scheme
        {
            Normal = new (Color.White, Color.Blue),
            Focus = new (Color.Black, Color.Gray),
            HotNormal = new (Color.BrightCyan, Color.Blue),
            HotFocus = new (Color.BrightBlue, Color.Gray),
            Active = new (Color.Gray, Color.Black),
            HotActive = new (Color.Blue, Color.Gray),
            Highlight = new (Color.Gray, Color.Black),
            Editable = new (Color.Gray, Color.Black),
            ReadOnly = new (Color.Gray, Color.Black),
            Disabled = new (Color.Gray, Color.Black),
        };

        string json = JsonSerializer.Serialize (expected, TuiSerializerContext.Instance.Options);
        Scheme? actual = JsonSerializer.Deserialize<Scheme> (json, TuiSerializerContext.Instance.Options);

        Assert.NotNull (actual);

        foreach (VisualRole role in Enum.GetValues<VisualRole> ())
        {
            Attribute expectedAttr = expected.GetAttributeForRole (role);
            Attribute actualAttr = actual.GetAttributeForRole (role);

            Assert.Equal (expectedAttr, actualAttr);
        }
    }

    [Fact]
    public void Deserialized_Attributes_AreExplicitlySet ()
    {
        const string json = """
                            {
                                "Normal": { "Foreground": "White", "Background": "Blue" },
                                "Focus": { "Foreground": "Black", "Background": "Gray" },
                                "HotNormal": { "Foreground": "BrightCyan", "Background": "Blue" },
                                "HotFocus": { "Foreground": "BrightBlue", "Background": "Gray" },
                                "Active": { "Foreground": "DarkGray", "Background": "Blue" },
                                "HotActive": { "Foreground": "DarkGray", "Background": "Blue" },
                                "Highlight": { "Foreground": "DarkGray", "Background": "Blue" },
                                "Editable": { "Foreground": "DarkGray", "Background": "Blue" },
                                "Readonly": { "Foreground": "DarkGray", "Background": "Blue" },
                                "Disabled": { "Foreground": "DarkGray", "Background": "Blue" }
                            }
                            """;

        Scheme scheme = JsonSerializer.Deserialize<Scheme> (json, TuiSerializerContext.Instance.Options)!;

        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Normal, out _));
        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotNormal, out _));
        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Focus, out _));
        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotFocus, out _));
        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Active, out _));
        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotActive, out _));
        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Highlight, out _));
        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Editable, out _));
        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.ReadOnly, out _));
        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Disabled, out _));
    }

    [Fact]
    public void Deserialized_Attributes_NotSpecified_AreImplicit ()
    {
        const string json = """
                            {
                              "Normal": { "Foreground": "White", "Background": "Black" }
                            }
                            """;

        Scheme scheme = JsonSerializer.Deserialize<Scheme> (json, TuiSerializerContext.Instance.Options)!;

        // explicitly set
        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Normal, out _));

        // derived from Normal
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotNormal, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Focus, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotFocus, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Active, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotActive, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Highlight, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Editable, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.ReadOnly, out _));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Disabled, out _));
    }

    [Fact]
    public void CodeToken_Attributes_RoundTrip ()
    {
        // Copilot
        Scheme expected = new ()
        {
            Normal = new (Color.White, Color.Black),
            CodeKeyword = new (Color.Blue, Color.Black, TextStyle.Bold)
        };

        string json = JsonSerializer.Serialize (expected, TuiSerializerContext.Instance.Options);
        Scheme? actual = JsonSerializer.Deserialize<Scheme> (json, TuiSerializerContext.Instance.Options);

        Assert.NotNull (actual);
        Assert.True (actual.TryGetExplicitlySetAttributeForRole (VisualRole.CodeKeyword, out Attribute? attr));
        Assert.Equal (expected.CodeKeyword, attr);
    }
}
