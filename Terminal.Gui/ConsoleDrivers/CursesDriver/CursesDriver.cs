//
// Driver.cs: Curses-based Driver
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NStack;
using Unix.Terminal;

namespace Terminal.Gui {

	/// <summary>
	/// This is the Curses driver for the gui.cs/Terminal framework.
	/// </summary>
	internal class CursesDriver : ConsoleDriver {
		public override int Cols => Curses.Cols;
		public override int Rows => Curses.Lines;
		public override int Left => 0;
		public override int Top => 0;
		public override bool EnableConsoleScrolling { get; set; }
		public override IClipboard Clipboard { get => clipboard; }

		CursorVisibility? initialCursorVisibility = null;
		CursorVisibility? currentCursorVisibility = null;
		IClipboard clipboard;
		int [,,] contents;

		public override int [,,] Contents => contents;

		// Current row, and current col, tracked by Move/AddRune only
		int ccol, crow;
		bool needMove;
		public override void Move (int col, int row)
		{
			ccol = col;
			crow = row;

			if (Clip.Contains (col, row)) {
				Curses.move (row, col);
				needMove = false;
			} else {
				Curses.move (Clip.Y, Clip.X);
				needMove = true;
			}
		}

		static bool sync = false;
		public override void AddRune (Rune rune)
		{
			rune = MakePrintable (rune);
			var runeWidth = Rune.ColumnWidth (rune);
			var validClip = IsValidContent (ccol, crow, Clip);

			if (validClip) {
				if (needMove) {
					Curses.move (crow, ccol);
					needMove = false;
				}
				if (runeWidth == 0 && ccol > 0) {
					var r = contents [crow, ccol - 1, 0];
					var s = new string (new char [] { (char)r, (char)rune });
					string sn;
					if (!s.IsNormalized ()) {
						sn = s.Normalize ();
					} else {
						sn = s;
					}
					var c = sn [0];
					Curses.mvaddch (crow, ccol - 1, (int)(uint)c);
					contents [crow, ccol - 1, 0] = c;
					contents [crow, ccol - 1, 1] = CurrentAttribute;
					contents [crow, ccol - 1, 2] = 1;

				} else {
					if (runeWidth < 2 && ccol > 0
						&& Rune.ColumnWidth ((char)contents [crow, ccol - 1, 0]) > 1) {

						var curAtttib = CurrentAttribute;
						Curses.attrset (contents [crow, ccol - 1, 1]);
						Curses.mvaddch (crow, ccol - 1, (int)(uint)' ');
						contents [crow, ccol - 1, 0] = (int)(uint)' ';
						Curses.move (crow, ccol);
						Curses.attrset (curAtttib);

					} else if (runeWidth < 2 && ccol <= Clip.Right - 1
						&& Rune.ColumnWidth ((char)contents [crow, ccol, 0]) > 1) {

						var curAtttib = CurrentAttribute;
						Curses.attrset (contents [crow, ccol + 1, 1]);
						Curses.mvaddch (crow, ccol + 1, (int)(uint)' ');
						contents [crow, ccol + 1, 0] = (int)(uint)' ';
						Curses.move (crow, ccol);
						Curses.attrset (curAtttib);

					}
					if (runeWidth > 1 && ccol == Clip.Right - 1) {
						Curses.addch ((int)(uint)' ');
						contents [crow, ccol, 0] = (int)(uint)' ';
					} else {
						Curses.addch ((int)(uint)rune);
						contents [crow, ccol, 0] = (int)(uint)rune;
					}
					contents [crow, ccol, 1] = CurrentAttribute;
					contents [crow, ccol, 2] = 1;
				}
			} else {
				needMove = true;
			}

			if (runeWidth < 0 || runeWidth > 0) {
				ccol++;
			}

			if (runeWidth > 1) {
				if (validClip && ccol < Clip.Right) {
					contents [crow, ccol, 1] = CurrentAttribute;
					contents [crow, ccol, 2] = 0;
				}
				ccol++;
			}

			if (sync) {
				UpdateScreen ();
			}
		}

		public override void AddStr (ustring str)
		{
			// TODO; optimize this to determine if the str fits in the clip region, and if so, use Curses.addstr directly
			foreach (var rune in str)
				AddRune (rune);
		}

		public override void Refresh ()
		{
			Curses.raw ();
			Curses.noecho ();
			Curses.refresh ();
			ProcessWinChange ();
		}

		private void ProcessWinChange ()
		{
			if (Curses.CheckWinChange ()) {
				ResizeScreen ();
				UpdateOffScreen ();
				TerminalResized?.Invoke ();
			}
		}

		public override void UpdateCursor () => Refresh ();

		public override void End ()
		{
			StopReportingMouseMoves ();
			SetCursorVisibility (CursorVisibility.Default);

			Curses.endwin ();
		}

		public override void UpdateScreen () => window.redrawwin ();

		public override void SetAttribute (Attribute c)
		{
			base.SetAttribute (c);
			Curses.attrset (CurrentAttribute);
		}

		public Curses.Window window;

		//static short last_color_pair = 16;

		/// <summary>
		/// Creates a curses color from the provided foreground and background colors
		/// </summary>
		/// <param name="foreground">Contains the curses attributes for the foreground (color, plus any attributes)</param>
		/// <param name="background">Contains the curses attributes for the background (color, plus any attributes)</param>
		/// <returns></returns>
		public static Attribute MakeColor (short foreground, short background)
		{
			var v = (short)((int)foreground | background << 4);
			//Curses.InitColorPair (++last_color_pair, foreground, background);
			Curses.InitColorPair (v, foreground, background);
			return new Attribute (
				//value: Curses.ColorPair (last_color_pair),
				value: Curses.ColorPair (v),
				//foreground: (Color)foreground,
				foreground: MapCursesColor (foreground),
				//background: (Color)background);
				background: MapCursesColor (background));
		}

