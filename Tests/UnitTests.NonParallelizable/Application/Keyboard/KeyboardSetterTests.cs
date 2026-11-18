// Tests that mutate Application.DefaultKeyBindings (static state).
// These MUST NOT be in UnitTestsParallelizable.

namespace UnitTests.NonParallelizable.ApplicationTests.Keyboard;

public class KeyboardSetterTests
{
    [Fact]
    public void QuitKey_SetValue_UpdatesKeyBindings ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.Quit];
        var keyboard = new ApplicationKeyboard ();
        Key newQuitKey = Key.Q.WithCtrl;

        try
        {
            // Act — use SetDefaultKeyBinding which fires the event automatically
            Application.SetDefaultKeyBinding (Command.Quit, Bind.All (newQuitKey));

            // Assert
            Assert.Equal (newQuitKey, Application.GetDefaultKey (Command.Quit));
            Assert.True (keyboard.KeyBindings.TryGet (newQuitKey, out KeyBinding binding));
            Assert.Contains (Command.Quit, binding.Commands);
        }
        finally
        {
            Application.SetDefaultKeyBinding (Command.Quit, original);
            keyboard.Dispose ();
        }
    }

    [Fact]
    public void NextTabKey_Setter_UpdatesBindings ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.NextTabStop];
        var keyboard = new ApplicationKeyboard ();
        Key newKey = Key.N.WithCtrl;

        try
        {
            // Act
            Application.SetDefaultKeyBinding (Command.NextTabStop, Bind.All (newKey));

            // Assert
            Assert.Equal (newKey, Application.GetDefaultKey (Command.NextTabStop));
            Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
            Assert.Contains (Command.NextTabStop, binding.Commands);
        }
        finally
        {
            Application.SetDefaultKeyBinding (Command.NextTabStop, original);
            keyboard.Dispose ();
        }
    }

    [Fact]
    public void PrevTabKey_Setter_UpdatesBindings ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.PreviousTabStop];
        var keyboard = new ApplicationKeyboard ();
        Key newKey = Key.P.WithCtrl;

        try
        {
            // Act
            Application.SetDefaultKeyBinding (Command.PreviousTabStop, Bind.All (newKey));

            // Assert
            Assert.Equal (newKey, Application.GetDefaultKey (Command.PreviousTabStop));
            Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
            Assert.Contains (Command.PreviousTabStop, binding.Commands);
        }
        finally
        {
            Application.SetDefaultKeyBinding (Command.PreviousTabStop, original);
            keyboard.Dispose ();
        }
    }

    [Fact]
    public void NextTabGroupKey_Setter_UpdatesBindings ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.NextTabGroup];
        var keyboard = new ApplicationKeyboard ();
        Key newKey = Key.PageDown.WithCtrl;

        try
        {
            // Act
            Application.SetDefaultKeyBinding (Command.NextTabGroup, Bind.All (newKey));

            // Assert
            Assert.Equal (newKey, Application.GetDefaultKey (Command.NextTabGroup));
            Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
            Assert.Contains (Command.NextTabGroup, binding.Commands);
        }
        finally
        {
            Application.SetDefaultKeyBinding (Command.NextTabGroup, original);
            keyboard.Dispose ();
        }
    }

    [Fact]
    public void PrevTabGroupKey_Setter_UpdatesBindings ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.PreviousTabGroup];
        var keyboard = new ApplicationKeyboard ();
        Key newKey = Key.PageUp.WithCtrl;

        try
        {
            // Act
            Application.SetDefaultKeyBinding (Command.PreviousTabGroup, Bind.All (newKey));

            // Assert
            Assert.Equal (newKey, Application.GetDefaultKey (Command.PreviousTabGroup));
            Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
            Assert.Contains (Command.PreviousTabGroup, binding.Commands);
        }
        finally
        {
            Application.SetDefaultKeyBinding (Command.PreviousTabGroup, original);
            keyboard.Dispose ();
        }
    }

    [Fact]
    public void ArrangeKey_Setter_UpdatesBindings ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.Arrange];
        var keyboard = new ApplicationKeyboard ();
        Key newKey = Key.A.WithCtrl;

        try
        {
            // Act
            Application.SetDefaultKeyBinding (Command.Arrange, Bind.All (newKey));

            // Assert
            Assert.Equal (newKey, Application.GetDefaultKey (Command.Arrange));
            Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
            Assert.Contains (Command.Arrange, binding.Commands);
        }
        finally
        {
            Application.SetDefaultKeyBinding (Command.Arrange, original);
            keyboard.Dispose ();
        }
    }

    // Copilot
    [Fact]
    public void SetDefaultKeyBinding_FiresChangedEvent ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.Quit];
        var eventFired = false;
        EventHandler handler = (_, _) => eventFired = true;
        Application.DefaultKeyBindingsChanged += handler;

        try
        {
            // Act
            Application.SetDefaultKeyBinding (Command.Quit, Bind.All (Key.Q.WithCtrl));

            // Assert
            Assert.True (eventFired);
        }
        finally
        {
            Application.DefaultKeyBindingsChanged -= handler;
            Application.SetDefaultKeyBinding (Command.Quit, original);
        }
    }

    // Copilot
    [Fact]
    public void RemoveDefaultKeyBinding_RemovesAndFiresEvent ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.Quit];
        var eventFired = false;
        EventHandler handler = (_, _) => eventFired = true;
        Application.DefaultKeyBindingsChanged += handler;

        try
        {
            // Act
            bool removed = Application.RemoveDefaultKeyBinding (Command.Quit);

            // Assert
            Assert.True (removed);
            Assert.True (eventFired);
            Assert.False (Application.DefaultKeyBindings!.ContainsKey (Command.Quit));
            Assert.Equal (Key.Empty, Application.GetDefaultKey (Command.Quit));
        }
        finally
        {
            Application.DefaultKeyBindingsChanged -= handler;
            Application.SetDefaultKeyBinding (Command.Quit, original);
        }
    }

    // Copilot
    [Fact]
    public void RemoveDefaultKeyBinding_NonExistentCommand_ReturnsFalse ()
    {
        // Act
        bool removed = Application.RemoveDefaultKeyBinding ((Command)9999);

        // Assert
        Assert.False (removed);
    }

    // Copilot
    [Fact]
    public void SetDefaultKeyBinding_NullDictionary_Throws ()
    {
        // Arrange
        Dictionary<Command, PlatformKeyBinding> original = Application.DefaultKeyBindings!;

        try
        {
            Application.DefaultKeyBindings = null;

            // Act
            Assert.Throws<InvalidOperationException> (() => Application.SetDefaultKeyBinding (Command.Quit, Bind.All (Key.Q.WithCtrl)));
        }
        finally
        {
            Application.DefaultKeyBindings = original;
        }
    }
}
