namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class InlineRunTests
{
    [Fact]
    public void Text_Copy_Has_Consistent_Values_And_Equality ()
    {
        InlineRun original = new ("before", MarkdownStyleRole.Strong, "url", "image", Role: VisualRole.Code)
        {
            AdditionalStyle = TextStyle.Bold | TextStyle.Italic
        };
        InlineRun copy = original with
        {
            Text = "after"
        };
        InlineRun expected = new ("after", MarkdownStyleRole.Strong, "url", "image", Role: VisualRole.Code)
        {
            AdditionalStyle = original.AdditionalStyle
        };

        Assert.Equal (expected, copy);
        Assert.Equal (expected.GetHashCode (), copy.GetHashCode ());
        Assert.DoesNotContain (typeof (InlineRun).GetProperties (), property => char.IsLower (property.Name [0]));
    }

    [Fact]
    public void Styled_Segment_Round_Trip_Preserves_Copied_Run_Metadata ()
    {
        InlineRun original = new ("before", MarkdownStyleRole.Link, "url", "image", new Terminal.Gui.Drawing.Attribute (Color.Red, Color.Blue), VisualRole.CodeKeyword)
        {
            AdditionalStyle = TextStyle.Bold | TextStyle.Strikethrough
        };
        InlineRun copy = original with
        {
            Text = "after"
        };
        InlineRun restored = InlineRun.FromStyledSegment (copy.ToStyledSegment ());

        Assert.Equal (copy, restored);
        Assert.Equal (copy.AdditionalStyle, restored.AdditionalStyle);
    }
}
