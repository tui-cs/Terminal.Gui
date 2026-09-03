// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="HexView.DefaultKeyBindings"/> static property.
/// </summary>
public class HexViewDefaultKeyBindingsTests
{
    [Fact]
    public void HexView_DefaultKeyBindings_IsNotNull () => Assert.NotNull (HexView.DefaultKeyBindings);

    [Fact]
    public void HexView_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in HexView.DefaultKeyBindings!)
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
    public void HexView_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in HexView.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void HexView_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (HexView).GetProperty (nameof (HexView.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");

        Assert.Null (attr);
    }

    [Theory]
    [InlineData (Command.StartOfPage)]
    [InlineData (Command.EndOfPage)]
    [InlineData (Command.Insert)]
    public void HexView_DefaultKeyBindings_ContainsUniqueCommands (Command command) => Assert.True (HexView.DefaultKeyBindings!.ContainsKey (command));
}
