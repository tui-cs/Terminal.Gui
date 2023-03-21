using System;

namespace Terminal.Gui {

	/// <summary>
	/// Event args for when a <see cref="Key"/> is changed from
	/// one value to a new value (e.g. in <see cref="View.HotKeyChanged"/>)
	/// </summary>
	public class KeyChangedEventArgs : EventArgs {

		/// <summary>
		/// Gets the old <see cref="Key"/> that was set before the event.
		/// Use <see cref="Key.Null"/> to check for empty.
		/// </summary>
		public Key OldKey { get; }

		/// <summary>
		/// Gets the new <see cref="Key"/> that is being used.
		/// Use <see cref="Key.Null"/> to check for empty.
		/// </summary>
		public Key NewKey { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="KeyChangedEventArgs"/> class
		/// </summary>
		/// <param name="oldKey"></param>
		/// <param name="newKey"></param>
		public KeyChangedEventArgs (Key oldKey, Key newKey)
		{
			this.OldKey = oldKey;
			this.NewKey = newKey;
		}
	}
}
