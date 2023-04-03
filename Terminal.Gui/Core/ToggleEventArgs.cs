using System;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="EventArgs"/> for the <see cref="CheckBox.Toggled"/> event
	/// </summary>
	public class ToggleEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="ToggleEventArgs"/> class.
		/// </summary>
		/// <param name="oldValue"></param>
		/// <param name="newValue"></param>
		public ToggleEventArgs (bool? oldValue, bool? newValue)
		{
			OldValue = oldValue;
			NewValue = newValue;
		}

		/// <summary>
		/// The previous checked state
		/// </summary>
		public bool? OldValue { get; }

		/// <summary>
		/// The new checked state
		/// </summary>
		public bool? NewValue { get; }
	}
}
