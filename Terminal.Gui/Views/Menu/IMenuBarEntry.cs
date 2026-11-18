namespace Terminal.Gui.Views;

/// <summary>
///     Defines the contract for menu bar entries, enabling <see cref="MenuBar"/> to operate
///     uniformly regardless of whether the entry uses a <see cref="PopoverMenu"/> or an inline
///     <see cref="Menu"/> (controlled by <see cref="MenuBarItem.UsePopoverMenu"/>).
/// </summary>
public interface IMenuBarEntry
{
    /// <summary>
    ///     Gets or sets whether the menu dropdown is currently open and visible.
    /// </summary>
    bool IsMenuOpen { get; set; }

    /// <summary>
    ///     Raised when <see cref="IsMenuOpen"/> has changed.
    /// </summary>
    event EventHandler<ValueChangedEventArgs<bool>>? MenuOpenChanged;

    /// <summary>
    ///     Gets the root <see cref="Menu"/> for this entry's dropdown, used for item
    ///     search and hierarchy traversal.
    /// </summary>
    Menu? RootMenu { get; }
}
