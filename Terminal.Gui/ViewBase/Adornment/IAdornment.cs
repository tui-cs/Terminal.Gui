namespace Terminal.Gui.ViewBase;

/// <summary>
///     Defines the contract for an adornment layer around a <see cref="View"/>.
///     Implemented by the lightweight <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/> classes.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="IAdornment"/> represents a pure settings object: it holds <see cref="Thickness"/> and provides
///         coordinate-conversion methods. The full <see cref="View"/>-level
///         backing object is created lazily via <see cref="View"/> and accessed through <see cref="IAdornmentView"/>.
///     </para>
/// </remarks>
public interface IAdornment
{
    /// <summary>
    ///     The thickness (space consumed by this adornment layer).
    ///     Changing this triggers layout recalculation on the parent <see cref="View"/>.
    /// </summary>
    Thickness Thickness { get; set; }

    /// <summary>
    ///     The calculated frame rectangle for this adornment layer, set by
    ///     <see cref="View.SetAdornmentFrames"/>. This is the single source of truth
    ///     for adornment geometry.
    /// </summary>
    Rectangle GetFrame ();

    /// <summary>
    ///     The <see cref="IAdornmentView"/> backing this adornment.
    ///     <see langword="null"/> until the adornment actually needs <see cref="View"/>-level functionality
    ///     (rendering, SubViews, mouse, arrangement, shadow). Once set, the lifetime is controlled by the
    ///     <see cref="IAdornmentView"/> implementation.
    /// </summary>
    IAdornmentView? View { get; }

    /// <summary>
    /// </summary>
    public View? Parent { get; set; }

    /// <summary>Fired when <see cref="Thickness"/> changes.</summary>
    event EventHandler? ThicknessChanged;
}
