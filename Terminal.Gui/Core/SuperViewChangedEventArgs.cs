using System;

namespace Terminal.Gui {
	/// <summary>
	/// Args for events where the <see cref="View.SuperView"/> of a <see cref="View"/> is changed
	/// (e.g. <see cref="View.Removed"/> / <see cref="View.Added"/> events).
	/// </summary>
	public class SuperViewChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="SuperViewChangedEventArgs"/> class.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="child"></param>
		public SuperViewChangedEventArgs (View parent, View child)
		{
			Parent = parent;
			Child = child;
		}

		/// <summary>
		/// The parent.  For <see cref="View.Removed"/> this is the old
		/// parent (new parent now being null).  For <see cref="View.Added"/>
		/// it is the new parent to whom view now belongs.
		/// </summary>
		public View Parent { get; }

		/// <summary>
		/// The view that is having it's <see cref="View.SuperView"/> changed
		/// </summary>
		public View Child { get; }
	}
}
