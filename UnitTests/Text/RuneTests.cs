using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;

namespace Terminal.Gui.TextTests;

public class RuneTests {

	// IsCombiningMark tests
	[Theory]
	[InlineData (0x0338, true)] // Combining Long Solidus Overlay (U+0338) (e.g. ≠)
	[InlineData (0x0300, true)] // Combining Grave Accent
	[InlineData (0x0301, true)] // Combining acute accent (é)
	[InlineData (0x0302, true)] // Combining Circumflex Accent
	[InlineData (0x0328, true)] //  Combining ogonek (a small hook or comma shape) U+0328
	[InlineData (0x00E9, false)] // Latin Small Letter E with Acute, Unicode U+00E9 é 
	[InlineData (0x0061, false)] // Latin Small Letter A is U+0061.
	public void TestIsCombiningMark (int codepoint, bool expected)
	{
		var rune = new Rune (codepoint);
		Assert.Equal (expected, rune.IsCombiningMark ());
	}

	[Theory]
	[InlineData (0x0000001F, 0x241F)]
	[InlineData (0x0000007F, 0x247F)]
	[InlineData (0x0000009F, 0x249F)]
	[InlineData (0x0001001A, 0x1001A)]
	public void MakePrintable_Converts_Control_Chars_To_Proper_Unicode (int code, int expected)
	{
		var actual = ((Rune)code).MakePrintable ();
		Assert.Equal (expected, actual.Value);
	}

	[Theory]
	[InlineData (0x20)]
	[InlineData (0x7E)]
	[InlineData (0xA0)]
	[InlineData (0x010020)]
	public void MakePrintable_Does_Not_Convert_Ansi_Chars_To_Unicode (int code)
	{
		var actual = ((Rune)code).MakePrintable ();
		Assert.Equal (code, actual.Value);
	}

	[Theory]
	[InlineData (0x0338)] // Combining Long Solidus Overlay (U+0338) (e.g. ≠)
	[InlineData (0x0300)] // Combining Grave Accent
	[InlineData (0x0301)] // Combining acute accent (é)
	[InlineData (0x0302)] // Combining Circumflex Accent
	[InlineData (0x0061)] // Combining ogonek (a small hook or comma shape)
	public void MakePrintable_Combining_Character_Is_Not_Printable (int code)
	{
		var rune = new Rune (code);
		var actual = rune.MakePrintable ();
		Assert.Equal (code, actual.Value);
	}

	[Theory]
	[InlineData ('a', "a", 1, 1, 1)]
	[InlineData ('b', "b", 1, 1, 1)]
	[InlineData (123, "{", 1, 1, 1)]        // { Left Curly Bracket
	[InlineData ('\u1150', "ᅐ", 2, 1, 3)]   // ᅐ Hangul Choseong Ceongchieumcieuc
	[InlineData ('\u1161', "ᅡ", 0, 1, 3)]   // ᅡ Hangul Jungseong A - Unicode Hangul Jamo for join with column width equal to 0 alone.
	[InlineData (31, "\u001f", -1, 1, 1)]   // non printable character - Information Separator One
	[InlineData (127, "\u007f", -1, 1, 1)]  // non printable character - Delete
	[InlineData ('\u20D0', "⃐", 0, 1, 3)]   // ◌⃐ Combining Left Harpoon Above
	[InlineData ('\u25a0', "■", 1, 1, 3)]   // ■ Black Square
	[InlineData ('\u25a1', "□", 1, 1, 3)]   // □ White Square
	[InlineData ('\uf61e', "", 1, 1, 3)]   // Private Use Area
	[InlineData ('\u2103', "℃", 1, 1, 3)]   // ℃ Degree Celsius
	[InlineData ('\u1100', "ᄀ", 2, 1, 3)]   // ᄀ Hangul Choseong Kiyeok
	[InlineData ('\u2501', "━", 1, 1, 3)]   // ━ Box Drawings Heavy Horizontal
	[InlineData ('\u2615', "☕", 2, 1, 3)] // ☕ Hot Beverage
	[InlineData ('\u231a', "⌚", 2, 1, 3)] // ⌚ Watch
	[InlineData ('\u231b', "⌛", 2, 1, 3)] // ⌛ Hourglass
	[InlineData ('\u231c', "⌜", 1, 1, 3)] // ⌜ Top Left Corner
	[InlineData ('\u1dc0', "᷀", 0, 1, 3)] // ◌᷀ Combining Dotted Grave Accent
	public void GetColumns_With_Single_Code (int code, string str, int runeLength, int stringLength, int utf8Length)
	{
		var rune = new Rune (code);
		Assert.Equal (str, rune.ToString ());
		Assert.Equal (runeLength, rune.GetColumns ());
		Assert.Equal (stringLength, rune.ToString ().Length);
		Assert.Equal (utf8Length, rune.Utf8SequenceLength);
		Assert.True (Rune.IsValid (rune.Value));
	}

