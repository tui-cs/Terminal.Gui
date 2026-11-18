
namespace Terminal.Gui.App;

public static partial class Application // Navigation stuff
{
    /// <summary>
    ///     Gets the <see cref="ApplicationNavigation"/> instance for the current <see cref="Application"/>.
    /// </summary>
    [Obsolete ("The legacy static Application object is going away.")]
    public static ApplicationNavigation? Navigation
    {
        get => ApplicationImpl.Instance.Navigation;
        internal set => ApplicationImpl.Instance.Navigation = value;
    }
}
