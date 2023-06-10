using System.Globalization;
using System.Text;

namespace Terminal.Gui;

/// <summary>
/// Extends <see cref="System.Text.Rune"/> to support TUI text manipulation.
/// </summary>
public static class RuneExtensions {
	/// <summary>
	/// Maximum Unicode code point.
	/// </summary>
	public static int MaxUnicodeCodePoint = 0x10FFFF;

	/// <summary>
	/// Gets the number of columns the rune occupies in the terminal.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="rune">The rune to measure.</param>
	/// <returns>
	/// The number of columns required to fit the rune, 0 if the argument is the null character, or
	/// -1 if the value is not printable, 
	/// otherwise the number of columns that the rune occupies.
	/// </returns>
	public static int GetColumns (this Rune rune)
	{
		// TODO: I believe there is a way to do this without using our own tables, using Rune.
		var codePoint = rune.Value;
		switch (codePoint) {
		case < 0x20:
		case >= 0x7f and < 0xa0:
			return -1;
		case < 0x7f:
			return 1;
		}
		/* binary search in table of non-spacing characters */
		if (BiSearch (codePoint, _combining, _combining.GetLength (0) - 1) != 0) {
			return 0;
		}
		/* if we arrive here, ucs is not a combining or C0/C1 control character */
		return 1 + (BiSearch (codePoint, _combiningWideChars, _combiningWideChars.GetLength (0) - 1) != 0 ? 1 : 0);
	}
	
	/// <summary>
	/// Returns <see langword="true"/> if the rune is a combining character.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="rune"></param>
	/// <returns></returns>
	public static bool IsCombiningMark (this System.Text.Rune rune)
	{
		UnicodeCategory category = Rune.GetUnicodeCategory (rune);
		return Rune.GetUnicodeCategory (rune) == UnicodeCategory.NonSpacingMark
			|| category == UnicodeCategory.SpacingCombiningMark
			|| category == UnicodeCategory.EnclosingMark;
	}

	/// <summary>
	/// Ensures the rune is not a control character and can be displayed by translating characters below 0x20
	/// to equivalent, printable, Unicode chars.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="rune"></param>
	/// <returns></returns>
	public static Rune MakePrintable (this System.Text.Rune rune) => Rune.IsControl (rune) ? new Rune (rune.Value + 0x2400) : rune;

	/// <summary>
	/// Get number of bytes required to encode the rune, based on the provided encoding.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="rune">The rune to probe.</param>
	/// <param name="encoding">The encoding used; the default is UTF8.</param>
	/// <returns>The number of bytes required.</returns>
	public static int GetEncodingLength (this Rune rune, Encoding encoding = null)
	{
		encoding ??= Encoding.UTF8;
		var bytes = encoding.GetBytes (rune.ToString ().ToCharArray ());
		var offset = 0;
		if (bytes [^1] == 0) {
			offset++;
		}
		return bytes.Length - offset;
	}

	/// <summary>
	/// Writes into the destination buffer starting at offset the UTF8 encoded version of the rune.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="rune">The rune to encode.</param>
	/// <param name="dest">The destination buffer.</param>
	/// <param name="start">Starting offset to look into.</param>
	/// <param name="count">Number of bytes valid in the buffer, or -1 to make it the length of the buffer.</param>
	/// <returns>he number of bytes written into the destination buffer.</returns>
	public static int Encode (this Rune rune, byte [] dest, int start = 0, int count = -1)
	{
		var bytes = Encoding.UTF8.GetBytes (rune.ToString ());
		var length = 0;
		for (var i = 0; i < (count == -1 ? bytes.Length : count); i++) {
			if (bytes [i] == 0) {
				break;
			}
			dest [start + i] = bytes [i];
			length++;
		}
		return length;
	}

	/// <summary>
	/// Attempts to decode the rune as a surrogate pair to UTF-16.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="rune">The rune to decode.</param>
	/// <param name="chars">The chars if the rune is a surrogate pair. Null otherwise.</param>
	/// <returns><see langword="true"/> if the rune is a valid surrogate pair; <see langword="false"/> otherwise.</returns>
	public static bool DecodeSurrogatePair (this Rune rune, out char [] chars)
	{
		if (rune.IsSurrogatePair ()) {
			chars = rune.ToString ().ToCharArray ();
			return true;
		}
		chars = null;
		return false;
	}