	[Theory]
	[InlineData (new byte [] { 0xf0, 0x9f, 0xa8, 0x81 }, "🨁", 1, 2)] // Neutral Chess Queen
	[InlineData (new byte [] { 0xf3, 0xa0, 0xbf, 0xa1 }, "󠿡", 1, 2)] // Undefined Character
	[InlineData (new byte [] { 0xf0, 0x9f, 0x8d, 0x95 }, "🍕", 2, 2)] // 🍕 Slice of Pizza
	[InlineData (new byte [] { 0xf0, 0x9f, 0xa4, 0x96 }, "🤖", 2, 2)] // 🤖 Robot Face
	[InlineData (new byte [] { 0xf0, 0x90, 0x90, 0xa1 }, "𐐡", 1, 2)] // 𐐡 Deseret Capital Letter Er
	[InlineData (new byte [] { 0xf0, 0x9f, 0x8c, 0xb9 }, "🌹", 2, 2)] // 🌹 Rose
	public void GetColumns_Utf8_Encode (byte [] code, string str, int runeLength, int stringLength)
	{
		var operationStatus = Rune.DecodeFromUtf8 (code, out Rune rune, out int bytesConsumed);
		Assert.Equal (OperationStatus.Done, operationStatus);
		Assert.Equal (str, rune.ToString ());
		Assert.Equal (runeLength, rune.GetColumns ());
		Assert.Equal (stringLength, rune.ToString ().Length);
		Assert.Equal (bytesConsumed, rune.Utf8SequenceLength);
		Assert.True (Rune.IsValid (rune.Value));
	}

	[Theory]
	[InlineData (new char [] { '\ud83e', '\ude01' }, "🨁", 1, 2, 4)]  // Neutral Chess Queen
	[InlineData (new char [] { '\udb43', '\udfe1' }, "󠿡", 1, 2, 4)]  // Undefined Character
	[InlineData (new char [] { '\ud83c', '\udf55' }, "🍕", 2, 2, 4)] // 🍕 Slice of Pizza
	[InlineData (new char [] { '\ud83e', '\udd16' }, "🤖", 2, 2, 4)] // 🤖 Robot Face
	[InlineData (new char [] { '\ud83e', '\udde0' }, "🧠", 2, 2, 4)] // 🧠 Brain
	[InlineData (new char [] { '\ud801', '\udc21' }, "𐐡", 1, 2, 4)]  // 𐐡 Deseret Capital Letter Er
	[InlineData (new char [] { '\ud83c', '\udf39' }, "🌹", 2, 2, 4)] // 🌹 Rose
	public void GetColumns_Utf16_Encode (char [] code, string str, int runeLength, int stringLength, int utf8Length)
	{
		var rune = new Rune (code [0], code [1]);
		Assert.Equal (str, rune.ToString ());
		Assert.Equal (runeLength, rune.GetColumns ());
		Assert.Equal (stringLength, rune.ToString ().Length);
		Assert.Equal (utf8Length, rune.Utf8SequenceLength);
		Assert.True (Rune.IsValid (rune.Value));
	}

