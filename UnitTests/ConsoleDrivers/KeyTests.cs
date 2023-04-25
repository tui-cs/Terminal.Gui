using System;
using System.Collections;
using System.Collections.Generic;
using Terminal.Gui;
using Xunit;

namespace Terminal.Gui.InputTests {
	public class KeyTests {
		enum SimpleEnum { Zero, One, Two, Three, Four, Five }

		[Flags]
		enum FlaggedEnum { Zero, One, Two, Three, Four, Five }

		enum SimpleHighValueEnum { Zero, One, Two, Three, Four, Last = 0x40000000 }

		[Flags]
		enum FlaggedHighValueEnum { Zero, One, Two, Three, Four, Last = 0x40000000 }

		[Fact]
		public void SimpleEnum_And_FlagedEnum ()
		{
			var simple = SimpleEnum.Three | SimpleEnum.Five;

			// Nothing will not be well compared here.
			Assert.True (simple.HasFlag (SimpleEnum.Zero | SimpleEnum.Five));
			Assert.True (simple.HasFlag (SimpleEnum.One | SimpleEnum.Five));
			Assert.True (simple.HasFlag (SimpleEnum.Two | SimpleEnum.Five));
			Assert.True (simple.HasFlag (SimpleEnum.Three | SimpleEnum.Five));
			Assert.True (simple.HasFlag (SimpleEnum.Four | SimpleEnum.Five));
			Assert.True ((simple & (SimpleEnum.Zero | SimpleEnum.Five)) != 0);
			Assert.True ((simple & (SimpleEnum.One | SimpleEnum.Five)) != 0);
			Assert.True ((simple & (SimpleEnum.Two | SimpleEnum.Five)) != 0);
			Assert.True ((simple & (SimpleEnum.Three | SimpleEnum.Five)) != 0);
			Assert.True ((simple & (SimpleEnum.Four | SimpleEnum.Five)) != 0);
			Assert.Equal (7, (int)simple); // As it is not flagged only shows as number.
			Assert.Equal ("7", simple.ToString ());
			Assert.False (simple == (SimpleEnum.Zero | SimpleEnum.Five));
			Assert.False (simple == (SimpleEnum.One | SimpleEnum.Five));
			Assert.True (simple == (SimpleEnum.Two | SimpleEnum.Five));
			Assert.True (simple == (SimpleEnum.Three | SimpleEnum.Five));
			Assert.False (simple == (SimpleEnum.Four | SimpleEnum.Five));

			var flagged = FlaggedEnum.Three | FlaggedEnum.Five;

			// Nothing will not be well compared here.
			Assert.True (flagged.HasFlag (FlaggedEnum.Zero | FlaggedEnum.Five));
			Assert.True (flagged.HasFlag (FlaggedEnum.One | FlaggedEnum.Five));
			Assert.True (flagged.HasFlag (FlaggedEnum.Two | FlaggedEnum.Five));
			Assert.True (flagged.HasFlag (FlaggedEnum.Three | FlaggedEnum.Five));
			Assert.True (flagged.HasFlag (FlaggedEnum.Four | FlaggedEnum.Five));
			Assert.True ((flagged & (FlaggedEnum.Zero | FlaggedEnum.Five)) != 0);
			Assert.True ((flagged & (FlaggedEnum.One | FlaggedEnum.Five)) != 0);
			Assert.True ((flagged & (FlaggedEnum.Two | FlaggedEnum.Five)) != 0);
			Assert.True ((flagged & (FlaggedEnum.Three | FlaggedEnum.Five)) != 0);
			Assert.True ((flagged & (FlaggedEnum.Four | FlaggedEnum.Five)) != 0);
			Assert.Equal (FlaggedEnum.Two | FlaggedEnum.Five, flagged); // As it is flagged shows as bitwise.
			Assert.Equal ("Two, Five", flagged.ToString ());
			Assert.False (flagged == (FlaggedEnum.Zero | FlaggedEnum.Five));
			Assert.False (flagged == (FlaggedEnum.One | FlaggedEnum.Five));
			Assert.True (flagged == (FlaggedEnum.Two | FlaggedEnum.Five));
			Assert.True (flagged == (FlaggedEnum.Three | FlaggedEnum.Five));
			Assert.False (flagged == (FlaggedEnum.Four | FlaggedEnum.Five));
		}

