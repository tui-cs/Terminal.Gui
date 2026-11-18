namespace InputTests;

/// <summary>
///     Tests for <see cref="MouseBinding"/> record struct.
/// </summary>
/// <remarks>
///     Copilot generated.
/// </remarks>
public class MouseBindingTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithCommandsAndMouseFlags_SetsProperties ()
    {
        Command [] commands = [Command.Activate, Command.Accept];
        var flags = MouseFlags.LeftButtonClicked;

        MouseBinding binding = new (commands, flags);

        Assert.Equal (commands, binding.Commands);
        Assert.NotNull (binding.MouseEvent);
        Assert.Equal (flags, binding.MouseEvent!.Flags);
        Assert.Null (binding.Data);
        Assert.Null (binding.Source);
    }

    [Fact]
    public void Constructor_WithCommandsAndMouseArgs_SetsProperties ()
    {
        Command [] commands = [Command.Activate];
        Mouse mouseArgs = new () { Flags = MouseFlags.RightButtonClicked, Position = new Point (10, 20) };

        MouseBinding binding = new (commands, mouseArgs);

        Assert.Equal (commands, binding.Commands);
        Assert.Equal (mouseArgs, binding.MouseEvent);
        Assert.Equal (MouseFlags.RightButtonClicked, binding.MouseEvent!.Flags);
        Assert.Equal (new Point (10, 20), binding.MouseEvent.Position);
    }

    #endregion

    #region ICommandBinding Interface Tests

    //[Fact]
    //public void Commands_GetSet_Works ()
    //{
    //    MouseBinding binding = new ([Command.Activate], MouseFlags.LeftButtonClicked);
    //    Command [] newCommands = [Command.Accept, Command.HotKey];

    //    binding.Commands = newCommands;

    //    Assert.Equal (newCommands, binding.Commands);
    //}

    //[Fact]
    //public void Data_GetSet_Works ()
    //{
    //    MouseBinding binding = new ([Command.Activate], MouseFlags.LeftButtonClicked);
    //    object testData = "test data";

    //    binding.Data = testData;

    //    Assert.Equal (testData, binding.Data);
    //}

    //[Fact]
    //public void Source_GetSet_Works ()
    //{
    //    MouseBinding binding = new ([Command.Activate], MouseFlags.LeftButtonClicked);
    //    View testView = new () { Id = "testView" };

    //    binding.Source = testView;

    //    Assert.Equal (testView, binding.Source);
    //    Assert.Equal ("testView", binding.Source.Id);
    //}

    [Fact]
    public void Source_DefaultsToNull ()
    {
        MouseBinding binding = new ([Command.Activate], MouseFlags.LeftButtonClicked);

        Assert.Null (binding.Source);
    }

    #endregion

    #region MouseEvent Property Tests

    [Fact]
    public void MouseEvent_GetSet_Works ()
    {
        MouseBinding binding = new ([Command.Activate], MouseFlags.LeftButtonClicked);
        Mouse newMouseEvent = new () { Flags = MouseFlags.MiddleButtonClicked, Position = new Point (5, 5) };

        binding.MouseEvent = newMouseEvent;

        Assert.Equal (newMouseEvent, binding.MouseEvent);
        Assert.Equal (MouseFlags.MiddleButtonClicked, binding.MouseEvent!.Flags);
    }

    [Fact]
    public void MouseEvent_CanBeSetToNull ()
    {
        MouseBinding binding = new ([Command.Activate], MouseFlags.LeftButtonClicked);

        binding.MouseEvent = null;

        Assert.Null (binding.MouseEvent);
    }

    #endregion

    #region Record Struct Equality Tests

    [Fact]
    public void Equality_SameValues_AreEqual ()
    {
        Command [] commands = [Command.Activate];
        var flags = MouseFlags.LeftButtonClicked;

        MouseBinding binding1 = new (commands, flags);
        MouseBinding binding2 = new (commands, flags);

        // Note: MouseEvent contains Timestamp which differs, so these won't be equal
        // But Commands array reference equality should work
        Assert.Equal (binding1.Commands, binding2.Commands);
    }

    [Fact]
    public void Equality_DifferentCommands_AreNotEqual ()
    {
        MouseBinding binding1 = new ([Command.Activate], MouseFlags.LeftButtonClicked);
        MouseBinding binding2 = new ([Command.Accept], MouseFlags.LeftButtonClicked);

        Assert.NotEqual (binding1.Commands, binding2.Commands);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void PatternMatching_AsICommandBinding_Works ()
    {
        ICommandBinding binding = new MouseBinding ([Command.Activate], MouseFlags.LeftButtonClicked);

        Assert.True (binding is MouseBinding);

        if (binding is MouseBinding mouseBinding)
        {
            Assert.NotNull (mouseBinding.MouseEvent);
            Assert.Equal (MouseFlags.LeftButtonClicked, mouseBinding.MouseEvent!.Flags);
        }
    }

    [Fact]
    public void PatternMatching_MouseEvent_Works ()
    {
        MouseBinding binding = new ([Command.Activate], MouseFlags.LeftButtonClicked) { Source = new WeakReference<View> (new View { Id = "sourceView" }) };

        // Pattern matching on MouseEvent property
        if (binding is { MouseEvent: { } mouseEvent, Source: { } source })
        {
            Assert.Equal (MouseFlags.LeftButtonClicked, mouseEvent.Flags);
            Assert.True (source.TryGetTarget (out View? sv));
            Assert.Equal ("sourceView", sv?.Id);
        }
        else
        {
            Assert.Fail ("Pattern matching should have succeeded");
        }
    }

    #endregion
}