	[Theory]
	[InlineData ("\U0001fa01", "🨁", 1, 2)] // Neutral Chess Queen
	[InlineData ("\U000e0fe1", "󠿡", 1, 2)] // Undefined Character
	[InlineData ("\U0001F355", "🍕", 2, 2)] // 🍕 Slice of Pizza
	[InlineData ("\U0001F916", "🤖", 2, 2)] // 🤖 Robot Face
	[InlineData ("\U0001f9e0", "🧠", 2, 2)] // 🧠 Brain
	[InlineData ("\U00010421", "𐐡", 1, 2)] // 𐐡 Deseret Capital Letter Er
	[InlineData ("\U0001f339", "🌹", 2, 2)] // 🌹 Rose
	public void GetColumns_Utf32_Encode (string code, string str, int runeLength, int stringLength)
	{
		var operationStatus = Rune.DecodeFromUtf16 (code, out Rune rune, out int charsConsumed);
		Assert.Equal (OperationStatus.Done, operationStatus);
		Assert.Equal (str, rune.ToString ());
		Assert.Equal (runeLength, rune.GetColumns ());
		Assert.Equal (stringLength, rune.ToString ().Length);
		Assert.Equal (charsConsumed, rune.Utf16SequenceLength);
		Assert.True (Rune.IsValid (rune.Value));

		// with DecodeRune
		(var nrune, var size) = code.DecodeRune ();
		Assert.Equal (str, nrune.ToString ());
		Assert.Equal (runeLength, nrune.GetColumns ());
		Assert.Equal (stringLength, nrune.ToString ().Length);
		Assert.Equal (size, nrune.Utf8SequenceLength);
		for (int x = 0; x < code.Length - 1; x++) {
			Assert.Equal (nrune.Value, char.ConvertToUtf32 (code [x], code [x + 1]));
			Assert.True (RuneExtensions.EncodeSurrogatePair (code [x], code [x + 1], out Rune result));
			Assert.Equal (rune, result);
		}
		Assert.True (Rune.IsValid (nrune.Value));
	}

	[Theory]
	[InlineData ("\u2615\ufe0f", "☕️", 2, 2, 2)] // \ufe0f forces it to be rendered as a colorful image as compared to a monochrome text variant.
	[InlineData ("\u1107\u1165\u11b8", "법", 3, 2, 1)] // the letters 법 join to form the Korean word for "rice:" U+BC95 법 (read from top left to bottom right)
	[InlineData ("\U0001F468\u200D\U0001F469\u200D\U0001F467", "👨‍👩‍👧", 8, 6, 8)] // Man, Woman and Girl emoji.
	[InlineData ("\u0915\u093f", "कि", 2, 2, 2)] // Hindi कि with DEVANAGARI LETTER KA and DEVANAGARI VOWEL SIGN I
	[InlineData ("\u0e4d\u0e32", "ํา", 2, 1, 2)] // Decomposition: ํ (U+0E4D) - า (U+0E32) = U+0E33 ำ Thai Character Sara Am
	[InlineData ("\u0e33", "ำ", 1, 1, 1)] // Decomposition: ํ (U+0E4D) - า (U+0E32) = U+0E33 ำ Thai Character Sara Am
	public void GetColumns_String_Without_SurrogatePair (string code, string str, int codeLength, int runesLength, int stringLength)
	{
		Assert.Equal (str, code.Normalize ());
		Assert.Equal (codeLength, code.Length);
		Assert.Equal (runesLength, code.EnumerateRunes ().Sum (x => x.GetColumns ()));
		Assert.Equal (runesLength, str.GetColumns ());
		Assert.Equal (stringLength, str.Length);
	}

	[Theory]
	[InlineData (new char [] { '\ud799', '\udc21' })]
	public void Rune_Exceptions_Utf16_Encode (char [] code)
	{
		Assert.False (RuneExtensions.EncodeSurrogatePair (code [0], code [1], out Rune rune));
		Assert.Throws<ArgumentOutOfRangeException> (() => new Rune (code [0], code [1]));
	}

	[Theory]
	[InlineData (0x12345678)]
	[InlineData ('\ud801')]
	public void Rune_Exceptions_Integers (int code)
	{
		Assert.Throws<ArgumentOutOfRangeException> (() => new Rune (code));
	}

	[Fact]
	public void GetColumns_GetRuneCount ()
	{
		PrintTextElementCount ('\u00e1'.ToString (), "á", 1, 1, 1, 1);
		PrintTextElementCount ("\u0061\u0301", "á", 1, 2, 2, 1);
		PrintTextElementCount ("\u0061\u0301", "á", 1, 2, 2, 1);
		PrintTextElementCount ("\u0065\u0301", "é", 1, 2, 2, 1);
		PrintTextElementCount ("\U0001f469\U0001f3fd\u200d\U0001f692", "👩🏽‍🚒", 6, 4, 7, 1);
		PrintTextElementCount ("\ud801\udccf", "𐓏", 1, 1, 2, 1);
	}

