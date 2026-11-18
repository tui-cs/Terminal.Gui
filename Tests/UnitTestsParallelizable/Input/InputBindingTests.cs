namespace InputTests;

/// <summary>
///     Tests for <see cref="CommandBinding"/> record struct.
/// </summary>
/// <remarks>
///     Copilot generated.
/// </remarks>
public class CommandBindingTests
{
    #region Constructor Tests

    [Fact ]
    public void Constructor_WithCommands_SetsCommands ()
    {
        Command [] commands = [Command.Activate, Command.Accept];

        CommandBinding binding = new (commands);

        Assert.Equal (commands, binding.Commands);
        Assert.Null (binding.Source);
        Assert.Null (binding.Data);
    }

    [Fact ]
    public void Constructor_WithCommandsAndSource_SetsBothProperties ()
    {
        Command [] commands = [Command.Activate];
        View source = new () { Id = "sourceView" };

        CommandBinding binding = new (commands, source);

        Assert.Equal (commands, binding.Commands);
        View? sv = null;
        Assert.True (binding.Source?.TryGetTarget (out sv) == true);
        Assert.Equal (source, sv);
        Assert.Null (binding.Data);
    }

    [Fact ]
    public void Constructor_WithAllParameters_SetsAllProperties ()
    {
        Command [] commands = [Command.Accept];
        View source = new () { Id = "sourceView" };
        object data = "test data";

        CommandBinding binding = new (commands, source, data);

        Assert.Equal (commands, binding.Commands);
        View? sv = null;
        Assert.True (binding.Source?.TryGetTarget (out sv) == true);
        Assert.Equal (source, sv);
        Assert.Equal ("test data", binding.Data);
    }

    #endregion

    #region Property Tests

    //[Fact ]
    //public void Commands_CanBeModified ()
    //{
    //    CommandBinding binding = new ([Command.Activate]);

    //    binding.Commands = [Command.Accept, Command.Cancel];

    //    Assert.Equal (2, binding.Commands.Length);
    //    Assert.Equal (Command.Accept, binding.Commands [0]);
    //    Assert.Equal (Command.Cancel, binding.Commands [1]);
    //}

    //[Fact ]
    //public void Source_CanBeModified ()
    //{
    //    CommandBinding binding = new ([Command.Activate]);
    //    View source = new () { Id = "newSource" };

    //    binding.Source = source;

    //    Assert.Equal ("newSource", binding.Source?.Id);
    //}

    //[Fact ]
    //public void Data_CanBeModified ()
    //{
    //    CommandBinding binding = new ([Command.Activate]);

    //    binding.Data = 42;

    //    Assert.Equal (42, binding.Data);
    //}

    #endregion

    #region ICommandBinding Interface Tests

    [Fact ]
    public void ImplementsICommandBinding ()
    {
        CommandBinding binding = new ([Command.Activate]) { Source = new WeakReference<View> (new View { Id = "test" }), Data = "data" };

        ICommandBinding iBinding = binding;

        Assert.Equal (binding.Commands, iBinding.Commands);
        Assert.Same (binding.Source, iBinding.Source);
        Assert.Equal (binding.Data, iBinding.Data);
    }

    [Fact ]
    public void CanBeUsedPolymorphically ()
    {
        ICommandBinding binding = new CommandBinding ([Command.Accept], new View { Id = "polymorphic" });

        Assert.Single (binding.Commands);
        Assert.Equal (Command.Accept, binding.Commands [0]);
        View? sv = null;
        Assert.True (binding.Source?.TryGetTarget (out sv) == true);
        Assert.Equal ("polymorphic", sv?.Id);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact ]
    public void PatternMatching_CanDistinguishFromKeyBinding ()
    {
        ICommandBinding inputBinding = new CommandBinding ([Command.Activate]);
        ICommandBinding keyBinding = new KeyBinding ([Command.Activate]) { Key = Key.Enter };

        Assert.True (inputBinding is CommandBinding);
        Assert.False (inputBinding is KeyBinding);
        Assert.True (keyBinding is KeyBinding);
        Assert.False (keyBinding is CommandBinding);
    }

