using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Drawing;

/// <summary>
///     Represents a theme definition that maps each <see cref="VisualRole"/> (such as <see cref="VisualRole.Focus"/>,
///     <see cref="VisualRole.Disabled"/>, etc.)
///     to an <see cref="Attribute"/> describing its foreground color, background color, and text style.
///     <para>
///         See <see href="https://tui-cs.github.io/Terminal.Gui/docs/drawing.html"/> for more information.
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         A <see cref="Scheme"/> enables consistent, semantic theming of UI elements by associating each visual state
///         with a specific style.
///         Each property (e.g., <see cref="Normal"/>, <see cref="Focus"/>, <see cref="Disabled"/>) is an
///         <see cref="Attribute"/>.
///         If a property is not explicitly set, its value is derived from other roles (typically <see cref="Normal"/>)
///         using well-defined inheritance rules.
///     </para>
///     <para>
///         <see cref="Scheme"/> objects are immutable. To update a scheme, create a new instance with the desired values.
///         Use <see cref="SchemeManager"/> to manage available schemes and apply them to views.
///     </para>
///     <para>
///         <b>Immutability:</b> Scheme objects are immutable. Once constructed, their properties cannot be changed. To
///         modify a Scheme,
///         create a new instance with the desired values (e.g., using the <see cref="Scheme"/> constructor).
///     </para>
///     <para>
///         <b>Attribute Resolution Algorithm:</b>
///         <br/>
///         Each <see cref="Scheme"/> property corresponds to a <see cref="VisualRole"/> and is an <see cref="Attribute"/>.
///         The <see cref="Normal"/> attribute must always be set.
///         All other attributes are optional. If an attribute for a given <see cref="VisualRole"/> is not explicitly set,
///         its value is derived using the following rules:
///         <list type="number">
///             <item>
///                 <description><b>Normal:</b> Must always be explicitly set.</description>
///             </item>
///             <item>
///                 <description>
///                     <b>Focus:</b> If not set, derived from <see cref="Normal"/> by swapping foreground and background
///                     colors. Any <see cref="Color.None"/> values are resolved to the terminal's actual default colors
///                     (queried via OSC 10/11) before swapping.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Active:</b> If not set, derived from <see cref="Focus"/> by:
///                     <list type="bullet">
///                         <item>
///                             <description>
///                                 Setting <c>Foreground</c> to <see cref="Focus"/>'s foreground with
///                                 <see cref="Color.GetBrighterColor(double, bool?)"/>, passing whether the
///                                 <see cref="Focus"/> background is dark or light.
///                             </description>
///                         </item>
///                         <item>
///                             <description>
///                                 Setting <c>Background</c> to <see cref="Focus"/>'s background with
///                                 <see cref="Color.GetDimmerColor(double, bool?)"/>, passing the same dark/light context.
///                             </description>
///                         </item>
///                         <item>
///                             <description>Adding <see cref="TextStyle.Bold"/> to the style.</description>
///                         </item>
///                     </list>
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Highlight:</b> If not set, derived from <see cref="Normal"/> by:
///                     <list type="bullet">
///                         <item>
///                             <description>
///                                 Setting <c>Foreground</c> to <see cref="Normal"/>'s background with
///                                 <see cref="Color.GetBrighterColor(double, bool?)"/>, using the
///                                 <see cref="Normal"/> background's dark/light context.
///                             </description>
///                         </item>
///                         <item>
///                             <description>Setting <c>Background</c> to <see cref="Normal"/>'s background.</description>
///                         </item>
///                         <item>
///                             <description>
///                                 Setting <c>Style</c> to <see cref="Editable"/>'s style with
///                                 <see cref="TextStyle.Italic"/> added.
///                             </description>
///                         </item>
///                     </list>
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Editable:</b> If not set, derived from <see cref="Normal"/> by:
///                     <list type="bullet">
///                         <item>
///                             <description>
///                                 Setting <c>Foreground</c> to <see cref="Normal"/>'s foreground (resolved).
///                             </description>
///                         </item>
///                         <item>
///                             <description>
///                                 Setting <c>Background</c> to <see cref="Normal"/>'s foreground with
///                                 <see cref="Color.GetDimmerColor(double, bool?)"/> at 50%, using the
///                                 <see cref="Normal"/> background's dark/light context.
///                             </description>
///                         </item>
///                     </list>
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>ReadOnly:</b> If not set, derived from <see cref="Editable"/> by dimming
///                     the foreground with <see cref="Color.GetDimmerColor(double, bool?)"/> at 5%,
///                     using the <see cref="Editable"/> background's dark/light context.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Disabled:</b> If not set, derived from <see cref="Normal"/> by dimming
///                     the foreground with <see cref="Color.GetDimmerColor(double, bool?)"/> at 5%,
///                     using the <see cref="Normal"/> background's dark/light context.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>HotNormal:</b> If not set, derived from <see cref="Normal"/> by adding
///                     <see cref="TextStyle.Underline"/> to the style.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>HotFocus:</b> If not set, derived from <see cref="Focus"/> by adding
///                     <see cref="TextStyle.Underline"/> to the style.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>HotActive:</b> If not set, derived from <see cref="Active"/> by adding
///                     <see cref="TextStyle.Underline"/> to the style.
///                 </description>
///             </item>
///         </list>
///         This algorithm ensures that every <see cref="VisualRole"/> always resolves to a valid <see cref="Attribute"/>,
///         either explicitly set or derived.
///     </para>
///     <para>
///         <b>Dark/Light Background Awareness:</b>
///         <br/>
///         When deriving roles that use color math (<see cref="Color.GetBrighterColor(double, bool?)"/> and
///         <see cref="Color.GetDimmerColor(double, bool?)"/>), the algorithm determines whether the relevant background
///         is dark or light via <see cref="Color.IsDarkColor"/>. This ensures correct visual results regardless
///         of whether the terminal has a dark or light background. For example, "dimming" a color on a light background
///         increases lightness (washing out toward white) rather than decreasing it.
///     </para>
///     <para>
///         <b><see cref="Color.None"/> Resolution:</b>
///         <br/>
///         Before performing color math, any <see cref="Color.None"/> values are resolved to the terminal's actual
///         default colors (detected via OSC 10/11 queries at startup). If the terminal's colors are not available,
///         the fallback is White for foreground and Black for background.
///     </para>
/// </remarks>
[JsonConverter (typeof (SchemeJsonConverter))]
public record Scheme : IEqualityOperators<Scheme, Scheme, bool>
{
    /// <summary>
    ///     INTERNAL: Gets the hard-coded set of <see cref="Scheme"/>s. Used for generating the built-in config.json and for
    ///     unit tests that don't depend on ConfigurationManager.
    /// </summary>
    /// <returns></returns>
    internal static ImmutableSortedDictionary<string, Scheme> GetHardCodedSchemes ()
    {
        return ImmutableSortedDictionary.CreateRange (StringComparer.InvariantCultureIgnoreCase,
                                                      [
                                                          new KeyValuePair<string, Scheme> (SchemeManager.SchemesToSchemeName (Schemes.Base)!, CreateBase ()),
                                                          new KeyValuePair<string, Scheme> (SchemeManager.SchemesToSchemeName (Schemes.Dialog)!,
                                                                                            CreateDialog ()),
                                                          new KeyValuePair<string, Scheme> (SchemeManager.SchemesToSchemeName (Schemes.Error)!, CreateError ()),
                                                          new KeyValuePair<string, Scheme> (SchemeManager.SchemesToSchemeName (Schemes.Menu)!, CreateMenu ()),
                                                          new KeyValuePair<string, Scheme> (SchemeManager.SchemesToSchemeName (Schemes.Accent)!,
                                                                                            CreateAccent ())
                                                      ]);

        Scheme CreateBase () => new ()
        {
            Normal = new Attribute (Color.None, Color.None),
            CodeComment = CreateCodeAttribute ("#6a9955"),
            CodeKeyword = CreateCodeAttribute ("#569cd6"),
            CodeString = CreateCodeAttribute ("#ce9178"),
            CodeNumber = CreateCodeAttribute ("#b5cea8"),
            CodeOperator = CreateCodeAttribute ("#d4d4d4"),
            CodeType = CreateCodeAttribute ("#4ec9b0"),
            CodePreprocessor = CreateCodeAttribute ("#c586c0"),
            CodeIdentifier = CreateCodeAttribute ("#9cdcfe"),
            CodeConstant = CreateCodeAttribute ("#569cd6"),
            CodePunctuation = CreateCodeAttribute ("#d4d4d4"),
            CodeFunctionName = CreateCodeAttribute ("#dcdcaa"),
            CodeAttribute = CreateCodeAttribute ("#9cdcfe")
        };

        Attribute CreateCodeAttribute (string foreground) => new (foreground, "None");

        Scheme CreateError () => new () { Normal = new Attribute (StandardColor.IndianRed, StandardColor.RaisinBlack) };

        Scheme CreateDialog () => new () { Normal = new Attribute (StandardColor.LightSkyBlue, StandardColor.OuterSpace) };

        Scheme CreateMenu () => new () { Normal = new Attribute (StandardColor.Charcoal, StandardColor.LightBlue, TextStyle.Bold) };

        Scheme CreateAccent () => new () { Normal = new Attribute (Color.None, Color.None) };
    }

