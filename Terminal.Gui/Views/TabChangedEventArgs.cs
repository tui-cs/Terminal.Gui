using System;

namespace Terminal.Gui {

	/// <summary>
	/// Describes a change in <see cref="TabView.SelectedTab"/>
	/// </summary>
	public class TabChangedEventArgs : EventArgs {

		/// <summary>
		/// The previously selected tab. May be null
		/// </summary>
		public TabView.Tab OldTab { get; }

		/// <summary>
		/// The currently selected tab. May be null
		/// </summary>
		public TabView.Tab NewTab { get; }

		/// <summary>
		/// Documents a tab change
		/// </summary>
		/// <param name="oldTab"></param>
		/// <param name="newTab"></param>
		public TabChangedEventArgs (TabView.Tab oldTab, TabView.Tab newTab)
		{
			OldTab = oldTab;
			NewTab = newTab;
		}
	}
}
