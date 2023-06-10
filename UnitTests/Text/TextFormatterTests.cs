using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.TextTests {
	public class TextFormatterTests {
		readonly ITestOutputHelper output;

		public TextFormatterTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Basic_Usage ()
		{
			var testText = "test";
			var expectedSize = new Size ();
			var testBounds = new Rect (0, 0, 100, 1);
			var tf = new TextFormatter ();

			tf.Text = testText;
			expectedSize = new Size (testText.Length, 1);
			Assert.Equal (testText, tf.Text);
			Assert.Equal (TextAlignment.Left, tf.Alignment);
			Assert.Equal (expectedSize, tf.Size);
			tf.Draw (testBounds, new Attribute (), new Attribute ());
			Assert.Equal (expectedSize, tf.Size);
			Assert.NotEmpty (tf.Lines);

			tf.Alignment = TextAlignment.Right;
			expectedSize = new Size (testText.Length, 1);
			Assert.Equal (testText, tf.Text);
			Assert.Equal (TextAlignment.Right, tf.Alignment);
			Assert.Equal (expectedSize, tf.Size);
			tf.Draw (testBounds, new Attribute (), new Attribute ());
			Assert.Equal (expectedSize, tf.Size);
			Assert.NotEmpty (tf.Lines);

			tf.Alignment = TextAlignment.Right;
			expectedSize = new Size (testText.Length * 2, 1);
			tf.Size = expectedSize;
			Assert.Equal (testText, tf.Text);
			Assert.Equal (TextAlignment.Right, tf.Alignment);
			Assert.Equal (expectedSize, tf.Size);
			tf.Draw (testBounds, new Attribute (), new Attribute ());
			Assert.Equal (expectedSize, tf.Size);
			Assert.NotEmpty (tf.Lines);

			tf.Alignment = TextAlignment.Centered;
			expectedSize = new Size (testText.Length * 2, 1);
			tf.Size = expectedSize;
			Assert.Equal (testText, tf.Text);
			Assert.Equal (TextAlignment.Centered, tf.Alignment);
			Assert.Equal (expectedSize, tf.Size);
			tf.Draw (testBounds, new Attribute (), new Attribute ());
			Assert.Equal (expectedSize, tf.Size);
			Assert.NotEmpty (tf.Lines);
		}

		[Fact]
		public void TestSize_TextChange ()
		{
			var tf = new TextFormatter () { Text = "你" };
			Assert.Equal (2, tf.Size.Width);
			tf.Text = "你你";
			Assert.Equal (4, tf.Size.Width);
		}

		[Fact]
		public void NeedsFormat_Sets ()
		{
			var testText = "test";
			var testBounds = new Rect (0, 0, 100, 1);
			var tf = new TextFormatter ();

			tf.Text = "test";
			Assert.True (tf.NeedsFormat); // get_Lines causes a Format
			Assert.NotEmpty (tf.Lines);
			Assert.False (tf.NeedsFormat); // get_Lines causes a Format
			Assert.Equal (testText, tf.Text);
			tf.Draw (testBounds, new Attribute (), new Attribute ());
			Assert.False (tf.NeedsFormat);

			tf.Size = new Size (1, 1);
			Assert.True (tf.NeedsFormat);
			Assert.NotEmpty (tf.Lines);
			Assert.False (tf.NeedsFormat); // get_Lines causes a Format

			tf.Alignment = TextAlignment.Centered;
			Assert.True (tf.NeedsFormat);
			Assert.NotEmpty (tf.Lines);
			Assert.False (tf.NeedsFormat); // get_Lines causes a Format
		}

		[Theory]
		[InlineData (null)]
		[InlineData ("")]
		[InlineData ("no hotkey")]
		[InlineData ("No hotkey, Upper Case")]
		[InlineData ("Non-english: Сохранить")]
		public void FindHotKey_Invalid_ReturnsFalse (string text)
		{
			Rune hotKeySpecifier = (Rune)'_';
			bool supportFirstUpperCase = false;
			int hotPos = 0;
			Key hotKey = Key.Unknown;
			bool result = false;

			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);
		}

		[Theory]
		[InlineData ("_K Before", true, 0, (Key)'K')]
		[InlineData ("a_K Second", true, 1, (Key)'K')]
		[InlineData ("Last _K", true, 5, (Key)'K')]
		[InlineData ("After K_", false, -1, Key.Unknown)]
		[InlineData ("Multiple _K and _R", true, 9, (Key)'K')]
		[InlineData ("Non-english: _Кдать", true, 13, (Key)'К')] // Cryllic K (К)
		[InlineData ("_K Before", true, 0, (Key)'K', true)] // Turn on FirstUpperCase and verify same results
		[InlineData ("a_K Second", true, 1, (Key)'K', true)]
		[InlineData ("Last _K", true, 5, (Key)'K', true)]
		[InlineData ("After K_", false, -1, Key.Unknown, true)]
		[InlineData ("Multiple _K and _R", true, 9, (Key)'K', true)]
		[InlineData ("Non-english: _Кдать", true, 13, (Key)'К', true)] // Cryllic K (К)
		public void FindHotKey_AlphaUpperCase_Succeeds (string text, bool expectedResult, int expectedHotPos, Key expectedKey, bool supportFirstUpperCase = false)
		{
			Rune hotKeySpecifier = (Rune)'_';

			var result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out int hotPos, out Key hotKey);
			if (expectedResult) {
				Assert.True (result);
			} else {
				Assert.False (result);
			}
			Assert.Equal (expectedResult, result);
			Assert.Equal (expectedHotPos, hotPos);
			Assert.Equal (expectedKey, hotKey);
		}

		[Theory]
		[InlineData ("_k Before", true, 0, (Key)'K')] // lower case should return uppercase Hotkey
		[InlineData ("a_k Second", true, 1, (Key)'K')]
		[InlineData ("Last _k", true, 5, (Key)'K')]
		[InlineData ("After k_", false, -1, Key.Unknown)]
		[InlineData ("Multiple _k and _R", true, 9, (Key)'K')]
		[InlineData ("Non-english: _кдать", true, 13, (Key)'К')] // Lower case Cryllic K (к)
		[InlineData ("_k Before", true, 0, (Key)'K', true)] // Turn on FirstUpperCase and verify same results
		[InlineData ("a_k Second", true, 1, (Key)'K', true)]
		[InlineData ("Last _k", true, 5, (Key)'K', true)]
		[InlineData ("After k_", false, -1, Key.Unknown, true)]
		[InlineData ("Multiple _k and _r", true, 9, (Key)'K', true)]
		[InlineData ("Non-english: _кдать", true, 13, (Key)'К', true)] // Cryllic K (К)
		public void FindHotKey_AlphaLowerCase_Succeeds (string text, bool expectedResult, int expectedHotPos, Key expectedKey, bool supportFirstUpperCase = false)
		{
			Rune hotKeySpecifier = (Rune)'_';

			var result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out int hotPos, out Key hotKey);
			if (expectedResult) {
				Assert.True (result);
			} else {
				Assert.False (result);
			}
			Assert.Equal (expectedResult, result);
			Assert.Equal (expectedHotPos, hotPos);
			Assert.Equal (expectedKey, hotKey);
		}

		[Theory]
		[InlineData ("_1 Before", true, 0, (Key)'1')] // Digits 
		[InlineData ("a_1 Second", true, 1, (Key)'1')]
		[InlineData ("Last _1", true, 5, (Key)'1')]
		[InlineData ("After 1_", false, -1, Key.Unknown)]
		[InlineData ("Multiple _1 and _2", true, 9, (Key)'1')]
		[InlineData ("_1 Before", true, 0, (Key)'1', true)] // Turn on FirstUpperCase and verify same results
		[InlineData ("a_1 Second", true, 1, (Key)'1', true)]
		[InlineData ("Last _1", true, 5, (Key)'1', true)]
		[InlineData ("After 1_", false, -1, Key.Unknown, true)]
		[InlineData ("Multiple _1 and _2", true, 9, (Key)'1', true)]
		public void FindHotKey_Numeric_Succeeds (string text, bool expectedResult, int expectedHotPos, Key expectedKey, bool supportFirstUpperCase = false)
		{
			Rune hotKeySpecifier = (Rune)'_';

			var result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out int hotPos, out Key hotKey);
			if (expectedResult) {
				Assert.True (result);
			} else {
				Assert.False (result);
			}
			Assert.Equal (expectedResult, result);
			Assert.Equal (expectedHotPos, hotPos);
			Assert.Equal (expectedKey, hotKey);
		}

		[Theory]
		[InlineData ("K Before", true, 0, (Key)'K')]
		[InlineData ("aK Second", true, 1, (Key)'K')]
		[InlineData ("last K", true, 5, (Key)'K')]
		[InlineData ("multiple K and R", true, 9, (Key)'K')]
		[InlineData ("non-english: Кдать", true, 13, (Key)'К')] // Cryllic K (К)
		public void FindHotKey_Legacy_FirstUpperCase_Succeeds (string text, bool expectedResult, int expectedHotPos, Key expectedKey)
		{
			var supportFirstUpperCase = true;

			Rune hotKeySpecifier = (Rune)0;

			var result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out int hotPos, out Key hotKey);
			if (expectedResult) {
				Assert.True (result);
			} else {
				Assert.False (result);
			}
			Assert.Equal (expectedResult, result);
			Assert.Equal (expectedHotPos, hotPos);
			Assert.Equal (expectedKey, hotKey);
		}

		[Theory]
		[InlineData ("\"k before")]
		[InlineData ("ak second")]
		[InlineData ("last k")]
		[InlineData ("multiple k and r")]
		[InlineData ("12345")]
		[InlineData ("`~!@#$%^&*()-_=+[{]}\\|;:'\",<.>/?")] // punctuation
		[InlineData (" ~  s  gui.cs   master ↑10")] // ~IsLetterOrDigit + Unicode
		[InlineData ("non-english: кдать")] // Lower case Cryllic K (к)
		public void FindHotKey_Legacy_FirstUpperCase_NotFound_Returns_False (string text)
		{
			bool supportFirstUpperCase = true;

			var hotKeySpecifier = (Rune)0;

			var result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out int hotPos, out Key hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);
		}

		static string testHotKeyAtStart = "_K Before";
		static string testHotKeyAtSecondPos = "a_K Second";
		static string testHotKeyAtLastPos = "Last _K";
		static string testHotKeyAfterLastChar = "After K_";
		static string testMultiHotKeys = "Multiple _K and _R";
		static string testNonEnglish = "Non-english: _Кдать";

		[Theory]
		[InlineData (null)]
		[InlineData ("")]
		[InlineData ("a")]
		public void RemoveHotKeySpecifier_InValid_ReturnsOriginal (string text)
		{
			Rune hotKeySpecifier = (Rune)'_';

			if (text == null) {
				Assert.Null (TextFormatter.RemoveHotKeySpecifier (text, 0, hotKeySpecifier));
				Assert.Null (TextFormatter.RemoveHotKeySpecifier (text, -1, hotKeySpecifier));
				Assert.Null (TextFormatter.RemoveHotKeySpecifier (text, 100, hotKeySpecifier));
			} else {
				Assert.Equal (text, TextFormatter.RemoveHotKeySpecifier (text, 0, hotKeySpecifier));
				Assert.Equal (text, TextFormatter.RemoveHotKeySpecifier (text, -1, hotKeySpecifier));
				Assert.Equal (text, TextFormatter.RemoveHotKeySpecifier (text, 100, hotKeySpecifier));
			}
		}

		[Theory]
		[InlineData ("_K Before", 0, "K Before")]
		[InlineData ("a_K Second", 1, "aK Second")]
		[InlineData ("Last _K", 5, "Last K")]
		[InlineData ("After K_", 7, "After K")]
		[InlineData ("Multiple _K and _R", 9, "Multiple K and _R")]
		[InlineData ("Non-english: _Кдать", 13, "Non-english: Кдать")]
		public void RemoveHotKeySpecifier_Valid_ReturnsStripped (string text, int hotPos, string expectedText)
		{
			Rune hotKeySpecifier = (Rune)'_';

			Assert.Equal (expectedText, TextFormatter.RemoveHotKeySpecifier (text, hotPos, hotKeySpecifier));
		}

		[Theory]
		[InlineData ("all lower case", 0)]
		[InlineData ("K Before", 0)]
		[InlineData ("aK Second", 1)]
		[InlineData ("Last K", 5)]
		[InlineData ("fter K", 7)]
		[InlineData ("Multiple K and R", 9)]
		[InlineData ("Non-english: Кдать", 13)]
		public void RemoveHotKeySpecifier_Valid_Legacy_ReturnsOriginal (string text, int hotPos)
		{
			Rune hotKeySpecifier = (Rune)'_';

			Assert.Equal (text, TextFormatter.RemoveHotKeySpecifier (text, hotPos, hotKeySpecifier));
		}

		[Theory]
		[InlineData (null)]
		[InlineData ("")]
		public void CalcRect_Invalid_Returns_Empty (string text)
		{
			Assert.Equal (Rect.Empty, TextFormatter.CalcRect (0, 0, text));
			Assert.Equal (new Rect (new Point (1, 2), Size.Empty), TextFormatter.CalcRect (1, 2, text));
			Assert.Equal (new Rect (new Point (-1, -2), Size.Empty), TextFormatter.CalcRect (-1, -2, text));
		}

		[Theory]
		[InlineData ("test")]
		[InlineData (" ~  s  gui.cs   master ↑10")]
		public void CalcRect_SingleLine_Returns_1High (string text)
		{
			Assert.Equal (new Rect (0, 0, text.GetRuneCount (), 1), TextFormatter.CalcRect (0, 0, text));
			Assert.Equal (new Rect (0, 0, text.GetColumns (), 1), TextFormatter.CalcRect (0, 0, text));
		}

		[Theory]
		[InlineData ("line1\nline2", 5, 2)]
		[InlineData ("\nline2", 5, 2)]
		[InlineData ("\n\n", 0, 3)]
		[InlineData ("\n\n\n", 0, 4)]
		[InlineData ("line1\nline2\nline3long!", 10, 3)]
		[InlineData ("line1\nline2\n\n", 5, 4)]
		[InlineData ("line1\r\nline2", 5, 2)]
		[InlineData (" ~  s  gui.cs   master ↑10\n", 31, 2)]
		[InlineData ("\n ~  s  gui.cs   master ↑10", 31, 2)]
		[InlineData (" ~  s  gui.cs   master\n↑10", 27, 2)]
		public void CalcRect_MultiLine_Returns_nHigh (string text, int expectedWidth, int expectedLines)
		{
			Assert.Equal (new Rect (0, 0, expectedWidth, expectedLines), TextFormatter.CalcRect (0, 0, text));
			var lines = text.Split (text.Contains (Environment.NewLine) ? Environment.NewLine : "\n");
			var maxWidth = lines.Max (s => s.GetColumns ());
			var lineWider = 0;
			for (int i = 0; i < lines.Length; i++) {
				var w = lines [i].GetColumns ();
				if (w == maxWidth) {
					lineWider = i;
				}
			}
			Assert.Equal (new Rect (0, 0, maxWidth, expectedLines), TextFormatter.CalcRect (0, 0, text));
			Assert.Equal (new Rect (0, 0, lines [lineWider].ToRuneList ().Sum (r => Math.Max (r.GetColumns (), 0)), expectedLines), TextFormatter.CalcRect (0, 0, text));
		}

		[Theory]
		[InlineData ("")]
		[InlineData (null)]
		[InlineData ("test")]
		public void ClipAndJustify_Invalid_Returns_Original (string text)
		{
			var expected = string.IsNullOrEmpty (text) ? text : "";
			Assert.Equal (expected, TextFormatter.ClipAndJustify (text, 0, TextAlignment.Left));
			Assert.Equal (expected, TextFormatter.ClipAndJustify (text, 0, TextAlignment.Left));
			Assert.Throws<ArgumentOutOfRangeException> (() => TextFormatter.ClipAndJustify (text, -1, TextAlignment.Left));
		}

		[Theory]
		[InlineData ("test", "", 0)]
		[InlineData ("test", "te", 2)]
		[InlineData ("test", "test", int.MaxValue)]
		[InlineData ("A sentence has words.", "A sentence has words.", 22)] // should fit
		[InlineData ("A sentence has words.", "A sentence has words.", 21)] // should fit
		[InlineData ("A sentence has words.", "A sentence has words.", int.MaxValue)] // should fit
		[InlineData ("A sentence has words.", "A sentence has words", 20)] // Should not fit
		[InlineData ("A sentence has words.", "A sentence", 10)] // Should not fit
		[InlineData ("A\tsentence\thas\twords.", "A\tsentence\thas\twords.", int.MaxValue)]
		[InlineData ("A\tsentence\thas\twords.", "A\tsentence", 10)]
		[InlineData ("line1\nline2\nline3long!", "line1\nline2\nline3long!", int.MaxValue)]
		[InlineData ("line1\nline2\nline3long!", "line1\nline", 10)]
		[InlineData (" ~  s  gui.cs   master ↑10", " ~  s  ", 10)] // Unicode
		[InlineData ("Ð ÑÐ", "Ð ÑÐ", 5)] // should fit
		[InlineData ("Ð ÑÐ", "Ð ÑÐ", 4)] // should fit
		[InlineData ("Ð ÑÐ", "Ð Ñ", 3)] // Should not fit
		public void ClipAndJustify_Valid_Left (string text, string justifiedText, int maxWidth)
		{
			var align = TextAlignment.Left;

			Assert.Equal (justifiedText, TextFormatter.ClipAndJustify (text, maxWidth, align));
			var expectedClippedWidth = Math.Min (justifiedText.GetRuneCount (), maxWidth);
			Assert.Equal (justifiedText, TextFormatter.ClipAndJustify (text, maxWidth, align));
			Assert.True (justifiedText.GetRuneCount () <= maxWidth);
			Assert.True (justifiedText.GetColumns () <= maxWidth);
			Assert.Equal (expectedClippedWidth, justifiedText.GetRuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.GetColumns (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.ToString (justifiedText.ToRunes () [0..expectedClippedWidth]), justifiedText);
		}

		[Theory]
		[InlineData ("test", "", 0)]
		[InlineData ("test", "te", 2)]
		[InlineData ("test", "test", int.MaxValue)]
		[InlineData ("A sentence has words.", "A sentence has words.", 22)] // should fit
		[InlineData ("A sentence has words.", "A sentence has words.", 21)] // should fit
		[InlineData ("A sentence has words.", "A sentence has words.", int.MaxValue)] // should fit
		[InlineData ("A sentence has words.", "A sentence has words", 20)] // Should not fit
		[InlineData ("A sentence has words.", "A sentence", 10)] // Should not fit
		[InlineData ("A\tsentence\thas\twords.", "A\tsentence\thas\twords.", int.MaxValue)]
		[InlineData ("A\tsentence\thas\twords.", "A\tsentence", 10)]
		[InlineData ("line1\nline2\nline3long!", "line1\nline2\nline3long!", int.MaxValue)]
		[InlineData ("line1\nline2\nline3long!", "line1\nline", 10)]
		[InlineData (" ~  s  gui.cs   master ↑10", " ~  s  ", 10)] // Unicode
		[InlineData ("Ð ÑÐ", "Ð ÑÐ", 5)] // should fit
		[InlineData ("Ð ÑÐ", "Ð ÑÐ", 4)] // should fit
		[InlineData ("Ð ÑÐ", "Ð Ñ", 3)] // Should not fit
		public void ClipAndJustify_Valid_Right (string text, string justifiedText, int maxWidth)
		{
			var align = TextAlignment.Right;

			Assert.Equal (justifiedText, TextFormatter.ClipAndJustify (text, maxWidth, align));
			var expectedClippedWidth = Math.Min (justifiedText.GetRuneCount (), maxWidth);
			Assert.Equal (justifiedText, TextFormatter.ClipAndJustify (text, maxWidth, align));
			Assert.True (justifiedText.GetRuneCount () <= maxWidth);
			Assert.True (justifiedText.GetColumns () <= maxWidth);
			Assert.Equal (expectedClippedWidth, justifiedText.GetRuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.GetColumns (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.ToString (justifiedText.ToRunes () [0..expectedClippedWidth]), justifiedText);
		}

		[Theory]
		[InlineData ("test", "", 0)]
		[InlineData ("test", "te", 2)]
		[InlineData ("test", "test", int.MaxValue)]
		[InlineData ("A sentence has words.", "A sentence has words.", 22)] // should fit
		[InlineData ("A sentence has words.", "A sentence has words.", 21)] // should fit
		[InlineData ("A sentence has words.", "A sentence has words.", int.MaxValue)] // should fit
		[InlineData ("A sentence has words.", "A sentence has words", 20)] // Should not fit
		[InlineData ("A sentence has words.", "A sentence", 10)] // Should not fit
		[InlineData ("A\tsentence\thas\twords.", "A\tsentence\thas\twords.", int.MaxValue)]
		[InlineData ("A\tsentence\thas\twords.", "A\tsentence", 10)]
		[InlineData ("line1\nline2\nline3long!", "line1\nline2\nline3long!", int.MaxValue)]
		[InlineData ("line1\nline2\nline3long!", "line1\nline", 10)]
		[InlineData (" ~  s  gui.cs   master ↑10", " ~  s  ", 10)] // Unicode
		[InlineData ("Ð ÑÐ", "Ð ÑÐ", 5)] // should fit
		[InlineData ("Ð ÑÐ", "Ð ÑÐ", 4)] // should fit
		[InlineData ("Ð ÑÐ", "Ð Ñ", 3)] // Should not fit
		public void ClipAndJustify_Valid_Centered (string text, string justifiedText, int maxWidth)
		{
			var align = TextAlignment.Centered;

			Assert.Equal (justifiedText, TextFormatter.ClipAndJustify (text, maxWidth, align));
			var expectedClippedWidth = Math.Min (justifiedText.GetRuneCount (), maxWidth);
			Assert.Equal (justifiedText, TextFormatter.ClipAndJustify (text, maxWidth, align));
			Assert.True (justifiedText.GetRuneCount () <= maxWidth);
			Assert.True (justifiedText.GetColumns () <= maxWidth);
			Assert.Equal (expectedClippedWidth, justifiedText.GetRuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.GetColumns (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.ToString (justifiedText.ToRunes () [0..expectedClippedWidth]), justifiedText);
		}

		[Theory]
		[InlineData ("test", "", 0)]
		[InlineData ("test", "te", 2)]
		[InlineData ("test", "test", int.MaxValue)]
		[InlineData ("A sentence has words.", "A  sentence has words.", 22)] // should fit
		[InlineData ("A sentence has words.", "A sentence has words.", 21)] // should fit
		[InlineData ("A sentence has words.", "A                                                                                                                                                                 sentence                                                                                                                                                                 has                                                                                                                                                                words.", 500)] // should fit
		[InlineData ("A sentence has words.", "A sentence has words", 20)] // Should not fit
		[InlineData ("A sentence has words.", "A sentence", 10)] // Should not fit
		[InlineData ("A\tsentence\thas\twords.", "A\tsentence\thas\twords.", int.MaxValue)]
		[InlineData ("A\tsentence\thas\twords.", "A\tsentence", 10)]
		[InlineData ("line1\nline2\nline3long!", "line1\nline2\nline3long!", int.MaxValue)]
		[InlineData ("line1\nline2\nline3long!", "line1\nline", 10)]
		[InlineData (" ~  s  gui.cs   master ↑10", " ~  s  ", 10)] // Unicode
		[InlineData ("Ð ÑÐ", "Ð  ÑÐ", 5)] // should fit
		[InlineData ("Ð ÑÐ", "Ð ÑÐ", 4)] // should fit
		[InlineData ("Ð ÑÐ", "Ð Ñ", 3)] // Should not fit
		public void ClipAndJustify_Valid_Justified (string text, string justifiedText, int maxWidth)
		{
			var align = TextAlignment.Justified;

			Assert.Equal (justifiedText, TextFormatter.ClipAndJustify (text, maxWidth, align));
			var expectedClippedWidth = Math.Min (justifiedText.GetRuneCount (), maxWidth);
			Assert.Equal (justifiedText, TextFormatter.ClipAndJustify (text, maxWidth, align));
			Assert.True (justifiedText.GetRuneCount () <= maxWidth);
			Assert.True (justifiedText.GetColumns () <= maxWidth);
			Assert.Equal (expectedClippedWidth, justifiedText.GetRuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.GetColumns (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.ToString (justifiedText.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// see Justify_ tests below
		}

		[Theory]
		[InlineData ("")]
		[InlineData (null)]
		[InlineData ("test")]
		public void Justify_Invalid (string text)
		{
			Assert.Equal (text, TextFormatter.Justify (text, 0));
			Assert.Equal (text, TextFormatter.Justify (text, 0));
			Assert.Throws<ArgumentOutOfRangeException> (() => TextFormatter.Justify (text, -1));
		}

		[Theory]
		[InlineData ("word")] // Even # of chars
		[InlineData ("word.")] // Odd # of chars
		[InlineData ("Ð¿ÑÐ¸Ð²ÐµÑ")] // Unicode (even #)
		[InlineData ("Ð¿ÑÐ¸Ð²ÐµÑ.")] // Unicode (odd # of chars)
		public void Justify_SingleWord (string text)
		{
			var justifiedText = text;
			char fillChar = '+';

			int width = text.GetRuneCount ();
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.GetRuneCount () + 1;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.GetRuneCount () + 2;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.GetRuneCount () + 10;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.GetRuneCount () + 11;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
		}

		[Theory]
		// Even # of spaces
		//            0123456789
		[InlineData ("012 456 89", "012 456 89", 10, 0, "+", true)]
		[InlineData ("012 456 89", "012++456+89", 11, 1)]
		[InlineData ("012 456 89", "012 456 89", 12, 2, "++", true)]
		[InlineData ("012 456 89", "012+++456++89", 13, 3)]
		[InlineData ("012 456 89", "012 456 89", 14, 4, "+++", true)]
		[InlineData ("012 456 89", "012++++456+++89", 15, 5)]
		[InlineData ("012 456 89", "012 456 89", 16, 6, "++++", true)]
		[InlineData ("012 456 89", "012 456 89", 30, 20, "+++++++++++", true)]
		[InlineData ("012 456 89", "012+++++++++++++456++++++++++++89", 33, 23)]
		// Odd # of spaces
		//            01234567890123
		[InlineData ("012 456 89 end", "012 456 89 end", 14, 0, "+", true)]
		[InlineData ("012 456 89 end", "012++456+89+end", 15, 1)]
		[InlineData ("012 456 89 end", "012++456++89+end", 16, 2)]
		[InlineData ("012 456 89 end", "012 456 89 end", 17, 3, "++", true)]
		[InlineData ("012 456 89 end", "012+++456++89++end", 18, 4)]
		[InlineData ("012 456 89 end", "012+++456+++89++end", 19, 5)]
		[InlineData ("012 456 89 end", "012 456 89 end", 20, 6, "+++", true)]
		[InlineData ("012 456 89 end", "012++++++++456++++++++89+++++++end", 34, 20)]
		[InlineData ("012 456 89 end", "012+++++++++456+++++++++89++++++++end", 37, 23)]
		// Unicode
		// Even # of chars
		//            0123456789
		[InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ Ð²Ð Ñ", 10, 0, "+", true)]
		[InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ++Ð²Ð+Ñ", 11, 1)]
		[InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ Ð²Ð Ñ", 12, 2, "++", true)]
		[InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ+++Ð²Ð++Ñ", 13, 3)]
		[InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ Ð²Ð Ñ", 14, 4, "+++", true)]
		[InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ++++Ð²Ð+++Ñ", 15, 5)]
		[InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ Ð²Ð Ñ", 16, 6, "++++", true)]
		[InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ Ð²Ð Ñ", 30, 20, "+++++++++++", true)]
		[InlineData ("Ð¿ÑÐ Ð²Ð Ñ", "Ð¿ÑÐ+++++++++++++Ð²Ð++++++++++++Ñ", 33, 23)]
		// Unicode
		// Odd # of chars
		//            0123456789
		[InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð ÑÐ Ð²Ð Ñ", 10, 0, "+", true)]
		[InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð++ÑÐ+Ð²Ð+Ñ", 11, 1)]
		[InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð++ÑÐ++Ð²Ð+Ñ", 12, 2)]
		[InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð ÑÐ Ð²Ð Ñ", 13, 3, "++", true)]
		[InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð+++ÑÐ++Ð²Ð++Ñ", 14, 4)]
		[InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð+++ÑÐ+++Ð²Ð++Ñ", 15, 5)]
		[InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð ÑÐ Ð²Ð Ñ", 16, 6, "+++", true)]
		[InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð++++++++ÑÐ++++++++Ð²Ð+++++++Ñ", 30, 20)]
		[InlineData ("Ð ÑÐ Ð²Ð Ñ", "Ð+++++++++ÑÐ+++++++++Ð²Ð++++++++Ñ", 33, 23)]
		public void Justify_Sentence (string text, string justifiedText, int forceToWidth, int widthOffset, string replaceWith = null, bool replace = false)
		{
			char fillChar = '+';

			Assert.Equal (forceToWidth, text.GetRuneCount () + widthOffset);
			if (replace) {
				justifiedText = text.Replace (" ", replaceWith);
			}
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.GetRuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.GetColumns ()) < text.Count (s => s == ' '));
		}

		[Fact]
		public void WordWrap_Invalid ()
		{
			var text = string.Empty;
			int width = 0;

			Assert.Empty (TextFormatter.WordWrapText (null, width));
			Assert.Empty (TextFormatter.WordWrapText (text, width));
			Assert.Throws<ArgumentOutOfRangeException> (() => TextFormatter.WordWrapText (text, -1));
		}

		[Fact]
		public void WordWrap_BigWidth ()
		{
			List<string> wrappedLines;

			var text = "Constantinople";
			wrappedLines = TextFormatter.WordWrapText (text, 100);
			Assert.True (wrappedLines.Count == 1);
			Assert.Equal ("Constantinople", wrappedLines [0]);
		}

		[Theory]
		[InlineData ("Constantinople", 14, 0, new string [] { "Constantinople" })]
		[InlineData ("Constantinople", 12, -2, new string [] { "Constantinop", "le" })]
		[InlineData ("Constantinople", 9, -5, new string [] { "Constanti", "nople" })]
		[InlineData ("Constantinople", 7, -7, new string [] { "Constan", "tinople" })]
		[InlineData ("Constantinople", 5, -9, new string [] { "Const", "antin", "ople" })]
		[InlineData ("Constantinople", 4, -10, new string [] { "Cons", "tant", "inop", "le" })]
		[InlineData ("Constantinople", 1, -13, new string [] { "C", "o", "n", "s", "t", "a", "n", "t", "i", "n", "o", "p", "l", "e" })]
		public void WordWrap_SingleWordLine (string text, int maxWidth, int widthOffset, IEnumerable<string> resultLines)
		{
			List<string> wrappedLines;

			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0));
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0));
			Assert.Equal (resultLines, wrappedLines);
		}

		[Theory]
		[InlineData ("กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ", 51, 0, new string [] { "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ" })]
		[InlineData ("กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ", 50, -1, new string [] { "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัา", "ำ" })]
		[InlineData ("กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ", 46, -5, new string [] { "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮ", "ฯะัาำ" })]
		[InlineData ("กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ", 26, -25, new string [] { "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบ", "ปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ" })]
		[InlineData ("กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ", 17, -34, new string [] { "กขฃคฅฆงจฉชซฌญฎฏฐฑ", "ฒณดตถทธนบปผฝพฟภมย", "รฤลฦวศษสหฬอฮฯะัาำ" })]
		[InlineData ("กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ", 13, -38, new string [] { "กขฃคฅฆงจฉชซฌญ", "ฎฏฐฑฒณดตถทธนบ", "ปผฝพฟภมยรฤลฦว", "ศษสหฬอฮฯะัาำ" })]
		[InlineData ("กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ", 1, -50, new string [] { "ก", "ข", "ฃ", "ค", "ฅ", "ฆ", "ง", "จ", "ฉ", "ช", "ซ", "ฌ", "ญ", "ฎ", "ฏ", "ฐ", "ฑ", "ฒ", "ณ", "ด", "ต", "ถ", "ท", "ธ", "น", "บ", "ป", "ผ", "ฝ", "พ", "ฟ", "ภ", "ม", "ย", "ร", "ฤ", "ล", "ฦ", "ว", "ศ", "ษ", "ส", "ห", "ฬ", "อ", "ฮ", "ฯ", "ะ", "ั", "า", "ำ" })]
		public void WordWrap_Unicode_SingleWordLine (string text, int maxWidth, int widthOffset, IEnumerable<string> resultLines)
		{
			List<string> wrappedLines;

			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0));
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0));
			Assert.Equal (resultLines, wrappedLines);
		}

		[Theory]
		[InlineData ("This\u00A0is\u00A0a\u00A0sentence.", 19, 0, new string [] { "This\u00A0is\u00A0a\u00A0sentence." })]
		[InlineData ("This\u00A0is\u00A0a\u00A0sentence.", 18, -1, new string [] { "This\u00A0is\u00A0a\u00A0sentence", "." })]
		[InlineData ("This\u00A0is\u00A0a\u00A0sentence.", 17, -2, new string [] { "This\u00A0is\u00A0a\u00A0sentenc", "e." })]
		[InlineData ("This\u00A0is\u00A0a\u00A0sentence.", 14, -5, new string [] { "This\u00A0is\u00A0a\u00A0sent", "ence." })]
		[InlineData ("This\u00A0is\u00A0a\u00A0sentence.", 10, -9, new string [] { "This\u00A0is\u00A0a\u00A0", "sentence." })]
		[InlineData ("This\u00A0is\u00A0a\u00A0sentence.", 7, -12, new string [] { "This\u00A0is", "\u00A0a\u00A0sent", "ence." })]
		[InlineData ("This\u00A0is\u00A0a\u00A0sentence.", 5, -14, new string [] { "This\u00A0", "is\u00A0a\u00A0", "sente", "nce." })]
		[InlineData ("This\u00A0is\u00A0a\u00A0sentence.", 1, -18, new string [] { "T", "h", "i", "s", "\u00A0", "i", "s", "\u00A0", "a", "\u00A0", "s", "e", "n", "t", "e", "n", "c", "e", "." })]
		public void WordWrap_Unicode_LineWithNonBreakingSpace (string text, int maxWidth, int widthOffset, IEnumerable<string> resultLines)
		{
			List<string> wrappedLines;

			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0));
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0));
			Assert.Equal (resultLines, wrappedLines);
		}

		[Theory]
		[InlineData ("This\u00A0is\n\u00A0a\u00A0sentence.", 20, 0, new string [] { "This\u00A0is\u00A0a\u00A0sentence." })]
		[InlineData ("This\u00A0is\n\u00A0a\u00A0sentence.", 19, -1, new string [] { "This\u00A0is\u00A0a\u00A0sentence." })]
		[InlineData ("\u00A0\u00A0\u00A0\u00A0\u00A0test\u00A0sentence.", 19, 0, new string [] { "\u00A0\u00A0\u00A0\u00A0\u00A0test\u00A0sentence." })]
		public void WordWrap_Unicode_2LinesWithNonBreakingSpace (string text, int maxWidth, int widthOffset, IEnumerable<string> resultLines)
		{
			List<string> wrappedLines;

			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0));
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0));
			Assert.Equal (resultLines, wrappedLines);
		}

		[Theory]
		[InlineData ("A sentence has words.", 21, 0, new string [] { "A sentence has words." })]
		[InlineData ("A sentence has words.", 20, -1, new string [] { "A sentence has", "words." })]
		[InlineData ("A sentence has words.", 15, -6, new string [] { "A sentence has", "words." })]
		[InlineData ("A sentence has words.", 14, -7, new string [] { "A sentence has", "words." })]
		[InlineData ("A sentence has words.", 13, -8, new string [] { "A sentence", "has words." })]
		// Unicode 
		[InlineData ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.", 42, 0, new string [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words." })]
		[InlineData ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.", 41, -1, new string [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has", "words." })]
		[InlineData ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.", 36, -6, new string [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has", "words." })]
		[InlineData ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.", 35, -7, new string [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has", "words." })]
		[InlineData ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.", 34, -8, new string [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ)", "has words." })]
		[InlineData ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.", 25, -17, new string [] { "A Unicode sentence", "(Ð¿ÑÐ¸Ð²ÐµÑ) has words." })]
		public void WordWrap_NoNewLines_Default (string text, int maxWidth, int widthOffset, IEnumerable<string> resultLines)
		{
			// Calls WordWrapText (text, width) and thus preserveTrailingSpaces defaults to false
			List<string> wrappedLines;

			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0));
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0));
			Assert.Equal (resultLines, wrappedLines);
		}

		/// <summary>
		/// WordWrap strips CRLF
		/// </summary>
		[Theory]
		[InlineData ("A sentence has words.\nA paragraph has lines.", 44, 0, new string [] { "A sentence has words.A paragraph has lines." })]
		[InlineData ("A sentence has words.\nA paragraph has lines.", 43, -1, new string [] { "A sentence has words.A paragraph has lines." })]
		[InlineData ("A sentence has words.\nA paragraph has lines.", 38, -6, new string [] { "A sentence has words.A paragraph has", "lines." })]
		[InlineData ("A sentence has words.\nA paragraph has lines.", 34, -10, new string [] { "A sentence has words.A paragraph", "has lines." })]
		[InlineData ("A sentence has words.\nA paragraph has lines.", 27, -17, new string [] { "A sentence has words.A", "paragraph has lines." })]
		// Unicode 
		[InlineData ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.\nA Unicode Пункт has Линии.", 69, 0, new string [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode Пункт has Линии." })]
		[InlineData ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.\nA Unicode Пункт has Линии.", 68, -1, new string [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode Пункт has Линии." })]
		[InlineData ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.\nA Unicode Пункт has Линии.", 63, -6, new string [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode Пункт has", "Линии." })]
		[InlineData ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.\nA Unicode Пункт has Линии.", 59, -10, new string [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode Пункт", "has Линии." })]
		[InlineData ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.\nA Unicode Пункт has Линии.", 52, -17, new string [] { "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode", "Пункт has Линии." })]
		public void WordWrap_WithNewLines (string text, int maxWidth, int widthOffset, IEnumerable<string> resultLines)
		{
			List<string> wrappedLines;

			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0));
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0));
			Assert.Equal (resultLines, wrappedLines);
		}

		[Theory]
		[InlineData ("A sentence has words.", 3, -18, new string [] { "A", "sen", "ten", "ce", "has", "wor", "ds." })]
		[InlineData ("A sentence has words.", 2, -19, new string [] { "A", "se", "nt", "en", "ce", "ha", "s", "wo", "rd", "s." })]
		[InlineData ("A sentence has words.", 1, -20, new string [] { "A", "s", "e", "n", "t", "e", "n", "c", "e", "h", "a", "s", "w", "o", "r", "d", "s", "." })]
		public void WordWrap_Narrow_Default (string text, int maxWidth, int widthOffset, IEnumerable<string> resultLines)
		{
			// Calls WordWrapText (text, width) and thus preserveTrailingSpaces defaults to false
			List<string> wrappedLines;

			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0));
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0));
			Assert.Equal (resultLines, wrappedLines);
		}

		[Theory]
		[InlineData ("A sentence has words.", 14, -7, new string [] { "A sentence ", "has words." })]
		[InlineData ("A sentence has words.", 8, -13, new string [] { "A ", "sentence", " has ", "words." })]
		[InlineData ("A sentence has words.", 6, -15, new string [] { "A ", "senten", "ce ", "has ", "words." })]
		[InlineData ("A sentence has words.", 3, -18, new string [] { "A ", "sen", "ten", "ce ", "has", " ", "wor", "ds." })]
		[InlineData ("A sentence has words.", 2, -19, new string [] { "A ", "se", "nt", "en", "ce", " ", "ha", "s ", "wo", "rd", "s." })]
		[InlineData ("A sentence has words.", 1, -20, new string [] { "A", " ", "s", "e", "n", "t", "e", "n", "c", "e", " ", "h", "a", "s", " ", "w", "o", "r", "d", "s", "." })]
		public void WordWrap_PreserveTrailingSpaces_True (string text, int maxWidth, int widthOffset, IEnumerable<string> resultLines)
		{
			List<string> wrappedLines;

			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, preserveTrailingSpaces: true);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0));
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0));
			Assert.Equal (resultLines, wrappedLines);
		}

		[Theory]
		[InlineData ("文に は言葉 があり ます。", 14, 0, new string [] { "文に は言葉 ", "があり ます。" })]
		[InlineData ("文に は言葉 があり ます。", 3, -11, new string [] { "文", "に ", "は", "言", "葉 ", "が", "あ", "り ", "ま", "す", "。" })]
		[InlineData ("文に は言葉 があり ます。", 2, -12, new string [] { "文", "に", " ", "は", "言", "葉", " ", "が", "あ", "り", " ", "ま", "す", "。" })]
		[InlineData ("文に は言葉 があり ます。", 1, -13, new string [] { })]
		public void WordWrap_PreserveTrailingSpaces_True_Wide_Runes (string text, int maxWidth, int widthOffset, IEnumerable<string> resultLines)
		{
			List<string> wrappedLines;

			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, preserveTrailingSpaces: true);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0));
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0));
			Assert.Equal (resultLines, wrappedLines);
		}

		[Theory]
		[InlineData ("文に は言葉 があり ます。", 14, 0, new string [] { "文に は言葉", "があり ます。" })]
		[InlineData ("文に は言葉 があり ます。", 3, -11, new string [] { "文", "に", "は", "言", "葉", "が", "あ", "り", "ま", "す", "。" })]
		[InlineData ("文に は言葉 があり ます。", 2, -12, new string [] { "文", "に", "は", "言", "葉", "が", "あ", "り", "ま", "す", "。" })]
		[InlineData ("文に は言葉 があり ます。", 1, -13, new string [] { })]
		public void WordWrap_PreserveTrailingSpaces_False_Wide_Runes (string text, int maxWidth, int widthOffset, IEnumerable<string> resultLines)
		{
			List<string> wrappedLines;

			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0));
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0));
			Assert.Equal (resultLines, wrappedLines);
		}

		[Theory]
		[InlineData ("A sentence has words. ", 3, new string [] { "A ", "sen", "ten", "ce ", "has", " ", "wor", "ds.", " " })]
		[InlineData ("A   sentence          has  words.  ", 3, new string [] { "A  ", " ", "sen", "ten", "ce ", "   ", "   ", "   ", "has", "  ", "wor", "ds.", "  " })]
		public void WordWrap_PreserveTrailingSpaces_True_With_Simple_Runes_Width_3 (string text, int width, IEnumerable<string> resultLines)
		{
			var wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: true);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.Equal (resultLines, wrappedLines);
			var breakLines = "";
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			var expected = string.Empty;
			foreach (var line in resultLines) {
				expected += $"{line}{Environment.NewLine}";
			}
			Assert.Equal (expected, breakLines);

			// Double space Complex example - this is how VS 2022 does it
			//text = "A  sentence      has words.  ";
			//breakLines = "";
			//wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: true);
			//foreach (var line in wrappedLines) {
			//	breakLines += $"{line}{Environment.NewLine}";
			//}
			//expected = "A  " + Environment.NewLine +
			//	" se" + Environment.NewLine +
			//	" nt" + Environment.NewLine +
			//	" en" + Environment.NewLine +
			//	" ce" + Environment.NewLine +
			//	"  " + Environment.NewLine +
			//	"  " + Environment.NewLine +
			//	"  " + Environment.NewLine +
			//	" ha" + Environment.NewLine +
			//	" s " + Environment.NewLine +
			//	" wo" + Environment.NewLine +
			//	" rd" + Environment.NewLine +
			//	" s." + Environment.NewLine;
			//Assert.Equal (expected, breakLines);
		}

		[Theory]
		[InlineData (null, 1, new string [] { })] // null input
		[InlineData ("", 1, new string [] { })] // Empty input
		[InlineData ("1 34", 1, new string [] { "1", "3", "4" })] // Single Spaces
		[InlineData ("1", 1, new string [] { "1" })] // Short input
		[InlineData ("12", 1, new string [] { "1", "2" })]
		[InlineData ("123", 1, new string [] { "1", "2", "3" })]
		[InlineData ("123456", 1, new string [] { "1", "2", "3", "4", "5", "6" })] // No spaces
		[InlineData (" ", 1, new string [] { " " })] // Just Spaces; should result in a single space
		[InlineData ("  ", 1, new string [] { " " })]
		[InlineData ("   ", 1, new string [] { " ", " " })]
		[InlineData ("    ", 1, new string [] { " ", " " })]
		[InlineData ("12 456", 1, new string [] { "1", "2", "4", "5", "6" })] // Single Spaces
		[InlineData (" 2 456", 1, new string [] { " ", "2", "4", "5", "6" })] // Leading spaces should be preserved.
		[InlineData (" 2 456 8", 1, new string [] { " ", "2", "4", "5", "6", "8" })]
		[InlineData ("A sentence has words. ", 1, new string [] { "A", "s", "e", "n", "t", "e", "n", "c", "e", "h", "a", "s", "w", "o", "r", "d", "s", "." })] // Complex example
		[InlineData ("12  567", 1, new string [] { "1", "2", " ", "5", "6", "7" })] // Double Spaces
		[InlineData ("  3 567", 1, new string [] { " ", "3", "5", "6", "7" })] // Double Leading spaces should be preserved.
		[InlineData ("  3  678  1", 1, new string [] { " ", "3", " ", "6", "7", "8", " ", "1" })]
		[InlineData ("1  456", 1, new string [] { "1", " ", "4", "5", "6" })]
		[InlineData ("A  sentence   has words.  ", 1, new string [] { "A", " ", "s", "e", "n", "t", "e", "n", "c", "e", " ", "h", "a", "s", "w", "o", "r", "d", "s", ".", " " })] // Double space Complex example
		public void WordWrap_PreserveTrailingSpaces_False_With_Simple_Runes_Width_1 (string text, int width, IEnumerable<string> resultLines)
		{
			var wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.Equal (resultLines, wrappedLines);
			var breakLines = "";
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			var expected = string.Empty;
			foreach (var line in resultLines) {
				expected += $"{line}{Environment.NewLine}";
			}
			Assert.Equal (expected, breakLines);
		}

		[Theory]
		[InlineData (null, 3, new string [] { })] // null input
		[InlineData ("", 3, new string [] { })] // Empty input
		[InlineData ("1", 3, new string [] { "1" })] // Short input
		[InlineData ("12", 3, new string [] { "12" })]
		[InlineData ("123", 3, new string [] { "123" })]
		[InlineData ("123456", 3, new string [] { "123", "456" })] // No spaces
		[InlineData ("1234567", 3, new string [] { "123", "456", "7" })] // No spaces
		[InlineData (" ", 3, new string [] { " " })] // Just Spaces; should result in a single space
		[InlineData ("  ", 3, new string [] { "  " })]
		[InlineData ("   ", 3, new string [] { "   " })]
		[InlineData ("    ", 3, new string [] { "   " })]
		[InlineData ("12 456", 3, new string [] { "12", "456" })] // Single Spaces
		[InlineData (" 2 456", 3, new string [] { " 2", "456" })] // Leading spaces should be preserved.
		[InlineData (" 2 456 8", 3, new string [] { " 2", "456", "8" })]
		[InlineData ("A sentence has words. ", 3, new string [] { "A", "sen", "ten", "ce", "has", "wor", "ds." })] // Complex example
		[InlineData ("12  567", 3, new string [] { "12 ", "567" })] // Double Spaces
		[InlineData ("  3 567", 3, new string [] { "  3", "567" })] // Double Leading spaces should be preserved.
		[InlineData ("  3  678  1", 3, new string [] { "  3", " 67", "8 ", "1" })]
		[InlineData ("1  456", 3, new string [] { "1 ", "456" })]
		[InlineData ("A  sentence      has words.  ", 3, new string [] { "A ", "sen", "ten", "ce ", "   ", "has", "wor", "ds.", " " })] // Double space Complex example
		public void WordWrap_PreserveTrailingSpaces_False_With_Simple_Runes_Width_3 (string text, int width, IEnumerable<string> resultLines)
		{
			var wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.Equal (resultLines, wrappedLines);
			var breakLines = "";
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			var expected = string.Empty;
			foreach (var line in resultLines) {
				expected += $"{line}{Environment.NewLine}";
			}
			Assert.Equal (expected, breakLines);
		}

		[Theory]
		[InlineData (null, 50, new string [] { })] // null input
		[InlineData ("", 50, new string [] { })] // Empty input
		[InlineData ("1", 50, new string [] { "1" })] // Short input
		[InlineData ("12", 50, new string [] { "12" })]
		[InlineData ("123", 50, new string [] { "123" })]
		[InlineData ("123456", 50, new string [] { "123456" })] // No spaces
		[InlineData ("1234567", 50, new string [] { "1234567" })] // No spaces
		[InlineData (" ", 50, new string [] { " " })] // Just Spaces; should result in a single space
		[InlineData ("  ", 50, new string [] { "  " })]
		[InlineData ("   ", 50, new string [] { "   " })]
		[InlineData ("12 456", 50, new string [] { "12 456" })] // Single Spaces
		[InlineData (" 2 456", 50, new string [] { " 2 456" })] // Leading spaces should be preserved.
		[InlineData (" 2 456 8", 50, new string [] { " 2 456 8" })]
		[InlineData ("A sentence has words. ", 50, new string [] { "A sentence has words. " })] // Complex example
		[InlineData ("12  567", 50, new string [] { "12  567" })] // Double Spaces
		[InlineData ("  3 567", 50, new string [] { "  3 567" })] // Double Leading spaces should be preserved.
		[InlineData ("  3  678  1", 50, new string [] { "  3  678  1" })]
		[InlineData ("1  456", 50, new string [] { "1  456" })]
		[InlineData ("A  sentence      has words.  ", 50, new string [] { "A  sentence      has words.  " })] // Double space Complex example
		public void WordWrap_PreserveTrailingSpaces_False_With_Simple_Runes_Width_50 (string text, int width, IEnumerable<string> resultLines)
		{
			var wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.Equal (resultLines, wrappedLines);
			var breakLines = "";
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			var expected = string.Empty;
			foreach (var line in resultLines) {
				expected += $"{line}{Environment.NewLine}";
			}
			Assert.Equal (expected, breakLines);
		}

		[Theory]
		[InlineData ("A sentence\t\t\t has words.", 14, -10, new string [] { "A sentence\t", "\t\t has ", "words." })]
		[InlineData ("A sentence\t\t\t has words.", 8, -16, new string [] { "A ", "sentence", "\t\t", "\t ", "has ", "words." })]
		[InlineData ("A sentence\t\t\t has words.", 3, -21, new string [] { "A ", "sen", "ten", "ce", "\t", "\t", "\t", " ", "has", " ", "wor", "ds." })]
		[InlineData ("A sentence\t\t\t has words.", 2, -22, new string [] { "A ", "se", "nt", "en", "ce", "\t", "\t", "\t", " ", "ha", "s ", "wo", "rd", "s." })]
		[InlineData ("A sentence\t\t\t has words.", 1, -23, new string [] { "A", " ", "s", "e", "n", "t", "e", "n", "c", "e", "\t", "\t", "\t", " ", "h", "a", "s", " ", "w", "o", "r", "d", "s", "." })]
		public void WordWrap_PreserveTrailingSpaces_True_With_Tab (string text, int maxWidth, int widthOffset, IEnumerable<string> resultLines, int tabWidth = 4)
		{
			List<string> wrappedLines;

			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, preserveTrailingSpaces: true, tabWidth: tabWidth);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0));
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0));
			Assert.Equal (resultLines, wrappedLines);
		}

		[Theory]
		[InlineData ("これが最初の行です。 こんにちは世界。 これが2行目です。", 29, 0, new string [] { "これが最初の行です。", "こんにちは世界。", "これが2行目です。" })]
		public void WordWrap_PreserveTrailingSpaces_False_Unicode_Wide_Runes (string text, int maxWidth, int widthOffset, IEnumerable<string> resultLines)
		{
			List<string> wrappedLines;

			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, preserveTrailingSpaces: false);
			Assert.Equal (wrappedLines.Count, resultLines.Count ());
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetRuneCount ()) : 0));
			Assert.True (expectedClippedWidth >= (wrappedLines.Count > 0 ? wrappedLines.Max (l => l.GetColumns ()) : 0));
			Assert.Equal (resultLines, wrappedLines);
		}

		[Theory]
		[InlineData ("test", 0, 't', "test")]
		[InlineData ("test", 1, 'e', "test")]
		[InlineData ("Ok", 0, 'O', "Ok")]
		[InlineData ("[◦ Ok ◦]", 3, 'O', "[◦ Ok ◦]")]
		[InlineData ("^k", 0, '^', "^k")]
		public void ReplaceHotKeyWithTag (string text, int hotPos, uint tag, string expected)
		{
			var tf = new TextFormatter ();
			var runes = text.ToRuneList ();
			Rune rune;
			if (Rune.TryGetRuneAt (text, hotPos, out rune)) {
				Assert.Equal (rune, (Rune)tag);

			}
			var result = tf.ReplaceHotKeyWithTag (text, hotPos);
			Assert.Equal (result, expected);
			Assert.Equal ((Rune)tag, result.ToRunes () [hotPos]);
			Assert.Equal (text.GetRuneCount (), runes.Count);
			Assert.Equal (text, StringExtensions.ToString (runes));
		}

		[Theory]
		[InlineData ("", -1, TextAlignment.Left, false, 0)]
		[InlineData (null, 0, TextAlignment.Left, false, 1)]
		[InlineData (null, 0, TextAlignment.Left, true, 1)]
		[InlineData ("", 0, TextAlignment.Left, false, 1)]
		[InlineData ("", 0, TextAlignment.Left, true, 1)]
		public void Reformat_Invalid (string text, int maxWidth, TextAlignment textAlignment, bool wrap, int linesCount)
		{
			if (maxWidth < 0) {
				Assert.Throws<ArgumentOutOfRangeException> (() => TextFormatter.Format (text, maxWidth, textAlignment, wrap));
			} else {
				var list = TextFormatter.Format (text, maxWidth, textAlignment, wrap);
				Assert.NotEmpty (list);
				Assert.True (list.Count == linesCount);
				Assert.Equal (string.Empty, list [0]);
			}
		}

		[Theory]
		[InlineData ("", 0, 0, TextAlignment.Left, false, 1, true)]
		[InlineData ("", 1, 1, TextAlignment.Left, false, 1, true)]
		[InlineData ("A sentence has words.", 0, -21, TextAlignment.Left, false, 1, true)]
		[InlineData ("A sentence has words.", 1, -20, TextAlignment.Left, false, 1, false)]
		[InlineData ("A sentence has words.", 5, -16, TextAlignment.Left, false, 1, false)]
		[InlineData ("A sentence has words.", 20, -1, TextAlignment.Left, false, 1, false)]
		// no clip
		[InlineData ("A sentence has words.", 21, 0, TextAlignment.Left, false, 1, false)]
		[InlineData ("A sentence has words.", 22, 1, TextAlignment.Left, false, 1, false)]
		public void Reformat_NoWordrap_SingleLine (string text, int maxWidth, int widthOffset, TextAlignment textAlignment, bool wrap, int linesCount, bool stringEmpty)
		{
			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			var list = TextFormatter.Format (text, maxWidth, textAlignment, wrap);
			Assert.NotEmpty (list);
			Assert.True (list.Count == linesCount);
			if (stringEmpty) {
				Assert.Equal (string.Empty, list [0]);
			} else {
				Assert.NotEqual (string.Empty, list [0]);
			}
			Assert.Equal (StringExtensions.ToString (text.ToRunes () [0..expectedClippedWidth]), list [0]);
		}

		[Theory]
		[InlineData ("A sentence has words.\nLine 2.", 0, -29, TextAlignment.Left, false, 1, true)]
		[InlineData ("A sentence has words.\nLine 2.", 1, -28, TextAlignment.Left, false, 1, false)]
		[InlineData ("A sentence has words.\nLine 2.", 5, -24, TextAlignment.Left, false, 1, false)]
		[InlineData ("A sentence has words.\nLine 2.", 28, -1, TextAlignment.Left, false, 1, false)]
		// no clip
		[InlineData ("A sentence has words.\nLine 2.", 29, 0, TextAlignment.Left, false, 1, false)]
		[InlineData ("A sentence has words.\nLine 2.", 30, 1, TextAlignment.Left, false, 1, false)]
		[InlineData ("A sentence has words.\r\nLine 2.", 0, -30, TextAlignment.Left, false, 1, true)]
		[InlineData ("A sentence has words.\r\nLine 2.", 1, -29, TextAlignment.Left, false, 1, false)]
		[InlineData ("A sentence has words.\r\nLine 2.", 5, -25, TextAlignment.Left, false, 1, false)]
		[InlineData ("A sentence has words.\r\nLine 2.", 29, -1, TextAlignment.Left, false, 1, false, 1)]
		[InlineData ("A sentence has words.\r\nLine 2.", 30, 0, TextAlignment.Left, false, 1, false)]
		[InlineData ("A sentence has words.\r\nLine 2.", 31, 1, TextAlignment.Left, false, 1, false)]
		public void Reformat_NoWordrap_NewLines (string text, int maxWidth, int widthOffset, TextAlignment textAlignment, bool wrap, int linesCount, bool stringEmpty, int clipWidthOffset = 0)
		{
			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth) + clipWidthOffset;
			var list = TextFormatter.Format (text, maxWidth, textAlignment, wrap);
			Assert.NotEmpty (list);
			Assert.True (list.Count == linesCount);
			if (stringEmpty) {
				Assert.Equal (string.Empty, list [0]);
			} else {
				Assert.NotEqual (string.Empty, list [0]);
			}
			if (text.Contains ("\r\n") && maxWidth > 0) {
				Assert.Equal (StringExtensions.ToString (text.ToRunes () [0..expectedClippedWidth]).Replace ("\r\n", " "), list [0]);
			} else if (text.Contains ('\n') && maxWidth > 0) {
				Assert.Equal (StringExtensions.ToString (text.ToRunes () [0..expectedClippedWidth]).Replace ("\n", " "), list [0]);
			} else {
				Assert.Equal (StringExtensions.ToString (text.ToRunes () [0..expectedClippedWidth]), list [0]);
			}
		}

		[Theory]
		// Even # of spaces
		//            0123456789
		[InlineData ("012 456 89", 0, -10, TextAlignment.Left, true, true, true, new string [] { "" })]
		[InlineData ("012 456 89", 1, -9, TextAlignment.Left, true, true, false, new string [] { "0", "1", "2", " ", "4", "5", "6", " ", "8", "9" }, "01245689")]
		[InlineData ("012 456 89", 5, -5, TextAlignment.Left, true, true, false, new string [] { "012 ", "456 ", "89" })]
		[InlineData ("012 456 89", 9, -1, TextAlignment.Left, true, true, false, new string [] { "012 456 ", "89" })]
		// no clip
		[InlineData ("012 456 89", 10, 0, TextAlignment.Left, true, true, false, new string [] { "012 456 89" })]
		[InlineData ("012 456 89", 11, 1, TextAlignment.Left, true, true, false, new string [] { "012 456 89" })]
		// Odd # of spaces
		//            01234567890123
		[InlineData ("012 456 89 end", 13, -1, TextAlignment.Left, true, true, false, new string [] { "012 456 89 ", "end" })]
		// no clip
		[InlineData ("012 456 89 end", 14, 0, TextAlignment.Left, true, true, false, new string [] { "012 456 89 end" })]
		[InlineData ("012 456 89 end", 15, 1, TextAlignment.Left, true, true, false, new string [] { "012 456 89 end" })]
		public void Reformat_Wrap_Spaces_No_NewLines (string text, int maxWidth, int widthOffset, TextAlignment textAlignment, bool wrap, bool preserveTrailingSpaces, bool stringEmpty, IEnumerable<string> resultLines, string noSpaceText = "")
		{
			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth);
			var list = TextFormatter.Format (text, maxWidth, textAlignment, wrap, preserveTrailingSpaces);
			Assert.NotEmpty (list);
			Assert.True (list.Count == resultLines.Count ());
			if (stringEmpty) {
				Assert.Equal (string.Empty, list [0]);
			} else {
				Assert.NotEqual (string.Empty, list [0]);
			}
			Assert.Equal (resultLines, list);

			if (maxWidth > 0) {
				// remove whitespace chars
				if (maxWidth < 5) {
					expectedClippedWidth = text.GetRuneCount () - text.Sum (r => r == ' ' ? 1 : 0);
				} else {
					expectedClippedWidth = Math.Min (text.GetRuneCount (), maxWidth - text.Sum (r => r == ' ' ? 1 : 0));
				}
				list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap, preserveTrailingSpaces: false);
				if (maxWidth == 1) {
					Assert.Equal (expectedClippedWidth, list.Count);
					Assert.Equal (noSpaceText, string.Concat (list.ToArray ()));
				}
				if (maxWidth > 1 && maxWidth < 10) {
					Assert.Equal (StringExtensions.ToString (text.ToRunes () [0..expectedClippedWidth]), list [0]);
				}
			}
		}

		[Theory]
		// Unicode
		// Even # of chars
		//       0123456789
		[InlineData ("\u2660Ð¿ÑÐ Ð²Ð Ñ", 10, -1, TextAlignment.Left, true, false, new string [] { "\u2660Ð¿ÑÐ Ð²Ð", "Ñ" })]
		// no clip
		[InlineData ("\u2660Ð¿ÑÐ Ð²Ð Ñ", 11, 0, TextAlignment.Left, true, false, new string [] { "\u2660Ð¿ÑÐ Ð²Ð Ñ" })]
		[InlineData ("\u2660Ð¿ÑÐ Ð²Ð Ñ", 12, 1, TextAlignment.Left, true, false, new string [] { "\u2660Ð¿ÑÐ Ð²Ð Ñ" })]
		// Unicode
		// Odd # of chars
		//            0123456789
		[InlineData ("\u2660 ÑÐ Ð²Ð Ñ", 9, -1, TextAlignment.Left, true, false, new string [] { "\u2660 ÑÐ Ð²Ð", "Ñ" })]
		// no clip
		[InlineData ("\u2660 ÑÐ Ð²Ð Ñ", 10, 0, TextAlignment.Left, true, false, new string [] { "\u2660 ÑÐ Ð²Ð Ñ" })]
		[InlineData ("\u2660 ÑÐ Ð²Ð Ñ", 11, 1, TextAlignment.Left, true, false, new string [] { "\u2660 ÑÐ Ð²Ð Ñ" })]
		public void Reformat_Unicode_Wrap_Spaces_No_NewLines (string text, int maxWidth, int widthOffset, TextAlignment textAlignment, bool wrap, bool preserveTrailingSpaces, IEnumerable<string> resultLines)
		{
			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var list = TextFormatter.Format (text, maxWidth, textAlignment, wrap, preserveTrailingSpaces);
			Assert.Equal (list.Count, resultLines.Count ());
			Assert.Equal (resultLines, list);
		}

		[Theory]
		// Unicode
		[InlineData ("\u2460\u2461\u2462\n\u2460\u2461\u2462\u2463\u2464", 8, -1, TextAlignment.Left, true, false, new string [] { "\u2460\u2461\u2462", "\u2460\u2461\u2462\u2463\u2464" })]
		// no clip
		[InlineData ("\u2460\u2461\u2462\n\u2460\u2461\u2462\u2463\u2464", 9, 0, TextAlignment.Left, true, false, new string [] { "\u2460\u2461\u2462", "\u2460\u2461\u2462\u2463\u2464" })]
		[InlineData ("\u2460\u2461\u2462\n\u2460\u2461\u2462\u2463\u2464", 10, 1, TextAlignment.Left, true, false, new string [] { "\u2460\u2461\u2462", "\u2460\u2461\u2462\u2463\u2464" })]
		public void Reformat_Unicode_Wrap_Spaces_NewLines (string text, int maxWidth, int widthOffset, TextAlignment textAlignment, bool wrap, bool preserveTrailingSpaces, IEnumerable<string> resultLines)
		{
			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var list = TextFormatter.Format (text, maxWidth, textAlignment, wrap, preserveTrailingSpaces);
			Assert.Equal (list.Count, resultLines.Count ());
			Assert.Equal (resultLines, list);
		}

		[Theory]
		[InlineData (" A sentence has words. \n This is the second Line - 2. ", 4, -50, TextAlignment.Left, true, false, new string [] { " A", "sent", "ence", "has", "word", "s. ", " Thi", "s is", "the", "seco", "nd", "Line", "- 2." }, " Asentencehaswords.  This isthesecondLine- 2.")]
		[InlineData (" A sentence has words. \n This is the second Line - 2. ", 4, -50, TextAlignment.Left, true, true, new string [] { " A ", "sent", "ence", " ", "has ", "word", "s. ", " ", "This", " is ", "the ", "seco", "nd ", "Line", " - ", "2. " }, " A sentence has words.  This is the second Line - 2. ")]
		public void Format_WordWrap_PreserveTrailingSpaces (string text, int maxWidth, int widthOffset, TextAlignment textAlignment, bool wrap, bool preserveTrailingSpaces, IEnumerable<string> resultLines, string expectedWrappedText)
		{
			Assert.Equal (maxWidth, text.GetRuneCount () + widthOffset);
			var list = TextFormatter.Format (text, maxWidth, textAlignment, wrap, preserveTrailingSpaces);
			Assert.Equal (list.Count, resultLines.Count ());
			Assert.Equal (resultLines, list);
			string wrappedText = string.Empty;
			foreach (var txt in list) wrappedText += txt;
			Assert.Equal (expectedWrappedText, wrappedText);
		}

		[Fact]
		public void Format_Dont_Throw_ArgumentException_With_WordWrap_As_False_And_Keep_End_Spaces_As_True ()
		{
			var exception = Record.Exception (() => TextFormatter.Format ("Some text", 4, TextAlignment.Left, false, true));
			Assert.Null (exception);
		}

		[Theory]
		[InlineData ("Hello world, how are you today? Pretty neat!", 44, 80, "Hello      world,      how      are      you      today?      Pretty      neat!")]
		public void Format_Justified_Always_Returns_Text_Width_Equal_To_Passed_Width_Horizontal (string text, int runeCount, int maxWidth, string justifiedText)
		{
			Assert.Equal (runeCount, text.GetRuneCount ());

			var fmtText = string.Empty;
			for (int i = text.GetRuneCount (); i < maxWidth; i++) {
				fmtText = TextFormatter.Format (text, i, TextAlignment.Justified, false, true) [0];
				Assert.Equal (i, fmtText.GetRuneCount ());
				var c = fmtText [^1];
				Assert.True (text.EndsWith (c));
			}
			Assert.Equal (justifiedText, fmtText);
		}

		[Theory]
		[InlineData ("Hello world, how are you today? Pretty neat!", 44, 80, "Hello      world,      how      are      you      today?      Pretty      neat!")]
		public void Format_Justified_Always_Returns_Text_Width_Equal_To_Passed_Width_Vertical (string text, int runeCount, int maxWidth, string justifiedText)
		{
			Assert.Equal (runeCount, text.GetRuneCount ());

			var fmtText = string.Empty;
			for (int i = text.GetRuneCount (); i < maxWidth; i++) {
				fmtText = TextFormatter.Format (text, i, TextAlignment.Justified, false, true, 0, TextDirection.TopBottom_LeftRight) [0];
				Assert.Equal (i, fmtText.GetRuneCount ());
				var c = fmtText [^1];
				Assert.True (text.EndsWith (c));
			}
			Assert.Equal (justifiedText, fmtText);
		}

		[Theory]
		[InlineData ("fff", 6, "fff   ")]
		[InlineData ("Hello World", 16, "Hello World     ")]
		public void TestClipOrPad_ShortWord (string text, int fillPad, string expectedText)
		{
			// word is short but we want it to fill # so it should be padded
			Assert.Equal (expectedText, TextFormatter.ClipOrPad (text, fillPad));
		}

		[Theory]
		[InlineData ("123456789", 3, "123")]
		[InlineData ("Hello World", 8, "Hello Wo")]
		public void TestClipOrPad_LongWord (string text, int fillPad, string expectedText)
		{
			// word is long but we want it to fill # space only
			Assert.Equal (expectedText, TextFormatter.ClipOrPad (text, fillPad));
		}

		[Fact]
		public void Internal_Tests ()
		{
			var tf = new TextFormatter ();
			Assert.Equal (Key.Null, tf.HotKey);
			tf.HotKey = Key.CtrlMask | Key.Q;
			Assert.Equal (Key.CtrlMask | Key.Q, tf.HotKey);
		}

		[Theory]
		[InlineData ("Hello World", 11)]
		[InlineData ("こんにちは世界", 14)]
		public void GetColumns_Simple_And_Wide_Runes (string text, int width)
		{
			Assert.Equal (width, text.GetColumns ());
		}

		[Theory]
		[InlineData ("Hello World", 11, 6, 1, 1)]
		[InlineData ("こんにちは 世界", 15, 6, 1, 2)]
		public void GetSumMaxCharWidth_Simple_And_Wide_Runes (string text, int width, int index, int length, int indexWidth)
		{
			Assert.Equal (width, TextFormatter.GetSumMaxCharWidth (text));
			Assert.Equal (indexWidth, TextFormatter.GetSumMaxCharWidth (text, index, length));
		}

		[Theory]
		[InlineData (new string [] { "Hello", "World" }, 2, 1, 1, 1)]
		[InlineData (new string [] { "こんにちは", "世界" }, 4, 1, 1, 2)]
		public void GetSumMaxCharWidth_List_Simple_And_Wide_Runes (IEnumerable<string> text, int width, int index, int length, int indexWidth)
		{
			Assert.Equal (width, TextFormatter.GetSumMaxCharWidth (text.ToList ()));
			Assert.Equal (indexWidth, TextFormatter.GetSumMaxCharWidth (text.ToList (), index, length));
		}

		[Theory]
		[InlineData ("test", 3, 3)]
		[InlineData ("test", 4, 4)]
		[InlineData ("test", 10, 4)]
		public void GetLengthThatFits_Runelist (string text, int columns, int expectedLength)
		{
			var runes = text.ToRuneList ();

			Assert.Equal (expectedLength, TextFormatter.GetLengthThatFits (runes, columns));
		}

		[Theory]
		[InlineData ("test", 3, 3)]
		[InlineData ("test", 4, 4)]
		[InlineData ("test", 10, 4)]
		[InlineData ("test", 1, 1)]
		[InlineData ("test", 0, 0)]
		[InlineData ("test", -1, 0)]
		[InlineData (null, -1, 0)]
		[InlineData ("", -1, 0)]
		public void GetLengthThatFits_String (string text, int columns, int expectedLength)
		{
			Assert.Equal (expectedLength, TextFormatter.GetLengthThatFits (text, columns));
		}

		[Theory]
		[InlineData ("Hello World", 6, 6)]
		[InlineData ("こんにちは 世界", 6, 3)]
		public void GetLengthThatFits_Simple_And_Wide_Runes (string text, int columns, int expectedLength)
		{
			Assert.Equal (expectedLength, TextFormatter.GetLengthThatFits (text, columns));
		}

		[Theory]
		[InlineData ("Hello World", 6, 6)]
		[InlineData ("こんにちは 世界", 6, 3)]
		[MemberData (nameof (CMGlyphs))]
		public void GetLengthThatFits_List_Simple_And_Wide_Runes (string text, int columns, int expectedLength)
		{
			var runes = text.ToRuneList ();
			Assert.Equal (expectedLength, TextFormatter.GetLengthThatFits (runes, columns));
		}

		public static IEnumerable<object []> CMGlyphs =>
			new List<object []>
			{
			    new object[] { $"{CM.Glyphs.LeftBracket} Say Hello 你 {CM.Glyphs.RightBracket}", 16, 15 }
			};

		[Theory]
		[InlineData ("Truncate", 3, "Tru")]
		[InlineData ("デモエムポンズ", 3, "デ")]
		public void Format_Truncate_Simple_And_Wide_Runes (string text, int width, string expected)
		{
			var list = TextFormatter.Format (text, width, false, false);
			Assert.Equal (expected, list [^1]);
		}

		[Theory]
		[MemberData (nameof (FormatEnvironmentNewLine))]
		public void Format_With_PreserveTrailingSpaces_And_Without_PreserveTrailingSpaces (string text, int width, IEnumerable<string> expected)
		{
			var preserveTrailingSpaces = false;
			var formated = TextFormatter.Format (text, width, false, true, preserveTrailingSpaces);
			Assert.Equal (expected, formated);

			preserveTrailingSpaces = true;
			formated = TextFormatter.Format (text, width, false, true, preserveTrailingSpaces);
			Assert.Equal (expected, formated);
		}

		public static IEnumerable<object []> FormatEnvironmentNewLine =>
			new List<object []>
			{
				new object[] { $"Line1{Environment.NewLine}Line2{Environment.NewLine}Line3{Environment.NewLine}", 60, new string [] { "Line1", "Line2", "Line3" } }
			};

		[Theory]
		[MemberData (nameof (SplitEnvironmentNewLine))]
		public void SplitNewLine_Ending__With_Or_Without_NewLine_Probably_CRLF (string text, IEnumerable<string> expected)
		{
			var splited = TextFormatter.SplitNewLine (text);
			Assert.Equal (expected, splited);
		}

		public static IEnumerable<object []> SplitEnvironmentNewLine =>
		new List<object []>
		{
			new object[] { $"First Line 界{Environment.NewLine}Second Line 界{Environment.NewLine}Third Line 界", new string [] { "First Line 界", "Second Line 界", "Third Line 界" } },
			new object[] { $"First Line 界{Environment.NewLine}Second Line 界{Environment.NewLine}Third Line 界{Environment.NewLine}", new string [] { "First Line 界", "Second Line 界", "Third Line 界", "" } }
		};

		[Theory]
		[InlineData ($"First Line 界\nSecond Line 界\nThird Line 界", new string [] { "First Line 界", "Second Line 界", "Third Line 界" })]
		public void SplitNewLine_Ending_Without_NewLine_Only_LF (string text, IEnumerable<string> expected)
		{
			var splited = TextFormatter.SplitNewLine (text);
			Assert.Equal (expected, splited);
		}

		[Theory]
		[InlineData ($"First Line 界\nSecond Line 界\nThird Line 界\n", new string [] { "First Line 界", "Second Line 界", "Third Line 界", "" })]
		public void SplitNewLine_Ending_With_NewLine_Only_LF (string text, IEnumerable<string> expected)
		{
			var splited = TextFormatter.SplitNewLine (text);
			Assert.Equal (expected, splited);
		}

		[Theory]
		[InlineData ("Single Line 界", 14)]
		[InlineData ($"First Line 界\nSecond Line 界\nThird Line 界\n", 14)]
		public void MaxWidthLine_With_And_Without_Newlines (string text, int expected)
		{
			Assert.Equal (expected, TextFormatter.MaxWidthLine (text));
		}

		[Theory]
		[InlineData ("New Test 你", 10, 10, 20320, 20320, 9, "你")]
		[InlineData ("New Test \U0001d539", 10, 11, 120121, 55349, 9, "𝔹")]
		public void String_Array_Is_Not_Always_Equal_ToRunes_Array (string text, int runesLength, int stringLength, int runeValue, int stringValue, int index, string expected)
		{
			var usToRunes = text.ToRunes ();
			Assert.Equal (runesLength, usToRunes.Length);
			Assert.Equal (stringLength, text.Length);
			Assert.Equal (runeValue, usToRunes [index].Value);
			Assert.Equal (stringValue, text [index]);
			Assert.Equal (expected, usToRunes [index].ToString ());
			if (char.IsHighSurrogate (text [index])) {
				// Rune array length isn't equal to string array
				Assert.Equal (expected, new string (new char [] { text [index], text [index + 1] }));
			} else {
				// Rune array length is equal to string array
				Assert.Equal (expected, text [index].ToString ());
			}
		}
	}
}