using System;
using NStack;
using System.Linq;
using System.Collections.Generic;

namespace Terminal.Gui {

	/// <summary>
	/// Specifies how a <see cref="MenuItem"/> shows selection state. 
	/// </summary>
	[Flags]
	public enum MenuItemCheckStyle {
		/// <summary>
		/// The menu item will be shown normally, with no check indicator. The default.
		/// </summary>
		NoCheck = 0b_0000_0000,

		/// <summary>
		/// The menu item will indicate checked/un-checked state (see <see cref="Checked"/>).
		/// </summary>
		Checked = 0b_0000_0001,

		/// <summary>
		/// The menu item is part of a menu radio group (see <see cref="Checked"/>) and will indicate selected state.
		/// </summary>
		Radio = 0b_0000_0010,
	};

	/// <summary>
	/// A <see cref="MenuItem"/> has title, an associated help text, and an action to execute on activation. 
	/// MenuItems can also have a checked indicator (see <see cref="Checked"/>).
	/// </summary>
	public class MenuItem {
		ustring title;
		ShortcutHelper shortcutHelper;
		bool allowNullChecked;
		MenuItemCheckStyle checkType;

		internal int TitleLength => GetMenuBarItemLength (Title);

		/// <summary>
		/// Gets or sets arbitrary data for the menu item.
		/// </summary>
		/// <remarks>This property is not used internally.</remarks>
		public object Data { get; set; }

		/// <summary>
		/// Initializes a new instance of <see cref="MenuItem"/>
		/// </summary>
		public MenuItem (Key shortcut = Key.Null) : this ("", "", null, null, null, shortcut) { }

		/// <summary>
		/// Initializes a new instance of <see cref="MenuItem"/>.
		/// </summary>
		/// <param name="title">Title for the menu item.</param>
		/// <param name="help">Help text to display.</param>
		/// <param name="action">Action to invoke when the menu item is activated.</param>
		/// <param name="canExecute">Function to determine if the action can currently be executed.</param>
		/// <param name="parent">The <see cref="Parent"/> of this menu item.</param>
		/// <param name="shortcut">The <see cref="Shortcut"/> keystroke combination.</param>
		public MenuItem (ustring title, ustring help, Action action, Func<bool> canExecute = null, MenuItem parent = null, Key shortcut = Key.Null)
		{
			Title = title ?? "";
			Help = help ?? "";
			Action = action;
			CanExecute = canExecute;
			Parent = parent;
			shortcutHelper = new ShortcutHelper ();
			if (shortcut != Key.Null) {
				shortcutHelper.Shortcut = shortcut;
			}
		}

		/// <summary>
		/// The HotKey is used to activate a <see cref="MenuItem"/> with the keyboard. HotKeys are defined by prefixing the <see cref="Title"/>
		/// of a MenuItem with an underscore ('_'). 
		/// <para>
		/// Pressing Alt-Hotkey for a <see cref="MenuBarItem"/> (menu items on the menu bar) works even if the menu is not active). 
		/// Once a menu has focus and is active, pressing just the HotKey will activate the MenuItem.
		/// </para>
		/// <para>
		/// For example for a MenuBar with a "_File" MenuBarItem that contains a "_New" MenuItem, Alt-F will open the File menu.
		/// Pressing the N key will then activate the New MenuItem.
		/// </para>
		/// <para>
		/// See also <see cref="Shortcut"/> which enable global key-bindings to menu items.
		/// </para>
		/// </summary>
		public Rune HotKey;

		/// <summary>
		/// Shortcut defines a key binding to the MenuItem that will invoke the MenuItem's action globally for the <see cref="View"/> that is
		/// the parent of the <see cref="MenuBar"/> or <see cref="ContextMenu"/> this <see cref="MenuItem"/>.
		/// <para>
		/// The <see cref="Key"/> will be drawn on the MenuItem to the right of the <see cref="Title"/> and <see cref="Help"/> text. See <see cref="ShortcutTag"/>.
		/// </para>
		/// </summary>
		public Key Shortcut {
			get => shortcutHelper.Shortcut;
			set {
				if (shortcutHelper.Shortcut != value && (ShortcutHelper.PostShortcutValidation (value) || value == Key.Null)) {
					shortcutHelper.Shortcut = value;
				}
			}
		}

		/// <summary>
		/// Gets the text describing the keystroke combination defined by <see cref="Shortcut"/>.
		/// </summary>
		public ustring ShortcutTag => ShortcutHelper.GetShortcutTag (shortcutHelper.Shortcut);

		/// <summary>
		/// Gets or sets the title of the menu item .
		/// </summary>
		/// <value>The title.</value>
		public ustring Title {
			get { return title; }
			set {
				if (title != value) {
					title = value;
					GetHotKey ();
				}
			}
		}

		/// <summary>
		/// Gets or sets the help text for the menu item. The help text is drawn to the right of the <see cref="Title"/>.
		/// </summary>
		/// <value>The help text.</value>
		public ustring Help { get; set; }

		/// <summary>
		/// Gets or sets the action to be invoked when the menu item is triggered.
		/// </summary>
		/// <value>Method to invoke.</value>
		public Action Action { get; set; }

		/// <summary>
		/// Gets or sets the action to be invoked to determine if the menu can be triggered. If <see cref="CanExecute"/> returns <see langword="true"/>
		/// the menu item will be enabled. Otherwise, it will be disabled. 
		/// </summary>
		/// <value>Function to determine if the action is can be executed or not.</value>
		public Func<bool> CanExecute { get; set; }

		/// <summary>
		/// Returns <see langword="true"/> if the menu item is enabled. This method is a wrapper around <see cref="CanExecute"/>.
		/// </summary>
		public bool IsEnabled ()
		{
			return CanExecute == null ? true : CanExecute ();
		}

		// 
		// ┌─────────────────────────────┐
		// │ Quit  Quit UI Catalog  Ctrl+Q │
		// └─────────────────────────────┘
		// ┌─────────────────┐
		// │ ◌ TopLevel Alt+T │
		// └─────────────────┘
		// TODO: Replace the `2` literals with named constants 
		internal int Width => 1 + // space before Title
			TitleLength +
			2 + // space after Title - BUGBUG: This should be 1 
			(Checked == true || CheckType.HasFlag (MenuItemCheckStyle.Checked) || CheckType.HasFlag (MenuItemCheckStyle.Radio) ? 2 : 0) + // check glyph + space 
			(Help.ConsoleWidth > 0 ? 2 + Help.ConsoleWidth : 0) + // Two spaces before Help
			(ShortcutTag.ConsoleWidth > 0 ? 2 + ShortcutTag.ConsoleWidth : 0); // Pad two spaces before shortcut tag (which are also aligned right)

		/// <summary>
		/// Sets or gets whether the <see cref="MenuItem"/> shows a check indicator or not. See <see cref="MenuItemCheckStyle"/>.
		/// </summary>
		public bool? Checked { set; get; }

		/// <summary>
		/// Used only if <see cref="CheckType"/> is of <see cref="MenuItemCheckStyle.Checked"/> type.
		/// If <see langword="true"/> allows <see cref="Checked"/> to be null, true or false.
		/// If <see langword="false"/> only allows <see cref="Checked"/> to be true or false.
		/// </summary>
		public bool AllowNullChecked {
			get => allowNullChecked;
			set {
				allowNullChecked = value;
				if (Checked == null) {
					Checked = false;
				}
			}
		}

		/// <summary>
		/// Sets or gets the <see cref="MenuItemCheckStyle"/> of a menu item where <see cref="Checked"/> is set to <see langword="true"/>.
		/// </summary>
		public MenuItemCheckStyle CheckType {
			get => checkType;
			set {
				checkType = value;
				if (checkType == MenuItemCheckStyle.Checked && !allowNullChecked && Checked == null) {
					Checked = false;
				}
			}
		}

		/// <summary>
		/// Gets the parent for this <see cref="MenuItem"/>.
		/// </summary>
		/// <value>The parent.</value>
		public MenuItem Parent { get; internal set; }

		/// <summary>
		/// Gets if this <see cref="MenuItem"/> is from a sub-menu.
		/// </summary>
		internal bool IsFromSubMenu { get { return Parent != null; } }

		/// <summary>
		/// Merely a debugging aid to see the interaction with main.
		/// </summary>
		public MenuItem GetMenuItem ()
		{
			return this;
		}

		/// <summary>
		/// Merely a debugging aid to see the interaction with main.
		/// </summary>
		public bool GetMenuBarItem ()
		{
			return IsFromSubMenu;
		}

