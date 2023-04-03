//
// TODO:
// * FindNCurses needs to remove the old probing code
// * Removal of that proxy code
// * Need to implement reading pointers with the new API
// * Can remove the manual Dlopen features
// * initscr() diagnostics based on DLL can be fixed
//
// binding.cs.in: Core binding for curses.
//
// This file attempts to call into ncurses without relying on Mono's
// dllmap, so it will work with .NET Core.  This means that it needs
// two sets of bindings, one for "ncurses" which works on OSX, and one
// that works against "libncursesw.so.5" which is what you find on
// assorted Linux systems.
//
// Additionally, I do not want to rely on an external native library
// which is why all this pain to bind two separate ncurses is here.
//
// Authors:
//   Miguel de Icaza (miguel.de.icaza@gmail.com)
//
// Copyright (C) 2007 Novell (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Runtime.InteropServices;

namespace Unix.Terminal {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

	public partial class Curses {
		//[StructLayout (LayoutKind.Sequential)]
		//public struct winsize {
		//	public ushort ws_row;
		//	public ushort ws_col;
		//	public ushort ws_xpixel;   /* unused */
		//	public ushort ws_ypixel;   /* unused */
		//};

		[StructLayout (LayoutKind.Sequential)]
		public struct MouseEvent {
			public short ID;
			public int X, Y, Z;
			public Event ButtonState;
		}

		static int lines, cols;
		static Window main_window;
		static IntPtr curses_handle, curscr_ptr, lines_ptr, cols_ptr;

		// If true, uses the DllImport into "ncurses", otherwise "libncursesw.so.5"
		//static bool use_naked_driver;

		static UnmanagedLibrary curses_library;
		static NativeMethods methods;


		[DllImport ("libc")]
		public extern static int setlocale (int cate, [MarshalAs (UnmanagedType.LPStr)] string locale);

		//[DllImport ("libc")]
		//public extern static int ioctl (int fd, int cmd, out winsize argp);

		static void LoadMethods ()
		{
			var libs = UnmanagedLibrary.IsMacOSPlatform ? new string [] { "libncurses.dylib" } : new string [] { "libncursesw.so.6", "libncursesw.so.5" };
			curses_library = new UnmanagedLibrary (libs, false);
			methods = new NativeMethods (curses_library);
		}

		static void FindNCurses ()
		{
			LoadMethods ();
			curses_handle = methods.UnmanagedLibrary.NativeLibraryHandle;

			stdscr = read_static_ptr ("stdscr");
			curscr_ptr = get_ptr ("curscr");
			lines_ptr = get_ptr ("LINES");
			cols_ptr = get_ptr ("COLS");
		}

		static public Window initscr ()
		{
			setlocale (LC_ALL, "");
			FindNCurses ();

			// Prevents the terminal from being locked after exiting.
			reset_shell_mode ();

			main_window = new Window (methods.initscr ());
			try {
				console_sharp_get_dims (out lines, out cols);
			} catch (DllNotFoundException) {
				endwin ();
				Console.Error.WriteLine ("Unable to find the @MONO_CURSES@ native library\n" +
							 "this is different than the managed mono-curses.dll\n\n" +
							 "Typically you need to install to a LD_LIBRARY_PATH directory\n" +
							 "or DYLD_LIBRARY_PATH directory or run /sbin/ldconfig");
				Environment.Exit (1);
			}
			return main_window;
		}

		public static int Lines {
			get {
				return lines;
			}
		}

		public static int Cols {
			get {
				return cols;
			}
		}

		//
		// Returns true if the window changed since the last invocation, as a
		// side effect, the Lines and Cols properties are updated
		//
		public static bool CheckWinChange ()
		{
			int l, c;

			console_sharp_get_dims (out l, out c);

			if (l == 1 || l != lines || c != cols) {
				lines = l;
				cols = c;
				return true;
			}
			return false;
		}

		public static int addstr (string format, params object [] args)
		{
			var s = string.Format (format, args);
			return addwstr (s);
		}

		static char [] r = new char [1];

		//
		// Have to wrap the native addch, as it can not
		// display unicode characters, we have to use addstr
		// for that.   but we need addch to render special ACS
		// characters
		//
		public static int addch (int ch)
		{
			if (ch < 127 || ch > 0xffff)
				return methods.addch (ch);
			char c = (char)ch;
			return addwstr (new String (c, 1));
		}

