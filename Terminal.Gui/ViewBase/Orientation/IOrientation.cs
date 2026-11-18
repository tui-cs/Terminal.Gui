

namespace Terminal.Gui.ViewBase;

using System;

/// <summary>
///     Implement this interface to provide orientation support.
/// </summary>
/// <remarks>
///     See <see cref="OrientationHelper"/> for a helper class that implements this interface.
/// </remarks>
public interface IOrientation
{
    /// <summary>
    ///     Gets or sets the orientation of the View.
    /// </summary>
    Orientation Orientation { get; set; }

    /// <summary>
    ///     Raised when <see cref="Orientation"/> is changing. Can be cancelled.
    /// </summary>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <summary>
    ///     Called when <see cref="Orientation"/> is changing.
    /// </summary>
    /// <param name="currentOrientation">The current orientation.</param>
    /// <param name="newOrientation">The new orientation.</param>
    /// <returns><see langword="true"/> to cancel the change.</returns>
    public bool OnOrientationChanging (Orientation currentOrientation, Orientation newOrientation) { return false; }

    /// <summary>
    ///     Raised when <see cref="Orientation"/> has changed.
    /// </summary>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;

    /// <summary>
    ///     Called when <see cref="Orientation"/> has been changed.
    /// </summary>
    /// <param name="newOrientation"></param>
    /// <returns></returns>
    public void OnOrientationChanged (Orientation newOrientation) { return; }
}