		public override Attribute MakeColor (Color fore, Color back)
		{
			return MakeColor ((short)MapColor (fore), (short)MapColor (back));
		}

		int [,] colorPairs = new int [16, 16];

		public override void SetColors (ConsoleColor foreground, ConsoleColor background)
		{
			// BUGBUG: This code is never called ?? See Issue #2300
			int f = (short)foreground;
			int b = (short)background;
			var v = colorPairs [f, b];
			if ((v & 0x10000) == 0) {
				b &= 0x7;
				bool bold = (f & 0x8) != 0;
				f &= 0x7;

				v = MakeColor ((short)f, (short)b) | (bold ? Curses.A_BOLD : 0);
				colorPairs [(int)foreground, (int)background] = v | 0x1000;
			}
			SetAttribute (v & 0xffff);
		}

		Dictionary<int, int> rawPairs = new Dictionary<int, int> ();
		public override void SetColors (short foreColorId, short backgroundColorId)
		{
			// BUGBUG: This code is never called ?? See Issue #2300
			int key = ((ushort)foreColorId << 16) | (ushort)backgroundColorId;
			if (!rawPairs.TryGetValue (key, out var v)) {
				v = MakeColor (foreColorId, backgroundColorId);
				rawPairs [key] = v;
			}
			SetAttribute (v);
		}

		static Key MapCursesKey (int cursesKey)
		{
			switch (cursesKey) {
			case Curses.KeyF1: return Key.F1;
			case Curses.KeyF2: return Key.F2;
			case Curses.KeyF3: return Key.F3;
			case Curses.KeyF4: return Key.F4;
			case Curses.KeyF5: return Key.F5;
			case Curses.KeyF6: return Key.F6;
			case Curses.KeyF7: return Key.F7;
			case Curses.KeyF8: return Key.F8;
			case Curses.KeyF9: return Key.F9;
			case Curses.KeyF10: return Key.F10;
			case Curses.KeyF11: return Key.F11;
			case Curses.KeyF12: return Key.F12;
			case Curses.KeyUp: return Key.CursorUp;
			case Curses.KeyDown: return Key.CursorDown;
			case Curses.KeyLeft: return Key.CursorLeft;
			case Curses.KeyRight: return Key.CursorRight;
			case Curses.KeyHome: return Key.Home;
			case Curses.KeyEnd: return Key.End;
			case Curses.KeyNPage: return Key.PageDown;
			case Curses.KeyPPage: return Key.PageUp;
			case Curses.KeyDeleteChar: return Key.DeleteChar;
			case Curses.KeyInsertChar: return Key.InsertChar;
			case Curses.KeyTab: return Key.Tab;
			case Curses.KeyBackTab: return Key.BackTab;
			case Curses.KeyBackspace: return Key.Backspace;
			case Curses.ShiftKeyUp: return Key.CursorUp | Key.ShiftMask;
			case Curses.ShiftKeyDown: return Key.CursorDown | Key.ShiftMask;
			case Curses.ShiftKeyLeft: return Key.CursorLeft | Key.ShiftMask;
			case Curses.ShiftKeyRight: return Key.CursorRight | Key.ShiftMask;
			case Curses.ShiftKeyHome: return Key.Home | Key.ShiftMask;
			case Curses.ShiftKeyEnd: return Key.End | Key.ShiftMask;
			case Curses.ShiftKeyNPage: return Key.PageDown | Key.ShiftMask;
			case Curses.ShiftKeyPPage: return Key.PageUp | Key.ShiftMask;
			case Curses.AltKeyUp: return Key.CursorUp | Key.AltMask;
			case Curses.AltKeyDown: return Key.CursorDown | Key.AltMask;
			case Curses.AltKeyLeft: return Key.CursorLeft | Key.AltMask;
			case Curses.AltKeyRight: return Key.CursorRight | Key.AltMask;
			case Curses.AltKeyHome: return Key.Home | Key.AltMask;
			case Curses.AltKeyEnd: return Key.End | Key.AltMask;
			case Curses.AltKeyNPage: return Key.PageDown | Key.AltMask;
			case Curses.AltKeyPPage: return Key.PageUp | Key.AltMask;
			case Curses.CtrlKeyUp: return Key.CursorUp | Key.CtrlMask;
			case Curses.CtrlKeyDown: return Key.CursorDown | Key.CtrlMask;
			case Curses.CtrlKeyLeft: return Key.CursorLeft | Key.CtrlMask;
			case Curses.CtrlKeyRight: return Key.CursorRight | Key.CtrlMask;
			case Curses.CtrlKeyHome: return Key.Home | Key.CtrlMask;
			case Curses.CtrlKeyEnd: return Key.End | Key.CtrlMask;
			case Curses.CtrlKeyNPage: return Key.PageDown | Key.CtrlMask;
			case Curses.CtrlKeyPPage: return Key.PageUp | Key.CtrlMask;
			case Curses.ShiftCtrlKeyUp: return Key.CursorUp | Key.ShiftMask | Key.CtrlMask;
			case Curses.ShiftCtrlKeyDown: return Key.CursorDown | Key.ShiftMask | Key.CtrlMask;
			case Curses.ShiftCtrlKeyLeft: return Key.CursorLeft | Key.ShiftMask | Key.CtrlMask;
			case Curses.ShiftCtrlKeyRight: return Key.CursorRight | Key.ShiftMask | Key.CtrlMask;
			case Curses.ShiftCtrlKeyHome: return Key.Home | Key.ShiftMask | Key.CtrlMask;
			case Curses.ShiftCtrlKeyEnd: return Key.End | Key.ShiftMask | Key.CtrlMask;
			case Curses.ShiftCtrlKeyNPage: return Key.PageDown | Key.ShiftMask | Key.CtrlMask;
			case Curses.ShiftCtrlKeyPPage: return Key.PageUp | Key.ShiftMask | Key.CtrlMask;
			case Curses.ShiftAltKeyUp: return Key.CursorUp | Key.ShiftMask | Key.AltMask;
			case Curses.ShiftAltKeyDown: return Key.CursorDown | Key.ShiftMask | Key.AltMask;
			case Curses.ShiftAltKeyLeft: return Key.CursorLeft | Key.ShiftMask | Key.AltMask;
			case Curses.ShiftAltKeyRight: return Key.CursorRight | Key.ShiftMask | Key.AltMask;
			case Curses.ShiftAltKeyNPage: return Key.PageDown | Key.ShiftMask | Key.AltMask;
			case Curses.ShiftAltKeyPPage: return Key.PageUp | Key.ShiftMask | Key.AltMask;
			case Curses.ShiftAltKeyHome: return Key.Home | Key.ShiftMask | Key.AltMask;
			case Curses.ShiftAltKeyEnd: return Key.End | Key.ShiftMask | Key.AltMask;
			case Curses.AltCtrlKeyNPage: return Key.PageDown | Key.AltMask | Key.CtrlMask;
			case Curses.AltCtrlKeyPPage: return Key.PageUp | Key.AltMask | Key.CtrlMask;
			case Curses.AltCtrlKeyHome: return Key.Home | Key.AltMask | Key.CtrlMask;
			case Curses.AltCtrlKeyEnd: return Key.End | Key.AltMask | Key.CtrlMask;
			default: return Key.Unknown;
			}
		}

