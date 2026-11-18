namespace InputTests;

/// <summary>
///     Tests for <see cref="KeyBinding"/> record struct.
/// </summary>
/// <remarks>
///     Copilot generated.
/// </remarks>
public class KeyBindingTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithCommands_SetsProperties ()
    {
        Command [] commands = [Command.Activate, Command.Accept];

        KeyBinding binding = new (commands);

        Assert.Equal (commands, binding.Commands);
        Assert.Null (binding.Data);
        Assert.Null (binding.Target);
        Assert.Null (binding.Source);
        Assert.Null (binding.Key);
    }

    [Fact]
    public void Constructor_WithCommandsAndContext_SetsProperties ()
    {
        Command [] commands = [Command.Activate];
        object context = "test context";

        KeyBinding binding = new (commands, context);

        Assert.Equal (commands, binding.Commands);
        Assert.Equal (context, binding.Data);
        Assert.Null (binding.Target);
        Assert.Null (binding.Source);
    }

    [Fact]
    public void Constructor_WithCommandsAndTarget_SetsProperties ()
    {
        Command [] commands = [Command.Activate];
        View targetView = new () { Id = "targetView" };

        KeyBinding binding = new (commands, targetView);

        Assert.Equal (commands, binding.Commands);
        Assert.Equal (targetView, binding.Target);
        Assert.Null (binding.Data);
        Assert.Null (binding.Source);
    }

    [Fact]
    public void Constructor_WithCommandsTargetAndData_SetsProperties ()
    {
        Command [] commands = [Command.Activate];
        View targetView = new () { Id = "targetView" };
        object data = "test data";

        KeyBinding binding = new (commands, targetView, data);

        Assert.Equal (commands, binding.Commands);
        Assert.Equal (targetView, binding.Target);
        Assert.Equal (data, binding.Data);
        Assert.Null (binding.Source);
    }

    #endregion

    #region ICommandBinding Interface Tests

    //[Fact]
    //public void Commands_GetSet_Works ()
    //{
    //    KeyBinding binding = new ([Command.Activate]);
    //    Command [] newCommands = [Command.Accept, Command.HotKey];

    //    binding.Commands = newCommands;

    //    Assert.Equal (newCommands, binding.Commands);
    //}

    //[Fact]
    //public void Data_GetSet_Works ()
    //{
    //    KeyBinding binding = new ([Command.Activate]);
    //    object testData = "test data";

    //    binding.Data = testData;

    //    Assert.Equal (testData, binding.Data);
    //}

    //[Fact]
    //public void Source_GetSet_Works ()
    //{
    //    KeyBinding binding = new ([Command.Activate]);
    //    View sourceView = new () { Id = "sourceView" };

    //    binding.Source = sourceView;

    //    Assert.Equal (sourceView, binding.Source);
    //    Assert.Equal ("sourceView", binding.Source.Id);
    //}

    [Fact]
    public void Source_DefaultsToNull ()
    {
        KeyBinding binding = new ([Command.Activate]);

        Assert.Null (binding.Source);
    }

    #endregion

    #region Target Property Tests (KeyBinding-specific)

    [Fact]
    public void Target_GetSet_Works ()
    {
        KeyBinding binding = new ([Command.Activate]);
        View targetView = new () { Id = "targetView" };

        binding.Target = targetView;

        Assert.Equal (targetView, binding.Target);
    }

    [Fact]
    public void Target_And_Source_CanBeDifferent ()
    {
        View sourceView = new () { Id = "sourceView" };
        View targetView = new () { Id = "targetView" };

        KeyBinding binding = new ([Command.HotKey]) { Source = new WeakReference<View> (sourceView), Target = targetView };

        View? sv = null;
        Assert.True (binding.Source?.TryGetTarget (out sv) == true);
        Assert.Equal (sourceView, sv);
        Assert.Equal (targetView, binding.Target);
        Assert.NotSame (sv, binding.Target);
    }

    [Fact]
    public void Target_And_Source_CanBeSame ()
    {
        View view = new () { Id = "sameView" };

        KeyBinding binding = new ([Command.Activate]) { Source = new WeakReference<View> (view), Target = view };

        View? sv = null;
        Assert.True (binding.Source?.TryGetTarget (out sv) == true);
        Assert.Equal (view, sv);
        Assert.Equal (view, binding.Target);
        Assert.Same (sv, binding.Target);
    }

    #endregion

    #region Key Property Tests

    [Fact]
    public void Key_GetSet_Works ()
    {
        KeyBinding binding = new ([Command.Activate]);

        binding.Key = Key.A;

        Assert.Equal (Key.A, binding.Key);
    }

    [Fact]
    public void Key_WithModifiers_Works ()
    {
        KeyBinding binding = new ([Command.Activate]) { Key = Key.A.WithCtrl.WithShift };

        Assert.Equal (Key.A.WithCtrl.WithShift, binding.Key);
    }

    [Fact]
    public void Key_DefaultsToNull ()
    {
        KeyBinding binding = new ([Command.Activate]);

        Assert.Null (binding.Key);
    }

    #endregion

    #region Record Struct Equality Tests

    [Fact]
    public void Equality_SameValues_AreEqual ()
    {
        Command [] commands = [Command.Activate];

        KeyBinding binding1 = new (commands) { Key = Key.A };
        KeyBinding binding2 = new (commands) { Key = Key.A };

        Assert.Equal (binding1.Commands, binding2.Commands);
        Assert.Equal (binding1.Key, binding2.Key);
    }

    [Fact]
    public void Equality_DifferentCommands_AreNotEqual ()
    {
        KeyBinding binding1 = new ([Command.Activate]) { Key = Key.A };
        KeyBinding binding2 = new ([Command.Accept]) { Key = Key.A };

        Assert.NotEqual (binding1.Commands, binding2.Commands);
    }

    [Fact]
    public void Equality_DifferentKeys_AreNotEqual ()
    {
        KeyBinding binding1 = new ([Command.Activate]) { Key = Key.A };
        KeyBinding binding2 = new ([Command.Activate]) { Key = Key.B };

        Assert.NotEqual (binding1.Key, binding2.Key);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void PatternMatching_AsICommandBinding_Works ()
    {
        ICommandBinding binding = new KeyBinding ([Command.Activate]) { Key = Key.Enter };

        Assert.True (binding is KeyBinding);

        if (binding is KeyBinding keyBinding)
        {
            Assert.Equal (Key.Enter, keyBinding.Key);
        }
    }

    [Fact]
    public void PatternMatching_Key_Works ()
    {
        KeyBinding binding = new ([Command.Activate]) { Key = Key.F5, Source = new WeakReference<View> (new View { Id = "sourceView" }), Target = new View { Id = "targetView" } };

        // Pattern matching on Key property
        if (binding is { Key: { } key, Source: { } source, Target: { } target })
        {
            Assert.Equal (Key.F5, key);
            Assert.True (source.TryGetTarget (out View? sv));
            Assert.Equal ("sourceView", sv?.Id);
            Assert.Equal ("targetView", target.Id);
        }
        else
        {
            Assert.Fail ("Pattern matching should have succeeded");
        }
    }

    [Fact]
    public void PatternMatching_WithKeyComparison_Works ()
    {
        KeyBinding binding = new ([Command.HotKey]) { Key = Key.A.WithCtrl };

        // Pattern used in actual code
        if (binding is { Key: { } key } && key == Key.A.WithCtrl)
        {
            Assert.True (true);
        }
        else
        {
            Assert.Fail ("Pattern matching with key comparison should have succeeded");
        }
    }

    #endregion
}
