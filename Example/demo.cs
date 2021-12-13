using NStack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Terminal.Gui;

static class Demo {
	class Box10x : View {
		int w = 40;
		int h = 50;

		public bool WantCursorPosition { get; set; } = false;

		public Box10x (int x, int y) : base (new Rect (x, y, 20, 10))
		{
		}

		public Size GetContentSize ()
		{
			return new Size (w, h);
		}

		public void SetCursorPosition (Point pos)
		{
			throw new NotImplementedException ();
		}

		public override void Redraw (Rect bounds)
		{
			//Point pos = new Point (region.X, region.Y);
			Driver.SetAttribute (ColorScheme.Focus);

			for (int y = 0; y < h; y++) {
				Move (0, y);
				Driver.AddStr (y.ToString ());
				for (int x = 0; x < w - y.ToString ().Length; x++) {
					//Driver.AddRune ((Rune)('0' + (x + y) % 10));
					if (y.ToString ().Length < w)
						Driver.AddStr (" ");
				}
			}
			//Move (pos.X, pos.Y);
		}
	}

	class Filler : View {
		int w = 40;
		int h = 50;

		public Filler (Rect rect) : base (rect)
		{
			w = rect.Width;
			h = rect.Height;
		}

		public Size GetContentSize ()
		{
			return new Size (w, h);
		}

		public override void Redraw (Rect bounds)
		{
			Driver.SetAttribute (ColorScheme.Focus);
			var f = Frame;
			w = 0;
			h = 0;

			for (int y = 0; y < f.Width; y++) {
				Move (0, y);
				var nw = 0;
				for (int x = 0; x < f.Height; x++) {
					Rune r;
					switch (x % 3) {
					case 0:
						var er = y.ToString ().ToCharArray (0, 1) [0];
						nw += er.ToString ().Length;
						Driver.AddRune (er);
						if (y > 9) {
							er = y.ToString ().ToCharArray (1, 1) [0];
							nw += er.ToString ().Length;
							Driver.AddRune (er);
						}
						r = '.';
						break;
					case 1:
						r = 'o';
						break;
					default:
						r = 'O';
						break;
					}
					Driver.AddRune (r);
					nw += Rune.RuneLen (r);
				}
				if (nw > w)
					w = nw;
				h = y + 1;
			}
		}
	}