	private void PrintTextElementCount (string us, string s, int consoleWidth, int runeCount, int stringCount, int txtElementCount)
	{
		Assert.Equal (us.Length, s.Length);
		Assert.Equal (us, s);
		Assert.Equal (consoleWidth, us.GetColumns ());
		Assert.Equal (runeCount, us.GetRuneCount ());
		Assert.Equal (stringCount, s.Length);

		TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator (s);

		int textElementCount = 0;
		while (enumerator.MoveNext ()) {
			textElementCount++; // For versions prior to Net5.0 the StringInfo class might handle some grapheme clusters incorrectly.
		}

		Assert.Equal (txtElementCount, textElementCount);
	}

	[Fact]
	public void TestRuneIsLetter ()
	{
		Assert.Equal (5, CountLettersInString ("Hello"));
		Assert.Equal (8, CountLettersInString ("𐓏𐓘𐓻𐓘𐓻𐓟 𐒻𐓟"));
	}

	private int CountLettersInString (string s)
	{
		int letterCount = 0;
		foreach (Rune rune in s.EnumerateRunes ()) {
			if (Rune.IsLetter (rune)) { letterCount++; }
		}

		return letterCount;
	}

	[Fact]
	public void Test_SurrogatePair_From_String ()
	{
		Assert.True (ProcessTestStringUseChar ("𐓏𐓘𐓻𐓘𐓻𐓟 𐒻𐓟"));
		Assert.Throws<Exception> (() => ProcessTestStringUseChar ("\ud801"));

		Assert.True (ProcessStringUseRune ("𐓏𐓘𐓻𐓘𐓻𐓟 𐒻𐓟"));
		Assert.Throws<Exception> (() => ProcessStringUseRune ("\ud801"));
	}

	private bool ProcessTestStringUseChar (string s)
	{
		char surrogateChar = default;
		for (int i = 0; i < s.Length; i++) {
			Rune r;
			if (char.IsSurrogate (s [i])) {
				if (surrogateChar != default && char.IsSurrogate (surrogateChar)) {
					r = new Rune (surrogateChar, s [i]);
					Assert.True (r.IsSurrogatePair ());
					int codePoint = char.ConvertToUtf32 (surrogateChar, s [i]);
					RuneExtensions.EncodeSurrogatePair (surrogateChar, s [i], out Rune rune);
					Assert.Equal (codePoint, rune.Value);
					string sp = new string (new char [] { surrogateChar, s [i] });
					r = (Rune)codePoint;
					Assert.Equal (sp, r.ToString ());
					Assert.True (r.IsSurrogatePair ());

					surrogateChar = default;
				} else if (i < s.Length - 1) {
					surrogateChar = s [i];
					continue;
				} else {
					Assert.Throws<ArgumentOutOfRangeException> (() => new Rune (s [i]));
					throw new Exception ("String was not well-formed UTF-16.");
				}
			} else {
				r = new Rune (s [i]);
				var buff = new byte [4];
				((Rune)s [i]).Encode (buff);
				Assert.Equal ((int)s [i], buff [0]);
				Assert.Equal (s [i], r.Value);
				Assert.True (Rune.IsValid (r.Value));
				Assert.False (r.IsSurrogatePair ());
			}
		}
		return true;
	}

	private bool ProcessStringUseRune (string s)
	{
		var us = s;
		string rs = "";
		Rune codePoint;
		List<Rune> runes = new List<Rune> ();
		int colWidth = 0;

		for (int i = 0; i < s.Length; i++) {
			Rune rune = default;
			if (Rune.IsValid (s [i])) {
				rune = new Rune (s [i]);
				Assert.True (Rune.IsValid (rune.Value));
				runes.Add (rune);
				Assert.Equal (s [i], rune.Value);
				Assert.False (rune.IsSurrogatePair ());
			} else if (i + 1 < s.Length && (RuneExtensions.EncodeSurrogatePair (s [i], s [i + 1], out codePoint))) {
				Assert.Equal (0, rune.Value);
				Assert.False (Rune.IsValid (s [i]));
				rune = codePoint;
				runes.Add (rune);
				string sp = new string (new char [] { s [i], s [i + 1] });
				Assert.Equal (sp, codePoint.ToString ());
				Assert.True (codePoint.IsSurrogatePair ());
				i++; // Increment the iterator by the number of surrogate pair
			} else {
				Assert.Throws<ArgumentOutOfRangeException> (() => new Rune (s [i]));
				throw new Exception ("String was not well-formed UTF-16.");
			}
			colWidth += rune.GetColumns (); // Increment the column width of this Rune
			rs += rune.ToString ();
		}
		Assert.Equal (us.GetColumns (), colWidth);
		Assert.Equal (s, rs);
		Assert.Equal (s, StringExtensions.ToString (runes));
		return true;
	}

