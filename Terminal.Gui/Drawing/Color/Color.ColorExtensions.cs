using System.Collections.Frozen;
using ColorHelper;

namespace Terminal.Gui.Drawing;

internal static class ColorExtensions
{
    // TODO: This should be refactored to support all W3CColors (`ColorStrings` and this should be merged).
    // TODO: ColorName and AnsiColorCode are only needed when a driver is in Force16Color mode and we
    // TODO: should be able to remove these from any non-Driver-specific usages.
    private static FrozenDictionary<Color, ColorName16>? _colorToNameMap;

    static ColorExtensions ()
    {
        Dictionary<ColorName16, AnsiColorCode> nameToCodeMap = new ()
        {
            { ColorName16.Black, AnsiColorCode.BLACK },
            { ColorName16.Blue, AnsiColorCode.BLUE },
            { ColorName16.Green, AnsiColorCode.GREEN },
            { ColorName16.Cyan, AnsiColorCode.CYAN },
            { ColorName16.Red, AnsiColorCode.RED },
            { ColorName16.Magenta, AnsiColorCode.MAGENTA },
            { ColorName16.Yellow, AnsiColorCode.YELLOW },
            { ColorName16.Gray, AnsiColorCode.WHITE },
            { ColorName16.DarkGray, AnsiColorCode.BRIGHT_BLACK },
            { ColorName16.BrightBlue, AnsiColorCode.BRIGHT_BLUE },
            { ColorName16.BrightGreen, AnsiColorCode.BRIGHT_GREEN },
            { ColorName16.BrightCyan, AnsiColorCode.BRIGHT_CYAN },
            { ColorName16.BrightRed, AnsiColorCode.BRIGHT_RED },
            { ColorName16.BrightMagenta, AnsiColorCode.BRIGHT_MAGENTA },
            { ColorName16.BrightYellow, AnsiColorCode.BRIGHT_YELLOW },
            { ColorName16.White, AnsiColorCode.BRIGHT_WHITE }
        };
        ColorName16ToAnsiColorMap = nameToCodeMap.ToFrozenDictionary ();

        ColorToName16Map = new Dictionary<Color, ColorName16>
        {
            // These match the values in Strings.resx, which are the W3C colors.
            { new Color(0, 0, 0), ColorName16.Black },
            { new Color(0, 0, 255), ColorName16.Blue },
            { new Color(0, 128, 0), ColorName16.Green },
            { new Color(0, 255, 255), ColorName16.Cyan },
            { new Color(255, 0, 0), ColorName16.Red },
            { new Color(255, 0, 255), ColorName16.Magenta },
            { new Color(255, 255, 0), ColorName16.Yellow },
            { new Color(128, 128, 128), ColorName16.Gray },
            { new Color(118, 118, 118), ColorName16.DarkGray },
            { new Color(59, 120, 255), ColorName16.BrightBlue },
            { new Color(22, 198, 12), ColorName16.BrightGreen },
            { new Color(97, 214, 214), ColorName16.BrightCyan },
            { new Color(231, 72, 86), ColorName16.BrightRed },
            { new Color(180, 0, 158), ColorName16.BrightMagenta },
            { new Color(249, 241, 165), ColorName16.BrightYellow },
            { new Color(255, 255, 255), ColorName16.White }
        }.ToFrozenDictionary ();
    }

    /// <summary>Defines the 16 legacy color names and their corresponding ANSI color codes.</summary>
    internal static FrozenDictionary<ColorName16, AnsiColorCode> ColorName16ToAnsiColorMap { get; }

    /// <summary>Reverse mapping for <see cref="ColorToName16Map"/>.</summary>
    internal static FrozenDictionary<ColorName16, Color>? ColorName16ToColorMap { get; private set; }

    /// <summary>
    ///     Gets or sets a <see cref="FrozenDictionary{TKey,TValue}"/> that maps legacy 16-color values to the
    ///     corresponding <see cref="ColorName16"/>.
    /// </summary>
    /// <remarks>
    ///     Setter should be called as infrequently as possible, as <see cref="FrozenDictionary{TKey,TValue}"/> is
    ///     expensive to create.
    /// </remarks>
    internal static FrozenDictionary<Color, ColorName16>? ColorToName16Map
    {
        get => _colorToNameMap;
        set
        {
            _colorToNameMap = value;

            //Also be sure to set the reverse mapping
            if (value is { })
            {
                ColorName16ToColorMap = value.ToFrozenDictionary (static kvp => kvp.Value, static kvp => kvp.Key);
            }
        }
    }
}
