// Copilot

using Terminal.Gui.Input;

namespace UnitTestsParallelizable.Input;

public class CommandEnumTests
{
    [Fact]
    public void EditorCommands_AreDefined ()
    {
        // Verify the editor-oriented Command enum values exist and are distinct
        Command [] editorCommands =
        [
            Command.Find,
            Command.FindNext,
            Command.FindPrevious,
            Command.Replace,
            Command.InsertTab,
            Command.Unindent
        ];

        // All values should be distinct
        Assert.Equal (editorCommands.Length, editorCommands.Distinct ().Count ());
    }

    [Theory]
    [InlineData (Command.Find)]
    [InlineData (Command.FindNext)]
    [InlineData (Command.FindPrevious)]
    [InlineData (Command.Replace)]
    [InlineData (Command.InsertTab)]
    [InlineData (Command.Unindent)]
    public void EditorCommand_IsDefined (Command command)
    {
        Assert.True (Enum.IsDefined (command));
    }
}
