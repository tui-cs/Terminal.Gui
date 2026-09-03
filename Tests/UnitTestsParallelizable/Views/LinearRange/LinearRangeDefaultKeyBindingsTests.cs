// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="LinearRangeViewBase{TOption,TValue}.DefaultKeyBindings"/> static property.
/// </summary>
public class LinearRangeDefaultKeyBindingsTests
{
    [Fact]
    public void LinearRange_DefaultKeyBindings_IsNotNull () => Assert.NotNull (LinearSelector<object>.DefaultKeyBindings);

    [Fact]
    public void LinearRange_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in LinearSelector<object>.DefaultKeyBindings!)
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
    public void LinearRange_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in LinearSelector<object>.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void LinearRange_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        // DefaultKeyBindings is declared on the abstract base LinearRangeViewBase<TOption, TValue>.
        PropertyInfo? property =
            typeof (LinearRangeViewBase<object, object>).GetProperty (
                                                                      nameof (LinearRangeViewBase<object, object>.DefaultKeyBindings),
                                                                      BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");

        Assert.Null (attr);
    }

    [Theory]
    [InlineData (Command.Accept)]
    [InlineData (Command.Activate)]
    public void LinearRange_DefaultKeyBindings_ContainsUniqueCommands (Command command) =>
        Assert.True (LinearSelector<object>.DefaultKeyBindings!.ContainsKey (command));
}
