using Microsoft.Extensions.Logging;
using UnitTests.Parallelizable;
using Terminal.Gui.Tracing;
using UnitTests;

namespace ViewBaseTests.Commands;

/// <summary>
///     Tests for command bubbling through view hierarchies via <see cref="View.CommandsToBubbleUp"/>,
///     including dispatch-down, bridge cancellation, and unbound command bubbling.
/// </summary>
public class CommandBubblingTests (ITestOutputHelper output)
{
    #region Command Propagation Tests

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_DefaultIsEmpty ()
    {
        View view = new ();
        Assert.Equal ([], view.CommandsToBubbleUp);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void Accept_Command_DoesNotBubbleByDefault ()
    {
        View superView = new ();
        View subView = new ();
        superView.Add (subView);

        var superViewAcceptingCalledCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCalledCount++;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (0, superViewAcceptingCalledCount);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void Activate_Command_DoesNotBubbleByDefault ()
    {
        View superView = new ();
        View subView = new ();
        superView.Add (subView);

        var superViewActivatingCalledCount = 0;
        superView.Activating += (_, _) => superViewActivatingCalledCount++;

        subView.InvokeCommand (Command.Activate);

        Assert.Equal (0, superViewActivatingCalledCount);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_CanDisableAllPropagation ()
    {
        View superView = new () { CommandsToBubbleUp = [] };
        View subView = new ();
        superView.Add (subView);

        var superViewAcceptingCalledCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCalledCount++;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (0, superViewAcceptingCalledCount);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_CanBeCustomized ()
    {
        View superView = new () { CommandsToBubbleUp = [Command.Accept, Command.Activate] };
        View subView = new ();
        superView.Add (subView);

        var superViewActivatingCalledCount = 0;
        superView.Activating += (_, _) => superViewActivatingCalledCount++;

        subView.InvokeCommand (Command.Activate);

        Assert.Equal (1, superViewActivatingCalledCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Activate_BubblingUp_Fires_Activated_On_SuperView ()
    {
        View superView = new () { CommandsToBubbleUp = [Command.Activate] };
        View subView = new ();
        superView.Add (subView);

        var activatedCount = 0;
        superView.Activated += (_, _) => activatedCount++;

        subView.InvokeCommand (Command.Activate);

        Assert.Equal (1, activatedCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Activate_BubblingUp_Fires_Activated_In_Deep_Hierarchy ()
    {
        View grandSuperView = new () { CommandsToBubbleUp = [Command.Activate] };
        View superView = new () { CommandsToBubbleUp = [Command.Activate] };
        View subView = new ();
        grandSuperView.Add (superView);
        superView.Add (subView);

        var grandActivatedCount = 0;
        grandSuperView.Activated += (_, _) => grandActivatedCount++;

        subView.InvokeCommand (Command.Activate);

        Assert.Equal (1, grandActivatedCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void ConsumeDispatch_Blocks_Further_Bubbling ()
    {
        // OptionSelector uses ConsumeDispatch=true — activation should NOT
        // propagate from its inner CheckBox to OptionSelector's SuperView
        View superView = new () { CommandsToBubbleUp = [Command.Activate] };
        OptionSelector selector = new () { Labels = ["Option1", "Option2"] };
        superView.Add (selector);

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // Activate an inner CheckBox with a binding (required for dispatch to occur)
        CheckBox innerCb = selector.SubViews.OfType<CheckBox> ().First ();
        KeyBinding binding = new ([Command.Activate], Key.Space, innerCb);
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (innerCb), binding);
        innerCb.InvokeCommand (Command.Activate, ctx);

        // Consume-dispatch blocks propagation to SuperView
        Assert.Equal (0, superViewActivatingCount);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_StopsWhenHandled ()
    {
        View superView = new () { CommandsToBubbleUp = [Command.Accept] };
        View subView = new ();
        superView.Add (subView);

        var superViewAcceptingCalledCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCalledCount++;

        // SubView handles the command
        subView.Accepting += (_, e) => e.Handled = true;

        subView.InvokeCommand (Command.Accept);

        // Should NOT propagate because subView handled it
        Assert.Equal (0, superViewAcceptingCalledCount);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_WorksInDeepHierarchy ()
    {
        View grandSuperView = new () { CommandsToBubbleUp = [Command.Accept] };
        View superView = new () { CommandsToBubbleUp = [Command.Accept] };
        View subView = new ();

        grandSuperView.Add (superView);
        superView.Add (subView);

        var grandSuperViewAcceptingCalledCount = 0;
        grandSuperView.Accepting += (_, _) => grandSuperViewAcceptingCalledCount++;

        var grandSuperViewAcceptedCalledCount = 0;
        grandSuperView.Accepted += (_, _) => grandSuperViewAcceptedCalledCount++;

        subView.InvokeCommand (Command.Accept);

        // Should propagate all the way up
        Assert.Equal (1, grandSuperViewAcceptingCalledCount);
        Assert.Equal (1, grandSuperViewAcceptedCalledCount);
    }

    // Claude - Sonnet 4.5
    [Fact]
    public void CommandsToBubbleUp_StopsAtIntermediateHandler ()
    {
        View grandSuperView = new () { CommandsToBubbleUp = [Command.Accept] };
        View superView = new () { CommandsToBubbleUp = [Command.Accept] };
        View subView = new ();

        grandSuperView.Add (superView);
        superView.Add (subView);

        var grandSuperViewAcceptingCalledCount = 0;
        grandSuperView.Accepting += (_, _) => grandSuperViewAcceptingCalledCount++;

        // SuperView handles it, so shouldn't propagate further
        superView.Accepting += (_, e) => e.Handled = true;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (0, grandSuperViewAcceptingCalledCount);
    }


    #endregion Command Propagation Tests

    #region Accept Bubbling Tests

    // Accept on subview should bubble up to parent
    [Fact]
    public void Accept_Command_Bubbles_Up_To_SuperView ()
    {
        var view = new ViewEventTester { Id = "view" };
        view.CommandsToBubbleUp = [Command.Accept];
        var subview = new ViewEventTester { Id = "subview" };
        view.Add (subview);

        subview.InvokeCommand (Command.Accept);
        Assert.Equal (1, subview.OnAcceptedCount);
        Assert.Equal (1, view.OnAcceptedCount);

        subview.HandleOnAccepted = true;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (2, subview.OnAcceptedCount);
        Assert.Equal (1, view.OnAcceptedCount);

        subview.HandleOnAccepted = false;
        subview.HandleAccepted = true;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (3, subview.OnAcceptedCount);
        Assert.Equal (1, view.OnAcceptedCount);

        // Add a super view to test deeper hierarchy
        var superView = new ViewEventTester { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Accept];
        superView.Add (view);

        subview.InvokeCommand (Command.Accept);
        Assert.Equal (4, subview.OnAcceptedCount);
        Assert.Equal (1, view.OnAcceptedCount);
        Assert.Equal (0, superView.OnAcceptedCount);

        subview.HandleAccepted = false;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (5, subview.OnAcceptedCount);
        Assert.Equal (2, view.OnAcceptedCount);
        Assert.Equal (1, superView.OnAcceptedCount);

        view.HandleAccepted = true;
        subview.InvokeCommand (Command.Accept);
        Assert.Equal (6, subview.OnAcceptedCount);
        Assert.Equal (3, view.OnAcceptedCount);
        Assert.Equal (1, superView.OnAcceptedCount);
    }

    #endregion Accept Bubbling Tests

    #region DefaultAcceptView Bubbling Tests

    [Fact]
    public void DefaultAcceptView_Peer_Accept_Bubbles_To_DefaultAcceptView ()
    {
        View superView = new () { CanFocus = true };
        AcceptTargetTestView nonDefaultAcceptView = new () { IsDefault = false, Id = "nonDefaultAcceptView", CanFocus = true };
        AcceptTargetTestView defaultAcceptView = new () { IsDefault = true, Id = "defaultAcceptView", CanFocus = true };

        superView.Add (nonDefaultAcceptView);
        superView.Add (defaultAcceptView);
        superView.CommandsToBubbleUp = [Command.Accept];
        superView.DefaultAcceptView = defaultAcceptView;

        var defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;
        var defaultAcceptViewAcceptedCount = 0;
        defaultAcceptView.Accepted += (_, _) => defaultAcceptViewAcceptedCount++;

        var subViewAcceptingCount = 0;
        nonDefaultAcceptView.Accepting += (_, _) => subViewAcceptingCount++;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        var superViewAcceptedCount = 0;
        superView.Accepted += (_, _) => superViewAcceptedCount++;

        nonDefaultAcceptView.InvokeCommand (Command.Accept);

        Assert.Equal (1, superViewAcceptingCount);
        Assert.Equal (1, superViewAcceptedCount);
        Assert.Equal (1, subViewAcceptingCount);
        Assert.Equal (1, defaultAcceptViewAcceptingCount);
        Assert.Equal (1, defaultAcceptViewAcceptedCount);
    }

    [Fact]
    public void DefaultAcceptView_Non_IAcceptTarget_Peer_Accept_Bubbles_To_DefaultAcceptView ()
    {
        View superView = new () { CanFocus = true };
        View subView = new () { Id = "subView", CanFocus = true };
        AcceptTargetTestView defaultAcceptView = new () { IsDefault = true, Id = "defaultAcceptView", CanFocus = true };

        superView.Add (subView);
        superView.Add (defaultAcceptView);
        superView.CommandsToBubbleUp = [Command.Accept];
        superView.DefaultAcceptView = defaultAcceptView;

        var defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;

        var defaultAcceptViewAcceptedCount = 0;
        defaultAcceptView.Accepted += (_, _) => defaultAcceptViewAcceptedCount++;

        var subViewAcceptingCount = 0;
        subView.Accepting += (_, _) => subViewAcceptingCount++;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        var superViewAcceptedCount = 0;
        superView.Accepted += (_, _) => superViewAcceptedCount++;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (1, superViewAcceptingCount);
        Assert.Equal (1, superViewAcceptedCount);
        Assert.Equal (1, subViewAcceptingCount);
        Assert.Equal (1, defaultAcceptViewAcceptingCount);
        Assert.Equal (1, defaultAcceptViewAcceptedCount);
    }

    [Fact]
    public void DefaultAcceptView_Peer_IAcceptTarget_NonDefault_Accept_Bubbles_To_DefaultAcceptView ()
    {
        View superView = new () { CanFocus = true };
        AcceptTargetTestView subView = new () { IsDefault = false, Id = "subView", CanFocus = true };
        AcceptTargetTestView defaultAcceptView = new () { IsDefault = true, Id = "defaultAcceptView", CanFocus = true };

        superView.Add (subView);
        superView.Add (defaultAcceptView);
        superView.CommandsToBubbleUp = [Command.Accept];
        superView.DefaultAcceptView = defaultAcceptView;

        var defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;
        var defaultAcceptViewAcceptedCount = 0;
        defaultAcceptView.Accepted += (_, _) => defaultAcceptViewAcceptedCount++;

        var subViewAcceptingCount = 0;
        subView.Accepting += (_, _) => subViewAcceptingCount++;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (1, superViewAcceptingCount);
        Assert.Equal (1, subViewAcceptingCount);
        Assert.Equal (1, defaultAcceptViewAcceptingCount);
        Assert.Equal (1, defaultAcceptViewAcceptedCount);
    }

    [Fact]
    public void DefaultAcceptView_Accept_DoesNotBubble_To_DefaultAcceptView_WhenHandled ()
    {
        View superView = new () { CanFocus = true };
        View subView = new () { CanFocus = true };
        AcceptTargetTestView defaultAcceptView = new () { IsDefault = true, Id = "defaultAcceptView", CanFocus = true };

        superView.Add (subView);
        superView.Add (defaultAcceptView);

        subView.Accepting += (_, e) => e.Handled = true;

        var defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;

        subView.InvokeCommand (Command.Accept);

        Assert.Equal (0, defaultAcceptViewAcceptingCount);
    }

    // CoPilot - ChatGPT o1
    [Fact]
    public void NonIAcceptTarget_Without_CommandsToBubbleUp_DoesNotRedirect_To_DefaultAcceptView ()
    {
        View superView = new () { CanFocus = true };
        View nonAcceptTarget = new () { Id = "nonAcceptTarget", CanFocus = true };
        View defaultAcceptView = new () { Id = "defaultAcceptView", CanFocus = true };

        superView.Add (nonAcceptTarget);
        superView.Add (defaultAcceptView);
        superView.DefaultAcceptView = defaultAcceptView;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        var defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;

        // Non-IAcceptTarget should redirect to DefaultAcceptView
        nonAcceptTarget.InvokeCommand (Command.Accept);

        Assert.Equal (0, defaultAcceptViewAcceptingCount);
        Assert.Equal (0, superViewAcceptingCount);
    }

    #endregion DefaultAcceptView Bubbling Tests

    #region IAcceptTarget Bubbling Tests

    // CoPilot - ChatGPT o1
    [Fact]
    public void Button_BubblesUp_To_SuperView ()
    {
        View superView = new () { CanFocus = true };
        superView.CommandsToBubbleUp = [Command.Accept];

        Button button = new () { Text = "OK" };
        superView.Add (button);

        var superViewAcceptedCount = 0;
        superView.Accepted += (_, _) => superViewAcceptedCount++;

        // Button (IAcceptTarget) should bubble up to SuperView
        button.InvokeCommand (Command.Accept);

        Assert.Equal (1, superViewAcceptedCount);
    }

    [Fact]
    public void IAcceptTarget_In_Deep_Hierarchy_BubblesUp ()
    {
        View root = new () { Id = "root", CanFocus = true };
        root.CommandsToBubbleUp = [Command.Accept]; // Enable bubbling
        AcceptTargetTestView rootIsDefaultView = new () { IsDefault = true, Id = "rootIsDefaultView", CanFocus = true };

        View middle = new () { Id = "middle", CanFocus = true };
        middle.CommandsToBubbleUp = [Command.Accept]; // Enable bubbling
        AcceptTargetTestView middleDefaultView = new () { IsDefault = true, Id = "middleDefaultView" };

        root.Add (middle);
        middle.Add (middleDefaultView);

        root.Add (rootIsDefaultView);
        root.DefaultAcceptView = rootIsDefaultView;

        var rootAcceptingCount = 0;
        root.Accepting += (_, _) => rootAcceptingCount++;

        var rootAcceptedCount = 0;
        root.Accepted += (_, _) => rootAcceptedCount++;

        var middleAcceptingCount = 0;
        middle.Accepting += (_, _) => middleAcceptingCount++;

        var middleAcceptedCount = 0;
        middle.Accepted += (_, _) => middleAcceptedCount++;

        var rootIsDefaultViewAcceptingCount = 0;
        rootIsDefaultView.Accepting += (_, _) => rootIsDefaultViewAcceptingCount++;

        var rootIsDefaultViewAcceptedCount = 0;
        rootIsDefaultView.Accepted += (_, _) => rootIsDefaultViewAcceptedCount++;

        var middleDefaultViewAcceptingCount = 0;
        middleDefaultView.Accepting += (_, _) => middleDefaultViewAcceptingCount++;

        var middleDefaultViewAcceptedCount = 0;
        middleDefaultView.Accepted += (_, _) => middleDefaultViewAcceptedCount++;

        middleDefaultView.InvokeCommand (Command.Accept);

        Assert.Equal (1, middleAcceptingCount); // 1 because Accept bubbled up from middleDefaultView
        Assert.Equal (0, middleAcceptedCount); // 0 because Accept bubbled up to root, so middle's DefaultAcceptHandler returned early
        Assert.Equal (1, rootAcceptingCount); // 1 because of CommandsToBubbleUp
        Assert.Equal (1, rootAcceptedCount); // 1 because root should receive the Accepted event after bubbling through middleDefaultView
        Assert.Equal (1, middleDefaultViewAcceptingCount); // 1 because middleDefaultView is an IAcceptTarget and should handle Accepting
        Assert.Equal (0, middleDefaultViewAcceptedCount); // 0 because Accept bubbled up, so middleDefaultView's DefaultAcceptHandler returned early
        Assert.Equal (1, rootIsDefaultViewAcceptingCount); // 1 because root's DefaultAcceptView is invoked via DispatchDown
        Assert.Equal (1, rootIsDefaultViewAcceptedCount); // 1 because root's DefaultAcceptView receives Accepted after DispatchDown
    }

    [Fact]
    public void IAcceptTarget_In_Deep_Hierarchy_BubblesUp2 ()
    {
        View root = new () { Id = "root", CanFocus = true };
        root.CommandsToBubbleUp = [Command.Accept]; // Enable bubbling
        AcceptTargetTestView rootIsDefaultView = new () { IsDefault = true, Id = "rootIsDefaultView", CanFocus = true };

        View middle = new () { Id = "middle", CanFocus = true };
        middle.CommandsToBubbleUp = [Command.Accept]; // Enable bubbling
        AcceptTargetTestView middleView = new () { IsDefault = false, Id = "middleView" };

        root.Add (middle);
        middle.Add (middleView);

        root.Add (rootIsDefaultView);
        root.DefaultAcceptView = rootIsDefaultView;

        var rootAcceptingCount = 0;
        root.Accepting += (_, _) => rootAcceptingCount++;

        var rootAcceptedCount = 0;
        root.Accepted += (_, _) => rootAcceptedCount++;

        var middleAcceptingCount = 0;
        middle.Accepting += (_, _) => middleAcceptingCount++;

        var middleAcceptedCount = 0;
        middle.Accepted += (_, _) => middleAcceptedCount++;

        var rootIsDefaultViewAcceptingCount = 0;
        rootIsDefaultView.Accepting += (_, _) => rootIsDefaultViewAcceptingCount++;

        var rootIsDefaultViewAcceptedCount = 0;
        rootIsDefaultView.Accepted += (_, _) => rootIsDefaultViewAcceptedCount++;

        var middleDefaultViewAcceptingCount = 0;
        middleView.Accepting += (_, _) => middleDefaultViewAcceptingCount++;

        var middleDefaultViewAcceptedCount = 0;
        middleView.Accepted += (_, _) => middleDefaultViewAcceptedCount++;

        middleView.InvokeCommand (Command.Accept);

        Assert.Equal (1, middleAcceptingCount); // 1 because Accept bubbled up from middleView
        Assert.Equal (0, middleAcceptedCount); // 0 because Accept bubbled up to root, so middle's DefaultAcceptHandler returned early
        Assert.Equal (1, rootAcceptingCount); // 1 because of CommandsToBubbleUp
        Assert.Equal (1, rootAcceptedCount); // 1 because root should receive the Accepted event after bubbling through middleView
        Assert.Equal (1, middleDefaultViewAcceptingCount); // 1 because middleView (non-default IAcceptTarget) fires Accepting
        Assert.Equal (0, middleDefaultViewAcceptedCount); // 0 because Accept bubbled up, so middleView's DefaultAcceptHandler returned early
        Assert.Equal (1, rootIsDefaultViewAcceptingCount); // 1 because root's DefaultAcceptView is invoked via DispatchDown
        Assert.Equal (1, rootIsDefaultViewAcceptedCount); // 1 because root's DefaultAcceptView receives Accepted after DispatchDown
    }

    [Fact]
    public void IAcceptTarget_In_Deep_Hierarchy_BubblesUp3 ()
    {
        View root = new () { Id = "root", CanFocus = true };
        root.CommandsToBubbleUp = [Command.Accept]; // Enable bubbling
        AcceptTargetTestView rootIsDefaultView = new () { IsDefault = true, Id = "rootIsDefaultView", CanFocus = true };

        View middle = new () { Id = "middle", CanFocus = true };
        middle.CommandsToBubbleUp = [Command.Accept]; // Enable bubbling
        View middleView = new () { Id = "middleView" };

        root.Add (middle);
        middle.Add (middleView);

        root.Add (rootIsDefaultView);
        root.DefaultAcceptView = rootIsDefaultView;

        var rootAcceptingCount = 0;
        root.Accepting += (_, _) => rootAcceptingCount++;

        var rootAcceptedCount = 0;
        root.Accepted += (_, _) => rootAcceptedCount++;

        var middleAcceptingCount = 0;
        middle.Accepting += (_, _) => middleAcceptingCount++;

        var middleAcceptedCount = 0;
        middle.Accepted += (_, _) => middleAcceptedCount++;

        var rootIsDefaultViewAcceptingCount = 0;
        rootIsDefaultView.Accepting += (_, _) => rootIsDefaultViewAcceptingCount++;

        var rootIsDefaultViewAcceptedCount = 0;
        rootIsDefaultView.Accepted += (_, _) => rootIsDefaultViewAcceptedCount++;

        var middleDefaultViewAcceptingCount = 0;
        middleView.Accepting += (_, _) => middleDefaultViewAcceptingCount++;

        var middleDefaultViewAcceptedCount = 0;
        middleView.Accepted += (_, _) => middleDefaultViewAcceptedCount++;

        middleView.InvokeCommand (Command.Accept);

        Assert.Equal (1, middleAcceptingCount); // 1 because Accept bubbled up from middleView
        Assert.Equal (0, middleAcceptedCount); // 0 because Accept bubbled up to root, so middle's DefaultAcceptHandler returned early
        Assert.Equal (1, rootAcceptingCount); // 1 because of CommandsToBubbleUp
        Assert.Equal (1, rootAcceptedCount); // 1 because root should receive the Accepted event after bubbling through middleView
        Assert.Equal (1, middleDefaultViewAcceptingCount); // 1 because middleView (plain View) fires Accepting
        Assert.Equal (0, middleDefaultViewAcceptedCount); // 0 because Accept bubbled up, so middleView's DefaultAcceptHandler returned early
        Assert.Equal (1, rootIsDefaultViewAcceptingCount); // 1 because root's DefaultAcceptView is invoked via DispatchDown
        Assert.Equal (1, rootIsDefaultViewAcceptedCount); // 1 because root's DefaultAcceptView receives Accepted after DispatchDown
    }

    // CoPilot - ChatGPT o1
    [Fact]
    public void IAcceptTarget_Handled_Does_Not_BubbleUp ()
    {
        View superView = new () { CanFocus = true };
        AcceptTargetTestView acceptTarget = new () { Id = "acceptTarget" };

        superView.Add (acceptTarget);

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        // Handle the Accepting event on the acceptTarget
        acceptTarget.Accepting += (_, e) => e.Handled = true;

        acceptTarget.InvokeCommand (Command.Accept);

        // Should not bubble up because it was handled
        Assert.Equal (0, superViewAcceptingCount);
    }

    #endregion IAcceptTarget Bubbling Tests

    #region DispatchDown Tests

    // Claude - Opus 4.6
    /// <summary>
    ///     Exposes the protected <see cref="View.DispatchDown"/> method for testing.
    /// </summary>
    private class BubbleDownTestView : View
    {
        public bool? TestBubbleDown (View target, ICommandContext? ctx) => DispatchDown (target, ctx);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_InvokesCommandOnTarget ()
    {
        BubbleDownTestView superView = new ();
        ViewEventTester target = new ();
        superView.Add (target);

        CommandContext ctx = new (Command.Activate, new WeakReference<View> (superView), null);

        superView.TestBubbleDown (target, ctx);

        Assert.Equal (1, target.OnActivatingCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_SetsIsBubblingDown_True ()
    {
        BubbleDownTestView superView = new ();
        View target = new ();
        superView.Add (target);

        ICommandContext? receivedCtx = null;
        target.Activating += (_, e) => receivedCtx = e.Context;

        CommandContext ctx = new (Command.Activate, new WeakReference<View> (superView), null);
        superView.TestBubbleDown (target, ctx);

        Assert.NotNull (receivedCtx);
        Assert.Equal (CommandRouting.DispatchingDown, receivedCtx!.Routing);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_PreservesBinding ()
    {
        BubbleDownTestView superView = new ();
        View target = new ();
        superView.Add (target);

        ICommandContext? receivedCtx = null;
        target.Activating += (_, e) => receivedCtx = e.Context;

        KeyBinding originalBinding = new ([Command.Activate]) { Key = Key.Space };
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (superView), originalBinding);
        superView.TestBubbleDown (target, ctx);

        Assert.NotNull (receivedCtx);
        Assert.NotNull (receivedCtx!.Binding);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_PreservesSource ()
    {
        BubbleDownTestView superView = new () { Id = "superView" };
        View target = new () { Id = "target" };
        superView.Add (target);

        ICommandContext? receivedCtx = null;
        target.Activating += (_, e) => receivedCtx = e.Context;

        WeakReference<View> originalSource = new (superView);
        CommandContext ctx = new (Command.Activate, originalSource, null);
        superView.TestBubbleDown (target, ctx);

        Assert.NotNull (receivedCtx);
        View? source = null;
        Assert.True (receivedCtx!.Source?.TryGetTarget (out source));
        Assert.Same (superView, source);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_PreservesCommand ()
    {
        BubbleDownTestView superView = new ();
        ViewEventTester target = new ();
        superView.Add (target);

        CommandContext ctx = new (Command.Accept, new WeakReference<View> (superView), null);
        superView.TestBubbleDown (target, ctx);

        Assert.Equal (1, target.OnAcceptedCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_UsesNotBound_WhenCtxIsNull ()
    {
        BubbleDownTestView superView = new ();
        ViewEventTester target = new ();
        superView.Add (target);

        superView.TestBubbleDown (target, null);

        // NotBound command should fire CommandNotBound
        Assert.Equal (1, target.OnCommandNotBoundCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_Target_DoesNotBubbleUp ()
    {
        BubbleDownTestView superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Activate];

        View target = new () { Id = "target" };
        superView.Add (target);

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        CommandContext ctx = new (Command.Activate, new WeakReference<View> (superView), null);
        superView.TestBubbleDown (target, ctx);

        // The target's Activate must NOT bubble back up to superView
        Assert.Equal (0, superViewActivatingCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_Target_DoesNotBubbleUp_Accept ()
    {
        BubbleDownTestView superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Accept];

        Button defaultButton = new () { IsDefault = true, Id = "defaultButton" };
        View target = new () { Id = "target" };
        superView.Add (target);
        superView.Add (defaultButton);

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        var defaultButtonAcceptingCount = 0;
        defaultButton.Accepting += (_, _) => defaultButtonAcceptingCount++;

        CommandContext ctx = new (Command.Accept, new WeakReference<View> (superView), null);
        superView.TestBubbleDown (target, ctx);

        // Neither superView Accepting nor DefaultAcceptView should fire
        Assert.Equal (0, superViewAcceptingCount);
        Assert.Equal (0, defaultButtonAcceptingCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_Target_DoesNotBubbleUp_DeepHierarchy ()
    {
        BubbleDownTestView root = new () { Id = "root" };
        root.CommandsToBubbleUp = [Command.Activate];

        View middle = new () { Id = "middle" };
        middle.CommandsToBubbleUp = [Command.Activate];
        root.Add (middle);

        View leaf = new () { Id = "leaf" };
        middle.Add (leaf);

        var rootActivatingCount = 0;
        root.Activating += (_, _) => rootActivatingCount++;

        var middleActivatingCount = 0;
        middle.Activating += (_, _) => middleActivatingCount++;

        // DispatchDown from root to leaf — should not bubble to middle or root
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (root), null);
        root.TestBubbleDown (leaf, ctx);

        Assert.Equal (0, middleActivatingCount);
        Assert.Equal (0, rootActivatingCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void CommandsToBubbleUp_SkipsWhenIsBubblingDown ()
    {
        View superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Activate];

        View subView = new () { Id = "subView" };
        superView.Add (subView);

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // Invoke Activate on subView with Routing = DispatchingDown
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (subView), null) { Routing = CommandRouting.DispatchingDown };
        subView.InvokeCommand (Command.Activate, ctx);

        // SuperView should NOT receive the event
        Assert.Equal (0, superViewActivatingCount);
    }

    // Claude - Opus 4.6
    [Fact]
    public void BubbleDown_Then_NormalInvoke_BubblesNormally ()
    {
        BubbleDownTestView superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Activate];

        View target = new () { Id = "target" };
        superView.Add (target);

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        // First: DispatchDown — should NOT bubble
        CommandContext downCtx = new (Command.Activate, new WeakReference<View> (superView), null);
        superView.TestBubbleDown (target, downCtx);
        Assert.Equal (0, superViewActivatingCount);

        // Second: Normal invoke — SHOULD bubble
        target.InvokeCommand (Command.Activate);
        Assert.Equal (1, superViewActivatingCount);
    }

    /// <summary>
    ///     Regression test: When <see cref="Command.Accept"/> is invoked directly on a view that has a
    ///     <see cref="View.DefaultAcceptView"/>, the DefaultAcceptView's <see cref="View.Accepting"/> should fire,
    ///     and the original view's <see cref="View.Accepted"/> should also fire afterward.
    ///     This replicates the failure in DialogTests.GenericString_Command_Accept_BubblesUp where
    ///     <c>dialog.InvokeCommand(Command.Accept)</c> did not cause the default button's Accepting to fire
    ///     (okAcceptingFired was 0 instead of 1) and the dialog's Accepted event never fired
    ///     (dialogAcceptedFired was 0 instead of 1).
    /// </summary>
    [Fact]
    public void Accept_Direct_On_View_With_DefaultAcceptView_Fires_DefaultAcceptView_Accepting_And_View_Accepted ()
    {
        // Arrange: A superView with CommandsToBubbleUp and a DefaultAcceptView (IAcceptTarget with IsDefault = true)
        View superView = new () { Id = "superView", CanFocus = true };
        superView.CommandsToBubbleUp = [Command.Accept];

        AcceptTargetTestView defaultAcceptView = new () { IsDefault = true, Id = "defaultAcceptView", CanFocus = true };
        superView.Add (defaultAcceptView);
        var defaultAcceptViewAcceptingCount = 0;
        defaultAcceptView.Accepting += (_, _) => defaultAcceptViewAcceptingCount++;

        var superViewAcceptedCount = 0;
        superView.Accepted += (_, _) => superViewAcceptedCount++;

        var superViewAcceptingCount = 0;
        superView.Accepting += (_, _) => superViewAcceptingCount++;

        // Act: Invoke Accept directly on the superView (not on a subview)
        superView.InvokeCommand (Command.Accept);

        // Assert: The DefaultAcceptView's Accepting should have fired
        Assert.Equal (1, defaultAcceptViewAcceptingCount);

        // Assert: The superView's Accepting should have fired
        Assert.Equal (1, superViewAcceptingCount);

        // Assert: The superView's Accepted should have fired after the DefaultAcceptView handled Accept
        Assert.Equal (1, superViewAcceptedCount);
    }

    #endregion DispatchDown Tests

    #region Values Chain Bubbling Tests

    // Claude - Sonnet 4.6
    /// <summary>
    ///     When a ConsumeDispatch composite implements <see cref="IValue"/>,
    ///     ancestors receiving <see cref="View.Activated"/> via <see cref="View.CommandsToBubbleUp"/>
    ///     must see the composite's post-mutation value in <see cref="ICommandContext.Value"/>.
    /// </summary>
    [Fact]
    public void Values_BubbleActivatedUp_Carries_Composite_Value_To_Ancestor ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

            ToggleView toggleView = new () { Id = "toggleView" };
            CompositeValueView composite = new () { Id = "composite" };
            composite.Add (toggleView);

            View ancestor = new () { Id = "ancestor" };
            ancestor.CommandsToBubbleUp = [Command.Activate];
            ancestor.Add (composite);

            object? ancestorCapturedValue = null;

            ancestor.Activated += (_, args) => { ancestorCapturedValue = args.Value?.Value; };

            // Act
            composite.InvokeCommand (Command.Activate);

            // Assert: ancestor sees the composite's value (42), not ToggleView's value.
            Assert.Equal (42, ancestorCapturedValue as int?);

            ancestor.Dispose ();
        }
    }

    #endregion Values Chain Bubbling Tests

    #region Bridge Cancellation Bug (PopoverMenus.cs line 192)

    // Claude - Opus 4.6
    /// <summary>
    ///     Replicates the BUGBUG at PopoverMenus.cs line 192: when a <see cref="CommandBridge"/>
    ///     relays an <c>Activated</c> event to an owner, and the owner (or its ancestor) tries to
    ///     cancel via <c>OnActivating</c>, the originator's state has already changed because the
    ///     bridge fires from the post-event (<c>Activated</c>), not the pre-event (<c>Activating</c>).
    ///     Also verifies that a <c>BridgedCancellation</c> trace warning is emitted.
    ///
    ///     Topology (uses only View base classes):
    ///     <code>
    ///     ancestor (Activating handler cancels for specific source)
    ///       └── owner  ← Bridge ←  container (CommandsToBubbleUp=[Activate])
    ///                                  └── toggleView (IValue, mutates in OnActivated)
    ///     </code>
    ///
    ///     Expected: cancelling at the ancestor's Activating should prevent the state change.
    ///     Actual:   toggleView.Value has already incremented by the time ancestor's Activating fires.
    /// </summary>
    [Fact]
    public void Bridge_Ancestor_Cancel_OnActivating_Does_Not_Prevent_Originator_State_Change ()
    {
        ListBackend traceBackend = new ();
        using IDisposable scope = Trace.PushScope (TraceCategory.Command, traceBackend);

        // Arrange: toggleView inside container, bridged to owner, owner inside ancestor.
        ToggleView toggleView = new () { Id = "toggleView" };

        View container = new () { Id = "container" };
        container.CommandsToBubbleUp = [Command.Activate];
        container.Add (toggleView);

        View owner = new () { Id = "owner" };

        View ancestor = new () { Id = "ancestor" };
        ancestor.CommandsToBubbleUp = [Command.Activate];
        ancestor.Add (owner);

        using CommandBridge bridge = CommandBridge.Connect (owner, container, Command.Activate);

        // Track the toggleView.Value at the moment ancestor's Activating fires.
        int? valueAtAncestorActivating = null;
        var ancestorActivatingFired = false;

        ancestor.Activating += (_, args) =>
                                {
                                    ancestorActivatingFired = true;
                                    valueAtAncestorActivating = toggleView.Value;

                                    // Cancel — this should prevent further processing,
                                    // but cannot undo the toggleView's state change.
                                    args.Handled = true;
                                };

        Assert.Equal (0, toggleView.Value);

        // Act: Activate the toggleView directly (simulates a user click on the inner view).
        toggleView.InvokeCommand (Command.Activate);

        // Assert: The bridge should have caused ancestor.Activating to fire.
        Assert.True (ancestorActivatingFired, "ancestor.Activating should have fired via bridge");

        // BUGBUG: By the time ancestor's Activating handler fires, toggleView.Value
        // has already been incremented. The cancellation is too late.
        Assert.Equal (1, toggleView.Value); // State change already happened
        Assert.Equal (1, valueAtAncestorActivating); // Was already 1 when ancestor saw it

#if DEBUG
        // Verify the BridgedCancellation trace warning was emitted.
        Assert.Contains (traceBackend.Entries, e => e.Phase == "BridgedCancellation" && e.Message!.Contains ("OnActivated"));
#endif
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Contrast test: in the normal (non-bridge) bubble-up path, cancelling at the ancestor's
    ///     <c>OnActivating</c> DOES prevent the originator's state change, because <c>TryBubbleUp</c>
    ///     calls <c>SuperView.InvokeCommand</c> during <c>RaiseActivating</c> (the pre-event phase).
    ///     The originator's <c>OnActivated</c> only fires if <c>RaiseActivating</c> succeeds.
    ///     No <c>BridgedCancellation</c> trace warning should appear.
    ///
    ///     Topology:
    ///     <code>
    ///     ancestor (Activating handler cancels)
    ///       └── toggleView (IValue, mutates in OnActivated)
    ///     </code>
    ///
    ///     This proves the asymmetry: direct containment bubbling supports cancellation;
    ///     bridge-based bubbling does not.
    /// </summary>
    [Fact]
    public void Direct_Ancestor_Cancel_OnActivating_Prevents_Originator_State_Change ()
    {
        ListBackend traceBackend = new ();
        using IDisposable scope = Trace.PushScope (TraceCategory.Command, traceBackend);

        // Arrange: toggleView inside ancestor (direct containment, no bridge).
        ToggleView toggleView = new () { Id = "toggleView" };

        View ancestor = new () { Id = "ancestor" };
        ancestor.CommandsToBubbleUp = [Command.Activate];
        ancestor.Add (toggleView);

        int? valueAtAncestorActivating = null;
        var ancestorActivatingFired = false;

        ancestor.Activating += (_, args) =>
                                {
                                    ancestorActivatingFired = true;
                                    valueAtAncestorActivating = toggleView.Value;

                                    // Cancel — in the direct path, this DOES prevent
                                    // the originator's OnActivated from firing.
                                    args.Handled = true;
                                };

        Assert.Equal (0, toggleView.Value);

        // Act: Activate the toggleView directly.
        toggleView.InvokeCommand (Command.Activate);

        // Assert: ancestor.Activating should have fired via TryBubbleUp.
        Assert.True (ancestorActivatingFired, "ancestor.Activating should have fired via TryBubbleUp");

        // In the direct containment path, cancellation at the ancestor DOES work:
        // toggleView.OnActivated never fires, so Value remains 0.
        Assert.Equal (0, toggleView.Value);
        Assert.Equal (0, valueAtAncestorActivating);

        // No BridgedCancellation warning — this is a direct containment path.
        Assert.DoesNotContain (traceBackend.Entries, e => e.Phase == "BridgedCancellation");
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Accept-side analog of the bridge cancellation bug: when a <see cref="CommandBridge"/>
    ///     relays an <c>Accepted</c> event to an owner, and the owner (or its ancestor) tries to
    ///     cancel via <c>OnAccepting</c>, the originator's state has already changed because the
    ///     bridge fires from the post-event (<c>Accepted</c>).
    ///     Verifies that a <c>BridgedCancellation</c> trace warning is emitted.
    ///
    ///     Topology:
    ///     <code>
    ///     ancestor (Accepting handler cancels)
    ///       └── owner  ← Bridge(Accept) ←  acceptToggleView (mutates in OnAccepted)
    ///     </code>
    /// </summary>
    [Fact]
    public void Bridge_Ancestor_Cancel_OnAccepting_Does_Not_Prevent_Originator_State_Change ()
    {
        ListBackend traceBackend = new ();
        using IDisposable scope = Trace.PushScope (TraceCategory.Command, traceBackend);

        // Arrange: acceptToggleView bridged to owner, owner inside ancestor.
        AcceptToggleView acceptToggleView = new () { Id = "acceptToggleView" };

        View owner = new () { Id = "owner" };

        View ancestor = new () { Id = "ancestor" };
        ancestor.CommandsToBubbleUp = [Command.Accept];
        ancestor.Add (owner);

        using CommandBridge bridge = CommandBridge.Connect (owner, acceptToggleView, Command.Accept);

        int? valueAtAncestorAccepting = null;
        var ancestorAcceptingFired = false;

        ancestor.Accepting += (_, args) =>
                               {
                                   ancestorAcceptingFired = true;
                                   valueAtAncestorAccepting = acceptToggleView.AcceptedCount;

                                   // Cancel — this should prevent further processing,
                                   // but cannot undo the acceptToggleView's state change.
                                   args.Handled = true;
                               };

        Assert.Equal (0, acceptToggleView.AcceptedCount);

        // Act: Accept on the remote view.
        acceptToggleView.InvokeCommand (Command.Accept);

        // Assert: The bridge should have caused ancestor.Accepting to fire.
        Assert.True (ancestorAcceptingFired, "ancestor.Accepting should have fired via bridge");

        // By the time ancestor's Accepting handler fires, acceptToggleView.OnAccepted
        // has already been called. The cancellation is too late.
        Assert.Equal (1, acceptToggleView.AcceptedCount); // State change already happened
        Assert.Equal (1, valueAtAncestorAccepting); // Was already 1 when ancestor saw it

        // Verify the BridgedCancellation trace warning was emitted.
#if DEBUG
        Assert.Contains (traceBackend.Entries, e => e.Phase == "BridgedCancellation" && e.Message!.Contains ("OnAccepted"));
#endif
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Contrast test for Accept: in the normal (non-bridge) bubble-up path, cancelling at the
    ///     ancestor's <c>OnAccepting</c> DOES prevent the originator's state change, because
    ///     <c>TryBubbleUp</c> calls <c>SuperView.InvokeCommand</c> during <c>RaiseAccepting</c>
    ///     (the pre-event phase). No <c>BridgedCancellation</c> trace warning should appear.
    ///
    ///     Topology:
    ///     <code>
    ///     ancestor (Accepting handler cancels)
    ///       └── acceptToggleView (mutates in OnAccepted)
    ///     </code>
    /// </summary>
    [Fact]
    public void Direct_Ancestor_Cancel_OnAccepting_Prevents_Originator_State_Change ()
    {
        ListBackend traceBackend = new ();
        using IDisposable scope = Trace.PushScope (TraceCategory.Command, traceBackend);

        // Arrange: acceptToggleView inside ancestor (direct containment, no bridge).
        AcceptToggleView acceptToggleView = new () { Id = "acceptToggleView" };

        View ancestor = new () { Id = "ancestor" };
        ancestor.CommandsToBubbleUp = [Command.Accept];
        ancestor.Add (acceptToggleView);

        int? valueAtAncestorAccepting = null;
        var ancestorAcceptingFired = false;

        ancestor.Accepting += (_, args) =>
                               {
                                   ancestorAcceptingFired = true;
                                   valueAtAncestorAccepting = acceptToggleView.AcceptedCount;

                                   // Cancel — in the direct path, this DOES prevent
                                   // the originator's OnAccepted from firing.
                                   args.Handled = true;
                               };

        Assert.Equal (0, acceptToggleView.AcceptedCount);

        // Act: Accept on the view directly.
        acceptToggleView.InvokeCommand (Command.Accept);

        // Assert: ancestor.Accepting should have fired via TryBubbleUp.
        Assert.True (ancestorAcceptingFired, "ancestor.Accepting should have fired via TryBubbleUp");

        // In the direct containment path, cancellation at the ancestor DOES work:
        // acceptToggleView.OnAccepted never fires, so AcceptedCount remains 0.
        Assert.Equal (0, acceptToggleView.AcceptedCount);
        Assert.Equal (0, valueAtAncestorAccepting);

        // No BridgedCancellation warning — this is a direct containment path.
        Assert.DoesNotContain (traceBackend.Entries, e => e.Phase == "BridgedCancellation");
    }

    #endregion Bridge Cancellation Bug


    #region Unbound Command Bubbling Tests

    // Copilot
    [Fact]
    public void UnboundCommand_BubblesToSuperView_ViaCommandsToBubbleUp ()
    {
        View superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Up];

        View subView = new () { Id = "subView" };
        superView.Add (subView);

        var superViewCommandNotBoundCount = 0;

        superView.CommandNotBound += (_, _) => superViewCommandNotBoundCount++;

        // Command.Up is NOT bound on subView, so it goes through DefaultCommandNotBoundHandler.
        // After upgrade, unbound commands should bubble via CommandsToBubbleUp.
        subView.InvokeCommand (Command.Up);

        Assert.Equal (1, superViewCommandNotBoundCount);
    }

    // Copilot
    [Fact]
    public void UnboundCommand_DoesNotBubble_WhenNotInCommandsToBubbleUp ()
    {
        View superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Activate]; // NOT Command.Up

        View subView = new () { Id = "subView" };
        superView.Add (subView);

        var superViewCommandNotBoundCount = 0;

        superView.CommandNotBound += (_, _) => superViewCommandNotBoundCount++;

        subView.InvokeCommand (Command.Up);

        Assert.Equal (0, superViewCommandNotBoundCount);
    }

    // Copilot
    [Fact]
    public void UnboundCommand_BubblesInDeepHierarchy ()
    {
        View grandSuperView = new () { Id = "grandSuperView" };
        grandSuperView.CommandsToBubbleUp = [Command.Up];

        View superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Up];
        grandSuperView.Add (superView);

        View subView = new () { Id = "subView" };
        superView.Add (subView);

        var grandCommandNotBoundCount = 0;

        grandSuperView.CommandNotBound += (_, _) => grandCommandNotBoundCount++;

        subView.InvokeCommand (Command.Up);

        Assert.Equal (1, grandCommandNotBoundCount);
    }

    // Copilot
    [Fact]
    public void UnboundCommand_StopsWhenHandled ()
    {
        View superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Up];

        View subView = new () { Id = "subView" };
        superView.Add (subView);

        var superViewCommandNotBoundCount = 0;

        superView.CommandNotBound += (_, _) => superViewCommandNotBoundCount++;

        // Handle CommandNotBound on subView — should stop bubbling
        subView.CommandNotBound += (_, e) => e.Handled = true;

        subView.InvokeCommand (Command.Up);

        Assert.Equal (0, superViewCommandNotBoundCount);
    }

    // Copilot
    [Fact]
    public void UnboundCommand_BubblingUp_ReturnsHandled_WhenBubbles ()
    {
        // When an unbound command bubbles via CommandsToBubbleUp, it reports as handled
        // (mirrors DefaultActivateHandler which returns true when CommandWillBubbleToAncestor).
        View superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Up];

        View subView = new () { Id = "subView" };
        superView.Add (subView);

        bool? result = subView.InvokeCommand (Command.Up);

        // Unbound commands that bubble report as handled, consistent with bound commands.
        Assert.True (result);
    }

    #endregion Unbound Command Bubbling Tests

    #region CommandBridge Arbitrary Command Tests

    // Copilot
    [Fact]
    public void CommandBridge_BridgesUnboundCommand ()
    {
        View owner = new () { Id = "owner" };
        View remote = new () { Id = "remote" };

        // Bridge Command.Up from remote to owner
        using CommandBridge bridge = CommandBridge.Connect (owner, remote, Command.Up);

        var ownerCommandNotBoundCount = 0;
        owner.CommandNotBound += (_, _) => ownerCommandNotBoundCount++;

        // Invoke Command.Up on remote — it's unbound, so it goes to DefaultCommandNotBoundHandler
        // The bridge should relay the CommandNotBound event to owner
        remote.InvokeCommand (Command.Up);

        Assert.Equal (1, ownerCommandNotBoundCount);
    }

    // Copilot
    [Fact]
    public void CommandBridge_IgnoresUnregisteredCommand ()
    {
        View owner = new () { Id = "owner" };
        View remote = new () { Id = "remote" };

        // Bridge only Command.Up
        using CommandBridge bridge = CommandBridge.Connect (owner, remote, Command.Up);

        var ownerCommandNotBoundCount = 0;
        owner.CommandNotBound += (_, _) => ownerCommandNotBoundCount++;

        // Invoke Command.Down — not registered with the bridge
        remote.InvokeCommand (Command.Down);

        Assert.Equal (0, ownerCommandNotBoundCount);
    }

    #endregion CommandBridge Arbitrary Command Tests

    #region DefaultCommandNotBoundHandler Does Not Raise Activated

    // Copilot
    [Fact]
    public void UnboundCommand_DoesNotRaise_Activated_OnSuperView ()
    {
        // BubbleActivatedUp must NOT be called for unbound commands — Activated is specific
        // to the Activate lifecycle and should not fire for arbitrary commands like Command.Up.
        View superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Up];

        View subView = new () { Id = "subView" };
        superView.Add (subView);

        var superViewActivatedCount = 0;

        superView.Activated += (_, _) => superViewActivatedCount++;

        subView.InvokeCommand (Command.Up);

        Assert.Equal (0, superViewActivatedCount);
    }

    // Copilot
    [Fact]
    public void UnboundCommand_DoesNotRaise_Activated_InDeepHierarchy ()
    {
        View grandSuperView = new () { Id = "grandSuperView" };
        grandSuperView.CommandsToBubbleUp = [Command.Up];

        View superView = new () { Id = "superView" };
        superView.CommandsToBubbleUp = [Command.Up];
        grandSuperView.Add (superView);

        View subView = new () { Id = "subView" };
        superView.Add (subView);

        var grandActivatedCount = 0;
        var superActivatedCount = 0;

        grandSuperView.Activated += (_, _) => grandActivatedCount++;
        superView.Activated += (_, _) => superActivatedCount++;

        subView.InvokeCommand (Command.Up);

        Assert.Equal (0, grandActivatedCount);
        Assert.Equal (0, superActivatedCount);
    }

    #endregion DefaultCommandNotBoundHandler Does Not Raise Activated

    #region RaiseCommandNotBound Null Return Tests

    // Copilot
    [Fact]
    public void UnboundCommand_ReturnsNull_WhenNoSubscribersNoBubblingNoDispatch ()
    {
        // RaiseCommandNotBound should return null when nobody observed/handled the command
        // and there are no subscribers, no dispatch, and no bubbling.
        View view = new () { Id = "view" };

        bool? result = view.InvokeCommand (Command.New);

        Assert.Null (result);
    }

    // Copilot
    [Fact]
    public void UnboundCommand_ReturnsFalse_WhenSubscriberExists_ButNotHandled ()
    {
        // When a subscriber exists but does not set Handled=true, return false (not null).
        View view = new () { Id = "view" };

        view.CommandNotBound += (_, _) => { }; // subscriber present, does NOT set Handled

        bool? result = view.InvokeCommand (Command.New);

        Assert.NotNull (result);
        Assert.False (result);
    }

    // Copilot
    [Fact]
    public void UnboundCommand_ReturnsTrue_WhenSubscriberHandles ()
    {
        View view = new () { Id = "view" };

        view.CommandNotBound += (_, e) => e.Handled = true;

        bool? result = view.InvokeCommand (Command.New);

        Assert.True (result);
    }

    #endregion RaiseCommandNotBound Null Return Tests

    #region CommandBridge Handled Guard Tests

    // Copilot
    [Fact]
    public void CommandBridge_DoesNotBridge_WhenAlreadyHandled ()
    {
        View owner = new () { Id = "owner" };
        View remote = new () { Id = "remote" };

        var ownerCommandNotBoundCount = 0;

        owner.CommandNotBound += (_, _) => ownerCommandNotBoundCount++;

        // A handler on remote sets Handled=true BEFORE the bridge subscribes.
        // Since event handlers fire in subscription order, this runs first.
        remote.CommandNotBound += (_, e) => e.Handled = true;

        // Bridge subscribes AFTER the handler, so it sees Handled=true and skips relaying.
        using CommandBridge bridge = CommandBridge.Connect (owner, remote, Command.Up);

        remote.InvokeCommand (Command.Up);

        Assert.Equal (0, ownerCommandNotBoundCount);
    }

    // Copilot
    [Fact]
    public void CommandBridge_SetsHandled_AfterRelay ()
    {
        View owner = new () { Id = "owner" };
        View remote = new () { Id = "remote" };

        using CommandBridge bridge = CommandBridge.Connect (owner, remote, Command.Up);

        var handledAfterBridge = false;

        // Subscribe AFTER the bridge — this handler sees the Handled flag the bridge set.
        remote.CommandNotBound += (_, e) => handledAfterBridge = e.Handled;

        remote.InvokeCommand (Command.Up);

        Assert.True (handledAfterBridge);
    }

    #endregion CommandBridge Handled Guard Tests

    #region Test Helpers

    // CoPilot - ChatGPT o1
    /// <summary>
    ///     Test view that implements <see cref="IAcceptTarget"/> to verify accept target behavior.
    /// </summary>
    private class AcceptTargetTestView : View, IAcceptTarget
    {
        public AcceptTargetTestView () => CanFocus = true;

        public bool IsDefault { get; set; }
    }

    public class ViewEventTester : View
    {
        public ViewEventTester ()
        {
            Id = "viewEventTester";
            CanFocus = true;

            Accepting += (_, a) =>
                         {
                             a.Handled = HandleAccepted;
                             AcceptedCount++;
                         };

            HandlingHotKey += (_, a) =>
                              {
                                  a.Handled = HandleHandlingHotKey;
                                  HandlingHotKeyCount++;
                              };

            Activating += (_, a) =>
                          {
                              a.Handled = HandleActivating;
                              ActivatingCount++;
                          };

            CommandNotBound += (_, a) =>
                               {
                                   a.Handled = HandleCommandNotBound;
                                   CommandNotBoundCount++;
                               };
        }

        public int OnAcceptedCount { get; set; }
        public int AcceptedCount { get; set; }
        public bool HandleOnAccepted { get; set; }

        /// <inheritdoc/>
        protected override bool OnAccepting (CommandEventArgs args)
        {
            OnAcceptedCount++;

            return HandleOnAccepted;
        }

        public bool HandleAccepted { get; set; }

        public int OnHandlingHotKeyCount { get; set; }
        public int HandlingHotKeyCount { get; set; }
        public bool HandleOnHandlingHotKey { get; set; }

        /// <inheritdoc/>
        protected override bool OnHandlingHotKey (CommandEventArgs args)
        {
            OnHandlingHotKeyCount++;

            return HandleOnHandlingHotKey;
        }

        public bool HandleHandlingHotKey { get; set; }

        public int OnActivatingCount { get; set; }
        public int ActivatingCount { get; set; }
        public bool HandleOnActivating { get; set; }
        public bool HandleActivating { get; set; }

        /// <inheritdoc/>
        protected override bool OnActivating (CommandEventArgs args)
        {
            OnActivatingCount++;

            return HandleOnActivating;
        }

        public int OnCommandNotBoundCount { get; set; }
        public int CommandNotBoundCount { get; set; }

        public bool HandleOnCommandNotBound { get; set; }

        public bool HandleCommandNotBound { get; set; }

        protected override bool OnCommandNotBound (CommandEventArgs args)
        {
            OnCommandNotBoundCount++;

            return HandleOnCommandNotBound;
        }
    }

    /// <summary>
    ///     A view that implements <see cref="IValue{T}"/> and increments its value in
    ///     <see cref="OnActivated"/>, similar to how <see cref="CheckBox"/> advances its
    ///     <see cref="CheckState"/>. Used to test the command pipeline independently of CheckBox.
    /// </summary>
    private class ToggleView : View, IValue<int>
    {
        /// <summary>Gets the number of times <see cref="OnActivated"/> has been called.</summary>
        public int ActivatedCount { get; private set; }

        public int Value
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                int old = field;
                ValueChanging?.Invoke (this, new ValueChangingEventArgs<int> (old, value));
                field = value;
                ValueChanged?.Invoke (this, new ValueChangedEventArgs<int> (old, value));
                ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (old, value));
            }
        }

        public event EventHandler<ValueChangingEventArgs<int>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<int>>? ValueChanged;

        private event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

        event EventHandler<ValueChangedEventArgs<object?>>? IValue.ValueChangedUntyped
        {
            add => ValueChangedUntyped += value;
            remove => ValueChangedUntyped -= value;
        }

        /// <inheritdoc/>
        protected override void OnActivated (ICommandContext? commandContext)
        {
            base.OnActivated (commandContext);
            ActivatedCount++;
            Value++;
        }
    }

    /// <summary>
    ///     A ConsumeDispatch composite that implements <see cref="IValue{T}"/> and updates its own
    ///     value in <see cref="OnActivated"/>. Replicates the OptionSelector/FlagSelector pattern
    ///     without depending on those classes.
    /// </summary>
    private class CompositeValueView : View, IValue<int?>
    {
        public CompositeValueView () => CommandsToBubbleUp = [Command.Activate];

        public int? Value
        {
            get;
            set
            {
                int? old = field;
                ValueChanging?.Invoke (this, new ValueChangingEventArgs<int?> (old, value));
                field = value;
                ValueChanged?.Invoke (this, new ValueChangedEventArgs<int?> (old, value));
                _valueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (old, value));
            }
        }

        /// <inheritdoc/>
        protected override bool ConsumeDispatch => true;

        /// <inheritdoc/>
        protected override View? GetDispatchTarget (ICommandContext? ctx) => SubViews.FirstOrDefault ();

        /// <inheritdoc/>
        protected override void OnActivated (ICommandContext? ctx)
        {
            base.OnActivated (ctx);

            // Simulate what OptionSelector.ApplyActivation does: update own value after base fires.
            Value = 42;
        }

        public event EventHandler<ValueChangingEventArgs<int?>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<int?>>? ValueChanged;

        private event EventHandler<ValueChangedEventArgs<object?>>? _valueChangedUntyped;

        event EventHandler<ValueChangedEventArgs<object?>>? IValue.ValueChangedUntyped
        {
            add => _valueChangedUntyped += value;
            remove => _valueChangedUntyped -= value;
        }
    }

    /// <summary>
    ///     A minimal view that tracks Accept-side state changes.
    ///     Increments <see cref="AcceptedCount"/> in <see cref="OnAccepted"/> to
    ///     provide a trackable state change for bridge cancellation tests.
    /// </summary>
    private class AcceptToggleView : View
    {
        /// <summary>Gets the number of times <see cref="OnAccepted"/> has been called.</summary>
        public int AcceptedCount { get; private set; }

        /// <inheritdoc/>
        protected override void OnAccepted (ICommandContext? commandContext)
        {
            base.OnAccepted (commandContext);
            AcceptedCount++;
        }
    }

    // Test view that implements IValue<object?>
    private class TestValueView : View, IValue<object?>
    {
        public object? Value
        {
            get;
            set
            {
                object? old = field;
                ValueChanging?.Invoke (this, new ValueChangingEventArgs<object?> (old, value));
                field = value;
                ValueChanged?.Invoke (this, new ValueChangedEventArgs<object?> (old, value));
                ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (old, value));
            }
        }

        public event EventHandler<ValueChangingEventArgs<object?>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<object?>>? ValueChanged;
        public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;
    }


    #endregion Test Helpers
}
