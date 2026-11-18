using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.Drawing;

/// <summary>
/// Standard (W3C+) color name resolver.
/// </summary>
public class StandardColorsNameResolver : IColorNameResolver
{
    /// <inheritdoc/>
    public IEnumerable<string> GetColorNames () => StandardColors.GetColorNames ();

    /// <inheritdoc/>
    public bool TryParseColor (ReadOnlySpan<char> name, out Color color) => StandardColors.TryParseColor (name, out color);

    /// <inheritdoc/>
    public bool TryNameColor (Color color, [NotNullWhen (true)] out string? name) => StandardColors.TryNameColor (color, out name);
}
