// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="TableView.DefaultKeyBindings"/> static property.
/// </summary>
public class TableViewDefaultKeyBindingsTests
{
    [Fact]
    public void TableView_DefaultKeyBindings_IsNotNull () => Assert.NotNull (TableView.DefaultKeyBindings);

    [Fact]
    public void TableView_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in TableView.DefaultKeyBindings!)
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
    public void TableView_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in TableView.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void TableView_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (TableView).GetProperty (nameof (TableView.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");

        Assert.Null (attr);
    }
}
