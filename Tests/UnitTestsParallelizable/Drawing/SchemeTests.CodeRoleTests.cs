// Copilot - Opus 4.6
// Tests for VisualRole.Code support in Scheme, ISyntaxHighlighter.ResetState(),
// fence language extraction, StyledSegment.Attribute, and MarkdownAttributeHelper.

namespace DrawingTests;

/// <summary>Tests for <see cref="VisualRole.Code"/> support in <see cref="Scheme"/>.</summary>
public class SchemeCodeRoleTests
{
    [Fact]
    public void Code_Not_Explicitly_Set_By_Default ()
    {
        Scheme scheme = new (new Attribute ("Red", "Blue"));
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Code, out _));
    }

    [Fact]
    public void Code_Derived_From_Editable_Has_Bold ()
    {
        Scheme scheme = new (new Attribute ("Red", "Blue"));

        Attribute code = scheme.GetAttributeForRole (VisualRole.Code);
        Assert.True (code.Style.HasFlag (TextStyle.Bold));
    }

    [Fact]
    public void Code_Derived_From_Editable_Has_Same_Foreground ()
    {
        Scheme scheme = new (new Attribute ("Red", "Blue"));

        Attribute editable = scheme.GetAttributeForRole (VisualRole.Editable);
        Attribute code = scheme.GetAttributeForRole (VisualRole.Code);
        Assert.Equal (editable.Foreground, code.Foreground);
    }

    [Fact]
    public void Code_Derived_Has_Dimmed_Background ()
    {
        Scheme scheme = new (new Attribute ("Red", "Blue"));

        Attribute editable = scheme.GetAttributeForRole (VisualRole.Editable);
        Attribute code = scheme.GetAttributeForRole (VisualRole.Code);

        // Background should be dimmed relative to Editable's background
        Assert.NotEqual (editable.Background, code.Background);
    }

    [Fact]
    public void Code_Explicitly_Set_Is_Returned_AsIs ()
    {
        Attribute codeAttr = new ("Green", "Yellow", TextStyle.Italic);

        Scheme scheme = new () { Normal = new Attribute ("Red", "Blue"), Code = codeAttr };

        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Code, out Attribute? retrieved));
        Assert.Equal (codeAttr, retrieved);
        Assert.Equal (codeAttr, scheme.Code);
    }

    [Fact]
    public void Code_CopyConstructor_Preserves ()
    {
        Attribute codeAttr = new ("Green", "Yellow", TextStyle.Italic);

        Scheme original = new () { Normal = new Attribute ("Red", "Blue"), Code = codeAttr };

        Scheme copy = new (original);

        Assert.True (copy.TryGetExplicitlySetAttributeForRole (VisualRole.Code, out Attribute? retrieved));
        Assert.Equal (codeAttr, retrieved);
    }

    [Fact]
    public void Code_CopyConstructor_Preserves_Not_Set ()
    {
        Scheme original = new (new Attribute ("Red", "Blue"));
        Scheme copy = new (original);

        Assert.False (copy.TryGetExplicitlySetAttributeForRole (VisualRole.Code, out _));
    }

    [Fact]
    public void Equals_Includes_Code ()
    {
        Attribute normal = new ("Red", "Blue");
        Attribute codeAttr = new ("Green", "Yellow");

        Scheme s1 = new () { Normal = normal, Code = codeAttr };
        Scheme s2 = new () { Normal = normal, Code = codeAttr };
        Scheme s3 = new () { Normal = normal };

        Assert.Equal (s1, s2);
        Assert.NotEqual (s1, s3);
    }

    [Fact]
    public void GetHashCode_Includes_Code ()
    {
        Attribute normal = new ("Red", "Blue");
        Attribute codeAttr = new ("Green", "Yellow");

        Scheme s1 = new () { Normal = normal, Code = codeAttr };
        Scheme s2 = new () { Normal = normal, Code = codeAttr };

        Assert.Equal (s1.GetHashCode (), s2.GetHashCode ());
    }

    [Fact]
    public void ToString_Includes_Code ()
    {
        Scheme scheme = new (new Attribute ("Red", "Blue"));
        var str = scheme.ToString ();
        Assert.Contains ("Code", str, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ObjectInitializer_Sets_Code ()
    {
        Attribute codeAttr = new ("Cyan", "Magenta");

        Scheme scheme = new () { Normal = new Attribute ("Red", "Blue"), Code = codeAttr };

        Assert.Equal (codeAttr, scheme.Code);
    }

    [Fact]
    public void Default_Constructor_Code_Not_Explicit ()
    {
        Scheme scheme = new ();
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Code, out _));
    }

    [Fact]
    public void CodeToken_Roles_Derive_From_Code ()
    {
        // Copilot
        Attribute codeAttr = new ("Green", "Yellow", TextStyle.Italic);
        Scheme scheme = new () { Normal = new Attribute ("Red", "Blue"), Code = codeAttr };

        foreach (VisualRole role in Enum.GetValues<VisualRole> ().Where (role => role > VisualRole.Code))
        {
            Assert.False (scheme.TryGetExplicitlySetAttributeForRole (role, out _));
            Assert.Equal (codeAttr, scheme.GetAttributeForRole (role));
        }
    }

    [Fact]
    public void CodeToken_Role_Can_Be_Explicitly_Set ()
    {
        // Copilot
        Attribute keywordAttr = new ("Cyan", "Black", TextStyle.Bold);
        Scheme scheme = new () { Normal = new Attribute ("Red", "Blue"), CodeKeyword = keywordAttr };

        Assert.True (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeKeyword, out Attribute? retrieved));
        Assert.Equal (keywordAttr, retrieved);
        Assert.Equal (keywordAttr, scheme.GetAttributeForRole (VisualRole.CodeKeyword));
    }

    [Fact]
    public void HardCoded_Base_Has_Default_CodeToken_Colors ()
    {
        // Copilot
        Scheme baseScheme = Scheme.GetHardCodedSchemes () ["Base"];

        Assert.True (baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeKeyword, out Attribute? keyword));
        Assert.NotEqual (baseScheme.Code.Foreground, keyword!.Value.Foreground);
    }

    [Fact]
    public void DeriveAccent_Preserves_Base_CodeToken_Colors ()
    {
        // Copilot
        Attribute terminalDefault = new (Color.White, Color.Black);
        Scheme baseScheme = Scheme.GetHardCodedSchemes () ["Base"];

        Scheme accent = Scheme.DeriveAccent (baseScheme, terminalDefault);

        Assert.True (accent.TryGetExplicitlySetAttributeForRole (VisualRole.CodeKeyword, out Attribute? keyword));
        Assert.Equal (baseScheme.CodeKeyword.Foreground, keyword!.Value.Foreground);
    }
}
