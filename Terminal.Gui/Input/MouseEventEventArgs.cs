using System;

namespace Terminal.Gui {
	/// <summary>
	/// Specifies the event arguments for <see cref="Terminal.Gui.MouseEvent"/>. This is a higher-level construct
	/// than the wrapped <see cref="MouseEvent"/> class and is used for the events defined on <see cref="View"/>
	/// and subclasses of View (e.g. <see cref="View.MouseEnter"/> and <see cref="View.MouseClick"/>).
	/// </summary>
	public class MouseEventEventArgs : EventArgs {
		/// <summary>
		/// Constructs.
		/// </summary>
		/// <param name="me">The mouse event.</param>
		public MouseEventEventArgs (MouseEvent me) => MouseEvent = me;
		/// <summary>
		/// The <see cref="Terminal.Gui.MouseEvent"/> for the event.
		/// </summary>
		public MouseEvent MouseEvent { get; set; }

		/// <summary>
		/// Indicates if the current mouse event has already been processed and the driver should stop notifying any other event subscriber.
		/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
		/// </summary>
		/// <remarks>This property forwards to the <see cref="MouseEvent.Handled"/> property and is provided as a convenience and for
		/// backwards compatibility</remarks>
		public bool Handled {
			get => MouseEvent.Handled;
			set => MouseEvent.Handled = value;
		}
	}
}
