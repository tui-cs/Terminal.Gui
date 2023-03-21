//
// WindowsDriver.cs: Windows specific driver
//
using NStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Terminal.Gui {

	internal class WindowsConsole {
		public const int STD_OUTPUT_HANDLE = -11;
		public const int STD_INPUT_HANDLE = -10;
		public const int STD_ERROR_HANDLE = -12;

		internal IntPtr InputHandle, OutputHandle;
		IntPtr ScreenBuffer;
		readonly uint originalConsoleMode;
		CursorVisibility? initialCursorVisibility = null;
		CursorVisibility? currentCursorVisibility = null;
		CursorVisibility? pendingCursorVisibility = null;

		public WindowsConsole ()
		{
			InputHandle = GetStdHandle (STD_INPUT_HANDLE);
			OutputHandle = GetStdHandle (STD_OUTPUT_HANDLE);
			originalConsoleMode = ConsoleMode;
			var newConsoleMode = originalConsoleMode;
			newConsoleMode |= (uint)(ConsoleModes.EnableMouseInput | ConsoleModes.EnableExtendedFlags);
			newConsoleMode &= ~(uint)ConsoleModes.EnableQuickEditMode;
			newConsoleMode &= ~(uint)ConsoleModes.EnableProcessedInput;
			ConsoleMode = newConsoleMode;
		}

		public CharInfo [] OriginalStdOutChars;

		public bool WriteToConsole (Size size, CharInfo [] charInfoBuffer, Coord coords, SmallRect window)
		{
			if (ScreenBuffer == IntPtr.Zero) {
				ReadFromConsoleOutput (size, coords, ref window);
			}

			return WriteConsoleOutput (ScreenBuffer, charInfoBuffer, coords, new Coord () { X = window.Left, Y = window.Top }, ref window);
		}

		public void ReadFromConsoleOutput (Size size, Coord coords, ref SmallRect window)
		{
			ScreenBuffer = CreateConsoleScreenBuffer (
				DesiredAccess.GenericRead | DesiredAccess.GenericWrite,
				ShareMode.FileShareRead | ShareMode.FileShareWrite,
				IntPtr.Zero,
				1,
				IntPtr.Zero
			);
			if (ScreenBuffer == INVALID_HANDLE_VALUE) {
				var err = Marshal.GetLastWin32Error ();

				if (err != 0)
					throw new System.ComponentModel.Win32Exception (err);
			}

			if (!initialCursorVisibility.HasValue && GetCursorVisibility (out CursorVisibility visibility)) {
				initialCursorVisibility = visibility;
			}

			if (!SetConsoleActiveScreenBuffer (ScreenBuffer)) {
				throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
			}

			OriginalStdOutChars = new CharInfo [size.Height * size.Width];

			if (!ReadConsoleOutput (ScreenBuffer, OriginalStdOutChars, coords, new Coord () { X = 0, Y = 0 }, ref window)) {
				throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
			}
		}

		public bool SetCursorPosition (Coord position)
		{
			return SetConsoleCursorPosition (ScreenBuffer, position);
		}

		public void SetInitialCursorVisibility ()
		{
			if (initialCursorVisibility.HasValue == false && GetCursorVisibility (out CursorVisibility visibility)) {
				initialCursorVisibility = visibility;
			}
		}

		public bool GetCursorVisibility (out CursorVisibility visibility)
		{
			if (ScreenBuffer == IntPtr.Zero) {
				visibility = CursorVisibility.Invisible;
				return false;
			}
			if (!GetConsoleCursorInfo (ScreenBuffer, out ConsoleCursorInfo info)) {
				var err = Marshal.GetLastWin32Error ();
				if (err != 0) {
					throw new System.ComponentModel.Win32Exception (err);
				}
				visibility = Gui.CursorVisibility.Default;

				return false;
			}

			if (!info.bVisible)
				visibility = CursorVisibility.Invisible;
			else if (info.dwSize > 50)
				visibility = CursorVisibility.Box;
			else
				visibility = CursorVisibility.Underline;

			return true;
		}

		public bool EnsureCursorVisibility ()
		{
			if (initialCursorVisibility.HasValue && pendingCursorVisibility.HasValue && SetCursorVisibility (pendingCursorVisibility.Value)) {
				pendingCursorVisibility = null;

				return true;
			}

			return false;
		}

		public void ForceRefreshCursorVisibility ()
		{
			if (currentCursorVisibility.HasValue) {
				pendingCursorVisibility = currentCursorVisibility;
				currentCursorVisibility = null;
			}
		}

		public bool SetCursorVisibility (CursorVisibility visibility)
		{
			if (initialCursorVisibility.HasValue == false) {
				pendingCursorVisibility = visibility;

				return false;
			}

			if (currentCursorVisibility.HasValue == false || currentCursorVisibility.Value != visibility) {
				ConsoleCursorInfo info = new ConsoleCursorInfo {
					dwSize = (uint)visibility & 0x00FF,
					bVisible = ((uint)visibility & 0xFF00) != 0
				};

				if (!SetConsoleCursorInfo (ScreenBuffer, ref info))
					return false;

				currentCursorVisibility = visibility;
			}

			return true;
		}

		public void Cleanup ()
		{
			if (initialCursorVisibility.HasValue) {
				SetCursorVisibility (initialCursorVisibility.Value);
			}

			SetConsoleOutputWindow (out _);

			ConsoleMode = originalConsoleMode;
			//ContinueListeningForConsoleEvents = false;
			if (!SetConsoleActiveScreenBuffer (OutputHandle)) {
				var err = Marshal.GetLastWin32Error ();
				Console.WriteLine ("Error: {0}", err);
			}

			if (ScreenBuffer != IntPtr.Zero)
				CloseHandle (ScreenBuffer);

			ScreenBuffer = IntPtr.Zero;
		}

		internal Size GetConsoleBufferWindow (out Point position)
		{
			if (ScreenBuffer == IntPtr.Zero) {
				position = Point.Empty;
				return Size.Empty;
			}

			var csbi = new CONSOLE_SCREEN_BUFFER_INFOEX ();
			csbi.cbSize = (uint)Marshal.SizeOf (csbi);
			if (!GetConsoleScreenBufferInfoEx (ScreenBuffer, ref csbi)) {
				//throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
				position = Point.Empty;
				return Size.Empty;
			}
			var sz = new Size (csbi.srWindow.Right - csbi.srWindow.Left + 1,
				csbi.srWindow.Bottom - csbi.srWindow.Top + 1);
			position = new Point (csbi.srWindow.Left, csbi.srWindow.Top);

			return sz;
		}

		internal Size GetConsoleOutputWindow (out Point position)
		{
			var csbi = new CONSOLE_SCREEN_BUFFER_INFOEX ();
			csbi.cbSize = (uint)Marshal.SizeOf (csbi);
			if (!GetConsoleScreenBufferInfoEx (OutputHandle, ref csbi)) {
				throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
			}
			var sz = new Size (csbi.srWindow.Right - csbi.srWindow.Left + 1,
				csbi.srWindow.Bottom - csbi.srWindow.Top + 1);
			position = new Point (csbi.srWindow.Left, csbi.srWindow.Top);

			return sz;
		}

		internal Size SetConsoleWindow (short cols, short rows)
		{
			var csbi = new CONSOLE_SCREEN_BUFFER_INFOEX ();
			csbi.cbSize = (uint)Marshal.SizeOf (csbi);
			if (!GetConsoleScreenBufferInfoEx (ScreenBuffer, ref csbi)) {
				throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
			}
			var maxWinSize = GetLargestConsoleWindowSize (ScreenBuffer);
			var newCols = Math.Min (cols, maxWinSize.X);
			var newRows = Math.Min (rows, maxWinSize.Y);
			csbi.dwSize = new Coord (newCols, Math.Max (newRows, (short)1));
			csbi.srWindow = new SmallRect (0, 0, newCols, newRows);
			csbi.dwMaximumWindowSize = new Coord (newCols, newRows);
			if (!SetConsoleScreenBufferInfoEx (ScreenBuffer, ref csbi)) {
				throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
			}
			var winRect = new SmallRect (0, 0, (short)(newCols - 1), (short)Math.Max (newRows - 1, 0));
			if (!SetConsoleWindowInfo (OutputHandle, true, ref winRect)) {
				//throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
				return new Size (cols, rows);
			}
			SetConsoleOutputWindow (csbi);
			return new Size (winRect.Right + 1, newRows - 1 < 0 ? 0 : winRect.Bottom + 1);
		}

		void SetConsoleOutputWindow (CONSOLE_SCREEN_BUFFER_INFOEX csbi)
		{
			if (ScreenBuffer != IntPtr.Zero && !SetConsoleScreenBufferInfoEx (ScreenBuffer, ref csbi)) {
				throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
			}
		}

		internal Size SetConsoleOutputWindow (out Point position)
		{
			if (ScreenBuffer == IntPtr.Zero) {
				position = Point.Empty;
				return Size.Empty;
			}

			var csbi = new CONSOLE_SCREEN_BUFFER_INFOEX ();
			csbi.cbSize = (uint)Marshal.SizeOf (csbi);
			if (!GetConsoleScreenBufferInfoEx (ScreenBuffer, ref csbi)) {
				throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
			}
			var sz = new Size (csbi.srWindow.Right - csbi.srWindow.Left + 1,
				Math.Max (csbi.srWindow.Bottom - csbi.srWindow.Top + 1, 0));
			position = new Point (csbi.srWindow.Left, csbi.srWindow.Top);
			SetConsoleOutputWindow (csbi);
			var winRect = new SmallRect (0, 0, (short)(sz.Width - 1), (short)Math.Max (sz.Height - 1, 0));
			if (!SetConsoleScreenBufferInfoEx (OutputHandle, ref csbi)) {
				throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
			}
			if (!SetConsoleWindowInfo (OutputHandle, true, ref winRect)) {
				throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
			}

			return sz;
		}

		//bool ContinueListeningForConsoleEvents = true;

		public uint ConsoleMode {
			get {
				GetConsoleMode (InputHandle, out uint v);
				return v;
			}
			set {
				SetConsoleMode (InputHandle, value);
			}
		}

		[Flags]
		public enum ConsoleModes : uint {
			EnableProcessedInput = 1,
			EnableMouseInput = 16,
			EnableQuickEditMode = 64,
			EnableExtendedFlags = 128,
		}

		[StructLayout (LayoutKind.Explicit, CharSet = CharSet.Unicode)]
		public struct KeyEventRecord {
			[FieldOffset (0), MarshalAs (UnmanagedType.Bool)]
			public bool bKeyDown;
			[FieldOffset (4), MarshalAs (UnmanagedType.U2)]
			public ushort wRepeatCount;
			[FieldOffset (6), MarshalAs (UnmanagedType.U2)]
			public ushort wVirtualKeyCode;
			[FieldOffset (8), MarshalAs (UnmanagedType.U2)]
			public ushort wVirtualScanCode;
			[FieldOffset (10)]
			public char UnicodeChar;
			[FieldOffset (12), MarshalAs (UnmanagedType.U4)]
			public ControlKeyState dwControlKeyState;
		}

		[Flags]
		public enum ButtonState {
			Button1Pressed = 1,
			Button2Pressed = 4,
			Button3Pressed = 8,
			Button4Pressed = 16,
			RightmostButtonPressed = 2
		}

		[Flags]
		public enum ControlKeyState {
			RightAltPressed = 1,
			LeftAltPressed = 2,
			RightControlPressed = 4,
			LeftControlPressed = 8,
			ShiftPressed = 16,
			NumlockOn = 32,
			ScrolllockOn = 64,
			CapslockOn = 128,
			EnhancedKey = 256
		}

		[Flags]
		public enum EventFlags {
			MouseMoved = 1,
			DoubleClick = 2,
			MouseWheeled = 4,
			MouseHorizontalWheeled = 8
		}

		[StructLayout (LayoutKind.Explicit)]
		public struct MouseEventRecord {
			[FieldOffset (0)]
			public Coord MousePosition;
			[FieldOffset (4)]
			public ButtonState ButtonState;
			[FieldOffset (8)]
			public ControlKeyState ControlKeyState;
			[FieldOffset (12)]
			public EventFlags EventFlags;

			public override string ToString ()
			{
				return $"[Mouse({MousePosition},{ButtonState},{ControlKeyState},{EventFlags}";
			}
		}

		public struct WindowBufferSizeRecord {
			public Coord size;

			public WindowBufferSizeRecord (short x, short y)
			{
				this.size = new Coord (x, y);
			}

			public override string ToString () => $"[WindowBufferSize{size}";
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct MenuEventRecord {
			public uint dwCommandId;
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct FocusEventRecord {
			public uint bSetFocus;
		}

		public enum EventType : ushort {
			Focus = 0x10,
			Key = 0x1,
			Menu = 0x8,
			Mouse = 2,
			WindowBufferSize = 4
		}

		[StructLayout (LayoutKind.Explicit)]
		public struct InputRecord {
			[FieldOffset (0)]
			public EventType EventType;
			[FieldOffset (4)]
			public KeyEventRecord KeyEvent;
			[FieldOffset (4)]
			public MouseEventRecord MouseEvent;
			[FieldOffset (4)]
			public WindowBufferSizeRecord WindowBufferSizeEvent;
			[FieldOffset (4)]
			public MenuEventRecord MenuEvent;
			[FieldOffset (4)]
			public FocusEventRecord FocusEvent;

			public override string ToString ()
			{
				switch (EventType) {
				case EventType.Focus:
					return FocusEvent.ToString ();
				case EventType.Key:
					return KeyEvent.ToString ();
				case EventType.Menu:
					return MenuEvent.ToString ();
				case EventType.Mouse:
					return MouseEvent.ToString ();
				case EventType.WindowBufferSize:
					return WindowBufferSizeEvent.ToString ();
				default:
					return "Unknown event type: " + EventType;
				}
			}
		};

		[Flags]
		enum ShareMode : uint {
			FileShareRead = 1,
			FileShareWrite = 2,
		}

		[Flags]
		enum DesiredAccess : uint {
			GenericRead = 2147483648,
			GenericWrite = 1073741824,
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct ConsoleScreenBufferInfo {
			public Coord dwSize;
			public Coord dwCursorPosition;
			public ushort wAttributes;
			public SmallRect srWindow;
			public Coord dwMaximumWindowSize;
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct Coord {
			public short X;
			public short Y;

			public Coord (short X, short Y)
			{
				this.X = X;
				this.Y = Y;
			}
			public override string ToString () => $"({X},{Y})";
		};

		[StructLayout (LayoutKind.Explicit, CharSet = CharSet.Unicode)]
		public struct CharUnion {
			[FieldOffset (0)] public char UnicodeChar;
			[FieldOffset (0)] public byte AsciiChar;
		}

		[StructLayout (LayoutKind.Explicit, CharSet = CharSet.Unicode)]
		public struct CharInfo {
			[FieldOffset (0)] public CharUnion Char;
			[FieldOffset (2)] public ushort Attributes;
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct SmallRect {
			public short Left;
			public short Top;
			public short Right;
			public short Bottom;

			public SmallRect (short left, short top, short right, short bottom)
			{
				Left = left;
				Top = top;
				Right = right;
				Bottom = bottom;
			}

			public static void MakeEmpty (ref SmallRect rect)
			{
				rect.Left = -1;
			}

			public static void Update (ref SmallRect rect, short col, short row)
			{
				if (rect.Left == -1) {
					//System.Diagnostics.Debugger.Log (0, "debug", $"damager From Empty {col},{row}\n");
					rect.Left = rect.Right = col;
					rect.Bottom = rect.Top = row;
					return;
				}
				if (col >= rect.Left && col <= rect.Right && row >= rect.Top && row <= rect.Bottom)
					return;
				if (col < rect.Left)
					rect.Left = col;
				if (col > rect.Right)
					rect.Right = col;
				if (row < rect.Top)
					rect.Top = row;
				if (row > rect.Bottom)
					rect.Bottom = row;
				//System.Diagnostics.Debugger.Log (0, "debug", $"Expanding {rect.ToString ()}\n");
			}

			public override string ToString ()
			{
				return $"Left={Left},Top={Top},Right={Right},Bottom={Bottom}";
			}
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct ConsoleKeyInfoEx {
			public ConsoleKeyInfo consoleKeyInfo;
			public bool CapsLock;
			public bool NumLock;
			public bool Scrolllock;

			public ConsoleKeyInfoEx (ConsoleKeyInfo consoleKeyInfo, bool capslock, bool numlock, bool scrolllock)
			{
				this.consoleKeyInfo = consoleKeyInfo;
				CapsLock = capslock;
				NumLock = numlock;
				Scrolllock = scrolllock;
			}
		}

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern IntPtr GetStdHandle (int nStdHandle);

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern bool CloseHandle (IntPtr handle);

		[DllImport ("kernel32.dll", EntryPoint = "ReadConsoleInputW", CharSet = CharSet.Unicode)]
		public static extern bool ReadConsoleInput (
			IntPtr hConsoleInput,
			IntPtr lpBuffer,
			uint nLength,
			out uint lpNumberOfEventsRead);

		[DllImport ("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern bool ReadConsoleOutput (
			IntPtr hConsoleOutput,
			[Out] CharInfo [] lpBuffer,
			Coord dwBufferSize,
			Coord dwBufferCoord,
			ref SmallRect lpReadRegion
		);

		[DllImport ("kernel32.dll", EntryPoint = "WriteConsoleOutput", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern bool WriteConsoleOutput (
			IntPtr hConsoleOutput,
			CharInfo [] lpBuffer,
			Coord dwBufferSize,
			Coord dwBufferCoord,
			ref SmallRect lpWriteRegion
		);

		[DllImport ("kernel32.dll")]
		static extern bool SetConsoleCursorPosition (IntPtr hConsoleOutput, Coord dwCursorPosition);

		[StructLayout (LayoutKind.Sequential)]
		public struct ConsoleCursorInfo {
			public uint dwSize;
			public bool bVisible;
		}

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern bool SetConsoleCursorInfo (IntPtr hConsoleOutput, [In] ref ConsoleCursorInfo lpConsoleCursorInfo);

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern bool GetConsoleCursorInfo (IntPtr hConsoleOutput, out ConsoleCursorInfo lpConsoleCursorInfo);

		[DllImport ("kernel32.dll")]
		static extern bool GetConsoleMode (IntPtr hConsoleHandle, out uint lpMode);


		[DllImport ("kernel32.dll")]
		static extern bool SetConsoleMode (IntPtr hConsoleHandle, uint dwMode);

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern IntPtr CreateConsoleScreenBuffer (
			DesiredAccess dwDesiredAccess,
			ShareMode dwShareMode,
			IntPtr secutiryAttributes,
			uint flags,
			IntPtr screenBufferData
		);

		internal static IntPtr INVALID_HANDLE_VALUE = new IntPtr (-1);

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern bool SetConsoleActiveScreenBuffer (IntPtr Handle);

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern bool GetNumberOfConsoleInputEvents (IntPtr handle, out uint lpcNumberOfEvents);
		public uint InputEventCount {
			get {
				GetNumberOfConsoleInputEvents (InputHandle, out uint v);
				return v;
			}
		}

		public InputRecord [] ReadConsoleInput ()
		{
			const int bufferSize = 1;
			var pRecord = Marshal.AllocHGlobal (Marshal.SizeOf<InputRecord> () * bufferSize);
			try {
				ReadConsoleInput (InputHandle, pRecord, bufferSize,
					out var numberEventsRead);

				return numberEventsRead == 0
					? null
					: new [] { Marshal.PtrToStructure<InputRecord> (pRecord) };
			} catch (Exception) {
				return null;
			} finally {
				Marshal.FreeHGlobal (pRecord);
			}
		}

#if false      // Not needed on the constructor. Perhaps could be used on resizing. To study.                                                                                     
		[DllImport ("kernel32.dll", ExactSpelling = true)]
		static extern IntPtr GetConsoleWindow ();

		[DllImport ("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool ShowWindow (IntPtr hWnd, int nCmdShow);

		public const int HIDE = 0;
		public const int MAXIMIZE = 3;
		public const int MINIMIZE = 6;
		public const int RESTORE = 9;

		internal void ShowWindow (int state)
		{
			IntPtr thisConsole = GetConsoleWindow ();
			ShowWindow (thisConsole, state);
		}
#endif
		// See: https://github.com/gui-cs/Terminal.Gui/issues/357

		[StructLayout (LayoutKind.Sequential)]
		public struct CONSOLE_SCREEN_BUFFER_INFOEX {
			public uint cbSize;
			public Coord dwSize;
			public Coord dwCursorPosition;
			public ushort wAttributes;
			public SmallRect srWindow;
			public Coord dwMaximumWindowSize;
			public ushort wPopupAttributes;
			public bool bFullscreenSupported;

			[MarshalAs (UnmanagedType.ByValArray, SizeConst = 16)]
			public COLORREF [] ColorTable;
		}

		[StructLayout (LayoutKind.Explicit, Size = 4)]
		public struct COLORREF {
			public COLORREF (byte r, byte g, byte b)
			{
				Value = 0;
				R = r;
				G = g;
				B = b;
			}

			public COLORREF (uint value)
			{
				R = 0;
				G = 0;
				B = 0;
				Value = value & 0x00FFFFFF;
			}

			[FieldOffset (0)]
			public byte R;
			[FieldOffset (1)]
			public byte G;
			[FieldOffset (2)]
			public byte B;

			[FieldOffset (0)]
			public uint Value;
		}

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern bool GetConsoleScreenBufferInfoEx (IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX csbi);

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern bool SetConsoleScreenBufferInfoEx (IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX ConsoleScreenBufferInfo);

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern bool SetConsoleWindowInfo (
			IntPtr hConsoleOutput,
			bool bAbsolute,
			[In] ref SmallRect lpConsoleWindow);

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern Coord GetLargestConsoleWindowSize (
			IntPtr hConsoleOutput);
	}

	internal class WindowsDriver : ConsoleDriver {
		static bool sync = false;
		WindowsConsole.CharInfo [] OutputBuffer;
		int cols, rows, left, top;
		WindowsConsole.SmallRect damageRegion;
		IClipboard clipboard;
		int [,,] contents;

		public override int Cols => cols;
		public override int Rows => rows;
		public override int Left => left;
		public override int Top => top;
		public override bool EnableConsoleScrolling { get; set; }
		public override IClipboard Clipboard => clipboard;
		public override int [,,] Contents => contents;

		public WindowsConsole WinConsole { get; private set; }

		Action<KeyEvent> keyHandler;
		Action<KeyEvent> keyDownHandler;
		Action<KeyEvent> keyUpHandler;
		Action<MouseEvent> mouseHandler;

		public WindowsDriver ()
		{
			WinConsole = new WindowsConsole ();
			clipboard = new WindowsClipboard ();
		}

		public override void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<KeyEvent> keyDownHandler, Action<KeyEvent> keyUpHandler, Action<MouseEvent> mouseHandler)
		{
			this.keyHandler = keyHandler;
			this.keyDownHandler = keyDownHandler;
			this.keyUpHandler = keyUpHandler;
			this.mouseHandler = mouseHandler;

			var mLoop = mainLoop.Driver as WindowsMainLoop;

			mLoop.ProcessInput = (e) => ProcessInput (e);

			mLoop.WinChanged = (s,e) => {
				ChangeWin (e.Size);
			};
		}

		private void ChangeWin (Size e)
		{
			if (!EnableConsoleScrolling) {
				var w = e.Width;
				if (w == cols - 3 && e.Height < rows) {
					w += 3;
				}
				var newSize = WinConsole.SetConsoleWindow (
					(short)Math.Max (w, 16), (short)Math.Max (e.Height, 0));
				left = 0;
				top = 0;
				cols = newSize.Width;
				rows = newSize.Height;
				ResizeScreen ();
				UpdateOffScreen ();
				TerminalResized.Invoke ();
			}
		}

		void ProcessInput (WindowsConsole.InputRecord inputEvent)
		{
			switch (inputEvent.EventType) {
			case WindowsConsole.EventType.Key:
				var fromPacketKey = inputEvent.KeyEvent.wVirtualKeyCode == (uint)ConsoleKey.Packet;
				if (fromPacketKey) {
					inputEvent.KeyEvent = FromVKPacketToKeyEventRecord (inputEvent.KeyEvent);
				}
				var map = MapKey (ToConsoleKeyInfoEx (inputEvent.KeyEvent));
				//var ke = inputEvent.KeyEvent;
				//System.Diagnostics.Debug.WriteLine ($"fromPacketKey: {fromPacketKey}");
				//if (ke.UnicodeChar == '\0') {
				//	System.Diagnostics.Debug.WriteLine ("UnicodeChar: 0'\\0'");
				//} else if (ke.UnicodeChar == 13) {
				//	System.Diagnostics.Debug.WriteLine ("UnicodeChar: 13'\\n'");
				//} else {
				//	System.Diagnostics.Debug.WriteLine ($"UnicodeChar: {(uint)ke.UnicodeChar}'{ke.UnicodeChar}'");
				//}
				//System.Diagnostics.Debug.WriteLine ($"bKeyDown: {ke.bKeyDown}");
				//System.Diagnostics.Debug.WriteLine ($"dwControlKeyState: {ke.dwControlKeyState}");
				//System.Diagnostics.Debug.WriteLine ($"wRepeatCount: {ke.wRepeatCount}");
				//System.Diagnostics.Debug.WriteLine ($"wVirtualKeyCode: {ke.wVirtualKeyCode}");
				//System.Diagnostics.Debug.WriteLine ($"wVirtualScanCode: {ke.wVirtualScanCode}");

				if (map == (Key)0xffffffff) {
					KeyEvent key = new KeyEvent ();

					// Shift = VK_SHIFT = 0x10
					// Ctrl = VK_CONTROL = 0x11
					// Alt = VK_MENU = 0x12

					if (inputEvent.KeyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.CapslockOn)) {
						inputEvent.KeyEvent.dwControlKeyState &= ~WindowsConsole.ControlKeyState.CapslockOn;
					}

					if (inputEvent.KeyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.ScrolllockOn)) {
						inputEvent.KeyEvent.dwControlKeyState &= ~WindowsConsole.ControlKeyState.ScrolllockOn;
					}

					if (inputEvent.KeyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.NumlockOn)) {
						inputEvent.KeyEvent.dwControlKeyState &= ~WindowsConsole.ControlKeyState.NumlockOn;
					}

					switch (inputEvent.KeyEvent.dwControlKeyState) {
					case WindowsConsole.ControlKeyState.RightAltPressed:
					case WindowsConsole.ControlKeyState.RightAltPressed |
						WindowsConsole.ControlKeyState.LeftControlPressed |
						WindowsConsole.ControlKeyState.EnhancedKey:
					case WindowsConsole.ControlKeyState.EnhancedKey:
						key = new KeyEvent (Key.CtrlMask | Key.AltMask, keyModifiers);
						break;
					case WindowsConsole.ControlKeyState.LeftAltPressed:
						key = new KeyEvent (Key.AltMask, keyModifiers);
						break;
					case WindowsConsole.ControlKeyState.RightControlPressed:
					case WindowsConsole.ControlKeyState.LeftControlPressed:
						key = new KeyEvent (Key.CtrlMask, keyModifiers);
						break;
					case WindowsConsole.ControlKeyState.ShiftPressed:
						key = new KeyEvent (Key.ShiftMask, keyModifiers);
						break;
					case WindowsConsole.ControlKeyState.NumlockOn:
						break;
					case WindowsConsole.ControlKeyState.ScrolllockOn:
						break;
					case WindowsConsole.ControlKeyState.CapslockOn:
						break;
					default:
						switch (inputEvent.KeyEvent.wVirtualKeyCode) {
						case 0x10:
							key = new KeyEvent (Key.ShiftMask, keyModifiers);
							break;
						case 0x11:
							key = new KeyEvent (Key.CtrlMask, keyModifiers);
							break;
						case 0x12:
							key = new KeyEvent (Key.AltMask, keyModifiers);
							break;
						default:
							key = new KeyEvent (Key.Unknown, keyModifiers);
							break;
						}
						break;
					}

					if (inputEvent.KeyEvent.bKeyDown)
						keyDownHandler (key);
					else
						keyUpHandler (key);
				} else {
					if (inputEvent.KeyEvent.bKeyDown) {
						// May occurs using SendKeys
						if (keyModifiers == null)
							keyModifiers = new KeyModifiers ();
						// Key Down - Fire KeyDown Event and KeyStroke (ProcessKey) Event
						keyDownHandler (new KeyEvent (map, keyModifiers));
						keyHandler (new KeyEvent (map, keyModifiers));
					} else {
						keyUpHandler (new KeyEvent (map, keyModifiers));
					}
				}
				if (!inputEvent.KeyEvent.bKeyDown && inputEvent.KeyEvent.dwControlKeyState == 0) {
					keyModifiers = null;
				}
				break;

			case WindowsConsole.EventType.Mouse:
				var me = ToDriverMouse (inputEvent.MouseEvent);
				mouseHandler (me);
				if (processButtonClick) {
					mouseHandler (
						new MouseEvent () {
							X = me.X,
							Y = me.Y,
							Flags = ProcessButtonClick (inputEvent.MouseEvent)
						});
				}
				break;

			case WindowsConsole.EventType.WindowBufferSize:
				var winSize = WinConsole.GetConsoleBufferWindow (out Point pos);
				left = pos.X;
				top = pos.Y;
				cols = inputEvent.WindowBufferSizeEvent.size.X;
				if (EnableConsoleScrolling) {
					rows = Math.Max (inputEvent.WindowBufferSizeEvent.size.Y, rows);
				} else {
					rows = inputEvent.WindowBufferSizeEvent.size.Y;
				}
				//System.Diagnostics.Debug.WriteLine ($"{EnableConsoleScrolling},{cols},{rows}");
				ResizeScreen ();
				UpdateOffScreen ();
				TerminalResized?.Invoke ();
				break;

			case WindowsConsole.EventType.Focus:
				break;
			}
		}

		WindowsConsole.ButtonState? lastMouseButtonPressed = null;
		bool isButtonPressed = false;
		bool isButtonReleased = false;
		bool isButtonDoubleClicked = false;
		Point? point;
		Point pointMove;
		//int buttonPressedCount;
		bool isOneFingerDoubleClicked = false;
		bool processButtonClick;

		MouseEvent ToDriverMouse (WindowsConsole.MouseEventRecord mouseEvent)
		{
			MouseFlags mouseFlag = MouseFlags.AllEvents;

			//System.Diagnostics.Debug.WriteLine (
			//	$"X:{mouseEvent.MousePosition.X};Y:{mouseEvent.MousePosition.Y};ButtonState:{mouseEvent.ButtonState};EventFlags:{mouseEvent.EventFlags}");

			if (isButtonDoubleClicked || isOneFingerDoubleClicked) {
				Application.MainLoop.AddIdle (() => {
					Task.Run (async () => await ProcessButtonDoubleClickedAsync ());
					return false;
				});
			}

			// The ButtonState member of the MouseEvent structure has bit corresponding to each mouse button.
			// This will tell when a mouse button is pressed. When the button is released this event will
			// be fired with it's bit set to 0. So when the button is up ButtonState will be 0.
			// To map to the correct driver events we save the last pressed mouse button so we can
			// map to the correct clicked event.
			if ((lastMouseButtonPressed != null || isButtonReleased) && mouseEvent.ButtonState != 0) {
				lastMouseButtonPressed = null;
				//isButtonPressed = false;
				isButtonReleased = false;
			}

			var p = new Point () {
				X = mouseEvent.MousePosition.X,
				Y = mouseEvent.MousePosition.Y
			};

			//if (!isButtonPressed && buttonPressedCount < 2
			//	&& mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseMoved
			//	&& (mouseEvent.ButtonState == WindowsConsole.ButtonState.Button1Pressed
			//	|| mouseEvent.ButtonState == WindowsConsole.ButtonState.Button2Pressed
			//	|| mouseEvent.ButtonState == WindowsConsole.ButtonState.Button3Pressed)) {

			//	lastMouseButtonPressed = mouseEvent.ButtonState;
			//	buttonPressedCount++;
			//} else if (!isButtonPressed && buttonPressedCount > 0 && mouseEvent.ButtonState == 0
			//	&& mouseEvent.EventFlags == 0) {

			//	buttonPressedCount++;
			//}
			//System.Diagnostics.Debug.WriteLine ($"isButtonPressed: {isButtonPressed};buttonPressedCount: {buttonPressedCount};lastMouseButtonPressed: {lastMouseButtonPressed}");
			//System.Diagnostics.Debug.WriteLine ($"isOneFingerDoubleClicked: {isOneFingerDoubleClicked}");

			//if (buttonPressedCount == 1 && lastMouseButtonPressed != null && p == point
			//	&& lastMouseButtonPressed == WindowsConsole.ButtonState.Button1Pressed
			//	|| lastMouseButtonPressed == WindowsConsole.ButtonState.Button2Pressed
			//	|| lastMouseButtonPressed == WindowsConsole.ButtonState.Button3Pressed) {

			//	switch (lastMouseButtonPressed) {
			//	case WindowsConsole.ButtonState.Button1Pressed:
			//		mouseFlag = MouseFlags.Button1DoubleClicked;
			//		break;

			//	case WindowsConsole.ButtonState.Button2Pressed:
			//		mouseFlag = MouseFlags.Button2DoubleClicked;
			//		break;

			//	case WindowsConsole.ButtonState.Button3Pressed:
			//		mouseFlag = MouseFlags.Button3DoubleClicked;
			//		break;
			//	}
			//	isOneFingerDoubleClicked = true;

			//} else if (buttonPressedCount == 3 && lastMouseButtonPressed != null && isOneFingerDoubleClicked && p == point
			//	&& lastMouseButtonPressed == WindowsConsole.ButtonState.Button1Pressed
			//	|| lastMouseButtonPressed == WindowsConsole.ButtonState.Button2Pressed
			//	|| lastMouseButtonPressed == WindowsConsole.ButtonState.Button3Pressed) {

			//	switch (lastMouseButtonPressed) {
			//	case WindowsConsole.ButtonState.Button1Pressed:
			//		mouseFlag = MouseFlags.Button1TripleClicked;
			//		break;

			//	case WindowsConsole.ButtonState.Button2Pressed:
			//		mouseFlag = MouseFlags.Button2TripleClicked;
			//		break;

			//	case WindowsConsole.ButtonState.Button3Pressed:
			//		mouseFlag = MouseFlags.Button3TripleClicked;
			//		break;
			//	}
			//	buttonPressedCount = 0;
			//	lastMouseButtonPressed = null;
			//	isOneFingerDoubleClicked = false;
			//	isButtonReleased = false;

			//}
			if ((mouseEvent.ButtonState != 0 && mouseEvent.EventFlags == 0 && lastMouseButtonPressed == null && !isButtonDoubleClicked) ||
				 (lastMouseButtonPressed == null && mouseEvent.EventFlags.HasFlag (WindowsConsole.EventFlags.MouseMoved) &&
				 mouseEvent.ButtonState != 0 && !isButtonReleased && !isButtonDoubleClicked)) {
				switch (mouseEvent.ButtonState) {
				case WindowsConsole.ButtonState.Button1Pressed:
					mouseFlag = MouseFlags.Button1Pressed;
					break;

				case WindowsConsole.ButtonState.Button2Pressed:
					mouseFlag = MouseFlags.Button2Pressed;
					break;

				case WindowsConsole.ButtonState.RightmostButtonPressed:
					mouseFlag = MouseFlags.Button3Pressed;
					break;
				}

				if (point == null)
					point = p;

				if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseMoved) {
					mouseFlag |= MouseFlags.ReportMousePosition;
					isButtonReleased = false;
					processButtonClick = false;
				}
				lastMouseButtonPressed = mouseEvent.ButtonState;
				isButtonPressed = true;

				if ((mouseFlag & MouseFlags.ReportMousePosition) == 0) {
					Application.MainLoop.AddIdle (() => {
						Task.Run (async () => await ProcessContinuousButtonPressedAsync (mouseFlag));
						return false;
					});
				}

			} else if (lastMouseButtonPressed != null && mouseEvent.EventFlags == 0
				&& !isButtonReleased && !isButtonDoubleClicked && !isOneFingerDoubleClicked) {
				switch (lastMouseButtonPressed) {
				case WindowsConsole.ButtonState.Button1Pressed:
					mouseFlag = MouseFlags.Button1Released;
					break;

				case WindowsConsole.ButtonState.Button2Pressed:
					mouseFlag = MouseFlags.Button2Released;
					break;

				case WindowsConsole.ButtonState.RightmostButtonPressed:
					mouseFlag = MouseFlags.Button3Released;
					break;
				}
				isButtonPressed = false;
				isButtonReleased = true;
				if (point != null && (((Point)point).X == mouseEvent.MousePosition.X && ((Point)point).Y == mouseEvent.MousePosition.Y)) {
					processButtonClick = true;
				} else {
					point = null;
				}
			} else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseMoved
				&& !isOneFingerDoubleClicked && isButtonReleased && p == point) {

				mouseFlag = ProcessButtonClick (mouseEvent);

			} else if (mouseEvent.EventFlags.HasFlag (WindowsConsole.EventFlags.DoubleClick)) {
				switch (mouseEvent.ButtonState) {
				case WindowsConsole.ButtonState.Button1Pressed:
					mouseFlag = MouseFlags.Button1DoubleClicked;
					break;

				case WindowsConsole.ButtonState.Button2Pressed:
					mouseFlag = MouseFlags.Button2DoubleClicked;
					break;

				case WindowsConsole.ButtonState.RightmostButtonPressed:
					mouseFlag = MouseFlags.Button3DoubleClicked;
					break;
				}
				isButtonDoubleClicked = true;
			} else if (mouseEvent.EventFlags == 0 && mouseEvent.ButtonState != 0 && isButtonDoubleClicked) {
				switch (mouseEvent.ButtonState) {
				case WindowsConsole.ButtonState.Button1Pressed:
					mouseFlag = MouseFlags.Button1TripleClicked;
					break;

				case WindowsConsole.ButtonState.Button2Pressed:
					mouseFlag = MouseFlags.Button2TripleClicked;
					break;

				case WindowsConsole.ButtonState.RightmostButtonPressed:
					mouseFlag = MouseFlags.Button3TripleClicked;
					break;
				}
				isButtonDoubleClicked = false;
			} else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseWheeled) {
				switch ((int)mouseEvent.ButtonState) {
				case int v when v > 0:
					mouseFlag = MouseFlags.WheeledUp;
					break;

				case int v when v < 0:
					mouseFlag = MouseFlags.WheeledDown;
					break;
				}

			} else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseWheeled &&
				mouseEvent.ControlKeyState == WindowsConsole.ControlKeyState.ShiftPressed) {
				switch ((int)mouseEvent.ButtonState) {
				case int v when v > 0:
					mouseFlag = MouseFlags.WheeledLeft;
					break;

				case int v when v < 0:
					mouseFlag = MouseFlags.WheeledRight;
					break;
				}

			} else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseHorizontalWheeled) {
				switch ((int)mouseEvent.ButtonState) {
				case int v when v < 0:
					mouseFlag = MouseFlags.WheeledLeft;
					break;

				case int v when v > 0:
					mouseFlag = MouseFlags.WheeledRight;
					break;
				}

			} else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseMoved) {
				mouseFlag = MouseFlags.ReportMousePosition;
				if (mouseEvent.MousePosition.X != pointMove.X || mouseEvent.MousePosition.Y != pointMove.Y) {
					pointMove = new Point (mouseEvent.MousePosition.X, mouseEvent.MousePosition.Y);
				}
			} else if (mouseEvent.ButtonState == 0 && mouseEvent.EventFlags == 0) {
				mouseFlag = 0;
			}

			mouseFlag = SetControlKeyStates (mouseEvent, mouseFlag);

			//System.Diagnostics.Debug.WriteLine (
			//	$"point.X:{(point != null ? ((Point)point).X : -1)};point.Y:{(point != null ? ((Point)point).Y : -1)}");

			return new MouseEvent () {
				X = mouseEvent.MousePosition.X,
				Y = mouseEvent.MousePosition.Y,
				Flags = mouseFlag
			};
		}

		MouseFlags ProcessButtonClick (WindowsConsole.MouseEventRecord mouseEvent)
		{
			MouseFlags mouseFlag = 0;
			switch (lastMouseButtonPressed) {
			case WindowsConsole.ButtonState.Button1Pressed:
				mouseFlag = MouseFlags.Button1Clicked;
				break;

			case WindowsConsole.ButtonState.Button2Pressed:
				mouseFlag = MouseFlags.Button2Clicked;
				break;

			case WindowsConsole.ButtonState.RightmostButtonPressed:
				mouseFlag = MouseFlags.Button3Clicked;
				break;
			}
			point = new Point () {
				X = mouseEvent.MousePosition.X,
				Y = mouseEvent.MousePosition.Y
			};
			lastMouseButtonPressed = null;
			isButtonReleased = false;
			processButtonClick = false;
			point = null;
			return mouseFlag;
		}

		async Task ProcessButtonDoubleClickedAsync ()
		{
			await Task.Delay (300);
			isButtonDoubleClicked = false;
			isOneFingerDoubleClicked = false;
			//buttonPressedCount = 0;
		}

		async Task ProcessContinuousButtonPressedAsync (MouseFlags mouseFlag)
		{
			while (isButtonPressed) {
				await Task.Delay (100);
				var me = new MouseEvent () {
					X = pointMove.X,
					Y = pointMove.Y,
					Flags = mouseFlag
				};

				var view = Application.WantContinuousButtonPressedView;
				if (view == null) {
					break;
				}
				if (isButtonPressed && (mouseFlag & MouseFlags.ReportMousePosition) == 0) {
					Application.MainLoop.Invoke (() => mouseHandler (me));
				}
			}
		}

		static MouseFlags SetControlKeyStates (WindowsConsole.MouseEventRecord mouseEvent, MouseFlags mouseFlag)
		{
			if (mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.RightControlPressed) ||
				mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.LeftControlPressed))
				mouseFlag |= MouseFlags.ButtonCtrl;

			if (mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.ShiftPressed))
				mouseFlag |= MouseFlags.ButtonShift;

			if (mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.RightAltPressed) ||
				 mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.LeftAltPressed))
				mouseFlag |= MouseFlags.ButtonAlt;
			return mouseFlag;
		}

		KeyModifiers keyModifiers;

		public WindowsConsole.ConsoleKeyInfoEx ToConsoleKeyInfoEx (WindowsConsole.KeyEventRecord keyEvent)
		{
			var state = keyEvent.dwControlKeyState;

			bool shift = (state & WindowsConsole.ControlKeyState.ShiftPressed) != 0;
			bool alt = (state & (WindowsConsole.ControlKeyState.LeftAltPressed | WindowsConsole.ControlKeyState.RightAltPressed)) != 0;
			bool control = (state & (WindowsConsole.ControlKeyState.LeftControlPressed | WindowsConsole.ControlKeyState.RightControlPressed)) != 0;
			bool capslock = (state & (WindowsConsole.ControlKeyState.CapslockOn)) != 0;
			bool numlock = (state & (WindowsConsole.ControlKeyState.NumlockOn)) != 0;
			bool scrolllock = (state & (WindowsConsole.ControlKeyState.ScrolllockOn)) != 0;

			if (keyModifiers == null)
				keyModifiers = new KeyModifiers ();
			if (shift)
				keyModifiers.Shift = shift;
			if (alt)
				keyModifiers.Alt = alt;
			if (control)
				keyModifiers.Ctrl = control;
			if (capslock)
				keyModifiers.Capslock = capslock;
			if (numlock)
				keyModifiers.Numlock = numlock;
			if (scrolllock)
				keyModifiers.Scrolllock = scrolllock;

			var ConsoleKeyInfo = new ConsoleKeyInfo (keyEvent.UnicodeChar, (ConsoleKey)keyEvent.wVirtualKeyCode, shift, alt, control);

			return new WindowsConsole.ConsoleKeyInfoEx (ConsoleKeyInfo, capslock, numlock, scrolllock);
		}

		public WindowsConsole.KeyEventRecord FromVKPacketToKeyEventRecord (WindowsConsole.KeyEventRecord keyEvent)
		{
			if (keyEvent.wVirtualKeyCode != (uint)ConsoleKey.Packet) {
				return keyEvent;
			}

			var mod = new ConsoleModifiers ();
			if (keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.ShiftPressed)) {
				mod |= ConsoleModifiers.Shift;
			}
			if (keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.RightAltPressed) ||
				keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.LeftAltPressed)) {
				mod |= ConsoleModifiers.Alt;
			}
			if (keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.LeftControlPressed) ||
				keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.RightControlPressed)) {
				mod |= ConsoleModifiers.Control;
			}
			var keyChar = ConsoleKeyMapping.GetKeyCharFromConsoleKey (keyEvent.UnicodeChar, mod, out uint virtualKey, out uint scanCode);

			return new WindowsConsole.KeyEventRecord {
				UnicodeChar = (char)keyChar,
				bKeyDown = keyEvent.bKeyDown,
				dwControlKeyState = keyEvent.dwControlKeyState,
				wRepeatCount = keyEvent.wRepeatCount,
				wVirtualKeyCode = (ushort)virtualKey,
				wVirtualScanCode = (ushort)scanCode
			};
		}

		public Key MapKey (WindowsConsole.ConsoleKeyInfoEx keyInfoEx)
		{
			var keyInfo = keyInfoEx.consoleKeyInfo;
			switch (keyInfo.Key) {
			case ConsoleKey.Escape:
				return MapKeyModifiers (keyInfo, Key.Esc);
			case ConsoleKey.Tab:
				return keyInfo.Modifiers == ConsoleModifiers.Shift ? Key.BackTab : Key.Tab;
			case ConsoleKey.Clear:
				return MapKeyModifiers (keyInfo, Key.Clear);
			case ConsoleKey.Home:
				return MapKeyModifiers (keyInfo, Key.Home);
			case ConsoleKey.End:
				return MapKeyModifiers (keyInfo, Key.End);
			case ConsoleKey.LeftArrow:
				return MapKeyModifiers (keyInfo, Key.CursorLeft);
			case ConsoleKey.RightArrow:
				return MapKeyModifiers (keyInfo, Key.CursorRight);
			case ConsoleKey.UpArrow:
				return MapKeyModifiers (keyInfo, Key.CursorUp);
			case ConsoleKey.DownArrow:
				return MapKeyModifiers (keyInfo, Key.CursorDown);
			case ConsoleKey.PageUp:
				return MapKeyModifiers (keyInfo, Key.PageUp);
			case ConsoleKey.PageDown:
				return MapKeyModifiers (keyInfo, Key.PageDown);
			case ConsoleKey.Enter:
				return MapKeyModifiers (keyInfo, Key.Enter);
			case ConsoleKey.Spacebar:
				return MapKeyModifiers (keyInfo, keyInfo.KeyChar == 0 ? Key.Space : (Key)keyInfo.KeyChar);
			case ConsoleKey.Backspace:
				return MapKeyModifiers (keyInfo, Key.Backspace);
			case ConsoleKey.Delete:
				return MapKeyModifiers (keyInfo, Key.DeleteChar);
			case ConsoleKey.Insert:
				return MapKeyModifiers (keyInfo, Key.InsertChar);
			case ConsoleKey.PrintScreen:
				return MapKeyModifiers (keyInfo, Key.PrintScreen);

			case ConsoleKey.NumPad0:
				return keyInfoEx.NumLock ? Key.D0 : Key.InsertChar;
			case ConsoleKey.NumPad1:
				return keyInfoEx.NumLock ? Key.D1 : Key.End;
			case ConsoleKey.NumPad2:
				return keyInfoEx.NumLock ? Key.D2 : Key.CursorDown;
			case ConsoleKey.NumPad3:
				return keyInfoEx.NumLock ? Key.D3 : Key.PageDown;
			case ConsoleKey.NumPad4:
				return keyInfoEx.NumLock ? Key.D4 : Key.CursorLeft;
			case ConsoleKey.NumPad5:
				return keyInfoEx.NumLock ? Key.D5 : (Key)((uint)keyInfo.KeyChar);
			case ConsoleKey.NumPad6:
				return keyInfoEx.NumLock ? Key.D6 : Key.CursorRight;
			case ConsoleKey.NumPad7:
				return keyInfoEx.NumLock ? Key.D7 : Key.Home;
			case ConsoleKey.NumPad8:
				return keyInfoEx.NumLock ? Key.D8 : Key.CursorUp;
			case ConsoleKey.NumPad9:
				return keyInfoEx.NumLock ? Key.D9 : Key.PageUp;

			case ConsoleKey.Oem1:
			case ConsoleKey.Oem2:
			case ConsoleKey.Oem3:
			case ConsoleKey.Oem4:
			case ConsoleKey.Oem5:
			case ConsoleKey.Oem6:
			case ConsoleKey.Oem7:
			case ConsoleKey.Oem8:
			case ConsoleKey.Oem102:
			case ConsoleKey.OemPeriod:
			case ConsoleKey.OemComma:
			case ConsoleKey.OemPlus:
			case ConsoleKey.OemMinus:
				if (keyInfo.KeyChar == 0)
					return Key.Unknown;

				return (Key)((uint)keyInfo.KeyChar);
			}

			var key = keyInfo.Key;
			//var alphaBase = ((keyInfo.Modifiers == ConsoleModifiers.Shift) ^ (keyInfoEx.CapsLock)) ? 'A' : 'a';

			if (key >= ConsoleKey.A && key <= ConsoleKey.Z) {
				var delta = key - ConsoleKey.A;
				if (keyInfo.Modifiers == ConsoleModifiers.Control) {
					return (Key)(((uint)Key.CtrlMask) | ((uint)Key.A + delta));
				}
				if (keyInfo.Modifiers == ConsoleModifiers.Alt) {
					return (Key)(((uint)Key.AltMask) | ((uint)Key.A + delta));
				}
				if (keyInfo.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Alt)) {
					return MapKeyModifiers (keyInfo, (Key)((uint)Key.A + delta));
				}
				if ((keyInfo.Modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0) {
					if (keyInfo.KeyChar == 0 || (keyInfo.KeyChar != 0 && keyInfo.KeyChar >= 1 && keyInfo.KeyChar <= 26)) {
						return MapKeyModifiers (keyInfo, (Key)((uint)Key.A + delta));
					}
				}
				//return (Key)((uint)alphaBase + delta);
				return (Key)((uint)keyInfo.KeyChar);
			}
			if (key >= ConsoleKey.D0 && key <= ConsoleKey.D9) {
				var delta = key - ConsoleKey.D0;
				if (keyInfo.Modifiers == ConsoleModifiers.Alt) {
					return (Key)(((uint)Key.AltMask) | ((uint)Key.D0 + delta));
				}
				if (keyInfo.Modifiers == ConsoleModifiers.Control) {
					return (Key)(((uint)Key.CtrlMask) | ((uint)Key.D0 + delta));
				}
				if (keyInfo.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Alt)) {
					return MapKeyModifiers (keyInfo, (Key)((uint)Key.D0 + delta));
				}
				if ((keyInfo.Modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0) {
					if (keyInfo.KeyChar == 0 || keyInfo.KeyChar == 30 || keyInfo.KeyChar == ((uint)Key.D0 + delta)) {
						return MapKeyModifiers (keyInfo, (Key)((uint)Key.D0 + delta));
					}
				}
				return (Key)((uint)keyInfo.KeyChar);
			}
			if (key >= ConsoleKey.F1 && key <= ConsoleKey.F12) {
				var delta = key - ConsoleKey.F1;
				if ((keyInfo.Modifiers & (ConsoleModifiers.Shift | ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0) {
					return MapKeyModifiers (keyInfo, (Key)((uint)Key.F1 + delta));
				}

				return (Key)((uint)Key.F1 + delta);
			}
			if (keyInfo.KeyChar != 0) {
				return MapKeyModifiers (keyInfo, (Key)((uint)keyInfo.KeyChar));
			}

			return (Key)(0xffffffff);
		}

		private Key MapKeyModifiers (ConsoleKeyInfo keyInfo, Key key)
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

		public override void Init (Action terminalResized)
		{
			TerminalResized = terminalResized;

			try {
				// Needed for Windows Terminal
				// ESC [ ? 1047 h  Activate xterm alternative buffer (no backscroll)
				// ESC [ ? 1047 l  Restore xterm working buffer (with backscroll)
				// ESC [ ? 1048 h  Save cursor position
				// ESC [ ? 1048 l  Restore cursor position
				// ESC [ ? 1049 h  Save cursor position and activate xterm alternative buffer (no backscroll)
				// ESC [ ? 1049 l  Restore cursor position and restore xterm working buffer (with backscroll)
				// Per Issue #2264 using the alterantive screen buffer is required for Windows Terminal to not 
				// wipe out the backscroll buffer when the application exits.
				Console.Out.Write ("\x1b[?1047h");

				// Console.Out.Flush () is not needed. See https://stackoverflow.com/a/20450486/297526

				var winSize = WinConsole.GetConsoleOutputWindow (out Point pos);
				cols = winSize.Width;
				rows = winSize.Height;
				WindowsConsole.SmallRect.MakeEmpty (ref damageRegion);

				CurrentAttribute = MakeColor (Color.White, Color.Black);
				InitalizeColorSchemes ();

				CurrentAttribute = MakeColor (Color.White, Color.Black);
				InitalizeColorSchemes ();

				ResizeScreen ();
				UpdateOffScreen ();
			} catch (Win32Exception e) {
				throw new InvalidOperationException ("The Windows Console output window is not available.", e);
			}
		}

		public override void ResizeScreen ()
		{
			OutputBuffer = new WindowsConsole.CharInfo [Rows * Cols];
			Clip = new Rect (0, 0, Cols, Rows);
			damageRegion = new WindowsConsole.SmallRect () {
				Top = 0,
				Left = 0,
				Bottom = (short)Rows,
				Right = (short)Cols
			};
			WinConsole.ForceRefreshCursorVisibility ();
			if (!EnableConsoleScrolling) {
				// ANSI ESC "[xJ" Clears part of the screen.
				// If n is 0 (or missing), clear from cursor to end of screen.
				// If n is 1, clear from cursor to beginning of the screen.
				// If n is 2, clear entire screen (and moves cursor to upper left on DOS ANSI.SYS).
				// If n is 3, clear entire screen and delete all lines saved in the scrollback buffer
				// DO NOT USE 3J - even with the alternate screen buffer, it clears the entire scrollback buffer
				Console.Out.Write ("\x1b[3J");
			}
		}

		public override void UpdateOffScreen ()
		{
			contents = new int [rows, cols, 3];
			for (int row = 0; row < rows; row++) {
				for (int col = 0; col < cols; col++) {
					int position = row * cols + col;
					OutputBuffer [position].Attributes = (ushort)Colors.TopLevel.Normal;
					OutputBuffer [position].Char.UnicodeChar = ' ';
					contents [row, col, 0] = OutputBuffer [position].Char.UnicodeChar;
					contents [row, col, 1] = OutputBuffer [position].Attributes;
					contents [row, col, 2] = 0;
				}
			}
		}

		int ccol, crow;
		public override void Move (int col, int row)
		{
			ccol = col;
			crow = row;
		}

		int GetOutputBufferPosition ()
		{
			return crow * Cols + ccol;
		}

		public override void AddRune (Rune rune)
		{
			rune = MakePrintable (rune);
			var runeWidth = Rune.ColumnWidth (rune);
			var position = GetOutputBufferPosition ();
			var validClip = IsValidContent (ccol, crow, Clip);

			if (validClip) {
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
					var prevPosition = crow * Cols + (ccol - 1);
					OutputBuffer [prevPosition].Char.UnicodeChar = c;
					contents [crow, ccol - 1, 0] = c;
					OutputBuffer [prevPosition].Attributes = (ushort)CurrentAttribute;
					contents [crow, ccol - 1, 1] = CurrentAttribute;
					contents [crow, ccol - 1, 2] = 1;
					WindowsConsole.SmallRect.Update (ref damageRegion, (short)(ccol - 1), (short)crow);
				} else {
					if (runeWidth < 2 && ccol > 0
						&& Rune.ColumnWidth ((char)contents [crow, ccol - 1, 0]) > 1) {

						var prevPosition = crow * Cols + (ccol - 1);
						OutputBuffer [prevPosition].Char.UnicodeChar = ' ';
						contents [crow, ccol - 1, 0] = (int)(uint)' ';

					} else if (runeWidth < 2 && ccol <= Clip.Right - 1
						&& Rune.ColumnWidth ((char)contents [crow, ccol, 0]) > 1) {

						var prevPosition = GetOutputBufferPosition () + 1;
						OutputBuffer [prevPosition].Char.UnicodeChar = (char)' ';
						contents [crow, ccol + 1, 0] = (int)(uint)' ';

					}
					if (runeWidth > 1 && ccol == Clip.Right - 1) {
						OutputBuffer [position].Char.UnicodeChar = (char)' ';
						contents [crow, ccol, 0] = (int)(uint)' ';
					} else {
						OutputBuffer [position].Char.UnicodeChar = (char)rune;
						contents [crow, ccol, 0] = (int)(uint)rune;
					}
					OutputBuffer [position].Attributes = (ushort)CurrentAttribute;
					contents [crow, ccol, 1] = CurrentAttribute;
					contents [crow, ccol, 2] = 1;
					WindowsConsole.SmallRect.Update (ref damageRegion, (short)ccol, (short)crow);
				}
			}

			if (runeWidth < 0 || runeWidth > 0) {
				ccol++;
			}

			if (runeWidth > 1) {
				if (validClip && ccol < Clip.Right) {
					position = GetOutputBufferPosition ();
					OutputBuffer [position].Attributes = (ushort)CurrentAttribute;
					OutputBuffer [position].Char.UnicodeChar = (char)0x00;
					contents [crow, ccol, 0] = (int)(uint)0x00;
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
			foreach (var rune in str)
				AddRune (rune);
		}

		public override void SetAttribute (Attribute c)
		{
			base.SetAttribute (c);
		}

		public override Attribute MakeColor (Color foreground, Color background)
		{
			return MakeColor ((ConsoleColor)foreground, (ConsoleColor)background);
		}

		Attribute MakeColor (ConsoleColor f, ConsoleColor b)
		{
			// Encode the colors into the int value.
			return new Attribute (
				value: ((int)f | (int)b << 4),
				foreground: (Color)f,
				background: (Color)b
			);
		}

		public override Attribute MakeAttribute (Color fore, Color back)
		{
			return MakeColor ((ConsoleColor)fore, (ConsoleColor)back);
		}

		public override void Refresh ()
		{
			UpdateScreen ();

			WinConsole.SetInitialCursorVisibility ();

			UpdateCursor ();
#if false
			var bufferCoords = new WindowsConsole.Coord (){
				X = (short)Clip.Width,
				Y = (short)Clip.Height
			};

			var window = new WindowsConsole.SmallRect (){
				Top = 0,
				Left = 0,
				Right = (short)Clip.Right,
				Bottom = (short)Clip.Bottom
			};

			UpdateCursor();
			WinConsole.WriteToConsole (OutputBuffer, bufferCoords, window);
#endif
		}

		public override void UpdateScreen ()
		{
			if (damageRegion.Left == -1)
				return;

			if (!EnableConsoleScrolling) {
				var windowSize = WinConsole.GetConsoleBufferWindow (out _);
				if (!windowSize.IsEmpty && (windowSize.Width != Cols || windowSize.Height != Rows))
					return;
			}

			var bufferCoords = new WindowsConsole.Coord () {
				X = (short)Clip.Width,
				Y = (short)Clip.Height
			};

			//var window = new WindowsConsole.SmallRect () {
			//	Top = 0,
			//	Left = 0,
			//	Right = (short)Clip.Right,
			//	Bottom = (short)Clip.Bottom
			//};

			WinConsole.WriteToConsole (new Size (Cols, Rows), OutputBuffer, bufferCoords, damageRegion);

			// System.Diagnostics.Debugger.Log (0, "debug", $"Region={damageRegion.Right - damageRegion.Left},{damageRegion.Bottom - damageRegion.Top}\n");
			WindowsConsole.SmallRect.MakeEmpty (ref damageRegion);
		}

		CursorVisibility savedCursorVisibility;

		public override void UpdateCursor ()
		{
			if (ccol < 0 || crow < 0 || ccol > Cols || crow > Rows) {
				GetCursorVisibility (out CursorVisibility cursorVisibility);
				savedCursorVisibility = cursorVisibility;
				SetCursorVisibility (CursorVisibility.Invisible);
				return;
			}

			SetCursorVisibility (savedCursorVisibility);
			var position = new WindowsConsole.Coord () {
				X = (short)ccol,
				Y = (short)crow
			};
			WinConsole.SetCursorPosition (position);
		}

		public override void End ()
		{
			WinConsole.Cleanup ();
			WinConsole = null;

			// Needed for Windows Terminal
			// Clear the alternative screen buffer from the cursor to the
			// end of the screen.
			// Note, [3J causes Windows Terminal to wipe out the entire NON ALTERNATIVE
			// backbuffer! So we need to use [0J instead.
			Console.Out.Write ("\x1b[0J");

			// Disable alternative screen buffer.
			Console.Out.Write ("\x1b[?1047l");

			// Console.Out.Flush () is not needed. See https://stackoverflow.com/a/20450486/297526
		}

		/// <inheritdoc/>
		public override bool GetCursorVisibility (out CursorVisibility visibility)
		{
			return WinConsole.GetCursorVisibility (out visibility);
		}

		/// <inheritdoc/>
		public override bool SetCursorVisibility (CursorVisibility visibility)
		{
			savedCursorVisibility = visibility;
			return WinConsole.SetCursorVisibility (visibility);
		}

		/// <inheritdoc/>
		public override bool EnsureCursorVisibility ()
		{
			return WinConsole.EnsureCursorVisibility ();
		}

		public override void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool control)
		{
			WindowsConsole.InputRecord input = new WindowsConsole.InputRecord {
				EventType = WindowsConsole.EventType.Key
			};

			WindowsConsole.KeyEventRecord keyEvent = new WindowsConsole.KeyEventRecord {
				bKeyDown = true
			};
			WindowsConsole.ControlKeyState controlKey = new WindowsConsole.ControlKeyState ();
			if (shift) {
				controlKey |= WindowsConsole.ControlKeyState.ShiftPressed;
				keyEvent.UnicodeChar = '\0';
				keyEvent.wVirtualKeyCode = 16;
			}
			if (alt) {
				controlKey |= WindowsConsole.ControlKeyState.LeftAltPressed;
				controlKey |= WindowsConsole.ControlKeyState.RightAltPressed;
				keyEvent.UnicodeChar = '\0';
				keyEvent.wVirtualKeyCode = 18;
			}
			if (control) {
				controlKey |= WindowsConsole.ControlKeyState.LeftControlPressed;
				controlKey |= WindowsConsole.ControlKeyState.RightControlPressed;
				keyEvent.UnicodeChar = '\0';
				keyEvent.wVirtualKeyCode = 17;
			}
			keyEvent.dwControlKeyState = controlKey;

			input.KeyEvent = keyEvent;

			if (shift || alt || control) {
				ProcessInput (input);
			}

			keyEvent.UnicodeChar = keyChar;
			if ((uint)key < 255) {
				keyEvent.wVirtualKeyCode = (ushort)key;
			} else {
				keyEvent.wVirtualKeyCode = '\0';
			}

			input.KeyEvent = keyEvent;

			try {
				ProcessInput (input);
			} catch (OverflowException) { } finally {
				keyEvent.bKeyDown = false;
				input.KeyEvent = keyEvent;
				ProcessInput (input);
			}
		}

		public override bool GetColors (int value, out Color foreground, out Color background)
		{
			bool hasColor = false;
			foreground = default;
			background = default;
			IEnumerable<int> values = Enum.GetValues (typeof (ConsoleColor))
				.OfType<ConsoleColor> ()
				.Select (s => (int)s);
			if (values.Contains ((value >> 4) & 0xffff)) {
				hasColor = true;
				background = (Color)(ConsoleColor)((value >> 4) & 0xffff);
			}
			if (values.Contains (value - ((int)background << 4))) {
				hasColor = true;
				foreground = (Color)(ConsoleColor)(value - ((int)background << 4));
			}
			return hasColor;
		}

		#region Unused
		public override void SetColors (ConsoleColor foreground, ConsoleColor background)
		{
		}

		public override void SetColors (short foregroundColorId, short backgroundColorId)
		{
		}

		public override void Suspend ()
		{
		}

		public override void StartReportingMouseMoves ()
		{
		}

		public override void StopReportingMouseMoves ()
		{
		}

		public override void UncookMouse ()
		{
		}

		public override void CookMouse ()
		{
		}
		#endregion
	}

	/// <summary>
	/// Mainloop intended to be used with the <see cref="WindowsDriver"/>, and can
	/// only be used on Windows.
	/// </summary>
	/// <remarks>
	/// This implementation is used for WindowsDriver.
	/// </remarks>
	internal class WindowsMainLoop : IMainLoopDriver {
		ManualResetEventSlim eventReady = new ManualResetEventSlim (false);
		ManualResetEventSlim waitForProbe = new ManualResetEventSlim (false);
		ManualResetEventSlim winChange = new ManualResetEventSlim (false);
		MainLoop mainLoop;
		ConsoleDriver consoleDriver;
		WindowsConsole winConsole;
		bool winChanged;
		Size windowSize;
		CancellationTokenSource tokenSource = new CancellationTokenSource ();

		// The records that we keep fetching
		Queue<WindowsConsole.InputRecord []> resultQueue = new Queue<WindowsConsole.InputRecord []> ();

		/// <summary>
		/// Invoked when a Key is pressed or released.
		/// </summary>
		public Action<WindowsConsole.InputRecord> ProcessInput;

		/// <summary>
		/// Invoked when the window is changed.
		/// </summary>
		public EventHandler<SizeChangedEventArgs> WinChanged;
		
		public WindowsMainLoop (ConsoleDriver consoleDriver = null)
		{
			this.consoleDriver = consoleDriver ?? throw new ArgumentNullException ("Console driver instance must be provided.");
			winConsole = ((WindowsDriver)consoleDriver).WinConsole;
		}

		void IMainLoopDriver.Setup (MainLoop mainLoop)
		{
			this.mainLoop = mainLoop;
			Task.Run (WindowsInputHandler);
			Task.Run (CheckWinChange);
		}

		void WindowsInputHandler ()
		{
			while (true) {
				waitForProbe.Wait ();
				waitForProbe.Reset ();

				if (resultQueue?.Count == 0) {
					resultQueue.Enqueue (winConsole.ReadConsoleInput ());
				}

				eventReady.Set ();
			}
		}

		void CheckWinChange ()
		{
			while (true) {
				winChange.Wait ();
				winChange.Reset ();
				WaitWinChange ();
				winChanged = true;
				eventReady.Set ();
			}
		}

		void WaitWinChange ()
		{
			while (true) {
				Thread.Sleep (100);
				if (!consoleDriver.EnableConsoleScrolling) {
					windowSize = winConsole.GetConsoleBufferWindow (out _);
					//System.Diagnostics.Debug.WriteLine ($"{consoleDriver.EnableConsoleScrolling},{windowSize.Width},{windowSize.Height}");
					if (windowSize != Size.Empty && windowSize.Width != consoleDriver.Cols
						|| windowSize.Height != consoleDriver.Rows) {
						return;
					}
				}
			}
		}

		void IMainLoopDriver.Wakeup ()
		{
			//tokenSource.Cancel ();
			eventReady.Set ();
		}

		bool IMainLoopDriver.EventsPending (bool wait)
		{
			waitForProbe.Set ();
			winChange.Set ();

			if (CheckTimers (wait, out var waitTimeout)) {
				return true;
			}

			try {
				if (!tokenSource.IsCancellationRequested) {
					eventReady.Wait (waitTimeout, tokenSource.Token);
				}
			} catch (OperationCanceledException) {
				return true;
			} finally {
				eventReady.Reset ();
			}

			if (!tokenSource.IsCancellationRequested) {
				return resultQueue.Count > 0 || CheckTimers (wait, out _) || winChanged;
			}

			tokenSource.Dispose ();
			tokenSource = new CancellationTokenSource ();
			return true;
		}

		bool CheckTimers (bool wait, out int waitTimeout)
		{
			long now = DateTime.UtcNow.Ticks;

			if (mainLoop.timeouts.Count > 0) {
				waitTimeout = (int)((mainLoop.timeouts.Keys [0] - now) / TimeSpan.TicksPerMillisecond);
				if (waitTimeout < 0)
					return true;
			} else {
				waitTimeout = -1;
			}

			if (!wait)
				waitTimeout = 0;

			int ic;
			lock (mainLoop.idleHandlers) {
				ic = mainLoop.idleHandlers.Count;
			}

			return ic > 0;
		}

		void IMainLoopDriver.MainIteration ()
		{
			while (resultQueue.Count > 0) {
				var inputRecords = resultQueue.Dequeue ();
				if (inputRecords != null && inputRecords.Length > 0) {
					var inputEvent = inputRecords [0];
					ProcessInput?.Invoke (inputEvent);
				}
			}
			if (winChanged) {
				winChanged = false;
				WinChanged?.Invoke (this, new SizeChangedEventArgs(windowSize));
			}
		}
	}

	class WindowsClipboard : ClipboardBase {
		public WindowsClipboard ()
		{
			IsSupported = IsClipboardFormatAvailable (cfUnicodeText);
		}

		public override bool IsSupported { get; }

		protected override string GetClipboardDataImpl ()
		{
			//if (!IsClipboardFormatAvailable (cfUnicodeText))
			//	return null;

			try {
				if (!OpenClipboard (IntPtr.Zero))
					return null;

				IntPtr handle = GetClipboardData (cfUnicodeText);
				if (handle == IntPtr.Zero)
					return null;

				IntPtr pointer = IntPtr.Zero;

				try {
					pointer = GlobalLock (handle);
					if (pointer == IntPtr.Zero)
						return null;

					int size = GlobalSize (handle);
					byte [] buff = new byte [size];

					Marshal.Copy (pointer, buff, 0, size);

					return System.Text.Encoding.Unicode.GetString (buff)
						.TrimEnd ('\0');
				} finally {
					if (pointer != IntPtr.Zero)
						GlobalUnlock (handle);
				}
			} finally {
				CloseClipboard ();
			}
		}

		protected override void SetClipboardDataImpl (string text)
		{
			OpenClipboard ();

			EmptyClipboard ();
			IntPtr hGlobal = default;
			try {
				var bytes = (text.Length + 1) * 2;
				hGlobal = Marshal.AllocHGlobal (bytes);

				if (hGlobal == default) {
					ThrowWin32 ();
				}

				var target = GlobalLock (hGlobal);

				if (target == default) {
					ThrowWin32 ();
				}

				try {
					Marshal.Copy (text.ToCharArray (), 0, target, text.Length);
				} finally {
					GlobalUnlock (target);
				}

				if (SetClipboardData (cfUnicodeText, hGlobal) == default) {
					ThrowWin32 ();
				}

				hGlobal = default;
			} finally {
				if (hGlobal != default) {
					Marshal.FreeHGlobal (hGlobal);
				}

				CloseClipboard ();
			}
		}

		void OpenClipboard ()
		{
			var num = 10;
			while (true) {
				if (OpenClipboard (default)) {
					break;
				}

				if (--num == 0) {
					ThrowWin32 ();
				}

				Thread.Sleep (100);
			}
		}

		const uint cfUnicodeText = 13;

		void ThrowWin32 ()
		{
			throw new Win32Exception (Marshal.GetLastWin32Error ());
		}

		[DllImport ("User32.dll", SetLastError = true)]
		[return: MarshalAs (UnmanagedType.Bool)]
		static extern bool IsClipboardFormatAvailable (uint format);

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern int GlobalSize (IntPtr handle);

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern IntPtr GlobalLock (IntPtr hMem);

		[DllImport ("kernel32.dll", SetLastError = true)]
		[return: MarshalAs (UnmanagedType.Bool)]
		static extern bool GlobalUnlock (IntPtr hMem);

		[DllImport ("user32.dll", SetLastError = true)]
		[return: MarshalAs (UnmanagedType.Bool)]
		static extern bool OpenClipboard (IntPtr hWndNewOwner);

		[DllImport ("user32.dll", SetLastError = true)]
		[return: MarshalAs (UnmanagedType.Bool)]
		static extern bool CloseClipboard ();

		[DllImport ("user32.dll", SetLastError = true)]
		static extern IntPtr SetClipboardData (uint uFormat, IntPtr data);

		[DllImport ("user32.dll")]
		static extern bool EmptyClipboard ();

		[DllImport ("user32.dll", SetLastError = true)]
		static extern IntPtr GetClipboardData (uint uFormat);
	}
}
