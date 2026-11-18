namespace InputTests;

public class KeyBindingsTests
{
    [Fact]
    public void Add_Adds ()
    {
        var keyBindings = new KeyBindings ();
        Command [] commands = { Command.Right, Command.Left };

        var key = new Key (Key.A);
        keyBindings.Add (Key.A, commands);
        KeyBinding binding = keyBindings.Get (key);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        binding = keyBindings.Get (key);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        Command [] resultCommands = keyBindings.GetCommands (key);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    // Claude - Opus 4.7
    [Fact]
    public void Add_PreservesKeyOnTheBinding ()
    {
        KeyBindings bindings = new ();
        Key f1 = new (KeyCode.F1);

        bindings.Add (f1, Command.Accept);
        bool found = bindings.TryGet (f1, out KeyBinding binding);

        Assert.True (found);
        Assert.Equal (f1, binding.Key);
        Assert.Contains (Command.Accept, binding.Commands);
    }

    [Fact]
    public void Add_Invalid_Key_Throws ()
    {
        var keyBindings = new KeyBindings ();
        List<Command> commands = new ();
        Assert.Throws<ArgumentException> (() => keyBindings.Add (Key.Empty, Command.Accept));
    }

    [Fact]
    public void Add_Multiple_Commands_Adds ()
    {
        var keyBindings = new KeyBindings ();
        Command [] commands = [Command.Right, Command.Left];

        keyBindings.Add (Key.A, commands);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);

        keyBindings.Add (Key.B, commands);
        resultCommands = keyBindings.GetCommands (Key.B);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void Add_No_Commands_Throws ()
    {
        var keyBindings = new KeyBindings ();
        List<Command> commands = new ();
        Assert.Throws<ArgumentException> (() => keyBindings.Add (Key.A, commands.ToArray ()));
    }

    [Fact]
    public void Add_Single_Command_Adds ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.HotKey);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.HotKey, resultCommands);

        keyBindings.Add (Key.B, Command.HotKey);
        resultCommands = keyBindings.GetCommands (Key.B);
        Assert.Contains (Command.HotKey, resultCommands);
    }

    // Add should not allow duplicates
    [Fact]
    public void Add_Throws_If_Exists ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.HotKey);
        Assert.Throws<InvalidOperationException> (() => keyBindings.Add (Key.A, Command.Accept));

        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.HotKey, resultCommands);

        keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.HotKey);
        Assert.Throws<InvalidOperationException> (() => keyBindings.Add (Key.A, Command.Accept));

        resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.HotKey, resultCommands);

        keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.HotKey);
        Assert.Throws<InvalidOperationException> (() => keyBindings.Add (Key.A, Command.Accept));

        resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.HotKey, resultCommands);

        keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.Accept);
        Assert.Throws<InvalidOperationException> (() => keyBindings.Add (Key.A, Command.ScrollDown));

        resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.Accept, resultCommands);

        keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, new KeyBinding ([Command.HotKey]));
        Assert.Throws<InvalidOperationException> (() => keyBindings.Add (Key.A, new KeyBinding (new [] { Command.Accept })));

        resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.HotKey, resultCommands);
    }

    // Clear
    [Fact]
    public void Clear_Clears ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.B, Command.HotKey);
        keyBindings.Clear ();
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Empty (resultCommands);
        resultCommands = keyBindings.GetCommands (Key.B);
        Assert.Empty (resultCommands);
    }

    [Fact]
    public void Defaults ()
    {
        var keyBindings = new KeyBindings ();
        Assert.Empty (keyBindings.GetBindings ());
        Assert.Null (keyBindings.GetFirstFromCommands (Command.Accept));
    }

    [Fact]
    public void Get_Binding_Not_Found_Throws ()
    {
        var keyBindings = new KeyBindings ();
        Assert.Throws<InvalidOperationException> (() => keyBindings.Get (Key.A));
        Assert.Throws<InvalidOperationException> (() => keyBindings.Get (Key.B));
    }

    [Fact]
    public void Get_Gets ()
    {
        var keyBindings = new KeyBindings ();
        Command [] commands = [Command.Right, Command.Left];

        var key = new Key (Key.A);
        keyBindings.Add (key, commands);
        KeyBinding binding = keyBindings.Get (key);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        binding = keyBindings.Get (key);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);
    }

    // GetCommands
    [Fact]
    public void GetCommands_Unknown_ReturnsEmpty ()
    {
        var keyBindings = new KeyBindings ();
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Empty (resultCommands);
    }

    [Fact]
    public void GetCommands_WithCommands_ReturnsCommands ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.HotKey);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.HotKey, resultCommands);
    }

    [Fact]
    public void GetCommands_WithMultipleBindings_ReturnsCommands ()
    {
        var keyBindings = new KeyBindings ();
        Command [] commands = { Command.Right, Command.Left };
        keyBindings.Add (Key.A, commands);
        keyBindings.Add (Key.B, commands);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
        resultCommands = keyBindings.GetCommands (Key.B);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void GetCommands_WithMultipleCommands_ReturnsCommands ()
    {
        var keyBindings = new KeyBindings ();
        Command [] commands = { Command.Right, Command.Left };
        keyBindings.Add (Key.A, commands);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void GetKeyFromCommands_MultipleCommands ()
    {
        var keyBindings = new KeyBindings ();
        Command [] commands1 = { Command.Right, Command.Left };
        keyBindings.Add (Key.A, commands1);

        Command [] commands2 = { Command.Up, Command.Down };
        keyBindings.Add (Key.B, commands2);

        Key? key = keyBindings.GetFirstFromCommands (commands1);
        Assert.Equal (Key.A, key);

        key = keyBindings.GetFirstFromCommands (commands2);
        Assert.Equal (Key.B, key);
    }

    [Fact]
    public void GetKeyFromCommands_OneCommand ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.Right);

        Key? key = keyBindings.GetFirstFromCommands (Command.Right);
        Assert.Equal (Key.A, key);
    }

    // GetKeyFromCommands
    [Fact]
    public void GetKeyFromCommands_Unknown_Returns_Key_Empty ()
    {
        var keyBindings = new KeyBindings ();
        Assert.Null (keyBindings.GetFirstFromCommands (Command.Accept));
    }

    [Fact]
    public void GetKeyFromCommands_WithCommands_ReturnsKey ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.HotKey);
        Key? resultKey = keyBindings.GetFirstFromCommands (Command.HotKey);
        Assert.Equal (Key.A, resultKey);
    }

    [Fact]
    public void ReplaceCommands_Replaces ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.Accept);

        keyBindings.ReplaceCommands (Key.A, Command.Refresh);

        bool result = keyBindings.TryGet (Key.A, out KeyBinding bindings);
        Assert.True (result);
        Assert.Contains (Command.Refresh, bindings.Commands);
    }

    [Fact]
    public void ReplaceKey_Adds_If_DoesNotContain_Old ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Replace (Key.A, Key.B);
        Assert.True (keyBindings.TryGet (Key.B, out _));
    }

    [Fact]
    public void ReplaceKey_Replaces ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.HotKey);
        keyBindings.Add (Key.B, Command.HotKey);
        keyBindings.Add (Key.C, Command.HotKey);
        keyBindings.Add (Key.D, Command.HotKey);

        keyBindings.Replace (Key.A, Key.E);
        Assert.Empty (keyBindings.GetCommands (Key.A));
        Assert.Contains (Command.HotKey, keyBindings.GetCommands (Key.E));

        keyBindings.Replace (Key.B, Key.F);
        Assert.Empty (keyBindings.GetCommands (Key.B));
        Assert.Contains (Command.HotKey, keyBindings.GetCommands (Key.F));

        keyBindings.Replace (Key.C, Key.G);
        Assert.Empty (keyBindings.GetCommands (Key.C));
        Assert.Contains (Command.HotKey, keyBindings.GetCommands (Key.G));

        keyBindings.Replace (Key.D, Key.H);
        Assert.Empty (keyBindings.GetCommands (Key.D));
        Assert.Contains (Command.HotKey, keyBindings.GetCommands (Key.H));
    }

    [Fact]
    public void ReplaceKey_Replaces_Leaves_Old_Binding ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.Accept);
        keyBindings.Add (Key.B, Command.HotKey);

        keyBindings.Replace (keyBindings.GetFirstFromCommands (Command.Accept)!, Key.C);
        Assert.Empty (keyBindings.GetCommands (Key.A));
        Assert.Contains (Command.Accept, keyBindings.GetCommands (Key.C));
    }

    [Fact]
    public void ReplaceKey_Throws_If_New_Is_Empty ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.HotKey);
        Assert.Throws<ArgumentException> (() => keyBindings.Replace (Key.A, Key.Empty));
    }

    // TryGet
    [Fact]
    public void TryGet_Succeeds ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.Q.WithCtrl, Command.HotKey);
        var key = new Key (Key.Q.WithCtrl);
        bool result = keyBindings.TryGet (key, out KeyBinding _);
        Assert.True (result);
        ;
    }

    [Fact]
    public void TryGet_Unknown_ReturnsFalse ()
    {
        var keyBindings = new KeyBindings ();
        bool result = keyBindings.TryGet (Key.A, out KeyBinding _);
        Assert.False (result);
    }

    [Fact]
    public void TryGet_WithCommands_ReturnsTrue ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.HotKey);
        bool result = keyBindings.TryGet (Key.A, out KeyBinding bindings);
        Assert.True (result);
        Assert.Contains (Command.HotKey, bindings.Commands);
    }
}
