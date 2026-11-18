using Xunit;

namespace DrawingTests;

public class SchemeGetAttributeForRoleAlgorithmTests
{
    [Fact]
    public void Normal_Is_Always_Explicit ()
    {
        Attribute normal = new ("Red", "Blue");
        Scheme scheme = new (normal);

        Assert.Equal (normal, scheme.GetAttributeForRole (VisualRole.Normal));
    }

    [Fact]
    public void Focus_Derived_From_Normal_Swaps_FgBg ()
    {
        Attribute normal = new ("Red", "Blue");
        Scheme scheme = new (normal);

        Attribute focus = scheme.GetAttributeForRole (VisualRole.Focus);
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Focus, out _));
        Assert.Equal (normal.Background, focus.Foreground);
        Assert.Equal (normal.Foreground, focus.Background);
    }

    //[Fact]
    //public void Highlight_Derived_From_Normal_HighlightColor ()
    //{
    //    Attribute normal = new ("Red", "Blue");
    //    Scheme scheme = new (normal);

    //    Attribute highlight = scheme.GetAttributeForRole (VisualRole.Highlight);
    //    Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Highlight, out _));
    //    Assert.Equal (normal.Background.GetHighlightColor (), highlight.Background);
    //}

    //[Fact]
    //public void Editable_Derived_From_Normal_LightYellow_Fg ()
    //{
    //    Attribute normal = new ("Red", "Blue");
    //    Scheme scheme = new (normal);

    //    Attribute editable = scheme.GetAttributeForRole (VisualRole.Editable);
    //    Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Editable, out _));
    //    Assert.Equal (new Color ("LightYellow"), editable.Foreground);
    //}

    //[Fact]
    //public void ReadOnly_Derived_From_Editable_Italic ()
    //{
    //    Attribute normal = new ("Red", "Blue");
    //    Scheme scheme = new (normal);

    //    Attribute readOnly = scheme.GetAttributeForRole (VisualRole.ReadOnly);
    //    Attribute editable = scheme.GetAttributeForRole (VisualRole.Editable);
    //    Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.ReadOnly, out _));
    //    Assert.Equal (editable.Foreground, readOnly.Foreground);
    //    Assert.True (readOnly.Style.HasFlag (TextStyle.Italic));
    //}

    //[Fact]
    //public void Disabled_Derived_From_Normal_Faint ()
    //{
    //    Attribute normal = new ("Red", "Blue");
    //    Scheme scheme = new (normal);

    //    Attribute disabled = scheme.GetAttributeForRole (VisualRole.Disabled);
    //    Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Disabled, out _));
    //    Assert.True (disabled.Style.HasFlag (TextStyle.Faint));
    //}

    [Fact]
    public void Active_Derived_Correctly ()
    {
        Attribute normal = new ("Red", "Blue");
        Scheme scheme = new (normal);

        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Active, out _));

        Attribute active = scheme.GetAttributeForRole (VisualRole.Active);
        Attribute focus = scheme.GetAttributeForRole (VisualRole.Focus);
        Assert.True (active.Style.HasFlag (TextStyle.Bold));
        //Assert.Equal (active.Foreground, focus.Foreground);
        //Assert.Equal (active.Background, active.Background);
    }

    [Fact]
    public void HotNormal_Derived_From_Normal_Underline ()
    {
        Attribute normal = new ("Red", "Blue");
        Scheme scheme = new (normal);

        Attribute hotNormal = scheme.GetAttributeForRole (VisualRole.HotNormal);
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotNormal, out _));
        Assert.True (hotNormal.Style.HasFlag (TextStyle.Underline));
    }

    [Fact]
    public void HotFocus_Derived_From_Focus_Underline ()
    {
        Attribute normal = new ("Red", "Blue");
        Scheme scheme = new (normal);

        Attribute hotFocus = scheme.GetAttributeForRole (VisualRole.HotFocus);
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotFocus, out _));
        Attribute focus = scheme.GetAttributeForRole (VisualRole.Focus);
        Assert.True (hotFocus.Style.HasFlag (TextStyle.Underline));
        Assert.Equal (focus.Foreground, hotFocus.Foreground);
        Assert.Equal (focus.Background, hotFocus.Background);
    }

    [Fact]
    public void HotActive_Derived_From_Active_Underline ()
    {
        Attribute normal = new ("Red", "Blue");
        Scheme scheme = new (normal);

        Attribute hotActive = scheme.GetAttributeForRole (VisualRole.HotActive);
        Assert.False (scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotActive, out _));
        Attribute active = scheme.GetAttributeForRole (VisualRole.Active);
        Assert.True (hotActive.Style.HasFlag (TextStyle.Underline));
        Assert.Equal (active.Foreground, hotActive.Foreground);
        Assert.Equal (active.Background, hotActive.Background);
    }
}
