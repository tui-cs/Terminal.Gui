using System;

namespace Terminal.Gui {
	/// <summary>
	/// Event args for events which relate to a single <see cref="Point"/>
	/// </summary>
	public class PointEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="PointEventArgs"/> class
		/// </summary>
		/// <param name="p"></param>
		public PointEventArgs (Point p)
		{
			this.Point = p;
		}

		/// <summary>
		/// The point the event happened at
		/// </summary>
		public Point Point { get; }
	}
}
