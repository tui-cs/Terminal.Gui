using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Terminal.Gui {
	/// <summary>
	/// Provides a platform-independent API for managing ANSI escape sequence codes.
	/// </summary>
	public static class EscSeqUtils {
		/// <summary>
		/// Represents the escape key.
		/// </summary>
		public static readonly char KeyEsc = (char)Key.Esc;
		/// <summary>
		/// Represents the CSI (Control Sequence Introducer).
		/// </summary>
		public static readonly string KeyCSI = $"{KeyEsc}[";
		/// <summary>
		/// Represents the CSI for enable any mouse event tracking.
		/// </summary>
		public static readonly string CSI_EnableAnyEventMouse = KeyCSI + "?1003h";
		/// <summary>
		/// Represents the CSI for enable SGR (Select Graphic Rendition).
		/// </summary>
		public static readonly string CSI_EnableSgrExtModeMouse = KeyCSI + "?1006h";
		/// <summary>
		/// Represents the CSI for enable URXVT (Unicode Extended Virtual Terminal).
		/// </summary>
		public static readonly string CSI_EnableUrxvtExtModeMouse = KeyCSI + "?1015h";
		/// <summary>
		/// Represents the CSI for disable any mouse event tracking.
		/// </summary>
		public static readonly string CSI_DisableAnyEventMouse = KeyCSI + "?1003l";
		/// <summary>
		/// Represents the CSI for disable SGR (Select Graphic Rendition).
		/// </summary>
		public static readonly string CSI_DisableSgrExtModeMouse = KeyCSI + "?1006l";
		/// <summary>
		/// Represents the CSI for disable URXVT (Unicode Extended Virtual Terminal).
		/// </summary>
		public static readonly string CSI_DisableUrxvtExtModeMouse = KeyCSI + "?1015l";

		/// <summary>
		/// Control sequence for enable mouse events.
		/// </summary>
		public static string EnableMouseEvents { get; set; } =
			CSI_EnableAnyEventMouse + CSI_EnableUrxvtExtModeMouse + CSI_EnableSgrExtModeMouse;
		/// <summary>
		/// Control sequence for disable mouse events.
		/// </summary>
		public static string DisableMouseEvents { get; set; } =
			CSI_DisableAnyEventMouse + CSI_DisableUrxvtExtModeMouse + CSI_DisableSgrExtModeMouse;

		/// <summary>
		/// Ensures a console key is mapped to one that works correctly with ANSI escape sequences.
		/// </summary>
		/// <param name="consoleKeyInfo">The <see cref="ConsoleKeyInfo"/>.</param>
		/// <returns>The <see cref="ConsoleKeyInfo"/> modified.</returns>
		public static ConsoleKeyInfo GetConsoleInputKey (ConsoleKeyInfo consoleKeyInfo)
		{
			ConsoleKeyInfo newConsoleKeyInfo = consoleKeyInfo;
			ConsoleKey key;
			var keyChar = consoleKeyInfo.KeyChar;
			switch ((uint)keyChar) {
			case 0:
				if (consoleKeyInfo.Key == (ConsoleKey)64) {    // Ctrl+Space in Windows.
					newConsoleKeyInfo = new ConsoleKeyInfo (' ', ConsoleKey.Spacebar,
						(consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
						(consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
						(consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);
				}
				break;
			case uint n when (n >= '\u0001' && n <= '\u001a'):
				if (consoleKeyInfo.Key == 0 && consoleKeyInfo.KeyChar == '\r') {
					key = ConsoleKey.Enter;
					newConsoleKeyInfo = new ConsoleKeyInfo (consoleKeyInfo.KeyChar,
						key,
						(consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
						(consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
						(consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);
				} else if (consoleKeyInfo.Key == 0) {
					key = (ConsoleKey)(char)(consoleKeyInfo.KeyChar + (uint)ConsoleKey.A - 1);
					newConsoleKeyInfo = new ConsoleKeyInfo ((char)key,
						key,
						(consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
						(consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
						true);
				}
				break;
			case 127:
				newConsoleKeyInfo = new ConsoleKeyInfo (consoleKeyInfo.KeyChar, ConsoleKey.Backspace,
					(consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
					(consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
					(consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);
				break;
			default:
				newConsoleKeyInfo = consoleKeyInfo;
				break;
			}

			return newConsoleKeyInfo;
		}

		/// <summary>
		/// A helper to resize the <see cref="ConsoleKeyInfo"/> as needed.
		/// </summary>
		/// <param name="consoleKeyInfo">The <see cref="ConsoleKeyInfo"/>.</param>
		/// <param name="cki">The <see cref="ConsoleKeyInfo"/> array to resize.</param>
		/// <returns>The <see cref="ConsoleKeyInfo"/> resized.</returns>
		public static ConsoleKeyInfo [] ResizeArray (ConsoleKeyInfo consoleKeyInfo, ConsoleKeyInfo [] cki)
		{
			Array.Resize (ref cki, cki == null ? 1 : cki.Length + 1);
			cki [cki.Length - 1] = consoleKeyInfo;
			return cki;
		}

		/// <summary>
		/// Decodes a escape sequence to been processed in the appropriate manner.
		/// </summary>
		/// <param name="escSeqReqProc">The <see cref="EscSeqReqProc"/> which may contain a request.</param>
		/// <param name="newConsoleKeyInfo">The <see cref="ConsoleKeyInfo"/> which may changes.</param>
		/// <param name="key">The <see cref="ConsoleKey"/> which may changes.</param>
		/// <param name="cki">The <see cref="ConsoleKeyInfo"/> array.</param>
		/// <param name="mod">The <see cref="ConsoleModifiers"/> which may changes.</param>
		/// <param name="c1Control">The control returned by the <see cref="GetC1ControlChar(char)"/> method.</param>
		/// <param name="code">The code returned by the <see cref="GetEscapeResult(char[])"/> method.</param>
		/// <param name="values">The values returned by the <see cref="GetEscapeResult(char[])"/> method.</param>
		/// <param name="terminating">The terminating returned by the <see cref="GetEscapeResult(char[])"/> method.</param>
		/// <param name="isKeyMouse">Indicates if the escape sequence is a mouse key.</param>
		/// <param name="buttonState">The <see cref="MouseFlags"/> button state.</param>
		/// <param name="pos">The <see cref="MouseFlags"/> position.</param>
		/// <param name="isReq">Indicates if the escape sequence is a response to a request.</param>
		/// <param name="continuousButtonPressedHandler">The handler that will process the event.</param>
		public static void DecodeEscSeq (EscSeqReqProc escSeqReqProc, ref ConsoleKeyInfo newConsoleKeyInfo, ref ConsoleKey key, ConsoleKeyInfo [] cki, ref ConsoleModifiers mod, out string c1Control, out string code, out string [] values, out string terminating, out bool isKeyMouse, out List<MouseFlags> buttonState, out Point pos, out bool isReq, Action<MouseFlags, Point> continuousButtonPressedHandler)
		{
			char [] kChars = GetKeyCharArray (cki);
			(c1Control, code, values, terminating) = GetEscapeResult (kChars);
			isKeyMouse = false;
			buttonState = new List<MouseFlags> () { 0 };
			pos = default;
			isReq = false;
			switch (c1Control) {
			case "ESC":
				if (values == null && string.IsNullOrEmpty (terminating)) {
					key = ConsoleKey.Escape;
					newConsoleKeyInfo = new ConsoleKeyInfo (cki [0].KeyChar, key,
						(mod & ConsoleModifiers.Shift) != 0,
						(mod & ConsoleModifiers.Alt) != 0,
						(mod & ConsoleModifiers.Control) != 0);
				} else if ((uint)cki [1].KeyChar >= 1 && (uint)cki [1].KeyChar <= 26) {
					key = (ConsoleKey)(char)(cki [1].KeyChar + (uint)ConsoleKey.A - 1);
					newConsoleKeyInfo = new ConsoleKeyInfo (cki [1].KeyChar,
						key,
						false,
						true,
						true);
				} else {
					if (cki [1].KeyChar >= 97 && cki [1].KeyChar <= 122) {
						key = (ConsoleKey)cki [1].KeyChar.ToString ().ToUpper () [0];
					} else {
						key = (ConsoleKey)cki [1].KeyChar;
					}
					newConsoleKeyInfo = new ConsoleKeyInfo ((char)key,
						(ConsoleKey)Math.Min ((uint)key, 255),
						false,
						true,
						false);
				}
				break;
			case "SS3":
				key = GetConsoleKey (terminating [0], values [0], ref mod);
				newConsoleKeyInfo = new ConsoleKeyInfo ('\0',
					key,
					(mod & ConsoleModifiers.Shift) != 0,
					(mod & ConsoleModifiers.Alt) != 0,
					(mod & ConsoleModifiers.Control) != 0);
				break;
			case "CSI":
				if (!string.IsNullOrEmpty (code) && code == "<") {
					GetMouse (cki, out buttonState, out pos, continuousButtonPressedHandler);
					isKeyMouse = true;
					return;
				} else if (escSeqReqProc != null && escSeqReqProc.Requested (terminating)) {
					isReq = true;
					escSeqReqProc.Remove (terminating);
					return;
				}
				key = GetConsoleKey (terminating [0], values [0], ref mod);
				if (key != 0 && values.Length > 1) {
					mod |= GetConsoleModifiers (values [1]);
				}
				newConsoleKeyInfo = new ConsoleKeyInfo ('\0',
					key,
					(mod & ConsoleModifiers.Shift) != 0,
					(mod & ConsoleModifiers.Alt) != 0,
					(mod & ConsoleModifiers.Control) != 0);
				break;
			}
		}

		/// <summary>
		/// Gets all the needed information about a escape sequence.
		/// </summary>
		/// <param name="kChar">The array with all chars.</param>
		/// <returns>
		/// The c1Control returned by <see cref="GetC1ControlChar(char)"/>, code, values and terminating.
		/// </returns>
		public static (string c1Control, string code, string [] values, string terminating) GetEscapeResult (char [] kChar)
		{
			if (kChar == null || kChar.Length == 0) {
				return (null, null, null, null);
			}
			if (kChar [0] != '\x1b') {
				throw new InvalidOperationException ("Invalid escape character!");
			}
			if (kChar.Length == 1) {
				return ("ESC", null, null, null);
			}
			if (kChar.Length == 2) {
				return ("ESC", null, null, kChar [1].ToString ());
			}
			string c1Control = GetC1ControlChar (kChar [1]);
			string code = null;
			int nSep = kChar.Count (x => x == ';') + 1;
			string [] values = new string [nSep];
			int valueIdx = 0;
			string terminating = "";
			for (int i = 2; i < kChar.Length; i++) {
				var c = kChar [i];
				if (char.IsDigit (c)) {
					values [valueIdx] += c.ToString ();
				} else if (c == ';') {
					valueIdx++;
				} else if (valueIdx == nSep - 1 || i == kChar.Length - 1) {
					terminating += c.ToString ();
				} else {
					code += c.ToString ();
				}
			}

			return (c1Control, code, values, terminating);
		}

		/// <summary>
		/// Gets the c1Control used in the called escape sequence.
		/// </summary>
		/// <param name="c">The char used.</param>
		/// <returns>The c1Control.</returns>
		public static string GetC1ControlChar (char c)
		{
			// These control characters are used in the vtXXX emulation.
			switch (c) {
			case 'D':
				return "IND"; // Index
			case 'E':
				return "NEL"; // Next Line
			case 'H':
				return "HTS"; // Tab Set
			case 'M':
				return "RI"; // Reverse Index
			case 'N':
				return "SS2"; // Single Shift Select of G2 Character Set: affects next character only
			case 'O':
				return "SS3"; // Single Shift Select of G3 Character Set: affects next character only
			case 'P':
				return "DCS"; // Device Control String
			case 'V':
				return "SPA"; // Start of Guarded Area
			case 'W':
				return "EPA"; // End of Guarded Area
			case 'X':
				return "SOS"; // Start of String
			case 'Z':
				return "DECID"; // Return Terminal ID Obsolete form of CSI c (DA)
			case '[':
				return "CSI"; // Control Sequence Introducer
			case '\\':
				return "ST"; // String Terminator
			case ']':
				return "OSC"; // Operating System Command
			case '^':
				return "PM"; // Privacy Message
			case '_':
				return "APC"; // Application Program Command
			default:
				return ""; // Not supported
			}
		}

		/// <summary>
		/// Gets the <see cref="ConsoleModifiers"/> from the value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The <see cref="ConsoleModifiers"/> or zero.</returns>
		public static ConsoleModifiers GetConsoleModifiers (string value)
		{
			switch (value) {
			case "2":
				return ConsoleModifiers.Shift;
			case "3":
				return ConsoleModifiers.Alt;
			case "4":
				return ConsoleModifiers.Shift | ConsoleModifiers.Alt;
			case "5":
				return ConsoleModifiers.Control;
			case "6":
				return ConsoleModifiers.Shift | ConsoleModifiers.Control;
			case "7":
				return ConsoleModifiers.Alt | ConsoleModifiers.Control;
			case "8":
				return ConsoleModifiers.Shift | ConsoleModifiers.Alt | ConsoleModifiers.Control;
			default:
				return 0;
			}
		}

		/// <summary>
		/// Gets the <see cref="ConsoleKey"/> depending on terminating and value.
		/// </summary>
		/// <param name="terminating">The terminating.</param>
		/// <param name="value">The value.</param>
		/// <param name="mod">The <see cref="ConsoleModifiers"/> which may changes.</param>
		/// <returns>The <see cref="ConsoleKey"/> and probably the <see cref="ConsoleModifiers"/>.</returns>
		public static ConsoleKey GetConsoleKey (char terminating, string value, ref ConsoleModifiers mod)
		{
			ConsoleKey key;
			switch (terminating) {
			case 'A':
				key = ConsoleKey.UpArrow;
				break;
			case 'B':
				key = ConsoleKey.DownArrow;
				break;
			case 'C':
				key = ConsoleKey.RightArrow;
				break;
			case 'D':
				key = ConsoleKey.LeftArrow;
				break;
			case 'F':
				key = ConsoleKey.End;
				break;
			case 'H':
				key = ConsoleKey.Home;
				break;
			case 'P':
				key = ConsoleKey.F1;
				break;
			case 'Q':
				key = ConsoleKey.F2;
				break;
			case 'R':
				key = ConsoleKey.F3;
				break;
			case 'S':
				key = ConsoleKey.F4;
				break;
			case 'Z':
				key = ConsoleKey.Tab;
				mod |= ConsoleModifiers.Shift;
				break;
			case '~':
				switch (value) {
				case "2":
					key = ConsoleKey.Insert;
					break;
				case "3":
					key = ConsoleKey.Delete;
					break;
				case "5":
					key = ConsoleKey.PageUp;
					break;
				case "6":
					key = ConsoleKey.PageDown;
					break;
				case "15":
					key = ConsoleKey.F5;
					break;
				case "17":
					key = ConsoleKey.F6;
					break;
				case "18":
					key = ConsoleKey.F7;
					break;
				case "19":
					key = ConsoleKey.F8;
					break;
				case "20":
					key = ConsoleKey.F9;
					break;
				case "21":
					key = ConsoleKey.F10;
					break;
				case "23":
					key = ConsoleKey.F11;
					break;
				case "24":
					key = ConsoleKey.F12;
					break;
				default:
					key = 0;
					break;
				}
				break;
			default:
				key = 0;
				break;
			}

			return key;
		}

		/// <summary>
		/// A helper to get only the <see cref="ConsoleKeyInfo.KeyChar"/> from the <see cref="ConsoleKeyInfo"/> array.
		/// </summary>
		/// <param name="cki"></param>
		/// <returns>The char array of the escape sequence.</returns>
		public static char [] GetKeyCharArray (ConsoleKeyInfo [] cki)
		{
			char [] kChar = new char [] { };
			var length = 0;
			foreach (var kc in cki) {
				length++;
				Array.Resize (ref kChar, length);
				kChar [length - 1] = kc.KeyChar;
			}

			return kChar;
		}

		private static MouseFlags? lastMouseButtonPressed;
		//private static MouseFlags? lastMouseButtonReleased;
		private static bool isButtonPressed;
		//private static bool isButtonReleased;
		private static bool isButtonClicked;
		private static bool isButtonDoubleClicked;
		private static bool isButtonTripleClicked;
		private static Point point;

		/// <summary>
		/// Gets the <see cref="MouseFlags"/> mouse button flags and the position.
		/// </summary>
		/// <param name="cki">The <see cref="ConsoleKeyInfo"/> array.</param>
		/// <param name="mouseFlags">The mouse button flags.</param>
		/// <param name="pos">The mouse position.</param>
		/// <param name="continuousButtonPressedHandler">The handler that will process the event.</param>
		public static void GetMouse (ConsoleKeyInfo [] cki, out List<MouseFlags> mouseFlags, out Point pos, Action<MouseFlags, Point> continuousButtonPressedHandler)
		{
			MouseFlags buttonState = 0;
			pos = new Point ();
			int buttonCode = 0;
			bool foundButtonCode = false;
			int foundPoint = 0;
			string value = "";
			var kChar = GetKeyCharArray (cki);
			//System.Diagnostics.Debug.WriteLine ($"kChar: {new string (kChar)}");
			for (int i = 0; i < kChar.Length; i++) {
				var c = kChar [i];
				if (c == '<') {
					foundButtonCode = true;
				} else if (foundButtonCode && c != ';') {
					value += c.ToString ();
				} else if (c == ';') {
					if (foundButtonCode) {
						foundButtonCode = false;
						buttonCode = int.Parse (value);
					}
					if (foundPoint == 1) {
						pos.X = int.Parse (value) - 1;
					}
					value = "";
					foundPoint++;
				} else if (foundPoint > 0 && c != 'm' && c != 'M') {
					value += c.ToString ();
				} else if (c == 'm' || c == 'M') {
					//pos.Y = int.Parse (value) + Console.WindowTop - 1;
					pos.Y = int.Parse (value) - 1;

					switch (buttonCode) {
					case 0:
					case 8:
					case 16:
					case 24:
					case 32:
					case 36:
					case 40:
					case 48:
					case 56:
						buttonState = c == 'M' ? MouseFlags.Button1Pressed
							: MouseFlags.Button1Released;
						break;
					case 1:
					case 9:
					case 17:
					case 25:
					case 33:
					case 37:
					case 41:
					case 45:
					case 49:
					case 53:
					case 57:
					case 61:
						buttonState = c == 'M' ? MouseFlags.Button2Pressed
							: MouseFlags.Button2Released;
						break;
					case 2:
					case 10:
					case 14:
					case 18:
					case 22:
					case 26:
					case 30:
					case 34:
					case 42:
					case 46:
					case 50:
					case 54:
					case 58:
					case 62:
						buttonState = c == 'M' ? MouseFlags.Button3Pressed
							: MouseFlags.Button3Released;
						break;
					case 35:
					//// Needed for Windows OS
					//if (isButtonPressed && c == 'm'
					//	&& (lastMouseEvent.ButtonState == MouseFlags.Button1Pressed
					//	|| lastMouseEvent.ButtonState == MouseFlags.Button2Pressed
					//	|| lastMouseEvent.ButtonState == MouseFlags.Button3Pressed)) {

					//	switch (lastMouseEvent.ButtonState) {
					//	case MouseFlags.Button1Pressed:
					//		buttonState = MouseFlags.Button1Released;
					//		break;
					//	case MouseFlags.Button2Pressed:
					//		buttonState = MouseFlags.Button2Released;
					//		break;
					//	case MouseFlags.Button3Pressed:
					//		buttonState = MouseFlags.Button3Released;
					//		break;
					//	}
					//} else {
					//	buttonState = MouseFlags.ReportMousePosition;
					//}
					//break;
					case 39:
					case 43:
					case 47:
					case 51:
					case 55:
					case 59:
					case 63:
						buttonState = MouseFlags.ReportMousePosition;
						break;
					case 64:
						buttonState = MouseFlags.WheeledUp;
						break;
					case 65:
						buttonState = MouseFlags.WheeledDown;
						break;
					case 68:
					case 72:
					case 80:
						buttonState = MouseFlags.WheeledLeft;       // Shift/Ctrl+WheeledUp
						break;
					case 69:
					case 73:
					case 81:
						buttonState = MouseFlags.WheeledRight;      // Shift/Ctrl+WheeledDown
						break;
					}
					// Modifiers.
					switch (buttonCode) {
					case 8:
					case 9:
					case 10:
					case 43:
						buttonState |= MouseFlags.ButtonAlt;
						break;
					case 14:
					case 47:
						buttonState |= MouseFlags.ButtonAlt | MouseFlags.ButtonShift;
						break;
					case 16:
					case 17:
					case 18:
					case 51:
						buttonState |= MouseFlags.ButtonCtrl;
						break;
					case 22:
					case 55:
						buttonState |= MouseFlags.ButtonCtrl | MouseFlags.ButtonShift;
						break;
					case 24:
					case 25:
					case 26:
					case 59:
						buttonState |= MouseFlags.ButtonCtrl | MouseFlags.ButtonAlt;
						break;
					case 30:
					case 63:
						buttonState |= MouseFlags.ButtonCtrl | MouseFlags.ButtonShift | MouseFlags.ButtonAlt;
						break;
					case 32:
					case 33:
					case 34:
						buttonState |= MouseFlags.ReportMousePosition;
						break;
					case 36:
					case 37:
						buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonShift;
						break;
					case 39:
					case 68:
					case 69:
						buttonState |= MouseFlags.ButtonShift;
						break;
					case 40:
					case 41:
					case 42:
						buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonAlt;
						break;
					case 45:
					case 46:
						buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonAlt | MouseFlags.ButtonShift;
						break;
					case 48:
					case 49:
					case 50:
						buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonCtrl;
						break;
					case 53:
					case 54:
						buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonCtrl | MouseFlags.ButtonShift;
						break;
					case 56:
					case 57:
					case 58:
						buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonCtrl | MouseFlags.ButtonAlt;
						break;
					case 61:
					case 62:
						buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonCtrl | MouseFlags.ButtonShift | MouseFlags.ButtonAlt;
						break;
					}
				}
			}

			mouseFlags = new List<MouseFlags> () { MouseFlags.AllEvents };

			if (lastMouseButtonPressed != null && !isButtonPressed && !buttonState.HasFlag (MouseFlags.ReportMousePosition)
				&& !buttonState.HasFlag (MouseFlags.Button1Released)
				&& !buttonState.HasFlag (MouseFlags.Button2Released)
				&& !buttonState.HasFlag (MouseFlags.Button3Released)
				&& !buttonState.HasFlag (MouseFlags.Button4Released)) {

				lastMouseButtonPressed = null;
				isButtonPressed = false;
			}

			if (!isButtonClicked && !isButtonDoubleClicked && ((buttonState == MouseFlags.Button1Pressed || buttonState == MouseFlags.Button2Pressed ||
				  buttonState == MouseFlags.Button3Pressed || buttonState == MouseFlags.Button4Pressed) && lastMouseButtonPressed == null) ||
				  isButtonPressed && lastMouseButtonPressed != null && buttonState.HasFlag (MouseFlags.ReportMousePosition)) {

				mouseFlags [0] = buttonState;
				lastMouseButtonPressed = buttonState;
				isButtonPressed = true;

				if ((mouseFlags [0] & MouseFlags.ReportMousePosition) == 0) {
					point = new Point () {
						X = pos.X,
						Y = pos.Y
					};

					Application.MainLoop.AddIdle (() => {
						Task.Run (async () => await ProcessContinuousButtonPressedAsync (buttonState, continuousButtonPressedHandler));
						return false;
					});
				} else if (mouseFlags [0] == MouseFlags.ReportMousePosition) {
					isButtonPressed = false;
				}

			} else if (isButtonDoubleClicked && (buttonState == MouseFlags.Button1Pressed || buttonState == MouseFlags.Button2Pressed ||
				buttonState == MouseFlags.Button3Pressed || buttonState == MouseFlags.Button4Pressed)) {

				mouseFlags [0] = GetButtonTripleClicked (buttonState);
				isButtonDoubleClicked = false;
				isButtonTripleClicked = true;

			} else if (isButtonClicked && (buttonState == MouseFlags.Button1Pressed || buttonState == MouseFlags.Button2Pressed ||
				buttonState == MouseFlags.Button3Pressed || buttonState == MouseFlags.Button4Pressed)) {

				mouseFlags [0] = GetButtonDoubleClicked (buttonState);
				isButtonClicked = false;
				isButtonDoubleClicked = true;
				Application.MainLoop.AddIdle (() => {
					Task.Run (async () => await ProcessButtonDoubleClickedAsync ());
					return false;
				});

			}
			//else if (isButtonReleased && !isButtonClicked && buttonState == MouseFlags.ReportMousePosition) {
			//	mouseFlag [0] = GetButtonClicked ((MouseFlags)lastMouseButtonReleased);
			//	lastMouseButtonReleased = null;
			//	isButtonReleased = false;
			//	isButtonClicked = true;
			//	Application.MainLoop.AddIdle (() => {
			//		Task.Run (async () => await ProcessButtonClickedAsync ());
			//		return false;
			//	});

			//} 
			else if (!isButtonClicked && !isButtonDoubleClicked && (buttonState == MouseFlags.Button1Released || buttonState == MouseFlags.Button2Released ||
				  buttonState == MouseFlags.Button3Released || buttonState == MouseFlags.Button4Released)) {

				mouseFlags [0] = buttonState;
				isButtonPressed = false;

				if (isButtonTripleClicked) {
					isButtonTripleClicked = false;
				} else if (pos.X == point.X && pos.Y == point.Y) {
					mouseFlags.Add (GetButtonClicked (buttonState));
					isButtonClicked = true;
					Application.MainLoop.AddIdle (() => {
						Task.Run (async () => await ProcessButtonClickedAsync ());
						return false;
					});
				}

				point = pos;

				//if ((lastMouseButtonPressed & MouseFlags.ReportMousePosition) == 0) {
				//	lastMouseButtonReleased = buttonState;
				//	isButtonPressed = false;
				//	isButtonReleased = true;
				//} else {
				//	lastMouseButtonPressed = null;
				//	isButtonPressed = false;
				//}

			} else if (buttonState == MouseFlags.WheeledUp) {

				mouseFlags [0] = MouseFlags.WheeledUp;

			} else if (buttonState == MouseFlags.WheeledDown) {

				mouseFlags [0] = MouseFlags.WheeledDown;

			} else if (buttonState == MouseFlags.WheeledLeft) {

				mouseFlags [0] = MouseFlags.WheeledLeft;

			} else if (buttonState == MouseFlags.WheeledRight) {

				mouseFlags [0] = MouseFlags.WheeledRight;

			} else if (buttonState == MouseFlags.ReportMousePosition) {
				mouseFlags [0] = MouseFlags.ReportMousePosition;

			} else {
				mouseFlags [0] = buttonState;
				//foreach (var flag in buttonState.GetUniqueFlags()) {
				//	mouseFlag [0] |= flag;
				//}
			}

			mouseFlags [0] = SetControlKeyStates (buttonState, mouseFlags [0]);
			//buttonState = mouseFlags;

			//System.Diagnostics.Debug.WriteLine ($"buttonState: {buttonState} X: {pos.X} Y: {pos.Y}");
			//foreach (var mf in mouseFlags) {
			//	System.Diagnostics.Debug.WriteLine ($"mouseFlags: {mf} X: {pos.X} Y: {pos.Y}");
			//}
		}

		private static async Task ProcessContinuousButtonPressedAsync (MouseFlags mouseFlag, Action<MouseFlags, Point> continuousButtonPressedHandler)
		{
			while (isButtonPressed) {
				await Task.Delay (100);
				//var me = new MouseEvent () {
				//	X = point.X,
				//	Y = point.Y,
				//	Flags = mouseFlag
				//};

				var view = Application.WantContinuousButtonPressedView;
				if (view == null)
					break;
				if (isButtonPressed && lastMouseButtonPressed != null && (mouseFlag & MouseFlags.ReportMousePosition) == 0) {
					Application.MainLoop.Invoke (() => continuousButtonPressedHandler (mouseFlag, point));
				}
			}
		}

		private static async Task ProcessButtonClickedAsync ()
		{
			await Task.Delay (300);
			isButtonClicked = false;
		}

		private static async Task ProcessButtonDoubleClickedAsync ()
		{
			await Task.Delay (300);
			isButtonDoubleClicked = false;
		}

		private static MouseFlags GetButtonClicked (MouseFlags mouseFlag)
		{
			MouseFlags mf = default;
			switch (mouseFlag) {
			case MouseFlags.Button1Released:
				mf = MouseFlags.Button1Clicked;
				break;

			case MouseFlags.Button2Released:
				mf = MouseFlags.Button2Clicked;
				break;

			case MouseFlags.Button3Released:
				mf = MouseFlags.Button3Clicked;
				break;
			}
			return mf;
		}

		private static MouseFlags GetButtonDoubleClicked (MouseFlags mouseFlag)
		{
			MouseFlags mf = default;
			switch (mouseFlag) {
			case MouseFlags.Button1Pressed:
				mf = MouseFlags.Button1DoubleClicked;
				break;

			case MouseFlags.Button2Pressed:
				mf = MouseFlags.Button2DoubleClicked;
				break;

			case MouseFlags.Button3Pressed:
				mf = MouseFlags.Button3DoubleClicked;
				break;
			}
			return mf;
		}

		private static MouseFlags GetButtonTripleClicked (MouseFlags mouseFlag)
		{
			MouseFlags mf = default;
			switch (mouseFlag) {
			case MouseFlags.Button1Pressed:
				mf = MouseFlags.Button1TripleClicked;
				break;

			case MouseFlags.Button2Pressed:
				mf = MouseFlags.Button2TripleClicked;
				break;

			case MouseFlags.Button3Pressed:
				mf = MouseFlags.Button3TripleClicked;
				break;
			}
			return mf;
		}

		private static MouseFlags SetControlKeyStates (MouseFlags buttonState, MouseFlags mouseFlag)
		{
			if ((buttonState & MouseFlags.ButtonCtrl) != 0 && (mouseFlag & MouseFlags.ButtonCtrl) == 0)
				mouseFlag |= MouseFlags.ButtonCtrl;

			if ((buttonState & MouseFlags.ButtonShift) != 0 && (mouseFlag & MouseFlags.ButtonShift) == 0)
				mouseFlag |= MouseFlags.ButtonShift;

			if ((buttonState & MouseFlags.ButtonAlt) != 0 && (mouseFlag & MouseFlags.ButtonAlt) == 0)
				mouseFlag |= MouseFlags.ButtonAlt;
			return mouseFlag;
		}

		/// <summary>
		/// Get the terminal that holds the console driver.
		/// </summary>
		/// <param name="process">The process.</param>
		/// <returns>If supported the executable console process, null otherwise.</returns>
		public static Process GetParentProcess (Process process)
		{
			if (!RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
				return null;
			}

			string query = "SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = " + process.Id;
			using (ManagementObjectSearcher mos = new ManagementObjectSearcher (query)) {
				foreach (ManagementObject mo in mos.Get ()) {
					if (mo ["ParentProcessId"] != null) {
						try {
							var id = Convert.ToInt32 (mo ["ParentProcessId"]);
							return Process.GetProcessById (id);
						} catch {
						}
					}
				}
			}
			return null;
		}
	}
}