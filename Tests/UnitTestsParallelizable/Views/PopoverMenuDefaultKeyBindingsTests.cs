// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="PopoverMenu.DefaultKeyBindings"/> static property.
/// </summary>
public class PopoverMenuDefaultKeyBindingsTests
{
    [Fact]
    public void PopoverMenu_DefaultKeyBindings_IsNotNull () => Assert.NotNull (PopoverMenu.DefaultKeyBindings);

    [Fact]
    public void PopoverMenu_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in PopoverMenu.DefaultKeyBindings!)
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
    public void PopoverMenu_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in PopoverMenu.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void PopoverMenu_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (PopoverMenu).GetProperty (nameof (PopoverMenu.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");

        Assert.Null (attr);
    }
}
