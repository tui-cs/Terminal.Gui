// Claude - Opus 4.7

using Terminal.Gui.Input;

namespace UnitTestsParallelizable.Input;

/// <summary>
///     Tests for the multi-caret <see cref="Command"/> members added for #5318
///     (consumed by tui-cs/Editor vertical multi-caret). The enum shape is part of
///     the contract: the members must exist, be distinct, carry their readable
///     names, and be appended at the end so persisted configs never silently
///     rebind when the enum grows.
/// </summary>
public class CommandInsertCaretEnumTests
{
    [Theory]
    [InlineData (Command.InsertCaretAbove)]
    [InlineData (Command.InsertCaretBelow)]
    public void InsertCaretCommand_IsDefined (Command command)
    {
        Assert.True (Enum.IsDefined (command));
    }

    [Fact]
    public void InsertCaretCommands_AreDistinct ()
    {
        Assert.NotEqual (Command.InsertCaretAbove, Command.InsertCaretBelow);
    }

    [Theory]
    [InlineData (Command.InsertCaretAbove, "InsertCaretAbove")]
    [InlineData (Command.InsertCaretBelow, "InsertCaretBelow")]
    public void InsertCaretCommand_HasReadableName (Command command, string expectedName)
    {
        // The readable name is what serializes into config.json (see
        // CommandInsertCaretKeyBindingTests). A rename would silently break
        // every user's persisted binding, so pin it.
        Assert.Equal (expectedName, Enum.GetName (command));
    }

    [Fact]
    public void InsertCaretCommands_AreAppendedAtEnd_NoRenumbering ()
    {
        // #5319 appends the two members at the END of the enum specifically so
        // no pre-existing member's implicit value shifts (serialization-stable;
        // a persisted (Command) value keeps resolving to the same command).
        // Assert it positionally: InsertCaretBelow is above InsertCaretAbove,
        // and both are after all members that existed before #5319.
        Command [] all = Enum.GetValues<Command> ();

        var aboveValue = (int)Command.InsertCaretAbove;
        var belowValue = (int)Command.InsertCaretBelow;

        Assert.True (belowValue > aboveValue);

        // All members except the InsertCaret and mouse-selection commands (added later)
        // should have lower values than InsertCaretAbove.
        HashSet<Command> tailCommands =
        [
            Command.InsertCaretAbove,
            Command.InsertCaretBelow,
            Command.StartSelection,
            Command.StartRectangleSelection
        ];

        Assert.All (
                    all.Where (c => !tailCommands.Contains (c)),
                    other => Assert.True ((int)other < aboveValue, $"{other} ({(int)other}) is not below InsertCaretAbove ({aboveValue}) — the enum was renumbered, breaking persisted configs"));
    }
}
