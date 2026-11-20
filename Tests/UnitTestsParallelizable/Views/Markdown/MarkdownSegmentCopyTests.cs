using TgMarkdown = Terminal.Gui.Views.Markdown;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownSegmentCopyTests
{
    [Fact]
    public void Segment_WithText_Preserves_All_Metadata ()
    {
        StyledSegment source = new ("before", MarkdownStyleRole.Link, "https://example.com", "image.png", new Terminal.Gui.Drawing.Attribute (Color.Red, Color.Blue), VisualRole.CodeKeyword)
        {
            AdditionalStyle = TextStyle.Bold | TextStyle.Italic | TextStyle.Strikethrough
        };
        StyledSegment copy = source.WithText ("after");

        Assert.Equal ("after", copy.Text);
        Assert.Equal (source.StyleRole, copy.StyleRole);
        Assert.Equal (source.Url, copy.Url);
        Assert.Equal (source.ImageSource, copy.ImageSource);
        Assert.Equal (source.Attribute, copy.Attribute);
        Assert.Equal (source.Role, copy.Role);
        Assert.Equal (source.AdditionalStyle, copy.AdditionalStyle);
    }

    [Fact]
    public void Code_Legend_Preserves_Additional_Style ()
    {
        StyledSegment source = new ("CodeKeyword", MarkdownStyleRole.CodeBlock)
        {
            AdditionalStyle = TextStyle.Bold | TextStyle.Italic
        };
        List<StyledSegment> output = [];
        CodeRoleLegendHighlighter.AddRoleSegments (output, source);
        StyledSegment segment = Assert.Single (output);

        Assert.Equal (source.AdditionalStyle, segment.AdditionalStyle);
        Assert.Equal (VisualRole.CodeKeyword, segment.Role);
    }
}
