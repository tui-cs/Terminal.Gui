//
// FakeConsole.cs: A fake .NET Windows Console API implementation for unit tests.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terminal.Gui {

#pragma warning disable RCS1138 // Add summary to documentation comment.
	/// <summary>
	/// 
	/// </summary>
	public static class FakeConsole {
#pragma warning restore RCS1138 // Add summary to documentation comment.

		//
		// Summary:
		//	Gets or sets the width of the console window.
		//
		// Returns:
		//	The width of the console window measured in columns.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	The value of the System.Console.WindowWidth property or the value of the System.Console.WindowHeight
		//	property is less than or equal to 0.-or-The value of the System.Console.WindowHeight
		//	property plus the value of the System.Console.WindowTop property is greater than
		//	or equal to System.Int16.MaxValue.-or-The value of the System.Console.WindowWidth
		//	property or the value of the System.Console.WindowHeight property is greater
		//	than the largest possible window width or height for the current screen resolution
		//	and console font.
		//
		//	T:System.IO.IOException:
		//	Error reading or writing information.
#pragma warning disable RCS1138 // Add summary to documentation comment.

		/// <summary>
		/// Specifies the initial console width.
		/// </summary>
		public const int WIDTH = 80;

		/// <summary>
		/// Specifies the initial console height.
		/// </summary>
		public const int HEIGHT = 25;

		/// <summary>
		/// 
		/// </summary>
		public static int WindowWidth { get; set; } = WIDTH;
		//
		// Summary:
		//	Gets a value that indicates whether output has been redirected from the standard
		//	output stream.
		//
		// Returns:
		//	true if output is redirected; otherwise, false.
		/// <summary>
		/// 
		/// </summary>
		public static bool IsOutputRedirected { get; }
		//
		// Summary:
		//	Gets a value that indicates whether the error output stream has been redirected
		//	from the standard error stream.
		//
		// Returns:
		//	true if error output is redirected; otherwise, false.
		/// <summary>
		/// 
		/// </summary>
		public static bool IsErrorRedirected { get; }
		//
		// Summary:
		//	Gets the standard input stream.
		//
		// Returns:
		//	A System.IO.TextReader that represents the standard input stream.
		/// <summary>
		/// 
		/// </summary>
		public static TextReader In { get; }
		//
		// Summary:
		//	Gets the standard output stream.
		//
		// Returns:
		//	A System.IO.TextWriter that represents the standard output stream.
		/// <summary>
		/// 
		/// </summary>
		public static TextWriter Out { get; }
		//
		// Summary:
		//	Gets the standard error output stream.
		//
		// Returns:
		//	A System.IO.TextWriter that represents the standard error output stream.
		/// <summary>
		/// 
		/// </summary>
		public static TextWriter Error { get; }
		//
		// Summary:
		//	Gets or sets the encoding the console uses to read input.
		//
		// Returns:
		//	The encoding used to read console input.
		//
		// Exceptions:
		//	T:System.ArgumentNullException:
		//	The property value in a set operation is null.
		//
		//	T:System.IO.IOException:
		//	An error occurred during the execution of this operation.
		//
		//	T:System.Security.SecurityException:
		//	Your application does not have permission to perform this operation.
		/// <summary>
		/// 
		/// </summary>
		public static Encoding InputEncoding { get; set; }
		//
		// Summary:
		//	Gets or sets the encoding the console uses to write output.
		//
		// Returns:
		//	The encoding used to write console output.
		//
		// Exceptions:
		//	T:System.ArgumentNullException:
		//	The property value in a set operation is null.
		//
		//	T:System.IO.IOException:
		//	An error occurred during the execution of this operation.
		//
		//	T:System.Security.SecurityException:
		//	Your application does not have permission to perform this operation.
		/// <summary>
		/// 
		/// </summary>
		public static Encoding OutputEncoding { get; set; }
		//
		// Summary:
		//	Gets or sets the background color of the console.
		//
		// Returns:
		//	A value that specifies the background color of the console; that is, the color
		//	that appears behind each character. The default is black.
		//
		// Exceptions:
		//	T:System.ArgumentException:
		//	The color specified in a set operation is not a valid member of System.ConsoleColor.
		//
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.

		static ConsoleColor _defaultBackgroundColor = ConsoleColor.Black;

		/// <summary>
		/// 
		/// </summary>
		public static ConsoleColor BackgroundColor { get; set; } = _defaultBackgroundColor;

		//
		// Summary:
		//	Gets or sets the foreground color of the console.
		//
		// Returns:
		//	A System.ConsoleColor that specifies the foreground color of the console; that
		//	is, the color of each character that is displayed. The default is gray.
		//
		// Exceptions:
		//	T:System.ArgumentException:
		//	The color specified in a set operation is not a valid member of System.ConsoleColor.
		//
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.

		static ConsoleColor _defaultForegroundColor = ConsoleColor.Gray;

		/// <summary>
		/// 
		/// </summary>
		public static ConsoleColor ForegroundColor { get; set; } = _defaultForegroundColor;
		//
		// Summary:
		//	Gets or sets the height of the buffer area.
		//
		// Returns:
		//	The current height, in rows, of the buffer area.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	The value in a set operation is less than or equal to zero.-or- The value in
		//	a set operation is greater than or equal to System.Int16.MaxValue.-or- The value
		//	in a set operation is less than System.Console.WindowTop + System.Console.WindowHeight.
		//
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		public static int BufferHeight { get; set; } = HEIGHT;
		//
		// Summary:
		//	Gets or sets the width of the buffer area.
		//
		// Returns:
		//	The current width, in columns, of the buffer area.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	The value in a set operation is less than or equal to zero.-or- The value in
		//	a set operation is greater than or equal to System.Int16.MaxValue.-or- The value
		//	in a set operation is less than System.Console.WindowLeft + System.Console.WindowWidth.
		//
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		public static int BufferWidth { get; set; } = WIDTH;
		//
		// Summary:
		//	Gets or sets the height of the console window area.
		//
		// Returns:
		//	The height of the console window measured in rows.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	The value of the System.Console.WindowWidth property or the value of the System.Console.WindowHeight
		//	property is less than or equal to 0.-or-The value of the System.Console.WindowHeight
		//	property plus the value of the System.Console.WindowTop property is greater than
		//	or equal to System.Int16.MaxValue.-or-The value of the System.Console.WindowWidth
		//	property or the value of the System.Console.WindowHeight property is greater
		//	than the largest possible window width or height for the current screen resolution
		//	and console font.
		//
		//	T:System.IO.IOException:
		//	Error reading or writing information.
		/// <summary>
		/// 
		/// </summary>
		public static int WindowHeight { get; set; } = HEIGHT;
		//
		// Summary:
		//	Gets or sets a value indicating whether the combination of the System.ConsoleModifiers.Control
		//	modifier key and System.ConsoleKey.C console key (Ctrl+C) is treated as ordinary
		//	input or as an interruption that is handled by the operating system.
		//
		// Returns:
		//	true if Ctrl+C is treated as ordinary input; otherwise, false.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	Unable to get or set the input mode of the console input buffer.
		/// <summary>
		/// 
		/// </summary>
		public static bool TreatControlCAsInput { get; set; }
		//
		// Summary:
		//	Gets the largest possible number of console window columns, based on the current
		//	font and screen resolution.
		//
		// Returns:
		//	The width of the largest possible console window measured in columns.
		/// <summary>
		/// 
		/// </summary>
		public static int LargestWindowWidth { get; }
		//
		// Summary:
		//	Gets the largest possible number of console window rows, based on the current
		//	font and screen resolution.
		//
		// Returns:
		//	The height of the largest possible console window measured in rows.
		/// <summary>
		/// 
		/// </summary>
		public static int LargestWindowHeight { get; }
		//
		// Summary:
		//	Gets or sets the leftmost position of the console window area relative to the
		//	screen buffer.
		//
		// Returns:
		//	The leftmost console window position measured in columns.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	In a set operation, the value to be assigned is less than zero.-or-As a result
		//	of the assignment, System.Console.WindowLeft plus System.Console.WindowWidth
		//	would exceed System.Console.BufferWidth.
		//
		//	T:System.IO.IOException:
		//	Error reading or writing information.
		/// <summary>
		/// 
		/// </summary>
		public static int WindowLeft { get; set; }
		//
		// Summary:
		//	Gets or sets the top position of the console window area relative to the screen
		//	buffer.
		//
		// Returns:
		//	The uppermost console window position measured in rows.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	In a set operation, the value to be assigned is less than zero.-or-As a result
		//	of the assignment, System.Console.WindowTop plus System.Console.WindowHeight
		//	would exceed System.Console.BufferHeight.
		//
		//	T:System.IO.IOException:
		//	Error reading or writing information.
		/// <summary>
		/// 
		/// </summary>
		public static int WindowTop { get; set; }
		//
		// Summary:
		//	Gets or sets the column position of the cursor within the buffer area.
		//
		// Returns:
		//	The current position, in columns, of the cursor.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	The value in a set operation is less than zero.-or- The value in a set operation
		//	is greater than or equal to System.Console.BufferWidth.
		//
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		public static int CursorLeft { get; set; }
		//
		// Summary:
		//	Gets or sets the row position of the cursor within the buffer area.
		//
		// Returns:
		//	The current position, in rows, of the cursor.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	The value in a set operation is less than zero.-or- The value in a set operation
		//	is greater than or equal to System.Console.BufferHeight.
		//
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		public static int CursorTop { get; set; }
		//
		// Summary:
		//	Gets or sets the height of the cursor within a character cell.
		//
		// Returns:
		//	The size of the cursor expressed as a percentage of the height of a character
		//	cell. The property value ranges from 1 to 100.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	The value specified in a set operation is less than 1 or greater than 100.
		//
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		public static int CursorSize { get; set; }
		//
		// Summary:
		//	Gets or sets a value indicating whether the cursor is visible.
		//
		// Returns:
		//	true if the cursor is visible; otherwise, false.
		//
		// Exceptions:
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		public static bool CursorVisible { get; set; }
		//
		// Summary:
		//	Gets or sets the title to display in the console title bar.
		//
		// Returns:
		//	The string to be displayed in the title bar of the console. The maximum length
		//	of the title string is 24500 characters.
		//
		// Exceptions:
		//	T:System.InvalidOperationException:
		//	In a get operation, the retrieved title is longer than 24500 characters.
		//
		//	T:System.ArgumentOutOfRangeException:
		//	In a set operation, the specified title is longer than 24500 characters.
		//
		//	T:System.ArgumentNullException:
		//	In a set operation, the specified title is null.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		public static string Title { get; set; }
		//
		// Summary:
		//	Gets a value indicating whether a key press is available in the input stream.
		//
		// Returns:
		//	true if a key press is available; otherwise, false.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//
		//	T:System.InvalidOperationException:
		//	Standard input is redirected to a file instead of the keyboard.
		/// <summary>
		/// 
		/// </summary>
		public static bool KeyAvailable { get; }
		//
		// Summary:
		//	Gets a value indicating whether the NUM LOCK keyboard toggle is turned on or
		//	turned off.
		//
		// Returns:
		//	true if NUM LOCK is turned on; false if NUM LOCK is turned off.
		/// <summary>
		/// 
		/// </summary>
		public static bool NumberLock { get; }
		//
		// Summary:
		//	Gets a value indicating whether the CAPS LOCK keyboard toggle is turned on or
		//	turned off.
		//
		// Returns:
		//	true if CAPS LOCK is turned on; false if CAPS LOCK is turned off.
		/// <summary>
		/// 
		/// </summary>
		public static bool CapsLock { get; }
		//
		// Summary:
		//	Gets a value that indicates whether input has been redirected from the standard
		//	input stream.
		//
		// Returns:
		//	true if input is redirected; otherwise, false.
		/// <summary>
		/// 
		/// </summary>
		public static bool IsInputRedirected { get; }

		//
		// Summary:
		//	Plays the sound of a beep through the console speaker.
		//
		// Exceptions:
		//	T:System.Security.HostProtectionException:
		//	This method was executed on a server, such as SQL Server, that does not permit
		//	access to a user interface.
		/// <summary>
		/// 
		/// </summary>
		public static void Beep ()
		{
			throw new NotImplementedException ();
		}
		//
		// Summary:
		//	Plays the sound of a beep of a specified frequency and duration through the console
		//	speaker.
		//
		// Parameters:
		//	frequency:
		//	The frequency of the beep, ranging from 37 to 32767 hertz.
		//
		//	duration:
		//	The duration of the beep measured in milliseconds.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	frequency is less than 37 or more than 32767 hertz.-or- duration is less than
		//	or equal to zero.
		//
		//	T:System.Security.HostProtectionException:
		//	This method was executed on a server, such as SQL Server, that does not permit
		//	access to the console.
		/// <summary>
		/// 
		/// </summary>
		public static void Beep (int frequency, int duration)
		{
			throw new NotImplementedException ();
		}
		//
		// Summary:
		//	Clears the console buffer and corresponding console window of display information.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.

		static char [,] _buffer = new char [WindowWidth, WindowHeight];

		/// <summary>
		/// 
		/// </summary>
		public static void Clear ()
		{
			_buffer = new char [BufferWidth, BufferHeight];
			SetCursorPosition (0, 0);
		}

		//
		// Summary:
		//	Copies a specified source area of the screen buffer to a specified destination
		//	area.
		//
		// Parameters:
		//	sourceLeft:
		//	The leftmost column of the source area.
		//
		//	sourceTop:
		//	The topmost row of the source area.
		//
		//	sourceWidth:
		//	The number of columns in the source area.
		//
		//	sourceHeight:
		//	The number of rows in the source area.
		//
		//	targetLeft:
		//	The leftmost column of the destination area.
		//
		//	targetTop:
		//	The topmost row of the destination area.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	One or more of the parameters is less than zero.-or- sourceLeft or targetLeft
		//	is greater than or equal to System.Console.BufferWidth.-or- sourceTop or targetTop
		//	is greater than or equal to System.Console.BufferHeight.-or- sourceTop + sourceHeight
		//	is greater than or equal to System.Console.BufferHeight.-or- sourceLeft + sourceWidth
		//	is greater than or equal to System.Console.BufferWidth.
		//
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		public static void MoveBufferArea (int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Copies a specified source area of the screen buffer to a specified destination
		//	area.
		//
		// Parameters:
		//	sourceLeft:
		//	The leftmost column of the source area.
		//
		//	sourceTop:
		//	The topmost row of the source area.
		//
		//	sourceWidth:
		//	The number of columns in the source area.
		//
		//	sourceHeight:
		//	The number of rows in the source area.
		//
		//	targetLeft:
		//	The leftmost column of the destination area.
		//
		//	targetTop:
		//	The topmost row of the destination area.
		//
		//	sourceChar:
		//	The character used to fill the source area.
		//
		//	sourceForeColor:
		//	The foreground color used to fill the source area.
		//
		//	sourceBackColor:
		//	The background color used to fill the source area.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	One or more of the parameters is less than zero.-or- sourceLeft or targetLeft
		//	is greater than or equal to System.Console.BufferWidth.-or- sourceTop or targetTop
		//	is greater than or equal to System.Console.BufferHeight.-or- sourceTop + sourceHeight
		//	is greater than or equal to System.Console.BufferHeight.-or- sourceLeft + sourceWidth
		//	is greater than or equal to System.Console.BufferWidth.
		//
		//	T:System.ArgumentException:
		//	One or both of the color parameters is not a member of the System.ConsoleColor
		//	enumeration.
		//
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//[SecuritySafeCritical]
		/// <summary>
		/// 
		/// </summary>
		public static void MoveBufferArea (int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Acquires the standard error stream.
		//
		// Returns:
		//	The standard error stream.
		/// <summary>
		/// 
		/// </summary>
		public static Stream OpenStandardError ()
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Acquires the standard error stream, which is set to a specified buffer size.
		//
		// Parameters:
		//	bufferSize:
		//	The internal stream buffer size.
		//
		// Returns:
		//	The standard error stream.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	bufferSize is less than or equal to zero.
		/// <summary>
		/// 
		/// </summary>
		public static Stream OpenStandardError (int bufferSize)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Acquires the standard input stream, which is set to a specified buffer size.
		//
		// Parameters:
		//	bufferSize:
		//	The internal stream buffer size.
		//
		// Returns:
		//	The standard input stream.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	bufferSize is less than or equal to zero.
		/// <summary>
		/// 
		/// </summary>
		public static Stream OpenStandardInput (int bufferSize)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Acquires the standard input stream.
		//
		// Returns:
		//	The standard input stream.
		/// <summary>
		/// 
		/// </summary>
		public static Stream OpenStandardInput ()
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Acquires the standard output stream, which is set to a specified buffer size.
		//
		// Parameters:
		//	bufferSize:
		//	The internal stream buffer size.
		//
		// Returns:
		//	The standard output stream.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	bufferSize is less than or equal to zero.
		/// <summary>
		/// 
		/// </summary>
		public static Stream OpenStandardOutput (int bufferSize)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Acquires the standard output stream.
		//
		// Returns:
		//	The standard output stream.
		/// <summary>
		/// 
		/// </summary>
		public static Stream OpenStandardOutput ()
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Reads the next character from the standard input stream.
		//
		// Returns:
		//	The next character from the input stream, or negative one (-1) if there are currently
		//	no more characters to be read.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		public static int Read ()
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Obtains the next character or function key pressed by the user. The pressed key
		//	is optionally displayed in the console window.
		//
		// Parameters:
		//	intercept:
		//	Determines whether to display the pressed key in the console window. true to
		//	not display the pressed key; otherwise, false.
		//
		// Returns:
		//	An object that describes the System.ConsoleKey constant and Unicode character,
		//	if any, that correspond to the pressed console key. The System.ConsoleKeyInfo
		//	object also describes, in a bitwise combination of System.ConsoleModifiers values,
		//	whether one or more Shift, Alt, or Ctrl modifier keys was pressed simultaneously
		//	with the console key.
		//
		// Exceptions:
		//	T:System.InvalidOperationException:
		//	The System.Console.In property is redirected from some stream other than the
		//	console.
		//[SecuritySafeCritical]
		/// <summary>
		/// 
		/// </summary>
		public static ConsoleKeyInfo ReadKey (bool intercept)
		{
			if (MockKeyPresses.Count > 0) {
				return MockKeyPresses.Pop ();
			} else {
				return new ConsoleKeyInfo ('\0', (ConsoleKey)'\0', false, false, false);
			}
		}

		/// <summary>
		/// A stack of keypresses to return when ReadKey is called.
		/// </summary>
		public static Stack<ConsoleKeyInfo> MockKeyPresses = new Stack<ConsoleKeyInfo> ();

		/// <summary>
		///  Helper to push a <see cref="Key"/> onto <see cref="MockKeyPresses"/>.
		/// </summary>
		/// <param name="key"></param>
		public static void PushMockKeyPress (Key key)
		{
			MockKeyPresses.Push (new ConsoleKeyInfo (
				(char)(key & ~Key.CtrlMask & ~Key.ShiftMask & ~Key.AltMask),
				ConsoleKeyMapping.GetConsoleKeyFromKey (key),
				key.HasFlag (Key.ShiftMask),
				key.HasFlag (Key.AltMask),
				key.HasFlag (Key.CtrlMask)));
		}

		//
		// Summary:
		//	Obtains the next character or function key pressed by the user. The pressed key
		//	is displayed in the console window.
		//
		// Returns:
		//	An object that describes the System.ConsoleKey constant and Unicode character,
		//	if any, that correspond to the pressed console key. The System.ConsoleKeyInfo
		//	object also describes, in a bitwise combination of System.ConsoleModifiers values,
		//	whether one or more Shift, Alt, or Ctrl modifier keys was pressed simultaneously
		//	with the console key.
		//
		// Exceptions:
		//	T:System.InvalidOperationException:
		//	The System.Console.In property is redirected from some stream other than the
		//	console.
		/// <summary>
		/// 
		/// </summary>
		public static ConsoleKeyInfo ReadKey ()
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Reads the next line of characters from the standard input stream.
		//
		// Returns:
		//	The next line of characters from the input stream, or null if no more lines are
		//	available.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//
		//	T:System.OutOfMemoryException:
		//	There is insufficient memory to allocate a buffer for the returned string.
		//
		//	T:System.ArgumentOutOfRangeException:
		//	The number of characters in the next line of characters is greater than System.Int32.MaxValue.
		/// <summary>
		/// 
		/// </summary>
		public static string ReadLine ()
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Sets the foreground and background console colors to their defaults.
		//
		// Exceptions:
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//[SecuritySafeCritical]
		/// <summary>
		/// 
		/// </summary>
		public static void ResetColor ()
		{
			BackgroundColor = _defaultBackgroundColor;
			ForegroundColor = _defaultForegroundColor;
		}

		//
		// Summary:
		//	Sets the height and width of the screen buffer area to the specified values.
		//
		// Parameters:
		//	width:
		//	The width of the buffer area measured in columns.
		//
		//	height:
		//	The height of the buffer area measured in rows.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	height or width is less than or equal to zero.-or- height or width is greater
		//	than or equal to System.Int16.MaxValue.-or- width is less than System.Console.WindowLeft
		//	+ System.Console.WindowWidth.-or- height is less than System.Console.WindowTop
		//	+ System.Console.WindowHeight.
		//
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//[SecuritySafeCritical]
		/// <summary>
		/// 
		/// </summary>
		public static void SetBufferSize (int width, int height)
		{
			BufferWidth = width;
			BufferHeight = height;
			_buffer = new char [BufferWidth, BufferHeight];
		}

		//
		// Summary:
		//	Sets the position of the cursor.
		//
		// Parameters:
		//	left:
		//	The column position of the cursor. Columns are numbered from left to right starting
		//	at 0.
		//
		//	top:
		//	The row position of the cursor. Rows are numbered from top to bottom starting
		//	at 0.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	left or top is less than zero.-or- left is greater than or equal to System.Console.BufferWidth.-or-
		//	top is greater than or equal to System.Console.BufferHeight.
		//
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//[SecuritySafeCritical]
		/// <summary>
		/// 
		/// </summary>
		public static void SetCursorPosition (int left, int top)
		{
			CursorLeft = left;
			CursorTop = top;
			WindowLeft = Math.Max (Math.Min (left, BufferWidth - WindowWidth), 0);
			WindowTop = Math.Max (Math.Min (top, BufferHeight - WindowHeight), 0);
		}

		//
		// Summary:
		//	Sets the System.Console.Error property to the specified System.IO.TextWriter
		//	object.
		//
		// Parameters:
		//	newError:
		//	A stream that is the new standard error output.
		//
		// Exceptions:
		//	T:System.ArgumentNullException:
		//	newError is null.
		//
		//	T:System.Security.SecurityException:
		//	The caller does not have the required permission.
		//[SecuritySafeCritical]
		/// <summary>
		/// 
		/// </summary>
		public static void SetError (TextWriter newError)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Sets the System.Console.In property to the specified System.IO.TextReader object.
		//
		// Parameters:
		//	newIn:
		//	A stream that is the new standard input.
		//
		// Exceptions:
		//	T:System.ArgumentNullException:
		//	newIn is null.
		//
		//	T:System.Security.SecurityException:
		//	The caller does not have the required permission.
		//[SecuritySafeCritical]
		/// <summary>
		/// 
		/// </summary>
		public static void SetIn (TextReader newIn)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Sets the System.Console.Out property to the specified System.IO.TextWriter object.
		//
		// Parameters:
		//	newOut:
		//	A stream that is the new standard output.
		//
		// Exceptions:
		//	T:System.ArgumentNullException:
		//	newOut is null.
		//
		//	T:System.Security.SecurityException:
		//	The caller does not have the required permission.
		//[SecuritySafeCritical]
		/// <summary>
		/// 
		/// </summary>
		/// <param name="newOut"></param>
		public static void SetOut (TextWriter newOut)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Sets the position of the console window relative to the screen buffer.
		//
		// Parameters:
		//	left:
		//	The column position of the upper left corner of the console window.
		//
		//	top:
		//	The row position of the upper left corner of the console window.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	left or top is less than zero.-or- left + System.Console.WindowWidth is greater
		//	than System.Console.BufferWidth.-or- top + System.Console.WindowHeight is greater
		//	than System.Console.BufferHeight.
		//
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//[SecuritySafeCritical]
		/// <summary>
		/// 
		/// </summary>
		/// <param name="left"></param>
		/// <param name="top"></param>
		public static void SetWindowPosition (int left, int top)
		{
			WindowLeft = left;
			WindowTop = top;
		}

		//
		// Summary:
		//	Sets the height and width of the console window to the specified values.
		//
		// Parameters:
		//	width:
		//	The width of the console window measured in columns.
		//
		//	height:
		//	The height of the console window measured in rows.
		//
		// Exceptions:
		//	T:System.ArgumentOutOfRangeException:
		//	width or height is less than or equal to zero.-or- width plus System.Console.WindowLeft
		//	or height plus System.Console.WindowTop is greater than or equal to System.Int16.MaxValue.
		//	-or- width or height is greater than the largest possible window width or height
		//	for the current screen resolution and console font.
		//
		//	T:System.Security.SecurityException:
		//	The user does not have permission to perform this action.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//[SecuritySafeCritical]
		/// <summary>
		/// 
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public static void SetWindowSize (int width, int height)
		{
			WindowWidth = width;
			WindowHeight = height;
		}

		//
		// Summary:
		//	Writes the specified string value to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void Write (string value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified object to the standard output
		//	stream.
		//
		// Parameters:
		//	value:
		//	The value to write, or null.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void Write (object value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified 64-bit unsigned integer value
		//	to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//[CLSCompliant (false)]
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void Write (ulong value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified 64-bit signed integer value to
		//	the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void Write (long value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified objects to the standard output
		//	stream using the specified format information.
		//
		// Parameters:
		//	format:
		//	A composite format string (see Remarks).
		//
		//	arg0:
		//	The first object to write using format.
		//
		//	arg1:
		//	The second object to write using format.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//
		//	T:System.ArgumentNullException:
		//	format is null.
		//
		//	T:System.FormatException:
		//	The format specification in format is invalid.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		public static void Write (string format, object arg0, object arg1)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified 32-bit signed integer value to
		//	the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void Write (int value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified object to the standard output
		//	stream using the specified format information.
		//
		// Parameters:
		//	format:
		//	A composite format string (see Remarks).
		//
		//	arg0:
		//	An object to write using format.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//
		//	T:System.ArgumentNullException:
		//	format is null.
		//
		//	T:System.FormatException:
		//	The format specification in format is invalid.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <param name="arg0"></param>
		public static void Write (string format, object arg0)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified 32-bit unsigned integer value
		//	to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//[CLSCompliant (false)]
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void Write (uint value)
		{
			throw new NotImplementedException ();
		}

		//[CLSCompliant (false)]
		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		/// <param name="arg2"></param>
		/// <param name="arg3"></param>
		public static void Write (string format, object arg0, object arg1, object arg2, object arg3)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified array of objects to the standard
		//	output stream using the specified format information.
		//
		// Parameters:
		//	format:
		//	A composite format string (see Remarks).
		//
		//	arg:
		//	An array of objects to write using format.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//
		//	T:System.ArgumentNullException:
		//	format or arg is null.
		//
		//	T:System.FormatException:
		//	The format specification in format is invalid.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <param name="arg"></param>
		public static void Write (string format, params object [] arg)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified Boolean value to the standard
		//	output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void Write (bool value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the specified Unicode character value to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void Write (char value)
		{
			_buffer [CursorLeft, CursorTop] = value;
		}

		//
		// Summary:
		//	Writes the specified array of Unicode characters to the standard output stream.
		//
		// Parameters:
		//	buffer:
		//	A Unicode character array.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		public static void Write (char [] buffer)
		{
			_buffer [CursorLeft, CursorTop] = (char)0;
			foreach (var ch in buffer) {
				_buffer [CursorLeft, CursorTop] += ch;
			}
		}

		//
		// Summary:
		//	Writes the specified subarray of Unicode characters to the standard output stream.
		//
		// Parameters:
		//	buffer:
		//	An array of Unicode characters.
		//
		//	index:
		//	The starting position in buffer.
		//
		//	count:
		//	The number of characters to write.
		//
		// Exceptions:
		//	T:System.ArgumentNullException:
		//	buffer is null.
		//
		//	T:System.ArgumentOutOfRangeException:
		//	index or count is less than zero.
		//
		//	T:System.ArgumentException:
		//	index plus count specify a position that is not within buffer.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <param name="count"></param>
		public static void Write (char [] buffer, int index, int count)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified objects to the standard output
		//	stream using the specified format information.
		//
		// Parameters:
		//	format:
		//	A composite format string (see Remarks).
		//
		//	arg0:
		//	The first object to write using format.
		//
		//	arg1:
		//	The second object to write using format.
		//
		//	arg2:
		//	The third object to write using format.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//
		//	T:System.ArgumentNullException:
		//	format is null.
		//
		//	T:System.FormatException:
		//	The format specification in format is invalid.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		/// <param name="arg2"></param>
		public static void Write (string format, object arg0, object arg1, object arg2)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified System.Decimal value to the standard
		//	output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void Write (decimal value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified single-precision floating-point
		//	value to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void Write (float value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified double-precision floating-point
		//	value to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void Write (double value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the current line terminator to the standard output stream.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		public static void WriteLine ()
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified single-precision floating-point
		//	value, followed by the current line terminator, to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void WriteLine (float value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified 32-bit signed integer value,
		//	followed by the current line terminator, to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void WriteLine (int value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified 32-bit unsigned integer value,
		//	followed by the current line terminator, to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//[CLSCompliant (false)]
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void WriteLine (uint value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified 64-bit signed integer value,
		//	followed by the current line terminator, to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void WriteLine (long value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified 64-bit unsigned integer value,
		//	followed by the current line terminator, to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//[CLSCompliant (false)]
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void WriteLine (ulong value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified object, followed by the current
		//	line terminator, to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void WriteLine (object value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the specified string value, followed by the current line terminator, to
		//	the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void WriteLine (string value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified object, followed by the current
		//	line terminator, to the standard output stream using the specified format information.
		//
		// Parameters:
		//	format:
		//	A composite format string (see Remarks).
		//
		//	arg0:
		//	An object to write using format.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//
		//	T:System.ArgumentNullException:
		//	format is null.
		//
		//	T:System.FormatException:
		//	The format specification in format is invalid.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <param name="arg0"></param>
		public static void WriteLine (string format, object arg0)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified objects, followed by the current
		//	line terminator, to the standard output stream using the specified format information.
		//
		// Parameters:
		//	format:
		//	A composite format string (see Remarks).
		//
		//	arg0:
		//	The first object to write using format.
		//
		//	arg1:
		//	The second object to write using format.
		//
		//	arg2:
		//	The third object to write using format.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//
		//	T:System.ArgumentNullException:
		//	format is null.
		//
		//	T:System.FormatException:
		//	The format specification in format is invalid.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		/// <param name="arg2"></param>
		public static void WriteLine (string format, object arg0, object arg1, object arg2)
		{
			throw new NotImplementedException ();
		}

		//[CLSCompliant (false)]
		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		/// <param name="arg2"></param>
		/// <param name="arg3"></param>
		public static void WriteLine (string format, object arg0, object arg1, object arg2, object arg3)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified array of objects, followed by
		//	the current line terminator, to the standard output stream using the specified
		//	format information.
		//
		// Parameters:
		//	format:
		//	A composite format string (see Remarks).
		//
		//	arg:
		//	An array of objects to write using format.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//
		//	T:System.ArgumentNullException:
		//	format or arg is null.
		//
		//	T:System.FormatException:
		//	The format specification in format is invalid.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <param name="arg"></param>
		public static void WriteLine (string format, params object [] arg)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the specified subarray of Unicode characters, followed by the current
		//	line terminator, to the standard output stream.
		//
		// Parameters:
		//	buffer:
		//	An array of Unicode characters.
		//
		//	index:
		//	The starting position in buffer.
		//
		//	count:
		//	The number of characters to write.
		//
		// Exceptions:
		//	T:System.ArgumentNullException:
		//	buffer is null.
		//
		//	T:System.ArgumentOutOfRangeException:
		//	index or count is less than zero.
		//
		//	T:System.ArgumentException:
		//	index plus count specify a position that is not within buffer.
		//
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <param name="count"></param>
		public static void WriteLine (char [] buffer, int index, int count)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified System.Decimal value, followed
		//	by the current line terminator, to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void WriteLine (decimal value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the specified array of Unicode characters, followed by the current line
		//	terminator, to the standard output stream.
		//
		// Parameters:
		//	buffer:
		//	A Unicode character array.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		public static void WriteLine (char [] buffer)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the specified Unicode character, followed by the current line terminator,
		//	value to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void WriteLine (char value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified Boolean value, followed by the
		//	current line terminator, to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void WriteLine (bool value)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified objects, followed by the current
		//	line terminator, to the standard output stream using the specified format information.
		//
		// Parameters:
		//	format:
		//	A composite format string (see Remarks).
		//
		//	arg0:
		//	The first object to write using format.
		//
		//	arg1:
		//	The second object to write using format.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		//
		//	T:System.ArgumentNullException:
		//	format is null.
		//
		//	T:System.FormatException:
		//	The format specification in format is invalid.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="format"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		public static void WriteLine (string format, object arg0, object arg1)
		{
			throw new NotImplementedException ();
		}

		//
		// Summary:
		//	Writes the text representation of the specified double-precision floating-point
		//	value, followed by the current line terminator, to the standard output stream.
		//
		// Parameters:
		//	value:
		//	The value to write.
		//
		// Exceptions:
		//	T:System.IO.IOException:
		//	An I/O error occurred.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public static void WriteLine (double value)
		{
			throw new NotImplementedException ();
		}

	}
}