		KeyModifiers keyModifiers;

		KeyModifiers MapKeyModifiers (Key key)
		{
			if (keyModifiers == null)
				keyModifiers = new KeyModifiers ();

			if (!keyModifiers.Shift && (key & Key.ShiftMask) != 0)
				keyModifiers.Shift = true;
			if (!keyModifiers.Alt && (key & Key.AltMask) != 0)
				keyModifiers.Alt = true;
			if (!keyModifiers.Ctrl && (key & Key.CtrlMask) != 0)
				keyModifiers.Ctrl = true;

			return keyModifiers;
		}

		void ProcessInput ()
		{
			int wch;
			var code = Curses.get_wch (out wch);
			//System.Diagnostics.Debug.WriteLine ($"code: {code}; wch: {wch}");
			if (code == Curses.ERR)
				return;

			keyModifiers = new KeyModifiers ();
			Key k = Key.Null;

			if (code == Curses.KEY_CODE_YES) {
				if (wch == Curses.KeyResize) {
					ProcessWinChange ();
				}
				if (wch == Curses.KeyMouse) {
					int wch2 = wch;

					while (wch2 == Curses.KeyMouse) {
						KeyEvent key = null;
						ConsoleKeyInfo [] cki = new ConsoleKeyInfo [] {
							new ConsoleKeyInfo ((char)Key.Esc, 0, false, false, false),
							new ConsoleKeyInfo ('[', 0, false, false, false),
							new ConsoleKeyInfo ('<', 0, false, false, false)
						};
						code = 0;
						GetEscSeq (ref code, ref k, ref wch2, ref key, ref cki);
					}
					return;
				}
				k = MapCursesKey (wch);
				if (wch >= 277 && wch <= 288) { // Shift+(F1 - F12)
					wch -= 12;
					k = Key.ShiftMask | MapCursesKey (wch);
				} else if (wch >= 289 && wch <= 300) { // Ctrl+(F1 - F12)
					wch -= 24;
					k = Key.CtrlMask | MapCursesKey (wch);
				} else if (wch >= 301 && wch <= 312) { // Ctrl+Shift+(F1 - F12)
					wch -= 36;
					k = Key.CtrlMask | Key.ShiftMask | MapCursesKey (wch);
				} else if (wch >= 313 && wch <= 324) { // Alt+(F1 - F12)
					wch -= 48;
					k = Key.AltMask | MapCursesKey (wch);
				} else if (wch >= 325 && wch <= 327) { // Shift+Alt+(F1 - F3)
					wch -= 60;
					k = Key.ShiftMask | Key.AltMask | MapCursesKey (wch);
				}
				keyDownHandler (new KeyEvent (k, MapKeyModifiers (k)));
				keyHandler (new KeyEvent (k, MapKeyModifiers (k)));
				keyUpHandler (new KeyEvent (k, MapKeyModifiers (k)));
				return;
			}

			// Special handling for ESC, we want to try to catch ESC+letter to simulate alt-letter as well as Alt-Fkey
			if (wch == 27) {
				Curses.timeout (10);

				code = Curses.get_wch (out int wch2);

				if (code == Curses.KEY_CODE_YES) {
					k = Key.AltMask | MapCursesKey (wch);
				}
				if (code == 0) {
					KeyEvent key = null;

					// The ESC-number handling, debatable.
					// Simulates the AltMask itself by pressing Alt + Space.
					if (wch2 == (int)Key.Space) {
						k = Key.AltMask;
					} else if (wch2 - (int)Key.Space >= (uint)Key.A && wch2 - (int)Key.Space <= (uint)Key.Z) {
						k = (Key)((uint)Key.AltMask + (wch2 - (int)Key.Space));
					} else if (wch2 >= (uint)Key.A - 64 && wch2 <= (uint)Key.Z - 64) {
						k = (Key)((uint)(Key.AltMask | Key.CtrlMask) + (wch2 + 64));
					} else if (wch2 >= (uint)Key.D0 && wch2 <= (uint)Key.D9) {
						k = (Key)((uint)Key.AltMask + (uint)Key.D0 + (wch2 - (uint)Key.D0));
					} else if (wch2 == Curses.KeyCSI) {
						ConsoleKeyInfo [] cki = new ConsoleKeyInfo [] {
							new ConsoleKeyInfo ((char)Key.Esc, 0, false, false, false),
							new ConsoleKeyInfo ('[', 0, false, false, false)
						};
						GetEscSeq (ref code, ref k, ref wch2, ref key, ref cki);
						return;
					} else {
						// Unfortunately there are no way to differentiate Ctrl+Alt+alfa and Ctrl+Shift+Alt+alfa.
						if (((Key)wch2 & Key.CtrlMask) != 0) {
							keyModifiers.Ctrl = true;
						}
						if (wch2 == 0) {
							k = Key.CtrlMask | Key.AltMask | Key.Space;
						} else if (wch >= (uint)Key.A && wch <= (uint)Key.Z) {
							keyModifiers.Shift = true;
							keyModifiers.Alt = true;
						} else if (wch2 < 256) {
							k = (Key)wch2;
							keyModifiers.Alt = true;
						} else {
							k = (Key)((uint)(Key.AltMask | Key.CtrlMask) + wch2);
						}
					}
					key = new KeyEvent (k, MapKeyModifiers (k));
					keyDownHandler (key);
					keyHandler (key);
				} else {
					k = Key.Esc;
					keyHandler (new KeyEvent (k, MapKeyModifiers (k)));
				}
			} else if (wch == Curses.KeyTab) {
				k = MapCursesKey (wch);
				keyDownHandler (new KeyEvent (k, MapKeyModifiers (k)));
				keyHandler (new KeyEvent (k, MapKeyModifiers (k)));
			} else {
				// Unfortunately there are no way to differentiate Ctrl+alfa and Ctrl+Shift+alfa.
				k = (Key)wch;
				if (wch == 0) {
					k = Key.CtrlMask | Key.Space;
				} else if (wch >= (uint)Key.A - 64 && wch <= (uint)Key.Z - 64) {
					if ((Key)(wch + 64) != Key.J) {
						k = Key.CtrlMask | (Key)(wch + 64);
					}
				} else if (wch >= (uint)Key.A && wch <= (uint)Key.Z) {
					keyModifiers.Shift = true;
				}
				keyDownHandler (new KeyEvent (k, MapKeyModifiers (k)));
				keyHandler (new KeyEvent (k, MapKeyModifiers (k)));
				keyUpHandler (new KeyEvent (k, MapKeyModifiers (k)));
			}
			// Cause OnKeyUp and OnKeyPressed. Note that the special handling for ESC above 
			// will not impact KeyUp.
			// This is causing ESC firing even if another keystroke was handled.
			//if (wch == Curses.KeyTab) {
			//	keyUpHandler (new KeyEvent (MapCursesKey (wch), keyModifiers));
			//} else {
			//	keyUpHandler (new KeyEvent ((Key)wch, keyModifiers));
			//}
		}

