// Copilot

using System.Reflection;

namespace ApplicationTests.Keyboard;

/// <summary>
///     Tests for <see cref="Application.DefaultKeyBindings"/> static property.
/// </summary>
[Collection ("Application Tests")]
public class ApplicationDefaultKeyBindingsTests
{
    [Fact]
    public void Application_DefaultKeyBindings_IsNotNull () => Assert.NotNull (Application.DefaultKeyBindings);

    [Fact]
    public void Application_DefaultKeyBindings_ContainsQuit ()
    {
        Dictionary<Command, PlatformKeyBinding>? bindings = Application.DefaultKeyBindings;
        Assert.NotNull (bindings);
        Assert.True (bindings.ContainsKey (Command.Quit));

        PlatformKeyBinding quit = bindings [Command.Quit];
        Assert.NotNull (quit.All);
        Assert.Contains (Key.Esc, quit.All!);
    }

    [Fact]
    public void Application_DefaultKeyBindings_SuspendIsNonWindows ()
    {
        Dictionary<Command, PlatformKeyBinding>? bindings = Application.DefaultKeyBindings;
        Assert.NotNull (bindings);
        Assert.True (bindings.ContainsKey (Command.Suspend));

        PlatformKeyBinding suspend = bindings [Command.Suspend];
        Assert.Null (suspend.All);
        Assert.NotNull (suspend.Linux);
        Assert.Contains (Key.Z.WithCtrl, suspend.Linux!);
        Assert.NotNull (suspend.Macos);
        Assert.Contains (Key.Z.WithCtrl, suspend.Macos!);
    }

    [Fact]
    public void Application_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        Dictionary<Command, PlatformKeyBinding>? bindings = Application.DefaultKeyBindings;
        Assert.NotNull (bindings);

        foreach (KeyValuePair<Command, PlatformKeyBinding> entry in bindings)
        {
            Command command = entry.Key;
            PlatformKeyBinding binding = entry.Value;

            AssertKeysValid (binding.All, command, "All");
            AssertKeysValid (binding.Windows, command, "Windows");
            AssertKeysValid (binding.Linux, command, "Linux");
            AssertKeysValid (binding.Macos, command, "Macos");
        }
    }

    [Fact]
    public void Application_DefaultKeyBindings_HasConfigurationPropertyAttribute ()
    {
        PropertyInfo? propertyInfo = typeof (Application).GetProperty ("DefaultKeyBindings", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (propertyInfo);

        System.Attribute? attr = propertyInfo.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "ConfigurationPropertyAttribute");
        Assert.Null (attr);
    }

    [Fact]
    public void Application_DefaultKeyBindings_ContainsExpectedCommands ()
    {
        Dictionary<Command, PlatformKeyBinding>? bindings = Application.DefaultKeyBindings;
        Assert.NotNull (bindings);

        Command [] expectedCommands = [Command.Quit, Command.Suspend, Command.Arrange, Command.NextTabStop, Command.PreviousTabStop, Command.NextTabGroup, Command.PreviousTabGroup, Command.Refresh];

        foreach (Command command in expectedCommands)
        {
            Assert.True (bindings.ContainsKey (command), $"Expected command '{command}' not found in DefaultKeyBindings.");
        }

        Assert.Equal (expectedCommands.Length, bindings.Count);
    }

    private static void AssertKeysValid (Key []? keys, Command command, string platformName)
    {
        if (keys is null)
        {
            return;
        }

        foreach (Key key in keys)
        {
            Assert.NotEqual (Key.Empty, key);
        }
    }
}
