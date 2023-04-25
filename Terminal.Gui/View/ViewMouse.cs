using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	public partial class View  {
		/// <summary>
		/// Event fired when the view receives the mouse event for the first time.
		/// </summary>
		public event EventHandler<MouseEventEventArgs> MouseEnter;

		/// <summary>
		/// Event fired when the view receives a mouse event for the last time.
		/// </summary>
		public event EventHandler<MouseEventEventArgs> MouseLeave;

		/// <summary>
		/// Event fired when a mouse event is generated.
		/// </summary>
		public event EventHandler<MouseEventEventArgs> MouseClick;

		/// <inheritdoc/>
		public override bool OnMouseEnter (MouseEvent mouseEvent)
		{
			if (!Enabled) {
				return true;
			}

			if (!CanBeVisible (this)) {
				return false;
			}

			var args = new MouseEventEventArgs (mouseEvent);
			MouseEnter?.Invoke (this, args);

			return args.Handled || base.OnMouseEnter (mouseEvent);
		}

		/// <inheritdoc/>
		public override bool OnMouseLeave (MouseEvent mouseEvent)
		{
			if (!Enabled) {
				return true;
			}

			if (!CanBeVisible (this)) {
				return false;
			}

			var args = new MouseEventEventArgs (mouseEvent);
			MouseLeave?.Invoke (this, args);

			return args.Handled || base.OnMouseLeave (mouseEvent);
		}

		/// <summary>
		/// Method invoked when a mouse event is generated
		/// </summary>
		/// <param name="mouseEvent"></param>
		/// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
		public virtual bool OnMouseEvent (MouseEvent mouseEvent)
		{
			if (!Enabled) {
				return true;
			}

			if (!CanBeVisible (this)) {
				return false;
			}

			var args = new MouseEventEventArgs (mouseEvent);
			if (OnMouseClick (args))
				return true;
			if (MouseEvent (mouseEvent))
				return true;

			if (mouseEvent.Flags == MouseFlags.Button1Clicked) {
				if (CanFocus && !HasFocus && SuperView != null) {
					SuperView.SetFocus (this);
					SetNeedsDisplay ();
				}

				return true;
			}
			return false;
		}

		/// <summary>
		/// Invokes the MouseClick event.
		/// </summary>
		protected bool OnMouseClick (MouseEventEventArgs args)
		{
			if (!Enabled) {
				return true;
			}

			MouseClick?.Invoke (this, args);
			return args.Handled;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="View"/> wants mouse position reports.
		/// </summary>
		/// <value><see langword="true"/> if want mouse position reports; otherwise, <see langword="false"/>.</value>
		public virtual bool WantMousePositionReports { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="View"/> want continuous button pressed event.
		/// </summary>
		public virtual bool WantContinuousButtonPressed { get; set; }
	}
}
