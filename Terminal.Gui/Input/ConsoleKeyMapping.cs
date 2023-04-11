using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Helper class to handle the scan code and virtual key from a <see cref="ConsoleKey"/>.
	/// </summary>
	public static class ConsoleKeyMapping {
		private class ScanCodeMapping : IEquatable<ScanCodeMapping> {
			public uint ScanCode;
			public uint VirtualKey;
			public ConsoleModifiers Modifiers;
			public uint UnicodeChar;

			public ScanCodeMapping (uint scanCode, uint virtualKey, ConsoleModifiers modifiers, uint unicodeChar)
			{
				ScanCode = scanCode;
				VirtualKey = virtualKey;
				Modifiers = modifiers;
				UnicodeChar = unicodeChar;
			}

			public bool Equals (ScanCodeMapping other)
			{
				return (this.ScanCode.Equals (other.ScanCode) &&
					this.VirtualKey.Equals (other.VirtualKey) &&
					this.Modifiers.Equals (other.Modifiers) &&
					this.UnicodeChar.Equals (other.UnicodeChar));
			}
		}

		private static ConsoleModifiers GetModifiers (uint unicodeChar, ConsoleModifiers modifiers, bool isConsoleKey)
		{
			if (modifiers.HasFlag (ConsoleModifiers.Shift) &&
				!modifiers.HasFlag (ConsoleModifiers.Alt) &&
				!modifiers.HasFlag (ConsoleModifiers.Control)) {

				return ConsoleModifiers.Shift;
			} else if (modifiers == (ConsoleModifiers.Alt | ConsoleModifiers.Control)) {
				return modifiers;
			} else if ((!isConsoleKey || (isConsoleKey && (modifiers.HasFlag (ConsoleModifiers.Shift) ||
				modifiers.HasFlag (ConsoleModifiers.Alt) || modifiers.HasFlag (ConsoleModifiers.Control)))) &&
				unicodeChar >= 65 && unicodeChar <= 90) {

				return ConsoleModifiers.Shift;
			}
			return 0;
		}

		private static ScanCodeMapping GetScanCode (string propName, uint keyValue, ConsoleModifiers modifiers)
		{
			switch (propName) {
			case "UnicodeChar":
				var sCode = scanCodes.FirstOrDefault ((e) => e.UnicodeChar == keyValue && e.Modifiers == modifiers);
				if (sCode == null && modifiers == (ConsoleModifiers.Alt | ConsoleModifiers.Control)) {
					return scanCodes.FirstOrDefault ((e) => e.UnicodeChar == keyValue && e.Modifiers == 0);
				}
				return sCode;
			case "VirtualKey":
				sCode = scanCodes.FirstOrDefault ((e) => e.VirtualKey == keyValue && e.Modifiers == modifiers);
				if (sCode == null && modifiers == (ConsoleModifiers.Alt | ConsoleModifiers.Control)) {
					return scanCodes.FirstOrDefault ((e) => e.VirtualKey == keyValue && e.Modifiers == 0);
				}
				return sCode;
			}
			
			return null;
		}

		/// <summary>
		/// Gets the <see cref="ConsoleKey"/> from the provided <see cref="Key"/>.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static ConsoleKey GetConsoleKeyFromKey (Key key)
		{
			ConsoleModifiers mod = new ConsoleModifiers ();
			if (key.HasFlag (Key.ShiftMask)) {
				mod |= ConsoleModifiers.Shift;
			}
			if (key.HasFlag (Key.AltMask)) {
				mod |= ConsoleModifiers.Alt;
			}
			if (key.HasFlag (Key.CtrlMask)) {
				mod |= ConsoleModifiers.Control;
			}
			return (ConsoleKey)ConsoleKeyMapping.GetConsoleKeyFromKey ((uint)(key & ~Key.CtrlMask & ~Key.ShiftMask & ~Key.AltMask), mod, out _, out _);
		}

		/// <summary>
		/// Get the <see cref="ConsoleKey"/> from a <see cref="Key"/>.
		/// </summary>
		/// <param name="keyValue">The key value.</param>
		/// <param name="modifiers">The modifiers keys.</param>
		/// <param name="scanCode">The resulting scan code.</param>
		/// <param name="outputChar">The resulting output character.</param>
		/// <returns>The <see cref="ConsoleKey"/> or the <paramref name="outputChar"/>.</returns>
		public static uint GetConsoleKeyFromKey (uint keyValue, ConsoleModifiers modifiers, out uint scanCode, out uint outputChar)
		{
			scanCode = 0;
			outputChar = keyValue;
			if (keyValue == 0) {
				return 0;
			}

			uint consoleKey = MapKeyToConsoleKey (keyValue, out bool mappable);
			if (mappable) {
				var mod = GetModifiers (keyValue, modifiers, false);
				var scode = GetScanCode ("UnicodeChar", keyValue, mod);
				if (scode != null) {
					consoleKey = scode.VirtualKey;
					scanCode = scode.ScanCode;
					outputChar = scode.UnicodeChar;
				} else {
					consoleKey = consoleKey < 0xff ? (uint)(consoleKey & 0xff | 0xff << 8) : consoleKey;
				}
			} else {
				var mod = GetModifiers (keyValue, modifiers, false);
				var scode = GetScanCode ("VirtualKey", consoleKey, mod);
				if (scode != null) {
					consoleKey = scode.VirtualKey;
					scanCode = scode.ScanCode;
					outputChar = scode.UnicodeChar;
				}
			}

			return consoleKey;
		}

		/// <summary>
		/// Get the output character from the <see cref="ConsoleKey"/>.
		/// </summary>
		/// <param name="unicodeChar">The unicode character.</param>
		/// <param name="modifiers">The modifiers keys.</param>
		/// <param name="consoleKey">The resulting console key.</param>
		/// <param name="scanCode">The resulting scan code.</param>
		/// <returns>The output character or the <paramref name="consoleKey"/>.</returns>
		public static uint GetKeyCharFromConsoleKey (uint unicodeChar, ConsoleModifiers modifiers, out uint consoleKey, out uint scanCode)
		{
			uint decodedChar = unicodeChar >> 8 == 0xff ? unicodeChar & 0xff : unicodeChar;
			uint keyChar = decodedChar;
			consoleKey = 0;
			var mod = GetModifiers (decodedChar, modifiers, true);
			scanCode = 0;
			var scode = unicodeChar != 0 && unicodeChar >> 8 != 0xff ? GetScanCode ("VirtualKey", decodedChar, mod) : null;
			if (scode != null) {
				consoleKey = scode.VirtualKey;
				keyChar = scode.UnicodeChar;
				scanCode = scode.ScanCode;
			}
			if (scode == null) {
				scode = unicodeChar != 0 ? GetScanCode ("UnicodeChar", decodedChar, mod) : null;
				if (scode != null) {
					consoleKey = scode.VirtualKey;
					keyChar = scode.UnicodeChar;
					scanCode = scode.ScanCode;
				}
			}
			if (decodedChar != 0 && scanCode == 0 && char.IsLetter ((char)decodedChar)) {
				string stFormD = ((char)decodedChar).ToString ().Normalize (System.Text.NormalizationForm.FormD);
				for (int i = 0; i < stFormD.Length; i++) {
					UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory (stFormD [i]);
					if (uc != UnicodeCategory.NonSpacingMark && uc != UnicodeCategory.OtherLetter) {
						consoleKey = char.ToUpper (stFormD [i]);
						scode = GetScanCode ("VirtualKey", char.ToUpper (stFormD [i]), 0);
						if (scode != null) {
							scanCode = scode.ScanCode;
						}
					}
				}
			}

			return keyChar;
		}

		/// <summary>
		/// Maps a <see cref="Key"/> to a <see cref="ConsoleKey"/>.
		/// </summary>
		/// <param name="keyValue">The key value.</param>
		/// <param name="isMappable">If <see langword="true"/> is mapped to a valid character, otherwise <see langword="false"/>.</param>
		/// <returns>The <see cref="ConsoleKey"/> or the <paramref name="keyValue"/>.</returns>
		public static uint MapKeyToConsoleKey (uint keyValue, out bool isMappable)
		{
			isMappable = false;

			switch ((Key)keyValue) {
			case Key.Delete:
				return (uint)ConsoleKey.Delete;
			case Key.CursorUp:
				return (uint)ConsoleKey.UpArrow;
			case Key.CursorDown:
				return (uint)ConsoleKey.DownArrow;
			case Key.CursorLeft:
				return (uint)ConsoleKey.LeftArrow;
			case Key.CursorRight:
				return (uint)ConsoleKey.RightArrow;
			case Key.PageUp:
				return (uint)ConsoleKey.PageUp;
			case Key.PageDown:
				return (uint)ConsoleKey.PageDown;
			case Key.Home:
				return (uint)ConsoleKey.Home;
			case Key.End:
				return (uint)ConsoleKey.End;
			case Key.InsertChar:
				return (uint)ConsoleKey.Insert;
			case Key.DeleteChar:
				return (uint)ConsoleKey.Delete;
			case Key.F1:
				return (uint)ConsoleKey.F1;
			case Key.F2:
				return (uint)ConsoleKey.F2;
			case Key.F3:
				return (uint)ConsoleKey.F3;
			case Key.F4:
				return (uint)ConsoleKey.F4;
			case Key.F5:
				return (uint)ConsoleKey.F5;
			case Key.F6:
				return (uint)ConsoleKey.F6;
			case Key.F7:
				return (uint)ConsoleKey.F7;
			case Key.F8:
				return (uint)ConsoleKey.F8;
			case Key.F9:
				return (uint)ConsoleKey.F9;
			case Key.F10:
				return (uint)ConsoleKey.F10;
			case Key.F11:
				return (uint)ConsoleKey.F11;
			case Key.F12:
				return (uint)ConsoleKey.F12;
			case Key.F13:
				return (uint)ConsoleKey.F13;
			case Key.F14:
				return (uint)ConsoleKey.F14;
			case Key.F15:
				return (uint)ConsoleKey.F15;
			case Key.F16:
				return (uint)ConsoleKey.F16;
			case Key.F17:
				return (uint)ConsoleKey.F17;
			case Key.F18:
				return (uint)ConsoleKey.F18;
			case Key.F19:
				return (uint)ConsoleKey.F19;
			case Key.F20:
				return (uint)ConsoleKey.F20;
			case Key.F21:
				return (uint)ConsoleKey.F21;
			case Key.F22:
				return (uint)ConsoleKey.F22;
			case Key.F23:
				return (uint)ConsoleKey.F23;
			case Key.F24:
				return (uint)ConsoleKey.F24;
			case Key.BackTab:
				return (uint)ConsoleKey.Tab;
			case Key.Unknown:
				isMappable = true;
				return 0;
			}
			isMappable = true;

			return keyValue;
		}

		/// <summary>
		/// Maps a <see cref="ConsoleKey"/> to a <see cref="Key"/>.
		/// </summary>
		/// <param name="consoleKey">The console key.</param>
		/// <param name="isMappable">If <see langword="true"/> is mapped to a valid character, otherwise <see langword="false"/>.</param>
		/// <returns>The <see cref="Key"/> or the <paramref name="consoleKey"/>.</returns>
		public static Key MapConsoleKeyToKey (ConsoleKey consoleKey, out bool isMappable)
		{
			isMappable = false;

			switch (consoleKey) {
			case ConsoleKey.Delete:
				return Key.Delete;
			case ConsoleKey.UpArrow:
				return Key.CursorUp;
			case ConsoleKey.DownArrow:
				return Key.CursorDown;
			case ConsoleKey.LeftArrow:
				return Key.CursorLeft;
			case ConsoleKey.RightArrow:
				return Key.CursorRight;
			case ConsoleKey.PageUp:
				return Key.PageUp;
			case ConsoleKey.PageDown:
				return Key.PageDown;
			case ConsoleKey.Home:
				return Key.Home;
			case ConsoleKey.End:
				return Key.End;
			case ConsoleKey.Insert:
				return Key.InsertChar;
			case ConsoleKey.F1:
				return Key.F1;
			case ConsoleKey.F2:
				return Key.F2;
			case ConsoleKey.F3:
				return Key.F3;
			case ConsoleKey.F4:
				return Key.F4;
			case ConsoleKey.F5:
				return Key.F5;
			case ConsoleKey.F6:
				return Key.F6;
			case ConsoleKey.F7:
				return Key.F7;
			case ConsoleKey.F8:
				return Key.F8;
			case ConsoleKey.F9:
				return Key.F9;
			case ConsoleKey.F10:
				return Key.F10;
			case ConsoleKey.F11:
				return Key.F11;
			case ConsoleKey.F12:
				return Key.F12;
			case ConsoleKey.F13:
				return Key.F13;
			case ConsoleKey.F14:
				return Key.F14;
			case ConsoleKey.F15:
				return Key.F15;
			case ConsoleKey.F16:
				return Key.F16;
			case ConsoleKey.F17:
				return Key.F17;
			case ConsoleKey.F18:
				return Key.F18;
			case ConsoleKey.F19:
				return Key.F19;
			case ConsoleKey.F20:
				return Key.F20;
			case ConsoleKey.F21:
				return Key.F21;
			case ConsoleKey.F22:
				return Key.F22;
			case ConsoleKey.F23:
				return Key.F23;
			case ConsoleKey.F24:
				return Key.F24;
			case ConsoleKey.Tab:
				return Key.BackTab;
			}
			isMappable = true;

			return (Key)consoleKey;
		}

		/// <summary>
		/// Maps a <see cref="ConsoleKeyInfo"/> to a <see cref="Key"/>.
		/// </summary>
		/// <param name="keyInfo">The console key info.</param>
		/// <param name="key">The key.</param>
		/// <returns>The <see cref="Key"/> with <see cref="ConsoleModifiers"/> or the <paramref name="key"/></returns>
		public static Key MapKeyModifiers (ConsoleKeyInfo keyInfo, Key key)
		{
			Key keyMod = new Key ();
			if ((keyInfo.Modifiers & ConsoleModifiers.Shift) != 0)
				keyMod = Key.ShiftMask;
			if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
				keyMod |= Key.CtrlMask;
			if ((keyInfo.Modifiers & ConsoleModifiers.Alt) != 0)
				keyMod |= Key.AltMask;

			return keyMod != Key.Null ? keyMod | key : key;
		}

		private static HashSet<ScanCodeMapping> scanCodes = new HashSet<ScanCodeMapping> {
			new ScanCodeMapping (1,27,0,27),	// Escape
			new ScanCodeMapping (1,27,ConsoleModifiers.Shift,27),
			new ScanCodeMapping (2,49,0,49),	// D1
			new ScanCodeMapping (2,49,ConsoleModifiers.Shift,33),
			new ScanCodeMapping (3,50,0,50),	// D2
			new ScanCodeMapping (3,50,ConsoleModifiers.Shift,34),
			new ScanCodeMapping (3,50,ConsoleModifiers.Alt | ConsoleModifiers.Control,64),
			new ScanCodeMapping (4,51,0,51),	// D3
			new ScanCodeMapping (4,51,ConsoleModifiers.Shift,35),
			new ScanCodeMapping (4,51,ConsoleModifiers.Alt | ConsoleModifiers.Control,163),
			new ScanCodeMapping (5,52,0,52),	// D4
			new ScanCodeMapping (5,52,ConsoleModifiers.Shift,36),
			new ScanCodeMapping (5,52,ConsoleModifiers.Alt | ConsoleModifiers.Control,167),
			new ScanCodeMapping (6,53,0,53),	// D5
			new ScanCodeMapping (6,53,ConsoleModifiers.Shift,37),
			new ScanCodeMapping (6,53,ConsoleModifiers.Alt | ConsoleModifiers.Control,8364),
			new ScanCodeMapping (7,54,0,54),	// D6
			new ScanCodeMapping (7,54,ConsoleModifiers.Shift,38),
			new ScanCodeMapping (8,55,0,55),	// D7
			new ScanCodeMapping (8,55,ConsoleModifiers.Shift,47),
			new ScanCodeMapping (8,55,ConsoleModifiers.Alt | ConsoleModifiers.Control,123),
			new ScanCodeMapping (9,56,0,56),	// D8
			new ScanCodeMapping (9,56,ConsoleModifiers.Shift,40),
			new ScanCodeMapping (9,56,ConsoleModifiers.Alt | ConsoleModifiers.Control,91),
			new ScanCodeMapping (10,57,0,57),	// D9
			new ScanCodeMapping (10,57,ConsoleModifiers.Shift,41),
			new ScanCodeMapping (10,57,ConsoleModifiers.Alt | ConsoleModifiers.Control,93),
			new ScanCodeMapping (11,48,0,48),	// D0
			new ScanCodeMapping (11,48,ConsoleModifiers.Shift,61),
			new ScanCodeMapping (11,48,ConsoleModifiers.Alt | ConsoleModifiers.Control,125),
			new ScanCodeMapping (12,219,0,39),	// Oem4
			new ScanCodeMapping (12,219,ConsoleModifiers.Shift,63),
			new ScanCodeMapping (13,221,0,171),	// Oem6
			new ScanCodeMapping (13,221,ConsoleModifiers.Shift,187),
			new ScanCodeMapping (14,8,0,8),		// Backspace
			new ScanCodeMapping (14,8,ConsoleModifiers.Shift,8),
			new ScanCodeMapping (15,9,0,9),		// Tab
			new ScanCodeMapping (15,9,ConsoleModifiers.Shift,15),
			new ScanCodeMapping (16,81,0,113),	// Q
			new ScanCodeMapping (16,81,ConsoleModifiers.Shift,81),
			new ScanCodeMapping (17,87,0,119),	// W
			new ScanCodeMapping (17,87,ConsoleModifiers.Shift,87),
			new ScanCodeMapping (18,69,0,101),	// E
			new ScanCodeMapping (18,69,ConsoleModifiers.Shift,69),
			new ScanCodeMapping (19,82,0,114),	// R
			new ScanCodeMapping (19,82,ConsoleModifiers.Shift,82),
			new ScanCodeMapping (20,84,0,116),	// T
			new ScanCodeMapping (20,84,ConsoleModifiers.Shift,84),
			new ScanCodeMapping (21,89,0,121),	// Y
			new ScanCodeMapping (21,89,ConsoleModifiers.Shift,89),
			new ScanCodeMapping (22,85,0,117),	// U
			new ScanCodeMapping (22,85,ConsoleModifiers.Shift,85),
			new ScanCodeMapping (23,73,0,105),	// I
			new ScanCodeMapping (23,73,ConsoleModifiers.Shift,73),
			new ScanCodeMapping (24,79,0,111),	// O
			new ScanCodeMapping (24,79,ConsoleModifiers.Shift,79),
			new ScanCodeMapping (25,80,0,112),	// P
			new ScanCodeMapping (25,80,ConsoleModifiers.Shift,80),
			new ScanCodeMapping (26,187,0,43),	// OemPlus
			new ScanCodeMapping (26,187,ConsoleModifiers.Shift,42),
			new ScanCodeMapping (26,187,ConsoleModifiers.Alt | ConsoleModifiers.Control,168),
			new ScanCodeMapping (27,186,0,180),	// Oem1
			new ScanCodeMapping (27,186,ConsoleModifiers.Shift,96),
			new ScanCodeMapping (28,13,0,13),	// Enter
			new ScanCodeMapping (28,13,ConsoleModifiers.Shift,13),
			new ScanCodeMapping (29,17,0,0),	// Control
			new ScanCodeMapping (29,17,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (30,65,0,97),	// A
			new ScanCodeMapping (30,65,ConsoleModifiers.Shift,65),
			new ScanCodeMapping (31,83,0,115),	// S
			new ScanCodeMapping (31,83,ConsoleModifiers.Shift,83),
			new ScanCodeMapping (32,68,0,100),	// D
			new ScanCodeMapping (32,68,ConsoleModifiers.Shift,68),
			new ScanCodeMapping (33,70,0,102),	// F
			new ScanCodeMapping (33,70,ConsoleModifiers.Shift,70),
			new ScanCodeMapping (34,71,0,103),	// G
			new ScanCodeMapping (34,71,ConsoleModifiers.Shift,71),
			new ScanCodeMapping (35,72,0,104),	// H
			new ScanCodeMapping (35,72,ConsoleModifiers.Shift,72),
			new ScanCodeMapping (36,74,0,106),	// J
			new ScanCodeMapping (36,74,ConsoleModifiers.Shift,74),
			new ScanCodeMapping (37,75,0,107),	// K
			new ScanCodeMapping (37,75,ConsoleModifiers.Shift,75),
			new ScanCodeMapping (38,76,0,108),	// L
			new ScanCodeMapping (38,76,ConsoleModifiers.Shift,76),
			new ScanCodeMapping (39,192,0,231),	// Oem3
			new ScanCodeMapping (39,192,ConsoleModifiers.Shift,199),
			new ScanCodeMapping (40,222,0,186),	// Oem7
			new ScanCodeMapping (40,222,ConsoleModifiers.Shift,170),
			new ScanCodeMapping (41,220,0,92),	// Oem5
			new ScanCodeMapping (41,220,ConsoleModifiers.Shift,124),
			new ScanCodeMapping (42,16,0,0),	// LShift
			new ScanCodeMapping (42,16,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (43,191,0,126),	// Oem2
			new ScanCodeMapping (43,191,ConsoleModifiers.Shift,94),
			new ScanCodeMapping (44,90,0,122),	// Z
			new ScanCodeMapping (44,90,ConsoleModifiers.Shift,90),
			new ScanCodeMapping (45,88,0,120),	// X
			new ScanCodeMapping (45,88,ConsoleModifiers.Shift,88),
			new ScanCodeMapping (46,67,0,99),	// C
			new ScanCodeMapping (46,67,ConsoleModifiers.Shift,67),
			new ScanCodeMapping (47,86,0,118),	// V
			new ScanCodeMapping (47,86,ConsoleModifiers.Shift,86),
			new ScanCodeMapping (48,66,0,98),	// B
			new ScanCodeMapping (48,66,ConsoleModifiers.Shift,66),
			new ScanCodeMapping (49,78,0,110),	// N
			new ScanCodeMapping (49,78,ConsoleModifiers.Shift,78),
			new ScanCodeMapping (50,77,0,109),	// M
			new ScanCodeMapping (50,77,ConsoleModifiers.Shift,77),
			new ScanCodeMapping (51,188,0,44),	// OemComma
			new ScanCodeMapping (51,188,ConsoleModifiers.Shift,59),
			new ScanCodeMapping (52,190,0,46),	// OemPeriod
			new ScanCodeMapping (52,190,ConsoleModifiers.Shift,58),
			new ScanCodeMapping (53,189,0,45),	// OemMinus
			new ScanCodeMapping (53,189,ConsoleModifiers.Shift,95),
			new ScanCodeMapping (54,16,0,0),	// RShift
			new ScanCodeMapping (54,16,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (55,44,0,0),	// PrintScreen
			new ScanCodeMapping (55,44,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (56,18,0,0),	// Alt
			new ScanCodeMapping (56,18,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (57,32,0,32),	// Spacebar
			new ScanCodeMapping (57,32,ConsoleModifiers.Shift,32),
			new ScanCodeMapping (58,20,0,0),	// Caps
			new ScanCodeMapping (58,20,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (59,112,0,0),	// F1
			new ScanCodeMapping (59,112,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (60,113,0,0),	// F2
			new ScanCodeMapping (60,113,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (61,114,0,0),	// F3
			new ScanCodeMapping (61,114,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (62,115,0,0),	// F4
			new ScanCodeMapping (62,115,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (63,116,0,0),	// F5
			new ScanCodeMapping (63,116,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (64,117,0,0),	// F6
			new ScanCodeMapping (64,117,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (65,118,0,0),	// F7
			new ScanCodeMapping (65,118,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (66,119,0,0),	// F8
			new ScanCodeMapping (66,119,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (67,120,0,0),	// F9
			new ScanCodeMapping (67,120,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (68,121,0,0),	// F10
			new ScanCodeMapping (68,121,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (69,144,0,0),	// Num
			new ScanCodeMapping (69,144,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (70,145,0,0),	// Scroll
			new ScanCodeMapping (70,145,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (71,36,0,0),	// Home
			new ScanCodeMapping (71,36,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (72,38,0,0),	// UpArrow
			new ScanCodeMapping (72,38,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (73,33,0,0),	// PageUp
			new ScanCodeMapping (73,33,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (74,109,0,45),	// Subtract
			new ScanCodeMapping (74,109,ConsoleModifiers.Shift,45),
			new ScanCodeMapping (75,37,0,0),	// LeftArrow
			new ScanCodeMapping (75,37,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (76,12,0,0),	// Center
			new ScanCodeMapping (76,12,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (77,39,0,0),	// RightArrow
			new ScanCodeMapping (77,39,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (78,107,0,43),	// Add
			new ScanCodeMapping (78,107,ConsoleModifiers.Shift,43),
			new ScanCodeMapping (79,35,0,0),	// End
			new ScanCodeMapping (79,35,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (80,40,0,0),	// DownArrow
			new ScanCodeMapping (80,40,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (81,34,0,0),	// PageDown
			new ScanCodeMapping (81,34,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (82,45,0,0),	// Insert
			new ScanCodeMapping (82,45,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (83,46,0,0),	// Delete
			new ScanCodeMapping (83,46,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (86,226,0,60),	// OEM 102
			new ScanCodeMapping (86,226,ConsoleModifiers.Shift,62),
			new ScanCodeMapping (87,122,0,0),	// F11
			new ScanCodeMapping (87,122,ConsoleModifiers.Shift,0),
			new ScanCodeMapping (88,123,0,0),	// F12
			new ScanCodeMapping (88,123,ConsoleModifiers.Shift,0)
		};
	}
}
