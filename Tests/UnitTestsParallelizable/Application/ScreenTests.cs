namespace ApplicationTests.Screen;

/// <summary>
///     Parallelizable tests for IApplication.ScreenChanged event and Screen property.
///     Tests using the modern instance-based IApplication API.
/// </summary>
[Collection("Application Tests")]
public class ScreenTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region ScreenChanged Event Tests

    [Fact]
    public void Screen_Size_Changes ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        IDriver? driver = app.Driver;

        app.Driver!.SetScreenSize (80, 25);

        Assert.Equal (new (0, 0, 80, 25), driver!.Screen);
        Assert.Equal (new (0, 0, 80, 25), app.Screen);

        // TODO: Should not be possible to manually change these at whim!
        driver.Cols = 100;
        driver.Rows = 30;

        app.Driver!.SetScreenSize (100, 30);

        Assert.Equal (new (0, 0, 100, 30), driver.Screen);

        app.Screen = new (0, 0, driver.Cols, driver.Rows);
        Assert.Equal (new (0, 0, 100, 30), driver.Screen);

        app.Dispose ();
    }

    [Fact]
    public void ScreenChanged_Event_Fires_When_Driver_Size_Changes ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var eventFired = false;
        Rectangle? newScreen = null;

        EventHandler<EventArgs<Rectangle>> handler = (sender, args) =>
                                                     {
                                                         eventFired = true;
                                                         newScreen = args.Value;
                                                     };

        app.ScreenChanged += handler;

        try
        {
            // Act
            app.Driver!.SetScreenSize (100, 40);

            // Assert
            Assert.True (eventFired);
            Assert.NotNull (newScreen);
            Assert.Equal (new (0, 0, 100, 40), newScreen.Value);
        }
        finally
        {
            app.ScreenChanged -= handler;
        }
    }

    [Fact]
    public void ScreenChanged_Event_Updates_Application_Screen_Property ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Rectangle initialScreen = app.Screen;
        Assert.Equal (new (0, 0, 80, 25), initialScreen);

        // Act
        app.Driver!.SetScreenSize (120, 50);

        // Assert
        Assert.Equal (new (0, 0, 120, 50), app.Screen);
    }

    [Fact]
    public void ScreenChanged_Event_Sender_Is_IApplication ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        object? eventSender = null;

        EventHandler<EventArgs<Rectangle>> handler = (sender, args) => { eventSender = sender; };

        app.ScreenChanged += handler;

        try
        {
            // Act
            app.Driver!.SetScreenSize (100, 30);

            // Assert
            Assert.NotNull (eventSender);
            Assert.IsAssignableFrom<IApplication> (eventSender);
        }
        finally
        {
            app.ScreenChanged -= handler;
        }
    }

    [Fact]
    public void ScreenChanged_Event_Provides_Correct_Rectangle_In_EventArgs ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Rectangle? capturedRectangle = null;

        EventHandler<EventArgs<Rectangle>> handler = (sender, args) => { capturedRectangle = args.Value; };

        app.ScreenChanged += handler;

        try
        {
            // Act
            app.Driver!.SetScreenSize (200, 60);

            // Assert
            Assert.NotNull (capturedRectangle);
            Assert.Equal (0, capturedRectangle.Value.X);
            Assert.Equal (0, capturedRectangle.Value.Y);
            Assert.Equal (200, capturedRectangle.Value.Width);
            Assert.Equal (60, capturedRectangle.Value.Height);
        }
        finally
        {
            app.ScreenChanged -= handler;
        }
    }

    [Fact]
    public void ScreenChanged_Event_Fires_Multiple_Times_For_Multiple_Resizes ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var eventCount = 0;
        List<Size> sizes = new ();

        EventHandler<EventArgs<Rectangle>> handler = (sender, args) =>
                                                     {
                                                         eventCount++;
                                                         sizes.Add (args.Value.Size);
                                                     };

        app.ScreenChanged += handler;

        try
        {
            // Act
            app.Driver!.SetScreenSize (100, 30);
            app.Driver!.SetScreenSize (120, 40);
            app.Driver!.SetScreenSize (80, 25);

            // Assert
            Assert.Equal (3, eventCount);
            Assert.Equal (3, sizes.Count);
            Assert.Equal (new (100, 30), sizes [0]);
            Assert.Equal (new (120, 40), sizes [1]);
            Assert.Equal (new (80, 25), sizes [2]);
        }
        finally
        {
            app.ScreenChanged -= handler;
        }
    }

    [Fact]
    public void ScreenChanged_Event_Does_Not_Fire_When_No_Resize_Occurs ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var eventFired = false;

        EventHandler<EventArgs<Rectangle>> handler = (sender, args) => { eventFired = true; };

        app.ScreenChanged += handler;

        try
        {
            // Act - Don't resize, just access Screen property
            Rectangle screen = app.Screen;

            // Assert
            Assert.False (eventFired);
            Assert.Equal (new (0, 0, 80, 25), screen);
        }
        finally
        {
            app.ScreenChanged -= handler;
        }
    }

    [Fact]
    public void ScreenChanged_Event_Can_Be_Unsubscribed ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var eventCount = 0;

        EventHandler<EventArgs<Rectangle>> handler = (sender, args) => { eventCount++; };

        app.ScreenChanged += handler;

        // Act - First resize should fire
        app.Driver!.SetScreenSize (100, 30);
        Assert.Equal (1, eventCount);

        // Unsubscribe
        app.ScreenChanged -= handler;

        // Second resize should not fire
        app.Driver!.SetScreenSize (120, 40);

        // Assert
        Assert.Equal (1, eventCount);
    }

    [Fact]
    public void ScreenChanged_Event_Sets_Runnables_To_NeedsLayout ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using var runnable = new Runnable ();
        SessionToken? token = app.Begin (runnable);

        Assert.NotNull (app.TopRunnableView);
        app.LayoutAndDraw ();

        // Clear the NeedsLayout flag
        Assert.False (app.TopRunnableView.NeedsLayout);

        try
        {
            // Act
            app.Driver!.SetScreenSize (100, 30);

            // Assert
            Assert.True (app.TopRunnableView.NeedsLayout);
        }
        finally
        {
            // Cleanup
            if (token is { })
            {
                app.End (token);
            }
        }
    }

    [Fact]
    public void ScreenChanged_Event_Handles_Multiple_Runnables_In_Session_Stack ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using var runnable1 = new Runnable ();
        SessionToken? token1 = app.Begin (runnable1);
        app.LayoutAndDraw ();

        using var runnable2 = new Runnable ();
        SessionToken? token2 = app.Begin (runnable2);
        app.LayoutAndDraw ();

        // Both should not need layout after drawing
        Assert.False (runnable1.NeedsLayout);
        Assert.False (runnable2.NeedsLayout);

        try
        {
            // Act - Resize should mark both as needing layout
            app.Driver!.SetScreenSize (100, 30);

            // Assert
            Assert.True (runnable1.NeedsLayout);
            Assert.True (runnable2.NeedsLayout);
        }
        finally
        {
            // Cleanup
            if (token2 is { })
            {
                app.End (token2);
            }

            if (token1 is { })
            {
                app.End (token1);
            }
        }
    }

    [Fact]
    public void ScreenChanged_Event_With_No_Active_Runnables_Does_Not_Throw ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var eventFired = false;

        EventHandler<EventArgs<Rectangle>> handler = (sender, args) => { eventFired = true; };

        app.ScreenChanged += handler;

        try
        {
            // Act - Resize with no runnables
            Exception? exception = Record.Exception (() => app.Driver!.SetScreenSize (100, 30));

            // Assert
            Assert.Null (exception);
            Assert.True (eventFired);
        }
        finally
        {
            app.ScreenChanged -= handler;
        }
    }

    #endregion ScreenChanged Event Tests

    #region Screen Property Tests

    [Fact]
    public void Screen_Property_Returns_Driver_Screen_When_Not_Set ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Act
        Rectangle screen = app.Screen;

        // Assert
        Assert.Equal (app.Driver!.Screen, screen);
        Assert.Equal (new (0, 0, 80, 25), screen);
    }

    [Fact]
    public void Screen_Property_Returns_Default_Size_When_Driver_Not_Initialized ()
    {
        // Arrange
        using IApplication app = Application.Create ();

        // Act - Don't call Init
        Rectangle screen = app.Screen;

        // Assert - Should return default size
        Assert.Equal (new (0, 0, 2048, 2048), screen);
    }

    // Copilot
    [Fact]
    public void Screen_Property_Allows_Setting_Non_Zero_Origin_In_Fullscreen ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Act — in fullscreen mode, non-zero origin is stored but Driver.SetScreenSize is
        // called with the new dimensions (the origin is ignored by the driver).
        Exception? exception = Record.Exception (() =>
                                                     app.Screen = new (10, 10, 80, 25));

        // Assert — no exception is thrown; the setter accepts non-zero origins.
        Assert.Null (exception);
    }

    [Fact]
    public void Screen_Property_Allows_Setting_With_Zero_Origin ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Act
        Exception? exception = Record.Exception (() =>
                                                     app.Screen = new (0, 0, 100, 50));

        // Assert
        Assert.Null (exception);
        Assert.Equal (new (0, 0, 100, 50), app.Screen);
    }

    // Copilot
    [Fact]
    public void Screen_Property_Setting_Before_Begin_Raises_ScreenChanged_Once ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        int eventCount = 0;
        Rectangle? newScreen = null;

        EventHandler<EventArgs<Rectangle>> handler = (_, args) =>
                                                     {
                                                         eventCount++;
                                                         newScreen = args.Value;
                                                     };

        app.ScreenChanged += handler;

        try
        {
            // Act
            app.Screen = new (0, 0, 100, 50);

            // Assert
            Assert.Equal (1, eventCount);
            Assert.Equal (new (0, 0, 100, 50), newScreen);
            Assert.Equal (new (0, 0, 100, 50), app.Screen);
        }
        finally
        {
            app.ScreenChanged -= handler;
        }
    }

    [Fact]
    public void Screen_Property_Setting_Raises_ScreenChanged_Event ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var eventFired = false;

        EventHandler<EventArgs<Rectangle>> handler = (sender, args) => { eventFired = true; };

        app.ScreenChanged += handler;

        try
        {
            // Act - Manually set Screen property 
            app.Screen = new (0, 0, 100, 50);

            Assert.True (eventFired);
            Assert.Equal (new (0, 0, 100, 50), app.Screen);
        }
        finally
        {
            app.ScreenChanged -= handler;
        }
    }

    [Fact]
    public void Screen_Property_Thread_Safe_Access ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        List<Exception> exceptions = new ();
        List<Task> tasks = new ();

        // Act - Access Screen property from multiple threads
        for (var i = 0; i < 10; i++)
        {
            tasks.Add (Task.Run (() =>
                                 {
                                     try
                                     {
                                         Rectangle screen = app.Screen;
                                         Assert.NotEqual (Rectangle.Empty, screen);
                                     }
                                     catch (Exception ex)
                                     {
                                         lock (exceptions)
                                         {
                                             exceptions.Add (ex);
                                         }
                                     }
                                 },
                                 TestContext.Current.CancellationToken));
        }