		void GetEscSeq (ref int code, ref Key k, ref int wch2, ref KeyEvent key, ref ConsoleKeyInfo [] cki)
		{
			ConsoleKey ck = 0;
			ConsoleModifiers mod = 0;
			while (code == 0) {
				code = Curses.get_wch (out wch2);
				var consoleKeyInfo = new ConsoleKeyInfo ((char)wch2, 0, false, false, false);
				if (wch2 == 0 || wch2 == 27 || wch2 == Curses.KeyMouse) {
					EscSeqUtils.DecodeEscSeq (null, ref consoleKeyInfo, ref ck, cki, ref mod, out _, out _, out _, out _, out bool isKeyMouse, out List<MouseFlags> mouseFlags, out Point pos, out _, ProcessContinuousButtonPressed);
					if (isKeyMouse) {
						foreach (var mf in mouseFlags) {
							ProcessMouseEvent (mf, pos);
						}
						cki = null;
						if (wch2 == 27) {
							cki = EscSeqUtils.ResizeArray (new ConsoleKeyInfo ((char)Key.Esc, 0,
								false, false, false), cki);
						}
					} else {
						k = ConsoleKeyMapping.MapConsoleKeyToKey (consoleKeyInfo.Key, out _);
						k = ConsoleKeyMapping.MapKeyModifiers (consoleKeyInfo, k);
						key = new KeyEvent (k, MapKeyModifiers (k));
						keyDownHandler (key);
						keyHandler (key);
					}
				} else {
					cki = EscSeqUtils.ResizeArray (consoleKeyInfo, cki);
				}
			}
		}

		void ProcessMouseEvent (MouseFlags mouseFlag, Point pos)
		{
			var me = new MouseEvent () {
				Flags = mouseFlag,
				X = pos.X,
				Y = pos.Y
			};
			mouseHandler (me);
		}

		void ProcessContinuousButtonPressed (MouseFlags mouseFlag, Point pos)
		{
			ProcessMouseEvent (mouseFlag, pos);
		}

		Action<KeyEvent> keyHandler;
		Action<KeyEvent> keyDownHandler;
		Action<KeyEvent> keyUpHandler;
		Action<MouseEvent> mouseHandler;

