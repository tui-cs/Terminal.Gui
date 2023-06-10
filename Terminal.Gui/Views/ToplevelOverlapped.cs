using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Terminal.Gui {
	public partial class Toplevel {
		/// <summary>
		/// Gets or sets if this Toplevel is a container for overlapped children.
		/// </summary>
		public bool IsOverlappedContainer { get; set; }

		/// <summary>
		/// Gets or sets if this Toplevel is in overlapped mode within a Toplevel container.
		/// </summary>
		public bool IsOverlapped {
			get {
				return Application.OverlappedTop != null && Application.OverlappedTop != this && !Modal;
			}
		}

	}

	public static partial class Application {

		/// <summary>
		/// Gets the list of the Overlapped children which are not modal <see cref="Toplevel"/> from the <see cref="OverlappedTop"/>.
		/// </summary>
		public static List<Toplevel> OverlappedChildren {
			get {
				if (OverlappedTop != null) {
					List<Toplevel> _overlappedChildren = new List<Toplevel> ();
					foreach (var top in _toplevels) {
						if (top != OverlappedTop && !top.Modal) {
							_overlappedChildren.Add (top);
						}
					}
					return _overlappedChildren;
				}
				return null;
			}
		}

		/// <summary>
		/// The <see cref="Toplevel"/> object used for the application on startup which <see cref="Toplevel.IsOverlappedContainer"/> is true.
		/// </summary>
		public static Toplevel OverlappedTop {
			get {
				if (Top.IsOverlappedContainer) {
					return Top;
				}
				return null;
			}
		}


		static View FindDeepestOverlappedView (View start, int x, int y, out int resx, out int resy)
		{
			if (start.GetType ().BaseType != typeof (Toplevel)
				&& !((Toplevel)start).IsOverlappedContainer) {
				resx = 0;
				resy = 0;
				return null;
			}

			var startFrame = start.Frame;

			if (!startFrame.Contains (x, y)) {
				resx = 0;
				resy = 0;
				return null;
			}

			int count = _toplevels.Count;
			for (int i = count - 1; i >= 0; i--) {
				foreach (var top in _toplevels) {
					var rx = x - startFrame.X;
					var ry = y - startFrame.Y;
					if (top.Visible && top.Frame.Contains (rx, ry)) {
						var deep = View.FindDeepestView (top, rx, ry, out resx, out resy);
						if (deep == null)
							return FindDeepestOverlappedView (top, rx, ry, out resx, out resy);
						if (deep != OverlappedTop)
							return deep;
					}
				}
			}
			resx = x - startFrame.X;
			resy = y - startFrame.Y;
			return start;
		}

		static bool OverlappedChildNeedsDisplay ()
		{
			if (OverlappedTop == null) {
				return false;
			}

			foreach (var top in _toplevels) {
				if (top != Current && top.Visible && (!top._needsDisplay.IsEmpty || top._subViewNeedsDisplay || top.LayoutNeeded)) {
					OverlappedTop.SetSubViewNeedsDisplay ();
					return true;
				}
			}
			return false;
		}


		static bool SetCurrentOverlappedAsTop ()
		{
			if (OverlappedTop == null && Current != Top && Current?.SuperView == null && Current?.Modal == false) {
				if (Current.Frame != new Rect (0, 0, Driver.Cols, Driver.Rows)) {
					Current.Frame = new Rect (0, 0, Driver.Cols, Driver.Rows);
				}
				Top = Current;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Move to the next Overlapped child from the <see cref="OverlappedTop"/>.
		/// </summary>
		public static void OverlappedMoveNext ()
		{
			if (OverlappedTop != null && !Current.Modal) {
				lock (_toplevels) {
					_toplevels.MoveNext ();
					var isOverlapped = false;
					while (_toplevels.Peek () == OverlappedTop || !_toplevels.Peek ().Visible) {
						if (!isOverlapped && _toplevels.Peek () == OverlappedTop) {
							isOverlapped = true;
						} else if (isOverlapped && _toplevels.Peek () == OverlappedTop) {
							MoveCurrent (Top);
							break;
						}
						_toplevels.MoveNext ();
					}
					Current = _toplevels.Peek ();
				}
			}
		}

		/// <summary>
		/// Move to the previous Overlapped child from the <see cref="OverlappedTop"/>.
		/// </summary>
		public static void OverlappedMovePrevious ()
		{
			if (OverlappedTop != null && !Current.Modal) {
				lock (_toplevels) {
					_toplevels.MovePrevious ();
					var isOverlapped = false;
					while (_toplevels.Peek () == OverlappedTop || !_toplevels.Peek ().Visible) {
						if (!isOverlapped && _toplevels.Peek () == OverlappedTop) {
							isOverlapped = true;
						} else if (isOverlapped && _toplevels.Peek () == OverlappedTop) {
							MoveCurrent (Top);
							break;
						}
						_toplevels.MovePrevious ();
					}
					Current = _toplevels.Peek ();
				}
			}
		}

		/// <summary>
		/// Move to the next Overlapped child from the <see cref="OverlappedTop"/> and set it as the <see cref="Top"/> if it is not already.
		/// </summary>
		/// <param name="top"></param>
		/// <returns></returns>
		public static bool MoveToOverlappedChild (Toplevel top)
		{
			if (top.Visible && OverlappedTop != null && Current?.Modal == false) {
				lock (_toplevels) {
					_toplevels.MoveTo (top, 0, new ToplevelEqualityComparer ());
					Current = top;
				}
				return true;
			}
			return false;
		}


		/// <summary>
		/// Brings the superview of the most focused overlapped view is on front.
		/// </summary>
		public static void BringOverlappedTopToFront ()
		{
			if (OverlappedTop != null) {
				return;
			}
			var top = FindTopFromView (Top?.MostFocused);
			if (top != null && Top.Subviews.Count > 1 && Top.Subviews [Top.Subviews.Count - 1] != top) {
				Top.BringSubviewToFront (top);
			}
		}


		/// <summary>
		/// Gets the current visible Toplevel overlapped child that matches the arguments pattern.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="exclude">The strings to exclude.</param>
		/// <returns>The matched view.</returns>
		public static Toplevel GetTopOverlappedChild (Type type = null, string [] exclude = null)
		{
			if (Application.OverlappedTop == null) {
				return null;
			}

			foreach (var top in Application.OverlappedChildren) {
				if (type != null && top.GetType () == type
					&& exclude?.Contains (top.Data.ToString ()) == false) {
					return top;
				} else if ((type != null && top.GetType () != type)
					|| (exclude?.Contains (top.Data.ToString ()) == true)) {
					continue;
				}
				return top;
			}
			return null;
		}

	}
}