		/// <summary>
		/// Toggle the <see cref="Checked"/> between three states if <see cref="AllowNullChecked"/> is <see langword="true"/>
		/// or between two states if <see cref="AllowNullChecked"/> is <see langword="false"/>.
		/// </summary>
		public void ToggleChecked ()
		{
			if (checkType != MenuItemCheckStyle.Checked) {
				throw new InvalidOperationException ("This isn't a Checked MenuItemCheckStyle!");
			}
			var previousChecked = Checked;
			if (AllowNullChecked) {
				switch (previousChecked) {
				case null:
					Checked = true;
					break;
				case true:
					Checked = false;
					break;
				case false:
					Checked = null;
					break;
				}
			} else {
				Checked = !Checked;
			}
		}

		void GetHotKey ()
		{
			bool nextIsHot = false;
			foreach (var x in title) {
				if (x == MenuBar.HotKeySpecifier) {
					nextIsHot = true;
				} else {
					if (nextIsHot) {
						HotKey = Char.ToUpper ((char)x);
						break;
					}
					nextIsHot = false;
					HotKey = default;
				}
			}
		}

		int GetMenuBarItemLength (ustring title)
		{
			int len = 0;
			foreach (var ch in title) {
				if (ch == MenuBar.HotKeySpecifier)
					continue;
				len += Math.Max (Rune.ColumnWidth (ch), 1);
			}

			return len;
		}
	}

	/// <summary>
	/// <see cref="MenuBarItem"/> is a menu item on an app's <see cref="MenuBar"/>. 
	/// MenuBarItems do not support <see cref="MenuItem.Shortcut"/>.
	/// </summary>
	public class MenuBarItem : MenuItem {
		/// <summary>
		/// Initializes a new <see cref="MenuBarItem"/> as a <see cref="MenuItem"/>.
		/// </summary>
		/// <param name="title">Title for the menu item.</param>
		/// <param name="help">Help text to display. Will be displayed next to the Title surrounded by parentheses.</param>
		/// <param name="action">Action to invoke when the menu item is activated.</param>
		/// <param name="canExecute">Function to determine if the action can currently be executed.</param>
		/// <param name="parent">The parent <see cref="MenuItem"/> of this if exist, otherwise is null.</param>
		public MenuBarItem (ustring title, ustring help, Action action, Func<bool> canExecute = null, MenuItem parent = null) : base (title, help, action, canExecute, parent)
		{
			Initialize (title, null, null, true);
		}

		/// <summary>
		/// Initializes a new <see cref="MenuBarItem"/>.
		/// </summary>
		/// <param name="title">Title for the menu item.</param>
		/// <param name="children">The items in the current menu.</param>
		/// <param name="parent">The parent <see cref="MenuItem"/> of this if exist, otherwise is null.</param>
		public MenuBarItem (ustring title, MenuItem [] children, MenuItem parent = null)
		{
			Initialize (title, children, parent);
		}

		/// <summary>
		/// Initializes a new <see cref="MenuBarItem"/> with separate list of items.
		/// </summary>
		/// <param name="title">Title for the menu item.</param>
		/// <param name="children">The list of items in the current menu.</param>
		/// <param name="parent">The parent <see cref="MenuItem"/> of this if exist, otherwise is null.</param>
		public MenuBarItem (ustring title, List<MenuItem []> children, MenuItem parent = null)
		{
			Initialize (title, children, parent);
		}

		/// <summary>
		/// Initializes a new <see cref="MenuBarItem"/>.
		/// </summary>
		/// <param name="children">The items in the current menu.</param>
		public MenuBarItem (MenuItem [] children) : this ("", children) { }

		/// <summary>
		/// Initializes a new <see cref="MenuBarItem"/>.
		/// </summary>
		public MenuBarItem () : this (children: new MenuItem [] { }) { }

		void Initialize (ustring title, object children, MenuItem parent = null, bool isTopLevel = false)
		{
			if (!isTopLevel && children == null) {
				throw new ArgumentNullException (nameof (children), "The parameter cannot be null. Use an empty array instead.");
			}
			SetTitle (title ?? "");
			if (parent != null) {
				Parent = parent;
			}
			if (children is List<MenuItem []>) {
				MenuItem [] childrens = new MenuItem [] { };
				foreach (var item in (List<MenuItem []>)children) {
					for (int i = 0; i < item.Length; i++) {
						SetChildrensParent (item);
						Array.Resize (ref childrens, childrens.Length + 1);
						childrens [childrens.Length - 1] = item [i];
					}
				}
				Children = childrens;
			} else if (children is MenuItem []) {
				SetChildrensParent ((MenuItem [])children);
				Children = (MenuItem [])children;
			} else {
				Children = null;
			}
		}

		void SetChildrensParent (MenuItem [] childrens)
		{
			foreach (var child in childrens) {
				if (child != null && child.Parent == null) {
					child.Parent = this;
				}
			}
		}

		/// <summary>
		/// Check if the children parameter is a <see cref="MenuBarItem"/>.
		/// </summary>
		/// <param name="children"></param>
		/// <returns>Returns a <see cref="MenuBarItem"/> or null otherwise.</returns>
		public MenuBarItem SubMenu (MenuItem children)
		{
			return children as MenuBarItem;
		}

