using System;

namespace Terminal.Gui {
	/// <summary>
	/// Args for events that relate to specific <see cref="View"/>
	/// </summary>
	public class ViewEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="Terminal.Gui.View"/> class.
		/// </summary>
		/// <param name="view"></param>
		public ViewEventArgs (View view)
		{
			View = view;
		}

		/// <summary>
		/// The view that the event is about.
		/// </summary>
		/// <remarks>
		/// Can be different from the sender of the <see cref="EventHandler"/>
		/// for example if event describes the adding a child then sender may
		/// be the parent while <see cref="View"/> is the child
		/// being added.
		/// </remarks>
		public View View { get; }
	}

	/// <summary>
	/// Event arguments for the <see cref="View.LayoutComplete"/> event.
	/// </summary>
	public class LayoutEventArgs : EventArgs {
		/// <summary>
		/// The view-relative bounds of the <see cref="View"/> before it was laid out.
		/// </summary>
		public Rect OldBounds { get; set; }
	}

	/// <summary>
	/// Event args for draw events
	/// </summary>
	public class DrawEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="DrawEventArgs"/> class.
		/// </summary>
		/// <param name="rect">Gets the view-relative rectangle describing the currently visible viewport into the <see cref="View"/>.</param>
		public DrawEventArgs (Rect rect)
		{
			Rect = rect;
		}

		/// <summary>
		/// Gets the view-relative rectangle describing the currently visible viewport into the <see cref="View"/>.
		/// </summary>
		public Rect Rect { get; }
	}

	/// <summary>
	/// Defines the event arguments for <see cref="View.SetFocus(View)"/>
	/// </summary>
	public class FocusEventArgs : EventArgs {
		/// <summary>
		/// Constructs.
		/// </summary>
		/// <param name="view">The view that gets or loses focus.</param>
		public FocusEventArgs (View view) { View = view; }
		/// <summary>
		/// Indicates if the current focus event has already been processed and the driver should stop notifying any other event subscriber.
		/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
		/// </summary>
		public bool Handled { get; set; }
		/// <summary>
		/// Indicates the current view that gets or loses focus.
		/// </summary>
		public View View { get; set; }
	}

}
