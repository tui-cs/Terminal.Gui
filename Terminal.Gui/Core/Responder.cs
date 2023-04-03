//
// Core.cs: The core engine for gui.cs
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// Pending:
//   - Check for NeedDisplay on the hierarchy and repaint
//   - Layout support
//   - "Colors" type or "Attributes" type?
//   - What to surface as "BackgroundCOlor" when clearing a window, an attribute or colors?
//
// Optimziations
//   - Add rendering limitation to the exposed area

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Terminal.Gui {
	/// <summary>
	/// Responder base class implemented by objects that want to participate on keyboard and mouse input.
	/// </summary>
	public class Responder : IDisposable {
		bool disposedValue;

#if DEBUG_IDISPOSABLE
		/// <summary>
		/// For debug purposes to verify objects are being disposed properly
		/// </summary>
		public bool WasDisposed = false;
		/// <summary>
		/// For debug purposes to verify objects are being disposed properly
		/// </summary>
		public int DisposedCount = 0;
		/// <summary>
		/// For debug purposes
		/// </summary>
		public static List<Responder> Instances = new List<Responder> ();
		/// <summary>
		/// For debug purposes
		/// </summary>
		public Responder ()
		{
			Instances.Add (this);
		}
#endif

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Responder"/> can focus.
		/// </summary>
		/// <value><c>true</c> if can focus; otherwise, <c>false</c>.</value>
		public virtual bool CanFocus { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Responder"/> has focus.
		/// </summary>
		/// <value><c>true</c> if has focus; otherwise, <c>false</c>.</value>
		public virtual bool HasFocus { get; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Responder"/> can respond to user interaction.
		/// </summary>
		public virtual bool Enabled { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Responder"/> and all its child controls are displayed.
		/// </summary>
		public virtual bool Visible { get; set; } = true;

		// Key handling
		/// <summary>
		///   This method can be overwritten by view that
		///     want to provide accelerator functionality
		///     (Alt-key for example).
		/// </summary>
		/// <remarks>
		///   <para>
		///     Before keys are sent to the subview on the
		///     current view, all the views are
		///     processed and the key is passed to the widgets
		///     to allow some of them to process the keystroke
		///     as a hot-key. </para>
		///  <para>
		///     For example, if you implement a button that
		///     has a hotkey ok "o", you would catch the
		///     combination Alt-o here.  If the event is
		///     caught, you must return true to stop the
		///     keystroke from being dispatched to other
		///     views.
		///  </para>
		/// </remarks>

		public virtual bool ProcessHotKey (KeyEvent kb)
		{
			return false;
		}

		/// <summary>
		///   If the view is focused, gives the view a
		///   chance to process the keystroke.
		/// </summary>
		/// <remarks>
		///   <para>
		///     Views can override this method if they are
		///     interested in processing the given keystroke.
		///     If they consume the keystroke, they must
		///     return true to stop the keystroke from being
		///     processed by other widgets or consumed by the
		///     widget engine.    If they return false, the
		///     keystroke will be passed using the ProcessColdKey
		///     method to other views to process.
		///   </para>
		///   <para>
		///     The View implementation does nothing but return false,
		///     so it is not necessary to call base.ProcessKey if you
		///     derive directly from View, but you should if you derive
		///     other View subclasses.
		///   </para>
		/// </remarks>
		/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
		public virtual bool ProcessKey (KeyEvent keyEvent)
		{
			return false;
		}

		/// <summary>
		///   This method can be overwritten by views that
		///     want to provide accelerator functionality
		///     (Alt-key for example), but without
		///     interefering with normal ProcessKey behavior.
		/// </summary>
		/// <remarks>
		///   <para>
		///     After keys are sent to the subviews on the
		///     current view, all the view are
		///     processed and the key is passed to the views
		///     to allow some of them to process the keystroke
		///     as a cold-key. </para>
		///  <para>
		///    This functionality is used, for example, by
		///    default buttons to act on the enter key.
		///    Processing this as a hot-key would prevent
		///    non-default buttons from consuming the enter
		///    keypress when they have the focus.
		///  </para>
		/// </remarks>
		/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
		public virtual bool ProcessColdKey (KeyEvent keyEvent)
		{
			return false;
		}

		/// <summary>
		/// Method invoked when a key is pressed.
		/// </summary>
		/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
		/// <returns>true if the event was handled</returns>
		public virtual bool OnKeyDown (KeyEvent keyEvent)
		{
			return false;
		}

		/// <summary>
		/// Method invoked when a key is released.
		/// </summary>
		/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
		/// <returns>true if the event was handled</returns>
		public virtual bool OnKeyUp (KeyEvent keyEvent)
		{
			return false;
		}

		/// <summary>
		/// Method invoked when a mouse event is generated
		/// </summary>
		/// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
		/// <param name="mouseEvent">Contains the details about the mouse event.</param>
		public virtual bool MouseEvent (MouseEvent mouseEvent)
		{
			return false;
		}

		/// <summary>
		/// Method invoked when a mouse event is generated for the first time.
		/// </summary>
		/// <param name="mouseEvent"></param>
		/// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
		public virtual bool OnMouseEnter (MouseEvent mouseEvent)
		{
			return false;
		}

		/// <summary>
		/// Method invoked when a mouse event is generated for the last time.
		/// </summary>
		/// <param name="mouseEvent"></param>
		/// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
		public virtual bool OnMouseLeave (MouseEvent mouseEvent)
		{
			return false;
		}

		/// <summary>
		/// Method invoked when a view gets focus.
		/// </summary>
		/// <param name="view">The view that is losing focus.</param>
		/// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
		public virtual bool OnEnter (View view)
		{
			return false;
		}

		/// <summary>
		/// Method invoked when a view loses focus.
		/// </summary>
		/// <param name="view">The view that is getting focus.</param>
		/// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
		public virtual bool OnLeave (View view)
		{
			return false;
		}

		/// <summary>
		/// Method invoked when the <see cref="CanFocus"/> property from a view is changed.
		/// </summary>
		public virtual void OnCanFocusChanged () { }

		/// <summary>
		/// Method invoked when the <see cref="Enabled"/> property from a view is changed.
		/// </summary>
		public virtual void OnEnabledChanged () { }

		/// <summary>
		/// Method invoked when the <see cref="Visible"/> property from a view is changed.
		/// </summary>
		public virtual void OnVisibleChanged () { }

		/// <summary>
		/// Utilty function to determine <paramref name="method"/> is overridden in the <paramref name="subclass"/>.
		/// </summary>
		/// <param name="subclass">The view.</param>
		/// <param name="method">The method name.</param>
		/// <returns><see langword="true"/> if it's overridden, <see langword="false"/> otherwise.</returns>
		internal static bool IsOverridden (Responder subclass, string method)
		{
			MethodInfo m = subclass.GetType ().GetMethod (method,
				BindingFlags.Instance
				| BindingFlags.Public
				| BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly);
			if (m == null) {
				return false;
			}
			return m.GetBaseDefinition ().DeclaringType != m.DeclaringType;
		}
		
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <remarks>
		/// If disposing equals true, the method has been called directly
		/// or indirectly by a user's code. Managed and unmanaged resources
		/// can be disposed.
		/// If disposing equals false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference
		/// other objects. Only unmanaged resources can be disposed.		
		/// </remarks>
		/// <param name="disposing"></param>
		protected virtual void Dispose (bool disposing)
		{
			if (!disposedValue) {
				if (disposing) {
					// TODO: dispose managed state (managed objects)
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resource.
		/// </summary>
		public void Dispose ()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose (disposing: true);
			GC.SuppressFinalize (this);
#if DEBUG_IDISPOSABLE
			WasDisposed = true;
#endif
		}
	}
}
