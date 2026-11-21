namespace Terminal.Gui.Views;

internal sealed class CodeRoleLegendHighlighter : ISyntaxHighlighter
{
    private readonly TextMateSyntaxHighlighter _textMate = new ();

    private static readonly (string Text, VisualRole Role) [] _roleMarkers =
    [
        (nameof (VisualRole.CodePreprocessor), VisualRole.CodePreprocessor),
        (nameof (VisualRole.CodeFunctionName), VisualRole.CodeFunctionName),
        (nameof (VisualRole.CodePunctuation), VisualRole.CodePunctuation),
        (nameof (VisualRole.CodeIdentifier), VisualRole.CodeIdentifier),
        (nameof (VisualRole.CodeAttribute), VisualRole.CodeAttribute),
        (nameof (VisualRole.CodeComment), VisualRole.CodeComment),
        (nameof (VisualRole.CodeKeyword), VisualRole.CodeKeyword),
        (nameof (VisualRole.CodeString), VisualRole.CodeString),
        (nameof (VisualRole.CodeNumber), VisualRole.CodeNumber),
        (nameof (VisualRole.CodeOperator), VisualRole.CodeOperator),
        (nameof (VisualRole.CodeConstant), VisualRole.CodeConstant),
        (nameof (VisualRole.CodeType), VisualRole.CodeType),
        (nameof (VisualRole.Code), VisualRole.Code)
    ];

    public IReadOnlyList<StyledSegment> Highlight (string code, string? language)
    {
        List<StyledSegment> segments = [];

        foreach (StyledSegment segment in _textMate.Highlight (code, language))
        {
            AddRoleSegments (segments, segment);
        }

        return segments;
    }

    public void ResetState () => _textMate.ResetState ();

    public string ThemeName => ((ISyntaxHighlighter)_textMate).ThemeName;

    public Color? DefaultBackground => _textMate.DefaultBackground;

    public Attribute? GetAttributeForScope (MarkdownStyleRole role) => _textMate.GetAttributeForScope (role);

    internal static void AddRoleSegments (List<StyledSegment> segments, StyledSegment segment)
    {
        int index = 0;

        while (index < segment.Text.Length)
        {
            (string text, VisualRole role) = FindRoleMarker (segment.Text, index);

            if (text.Length == 0)
            {
                AddSegment (segments, segment, segment.Text [index..(index + 1)], segment.Role);
                index++;

                continue;
            }

            AddSegment (segments, segment, text, role);
            index += text.Length;
        }
    }

    private static (string Text, VisualRole Role) FindRoleMarker (string text, int index)
    {
        foreach ((string marker, VisualRole role) in _roleMarkers)
        {
            if (text.AsSpan (index).StartsWith (marker, StringComparison.Ordinal))
            {
                return (marker, role);
            }
        }

        return (string.Empty, VisualRole.Code);
    }

    private static void AddSegment (List<StyledSegment> segments, StyledSegment source, string text, VisualRole? role)
    {
        segments.Add (source.WithText (text, role));
    }
}
