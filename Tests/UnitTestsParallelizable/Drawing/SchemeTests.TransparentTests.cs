namespace DrawingTests;

public class SchemeTransparentTests
{
    [Fact]
    public void Focus_Derived_From_Normal_WithTransparentBackground_ResolvesForegroundToBlack ()
    {
        // When Normal has a Transparent background, the Focus derivation swaps fg/bg.
        // Without ResolveTransparentToBlack, Focus.Foreground would be Transparent,
        // making text invisible. It should be resolved to Black instead.
        Attribute normal = new (new Color ("Red"), Color.None);
        Scheme scheme = new (normal);

        Attribute focus = scheme.GetAttributeForRole (VisualRole.Focus);

        // Focus.Foreground should be Black (resolved from Transparent), not Transparent
        Assert.Equal (new Color (0, 0, 0), focus.Foreground);
        // Focus.Background should be Normal's foreground (Red)
        Assert.Equal (normal.Foreground, focus.Background);
    }

    [Fact]
    public void Focus_Derived_From_Normal_WithOpaqueBackground_PreservesColors ()
    {
        // When Normal has an opaque background, Focus derivation should swap as usual
        // without any ResolveTransparentToBlack intervention.
        Color fg = new ("Red");
        Color bg = new ("Blue");
        Attribute normal = new (fg, bg);
        Scheme scheme = new (normal);

        Attribute focus = scheme.GetAttributeForRole (VisualRole.Focus);

        // Standard swap: Focus.Foreground = Normal.Background, Focus.Background = Normal.Foreground
        Assert.Equal (bg, focus.Foreground);
        Assert.Equal (fg, focus.Background);
    }

    [Fact]
    public void Focus_WithTransparentBackground_ForegroundIsNotTransparent ()
    {
        // Explicitly verify that Focus.Foreground is never Transparent
        Attribute normal = new (new Color ("White"), Color.None);
        Scheme scheme = new (normal);

        Attribute focus = scheme.GetAttributeForRole (VisualRole.Focus);

        Assert.NotEqual (Color.None, focus.Foreground);
    }

    [Fact]
    public void Normal_WithTransparentBackground_IsPreserved ()
    {
        // Normal's Transparent background should be preserved as-is (not resolved)
        Attribute normal = new (new Color ("White"), Color.None);
        Scheme scheme = new (normal);

        Attribute normalResult = scheme.GetAttributeForRole (VisualRole.Normal);

        Assert.Equal (Color.None, normalResult.Background);
    }

    [Fact]
    public void HotFocus_Derived_From_Focus_WithTransparentNormalBackground_HasResolvedForeground ()
    {
        // HotFocus derives from Focus, which derives from Normal.
        // If Normal has Transparent background, Focus resolves foreground to Black.
        // HotFocus should inherit Focus's resolved foreground (Black), plus Underline.
        Attribute normal = new (new Color ("LightBlue"), Color.None);
        Scheme scheme = new (normal);

        Attribute focus = scheme.GetAttributeForRole (VisualRole.Focus);
        Attribute hotFocus = scheme.GetAttributeForRole (VisualRole.HotFocus);

        Assert.Equal (focus.Foreground, hotFocus.Foreground);
        Assert.Equal (focus.Background, hotFocus.Background);
        Assert.True (hotFocus.Style.HasFlag (TextStyle.Underline));
    }

    [Fact]
    public void HotNormal_WithTransparentBackground_PreservesTransparentBackground ()
    {
        // HotNormal derives from Normal with Underline style added.
        // The Transparent background should remain Transparent.
        Attribute normal = new (new Color ("Red"), Color.None);
        Scheme scheme = new (normal);

        Attribute hotNormal = scheme.GetAttributeForRole (VisualRole.HotNormal);

        Assert.Equal (Color.None, hotNormal.Background);
        Assert.Equal (normal.Foreground, hotNormal.Foreground);
        Assert.True (hotNormal.Style.HasFlag (TextStyle.Underline));
    }
}