	static void ShowTextAlignments ()
	{
		var container = new Window ("Show Text Alignments - Press Esc to return") {
			X = 0,
			Y = 0,
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		container.KeyUp += (e) => {
			if (e.KeyEvent.Key == Key.Esc)
				container.Running = false;
		};

		int i = 0;
		string txt = "Hello world, how are you doing today?";
		container.Add (
				new Label ($"{i + 1}-{txt}") { TextAlignment = TextAlignment.Left, Y = 3, Width = Dim.Fill () },
				new Label ($"{i + 2}-{txt}") { TextAlignment = TextAlignment.Right, Y = 5, Width = Dim.Fill () },
				new Label ($"{i + 3}-{txt}") { TextAlignment = TextAlignment.Centered, Y = 7, Width = Dim.Fill () },
				new Label ($"{i + 4}-{txt}") { TextAlignment = TextAlignment.Justified, Y = 9, Width = Dim.Fill () }
			);

		Application.Run (container);
	}

	static void ShowEntries (View container)
	{
		var scrollView = new ScrollView (new Rect (50, 10, 20, 8)) {
			ContentSize = new Size (20, 50),
			//ContentOffset = new Point (0, 0),
			ShowVerticalScrollIndicator = true,
			ShowHorizontalScrollIndicator = true
		};
#if false
		scrollView.Add (new Box10x (0, 0));
#else
		var filler = new Filler (new Rect (0, 0, 40, 40));
		scrollView.Add (filler);
		scrollView.DrawContent += (r) => {
			scrollView.ContentSize = filler.GetContentSize ();
		};
#endif

		// This is just to debug the visuals of the scrollview when small
		var scrollView2 = new ScrollView (new Rect (72, 10, 3, 3)) {
			ContentSize = new Size (100, 100),
			ShowVerticalScrollIndicator = true,
			ShowHorizontalScrollIndicator = true
		};
		scrollView2.Add (new Box10x (0, 0));
		var progress = new ProgressBar (new Rect (68, 1, 10, 1));
		bool timer (MainLoop caller)
		{
			progress.Pulse ();
			return true;
		}

		Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (300), timer);


		// A little convoluted, this is because I am using this to test the
		// layout based on referencing elements of another view:

		var login = new Label ("Login: ") { X = 3, Y = 6 };
		var password = new Label ("Password: ") {
			X = Pos.Left (login),
			Y = Pos.Bottom (login) + 1
		};
		var loginText = new TextField ("") {
			X = Pos.Right (password),
			Y = Pos.Top (login),
			Width = 40
		};

		var passText = new TextField ("") {
			Secret = true,
			X = Pos.Left (loginText),
			Y = Pos.Top (password),
			Width = Dim.Width (loginText)
		};

		var tf = new Button (3, 19, "Ok");
		var frameView = new FrameView (new Rect (3, 10, 25, 6), "Options");
		frameView.Add (new CheckBox (1, 0, "Remember me"));
		frameView.Add (new RadioGroup (1, 2, new ustring [] { "_Personal", "_Company" }));
		// Add some content
		container.Add (
			login,
			loginText,
			password,
			passText,
			frameView,
			new ListView (new Rect (59, 6, 16, 4), new string [] {
				"First row",
				"<>",
				"This is a very long row that should overflow what is shown",
				"4th",
				"There is an empty slot on the second row",
				"Whoa",
				"This is so cool"
			}),
			scrollView,
			scrollView2,
			tf,
			new Button (10, 19, "Cancel"),
			new TimeField (3, 20, DateTime.Now.TimeOfDay),
			new TimeField (23, 20, DateTime.Now.TimeOfDay, true),
			new DateField (3, 22, DateTime.Now),
			new DateField (23, 22, DateTime.Now, true),
			progress,
			new Label (3, 24, "Press F9 (on Unix, ESC+9 is an alias) or Ctrl+T to activate the menubar"),
			menuKeysStyle,
			menuAutoMouseNav

		);
		container.SendSubviewToBack (tf);
	}

	public static Label ml2;

	static void NewFile ()
	{
		var ok = new Button ("Ok", is_default: true);
		ok.Clicked += () => { Application.RequestStop (); };
		var cancel = new Button ("Cancel");
		cancel.Clicked += () => { Application.RequestStop (); };
		var d = new Dialog ("New File", 50, 20, ok, cancel);
		ml2 = new Label (1, 1, "Mouse Debug Line");
		d.Add (ml2);
		Application.Run (d);
	}

