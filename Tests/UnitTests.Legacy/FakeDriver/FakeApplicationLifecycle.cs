#nullable enable
namespace Terminal.Gui.Drivers;

/// <summary>
///     Implements a fake application lifecycle for testing purposes. Cleans up the application on dispose by cancelling
///     the provided <see cref="CancellationTokenSource"/> and shutting down the application.
/// </summary>
/// <param name="hardStop"></param>
internal class FakeApplicationLifecycle (IApplication? app, CancellationTokenSource? hardStop) : IDisposable
{
    /// <inheritdoc/>
    public void Dispose ()
    {
        hardStop?.Cancel ();

        app?.TopRunnableView?.Dispose ();
        app?.Dispose ();
    }
}
