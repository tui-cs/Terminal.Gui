using System;
using static Terminal.Gui.MainLoop;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="EventArgs"/> for timeout events (e.g. <see cref="MainLoop.TimeoutAdded"/>)
	/// </summary>
	public class TimeoutEventArgs : EventArgs {
		/// <summary>
		/// Gets the timeout callback handler
		/// </summary>
		public Timeout Timeout { get; }

		/// <summary>
		/// Gets the <see cref="DateTime.Ticks"/> in UTC time when the 
		/// <see cref="Timeout"/> will next execute after.
		/// </summary>
		public long Ticks { get; }


		/// <summary>
		/// Creates a new instance of the <see cref="TimeoutEventArgs"/> class.
		/// </summary>
		/// <param name="timeout"></param>
		/// <param name="ticks"></param>
		public TimeoutEventArgs (Timeout timeout, long ticks)
		{
			Timeout = timeout;
			Ticks = ticks;
		}
	}
}
