// Claude - Opus 4.8
// Tests for CR feedback #2: MecThemeManager must not leak a static-event subscription.

using System.Reflection;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

/// <summary>
///     Verifies that <see cref="MecThemeManager"/> does not leak subscriptions on the static
///     <see cref="ThemeManager.ThemeChanged"/> event (CR feedback #2). Forwarding is wired up only while
///     the instance has subscribers, so an instance with no subscribers can be collected and does not
///     multiply event invocations.
/// </summary>
[Collection ("StaticSettingsTests")]
public class MecThemeManagerLeakTests
{
    /// <summary>
    ///     Constructing a <see cref="MecThemeManager"/> with no subscribers must NOT register a handler on the
    ///     static <see cref="ThemeManager.ThemeChanged"/> event — otherwise the instance can never be collected.
    /// </summary>
    [Fact]
    public void Construction_WithNoSubscribers_DoesNotRegisterStaticHandler ()
    {
        int before = GetStaticThemeChangedHandlerCount ();

        TuiConfigurationBuilder builder = new ();
        MecThemeManager manager = new (builder);

        Assert.Equal (before, GetStaticThemeChangedHandlerCount ());

        GC.KeepAlive (manager);
    }

    /// <summary>
    ///     Adding the first subscriber wires up exactly one static handler; removing the last subscriber
    ///     removes it again, leaving no leftover subscription.
    /// </summary>
    [Fact]
    public void SubscribeThenUnsubscribe_LeavesNoStaticHandler ()
    {
        int before = GetStaticThemeChangedHandlerCount ();

        TuiConfigurationBuilder builder = new ();
        MecThemeManager manager = new (builder);

        void Handler (object? sender, EventArgs<string> e) { }

        manager.ThemeChanged += Handler;

        Assert.Equal (before + 1, GetStaticThemeChangedHandlerCount ());

        manager.ThemeChanged -= Handler;

        Assert.Equal (before, GetStaticThemeChangedHandlerCount ());

        GC.KeepAlive (manager);
    }

    /// <summary>
    ///     Two subscribers on the same instance still wire up only a single static handler, and the static
    ///     handler is removed only once the last subscriber detaches.
    /// </summary>
    [Fact]
    public void MultipleSubscribers_ShareSingleStaticHandler ()
    {
        int before = GetStaticThemeChangedHandlerCount ();

        TuiConfigurationBuilder builder = new ();
        MecThemeManager manager = new (builder);

        void First (object? sender, EventArgs<string> e) { }
        void Second (object? sender, EventArgs<string> e) { }

        manager.ThemeChanged += First;
        manager.ThemeChanged += Second;

        Assert.Equal (before + 1, GetStaticThemeChangedHandlerCount ());

        manager.ThemeChanged -= First;

        // Still one subscriber left -> static handler must remain.
        Assert.Equal (before + 1, GetStaticThemeChangedHandlerCount ());

        manager.ThemeChanged -= Second;

        Assert.Equal (before, GetStaticThemeChangedHandlerCount ());

        GC.KeepAlive (manager);
    }

    /// <summary>
    ///     A subscriber on the instance is invoked when the legacy <see cref="ThemeManager.ThemeChanged"/> fires;
    ///     after unsubscribing, the legacy event no longer reaches the instance.
    /// </summary>
    [Fact]
    public void LegacyThemeChanged_IsForwarded_OnlyWhileSubscribed ()
    {
        TuiConfigurationBuilder builder = new ();
        MecThemeManager manager = new (builder);

        int count = 0;

        void Handler (object? sender, EventArgs<string> e) => count++;

        manager.ThemeChanged += Handler;
        ThemeManager.OnThemeChanged ("A", "B");
        Assert.Equal (1, count);

        manager.ThemeChanged -= Handler;
        ThemeManager.OnThemeChanged ("B", "C");
        Assert.Equal (1, count);
    }

    /// <summary>
    ///     Reads the invocation-list length of the static <see cref="ThemeManager.ThemeChanged"/> event via its
    ///     compiler-generated backing field, so the test can assert the absence of leaked subscriptions.
    /// </summary>
    private static int GetStaticThemeChangedHandlerCount ()
    {
        FieldInfo? field = typeof (ThemeManager).GetField (
                                                           nameof (ThemeManager.ThemeChanged),
                                                           BindingFlags.Static | BindingFlags.NonPublic);

        if (field?.GetValue (null) is Delegate handler)
        {
            return handler.GetInvocationList ().Length;
        }

        return 0;
    }
}
