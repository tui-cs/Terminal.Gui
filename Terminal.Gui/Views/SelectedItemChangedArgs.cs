using System;

namespace Terminal.Gui {
	/// <summary>
	/// Event arguments for the SelectedItemChagned event.
	/// </summary>
	public class SelectedItemChangedArgs : EventArgs {
		/// <summary>
		/// Gets the index of the item that was previously selected. -1 if there was no previous selection.
		/// </summary>
		public int PreviousSelectedItem { get; }

		/// <summary>
		/// Gets the index of the item that is now selected. -1 if there is no selection.
		/// </summary>
		public int SelectedItem { get; }

		/// <summary>
		/// Initializes a new <see cref="SelectedItemChangedArgs"/> class.
		/// </summary>
		/// <param name="selectedItem"></param>
		/// <param name="previousSelectedItem"></param>
		public SelectedItemChangedArgs (int selectedItem, int previousSelectedItem)
		{
			PreviousSelectedItem = previousSelectedItem;
			SelectedItem = selectedItem;
		}
	}
}
