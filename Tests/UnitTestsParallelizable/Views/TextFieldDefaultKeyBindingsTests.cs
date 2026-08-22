// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="TextField.DefaultKeyBindings"/> static property.
/// </summary>
public class TextFieldDefaultKeyBindingsTests
{
    [Fact]
    public void TextField_DefaultKeyBindings_IsNotNull () => Assert.NotNull (TextField.DefaultKeyBindings);

    [Fact]
    public void TextField_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in TextField.DefaultKeyBindings!)
        {
            Key [] [] allKeyArrays = [platformBinding.All ?? [], platformBinding.Windows ?? [], platformBinding.Linux ?? [], platformBinding.Macos ?? []];

            foreach (Key [] keyArray in allKeyArrays)
            {
                foreach (Key key in keyArray)
                {
                    Assert.NotEqual (Key.Empty, key);
                }
            }
        }
    }

    [Fact]
    public void TextField_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in TextField.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void TextField_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (TextField).GetProperty (nameof (TextField.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");

        Assert.Null (attr);
    }

    [Fact]
    public void TextField_DefaultKeyBindings_ContainsUniqueCommands ()
    {
        Dictionary<Command, PlatformKeyBinding> bindings = TextField.DefaultKeyBindings!;

        // Verify TextField-specific commands are present
        Assert.True (bindings.ContainsKey (Command.WordLeft), "Should contain WordLeft");
        Assert.True (bindings.ContainsKey (Command.WordRight), "Should contain WordRight");
        Assert.True (bindings.ContainsKey (Command.WordLeftExtend), "Should contain WordLeftExtend");
        Assert.True (bindings.ContainsKey (Command.WordRightExtend), "Should contain WordRightExtend");
        Assert.True (bindings.ContainsKey (Command.CutToEndOfLine), "Should contain CutToEndOfLine");
        Assert.True (bindings.ContainsKey (Command.CutToStartOfLine), "Should contain CutToStartOfLine");
        Assert.True (bindings.ContainsKey (Command.KillWordRight), "Should contain KillWordRight");
        Assert.True (bindings.ContainsKey (Command.KillWordLeft), "Should contain KillWordLeft");
        Assert.True (bindings.ContainsKey (Command.ToggleOverwrite), "Should contain ToggleOverwrite");
        Assert.True (bindings.ContainsKey (Command.DeleteAll), "Should contain DeleteAll");

        // Verify Emacs bindings (all platforms)
        Assert.True (bindings.ContainsKey (Command.Left), "Should contain Left (Emacs Ctrl+B)");
        Assert.NotNull (bindings [Command.Left].All);

        Assert.True (bindings.ContainsKey (Command.Right), "Should contain Right (Emacs Ctrl+F)");
        Assert.NotNull (bindings [Command.Right].All);
    }
}
