using Moq;

namespace ApplicationTests;

/// <summary>
/// Tests for cursor positioning functionality using instance-based application model.
/// </summary>
/// <remarks>
/// CoPilot - GitHub Copilot Agent
/// </remarks>
[Collection("Application Tests")]
public class CursorTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private class TestView : View
    {
        private Point? _testLocation;

        public Point? TestLocation
        {
            get => _testLocation;
            set
            {
                _testLocation = value;
                UpdateTestCursor ();
            }
        }

        private void UpdateTestCursor ()
        {
            if (_testLocation.HasValue)
            {
                // Check if cursor is within viewport bounds
                if (_testLocation.Value.X >= 0 && _testLocation.Value.X < Viewport.Width
                    && _testLocation.Value.Y >= 0 && _testLocation.Value.Y < Viewport.Height)
                {
                    Cursor = Cursor with { Position = _testLocation, Style = CursorStyle.BlinkingBlock };
                }
                else
                {
                    // Cursor outside viewport - hide it
                    Cursor = Cursor with { Position = null, Style = Cursor.Style };
                }
            }
            else
            {
                Cursor = Cursor with { Position = null, Style = Cursor.Style };
            }
        }
    }

    [Fact]
    public void UpdateCursor_No_Focus_Returns_Early ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Navigation?.SetFocused (null);

        // UpdateCursor should return early when there's no focus
        // Just verify it doesn't crash
        app.Navigation?.UpdateCursor ();

        app.Dispose ();
    }

    [Fact]
    public void UpdateCursor_View_Without_CanFocus_Returns_Early ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TestView view = new ()
        {
            CanFocus = false,
            Width = 10,
            Height = 5
        };
        view.TestLocation = new Point (0, 0);

        Runnable runnable = new ();
        runnable.Add (view);
        SessionToken? token = app.Begin (runnable);

        // UpdateCursor should return early when focused view can't have focus
        // Just verify it doesn't crash
        app.Navigation?.UpdateCursor ();

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void UpdateCursor_View_Returns_Null_Position_Returns_Early ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TestView view = new ()
        {
            CanFocus = true,
            Width = 10,
            Height = 5
        };
        // TestLocation is null by default

        Runnable runnable = new ();
        runnable.Add (view);
        SessionToken? token = app.Begin (runnable);

        view.SetFocus ();
        Assert.True (view.HasFocus);

        // UpdateCursor should return early when PositionCursor returns null
        // Just verify it doesn't crash
        app.Navigation?.UpdateCursor ();

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void UpdateCursor_Focused_With_Position_Succeeds ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TestView view = new ()
        {
            CanFocus = true,
            Width = 10,
            Height = 5
        };
        view.TestLocation = new Point (2, 1);

        Runnable runnable = new ();
        runnable.Add (view);
        SessionToken? token = app.Begin (runnable);

        view.SetFocus ();
        Assert.True (view.HasFocus);

        // UpdateCursor should succeed when view has focus and valid position
        // Just verify it doesn't crash
        app.Navigation?.UpdateCursor ();

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }


    // Tests for Issue #3444 - Cursor should be hidden when positioned outside viewport
    [Fact]
    public void Cursor_OutsideViewport_Is_Invisible ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Test with a generic View that positions cursor outside viewport
        TestView view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 5,
            CanFocus = true
        };

        Runnable runnable = new ();
        runnable.Add (view);
        SessionToken? token = app.Begin (runnable);

        view.SetFocus ();

        // Position cursor outside viewport width
        view.TestLocation = new Point (15, 0);
        Point? cursorPos = view.Cursor.Position;

        // Cursor should be hidden (return null) when outside viewport
        Assert.Null (cursorPos);

        // Position cursor outside viewport height
        view.TestLocation = new Point (0, 10);
        cursorPos = view.Cursor.Position;

        Assert.Null (cursorPos);

        // Position cursor at negative position
        view.TestLocation = new Point (-1, 0);
        cursorPos = view.Cursor.Position;

        Assert.Null (cursorPos);

        view.TestLocation = new Point (0, -1);
        cursorPos = view.Cursor.Position;

        Assert.Null (cursorPos);

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void UpdateCursor_CursorOutsideAncestorViewport_Succeeds ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Create parent with small viewport
        View parent = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 5
        };

        // Create child that extends beyond parent viewport
        TestView child = new ()
        {
            X = 0,
            Y = 0,
            Width = 20,
            Height = 10,
            CanFocus = true
        };

        parent.Add (child);
        Runnable runnable = new ();
        runnable.Add (parent);
        SessionToken? token = app.Begin (runnable);

        child.SetFocus ();

        // Position cursor within child's viewport but outside parent's viewport
        child.TestLocation = new Point (15, 0);

        // UpdateCursor should handle cursor outside ancestor viewport
        // Just verify it doesn't crash
        app.Navigation?.UpdateCursor ();

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void UpdateCursor_CursorWithinAllAncestors_Succeeds ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Create parent viewport
        View parent = new ()
        {
            X = 0,
            Y = 0,
            Width = 20,
            Height = 10
        };

        // Create child within parent viewport
        TestView child = new ()
        {
            X = 2,
            Y = 2,
            Width = 10,
            Height = 5,
            CanFocus = true
        };

        parent.Add (child);
        Runnable runnable = new ();
        runnable.Add (parent);
        SessionToken? token = app.Begin (runnable);

        child.SetFocus ();

        // Position cursor within both child and parent viewports
        child.TestLocation = new Point (2, 1);

        // UpdateCursor should succeed when cursor is within all ancestor viewports
        // Just verify it doesn't crash
        app.Navigation?.UpdateCursor ();

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    /// <summary>Driver double that records cursor writes and can refuse to apply them.</summary>
    private sealed class CursorDriverDouble
    {
        public Mock<IDriver> Mock { get; } = new ();
        public bool NeedsUpdate { get; set; } = true;
        public bool AppliesCursor { get; set; } = true;
        public int SetCursorCalls { get; private set; }
        public Cursor Current { get; private set; } = new ();

        public CursorDriverDouble ()
        {
            Mock.Setup (d => d.GetCursorNeedsUpdate ()).Returns (() => NeedsUpdate);
            Mock.Setup (d => d.SetCursorNeedsUpdate (It.IsAny<bool> ())).Callback<bool> (value => NeedsUpdate = value);
            Mock.Setup (d => d.GetCursor ()).Returns (() => Current);

            Mock.Setup (d => d.SetCursor (It.IsAny<Cursor> ()))
                .Callback<Cursor> (cursor =>
                                   {
                                       SetCursorCalls++;

                                       if (AppliesCursor)
                                       {
                                           Current = cursor;
                                       }
                                   });
        }
    }

    [Fact]
    public void UpdateCursor_Skips_Writes_When_Cursor_Unchanged ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TestView view = new () { CanFocus = true, Width = 10, Height = 5 };
        Runnable runnable = new ();
        runnable.Add (view);
        SessionToken? token = app.Begin (runnable);
        view.SetFocus ();

        // Real driver: Refresh moves the terminal cursor, so the next UpdateCursor re-applies it and clears the flag again.
        Assert.True (app.Driver!.GetCursorNeedsUpdate ());
        app.Navigation!.UpdateCursor ();
        Assert.False (app.Driver.GetCursorNeedsUpdate ());
        app.Driver.Refresh ();
        Assert.True (app.Driver.GetCursorNeedsUpdate ());
        app.Navigation.UpdateCursor ();
        Assert.False (app.Driver.GetCursorNeedsUpdate ());

        app.Driver.SetScreenSize (40, 12);
        Assert.True (app.Driver.GetCursorNeedsUpdate ());

        // Driver double: count the cursor writes the main loop would make.
        CursorDriverDouble driver = new ();
        app.Driver = driver.Mock.Object;

        app.Navigation.UpdateCursor ();
        Assert.Equal (1, driver.SetCursorCalls);
        Assert.False (driver.Current.IsVisible); // the view has no cursor yet
        Assert.False (driver.NeedsUpdate);

        // Idle iterations write nothing
        app.Navigation.UpdateCursor ();
        app.Navigation.UpdateCursor ();
        Assert.Equal (1, driver.SetCursorCalls);

        // A cursor change on the focused view requests one write, then idles again
        view.TestLocation = new Point (2, 1);
        Assert.True (driver.NeedsUpdate);
        app.Navigation.UpdateCursor ();
        Assert.Equal (2, driver.SetCursorCalls);
        Assert.True (driver.Current.IsVisible);
        Assert.Equal (new Point (2, 1), driver.Current.Position);
        Assert.False (driver.NeedsUpdate);

        app.Navigation.UpdateCursor ();
        Assert.Equal (2, driver.SetCursorCalls);

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void UpdateCursor_Retries_Until_The_Driver_Applies_The_Cursor ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TestView view = new () { CanFocus = true, Width = 10, Height = 5, TestLocation = new Point (2, 1) };
        Runnable runnable = new ();
        runnable.Add (view);
        SessionToken? token = app.Begin (runnable);
        view.SetFocus ();

        // An output can return from SetCursor without writing (inline placement unknown, resize pending, IOException).
        CursorDriverDouble driver = new () { AppliesCursor = false };
        app.Driver = driver.Mock.Object;

        app.Navigation!.UpdateCursor ();
        app.Navigation.UpdateCursor ();
        Assert.Equal (2, driver.SetCursorCalls);
        Assert.True (driver.NeedsUpdate);

        driver.AppliesCursor = true;
        app.Navigation.UpdateCursor ();
        Assert.Equal (3, driver.SetCursorCalls);
        Assert.Equal (new Point (2, 1), driver.Current.Position);
        Assert.False (driver.NeedsUpdate);

        app.Navigation.UpdateCursor ();
        Assert.Equal (3, driver.SetCursorCalls);

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void SetCursorNeedsUpdate_Signals_Cursor_Update ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TestView view = new ()
        {
            CanFocus = true,
            Width = 10,
            Height = 5
        };

        Runnable runnable = new ();
        runnable.Add (view);
        SessionToken? token = app.Begin (runnable);

        view.SetFocus ();

        // Call SetCursorNeedsUpdate - should set flag in ApplicationNavigation
        view.SetCursorNeedsUpdate ();

        // This test verifies the API exists and can be called
        // The actual caching behavior is disabled per cursor.md

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void TextField_InsertionPoint_Stays_Within_Viewport ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TextField textField = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Text = "This is a very long text that extends beyond the viewport"
        };

        Runnable runnable = new ();
        runnable.Add (textField);
        SessionToken? token = app.Begin (runnable);

        textField.SetFocus ();

        // Position cursor at end of text (beyond viewport)
        textField.InsertionPoint = textField.Text.Length;

        // PositionCursor should return a position within viewport
        Point? cursorPos = textField.Cursor.Position;

        if (cursorPos.HasValue)
        {
            Assert.True (cursorPos.Value.X >= 0);
            Assert.True (cursorPos.Value.X < textField.Viewport.Width);
        }

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }
}
