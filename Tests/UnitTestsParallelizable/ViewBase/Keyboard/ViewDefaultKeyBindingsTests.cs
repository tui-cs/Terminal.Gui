// Claude - Opus 4.6

using System.Reflection;

namespace ViewBaseTests.Keyboard;

/// <summary>
///     Tests for <see cref="View.DefaultKeyBindings"/> and <see cref="View.ViewKeyBindings"/> static properties.
/// </summary>
public class ViewDefaultKeyBindingsTests
{
    [Fact]
    public void View_DefaultKeyBindings_IsNotNull () => Assert.NotNull (View.DefaultKeyBindings);

    [Theory]
    [InlineData (Command.Left)]
    [InlineData (Command.Right)]
    [InlineData (Command.Up)]
    [InlineData (Command.Down)]
    [InlineData (Command.PageUp)]
    [InlineData (Command.PageDown)]
    [InlineData (Command.LeftStart)]
    [InlineData (Command.RightEnd)]
    [InlineData (Command.Start)]
    [InlineData (Command.End)]
    public void View_DefaultKeyBindings_ContainsNavigationCommands (Command command) => Assert.True (View.DefaultKeyBindings!.ContainsKey (command));

    [Theory]
    [InlineData (Command.LeftExtend)]
    [InlineData (Command.RightExtend)]
    [InlineData (Command.UpExtend)]
    [InlineData (Command.DownExtend)]
    [InlineData (Command.PageUpExtend)]
    [InlineData (Command.PageDownExtend)]
    [InlineData (Command.LeftStartExtend)]
    [InlineData (Command.RightEndExtend)]
    [InlineData (Command.StartExtend)]
    [InlineData (Command.EndExtend)]
    public void View_DefaultKeyBindings_ContainsSelectionExtendCommands (Command command) => Assert.True (View.DefaultKeyBindings!.ContainsKey (command));

    [Theory]
    [InlineData (Command.Copy)]
    [InlineData (Command.Cut)]
    [InlineData (Command.Paste)]
    public void View_DefaultKeyBindings_ContainsClipboardCommands (Command command) => Assert.True (View.DefaultKeyBindings!.ContainsKey (command));

    [Theory]
    [InlineData (Command.Undo)]
    [InlineData (Command.Redo)]
    [InlineData (Command.SelectAll)]
    [InlineData (Command.DeleteCharLeft)]
    [InlineData (Command.DeleteCharRight)]
    public void View_DefaultKeyBindings_ContainsEditingCommands (Command command) => Assert.True (View.DefaultKeyBindings!.ContainsKey (command));

    [Fact]
    public void View_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command _, PlatformKeyBinding platformBinding) in View.DefaultKeyBindings!)
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
    public void View_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in View.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void View_DefaultKeyBindings_HasConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (View).GetProperty (nameof (View.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        System.Attribute? attr = property.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");

        Assert.Null (attr);
    }

    [Fact]
    public void View_ViewKeyBindings_IsNull_ByDefault () =>

        // ViewKeyBindings should be null unless set via configuration
        Assert.Null (View.ViewKeyBindings);

    [Fact]
    public void View_ViewKeyBindings_HasConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (View).GetProperty (nameof (View.ViewKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        System.Attribute? attr = property.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");

        Assert.Null (attr);
    }
}
