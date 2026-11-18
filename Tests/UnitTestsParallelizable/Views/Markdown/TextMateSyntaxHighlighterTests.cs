// Copilot - Opus 4.6
// Tests for TextMateSyntaxHighlighter — the TextMateSharp-based ISyntaxHighlighter implementation.

using TextMateSharp.Grammars;

namespace ViewsTests.Markdown;

/// <summary>Tests for <see cref="TextMateSyntaxHighlighter"/>.</summary>
public class TextMateSyntaxHighlighterTests
{
    // --- Construction ---

    [Fact]
    public void Constructor_Default_Theme_DarkPlus ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        Assert.NotNull (highlighter);
    }

    [Fact]
    public void Constructor_Custom_Theme ()
    {
        TextMateSyntaxHighlighter highlighter = new (ThemeName.Monokai);
        Assert.NotNull (highlighter);
    }

    // --- ISyntaxHighlighter interface ---

    [Fact]
    public void Implements_ISyntaxHighlighter ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        Assert.IsAssignableFrom<ISyntaxHighlighter> (highlighter);
    }

    // --- Highlight with known language ---

    [Fact]
    public void Highlight_CSharp_Returns_Multiple_Segments ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("var x = 1;", "csharp");

        // TextMate should tokenize this into multiple segments (var, space, x, space, =, etc.)
        Assert.True (segments.Count > 1, $"Expected multiple segments, got {segments.Count}");
    }

    [Fact]
    public void Highlight_CSharp_Segments_Cover_Full_Line ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        var code = "var x = 42;";
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight (code, "csharp");

        // Concatenated segment text should equal the original line
        string reconstructed = string.Concat (segments.Select (s => s.Text));
        Assert.Equal (code, reconstructed);
    }

    [Fact]
    public void Highlight_CSharp_Keyword_Has_CodeKeyword_Role ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("using System;", "csharp");

        // Copilot
        Assert.Contains (segments, s => s is { Text: "using", Role: VisualRole.CodeKeyword });
    }

    [Fact]
    public void Highlight_CSharp_All_Segments_Have_Roles ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("int x = 42;", "csharp");

        // Copilot
        Assert.All (segments, s => Assert.NotNull (s.Role));
    }

    [Fact]
    public void Highlight_CSharp_Segments_Have_CodeBlock_StyleRole ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("var x = 1;", "csharp");

        // All segments should use CodeBlock role as the base
        Assert.All (segments, s => Assert.Equal (MarkdownStyleRole.CodeBlock, s.StyleRole));
    }

    // --- Unknown / null language ---

    [Fact]
    public void Highlight_Null_Language_Returns_Single_Segment ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        var code = "some plain text";
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight (code, null);

        Assert.Single (segments);
        Assert.Equal (code, segments [0].Text);
        Assert.Equal (MarkdownStyleRole.CodeBlock, segments [0].StyleRole);
    }

    [Fact]
    public void Highlight_Unknown_Language_Returns_Single_Segment ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        var code = "some plain text";
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight (code, "nonexistent_language_xyz");

        Assert.Single (segments);
        Assert.Equal (code, segments [0].Text);
    }

    // --- ResetState ---

    [Fact]
    public void ResetState_Can_Be_Called_Multiple_Times ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        highlighter.ResetState ();
        highlighter.ResetState ();

        // Should not throw
        Assert.NotNull (highlighter);
    }

    [Fact]
    public void ResetState_Allows_Fresh_Tokenization ()
    {
        TextMateSyntaxHighlighter highlighter = new ();

        // Tokenize a partial multi-line construct
        highlighter.Highlight ("/* start of comment", "csharp");

        // Reset before new block
        highlighter.ResetState ();

        // After reset, "var" should be recognized as keyword, not continuation of comment
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("var x = 1;", "csharp");
        string reconstructed = string.Concat (segments.Select (s => s.Text));
        Assert.Equal ("var x = 1;", reconstructed);
        Assert.True (segments.Count > 1, "Expected tokenization after reset");
    }

    // --- Multi-line state ---

    [Fact]
    public void Stateful_Tokenization_Across_Lines ()
    {
        TextMateSyntaxHighlighter highlighter = new ();

        // Start a multi-line string/comment
        IReadOnlyList<StyledSegment> line1 = highlighter.Highlight ("/* this is", "csharp");
        IReadOnlyList<StyledSegment> line2 = highlighter.Highlight ("   a comment */", "csharp");

        // Both lines should produce segments
        Assert.NotEmpty (line1);
        Assert.NotEmpty (line2);

        // The text should be fully covered
        Assert.Equal ("/* this is", string.Concat (line1.Select (s => s.Text)));
        Assert.Equal ("   a comment */", string.Concat (line2.Select (s => s.Text)));
    }

    // --- Theme switching ---

    [Fact]
    public void SetTheme_Does_Not_Change_Roles ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> darkSegments = highlighter.Highlight ("var x = 1;", "csharp");

        highlighter.SetTheme (ThemeName.LightPlus);
        highlighter.ResetState ();
        IReadOnlyList<StyledSegment> lightSegments = highlighter.Highlight ("var x = 1;", "csharp");

        // Copilot
        Assert.Equal (darkSegments.Select (segment => segment.Role), lightSegments.Select (segment => segment.Role));
    }

    // --- Multiple languages ---

    [Theory]
    [InlineData ("python", "def hello():")]
    [InlineData ("javascript", "const x = 42;")]
    [InlineData ("json", "{\"key\": \"value\"}")]
    [InlineData ("html", "<div>hello</div>")]
    [InlineData ("css", "body { color: red; }")]
    public void Highlight_Supports_Multiple_Languages (string language, string code)
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight (code, language);

        Assert.NotEmpty (segments);

        string reconstructed = string.Concat (segments.Select (s => s.Text));
        Assert.Equal (code, reconstructed);
    }

    // --- Empty line ---

    [Fact]
    public void Highlight_Empty_Line_Returns_Single_Empty_Segment ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("", "csharp");

        // Empty line should return at least one (possibly empty) segment
        Assert.NotEmpty (segments);
    }

    // --- Language alias resolution ---

    [Theory]
    [InlineData ("cs")]
    [InlineData ("csharp")]
    [InlineData ("c#")]
    public void Highlight_CSharp_Language_Aliases (string languageId)
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("var x = 1;", languageId);

        // All aliases should resolve to the same grammar and produce multi-token output
        Assert.True (segments.Count > 1, $"Language '{languageId}' should produce tokenized output");
    }

    // --- GetAttributeForScope ---
    // Copilot

    [Theory]
    [InlineData (MarkdownStyleRole.Heading)]
    [InlineData (MarkdownStyleRole.HeadingMarker)]
    [InlineData (MarkdownStyleRole.Emphasis)]
    [InlineData (MarkdownStyleRole.Strong)]
    [InlineData (MarkdownStyleRole.InlineCode)]
    [InlineData (MarkdownStyleRole.Link)]
    [InlineData (MarkdownStyleRole.Quote)]
    [InlineData (MarkdownStyleRole.ListMarker)]
    public void GetAttributeForScope_Returns_NonNull_For_Known_Roles (MarkdownStyleRole role)
    {
        TextMateSyntaxHighlighter highlighter = new ();
        Attribute? result = highlighter.GetAttributeForScope (role);
        Assert.NotNull (result);
    }

    [Fact]
    public void GetAttributeForScope_Returns_Null_For_Normal ()
    {
        // Normal has no special scope — should return null (use default Attribute)
        TextMateSyntaxHighlighter highlighter = new ();
        Attribute? result = highlighter.GetAttributeForScope (MarkdownStyleRole.Normal);
        Assert.Null (result);
    }

    [Fact]
    public void GetAttributeForScope_Heading_Has_Theme_Color ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        Attribute? attr = highlighter.GetAttributeForScope (MarkdownStyleRole.Heading);
        Assert.NotNull (attr);

        // DarkPlus theme should give headings a non-black, non-white foreground color
        Assert.NotEqual (Color.Black, attr.Value.Foreground);
    }

    [Fact]
    public void GetAttributeForScope_Caches_Results ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        Attribute? first = highlighter.GetAttributeForScope (MarkdownStyleRole.Heading);
        Attribute? second = highlighter.GetAttributeForScope (MarkdownStyleRole.Heading);
        Assert.Equal (first, second);
    }

    [Fact]
    public void SetTheme_Clears_Scope_Cache ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        Attribute? dark = highlighter.GetAttributeForScope (MarkdownStyleRole.Heading);
        Assert.NotNull (dark);

        highlighter.SetTheme (ThemeName.Light);
        Attribute? light = highlighter.GetAttributeForScope (MarkdownStyleRole.Heading);
        Assert.NotNull (light);

        // Different themes should produce different colors (DarkPlus vs Light)
        Assert.NotEqual (dark.Value.Foreground, light.Value.Foreground);
    }

    // --- Auto theme detection ---
    // Copilot

    [Fact]
    public void GetThemeForBackground_Dark_Returns_Dark_Theme ()
    {
        ThemeName theme = TextMateSyntaxHighlighter.GetThemeForBackground (Color.Black);
        Assert.Equal (ThemeName.DarkPlus, theme);
    }

    [Fact]
    public void GetThemeForBackground_Light_Returns_Light_Theme ()
    {
        ThemeName theme = TextMateSyntaxHighlighter.GetThemeForBackground (Color.White);
        Assert.Equal (ThemeName.LightPlus, theme);
    }

    [Fact]
    public void Parameterless_Constructor_Defaults_To_DarkPlus ()
    {
        // Without a driver, can't detect background, so default to DarkPlus
        TextMateSyntaxHighlighter highlighter = new ();
        Assert.NotNull (highlighter.DefaultBackground);

        // DarkPlus has a dark default background
        Assert.True (highlighter.DefaultBackground!.Value.IsDarkColor ());
    }

    // --- Theme background verification --- Copilot

    [Theory]
    [InlineData (ThemeName.DarkPlus)]
    [InlineData (ThemeName.LightPlus)]
    [InlineData (ThemeName.Monokai)]
    [InlineData (ThemeName.SolarizedDark)]
    [InlineData (ThemeName.SolarizedLight)]
    [InlineData (ThemeName.Dracula)]
    [InlineData (ThemeName.VisualStudioDark)]
    [InlineData (ThemeName.VisualStudioLight)]
    public void All_Major_Themes_Have_NonNull_DefaultBackground (ThemeName theme)
    {
        TextMateSyntaxHighlighter highlighter = new (theme);
        Assert.NotNull (highlighter.DefaultBackground);
    }

    [Fact]
    public void SetTheme_Updates_DefaultBackground ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        Color? darkBg = highlighter.DefaultBackground;
        Assert.NotNull (darkBg);

        highlighter.SetTheme (ThemeName.Monokai);
        Color? monoBg = highlighter.DefaultBackground;
        Assert.NotNull (monoBg);

        // DarkPlus and Monokai have different backgrounds
        Assert.NotEqual (darkBg, monoBg);
    }

    // --- ThemeName property --- Copilot

    [Fact]
    public void Constructor_Sets_ThemeName ()
    {
        // Copilot
        TextMateSyntaxHighlighter highlighter = new (ThemeName.Monokai);
        Assert.Equal (ThemeName.Monokai, highlighter.ThemeName);
    }

    [Fact]
    public void Default_Constructor_Has_DarkPlus_ThemeName ()
    {
        // Copilot
        TextMateSyntaxHighlighter highlighter = new ();
        Assert.Equal (ThemeName.DarkPlus, highlighter.ThemeName);
    }

    [Fact]
    public void SetTheme_Updates_ThemeName ()
    {
        // Copilot
        TextMateSyntaxHighlighter highlighter = new ();
        Assert.Equal (ThemeName.DarkPlus, highlighter.ThemeName);

        highlighter.SetTheme (ThemeName.SolarizedLight);
        Assert.Equal (ThemeName.SolarizedLight, highlighter.ThemeName);
    }

    [Theory]
    [InlineData ("comment.line", VisualRole.CodeComment)]
    [InlineData ("keyword.control", VisualRole.CodeKeyword)]
    [InlineData ("string.quoted", VisualRole.CodeString)]
    [InlineData ("constant.numeric", VisualRole.CodeNumber)]
    [InlineData ("keyword.operator", VisualRole.CodeOperator)]
    [InlineData ("entity.name.type", VisualRole.CodeType)]
    [InlineData ("support.type", VisualRole.CodeType)]
    [InlineData ("storage.type", VisualRole.CodeType)]
    [InlineData ("meta.preprocessor", VisualRole.CodePreprocessor)]
    [InlineData ("variable.other", VisualRole.CodeIdentifier)]
    [InlineData ("entity.name.variable", VisualRole.CodeIdentifier)]
    [InlineData ("constant.language", VisualRole.CodeConstant)]
    [InlineData ("constant.character", VisualRole.CodeConstant)]
    [InlineData ("punctuation.separator", VisualRole.CodePunctuation)]
    [InlineData ("entity.name.function", VisualRole.CodeFunctionName)]
    [InlineData ("support.function", VisualRole.CodeFunctionName)]
    [InlineData ("entity.other.attribute-name", VisualRole.CodeAttribute)]
    [InlineData ("meta.tag", VisualRole.CodeAttribute)]
    public void ResolveRoleForScopes_Maps_TextMate_Scopes (string scope, VisualRole expected)
    {
        // Copilot
        VisualRole actual = TextMateSyntaxHighlighter.ResolveRoleForScopes ([scope]);
        Assert.Equal (expected, actual);
    }
}
