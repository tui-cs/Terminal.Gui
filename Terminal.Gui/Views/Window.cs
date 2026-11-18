namespace Terminal.Gui.Views;

/// <summary>
///     An overlapped container for other views with a border and optional title.
/// </summary>
/// <remarks>
/// <img src="../images/views/Window.gif" alt="Window demo"/>
///     <para>
///         Window has <see cref="View.BorderStyle"/> set to <see cref="float"/>, <see cref="View.Arrangement"/>
///         set to <see cref="ViewArrangement.Overlapped"/>, and
///         uses the "Base" <see cref="Scheme"/> scheme by default.
///     </para>
///     <para>
///         To enable Window to be sized and moved by the user, adjust <see cref="View.Arrangement"/>.
///     </para>
/// </remarks>
/// <seealso cref="FrameView"/>
public class Window : Runnable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Window"/> class.
    /// </summary>
    public Window ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;
        Arrangement = ViewArrangement.Overlapped;
        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);
        BorderStyle = DefaultBorderStyle;
        base.ShadowStyle = DefaultShadow;
    }

    /// <summary>
    ///     Gets or sets whether all <see cref="Window"/>s are shown with a shadow effect by default.
    /// </summary>
    public static ShadowStyles DefaultShadow => WindowSettings.Current.DefaultShadow;

    // TODO: enable this
    ///// <summary>
    ///// The default <see cref="LineStyle"/> for <see cref="Window"/>'s border. The default is <see cref="LineStyle.Single"/>.
    ///// </summary>
    ///// <remarks>
    ///// This property can be set in a Theme to change the default <see cref="LineStyle"/> for all <see cref="Window"/>s.
    ///// </remarks>


    /// <summary>
    ///     The default <see cref="LineStyle"/> for <see cref="Window"/>'s border. The default is
    ///     <see cref="LineStyle.Single"/>.
    /// </summary>
    /// <remarks>
    ///     This property can be set in a Theme to change the default <see cref="LineStyle"/> for all <see cref="Window"/>
    ///     s.
    /// </remarks>
    public static LineStyle DefaultBorderStyle => WindowSettings.Current.DefaultBorderStyle;
}