		public static int mvaddch (int y, int x, int ch)
		{
			if (ch < 127 || ch > 0xffff)
				return methods.mvaddch (y, x, ch);
			char c = (char)ch;
			return mvaddwstr (y, x, new String (c, 1));
		}

		static IntPtr stdscr;

		static IntPtr get_ptr (string key)
		{
			var ptr = curses_library.LoadSymbol (key);

			if (ptr == IntPtr.Zero)
				throw new Exception ("Could not load the key " + key);
			return ptr;
		}

		internal static IntPtr read_static_ptr (string key)
		{
			var ptr = get_ptr (key);
			return Marshal.ReadIntPtr (ptr);
		}

		internal static IntPtr console_sharp_get_stdscr () => stdscr;

		internal static IntPtr console_sharp_get_curscr ()
		{
			return Marshal.ReadIntPtr (curscr_ptr);
		}

		internal static void console_sharp_get_dims (out int lines, out int cols)
		{
			lines = Marshal.ReadInt32 (lines_ptr);
			cols = Marshal.ReadInt32 (cols_ptr);

			//int cmd;
			//if (UnmanagedLibrary.IsMacOSPlatform) {
			//	cmd = TIOCGWINSZ_MAC;
			//} else {
			//	cmd = TIOCGWINSZ;
			//}

			//if (ioctl (1, cmd, out winsize ws) == 0) {
			//	lines = ws.ws_row;
			//	cols = ws.ws_col;

			//	if (lines == Lines && cols == Cols) {
			//		return;
			//	}

			//	resizeterm (lines, cols);
			//} else {
			//	lines = Lines;
			//	cols = Cols;
			//}
		}

		public static Event mousemask (Event newmask, out Event oldmask)
		{
			IntPtr e;
			var ret = (Event)(methods.mousemask ((IntPtr)newmask, out e));
			oldmask = (Event)e;
			return ret;
		}

		// We encode ESC + char (what Alt-char generates) as 0x2000 + char
		public const int KeyAlt = 0x2000;

		static public int IsAlt (int key)
		{
			if ((key & KeyAlt) != 0)
				return key & ~KeyAlt;
			return 0;
		}

		public static int StartColor () => methods.start_color ();
		public static bool HasColors => methods.has_colors ();
		public static int InitColorPair (short pair, short foreground, short background) => methods.init_pair (pair, foreground, background);
		public static int UseDefaultColors () => methods.use_default_colors ();
		public static int ColorPairs => methods.COLOR_PAIRS ();