		[Fact]
		public void SimpleHighValueEnum_And_FlaggedHighValueEnum ()
		{
			var simple = SimpleHighValueEnum.Three | SimpleHighValueEnum.Last;

			// This will not be well compared.
			Assert.True (simple.HasFlag (SimpleHighValueEnum.Zero | SimpleHighValueEnum.Last));
			Assert.True (simple.HasFlag (SimpleHighValueEnum.One | SimpleHighValueEnum.Last));
			Assert.True (simple.HasFlag (SimpleHighValueEnum.Two | SimpleHighValueEnum.Last));
			Assert.True (simple.HasFlag (SimpleHighValueEnum.Three | SimpleHighValueEnum.Last));
			Assert.False (simple.HasFlag (SimpleHighValueEnum.Four | SimpleHighValueEnum.Last));
			Assert.True ((simple & (SimpleHighValueEnum.Zero | SimpleHighValueEnum.Last)) != 0);
			Assert.True ((simple & (SimpleHighValueEnum.One | SimpleHighValueEnum.Last)) != 0);
			Assert.True ((simple & (SimpleHighValueEnum.Two | SimpleHighValueEnum.Last)) != 0);
			Assert.True ((simple & (SimpleHighValueEnum.Three | SimpleHighValueEnum.Last)) != 0);
			Assert.True ((simple & (SimpleHighValueEnum.Four | SimpleHighValueEnum.Last)) != 0);

			// This will be well compared, because the SimpleHighValueEnum.Last have a high value.
			Assert.Equal (1073741827, (int)simple); // As it is not flagged only shows as number.
			Assert.Equal ("1073741827", simple.ToString ()); // As it is not flagged only shows as number.
			Assert.False (simple == (SimpleHighValueEnum.Zero | SimpleHighValueEnum.Last));
			Assert.False (simple == (SimpleHighValueEnum.One | SimpleHighValueEnum.Last));
			Assert.False (simple == (SimpleHighValueEnum.Two | SimpleHighValueEnum.Last));
			Assert.True (simple == (SimpleHighValueEnum.Three | SimpleHighValueEnum.Last));
			Assert.False (simple == (SimpleHighValueEnum.Four | SimpleHighValueEnum.Last));

			var flagged = FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last;

			// This will not be well compared.
			Assert.True (flagged.HasFlag (FlaggedHighValueEnum.Zero | FlaggedHighValueEnum.Last));
			Assert.True (flagged.HasFlag (FlaggedHighValueEnum.One | FlaggedHighValueEnum.Last));
			Assert.True (flagged.HasFlag (FlaggedHighValueEnum.Two | FlaggedHighValueEnum.Last));
			Assert.True (flagged.HasFlag (FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last));
			Assert.False (flagged.HasFlag (FlaggedHighValueEnum.Four | FlaggedHighValueEnum.Last));
			Assert.True ((flagged & (FlaggedHighValueEnum.Zero | FlaggedHighValueEnum.Last)) != 0);
			Assert.True ((flagged & (FlaggedHighValueEnum.One | FlaggedHighValueEnum.Last)) != 0);
			Assert.True ((flagged & (FlaggedHighValueEnum.Two | FlaggedHighValueEnum.Last)) != 0);
			Assert.True ((flagged & (FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last)) != 0);
			Assert.True ((flagged & (FlaggedHighValueEnum.Four | FlaggedHighValueEnum.Last)) != 0);

			// This will be well compared, because the SimpleHighValueEnum.Last have a high value.
			Assert.Equal (FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last, flagged); // As it is flagged shows as bitwise.
			Assert.Equal ("Three, Last", flagged.ToString ()); // As it is flagged shows as bitwise.
			Assert.False (flagged == (FlaggedHighValueEnum.Zero | FlaggedHighValueEnum.Last));
			Assert.False (flagged == (FlaggedHighValueEnum.One | FlaggedHighValueEnum.Last));
			Assert.False (flagged == (FlaggedHighValueEnum.Two | FlaggedHighValueEnum.Last));
			Assert.True (flagged == (FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last));
			Assert.False (flagged == (FlaggedHighValueEnum.Four | FlaggedHighValueEnum.Last));
		}

		[Fact]
		public void Key_Enum_Ambiguity_Check ()
		{
			var key = Key.Y | Key.CtrlMask;

			// This will not be well compared.
			Assert.True (key.HasFlag (Key.Q | Key.CtrlMask));
			Assert.True ((key & (Key.Q | Key.CtrlMask)) != 0);
			Assert.Equal (Key.Y | Key.CtrlMask, key);
			Assert.Equal ("Y, CtrlMask", key.ToString ());

			// This will be well compared, because the Key.CtrlMask have a high value.
			Assert.False (key == Application.QuitKey);
			switch (key) {
			case Key.Q | Key.CtrlMask:
				// Never goes here.
				break;
			case Key.Y | Key.CtrlMask:
				Assert.True (key == (Key.Y | Key.CtrlMask));
				break;
			default:
				// Never goes here.
				break;
			}
		}

