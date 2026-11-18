using UnitTests;

namespace ApplicationTests.Init;

/// <summary>
///     Comprehensive tests for ApplicationImpl.Begin/End logic that manages Current and SessionStack.
///     These tests ensure the fragile state management logic is robust and catches regressions.
///     Tests work directly with ApplicationImpl instances to avoid global Application state issues.
/// </summary>
[Collection ("Application Tests")]
public class InitTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Init_Unbalanced_Throws ()
    {
        using (TestLogging.Verbose (_output))
        {
            IApplication? app = Application.Create ();
            app.Init (DriverRegistry.Names.ANSI);

            Assert.Throws<InvalidOperationException> (() => app.Init (DriverRegistry.Names.ANSI));
        }
    }

    [Fact]
    public void Init_Null_Driver_Should_Pick_A_Driver ()
    {
        using (TestLogging.Verbose (_output))
        {
            IApplication app = Application.Create ();
            app.Init ();

            Assert.NotNull (app.Driver);

            app.Dispose ();
        }
    }

    [Fact]
    public void Init_Dispose_Cleans_Up ()
    {
        using (TestLogging.Verbose (_output))
        {
            IApplication app = Application.Create ();

            app.Init (DriverRegistry.Names.ANSI);

            app.Dispose ();
        }
    }

    [Fact]
    public void Init_Dispose_Fire_InitializedChanged ()
    {
        using (TestLogging.Verbose (_output))
        {
            var initialized = false;
            var Dispose = false;

            IApplication app = Application.Create ();

            app.InitializedChanged += OnApplicationOnInitializedChanged;

            app.Init (DriverRegistry.Names.ANSI);
            Assert.True (initialized);
            Assert.False (Dispose);

            app.Dispose ();
            Assert.True (initialized);
            Assert.True (Dispose);

            app.InitializedChanged -= OnApplicationOnInitializedChanged;

            return;

            void OnApplicationOnInitializedChanged (object? s, EventArgs<bool> a)
            {
                if (a.Value)
                {
                    initialized = true;
                }
                else
                {
                    Dispose = true;
                }
            }
        }
    }

    [Fact]
    public void Init_KeyBindings_Are_Not_Reset ()
    {
        using (TestLogging.Verbose (_output))
        {
            PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.Quit];

            try
            {
                IApplication app = Application.Create ();

                // Set via static DefaultKeyBindings (modern API)
                Application.DefaultKeyBindings [Command.Quit] = Bind.All (Key.Q);
                Assert.Equal (Key.Q, Application.GetDefaultKey (Command.Quit));

                app.Init (DriverRegistry.Names.ANSI);

                Assert.Equal (Key.Q, Application.GetDefaultKey (Command.Quit));

                app.Dispose ();
            }
            finally
            {
                Application.DefaultKeyBindings [Command.Quit] = original;
            }
        }
    }

    [Fact]
    public void Init_NoParam_ForceDriver_Works ()
    {
        using (TestLogging.Verbose (_output))
        {
            using IApplication app = Application.Create ();

            app.ForceDriver = DriverRegistry.Names.ANSI;

            // Note: Init() without params picks up driver configuration
            app.Init ();

            Assert.Equal (DriverRegistry.Names.ANSI, app.Driver!.GetName ());
        }
    }

    [Fact]
    public void Init_Invalid_DriverName_Throws ()
    {
        using (TestLogging.Verbose (_output))
        {
            using IApplication app = Application.Create ();
            Assert.Throws<ArgumentException> (() => app.Init ("nonexistent_driver"));
            Assert.Throws<ArgumentException> (() => app.Init ("fake"));
        }
    }

    [Fact]
    public void Init_Dispose_Resets_Instance_Properties ()
    {
        using (TestLogging.Verbose (_output))
        {
            IApplication app = Application.Create ();

            // Init the app
            app.Init (DriverRegistry.Names.ANSI);

            // Verify initialized
            Assert.True (app.Initialized);
            Assert.NotNull (app.Driver);

            // Dispose cleans up
            app.Dispose ();

            // Check reset state on the instance
            CheckReset (app);

            // Create a new instance and set values
            app = Application.Create ();
            app.Init (DriverRegistry.Names.ANSI);

            // Save static DefaultKeyBindings entries before mutation
            PlatformKeyBinding origPrevTabGroup = Application.DefaultKeyBindings! [Command.PreviousTabGroup];
            PlatformKeyBinding origNextTabGroup = Application.DefaultKeyBindings! [Command.NextTabGroup];
            PlatformKeyBinding origQuit = Application.DefaultKeyBindings! [Command.Quit];

            try
            {
                app.StopAfterFirstIteration = true;
                Application.DefaultKeyBindings! [Command.PreviousTabGroup] = Bind.All (Key.A);
                Application.DefaultKeyBindings! [Command.NextTabGroup] = Bind.All (Key.B);
                Application.DefaultKeyBindings! [Command.Quit] = Bind.All (Key.C);
                app.Keyboard.KeyBindings.Add (Key.D, Command.Cancel);

                app.Mouse.CachedViewsUnderMouse.Clear ();
                app.Mouse.LastMousePosition = new Point (1, 1);

                // Dispose and check reset
                app.Dispose ();
                CheckReset (app);
            }
            finally
            {
                // Restore static DefaultKeyBindings to avoid polluting other parallel tests
                Application.DefaultKeyBindings! [Command.PreviousTabGroup] = origPrevTabGroup;
                Application.DefaultKeyBindings! [Command.NextTabGroup] = origNextTabGroup;
                Application.DefaultKeyBindings! [Command.Quit] = origQuit;
            }
        }

        return;

        void CheckReset (IApplication application)
        {
            // Check that all fields and properties are reset on the instance

            // Public Properties
            Assert.Null (application.TopRunnableView);
            Assert.Null (application.Driver);
            Assert.False (application.StopAfterFirstIteration);

            // Internal properties
            Assert.False (application.Initialized);
            Assert.Null (application.MainThreadId);
            Assert.Empty (application.Mouse.CachedViewsUnderMouse);
        }
    }
}
