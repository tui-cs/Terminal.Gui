using System.Runtime.InteropServices;

namespace ApplicationTests.Keyboard;

/// <summary>
///     Parallelizable tests for keyboard handling.
///     These tests use isolated instances of <see cref="IKeyboard"/> to avoid static state dependencies.
/// </summary>
[Collection ("Application Tests")]
public class KeyboardTests
{
    [Fact]
    public void Init_CreatesKeybindings ()
    {
        IApplication app = Application.Create ();

        app.Keyboard.KeyBindings.Clear ();

        Assert.Empty (app.Keyboard.KeyBindings.GetBindings ());

        app.Init (DriverRegistry.Names.ANSI);

        Assert.NotEmpty (app.Keyboard.KeyBindings.GetBindings ());

        app.Dispose ();
    }

    [Fact]
    public void Constructor_InitializesKeyBindings ()
    {
        // Arrange & Act
        var keyboard = new ApplicationKeyboard ();

        // Assert
        Assert.NotNull (keyboard.KeyBindings);

        // Verify that some default bindings exist
        Assert.True (keyboard.KeyBindings.TryGet (Application.GetDefaultKey (Command.Quit), out _));
    }

    [Fact]
    public void QuitKey_DefaultValue_IsEsc ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();

        // Assert
        Assert.Equal (Key.Esc, Application.GetDefaultKey (Command.Quit));
    }

    [Fact]
    public void ArrangeKey_DefaultValue_IsCtrlF5 ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();

        // Assert
        Assert.Equal (Key.F5.WithCtrl, Application.GetDefaultKey (Command.Arrange));
        keyboard.Dispose ();
    }

    [Fact]
    public void NextTabKey_DefaultValue_IsTab ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();

        // Assert
        Assert.Equal (Key.Tab, Application.GetDefaultKey (Command.NextTabStop));
        keyboard.Dispose ();
    }

    [Fact]
    public void PrevTabKey_DefaultValue_IsShiftTab ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();

        // Assert
        Assert.Equal (Key.Tab.WithShift, Application.GetDefaultKey (Command.PreviousTabStop));
        keyboard.Dispose ();
    }

    [Fact]
    public void NextTabGroupKey_DefaultValue_IsF6 ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();

        // Assert
        Assert.Equal (Key.F6, Application.GetDefaultKey (Command.NextTabGroup));
        keyboard.Dispose ();
    }

    [Fact]
    public void PrevTabGroupKey_DefaultValue_IsShiftF6 ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();

        // Assert
        Assert.Equal (Key.F6.WithShift, Application.GetDefaultKey (Command.PreviousTabGroup));
        keyboard.Dispose ();
    }

    [Fact]
    public void KeyBindings_Add_CanAddCustomBinding ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();
        Key customKey = Key.K.WithCtrl;

        // Act
        keyboard.KeyBindings.Add (customKey, Command.Accept);

        // Assert
        Assert.True (keyboard.KeyBindings.TryGet (customKey, out KeyBinding binding));
        Assert.Contains (Command.Accept, binding.Commands);
    }

    [Fact]
    public void KeyBindings_Remove_CanRemoveBinding ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();
        Key customKey = Key.K.WithCtrl;
        keyboard.KeyBindings.Add (customKey, Command.Accept);

        // Act
        keyboard.KeyBindings.Remove (customKey);

        // Assert
        Assert.False (keyboard.KeyBindings.TryGet (customKey, out _));
    }

    [Fact]
    public void KeyDown_Event_CanBeSubscribed ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();
        var eventRaised = false;

        // Act
        keyboard.KeyDown += (sender, key) => { eventRaised = true; };

        // Assert - event subscription doesn't throw
        Assert.False (eventRaised); // Event hasn't been raised yet
    }

    [Fact]
    public void InvokeCommand_WithInvalidCommand_ThrowsNotSupportedException ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();

        // Pick a command that isn't registered
        var invalidCommand = (Command)9999;
        Key testKey = Key.A;
        var binding = new KeyBinding ([invalidCommand]);

        // Act & Assert
        Assert.Throws<NotSupportedException> (() => keyboard.InvokeCommand (invalidCommand, testKey, binding));
    }

    [Fact]
    public void Multiple_Keyboards_CanExistIndependently ()
    {
        // Arrange & Act
        var keyboard1 = new ApplicationKeyboard ();
        var keyboard2 = new ApplicationKeyboard ();

        // Both keyboards should have the same default Quit binding from Application.DefaultKeyBindings
        Key defaultQuitKey = Application.GetDefaultKey (Command.Quit);
        Assert.True (keyboard1.KeyBindings.TryGet (defaultQuitKey, out _));
        Assert.True (keyboard2.KeyBindings.TryGet (defaultQuitKey, out _));

        // Each keyboard maintains independent custom bindings
        keyboard1.KeyBindings.Add (Key.X, Command.Accept);
        Assert.True (keyboard1.KeyBindings.TryGet (Key.X, out _));
        Assert.False (keyboard2.KeyBindings.TryGet (Key.X, out _));

        keyboard1.Dispose ();
        keyboard2.Dispose ();
    }

    [Fact]
    public void KeyBindings_Replace_UpdatesExistingBinding ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();
        Key oldKey = Key.Esc;
        Key newKey = Key.Q.WithCtrl;

        // Verify initial state
        Assert.True (keyboard.KeyBindings.TryGet (oldKey, out KeyBinding oldBinding));
        Assert.Contains (Command.Quit, oldBinding.Commands);

        // Act
        keyboard.KeyBindings.Replace (oldKey, newKey);

        // Assert - old key should no longer have the binding
        Assert.False (keyboard.KeyBindings.TryGet (oldKey, out _));

        // New key should have the binding
        Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding newBinding));
        Assert.Contains (Command.Quit, newBinding.Commands);
    }

    [Fact]
    public void KeyBindings_Clear_RemovesAllBindings ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();

        // Verify initial state has bindings
        Assert.True (keyboard.KeyBindings.TryGet (Application.GetDefaultKey (Command.Quit), out _));

        // Act
        keyboard.KeyBindings.Clear ();

        // Assert - previously existing binding is gone
        Assert.False (keyboard.KeyBindings.TryGet (Application.GetDefaultKey (Command.Quit), out _));
    }

    [Fact]
    public void AddKeyBindings_PopulatesDefaultBindings ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();
        keyboard.KeyBindings.Clear ();
        Assert.False (keyboard.KeyBindings.TryGet (Application.GetDefaultKey (Command.Quit), out _));

        // Act
        keyboard.AddKeyBindings ();

        // Assert
        Assert.True (keyboard.KeyBindings.TryGet (Application.GetDefaultKey (Command.Quit), out KeyBinding binding));
        Assert.Contains (Command.Quit, binding.Commands);
    }

    // Migrated from UnitTests/Application/KeyboardTests.cs

    [Fact]
    public void KeyBindings_Add_Adds ()
    {
        // Arrange — Dispose immediately to unsubscribe from DefaultKeyBindingsChanged,
        // preventing interference from parallel tests modifying Application.DefaultKeyBindings.
        var keyboard = new ApplicationKeyboard ();
        keyboard.Dispose ();

        // Re-init bindings manually without event subscription
        keyboard.KeyBindings.Clear ();

        // Act
        keyboard.KeyBindings.Add (Key.A, Command.Accept);
        keyboard.KeyBindings.Add (Key.B, Command.Accept);

        // Assert
        Assert.True (keyboard.KeyBindings.TryGet (Key.A, out KeyBinding binding));
        Assert.Null (binding.Target);
        Assert.True (keyboard.KeyBindings.TryGet (Key.B, out binding));
        Assert.Null (binding.Target);
    }

    [Fact]
    public void KeyBindings_Remove_Removes ()
    {
        // Arrange — dispose immediately to detach from DefaultKeyBindingsChanged,
        // preventing interference from parallel tests modifying Application.DefaultKeyBindings.
        var keyboard = new ApplicationKeyboard ();
        keyboard.Dispose ();

        // Re-init bindings manually without event subscription
        keyboard.KeyBindings.Clear ();
        keyboard.KeyBindings.Add (Key.A, Command.Accept);
        Assert.True (keyboard.KeyBindings.TryGet (Key.A, out _));

        // Act
        keyboard.KeyBindings.Remove (Key.A);

        // Assert
        Assert.False (keyboard.KeyBindings.TryGet (Key.A, out _));
    }

    [Fact]
    public void QuitKey_Default_Is_Esc ()
    {
        // Arrange & Act
        var keyboard = new ApplicationKeyboard ();

        // Assert
        Assert.Equal (Key.Esc, Application.GetDefaultKey (Command.Quit));
    }

    // Setter tests that mutate Application.DefaultKeyBindings have been moved to
    // Tests/UnitTests/Application/Keyboard/KeyboardSetterTests.cs

    [Fact]
    public void KeyBindings_AddWithTarget_StoresTarget ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();
        var view = new View ();

        // Act
        keyboard.KeyBindings.AddApp (Key.A.WithCtrl, view, Command.Accept);

        // Assert
        Assert.True (keyboard.KeyBindings.TryGet (Key.A.WithCtrl, out KeyBinding binding));
        Assert.Equal (view, binding.Target);
        Assert.Contains (Command.Accept, binding.Commands);

        view.Dispose ();
    }

    [Fact]
    public void InvokeCommandsBoundToKey_ReturnsNull_WhenNoBindingExists ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();
        Key unboundKey = Key.Z.WithAlt.WithCtrl;

        // Act
        bool? result = keyboard.InvokeCommandsBoundToKey (unboundKey);

        // Assert
        Assert.Null (result);
    }

    [Fact]
    public void InvokeCommandsBoundToKey_InvokesCommand_WhenBindingExists ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();

        // QuitKey has a bound command by default

        // Act
        bool? result = keyboard.InvokeCommandsBoundToKey (Application.GetDefaultKey (Command.Quit));

        // Assert
        // Command.Quit would normally call Application.RequestStop, 
        // but in isolation it should return true (handled)
        Assert.NotNull (result);
    }

    [Fact]
    public void Multiple_Keyboards_Independent_KeyBindings ()
    {
        // Arrange
        var keyboard1 = new ApplicationKeyboard ();
        var keyboard2 = new ApplicationKeyboard ();

        // Act
        keyboard1.KeyBindings.Add (Key.X, Command.Accept);
        keyboard2.KeyBindings.Add (Key.Y, Command.Cancel);

        // Assert
        Assert.True (keyboard1.KeyBindings.TryGet (Key.X, out _));
        Assert.False (keyboard1.KeyBindings.TryGet (Key.Y, out _));

        Assert.True (keyboard2.KeyBindings.TryGet (Key.Y, out _));
        Assert.False (keyboard2.KeyBindings.TryGet (Key.X, out _));
    }

    [Fact]
    public void KeyBindings_Replace_PreservesCommandsForNewKey ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();
        Key oldKey = Key.Esc;
        Key newKey = Key.Q.WithCtrl;

        // Get the commands from the old binding
        Assert.True (keyboard.KeyBindings.TryGet (oldKey, out KeyBinding oldBinding));
        Command [] oldCommands = oldBinding.Commands.ToArray ();

        // Act
        keyboard.KeyBindings.Replace (oldKey, newKey);

        // Assert - new key should have the same commands
        Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding newBinding));
        Assert.Equal (oldCommands, newBinding.Commands);
    }

    [Fact]
    public void InvokeCommandsBoundToKey_Suspend_ReturnsNotNull ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();
        Key key = Key.Z.WithCtrl;

        // Act
        bool? result = keyboard.InvokeCommandsBoundToKey (key);

        // Assert — Suspend is NonWindows only, so on Windows this key is unbound
        if (OperatingSystem.IsWindows ())
        {
            Assert.Null (result);
        }
        else
        {
            Assert.True (result);
        }
    }
}
