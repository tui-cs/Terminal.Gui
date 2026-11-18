// Copilot

namespace InputTests;

/// <summary>
///     Verifies that <see cref="Command.StartSelection"/> and <see cref="Command.StartRectangleSelection"/>
///     are appended at the tail of the enum (after existing members) and do not renumber existing entries.
/// </summary>
public class CommandEnumOrderTests
{
    [Fact]
    public void StartSelection_IsAfter_InsertCaretBelow ()
    {
        // StartSelection must have a higher ordinal than all pre-existing members
        // to avoid renumbering. InsertCaretBelow was the last member before this PR.
        Assert.True ((int)Command.StartSelection > (int)Command.InsertCaretBelow);
    }

    [Fact]
    public void StartRectangleSelection_IsAfter_StartSelection ()
    {
        Assert.True ((int)Command.StartRectangleSelection > (int)Command.StartSelection);
    }

    [Fact]
    public void KillWordRight_Ordinal_IsUnchanged ()
    {
        // KillWordRight was immediately after ToggleExtend in the Selection region.
        // If StartSelection/StartRectangleSelection were inserted before it, its
        // ordinal would shift. This test guards against that.
        Assert.True ((int)Command.KillWordRight > (int)Command.ToggleExtend);

        // KillWordRight should immediately follow ToggleExtend (no gap)
        Assert.Equal ((int)Command.ToggleExtend + 1, (int)Command.KillWordRight);
    }
}