		public override void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<KeyEvent> keyDownHandler, Action<KeyEvent> keyUpHandler, Action<MouseEvent> mouseHandler)
		{
			// Note: Curses doesn't support keydown/up events and thus any passed keyDown/UpHandlers will never be called
			Curses.timeout (0);
			this.keyHandler = keyHandler;
			this.keyDownHandler = keyDownHandler;
			this.keyUpHandler = keyUpHandler;
			this.mouseHandler = mouseHandler;

			var mLoop = mainLoop.Driver as UnixMainLoop;

			mLoop.AddWatch (0, UnixMainLoop.Condition.PollIn, x => {
				ProcessInput ();
				return true;
			});

			mLoop.WinChanged += () => {
				ProcessWinChange ();
			};
		}

		public override void Init (Action terminalResized)
		{
			if (window != null)
				return;

			try {
				window = Curses.initscr ();
				Curses.set_escdelay (10);
			} catch (Exception e) {
				throw new Exception ($"Curses failed to initialize, the exception is: {e.Message}");
			}

			// Ensures that all procedures are performed at some previous closing.
			Curses.doupdate ();

			// 
			// We are setting Invisible as default so we could ignore XTerm DECSUSR setting
			//
			switch (Curses.curs_set (0)) {
			case 0:
				currentCursorVisibility = initialCursorVisibility = CursorVisibility.Invisible;
				break;

			case 1:
				currentCursorVisibility = initialCursorVisibility = CursorVisibility.Underline;
				Curses.curs_set (1);
				break;

			case 2:
				currentCursorVisibility = initialCursorVisibility = CursorVisibility.Box;
				Curses.curs_set (2);
				break;

			default:
				currentCursorVisibility = initialCursorVisibility = null;
				break;
			}

			if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
				clipboard = new MacOSXClipboard ();
			} else {
				if (Is_WSL_Platform ()) {
					clipboard = new WSLClipboard ();
				} else {
					clipboard = new CursesClipboard ();
				}
			}

			Curses.raw ();
			Curses.noecho ();

			Curses.Window.Standard.keypad (true);
			TerminalResized = terminalResized;
			StartReportingMouseMoves ();

			CurrentAttribute = MakeColor (Color.White, Color.Black);

			if (Curses.HasColors) {
				Curses.StartColor ();
				Curses.UseDefaultColors ();

				InitalizeColorSchemes ();
			} else {
				InitalizeColorSchemes (false);

				// BUGBUG: This is a hack to make the colors work on the Mac?
				// The new Theme support overwrites these colors, so this is not needed?
				Colors.TopLevel.Normal = Curses.COLOR_GREEN;
				Colors.TopLevel.Focus = Curses.COLOR_WHITE;
				Colors.TopLevel.HotNormal = Curses.COLOR_YELLOW;
				Colors.TopLevel.HotFocus = Curses.COLOR_YELLOW;
				Colors.TopLevel.Disabled = Curses.A_BOLD | Curses.COLOR_GRAY;
				Colors.Base.Normal = Curses.A_NORMAL;
				Colors.Base.Focus = Curses.A_REVERSE;
				Colors.Base.HotNormal = Curses.A_BOLD;
				Colors.Base.HotFocus = Curses.A_BOLD | Curses.A_REVERSE;
				Colors.Base.Disabled = Curses.A_BOLD | Curses.COLOR_GRAY;
				Colors.Menu.Normal = Curses.A_REVERSE;
				Colors.Menu.Focus = Curses.A_NORMAL;
				Colors.Menu.HotNormal = Curses.A_BOLD;
				Colors.Menu.HotFocus = Curses.A_NORMAL;
				Colors.Menu.Disabled = Curses.A_BOLD | Curses.COLOR_GRAY;
				Colors.Dialog.Normal = Curses.A_REVERSE;
				Colors.Dialog.Focus = Curses.A_NORMAL;
				Colors.Dialog.HotNormal = Curses.A_BOLD;
				Colors.Dialog.HotFocus = Curses.A_NORMAL;
				Colors.Dialog.Disabled = Curses.A_BOLD | Curses.COLOR_GRAY;
				Colors.Error.Normal = Curses.A_BOLD;
				Colors.Error.Focus = Curses.A_BOLD | Curses.A_REVERSE;
				Colors.Error.HotNormal = Curses.A_BOLD | Curses.A_REVERSE;
				Colors.Error.HotFocus = Curses.A_REVERSE;
				Colors.Error.Disabled = Curses.A_BOLD | Curses.COLOR_GRAY;
			}

