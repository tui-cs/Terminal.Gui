// Claude - Opus 4.6

namespace ViewBaseTests.Keyboard;

/// <summary>
///     Tests for <see cref="View.ApplyKeyBindings"/>.
/// </summary>
public class ApplyKeyBindingsTests
{
    /// <summary>
    ///     A test view that exposes <see cref="View.ApplyKeyBindings"/> publicly and supports configurable commands.
    /// </summary>
    private class TestView : View
    {
        public TestView (params Command [] supportedCommands)
        {
            foreach (Command cmd in supportedCommands)
            {
                AddCommand (cmd, _ => true);
            }
        }

        public void CallApplyKeyBindings (params Dictionary<Command, PlatformKeyBinding>? [] layers) => ApplyKeyBindings (layers);
    }

    [Fact]
    public void ApplyKeyBindings_AllPlatform_BindsKey ()
    {
        TestView view = new (Command.Left);
        Dictionary<Command, PlatformKeyBinding> layer = new () { [Command.Left] = Bind.All (Key.CursorLeft) };

        view.CallApplyKeyBindings (layer);

        Assert.True (view.KeyBindings.TryGet (Key.CursorLeft, out KeyBinding binding));
        Assert.Contains (Command.Left, binding.Commands);
    }

    [Fact]
    public void ApplyKeyBindings_BindsSupportedCommand ()
    {
        TestView view = new (Command.Right);
        Dictionary<Command, PlatformKeyBinding> layer = new () { [Command.Right] = Bind.All (Key.CursorRight) };

        view.CallApplyKeyBindings (layer);

        Assert.True (view.KeyBindings.TryGet (Key.CursorRight, out _));
    }

    [Fact]
    public void ApplyKeyBindings_SkipsUnsupportedCommand ()
    {
        // View does NOT support Command.Left
        TestView view = new (Command.Right);
        Dictionary<Command, PlatformKeyBinding> layer = new () { [Command.Left] = Bind.All (Key.CursorLeft) };

        view.CallApplyKeyBindings (layer);

        Assert.False (view.KeyBindings.TryGet (Key.CursorLeft, out _));
    }

    [Fact]
    public void ApplyKeyBindings_MultipleLayers_Additive ()
    {
        TestView view = new (Command.Left, Command.Right);
        Dictionary<Command, PlatformKeyBinding> layer1 = new () { [Command.Left] = Bind.All (Key.CursorLeft) };
        Dictionary<Command, PlatformKeyBinding> layer2 = new () { [Command.Right] = Bind.All (Key.CursorRight) };

        view.CallApplyKeyBindings (layer1, layer2);

        Assert.True (view.KeyBindings.TryGet (Key.CursorLeft, out _));
        Assert.True (view.KeyBindings.TryGet (Key.CursorRight, out _));
    }

    [Fact]
    public void ApplyKeyBindings_NullLayer_Skipped ()
    {
        TestView view = new (Command.Left);
        Dictionary<Command, PlatformKeyBinding> validLayer = new () { [Command.Left] = Bind.All (Key.CursorLeft) };

        // Should not throw
        view.CallApplyKeyBindings (null, validLayer);

        Assert.True (view.KeyBindings.TryGet (Key.CursorLeft, out _));
    }

    [Fact]
    public void ApplyKeyBindings_InvalidCommandName_Skipped ()
    {
        TestView view = new (Command.Left);
        Dictionary<Command, PlatformKeyBinding> layer = new () { [(Command)9999] = Bind.All (Key.X) };

        // Should not throw
        view.CallApplyKeyBindings (layer);
    }

    [Fact]
    public void ApplyKeyBindings_EmptyKey_Skipped ()
    {
        TestView view = new (Command.Left);
        Dictionary<Command, PlatformKeyBinding> layer = new () { [Command.Left] = new PlatformKeyBinding { All = [Key.Empty] } };

        // Should not throw
        view.CallApplyKeyBindings (layer);

        // No key should have been bound for Command.Left beyond defaults
    }

    [Fact]
    public void ApplyKeyBindings_AlreadyBoundKey_NotOverwritten ()
    {
        TestView view = new (Command.Left, Command.Right);

        // Manually bind CursorLeft to Command.Right first
        view.KeyBindings.Add (Key.CursorLeft, Command.Right);

        Dictionary<Command, PlatformKeyBinding> layer = new () { [Command.Left] = Bind.All (Key.CursorLeft) };

        view.CallApplyKeyBindings (layer);

        // Should still be bound to Command.Right, not Command.Left
        Assert.True (view.KeyBindings.TryGet (Key.CursorLeft, out KeyBinding binding));
        Assert.Contains (Command.Right, binding.Commands);
        Assert.DoesNotContain (Command.Left, binding.Commands);
    }

    [Fact]
    public void ApplyKeyBindings_MultipleKeysPerCommand ()
    {
        TestView view = new (Command.Left);
        Dictionary<Command, PlatformKeyBinding> layer = new () { [Command.Left] = Bind.All (Key.CursorLeft, Key.B.WithCtrl) };

        view.CallApplyKeyBindings (layer);

        Assert.True (view.KeyBindings.TryGet (Key.CursorLeft, out _));
        Assert.True (view.KeyBindings.TryGet (Key.B.WithCtrl, out _));
    }

    [Fact]
    public void ApplyKeyBindings_EmptyDict_NoOp ()
    {
        TestView view = new (Command.Left);
        Dictionary<Command, PlatformKeyBinding> layer = new ();

        // Should not throw
        view.CallApplyKeyBindings (layer);
    }

    [Fact]
    public void ApplyKeyBindings_ViewKeyBindings_Null_NoOp ()
    {
        Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? saved = View.ViewKeyBindings;

        try
        {
            View.ViewKeyBindings = null;
            TestView view = new (Command.Left);

            // Should not throw
            view.CallApplyKeyBindings ();
        }
        finally
        {
            View.ViewKeyBindings = saved;
        }
    }

    [Fact]
    public void ApplyKeyBindings_ViewKeyBindings_NoEntryForType_NoOp ()
    {
        Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? saved = View.ViewKeyBindings;

        try
        {
            View.ViewKeyBindings = new Dictionary<string, Dictionary<Command, PlatformKeyBinding>>
            {
                ["SomeOtherView"] = new () { [Command.Left] = Bind.All (Key.CursorLeft) }
            };

            TestView view = new (Command.Left);

            // Should not throw; no binding for "TestView"
            view.CallApplyKeyBindings ();

            Assert.False (view.KeyBindings.TryGet (Key.CursorLeft, out _));
        }
        finally
        {
            View.ViewKeyBindings = saved;
        }
    }
}
