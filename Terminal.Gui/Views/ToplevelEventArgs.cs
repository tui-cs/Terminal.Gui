using System;

namespace Terminal.Gui {
	/// <summary>
	/// Args for events that relate to a specific <see cref="Toplevel"/>.
	/// </summary>
	public class ToplevelEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="ToplevelClosingEventArgs"/> class.
		/// </summary>
		/// <param name="toplevel"></param>
		public ToplevelEventArgs (Toplevel toplevel)
		{
			Toplevel = toplevel;
		}

		/// <summary>
		/// Gets the <see cref="Toplevel"/> that the event is about.
		/// </summary>
		/// <remarks>This is usually but may not always be the same as the sender 
		/// in <see cref="EventHandler"/>.  For example if the reported event
		/// is about a different  <see cref="Toplevel"/> or the event is
		/// raised by a separate class.
		/// </remarks>
		public Toplevel Toplevel { get; }
	}

	/// <summary>
	/// <see cref="EventArgs"/> implementation for the <see cref="Toplevel.Closing"/> event.
	/// </summary>
	public class ToplevelClosingEventArgs : EventArgs {
		/// <summary>
		/// The Toplevel requesting stop.
		/// </summary>
		public View RequestingTop { get; }
		/// <summary>
		/// Provides an event cancellation option.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Initializes the event arguments with the requesting Toplevel.
		/// </summary>
		/// <param name="requestingTop">The <see cref="RequestingTop"/>.</param>
		public ToplevelClosingEventArgs (Toplevel requestingTop)
		{
			RequestingTop = requestingTop;
		}
	}
}