			ResizeScreen ();
			UpdateOffScreen ();

		}

		public override void ResizeScreen ()
		{
			Clip = new Rect (0, 0, Cols, Rows);
			Curses.refresh ();
		}

		public override void UpdateOffScreen ()
		{
			contents = new int [Rows, Cols, 3];
			for (int row = 0; row < Rows; row++) {
				for (int col = 0; col < Cols; col++) {
					//Curses.move (row, col);
					//Curses.attrset (Colors.TopLevel.Normal);
					//Curses.addch ((int)(uint)' ');
					contents [row, col, 0] = ' ';
					contents [row, col, 1] = Colors.TopLevel.Normal;
					contents [row, col, 2] = 0;
				}
			}
		}

		public static bool Is_WSL_Platform ()
		{
			// xclip does not work on WSL, so we need to use the Windows clipboard vis Powershell
			//if (new CursesClipboard ().IsSupported) {
			//	// If xclip is installed on Linux under WSL, this will return true.
			//	return false;
			//}
			var (exitCode, result) = ClipboardProcessRunner.Bash ("uname -a", waitForOutput: true);
			if (exitCode == 0 && result.Contains ("microsoft") && result.Contains ("WSL")) {
				return true;
			}
			return false;
		}

		static int MapColor (Color color)
		{
			switch (color) {
			case Color.Black:
				return Curses.COLOR_BLACK;
			case Color.Blue:
				return Curses.COLOR_BLUE;
			case Color.Green:
				return Curses.COLOR_GREEN;
			case Color.Cyan:
				return Curses.COLOR_CYAN;
			case Color.Red:
				return Curses.COLOR_RED;
			case Color.Magenta:
				return Curses.COLOR_MAGENTA;
			case Color.Brown:
				return Curses.COLOR_YELLOW;
			case Color.Gray:
				return Curses.COLOR_WHITE;
			case Color.DarkGray:
				//return Curses.COLOR_BLACK | Curses.A_BOLD;
				return Curses.COLOR_GRAY;
			case Color.BrightBlue:
				return Curses.COLOR_BLUE | Curses.A_BOLD | Curses.COLOR_GRAY;
			case Color.BrightGreen:
				return Curses.COLOR_GREEN | Curses.A_BOLD | Curses.COLOR_GRAY;
			case Color.BrightCyan:
				return Curses.COLOR_CYAN | Curses.A_BOLD | Curses.COLOR_GRAY;
			case Color.BrightRed:
				return Curses.COLOR_RED | Curses.A_BOLD | Curses.COLOR_GRAY;
			case Color.BrightMagenta:
				return Curses.COLOR_MAGENTA | Curses.A_BOLD | Curses.COLOR_GRAY;
			case Color.BrightYellow:
				return Curses.COLOR_YELLOW | Curses.A_BOLD | Curses.COLOR_GRAY;
			case Color.White:
				return Curses.COLOR_WHITE | Curses.A_BOLD | Curses.COLOR_GRAY;
			}
			throw new ArgumentException ("Invalid color code");
		}

		static Color MapCursesColor (int color)
		{
			switch (color) {
			case Curses.COLOR_BLACK:
				return Color.Black;
			case Curses.COLOR_BLUE:
				return Color.Blue;
			case Curses.COLOR_GREEN:
				return Color.Green;
			case Curses.COLOR_CYAN:
				return Color.Cyan;
			case Curses.COLOR_RED:
				return Color.Red;
			case Curses.COLOR_MAGENTA:
				return Color.Magenta;
			case Curses.COLOR_YELLOW:
				return Color.Brown;
			case Curses.COLOR_WHITE:
				return Color.Gray;
			case Curses.COLOR_GRAY:
				return Color.DarkGray;
			case Curses.COLOR_BLUE | Curses.COLOR_GRAY:
				return Color.BrightBlue;
			case Curses.COLOR_GREEN | Curses.COLOR_GRAY:
				return Color.BrightGreen;
			case Curses.COLOR_CYAN | Curses.COLOR_GRAY:
				return Color.BrightCyan;
			case Curses.COLOR_RED | Curses.COLOR_GRAY:
				return Color.BrightRed;
			case Curses.COLOR_MAGENTA | Curses.COLOR_GRAY:
				return Color.BrightMagenta;
			case Curses.COLOR_YELLOW | Curses.COLOR_GRAY:
				return Color.BrightYellow;
			case Curses.COLOR_WHITE | Curses.COLOR_GRAY:
				return Color.White;
			}
			throw new ArgumentException ("Invalid curses color code");
		}

		public override Attribute MakeAttribute (Color fore, Color back)
		{
			var f = MapColor (fore);
			//return MakeColor ((short)(f & 0xffff), (short)MapColor (back)) | ((f & Curses.A_BOLD) != 0 ? Curses.A_BOLD : 0);
			return MakeColor ((short)(f & 0xffff), (short)MapColor (back));
		}

		public override void Suspend ()
		{
			StopReportingMouseMoves ();
			Platform.Suspend ();
			Curses.Window.Standard.redrawwin ();
			Curses.refresh ();
			StartReportingMouseMoves ();
		}

		public override void StartReportingMouseMoves ()
		{
			Console.Out.Write (EscSeqUtils.EnableMouseEvents);
		}

		public override void StopReportingMouseMoves ()
		{
			Console.Out.Write (EscSeqUtils.DisableMouseEvents);
		}

		//int lastMouseInterval;
		//bool mouseGrabbed;

		public override void UncookMouse ()
		{
			//if (mouseGrabbed)
			//	return;
			//lastMouseInterval = Curses.mouseinterval (0);
			//mouseGrabbed = true;
		}

		public override void CookMouse ()
		{
			//mouseGrabbed = false;
			//Curses.mouseinterval (lastMouseInterval);
		}

		/// <inheritdoc/>
		public override bool GetCursorVisibility (out CursorVisibility visibility)
		{
			visibility = CursorVisibility.Invisible;

			if (!currentCursorVisibility.HasValue)
				return false;

			visibility = currentCursorVisibility.Value;

			return true;
		}

		/// <inheritdoc/>
		public override bool SetCursorVisibility (CursorVisibility visibility)
		{
			if (initialCursorVisibility.HasValue == false)
				return false;

			Curses.curs_set (((int)visibility >> 16) & 0x000000FF);

			if (visibility != CursorVisibility.Invisible) {
				Console.Out.Write ("\x1b[{0} q", ((int)visibility >> 24) & 0xFF);
			}

			currentCursorVisibility = visibility;

			return true;
		}

		/// <inheritdoc/>
		public override bool EnsureCursorVisibility ()
		{
			return false;
		}

		public override void SendKeys (char keyChar, ConsoleKey consoleKey, bool shift, bool alt, bool control)
		{
			Key key;

			if (consoleKey == ConsoleKey.Packet) {
				ConsoleModifiers mod = new ConsoleModifiers ();
				if (shift) {
					mod |= ConsoleModifiers.Shift;
				}
				if (alt) {
					mod |= ConsoleModifiers.Alt;
				}
				if (control) {
					mod |= ConsoleModifiers.Control;
				}
				var kchar = ConsoleKeyMapping.GetKeyCharFromConsoleKey (keyChar, mod, out uint ckey, out _);
				key = ConsoleKeyMapping.MapConsoleKeyToKey ((ConsoleKey)ckey, out bool mappable);
				if (mappable) {
					key = (Key)kchar;
				}
			} else {
				key = (Key)keyChar;
			}

			KeyModifiers km = new KeyModifiers ();
			if (shift) {
				if (keyChar == 0) {
					key |= Key.ShiftMask;
				}
				km.Shift = shift;
			}
			if (alt) {
				key |= Key.AltMask;
				km.Alt = alt;
			}
			if (control) {
				key |= Key.CtrlMask;
				km.Ctrl = control;
			}
			keyDownHandler (new KeyEvent (key, km));
			keyHandler (new KeyEvent (key, km));
			keyUpHandler (new KeyEvent (key, km));
		}

		public override bool GetColors (int value, out Color foreground, out Color background)
		{
			bool hasColor = false;
			foreground = default;
			background = default;
			int back = -1;
			IEnumerable<int> values = Enum.GetValues (typeof (ConsoleColor))
				.OfType<ConsoleColor> ()
				.Select (s => (int)s);
			if (values.Contains ((value >> 12) & 0xffff)) {
				hasColor = true;
				back = (value >> 12) & 0xffff;
				background = MapCursesColor (back);
			}
			if (values.Contains ((value - (back << 12)) >> 8)) {
				hasColor = true;
				foreground = MapCursesColor ((value - (back << 12)) >> 8);
			}
			return hasColor;
		}
	}

	internal static class Platform {
		[DllImport ("libc")]
		static extern int uname (IntPtr buf);

		[DllImport ("libc")]
		static extern int killpg (int pgrp, int pid);

		static int suspendSignal;

		static int GetSuspendSignal ()
		{
			if (suspendSignal != 0)
				return suspendSignal;

			IntPtr buf = Marshal.AllocHGlobal (8192);
			if (uname (buf) != 0) {
				Marshal.FreeHGlobal (buf);
				suspendSignal = -1;
				return suspendSignal;
			}
			try {
				switch (Marshal.PtrToStringAnsi (buf)) {
				case "Darwin":
				case "DragonFly":
				case "FreeBSD":
				case "NetBSD":
				case "OpenBSD":
					suspendSignal = 18;
					break;
				case "Linux":
					// TODO: should fetch the machine name and
					// if it is MIPS return 24
					suspendSignal = 20;
					break;
				case "Solaris":
					suspendSignal = 24;
					break;
				default:
					suspendSignal = -1;
					break;
				}
				return suspendSignal;
			} finally {
				Marshal.FreeHGlobal (buf);
			}
		}

		/// <summary>
		/// Suspends the process by sending SIGTSTP to itself
		/// </summary>
		/// <returns>The suspend.</returns>
		static public bool Suspend ()
		{
			int signal = GetSuspendSignal ();
			if (signal == -1)
				return false;
			killpg (0, signal);
			return true;
		}
	}

	/// <summary>
	///  A clipboard implementation for Linux.
	///  This implementation uses the xclip command to access the clipboard.
	/// </summary>	
	/// <remarks>
	/// If xclip is not installed, this implementation will not work.
	/// </remarks>
	class CursesClipboard : ClipboardBase {
		public CursesClipboard ()
		{
			IsSupported = CheckSupport ();
		}

		string xclipPath = string.Empty;
		public override bool IsSupported { get; }

		bool CheckSupport ()
		{
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			try {
				var (exitCode, result) = ClipboardProcessRunner.Bash ("which xclip", waitForOutput: true);
				if (exitCode == 0 && result.FileExists ()) {
					xclipPath = result;
					return true;
				}
			} catch (Exception) {
				// Permissions issue.
			}
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
			return false;
		}

		protected override string GetClipboardDataImpl ()
		{
			var tempFileName = System.IO.Path.GetTempFileName ();
			var xclipargs = "-selection clipboard -o";

			try {
				var (exitCode, result) = ClipboardProcessRunner.Bash ($"{xclipPath} {xclipargs} > {tempFileName}", waitForOutput: false);
				if (exitCode == 0) {
					if (Application.Driver is CursesDriver) {
						Curses.raw ();
						Curses.noecho ();
					}
					return System.IO.File.ReadAllText (tempFileName);
				}
			} catch (Exception e) {
				throw new NotSupportedException ($"\"{xclipPath} {xclipargs}\" failed.", e);
			} finally {
				System.IO.File.Delete (tempFileName);
			}
			return string.Empty;
		}

		protected override void SetClipboardDataImpl (string text)
		{
			var xclipargs = "-selection clipboard -i";
			try {
				var (exitCode, _) = ClipboardProcessRunner.Bash ($"{xclipPath} {xclipargs}", text, waitForOutput: false);
				if (exitCode == 0 && Application.Driver is CursesDriver) {
					Curses.raw ();
					Curses.noecho ();
				}
			} catch (Exception e) {
				throw new NotSupportedException ($"\"{xclipPath} {xclipargs} < {text}\" failed", e);
			}
		}
	}

	/// <summary>
	///  A clipboard implementation for MacOSX. 
	///  This implementation uses the Mac clipboard API (via P/Invoke) to copy/paste.
	///  The existance of the Mac pbcopy and pbpaste commands 
	///  is used to determine if copy/paste is supported.
	/// </summary>	
	class MacOSXClipboard : ClipboardBase {
		IntPtr nsString = objc_getClass ("NSString");
		IntPtr nsPasteboard = objc_getClass ("NSPasteboard");
		IntPtr utfTextType;
		IntPtr generalPasteboard;
		IntPtr initWithUtf8Register = sel_registerName ("initWithUTF8String:");
		IntPtr allocRegister = sel_registerName ("alloc");
		IntPtr setStringRegister = sel_registerName ("setString:forType:");
		IntPtr stringForTypeRegister = sel_registerName ("stringForType:");
		IntPtr utf8Register = sel_registerName ("UTF8String");
		IntPtr nsStringPboardType;
		IntPtr generalPasteboardRegister = sel_registerName ("generalPasteboard");
		IntPtr clearContentsRegister = sel_registerName ("clearContents");

		public MacOSXClipboard ()
		{
			utfTextType = objc_msgSend (objc_msgSend (nsString, allocRegister), initWithUtf8Register, "public.utf8-plain-text");
			nsStringPboardType = objc_msgSend (objc_msgSend (nsString, allocRegister), initWithUtf8Register, "NSStringPboardType");
			generalPasteboard = objc_msgSend (nsPasteboard, generalPasteboardRegister);
			IsSupported = CheckSupport ();
		}

		public override bool IsSupported { get; }

		bool CheckSupport ()
		{
			var (exitCode, result) = ClipboardProcessRunner.Bash ("which pbcopy", waitForOutput: true);
			if (exitCode != 0 || !result.FileExists ()) {
				return false;
			}
			(exitCode, result) = ClipboardProcessRunner.Bash ("which pbpaste", waitForOutput: true);
			return exitCode == 0 && result.FileExists ();
		}

		protected override string GetClipboardDataImpl ()
		{
			var ptr = objc_msgSend (generalPasteboard, stringForTypeRegister, nsStringPboardType);
			var charArray = objc_msgSend (ptr, utf8Register);
			return Marshal.PtrToStringAnsi (charArray);
		}

		protected override void SetClipboardDataImpl (string text)
		{
			IntPtr str = default;
			try {
				str = objc_msgSend (objc_msgSend (nsString, allocRegister), initWithUtf8Register, text);
				objc_msgSend (generalPasteboard, clearContentsRegister);
				objc_msgSend (generalPasteboard, setStringRegister, str, utfTextType);
			} finally {
				if (str != default) {
					objc_msgSend (str, sel_registerName ("release"));
				}
			}
		}

		[DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
		static extern IntPtr objc_getClass (string className);

		[DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
		static extern IntPtr objc_msgSend (IntPtr receiver, IntPtr selector);

		[DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
		static extern IntPtr objc_msgSend (IntPtr receiver, IntPtr selector, string arg1);

		[DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
		static extern IntPtr objc_msgSend (IntPtr receiver, IntPtr selector, IntPtr arg1);

		[DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
		static extern IntPtr objc_msgSend (IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

		[DllImport ("/System/Library/Frameworks/AppKit.framework/AppKit")]
		static extern IntPtr sel_registerName (string selectorName);
	}

	/// <summary>
	///  A clipboard implementation for Linux, when running under WSL. 
	///  This implementation uses the Windows clipboard to store the data, and uses Windows'
	///  powershell.exe (launched via WSL interop services) to set/get the Windows
	///  clipboard. 
	/// </summary>
	class WSLClipboard : ClipboardBase {
		bool isSupported = false;
		public WSLClipboard ()
		{
			isSupported = CheckSupport ();
		}

		public override bool IsSupported {
			get {
				return isSupported = CheckSupport ();
			}
		}

		private static string powershellPath = string.Empty;

		bool CheckSupport ()
		{
			if (string.IsNullOrEmpty (powershellPath)) {
				// Specify pwsh.exe (not pwsh) to ensure we get the Windows version (invoked via WSL)
				var (exitCode, result) = ClipboardProcessRunner.Bash ("which pwsh.exe", waitForOutput: true);
				if (exitCode > 0) {
					(exitCode, result) = ClipboardProcessRunner.Bash ("which powershell.exe", waitForOutput: true);
				}

				if (exitCode == 0) {
					powershellPath = result;
				}
			}
			return !string.IsNullOrEmpty (powershellPath);
		}

		protected override string GetClipboardDataImpl ()
		{
			if (!IsSupported) {
				return string.Empty;
			}

			var (exitCode, output) = ClipboardProcessRunner.Process (powershellPath, "-noprofile -command \"Get-Clipboard\"");
			if (exitCode == 0) {
				if (Application.Driver is CursesDriver) {
					Curses.raw ();
					Curses.noecho ();
				}

				if (output.EndsWith ("\r\n")) {
					output = output.Substring (0, output.Length - 2);
				}
				return output;
			}
			return string.Empty;
		}

		protected override void SetClipboardDataImpl (string text)
		{
			if (!IsSupported) {
				return;
			}

			var (exitCode, output) = ClipboardProcessRunner.Process (powershellPath, $"-noprofile -command \"Set-Clipboard -Value \\\"{text}\\\"\"");
			if (exitCode == 0) {
				if (Application.Driver is CursesDriver) {
					Curses.raw ();
					Curses.noecho ();
				}
			}
		}
	}
}
