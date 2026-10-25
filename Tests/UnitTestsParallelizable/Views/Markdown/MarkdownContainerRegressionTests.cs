using System.Text.RegularExpressions;
using Markdig;
using TgMarkdown = Terminal.Gui.Views.Markdown;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownContainerRegressionTests
{
    [Fact]
    public void Custom_Container_In_List_Retains_Develop_Prefix_Behavior ()
    {
        using TgMarkdown view = new ()
        {
            MarkdownPipeline = new MarkdownPipelineBuilder ().UseCustomContainers ().Build ()
        };
        string ansi = view.RenderToAnsi ("- ::: box\n  first\n\n  second\n  :::");
        string plain = Regex.Replace (ansi, @"\x1b\[[0-?]*[ -/]*[@-~]", string.Empty);
        string [] lines = [.. plain.Split ('\n').Select (line => line.TrimEnd ()).Where (line => line.Length > 0)];

        Assert.Equal (["• first", "• second"], lines);
    }
}
