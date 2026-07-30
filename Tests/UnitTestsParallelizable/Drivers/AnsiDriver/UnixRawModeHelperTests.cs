// Claude - Opus 4.7
// Tests UnixRawModeHelper safety guarantees from issue #5164:
//   - Restore() is a no-op when TryEnable() never succeeded (no syscall risk).
//   - Restore() is idempotent.
//   - Dispose() unhooks the ProcessExit handler it registered in TryEnable().
// These tests are observable on any platform: the platform gate inside TryEnable
// short-circuits non-Unix runs to a "never enabled" state, which is exactly the
// condition the safety guards protect against.

using System.Reflection;
using System.Runtime.InteropServices;
using Terminal.Gui.Drivers;

namespace DriverTests.AnsiDriver;

[Trait ("Category", "Unix")]
public class UnixRawModeHelperTests
{
    [Fact]
    public void Restore_WithoutTryEnable_IsNoOp_AndDoesNotThrow ()
    {
        UnixRawModeHelper helper = new ();

        // Sanity: never enabled before Restore().
        Assert.False (helper.IsRawModeEnabled);

        // Should not throw and should not write garbage termios. The internal guard
        // is _haveSavedTermios; we verify it stays false via reflection.
        Exception? thrown = Record.Exception (() => helper.Restore ());
        Assert.Null (thrown);

        Assert.False (helper.IsRawModeEnabled);
        Assert.False (GetHaveSavedTermios (helper));
    }

    [Fact]
    public void Restore_IsIdempotent ()
    {
        UnixRawModeHelper helper = new ();

        Exception? first = Record.Exception (() => helper.Restore ());
        Exception? second = Record.Exception (() => helper.Restore ());
        Exception? third = Record.Exception (() => helper.Restore ());

        Assert.Null (first);
        Assert.Null (second);
        Assert.Null (third);
    }

    [Fact]
    public void Dispose_WithoutTryEnable_IsNoOp_AndDoesNotThrow ()
    {
        UnixRawModeHelper helper = new ();

        Exception? thrown = Record.Exception (() => helper.Dispose ());
        Assert.Null (thrown);

        // Second dispose should also be a no-op.
        Exception? second = Record.Exception (() => helper.Dispose ());
        Assert.Null (second);
    }

    [Fact]
    public void TryEnable_OnNonUnix_ReturnsFalse_AndLeavesNoSavedTermios ()
    {
        if (!OperatingSystem.IsWindows ())
        {
            // Behaviour on Unix depends on whether stdin is a tty; covered by
            // integration tests, not this unit test.
            return;
        }

        UnixRawModeHelper helper = new ();

        bool enabled = helper.TryEnable ();

        Assert.False (enabled);
        Assert.False (helper.IsRawModeEnabled);
        Assert.False (GetHaveSavedTermios (helper));

        // Restore() after a failed TryEnable must not attempt a syscall.
        Exception? thrown = Record.Exception (() => helper.Restore ());
        Assert.Null (thrown);
    }

    [Fact]
    public void TryEnable_WhenSucceeds_HooksProcessExit_AndDisposeUnhooks ()
    {
        // This test is meaningful only when raw mode actually enables (real tty
        // on Unix). On other platforms or when stdin is redirected, TryEnable
        // returns false and we have nothing to assert.
        UnixRawModeHelper helper = new ();

        if (!helper.TryEnable ())
        {
            return;
        }

        try
        {
            Assert.True (helper.IsRawModeEnabled);
            Assert.True (GetHaveSavedTermios (helper));
            Assert.NotNull (GetProcessExitHandler (helper));
        }
        finally
        {
            helper.Dispose ();
        }

        Assert.False (helper.IsRawModeEnabled);
        Assert.Null (GetProcessExitHandler (helper));
    }

    private static bool GetHaveSavedTermios (UnixRawModeHelper helper)
    {
        FieldInfo field = typeof (UnixRawModeHelper).GetField (
                                                              "_haveSavedTermios",
                                                              BindingFlags.Instance | BindingFlags.NonPublic)!;

        return (bool)field.GetValue (helper)!;
    }

    private static object? GetProcessExitHandler (UnixRawModeHelper helper)
    {
        FieldInfo field = typeof (UnixRawModeHelper).GetField (
                                                              "_processExitHandler",
                                                              BindingFlags.Instance | BindingFlags.NonPublic)!;

        return field.GetValue (helper);
    }
}