		//
		// The proxy methods to call into each version
		//
		static public int endwin () => methods.endwin ();
		static public bool isendwin () => methods.isendwin ();
		static public int cbreak () => methods.cbreak ();
		static public int nocbreak () => methods.nocbreak ();
		static public int echo () => methods.echo ();
		static public int noecho () => methods.noecho ();
		static public int halfdelay (int t) => methods.halfdelay (t);
		static public int raw () => methods.raw ();
		static public int noraw () => methods.noraw ();
		static public void noqiflush () => methods.noqiflush ();
		static public void qiflush () => methods.qiflush ();
		static public int typeahead (IntPtr fd) => methods.typeahead (fd);
		static public int timeout (int delay) => methods.timeout (delay);
		static public int wtimeout (IntPtr win, int delay) => methods.wtimeout (win, delay);
		static public int notimeout (IntPtr win, bool bf) => methods.notimeout (win, bf);
		static public int keypad (IntPtr win, bool bf) => methods.keypad (win, bf);
		static public int meta (IntPtr win, bool bf) => methods.meta (win, bf);
		static public int intrflush (IntPtr win, bool bf) => methods.intrflush (win, bf);
		static public int clearok (IntPtr win, bool bf) => methods.clearok (win, bf);
		static public int idlok (IntPtr win, bool bf) => methods.idlok (win, bf);
		static public void idcok (IntPtr win, bool bf) => methods.idcok (win, bf);
		static public void immedok (IntPtr win, bool bf) => methods.immedok (win, bf);
		static public int leaveok (IntPtr win, bool bf) => methods.leaveok (win, bf);
		static public int wsetscrreg (IntPtr win, int top, int bot) => methods.wsetscrreg (win, top, bot);
		static public int scrollok (IntPtr win, bool bf) => methods.scrollok (win, bf);
		static public int nl () => methods.nl ();
		static public int nonl () => methods.nonl ();
		static public int setscrreg (int top, int bot) => methods.setscrreg (top, bot);
		static public int refresh () => methods.refresh ();
		static public int doupdate () => methods.doupdate ();
		static public int wrefresh (IntPtr win) => methods.wrefresh (win);
		static public int redrawwin (IntPtr win) => methods.redrawwin (win);
		//static public int wredrawwin (IntPtr win, int beg_line, int num_lines) => methods.wredrawwin (win, beg_line, num_lines);
		static public int wnoutrefresh (IntPtr win) => methods.wnoutrefresh (win);
		static public int move (int line, int col) => methods.move (line, col);
		static public int curs_set (int visibility) => methods.curs_set (visibility);
		//static public int addch (int ch) => methods.addch (ch);
		static public int addwstr (string s) => methods.addwstr (s);
		static public int mvaddwstr (int y, int x, string s) => methods.mvaddwstr (y, x, s);
		static public int wmove (IntPtr win, int line, int col) => methods.wmove (win, line, col);
		static public int waddch (IntPtr win, int ch) => methods.waddch (win, ch);
		static public int attron (int attrs) => methods.attron (attrs);
		static public int attroff (int attrs) => methods.attroff (attrs);
		static public int attrset (int attrs) => methods.attrset (attrs);
		static public int getch () => methods.getch ();
		static public int get_wch (out int sequence) => methods.get_wch (out sequence);
		static public int ungetch (int ch) => methods.ungetch (ch);
		static public int mvgetch (int y, int x) => methods.mvgetch (y, x);
		static public bool has_colors () => methods.has_colors ();
		static public int start_color () => methods.start_color ();
		static public int init_pair (short pair, short f, short b) => methods.init_pair (pair, f, b);
		static public int use_default_colors () => methods.use_default_colors ();
		static public int COLOR_PAIRS () => methods.COLOR_PAIRS ();
		static public uint getmouse (out MouseEvent ev) => methods.getmouse (out ev);
		static public uint ungetmouse (ref MouseEvent ev) => methods.ungetmouse (ref ev);
		static public int mouseinterval (int interval) => methods.mouseinterval (interval);
		static public bool is_term_resized (int lines, int columns) => methods.is_term_resized (lines, columns);
		static public int resize_term (int lines, int columns) => methods.resize_term (lines, columns);
		static public int resizeterm (int lines, int columns) => methods.resizeterm (lines, columns);
		static public void use_env (bool f) => methods.use_env (f);
		static public int flushinp () => methods.flushinp ();
		static public int def_prog_mode () => methods.def_prog_mode ();
		static public int def_shell_mode () => methods.def_shell_mode ();
		static public int reset_prog_mode () => methods.reset_prog_mode ();
		static public int reset_shell_mode () => methods.reset_shell_mode ();
		static public int savetty () => methods.savetty ();
		static public int resetty () => methods.resetty ();
		static public int set_escdelay (int size) => methods.set_escdelay (size);
	}

#pragma warning disable RCS1102 // Make class static.
	internal class Delegates {
#pragma warning restore RCS1102 // Make class static.
		public delegate IntPtr initscr ();
		public delegate int endwin ();
		public delegate bool isendwin ();
		public delegate int cbreak ();
		public delegate int nocbreak ();
		public delegate int echo ();
		public delegate int noecho ();
		public delegate int halfdelay (int t);
		public delegate int raw ();
		public delegate int noraw ();
		public delegate void noqiflush ();
		public delegate void qiflush ();
		public delegate int typeahead (IntPtr fd);
		public delegate int timeout (int delay);
		public delegate int wtimeout (IntPtr win, int delay);
		public delegate int notimeout (IntPtr win, bool bf);
		public delegate int keypad (IntPtr win, bool bf);
		public delegate int meta (IntPtr win, bool bf);
		public delegate int intrflush (IntPtr win, bool bf);
		public delegate int clearok (IntPtr win, bool bf);
		public delegate int idlok (IntPtr win, bool bf);
		public delegate void idcok (IntPtr win, bool bf);
		public delegate void immedok (IntPtr win, bool bf);
		public delegate int leaveok (IntPtr win, bool bf);
		public delegate int wsetscrreg (IntPtr win, int top, int bot);
		public delegate int scrollok (IntPtr win, bool bf);
		public delegate int nl ();
		public delegate int nonl ();
		public delegate int setscrreg (int top, int bot);
		public delegate int refresh ();
		public delegate int doupdate ();
		public delegate int wrefresh (IntPtr win);
		public delegate int redrawwin (IntPtr win);
		//public delegate int wredrawwin (IntPtr win, int beg_line, int num_lines);
		public delegate int wnoutrefresh (IntPtr win);
		public delegate int move (int line, int col);
		public delegate int curs_set (int visibility);
		public delegate int addch (int ch);
		public delegate int mvaddch (int y, int x, int ch);
		public delegate int addwstr ([MarshalAs (UnmanagedType.LPWStr)] string s);
		public delegate int mvaddwstr (int y, int x, [MarshalAs (UnmanagedType.LPWStr)] string s);
		public delegate int wmove (IntPtr win, int line, int col);
		public delegate int waddch (IntPtr win, int ch);
		public delegate int attron (int attrs);
		public delegate int attroff (int attrs);
		public delegate int attrset (int attrs);
		public delegate int getch ();
		public delegate int get_wch (out int sequence);
		public delegate int ungetch (int ch);
		public delegate int mvgetch (int y, int x);
		public delegate bool has_colors ();
		public delegate int start_color ();
		public delegate int init_pair (short pair, short f, short b);
		public delegate int use_default_colors ();
		public delegate int COLOR_PAIRS ();
		public delegate uint getmouse (out Curses.MouseEvent ev);
		public delegate uint ungetmouse (ref Curses.MouseEvent ev);
		public delegate int mouseinterval (int interval);
		public delegate IntPtr mousemask (IntPtr newmask, out IntPtr oldMask);
		public delegate bool is_term_resized (int lines, int columns);
		public delegate int resize_term (int lines, int columns);
		public delegate int resizeterm (int lines, int columns);
		public delegate void use_env (bool f);
		public delegate int flushinp ();
		public delegate int def_prog_mode ();
		public delegate int def_shell_mode ();
		public delegate int reset_prog_mode ();
		public delegate int reset_shell_mode ();
		public delegate int savetty ();
		public delegate int resetty ();
		public delegate int set_escdelay (int size);
	}