		[Fact]
		public void Key_ToString ()
		{
			var k = Key.Y | Key.CtrlMask;
			Assert.Equal ("Y, CtrlMask", k.ToString ());

			k = Key.CtrlMask | Key.Y;
			Assert.Equal ("Y, CtrlMask", k.ToString ());

			k = Key.Space;
			Assert.Equal ("Space", k.ToString ());

			k = Key.Space | Key.D;
			Assert.Equal ("d", k.ToString ());

			k = (Key)'d';
			Assert.Equal ("d", k.ToString ());

			k = Key.d;
			Assert.Equal ("d", k.ToString ());

			k = Key.D;
			Assert.Equal ("D", k.ToString ());

			// In a console this will always returns Key.D
			k = Key.D | Key.ShiftMask;
			Assert.Equal ("D, ShiftMask", k.ToString ());

			// In a console this will always returns Key.D
			k = Key.d | Key.ShiftMask;
			Assert.Equal ("d, ShiftMask", k.ToString ());
		}

		private static object packetLock = new object ();

		/// <summary>
		/// Sometimes when using remote tools EventKeyRecord sends 'virtual keystrokes'.
		/// These are indicated with the wVirtualKeyCode of 231. When we see this code
		/// then we need to look to the unicode character (UnicodeChar) instead of the key
		/// when telling the rest of the framework what button was pressed. For full details
		/// see: https://github.com/gui-cs/Terminal.Gui/issues/2008
		/// </summary>
		[Theory, AutoInitShutdown]
		[ClassData (typeof (PacketTest))]
		public void TestVKPacket (uint unicodeCharacter, bool shift, bool alt, bool control, uint initialVirtualKey, uint initialScanCode, Key expectedRemapping, uint expectedVirtualKey, uint expectedScanCode)
		{
			var modifiers = new ConsoleModifiers ();
			if (shift) modifiers |= ConsoleModifiers.Shift;
			if (alt) modifiers |= ConsoleModifiers.Alt;
			if (control) modifiers |= ConsoleModifiers.Control;
			var mappedConsoleKey = ConsoleKeyMapping.GetConsoleKeyFromKey (unicodeCharacter, modifiers, out uint scanCode, out uint outputChar);

			if ((scanCode > 0 || mappedConsoleKey == 0) && mappedConsoleKey == initialVirtualKey) Assert.Equal (mappedConsoleKey, initialVirtualKey);
			else Assert.Equal (mappedConsoleKey, outputChar < 0xff ? outputChar & 0xff | 0xff << 8 : outputChar);
			Assert.Equal (scanCode, initialScanCode);

			var keyChar = ConsoleKeyMapping.GetKeyCharFromConsoleKey (mappedConsoleKey, modifiers, out uint consoleKey, out scanCode);

			//if (scanCode > 0 && consoleKey == keyChar && consoleKey > 48 && consoleKey > 57 && consoleKey < 65 && consoleKey > 91) {
			if (scanCode > 0 && keyChar == 0 && consoleKey == mappedConsoleKey) Assert.Equal (0, (double)keyChar);
			else Assert.Equal (keyChar, unicodeCharacter);
			Assert.Equal (consoleKey, expectedVirtualKey);
			Assert.Equal (scanCode, expectedScanCode);

			var top = Application.Top;

			top.KeyPress += (s, e) => {
				var after = ShortcutHelper.GetModifiersKey (e.KeyEvent);
				Assert.Equal (expectedRemapping, after);
				e.Handled = true;
				Application.RequestStop ();
			};

			var iterations = -1;

			Application.Iteration += () => {
				iterations++;
				if (iterations == 0) Application.Driver.SendKeys ((char)mappedConsoleKey, ConsoleKey.Packet, shift, alt, control);
			};

			lock (packetLock) {
				Application.Run ();
				Application.Shutdown ();
			}
		}

