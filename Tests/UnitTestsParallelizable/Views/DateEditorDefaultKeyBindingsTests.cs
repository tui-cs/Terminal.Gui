// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="DateEditor.DefaultKeyBindings"/> static property.
/// </summary>
public class DateEditorDefaultKeyBindingsTests
{
    [Fact]
    public void DateEditor_DefaultKeyBindings_IsNotNull () => Assert.NotNull (DateEditor.DefaultKeyBindings);

    [Fact]
    public void DateEditor_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in DateEditor.DefaultKeyBindings!)
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
    public void DateEditor_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in DateEditor.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void DateEditor_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (DateEditor).GetProperty (nameof (DateEditor.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property!.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");

        Assert.Null (attr);
    }
}