	internal class NativeMethods {
		public readonly Delegates.initscr initscr;
		public readonly Delegates.endwin endwin;
		public readonly Delegates.isendwin isendwin;
		public readonly Delegates.cbreak cbreak;
		public readonly Delegates.nocbreak nocbreak;
		public readonly Delegates.echo echo;
		public readonly Delegates.noecho noecho;
		public readonly Delegates.halfdelay halfdelay;
		public readonly Delegates.raw raw;
		public readonly Delegates.noraw noraw;
		public readonly Delegates.noqiflush noqiflush;
		public readonly Delegates.qiflush qiflush;
		public readonly Delegates.typeahead typeahead;
		public readonly Delegates.timeout timeout;
		public readonly Delegates.wtimeout wtimeout;
		public readonly Delegates.notimeout notimeout;
		public readonly Delegates.keypad keypad;
		public readonly Delegates.meta meta;
		public readonly Delegates.intrflush intrflush;
		public readonly Delegates.clearok clearok;
		public readonly Delegates.idlok idlok;
		public readonly Delegates.idcok idcok;
		public readonly Delegates.immedok immedok;
		public readonly Delegates.leaveok leaveok;
		public readonly Delegates.wsetscrreg wsetscrreg;
		public readonly Delegates.scrollok scrollok;
		public readonly Delegates.nl nl;
		public readonly Delegates.nonl nonl;
		public readonly Delegates.setscrreg setscrreg;
		public readonly Delegates.refresh refresh;
		public readonly Delegates.doupdate doupdate;
		public readonly Delegates.wrefresh wrefresh;
		public readonly Delegates.redrawwin redrawwin;
		//public readonly Delegates.wredrawwin wredrawwin;
		public readonly Delegates.wnoutrefresh wnoutrefresh;
		public readonly Delegates.move move;
		public readonly Delegates.curs_set curs_set;
		public readonly Delegates.addch addch;
		public readonly Delegates.mvaddch mvaddch;
		public readonly Delegates.addwstr addwstr;
		public readonly Delegates.mvaddwstr mvaddwstr;
		public readonly Delegates.wmove wmove;
		public readonly Delegates.waddch waddch;
		public readonly Delegates.attron attron;
		public readonly Delegates.attroff attroff;
		public readonly Delegates.attrset attrset;
		public readonly Delegates.getch getch;
		public readonly Delegates.get_wch get_wch;
		public readonly Delegates.ungetch ungetch;
		public readonly Delegates.mvgetch mvgetch;
		public readonly Delegates.has_colors has_colors;
		public readonly Delegates.start_color start_color;
		public readonly Delegates.init_pair init_pair;
		public readonly Delegates.use_default_colors use_default_colors;
		public readonly Delegates.COLOR_PAIRS COLOR_PAIRS;
		public readonly Delegates.getmouse getmouse;
		public readonly Delegates.ungetmouse ungetmouse;
		public readonly Delegates.mouseinterval mouseinterval;
		public readonly Delegates.mousemask mousemask;
		public readonly Delegates.is_term_resized is_term_resized;
		public readonly Delegates.resize_term resize_term;
		public readonly Delegates.resizeterm resizeterm;
		public readonly Delegates.use_env use_env;
		public readonly Delegates.flushinp flushinp;
		public readonly Delegates.def_prog_mode def_prog_mode;
		public readonly Delegates.def_shell_mode def_shell_mode;
		public readonly Delegates.reset_prog_mode reset_prog_mode;
		public readonly Delegates.reset_shell_mode reset_shell_mode;
		public readonly Delegates.savetty savetty;
		public readonly Delegates.resetty resetty;
		public readonly Delegates.set_escdelay set_escdelay;
		public UnmanagedLibrary UnmanagedLibrary;

