// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="TextView.DefaultKeyBindings"/> static property.
/// </summary>
public class TextViewDefaultKeyBindingsTests
{
    [Fact]
    public void TextView_DefaultKeyBindings_IsNotNull () => Assert.NotNull (TextView.DefaultKeyBindings);

    [Fact]
    public void TextView_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in TextView.DefaultKeyBindings!)
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
    public void TextView_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in TextView.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void TextView_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (TextView).GetProperty (nameof (TextView.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");

        Assert.Null (attr);
    }

    [Fact]
    public void TextView_DefaultKeyBindings_ContainsTextViewSpecificCommands ()
    {
        Dictionary<Command, PlatformKeyBinding> bindings = TextView.DefaultKeyBindings!;

        // Verify TextView-specific commands are present
        Assert.True (bindings.ContainsKey (Command.ToggleExtend), "Should contain ToggleExtend");
        Assert.True (bindings.ContainsKey (Command.CutToEndOfLine), "Should contain CutToEndOfLine");
        Assert.True (bindings.ContainsKey (Command.CutToStartOfLine), "Should contain CutToStartOfLine");
        Assert.True (bindings.ContainsKey (Command.DeleteAll), "Should contain DeleteAll");
        Assert.True (bindings.ContainsKey (Command.WordLeft), "Should contain WordLeft");
        Assert.True (bindings.ContainsKey (Command.WordRight), "Should contain WordRight");
        Assert.True (bindings.ContainsKey (Command.WordLeftExtend), "Should contain WordLeftExtend");
        Assert.True (bindings.ContainsKey (Command.WordRightExtend), "Should contain WordRightExtend");
        Assert.True (bindings.ContainsKey (Command.KillWordRight), "Should contain KillWordRight");
        Assert.True (bindings.ContainsKey (Command.KillWordLeft), "Should contain KillWordLeft");
        Assert.True (bindings.ContainsKey (Command.ToggleOverwrite), "Should contain ToggleOverwrite");
        Assert.True (bindings.ContainsKey (Command.Open), "Should contain Open");
    }
}
