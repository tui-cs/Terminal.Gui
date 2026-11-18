using Microsoft.Extensions.Logging;
using Terminal.Gui.Tracing;
using UnitTests;
using UnitTests.Parallelizable;

namespace ApplicationTests.Popover;

[Collection ("Application Tests")]
public class PopoverImplTests (ITestOutputHelper output)
{
    // Minimal concrete implementation for testing
    private class TestPopover : PopoverImpl
    { }

    [Fact]
    public void Constructor_SetsDefaults ()
    {
        var popover = new TestPopover ();
#if DEBUG
        Assert.Equal ("popoverImpl", popover.Id);
#endif
        Assert.True (popover.CanFocus);
        Assert.Equal (Dim.Fill (), popover.Width);
        Assert.Equal (Dim.Fill (), popover.Height);
        Assert.True (popover.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent));
        Assert.True (popover.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse));
    }

    [Fact]
    public void Owner_Property_CanBeSetAndGet ()
    {
        var popover = new TestPopover ();
        var top = new Runnable ();
        popover.Owner = top;
        Assert.Same (top, popover.Owner);
    }

    [Fact]
    public void Show_ThrowsIfPopoverMissingRequiredFlags ()
    {
        var popover = new TestPopover ();

        // Popover missing Transparent flags
        popover.ViewportSettings = ViewportSettingsFlags.None; // Remove required flags

        var popoverManager = new ApplicationPopover ();

        // Test missing Transparent flags
        Assert.ThrowsAny<Exception> (() => popoverManager.Show (popover));
    }

    [Fact]
    public void Show_ThrowsIfPopoverMissingQuitCommand ()
    {
        var popover = new TestPopover ();

        // Popover missing Command.Quit binding
        popover.KeyBindings.Clear (); // Remove all key bindings

        var popoverManager = new ApplicationPopover ();
        Assert.ThrowsAny<Exception> (() => popoverManager.Show (popover));
    }

    [Fact]
    public void Show_Throw_If_Not_Registered ()
    {
        var popover = new TestPopover ();

        var popoverManager = new ApplicationPopover ();
        Assert.Throws<InvalidOperationException> (() => popoverManager.Show (popover));
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Default_Is_Null ()
    {
        TestPopover popover = new ();
        Assert.Null (popover.Target);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Get_Returns_Set_Value ()
    {
        TestPopover popover = new ();
        View target = new ();
        WeakReference<View> weakRef = new (target);

        popover.Target = weakRef;

        Assert.Same (weakRef, popover.Target);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Set_Creates_Bridge_Activated_Reaches_Target ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Only uncomment this for debugging; setting a static property in parallel tests will cause interference between tests
            // Trace.EnabledCategories = TraceCategory.Command;

            TestPopover popover = new ();
            View target = new () { Id = "target" };

            popover.Target = new WeakReference<View> (target);

            var activatedCount = 0;
            ICommandContext? capturedCtx = null;

            target.Activated += (_, args) =>
                                {
                                    activatedCount++;
                                    capturedCtx = args.Value;
                                };

            popover.InvokeCommand (Command.Activate);

            Assert.Equal (1, activatedCount);
            Assert.NotNull (capturedCtx);
            Assert.Equal (CommandRouting.Bridged, capturedCtx!.Routing);
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Set_Creates_Bridge_Accepted_Reaches_Target ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Only uncomment this for debugging; setting a static property in parallel tests will cause interference between tests
            // Trace.EnabledCategories = TraceCategory.Command;

            TestPopover popover = new ();
            View target = new () { Id = "target" };

            popover.Target = new WeakReference<View> (target);

            var acceptedCount = 0;
            ICommandContext? capturedCtx = null;

            target.Accepted += (_, args) =>
                               {
                                   acceptedCount++;
                                   capturedCtx = args.Context;
                               };

            popover.InvokeCommand (Command.Accept);

            Assert.Equal (1, acceptedCount);
            Assert.NotNull (capturedCtx);
            Assert.Equal (CommandRouting.Bridged, capturedCtx!.Routing);
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Activated_Bubbles_Through_Target_SuperView_Chain ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Only uncomment this for debugging; setting a static property in parallel tests will cause interference between tests
            // Trace.EnabledCategories = TraceCategory.Command;

            TestPopover popover = new ();
            View target = new () { Id = "target" };
            View superView = new () { Id = "superView", CommandsToBubbleUp = [Command.Activate] };
            superView.Add (target);

            popover.Target = new WeakReference<View> (target);

            var superViewActivatedCount = 0;

            superView.Activated += (_, _) => { superViewActivatedCount++; };

            popover.InvokeCommand (Command.Activate);

            Assert.Equal (1, superViewActivatedCount);
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Changed_Disposes_Old_Bridge_Uses_New ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Only uncomment this for debugging; setting a static property in parallel tests will cause interference between tests
            // Trace.EnabledCategories = TraceCategory.Command;

            TestPopover popover = new ();
            View viewA = new () { Id = "viewA" };
            View viewB = new () { Id = "viewB" };

            popover.Target = new WeakReference<View> (viewA);

            // Change target to viewB
            popover.Target = new WeakReference<View> (viewB);

            var viewAActivatedCount = 0;
            var viewBActivatedCount = 0;

            viewA.Activated += (_, _) => { viewAActivatedCount++; };
            viewB.Activated += (_, _) => { viewBActivatedCount++; };

            popover.InvokeCommand (Command.Activate);

            Assert.Equal (0, viewAActivatedCount);
            Assert.Equal (1, viewBActivatedCount);
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void Target_Set_Null_Disposes_Bridge ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Only uncomment this for debugging; setting a static property in parallel tests will cause interference between tests
            // Trace.EnabledCategories = TraceCategory.Command;

            TestPopover popover = new ();
            View target = new () { Id = "target" };

            popover.Target = new WeakReference<View> (target);

            // Now set target to null
            popover.Target = null;

            var activatedCount = 0;
            target.Activated += (_, _) => { activatedCount++; };

            popover.InvokeCommand (Command.Activate);

            Assert.Equal (0, activatedCount);
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void Dispose_Cleans_Up_Target_Bridge ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Only uncomment this for debugging; setting a static property in parallel tests will cause interference between tests
            // Trace.EnabledCategories = TraceCategory.Command;

            TestPopover popover = new ();
            View target = new () { Id = "target" };

            popover.Target = new WeakReference<View> (target);

            popover.Dispose ();

            var activatedCount = 0;
            target.Activated += (_, _) => { activatedCount++; };

            // The popover is disposed, so invoking a command on it should not bridge
            popover.InvokeCommand (Command.Activate);

            Assert.Equal (0, activatedCount);
        }
    }
}
