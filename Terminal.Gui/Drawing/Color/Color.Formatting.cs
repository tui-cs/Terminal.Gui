using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Terminal.Gui.Drawing;

public readonly partial record struct Color
{
    /// <inheritdoc cref="object.ToString"/>
    /// <summary>
    ///     Returns a <see langword="string"/> representation of the current <see cref="Color"/> value, according to the
    ///     provided <paramref name="formatString"/> and optional <paramref name="formatProvider"/>.
    /// </summary>
    /// <param name="formatString">
    ///     A format string that will be passed to
    ///     <see cref="string.Format(System.IFormatProvider?,string,object?[])"/>.
    ///     <para/>
    ///     See remarks for parameters passed to that method.
    /// </param>
    /// <param name="formatProvider">
    ///     An optional <see cref="IFormatProvider"/> to use when formatting the <see cref="Color"/>
    ///     using custom format strings not specified for this method. Provides this instance as <see cref="Argb"/>. <br/> If
    ///     this parameter is not null, the specified <see cref="IFormatProvider"/> will be used instead of the custom
    ///     formatting provided by the <see cref="Color"/> type.
    ///     <para/>
    ///     See remarks for defined format strings.
    /// </param>
    /// <remarks>
    ///     Pre-defined format strings for this method, if a custom <paramref name="formatProvider"/> is not supplied are:
    ///     <list type="bullet">
    ///         <listheader>
    ///             <term>Value</term> <description>Result</description>
    ///         </listheader>
    ///         <item>
    ///             <term>g or null or empty string</term>
    ///             <description>
    ///                 General/default format - Returns a named <see cref="Color"/> if there is a match, or a
    ///                 24-bit/3-byte/6-hex digit string in "#RRGGBB" format.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>G</term>
    ///             <description>
    ///                 Extended general format - Returns a named <see cref="Color"/> if there is a match, or a
    ///                 32-bit/4-byte/8-hex digit string in "#AARRGGBB" format.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>d</term>
    ///             <description>
    ///                 Decimal format - Returns a 3-component decimal representation of the <see cref="Color"/> in
    ///                 "rgb(R,G,B)" format.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>D</term>
    ///             <description>
    ///                 Extended decimal format - Returns a 4-component decimal representation of the
    ///                 <see cref="Color"/> in "rgba(R,G,B,A)" format.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         If <paramref name="formatProvider"/> is provided and is a non-null <see cref="ICustomColorFormatter"/>, the
    ///         following behaviors are available, for the specified values of <paramref name="formatString"/>:
    ///         <list type="bullet">
    ///             <listheader>
    ///                 <term>Value</term> <description>Result</description>
    ///             </listheader>
    ///             <item>
    ///                 <term>null or empty string</term>
    ///                 <description>
    ///                     Calls <see cref="ICustomColorFormatter.Format(string?,byte,byte,byte,byte)"/> on the
    ///                     provided <paramref name="formatProvider"/> with the null string, and <see cref="R"/>,
    ///                     <see cref="G"/>, <see cref="B"/>, and <see cref="A"/> as typed arguments of type <see cref="Byte"/>
    ///                     .
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <term>All other values</term>
    ///                 <description>
    ///                     Calls <see cref="string.Format{TArg0}"/> with the provided
    ///                     <paramref name="formatProvider"/> and <paramref name="formatString"/> (parsed as a
    ///                     <see cref="CompositeFormat"/>), with the value of <see cref="Argb"/> as the sole
    ///                     <see langword="uint"/>-typed argument.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    [SkipLocalsInit]
    public string ToString (
        [StringSyntax (StringSyntaxAttribute.CompositeFormat)] string? formatString,
        IFormatProvider? formatProvider = null
    )
    {
        return (formatString, formatProvider) switch
        {
            // Null or empty string and null formatProvider - Revert to 'g' case behavior
            (null or { Length: 0 }, null) => ToString (),

            // Null or empty string and formatProvider is an ICustomColorFormatter - Output according to the given ICustomColorFormatted, with R, G, B, and A as typed arguments
            (null or { Length: 0 }, ICustomColorFormatter ccf) => ccf.Format (null, R, G, B, A),

            // Null or empty string and formatProvider is otherwise non-null but not the invariant culture - Output according to string.Format with the given IFormatProvider and R, G, B, and A as boxed arguments, with string.Empty as the format string
            (null or { Length: 0 }, { }) when !Equals (formatProvider, CultureInfo.InvariantCulture) =>
                string.Format (formatProvider, formatString ?? string.Empty, R, G, B, A),

            // Null or empty string and formatProvider is the invariant culture - Output according to string.Format with the given IFormatProvider and R, G, B, and A as boxed arguments, with string.Empty as the format string
            (null or { Length: 0 }, { }) when Equals (formatProvider, CultureInfo.InvariantCulture) =>
                $"#{R:X2}{G:X2}{B:X2}",

            // Non-null string and non-null formatProvider - let formatProvider handle it and give it R, G, B, and A
            ({ }, { }) => string.Format (formatProvider, CompositeFormat.Parse (formatString), R, G, B, A),

            // g format string and null formatProvider - Output as 24-bit hex according to invariant culture rules from R, G, and B
            ( ['g'], null) => ToString (),

            // G format string and null formatProvider - Output as 32-bit hex according to invariant culture rules from Argb
            ( ['G'], null) => $"#{A:X2}{R:X2}{G:X2}{B:X2}",

            // d format string and null formatProvider - Output as 24-bit decimal rgb(r,g,b) according to invariant culture rules from R, G, and B
            ( ['d'], null) => $"rgb({R:D},{G:D},{B:D})",

            // D format string and null formatProvider - Output as 32-bit decimal rgba(r,g,b,a) according to invariant culture rules from R, G, B, and A. Non-standard: a is a decimal byte value.
            ( ['D'], null) => $"rgba({R:D},{G:D},{B:D},{A:D})",

            // All other cases (formatString is not null here) - Delegate to formatProvider, first, and otherwise to invariant culture, and try to format the provided string from the channels
            ({ }, _) => string.Format (
                                       formatProvider ?? CultureInfo.InvariantCulture,
                                       CompositeFormat.Parse (formatString),
                                       R,
                                       G,
                                       B,
                                       A
                                      ),
            _ => throw new InvalidOperationException (
                                                      $"Unable to create string from Color with value {Argb}, using format string {formatString}"
                                                     )
        }
               ?? throw new InvalidOperationException (
                                                       $"Unable to create string from Color with value {Argb}, using format string {formatString}"
                                                      );
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     <para>
    ///         This method should be used only when absolutely necessary, because it <b>always</b> has more overhead than
    ///         <see cref="ToString(string?,System.IFormatProvider?)"/>, as this method results in an intermediate allocation
    ///         of one or more instances of <see langword="string"/> and a copy of that string to
    ///         <paramref name="destination"/> if formatting was successful. <br/> When possible, use
    ///         <see cref="ToString(string?,System.IFormatProvider?)"/>, which attempts to avoid intermediate allocations.
    ///     </para>
    ///     <para>
    ///         This method only returns <see langword="true"/> and with its output written to <paramref name="destination"/>
    ///         if the formatted string, <i>in its entirety</i>, will fit in <paramref name="destination"/>. If the resulting
    ///         formatted string is too large to fit in <paramref name="destination"/>, the result will be false and
    ///         <paramref name="destination"/> will be unaltered.
    ///     </para>
    ///     <para>
    ///         The resulting formatted string may be <b>shorter</b> than <paramref name="destination"/>. When this method
    ///         returns <see langword="true"/>, use <paramref name="charsWritten"/> when handling the value of
    ///         <paramref name="destination"/>.
    ///     </para>
    /// </remarks>
    [Pure]
    [SkipLocalsInit]
    public bool TryFormat (
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        // TODO: This can probably avoid a string allocation with a little more work
        try
        {
            string formattedString = ToString (format.ToString (), provider);

            if (formattedString.Length <= destination.Length)
            {
                formattedString.CopyTo (destination);
                charsWritten = formattedString.Length;

                return true;
            }
        }
        catch
        {
            destination.Clear ();
            charsWritten = 0;

            return false;
        }

        destination.Clear ();
        charsWritten = 0;

        return false;
    }

    /// <summary>Converts the provided <see langword="string"/> to a new <see cref="Color"/> value.</summary>
    /// <param name="text">
    ///     The text to analyze. Formats supported are "#RGB", "#RRGGBB", "#ARGB", "#AARRGGBB", "rgb(r,g,b)",
    ///     "rgb(r,g,b,a)", "rgba(r,g,b)", "rgba(r,g,b,a)", and any of the <see cref="ColorName16"/> string values.
    /// </param>
    /// <param name="formatProvider">
    ///     If specified and not <see langword="null"/>, will be passed to
    ///     <see cref="Parse(System.ReadOnlySpan{char},System.IFormatProvider?)"/>.
    /// </param>
    /// <returns>A <see cref="Color"/> value equivalent to <paramref name="text"/>, if parsing was successful.</returns>
    /// <remarks>While <see cref="Color"/> supports the alpha channel <see cref="A"/>, Terminal.Gui does not.</remarks>
    /// <exception cref="ArgumentNullException">If <paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    ///     If <paramref name="text"/> is an empty string or consists of only whitespace
    ///     characters.
    /// </exception>
    /// <exception cref="ColorParseException">
    ///     If thrown by
    ///     <see cref="Parse(System.ReadOnlySpan{char},System.IFormatProvider?)"/>.
    /// </exception>
    [Pure]
    [SkipLocalsInit]
    public static Color Parse (string? text, IFormatProvider? formatProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace (text, nameof (text));

        if (text is { Length: < 3 } && formatProvider is null)
        {
            throw new ColorParseException (
                                           text,
                                           reason: "Provided text is too short to be any known color format.",
                                           badValue: text
                                          );
        }

        return Parse (text.AsSpan (), formatProvider ?? CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts the provided <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to a new <see cref="Color"/>
    ///     value.
    /// </summary>
    /// <param name="text">
    ///     The text to analyze. Formats supported are "#RGB", "#RRGGBB", "#RGBA", "#AARRGGBB", "rgb(r,g,b)",
    ///     "rgb(r,g,b,a)", "rgba(r,g,b)", "rgba(r,g,b,a)", and any of the <see cref="ColorName16"/> string values.
    /// </param>
    /// <param name="formatProvider">
    ///     Optional <see cref="IFormatProvider"/> to provide parsing services for the input text.
    ///     <br/> Defaults to <see cref="CultureInfo.InvariantCulture"/> if <see langword="null"/>. <br/> If not null, must
    ///     implement <see cref="ICustomColorFormatter"/> or will be ignored and <see cref="CultureInfo.InvariantCulture"/>
    ///     will be used.
    /// </param>
    /// <returns>A <see cref="Color"/> value equivalent to <paramref name="text"/>, if parsing was successful.</returns>
    /// <remarks>While <see cref="Color"/> supports the alpha channel <see cref="A"/>, Terminal.Gui does not.</remarks>
    /// <exception cref="ArgumentException">
    ///     with an inner <see cref="FormatException"/> if <paramref name="text"/> was unable
    ///     to be successfully parsed as a <see cref="Color"/>, for any reason.
    /// </exception>
    [Pure]
    [SkipLocalsInit]
    public static Color Parse (ReadOnlySpan<char> text, IFormatProvider? formatProvider = null)
    {
        return text switch
        {
            // Null string or empty span provided
            { IsEmpty: true } when formatProvider is null =>
                throw new ColorParseException (in text, "The text provided was null or empty.", in text),

            // A valid ICustomColorFormatter was specified and the text wasn't null or empty
            { IsEmpty: false } when formatProvider is ICustomColorFormatter f => f.Parse (text),

            // Input string is only whitespace
            { Length: > 0 } when text.IsWhiteSpace () =>
                throw new ColorParseException (in text, "The text provided consisted of only whitespace characters.", in text),

            // Any string too short to possibly be any supported format.
            { Length: > 0 and < 3 } =>
                throw new ColorParseException (in text, "Text was too short to be any possible supported format.", in text),

            // The various hexadecimal cases
            ['#', ..] hexString => hexString switch
            {
                // #RGB
                ['#', var rChar, var gChar, var bChar] chars when chars [1..]
                    .IsAllAsciiHexDigits () =>
                        new Color (
                            byte.Parse ([rChar, rChar], NumberStyles.HexNumber),
                            byte.Parse ([gChar, gChar], NumberStyles.HexNumber),
                            byte.Parse ([bChar, bChar], NumberStyles.HexNumber)
                        ),

                // #ARGB
                ['#', var aChar, var rChar, var gChar, var bChar] chars when chars [1..]
                    .IsAllAsciiHexDigits () =>
                        new Color (
                            byte.Parse ([rChar, rChar], NumberStyles.HexNumber),
                            byte.Parse ([gChar, gChar], NumberStyles.HexNumber),
                            byte.Parse ([bChar, bChar], NumberStyles.HexNumber),
                            byte.Parse ([aChar, aChar], NumberStyles.HexNumber)
                        ),

                // #RRGGBB
                [
                '#', var r1Char, var r2Char, var g1Char, var g2Char, var b1Char, var b2Char
                ] chars when chars [1..].IsAllAsciiHexDigits () =>
                    new Color (
                        byte.Parse ([r1Char, r2Char], NumberStyles.HexNumber),
                        byte.Parse ([g1Char, g2Char], NumberStyles.HexNumber),
                        byte.Parse ([b1Char, b2Char], NumberStyles.HexNumber)
                    ),

                // #AARRGGBB
                [
                    '#', var a1Char, var a2Char,
                    var r1Char, var r2Char,
                    var g1Char, var g2Char,
                    var b1Char, var b2Char
                ] chars when chars [1..].IsAllAsciiHexDigits () =>
                    new Color (
                        byte.Parse ([r1Char, r2Char], NumberStyles.HexNumber),
                        byte.Parse ([g1Char, g2Char], NumberStyles.HexNumber),
                        byte.Parse ([b1Char, b2Char], NumberStyles.HexNumber),
                        byte.Parse ([a1Char, a2Char], NumberStyles.HexNumber)
                    ),
                _ => throw new ColorParseException (
                        in hexString,
                        $"Color hex string {hexString} was not in a supported format",
                        in hexString
                    )
            },

            // rgb(r,g,b) or rgb(r,g,b,a)
            ['r', 'g', 'b', '(', .., ')'] => ParseRgbaFormat (in text, 4),

            // rgba(r,g,b,a) or rgba(r,g,b)
            ['r', 'g', 'b', 'a', '(', .., ')'] => ParseRgbaFormat (in text, 5),
            // Attempt named colors
            { } when char.IsLetter (text [0]) && ColorStrings.TryParseNamedColor (text, out Color color) => color,
            // Any other input
            _ => throw new ColorParseException (in text, "Text did not match any expected format.", in text, [])
        };

        [Pure]
        [SkipLocalsInit]
        static Color ParseRgbaFormat (in ReadOnlySpan<char> originalString, in int startIndex)
        {
            ReadOnlySpan<char> valuesSubstring = originalString [startIndex..^1];
            Span<Range> valueRanges = stackalloc Range [4];

            int rangeCount = valuesSubstring.Split (
                                                    valueRanges,
                                                    ',',
                                                    StringSplitOptions.RemoveEmptyEntries
                                                    | StringSplitOptions.TrimEntries
                                                   );

            switch (rangeCount)
            {
                case 3:
                    {
                        // rgba(r,g,b)
                        ParseRgbValues (
                                        in valuesSubstring,
                                        in valueRanges,
                                        in originalString,
                                        out ReadOnlySpan<char> rSpan,
                                        out ReadOnlySpan<char> gSpan,
                                        out ReadOnlySpan<char> bSpan
                                       );

                        return new Color (int.Parse (rSpan), int.Parse (gSpan), int.Parse (bSpan));
                    }
                case 4:
                    {
                        // rgba(r,g,b,a)
                        ParseRgbValues (
                                        in valuesSubstring,
                                        in valueRanges,
                                        in originalString,
                                        out ReadOnlySpan<char> rSpan,
                                        out ReadOnlySpan<char> gSpan,
                                        out ReadOnlySpan<char> bSpan
                                       );
                        ReadOnlySpan<char> aSpan = valuesSubstring [valueRanges [3]];

                        if (!aSpan.IsAllAsciiDigits ())
                        {
                            throw new ColorParseException (
                                                           in originalString,
                                                           "Value was not composed entirely of decimal digits.",
                                                           in aSpan,
                                                           nameof (A)
                                                          );
                        }

                        return new Color (int.Parse (rSpan), int.Parse (gSpan), int.Parse (bSpan), int.Parse (aSpan));
                    }
                default:
                    throw new ColorParseException (
                                                   in originalString,
                                                   $"Wrong number of values. Expected 3 or 4 decimal integers. Got {rangeCount}.",
                                                   in originalString
                                                  );
            }

            [Pure]
            [SkipLocalsInit]
            static void ParseRgbValues (
                in ReadOnlySpan<char> valuesString,
                in Span<Range> valueComponentRanges,
                in ReadOnlySpan<char> originalString,
                out ReadOnlySpan<char> rSpan,
                out ReadOnlySpan<char> gSpan,
                out ReadOnlySpan<char> bSpan
            )
            {
                rSpan = valuesString [valueComponentRanges [0]];

                if (!rSpan.IsAllAsciiDigits ())
                {
                    throw new ColorParseException (
                                                   in originalString,
                                                   "Value was not composed entirely of decimal digits.",
                                                   in rSpan,
                                                   nameof (R)
                                                  );
                }

                gSpan = valuesString [valueComponentRanges [1]];

                if (!gSpan.IsAllAsciiDigits ())
                {
                    throw new ColorParseException (
                                                   in originalString,
                                                   "Value was not composed entirely of decimal digits.",
                                                   in gSpan,
                                                   nameof (G)
                                                  );
                }

                bSpan = valuesString [valueComponentRanges [2]];

                if (!bSpan.IsAllAsciiDigits ())
                {
                    throw new ColorParseException (
                                                   in originalString,
                                                   "Value was not composed entirely of decimal digits.",
                                                   in bSpan,
                                                   nameof (B)
                                                  );
                }
            }
        }
    }

    /// <summary>Converts the provided <see langword="string"/> to a new <see cref="Color"/> value.</summary>
    /// <param name="text">
    ///     The text to analyze. Formats supported are "#RGB", "#RRGGBB", "#ARGB", "#AARRGGBB", "rgb(r,g,b)",
    ///     "rgb(r,g,b,a)", "rgba(r,g,b)", "rgba(r,g,b,a)", and any of the <see cref="GetClosestNamedColor16(Color)"/> string
    ///     values.
    /// </param>
    /// <param name="formatProvider">
    ///     Optional <see cref="IFormatProvider"/> to provide formatting services for the input text.
    ///     <br/> Defaults to <see cref="CultureInfo.InvariantCulture"/> if <see langword="null"/>.
    /// </param>
    /// <param name="result">
    ///     The parsed value, if successful, or <see langword="default"/>(<see cref="Color"/>), if
    ///     unsuccessful.
    /// </param>
    /// <returns>A <see langword="bool"/> value indicating whether parsing was successful.</returns>
    /// <remarks>While <see cref="Color"/> supports the alpha channel <see cref="A"/>, Terminal.Gui does not.</remarks>
    [Pure]
    [SkipLocalsInit]
    public static bool TryParse (string? text, IFormatProvider? formatProvider, out Color result)
    {
        return TryParse (
                         text.AsSpan (),
                         formatProvider ?? CultureInfo.InvariantCulture,
                         out result
                        );
    }

    /// <summary>
    ///     Converts the provided <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to a new <see cref="Color"/>
    ///     value.
    /// </summary>
    /// <param name="text">
    ///     The text to analyze. Formats supported are "#RGB", "#RRGGBB", "#ARGB", "#AARRGGBB", "rgb(r,g,b)",
    ///     "rgb(r,g,b,a)", "rgba(r,g,b)", "rgba(r,g,b,a)", and any W3C color name."/> string
    ///     values.
    /// </param>
    /// <param name="formatProvider">
    ///     If specified and not <see langword="null"/>, will be passed to
    ///     <see cref="Parse(System.ReadOnlySpan{char},System.IFormatProvider?)"/>.
    /// </param>
    /// <param name="color">
    ///     The parsed value, if successful, or <see langword="default"/>(<see cref="Color"/>), if
    ///     unsuccessful.
    /// </param>
    /// <returns>A <see langword="bool"/> value indicating whether parsing was successful.</returns>
    /// <remarks>While <see cref="Color"/> supports the alpha channel <see cref="A"/>, Terminal.Gui does not.</remarks>
    [Pure]
    [SkipLocalsInit]
    public static bool TryParse (ReadOnlySpan<char> text, IFormatProvider? formatProvider, out Color color)
    {
        try
        {
            Color c = Parse (text, formatProvider);
            color = c;

            return true;
        }
        catch (ColorParseException)
        {
            color = default (Color);

            return false;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Use of this method involves a stack allocation of <paramref name="utf8Destination"/>.Length * 2 bytes. Use of
    ///     the overload taking a char span is recommended.
    /// </remarks>
    [SkipLocalsInit]
    public bool TryFormat (
        Span<byte> utf8Destination,
        out int bytesWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        Span<char> charDestination = stackalloc char [utf8Destination.Length * 2];

        if (TryFormat (charDestination, out int charsWritten, format, provider))
        {
            Encoding.UTF8.GetBytes (charDestination, utf8Destination);
            bytesWritten = charsWritten / 2;

            return true;
        }

        utf8Destination.Clear ();
        bytesWritten = 0;

        return false;
    }

    /// <inheritdoc/>
    [Pure]
    [SkipLocalsInit]
    public static Color Parse (ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) { return Parse (Encoding.UTF8.GetString (utf8Text), provider); }

    /// <inheritdoc/>
    [Pure]
    [SkipLocalsInit]
    public static bool TryParse (ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Color result)
    {
        return TryParse (Encoding.UTF8.GetString (utf8Text), provider, out result);
    }

    /// <summary>Converts the color to a string representation.</summary>
    /// <remarks>
    ///     <para>If the color is a named color, the name is returned. Otherwise, the color is returned as a hex string.</para>
    ///     <para><see cref="A"/> (Alpha channel) is ignored and the returned string will not include it for this overload.</para>
    /// </remarks>
    /// <returns>The string representation of this value in #RRGGBB format.</returns>
    [Pure]
    [SkipLocalsInit]
    public override string ToString ()
    {
        if (ColorStrings.GetColorName (this) is string colorName)
        {
            return colorName;
        }

        return $"#{R:X2}{G:X2}{B:X2}";
    }

    /// <summary>Converts the provided string to a new <see cref="Color"/> instance.</summary>
    /// <param name="text">
    ///     The text to analyze. Formats supported are "#RGB", "#RRGGBB", "#ARGB", "#AARRGGBB", "rgb(r,g,b)",
    ///     "rgb(r,g,b,a)", "rgba(r,g,b)", "rgba(r,g,b,a)", and any of the <see cref="ColorName16"/> string values.
    /// </param>
    /// <param name="color">The parsed value.</param>
    /// <returns>A boolean value indicating whether parsing was successful.</returns>
    /// <remarks>While <see cref="Color"/> supports the alpha channel <see cref="A"/>, Terminal.Gui does not.</remarks>
    public static bool TryParse (string text, [NotNullWhen (true)] out Color? color)
    {
        if (TryParse (text.AsSpan (), null, out Color c))
        {
            color = c;

            return true;
        }

        color = null;

        return false;
    }
}