	[Fact]
	public void TestSplit ()
	{
		string inputString = "🐂, 🐄, 🐆";
		string [] splitOnSpace = inputString.Split (' ');
		string [] splitOnComma = inputString.Split (',');
		Assert.Equal (3, splitOnSpace.Length);
		Assert.Equal (3, splitOnComma.Length);
	}

	[Theory]
	[InlineData (true, '\u1100')]
	[InlineData (true, '\ud83c', '\udf39')]
	[InlineData (true, '\udbff', '\udfff')]
	[InlineData (false, '\ud801')]
	[InlineData (false, '\ud83e')]
	public void Rune_IsValid (bool valid, params char [] chars)
	{
		Rune rune = default;
		bool isValid = true;
		if (chars.Length == 1) {
			try {
				rune = new Rune (chars [0]);
			} catch (Exception) {

				isValid = false;
			}
		} else {
			rune = new Rune (chars [0], chars [1]);
		}
		if (valid) {
			Assert.NotEqual (default, rune);
			Assert.True (Rune.IsValid (rune.Value));
			Assert.True (valid);
		} else {
			Assert.False (valid);
			Assert.False (isValid);
		}
	}

	[Theory]
	[InlineData ('\u006f', '\u0302', "\u006f\u0302", 1, 0, 2, "o", "̂", "ô", 1, 2)]
	[InlineData ('\u0065', '\u0301', "\u0065\u0301", 1, 0, 2, "e", "́", "é", 1, 2)]
	public void Test_NonSpacingChar (int code1, int code2, string code, int rune1Length, int rune2Length, int codeLength, string code1String, string code2String, string joinString, int joinLength, int bytesLength)
	{
		var rune = new Rune (code1);
		var nsRune = new Rune (code2);
		Assert.Equal (rune1Length, rune.GetColumns ());
		Assert.Equal (rune2Length, nsRune.GetColumns ());
		var ul = rune.ToString ();
		Assert.Equal (code1String, ul);
		var uns = nsRune.ToString ();
		Assert.Equal (code2String, uns);
		var f = $"{rune}{nsRune}".Normalize ();
		Assert.Equal (f, joinString);
		Assert.Equal (f, code.Normalize ());
		Assert.Equal (joinLength, f.GetColumns ());
		Assert.Equal (joinLength, code.EnumerateRunes ().Sum (c => c.GetColumns ()));
		Assert.Equal (codeLength, code.Length);
		(var nrune, var size) = f.DecodeRune ();
		Assert.Equal (f.ToRunes () [0], nrune);
		Assert.Equal (bytesLength, size);
	}

	[Theory]
	[InlineData (500000000)]
	[InlineData (0xf801, 0xdfff)]
	public void Test_MaxRune (params int [] codes)
	{
		if (codes.Length == 1) {
			Assert.Throws<ArgumentOutOfRangeException> (() => new Rune (codes [0]));
		} else {
			Assert.Throws<ArgumentOutOfRangeException> (() => new Rune ((char)codes [0], (char)codes [1]));
		}
	}

	[Fact]
	public void Sum_Of_Rune_GetColumns_Is_Not_Always_Equal_To_String_GetColumns ()
	{
		const int start = 0x000000;
		const int end = 0x10ffff;

		for (int i = start; i <= end; i++) {
			if (char.IsSurrogate ((char)i)) {
				continue;
			}
			Rune r = new Rune ((uint)i);
			string us = r.ToString ();
			string hex = i.ToString ("x6");
			int v = int.Parse (hex, System.Globalization.NumberStyles.HexNumber);
			string s = char.ConvertFromUtf32 (v);

			if (!r.IsSurrogatePair ()) {
				Assert.Equal (r.ToString (), us);
				Assert.Equal (us, s);
				if (r.GetColumns () < 0) {
					Assert.NotEqual (r.GetColumns (), us.GetColumns ());
					Assert.NotEqual (s.EnumerateRunes ().Sum (c => c.GetColumns ()), us.GetColumns ());
				} else {
					Assert.Equal (r.GetColumns (), us.GetColumns ());
					Assert.Equal (s.EnumerateRunes ().Sum (c => c.GetColumns ()), us.GetColumns ());
				}
				Assert.Equal (us.GetRuneCount (), s.Length);
			} else {
				Assert.Equal (r.ToString (), us);
				Assert.Equal (us, s);
				Assert.Equal (r.GetColumns (), us.GetColumns ());
				Assert.Equal (s.GetColumns (), us.GetColumns ());
				Assert.Equal (1, us.GetRuneCount ()); // Here returns 1 because is a valid surrogate pair resulting in only rune >=U+10000..U+10FFFF
				Assert.Equal (2, s.Length); // String always preserves the originals values of each surrogate pair
			}
		}
	}

