using System.Reflection;
using System.Text.Unicode;

namespace Terminal.Gui.Views;

/// <summary>
///     Represents all of the Uniicode ranges.from System.Text.Unicode.UnicodeRange plus
///     the non-BMP ranges not included.
/// </summary>
public class UnicodeRange (int start, int end, string category)
{
    /// <summary>
    ///     Gets the list of all ranges.
    /// </summary>
    public static List<UnicodeRange> Ranges => GetRanges ();

    /// <summary>
    ///     The category.
    /// </summary>
    public string Category { get; set; } = category;

    /// <summary>
    ///     Te codepoint at the start of the range.
    /// </summary>
    public int Start { get; set; } = start;

    /// <summary>
    ///     The codepoint at the end of the range.
    /// </summary>
    public int End { get; set; } = end;

    /// <summary>
    ///     Gets the list of all ranges..
    /// </summary>
    /// <returns></returns>
    public static List<UnicodeRange> GetRanges ()
    {
        IEnumerable<UnicodeRange> ranges =
            from r in typeof (UnicodeRanges).GetProperties (BindingFlags.Static | BindingFlags.Public)
            let urange = r.GetValue (null) as System.Text.Unicode.UnicodeRange
            let name = string.IsNullOrEmpty (r.Name)
                           ? $"U+{urange.FirstCodePoint:x5}-U+{urange.FirstCodePoint + urange.Length:x5}"
                           : r.Name
            where name != "None" && name != "All"
            select new UnicodeRange (urange.FirstCodePoint, urange.FirstCodePoint + urange.Length, name);

        // .NET 8.0 only supports BMP in UnicodeRanges: https://learn.microsoft.com/en-us/dotnet/api/system.text.unicode.unicoderanges?view=net-8.0
        List<UnicodeRange> nonBmpRanges = new ()
        {
            new (
                 0x1F130,
                 0x1F149,
                 "Squared Latin Capital Letters"
                ),
            new (
                 0x12400,
                 0x1240f,
                 "Cuneiform Numbers and Punctuation"
                ),
            new (0x10000, 0x1007F, "Linear B Syllabary"),
            new (0x10080, 0x100FF, "Linear B Ideograms"),
            new (0x10100, 0x1013F, "Aegean Numbers"),
            new (0x10300, 0x1032F, "Old Italic"),
            new (0x10330, 0x1034F, "Gothic"),
            new (0x10380, 0x1039F, "Ugaritic"),
            new (0x10400, 0x1044F, "Deseret"),
            new (0x10450, 0x1047F, "Shavian"),
            new (0x10480, 0x104AF, "Osmanya"),
            new (0x10800, 0x1083F, "Cypriot Syllabary"),
            new (
                 0x1D000,
                 0x1D0FF,
                 "Byzantine Musical Symbols"
                ),
            new (0x1D100, 0x1D1FF, "Musical Symbols"),
            new (0x1D300, 0x1D35F, "Tai Xuan Jing Symbols"),
            new (
                 0x1D400,
                 0x1D7FF,
                 "Mathematical Alphanumeric Symbols"
                ),
            new (0x1F600, 0x1F532, "Emojis Symbols"),
            new (
                 0x20000,
                 0x2A6DF,
                 "CJK Unified Ideographs Extension B"
                ),
            new (
                 0x2F800,
                 0x2FA1F,
                 "CJK Compatibility Ideographs Supplement"
                ),
            new (0xE0000, 0xE007F, "Tags")
        };

        return ranges.Concat (nonBmpRanges).OrderBy (r => r.Category).ToList ();
    }
}
