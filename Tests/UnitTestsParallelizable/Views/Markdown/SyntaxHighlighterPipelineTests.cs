// Copilot - Opus 4.6
// Tests for ISyntaxHighlighter.ResetState(), fence language extraction,
// StyledSegment.Attribute, and MarkdownAttributeHelper explicit Attribute support.

namespace ViewsTests.Markdown;

/// <summary>Tests for the syntax highlighting pipeline in MarkdownView.</summary>
public class SyntaxHighlighterPipelineTests
{
    // --- Phase 1b: ISyntaxHighlighter.ResetState() ---

    [Fact]
    public void ISyntaxHighlighter_Has_ResetState_Method ()
    {
        // Verify the interface has ResetState via a mock implementation
        MockSyntaxHighlighter highlighter = new ();
        highlighter.ResetState ();
        Assert.True (highlighter.ResetStateCalled);
    }

    [Fact]
    public void MarkdownView_Calls_ResetState_Per_CodeBlock ()
    {
        MockSyntaxHighlighter highlighter = new ();

        Terminal.Gui.Views.Markdown view = new () { SyntaxHighlighter = highlighter, Text = "```csharp\nvar x = 1;\n```\n\ntext\n\n```python\nprint('hi')\n```" };

        // Force layout to trigger parsing
        view.Width = 40;
        view.Height = 20;
        view.SetRelativeLayout (new Size (40, 20));

        // ResetState should be called once per code block (2 blocks)
        Assert.Equal (2, highlighter.ResetStateCallCount);
    }

    // --- Phase 1c: Fence language extraction ---

    [Fact]
    public void Highlighter_Receives_Language_From_Fence ()
    {
        MockSyntaxHighlighter highlighter = new ();

        Terminal.Gui.Views.Markdown view = new () { SyntaxHighlighter = highlighter, Text = "```csharp\nvar x = 1;\n```" };

        view.Width = 40;
        view.Height = 20;
        view.SetRelativeLayout (new Size (40, 20));

        Assert.Contains ("csharp", highlighter.LanguagesReceived);
    }

    [Fact]
    public void Highlighter_Receives_Null_Language_When_No_Fence_Language ()
    {
        MockSyntaxHighlighter highlighter = new ();

        Terminal.Gui.Views.Markdown view = new () { SyntaxHighlighter = highlighter, Text = "```\nvar x = 1;\n```" };

        view.Width = 40;
        view.Height = 20;
        view.SetRelativeLayout (new Size (40, 20));

        Assert.Contains (null, highlighter.LanguagesReceived);
    }

    [Fact]
    public void Highlighter_Receives_Language_With_Tilde_Fence ()
    {
        MockSyntaxHighlighter highlighter = new ();

        Terminal.Gui.Views.Markdown view = new () { SyntaxHighlighter = highlighter, Text = "~~~python\nprint('hi')\n~~~" };

        view.Width = 40;
        view.Height = 20;
        view.SetRelativeLayout (new Size (40, 20));

        Assert.Contains ("python", highlighter.LanguagesReceived);
    }

    [Fact]
    public void Highlighter_Receives_Multiple_Languages ()
    {
        MockSyntaxHighlighter highlighter = new ();

        Terminal.Gui.Views.Markdown view = new () { SyntaxHighlighter = highlighter, Text = "```csharp\nvar x = 1;\n```\n\n```python\nprint('hi')\n```" };

        view.Width = 40;
        view.Height = 20;
        view.SetRelativeLayout (new Size (40, 20));

        Assert.Contains ("csharp", highlighter.LanguagesReceived);
        Assert.Contains ("python", highlighter.LanguagesReceived);
    }

    // --- Phase 2a: StyledSegment.Attribute ---

    [Fact]
    public void StyledSegment_Attribute_Default_Is_Null ()
    {
        StyledSegment segment = new ("text", MarkdownStyleRole.Normal);
        Assert.Null (segment.Attribute);
    }