	[Theory]
	[InlineData (0x20D0, 0x20EF)]
	[InlineData (0x2310, 0x231F)]
	[InlineData (0x1D800, 0x1D80F)]
	public void Test_Range (int start, int end)
	{
		for (int i = start; i <= end; i++) {
			Rune r = new Rune ((uint)i);
			string us = r.ToString ();
			string hex = i.ToString ("x6");
			int v = int.Parse (hex, System.Globalization.NumberStyles.HexNumber);
			string s = char.ConvertFromUtf32 (v);

			if (!r.IsSurrogatePair ()) {
				Assert.Equal (r.ToString (), us);
				Assert.Equal (us, s);
				Assert.Equal (r.GetColumns (), us.GetColumns ());
				Assert.Equal (us.GetRuneCount (), s.Length); // For not surrogate pairs string.RuneCount is always equal to String.Length
			} else {
				Assert.Equal (r.ToString (), us);
				Assert.Equal (us, s);
				Assert.Equal (r.GetColumns (), us.GetColumns ());
				Assert.Equal (1, us.GetRuneCount ()); // Here returns 1 because is a valid surrogate pair resulting in only rune >=U+10000..U+10FFFF
				Assert.Equal (2, s.Length); // String always preserves the originals values of each surrogate pair
			}
			Assert.Equal (s.GetColumns (), us.GetColumns ());
		}
	}

	[Theory]
	[InlineData ('\ue0fd', false)]
	[InlineData ('\ud800', true)]
	[InlineData ('\udfff', true)]
	public void Test_IsSurrogate (char code, bool isSurrogate)
	{
		if (isSurrogate) {
			Assert.True (char.IsSurrogate (code.ToString (), 0));
		} else {
			Assert.False (char.IsSurrogate (code.ToString (), 0));
		}
	}

	[Theory]
	[InlineData (unchecked((char)0x40D7C0), (char)0xDC20, 0, "\0", false)]
	[InlineData ((char)0x0065, (char)0x0301, 0, "\0", false)]
	[InlineData ('\ud83c', '\udf56', 0x1F356, "🍖", true)] // 🍖 Meat On Bone
	public void Test_EncodeSurrogatePair (char highSurrogate, char lowSurrogate, int runeValue, string runeString, bool isSurrogatePair)
	{
		Rune rune;
		if (isSurrogatePair) {
			Assert.True (RuneExtensions.EncodeSurrogatePair ('\ud83c', '\udf56', out rune));
		} else {
			Assert.False (RuneExtensions.EncodeSurrogatePair (highSurrogate, lowSurrogate, out rune));
		}
		Assert.Equal (runeValue, rune.Value);
		Assert.Equal (runeString, rune.ToString ());
	}

	[Theory]
	[InlineData ('\uea85', null, "", false)] // Private Use Area
	[InlineData (0x1F356, new char [] { '\ud83c', '\udf56' }, "🍖", true)] // 🍖 Meat On Bone
	public void Test_DecodeSurrogatePair (int code, char [] charsValue, string runeString, bool isSurrogatePair)
	{
		Rune rune = new Rune (code);
		char [] chars;
		if (isSurrogatePair) {
			Assert.True (rune.DecodeSurrogatePair (out chars));
			Assert.Equal (2, chars.Length);
			Assert.Equal (charsValue [0], chars [0]);
			Assert.Equal (charsValue [1], chars [1]);
			Assert.Equal (runeString, new Rune (chars [0], chars [1]).ToString ());
		} else {
			Assert.False (rune.DecodeSurrogatePair (out chars));
			Assert.Null (chars);
			Assert.Equal (runeString, rune.ToString ());
		}
		Assert.Equal (chars, charsValue);
	}

