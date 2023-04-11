using System;

namespace Terminal.Gui {

	/// <summary>
	/// Defines the event arguments for <see cref="KeyEvent"/>
	/// </summary>
	public class KeyEventEventArgs : EventArgs {
		/// <summary>
		/// Constructs.
		/// </summary>
		/// <param name="ke"></param>
		public KeyEventEventArgs (KeyEvent ke) => KeyEvent = ke;
		/// <summary>
		/// The <see cref="KeyEvent"/> for the event.
		/// </summary>
		public KeyEvent KeyEvent { get; set; }
		/// <summary>
		/// Indicates if the current Key event has already been processed and the driver should stop notifying any other event subscriber.
		/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
		/// </summary>
		public bool Handled { get; set; } = false;
	}
}