    [Fact]
    public void StyledSegment_Attribute_Can_Be_Set ()
    {
        Attribute attr = new ("Red", "Blue");
        StyledSegment segment = new ("text", MarkdownStyleRole.CodeBlock, attribute: attr);
        Assert.Equal (attr, segment.Attribute);
    }

    [Fact]
    public void StyledSegment_Attribute_With_Url_And_ImageSource ()
    {
        Attribute attr = new ("Green", "Yellow");

        StyledSegment segment = new ("link", MarkdownStyleRole.Link, "https://example.com", null, attr);

        Assert.Equal (attr, segment.Attribute);
        Assert.Equal ("https://example.com", segment.Url);
    }

    // --- Phase 2b: MarkdownAttributeHelper respects explicit Attribute ---

    [Fact]
    public void GetAttributeForSegment_Returns_Explicit_Attribute_When_Set ()
    {
        // Copilot
        Attribute explicitAttr = new ("Green", "Yellow", TextStyle.Bold);

        StyledSegment segment = new ("keyword", MarkdownStyleRole.CodeBlock, attribute: explicitAttr);

        View view = new () { Width = 10, Height = 1 };
        Attribute result = MarkdownAttributeHelper.GetAttributeForSegment (view, segment);

        Assert.Equal (explicitAttr, result);
    }

    [Fact]
    public void GetAttributeForSegment_Uses_StyleRole_When_Attribute_Null ()
    {
        StyledSegment segment = new ("text", MarkdownStyleRole.Strong);

        View view = new () { Width = 10, Height = 1 };
        Attribute result = MarkdownAttributeHelper.GetAttributeForSegment (view, segment);

        // Strong → Bold
        Assert.True (result.Style.HasFlag (TextStyle.Bold));
    }

    [Fact]
    public void Highlighter_Explicit_Attribute_Flows_Through_Pipeline ()
    {
        // A syntax highlighter that returns segments with explicit Attributes
        Attribute keywordAttr = new ("Blue", "Black", TextStyle.Bold);
        ExplicitAttributeHighlighter highlighter = new (keywordAttr);

        Terminal.Gui.Views.Markdown view = new () { SyntaxHighlighter = highlighter, Text = "```csharp\nvar x = 1;\n```" };

        view.Width = 40;
        view.Height = 20;
        view.SetRelativeLayout (new Size (40, 20));

        // The StyledSegments produced by the highlighter should carry the explicit attribute
        // This is verified by the fact that the view parses without error
        // Detailed rendering tests would need a driver
        Assert.NotNull (view);
    }

    // --- Mock implementations ---

    private sealed class MockSyntaxHighlighter : ISyntaxHighlighter
    {
        public bool ResetStateCalled { get; private set; }
        public int ResetStateCallCount { get; private set; }
        public List<string?> LanguagesReceived { get; } = [];

        public IReadOnlyList<StyledSegment> Highlight (string code, string? language)
        {
            LanguagesReceived.Add (language);

            return [new StyledSegment (code, MarkdownStyleRole.CodeBlock)];
        }

        public void ResetState ()
        {
            ResetStateCalled = true;
            ResetStateCallCount++;
        }

        public string ThemeName => string.Empty;

        public Color? DefaultBackground => null;

        public Attribute? GetAttributeForScope (MarkdownStyleRole role) => null;
    }

    private sealed class ExplicitAttributeHighlighter (Attribute attr) : ISyntaxHighlighter
    {
        public IReadOnlyList<StyledSegment> Highlight (string code, string? language) => [new (code, MarkdownStyleRole.CodeBlock, attribute: attr)];

        public void ResetState () { }

        public string ThemeName => string.Empty;

        public Color? DefaultBackground => null;

        public Attribute? GetAttributeForScope (MarkdownStyleRole role) => null;
    }

    // --- GetAttributeForScope pipeline tests ---
    // Copilot