	[Fact]
	public void Test_All_Surrogate_Pairs_Range ()
	{
		for (uint h = 0xd800; h <= 0xdbff; h++) {
			for (uint l = 0xdc00; l <= 0xdfff; l++) {
				Rune r = new Rune ((char)h, (char)l);
				string us = r.ToString ();
				string hex = r.Value.ToString ("x6");
				int v = int.Parse (hex, System.Globalization.NumberStyles.HexNumber);
				string s = char.ConvertFromUtf32 (v);

				Assert.True (v >= 0x10000 && v <= RuneExtensions.MaxUnicodeCodePoint);
				Assert.Equal (r.ToString (), us);
				Assert.Equal (us, s);
				Assert.Equal (r.GetColumns (), us.GetColumns ());
				Assert.Equal (s.GetColumns (), us.GetColumns ());
				Assert.Equal (1, us.GetRuneCount ()); // Here returns 1 because is a valid surrogate pair resulting in only rune >=U+10000..U+10FFFF
				Assert.Equal (2, s.Length); // String always preserves the originals values of each surrogate pair
			}
		}
	}

	[Theory]
	[InlineData ("Hello, 世界", 13, 11, 9)]   // Without Surrogate Pairs
	[InlineData ("Hello, 𝔹𝕆𝔹", 19, 13, 13)] // With Surrogate Pairs
	public void Test_DecodeRune_Extension (string text, int bytesLength, int colsLength, int textLength)
	{
		List<Rune> runes = new List<Rune> ();
		int tSize = 0;
		for (int i = 0; i < text.GetRuneCount (); i++) {
			(Rune rune, int size) = text.DecodeRune (i);
			runes.Add (rune);
			tSize += size;
		}
		string result = StringExtensions.ToString (runes);
		Assert.Equal (text, result);
		Assert.Equal (bytesLength, tSize);
		Assert.Equal (colsLength, result.GetColumns ());
		Assert.Equal (textLength, result.Length);
	}

	[Theory]
	[InlineData ("Hello, 世界", 13, 11, 9, "界世 ,olleH")]   // Without Surrogate Pairs
	[InlineData ("Hello, 𝔹𝕆𝔹", 19, 13, 13, "𝔹𝕆𝔹 ,olleH")] // With Surrogate Pairs
	public void Test_DecodeLastRune_Extension (string text, int bytesLength, int colsLength, int textLength, string encoded)
	{
		List<Rune> runes = new List<Rune> ();
		int tSize = 0;
		for (int i = text.GetRuneCount () - 1; i >= 0; i--) {
			(Rune rune, int size) = text.DecodeLastRune (i);
			runes.Add (rune);
			tSize += size;
		}
		string result = StringExtensions.ToString (runes);
		Assert.Equal (encoded, result);
		Assert.Equal (bytesLength, tSize);
		Assert.Equal (colsLength, result.GetColumns ());
		Assert.Equal (textLength, result.Length);
	}

	[Theory]
	[InlineData ("���", false)]
	[InlineData ("Hello, 世界", true)]
	[InlineData (new byte [] { 0xff, 0xfe, 0xfd }, false)]
	[InlineData (new byte [] { 0xf0, 0x9f, 0x8d, 0x95 }, true)]
	public void Test_CanBeEncodedAsRune_Extension (object text, bool canBeEncodedAsRune)
	{
		string str;
		if (text is string) {
			str = (string)text;
			if (canBeEncodedAsRune) {
				Assert.True (RuneExtensions.CanBeEncodedAsRune (Encoding.Unicode.GetBytes (str.ToCharArray ())));
			} else {
				Assert.False (RuneExtensions.CanBeEncodedAsRune (Encoding.Unicode.GetBytes (str.ToCharArray ())));
			}
		} else if (text is byte []) {
			str = StringExtensions.ToString ((byte [])text);
			if (canBeEncodedAsRune) {
				Assert.True (RuneExtensions.CanBeEncodedAsRune (Encoding.Unicode.GetBytes (str.ToCharArray ())));
			} else {
				Assert.False (RuneExtensions.CanBeEncodedAsRune (Encoding.Unicode.GetBytes (str.ToCharArray ())));
			}
		}
	}

