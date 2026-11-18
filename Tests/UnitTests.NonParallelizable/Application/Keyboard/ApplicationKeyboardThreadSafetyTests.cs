#nullable enable
// ReSharper disable AccessToDisposedClosure

namespace UnitTests.NonParallelizable.ApplicationTests.Keyboard;

/// <summary>
///     Tests to verify that ApplicationKeyboard is thread-safe for concurrent access scenarios.
/// </summary>
[Collection ("Application Tests")]
public class ApplicationKeyboardThreadSafetyTests
{
    [Fact]
    public void AddCommand_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();
        List<Exception> exceptions = [];
        const int NUM_THREADS = 10;
        const int OPERATIONS_PER_THREAD = 50;

        // Act
        List<Task> tasks = [];

        for (var i = 0; i < NUM_THREADS; i++)
        {
            tasks.Add (Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             // AddKeyBindings internally calls AddCommand multiple times
                                             keyboard.AddKeyBindings ();
                                         }
                                         catch (InvalidOperationException)
                                         {
                                             // Expected - AddKeyBindings tries to add keys that already exist
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 },
                                 TestContext.Current.CancellationToken));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray (), TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
    }

    [Fact]
    public void Dispose_WhileOperationsInProgress_NoExceptions ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var keyboard = new ApplicationKeyboard { App = app };
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        var continueRunning = true;

        // Act
        Task operationsTask = Task.Run (() =>
                                        {
                                            while (continueRunning)
                                            {
                                                try
                                                {
                                                    keyboard.InvokeCommandsBoundToKey (Key.Q.WithCtrl);
                                                    IEnumerable<KeyValuePair<Key, KeyBinding>> bindings = keyboard.KeyBindings.GetBindings ();
                                                    int count = bindings.Count ();
                                                }
                                                catch (ObjectDisposedException)
                                                {
                                                    // Expected - keyboard was disposed
                                                    break;
                                                }
                                                catch (Exception ex)
                                                {
                                                    exceptions.Add (ex);

                                                    break;
                                                }
                                            }
                                        },
                                        TestContext.Current.CancellationToken);

        // Give operations a chance to start
        Thread.Sleep (10);

        // Dispose while operations are running
        keyboard.Dispose ();
        continueRunning = false;

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        operationsTask.Wait (TimeSpan.FromSeconds (2), TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        app.Dispose ();
    }

    [Fact]
    public void InvokeCommand_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var keyboard = new ApplicationKeyboard { App = app };
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        const int NUM_THREADS = 10;
        const int OPERATIONS_PER_THREAD = 50;

        // Act
        List<Task> tasks = new ();

        for (var i = 0; i < NUM_THREADS; i++)
        {
            tasks.Add (Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             var binding = new KeyBinding ([Command.Quit]);
                                             keyboard.InvokeCommand (Command.Quit, Key.Q.WithCtrl, binding);
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 },
                                 TestContext.Current.CancellationToken));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray (), TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void InvokeCommandsBoundToKey_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var keyboard = new ApplicationKeyboard { App = app };
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        const int NUM_THREADS = 10;
        const int OPERATIONS_PER_THREAD = 50;

        // Act
        List<Task> tasks = [];

        for (var i = 0; i < NUM_THREADS; i++)
        {
            tasks.Add (Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             keyboard.InvokeCommandsBoundToKey (Key.Q.WithCtrl);
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 },
                                 TestContext.Current.CancellationToken));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray (), TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void KeyBindings_ConcurrentAdd_NoExceptions ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();

        // Don't call AddKeyBindings here to avoid conflicts
        List<Exception> exceptions = [];
        const int NUM_THREADS = 10;
        const int OPERATIONS_PER_THREAD = 50;

        // Act
        List<Task> tasks = new ();

        for (var i = 0; i < NUM_THREADS; i++)
        {
            int threadId = i;

            tasks.Add (Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             // Use unique keys per thread to avoid conflicts
                                             Key key = Key.F1 + threadId * OPERATIONS_PER_THREAD + j;
                                             keyboard.KeyBindings.Add (key, Command.Refresh);
                                         }
                                         catch (InvalidOperationException)
                                         {
                                             // Expected - duplicate key
                                         }
                                         catch (ArgumentException)
                                         {
                                             // Expected - invalid key
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 },
                                 TestContext.Current.CancellationToken));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray (), TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
    }

    [Fact]
    public void KeyDown_Events_ConcurrentSubscription_NoExceptions ()
    {
        // Arrange
        var keyboard = new ApplicationKeyboard ();
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        const int NUM_THREADS = 10;
        const int OPERATIONS_PER_THREAD = 20;
        var keyDownCount = 0;

        // Act
        List<Task> tasks = new ();

        // Threads subscribing to events
        for (var i = 0; i < NUM_THREADS; i++)
        {
            tasks.Add (Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             EventHandler<Key> handler = (s, e) => { Interlocked.Increment (ref keyDownCount); };
                                             keyboard.KeyDown += handler;
                                             keyboard.KeyDown -= handler;
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 },
                                 TestContext.Current.CancellationToken));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray (), TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
    }

    [Fact]
    public void KeyProperty_Setters_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        // Save original bindings so parallel mutation doesn't pollute other tests
        Dictionary<Command, PlatformKeyBinding>? savedBindings = Application.DefaultKeyBindings is { }
                                                                     ? new Dictionary<Command, PlatformKeyBinding> (Application.DefaultKeyBindings)
                                                                     : null;
        var keyboard = new ApplicationKeyboard ();

        // Initialize once before concurrent access
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        const int NUM_THREADS = 10;
        const int OPERATIONS_PER_THREAD = 20;

        // Act
        List<Task> tasks = [];

        for (var i = 0; i < NUM_THREADS; i++)
        {
            int threadId = i;

            tasks.Add (Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             // Cycle through different key combinations
                                             switch (j % 6)
                                             {
                                                 case 0:
                                                     Application.DefaultKeyBindings! [Command.Quit] = Bind.All (Key.Q.WithCtrl);

                                                     break;

                                                 case 1:
                                                     Application.DefaultKeyBindings! [Command.Arrange] = Bind.All (Key.F6.WithCtrl);

                                                     break;

                                                 case 2:
                                                     Application.DefaultKeyBindings! [Command.NextTabStop] = Bind.All (Key.Tab);

                                                     break;

                                                 case 3:
                                                     Application.DefaultKeyBindings! [Command.PreviousTabStop] = Bind.All (Key.Tab.WithShift);

                                                     break;

                                                 case 4:
                                                     Application.DefaultKeyBindings! [Command.NextTabGroup] = Bind.All (Key.F6);

                                                     break;

                                                 case 5:
                                                     Application.DefaultKeyBindings! [Command.PreviousTabGroup] = Bind.All (Key.F6.WithShift);

                                                     break;
                                             }
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 },
                                 TestContext.Current.CancellationToken));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray (), TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();

        // Restore original bindings
        if (savedBindings is { })
        {
            foreach (KeyValuePair<Command, PlatformKeyBinding> kvp in savedBindings)
            {
                Application.DefaultKeyBindings! [kvp.Key] = kvp.Value;
            }
        }
    }

    [Fact]
    public void MixedOperations_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        PlatformKeyBinding origQuit = Application.DefaultKeyBindings! [Command.Quit];
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var keyboard = new ApplicationKeyboard { App = app };
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        const int OPERATIONS_PER_THREAD = 30;

        // Act
        List<Task> tasks = new ();

        // Thread 1: Add bindings with unique keys
        tasks.Add (Task.Run (() =>
                             {
                                 for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                 {
                                     try
                                     {
                                         // Use high key codes to avoid conflicts
                                         var key = new Key ((KeyCode)((int)KeyCode.F20 + j));
                                         keyboard.KeyBindings.Add (key, Command.Refresh);
                                     }
                                     catch (InvalidOperationException)
                                     {
                                         // Expected - duplicate
                                     }
                                     catch (ArgumentException)
                                     {
                                         // Expected - invalid key
                                     }
                                     catch (Exception ex)
                                     {
                                         exceptions.Add (ex);
                                     }
                                 }
                             },
                             TestContext.Current.CancellationToken));

        // Thread 2: Invoke commands
        tasks.Add (Task.Run (() =>
                             {
                                 for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                 {
                                     try
                                     {
                                         keyboard.InvokeCommandsBoundToKey (Key.Q.WithCtrl);
                                     }
                                     catch (Exception ex)
                                     {
                                         exceptions.Add (ex);
                                     }
                                 }
                             },
                             TestContext.Current.CancellationToken));

        // Thread 3: Read bindings
        tasks.Add (Task.Run (() =>
                             {
                                 for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                 {
                                     try
                                     {
                                         IEnumerable<KeyValuePair<Key, KeyBinding>> bindings = keyboard.KeyBindings.GetBindings ();
                                         int count = bindings.Count ();
                                         Assert.True (count >= 0);
                                     }
                                     catch (Exception ex)
                                     {
                                         exceptions.Add (ex);
                                     }
                                 }
                             },
                             TestContext.Current.CancellationToken));

        // Thread 4: Change key properties
        tasks.Add (Task.Run (() =>
                             {
                                 for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                 {
                                     try
                                     {
                                         Application.DefaultKeyBindings! [Command.Quit] = Bind.All (j % 2 == 0 ? Key.Q.WithCtrl : Key.Esc);
                                     }
                                     catch (Exception ex)
                                     {
                                         exceptions.Add (ex);
                                     }
                                 }
                             },
                             TestContext.Current.CancellationToken));

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray (), TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
        app.Dispose ();

        // Restore static DefaultKeyBindings to avoid polluting other tests
        Application.DefaultKeyBindings! [Command.Quit] = origQuit;
    }

    [Fact]
    public void RaiseKeyDownEvent_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        var keyboard = new ApplicationKeyboard { App = app };
        keyboard.AddKeyBindings ();
        List<Exception> exceptions = [];
        const int NUM_THREADS = 5;
        const int OPERATIONS_PER_THREAD = 20;

        // Act
        List<Task> tasks = new ();

        for (var i = 0; i < NUM_THREADS; i++)
        {
            tasks.Add (Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             keyboard.RaiseKeyDownEvent (Key.A);
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 },
                                 TestContext.Current.CancellationToken));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (tasks.ToArray (), TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
        keyboard.Dispose ();
        app.Dispose ();
    }
}
