using System;

namespace Terminal.Gui {

	/// <summary>
	/// Args for events about Size (e.g. Resized)
	/// </summary>
	public class SizeChangedEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="SizeChangedEventArgs"/> class.
		/// </summary>
		/// <param name="size"></param>
		public SizeChangedEventArgs (Size size)
		{
			Size = size;
		}

		/// <summary>
		/// Gets the size the event describes.  This should
		/// reflect the new/current size after the event
		/// resolved.
		/// </summary>
		public Size Size { get; }
	}
}
