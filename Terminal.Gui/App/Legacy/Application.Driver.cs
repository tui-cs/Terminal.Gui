namespace Terminal.Gui.App;

public static partial class Application // Driver abstractions
{
    /// <inheritdoc cref="IApplication.Driver"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static IDriver? Driver { get => ApplicationImpl.Instance.Driver; internal set => ApplicationImpl.Instance.Driver = value; }

    /// <summary>Raised when <see cref="ForceDriver"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<string>>? ForceDriverChanged;

    /// <summary>Raised when <see cref="AppModel"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<AppModel>>? AppModelChanged;

    /// <summary>Raised when <see cref="ForceInlinePosition"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<Point?>>? ForceInlinePositionChanged;
}