    /// <summary>
    ///     Derives an opaque Accent <see cref="Scheme"/> from the specified Base scheme at draw time.
    ///     The background is shifted slightly brighter (on dark) or dimmer (on light) relative to Base,
    ///     and forced to be opaque (A=255). The foreground is resolved from Base.
    /// </summary>
    /// <param name="baseScheme">The Base scheme to derive Accent colors from.</param>
    /// <param name="defaultTerminalColors">
    ///     The terminal's actual default foreground/background colors (queried via OSC 10/11), used to resolve
    ///     <see cref="Color.None"/>. If <see langword="null"/>, falls back to White/Black.
    /// </param>
    /// <returns>A new <see cref="Scheme"/> with an opaque Normal attribute derived from <paramref name="baseScheme"/>.</returns>
    public static Scheme DeriveAccent (Scheme baseScheme, Attribute? defaultTerminalColors)
    {
        Color resolvedBg = ResolveNone (baseScheme.Normal.Background, defaultTerminalColors);
        Color resolvedFg = ResolveNone (baseScheme.Normal.Foreground, defaultTerminalColors, true);
        bool isDark = resolvedBg.IsDarkColor ();
        Color accentBg = isDark ? resolvedBg.GetBrighterColor (0.1, isDark) : resolvedBg.GetDimmerColor (0.1, isDark);

        // Force opaque
        accentBg = new Color (accentBg.R, accentBg.G, accentBg.B, 255);

        return new Scheme (baseScheme, new Attribute (resolvedFg, accentBg));
    }