#pragma warning disable xUnit1031
        Task.WaitAll (tasks.ToArray (), TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031

        // Assert - No exceptions should occur
        Assert.Empty (exceptions);
    }

    #endregion Screen Property Tests

    // Copilot
    [Fact]
    public void Screen_Setter_Before_Driver_Init_Fires_ScreenChanged_Without_Delegating_To_Driver ()
    {
        // Arrange — Create app but do NOT call Init, so Driver remains null.
        using IApplication app = Application.Create ();

        // Verify precondition: Driver is null before Init.
        Assert.Null (app.Driver);

        var eventCount = 0;
        Rectangle? reported = null;

        EventHandler<EventArgs<Rectangle>> handler = (_, args) =>
                                                     {
                                                         eventCount++;
                                                         reported = args.Value;
                                                     };

        app.ScreenChanged += handler;

        try
        {
            // Act — setting Screen with no Driver should still raise ScreenChanged.
            app.Screen = new (0, 0, 120, 40);

            // Assert — event fired exactly once with the default getter value (2048x2048)
            // because _screen is reset to null in fullscreen mode and Driver is null.
            Assert.Equal (1, eventCount);
            Assert.NotNull (reported);
            Assert.Equal (new (0, 0, 2048, 2048), reported.Value);
        }
        finally
        {
            app.ScreenChanged -= handler;
        }
    }

    #region Inline Mode Screen Tests

    // Copilot

    [Fact]
    public void InlineMode_LayoutAndDraw_Sets_Screen_Height_From_Runnable_View ()
    {
        // Arrange — use instance-based AppModel, not the global static
        using IApplication app = Application.Create ();
        app.AppModel = AppModel.Inline;
        app.Init (DriverRegistry.Names.ANSI);

        // Set driver to a large terminal size
        app.Driver!.SetScreenSize (80, 25);
        Assert.Equal (25, app.Screen.Height);

        // Create a view that uses Dim.Auto with minimumContentDim: 10 and Y = Pos.AnchorEnd()
        Window inlineView = new ()
        {
            Width = Dim.Fill (),
            Y = Pos.AnchorEnd (),
            Height = Dim.Auto (minimumContentDim: 10)
        };

        // Begin adds the view to the session stack and calls LayoutAndDraw
        SessionToken? token = app.Begin (inlineView);

        // Assert — Screen.Height should now be resized to match the view's frame height
        Assert.True (app.Screen.Height <= 25, $"Screen.Height ({app.Screen.Height}) should be <= terminal height (25)");
        Assert.True (app.Screen.Height >= 10, $"Screen.Height ({app.Screen.Height}) should be >= minimumContentDim (10)");
        Assert.Equal (80, app.Screen.Width);

        // Cleanup
        if (token is { })
        {
            app.End (token);
        }

        inlineView.Dispose ();
    }

    [Fact]
    public void FullScreenMode_LayoutAndDraw_Does_Not_Resize_Screen ()
    {
        // Arrange — use instance-based AppModel, not the global static
        using IApplication app = Application.Create ();
        app.AppModel = AppModel.FullScreen;
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver!.SetScreenSize (80, 25);
        Assert.Equal (25, app.Screen.Height);

        Window fullScreenView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        SessionToken? token = app.Begin (fullScreenView);

        // Assert — Screen should remain the full terminal size
        Assert.Equal (25, app.Screen.Height);
        Assert.Equal (80, app.Screen.Width);

        if (token is { })
        {
            app.End (token);
        }

        fullScreenView.Dispose ();
    }

    #endregion Inline Mode Screen Tests
}
