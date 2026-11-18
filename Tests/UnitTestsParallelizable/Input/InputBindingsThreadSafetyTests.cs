namespace InputTests;

/// <summary>
///     Tests to verify that CommandBindings (KeyBindings and MouseBindings) are thread-safe
///     for concurrent access scenarios.
/// </summary>
public class CommandBindingsThreadSafetyTests
{
    [Fact]
    public void Add_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        var bindings = new TestCommandBindings ();
        const int NUM_THREADS = 10;
        const int ITEMS_PER_THREAD = 100;

        // Act
        Parallel.For (0,
                      NUM_THREADS,
                      i =>
                      {
                          for (var j = 0; j < ITEMS_PER_THREAD; j++)
                          {
                              var key = $"key_{i}_{j}";

                              try
                              {
                                  bindings.Add (key, Command.Accept);
                              }
                              catch (InvalidOperationException)
                              {
                                  // Expected if duplicate key - this is OK
                              }
                          }
                      });

        // Assert
        IEnumerable<KeyValuePair<string, KeyBinding>> allBindings = bindings.GetBindings ();
        Assert.NotEmpty (allBindings);
        Assert.True (allBindings.Count () <= NUM_THREADS * ITEMS_PER_THREAD);
    }

    [Fact]
    public void Clear_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        var bindings = new TestCommandBindings ();
        const int NUM_THREADS = 10;

        // Populate initial data
        for (var i = 0; i < 100; i++)
        {
            bindings.Add ($"key_{i}", Command.Accept);
        }

        // Act - Multiple threads clearing simultaneously
        Parallel.For (0,
                      NUM_THREADS,
                      i =>
                      {
                          try
                          {
                              bindings.Clear ();
                          }
                          catch (Exception ex)
                          {
                              Assert.Fail ($"Clear should not throw: {ex.Message}");
                          }
                      });

        // Assert
        Assert.Empty (bindings.GetBindings ());
    }

    [Fact]
    public void GetAllFromCommands_DuringModification_NoExceptions ()
    {
        // Arrange
        var bindings = new TestCommandBindings ();
        var continueRunning = true;
        List<Exception> exceptions = new ();
        const int MAX_ADDITIONS = 200; // Limit total additions to prevent infinite loop

        // Populate initial data
        for (var i = 0; i < 50; i++)
        {
            bindings.Add ($"key_{i}", Command.Accept);
        }

        // Act - Modifier thread
        Task modifierTask = Task.Run (() =>
                                      {
                                          var counter = 50;

                                          while (continueRunning && counter < MAX_ADDITIONS)
                                          {
                                              try
                                              {
                                                  bindings.Add ($"key_{counter++}", Command.Accept);
                                                  Thread.Sleep (1); // Small delay to prevent CPU spinning
                                              }
                                              catch (InvalidOperationException)
                                              {
                                                  // Expected
                                              }
                                          }
                                      },
                                      TestContext.Current.CancellationToken);

        // Act - Reader threads
        List<Task> readerTasks = new ();

        for (var i = 0; i < 5; i++)
        {
            readerTasks.Add (Task.Run (() =>
                                       {
                                           for (var j = 0; j < 50; j++)
                                           {
                                               try
                                               {
                                                   IEnumerable<string> results = bindings.GetAllFromCommands (Command.Accept);
                                                   int count = results.Count ();
                                                   Assert.True (count >= 0);
                                               }
                                               catch (Exception ex)
                                               {
                                                   exceptions.Add (ex);
                                               }

                                               Thread.Sleep (1); // Small delay between iterations
                                           }
                                       },
                                       TestContext.Current.CancellationToken));
        }

#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (readerTasks.ToArray (), TestContext.Current.CancellationToken);
        continueRunning = false;
        modifierTask.Wait (TimeSpan.FromSeconds (5), TestContext.Current.CancellationToken); // Add timeout to prevent indefinite hang
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
    }

    [Fact]
    public void GetBindings_DuringConcurrentModification_NoExceptions ()
    {
        // Arrange
        var bindings = new TestCommandBindings ();
        var continueRunning = true;
        List<Exception> exceptions = new ();
        const int MAX_MODIFICATIONS = 200; // Limit total modifications

        // Populate some initial data
        for (var i = 0; i < 50; i++)
        {
            bindings.Add ($"initial_{i}", Command.Accept);
        }

        // Act - Start modifier thread
        Task modifierTask = Task.Run (() =>
                                      {
                                          var counter = 0;

                                          while (continueRunning && counter < MAX_MODIFICATIONS)
                                          {
                                              try
                                              {
                                                  bindings.Add ($"key_{counter++}", Command.Cancel);
                                              }
                                              catch (InvalidOperationException)
                                              {
                                                  // Expected - duplicate key
                                              }
                                              catch (Exception ex)
                                              {
                                                  exceptions.Add (ex);
                                              }

                                              if (counter % 10 == 0)
                                              {
                                                  bindings.Clear (Command.Accept);
                                              }

                                              Thread.Sleep (1); // Small delay to prevent CPU spinning
                                          }
                                      },
                                      TestContext.Current.CancellationToken);

        // Act - Start reader threads
        List<Task> readerTasks = new ();

        for (var i = 0; i < 5; i++)
        {
            readerTasks.Add (Task.Run (() =>
                                       {
                                           for (var j = 0; j < 100; j++)
                                           {
                                               try
                                               {
                                                   // This should never throw "Collection was modified" exception
                                                   IEnumerable<KeyValuePair<string, KeyBinding>> snapshot = bindings.GetBindings ();
                                                   int count = snapshot.Count ();
                                                   Assert.True (count >= 0);
                                               }
                                               catch (InvalidOperationException ex) when (ex.Message.Contains ("Collection was modified"))
                                               {
                                                   exceptions.Add (ex);
                                               }
                                               catch (Exception ex)
                                               {
                                                   exceptions.Add (ex);
                                               }

                                               Thread.Sleep (1); // Small delay between iterations
                                           }
                                       },
                                       TestContext.Current.CancellationToken));
        }

        // Wait for readers to complete
#pragma warning disable xUnit1031 // Test methods should not use blocking task operations - intentional for stress testing
        Task.WaitAll (readerTasks.ToArray (), TestContext.Current.CancellationToken);
        continueRunning = false;
        modifierTask.Wait (TimeSpan.FromSeconds (5), TestContext.Current.CancellationToken); // Add timeout to prevent indefinite hang
#pragma warning restore xUnit1031

        // Assert
        Assert.Empty (exceptions);
    }

    [Fact]
    public void KeyBindings_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        var view = new View ();
        KeyBindings keyBindings = view.KeyBindings;
        List<Exception> exceptions = new ();
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
                                             Key key = Key.A.WithShift.WithCtrl + threadId + j;
                                             keyBindings.Add (key, Command.Accept);
                                         }
                                         catch (InvalidOperationException)
                                         {
                                             // Expected - duplicate or invalid key
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
        IEnumerable<KeyValuePair<Key, KeyBinding>> bindings = keyBindings.GetBindings ();
        Assert.NotEmpty (bindings);

        view.Dispose ();
    }

    [Fact]
    public void MixedOperations_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        var bindings = new TestCommandBindings ();
        List<Exception> exceptions = new ();
        const int OPERATIONS_PER_THREAD = 100;

        // Act - Multiple threads doing various operations
        List<Task> tasks = new ();

        // Adder threads
        for (var i = 0; i < 3; i++)
        {
            int threadId = i;

            tasks.Add (Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             bindings.Add ($"add_{threadId}_{j}", Command.Accept);
                                         }
                                         catch (InvalidOperationException)
                                         {
                                             // Expected - duplicate
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 },
                                 TestContext.Current.CancellationToken));
        }

        // Reader threads
        for (var i = 0; i < 3; i++)
        {
            tasks.Add (Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             IEnumerable<KeyValuePair<string, KeyBinding>> snapshot = bindings.GetBindings ();
                                             int count = snapshot.Count ();
                                             Assert.True (count >= 0);
                                         }
                                         catch (Exception ex)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 },
                                 TestContext.Current.CancellationToken));
        }

        // Remover threads
        for (var i = 0; i < 2; i++)
        {
            int threadId = i;

            tasks.Add (Task.Run (() =>
                                 {
                                     for (var j = 0; j < OPERATIONS_PER_THREAD; j++)
                                     {
                                         try
                                         {
                                             bindings.Remove ($"add_{threadId}_{j}");
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
    }

    [Fact]
    public void MouseBindings_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        var view = new View ();
        MouseBindings mouseBindings = view.MouseBindings;
        List<Exception> exceptions = new ();
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
                                             MouseFlags flags = MouseFlags.LeftButtonClicked | (MouseFlags)(threadId * 1000 + j);
                                             mouseBindings.Add (flags, Command.Accept);
                                         }
                                         catch (InvalidOperationException)
                                         {
                                             // Expected - duplicate or invalid flags
                                         }
                                         catch (ArgumentException)
                                         {
                                             // Expected - invalid mouse flags
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

        view.Dispose ();
    }

    [Fact]
    public void Remove_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        var bindings = new TestCommandBindings ();
        const int NUM_ITEMS = 100;

        // Populate data
        for (var i = 0; i < NUM_ITEMS; i++)
        {
            bindings.Add ($"key_{i}", Command.Accept);
        }

        // Act - Multiple threads removing items
        Parallel.For (0,
                      NUM_ITEMS,
                      i =>
                      {
                          try
                          {
                              bindings.Remove ($"key_{i}");
                          }
                          catch (Exception ex)
                          {
                              Assert.Fail ($"Remove should not throw: {ex.Message}");
                          }
                      });

        // Assert
        Assert.Empty (bindings.GetBindings ());
    }

    [Fact]
    public void Replace_ConcurrentAccess_NoExceptions ()
    {
        // Arrange
        var bindings = new TestCommandBindings ();
        const string OLD_KEY = "old_key";
        const string NEW_KEY = "new_key";

        bindings.Add (OLD_KEY, Command.Accept);

        // Act - Multiple threads trying to replace
        List<Exception> exceptions = new ();

        Parallel.For (0,
                      10,
                      i =>
                      {
                          try
                          {
                              bindings.Replace (OLD_KEY, $"{NEW_KEY}_{i}");
                          }
                          catch (InvalidOperationException)
                          {
                              // Expected - key might already be replaced
                          }
                          catch (Exception ex)
                          {
                              exceptions.Add (ex);
                          }
                      });

        // Assert
        Assert.Empty (exceptions);
    }

    [Fact]
    public void TryGet_ConcurrentAccess_ReturnsConsistentResults ()
    {
        // Arrange
        var bindings = new TestCommandBindings ();
        const string TEST_KEY = "test_key";

        bindings.Add (TEST_KEY, Command.Accept);

        // Act
        var results = new bool [100];

        Parallel.For (0, 100, i => { results [i] = bindings.TryGet (TEST_KEY, out _); });

        // Assert - All threads should consistently find the binding
        Assert.All (results, result => Assert.True (result));
    }

    /// <summary>
    ///     Test implementation of CommandBindings for testing purposes.
    /// </summary>
    private class TestCommandBindings ()
        : CommandBindingsBase<string, KeyBinding> ((commands, evt, source) => new KeyBinding { Commands = commands, Key = Key.Empty },
                                             StringComparer.OrdinalIgnoreCase)
    {
        public override bool IsValid (string eventArgs) => !string.IsNullOrEmpty (eventArgs);
    }
}