    [Fact ]
    public void PatternMatching_CanDistinguishFromMouseBinding ()
    {
        ICommandBinding inputBinding = new CommandBinding ([Command.Activate]);
        ICommandBinding mouseBinding = new MouseBinding ([Command.Activate], MouseFlags.LeftButtonClicked);

        Assert.True (inputBinding is CommandBinding);
        Assert.False (inputBinding is MouseBinding);
        Assert.True (mouseBinding is MouseBinding);
        Assert.False (mouseBinding is CommandBinding);
    }

    [Fact ]
    public void PatternMatching_SwitchExpression_Works ()
    {
        ICommandBinding binding = new CommandBinding ([Command.Accept], new View { Id = "source" });

        string bindingType = binding switch
                             {
                                 KeyBinding => "key",
                                 MouseBinding => "mouse",
                                 CommandBinding => "input",
                                 _ => "unknown"
                             };

        Assert.Equal ("input", bindingType);
    }

    #endregion

    #region CommandContext Integration Tests

    [Fact ]
    public void CanBeUsedWithCommandContext ()
    {
        View source = new () { Id = "contextSource" };
        CommandBinding binding = new ([Command.Activate], source, "contextData");

        CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (source), Binding = binding };

        Assert.Equal (Command.Activate, ctx.Command);
        View? ctxSource = null;
        ctx.Source?.TryGetTarget (out ctxSource);
        Assert.Equal (source, ctxSource);
        Assert.NotNull (ctx.Binding);

        if (ctx.Binding is CommandBinding ib)
        {
            Assert.Equal ("contextData", ib.Data);
        }
        else
        {
            Assert.Fail ("Binding should be CommandBinding");
        }
    }

    [Fact ]
    public void Binding_Property_ReturnsICommandBinding ()
    {
        CommandBinding binding = new ([Command.Accept]);

        CommandContext ctx = new () { Command = Command.Accept, Binding = binding };

        // Binding property (from ICommandContext) returns ICommandBinding
        Assert.NotNull (ctx.Binding);
        Assert.IsType<CommandBinding> (ctx.Binding);
    }

    [Fact ]
    public void PatternMatching_ThroughICommandContext_Works ()
    {
        CommandBinding binding = new ([Command.Accept], new View { Id = "test" });
        ICommandContext ctx = new CommandContext { Command = Command.Accept, Binding = binding };

        // Can pattern match the binding from the interface
        // Can pattern match the binding from the interface
        if (ctx.Binding is CommandBinding ib)
        {
            View? sv = null;
            Assert.True (ib.Source?.TryGetTarget (out sv) == true);
            Assert.Equal ("test", sv?.Id);
        }
        else
        {
            Assert.Fail ("Should be able to pattern match CommandBinding from ICommandContext.Binding");
        }
    }

    #endregion

    #region Record Equality Tests

    [Fact ]
    // Claude - Opus 4.6
    public void RecordEquality_SameValues_AreEqual ()
    {
        Command [] commands = [Command.Activate];
        View source = new () { Id = "source" };
        WeakReference<View> weakSource = new (source);

        // Share the same WeakReference instance — two distinct WeakReferences to the same target
        // are not reference-equal, so record equality requires the same instance.
        CommandBinding binding1 = new () { Commands = commands, Source = weakSource, Data = "data" };
        CommandBinding binding2 = new () { Commands = commands, Source = weakSource, Data = "data" };

        Assert.Equal (binding1, binding2);
    }

    [Fact ]
    public void RecordEquality_DifferentCommands_AreNotEqual ()
    {
        CommandBinding binding1 = new ([Command.Activate]);
        CommandBinding binding2 = new ([Command.Accept]);

        Assert.NotEqual (binding1, binding2);
    }

    [Fact ]
    public void RecordEquality_DifferentSources_AreNotEqual ()
    {
        CommandBinding binding1 = new ([Command.Activate], new View { Id = "source1" });
        CommandBinding binding2 = new ([Command.Activate], new View { Id = "source2" });

        Assert.NotEqual (binding1, binding2);
    }

    #endregion
}
