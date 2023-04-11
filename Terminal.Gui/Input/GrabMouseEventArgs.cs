using System;

namespace Terminal.Gui {
	/// <summary>
	/// Args for events that relate to specific <see cref="Application.MouseGrabView"/>
	/// </summary>
	public class GrabMouseEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="GrabMouseEventArgs"/> class.
		/// </summary>
		/// <param name="view">The view that the event is about.</param>
		public GrabMouseEventArgs (View view)
		{
			View = view;
		}

		/// <summary>
		/// The view that the event is about.
		/// </summary>
		public View View { get; }

		/// <summary>
		/// Flag that allows the cancellation of the event. If set to <see langword="true"/> in the
		/// event handler, the event will be canceled.
		/// </summary>
		public bool Cancel { get; set; }
	}
}
