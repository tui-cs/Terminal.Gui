using System;

namespace Terminal.Gui {
	/// <summary>
	/// An <see cref="EventArgs"/> which allows passing a cancelable menu opening event or replacing with a new <see cref="MenuBarItem"/>.
	/// </summary>
	public class MenuOpeningEventArgs : EventArgs {
		/// <summary>
		/// The current <see cref="MenuBarItem"/> parent.
		/// </summary>
		public MenuBarItem CurrentMenu { get; }

		/// <summary>
		/// The new <see cref="MenuBarItem"/> to be replaced.
		/// </summary>
		public MenuBarItem NewMenuBarItem { get; set; }
		/// <summary>
		/// Flag that allows the cancellation of the event. If set to <see langword="true"/> in the
		/// event handler, the event will be canceled. 
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Initializes a new instance of <see cref="MenuOpeningEventArgs"/>.
		/// </summary>
		/// <param name="currentMenu">The current <see cref="MenuBarItem"/> parent.</param>
		public MenuOpeningEventArgs (MenuBarItem currentMenu)
		{
			CurrentMenu = currentMenu;
		}
	}

	/// <summary>
	/// Defines arguments for the <see cref="MenuBar.MenuOpened"/> event
	/// </summary>
	public class MenuOpenedEventArgs : EventArgs {
		/// <summary>
		/// Creates a new instance of the <see cref="MenuOpenedEventArgs"/> class
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="menuItem"></param>
		public MenuOpenedEventArgs (MenuBarItem parent, MenuItem menuItem)
		{
			Parent = parent;
			MenuItem = menuItem;
		}

		/// <summary>
		/// The parent of <see cref="MenuItem"/>.  Will be null if menu opening
		/// is the root (see <see cref="MenuBarItem.IsTopLevel"/>).
		/// </summary>
		public MenuBarItem Parent { get; }

		/// <summary>
		/// Gets the <see cref="MenuItem"/> being opened.
		/// </summary>
		public MenuItem MenuItem { get; }
	}

	/// <summary>
	/// An <see cref="EventArgs"/> which allows passing a cancelable menu closing event.
	/// </summary>
	public class MenuClosingEventArgs : EventArgs {
		/// <summary>
		/// The current <see cref="MenuBarItem"/> parent.
		/// </summary>
		public MenuBarItem CurrentMenu { get; }

		/// <summary>
		/// Indicates whether the current menu will reopen.
		/// </summary>
		public bool Reopen { get; }

		/// <summary>
		/// Indicates whether the current menu is a sub-menu.
		/// </summary>
		public bool IsSubMenu { get; }

		/// <summary>
		/// Flag that allows the cancellation of the event. If set to <see langword="true"/> in the
		/// event handler, the event will be canceled. 
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Initializes a new instance of <see cref="MenuClosingEventArgs"/>.
		/// </summary>
		/// <param name="currentMenu">The current <see cref="MenuBarItem"/> parent.</param>
		/// <param name="reopen">Whether the current menu will reopen.</param>
		/// <param name="isSubMenu">Indicates whether it is a sub-menu.</param>
		public MenuClosingEventArgs (MenuBarItem currentMenu, bool reopen, bool isSubMenu)
		{
			CurrentMenu = currentMenu;
			Reopen = reopen;
			IsSubMenu = isSubMenu;
		}
	}
}
