namespace InputTests;

public class MouseBindingsTests
{
    [Fact]
    public void Add_Adds ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] commands = [Command.Right, Command.Left];

        var flags = MouseFlags.AllEvents;
        mouseBindings.Add (flags, commands);
        MouseBinding binding = mouseBindings.Get (flags);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        binding = mouseBindings.Get (flags);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        Command [] resultCommands = mouseBindings.GetCommands (flags);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void Add_Invalid_Flag_Throws ()
    {
        var mouseBindings = new MouseBindings ();
        List<Command> commands = new ();
        Assert.Throws<ArgumentException> (() => mouseBindings.Add (MouseFlags.None, Command.Accept));
    }

    [Fact]
    public void Add_Multiple_Commands_Adds ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] commands = [Command.Right, Command.Left];

        mouseBindings.Add (MouseFlags.LeftButtonClicked, commands);
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.LeftButtonClicked);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);

        mouseBindings.Add (MouseFlags.MiddleButtonClicked, commands);
        resultCommands = mouseBindings.GetCommands (MouseFlags.MiddleButtonClicked);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void Add_No_Commands_Throws ()
    {
        var mouseBindings = new MouseBindings ();
        List<Command> commands = new ();
        Assert.Throws<ArgumentException> (() => mouseBindings.Add (MouseFlags.LeftButtonClicked, commands.ToArray ()));
    }

    [Fact]
    public void Add_Single_Command_Adds ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.LeftButtonClicked);
        Assert.Contains (Command.HotKey, resultCommands);

        mouseBindings.Add (MouseFlags.MiddleButtonClicked, Command.HotKey);
        resultCommands = mouseBindings.GetCommands (MouseFlags.MiddleButtonClicked);
        Assert.Contains (Command.HotKey, resultCommands);
    }

    // Add should not allow duplicates
    [Fact]
    public void Add_Throws_If_Exists ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);
        Assert.Throws<InvalidOperationException> (() => mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Accept));

        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.LeftButtonClicked);
        Assert.Contains (Command.HotKey, resultCommands);

        mouseBindings = new ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);
        Assert.Throws<InvalidOperationException> (() => mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Accept));

        resultCommands = mouseBindings.GetCommands (MouseFlags.LeftButtonClicked);
        Assert.Contains (Command.HotKey, resultCommands);

        mouseBindings = new ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);
        Assert.Throws<InvalidOperationException> (() => mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Accept));

        resultCommands = mouseBindings.GetCommands (MouseFlags.LeftButtonClicked);
        Assert.Contains (Command.HotKey, resultCommands);

        mouseBindings = new ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Accept);
        Assert.Throws<InvalidOperationException> (() => mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.ScrollDown));

        resultCommands = mouseBindings.GetCommands (MouseFlags.LeftButtonClicked);
        Assert.Contains (Command.Accept, resultCommands);

        mouseBindings = new ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, new MouseBinding ([Command.HotKey], MouseFlags.LeftButtonClicked));

        Assert.Throws<InvalidOperationException> (
                                                  () => mouseBindings.Add (
                                                                           MouseFlags.LeftButtonClicked,
                                                                           new MouseBinding ([Command.Accept], MouseFlags.LeftButtonClicked)));

        resultCommands = mouseBindings.GetCommands (MouseFlags.LeftButtonClicked);
        Assert.Contains (Command.HotKey, resultCommands);
    }

    // Clear
    [Fact]
    public void Clear_Clears ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);
        mouseBindings.Clear ();
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.LeftButtonClicked);
        Assert.Empty (resultCommands);
        resultCommands = mouseBindings.GetCommands (MouseFlags.LeftButtonClicked);
        Assert.Empty (resultCommands);
    }

    [Fact]
    public void Defaults ()
    {
        var mouseBindings = new MouseBindings ();
        Assert.Empty (mouseBindings.GetBindings ());
        Assert.Equal (MouseFlags.None, mouseBindings.GetFirstFromCommands (Command.Accept));
    }

    [Fact]
    public void Get_Binding_Not_Found_Throws ()
    {
        var mouseBindings = new MouseBindings ();
        Assert.Throws<InvalidOperationException> (() => mouseBindings.Get (MouseFlags.LeftButtonClicked));
        Assert.Throws<InvalidOperationException> (() => mouseBindings.Get (MouseFlags.AllEvents));
    }

    // GetCommands
    [Fact]
    public void GetCommands_Unknown_ReturnsEmpty ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.LeftButtonClicked);
        Assert.Empty (resultCommands);
    }

    [Fact]
    public void GetCommands_WithCommands_ReturnsCommands ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.LeftButtonClicked);
        Assert.Contains (Command.HotKey, resultCommands);
    }

    [Fact]
    public void GetCommands_WithMultipleBindings_ReturnsCommands ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] commands = [Command.Right, Command.Left];
        mouseBindings.Add (MouseFlags.LeftButtonClicked, commands);
        mouseBindings.Add (MouseFlags.MiddleButtonClicked, commands);
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.LeftButtonClicked);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
        resultCommands = mouseBindings.GetCommands (MouseFlags.MiddleButtonClicked);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void GetCommands_WithMultipleCommands_ReturnsCommands ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] commands = [Command.Right, Command.Left];
        mouseBindings.Add (MouseFlags.LeftButtonClicked, commands);
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.LeftButtonClicked);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void GetMouseFlagsFromCommands_MultipleCommands ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] commands1 = [Command.Right, Command.Left];
        mouseBindings.Add (MouseFlags.LeftButtonClicked, commands1);

        Command [] commands2 = { Command.Up, Command.Down };
        mouseBindings.Add (MouseFlags.MiddleButtonClicked, commands2);

        MouseFlags mouseFlags = mouseBindings.GetFirstFromCommands (commands1);
        Assert.Equal (MouseFlags.LeftButtonClicked, mouseFlags);

        mouseFlags = mouseBindings.GetFirstFromCommands (commands2);
        Assert.Equal (MouseFlags.MiddleButtonClicked, mouseFlags);
    }

    [Fact]
    public void GetMouseFlagsFromCommands_OneCommand ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Right);

        MouseFlags mouseFlags = mouseBindings.GetFirstFromCommands (Command.Right);
        Assert.Equal (MouseFlags.LeftButtonClicked, mouseFlags);
    }

    // GetMouseFlagsFromCommands
    [Fact]
    public void GetMouseFlagsFromCommands_Unknown_Returns_Key_Empty ()
    {
        var mouseBindings = new MouseBindings ();
        Assert.Equal (MouseFlags.None, mouseBindings.GetFirstFromCommands (Command.Accept));
    }

    [Fact]
    public void GetMouseFlagsFromCommands_WithCommands_ReturnsKey ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);
        MouseFlags mouseFlags = mouseBindings.GetFirstFromCommands (Command.HotKey);
        Assert.Equal (MouseFlags.LeftButtonClicked, mouseFlags);
    }

    [Fact]
    public void ReplaceMouseFlags_Replaces ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);
        mouseBindings.Add (MouseFlags.MiddleButtonClicked, Command.HotKey);
        mouseBindings.Add (MouseFlags.RightButtonClicked, Command.HotKey);
        mouseBindings.Add (MouseFlags.Button4Clicked, Command.HotKey);

        mouseBindings.Replace (MouseFlags.LeftButtonClicked, MouseFlags.LeftButtonDoubleClicked);
        Assert.Empty (mouseBindings.GetCommands (MouseFlags.LeftButtonClicked));
        Assert.Contains (Command.HotKey, mouseBindings.GetCommands (MouseFlags.LeftButtonDoubleClicked));

        mouseBindings.Replace (MouseFlags.MiddleButtonClicked, MouseFlags.MiddleButtonDoubleClicked);
        Assert.Empty (mouseBindings.GetCommands (MouseFlags.MiddleButtonClicked));
        Assert.Contains (Command.HotKey, mouseBindings.GetCommands (MouseFlags.MiddleButtonDoubleClicked));

        mouseBindings.Replace (MouseFlags.RightButtonClicked, MouseFlags.RightButtonDoubleClicked);
        Assert.Empty (mouseBindings.GetCommands (MouseFlags.RightButtonClicked));
        Assert.Contains (Command.HotKey, mouseBindings.GetCommands (MouseFlags.RightButtonDoubleClicked));

        mouseBindings.Replace (MouseFlags.Button4Clicked, MouseFlags.Button4DoubleClicked);
        Assert.Empty (mouseBindings.GetCommands (MouseFlags.Button4Clicked));
        Assert.Contains (Command.HotKey, mouseBindings.GetCommands (MouseFlags.Button4DoubleClicked));
    }

    [Fact]
    public void ReplaceMouseFlags_Replaces_Leaves_Old_Binding ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Accept);
        mouseBindings.Add (MouseFlags.MiddleButtonClicked, Command.HotKey);

        mouseBindings.Replace (mouseBindings.GetFirstFromCommands (Command.Accept), MouseFlags.RightButtonClicked);
        Assert.Empty (mouseBindings.GetCommands (MouseFlags.LeftButtonClicked));
        Assert.Contains (Command.Accept, mouseBindings.GetCommands (MouseFlags.RightButtonClicked));
    }

    [Fact]
    public void ReplaceMouseFlags_Adds_If_DoesNotContain_Old ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Replace (MouseFlags.LeftButtonClicked, MouseFlags.MiddleButtonClicked);
        Assert.True (mouseBindings.TryGet (MouseFlags.MiddleButtonClicked, out _));
    }

    [Fact]
    public void ReplaceMouseFlags_Throws_If_New_Is_None ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);
        Assert.Throws<ArgumentException> (() => mouseBindings.Replace (MouseFlags.LeftButtonClicked, MouseFlags.None));
    }

    [Fact]
    public void Get_Gets ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] commands = [Command.Right, Command.Left];

        mouseBindings.Add (MouseFlags.LeftButtonClicked, commands);
        MouseBinding binding = mouseBindings.Get (MouseFlags.LeftButtonClicked);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        binding = mouseBindings.Get (MouseFlags.LeftButtonClicked);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);
    }

    // TryGet
    [Fact]
    public void TryGet_Succeeds ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);
        bool result = mouseBindings.TryGet (MouseFlags.LeftButtonClicked, out MouseBinding _);
        Assert.True (result);
        ;
    }

    [Fact]
    public void TryGet_Unknown_ReturnsFalse ()
    {
        var mouseBindings = new MouseBindings ();
        bool result = mouseBindings.TryGet (MouseFlags.LeftButtonClicked, out MouseBinding _);
        Assert.False (result);
    }

    [Fact]
    public void TryGet_WithCommands_ReturnsTrue ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);
        bool result = mouseBindings.TryGet (MouseFlags.LeftButtonClicked, out MouseBinding bindings);
        Assert.True (result);
        Assert.Contains (Command.HotKey, bindings.Commands);
    }

    [Fact]
    public void ReplaceCommands_Replaces ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Accept);

        mouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked, Command.Refresh);

        bool result = mouseBindings.TryGet (MouseFlags.LeftButtonClicked, out MouseBinding bindings);
        Assert.True (result);
        Assert.Contains (Command.Refresh, bindings.Commands);
    }
}
