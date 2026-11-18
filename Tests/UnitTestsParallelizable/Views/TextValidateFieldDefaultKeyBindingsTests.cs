// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="TextValidateField.DefaultKeyBindings"/> static property.
/// </summary>
public class TextValidateFieldDefaultKeyBindingsTests
{
    [Fact]
    public void TextValidateField_DefaultKeyBindings_IsNotNull () => Assert.NotNull (TextValidateField.DefaultKeyBindings);

    [Fact]
    public void TextValidateField_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in TextValidateField.DefaultKeyBindings!)
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
    public void TextValidateField_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in TextValidateField.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void TextValidateField_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property =
            typeof (TextValidateField).GetProperty (nameof (TextValidateField.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");

        Assert.Null (attr);
    }
}