		/// <summary>
		/// Check if the <see cref="MenuItem"/> parameter is a child of this.
		/// </summary>
		/// <param name="menuItem"></param>
		/// <returns>Returns <c>true</c> if it is a child of this. <c>false</c> otherwise.</returns>
		public bool IsSubMenuOf (MenuItem menuItem)
		{
			foreach (var child in Children) {
				if (child == menuItem && child.Parent == menuItem.Parent) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Get the index of the <see cref="MenuItem"/> parameter.
		/// </summary>
		/// <param name="children"></param>
		/// <returns>Returns a value bigger than -1 if the <see cref="MenuItem"/> is a child of this.</returns>
		public int GetChildrenIndex (MenuItem children)
		{
			if (Children?.Length == 0) {
				return -1;
			}
			int i = 0;
			foreach (var child in Children) {
				if (child == children) {
					return i;
				}
				i++;
			}
			return -1;
		}

		void SetTitle (ustring title)
		{
			if (title == null)
				title = ustring.Empty;
			Title = title;
		}

		/// <summary>
		/// Gets or sets an array of <see cref="MenuItem"/> objects that are the children of this <see cref="MenuBarItem"/>
		/// </summary>
		/// <value>The children.</value>
		public MenuItem [] Children { get; set; }

		internal bool IsTopLevel { get => Parent == null && (Children == null || Children.Length == 0) && Action != null; }
	}

	class Menu : View {
		internal MenuBarItem barItems;
		internal MenuBar host;
		internal int current;
		internal View previousSubFocused;

		internal static Rect MakeFrame (int x, int y, MenuItem [] items, Menu parent = null)
		{
			if (items == null || items.Length == 0) {
				return new Rect ();
			}
			int minX = x;
			int minY = y;
			int maxW = (items.Max (z => z?.Width) ?? 0) + 2; // This 2 is frame border?
			int maxH = items.Length + 2; // This 2 is frame border?
			if (parent != null && x + maxW > Driver.Cols) {
				minX = Math.Max (parent.Frame.Right - parent.Frame.Width - maxW, 0);
			}
			if (y + maxH > Driver.Rows) {
				minY = Math.Max (Driver.Rows - maxH, 0);
			}
			return new Rect (minX, minY, maxW, maxH);
		}

		public Menu (MenuBar host, int x, int y, MenuBarItem barItems, Menu parent = null)
			: base (MakeFrame (x, y, barItems.Children, parent))
		{
			this.barItems = barItems;
			this.host = host;
			if (barItems.IsTopLevel) {
				// This is a standalone MenuItem on a MenuBar
				ColorScheme = host.ColorScheme;
				CanFocus = true;
			} else {

				current = -1;
				for (int i = 0; i < barItems.Children?.Length; i++) {
					if (barItems.Children [i] != null) {
						current = i;
						break;
					}
				}
				ColorScheme = host.ColorScheme;
				CanFocus = true;
				WantMousePositionReports = host.WantMousePositionReports;
			}

			if (Application.Current != null) {
				Application.Current.DrawContentComplete += Current_DrawContentComplete;
			}
			Application.RootMouseEvent += Application_RootMouseEvent;

			// Things this view knows how to do
			AddCommand (Command.LineUp, () => MoveUp ());
			AddCommand (Command.LineDown, () => MoveDown ());
			AddCommand (Command.Left, () => { this.host.PreviousMenu (true); return true; });
			AddCommand (Command.Right, () => {
				this.host.NextMenu (!this.barItems.IsTopLevel || (this.barItems.Children != null
					&& this.barItems.Children.Length > 0 && current > -1
					&& current < this.barItems.Children.Length && this.barItems.Children [current].IsFromSubMenu),
					this.barItems.Children != null && this.barItems.Children.Length > 0 && current > -1
					&& host.UseSubMenusSingleFrame && this.barItems.SubMenu (this.barItems.Children [current]) != null);

				return true;
			});
			AddCommand (Command.Cancel, () => { CloseAllMenus (); return true; });
			AddCommand (Command.Accept, () => { RunSelected (); return true; });

			// Default keybindings for this view
			AddKeyBinding (Key.CursorUp, Command.LineUp);
			AddKeyBinding (Key.CursorDown, Command.LineDown);
			AddKeyBinding (Key.CursorLeft, Command.Left);
			AddKeyBinding (Key.CursorRight, Command.Right);
			AddKeyBinding (Key.Esc, Command.Cancel);
			AddKeyBinding (Key.Enter, Command.Accept);
		}

		private void Application_RootMouseEvent (MouseEvent me)
		{
			var view = View.FindDeepestView (this, me.X, me.Y, out int rx, out int ry);
			if (view == this) {
				var nme = new MouseEvent () {
					X = rx,
					Y = ry,
					Flags = me.Flags,
					View = view
				};
				if (MouseEvent (nme) || me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1Released) {
					me.Handled = true;
				}
			}
		}

		internal Attribute DetermineColorSchemeFor (MenuItem item, int index)
		{
			if (item != null) {
				if (index == current) return ColorScheme.Focus;
				if (!item.IsEnabled ()) return ColorScheme.Disabled;
			}
			return GetNormalColor ();
		}

		public override void Redraw (Rect bounds)
		{
		}

		// Draws the Menu, within the Frame
		private void Current_DrawContentComplete (object sender, DrawEventArgs e)
		{
			if (barItems.Children == null) {
				return;
			}
			var savedClip = Driver.Clip;
			Application.Driver.Clip = Application.Top.Frame;

			Driver.SetAttribute (GetNormalColor ());
			DrawFrame (Bounds, padding: 0, fill: true);

			for (int i = Bounds.Y; i < barItems.Children.Length; i++) {
				if (i < 0)
					continue;
				var item = barItems.Children [i];
				Driver.SetAttribute (item == null ? GetNormalColor ()
					: i == current ? ColorScheme.Focus : GetNormalColor ());
				if (item == null) {
					Move (0, i + 1);
					Driver.AddRune (Driver.LeftTee);
				} else if (Frame.X + 1 < Driver.Cols)
					Move (1, i + 1);

				Driver.SetAttribute (DetermineColorSchemeFor (item, i));
				for (int p = Bounds.X; p < Frame.Width - 2; p++) { // This - 2 is for the border
					if (p < 0)
						continue;
					if (item == null)
						Driver.AddRune (Driver.HLine);
					else if (i == 0 && p == 0 && host.UseSubMenusSingleFrame && item.Parent.Parent != null)
						Driver.AddRune (Driver.LeftArrow);
					// This `- 3` is left border + right border + one row in from right
					else if (p == Frame.Width - 3 && barItems.SubMenu (barItems.Children [i]) != null)
						Driver.AddRune (Driver.RightArrow);
					else
						Driver.AddRune (' ');
				}

				if (item == null) {
					if (SuperView?.Frame.Right - Frame.X > Frame.Width - 1) {
						Move (Frame.Width - 1, i + 1);
						Driver.AddRune (Driver.RightTee);
					}
					continue;
				}

				ustring textToDraw;
				var nullCheckedChar = Driver.NullChecked;
				var checkChar = Driver.Selected;
				var uncheckedChar = Driver.UnSelected;

				if (item.CheckType.HasFlag (MenuItemCheckStyle.Checked)) {
					checkChar = Driver.Checked;
					uncheckedChar = Driver.UnChecked;
				}

				// Support Checked even though CheckType wasn't set
				if (item.CheckType == MenuItemCheckStyle.Checked && item.Checked == null) {
					textToDraw = ustring.Make (new Rune [] { nullCheckedChar, ' ' }) + item.Title;
				} else if (item.Checked == true) {
					textToDraw = ustring.Make (new Rune [] { checkChar, ' ' }) + item.Title;
				} else if (item.CheckType.HasFlag (MenuItemCheckStyle.Checked) || item.CheckType.HasFlag (MenuItemCheckStyle.Radio)) {
					textToDraw = ustring.Make (new Rune [] { uncheckedChar, ' ' }) + item.Title;
				} else {
					textToDraw = item.Title;
				}

				ViewToScreen (2, i + 1, out int vtsCol, out _, false);
				if (vtsCol < Driver.Cols) {
					Move (2, i + 1);
					if (!item.IsEnabled ()) {
						DrawHotString (textToDraw, ColorScheme.Disabled, ColorScheme.Disabled);
					} else if (i == 0 && host.UseSubMenusSingleFrame && item.Parent.Parent != null) {
						var tf = new TextFormatter () {
							Alignment = TextAlignment.Centered,
							HotKeySpecifier = MenuBar.HotKeySpecifier,
							Text = textToDraw
						};
						// The -3 is left/right border + one space (not sure what for)
						tf.Draw (ViewToScreen (new Rect (2, i + 1, Frame.Width - 3, 1)),
							i == current ? ColorScheme.Focus : GetNormalColor (),
							i == current ? ColorScheme.HotFocus : ColorScheme.HotNormal,
							SuperView == null ? default : SuperView.ViewToScreen (SuperView.Bounds));
					} else {
						DrawHotString (textToDraw,
							i == current ? ColorScheme.HotFocus : ColorScheme.HotNormal,
							i == current ? ColorScheme.Focus : GetNormalColor ());
					}

					// The help string
					var l = item.ShortcutTag.ConsoleWidth == 0 ? item.Help.ConsoleWidth : item.Help.ConsoleWidth + item.ShortcutTag.ConsoleWidth + 2;
					var col = Frame.Width - l - 2;
					ViewToScreen (col, i + 1, out vtsCol, out _, false);
					if (vtsCol < Driver.Cols) {
						Move (col, 1 + i);
						Driver.AddStr (item.Help);

						// The shortcut tag string
						if (!item.ShortcutTag.IsEmpty) {
							l = item.ShortcutTag.ConsoleWidth;
							Move (Frame.Width - l - 2, 1 + i);
							Driver.AddStr (item.ShortcutTag);
						}
					}
				}
			}
			Driver.Clip = savedClip;

			PositionCursor ();
		}

		public override void PositionCursor ()
		{
			if (host == null || host.IsMenuOpen)
				if (barItems.IsTopLevel) {
					host.PositionCursor ();
				} else
					Move (2, 1 + current);
			else
				host.PositionCursor ();
		}

		public void Run (Action action)
		{
			if (action == null)
				return;

			Application.UngrabMouse ();
			host.CloseAllMenus ();
			Application.Refresh ();

			Application.MainLoop.AddIdle (() => {
				action ();
				return false;
			});
		}

		public override bool OnLeave (View view)
		{
			return host.OnLeave (view);
		}

		public override bool OnKeyDown (KeyEvent keyEvent)
		{
			if (keyEvent.IsAlt) {
				host.CloseAllMenus ();
				return true;
			}

			return false;
		}

		public override bool ProcessHotKey (KeyEvent keyEvent)
		{
			// To ncurses simulate a AltMask key pressing Alt+Space because
			// it can�t detect an alone special key down was pressed.
			if (keyEvent.IsAlt && keyEvent.Key == Key.AltMask) {
				OnKeyDown (keyEvent);
				return true;
			}

			return false;
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			// TODO: rune-ify
			if (barItems.Children != null && Char.IsLetterOrDigit ((char)kb.KeyValue)) {
				var x = Char.ToUpper ((char)kb.KeyValue);
				var idx = -1;
				foreach (var item in barItems.Children) {
					idx++;
					if (item == null) continue;
					if (item.IsEnabled () && item.HotKey == x) {
						current = idx;
						RunSelected ();
						return true;
					}
				}
			}
			return host.ProcessHotKey (kb);
		}

		void RunSelected ()
		{
			if (barItems.IsTopLevel) {
				Run (barItems.Action);
			} else if (current > -1 && barItems.Children [current].Action != null) {
				Run (barItems.Children [current].Action);
			} else if (current == 0 && host.UseSubMenusSingleFrame
				&& barItems.Children [current].Parent.Parent != null) {

				host.PreviousMenu (barItems.Children [current].Parent.IsFromSubMenu, true);
			} else if (current > -1 && barItems.SubMenu (barItems.Children [current]) != null) {

				CheckSubMenu ();
			}
		}

		void CloseAllMenus ()
		{
			Application.UngrabMouse ();
			host.CloseAllMenus ();
		}

		bool MoveDown ()
		{
			if (barItems.IsTopLevel) {
				return true;
			}
			bool disabled;
			do {
				current++;
				if (current >= barItems.Children.Length) {
					current = 0;
				}
				if (this != host.openCurrentMenu && barItems.Children [current]?.IsFromSubMenu == true && host.selectedSub > -1) {
					host.PreviousMenu (true);
					host.SelectEnabledItem (barItems.Children, current, out current);
					host.openCurrentMenu = this;
				}
				var item = barItems.Children [current];
				if (item?.IsEnabled () != true) {
					disabled = true;
				} else {
					disabled = false;
				}
				if (!host.UseSubMenusSingleFrame && host.UseKeysUpDownAsKeysLeftRight && barItems.SubMenu (barItems.Children [current]) != null &&
					!disabled && host.IsMenuOpen) {
					if (!CheckSubMenu ())
						return false;
					break;
				}
				if (!host.IsMenuOpen) {
					host.OpenMenu (host.selected);
				}
			} while (barItems.Children [current] == null || disabled);
			SetNeedsDisplay ();
			SetParentSetNeedsDisplay ();
			if (!host.UseSubMenusSingleFrame)
				host.OnMenuOpened ();
			return true;
		}

		bool MoveUp ()
		{
			if (barItems.IsTopLevel || current == -1) {
				return true;
			}
			bool disabled;
			do {
				current--;
				if (host.UseKeysUpDownAsKeysLeftRight && !host.UseSubMenusSingleFrame) {
					if ((current == -1 || this != host.openCurrentMenu) && barItems.Children [current + 1].IsFromSubMenu && host.selectedSub > -1) {
						current++;
						host.PreviousMenu (true);
						if (current > 0) {
							current--;
							host.openCurrentMenu = this;
						}
						break;
					}
				}
				if (current < 0)
					current = barItems.Children.Length - 1;
				if (!host.SelectEnabledItem (barItems.Children, current, out current, false)) {
					current = 0;
					if (!host.SelectEnabledItem (barItems.Children, current, out current) && !host.CloseMenu (false)) {
						return false;
					}
					break;
				}
				var item = barItems.Children [current];
				if (item?.IsEnabled () != true) {
					disabled = true;
				} else {
					disabled = false;
				}
				if (!host.UseSubMenusSingleFrame && host.UseKeysUpDownAsKeysLeftRight && barItems.SubMenu (barItems.Children [current]) != null &&
					!disabled && host.IsMenuOpen) {
					if (!CheckSubMenu ())
						return false;
					break;
				}
			} while (barItems.Children [current] == null || disabled);
			SetNeedsDisplay ();
			SetParentSetNeedsDisplay ();
			if (!host.UseSubMenusSingleFrame)
				host.OnMenuOpened ();
			return true;
		}

		private void SetParentSetNeedsDisplay ()
		{
			if (host.openSubMenu != null) {
				foreach (var menu in host.openSubMenu) {
					menu.SetNeedsDisplay ();
				}
			}

			host?.openMenu?.SetNeedsDisplay ();
			host.SetNeedsDisplay ();
		}

		public override bool MouseEvent (MouseEvent me)
		{
			if (!host.handled && !host.HandleGrabView (me, this)) {
				return false;
			}
			host.handled = false;
			bool disabled;
			if (me.Flags == MouseFlags.Button1Clicked) {
				disabled = false;
				if (me.Y < 1)
					return true;
				var meY = me.Y - 1;
				if (meY >= barItems.Children.Length)
					return true;
				var item = barItems.Children [meY];
				if (item == null || !item.IsEnabled ()) disabled = true;
				if (disabled) return true;
				current = meY;
				if (item != null && !disabled)
					RunSelected ();
				return true;
			} else if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked ||
				me.Flags == MouseFlags.Button1TripleClicked || me.Flags == MouseFlags.ReportMousePosition ||
				me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {

				disabled = false;
				if (me.Y < 1 || me.Y - 1 >= barItems.Children.Length) {
					return true;
				}
				var item = barItems.Children [me.Y - 1];
				if (item == null) return true;
				if (item == null || !item.IsEnabled ()) disabled = true;
				if (item != null && !disabled)
					current = me.Y - 1;
				if (host.UseSubMenusSingleFrame || !CheckSubMenu ()) {
					SetNeedsDisplay ();
					SetParentSetNeedsDisplay ();
					return true;
				}
				host.OnMenuOpened ();
				return true;
			}
			return false;
		}

		internal bool CheckSubMenu ()
		{
			if (current == -1 || barItems.Children [current] == null) {
				return true;
			}
			var subMenu = barItems.SubMenu (barItems.Children [current]);
			if (subMenu != null) {
				int pos = -1;
				if (host.openSubMenu != null) {
					pos = host.openSubMenu.FindIndex (o => o?.barItems == subMenu);
				}
				if (pos == -1 && this != host.openCurrentMenu && subMenu.Children != host.openCurrentMenu.barItems.Children
					&& !host.CloseMenu (false, true)) {
					return false;
				}
				host.Activate (host.selected, pos, subMenu);
			} else if (host.openSubMenu?.Count == 0 || host.openSubMenu?.Last ().barItems.IsSubMenuOf (barItems.Children [current]) == false) {
				return host.CloseMenu (false, true);
			} else {
				SetNeedsDisplay ();
				SetParentSetNeedsDisplay ();
			}
			return true;
		}

		int GetSubMenuIndex (MenuBarItem subMenu)
		{
			int pos = -1;
			if (this != null && Subviews.Count > 0) {
				Menu v = null;
				foreach (var menu in Subviews) {
					if (((Menu)menu).barItems == subMenu)
						v = (Menu)menu;
				}
				if (v != null)
					pos = Subviews.IndexOf (v);
			}

			return pos;
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}

		protected override void Dispose (bool disposing)
		{
			if (Application.Current != null) {
				Application.Current.DrawContentComplete -= Current_DrawContentComplete;
			}
			Application.RootMouseEvent -= Application_RootMouseEvent;
			base.Dispose (disposing);
		}
	}

	/// <summary>
	///	<para>
	/// Provides a menu bar that spans the top of a <see cref="Toplevel"/> View with drop-down and cascading menus. 
	///	</para>
	/// <para>
	/// By default, any sub-sub-menus (sub-menus of the <see cref="MenuItem"/>s added to <see cref="MenuBarItem"/>s) 
	/// are displayed in a cascading manner, where each sub-sub-menu pops out of the sub-menu frame
	/// (either to the right or left, depending on where the sub-menu is relative to the edge of the screen). By setting
	/// <see cref="UseSubMenusSingleFrame"/> to <see langword="true"/>, this behavior can be changed such that all sub-sub-menus are
	/// drawn within a single frame below the MenuBar.
	/// </para>
	/// </summary>
	/// <remarks>
	///	<para>
	///	The <see cref="MenuBar"/> appears on the first row of the parent <see cref="Toplevel"/> View and uses the full width.
	///	</para>
	///	<para>
	///	The <see cref="MenuBar"/> provides global hotkeys for the application. See <see cref="MenuItem.HotKey"/>.
	///	</para>
	///	<para>
	///	See also: <see cref="ContextMenu"/>
	///	</para>
	/// </remarks>
	public class MenuBar : View {
		internal int selected;
		internal int selectedSub;

		/// <summary>
		/// Gets or sets the array of <see cref="MenuBarItem"/>s for the menu. Only set this after the <see cref="MenuBar"/> is visible.
		/// </summary>
		/// <value>The menu array.</value>
		public MenuBarItem [] Menus { get; set; }

		private bool useKeysUpDownAsKeysLeftRight = false;

		/// <summary>
		/// Used for change the navigation key style.
		/// </summary>
		public bool UseKeysUpDownAsKeysLeftRight {
			get => useKeysUpDownAsKeysLeftRight;
			set {
				useKeysUpDownAsKeysLeftRight = value;
				if (value && UseSubMenusSingleFrame) {
					UseSubMenusSingleFrame = false;
					SetNeedsDisplay ();
				}
			}
		}

		static ustring shortcutDelimiter = "+";
		/// <summary>
		/// Sets or gets the shortcut delimiter separator. The default is "+".
		/// </summary>
		public static ustring ShortcutDelimiter {
			get => shortcutDelimiter;
			set {
				if (shortcutDelimiter != value) {
					shortcutDelimiter = value == ustring.Empty ? " " : value;
				}
			}
		}

		/// <summary>
		/// The specifier character for the hotkey to all menus.
		/// </summary>
		new public static Rune HotKeySpecifier => '_';

		private bool useSubMenusSingleFrame;

		/// <summary>
		/// Gets or sets if the sub-menus must be displayed in a single or multiple frames.
		/// <para>
		/// By default any sub-sub-menus (sub-menus of the main <see cref="MenuItem"/>s) are displayed in a cascading manner, 
		/// where each sub-sub-menu pops out of the sub-menu frame
		/// (either to the right or left, depending on where the sub-menu is relative to the edge of the screen). By setting
		/// <see cref="UseSubMenusSingleFrame"/> to <see langword="true"/>, this behavior can be changed such that all sub-sub-menus are
		/// drawn within a single frame below the MenuBar.
		/// </para>		
		/// </summary>
		public bool UseSubMenusSingleFrame {
			get => useSubMenusSingleFrame;
			set {
				useSubMenusSingleFrame = value;
				if (value && UseKeysUpDownAsKeysLeftRight) {
					useKeysUpDownAsKeysLeftRight = false;
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// The <see cref="Gui.Key"/> used to activate the menu bar by keyboard.
		/// </summary>
		public Key Key { get; set; } = Key.F9;

		/// <summary>
		/// Initializes a new instance of the <see cref="MenuBar"/>.
		/// </summary>
		public MenuBar () : this (new MenuBarItem [] { }) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="MenuBar"/> class with the specified set of toplevel menu items.
		/// </summary>
		/// <param name="menus">Individual menu items; a null item will result in a separator being drawn.</param>
		public MenuBar (MenuBarItem [] menus) : base ()
		{
			X = 0;
			Y = 0;
			Width = Dim.Fill ();
			Height = 1;
			Menus = menus;
			//CanFocus = true;
			selected = -1;
			selectedSub = -1;
			ColorScheme = Colors.Menu;
			WantMousePositionReports = true;
			IsMenuOpen = false;

			Added += MenuBar_Added;

			// Things this view knows how to do
			AddCommand (Command.Left, () => { MoveLeft (); return true; });
			AddCommand (Command.Right, () => { MoveRight (); return true; });
			AddCommand (Command.Cancel, () => { CloseMenuBar (); return true; });
			AddCommand (Command.Accept, () => { ProcessMenu (selected, Menus [selected]); return true; });

			// Default keybindings for this view
			AddKeyBinding (Key.CursorLeft, Command.Left);
			AddKeyBinding (Key.CursorRight, Command.Right);
			AddKeyBinding (Key.Esc, Command.Cancel);
			AddKeyBinding (Key.C | Key.CtrlMask, Command.Cancel);
			AddKeyBinding (Key.CursorDown, Command.Accept);
			AddKeyBinding (Key.Enter, Command.Accept);
		}

		bool _initialCanFocus;

		private void MenuBar_Added (object sender, SuperViewChangedEventArgs e)
		{
			_initialCanFocus = CanFocus;
			Added -= MenuBar_Added;
		}

		bool openedByAltKey;

		bool isCleaning;

		///<inheritdoc/>
		public override bool OnLeave (View view)
		{
			if ((!(view is MenuBar) && !(view is Menu) || !(view is MenuBar) && !(view is Menu) && openMenu != null) && !isCleaning && !reopen) {
				CleanUp ();
			}
			return base.OnLeave (view);
		}

		///<inheritdoc/>
		public override bool OnKeyDown (KeyEvent keyEvent)
		{
			if (keyEvent.IsAlt || (keyEvent.IsCtrl && keyEvent.Key == (Key.CtrlMask | Key.Space))) {
				openedByAltKey = true;
				SetNeedsDisplay ();
				openedByHotKey = false;
			}
			return false;
		}

		///<inheritdoc/>
		public override bool OnKeyUp (KeyEvent keyEvent)
		{
			if (keyEvent.IsAlt || keyEvent.Key == Key.AltMask || (keyEvent.IsCtrl && keyEvent.Key == (Key.CtrlMask | Key.Space))) {
				// User pressed Alt - this may be a precursor to a menu accelerator (e.g. Alt-F)
				if (openedByAltKey && !IsMenuOpen && openMenu == null && (((uint)keyEvent.Key & (uint)Key.CharMask) == 0
					|| ((uint)keyEvent.Key & (uint)Key.CharMask) == (uint)Key.Space)) {
					// There's no open menu, the first menu item should be highlight.
					// The right way to do this is to SetFocus(MenuBar), but for some reason
					// that faults.

					var mbar = GetMouseGrabViewInstance (this);
					if (mbar != null) {
						mbar.CleanUp ();
					}

					//Activate (0);
					//StartMenu ();
					IsMenuOpen = true;
					selected = 0;
					CanFocus = true;
					lastFocused = SuperView == null ? Application.Current.MostFocused : SuperView.MostFocused;
					SetFocus ();
					SetNeedsDisplay ();
					Application.GrabMouse (this);
				} else if (!openedByHotKey) {
					// There's an open menu. If this Alt key-up is a pre-cursor to an accelerator
					// we don't want to close the menu because it'll flash.
					// How to deal with that?

					CleanUp ();
				}

				return true;
			}
			return false;
		}

		internal void CleanUp ()
		{
			isCleaning = true;
			if (openMenu != null) {
				CloseAllMenus ();
			}
			openedByAltKey = false;
			IsMenuOpen = false;
			selected = -1;
			CanFocus = _initialCanFocus;
			if (lastFocused != null) {
				lastFocused.SetFocus ();
			}
			SetNeedsDisplay ();
			Application.UngrabMouse ();
			isCleaning = false;
		}

		// The column where the MenuBar starts
		static int xOrigin = 0;
		// Spaces before the Title
		static int leftPadding = 1;
		// Spaces after the Title
		static int rightPadding = 1;
		// Spaces after the submenu Title, before Help
		static int parensAroundHelp = 3;
		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Move (0, 0);
			Driver.SetAttribute (GetNormalColor ());
			for (int i = 0; i < Frame.Width; i++)
				Driver.AddRune (' ');

			Move (1, 0);
			int pos = 0;

			for (int i = 0; i < Menus.Length; i++) {
				var menu = Menus [i];
				Move (pos, 0);
				Attribute hotColor, normalColor;
				if (i == selected && IsMenuOpen) {
					hotColor = i == selected ? ColorScheme.HotFocus : ColorScheme.HotNormal;
					normalColor = i == selected ? ColorScheme.Focus : GetNormalColor ();
				} else {
					hotColor = ColorScheme.HotNormal;
					normalColor = GetNormalColor ();
				}
				// Note Help on MenuBar is drawn with parens around it
				DrawHotString (menu.Help.IsEmpty ? $" {menu.Title} " : $" {menu.Title} ({menu.Help}) ", hotColor, normalColor);
				pos += leftPadding + menu.TitleLength + (menu.Help.ConsoleWidth > 0 ? leftPadding + menu.Help.ConsoleWidth + parensAroundHelp : 0) + rightPadding;
			}
			PositionCursor ();
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			if (selected == -1 && HasFocus && Menus.Length > 0) {
				selected = 0;
			}
			int pos = 0;
			for (int i = 0; i < Menus.Length; i++) {
				if (i == selected) {
					pos++;
					Move (pos + 1, 0);
					return;
				} else {
					pos += leftPadding + Menus [i].TitleLength + (Menus [i].Help.ConsoleWidth > 0 ? Menus [i].Help.ConsoleWidth + parensAroundHelp : 0) + rightPadding;
				}
			}
		}

		void Selected (MenuItem item)
		{
			var action = item.Action;

			if (action == null)
				return;

			Application.UngrabMouse ();
			CloseAllMenus ();
			Application.Refresh ();

			Application.MainLoop.AddIdle (() => {
				action ();
				return false;
			});
		}

		/// <summary>
		/// Raised as a menu is opening.
		/// </summary>
		public event EventHandler<MenuOpeningEventArgs> MenuOpening;

		/// <summary>
		/// Raised when a menu is opened.
		/// </summary>
		public event EventHandler<MenuOpenedEventArgs> MenuOpened;

		/// <summary>
		/// Raised when a menu is closing passing <see cref="MenuClosingEventArgs"/>.
		/// </summary>
		public event EventHandler<MenuClosingEventArgs> MenuClosing;

		/// <summary>
		/// Raised when all the menu is closed.
		/// </summary>
		public event EventHandler MenuAllClosed;

		internal Menu openMenu;
		Menu ocm;
		internal Menu openCurrentMenu {
			get => ocm;
			set {
				if (ocm != value) {
					ocm = value;
					if (ocm.current > -1) {
						OnMenuOpened ();
					}
				}
			}
		}
		internal List<Menu> openSubMenu;
		View previousFocused;
		internal bool isMenuOpening;
		internal bool isMenuClosing;

		/// <summary>
		/// <see langword="true"/> if the menu is open; otherwise <see langword="true"/>.
		/// </summary>
		public bool IsMenuOpen { get; protected set; }

		/// <summary>
		/// Virtual method that will invoke the <see cref="MenuOpening"/> event if it's defined.
		/// </summary>
		/// <param name="currentMenu">The current menu to be replaced.</param>
		/// <returns>Returns the <see cref="MenuOpeningEventArgs"/></returns>
		public virtual MenuOpeningEventArgs OnMenuOpening (MenuBarItem currentMenu)
		{
			var ev = new MenuOpeningEventArgs (currentMenu);
			MenuOpening?.Invoke (this, ev);
			return ev;
		}

		/// <summary>
		/// Virtual method that will invoke the <see cref="MenuOpened"/> event if it's defined.
		/// </summary>
		public virtual void OnMenuOpened ()
		{
			MenuItem mi = null;
			MenuBarItem parent;

			if (openCurrentMenu.barItems.Children != null && openCurrentMenu.barItems.Children.Length > 0
				&& openCurrentMenu?.current > -1) {
				parent = openCurrentMenu.barItems;
				mi = parent.Children [openCurrentMenu.current];
			} else if (openCurrentMenu.barItems.IsTopLevel) {
				parent = null;
				mi = openCurrentMenu.barItems;
			} else {
				parent = openMenu.barItems;
				mi = parent.Children [openMenu.current];
			}
			MenuOpened?.Invoke (this, new MenuOpenedEventArgs (parent, mi));
		}

		/// <summary>
		/// Virtual method that will invoke the <see cref="MenuClosing"/>.
		/// </summary>
		/// <param name="currentMenu">The current menu to be closed.</param>
		/// <param name="reopen">Whether the current menu will be reopen.</param>
		/// <param name="isSubMenu">Whether is a sub-menu or not.</param>
		public virtual MenuClosingEventArgs OnMenuClosing (MenuBarItem currentMenu, bool reopen, bool isSubMenu)
		{
			var ev = new MenuClosingEventArgs (currentMenu, reopen, isSubMenu);
			MenuClosing?.Invoke (this, ev);
			return ev;
		}

		/// <summary>
		/// Virtual method that will invoke the <see cref="MenuAllClosed"/>.
		/// </summary>
		public virtual void OnMenuAllClosed ()
		{
			MenuAllClosed?.Invoke (this, EventArgs.Empty);
		}

		View lastFocused;

		/// <summary>
		/// Gets the view that was last focused before opening the menu.
		/// </summary>
		public View LastFocused { get; private set; }

		internal void OpenMenu (int index, int sIndex = -1, MenuBarItem subMenu = null)
		{
			isMenuOpening = true;
			var newMenu = OnMenuOpening (Menus [index]);
			if (newMenu.Cancel) {
				isMenuOpening = false;
				return;
			}
			if (newMenu.NewMenuBarItem != null) {
				Menus [index] = newMenu.NewMenuBarItem;
			}
			int pos = 0;
			switch (subMenu) {
			case null:
				// Open a submenu below a MenuBar
				lastFocused ??= (SuperView == null ? Application.Current.MostFocused : SuperView.MostFocused);
				if (openSubMenu != null && !CloseMenu (false, true))
					return;
				if (openMenu != null) {
					Application.Top.Remove (openMenu);
					openMenu.Dispose ();
					openMenu = null;
				}

				// This positions the submenu horizontally aligned with the first character of the
				// menu it belongs to's text
				for (int i = 0; i < index; i++)
					pos += Menus [i].TitleLength + (Menus [i].Help.ConsoleWidth > 0 ? Menus [i].Help.ConsoleWidth + 2 : 0) + leftPadding + rightPadding;
				var superView = SuperView == null ? Application.Top : SuperView;
				Point locationOffset;
				if (superView.Border != null && superView.Border.BorderStyle != BorderStyle.None) {
					locationOffset = new Point (superView.Frame.X + 1, superView.Frame.Y + 1);
				} else {
					locationOffset = new Point (superView.Frame.X, superView.Frame.Y);
				}
				openMenu = new Menu (this, Frame.X + pos + locationOffset.X, Frame.Y + 1 + locationOffset.Y, Menus [index]);
				openCurrentMenu = openMenu;
				openCurrentMenu.previousSubFocused = openMenu;

				Application.Top.Add (openMenu);
				openMenu.SetFocus ();
				break;
			default:
				// Opens a submenu next to another submenu (openSubMenu)
				if (openSubMenu == null)
					openSubMenu = new List<Menu> ();
				if (sIndex > -1) {
					RemoveSubMenu (sIndex);
				} else {
					var last = openSubMenu.Count > 0 ? openSubMenu.Last () : openMenu;
					if (!UseSubMenusSingleFrame) {
						openCurrentMenu = new Menu (this, last.Frame.Left + last.Frame.Width, last.Frame.Top + 1 + last.current, subMenu, last);
					} else {
						var first = openSubMenu.Count > 0 ? openSubMenu.First () : openMenu;
						var mbi = new MenuItem [2 + subMenu.Children.Length];
						mbi [0] = new MenuItem () { Title = subMenu.Title, Parent = subMenu };
						mbi [1] = null;
						for (int j = 0; j < subMenu.Children.Length; j++) {
							mbi [j + 2] = subMenu.Children [j];
						}
						var newSubMenu = new MenuBarItem (mbi);
						ViewToScreen (first.Frame.Left, first.Frame.Top, out int rx, out int ry);
						openCurrentMenu = new Menu (this, rx, ry, newSubMenu);
						last.Visible = false;
						Application.GrabMouse (openCurrentMenu);
					}
					openCurrentMenu.previousSubFocused = last.previousSubFocused;
					openSubMenu.Add (openCurrentMenu);
					Application.Top.Add (openCurrentMenu);
				}
				selectedSub = openSubMenu.Count - 1;
				if (selectedSub > -1 && SelectEnabledItem (openCurrentMenu.barItems.Children, openCurrentMenu.current, out openCurrentMenu.current)) {
					openCurrentMenu.SetFocus ();
				}
				break;
			}
			isMenuOpening = false;
			IsMenuOpen = true;
		}

		/// <summary>
		/// Opens the Menu programatically, as though the F9 key were pressed.
		/// </summary>
		public void OpenMenu ()
		{
			var mbar = GetMouseGrabViewInstance (this);
			if (mbar != null) {
				mbar.CleanUp ();
			}

			if (openMenu != null)
				return;
			selected = 0;
			SetNeedsDisplay ();

			previousFocused = SuperView == null ? Application.Current.Focused : SuperView.Focused;
			OpenMenu (selected);
			if (!SelectEnabledItem (openCurrentMenu.barItems.Children, openCurrentMenu.current, out openCurrentMenu.current) && !CloseMenu (false)) {
				return;
			}
			if (!openCurrentMenu.CheckSubMenu ())
				return;
			Application.GrabMouse (this);
		}

		// Activates the menu, handles either first focus, or activating an entry when it was already active
		// For mouse events.
		internal void Activate (int idx, int sIdx = -1, MenuBarItem subMenu = null)
		{
			selected = idx;
			selectedSub = sIdx;
			if (openMenu == null)
				previousFocused = SuperView == null ? Application.Current.Focused : SuperView.Focused;

			OpenMenu (idx, sIdx, subMenu);
			SetNeedsDisplay ();
		}

		internal bool SelectEnabledItem (IEnumerable<MenuItem> chldren, int current, out int newCurrent, bool forward = true)
		{
			if (chldren == null) {
				newCurrent = -1;
				return true;
			}

			IEnumerable<MenuItem> childrens;
			if (forward) {
				childrens = chldren;
			} else {
				childrens = chldren.Reverse ();
			}
			int count;
			if (forward) {
				count = -1;
			} else {
				count = childrens.Count ();
			}
			foreach (var child in childrens) {
				if (forward) {
					if (++count < current) {
						continue;
					}
				} else {
					if (--count > current) {
						continue;
					}
				}
				if (child == null || !child.IsEnabled ()) {
					if (forward) {
						current++;
					} else {
						current--;
					}
				} else {
					newCurrent = current;
					return true;
				}
			}
			newCurrent = -1;
			return false;
		}

		/// <summary>
		/// Closes the Menu programmatically if open and not canceled (as though F9 were pressed).
		/// </summary>
		public bool CloseMenu (bool ignoreUseSubMenusSingleFrame = false)
		{
			return CloseMenu (false, false, ignoreUseSubMenusSingleFrame);
		}

		bool reopen;

		internal bool CloseMenu (bool reopen = false, bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
		{
			var mbi = isSubMenu ? openCurrentMenu.barItems : openMenu?.barItems;
			if (UseSubMenusSingleFrame && mbi != null &&
				!ignoreUseSubMenusSingleFrame && mbi.Parent != null) {
				return false;
			}
			isMenuClosing = true;
			this.reopen = reopen;
			var args = OnMenuClosing (mbi, reopen, isSubMenu);
			if (args.Cancel) {
				isMenuClosing = false;
				if (args.CurrentMenu.Parent != null)
					openMenu.current = ((MenuBarItem)args.CurrentMenu.Parent).Children.IndexOf (args.CurrentMenu);
				return false;
			}
			switch (isSubMenu) {
			case false:
				if (openMenu != null) {
					Application.Top.Remove (openMenu);
				}
				SetNeedsDisplay ();
				if (previousFocused != null && previousFocused is Menu && openMenu != null && previousFocused.ToString () != openCurrentMenu.ToString ())
					previousFocused.SetFocus ();
				openMenu?.Dispose ();
				openMenu = null;
				if (lastFocused is Menu || lastFocused is MenuBar) {
					lastFocused = null;
				}
				LastFocused = lastFocused;
				lastFocused = null;
				if (LastFocused != null && LastFocused.CanFocus) {
					if (!reopen) {
						selected = -1;
					}
					LastFocused.SetFocus ();
				} else if (openSubMenu == null || openSubMenu.Count == 0) {
					CloseAllMenus ();
				} else {
					SetFocus ();
					PositionCursor ();
				}
				IsMenuOpen = false;
				break;

			case true:
				selectedSub = -1;
				SetNeedsDisplay ();
				RemoveAllOpensSubMenus ();
				openCurrentMenu.previousSubFocused.SetFocus ();
				openSubMenu = null;
				IsMenuOpen = true;
				break;
			}
			this.reopen = false;
			isMenuClosing = false;
			return true;
		}

		void RemoveSubMenu (int index, bool ignoreUseSubMenusSingleFrame = false)
		{
			if (openSubMenu == null || (UseSubMenusSingleFrame
				&& !ignoreUseSubMenusSingleFrame && openSubMenu.Count == 0))

				return;
			for (int i = openSubMenu.Count - 1; i > index; i--) {
				isMenuClosing = true;
				Menu menu;
				if (openSubMenu.Count - 1 > 0)
					menu = openSubMenu [i - 1];
				else
					menu = openMenu;
				if (!menu.Visible)
					menu.Visible = true;
				openCurrentMenu = menu;
				openCurrentMenu.SetFocus ();
				if (openSubMenu != null) {
					menu = openSubMenu [i];
					Application.Top.Remove (menu);
					openSubMenu.Remove (menu);
					menu.Dispose ();
				}
				RemoveSubMenu (i, ignoreUseSubMenusSingleFrame);
			}
			if (openSubMenu.Count > 0)
				openCurrentMenu = openSubMenu.Last ();

			isMenuClosing = false;
		}

		internal void RemoveAllOpensSubMenus ()
		{
			if (openSubMenu != null) {
				foreach (var item in openSubMenu) {
					Application.Top.Remove (item);
					item.Dispose ();
				}
			}
		}

		internal void CloseAllMenus ()
		{
			if (!isMenuOpening && !isMenuClosing) {
				if (openSubMenu != null && !CloseMenu (false, true))
					return;
				if (!CloseMenu (false))
					return;
				if (LastFocused != null && LastFocused != this)
					selected = -1;
				Application.UngrabMouse ();
			}
			IsMenuOpen = false;
			openedByHotKey = false;
			openedByAltKey = false;
			OnMenuAllClosed ();
		}

		View FindDeepestMenu (View view, ref int count)
		{
			count = count > 0 ? count : 0;
			foreach (var menu in view.Subviews) {
				if (menu is Menu) {
					count++;
					return FindDeepestMenu ((Menu)menu, ref count);
				}
			}
			return view;
		}

		internal void PreviousMenu (bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
		{
			switch (isSubMenu) {
			case false:
				if (selected <= 0)
					selected = Menus.Length - 1;
				else
					selected--;

				if (selected > -1 && !CloseMenu (true, false, ignoreUseSubMenusSingleFrame))
					return;
				OpenMenu (selected);
				if (!SelectEnabledItem (openCurrentMenu.barItems.Children, openCurrentMenu.current, out openCurrentMenu.current, false)) {
					openCurrentMenu.current = 0;
				}
				break;
			case true:
				if (selectedSub > -1) {
					selectedSub--;
					RemoveSubMenu (selectedSub, ignoreUseSubMenusSingleFrame);
					SetNeedsDisplay ();
				} else
					PreviousMenu ();

				break;
			}
		}

		internal void NextMenu (bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
		{
			switch (isSubMenu) {
			case false:
				if (selected == -1)
					selected = 0;
				else if (selected + 1 == Menus.Length)
					selected = 0;
				else
					selected++;

				if (selected > -1 && !CloseMenu (true, ignoreUseSubMenusSingleFrame))
					return;
				OpenMenu (selected);
				SelectEnabledItem (openCurrentMenu.barItems.Children, openCurrentMenu.current, out openCurrentMenu.current);
				break;
			case true:
				if (UseKeysUpDownAsKeysLeftRight) {
					if (CloseMenu (false, true, ignoreUseSubMenusSingleFrame)) {
						NextMenu (false, ignoreUseSubMenusSingleFrame);
					}
				} else {
					var subMenu = openCurrentMenu.current > -1 && openCurrentMenu.barItems.Children.Length > 0
						? openCurrentMenu.barItems.SubMenu (openCurrentMenu.barItems.Children [openCurrentMenu.current])
						: null;
					if ((selectedSub == -1 || openSubMenu == null || openSubMenu?.Count - 1 == selectedSub) && subMenu == null) {
						if (openSubMenu != null && !CloseMenu (false, true))
							return;
						NextMenu (false, ignoreUseSubMenusSingleFrame);
					} else if (subMenu != null || (openCurrentMenu.current > -1
						&& !openCurrentMenu.barItems.Children [openCurrentMenu.current].IsFromSubMenu)) {
						selectedSub++;
						openCurrentMenu.CheckSubMenu ();
					} else {
						if (CloseMenu (false, true, ignoreUseSubMenusSingleFrame)) {
							NextMenu (false, ignoreUseSubMenusSingleFrame);
						}
						return;
					}

					SetNeedsDisplay ();
					if (UseKeysUpDownAsKeysLeftRight)
						openCurrentMenu.CheckSubMenu ();
				}
				break;
			}
		}

		bool openedByHotKey;
		internal bool FindAndOpenMenuByHotkey (KeyEvent kb)
		{
			//int pos = 0;
			var c = ((uint)kb.Key & (uint)Key.CharMask);
			for (int i = 0; i < Menus.Length; i++) {
				// TODO: this code is duplicated, hotkey should be part of the MenuBarItem
				var mi = Menus [i];
				int p = mi.Title.IndexOf (MenuBar.HotKeySpecifier);
				if (p != -1 && p + 1 < mi.Title.RuneCount) {
					if (Char.ToUpperInvariant ((char)mi.Title [p + 1]) == c) {
						ProcessMenu (i, mi);
						return true;
					}
				}
			}
			return false;
		}

		internal bool FindAndOpenMenuByShortcut (KeyEvent kb, MenuItem [] children = null)
		{
			if (children == null) {
				children = Menus;
			}

			var key = kb.KeyValue;
			var keys = ShortcutHelper.GetModifiersKey (kb);
			key |= (int)keys;
			for (int i = 0; i < children.Length; i++) {
				var mi = children [i];
				if (mi == null) {
					continue;
				}
				if ((!(mi is MenuBarItem mbiTopLevel) || mbiTopLevel.IsTopLevel) && mi.Shortcut != Key.Null && mi.Shortcut == (Key)key) {
					var action = mi.Action;
					if (action != null) {
						Application.MainLoop.AddIdle (() => {
							action ();
							return false;
						});
					}
					return true;
				}
				if (mi is MenuBarItem menuBarItem && !menuBarItem.IsTopLevel && FindAndOpenMenuByShortcut (kb, menuBarItem.Children)) {
					return true;
				}
			}

			return false;
		}

		private void ProcessMenu (int i, MenuBarItem mi)
		{
			if (selected < 0 && IsMenuOpen) {
				return;
			}

			if (mi.IsTopLevel) {
				ViewToScreen (i, 0, out int rx, out int ry);
				var menu = new Menu (this, rx, ry, mi);
				menu.Run (mi.Action);
				menu.Dispose ();
			} else {
				openedByHotKey = true;
				Application.GrabMouse (this);
				selected = i;
				OpenMenu (i);
				if (!SelectEnabledItem (openCurrentMenu.barItems.Children, openCurrentMenu.current, out openCurrentMenu.current) && !CloseMenu (false)) {
					return;
				}
				if (!openCurrentMenu.CheckSubMenu ())
					return;
			}
			SetNeedsDisplay ();
		}

		///<inheritdoc/>
		public override bool ProcessHotKey (KeyEvent kb)
		{
			if (kb.Key == Key) {
				if (!IsMenuOpen)
					OpenMenu ();
				else
					CloseAllMenus ();
				return true;
			}

			// To ncurses simulate a AltMask key pressing Alt+Space because
			// it can't detect an alone special key down was pressed.
			if (kb.IsAlt && kb.Key == Key.AltMask && openMenu == null) {
				OnKeyDown (kb);
				OnKeyUp (kb);
				return true;
			} else if (kb.IsAlt && !kb.IsCtrl && !kb.IsShift) {
				if (FindAndOpenMenuByHotkey (kb)) return true;
			}
			//var kc = kb.KeyValue;

			return base.ProcessHotKey (kb);
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			if (InvokeKeybindings (kb) == true)
				return true;

			var key = kb.KeyValue;
			if ((key >= 'a' && key <= 'z') || (key >= 'A' && key <= 'Z') || (key >= '0' && key <= '9')) {
				char c = Char.ToUpper ((char)key);

				if (selected == -1 || Menus [selected].IsTopLevel)
					return false;

				foreach (var mi in Menus [selected].Children) {
					if (mi == null)
						continue;
					int p = mi.Title.IndexOf (MenuBar.HotKeySpecifier);
					if (p != -1 && p + 1 < mi.Title.RuneCount) {
						if (mi.Title [p + 1] == c) {
							Selected (mi);
							return true;
						}
					}
				}
			}

			return false;
		}

		void CloseMenuBar ()
		{
			if (!CloseMenu (false))
				return;
			if (openedByAltKey) {
				openedByAltKey = false;
				LastFocused?.SetFocus ();
			}
			SetNeedsDisplay ();
		}

		void MoveRight ()
		{
			selected = (selected + 1) % Menus.Length;
			OpenMenu (selected);
			SetNeedsDisplay ();
		}

		void MoveLeft ()
		{
			selected--;
			if (selected < 0)
				selected = Menus.Length - 1;
			OpenMenu (selected);
			SetNeedsDisplay ();
		}

		///<inheritdoc/>
		public override bool ProcessColdKey (KeyEvent kb)
		{
			return FindAndOpenMenuByShortcut (kb);
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!handled && !HandleGrabView (me, this)) {
				return false;
			}
			handled = false;

			if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked || me.Flags == MouseFlags.Button1TripleClicked || me.Flags == MouseFlags.Button1Clicked ||
				(me.Flags == MouseFlags.ReportMousePosition && selected > -1) ||
				(me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) && selected > -1)) {
				int pos = xOrigin;
				int cx = me.X;
				for (int i = 0; i < Menus.Length; i++) {
					if (cx >= pos && cx < pos + leftPadding + Menus [i].TitleLength + Menus [i].Help.ConsoleWidth + rightPadding) {
						if (me.Flags == MouseFlags.Button1Clicked) {
							if (Menus [i].IsTopLevel) {
								ViewToScreen (i, 0, out int rx, out int ry);
								var menu = new Menu (this, rx, ry, Menus [i]);
								menu.Run (Menus [i].Action);
								menu.Dispose ();
							} else if (!IsMenuOpen) {
								Activate (i);
							}
						} else if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked || me.Flags == MouseFlags.Button1TripleClicked) {
							if (IsMenuOpen && !Menus [i].IsTopLevel) {
								CloseAllMenus ();
							} else if (!Menus [i].IsTopLevel) {
								Activate (i);
							}
						} else if (selected != i && selected > -1 && (me.Flags == MouseFlags.ReportMousePosition ||
							me.Flags == MouseFlags.Button1Pressed && me.Flags == MouseFlags.ReportMousePosition)) {
							if (IsMenuOpen) {
								if (!CloseMenu (true, false)) {
									return true;
								}
								Activate (i);
							}
						} else {
							if (IsMenuOpen)
								Activate (i);
						}
						return true;
					}
					pos += leftPadding + Menus [i].TitleLength + rightPadding;
				}
			}
			return false;
		}

		internal bool handled;
		internal bool isContextMenuLoading;

		internal bool HandleGrabView (MouseEvent me, View current)
		{
			if (Application.MouseGrabView != null) {
				if (me.View is MenuBar || me.View is Menu) {
					var mbar = GetMouseGrabViewInstance (me.View);
					if (mbar != null) {
						if (me.Flags == MouseFlags.Button1Clicked) {
							mbar.CleanUp ();
							Application.GrabMouse (me.View);
						} else {
							handled = false;
							return false;
						}
					}
					if (me.View != current) {
						Application.UngrabMouse ();
						var v = me.View;
						Application.GrabMouse (v);
						MouseEvent nme;
						if (me.Y > -1) {
							var newxy = v.ScreenToView (me.X, me.Y);
							nme = new MouseEvent () {
								X = newxy.X,
								Y = newxy.Y,
								Flags = me.Flags,
								OfX = me.X - newxy.X,
								OfY = me.Y - newxy.Y,
								View = v
							};
						} else {
							nme = new MouseEvent () {
								X = me.X + current.Frame.X,
								Y = 0,
								Flags = me.Flags,
								View = v
							};
						}

						v.MouseEvent (nme);
						return false;
					}
				} else if (!isContextMenuLoading && !(me.View is MenuBar || me.View is Menu)
					&& me.Flags != MouseFlags.ReportMousePosition && me.Flags != 0) {

					Application.UngrabMouse ();
					if (IsMenuOpen)
						CloseAllMenus ();
					handled = false;
					return false;
				} else {
					handled = false;
					isContextMenuLoading = false;
					return false;
				}
			} else if (!IsMenuOpen && (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked
				|| me.Flags == MouseFlags.Button1TripleClicked || me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))) {

				Application.GrabMouse (current);
			} else if (IsMenuOpen && (me.View is MenuBar || me.View is Menu)) {
				Application.GrabMouse (me.View);
			} else {
				handled = false;
				return false;
			}

			handled = true;

			return true;
		}

		MenuBar GetMouseGrabViewInstance (View view)
		{
			if (view == null || Application.MouseGrabView == null) {
				return null;
			}

			MenuBar hostView = null;
			if (view is MenuBar) {
				hostView = (MenuBar)view;
			} else if (view is Menu) {
				hostView = ((Menu)view).host;
			}

			var grabView = Application.MouseGrabView;
			MenuBar hostGrabView = null;
			if (grabView is MenuBar) {
				hostGrabView = (MenuBar)grabView;
			} else if (grabView is Menu) {
				hostGrabView = ((Menu)grabView).host;
			}

			return hostView != hostGrabView ? hostGrabView : null;
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}
}