	/// <summary>
	/// Attempts to encode (as UTF-16) a surrogate pair.
	/// </summary>
	/// <param name="highSurrogate">The high surrogate code point.</param>
	/// <param name="lowSurrogate">The low surrogate code point.</param>
	/// <param name="result">The encoded rune.</param>
	/// <returns><see langword="true"/> if the encoding succeeded; <see langword="false"/> otherwise.</returns>
	public static bool EncodeSurrogatePair (char highSurrogate, char lowSurrogate, out Rune result)
	{
		result = default;
		if (char.IsSurrogatePair (highSurrogate, lowSurrogate)) {
			result = (Rune)char.ConvertToUtf32 (highSurrogate, lowSurrogate);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Reports whether a rune is a surrogate code point.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="System.Text.Rune"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="rune">The rune to probe.</param>
	/// <returns><see langword="true"/> if the rune is a surrogate code point; <see langword="false"/> otherwise.</returns>
	public static bool IsSurrogatePair (this Rune rune)
	{
		return char.IsSurrogatePair (rune.ToString (), 0);
	}

	/// <summary>
	/// Reports if the provided array of bytes can be encoded as UTF-8.
	/// </summary>
	/// <param name="buffer">The byte array to probe.</param>
	/// <value><c>true</c> if is valid; otherwise, <c>false</c>.</value>
	public static bool CanBeEncodedAsRune (byte [] buffer)
	{
		var str = Encoding.Unicode.GetString (buffer);
		foreach (var rune in str.EnumerateRunes ()) {
			if (rune == Rune.ReplacementChar) {
				return false;
			}
		}
		return true;
	}

	// ---------------- implementation details ------------------
	// TODO: Can this be handled by the new .NET 8 Rune type?
	static readonly int [,] _combining = new int [,] {
		{ 0x0300, 0x036F }, { 0x0483, 0x0486 }, { 0x0488, 0x0489 },
		{ 0x0591, 0x05BD }, { 0x05BF, 0x05BF }, { 0x05C1, 0x05C2 },
		{ 0x05C4, 0x05C5 }, { 0x05C7, 0x05C7 }, { 0x0600, 0x0603 },
		{ 0x0610, 0x0615 }, { 0x064B, 0x065E }, { 0x0670, 0x0670 },
		{ 0x06D6, 0x06E4 }, { 0x06E7, 0x06E8 }, { 0x06EA, 0x06ED },
		{ 0x070F, 0x070F }, { 0x0711, 0x0711 }, { 0x0730, 0x074A },
		{ 0x07A6, 0x07B0 }, { 0x07EB, 0x07F3 }, { 0x0901, 0x0902 },
		{ 0x093C, 0x093C }, { 0x0941, 0x0948 }, { 0x094D, 0x094D },
		{ 0x0951, 0x0954 }, { 0x0962, 0x0963 }, { 0x0981, 0x0981 },
		{ 0x09BC, 0x09BC }, { 0x09C1, 0x09C4 }, { 0x09CD, 0x09CD },
		{ 0x09E2, 0x09E3 }, { 0x0A01, 0x0A02 }, { 0x0A3C, 0x0A3C },
		{ 0x0A41, 0x0A42 }, { 0x0A47, 0x0A48 }, { 0x0A4B, 0x0A4D },
		{ 0x0A70, 0x0A71 }, { 0x0A81, 0x0A82 }, { 0x0ABC, 0x0ABC },
		{ 0x0AC1, 0x0AC5 }, { 0x0AC7, 0x0AC8 }, { 0x0ACD, 0x0ACD },
		{ 0x0AE2, 0x0AE3 }, { 0x0B01, 0x0B01 }, { 0x0B3C, 0x0B3C },
		{ 0x0B3F, 0x0B3F }, { 0x0B41, 0x0B43 }, { 0x0B4D, 0x0B4D },
		{ 0x0B56, 0x0B56 }, { 0x0B82, 0x0B82 }, { 0x0BC0, 0x0BC0 },
		{ 0x0BCD, 0x0BCD }, { 0x0C3E, 0x0C40 }, { 0x0C46, 0x0C48 },
		{ 0x0C4A, 0x0C4D }, { 0x0C55, 0x0C56 }, { 0x0CBC, 0x0CBC },
		{ 0x0CBF, 0x0CBF }, { 0x0CC6, 0x0CC6 }, { 0x0CCC, 0x0CCD },
		{ 0x0CE2, 0x0CE3 }, { 0x0D41, 0x0D43 }, { 0x0D4D, 0x0D4D },
		{ 0x0DCA, 0x0DCA }, { 0x0DD2, 0x0DD4 }, { 0x0DD6, 0x0DD6 },
		{ 0x0E31, 0x0E31 }, { 0x0E34, 0x0E3A }, { 0x0E47, 0x0E4E },
		{ 0x0EB1, 0x0EB1 }, { 0x0EB4, 0x0EB9 }, { 0x0EBB, 0x0EBC },
		{ 0x0EC8, 0x0ECD }, { 0x0F18, 0x0F19 }, { 0x0F35, 0x0F35 },
		{ 0x0F37, 0x0F37 }, { 0x0F39, 0x0F39 }, { 0x0F71, 0x0F7E },
		{ 0x0F80, 0x0F84 }, { 0x0F86, 0x0F87 }, { 0x0F90, 0x0F97 },
		{ 0x0F99, 0x0FBC }, { 0x0FC6, 0x0FC6 }, { 0x102D, 0x1030 },
		{ 0x1032, 0x1032 }, { 0x1036, 0x1037 }, { 0x1039, 0x1039 },
		{ 0x1058, 0x1059 }, { 0x1160, 0x11FF }, { 0x135F, 0x135F },
		{ 0x1712, 0x1714 }, { 0x1732, 0x1734 }, { 0x1752, 0x1753 },
		{ 0x1772, 0x1773 }, { 0x17B4, 0x17B5 }, { 0x17B7, 0x17BD },
		{ 0x17C6, 0x17C6 }, { 0x17C9, 0x17D3 }, { 0x17DD, 0x17DD },
		{ 0x180B, 0x180D }, { 0x18A9, 0x18A9 }, { 0x1920, 0x1922 },
		{ 0x1927, 0x1928 }, { 0x1932, 0x1932 }, { 0x1939, 0x193B },
		{ 0x1A17, 0x1A18 }, { 0x1B00, 0x1B03 }, { 0x1B34, 0x1B34 },
		{ 0x1B36, 0x1B3A }, { 0x1B3C, 0x1B3C }, { 0x1B42, 0x1B42 },
		{ 0x1B6B, 0x1B73 }, { 0x1DC0, 0x1DCA }, { 0x1DFE, 0x1DFF },
		{ 0x200B, 0x200F }, { 0x202A, 0x202E }, { 0x2060, 0x2063 },
		{ 0x206A, 0x206F }, { 0x20D0, 0x20EF }, { 0x2E9A, 0x2E9A },
		{ 0x2EF4, 0x2EFF }, { 0x2FD6, 0x2FEF }, { 0x2FFC, 0x2FFF },
		{ 0x31E4, 0x31EF }, { 0x321F, 0x321F }, { 0xA48D, 0xA48F },
		{ 0xA806, 0xA806 }, { 0xA80B, 0xA80B }, { 0xA825, 0xA826 },
		{ 0xFB1E, 0xFB1E }, { 0xFE00, 0xFE0F }, { 0xFE1A, 0xFE1F },
		{ 0xFE20, 0xFE23 }, { 0xFE53, 0xFE53 }, { 0xFE67, 0xFE67 },
		{ 0xFEFF, 0xFEFF }, { 0xFFF9, 0xFFFB },
		{ 0x10A01, 0x10A03 }, { 0x10A05, 0x10A06 }, { 0x10A0C, 0x10A0F },
		{ 0x10A38, 0x10A3A }, { 0x10A3F, 0x10A3F }, { 0x1D167, 0x1D169 },
		{ 0x1D173, 0x1D182 }, { 0x1D185, 0x1D18B }, { 0x1D1AA, 0x1D1AD },
		{ 0x1D242, 0x1D244 }, { 0xE0001, 0xE0001 }, { 0xE0020, 0xE007F },
		{ 0xE0100, 0xE01EF }
	};

	static readonly int [,] _combiningWideChars = new int [,] {
		/* Hangul Jamo init. consonants - 0x1100, 0x11ff */
		/* Miscellaneous Technical - 0x2300, 0x23ff */
		/* Hangul Syllables - 0x11a8, 0x11c2 */
		/* CJK Compatibility Ideographs - f900, fad9 */
		/* Vertical forms - fe10, fe19 */
		/* CJK Compatibility Forms - fe30, fe4f */
		/* Fullwidth Forms - ff01, ffee */
		/* Alphabetic Presentation Forms - 0xFB00, 0xFb4f */
		/* Chess Symbols - 0x1FA00, 0x1FA0f */

		{ 0x1100, 0x115f }, { 0x231a, 0x231b }, { 0x2329, 0x232a },
		{ 0x23e9, 0x23ec }, { 0x23f0, 0x23f0 }, { 0x23f3, 0x23f3 },
		{ 0x25fd, 0x25fe }, { 0x2614, 0x2615 }, { 0x2648, 0x2653 },
		{ 0x267f, 0x267f }, { 0x2693, 0x2693 }, { 0x26a1, 0x26a1 },
		{ 0x26aa, 0x26ab }, { 0x26bd, 0x26be }, { 0x26c4, 0x26c5 },
		{ 0x26ce, 0x26ce }, { 0x26d4, 0x26d4 }, { 0x26ea, 0x26ea },
		{ 0x26f2, 0x26f3 }, { 0x26f5, 0x26f5 }, { 0x26fa, 0x26fa },
		{ 0x26fd, 0x26fd }, { 0x2705, 0x2705 }, { 0x270a, 0x270b },
		{ 0x2728, 0x2728 }, { 0x274c, 0x274c }, { 0x274e, 0x274e },
		{ 0x2753, 0x2755 }, { 0x2757, 0x2757 }, { 0x2795, 0x2797 },
		{ 0x27b0, 0x27b0 }, { 0x27bf, 0x27bf }, { 0x2b1b, 0x2b1c },
		{ 0x2b50, 0x2b50 }, { 0x2b55, 0x2b55 }, { 0x2e80, 0x303e },
		{ 0x3041, 0x3096 }, { 0x3099, 0x30ff }, { 0x3105, 0x312f },
		{ 0x3131, 0x318e }, { 0x3190, 0x3247 }, { 0x3250, 0x4dbf },
		{ 0x4e00, 0xa4c6 }, { 0xa960, 0xa97c }, { 0xac00, 0xd7a3 },
		{ 0xf900, 0xfaff }, { 0xfe10, 0xfe1f }, { 0xfe30, 0xfe6b },
		{ 0xff01, 0xff60 }, { 0xffe0, 0xffe6 },
		{ 0x16fe0, 0x16fe4 }, { 0x16ff0, 0x16ff1 }, { 0x17000, 0x187f7 },
		{ 0x18800, 0x18cd5 }, { 0x18d00, 0x18d08 }, { 0x1aff0, 0x1affc },
		{ 0x1b000, 0x1b122 }, { 0x1b150, 0x1b152 }, { 0x1b164, 0x1b167 }, { 0x1b170, 0x1b2fb }, { 0x1d538, 0x1d550 },
		{ 0x1f004, 0x1f004 }, { 0x1f0cf, 0x1f0cf }, /*{ 0x1f100, 0x1f10a },*/
		//{ 0x1f110, 0x1f12d }, { 0x1f130, 0x1f169 }, { 0x1f170, 0x1f1ac },
		{ 0x1f18f, 0x1f199 },
		{ 0x1f1e6, 0x1f1ff }, { 0x1f200, 0x1f202 }, { 0x1f210, 0x1f23b },
		{ 0x1f240, 0x1f248 }, { 0x1f250, 0x1f251 }, { 0x1f260, 0x1f265 },
		{ 0x1f300, 0x1f320 }, { 0x1f32d, 0x1f33e }, { 0x1f340, 0x1f37e },
		{ 0x1f380, 0x1f393 }, { 0x1f3a0, 0x1f3ca }, { 0x1f3cf, 0x1f3d3 },
		{ 0x1f3e0, 0x1f3f0 }, { 0x1f3f4, 0x1f3f4 }, { 0x1f3f8, 0x1f43e },
		{ 0x1f440, 0x1f44e }, { 0x1f450, 0x1f4fc }, { 0x1f4ff, 0x1f53d },
		{ 0x1f54b, 0x1f54e }, { 0x1f550, 0x1f567 }, { 0x1f57a, 0x1f57a },
		{ 0x1f595, 0x1f596 }, { 0x1f5a4, 0x1f5a4 }, { 0x1f5fb, 0x1f606 },
		{ 0x1f607, 0x1f64f }, { 0x1f680, 0x1f6c5 }, { 0x1f6cc, 0x1f6cc },
		{ 0x1f6d0, 0x1f6d2 }, { 0x1f6d5, 0x1f6d7 }, { 0x1f6dd, 0x1f6df }, { 0x1f6eb, 0x1f6ec },
		{ 0x1f6f4, 0x1f6fc }, { 0x1f7e0, 0x1f7eb }, { 0x1f7f0, 0x1f7f0 }, { 0x1f90c, 0x1f93a },
		{ 0x1f93c, 0x1f945 }, { 0x1f947, 0x1f97f }, { 0x1f980, 0x1f9cc },
		{ 0x1f9cd, 0x1f9ff }, { 0x1fa70, 0x1fa74 }, { 0x1fa78, 0x1fa7c }, { 0x1fa80, 0x1fa86 },
		{ 0x1fa90, 0x1faac }, { 0x1fab0, 0x1faba }, { 0x1fac0, 0x1fac5 },
		{ 0x1fad0, 0x1fad9 }, { 0x1fae0, 0x1fae7 }, { 0x1faf0, 0x1faf6 }, { 0x20000, 0x2fffd }, { 0x30000, 0x3fffd },
		//{ 0xe0100, 0xe01ef }, { 0xf0000, 0xffffd }, { 0x100000, 0x10fffd }
	};

	static int BiSearch (int rune, int [,] table, int max)
	{
		var min = 0;

		if (rune < table [0, 0] || rune > table [max, 1]) {
			return 0;
		}
		while (max >= min) {
			var mid = (min + max) / 2;
			if (rune > table [mid, 1]) {
				min = mid + 1;
			} else if (rune < table [mid, 0]) {
				max = mid - 1;
			} else {
				return 1;
			}
		}

		return 0;
	}
}