	[Fact]
	public void Equals_ToRuneList ()
	{
		var a = new List<List<Rune>> () { "First line.".ToRuneList () };
		var b = new List<List<Rune>> () { "First line.".ToRuneList (), "Second line.".ToRuneList () };
		var c = new List<Rune> (a [0]);
		var d = a [0];

		Assert.Equal (a [0], b [0]);
		// Not the same reference
		Assert.False (a [0] == b [0]);
		Assert.NotEqual (a [0], b [1]);
		Assert.False (a [0] == b [1]);

		Assert.Equal (c, a [0]);
		Assert.False (c == a [0]);
		Assert.Equal (c, b [0]);
		Assert.False (c == b [0]);
		Assert.NotEqual (c, b [1]);
		Assert.False (c == b [1]);

		Assert.Equal (d, a [0]);
		// Is the same reference
		Assert.True (d == a [0]);
		Assert.Equal (d, b [0]);
		Assert.False (d == b [0]);
		Assert.NotEqual (d, b [1]);
		Assert.False (d == b [1]);

		Assert.True (a [0].SequenceEqual (b [0]));
		Assert.False (a [0].SequenceEqual (b [1]));

		Assert.True (c.SequenceEqual (a [0]));
		Assert.True (c.SequenceEqual (b [0]));
		Assert.False (c.SequenceEqual (b [1]));

		Assert.True (d.SequenceEqual (a [0]));
		Assert.True (d.SequenceEqual (b [0]));
		Assert.False (d.SequenceEqual (b [1]));
	}

	[Fact]
	public void Rune_GetColumns_Versus_String_GetColumns_With_Non_Printable_Characters ()
	{
		int sumRuneWidth = 0;
		int sumConsoleWidth = 0;
		for (uint i = 0; i < 32; i++) {
			sumRuneWidth += ((Rune)i).GetColumns ();
			sumConsoleWidth += ((Rune)i).ToString ().GetColumns ();
		}

		Assert.Equal (-32, sumRuneWidth);
		Assert.Equal (0, sumConsoleWidth);
	}

	[Theory]
	[InlineData ("01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789", 200, 200, 200)]
	[InlineData ("01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789\n", 201, 200, 199)]  // has a '\n' newline
	[InlineData ("\t01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789\n", 202, 200, 198)] // has a '\t' and a '\n' newline
	public void Rune_ColumnWidth_Versus_String_ConsoleWidth (string text, int stringLength, int strCols, int runeCols)
	{
		Assert.Equal (stringLength, text.Length);
		Assert.Equal (stringLength, text.GetRuneCount ());
		Assert.Equal (strCols, text.GetColumns ());
		int sumRuneWidth = text.EnumerateRunes ().Sum (x => x.GetColumns ());
		Assert.Equal (runeCols, sumRuneWidth);
	}

	[Theory]
	[InlineData ('\ud800', true)]
	[InlineData ('\udbff', true)]
	[InlineData ('\udc00', false)]
	[InlineData ('\udfff', false)]
	[InlineData ('\uefff', null)]
	public void Rune_IsHighSurrogate_IsLowSurrogate (char code, bool? isHighSurrogate)
	{
		if (isHighSurrogate == true) {
			Assert.True (char.IsHighSurrogate (code));
		} else if (isHighSurrogate == false) {
			Assert.True (char.IsLowSurrogate (code));
		} else {
			Assert.False (char.IsHighSurrogate (code));
			Assert.False (char.IsLowSurrogate (code));
		}
	}

	[Theory]
	[InlineData ("First line.")]
	[InlineData ("Hello, 𝔹𝕆𝔹")]
	public void Rune_ToRunes (string text)
	{
		var runes = text.ToRunes ();
		for (int i = 0; i < runes.Length; i++) {
			Assert.Equal (text.EnumerateRunes ().ToArray () [i].Value, runes [i].Value);
		}
	}

	[Theory]
	[InlineData ('a', 1, 1)]
	[InlineData (31, 1, 1)]
	[InlineData (123, 1, 1)]
	[InlineData (127, 1, 1)]
	[InlineData ('\u1150', 1, 3)]
	[InlineData ('\u1161', 1, 3)]
	[InlineData (0x16fe0, 2, 4)]
	public void System_Text_Rune_SequenceLength (int code, int utf16Length, int utf8Length)
	{
		var r = new System.Text.Rune (code);
		Assert.Equal (utf16Length, r.Utf16SequenceLength);
		Assert.Equal (utf8Length, r.Utf8SequenceLength);
	}

	[Fact]
	public void Cast_To_Char_Durrogate_Pair_Return_UTF16 ()
	{
		Assert.NotEqual ("𝔹", $"{new Rune (unchecked((char)0x1d539))}");
		Assert.Equal ("픹", $"{new Rune (unchecked((char)0x1d539))}");
		Assert.Equal ("픹", $"{new Rune (0xd539)}");
		Assert.Equal ("𝔹", $"{new Rune (0x1d539)}");
	}
}
