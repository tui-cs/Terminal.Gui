namespace Terminal.Gui.Input;

/// <summary>
///     Interface for views that handle <see cref="Command.Accept"/> as terminal destinations.
///     Views implementing this interface can bubble their <see cref="Command.Accept"/> commands up to
///     their SuperView or be treated as the default accept target depending on <see cref="IsDefault"/>.
/// </summary>
/// <remarks>
///     <para>
///         When a view implementing <see cref="IAcceptTarget"/> invokes <see cref="Command.Accept"/>:
///     </para>
///     <list type="bullet">
///         <item>
///             If <see cref="IsDefault"/> is <see langword="true"/>, the command flows normally without
///             redirection or bubbling. This view IS the <see cref="View.DefaultAcceptView"/>.
///         </item>
///         <item>
///             If <see cref="IsDefault"/> is <see langword="false"/>, the command bubbles up through the
///             SuperView hierarchy, allowing parent views (like <see cref="Dialog{TResult}"/>) to handle
///             it and determine which accept target was activated.
///         </item>
///     </list>
///     <para>
///         When a view that does NOT implement <see cref="IAcceptTarget"/> invokes <see cref="Command.Accept"/>,
///         the command may be redirected to the <see cref="View.DefaultAcceptView"/> (typically a Button
///         with <see cref="IsDefault"/> = <see langword="true"/>).
///     </para>
///     <para>
///         <see cref="Button"/> implements this interface using its existing <see cref="Button.IsDefault"/>
///         property. Other views that want to be terminal Accept handlers should also implement it.
///     </para>
/// </remarks>
public interface IAcceptTarget
{
    /// <summary>
    ///     Gets or sets a value indicating whether this view is the default accept target.
    ///     When <see langword="true"/>, this view will NOT redirect Accept commands to another
    ///     DefaultAcceptView and will NOT bubble up when it is the DefaultAcceptView.
    /// </summary>
    bool IsDefault { get; set; }
}
