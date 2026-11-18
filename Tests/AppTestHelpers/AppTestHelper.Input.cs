using System.Drawing;
using Terminal.Gui.Testing;
using Terminal.Gui.Time;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AppTestHelpers;

public partial class AppTestHelper
{
    /// <summary>
    ///     Simulates a right click at the given screen coordinates on the current driver.
    ///     This is a raw input event that goes through entire processing pipeline as though
    ///     user had pressed the mouse button physically.
    /// </summary>
    /// <param name="screenX">0 indexed screen coordinates</param>
    /// <param name="screenY">0 indexed screen coordinates</param>
    /// <returns></returns>
    public AppTestHelper RightClick (int screenX, int screenY)
    {
        InjectMouseEvent (new Mouse
        {
            Flags = MouseFlags.RightButtonPressed, ScreenPosition = new Point (screenX, screenY), Position = new Point (screenX, screenY)
        }); // Don't advance time between Press and Release

        return InjectMouseEvent (new Mouse
                                 {
                                     Flags = MouseFlags.RightButtonReleased,
                                     ScreenPosition = new Point (screenX, screenY),
                                     Position = new Point (screenX, screenY)
                                 },
                                 true); // Advance time after the complete click
    }

    /// <summary>
    ///     Simulates a left click at the given screen coordinates on the current driver.
    ///     This is a raw input event that goes through entire processing pipeline as though
    ///     user had pressed the mouse button physically.
    /// </summary>
    /// <param name="screenX">0 indexed screen coordinates</param>
    /// <param name="screenY">0 indexed screen coordinates</param>
    /// <returns></returns>
    public AppTestHelper LeftClick (int screenX, int screenY)
    {
        InjectMouseEvent (new Mouse
        {
            Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (screenX, screenY), Position = new Point (screenX, screenY)
        }); // Don't advance time between Press and Release

        return InjectMouseEvent (new Mouse
                                 {
                                     Flags = MouseFlags.LeftButtonReleased,
                                     ScreenPosition = new Point (screenX, screenY),
                                     Position = new Point (screenX, screenY)
                                 },
                                 true); // Advance time after the complete click
    }

    /// <summary>
    ///     Simulates a left mouse click on the top-left cell of the Viewport of the View of type TView determined by the
    ///     <paramref name="evaluator"/>.
    /// </summary>
    /// <typeparam name="TView"></typeparam>
    /// <param name="evaluator"></param>
    /// <returns></returns>
    public AppTestHelper LeftClick<TView> (Func<TView, bool> evaluator) where TView : View =>
        InjectMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonClicked }, evaluator);

    /// <summary>
    ///     Injects a mouse event to the current driver's input processor.
    ///     Uses the new input injection infrastructure with virtual time support.
    /// </summary>
    /// <param name="mouse">The mouse event to inject.</param>
    /// <param name="advanceTimeAfter">
    ///     Whether to advance time after this event to space clicks apart (prevents multi-click
    ///     detection).
    /// </param>
    /// <returns>This AppTestHelper for fluent chaining.</returns>
    private AppTestHelper InjectMouseEvent (Mouse mouse, bool advanceTimeAfter = false)
    {
        // Use the new injection infrastructure
        WaitIteration (app =>
                       {
                           if (app.Driver is { })
                           {
                               // Set timestamp from virtual time provider
                               mouse.Timestamp = TimeProvider.Now;
                               mouse.Position = mouse.ScreenPosition;

                               // Use the new simplified injection API
                               app.InjectMouse (mouse);

                               // Advance virtual time after complete clicks to space them apart
                               // This prevents rapid clicks from being detected as multi-clicks
                               // while keeping Press+Release pairs together (no delay within a single click)
                               if (advanceTimeAfter && TimeProvider is VirtualTimeProvider vtp)
                               {
                                   // Advance virtual time beyond the double-click threshold (500ms)
                                   // This is instant - no real delay!
                                   vtp.Advance (TimeSpan.FromMilliseconds (550));
                               }
                           }
                           else
                           {
                               Fail ("Expected Application.Driver to be non-null.");
                           }
                       });

        // Wait for the event to be processed
        return WaitIteration ();
    }

    /// <summary>
    ///     Injects a mouse event to the current driver's input processor.
    ///     Uses the new input injection infrastructure with virtual time support.
    /// </summary>
    /// <param name="mouse">The mouse event to inject.</param>
    /// <param name="evaluator">Function to find the target view.</param>
    /// <returns>This AppTestHelper for fluent chaining.</returns>
    private AppTestHelper InjectMouseEvent<TView> (Mouse mouse, Func<TView, bool> evaluator) where TView : View
    {
        var screen = Point.Empty;

        AppTestHelper ctx = WaitIteration (_ =>
                                         {
                                             TView v = Find (evaluator);
                                             screen = v.ViewportToScreen (new Point (0, 0));
                                         });
        mouse.ScreenPosition = screen;
        mouse.Position = screen;

        InjectMouseEvent (mouse);

        return ctx;
    }

    /// <summary>
    ///     Injects a key down event to the current driver's input processor.
    ///     Uses the new simplified injection API with virtual time support.
    /// </summary>
    /// <param name="key">The key to inject.</param>
    /// <returns>This AppTestHelper for fluent chaining.</returns>
    public AppTestHelper KeyDown (Key key)
    {
        //Logging.Trace ($"Injecting key: {key}");

        // Use the new injection infrastructure - same pattern as mouse injection
        WaitIteration (app =>
                       {
                           if (app.Driver is { })
                           {
                               // Use the simplified injection API with default Direct mode
                               // This is faster and more reliable than Pipeline mode
                               app.InjectKey (key);
                           }
                           else
                           {
                               Fail ("Expected Application.Driver to be non-null.");
                           }
                       });

        // Wait for the event to be processed
        return WaitIteration ();
    }
}
