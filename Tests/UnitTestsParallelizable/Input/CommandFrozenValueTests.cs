using Terminal.Gui.Input;

namespace InputTests;

/// <summary>
///     Locks the explicit integer value of every <see cref="Command"/> member. These values are an ABI
///     contract: separately-compiled assemblies (notably the <c>Terminal.Gui.Editor</c> package) bake the
///     integer of each command into their key bindings, so inserting/reordering/renumbering a member
///     silently re-maps already-compiled bindings to the wrong command — the tui-cs/Editor#241 regression
///     where Backspace invoked <see cref="Command.SelectAll"/>.
///     <para>
///         When you ADD a command, append it with the next unused number and add a line here. If this test
///         fails because an existing value changed, that is the bug — restore the value, do not edit the
///         expectation.
///     </para>
/// </summary>
public class CommandFrozenValueTests
{
    // The complete, frozen map. Index == the wire value the enum must keep forever.
    private static readonly (Command Command, int Value) [] Frozen =
    [
        (Command.NotBound, 0),
        (Command.Accept, 1),
        (Command.HotKey, 2),
        (Command.Activate, 3),
        (Command.Up, 4),
        (Command.Down, 5),
        (Command.Left, 6),
        (Command.Right, 7),
        (Command.PageUp, 8),
        (Command.PageDown, 9),
        (Command.PageLeft, 10),
        (Command.PageRight, 11),
        (Command.StartOfPage, 12),
        (Command.EndOfPage, 13),
        (Command.Start, 14),
        (Command.Home, 15),
        (Command.End, 16),
        (Command.LeftStart, 17),
        (Command.RightEnd, 18),
        (Command.WordLeft, 19),
        (Command.DeleteCharRight, 40),
        (Command.DeleteCharLeft, 41),
        (Command.SelectAll, 42),
    ];

    [Fact]
    public void FrozenCommands_HaveTheirContractValue ()
    {
        foreach ((Command command, int value) in Frozen)
        {
            Assert.Equal (value, (int)command);
        }
    }

    [Fact]
    public void NotBound_IsZero ()
    {
        Assert.Equal (0, (int)Command.NotBound);
    }

    [Fact]
    public void AllValues_AreUnique ()
    {
        int [] values = Enum.GetValues<Command> ().Cast<Command> ().Select (c => (int)c).ToArray ();
        Assert.Equal (values.Length, values.Distinct ().Count ());
    }

    [Fact]
    public void BindingCriticalCommands_DoNotCollide ()
    {
        // The exact trio behind tui-cs/Editor#241: a one-off shift made Backspace's bound command
        // (DeleteCharLeft) resolve to SelectAll. They must stay distinct and in this fixed order.
        Assert.Equal (40, (int)Command.DeleteCharRight);
        Assert.Equal (41, (int)Command.DeleteCharLeft);
        Assert.Equal (42, (int)Command.SelectAll);
    }
}
