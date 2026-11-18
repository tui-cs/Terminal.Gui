// Claude - Opus 4.7
#nullable enable

namespace UnitTests.NonParallelizable.ApplicationTests;

/// <summary>
///     Tests asserting <see cref="IApplication.Invoke(Action)"/> and
///     <see cref="IApplication.Invoke(System.Action{IApplication})"/> contract relative to the
///     <see cref="IApplication.Init"/> / <see cref="IDisposable.Dispose"/> lifecycle.
///     Regression coverage for issue #5163.
/// </summary>
public class ApplicationInvokeLifecycleTests
{
    [Fact]
    public void Invoke_Action_BeforeInit_Throws_NotInitializedException ()
    {
        IApplication app = Application.Create ();

        try
        {
            Assert.Throws<NotInitializedException> (() => app.Invoke (() => { }));
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void Invoke_ActionOfApplication_BeforeInit_Throws_NotInitializedException ()
    {
        IApplication app = Application.Create ();

        try
        {
            Assert.Throws<NotInitializedException> (() => app.Invoke (static _ => { }));
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void Invoke_Action_AfterDispose_Throws_NotInitializedException ()
    {
        IApplication app = Application.Create ();

        try
        {
            app.Init (DriverRegistry.Names.ANSI);
        }
        finally
        {
            app.Dispose ();
        }

        Assert.Throws<NotInitializedException> (() => app.Invoke (() => { }));
    }

    [Fact]
    public void Invoke_ActionOfApplication_AfterDispose_Throws_NotInitializedException ()
    {
        IApplication app = Application.Create ();

        try
        {
            app.Init (DriverRegistry.Names.ANSI);
        }
        finally
        {
            app.Dispose ();
        }

        Assert.Throws<NotInitializedException> (() => app.Invoke (static _ => { }));
    }
}