		public class PacketTest : IEnumerable, IEnumerable<object []> {
			public IEnumerator<object []> GetEnumerator ()
			{
				lock (packetLock) {
					yield return new object [] { 'a', false, false, false, 'A', 30, Key.a, 'A', 30 };
					yield return new object [] { 'A', true, false, false, 'A', 30, Key.A | Key.ShiftMask, 'A', 30 };
					yield return new object [] { 'A', true, true, false, 'A', 30, Key.A | Key.ShiftMask | Key.AltMask, 'A', 30 };
					yield return new object [] { 'A', true, true, true, 'A', 30, Key.A | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 'A', 30 };
					yield return new object [] { 'z', false, false, false, 'Z', 44, Key.z, 'Z', 44 };
					yield return new object [] { 'Z', true, false, false, 'Z', 44, Key.Z | Key.ShiftMask, 'Z', 44 };
					yield return new object [] { 'Z', true, true, false, 'Z', 44, Key.Z | Key.ShiftMask | Key.AltMask, 'Z', 44 };
					yield return new object [] { 'Z', true, true, true, 'Z', 44, Key.Z | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 'Z', 44 };
					yield return new object [] { '英', false, false, false, '\0', 0, (Key)'英', '\0', 0 };
					yield return new object [] { '英', true, false, false, '\0', 0, (Key)'英' | Key.ShiftMask, '\0', 0 };
					yield return new object [] { '英', true, true, false, '\0', 0, (Key)'英' | Key.ShiftMask | Key.AltMask, '\0', 0 };
					yield return new object [] { '英', true, true, true, '\0', 0, (Key)'英' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '\0', 0 };
					yield return new object [] { '+', false, false, false, 187, 26, (Key)'+', 187, 26 };
					yield return new object [] { '*', true, false, false, 187, 26, (Key)'*' | Key.ShiftMask, 187, 26 };
					yield return new object [] { '+', true, true, false, 187, 26, (Key)'+' | Key.ShiftMask | Key.AltMask, 187, 26 };
					yield return new object [] { '+', true, true, true, 187, 26, (Key)'+' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 187, 26 };
					yield return new object [] { '1', false, false, false, '1', 2, Key.D1, '1', 2 };
					yield return new object [] { '!', true, false, false, '1', 2, (Key)'!' | Key.ShiftMask, '1', 2 };
					yield return new object [] { '1', true, true, false, '1', 2, Key.D1 | Key.ShiftMask | Key.AltMask, '1', 2 };
					yield return new object [] { '1', true, true, true, '1', 2, Key.D1 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '1', 2 };
					yield return new object [] { '1', false, true, true, '1', 2, Key.D1 | Key.AltMask | Key.CtrlMask, '1', 2 };
					yield return new object [] { '2', false, false, false, '2', 3, Key.D2, '2', 3 };
					yield return new object [] { '"', true, false, false, '2', 3, (Key)'"' | Key.ShiftMask, '2', 3 };
					yield return new object [] { '2', true, true, false, '2', 3, Key.D2 | Key.ShiftMask | Key.AltMask, '2', 3 };
					yield return new object [] { '2', true, true, true, '2', 3, Key.D2 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '2', 3 };
					yield return new object [] { '@', false, true, true, '2', 3, (Key)'@' | Key.AltMask | Key.CtrlMask, '2', 3 };
					yield return new object [] { '3', false, false, false, '3', 4, Key.D3, '3', 4 };
					yield return new object [] { '#', true, false, false, '3', 4, (Key)'#' | Key.ShiftMask, '3', 4 };
					yield return new object [] { '3', true, true, false, '3', 4, Key.D3 | Key.ShiftMask | Key.AltMask, '3', 4 };
					yield return new object [] { '3', true, true, true, '3', 4, Key.D3 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '3', 4 };
					yield return new object [] { '£', false, true, true, '3', 4, (Key)'£' | Key.AltMask | Key.CtrlMask, '3', 4 };
					yield return new object [] { '4', false, false, false, '4', 5, Key.D4, '4', 5 };
					yield return new object [] { '$', true, false, false, '4', 5, (Key)'$' | Key.ShiftMask, '4', 5 };
					yield return new object [] { '4', true, true, false, '4', 5, Key.D4 | Key.ShiftMask | Key.AltMask, '4', 5 };
					yield return new object [] { '4', true, true, true, '4', 5, Key.D4 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '4', 5 };
					yield return new object [] { '§', false, true, true, '4', 5, (Key)'§' | Key.AltMask | Key.CtrlMask, '4', 5 };
					yield return new object [] { '5', false, false, false, '5', 6, Key.D5, '5', 6 };
					yield return new object [] { '%', true, false, false, '5', 6, (Key)'%' | Key.ShiftMask, '5', 6 };
					yield return new object [] { '5', true, true, false, '5', 6, Key.D5 | Key.ShiftMask | Key.AltMask, '5', 6 };
					yield return new object [] { '5', true, true, true, '5', 6, Key.D5 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '5', 6 };
					yield return new object [] { '€', false, true, true, '5', 6, (Key)'€' | Key.AltMask | Key.CtrlMask, '5', 6 };
					yield return new object [] { '6', false, false, false, '6', 7, Key.D6, '6', 7 };
					yield return new object [] { '&', true, false, false, '6', 7, (Key)'&' | Key.ShiftMask, '6', 7 };
					yield return new object [] { '6', true, true, false, '6', 7, Key.D6 | Key.ShiftMask | Key.AltMask, '6', 7 };
					yield return new object [] { '6', true, true, true, '6', 7, Key.D6 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '6', 7 };
					yield return new object [] { '6', false, true, true, '6', 7, Key.D6 | Key.AltMask | Key.CtrlMask, '6', 7 };
					yield return new object [] { '7', false, false, false, '7', 8, Key.D7, '7', 8 };
					yield return new object [] { '/', true, false, false, '7', 8, (Key)'/' | Key.ShiftMask, '7', 8 };
					yield return new object [] { '7', true, true, false, '7', 8, Key.D7 | Key.ShiftMask | Key.AltMask, '7', 8 };
					yield return new object [] { '7', true, true, true, '7', 8, Key.D7 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '7', 8 };
					yield return new object [] { '{', false, true, true, '7', 8, (Key)'{' | Key.AltMask | Key.CtrlMask, '7', 8 };
					yield return new object [] { '8', false, false, false, '8', 9, Key.D8, '8', 9 };
					yield return new object [] { '(', true, false, false, '8', 9, (Key)'(' | Key.ShiftMask, '8', 9 };
					yield return new object [] { '8', true, true, false, '8', 9, Key.D8 | Key.ShiftMask | Key.AltMask, '8', 9 };
					yield return new object [] { '8', true, true, true, '8', 9, Key.D8 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '8', 9 };
					yield return new object [] { '[', false, true, true, '8', 9, (Key)'[' | Key.AltMask | Key.CtrlMask, '8', 9 };
					yield return new object [] { '9', false, false, false, '9', 10, Key.D9, '9', 10 };
					yield return new object [] { ')', true, false, false, '9', 10, (Key)')' | Key.ShiftMask, '9', 10 };
					yield return new object [] { '9', true, true, false, '9', 10, Key.D9 | Key.ShiftMask | Key.AltMask, '9', 10 };
					yield return new object [] { '9', true, true, true, '9', 10, Key.D9 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '9', 10 };
					yield return new object [] { ']', false, true, true, '9', 10, (Key)']' | Key.AltMask | Key.CtrlMask, '9', 10 };
					yield return new object [] { '0', false, false, false, '0', 11, Key.D0, '0', 11 };
					yield return new object [] { '=', true, false, false, '0', 11, (Key)'=' | Key.ShiftMask, '0', 11 };
					yield return new object [] { '0', true, true, false, '0', 11, Key.D0 | Key.ShiftMask | Key.AltMask, '0', 11 };
					yield return new object [] { '0', true, true, true, '0', 11, Key.D0 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '0', 11 };
					yield return new object [] { '}', false, true, true, '0', 11, (Key)'}' | Key.AltMask | Key.CtrlMask, '0', 11 };
					yield return new object [] { '\'', false, false, false, 219, 12, (Key)'\'', 219, 12 };
					yield return new object [] { '?', true, false, false, 219, 12, (Key)'?' | Key.ShiftMask, 219, 12 };
					yield return new object [] { '\'', true, true, false, 219, 12, (Key)'\'' | Key.ShiftMask | Key.AltMask, 219, 12 };
					yield return new object [] { '\'', true, true, true, 219, 12, (Key)'\'' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 219, 12 };
					yield return new object [] { '«', false, false, false, 221, 13, (Key)'«', 221, 13 };
					yield return new object [] { '»', true, false, false, 221, 13, (Key)'»' | Key.ShiftMask, 221, 13 };
					yield return new object [] { '«', true, true, false, 221, 13, (Key)'«' | Key.ShiftMask | Key.AltMask, 221, 13 };
					yield return new object [] { '«', true, true, true, 221, 13, (Key)'«' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 221, 13 };
					yield return new object [] { 'á', false, false, false, 'á', 0, (Key)'á', 'A', 30 };
					yield return new object [] { 'Á', true, false, false, 'Á', 0, (Key)'Á' | Key.ShiftMask, 'A', 30 };
					yield return new object [] { 'à', false, false, false, 'à', 0, (Key)'à', 'A', 30 };
					yield return new object [] { 'À', true, false, false, 'À', 0, (Key)'À' | Key.ShiftMask, 'A', 30 };
					yield return new object [] { 'é', false, false, false, 'é', 0, (Key)'é', 'E', 18 };
					yield return new object [] { 'É', true, false, false, 'É', 0, (Key)'É' | Key.ShiftMask, 'E', 18 };
					yield return new object [] { 'è', false, false, false, 'è', 0, (Key)'è', 'E', 18 };
					yield return new object [] { 'È', true, false, false, 'È', 0, (Key)'È' | Key.ShiftMask, 'E', 18 };
					yield return new object [] { 'í', false, false, false, 'í', 0, (Key)'í', 'I', 23 };
					yield return new object [] { 'Í', true, false, false, 'Í', 0, (Key)'Í' | Key.ShiftMask, 'I', 23 };
					yield return new object [] { 'ì', false, false, false, 'ì', 0, (Key)'ì', 'I', 23 };
					yield return new object [] { 'Ì', true, false, false, 'Ì', 0, (Key)'Ì' | Key.ShiftMask, 'I', 23 };
					yield return new object [] { 'ó', false, false, false, 'ó', 0, (Key)'ó', 'O', 24 };
					yield return new object [] { 'Ó', true, false, false, 'Ó', 0, (Key)'Ó' | Key.ShiftMask, 'O', 24 };
					yield return new object [] { 'ò', false, false, false, 'Ó', 0, (Key)'ò', 'O', 24 };
					yield return new object [] { 'Ò', true, false, false, 'Ò', 0, (Key)'Ò' | Key.ShiftMask, 'O', 24 };
					yield return new object [] { 'ú', false, false, false, 'ú', 0, (Key)'ú', 'U', 22 };
					yield return new object [] { 'Ú', true, false, false, 'Ú', 0, (Key)'Ú' | Key.ShiftMask, 'U', 22 };
					yield return new object [] { 'ù', false, false, false, 'ù', 0, (Key)'ù', 'U', 22 };
					yield return new object [] { 'Ù', true, false, false, 'Ù', 0, (Key)'Ù' | Key.ShiftMask, 'U', 22 };
					yield return new object [] { 'ö', false, false, false, 'ó', 0, (Key)'ö', 'O', 24 };
					yield return new object [] { 'Ö', true, false, false, 'Ó', 0, (Key)'Ö' | Key.ShiftMask, 'O', 24 };
					yield return new object [] { '<', false, false, false, 226, 86, (Key)'<', 226, 86 };
					yield return new object [] { '>', true, false, false, 226, 86, (Key)'>' | Key.ShiftMask, 226, 86 };
					yield return new object [] { '<', true, true, false, 226, 86, (Key)'<' | Key.ShiftMask | Key.AltMask, 226, 86 };
					yield return new object [] { '<', true, true, true, 226, 86, (Key)'<' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 226, 86 };
					yield return new object [] { 'ç', false, false, false, 192, 39, (Key)'ç', 192, 39 };
					yield return new object [] { 'Ç', true, false, false, 192, 39, (Key)'Ç' | Key.ShiftMask, 192, 39 };
					yield return new object [] { 'ç', true, true, false, 192, 39, (Key)'ç' | Key.ShiftMask | Key.AltMask, 192, 39 };
					yield return new object [] { 'ç', true, true, true, 192, 39, (Key)'ç' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 192, 39 };
					yield return new object [] { '¨', false, true, true, 187, 26, (Key)'¨' | Key.AltMask | Key.CtrlMask, 187, 26 };
					yield return new object [] { (uint)Key.PageUp, false, false, false, 33, 73, Key.PageUp, 33, 73 };
					yield return new object [] { (uint)Key.PageUp, true, false, false, 33, 73, Key.PageUp | Key.ShiftMask, 33, 73 };
					yield return new object [] { (uint)Key.PageUp, true, true, false, 33, 73, Key.PageUp | Key.ShiftMask | Key.AltMask, 33, 73 };
					yield return new object [] { (uint)Key.PageUp, true, true, true, 33, 73, Key.PageUp | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 33, 73 };
				}
			}

			IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
		}
	}
}