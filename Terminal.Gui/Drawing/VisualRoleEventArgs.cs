namespace Terminal.Gui.Drawing;

using System;

#pragma warning disable CS1711

/// <summary>
///     Provides data for cancellable workflow events that resolve an <see cref="Attribute"/> for a specific
///     <see cref="VisualRole"/> in the Cancellable Work Pattern (CWP).
/// </summary>
/// <remarks>
///     <para>
///         Used in events like <see cref="View.GettingAttributeForRole"/> to allow customization or cancellation
///         of attribute resolution for a <see cref="VisualRole"/>, such as determining the appearance of a
///         <see cref="View"/> based on its state (e.g., focused, disabled).
///     </para>
///     <para>
///         Inherits from <see cref="ResultEventArgs{T}"/> with <c>T = <see cref="Attribute"/></c>, providing a
///         cancellable result workflow where event handlers can supply a custom <see cref="Attribute"/> or mark
///         the operation as handled.
///     </para>
/// </remarks>
/// <typeparam name="T">The type of the result, constrained to <see cref="Attribute"/>.</typeparam>
/// <example>
///     <code>
///         View view = new();
///         view.GettingAttributeForRole += (sender, args) =>
///         {
///             if (args.Role == VisualRole.Focus)
///             {
///                 args.Result = new Attribute(Color.BrightCyan, Color.Black);
///                 args.Handled = true;
///             }
///         };
///         Attribute attribute = view.GetAttributeForRole(VisualRole.Focus);
///     </code>
/// </example>
/// <seealso cref="ResultEventArgs{T}"/>
/// <seealso cref="VisualRole"/>
/// <seealso cref="Attribute"/>
/// <seealso cref="View.GetAttributeForRole"/>
public class VisualRoleEventArgs : ResultEventArgs<Attribute?>
{
    /// <summary>
    ///     Gets the <see cref="VisualRole"/> for which an <see cref="Attribute"/> is being resolved.
    /// </summary>
    public VisualRole Role { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="VisualRoleEventArgs"/> class with the specified
    ///     <see cref="VisualRole"/> and initial <see cref="Attribute"/> result.
    /// </summary>
    /// <param name="role">The <see cref="VisualRole"/> for which the attribute is being resolved.</param>
    /// <param name="result">The initial attribute result, which may be null if no result is provided.</param>
    public VisualRoleEventArgs (in VisualRole role, Attribute? result)
        : base (result)
    {
        Role = role;
    }
}

#pragma warning restore CS1711