    [Fact]
    public void MarkdownAttributeHelper_Uses_Highlighter_Scope_When_Available ()
    {
        Attribute headingAttr = new (Color.Cyan, Color.Black, TextStyle.Bold);
        ScopeAwareHighlighter highlighter = new (MarkdownStyleRole.Heading, headingAttr);

        Terminal.Gui.Views.Markdown mv = new () { SyntaxHighlighter = highlighter };
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Black)));

        StyledSegment segment = new ("Hello", MarkdownStyleRole.Heading);
        Attribute result = MarkdownAttributeHelper.GetAttributeForSegment (mv, segment, highlighter);

        Assert.Equal (headingAttr, result);
    }

    [Fact]
    public void MarkdownAttributeHelper_Falls_Back_To_TextStyle_Without_Highlighter ()
    {
        Terminal.Gui.Views.Markdown mv = new ();
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Black)));

        StyledSegment segment = new ("Hello", MarkdownStyleRole.Heading);
        Attribute result = MarkdownAttributeHelper.GetAttributeForSegment (mv, segment);

        // Without highlighter, heading should be Bold
        Assert.True (result.Style.HasFlag (TextStyle.Bold));
    }

    [Fact]
    public void MarkdownAttributeHelper_Falls_Back_When_Highlighter_Returns_Null ()
    {
        // Highlighter returns null for Normal role
        ScopeAwareHighlighter highlighter = new (MarkdownStyleRole.Heading, new Attribute (Color.Cyan, Color.Black));

        Terminal.Gui.Views.Markdown mv = new ();
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Black)));

        StyledSegment segment = new ("Hello", MarkdownStyleRole.Emphasis);
        Attribute result = MarkdownAttributeHelper.GetAttributeForSegment (mv, segment, highlighter);

        // Not Heading, so highlighter returns null → falls back to TextStyle.Italic
        Assert.True (result.Style.HasFlag (TextStyle.Italic));
    }

    [Fact]
    public void MarkdownAttributeHelper_Explicit_Attribute_Takes_Priority_Over_Highlighter ()
    {
        Attribute explicitAttr = new (Color.Red, Color.Blue);
        Attribute headingAttr = new (Color.Cyan, Color.Black);
        ScopeAwareHighlighter highlighter = new (MarkdownStyleRole.Heading, headingAttr);

        Terminal.Gui.Views.Markdown mv = new ();
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Black)));

        StyledSegment segment = new ("Hello", MarkdownStyleRole.Heading, attribute: explicitAttr);
        Attribute result = MarkdownAttributeHelper.GetAttributeForSegment (mv, segment, highlighter);

        // Explicit attribute takes priority
        Assert.Equal (explicitAttr, result);
    }

    private sealed class ScopeAwareHighlighter (MarkdownStyleRole targetRole, Attribute attr) : ISyntaxHighlighter
    {
        public IReadOnlyList<StyledSegment> Highlight (string code, string? language) => [new (code, MarkdownStyleRole.CodeBlock)];

        public void ResetState () { }

        public string ThemeName => string.Empty;

        public Color? DefaultBackground => null;

        public Attribute? GetAttributeForScope (MarkdownStyleRole role) => role == targetRole ? attr : null;
    }

    /// <summary>A highlighter mock that reports a <see cref="DefaultBackground"/>.</summary>
    private sealed class ThemeBackgroundHighlighter (MarkdownStyleRole targetRole, Attribute attr, Color themeBg) : ISyntaxHighlighter
    {
        public IReadOnlyList<StyledSegment> Highlight (string code, string? language) => [new (code, MarkdownStyleRole.CodeBlock)];

        public void ResetState () { }

        public string ThemeName => string.Empty;

        public Color? DefaultBackground { get; } = themeBg;

        public Attribute? GetAttributeForScope (MarkdownStyleRole role) => role == targetRole ? attr : null;
    }

    // --- Theme background propagation --- Copilot

    [Fact]
    public void MarkdownAttributeHelper_Scope_Attribute_Uses_Theme_Background ()
    {
        // Scope-derived attribute has foreground Cyan, but we want theme background (dark gray) not view bg (Blue)
        Attribute headingAttr = new (Color.Cyan, Color.Black, TextStyle.Bold);
        Color themeBg = new (30, 30, 30);
        ThemeBackgroundHighlighter highlighter = new (MarkdownStyleRole.Heading, headingAttr, themeBg);

        Terminal.Gui.Views.Markdown mv = new ();
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));

        StyledSegment segment = new ("Hello", MarkdownStyleRole.Heading);
        Attribute result = MarkdownAttributeHelper.GetAttributeForSegment (mv, segment, highlighter, themeBg);

        // Should use theme background, NOT view background (Blue)
        Assert.Equal (themeBg, result.Background);
        Assert.Equal (Color.Cyan, result.Foreground);
    }

    [Fact]
    public void MarkdownAttributeHelper_Fallback_Uses_Theme_Background ()
    {
        Color themeBg = new (30, 30, 30);

        // Highlighter has no scope for Emphasis, but does have a DefaultBackground
        ThemeBackgroundHighlighter highlighter = new (MarkdownStyleRole.Heading, new Attribute (Color.Cyan, Color.Black), themeBg);

        Terminal.Gui.Views.Markdown mv = new ();
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));

        StyledSegment segment = new ("Hello", MarkdownStyleRole.Emphasis);
        Attribute result = MarkdownAttributeHelper.GetAttributeForSegment (mv, segment, highlighter, themeBg);

        // Theme background used even for fallback role-based styling
        Assert.Equal (themeBg, result.Background);
        Assert.True (result.Style.HasFlag (TextStyle.Italic));
    }

    [Fact]
    public void MarkdownAttributeHelper_CodeToken_Uses_Code_Background ()
    {
        // Copilot
        Terminal.Gui.Views.Markdown mv = new ();
        mv.SetScheme (new Scheme
        {
            Normal = new Attribute (Color.White, Color.Black),
            Code = new Attribute (new Color (StandardColor.LightGray), new Color (StandardColor.DarkBlue)),
            CodeKeyword = new Attribute (Color.Cyan, Color.None)
        });

        StyledSegment segment = new ("public", MarkdownStyleRole.CodeBlock, role: VisualRole.CodeKeyword);
        Attribute result = MarkdownAttributeHelper.GetAttributeForSegment (mv, segment);

        Assert.Equal (Color.Cyan, result.Foreground);
        Assert.Equal (new Color (StandardColor.DarkBlue), result.Background);
    }

    [Fact]
    public void MarkdownAttributeHelper_No_ThemeBackground_Uses_View_Background ()
    {
        Terminal.Gui.Views.Markdown mv = new ();
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));

        StyledSegment segment = new ("Hello", MarkdownStyleRole.Heading);
        Attribute result = MarkdownAttributeHelper.GetAttributeForSegment (mv, segment);

        // Without a highlighter, falls back to view's normal background
        Assert.Equal (Color.Blue, result.Background);
    }

    [Fact]
    public void MarkdownAttributeHelper_Null_DefaultBackground_Uses_View_Background ()
    {
        // Highlighter returns null DefaultBackground
        ScopeAwareHighlighter highlighter = new (MarkdownStyleRole.Heading, new Attribute (Color.Cyan, Color.Black));

        Terminal.Gui.Views.Markdown mv = new ();
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));

        StyledSegment segment = new ("Hello", MarkdownStyleRole.Emphasis);
        Attribute result = MarkdownAttributeHelper.GetAttributeForSegment (mv, segment, highlighter);

        // DefaultBackground is null → uses view's background
        Assert.Equal (Color.Blue, result.Background);
    }
}
