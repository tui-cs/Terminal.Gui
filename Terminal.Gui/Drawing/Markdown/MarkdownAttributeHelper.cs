namespace Terminal.Gui.Drawing;

/// <summary>
///     Resolves <see cref="MarkdownStyleRole"/> values to <see cref="Attribute"/> instances
///     for rendering styled Markdown content. Used by both <see cref="Markdown"/> and
///     <see cref="MarkdownTable"/> to ensure consistent visual treatment of inline styles.
/// </summary>
internal static class MarkdownAttributeHelper
{
    /// <summary>
    ///     Returns the <see cref="Attribute"/> that should be used to render a
    ///     <see cref="StyledSegment"/> based on its <see cref="MarkdownStyleRole"/>.
    /// </summary>
    /// <param name="view">
    ///     The <see cref="View"/> whose scheme provides the base <see cref="Attribute"/>
    ///     via <see cref="View.GetAttributeForRole"/>.
    /// </param>
    /// <param name="segment">The styled segment to resolve an attribute for.</param>
    /// <param name="highlighter">
    ///     Optional syntax highlighter. When non-null, the highlighter is queried first for
    ///     a theme-derived attribute via <see cref="ISyntaxHighlighter.GetAttributeForScope"/>.
    /// </param>
    /// <param name="themeBackground">
    ///     When non-null, overrides the view's normal background with the given color.
    ///     Typically set to <see cref="ISyntaxHighlighter.DefaultBackground"/> when
    ///     <see cref="Markdown.UseThemeBackground"/> is <see langword="true"/>.
    /// </param>
    /// <returns>A fully resolved <see cref="Attribute"/> ready for drawing.</returns>
    public static Attribute GetAttributeForSegment (View view, StyledSegment segment, ISyntaxHighlighter? highlighter = null, Color? themeBackground = null)
    {
        Attribute attribute = ResolveSegmentAttribute (view, segment, highlighter, themeBackground);

        return attribute with { Style = attribute.Style | segment.AdditionalStyle };
    }

    private static Attribute ResolveSegmentAttribute (View view, StyledSegment segment, ISyntaxHighlighter? highlighter, Color? themeBackground)
    {
        if (segment.Role is { } role)
        {
            Attribute roleAttr = view.GetAttributeForRole (role);

            return ResolveRoleAttribute (view, role, roleAttr, themeBackground);
        }

        if (segment.Attribute is { } explicitAttr)
        {
            // When a caller-provided background override is present, apply it even to
            // segments that carry an explicit attribute from the highlighter. This keeps
            // the token foreground colours but ensures the background matches the fill
            // colour of the containing code block / viewport.
            return themeBackground is { } overrideBg ? explicitAttr with { Background = overrideBg } : explicitAttr;
        }

        // Use the provided theme background, or fall back to the view's normal background.
        Attribute viewNormal = view.GetAttributeForRole (VisualRole.Normal);
        Color bg = themeBackground ?? viewNormal.Background;

        if (highlighter?.GetAttributeForScope (segment.StyleRole) is { } scopeAttr)
        {
            return scopeAttr with { Background = bg };
        }

        Attribute baseAttr = viewNormal with { Background = bg };

        return segment.StyleRole switch
        {
            MarkdownStyleRole.Heading => baseAttr with { Style = baseAttr.Style | TextStyle.Bold },
            MarkdownStyleRole.HeadingMarker => baseAttr with { Style = baseAttr.Style | TextStyle.Bold },
            MarkdownStyleRole.Emphasis => baseAttr with { Style = baseAttr.Style | TextStyle.Italic },
            MarkdownStyleRole.Strong => baseAttr with { Style = baseAttr.Style | TextStyle.Bold },
            MarkdownStyleRole.InlineCode or MarkdownStyleRole.CodeBlock => themeBackground is { } codeBg
                                                                               ? view.GetAttributeForRole (VisualRole.Code) with { Background = codeBg }
                                                                               : view.GetAttributeForRole (VisualRole.Code),
            MarkdownStyleRole.Link => MakeLinkAttribute (baseAttr, segment),
            MarkdownStyleRole.Quote => baseAttr with { Style = baseAttr.Style | TextStyle.Faint },
            MarkdownStyleRole.Table => baseAttr with { Style = baseAttr.Style | TextStyle.Bold },
            MarkdownStyleRole.ThematicBreak => baseAttr with { Style = baseAttr.Style | TextStyle.Faint },
            MarkdownStyleRole.ImageAlt => baseAttr with { Style = baseAttr.Style | TextStyle.Italic },
            MarkdownStyleRole.TaskDone => baseAttr with { Style = baseAttr.Style | TextStyle.Strikethrough },
            MarkdownStyleRole.TaskTodo => baseAttr with { Style = baseAttr.Style | TextStyle.Bold },
            MarkdownStyleRole.ListMarker => baseAttr with { Style = baseAttr.Style | TextStyle.Bold },
            MarkdownStyleRole.Strikethrough => baseAttr with { Style = baseAttr.Style | TextStyle.Strikethrough },
            _ => baseAttr
        };
    }

    private static Attribute ResolveRoleAttribute (View view, VisualRole role, Attribute roleAttr, Color? themeBackground)
    {
        if (themeBackground is { } roleBg)
        {
            return roleAttr with { Background = roleBg };
        }

        if (!IsCodeTokenRole (role) || roleAttr.Background != Color.None)
        {
            return roleAttr;
        }

        return roleAttr with { Background = view.GetAttributeForRole (VisualRole.Code).Background };
    }

    private static bool IsCodeTokenRole (VisualRole role) =>
        role is VisualRole.CodeComment
             or VisualRole.CodeKeyword
             or VisualRole.CodeString
             or VisualRole.CodeNumber
             or VisualRole.CodeOperator
             or VisualRole.CodeType
             or VisualRole.CodePreprocessor
             or VisualRole.CodeIdentifier
             or VisualRole.CodeConstant
             or VisualRole.CodePunctuation
             or VisualRole.CodeFunctionName
             or VisualRole.CodeAttribute;

    /// <summary>
    ///     Converts a list of <see cref="InlineRun"/> (from parsing) into
    ///     <see cref="StyledSegment"/> instances suitable for rendering.
    /// </summary>
    public static List<StyledSegment> ToStyledSegments (IReadOnlyList<InlineRun> runs)
    {
        List<StyledSegment> segments = new (runs.Count);

        foreach (InlineRun run in runs)
        {
            segments.Add (run.ToStyledSegment ());
        }

        return segments;
    }

    private static Attribute MakeLinkAttribute (Attribute normal, StyledSegment segment) =>
        IsMarkdownLinkTarget (segment.Url) ? normal with { Style = normal.Style | TextStyle.Underline } : normal;

    internal static bool IsMarkdownLinkTarget (string? url)
    {
        if (string.IsNullOrWhiteSpace (url))
        {
            return false;
        }

        if (url.StartsWith ('#'))
        {
            return true;
        }

        if (TryCreateSafeAbsoluteUri (url, out _))
        {
            return true;
        }

        return Uri.TryCreate (url, UriKind.Relative, out _);
    }

    internal static bool TryCreateSafeAbsoluteUri (string? url, out Uri? absoluteUri)
    {
        absoluteUri = null;

        if (!Uri.TryCreate (url, UriKind.Absolute, out Uri? parsed) || parsed is null)
        {
            return false;
        }

        if (!Link.SafeSchemes.Contains (parsed.Scheme))
        {
            return false;
        }

        absoluteUri = parsed;

        return true;
    }
}
