// Copilot

namespace ViewBaseTests;

/// <summary>
///     Verifies that <see cref="Command.Context"/> flows through the <see cref="Command.NotBound"/> path
///     in base <see cref="View"/> (since View has no default context behavior). This ensures
///     <see cref="View.CommandNotBound"/> fires for Command.Context on views that don't handle it.
/// </summary>
public class CommandContextNotBoundTests
{
    [Fact]
    public void Context_Without_Handler_Fires_CommandNotBound ()
    {
        View view = new () { Width = 10, Height = 1 };

        bool commandNotBoundFired = false;
        Command? notBoundCommand = null;

        view.CommandNotBound += (_, args) =>
        {
            commandNotBoundFired = true;
            notBoundCommand = args.Context?.Command;
        };

        // Command.Context should not be registered by default in base View,
        // so invoking it should route through NotBound handler.
        view.InvokeCommand (Command.Context);

        Assert.True (commandNotBoundFired, "CommandNotBound event should fire when Command.Context is invoked on a base View.");
        Assert.Equal (Command.Context, notBoundCommand);
    }

    [Fact]
    public void Context_Without_Handler_Returns_Null_Or_False_WhenNotHandled ()
    {
        View view = new () { Width = 10, Height = 1 };

        // With no subscriber to CommandNotBound, invoking an unbound command
        // should NOT return true (which would stop input processing).
        bool? result = view.InvokeCommand (Command.Context);

        Assert.NotEqual (true, result);
    }

    [Fact]
    public void Context_With_Custom_Handler_Does_Not_Fire_CommandNotBound ()
    {
        ViewWithContextHandler view = new ();

        bool commandNotBoundFired = false;
        view.CommandNotBound += (_, _) => { commandNotBoundFired = true; };

        view.InvokeCommand (Command.Context);

        Assert.True (view.ContextHandlerCalled);
        Assert.False (commandNotBoundFired, "CommandNotBound should NOT fire when Command.Context has a registered handler.");
    }

    /// <summary>Helper view that registers a Command.Context handler.</summary>
    private class ViewWithContextHandler : View
    {
        public bool ContextHandlerCalled { get; private set; }

        public ViewWithContextHandler ()
        {
            Width = 10;
            Height = 1;
            AddCommand (Command.Context, _ =>
            {
                ContextHandlerCalled = true;

                return true;
            });
        }
    }
}