		public NativeMethods (UnmanagedLibrary lib)
		{
			this.UnmanagedLibrary = lib;
			initscr = lib.GetNativeMethodDelegate<Delegates.initscr> ("initscr");
			endwin = lib.GetNativeMethodDelegate<Delegates.endwin> ("endwin");
			isendwin = lib.GetNativeMethodDelegate<Delegates.isendwin> ("isendwin");
			cbreak = lib.GetNativeMethodDelegate<Delegates.cbreak> ("cbreak");
			nocbreak = lib.GetNativeMethodDelegate<Delegates.nocbreak> ("nocbreak");
			echo = lib.GetNativeMethodDelegate<Delegates.echo> ("echo");
			noecho = lib.GetNativeMethodDelegate<Delegates.noecho> ("noecho");
			halfdelay = lib.GetNativeMethodDelegate<Delegates.halfdelay> ("halfdelay");
			raw = lib.GetNativeMethodDelegate<Delegates.raw> ("raw");
			noraw = lib.GetNativeMethodDelegate<Delegates.noraw> ("noraw");
			noqiflush = lib.GetNativeMethodDelegate<Delegates.noqiflush> ("noqiflush");
			qiflush = lib.GetNativeMethodDelegate<Delegates.qiflush> ("qiflush");
			typeahead = lib.GetNativeMethodDelegate<Delegates.typeahead> ("typeahead");
			timeout = lib.GetNativeMethodDelegate<Delegates.timeout> ("timeout");
			wtimeout = lib.GetNativeMethodDelegate<Delegates.wtimeout> ("wtimeout");
			notimeout = lib.GetNativeMethodDelegate<Delegates.notimeout> ("notimeout");
			keypad = lib.GetNativeMethodDelegate<Delegates.keypad> ("keypad");
			meta = lib.GetNativeMethodDelegate<Delegates.meta> ("meta");
			intrflush = lib.GetNativeMethodDelegate<Delegates.intrflush> ("intrflush");
			clearok = lib.GetNativeMethodDelegate<Delegates.clearok> ("clearok");
			idlok = lib.GetNativeMethodDelegate<Delegates.idlok> ("idlok");
			idcok = lib.GetNativeMethodDelegate<Delegates.idcok> ("idcok");
			immedok = lib.GetNativeMethodDelegate<Delegates.immedok> ("immedok");
			leaveok = lib.GetNativeMethodDelegate<Delegates.leaveok> ("leaveok");
			wsetscrreg = lib.GetNativeMethodDelegate<Delegates.wsetscrreg> ("wsetscrreg");
			scrollok = lib.GetNativeMethodDelegate<Delegates.scrollok> ("scrollok");
			nl = lib.GetNativeMethodDelegate<Delegates.nl> ("nl");
			nonl = lib.GetNativeMethodDelegate<Delegates.nonl> ("nonl");
			setscrreg = lib.GetNativeMethodDelegate<Delegates.setscrreg> ("setscrreg");
			refresh = lib.GetNativeMethodDelegate<Delegates.refresh> ("refresh");
			doupdate = lib.GetNativeMethodDelegate<Delegates.doupdate> ("doupdate");
			wrefresh = lib.GetNativeMethodDelegate<Delegates.wrefresh> ("wrefresh");
			redrawwin = lib.GetNativeMethodDelegate<Delegates.redrawwin> ("redrawwin");
			//wredrawwin = lib.GetNativeMethodDelegate<Delegates.wredrawwin> ("wredrawwin");
			wnoutrefresh = lib.GetNativeMethodDelegate<Delegates.wnoutrefresh> ("wnoutrefresh");
			move = lib.GetNativeMethodDelegate<Delegates.move> ("move");
			curs_set = lib.GetNativeMethodDelegate<Delegates.curs_set> ("curs_set");
			addch = lib.GetNativeMethodDelegate<Delegates.addch> ("addch");
			mvaddch = lib.GetNativeMethodDelegate<Delegates.mvaddch> ("mvaddch");
			addwstr = lib.GetNativeMethodDelegate<Delegates.addwstr> ("addwstr");
			mvaddwstr = lib.GetNativeMethodDelegate<Delegates.mvaddwstr> ("mvaddwstr");
			wmove = lib.GetNativeMethodDelegate<Delegates.wmove> ("wmove");
			waddch = lib.GetNativeMethodDelegate<Delegates.waddch> ("waddch");
			attron = lib.GetNativeMethodDelegate<Delegates.attron> ("attron");
			attroff = lib.GetNativeMethodDelegate<Delegates.attroff> ("attroff");
			attrset = lib.GetNativeMethodDelegate<Delegates.attrset> ("attrset");
			getch = lib.GetNativeMethodDelegate<Delegates.getch> ("getch");
			get_wch = lib.GetNativeMethodDelegate<Delegates.get_wch> ("get_wch");
			ungetch = lib.GetNativeMethodDelegate<Delegates.ungetch> ("ungetch");
			mvgetch = lib.GetNativeMethodDelegate<Delegates.mvgetch> ("mvgetch");
			has_colors = lib.GetNativeMethodDelegate<Delegates.has_colors> ("has_colors");
			start_color = lib.GetNativeMethodDelegate<Delegates.start_color> ("start_color");
			init_pair = lib.GetNativeMethodDelegate<Delegates.init_pair> ("init_pair");
			use_default_colors = lib.GetNativeMethodDelegate<Delegates.use_default_colors> ("use_default_colors");
			COLOR_PAIRS = lib.GetNativeMethodDelegate<Delegates.COLOR_PAIRS> ("COLOR_PAIRS");
			getmouse = lib.GetNativeMethodDelegate<Delegates.getmouse> ("getmouse");
			ungetmouse = lib.GetNativeMethodDelegate<Delegates.ungetmouse> ("ungetmouse");
			mouseinterval = lib.GetNativeMethodDelegate<Delegates.mouseinterval> ("mouseinterval");
			mousemask = lib.GetNativeMethodDelegate<Delegates.mousemask> ("mousemask");
			is_term_resized = lib.GetNativeMethodDelegate<Delegates.is_term_resized> ("is_term_resized");
			resize_term = lib.GetNativeMethodDelegate<Delegates.resize_term> ("resize_term");
			resizeterm = lib.GetNativeMethodDelegate<Delegates.resizeterm> ("resizeterm");
			use_env = lib.GetNativeMethodDelegate<Delegates.use_env> ("use_env");
			flushinp = lib.GetNativeMethodDelegate<Delegates.flushinp> ("flushinp");
			def_prog_mode = lib.GetNativeMethodDelegate<Delegates.def_prog_mode> ("def_prog_mode");
			def_shell_mode = lib.GetNativeMethodDelegate<Delegates.def_shell_mode> ("def_shell_mode");
			reset_prog_mode = lib.GetNativeMethodDelegate<Delegates.reset_prog_mode> ("reset_prog_mode");
			reset_shell_mode = lib.GetNativeMethodDelegate<Delegates.reset_shell_mode> ("reset_shell_mode");
			savetty = lib.GetNativeMethodDelegate<Delegates.savetty> ("savetty");
			resetty = lib.GetNativeMethodDelegate<Delegates.resetty> ("resetty");
			set_escdelay = lib.GetNativeMethodDelegate<Delegates.set_escdelay> ("set_escdelay");
		}
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