    /// <summary>Creates a new instance set to the default attributes (see <see cref="Attribute.Default"/>).</summary>
    public Scheme () : this (Attribute.Default) { }

    /// <summary>Creates a new instance, initialized with the values from <paramref name="scheme"/>.</summary>
    /// <param name="scheme">The scheme to initialize the new instance with.</param>
    public Scheme (Scheme? scheme)
    {
        ArgumentNullException.ThrowIfNull (scheme);

        Normal = scheme.Normal;

        _hotNormal = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotNormal, out Attribute? hotNormal) ? hotNormal : null;
        _focus = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Focus, out Attribute? focus) ? focus : null;
        _hotFocus = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotFocus, out Attribute? hotFocus) ? hotFocus : null;
        _active = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Active, out Attribute? active) ? active : null;
        _hotActive = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.HotActive, out Attribute? hotActive) ? hotActive : null;
        _highlight = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Highlight, out Attribute? highlight) ? highlight : null;
        _editable = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Editable, out Attribute? editable) ? editable : null;
        _readOnly = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.ReadOnly, out Attribute? readOnly) ? readOnly : null;
        _disabled = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Disabled, out Attribute? disabled) ? disabled : null;
        _code = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.Code, out Attribute? code) ? code : null;
        _codeComment = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeComment, out Attribute? codeComment) ? codeComment : null;
        _codeKeyword = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeKeyword, out Attribute? codeKeyword) ? codeKeyword : null;
        _codeString = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeString, out Attribute? codeString) ? codeString : null;
        _codeNumber = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeNumber, out Attribute? codeNumber) ? codeNumber : null;
        _codeOperator = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeOperator, out Attribute? codeOperator) ? codeOperator : null;
        _codeType = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeType, out Attribute? codeType) ? codeType : null;
        _codePreprocessor = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodePreprocessor, out Attribute? codePreprocessor) ? codePreprocessor : null;
        _codeIdentifier = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeIdentifier, out Attribute? codeIdentifier) ? codeIdentifier : null;
        _codeConstant = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeConstant, out Attribute? codeConstant) ? codeConstant : null;
        _codePunctuation = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodePunctuation, out Attribute? codePunctuation) ? codePunctuation : null;
        _codeFunctionName = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeFunctionName, out Attribute? codeFunctionName) ? codeFunctionName : null;
        _codeAttribute = scheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeAttribute, out Attribute? codeAttribute) ? codeAttribute : null;
    }

    private Scheme (Scheme baseScheme, Attribute normal)
    {
        Normal = normal;

        _code = baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.Code, out Attribute? code) ? code : null;
        _codeComment = baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeComment, out Attribute? codeComment) ? codeComment : null;
        _codeKeyword = baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeKeyword, out Attribute? codeKeyword) ? codeKeyword : null;
        _codeString = baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeString, out Attribute? codeString) ? codeString : null;
        _codeNumber = baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeNumber, out Attribute? codeNumber) ? codeNumber : null;
        _codeOperator = baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeOperator, out Attribute? codeOperator) ? codeOperator : null;
        _codeType = baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeType, out Attribute? codeType) ? codeType : null;
        _codePreprocessor = baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodePreprocessor, out Attribute? codePreprocessor) ? codePreprocessor : null;
        _codeIdentifier = baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeIdentifier, out Attribute? codeIdentifier) ? codeIdentifier : null;
        _codeConstant = baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeConstant, out Attribute? codeConstant) ? codeConstant : null;
        _codePunctuation = baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodePunctuation, out Attribute? codePunctuation) ? codePunctuation : null;
        _codeFunctionName = baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeFunctionName, out Attribute? codeFunctionName) ? codeFunctionName : null;
        _codeAttribute = baseScheme.TryGetExplicitlySetAttributeForRole (VisualRole.CodeAttribute, out Attribute? codeAttribute) ? codeAttribute : null;
    }

    /// <summary>Creates a new instance, initialized with the values from <paramref name="attribute"/>.</summary>
    /// <param name="attribute">The attribute to initialize the new instance with.</param>
    public Scheme (Attribute attribute) =>

        // Only set Normal as explicitly set
        Normal = attribute;

    /// <summary>
    ///     Gets the <see cref="Attribute"/> associated with a specified <see cref="VisualRole"/>,
    ///     applying inheritance rules for attributes not explicitly set.
    /// </summary>
    /// <param name="role">The semantic <see cref="VisualRole"/> describing the element being rendered.</param>
    /// <param name="defaultTerminalColors">
    ///     The terminal's actual default foreground/background colors (queried via OSC 10/11), used to resolve
    ///     <see cref="Color.None"/> during color derivation. If <see langword="null"/>, falls back to White/Black.
    /// </param>
    /// <returns>
    ///     The corresponding <see cref="Attribute"/> from the <see cref="Scheme"/>, possibly derived if not explicitly
    ///     set.
    /// </returns>
    public Attribute GetAttributeForRole (VisualRole role, Attribute? defaultTerminalColors = null) =>

        // Use a HashSet to guard against recursion cycles
        GetAttributeForRoleCore (role, [], defaultTerminalColors);

    /// <summary>
    ///     Attempts to get the <see cref="Attribute"/> associated with a specified <see cref="VisualRole"/>. If the
    ///     role is not explicitly set, it will return false and the out parameter will be null.
    /// </summary>
    /// <param name="role"></param>
    /// <param name="attribute"></param>
    /// <returns></returns>
    public bool TryGetExplicitlySetAttributeForRole (VisualRole role, out Attribute? attribute)
    {
        // Use a HashSet to guard against recursion cycles
        attribute = role switch
        {
            VisualRole.Normal => _normal,
            VisualRole.HotNormal => _hotNormal,
            VisualRole.Focus => _focus,
            VisualRole.HotFocus => _hotFocus,
            VisualRole.Active => _active,
            VisualRole.HotActive => _hotActive,
            VisualRole.Highlight => _highlight,
            VisualRole.Editable => _editable,
            VisualRole.ReadOnly => _readOnly,
            VisualRole.Disabled => _disabled,
            VisualRole.Code => _code,
            VisualRole.CodeComment => _codeComment,
            VisualRole.CodeKeyword => _codeKeyword,
            VisualRole.CodeString => _codeString,
            VisualRole.CodeNumber => _codeNumber,
            VisualRole.CodeOperator => _codeOperator,
            VisualRole.CodeType => _codeType,
            VisualRole.CodePreprocessor => _codePreprocessor,
            VisualRole.CodeIdentifier => _codeIdentifier,
            VisualRole.CodeConstant => _codeConstant,
            VisualRole.CodePunctuation => _codePunctuation,
            VisualRole.CodeFunctionName => _codeFunctionName,
            VisualRole.CodeAttribute => _codeAttribute,
            _ => null
        };

        return attribute is { };
    }

    // TODO: Provide a CWP-based API that lets devs override this algo?

    private Attribute GetAttributeForRoleCore (VisualRole role, HashSet<VisualRole> stack, Attribute? defaultTerminalColors)
    {
        // Prevent infinite recursion
        if (!stack.Add (role))
        {
            return Normal; // fallback
        }

        Attribute? attr = Normal;

        if (role == VisualRole.Normal || TryGetExplicitlySetAttributeForRole (role, out attr))
        {
            stack.Remove (role);

            Debug.Assert (attr != null, nameof (attr) + " != null");

            return attr.Value;
        }

        // TODO: Provide an API that lets devs override this algo?

        // Derivation algorithm as documented
        Attribute result;

        switch (role)
        {
            case VisualRole.Focus:
                {
                    Attribute normal = GetAttributeForRoleCore (VisualRole.Normal, stack, defaultTerminalColors);

                    result = normal with
                    {
                        Foreground = ResolveNone (normal.Background, defaultTerminalColors),
                        Background = ResolveNone (normal.Foreground, defaultTerminalColors, true)
                    };

                    break;
                }

            case VisualRole.Active:
                {
                    Attribute focus = GetAttributeForRoleCore (VisualRole.Focus, stack, defaultTerminalColors);
                    Color focusBg = ResolveNone (focus.Background, defaultTerminalColors);
                    bool isDark = focusBg.IsDarkColor ();

                    result = focus with
                    {
                        Foreground = ResolveNone (focus.Foreground, defaultTerminalColors, true).GetBrighterColor (0.2, isDark),
                        Background = focusBg.GetDimmerColor (0.2, !isDark),
                        Style = focus.Style | TextStyle.Bold
                    };

                    break;
                }

            case VisualRole.Highlight:
                {
                    Attribute normal = GetAttributeForRoleCore (VisualRole.Normal, stack, defaultTerminalColors);
                    Color normalBg = ResolveNone (normal.Background, defaultTerminalColors);
                    bool isDark = normalBg.IsDarkColor ();

                    result = normal with
                    {
                        Foreground = normalBg.GetBrighterColor (0.2, isDark),
                        Background = normal.Background,
                        Style = GetAttributeForRoleCore (VisualRole.Editable, stack, defaultTerminalColors).Style | TextStyle.Italic
                    };

                    break;
                }

            case VisualRole.Editable:
                {
                    Attribute normal = GetAttributeForRoleCore (VisualRole.Normal, stack, defaultTerminalColors);
                    Color normalBg = ResolveNone (normal.Background, defaultTerminalColors);
                    bool isDark = normalBg.IsDarkColor ();
                    Color resolvedFg = ResolveNone (normal.Foreground, defaultTerminalColors, true);

                    result = normal with { Foreground = resolvedFg, Background = resolvedFg.GetDimmerColor (0.5, isDark) };

                    break;
                }

            case VisualRole.ReadOnly:
                {
                    Attribute editable = GetAttributeForRoleCore (VisualRole.Editable, stack, defaultTerminalColors);
                    bool isDark = ResolveNone (editable.Background, defaultTerminalColors).IsDarkColor ();

                    result = editable with { Foreground = editable.Foreground.GetDimmerColor (0.05, isDark) };

                    break;
                }

            case VisualRole.Code:
                {
                    Attribute editable = GetAttributeForRoleCore (VisualRole.Editable, stack, defaultTerminalColors);
                    bool isDark = ResolveNone (editable.Background, defaultTerminalColors).IsDarkColor ();

                    result = editable with { Background = editable.Background.GetDimmerColor (0.2, isDark), Style = editable.Style | TextStyle.Bold };

                    break;
                }

            case VisualRole.CodeComment:
            case VisualRole.CodeKeyword:
            case VisualRole.CodeString:
            case VisualRole.CodeNumber:
            case VisualRole.CodeOperator:
            case VisualRole.CodeType:
            case VisualRole.CodePreprocessor:
            case VisualRole.CodeIdentifier:
            case VisualRole.CodeConstant:
            case VisualRole.CodePunctuation:
            case VisualRole.CodeFunctionName:
            case VisualRole.CodeAttribute:
                {
                    result = GetAttributeForRoleCore (VisualRole.Code, stack, defaultTerminalColors);

                    break;
                }

            case VisualRole.Disabled:
                {
                    Attribute normal = GetAttributeForRoleCore (VisualRole.Normal, stack, defaultTerminalColors);
                    Color normalBg = ResolveNone (normal.Background, defaultTerminalColors);
                    bool isDark = normalBg.IsDarkColor ();

                    result = normal with { Foreground = ResolveNone (normal.Foreground, defaultTerminalColors, true).GetDimmerColor (0.05, isDark) };

                    break;
                }

            case VisualRole.HotNormal:
                {
                    Attribute normal = GetAttributeForRoleCore (VisualRole.Normal, stack, defaultTerminalColors);

                    result = normal with { Style = normal.Style | TextStyle.Underline };

                    break;
                }

            case VisualRole.HotFocus:
                {
                    Attribute focus = GetAttributeForRoleCore (VisualRole.Focus, stack, defaultTerminalColors);

                    result = focus with { Style = focus.Style | TextStyle.Underline };

                    break;
                }

            case VisualRole.HotActive:
                {
                    Attribute active = GetAttributeForRoleCore (VisualRole.Active, stack, defaultTerminalColors);

                    result = active with { Style = active.Style | TextStyle.Underline };

                    break;
                }

            default:
                result = GetAttributeForRoleCore (VisualRole.Normal, stack, defaultTerminalColors);

                break;
        }

        stack.Remove (role);

        return result;
    }

    /// <summary>
    ///     Gets the <see cref="Attribute"/> associated with a specified <see cref="VisualRole"/> string.
    /// </summary>
    /// <param name="roleName">The name of the <see cref="VisualRole"/> describing the element being rendered.</param>
    /// <returns>The corresponding <see cref="Attribute"/> from the <see cref="Scheme"/>.</returns>
    public Attribute GetAttributeForRole (string roleName) => Enum.TryParse (roleName, true, out VisualRole role) ? GetAttributeForRole (role) : Normal;

    // Helper method for property _get implementation
    private Attribute GetAttributeForRoleProperty (Attribute? explicitValue, VisualRole role) => explicitValue ?? GetAttributeForRoleCore (role, [], null);

    // Helper method for property _set implementation
    private Attribute? SetAttributeForRoleProperty (Attribute value, VisualRole role)
    {
        // Reassigning an explicit value must not change the role to implicit.
        if (TryGetExplicitlySetAttributeForRole (role, out Attribute? explicitValue) && explicitValue == value)
        {
            return value;
        }

        // If value is the same as the algorithm value, use null
        if (GetAttributeForRoleCore (role, [], null) == value)
        {
            return null;
        }

        return value;
    }

    private readonly Attribute? _normal;

    /// <summary>
    ///     The default visual role for unfocused, unselected, enabled elements.
    ///     The Normal attribute must always be set. All other attributes are optional, and if not explicitly
    ///     set, will be automatically generated. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute Normal { get => _normal ?? Attribute.Default; init => _normal = value; }

    private readonly Attribute? _hotNormal;

    /// <summary>
    ///     The visual role for <see cref="Normal"/> elements with a <see cref="View.HotKey"/> indicator.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute HotNormal
    {
        get => GetAttributeForRoleProperty (_hotNormal, VisualRole.HotNormal);
        init => _hotNormal = SetAttributeForRoleProperty (value, VisualRole.HotNormal);
    }

    private readonly Attribute? _focus;

    /// <summary>
    ///     The visual role when the element is focused.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute Focus
    {
        get => GetAttributeForRoleProperty (_focus, VisualRole.Focus);
        init => _focus = SetAttributeForRoleProperty (value, VisualRole.Focus);
    }

    private readonly Attribute? _hotFocus;

    /// <summary>
    ///     The visual role for <see cref="Focus"/> elements with a <see cref="View.HotKey"/> indicator.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute HotFocus
    {
        get => GetAttributeForRoleProperty (_hotFocus, VisualRole.HotFocus);
        init => _hotFocus = SetAttributeForRoleProperty (value, VisualRole.HotFocus);
    }

    private readonly Attribute? _active;

    /// <summary>
    ///     The visual role for elements that are active or selected (e.g., selected item in a <see cref="ListView"/>). Also
    ///     used
    ///     for headers in, <see cref="HexView"/>, <see cref="CharMap"/>.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute Active
    {
        get => GetAttributeForRoleProperty (_active, VisualRole.Active);
        init => _active = SetAttributeForRoleProperty (value, VisualRole.Active);
    }

    private readonly Attribute? _hotActive;

    /// <summary>
    ///     The visual role for <see cref="Active"/> elements with a <see cref="View.HotKey"/> indicator.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute HotActive
    {
        get => GetAttributeForRoleProperty (_hotActive, VisualRole.HotActive);
        init => _hotActive = SetAttributeForRoleProperty (value, VisualRole.HotActive);
    }

    private readonly Attribute? _highlight;

    /// <summary>
    ///     The visual role for elements that are highlighted (e.g., when the mouse is inside a <see cref="Button"/>).
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute Highlight
    {
        get => GetAttributeForRoleProperty (_highlight, VisualRole.Highlight);
        init => _highlight = SetAttributeForRoleProperty (value, VisualRole.Highlight);
    }

    private readonly Attribute? _editable;

    /// <summary>
    ///     The visual role for elements that are editable (e.g., <see cref="TextField"/>.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute Editable
    {
        get => GetAttributeForRoleProperty (_editable, VisualRole.Editable);
        init => _editable = SetAttributeForRoleProperty (value, VisualRole.Editable);
    }

    private readonly Attribute? _readOnly;

    /// <summary>
    ///     The visual role for elements that are normally editable but currently read-only.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute ReadOnly
    {
        get => GetAttributeForRoleProperty (_readOnly, VisualRole.ReadOnly);
        init => _readOnly = SetAttributeForRoleProperty (value, VisualRole.ReadOnly);
    }

    private readonly Attribute? _disabled;

    /// <summary>
    ///     The visual role for elements that are disabled and not interactable.
    ///     If not explicitly set, will be a derived value. See the description for <see cref="Scheme"/> for details on the
    ///     algorithm used.
    /// </summary>
    public Attribute Disabled
    {
        get => GetAttributeForRoleProperty (_disabled, VisualRole.Disabled);
        init => _disabled = SetAttributeForRoleProperty (value, VisualRole.Disabled);
    }

    private readonly Attribute? _code;

    /// <summary>
    ///     The visual role for preformatted or source code content (e.g., <see cref="MarkdownCodeBlock"/>, inline code).
    ///     If not explicitly set, derived from <see cref="Editable"/> with a dimmed background and
    ///     <see cref="TextStyle.Bold"/>.
    /// </summary>
    public Attribute Code { get => GetAttributeForRoleProperty (_code, VisualRole.Code); init => _code = SetAttributeForRoleProperty (value, VisualRole.Code); }


    private readonly Attribute? _codeComment;

    /// <summary>The visual role for source-code comments.</summary>
    public Attribute CodeComment { get => GetAttributeForRoleProperty (_codeComment, VisualRole.CodeComment); init => _codeComment = SetAttributeForRoleProperty (value, VisualRole.CodeComment); }

    private readonly Attribute? _codeKeyword;

    /// <summary>The visual role for source-code keywords.</summary>
    public Attribute CodeKeyword { get => GetAttributeForRoleProperty (_codeKeyword, VisualRole.CodeKeyword); init => _codeKeyword = SetAttributeForRoleProperty (value, VisualRole.CodeKeyword); }

    private readonly Attribute? _codeString;

    /// <summary>The visual role for source-code string literals.</summary>
    public Attribute CodeString { get => GetAttributeForRoleProperty (_codeString, VisualRole.CodeString); init => _codeString = SetAttributeForRoleProperty (value, VisualRole.CodeString); }

    private readonly Attribute? _codeNumber;

    /// <summary>The visual role for source-code numeric literals.</summary>
    public Attribute CodeNumber { get => GetAttributeForRoleProperty (_codeNumber, VisualRole.CodeNumber); init => _codeNumber = SetAttributeForRoleProperty (value, VisualRole.CodeNumber); }

    private readonly Attribute? _codeOperator;

    /// <summary>The visual role for source-code operators.</summary>
    public Attribute CodeOperator { get => GetAttributeForRoleProperty (_codeOperator, VisualRole.CodeOperator); init => _codeOperator = SetAttributeForRoleProperty (value, VisualRole.CodeOperator); }

    private readonly Attribute? _codeType;

    /// <summary>The visual role for source-code type names.</summary>
    public Attribute CodeType { get => GetAttributeForRoleProperty (_codeType, VisualRole.CodeType); init => _codeType = SetAttributeForRoleProperty (value, VisualRole.CodeType); }

    private readonly Attribute? _codePreprocessor;

    /// <summary>The visual role for source-code preprocessor directives.</summary>
    public Attribute CodePreprocessor { get => GetAttributeForRoleProperty (_codePreprocessor, VisualRole.CodePreprocessor); init => _codePreprocessor = SetAttributeForRoleProperty (value, VisualRole.CodePreprocessor); }

    private readonly Attribute? _codeIdentifier;

    /// <summary>The visual role for source-code identifiers.</summary>
    public Attribute CodeIdentifier { get => GetAttributeForRoleProperty (_codeIdentifier, VisualRole.CodeIdentifier); init => _codeIdentifier = SetAttributeForRoleProperty (value, VisualRole.CodeIdentifier); }

    private readonly Attribute? _codeConstant;

    /// <summary>The visual role for source-code constants.</summary>
    public Attribute CodeConstant { get => GetAttributeForRoleProperty (_codeConstant, VisualRole.CodeConstant); init => _codeConstant = SetAttributeForRoleProperty (value, VisualRole.CodeConstant); }

    private readonly Attribute? _codePunctuation;

    /// <summary>The visual role for source-code punctuation.</summary>
    public Attribute CodePunctuation { get => GetAttributeForRoleProperty (_codePunctuation, VisualRole.CodePunctuation); init => _codePunctuation = SetAttributeForRoleProperty (value, VisualRole.CodePunctuation); }

    private readonly Attribute? _codeFunctionName;

    /// <summary>The visual role for source-code function names.</summary>
    public Attribute CodeFunctionName { get => GetAttributeForRoleProperty (_codeFunctionName, VisualRole.CodeFunctionName); init => _codeFunctionName = SetAttributeForRoleProperty (value, VisualRole.CodeFunctionName); }

    private readonly Attribute? _codeAttribute;

    /// <summary>The visual role for source-code attributes.</summary>
    public Attribute CodeAttribute { get => GetAttributeForRoleProperty (_codeAttribute, VisualRole.CodeAttribute); init => _codeAttribute = SetAttributeForRoleProperty (value, VisualRole.CodeAttribute); }

    /// <inheritdoc/>
    public virtual bool Equals (Scheme? other) =>
        other is { }
        && EqualityComparer<Attribute>.Default.Equals (Normal, other.Normal)
        && EqualityComparer<Attribute>.Default.Equals (HotNormal, other.HotNormal)
        && EqualityComparer<Attribute>.Default.Equals (Focus, other.Focus)
        && EqualityComparer<Attribute>.Default.Equals (HotFocus, other.HotFocus)
        && EqualityComparer<Attribute>.Default.Equals (Active, other.Active)
        && EqualityComparer<Attribute>.Default.Equals (HotActive, other.HotActive)
        && EqualityComparer<Attribute>.Default.Equals (Highlight, other.Highlight)
        && EqualityComparer<Attribute>.Default.Equals (Editable, other.Editable)
        && EqualityComparer<Attribute>.Default.Equals (ReadOnly, other.ReadOnly)
        && EqualityComparer<Attribute>.Default.Equals (Disabled, other.Disabled)
        && EqualityComparer<Attribute>.Default.Equals (Code, other.Code)
        && EqualityComparer<Attribute>.Default.Equals (CodeComment, other.CodeComment)
        && EqualityComparer<Attribute>.Default.Equals (CodeKeyword, other.CodeKeyword)
        && EqualityComparer<Attribute>.Default.Equals (CodeString, other.CodeString)
        && EqualityComparer<Attribute>.Default.Equals (CodeNumber, other.CodeNumber)
        && EqualityComparer<Attribute>.Default.Equals (CodeOperator, other.CodeOperator)
        && EqualityComparer<Attribute>.Default.Equals (CodeType, other.CodeType)
        && EqualityComparer<Attribute>.Default.Equals (CodePreprocessor, other.CodePreprocessor)
        && EqualityComparer<Attribute>.Default.Equals (CodeIdentifier, other.CodeIdentifier)
        && EqualityComparer<Attribute>.Default.Equals (CodeConstant, other.CodeConstant)
        && EqualityComparer<Attribute>.Default.Equals (CodePunctuation, other.CodePunctuation)
        && EqualityComparer<Attribute>.Default.Equals (CodeFunctionName, other.CodeFunctionName)
        && EqualityComparer<Attribute>.Default.Equals (CodeAttribute, other.CodeAttribute);

    /// <inheritdoc/>
    public override int GetHashCode () =>
        HashCode.Combine (HashCode.Combine (Normal, HotNormal, Focus, HotFocus, Active, HotActive, Highlight, Editable),
                          HashCode.Combine (ReadOnly, Disabled, Code, CodeComment, CodeKeyword, CodeString, CodeNumber, CodeOperator),
                          HashCode.Combine (CodeType, CodePreprocessor, CodeIdentifier, CodeConstant, CodePunctuation, CodeFunctionName, CodeAttribute));

    /// <inheritdoc/>
    public override string ToString () =>
        $"Normal: {Normal}; HotNormal: {HotNormal}; Focus: {Focus}; HotFocus: {HotFocus}; "
        + $"Active: {Active}; HotActive: {HotActive}; Highlight: {Highlight}; Editable: {Editable}; "
        + $"ReadOnly: {ReadOnly}; Disabled: {Disabled}; Code: {Code}; "
        + $"CodeComment: {CodeComment}; CodeKeyword: {CodeKeyword}; CodeString: {CodeString}; CodeNumber: {CodeNumber}; "
        + $"CodeOperator: {CodeOperator}; CodeType: {CodeType}; CodePreprocessor: {CodePreprocessor}; "
        + $"CodeIdentifier: {CodeIdentifier}; CodeConstant: {CodeConstant}; CodePunctuation: {CodePunctuation}; "
        + $"CodeFunctionName: {CodeFunctionName}; CodeAttribute: {CodeAttribute}";

    /// <summary>
    ///     Resolves <see cref="Color.None"/> to a concrete color for use in color math (brighten, dim, invert).
    ///     Uses the provided terminal default colors (detected via OSC 10/11) if available,
    ///     otherwise falls back to White (foreground) or Black (background).
    /// </summary>
    /// <param name="color">The color to resolve.</param>
    /// <param name="defaultTerminalColors">
    ///     The terminal's actual default foreground/background colors, or <see langword="null"/> to use fallback.
    /// </param>
    /// <param name="isForeground">
    ///     <see langword="true"/> to resolve as a foreground color (falls back to White);
    ///     <see langword="false"/> to resolve as a background color (falls back to Black).
    /// </param>
    /// <returns>The resolved color, guaranteed to not be <see cref="Color.None"/>.</returns>
    private static Color ResolveNone (Color color, Attribute? defaultTerminalColors, bool isForeground = false)
    {
        if (color != Color.None)
        {
            return color;
        }

        if (defaultTerminalColors is { } attr)
        {
            return isForeground ? attr.Foreground : attr.Background;
        }

        return isForeground ? new Color (255, 255, 255) : new Color (0, 0);
    }
}
