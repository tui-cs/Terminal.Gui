using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

public partial class View
{
    // Cached scheme name strings used on the draw path to avoid repeated Enum.GetName allocations.
    private static readonly string _accentSchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent)!;
    private static readonly string _baseSchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base)!;

    #region Get

    /// <summary>Gets the current <see cref="System.Attribute"/> used by <see cref="AddRune(System.Text.Rune)"/>.</summary>
    /// <returns>The current attribute.</returns>
    public Attribute GetCurrentAttribute () => Driver?.GetAttribute () ?? Attribute.Default;

    /// <summary>
    ///     Gets the <see cref="Attribute"/> associated with a specified <see cref="VisualRole"/>
    ///     from the <see cref="Scheme"/>.
    ///     <para>
    ///         Raises <see cref="OnGettingAttributeForRole"/>/<see cref="GettingAttributeForRole"/>
    ///         which can cancel the default behavior, and optionally change the attribute in the event args.
    ///     </para>
    ///     <para>
    ///         If <see cref="Enabled"/> is <see langword="false"/>, <see cref="VisualRole.Disabled"/>
    ///         will be used instead of <paramref name="role"></paramref>.
    ///         To override this behavior use  <see cref="OnGettingAttributeForRole"/>/<see cref="GettingAttributeForRole"/>
    ///         to cancel the method, and return a different attribute.
    ///     </para>
    ///     <para>
    ///         If <see cref="MouseHighlightStates"/> is not <see cref="MouseState.None"/> and <see cref="MouseState"/> is
    ///         <see cref="MouseState.In"/>
    ///         the <see cref="VisualRole.Highlight"/> will be used instead of <paramref name="role"/>.
    ///         To override this behavior use  <see cref="OnGettingAttributeForRole"/>/<see cref="GettingAttributeForRole"/>
    ///         to cancel the method, and return a different attribute.
    ///     </para>
    /// </summary>
    /// <param name="role">The semantic <see cref="Drawing.VisualRole"/> describing the element being rendered.</param>
    /// <returns>The corresponding <see cref="Attribute"/> from the <see cref="Drawing.Scheme"/>.</returns>
    public Attribute GetAttributeForRole (VisualRole role)
    {
        // Get the base attribute
        // If this view doesn't have an explicit scheme or scheme name, defer to SuperView for attribute resolution.
        // This allows parent views to customize attribute resolution for their children via
        // OnGettingAttributeForRole/GettingAttributeForRole.
        // This matches the logic in GetScheme() where SchemeName takes precedence over SuperView inheritance.
        Attribute schemeAttribute;

        if (!HasScheme && string.IsNullOrEmpty (SchemeName) && SuperView is { })
        {
            schemeAttribute = SuperView.GetAttributeForRole (role);
        }
        else
        {
            Scheme scheme = GetScheme ();
            Attribute? defaultAttr = App?.Driver?.DefaultAttribute;

            // If this view uses the Accent scheme and its Normal still has Color.None (the sentinel),
            // derive an opaque Accent scheme from Base at draw time.
            if (SchemeName == _accentSchemeName
                && scheme.Normal.Foreground == Color.None
                && scheme.Normal.Background == Color.None
                && SchemeManager.TryGetScheme (_baseSchemeName, out Scheme? baseScheme))
            {
                scheme = Scheme.DeriveAccent (baseScheme, defaultAttr);
            }

            schemeAttribute = scheme.GetAttributeForRole (role, defaultAttr);
        }

        if (OnGettingAttributeForRole (role, ref schemeAttribute))
        {
            // The implementation may have changed the attribute
            return schemeAttribute;
        }

        VisualRoleEventArgs args = new (role, schemeAttribute);
        GettingAttributeForRole?.Invoke (this, args);

        if (args is { Handled: true, Result: { } })
        {
            // A handler may have changed the attribute
            return args.Result.Value;
        }

        if (role == VisualRole.Disabled || MouseHighlightStates == MouseState.None)
        {
            return Enabled || role == VisualRole.Disabled ? schemeAttribute : GetAttributeForRole (VisualRole.Disabled);
        }

        // The default behavior for MouseHighlightStates of MouseState.Over is to use the Highlight role
        if (((MouseHighlightStates.FastHasFlags (MouseState.In) && MouseState.FastHasFlags (MouseState.In))
             || (MouseHighlightStates.FastHasFlags (MouseState.Pressed) && MouseState.FastHasFlags (MouseState.Pressed)))
            && role != VisualRole.Highlight
            && !HasFocus)
        {
            schemeAttribute = GetAttributeForRole (VisualRole.Highlight);
        }

        return Enabled ? schemeAttribute : GetAttributeForRole (VisualRole.Disabled);
    }

    /// <summary>
    ///     Called when the Attribute for a <see cref="GetAttributeForRole(VisualRole)"/> is being retrieved.
    ///     Implementations can
    ///     return <see langword="true"/> to stop further processing and optionally set the <see cref="Attribute"/> in the
    ///     event args to a different value.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="currentAttribute">The current value of the Attribute for the VisualRole. This by-ref value can be changed</param>
    /// <returns></returns>
    protected virtual bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute) => false;

    /// <summary>
    ///     Raised when the Attribute for a <see cref="GetAttributeForRole(VisualRole)"/> is being retrieved.
    ///     Handlers should check if <see cref="CancelEventArgs.Cancel"/>
    ///     has been set to <see langword="true"/> and do nothing if so. If Cancel is <see langword="false"/>
    ///     a handler can set it to <see langword="true"/> to stop further processing optionally change the
    ///     `CurrentValue` in the event args to a different value.
    /// </summary>
    public event EventHandler<VisualRoleEventArgs>? GettingAttributeForRole;

    #endregion Get

    #region Set

    /// <summary>
    ///     Selects the specified Attribute
    ///     as the Attribute to use for subsequent calls to <see cref="AddRune(System.Text.Rune)"/> and
    ///     <see cref="AddStr(string)"/>.
    /// </summary>
    /// <param name="attribute">THe Attribute to set.</param>
    /// <returns>The previously set Attribute.</returns>
    public Attribute SetAttribute (Attribute attribute) => Driver?.SetAttribute (attribute) ?? Attribute.Default;

    /// <summary>
    ///     Selects the Attribute associated with the specified <see cref="VisualRole"/>
    ///     as the Attribute to use for subsequent calls to <see cref="AddRune(System.Text.Rune)"/> and
    ///     <see cref="AddStr(string)"/>.
    ///     <para>
    ///         Calls <see cref="GetAttributeForRole"/> to get the Attribute associated with the specified role, which will
    ///         raise <see cref="OnGettingAttributeForRole"/>/<see cref="GettingAttributeForRole"/>.
    ///     </para>
    /// </summary>
    /// <param name="role">The semantic <see cref="VisualRole"/> describing the element being rendered.</param>
    /// <returns>The previously set Attribute.</returns>
    public Attribute? SetAttributeForRole (VisualRole role)
    {
        Attribute schemeAttribute = GetAttributeForRole (role);

        return SetAttribute (schemeAttribute);
    }

    #endregion Set
}
