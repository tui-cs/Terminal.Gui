#nullable disable
using System.Text;
using UnitTests;

namespace ViewBaseTests.Viewport;

public class ViewClearViewportTests () : TestDriverBase
{
    [Fact]
    public void ClearViewport_FillsViewportArea ()
    {
        IDriver driver = CreateTestDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Clear the driver contents first
        driver.FillRect (driver.Screen, new Rune ('X'));

        view.ClearViewport ();

        // The viewport area should be filled with spaces
        Rectangle viewportScreen = view.ViewportToScreen (view.Viewport with { Location = new (0, 0) });

        for (int y = viewportScreen.Y; y < viewportScreen.Y + viewportScreen.Height; y++)
        {
            for (int x = viewportScreen.X; x < viewportScreen.X + viewportScreen.Width; x++)
            {
                Assert.Equal (" ", driver.Contents [y, x].Grapheme);
            }
        }
    }

    [Fact]
    public void ClearViewport_WithClearContentOnly_LimitsToVisibleContent ()
    {
        IDriver driver = CreateTestDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.SetContentSize (new Size (100, 100));  // Content larger than viewport
        view.ViewportSettings = ViewportSettingsFlags.ClearContentOnly;
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Clear the driver contents first
        driver.FillRect (driver.Screen, new Rune ('X'));

        view.ClearViewport ();

        // The visible content area should be cleared
        Rectangle visibleContent = view.ViewportToScreen (new Rectangle (new (-view.Viewport.X, -view.Viewport.Y), view.GetContentSize ()));
        Rectangle viewportScreen = view.ViewportToScreen (view.Viewport with { Location = new (0, 0) });
        Rectangle toClear = Rectangle.Intersect (viewportScreen, visibleContent);

        for (int y = toClear.Y; y < toClear.Y + toClear.Height; y++)
        {
            for (int x = toClear.X; x < toClear.X + toClear.Width; x++)
            {
                Assert.Equal (" ", driver.Contents [y, x].Grapheme);
            }
        }
    }

    [Fact]
    public void ClearViewport_NullDriver_DoesNotThrow ()
    {
        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 20,
            Height = 20
        };
        view.BeginInit ();
        view.EndInit ();
        var exception = Record.Exception (() => view.ClearViewport ());
        Assert.Null (exception);
    }

    [Fact]
    public void ClearViewport_SetsNeedsDraw ()
    {
        IDriver driver = CreateTestDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Clear NeedsDraw first
        view.Draw ();
        Assert.False (view.NeedsDraw);

        view.ClearViewport ();

        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void ClearViewport_WithTransparentFlag_DoesNotClear ()
    {
        IDriver driver = CreateTestDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 20,
            Height = 20,
            Driver = driver,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Fill driver with a character
        driver.FillRect (driver.Screen, new Rune ('X'));

        view.Draw ();

        // The viewport area should still have 'X' (not cleared)
        Rectangle viewportScreen = view.ViewportToScreen (view.Viewport with { Location = new (0, 0) });

        for (int y = viewportScreen.Y; y < viewportScreen.Y + viewportScreen.Height; y++)
        {
            for (int x = viewportScreen.X; x < viewportScreen.X + viewportScreen.Width; x++)
            {
                Assert.Equal ("X", driver.Contents [y, x].Grapheme);
            }
        }
    }

    [Fact]
    public void ClearingViewport_Event_Raised ()
    {
        IDriver driver = CreateTestDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        bool eventRaised = false;
        Rectangle? receivedRect = null;

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.ClearingViewport += (s, e) =>
        {
            eventRaised = true;
            receivedRect = e.NewViewport;
        };

        view.Draw ();

        Assert.True (eventRaised);
        Assert.NotNull (receivedRect);
        Assert.Equal (view.Viewport, receivedRect);
    }

    [Fact]
    public void ClearedViewport_Event_Raised ()
    {
        IDriver driver = CreateTestDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        bool eventRaised = false;

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.ClearedViewport += (s, e) => eventRaised = true;

        view.Draw ();

        Assert.True (eventRaised);
    }

    [Fact]
    public void OnClearingViewport_CanPreventClear ()
    {
        IDriver driver = CreateTestDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        bool clearedCalled = false;

        var view = new TestView
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Driver = driver,
            PreventClear = true
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.ClearedViewport += (s, e) => clearedCalled = true;

        view.Draw ();

        Assert.False (clearedCalled);
    }

    [Fact]
    public void ClearViewport_EmptyViewport_DoesNotThrow ()
    {
        IDriver driver = CreateTestDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 1,
            Height = 1,
            Driver = driver
        };
        view.Border.Thickness = new Thickness (1);
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // With border of 1, viewport should be empty
        Assert.True (view.Viewport.Width == 0 || view.Viewport.Height == 0);

        var exception = Record.Exception (() => view.ClearViewport ());

        Assert.Null (exception);
    }

    [Fact]
    public void ClearViewport_WithScrolledViewport_ClearsCorrectArea ()
    {
        IDriver driver = CreateTestDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.SetContentSize (new Size (100, 100));
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Scroll the viewport
        view.Viewport = view.Viewport with { X = 10, Y = 10 };

        // Fill driver with a character
        driver.FillRect (driver.Screen, new Rune ('X'));

        view.ClearViewport ();

        // The viewport area should be cleared (not the scrolled content area)
        Rectangle viewportScreen = view.ViewportToScreen (view.Viewport with { Location = new (0, 0) });

        for (int y = viewportScreen.Y; y < viewportScreen.Y + viewportScreen.Height; y++)
        {
            for (int x = viewportScreen.X; x < viewportScreen.X + viewportScreen.Width; x++)
            {
                Assert.Equal (" ", driver.Contents [y, x].Grapheme);
            }
        }
    }

    [Fact]
    public void ClearViewport_WithClearContentOnly_AndScrolledViewport_ClearsOnlyVisibleContent ()
    {
        IDriver driver = CreateTestDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.SetContentSize (new Size (15, 15));  // Content smaller than viewport
        view.ViewportSettings = ViewportSettingsFlags.ClearContentOnly;
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Scroll past the content
        view.Viewport = view.Viewport with { X = 5, Y = 5 };

        // Fill driver with a character
        driver.FillRect (driver.Screen, new Rune ('X'));

        view.ClearViewport ();

        // Only the visible part of the content should be cleared
        Rectangle visibleContent = view.ViewportToScreen (new Rectangle (new (-view.Viewport.X, -view.Viewport.Y), view.GetContentSize ()));
        Rectangle viewportScreen = view.ViewportToScreen (view.Viewport with { Location = new (0, 0) });
        Rectangle toClear = Rectangle.Intersect (viewportScreen, visibleContent);

        if (toClear != Rectangle.Empty)
        {
            for (int y = toClear.Y; y < toClear.Y + toClear.Height; y++)
            {
                for (int x = toClear.X; x < toClear.X + toClear.Width; x++)
                {
                    Assert.Equal (" ", driver.Contents[y, x].Grapheme);
                }
            }
        }
    }

    private class TestView : View
    {
        public bool PreventClear { get; set; }

        protected override bool OnClearingViewport ()
        {
            return PreventClear || base.OnClearingViewport ();
        }
    }
}
