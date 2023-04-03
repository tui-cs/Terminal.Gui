//
// StatusBar.cs: a statusbar for an application
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// TODO:
//   Add mouse support
using System;
using System.Collections.Generic;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="StatusItem"/> objects are contained by <see cref="StatusBar"/> <see cref="View"/>s. 
	/// Each <see cref="StatusItem"/> has a title, a shortcut (hotkey), and an <see cref="Action"/> that will be invoked when the 
	/// <see cref="StatusItem.Shortcut"/> is pressed.
	/// The <see cref="StatusItem.Shortcut"/> will be a global hotkey for the application in the current context of the screen.
	/// The colour of the <see cref="StatusItem.Title"/> will be changed after each ~. 
	/// A <see cref="StatusItem.Title"/> set to `~F1~ Help` will render as *F1* using <see cref="ColorScheme.HotNormal"/> and
	/// *Help* as <see cref="ColorScheme.HotNormal"/>.
	/// </summary>
	public class StatusItem {
		/// <summary>
		/// Initializes a new <see cref="StatusItem"/>.
		/// </summary>
		/// <param name="shortcut">Shortcut to activate the <see cref="StatusItem"/>.</param>
		/// <param name="title">Title for the <see cref="StatusItem"/>.</param>
		/// <param name="action">Action to invoke when the <see cref="StatusItem"/> is activated.</param>
		public StatusItem (Key shortcut, ustring title, Action action)
		{
			Title = title ?? "";
			Shortcut = shortcut;
			Action = action;
		}

		/// <summary>
		/// Gets the global shortcut to invoke the action on the menu.
		/// </summary>
		public Key Shortcut { get; set; }

		/// <summary>
		/// Gets or sets the title.
		/// </summary>
		/// <value>The title.</value>
		/// <remarks>
		/// The colour of the <see cref="StatusItem.Title"/> will be changed after each ~. 
		/// A <see cref="StatusItem.Title"/> set to `~F1~ Help` will render as *F1* using <see cref="ColorScheme.HotNormal"/> and
		/// *Help* as <see cref="ColorScheme.HotNormal"/>.
		/// </remarks>
		public ustring Title { get; set; }

		/// <summary>
		/// Gets or sets the action to be invoked when the statusbar item is triggered
		/// </summary>
		/// <value>Action to invoke.</value>
		public Action Action { get; }

		/// <summary>
		/// Gets or sets arbitrary data for the status item.
		/// </summary>
		/// <remarks>This property is not used internally.</remarks>
		public object Data { get; set; }
	};

	/// <summary>
	/// A status bar is a <see cref="View"/> that snaps to the bottom of a <see cref="Toplevel"/> displaying set of <see cref="StatusItem"/>s.
	/// The <see cref="StatusBar"/> should be context sensitive. This means, if the main menu and an open text editor are visible, the items probably shown will
	/// be ~F1~ Help ~F2~ Save ~F3~ Load. While a dialog to ask a file to load is executed, the remaining commands will probably be ~F1~ Help.
	/// So for each context must be a new instance of a statusbar.
	/// </summary>
	public class StatusBar : View {
		/// <summary>
		/// The items that compose the <see cref="StatusBar"/>
		/// </summary>
		public StatusItem [] Items { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="StatusBar"/> class.
		/// </summary>
		public StatusBar () : this (items: new StatusItem [] { }) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="StatusBar"/> class with the specified set of <see cref="StatusItem"/>s.
		/// The <see cref="StatusBar"/> will be drawn on the lowest line of the terminal or <see cref="View.SuperView"/> (if not null).
		/// </summary>
		/// <param name="items">A list of statusbar items.</param>
		public StatusBar (StatusItem [] items) : base ()
		{
			Items = items;
			CanFocus = false;
			ColorScheme = Colors.Menu;
			X = 0;
			Y = Pos.AnchorEnd (1);
			Width = Dim.Fill ();
			Height = 1;
		}

		static ustring shortcutDelimiter = "-";
		/// <summary>
		/// Used for change the shortcut delimiter separator.
		/// </summary>
		public static ustring ShortcutDelimiter {
			get => shortcutDelimiter;
			set {
				if (shortcutDelimiter != value) {
					shortcutDelimiter = value == ustring.Empty ? " " : value;
				}
			}
		}

		Attribute ToggleScheme (Attribute scheme)
		{
			var result = scheme == ColorScheme.Normal ? ColorScheme.HotNormal : ColorScheme.Normal;
			Driver.SetAttribute (result);
			return result;
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Move (0, 0);
			Driver.SetAttribute (GetNormalColor ());
			for (int i = 0; i < Frame.Width; i++)
				Driver.AddRune (' ');

			Move (1, 0);
			var scheme = GetNormalColor ();
			Driver.SetAttribute (scheme);
			for (int i = 0; i < Items.Length; i++) {
				var title = Items [i].Title.ToString ();
				for (int n = 0; n < Items [i].Title.RuneCount; n++) {
					if (title [n] == '~') {
						scheme = ToggleScheme (scheme);
						continue;
					}
					Driver.AddRune (title [n]);
				}
				if (i + 1 < Items.Length) {
					Driver.AddRune (' ');
					Driver.AddRune (Driver.VLine);
					Driver.AddRune (' ');
				}
			}
		}

		///<inheritdoc/>
		public override bool ProcessHotKey (KeyEvent kb)
		{
			foreach (var item in Items) {
				if (kb.Key == item.Shortcut) {
					Run (item.Action);
					return true;
				}
			}
			return false;
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (me.Flags != MouseFlags.Button1Clicked)
				return false;

			int pos = 1;
			for (int i = 0; i < Items.Length; i++) {
				if (me.X >= pos && me.X < pos + GetItemTitleLength (Items [i].Title)) {
					Run (Items [i].Action);
					break;
				}
				pos += GetItemTitleLength (Items [i].Title) + 3;
			}
			return true;
		}

		int GetItemTitleLength (ustring title)
		{
			int len = 0;
			foreach (var ch in title) {
				if (ch == '~')
					continue;
				len++;
			}

			return len;
		}

		void Run (Action action)
		{
			if (action == null)
				return;

			Application.MainLoop.AddIdle (() => {
				action ();
				return false;
			});
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}

		/// <summary>
		/// Inserts a <see cref="StatusItem"/> in the specified index of <see cref="Items"/>.
		/// </summary>
		/// <param name="index">The zero-based index at which item should be inserted.</param>
		/// <param name="item">The item to insert.</param>
		public void AddItemAt (int index, StatusItem item)
		{
			var itemsList = new List<StatusItem> (Items);
			itemsList.Insert (index, item);
			Items = itemsList.ToArray ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Removes a <see cref="StatusItem"/> at specified index of <see cref="Items"/>.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		/// <returns>The <see cref="StatusItem"/> removed.</returns>
		public StatusItem RemoveItem (int index)
		{
			var itemsList = new List<StatusItem> (Items);
			var item = itemsList [index];
			itemsList.RemoveAt (index);
			Items = itemsList.ToArray ();
			SetNeedsDisplay ();

			return item;
		}
	}
}