	//
	static void Editor ()
	{
		Application.Init ();
		Application.HeightAsBuffer = heightAsBuffer;

		var ntop = Application.Top;

		var text = new TextView () { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

		string fname = GetFileName ();

		var win = new Window (fname ?? "Untitled") {
			X = 0,
			Y = 1,
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		ntop.Add (win);

		if (fname != null)
			text.Text = System.IO.File.ReadAllText (fname);
		win.Add (text);

		void Paste ()
		{
			if (text != null) {
				text.Paste ();
			}
		}

		void Cut ()
		{
			if (text != null) {
				text.Cut ();
			}
		}

		void Copy ()
		{
			if (text != null) {
				text.Copy ();
			}
		}

		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_Close", "", () => { if (Quit ()) { running = MainApp; Application.RequestStop (); } }, null, null, Key.AltMask | Key.Q),
			}),
			new MenuBarItem ("_Edit", new MenuItem [] {
				new MenuItem ("_Copy", "", Copy, null, null, Key.C | Key.CtrlMask),
				new MenuItem ("C_ut", "", Cut, null, null, Key.X | Key.CtrlMask),
				new MenuItem ("_Paste", "", Paste, null, null, Key.Y | Key.CtrlMask)
			}),
		});
		ntop.Add (menu);

		Application.Run (ntop);
	}

	private static string GetFileName ()
	{
		string fname = null;
		foreach (var s in new [] { "/etc/passwd", "c:\\windows\\win.ini" })
			if (System.IO.File.Exists (s)) {
				fname = s;
				break;
			}

		return fname;
	}

	static bool Quit ()
	{
		var n = MessageBox.Query (50, 7, "Quit Demo", "Are you sure you want to quit this demo?", "Yes", "No");
		return n == 0;
	}

	static void Close ()
	{
		MessageBox.ErrorQuery (50, 7, "Error", "There is nothing to close", "Ok");
	}

	// Watch what happens when I try to introduce a newline after the first open brace
	// it introduces a new brace instead, and does not indent.  Then watch me fight
	// the editor as more oddities happen.

	public static void Open ()
	{
		var d = new OpenDialog ("Open", "Open a file") { AllowsMultipleSelection = true };
		Application.Run (d);

		if (!d.Canceled)
			MessageBox.Query (50, 7, "Selected File", d.FilePaths.Count > 0 ? string.Join (", ", d.FilePaths) : d.FilePath, "Ok");
	}

	public static void ShowHex ()
	{
		var ntop = Application.Top;
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_Close", "", () => { running = MainApp; Application.RequestStop (); }, null, null, Key.AltMask | Key.Q),
			}),
		});
		ntop.Add (menu);

		string fname = GetFileName ();
		var win = new Window (fname) {
			X = 0,
			Y = 1,
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		ntop.Add (win);

		var source = System.IO.File.OpenRead (fname);
		var hex = new HexView (source) {
			X = 0,
			Y = 0,
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (hex);
		Application.Run (ntop);
	}

	public class MenuItemDetails : MenuItem {
		ustring title;
		string help;
		Action action;

		public MenuItemDetails (ustring title, string help, Action action) : base (title, help, action)
		{
			this.title = title;
			this.help = help;
			this.action = action;
		}

		public static MenuItemDetails Instance (MenuItem mi)
		{
			return (MenuItemDetails)mi.GetMenuItem ();
		}
	}

	public delegate MenuItem MenuItemDelegate (MenuItemDetails menuItem);

	public static void ShowMenuItem (MenuItem mi)
	{
		BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
		MethodInfo minfo = typeof (MenuItemDetails).GetMethod ("Instance", flags);
		MenuItemDelegate mid = (MenuItemDelegate)Delegate.CreateDelegate (typeof (MenuItemDelegate), minfo);
		MessageBox.Query (70, 7, mi.Title.ToString (),
			$"{mi.Title.ToString ()} selected. Is from submenu: {mi.GetMenuBarItem ()}", "Ok");
	}

	static void MenuKeysStyle_Toggled (bool e)
	{
		menu.UseKeysUpDownAsKeysLeftRight = menuKeysStyle.Checked;
	}

	static void MenuAutoMouseNav_Toggled (bool e)
	{
		menu.WantMousePositionReports = menuAutoMouseNav.Checked;
	}

	static void Copy ()
	{
		TextField textField = menu.LastFocused as TextField ?? Application.Top.MostFocused as TextField;
		if (textField != null && textField.SelectedLength != 0) {
			textField.Copy ();
		}
	}

	static void Cut ()
	{
		TextField textField = menu.LastFocused as TextField ?? Application.Top.MostFocused as TextField;
		if (textField != null && textField.SelectedLength != 0) {
			textField.Cut ();
		}
	}

	static void Paste ()
	{
		TextField textField = menu.LastFocused as TextField ?? Application.Top.MostFocused as TextField;
		textField?.Paste ();
	}

	static void Help ()
	{
		MessageBox.Query (50, 7, "Help", "This is a small help\nBe kind.", "Ok");
	}

	static void Load ()
	{
		MessageBox.Query (50, 7, "Load", "This is a small load\nBe kind.", "Ok");
	}

	static void Save ()
	{
		MessageBox.Query (50, 7, "Save", "This is a small save\nBe kind.", "Ok");
	}


	#region Selection Demo

	static void ListSelectionDemo (bool multiple)
	{
		var ok = new Button ("Ok", is_default: true);
		ok.Clicked += () => { Application.RequestStop (); };
		var cancel = new Button ("Cancel");
		cancel.Clicked += () => { Application.RequestStop (); };
		var d = new Dialog ("Selection Demo", 60, 20, ok, cancel);

		var animals = new List<string> () { "Alpaca", "Llama", "Lion", "Shark", "Goat" };
		var msg = new Label ("Use space bar or control-t to toggle selection") {
			X = 1,
			Y = 1,
			Width = Dim.Fill () - 1,
			Height = 1
		};

		var list = new ListView (animals) {
			X = 1,
			Y = 3,
			Width = Dim.Fill () - 4,
			Height = Dim.Fill () - 4,
			AllowsMarking = true,
			AllowsMultipleSelection = multiple
		};
		d.Add (msg, list);
		Application.Run (d);

		var result = "";
		for (int i = 0; i < animals.Count; i++) {
			if (list.Source.IsMarked (i)) {
				result += animals [i] + " ";
			}
		}
		MessageBox.Query (60, 10, "Selected Animals", result == "" ? "No animals selected" : result, "Ok");
	}

	class MultiLineViewSource : IListDataSource {
		class Item {
			internal ustring Label;
			internal ustring Sublabel;
			internal bool Marked;
			public Item (String label, String sub)
			{
				Label = label;
				Sublabel = sub;
			}
		}

		List<Item> items = new List<Item> () {
			new Item ("CPU", "14% of 3.41Ghz"),
			new Item ("Memory", "15.9/31.9 Gb (50%)"),
			new Item ("Disk 0 - C:", "14%")
		};

		public int Count => items.Count;

		public int Length => 40;

		public bool IsMarked (int item)
		{
			return items [item].Marked;	
		}

		void Draw (ConsoleDriver driver, ustring ustr, int width)
		{
			int byteLen = ustr.Length;
			int used = 0;
			
			for (int i = 0; i < byteLen;) {
				(var rune, var size) = Utf8.DecodeRune (ustr, i, i - byteLen);
				var count = Rune.ColumnWidth (rune);
				if (used + count > width)
					break;
				driver.AddRune (rune);
				used += count;
				i += size;
			}
			for (; used < width; used++) {
				driver.AddRune (' ');
			}
		}

		public void Render (ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
		{
			var height = container.ItemHeight;
			
			var t = items [item];
			container.Move (col, line);
			Draw (driver, t.Label, width);
			container.Move (col, line+1);
			Draw (driver, t.Sublabel, width);
		}

		public void SetMark (int item, bool value)
		{
			items [item].Marked = value;
		}

		public IList ToList ()
		{
			return items;
		}
	}

	static void MultiLineDemo ()
	{
		var ok = new Button ("Ok", is_default: true);
		ok.Clicked += () => { Application.RequestStop (); };
		var cancel = new Button ("Cancel");
		cancel.Clicked += () => { Application.RequestStop (); };
		var d = new Dialog ("Multi-line Demo", 60, 20, ok, cancel);
		
		var msg = new Label ("Each item uses two lines") {
			X = 1,
			Y = 1,
			Width = Dim.Fill () - 1,
			Height = 1
		};

		var multiSource = new MultiLineViewSource ();
		var list = new ListView (multiSource) {
			X = 1,
			Y = 3,
			Width = Dim.Fill () - 4,
			Height = Dim.Fill () - 4,
		};
		list.ItemHeight = 2;
		d.Add (msg, list);
		Application.Run (d);
	}

	static void ComboBoxDemo ()
	{
		//TODO: Duplicated code in ListsAndCombos.cs Consider moving to shared assembly
		var items = new List<ustring> ();
		foreach (var dir in new [] { "/etc", @$"{Environment.GetEnvironmentVariable ("SystemRoot")}\System32" }) {
			if (Directory.Exists (dir)) {
				items = Directory.GetFiles (dir).Union (Directory.GetDirectories (dir))
					.Select (Path.GetFileName)
					.Where (x => char.IsLetterOrDigit (x [0]))
					.OrderBy (x => x).Select (x => ustring.Make (x)).ToList ();
			}
		}
		var list = new ComboBox () { Width = Dim.Fill (), Height = Dim.Fill () };
		list.SetSource (items);
		list.OpenSelectedItem += (ListViewItemEventArgs text) => { Application.RequestStop (); };

		var d = new Dialog () { Title = "Select source file", Width = Dim.Percent (50), Height = Dim.Percent (50) };
		d.Add (list);
		Application.Run (d);

		MessageBox.Query (60, 10, "Selected file", list.Text.ToString () == "" ? "Nothing selected" : list.Text.ToString (), "Ok");
	}
	#endregion


	#region KeyDown / KeyPress / KeyUp Demo
	private static void OnKeyDownPressUpDemo ()
	{
		var close = new Button ("Close");
		close.Clicked += () => { Application.RequestStop (); };
		var container = new Dialog ("KeyDown & KeyPress & KeyUp demo", 80, 20, close) {
			Width = Dim.Fill (),
			Height = Dim.Fill (),
		};

		var list = new List<string> ();
		var listView = new ListView (list) {
			X = 0,
			Y = 0,
			Width = Dim.Fill () - 1,
			Height = Dim.Fill () - 2,
		};
		listView.ColorScheme = Colors.TopLevel;
		container.Add (listView);

		void KeyDownPressUp (KeyEvent keyEvent, string updown)
		{
			const int ident = -5;
			switch (updown) {
			case "Down":
			case "Up":
			case "Press":
				var msg = $"Key{updown,ident}: ";
				if ((keyEvent.Key & Key.ShiftMask) != 0)
					msg += "Shift ";
				if ((keyEvent.Key & Key.CtrlMask) != 0)
					msg += "Ctrl ";
				if ((keyEvent.Key & Key.AltMask) != 0)
					msg += "Alt ";
				msg += $"{(((uint)keyEvent.KeyValue & (uint)Key.CharMask) > 26 ? $"{(char)keyEvent.KeyValue}" : $"{keyEvent.Key}")}";
				//list.Add (msg);
				list.Add (keyEvent.ToString ());

				break;

			default:
				if ((keyEvent.Key & Key.ShiftMask) != 0) {
					list.Add ($"Key{updown,ident}: Shift ");
				} else if ((keyEvent.Key & Key.CtrlMask) != 0) {
					list.Add ($"Key{updown,ident}: Ctrl ");
				} else if ((keyEvent.Key & Key.AltMask) != 0) {
					list.Add ($"Key{updown,ident}: Alt ");
				} else {
					list.Add ($"Key{updown,ident}: {(((uint)keyEvent.KeyValue & (uint)Key.CharMask) > 26 ? $"{(char)keyEvent.KeyValue}" : $"{keyEvent.Key}")}");
				}

				break;
			}
			listView.MoveDown ();
		}

		container.KeyDown += (e) => KeyDownPressUp (e.KeyEvent, "Down");
		container.KeyPress += (e) => KeyDownPressUp (e.KeyEvent, "Press");
		container.KeyUp += (e) => KeyDownPressUp (e.KeyEvent, "Up");
		Application.Run (container);
	}
	#endregion

	public static Action running = MainApp;
	static void Main (string [] args)
	{
		if (args.Length > 0 && args.Contains ("-usc")) {
			Application.UseSystemConsole = true;
		}

		Console.OutputEncoding = System.Text.Encoding.Default;

		while (running != null) {
			running.Invoke ();
		}
		Application.Shutdown ();
	}

	public static Label ml;
	public static MenuBar menu;
	public static CheckBox menuKeysStyle;
	public static CheckBox menuAutoMouseNav;
	private static bool heightAsBuffer = false;
	static void MainApp ()
	{
		if (Debugger.IsAttached)
			CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");

		Application.Init ();
		Application.HeightAsBuffer = heightAsBuffer;
		//ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.FramePadding | ConsoleDriver.DiagnosticFlags.FrameRuler;

		var top = Application.Top;

		//Open ();
#if true
		int margin = 3;
		var win = new Window ("Hello") {
			X = 1,
			Y = 1,

			Width = Dim.Fill () - margin,
			Height = Dim.Fill () - margin
		};
#else
		var tframe = top.Frame;

		var win = new Window (new Rect (0, 1, tframe.Width, tframe.Height - 1), "Hello");
#endif
		MenuItemDetails [] menuItems = {
			new MenuItemDetails ("F_ind", "", null),
			new MenuItemDetails ("_Replace", "", null),
			new MenuItemDetails ("_Item1", "", null),
			new MenuItemDetails ("_Also From Sub Menu", "", null)
		};

		menuItems [0].Action = () => ShowMenuItem (menuItems [0]);
		menuItems [1].Action = () => ShowMenuItem (menuItems [1]);
		menuItems [2].Action = () => ShowMenuItem (menuItems [2]);
		menuItems [3].Action = () => ShowMenuItem (menuItems [3]);

		MenuItem miUseSubMenusSingleFrame = null;
		var useSubMenusSingleFrame = false;
		MenuItem miUseKeysUpDownAsKeysLeftRight = null;
		var useKeysUpDownAsKeysLeftRight = false;

		MenuItem miHeightAsBuffer = null;

		menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("Text _Editor Demo", "", () => { running = Editor; Application.RequestStop (); }, null, null, Key.AltMask | Key.CtrlMask | Key.D),
				new MenuItem ("_New", "Creates new file", NewFile, null, null, Key.AltMask | Key.CtrlMask| Key.N),
				new MenuItem ("_Open", "", Open, null, null, Key.AltMask | Key.CtrlMask| Key.O),
				new MenuItem ("_Hex", "", () => { running = ShowHex; Application.RequestStop (); }, null, null, Key.AltMask | Key.CtrlMask | Key.H),
				new MenuItem ("_Close", "", Close, null, null, Key.AltMask | Key.Q),
				new MenuItem ("_Disabled", "", () => { }, () => false),
				new MenuBarItem ("_SubMenu Disabled", new MenuItem [] {
					new MenuItem ("_Disabled", "", () => { }, () => false)
				}),
				null,
				new MenuItem ("_Quit", "", () => { if (Quit ()) { running = null; top.Running = false; } }, null, null, Key.CtrlMask | Key.Q)
			}),
			new MenuBarItem ("_Edit", new MenuItem [] {
				new MenuItem ("_Copy", "", Copy, null, null, Key.AltMask | Key.CtrlMask | Key.C),
				new MenuItem ("C_ut", "", Cut, null, null, Key.AltMask | Key.CtrlMask| Key.X),
				new MenuItem ("_Paste", "", Paste, null, null, Key.AltMask | Key.CtrlMask| Key.V),
				new MenuBarItem ("_Find and Replace",
					new MenuItem [] { menuItems [0], menuItems [1] }),
				menuItems[3],
				miUseKeysUpDownAsKeysLeftRight = new MenuItem ("Use_KeysUpDownAsKeysLeftRight", "",
				() => {
				menu.UseKeysUpDownAsKeysLeftRight = miUseKeysUpDownAsKeysLeftRight.Checked = useKeysUpDownAsKeysLeftRight = !useKeysUpDownAsKeysLeftRight;
					miUseSubMenusSingleFrame.Checked = useSubMenusSingleFrame = menu.UseSubMenusSingleFrame;
					}) {
					CheckType = MenuItemCheckStyle.Checked, Checked = useKeysUpDownAsKeysLeftRight
				},
				miUseSubMenusSingleFrame = new MenuItem ("Use_SubMenusSingleFrame", "",
				() => {
				menu.UseSubMenusSingleFrame = miUseSubMenusSingleFrame.Checked = useSubMenusSingleFrame = !useSubMenusSingleFrame;
					miUseKeysUpDownAsKeysLeftRight.Checked = useKeysUpDownAsKeysLeftRight = menu.UseKeysUpDownAsKeysLeftRight;
					}) {
					CheckType = MenuItemCheckStyle.Checked, Checked = useSubMenusSingleFrame
				},
				miHeightAsBuffer = new MenuItem ("_Height As Buffer", "", () => {
					miHeightAsBuffer.Checked = heightAsBuffer = !heightAsBuffer;
					Application.HeightAsBuffer = heightAsBuffer;
				}) { CheckType = MenuItemCheckStyle.Checked, Checked = heightAsBuffer }
			}),
			new MenuBarItem ("_List Demos", new MenuItem [] {
				new MenuItem ("Select _Multiple Items", "", () => ListSelectionDemo (true), null, null, Key.AltMask + 0.ToString () [0]),
				new MenuItem ("Select _Single Item", "", () => ListSelectionDemo (false), null, null, Key.AltMask + 1.ToString () [0]),
				new MenuItem ("Search Single Item", "", ComboBoxDemo, null, null, Key.AltMask + 2.ToString () [0]),
				new MenuItem ("Multi-line item", "", MultiLineDemo, null, null, Key.AltMask + 3.ToString () [0])
			}),
			new MenuBarItem ("A_ssorted", new MenuItem [] {
				new MenuItem ("_Show text alignments", "", () => ShowTextAlignments (), null, null, Key.AltMask | Key.CtrlMask | Key.G),
				new MenuItem ("_OnKeyDown/Press/Up", "", () => OnKeyDownPressUpDemo (), null, null, Key.AltMask | Key.CtrlMask | Key.K)
			}),
			new MenuBarItem ("_Test Menu and SubMenus", new MenuBarItem [] {
				new MenuBarItem ("SubMenu1Item_1",  new MenuBarItem [] {
					new MenuBarItem ("SubMenu2Item_1", new MenuBarItem [] {
						new MenuBarItem ("SubMenu3Item_1",
							new MenuItem [] { menuItems [2] })
					})
				})
			}),
			new MenuBarItem ("_About...", "Demonstrates top-level menu item", () =>  MessageBox.ErrorQuery (50, 7, "About Demo", "This is a demo app for gui.cs", "Ok")),
		});

		menuKeysStyle = new CheckBox (3, 25, "UseKeysUpDownAsKeysLeftRight", true);
		menuKeysStyle.Toggled += MenuKeysStyle_Toggled;
		menuAutoMouseNav = new CheckBox (40, 25, "UseMenuAutoNavigation", true);
		menuAutoMouseNav.Toggled += MenuAutoMouseNav_Toggled;

		ShowEntries (win);

		int count = 0;
		ml = new Label (new Rect (3, 17, 47, 1), "Mouse: ");
		Application.RootMouseEvent += delegate (MouseEvent me) {
			ml.Text = $"Mouse: ({me.X},{me.Y}) - {me.Flags} {count++}";
		};

		var test = new Label (3, 18, "Se iniciará el análisis");
		win.Add (test);
		win.Add (ml);

		var drag = new Label ("Drag: ") { X = 70, Y = 22 };
		var dragText = new TextField ("") {
			X = Pos.Right (drag),
			Y = Pos.Top (drag),
			Width = 40
		};

		var statusBar = new StatusBar (new StatusItem [] {
			new StatusItem(Key.F1, "~F1~ Help", () => Help()),
			new StatusItem(Key.F2, "~F2~ Load", Load),
			new StatusItem(Key.F3, "~F3~ Save", Save),
			new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => { if (Quit ()) { running = null; top.Running = false; } }),
			new StatusItem(Key.Null, Application.Driver.GetType().Name, null)
		});

		win.Add (drag, dragText);

		var bottom = new Label ("This should go on the bottom of the same top-level!");
		win.Add (bottom);
		var bottom2 = new Label ("This should go on the bottom of another top-level!");
		top.Add (bottom2);

		top.LayoutComplete += (e) => {
			bottom.X = win.X;
			bottom.Y = Pos.Bottom (win) - Pos.Top (win) - margin;
			bottom2.X = Pos.Left (win);
			bottom2.Y = Pos.Bottom (win);
		};

		win.KeyPress += Win_KeyPress;

		top.Add (win);
		//top.Add (menu);
		top.Add (menu, statusBar);
		Application.Run (top);
	}

	private static void Win_KeyPress (View.KeyEventEventArgs e)
	{
		switch (ShortcutHelper.GetModifiersKey (e.KeyEvent)) {
		case Key.CtrlMask | Key.T:
			if (menu.IsMenuOpen)
				menu.CloseMenu ();
			else
				menu.OpenMenu ();
			e.Handled = true;
			break;
		}
	}
}
