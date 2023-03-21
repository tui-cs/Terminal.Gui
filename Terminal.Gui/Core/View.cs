using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// Determines the LayoutStyle for a <see cref="View"/>, if Absolute, during <see cref="View.LayoutSubviews"/>, the
	/// value from the <see cref="View.Frame"/> will be used, if the value is Computed, then <see cref="View.Frame"/>
	/// will be updated from the X, Y <see cref="Pos"/> objects and the Width and Height <see cref="Dim"/> objects.
	/// </summary>
	public enum LayoutStyle {
		/// <summary>
		/// The position and size of the view are based on the Frame value.
		/// </summary>
		Absolute,

		/// <summary>
		/// The position and size of the view will be computed based on the
		/// X, Y, Width and Height properties and set on the Frame.
		/// </summary>
		Computed
	}

	/// <summary>
	/// View is the base class for all views on the screen and represents a visible element that can render itself and 
	/// contains zero or more nested views.
	/// </summary>
	/// <remarks>
	/// <para>
	///    The View defines the base functionality for user interface elements in Terminal.Gui. Views
	///    can contain one or more subviews, can respond to user input and render themselves on the screen.
	/// </para>
	/// <para>
	///    Views supports two layout styles: <see cref="LayoutStyle.Absolute"/> or <see cref="LayoutStyle.Computed"/>. 
	///    The choice as to which layout style is used by the View 
	///    is determined when the View is initialized. To create a View using Absolute layout, call a constructor that takes a
	///    Rect parameter to specify the absolute position and size (the View.<see cref="View.Frame "/>). To create a View 
	///    using Computed layout use a constructor that does not take a Rect parameter and set the X, Y, Width and Height 
	///    properties on the view. Both approaches use coordinates that are relative to the container they are being added to. 
	/// </para>
	/// <para>
	///    To switch between Absolute and Computed layout, use the <see cref="LayoutStyle"/> property. 
	/// </para>
	/// <para>
	///    Computed layout is more flexible and supports dynamic console apps where controls adjust layout
	///    as the terminal resizes or other Views change size or position. The X, Y, Width and Height 
	///    properties are Dim and Pos objects that dynamically update the position of a view.
	///    The X and Y properties are of type <see cref="Pos"/>
	///    and you can use either absolute positions, percentages or anchor
	///    points. The Width and Height properties are of type
	///    <see cref="Dim"/> and can use absolute position,
	///    percentages and anchors. These are useful as they will take
	///    care of repositioning views when view's frames are resized or
	///    if the terminal size changes.
	/// </para>
	/// <para>
	///    Absolute layout requires specifying coordinates and sizes of Views explicitly, and the
	///    View will typically stay in a fixed position and size. To change the position and size use the
	///    <see cref="Frame"/> property.
	/// </para>
	/// <para>
	///    Subviews (child views) can be added to a View by calling the <see cref="Add(View)"/> method. 
	///    The container of a View can be accessed with the <see cref="SuperView"/> property.
	/// </para>
	/// <para>
	///    To flag a region of the View's <see cref="Bounds"/> to be redrawn call <see cref="SetNeedsDisplay(Rect)"/>. 
	///    To flag the entire view for redraw call <see cref="SetNeedsDisplay()"/>.
	/// </para>
	/// <para>
	///    Views have a <see cref="ColorScheme"/> property that defines the default colors that subviews
	///    should use for rendering. This ensures that the views fit in the context where
	///    they are being used, and allows for themes to be plugged in. For example, the
	///    default colors for windows and toplevels uses a blue background, while it uses
	///    a white background for dialog boxes and a red background for errors.
	/// </para>
	/// <para>
	///    Subclasses should not rely on <see cref="ColorScheme"/> being
	///    set at construction time. If a <see cref="ColorScheme"/> is not set on a view, the view will inherit the
	///    value from its <see cref="SuperView"/> and the value might only be valid once a view has been
	///    added to a SuperView. 
	/// </para>
	/// <para>
	///    By using  <see cref="ColorScheme"/> applications will work both
	///    in color as well as black and white displays.
	/// </para>
	/// <para>
	///    Views that are focusable should implement the <see cref="PositionCursor"/> to make sure that
	///    the cursor is placed in a location that makes sense. Unix terminals do not have
	///    a way of hiding the cursor, so it can be distracting to have the cursor left at
	///    the last focused view. So views should make sure that they place the cursor
	///    in a visually sensible place.
	/// </para>
	/// <para>
	///    The <see cref="LayoutSubviews"/> method is invoked when the size or layout of a view has
	///    changed. The default processing system will keep the size and dimensions
	///    for views that use the <see cref="LayoutStyle.Absolute"/>, and will recompute the
	///    frames for the vies that use <see cref="LayoutStyle.Computed"/>.
	/// </para>
	/// <para>
	///     Views can also opt-in to more sophisticated initialization
	///     by implementing overrides to <see cref="ISupportInitialize.BeginInit"/> and
	///     <see cref="ISupportInitialize.EndInit"/> which will be called
	///     when the view is added to a <see cref="SuperView"/>. 
	/// </para>
	/// <para>
	///     If first-run-only initialization is preferred, overrides to <see cref="ISupportInitializeNotification"/>
	///     can be implemented, in which case the <see cref="ISupportInitialize"/>
	///     methods will only be called if <see cref="ISupportInitializeNotification.IsInitialized"/>
	///     is <see langword="false"/>. This allows proper <see cref="View"/> inheritance hierarchies
	///     to override base class layout code optimally by doing so only on first run,
	///     instead of on every run.
	///   </para>
	/// </remarks>
	public partial class View : Responder, ISupportInitializeNotification {

		internal enum Direction {
			Forward,
			Backward
		}

		// container == SuperView
		View container = null;
		View focused = null;
		Direction focusDirection;
		bool autoSize;

		ShortcutHelper shortcutHelper;

		/// <summary>
		/// Event fired when this view is added to another.
		/// </summary>
		public event EventHandler<SuperViewChangedEventArgs> Added;

		/// <summary>
		/// Event fired when this view is removed from another.
		/// </summary>
		public event EventHandler<SuperViewChangedEventArgs> Removed;

		/// <summary>
		/// Event fired when the view gets focus.
		/// </summary>
		public event EventHandler<FocusEventArgs> Enter;

		/// <summary>
		/// Event fired when the view looses focus.
		/// </summary>
		public event EventHandler<FocusEventArgs> Leave;

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

		/// <summary>
		/// Event fired when the <see cref="CanFocus"/> value is being changed.
		/// </summary>
		public event EventHandler CanFocusChanged;

		/// <summary>
		/// Event fired when the <see cref="Enabled"/> value is being changed.
		/// </summary>
		public event EventHandler EnabledChanged;

		/// <summary>
		/// Event fired when the <see cref="Visible"/> value is being changed.
		/// </summary>
		public event EventHandler VisibleChanged;

		/// <summary>
		/// Event invoked when the <see cref="HotKey"/> is changed.
		/// </summary>
		public event EventHandler<KeyChangedEventArgs> HotKeyChanged;

		Key hotKey = Key.Null;

		/// <summary>
		/// Gets or sets the HotKey defined for this view. A user pressing HotKey on the keyboard while this view has focus will cause the Clicked event to fire.
		/// </summary>
		public virtual Key HotKey {
			get => hotKey;
			set {
				if (hotKey != value) {
					hotKey = TextFormatter.HotKey = (value == Key.Unknown ? Key.Null : value);
				}
			}
		}

		/// <summary>
		/// Gets or sets the specifier character for the hotkey (e.g. '_'). Set to '\xffff' to disable hotkey support for this View instance. The default is '\xffff'. 
		/// </summary>
		public virtual Rune HotKeySpecifier {
			get => TextFormatter.HotKeySpecifier;
			set {
				TextFormatter.HotKeySpecifier = value;
				SetHotKey ();
			}
		}

		/// <summary>
		/// This is the global setting that can be used as a global shortcut to invoke an action if provided.
		/// </summary>
		public Key Shortcut {
			get => shortcutHelper.Shortcut;
			set {
				if (shortcutHelper.Shortcut != value && (ShortcutHelper.PostShortcutValidation (value) || value == Key.Null)) {
					shortcutHelper.Shortcut = value;
				}
			}
		}

		/// <summary>
		/// The keystroke combination used in the <see cref="Shortcut"/> as string.
		/// </summary>
		public ustring ShortcutTag => ShortcutHelper.GetShortcutTag (shortcutHelper.Shortcut);

		/// <summary>
		/// The action to run if the <see cref="Shortcut"/> is defined.
		/// </summary>
		public virtual Action ShortcutAction { get; set; }

		/// <summary>
		/// Gets or sets arbitrary data for the view.
		/// </summary>
		/// <remarks>This property is not used internally.</remarks>
		public object Data { get; set; }

		internal Direction FocusDirection {
			get => SuperView?.FocusDirection ?? focusDirection;
			set {
				if (SuperView != null)
					SuperView.FocusDirection = value;
				else
					focusDirection = value;
			}
		}

		/// <summary>
		/// Points to the current driver in use by the view, it is a convenience property
		/// for simplifying the development of new views.
		/// </summary>
		public static ConsoleDriver Driver => Application.Driver;

		static readonly IList<View> empty = new List<View> (0).AsReadOnly ();

		// This is null, and allocated on demand.
		List<View> subviews;

		/// <summary>
		/// This returns a list of the subviews contained by this view.
		/// </summary>
		/// <value>The subviews.</value>
		public IList<View> Subviews => subviews?.AsReadOnly () ?? empty;

		// Internally, we use InternalSubviews rather than subviews, as we do not expect us
		// to make the same mistakes our users make when they poke at the Subviews.
		internal IList<View> InternalSubviews => subviews ?? empty;

		// This is null, and allocated on demand.
		List<View> tabIndexes;

		/// <summary>
		/// Configurable keybindings supported by the control
		/// </summary>
		private Dictionary<Key, Command []> KeyBindings { get; set; } = new Dictionary<Key, Command []> ();
		private Dictionary<Command, Func<bool?>> CommandImplementations { get; set; } = new Dictionary<Command, Func<bool?>> ();

		/// <summary>
		/// This returns a tab index list of the subviews contained by this view.
		/// </summary>
		/// <value>The tabIndexes.</value>
		public IList<View> TabIndexes => tabIndexes?.AsReadOnly () ?? empty;

		int tabIndex = -1;

		/// <summary>
		/// Indicates the index of the current <see cref="View"/> from the <see cref="TabIndexes"/> list.
		/// </summary>
		public int TabIndex {
			get { return tabIndex; }
			set {
				if (!CanFocus) {
					tabIndex = -1;
					return;
				} else if (SuperView?.tabIndexes == null || SuperView?.tabIndexes.Count == 1) {
					tabIndex = 0;
					return;
				} else if (tabIndex == value) {
					return;
				}
				tabIndex = value > SuperView.tabIndexes.Count - 1 ? SuperView.tabIndexes.Count - 1 : value < 0 ? 0 : value;
				tabIndex = GetTabIndex (tabIndex);
				if (SuperView.tabIndexes.IndexOf (this) != tabIndex) {
					SuperView.tabIndexes.Remove (this);
					SuperView.tabIndexes.Insert (tabIndex, this);
					SetTabIndex ();
				}
			}
		}

		int GetTabIndex (int idx)
		{
			var i = 0;
			foreach (var v in SuperView.tabIndexes) {
				if (v.tabIndex == -1 || v == this) {
					continue;
				}
				i++;
			}
			return Math.Min (i, idx);
		}

		void SetTabIndex ()
		{
			var i = 0;
			foreach (var v in SuperView.tabIndexes) {
				if (v.tabIndex == -1) {
					continue;
				}
				v.tabIndex = i;
				i++;
			}
		}

		bool tabStop = true;

		/// <summary>
		/// This only be <see langword="true"/> if the <see cref="CanFocus"/> is also <see langword="true"/> 
		/// and the focus can be avoided by setting this to <see langword="false"/>
		/// </summary>
		public bool TabStop {
			get => tabStop;
			set {
				if (tabStop == value) {
					return;
				}
				tabStop = CanFocus && value;
			}
		}

		bool oldCanFocus;
		int oldTabIndex;

		/// <inheritdoc/>
		public override bool CanFocus {
			get => base.CanFocus;
			set {
				if (!addingView && IsInitialized && SuperView?.CanFocus == false && value) {
					throw new InvalidOperationException ("Cannot set CanFocus to true if the SuperView CanFocus is false!");
				}
				if (base.CanFocus != value) {
					base.CanFocus = value;

					switch (value) {
					case false when tabIndex > -1:
						TabIndex = -1;
						break;
					case true when SuperView?.CanFocus == false && addingView:
						SuperView.CanFocus = true;
						break;
					}

					if (value && tabIndex == -1) {
						TabIndex = SuperView != null ? SuperView.tabIndexes.IndexOf (this) : -1;
					}
					TabStop = value;

					if (!value && SuperView?.Focused == this) {
						SuperView.focused = null;
					}
					if (!value && HasFocus) {
						SetHasFocus (false, this);
						SuperView?.EnsureFocus ();
						if (SuperView != null && SuperView.Focused == null) {
							SuperView.FocusNext ();
							if (SuperView.Focused == null) {
								Application.Current.FocusNext ();
							}
							Application.EnsuresTopOnFront ();
						}
					}
					if (subviews != null && IsInitialized) {
						foreach (var view in subviews) {
							if (view.CanFocus != value) {
								if (!value) {
									view.oldCanFocus = view.CanFocus;
									view.oldTabIndex = view.tabIndex;
									view.CanFocus = false;
									view.tabIndex = -1;
								} else {
									if (addingView) {
										view.addingView = true;
									}
									view.CanFocus = view.oldCanFocus;
									view.tabIndex = view.oldTabIndex;
									view.addingView = false;
								}
							}
						}
					}
					OnCanFocusChanged ();
					SetNeedsDisplay ();
				}
			}
		}

		internal Rect NeedDisplay { get; private set; } = Rect.Empty;

		// The frame for the object. Superview relative.
		Rect frame;

		/// <summary>
		/// Gets or sets an identifier for the view;
		/// </summary>
		/// <value>The identifier.</value>
		/// <remarks>The id should be unique across all Views that share a SuperView.</remarks>
		public ustring Id { get; set; } = "";

		/// <summary>
		/// Returns a value indicating if this View is currently on Top (Active)
		/// </summary>
		public bool IsCurrentTop => Application.Current == this;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="View"/> wants mouse position reports.
		/// </summary>
		/// <value><see langword="true"/> if want mouse position reports; otherwise, <see langword="false"/>.</value>
		public virtual bool WantMousePositionReports { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="View"/> want continuous button pressed event.
		/// </summary>
		public virtual bool WantContinuousButtonPressed { get; set; }

		/// <summary>
		/// Gets or sets the frame for the view. The frame is relative to the view's container (<see cref="SuperView"/>).
		/// </summary>
		/// <value>The frame.</value>
		/// <remarks>
		/// <para>
		///    Change the Frame when using the <see cref="Terminal.Gui.LayoutStyle.Absolute"/> layout style to move or resize views. 
		/// </para>
		/// <para>
		///    Altering the Frame of a view will trigger the redrawing of the
		///    view as well as the redrawing of the affected regions of the <see cref="SuperView"/>.
		/// </para>
		/// </remarks>
		public virtual Rect Frame {
			get => frame;
			set {
				frame = new Rect (value.X, value.Y, Math.Max (value.Width, 0), Math.Max (value.Height, 0));
				TextFormatter.Size = GetSizeNeededForTextAndHotKey ();
				SetNeedsLayout ();
				SetNeedsDisplay ();
			}
		}

		///// <summary>
		///// Gets an enumerator that enumerates the subviews in this view.
		///// </summary>
		///// <returns>The enumerator.</returns>
		//public IEnumerator GetEnumerator ()
		//{
		//	foreach (var v in InternalSubviews)
		//		yield return v;
		//}

		LayoutStyle layoutStyle;

		/// <summary>
		/// Controls how the View's <see cref="Frame"/> is computed during the LayoutSubviews method, if the style is set to
		/// <see cref="Terminal.Gui.LayoutStyle.Absolute"/>, 
		/// LayoutSubviews does not change the <see cref="Frame"/>. If the style is <see cref="Terminal.Gui.LayoutStyle.Computed"/>
		/// the <see cref="Frame"/> is updated using
		/// the <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties.
		/// </summary>
		/// <value>The layout style.</value>
		public LayoutStyle LayoutStyle {
			get => layoutStyle;
			set {
				layoutStyle = value;
				SetNeedsLayout ();
			}
		}

		/// <summary>
		/// The bounds represent the View-relative rectangle used for this view; the area inside of the view.
		/// </summary>
		/// <value>The bounds.</value>
		/// <remarks>
		/// <para>
		/// Updates to the Bounds update the <see cref="Frame"/>,
		/// and has the same side effects as updating the <see cref="Frame"/>.
		/// </para>
		/// <para>
		/// Because <see cref="Bounds"/> coordinates are relative to the upper-left corner of the <see cref="View"/>, 
		/// the coordinates of the upper-left corner of the rectangle returned by this property are (0,0). 
		/// Use this property to obtain the size and coordinates of the client area of the 
		/// control for tasks such as drawing on the surface of the control.
		/// </para>
		/// </remarks>
		public Rect Bounds {
			get => new Rect (Point.Empty, Frame.Size);
			set => Frame = new Rect (frame.Location, value.Size);
		}

		Pos x, y;

		/// <summary>
		/// Gets or sets the X position for the view (the column). Only used if the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The X Position.</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Pos X {
			get => x;
			set {
				if (ForceValidatePosDim && !ValidatePosDim (x, value)) {
					throw new ArgumentException ();
				}

				x = value;

				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// Gets or sets the Y position for the view (the row). Only used if the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The y position (line).</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Pos Y {
			get => y;
			set {
				if (ForceValidatePosDim && !ValidatePosDim (y, value)) {
					throw new ArgumentException ();
				}

				y = value;

				OnResizeNeeded ();
			}
		}
		Dim width, height;

		/// <summary>
		/// Gets or sets the width of the view. Only used the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The width.</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Dim Width {
			get => width;
			set {
				if (ForceValidatePosDim && !ValidatePosDim (width, value)) {
					throw new ArgumentException ("ForceValidatePosDim is enabled", nameof (Width));
				}

				width = value;

				if (ForceValidatePosDim) {
					var isValidNewAutSize = autoSize && IsValidAutoSizeWidth (width);

					if (IsAdded && autoSize && !isValidNewAutSize) {
						throw new InvalidOperationException ("Must set AutoSize to false before set the Width.");
					}
				}
				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// Gets or sets the height of the view. Only used the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The height.</value>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		public Dim Height {
			get => height;
			set {
				if (ForceValidatePosDim && !ValidatePosDim (height, value)) {
					throw new ArgumentException ("ForceValidatePosDim is enabled", nameof (Height));
				}

				height = value;

				if (ForceValidatePosDim) {
					var isValidNewAutSize = autoSize && IsValidAutoSizeHeight (height);

					if (IsAdded && autoSize && !isValidNewAutSize) {
						throw new InvalidOperationException ("Must set AutoSize to false before set the Height.");
					}
				}
				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// Forces validation with <see cref="Terminal.Gui.LayoutStyle.Computed"/> layout
		///  to avoid breaking the <see cref="Pos"/> and <see cref="Dim"/> settings.
		/// </summary>
		public bool ForceValidatePosDim { get; set; }

		bool ValidatePosDim (object oldValue, object newValue)
		{
			if (!IsInitialized || layoutStyle == LayoutStyle.Absolute || oldValue == null || oldValue.GetType () == newValue.GetType () || this is Toplevel) {
				return true;
			}
			if (layoutStyle == LayoutStyle.Computed) {
				if (oldValue.GetType () != newValue.GetType () && !(newValue is Pos.PosAbsolute || newValue is Dim.DimAbsolute)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets the minimum dimensions required to fit the View's <see cref="Text"/>, factoring in <see cref="TextDirection"/>.
		/// </summary>
		/// <param name="size">The minimum dimensions required.</param>
		/// <returns><see langword="true"/> if the dimensions fit within the View's <see cref="Bounds"/>, <see langword="false"/> otherwise.</returns>
		/// <remarks>
		/// Always returns <see langword="false"/> if <see cref="AutoSize"/> is <see langword="true"/> or
		/// if <see cref="Height"/> (Horizontal) or <see cref="Width"/> (Vertical) are not not set or zero.
		/// Does not take into account word wrapping.
		/// </remarks>
		public bool GetMinimumBounds (out Size size)
		{
			size = Size.Empty;

			if (!AutoSize && !ustring.IsNullOrEmpty (TextFormatter.Text)) {
				switch (TextFormatter.IsVerticalDirection (TextDirection)) {
				case true:
					var colWidth = TextFormatter.GetSumMaxCharWidth (new List<ustring> { TextFormatter.Text }, 0, 1);
					// TODO: v2 - This uses frame.Width; it should only use Bounds
					if (frame.Width < colWidth &&
						(Width == null ||
							(Bounds.Width >= 0 &&
								Width is Dim.DimAbsolute &&
								Width.Anchor (0) >= 0 &&
								Width.Anchor (0) < colWidth))) {
						size = new Size (colWidth, Bounds.Height);
						return true;
					}
					break;
				default:
					if (frame.Height < 1 &&
						(Height == null ||
							(Height is Dim.DimAbsolute &&
								Height.Anchor (0) == 0))) {
						size = new Size (Bounds.Width, 1);
						return true;
					}
					break;
				}
			}
			return false;
		}

		/// <summary>
		/// Sets the size of the View to the minimum width or height required to fit <see cref="Text"/> (see <see cref="GetMinimumBounds(out Size)"/>.
		/// </summary>
		/// <returns><see langword="true"/> if the size was changed, <see langword="false"/> if <see cref="Text"/>
		/// will not fit.</returns>
		public bool SetMinWidthHeight ()
		{
			if (GetMinimumBounds (out Size size)) {
				Bounds = new Rect (Bounds.Location, size);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Gets or sets the <see cref="Terminal.Gui.TextFormatter"/> which can be handled differently by any derived class.
		/// </summary>
		public TextFormatter TextFormatter { get; set; }

		/// <summary>
		/// Returns the container for this view, or null if this view has not been added to a container.
		/// </summary>
		/// <value>The super view.</value>
		public View SuperView => container;

		/// <summary>
		/// Initializes a new instance of a <see cref="Terminal.Gui.LayoutStyle.Absolute"/> <see cref="View"/> class with the absolute
		/// dimensions specified in the <see langword="frame"/> parameter. 
		/// </summary>
		/// <param name="frame">The region covered by this view.</param>
		/// <remarks>
		/// This constructor initialize a View with a <see cref="LayoutStyle"/> of <see cref="Terminal.Gui.LayoutStyle.Absolute"/>.
		/// Use <see cref="View"/> to initialize a View with  <see cref="LayoutStyle"/> of <see cref="Terminal.Gui.LayoutStyle.Computed"/> 
		/// </remarks>
		public View (Rect frame)
		{
			SetInitialProperties (ustring.Empty, frame, LayoutStyle.Absolute, TextDirection.LeftRight_TopBottom);
		}

		/// <summary>
		///   Initializes a new instance of <see cref="View"/> using <see cref="Terminal.Gui.LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   Use <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties to dynamically control the size and location of the view.
		///   The <see cref="View"/> will be created using <see cref="Terminal.Gui.LayoutStyle.Computed"/>
		///   coordinates. The initial size (<see cref="View.Frame"/>) will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   If <see cref="Height"/> is greater than one, word wrapping is provided.
		/// </para>
		/// <para>
		///   This constructor initialize a View with a <see cref="LayoutStyle"/> of <see cref="Terminal.Gui.LayoutStyle.Computed"/>. 
		///   Use <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties to dynamically control the size and location of the view.
		/// </para>
		/// </remarks>
		public View () : this (text: string.Empty, direction: TextDirection.LeftRight_TopBottom) { }

		/// <summary>
		///   Initializes a new instance of <see cref="View"/> using <see cref="Terminal.Gui.LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   The <see cref="View"/> will be created at the given
		///   coordinates with the given string. The size (<see cref="View.Frame"/>) will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   No line wrapping is provided.
		/// </para>
		/// </remarks>
		/// <param name="x">column to locate the View.</param>
		/// <param name="y">row to locate the View.</param>
		/// <param name="text">text to initialize the <see cref="Text"/> property with.</param>
		public View (int x, int y, ustring text) : this (TextFormatter.CalcRect (x, y, text), text) { }

		/// <summary>
		///   Initializes a new instance of <see cref="View"/> using <see cref="Terminal.Gui.LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   The <see cref="View"/> will be created at the given
		///   coordinates with the given string. The initial size (<see cref="View.Frame"/>) will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   If <c>rect.Height</c> is greater than one, word wrapping is provided.
		/// </para>
		/// </remarks>
		/// <param name="rect">Location.</param>
		/// <param name="text">text to initialize the <see cref="Text"/> property with.</param>
		/// <param name="border">The <see cref="Border"/>.</param>
		public View (Rect rect, ustring text, Border border = null)
		{
			SetInitialProperties (text, rect, LayoutStyle.Absolute, TextDirection.LeftRight_TopBottom, border);
		}

		/// <summary>
		///   Initializes a new instance of <see cref="View"/> using <see cref="Terminal.Gui.LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <remarks>
		/// <para>
		///   The <see cref="View"/> will be created using <see cref="Terminal.Gui.LayoutStyle.Computed"/>
		///   coordinates with the given string. The initial size (<see cref="View.Frame"/>) will be 
		///   adjusted to fit the contents of <see cref="Text"/>, including newlines ('\n') for multiple lines. 
		/// </para>
		/// <para>
		///   If <see cref="Height"/> is greater than one, word wrapping is provided.
		/// </para>
		/// </remarks>
		/// <param name="text">text to initialize the <see cref="Text"/> property with.</param>
		/// <param name="direction">The text direction.</param>
		/// <param name="border">The <see cref="Border"/>.</param>
		public View (ustring text, TextDirection direction = TextDirection.LeftRight_TopBottom, Border border = null)
		{
			SetInitialProperties (text, Rect.Empty, LayoutStyle.Computed, direction, border);
		}

		// TODO: v2 - Remove constructors with parameters
		/// <summary>
		/// Private helper to set the initial properties of the View that were provided via constructors.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="rect"></param>
		/// <param name="layoutStyle"></param>
		/// <param name="direction"></param>
		/// <param name="border"></param>
		void SetInitialProperties (ustring text, Rect rect, LayoutStyle layoutStyle = LayoutStyle.Computed,
		    TextDirection direction = TextDirection.LeftRight_TopBottom, Border border = null)
		{

			TextFormatter = new TextFormatter ();
			TextFormatter.HotKeyChanged += TextFormatter_HotKeyChanged;
			TextDirection = direction;
			shortcutHelper = new ShortcutHelper ();
			CanFocus = false;
			TabIndex = -1;
			TabStop = false;
			LayoutStyle = layoutStyle;
			// BUGBUG: CalcRect doesn't account for line wrapping

			// TODO: Remove this once v2's new Frames is ready
			Border = border;
			if (Border != null) {
				Border.Child = this;
			}

			var r = rect.IsEmpty ? TextFormatter.CalcRect (0, 0, text, direction) : rect;
			Frame = r;

			Text = text;

			OnResizeNeeded ();
		}

		/// <summary>
		/// Can be overridden if the <see cref="Terminal.Gui.TextFormatter.Text"/> has
		///  different format than the default.
		/// </summary>
		protected virtual void UpdateTextFormatterText ()
		{
			TextFormatter.Text = text;
		}

		/// <summary>
		/// Called whenever the view needs to be resized. 
		/// Can be overridden if the view resize behavior is
		///  different than the default.
		/// </summary>
		protected virtual void OnResizeNeeded ()
		{
			var actX = x is Pos.PosAbsolute ? x.Anchor (0) : frame.X;
			var actY = y is Pos.PosAbsolute ? y.Anchor (0) : frame.Y;

			if (AutoSize) {
				var s = GetAutoSize ();
				var w = width is Dim.DimAbsolute && width.Anchor (0) > s.Width ? width.Anchor (0) : s.Width;
				var h = height is Dim.DimAbsolute && height.Anchor (0) > s.Height ? height.Anchor (0) : s.Height;
				frame = new Rect (new Point (actX, actY), new Size (w, h));
			} else {
				var w = width is Dim.DimAbsolute ? width.Anchor (0) : frame.Width;
				var h = height is Dim.DimAbsolute ? height.Anchor (0) : frame.Height;
				frame = new Rect (new Point (actX, actY), new Size (w, h));
				SetMinWidthHeight ();
			}
			// BUGBUG: I think these calls are redundant or should be moved into just the AutoSize case
			TextFormatter.Size = GetSizeNeededForTextAndHotKey ();
			SetNeedsLayout ();
			SetNeedsDisplay ();
		}

		void TextFormatter_HotKeyChanged (object sender, KeyChangedEventArgs e)
		{
			HotKeyChanged?.Invoke (this, e);
		}

		/// <summary>
		/// Sets a flag indicating this view needs to be redisplayed because its state has changed.
		/// </summary>
		public void SetNeedsDisplay ()
		{
			SetNeedsDisplay (Bounds);
		}

		internal bool LayoutNeeded { get; private set; } = true;

		internal void SetNeedsLayout ()
		{
			if (LayoutNeeded)
				return;
			LayoutNeeded = true;
			if (SuperView == null)
				return;
			SuperView.SetNeedsLayout ();
			foreach (var view in Subviews) {
				view.SetNeedsLayout ();
			}
			TextFormatter.NeedsFormat = true;
		}

		/// <summary>
		/// Removes the <see cref="SetNeedsLayout"/> setting on this view.
		/// </summary>
		protected void ClearLayoutNeeded ()
		{
			LayoutNeeded = false;
		}

		/// <summary>
		/// Flags the view-relative region on this View as needing to be repainted.
		/// </summary>
		/// <param name="region">The view-relative region that must be flagged for repaint.</param>
		public void SetNeedsDisplay (Rect region)
		{
			if (NeedDisplay.IsEmpty)
				NeedDisplay = region;
			else {
				var x = Math.Min (NeedDisplay.X, region.X);
				var y = Math.Min (NeedDisplay.Y, region.Y);
				var w = Math.Max (NeedDisplay.Width, region.Width);
				var h = Math.Max (NeedDisplay.Height, region.Height);
				NeedDisplay = new Rect (x, y, w, h);
			}
			container?.SetSubViewNeedsDisplay ();

			if (subviews == null)
				return;

			foreach (var view in subviews)
				if (view.Frame.IntersectsWith (region)) {
					var childRegion = Rect.Intersect (view.Frame, region);
					childRegion.X -= view.Frame.X;
					childRegion.Y -= view.Frame.Y;
					view.SetNeedsDisplay (childRegion);
				}
		}

		internal bool ChildNeedsDisplay { get; private set; }

		/// <summary>
		/// Indicates that any child views (in the <see cref="Subviews"/> list) need to be repainted.
		/// </summary>
		public void SetSubViewNeedsDisplay ()
		{
			ChildNeedsDisplay = true;
			if (container != null)
				container.SetSubViewNeedsDisplay ();
		}

		internal bool addingView;

		/// <summary>
		///   Adds a subview (child) to this view.
		/// </summary>
		/// <remarks>
		/// The Views that have been added to this view can be retrieved via the <see cref="Subviews"/> property. 
		/// See also <seealso cref="Remove(View)"/> <seealso cref="RemoveAll"/> 
		/// </remarks>
		public virtual void Add (View view)
		{
			if (view == null)
				return;
			if (subviews == null) {
				subviews = new List<View> ();
			}
			if (tabIndexes == null) {
				tabIndexes = new List<View> ();
			}
			subviews.Add (view);
			tabIndexes.Add (view);
			view.container = this;
			if (view.CanFocus) {
				addingView = true;
				if (SuperView?.CanFocus == false) {
					SuperView.addingView = true;
					SuperView.CanFocus = true;
					SuperView.addingView = false;
				}
				CanFocus = true;
				view.tabIndex = tabIndexes.IndexOf (view);
				addingView = false;
			}
			if (view.Enabled && !Enabled) {
				view.oldEnabled = true;
				view.Enabled = false;
			}
			SetNeedsLayout ();
			SetNeedsDisplay ();
      
			OnAdded (new SuperViewChangedEventArgs (this, view));
			if (IsInitialized && !view.IsInitialized) {
				view.BeginInit ();
				view.EndInit ();
			}
		}

		/// <summary>
		/// Adds the specified views (children) to the view.
		/// </summary>
		/// <param name="views">Array of one or more views (can be optional parameter).</param>
		/// <remarks>
		/// The Views that have been added to this view can be retrieved via the <see cref="Subviews"/> property. 
		/// See also <seealso cref="Remove(View)"/> <seealso cref="RemoveAll"/> 
		/// </remarks>
		public void Add (params View [] views)
		{
			if (views == null)
				return;
			foreach (var view in views)
				Add (view);
		}

		/// <summary>
		///   Removes all subviews (children) added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
		/// </summary>
		public virtual void RemoveAll ()
		{
			if (subviews == null)
				return;

			while (subviews.Count > 0) {
				Remove (subviews [0]);
			}
		}

		/// <summary>
		///   Removes a subview added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public virtual void Remove (View view)
		{
			if (view == null || subviews == null)
				return;

			var touched = view.Frame;
			subviews.Remove (view);
			tabIndexes.Remove (view);
			view.container = null;
			view.tabIndex = -1;
			SetNeedsLayout ();
			SetNeedsDisplay ();
			if (subviews.Count < 1) {
				CanFocus = false;
			}
			foreach (var v in subviews) {
				if (v.Frame.IntersectsWith (touched))
					view.SetNeedsDisplay ();
			}
			OnRemoved (new SuperViewChangedEventArgs (this, view));
			if (focused == view) {
				focused = null;
			}
		}

		void PerformActionForSubview (View subview, Action<View> action)
		{
			if (subviews.Contains (subview)) {
				action (subview);
			}

			SetNeedsDisplay ();
			subview.SetNeedsDisplay ();
		}

		/// <summary>
		/// Brings the specified subview to the front so it is drawn on top of any other views.
		/// </summary>
		/// <param name="subview">The subview to send to the front</param>
		/// <remarks>
		///   <seealso cref="SendSubviewToBack"/>.
		/// </remarks>
		public void BringSubviewToFront (View subview)
		{
			PerformActionForSubview (subview, x => {
				subviews.Remove (x);
				subviews.Add (x);
			});
		}

		/// <summary>
		/// Sends the specified subview to the front so it is the first view drawn
		/// </summary>
		/// <param name="subview">The subview to send to the front</param>
		/// <remarks>
		///   <seealso cref="BringSubviewToFront(View)"/>.
		/// </remarks>
		public void SendSubviewToBack (View subview)
		{
			PerformActionForSubview (subview, x => {
				subviews.Remove (x);
				subviews.Insert (0, subview);
			});
		}

		/// <summary>
		/// Moves the subview backwards in the hierarchy, only one step
		/// </summary>
		/// <param name="subview">The subview to send backwards</param>
		/// <remarks>
		/// If you want to send the view all the way to the back use SendSubviewToBack.
		/// </remarks>
		public void SendSubviewBackwards (View subview)
		{
			PerformActionForSubview (subview, x => {
				var idx = subviews.IndexOf (x);
				if (idx > 0) {
					subviews.Remove (x);
					subviews.Insert (idx - 1, x);
				}
			});
		}

		/// <summary>
		/// Moves the subview backwards in the hierarchy, only one step
		/// </summary>
		/// <param name="subview">The subview to send backwards</param>
		/// <remarks>
		/// If you want to send the view all the way to the back use SendSubviewToBack.
		/// </remarks>
		public void BringSubviewForward (View subview)
		{
			PerformActionForSubview (subview, x => {
				var idx = subviews.IndexOf (x);
				if (idx + 1 < subviews.Count) {
					subviews.Remove (x);
					subviews.Insert (idx + 1, x);
				}
			});
		}

		/// <summary>
		///   Clears the view region with the current color.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This clears the entire region used by this view.
		///   </para>
		/// </remarks>
		public void Clear ()
		{
			var h = Frame.Height;
			var w = Frame.Width;
			for (var line = 0; line < h; line++) {
				Move (0, line);
				for (var col = 0; col < w; col++)
					Driver.AddRune (' ');
			}
		}

		/// <summary>
		///   Clears the specified region with the current color. 
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="regionScreen">The screen-relative region to clear.</param>
		public void Clear (Rect regionScreen)
		{
			var h = regionScreen.Height;
			var w = regionScreen.Width;
			for (var line = regionScreen.Y; line < regionScreen.Y + h; line++) {
				Driver.Move (regionScreen.X, line);
				for (var col = 0; col < w; col++)
					Driver.AddRune (' ');
			}
		}

		/// <summary>
		/// Converts a view-relative (col,row) position to a screen-relative position (col,row). The values are optionally clamped to the screen dimensions.
		/// </summary>
		/// <param name="col">View-relative column.</param>
		/// <param name="row">View-relative row.</param>
		/// <param name="rcol">Absolute column; screen-relative.</param>
		/// <param name="rrow">Absolute row; screen-relative.</param>
		/// <param name="clipped">Whether to clip the result of the ViewToScreen method, if set to <see langword="true"/>, the rcol, rrow values are clamped to the screen (terminal) dimensions (0..TerminalDim-1).</param>
		public void ViewToScreen (int col, int row, out int rcol, out int rrow, bool clipped = true)
		{
			// Computes the real row, col relative to the screen.
			rrow = row + frame.Y;
			rcol = col + frame.X;

			var curContainer = container;
			while (curContainer != null) {
				rrow += curContainer.frame.Y;
				rcol += curContainer.frame.X;
				curContainer = curContainer.container;
			}

			// The following ensures that the cursor is always in the screen boundaries.
			if (clipped) {
				rrow = Math.Min (rrow, Driver.Rows - 1);
				rcol = Math.Min (rcol, Driver.Cols - 1);
			}
		}

		/// <summary>
		/// Converts a point from screen-relative coordinates to view-relative coordinates.
		/// </summary>
		/// <returns>The mapped point.</returns>
		/// <param name="x">X screen-coordinate point.</param>
		/// <param name="y">Y screen-coordinate point.</param>
		public Point ScreenToView (int x, int y)
		{
			if (SuperView == null) {
				return new Point (x - Frame.X, y - frame.Y);
			} else {
				var parent = SuperView.ScreenToView (x, y);
				return new Point (parent.X - frame.X, parent.Y - frame.Y);
			}
		}

		/// <summary>
		/// Converts a region in view-relative coordinates to screen-relative coordinates.
		/// </summary>
		internal Rect ViewToScreen (Rect region)
		{
			ViewToScreen (region.X, region.Y, out var x, out var y, clipped: false);
			return new Rect (x, y, region.Width, region.Height);
		}

		// Clips a rectangle in screen coordinates to the dimensions currently available on the screen
		internal Rect ScreenClip (Rect regionScreen)
		{
			var x = regionScreen.X < 0 ? 0 : regionScreen.X;
			var y = regionScreen.Y < 0 ? 0 : regionScreen.Y;
			var w = regionScreen.X + regionScreen.Width >= Driver.Cols ? Driver.Cols - regionScreen.X : regionScreen.Width;
			var h = regionScreen.Y + regionScreen.Height >= Driver.Rows ? Driver.Rows - regionScreen.Y : regionScreen.Height;

			return new Rect (x, y, w, h);
		}

		/// <summary>
		/// Sets the <see cref="ConsoleDriver"/>'s clip region to the current View's <see cref="Bounds"/>.
		/// </summary>
		/// <returns>The existing driver's clip region, which can be then re-applied by setting <c><see cref="Driver"/>.Clip</c> (<see cref="ConsoleDriver.Clip"/>).</returns>
		/// <remarks>
		/// <see cref="Bounds"/> is View-relative.
		/// </remarks>
		public Rect ClipToBounds ()
		{
			return SetClip (Bounds);
		}

		/// <summary>
		/// Sets the clip region to the specified view-relative region.
		/// </summary>
		/// <returns>The previous screen-relative clip region.</returns>
		/// <param name="region">View-relative clip region.</param>
		public Rect SetClip (Rect region)
		{
			var previous = Driver.Clip;
			Driver.Clip = Rect.Intersect (previous, ViewToScreen (region));
			return previous;
		}

		/// <summary>
		/// Draws a frame in the current view, clipped by the boundary of this view
		/// </summary>
		/// <param name="region">View-relative region for the frame to be drawn.</param>
		/// <param name="padding">The padding to add around the outside of the drawn frame.</param>
		/// <param name="fill">If set to <see langword="true"/> it fill will the contents.</param>
		public void DrawFrame (Rect region, int padding = 0, bool fill = false)
		{
			var scrRect = ViewToScreen (region);
			var savedClip = ClipToBounds ();
			Driver.DrawWindowFrame (scrRect, padding + 1, padding + 1, padding + 1, padding + 1, border: true, fill: fill);
			Driver.Clip = savedClip;
		}

		/// <summary>
		/// Utility function to draw strings that contain a hotkey.
		/// </summary>
		/// <param name="text">String to display, the hotkey specifier before a letter flags the next letter as the hotkey.</param>
		/// <param name="hotColor">Hot color.</param>
		/// <param name="normalColor">Normal color.</param>
		/// <remarks>
		/// <para>The hotkey is any character following the hotkey specifier, which is the underscore ('_') character by default.</para>
		/// <para>The hotkey specifier can be changed via <see cref="HotKeySpecifier"/></para>
		/// </remarks>
		public void DrawHotString (ustring text, Attribute hotColor, Attribute normalColor)
		{
			var hotkeySpec = HotKeySpecifier == (Rune)0xffff ? (Rune)'_' : HotKeySpecifier;
			Application.Driver.SetAttribute (normalColor);
			foreach (var rune in text) {
				if (rune == hotkeySpec) {
					Application.Driver.SetAttribute (hotColor);
					continue;
				}
				Application.Driver.AddRune (rune);
				Application.Driver.SetAttribute (normalColor);
			}
		}

		/// <summary>
		/// Utility function to draw strings that contains a hotkey using a <see cref="ColorScheme"/> and the "focused" state.
		/// </summary>
		/// <param name="text">String to display, the underscore before a letter flags the next letter as the hotkey.</param>
		/// <param name="focused">If set to <see langword="true"/> this uses the focused colors from the color scheme, otherwise the regular ones.</param>
		/// <param name="scheme">The color scheme to use.</param>
		public void DrawHotString (ustring text, bool focused, ColorScheme scheme)
		{
			if (focused)
				DrawHotString (text, scheme.HotFocus, scheme.Focus);
			else
				DrawHotString (text, Enabled ? scheme.HotNormal : scheme.Disabled, Enabled ? scheme.Normal : scheme.Disabled);
		}

		/// <summary>
		/// This moves the cursor to the specified column and row in the view.
		/// </summary>
		/// <returns>The move.</returns>
		/// <param name="col">Col.</param>
		/// <param name="row">Row.</param>
		/// <param name="clipped">Whether to clip the result of the ViewToScreen method,
		///  if set to <see langword="true"/>, the col, row values are clamped to the screen (terminal) dimensions (0..TerminalDim-1).</param>
		public void Move (int col, int row, bool clipped = true)
		{
			if (Driver.Rows == 0) {
				return;
			}

			ViewToScreen (col, row, out var rCol, out var rRow, clipped);
			Driver.Move (rCol, rRow);
		}

		/// <summary>
		///   Positions the cursor in the right position based on the currently focused view in the chain.
		/// </summary>
		///    Views that are focusable should override <see cref="PositionCursor"/> to ensure
		///    the cursor is placed in a location that makes sense. Unix terminals do not have
		///    a way of hiding the cursor, so it can be distracting to have the cursor left at
		///    the last focused view. Views should make sure that they place the cursor
		///    in a visually sensible place.
		public virtual void PositionCursor ()
		{
			if (!CanBeVisible (this) || !Enabled) {
				return;
			}

			if (focused == null && SuperView != null) {
				SuperView.EnsureFocus ();
			} else if (focused?.Visible == true && focused?.Enabled == true && focused?.Frame.Width > 0 && focused.Frame.Height > 0) {
				focused.PositionCursor ();
			} else if (focused?.Visible == true && focused?.Enabled == false) {
				focused = null;
			} else if (CanFocus && HasFocus && Visible && Frame.Width > 0 && Frame.Height > 0) {
				Move (TextFormatter.HotKeyPos == -1 ? 0 : TextFormatter.CursorPosition, 0);
			} else {
				Move (frame.X, frame.Y);
			}
		}

		bool hasFocus;

		/// <inheritdoc/>
		public override bool HasFocus => hasFocus;

		void SetHasFocus (bool value, View view, bool force = false)
		{
			if (hasFocus != value || force) {
				hasFocus = value;
				if (value) {
					OnEnter (view);
				} else {
					OnLeave (view);
				}
				SetNeedsDisplay ();
			}

			// Remove focus down the chain of subviews if focus is removed
			if (!value && focused != null) {
				var f = focused;
				f.OnLeave (view);
				f.SetHasFocus (false, view);
				focused = null;
			}
		}

		/// <summary>
		/// Method invoked when a subview is being added to this view.
		/// </summary>
		/// <param name="e">Event where <see cref="ViewEventArgs.View"/> is the subview being added.</param>
		public virtual void OnAdded (SuperViewChangedEventArgs e)
		{
			var view = e.Child;
			view.IsAdded = true;
			view.x ??= view.frame.X;
			view.y ??= view.frame.Y;
			view.width ??= view.frame.Width;
			view.height ??= view.frame.Height;

			view.Added?.Invoke (this, e);
		}

		/// <summary>
		/// Method invoked when a subview is being removed from this view.
		/// </summary>
		/// <param name="e">Event args describing the subview being removed.</param>
		public virtual void OnRemoved (SuperViewChangedEventArgs e)
		{
			var view = e.Child;
			view.IsAdded = false;
			view.Removed?.Invoke (this, e);
		}

		/// <inheritdoc/>
		public override bool OnEnter (View view)
		{
			var args = new FocusEventArgs (view);
			Enter?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (base.OnEnter (view))
				return true;

			return false;
		}

		/// <inheritdoc/>
		public override bool OnLeave (View view)
		{
			var args = new FocusEventArgs (view);
			Leave?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (base.OnLeave (view))
				return true;

			return false;
		}

		/// <summary>
		/// Returns the currently focused view inside this view, or null if nothing is focused.
		/// </summary>
		/// <value>The focused.</value>
		public View Focused => focused;

		/// <summary>
		/// Returns the most focused view in the chain of subviews (the leaf view that has the focus).
		/// </summary>
		/// <value>The most focused View.</value>
		public View MostFocused {
			get {
				if (Focused == null)
					return null;
				var most = Focused.MostFocused;
				if (most != null)
					return most;
				return Focused;
			}
		}

		ColorScheme colorScheme;

		/// <summary>
		/// The color scheme for this view, if it is not defined, it returns the <see cref="SuperView"/>'s
		/// color scheme.
		/// </summary>
		public virtual ColorScheme ColorScheme {
			get {
				if (colorScheme == null) {
					return SuperView?.ColorScheme;
				}
				return colorScheme;
			}
			set {
				if (colorScheme != value) {
					colorScheme = value;
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Displays the specified character in the specified column and row of the View.
		/// </summary>
		/// <param name="col">Column (view-relative).</param>
		/// <param name="row">Row (view-relative).</param>
		/// <param name="ch">Ch.</param>
		public void AddRune (int col, int row, Rune ch)
		{
			if (row < 0 || col < 0)
				return;
			if (row > frame.Height - 1 || col > frame.Width - 1)
				return;
			Move (col, row);
			Driver.AddRune (ch);
		}

		/// <summary>
		/// Removes the <see cref="SetNeedsDisplay()"/> and the <see cref="ChildNeedsDisplay"/> setting on this view.
		/// </summary>
		protected void ClearNeedsDisplay ()
		{
			NeedDisplay = Rect.Empty;
			ChildNeedsDisplay = false;
		}

		/// <summary>
		/// Redraws this view and its subviews; only redraws the views that have been flagged for a re-display.
		/// </summary>
		/// <param name="bounds">The bounds (view-relative region) to redraw.</param>
		/// <remarks>
		/// <para>
		///    Always use <see cref="Bounds"/> (view-relative) when calling <see cref="Redraw(Rect)"/>, NOT <see cref="Frame"/> (superview-relative).
		/// </para>
		/// <para>
		///    Views should set the color that they want to use on entry, as otherwise this will inherit
		///    the last color that was set globally on the driver.
		/// </para>
		/// <para>
		///    Overrides of <see cref="Redraw"/> must ensure they do not set <c>Driver.Clip</c> to a clip region
		///    larger than the <ref name="bounds"/> parameter, as this will cause the driver to clip the entire region.
		/// </para>
		/// </remarks>
		public virtual void Redraw (Rect bounds)
		{
			if (!CanBeVisible (this)) {
				return;
			}

			var clipRect = new Rect (Point.Empty, frame.Size);

			if (ColorScheme != null) {
				Driver.SetAttribute (HasFocus ? GetFocusColor () : GetNormalColor ());
			}

			var boundsAdjustedForBorder = Bounds;
			if (!IgnoreBorderPropertyOnRedraw && Border != null) {
				Border.DrawContent (this);
			} else if (ustring.IsNullOrEmpty (TextFormatter.Text) &&
				(GetType ().IsNestedPublic && !IsOverridden (this, "Redraw") || GetType ().Name == "View") &&
				(!NeedDisplay.IsEmpty || ChildNeedsDisplay || LayoutNeeded)) {

				Clear ();
				SetSubViewNeedsDisplay ();
			}

			if (!ustring.IsNullOrEmpty (TextFormatter.Text)) {
				Rect containerBounds = GetContainerBounds ();
				if (!containerBounds.IsEmpty) {
					Clear (GetNeedDisplay (containerBounds));
					SetSubViewNeedsDisplay ();
					// Draw any Text
					if (TextFormatter != null) {
						TextFormatter.NeedsFormat = true;
					}
					TextFormatter?.Draw (ViewToScreen (Bounds), HasFocus ? GetFocusColor () : GetNormalColor (),
					    HasFocus ? ColorScheme.HotFocus : GetHotNormalColor (),
					    containerBounds);
				}
			}

			// Invoke DrawContentEvent
			OnDrawContent (boundsAdjustedForBorder);

			if (subviews != null) {
				foreach (var view in subviews) {
					if (!view.NeedDisplay.IsEmpty || view.ChildNeedsDisplay || view.LayoutNeeded) {
						if (view.Frame.IntersectsWith (clipRect) && (view.Frame.IntersectsWith (bounds) || bounds.X < 0 || bounds.Y < 0)) {
							if (view.LayoutNeeded) {
								view.LayoutSubviews ();
							}

							// Draw the subview
							// Use the view's bounds (view-relative; Location will always be (0,0)
							if (view.Visible && view.Frame.Width > 0 && view.Frame.Height > 0) {
								var rect = view.Bounds;
								view.OnDrawContent (rect);
								view.Redraw (rect);
								view.OnDrawContentComplete (rect);
							}
						}
						view.NeedDisplay = Rect.Empty;
						view.ChildNeedsDisplay = false;
					}
				}
			}

			// Invoke DrawContentCompleteEvent
			OnDrawContentComplete (boundsAdjustedForBorder);

			ClearLayoutNeeded ();
			ClearNeedsDisplay ();
		}

		Rect GetNeedDisplay (Rect containerBounds)
		{
			Rect rect = ViewToScreen (NeedDisplay);
			if (!containerBounds.IsEmpty) {
				rect.Width = Math.Min (NeedDisplay.Width, containerBounds.Width);
				rect.Height = Math.Min (NeedDisplay.Height, containerBounds.Height);
			}

			return rect;
		}

		Rect GetContainerBounds ()
		{
			var containerBounds = SuperView == null ? default : SuperView.ViewToScreen (SuperView.Bounds);
			var driverClip = Driver == null ? Rect.Empty : Driver.Clip;
			containerBounds.X = Math.Max (containerBounds.X, driverClip.X);
			containerBounds.Y = Math.Max (containerBounds.Y, driverClip.Y);
			var lenOffset = (driverClip.X + driverClip.Width) - (containerBounds.X + containerBounds.Width);
			if (containerBounds.X + containerBounds.Width > driverClip.X + driverClip.Width) {
				containerBounds.Width = Math.Max (containerBounds.Width + lenOffset, 0);
			} else {
				containerBounds.Width = Math.Min (containerBounds.Width, driverClip.Width);
			}
			lenOffset = (driverClip.Y + driverClip.Height) - (containerBounds.Y + containerBounds.Height);
			if (containerBounds.Y + containerBounds.Height > driverClip.Y + driverClip.Height) {
				containerBounds.Height = Math.Max (containerBounds.Height + lenOffset, 0);
			} else {
				containerBounds.Height = Math.Min (containerBounds.Height, driverClip.Height);
			}
			return containerBounds;
		}

		/// <summary>
		/// Event invoked when the content area of the View is to be drawn.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Will be invoked before any subviews added with <see cref="Add(View)"/> have been drawn.
		/// </para>
		/// <para>
		/// Rect provides the view-relative rectangle describing the currently visible viewport into the <see cref="View"/>.
		/// </para>
		/// </remarks>
		public event EventHandler<DrawEventArgs> DrawContent;

		/// <summary>
		/// Enables overrides to draw infinitely scrolled content and/or a background behind added controls. 
		/// </summary>
		/// <param name="viewport">The view-relative rectangle describing the currently visible viewport into the <see cref="View"/></param>
		/// <remarks>
		/// This method will be called before any subviews added with <see cref="Add(View)"/> have been drawn. 
		/// </remarks>
		public virtual void OnDrawContent (Rect viewport)
		{
			DrawContent?.Invoke (this, new DrawEventArgs (viewport));
		}

		/// <summary>
		/// Event invoked when the content area of the View is completed drawing.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Will be invoked after any subviews removed with <see cref="Remove(View)"/> have been completed drawing.
		/// </para>
		/// <para>
		/// Rect provides the view-relative rectangle describing the currently visible viewport into the <see cref="View"/>.
		/// </para>
		/// </remarks>
		public event EventHandler<DrawEventArgs> DrawContentComplete;

		/// <summary>
		/// Enables overrides after completed drawing infinitely scrolled content and/or a background behind removed controls.
		/// </summary>
		/// <param name="viewport">The view-relative rectangle describing the currently visible viewport into the <see cref="View"/></param>
		/// <remarks>
		/// This method will be called after any subviews removed with <see cref="Remove(View)"/> have been completed drawing.
		/// </remarks>
		public virtual void OnDrawContentComplete (Rect viewport)
		{
			DrawContentComplete?.Invoke (this, new DrawEventArgs (viewport));
		}

		/// <summary>
		/// Causes the specified subview to have focus.
		/// </summary>
		/// <param name="view">View.</param>
		void SetFocus (View view)
		{
			if (view == null)
				return;
			//Console.WriteLine ($"Request to focus {view}");
			if (!view.CanFocus || !view.Visible || !view.Enabled)
				return;
			if (focused?.hasFocus == true && focused == view)
				return;

			// Make sure that this view is a subview
			View c;
			for (c = view.container; c != null; c = c.container)
				if (c == this)
					break;
			if (c == null)
				throw new ArgumentException ("the specified view is not part of the hierarchy of this view");

			if (focused != null)
				focused.SetHasFocus (false, view);

			var f = focused;
			focused = view;
			focused.SetHasFocus (true, f);
			focused.EnsureFocus ();

			// Send focus upwards
			SuperView?.SetFocus (this);
		}

		/// <summary>
		/// Causes the specified view and the entire parent hierarchy to have the focused order updated.
		/// </summary>
		public void SetFocus ()
		{
			if (!CanBeVisible (this) || !Enabled) {
				if (HasFocus) {
					SetHasFocus (false, this);
				}
				return;
			}

			SuperView?.SetFocus (this);
		}

		/// <summary>
		/// Invoked when a character key is pressed and occurs after the key up event.
		/// </summary>
		public event EventHandler<KeyEventEventArgs> KeyPress;

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyPress?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (Focused?.Enabled == true) {
				Focused?.KeyPress?.Invoke (this, args);
				if (args.Handled)
					return true;
			}

			return Focused?.Enabled == true && Focused?.ProcessKey (keyEvent) == true;
		}

		/// <summary>
		/// Invokes any binding that is registered on this <see cref="View"/>
		/// and matches the <paramref name="keyEvent"/>
		/// </summary>
		/// <param name="keyEvent">The key event passed.</param>
		protected bool? InvokeKeybindings (KeyEvent keyEvent)
		{
			bool? toReturn = null;

			if (KeyBindings.ContainsKey (keyEvent.Key)) {

				foreach (var command in KeyBindings [keyEvent.Key]) {

					if (!CommandImplementations.ContainsKey (command)) {
						throw new NotSupportedException ($"A KeyBinding was set up for the command {command} ({keyEvent.Key}) but that command is not supported by this View ({GetType ().Name})");
					}

					// each command has its own return value
					var thisReturn = CommandImplementations [command] ();

					// if we haven't got anything yet, the current command result should be used
					if (toReturn == null) {
						toReturn = thisReturn;
					}

					// if ever see a true then that's what we will return
					if (thisReturn ?? false) {
						toReturn = true;
					}
				}
			}

			return toReturn;
		}


		/// <summary>
		/// <para>Adds a new key combination that will trigger the given <paramref name="command"/>
		/// (if supported by the View - see <see cref="GetSupportedCommands"/>)
		/// </para>
		/// <para>If the key is already bound to a different <see cref="Command"/> it will be
		/// rebound to this one</para>
		/// <remarks>Commands are only ever applied to the current <see cref="View"/>(i.e. this feature
		/// cannot be used to switch focus to another view and perform multiple commands there) </remarks>
		/// </summary>
		/// <param name="key"></param>
		/// <param name="command">The command(s) to run on the <see cref="View"/> when <paramref name="key"/> is pressed.
		/// When specifying multiple commands, all commands will be applied in sequence. The bound <paramref name="key"/> strike
		/// will be consumed if any took effect.</param>
		public void AddKeyBinding (Key key, params Command [] command)
		{
			if (command.Length == 0) {
				throw new ArgumentException ("At least one command must be specified", nameof (command));
			}

			if (KeyBindings.ContainsKey (key)) {
				KeyBindings [key] = command;
			} else {
				KeyBindings.Add (key, command);
			}
		}

		/// <summary>
		/// Replaces a key combination already bound to <see cref="Command"/>.
		/// </summary>
		/// <param name="fromKey">The key to be replaced.</param>
		/// <param name="toKey">The new key to be used.</param>
		protected void ReplaceKeyBinding (Key fromKey, Key toKey)
		{
			if (KeyBindings.ContainsKey (fromKey)) {
				var value = KeyBindings [fromKey];
				KeyBindings.Remove (fromKey);
				KeyBindings [toKey] = value;
			}
		}

		/// <summary>
		/// Checks if the key binding already exists.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns><see langword="true"/> If the key already exist, <see langword="false"/> otherwise.</returns>
		public bool ContainsKeyBinding (Key key)
		{
			return KeyBindings.ContainsKey (key);
		}

		/// <summary>
		/// Removes all bound keys from the View and resets the default bindings.
		/// </summary>
		public void ClearKeybindings ()
		{
			KeyBindings.Clear ();
		}

		/// <summary>
		/// Clears the existing keybinding (if any) for the given <paramref name="key"/>.
		/// </summary>
		/// <param name="key"></param>
		public void ClearKeybinding (Key key)
		{
			KeyBindings.Remove (key);
		}

		/// <summary>
		/// Removes all key bindings that trigger the given command. Views can have multiple different
		/// keys bound to the same command and this method will clear all of them.
		/// </summary>
		/// <param name="command"></param>
		public void ClearKeybinding (params Command [] command)
		{
			foreach (var kvp in KeyBindings.Where (kvp => kvp.Value.SequenceEqual (command)).ToArray ()) {
				KeyBindings.Remove (kvp.Key);
			}
		}

		/// <summary>
		/// <para>States that the given <see cref="View"/> supports a given <paramref name="command"/>
		/// and what <paramref name="f"/> to perform to make that command happen
		/// </para>
		/// <para>If the <paramref name="command"/> already has an implementation the <paramref name="f"/>
		/// will replace the old one</para>
		/// </summary>
		/// <param name="command">The command.</param>
		/// <param name="f">The function.</param>
		protected void AddCommand (Command command, Func<bool?> f)
		{
			// if there is already an implementation of this command
			if (CommandImplementations.ContainsKey (command)) {
				// replace that implementation
				CommandImplementations [command] = f;
			} else {
				// else record how to perform the action (this should be the normal case)
				CommandImplementations.Add (command, f);
			}
		}

		/// <summary>
		/// Returns all commands that are supported by this <see cref="View"/>.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Command> GetSupportedCommands ()
		{
			return CommandImplementations.Keys;
		}

		/// <summary>
		/// Gets the key used by a command.
		/// </summary>
		/// <param name="command">The command to search.</param>
		/// <returns>The <see cref="Key"/> used by a <see cref="Command"/></returns>
		public Key GetKeyFromCommand (params Command [] command)
		{
			return KeyBindings.First (kb => kb.Value.SequenceEqual (command)).Key;
		}

		/// <inheritdoc/>
		public override bool ProcessHotKey (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			if (MostFocused?.Enabled == true) {
				MostFocused?.KeyPress?.Invoke (this, args);
				if (args.Handled)
					return true;
			}
			if (MostFocused?.Enabled == true && MostFocused?.ProcessKey (keyEvent) == true)
				return true;
			if (subviews == null || subviews.Count == 0)
				return false;

			foreach (var view in subviews)
				if (view.Enabled && view.ProcessHotKey (keyEvent))
					return true;
			return false;
		}

		/// <inheritdoc/>
		public override bool ProcessColdKey (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyPress?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (MostFocused?.Enabled == true) {
				MostFocused?.KeyPress?.Invoke (this, args);
				if (args.Handled)
					return true;
			}
			if (MostFocused?.Enabled == true && MostFocused?.ProcessKey (keyEvent) == true)
				return true;
			if (subviews == null || subviews.Count == 0)
				return false;

			foreach (var view in subviews)
				if (view.Enabled && view.ProcessColdKey (keyEvent))
					return true;
			return false;
		}

		/// <summary>
		/// Invoked when a key is pressed.
		/// </summary>
		public event EventHandler<KeyEventEventArgs> KeyDown;

		/// <inheritdoc/>
		public override bool OnKeyDown (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyDown?.Invoke (this, args);
			if (args.Handled) {
				return true;
			}
			if (Focused?.Enabled == true) {
				Focused.KeyDown?.Invoke (this, args);
				if (args.Handled) {
					return true;
				}
				if (Focused?.OnKeyDown (keyEvent) == true) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Invoked when a key is released.
		/// </summary>
		public event EventHandler<KeyEventEventArgs> KeyUp;

		/// <inheritdoc/>
		public override bool OnKeyUp (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyUp?.Invoke (this, args);
			if (args.Handled) {
				return true;
			}
			if (Focused?.Enabled == true) {
				Focused.KeyUp?.Invoke (this, args);
				if (args.Handled) {
					return true;
				}
				if (Focused?.OnKeyUp (keyEvent) == true) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Finds the first view in the hierarchy that wants to get the focus if nothing is currently focused, otherwise, does nothing.
		/// </summary>
		public void EnsureFocus ()
		{
			if (focused == null && subviews?.Count > 0) {
				if (FocusDirection == Direction.Forward) {
					FocusFirst ();
				} else {
					FocusLast ();
				}
			}
		}

		/// <summary>
		/// Focuses the first focusable subview if one exists.
		/// </summary>
		public void FocusFirst ()
		{
			if (!CanBeVisible (this)) {
				return;
			}

			if (tabIndexes == null) {
				SuperView?.SetFocus (this);
				return;
			}

			foreach (var view in tabIndexes) {
				if (view.CanFocus && view.tabStop && view.Visible && view.Enabled) {
					SetFocus (view);
					return;
				}
			}
		}

		/// <summary>
		/// Focuses the last focusable subview if one exists.
		/// </summary>
		public void FocusLast ()
		{
			if (!CanBeVisible (this)) {
				return;
			}

			if (tabIndexes == null) {
				SuperView?.SetFocus (this);
				return;
			}

			for (var i = tabIndexes.Count; i > 0;) {
				i--;

				var v = tabIndexes [i];
				if (v.CanFocus && v.tabStop && v.Visible && v.Enabled) {
					SetFocus (v);
					return;
				}
			}
		}

		/// <summary>
		/// Focuses the previous view.
		/// </summary>
		/// <returns><see langword="true"/> if previous was focused, <see langword="false"/> otherwise.</returns>
		public bool FocusPrev ()
		{
			if (!CanBeVisible (this)) {
				return false;
			}

			FocusDirection = Direction.Backward;
			if (tabIndexes == null || tabIndexes.Count == 0)
				return false;

			if (focused == null) {
				FocusLast ();
				return focused != null;
			}

			var focusedIdx = -1;
			for (var i = tabIndexes.Count; i > 0;) {
				i--;
				var w = tabIndexes [i];

				if (w.HasFocus) {
					if (w.FocusPrev ())
						return true;
					focusedIdx = i;
					continue;
				}
				if (w.CanFocus && focusedIdx != -1 && w.tabStop && w.Visible && w.Enabled) {
					focused.SetHasFocus (false, w);

					if (w.CanFocus && w.tabStop && w.Visible && w.Enabled)
						w.FocusLast ();

					SetFocus (w);
					return true;
				}
			}
			if (focused != null) {
				focused.SetHasFocus (false, this);
				focused = null;
			}
			return false;
		}

		/// <summary>
		/// Focuses the next view.
		/// </summary>
		/// <returns><see langword="true"/> if next was focused, <see langword="false"/> otherwise.</returns>
		public bool FocusNext ()
		{
			if (!CanBeVisible (this)) {
				return false;
			}

			FocusDirection = Direction.Forward;
			if (tabIndexes == null || tabIndexes.Count == 0)
				return false;

			if (focused == null) {
				FocusFirst ();
				return focused != null;
			}
			var n = tabIndexes.Count;
			var focusedIdx = -1;
			for (var i = 0; i < n; i++) {
				var w = tabIndexes [i];

				if (w.HasFocus) {
					if (w.FocusNext ())
						return true;
					focusedIdx = i;
					continue;
				}
				if (w.CanFocus && focusedIdx != -1 && w.tabStop && w.Visible && w.Enabled) {
					focused.SetHasFocus (false, w);

					if (w.CanFocus && w.tabStop && w.Visible && w.Enabled)
						w.FocusFirst ();

					SetFocus (w);
					return true;
				}
			}
			if (focused != null) {
				focused.SetHasFocus (false, this);
				focused = null;
			}
			return false;
		}

		View GetMostFocused (View view)
		{
			if (view == null) {
				return null;
			}

			return view.focused != null ? GetMostFocused (view.focused) : view;
		}

		/// <summary>
		/// Sets the View's <see cref="Frame"/> to the frame-relative coordinates if its container. The
		/// container size and location are specified by <paramref name="superviewFrame"/> and are relative to the
		/// View's superview.
		/// </summary>
		/// <param name="superviewFrame">The supserview-relative rectangle describing View's container (nominally the 
		/// same as <c>this.SuperView.Frame</c>).</param>
		internal void SetRelativeLayout (Rect superviewFrame)
		{
			int newX, newW, newY, newH;
			var autosize = Size.Empty;

			if (AutoSize) {
				// Note this is global to this function and used as such within the local functions defined
				// below. In v2 AutoSize will be re-factored to not need to be dealt with in this function.
				autosize = GetAutoSize ();
			}

			// Returns the new dimension (width or height) and location (x or y) for the View given
			//   the superview's Frame.X or Frame.Y
			//   the superview's width or height
			//   the current Pos (View.X or View.Y)
			//   the current Dim (View.Width or View.Height)
			(int newLocation, int newDimension) GetNewLocationAndDimension (int superviewLocation, int superviewDimension, Pos pos, Dim dim, int autosizeDimension)
			{
				int newDimension, newLocation;

				switch (pos) {
				case Pos.PosCenter:
					if (dim == null) {
						newDimension = AutoSize ? autosizeDimension : superviewDimension;
					} else {
						newDimension = dim.Anchor (superviewDimension);
						newDimension = AutoSize && autosizeDimension > newDimension ? autosizeDimension : newDimension;
					}
					newLocation = pos.Anchor (superviewDimension - newDimension);
					break;

				case Pos.PosCombine combine:
					int left, right;
					(left, newDimension) = GetNewLocationAndDimension (superviewLocation, superviewDimension, combine.left, dim, autosizeDimension);
					(right, newDimension) = GetNewLocationAndDimension (superviewLocation, superviewDimension, combine.right, dim, autosizeDimension);
					if (combine.add) {
						newLocation = left + right;
					} else {
						newLocation = left - right;
					}
					newDimension = Math.Max (CalculateNewDimension (dim, newLocation, superviewDimension, autosizeDimension), 0);
					break;

				case Pos.PosAbsolute:
				case Pos.PosAnchorEnd:
				case Pos.PosFactor:
				case Pos.PosFunc:
				case Pos.PosView:
				default:
					newLocation = pos?.Anchor (superviewDimension) ?? 0;
					newDimension = Math.Max (CalculateNewDimension (dim, newLocation, superviewDimension, autosizeDimension), 0);
					break;
				}
				return (newLocation, newDimension);
			}

			// Recursively calculates the new dimension (width or height) of the given Dim given:
			//   the current location (x or y)
			//   the current dimension (width or height)
			int CalculateNewDimension (Dim d, int location, int dimension, int autosize)
			{
				int newDimension;
				switch (d) {
				case null:
					newDimension = AutoSize ? autosize : dimension;
					break;
				case Dim.DimCombine combine:
					int leftNewDim = CalculateNewDimension (combine.left, location, dimension, autosize);
					int rightNewDim = CalculateNewDimension (combine.right, location, dimension, autosize);
					if (combine.add) {
						newDimension = leftNewDim + rightNewDim;
					} else {
						newDimension = leftNewDim - rightNewDim;
					}
					newDimension = AutoSize && autosize > newDimension ? autosize : newDimension;
					break;

				case Dim.DimFactor factor when !factor.IsFromRemaining ():
					newDimension = d.Anchor (dimension);
					newDimension = AutoSize && autosize > newDimension ? autosize : newDimension;
					break;

				case Dim.DimFill:
				default:
					newDimension = Math.Max (d.Anchor (dimension - location), 0);
					newDimension = AutoSize && autosize > newDimension ? autosize : newDimension;
					break;
				}

				return newDimension;
			}


			// horizontal
			(newX, newW) = GetNewLocationAndDimension (superviewFrame.X, superviewFrame.Width, x, width, autosize.Width);

			// vertical
			(newY, newH) = GetNewLocationAndDimension (superviewFrame.Y, superviewFrame.Height, y, height, autosize.Height);

			var r = new Rect (newX, newY, newW, newH);
			if (Frame != r) {
				Frame = r;
				// BUGBUG: Why is this AFTER setting Frame? Seems duplicative.
				if (!SetMinWidthHeight ()) {
					TextFormatter.Size = GetSizeNeededForTextAndHotKey ();
				}
			}
		}

		/// <summary>
		/// Fired after the View's <see cref="LayoutSubviews"/> method has completed. 
		/// </summary>
		/// <remarks>
		/// Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has otherwise changed.
		/// </remarks>
		public event EventHandler<LayoutEventArgs> LayoutStarted;

		/// <summary>
		/// Raises the <see cref="LayoutStarted"/> event. Called from  <see cref="LayoutSubviews"/> before any subviews have been laid out.
		/// </summary>
		internal virtual void OnLayoutStarted (LayoutEventArgs args)
		{
			LayoutStarted?.Invoke (this, args);
		}

		/// <summary>
		/// Fired after the View's <see cref="LayoutSubviews"/> method has completed. 
		/// </summary>
		/// <remarks>
		/// Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has otherwise changed.
		/// </remarks>
		public event EventHandler<LayoutEventArgs> LayoutComplete;

		/// <summary>
		/// Event called only once when the <see cref="View"/> is being initialized for the first time.
		/// Allows configurations and assignments to be performed before the <see cref="View"/> being shown.
		/// This derived from <see cref="ISupportInitializeNotification"/> to allow notify all the views that are being initialized.
		/// </summary>
		public event EventHandler Initialized;

		/// <summary>
		/// Raises the <see cref="LayoutComplete"/> event. Called from  <see cref="LayoutSubviews"/> before all sub-views have been laid out.
		/// </summary>
		internal virtual void OnLayoutComplete (LayoutEventArgs args)
		{
			LayoutComplete?.Invoke (this, args);
		}

		internal void CollectPos (Pos pos, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
		{
			switch (pos) {
			case Pos.PosView pv:
				if (pv.Target != this) {
					nEdges.Add ((pv.Target, from));
				}
				foreach (var v in from.InternalSubviews) {
					CollectAll (v, ref nNodes, ref nEdges);
				}
				return;
			case Pos.PosCombine pc:
				foreach (var v in from.InternalSubviews) {
					CollectPos (pc.left, from, ref nNodes, ref nEdges);
					CollectPos (pc.right, from, ref nNodes, ref nEdges);
				}
				break;
			}
		}

		internal void CollectDim (Dim dim, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
		{
			switch (dim) {
			case Dim.DimView dv:
				if (dv.Target != this) {
					nEdges.Add ((dv.Target, from));
				}
				foreach (var v in from.InternalSubviews) {
					CollectAll (v, ref nNodes, ref nEdges);
				}
				return;
			case Dim.DimCombine dc:
				foreach (var v in from.InternalSubviews) {
					CollectDim (dc.left, from, ref nNodes, ref nEdges);
					CollectDim (dc.right, from, ref nNodes, ref nEdges);
				}
				break;
			}
		}

		internal void CollectAll (View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
		{
			foreach (var v in from.InternalSubviews) {
				nNodes.Add (v);
				if (v.layoutStyle != LayoutStyle.Computed) {
					continue;
				}
				CollectPos (v.X, v, ref nNodes, ref nEdges);
				CollectPos (v.Y, v, ref nNodes, ref nEdges);
				CollectDim (v.Width, v, ref nNodes, ref nEdges);
				CollectDim (v.Height, v, ref nNodes, ref nEdges);
			}
		}

		// https://en.wikipedia.org/wiki/Topological_sorting
		internal static List<View> TopologicalSort (View superView, IEnumerable<View> nodes, ICollection<(View From, View To)> edges)
		{
			var result = new List<View> ();

			// Set of all nodes with no incoming edges
			var noEdgeNodes = new HashSet<View> (nodes.Where (n => edges.All (e => !e.To.Equals (n))));

			while (noEdgeNodes.Any ()) {
				//  remove a node n from S
				var n = noEdgeNodes.First ();
				noEdgeNodes.Remove (n);

				// add n to tail of L
				if (n != superView)
					result.Add (n);

				// for each node m with an edge e from n to m do
				foreach (var e in edges.Where (e => e.From.Equals (n)).ToArray ()) {
					var m = e.To;

					// remove edge e from the graph
					edges.Remove (e);

					// if m has no other incoming edges then
					if (edges.All (me => !me.To.Equals (m)) && m != superView) {
						// insert m into S
						noEdgeNodes.Add (m);
					}
				}
			}

			if (edges.Any ()) {
				(var from, var to) = edges.First ();
				if (from != superView?.GetTopSuperView (to, from)) {
					if (!ReferenceEquals (from, to)) {
						if (ReferenceEquals (from.SuperView, to)) {
							throw new InvalidOperationException ($"ComputedLayout for \"{superView}\": \"{to}\" references a SubView (\"{from}\").");
						} else {
							throw new InvalidOperationException ($"ComputedLayout for \"{superView}\": \"{from}\" linked with \"{to}\" was not found. Did you forget to add it to {superView}?");
						}
					} else {
						throw new InvalidOperationException ($"ComputedLayout for \"{superView}\": A recursive cycle was found in the relative Pos/Dim of the SubViews.");
					}
				}
			}
			// return L (a topologically sorted order)
			return result;
		} // TopologicalSort


		/// <summary>
		/// Invoked when a view starts executing or when the dimensions of the view have changed, for example in
		/// response to the container view or terminal resizing.
		/// </summary>
		/// <remarks>
		/// Calls <see cref="OnLayoutComplete"/> (which raises the <see cref="LayoutComplete"/> event) before it returns.
		/// </remarks>
		public virtual void LayoutSubviews ()
		{
			if (!LayoutNeeded) {
				return;
			}

			var oldBounds = Bounds;
			OnLayoutStarted (new LayoutEventArgs () { OldBounds = oldBounds });

			TextFormatter.Size = GetSizeNeededForTextAndHotKey ();

			// Sort out the dependencies of the X, Y, Width, Height properties
			var nodes = new HashSet<View> ();
			var edges = new HashSet<(View, View)> ();
			CollectAll (this, ref nodes, ref edges);
			var ordered = View.TopologicalSort (SuperView, nodes, edges);
			foreach (var v in ordered) {
				LayoutSubview (v, Frame);
			}

			// If the 'to' is rooted to 'from' and the layoutstyle is Computed it's a special-case.
			// Use LayoutSubview with the Frame of the 'from' 
			if (SuperView != null && GetTopSuperView () != null && LayoutNeeded
			    && ordered.Count == 0 && edges.Count > 0 && LayoutStyle == LayoutStyle.Computed) {

				(var from, var to) = edges.First ();
				LayoutSubview (to, from.Frame);
			}

			LayoutNeeded = false;

			OnLayoutComplete (new LayoutEventArgs () { OldBounds = oldBounds });
		}

		private void LayoutSubview (View v, Rect hostFrame)
		{
			if (v.LayoutStyle == LayoutStyle.Computed) {
				v.SetRelativeLayout (hostFrame);
			}

			v.LayoutSubviews ();
			v.LayoutNeeded = false;
		}

		ustring text;

		/// <summary>
		///   The text displayed by the <see cref="View"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		///  If provided, the text will be drawn before any subviews are drawn.
		/// </para>
		/// <para>
		///  The text will be drawn starting at the view origin (0, 0) and will be formatted according
		///  to the <see cref="TextAlignment"/> property. If the view's height is greater than 1, the
		///  text will word-wrap to additional lines if it does not fit horizontally. If the view's height
		///  is 1, the text will be clipped.
		/// </para>
		/// <para>
		///  Set the <see cref="HotKeySpecifier"/> to enable hotkey support. To disable hotkey support set <see cref="HotKeySpecifier"/> to
		///  <c>(Rune)0xffff</c>.
		/// </para>
		/// </remarks>
		public virtual ustring Text {
			get => text;
			set {
				text = value;
				SetHotKey ();
				UpdateTextFormatterText ();
				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// Gets or sets a flag that determines whether the View will be automatically resized to fit the <see cref="Text"/> 
		/// within <see cref="Bounds"/>
		/// <para>
		/// The default is <see langword="false"/>. Set to <see langword="true"/> to turn on AutoSize. If <see langword="true"/> then
		/// <see cref="Width"/> and <see cref="Height"/> will be used if <see cref="Text"/> can fit; 
		/// if <see cref="Text"/> won't fit the view will be resized as needed.
		/// </para>
		/// <para>
		/// In addition, if <see cref="ForceValidatePosDim"/> is <see langword="true"/> the new values of <see cref="Width"/> and
		/// <see cref="Height"/> must be of the same types of the existing one to avoid breaking the <see cref="Dim"/> settings.
		/// </para>
		/// </summary>
		public virtual bool AutoSize {
			get => autoSize;
			set {
				var v = ResizeView (value);
				TextFormatter.AutoSize = v;
				if (autoSize != v) {
					autoSize = v;
					TextFormatter.NeedsFormat = true;
					UpdateTextFormatterText ();
					OnResizeNeeded ();
				}
			}
		}

		/// <summary>
		/// Gets or sets a flag that determines whether <see cref="Terminal.Gui.TextFormatter.Text"/> will have trailing spaces preserved
		/// or not when <see cref="Terminal.Gui.TextFormatter.WordWrap"/> is enabled. If <see langword="true"/> 
		/// any trailing spaces will be trimmed when either the <see cref="Text"/> property is changed or 
		/// when <see cref="Terminal.Gui.TextFormatter.WordWrap"/> is set to <see langword="true"/>.
		/// The default is <see langword="false"/>.
		/// </summary>
		public virtual bool PreserveTrailingSpaces {
			get => TextFormatter.PreserveTrailingSpaces;
			set {
				if (TextFormatter.PreserveTrailingSpaces != value) {
					TextFormatter.PreserveTrailingSpaces = value;
					TextFormatter.NeedsFormat = true;
				}
			}
		}

		/// <summary>
		/// Gets or sets how the View's <see cref="Text"/> is aligned horizontally when drawn. Changing this property will redisplay the <see cref="View"/>.
		/// </summary>
		/// <value>The text alignment.</value>
		public virtual TextAlignment TextAlignment {
			get => TextFormatter.Alignment;
			set {
				TextFormatter.Alignment = value;
				UpdateTextFormatterText ();
				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// Gets or sets how the View's <see cref="Text"/> is aligned vertically when drawn. Changing this property will redisplay the <see cref="View"/>.
		/// </summary>
		/// <value>The text alignment.</value>
		public virtual VerticalTextAlignment VerticalTextAlignment {
			get => TextFormatter.VerticalAlignment;
			set {
				TextFormatter.VerticalAlignment = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets the direction of the View's <see cref="Text"/>. Changing this property will redisplay the <see cref="View"/>.
		/// </summary>
		/// <value>The text alignment.</value>
		public virtual TextDirection TextDirection {
			get => TextFormatter.Direction;
			set {
				if (TextFormatter.Direction != value) {
					var isValidOldAutSize = autoSize && IsValidAutoSize (out var _);
					var directionChanged = TextFormatter.IsHorizontalDirection (TextFormatter.Direction)
					    != TextFormatter.IsHorizontalDirection (value);

					TextFormatter.Direction = value;
					UpdateTextFormatterText ();

					if ((!ForceValidatePosDim && directionChanged && AutoSize)
					    || (ForceValidatePosDim && directionChanged && AutoSize && isValidOldAutSize)) {
						OnResizeNeeded ();
					} else if (directionChanged && IsAdded) {
						SetWidthHeight (Bounds.Size);
						SetMinWidthHeight ();
					} else {
						SetMinWidthHeight ();
					}
					TextFormatter.Size = GetSizeNeededForTextAndHotKey ();
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Get or sets if  the <see cref="View"/> has been initialized (via <see cref="ISupportInitialize.BeginInit"/> 
		/// and <see cref="ISupportInitialize.EndInit"/>).
		/// </summary>
		/// <para>
		///     If first-run-only initialization is preferred, overrides to <see cref="ISupportInitializeNotification.IsInitialized"/>
		///     can be implemented, in which case the <see cref="ISupportInitialize"/>
		///     methods will only be called if <see cref="ISupportInitializeNotification.IsInitialized"/>
		///     is <see langword="false"/>. This allows proper <see cref="View"/> inheritance hierarchies
		///     to override base class layout code optimally by doing so only on first run,
		///     instead of on every run.
		///   </para>
		public virtual bool IsInitialized { get; set; }

		/// <summary>
		/// Gets information if the view was already added to the <see cref="SuperView"/>.
		/// </summary>
		public bool IsAdded { get; private set; }

		bool oldEnabled;

		/// <inheritdoc/>
		public override bool Enabled {
			get => base.Enabled;
			set {
				if (base.Enabled != value) {
					if (value) {
						if (SuperView == null || SuperView?.Enabled == true) {
							base.Enabled = value;
						}
					} else {
						base.Enabled = value;
					}
					if (!value && HasFocus) {
						SetHasFocus (false, this);
					}
					OnEnabledChanged ();
					SetNeedsDisplay ();

					if (subviews != null) {
						foreach (var view in subviews) {
							if (!value) {
								view.oldEnabled = view.Enabled;
								view.Enabled = false;
							} else {
								view.Enabled = view.oldEnabled;
								view.addingView = false;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets whether a view is cleared if the <see cref="Visible"/> property is <see langword="false"/>.
		/// </summary>
		public bool ClearOnVisibleFalse { get; set; } = true;

		/// <inheritdoc/>>
		public override bool Visible {
			get => base.Visible;
			set {
				if (base.Visible != value) {
					base.Visible = value;
					if (!value) {
						if (HasFocus) {
							SetHasFocus (false, this);
						}
						if (ClearOnVisibleFalse) {
							Clear ();
						}
					}
					OnVisibleChanged ();
					SetNeedsDisplay ();
				}
			}
		}

		Border border;

		/// <inheritdoc/>
		public virtual Border Border {
			get => border;
			set {
				if (border != value) {
					border = value;
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Get or sets whether the view will use <see cref="Terminal.Gui.Border"/> (if <see cref="Border"/> is set) to draw 
		/// a border. If <see langword="false"/> (the default),
		/// <see cref="View.Redraw(Rect)"/> will call <see cref="Border.DrawContent(View, bool)"/>
		/// to draw the view's border. If <see langword="true"/> no border is drawn (and the view is expected to draw the border
		/// itself).
		/// </summary>
		public virtual bool IgnoreBorderPropertyOnRedraw { get; set; }

		/// <summary>
		/// Pretty prints the View
		/// </summary>
		/// <returns></returns>
		public override string ToString ()
		{
			return $"{GetType ().Name}({Id})({Frame})";
		}

		void SetHotKey ()
		{
			TextFormatter.FindHotKey (text, HotKeySpecifier, true, out _, out var hk);
			if (hotKey != hk) {
				HotKey = hk;
			}
		}

		bool ResizeView (bool autoSize)
		{
			if (!autoSize) {
				return false;
			}

			var aSize = true;
			var nBoundsSize = GetAutoSize ();
			if (nBoundsSize != Bounds.Size) {
				if (ForceValidatePosDim) {
					aSize = SetWidthHeight (nBoundsSize);
				} else {
					Bounds = new Rect (Bounds.X, Bounds.Y, nBoundsSize.Width, nBoundsSize.Height);
				}
			}
			// BUGBUG: This call may be redundant
			TextFormatter.Size = GetSizeNeededForTextAndHotKey ();
			return aSize;
		}

		/// <summary>
		/// Resizes the View to fit the specified <see cref="Bounds"/> size.
		/// </summary>
		/// <param name="nBounds"></param>
		/// <returns></returns>
		bool SetWidthHeight (Size nBounds)
		{
			var aSize = false;
			var canSizeW = TrySetWidth (nBounds.Width - GetHotKeySpecifierLength (), out var rW);
			var canSizeH = TrySetHeight (nBounds.Height - GetHotKeySpecifierLength (false), out var rH);
			if (canSizeW) {
				aSize = true;
				width = rW;
			}
			if (canSizeH) {
				aSize = true;
				height = rH;
			}
			if (aSize) {
				Bounds = new Rect (Bounds.X, Bounds.Y, canSizeW ? rW : Bounds.Width, canSizeH ? rH : Bounds.Height);
			}

			return aSize;
		}

		/// <summary>
		/// Gets the dimensions required to fit <see cref="Text"/> using the text <see cref="Direction"/> specified by the
		/// <see cref="TextFormatter"/> property and accounting for any <see cref="HotKeySpecifier"/> characters.
		/// .
		/// </summary>
		/// <returns>The <see cref="Size"/> required to fit the text.</returns>
		public Size GetAutoSize ()
		{
			var rect = TextFormatter.CalcRect (Bounds.X, Bounds.Y, TextFormatter.Text, TextFormatter.Direction);
			return new Size (rect.Size.Width - GetHotKeySpecifierLength (),
			    rect.Size.Height - GetHotKeySpecifierLength (false));
		}

		bool IsValidAutoSize (out Size autoSize)
		{
			var rect = TextFormatter.CalcRect (frame.X, frame.Y, TextFormatter.Text, TextDirection);
			autoSize = new Size (rect.Size.Width - GetHotKeySpecifierLength (),
			    rect.Size.Height - GetHotKeySpecifierLength (false));
			return !(ForceValidatePosDim && (!(Width is Dim.DimAbsolute) || !(Height is Dim.DimAbsolute))
			    || frame.Size.Width != rect.Size.Width - GetHotKeySpecifierLength ()
			    || frame.Size.Height != rect.Size.Height - GetHotKeySpecifierLength (false));
		}

		bool IsValidAutoSizeWidth (Dim width)
		{
			var rect = TextFormatter.CalcRect (frame.X, frame.Y, TextFormatter.Text, TextDirection);
			var dimValue = width.Anchor (0);
			return !(ForceValidatePosDim && (!(width is Dim.DimAbsolute)) || dimValue != rect.Size.Width
			    - GetHotKeySpecifierLength ());
		}

		bool IsValidAutoSizeHeight (Dim height)
		{
			var rect = TextFormatter.CalcRect (frame.X, frame.Y, TextFormatter.Text, TextDirection);
			var dimValue = height.Anchor (0);
			return !(ForceValidatePosDim && (!(height is Dim.DimAbsolute)) || dimValue != rect.Size.Height
			    - GetHotKeySpecifierLength (false));
		}

		/// <summary>
		/// Gets the width or height of the <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/> characters 
		/// in the <see cref="Text"/> property.
		/// </summary>
		/// <remarks>
		/// Only the first hotkey specifier found in <see cref="Text"/> is supported.
		/// </remarks>
		/// <param name="isWidth">If <see langword="true"/> (the default) the width required for the hotkey specifier is returned. Otherwise the height is returned.</param>
		/// <returns>The number of characters required for the <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/>. If the text direction specified
		/// by <see cref="TextDirection"/> does not match the <paramref name="isWidth"/> parameter, <c>0</c> is returned.</returns>
		public int GetHotKeySpecifierLength (bool isWidth = true)
		{
			if (isWidth) {
				return TextFormatter.IsHorizontalDirection (TextDirection) &&
				    TextFormatter.Text?.Contains (HotKeySpecifier) == true
				    ? Math.Max (Rune.ColumnWidth (HotKeySpecifier), 0) : 0;
			} else {
				return TextFormatter.IsVerticalDirection (TextDirection) &&
				    TextFormatter.Text?.Contains (HotKeySpecifier) == true
				    ? Math.Max (Rune.ColumnWidth (HotKeySpecifier), 0) : 0;
			}
		}

		/// <summary>
		/// Gets the dimensions required for <see cref="Text"/> ignoring a <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/>.
		/// </summary>
		/// <returns></returns>
		public Size GetSizeNeededForTextWithoutHotKey ()
		{
			return new Size (TextFormatter.Size.Width - GetHotKeySpecifierLength (),
			    TextFormatter.Size.Height - GetHotKeySpecifierLength (false));
		}

		/// <summary>
		/// Gets the dimensions required for <see cref="Text"/> accounting for a <see cref="Terminal.Gui.TextFormatter.HotKeySpecifier"/> .
		/// </summary>
		/// <returns></returns>
		public Size GetSizeNeededForTextAndHotKey ()
		{
			if (ustring.IsNullOrEmpty (TextFormatter.Text)) {
				return Bounds.Size;
			}

			// BUGBUG: This IGNORES what Text is set to, using on only the current View size. This doesn't seem to make sense.
			// BUGBUG: This uses Frame; in v2 it should be Bounds
			return new Size (frame.Size.Width + GetHotKeySpecifierLength (),
			    frame.Size.Height + GetHotKeySpecifierLength (false));
		}

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

		/// <inheritdoc/>
		public override void OnCanFocusChanged () => CanFocusChanged?.Invoke (this, EventArgs.Empty);

		/// <inheritdoc/>
		public override void OnEnabledChanged () => EnabledChanged?.Invoke (this, EventArgs.Empty);

		/// <inheritdoc/>
		public override void OnVisibleChanged () => VisibleChanged?.Invoke (this, EventArgs.Empty);

		/// <inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			for (var i = InternalSubviews.Count - 1; i >= 0; i--) {
				var subview = InternalSubviews [i];
				Remove (subview);
				subview.Dispose ();
			}
			base.Dispose (disposing);
		}

		/// <summary>
		///  Signals the View that initialization is starting. See <see cref="ISupportInitialize"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		///     Views can opt-in to more sophisticated initialization
		///     by implementing overrides to <see cref="ISupportInitialize.BeginInit"/> and
		///     <see cref="ISupportInitialize.EndInit"/> which will be called
		///     when the view is added to a <see cref="SuperView"/>. 
		/// </para>
		/// <para>
		///     If first-run-only initialization is preferred, overrides to <see cref="ISupportInitializeNotification"/>
		///     can be implemented too, in which case the <see cref="ISupportInitialize"/>
		///     methods will only be called if <see cref="ISupportInitializeNotification.IsInitialized"/>
		///     is <see langword="false"/>. This allows proper <see cref="View"/> inheritance hierarchies
		///     to override base class layout code optimally by doing so only on first run,
		///     instead of on every run.
		///   </para>
		/// </remarks>
		public virtual void BeginInit ()
		{
			if (!IsInitialized) {
				oldCanFocus = CanFocus;
				oldTabIndex = tabIndex;

				//UpdateTextFormatterText ();
				// TODO: Figure out why ScrollView and other tests fail if this call is put here 
				// instead of the constructor.
				// OnSizeChanged ();

			} else {
				//throw new InvalidOperationException ("The view is already initialized.");
			}

			if (subviews?.Count > 0) {
				foreach (var view in subviews) {
					if (!view.IsInitialized) {
						view.BeginInit ();
					}
				}
			}
		}

		/// <summary>
		///  Signals the View that initialization is ending. See <see cref="ISupportInitialize"/>.
		/// </summary>
		public void EndInit ()
		{
			IsInitialized = true;
			if (subviews != null) {
				foreach (var view in subviews) {
					if (!view.IsInitialized) {
						view.EndInit ();
					}
				}
			}
			Initialized?.Invoke (this, EventArgs.Empty);
		}

		bool CanBeVisible (View view)
		{
			if (!view.Visible) {
				return false;
			}
			for (var c = view.SuperView; c != null; c = c.SuperView) {
				if (!c.Visible) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Determines if the View's <see cref="Width"/> can be set to a new value.
		/// </summary>
		/// <param name="desiredWidth"></param>
		/// <param name="resultWidth">Contains the width that would result if <see cref="Width"/> were set to <paramref name="desiredWidth"/>"/> </param>
		/// <returns><see langword="true"/> if the View's <see cref="Width"/> can be changed to the specified value. False otherwise.</returns>
		internal bool TrySetWidth (int desiredWidth, out int resultWidth)
		{
			var w = desiredWidth;
			bool canSetWidth;
			if (Width is Dim.DimCombine || Width is Dim.DimView || Width is Dim.DimFill) {
				// It's a Dim.DimCombine and so can't be assigned. Let it have it's width anchored.
				w = Width.Anchor (w);
				canSetWidth = !ForceValidatePosDim;
			} else if (Width is Dim.DimFactor factor) {
				// Tries to get the SuperView width otherwise the view width.
				var sw = SuperView != null ? SuperView.Frame.Width : w;
				if (factor.IsFromRemaining ()) {
					sw -= Frame.X;
				}
				w = Width.Anchor (sw);
				canSetWidth = !ForceValidatePosDim;
			} else {
				canSetWidth = true;
			}
			resultWidth = w;

			return canSetWidth;
		}

		/// <summary>
		/// Determines if the View's <see cref="Height"/> can be set to a new value.
		/// </summary>
		/// <param name="desiredHeight"></param>
		/// <param name="resultHeight">Contains the width that would result if <see cref="Height"/> were set to <paramref name="desiredHeight"/>"/> </param>
		/// <returns><see langword="true"/> if the View's <see cref="Height"/> can be changed to the specified value. False otherwise.</returns>
		internal bool TrySetHeight (int desiredHeight, out int resultHeight)
		{
			var h = desiredHeight;
			bool canSetHeight;
			switch (Height) {
			case Dim.DimCombine _:
			case Dim.DimView _:
			case Dim.DimFill _:
				// It's a Dim.DimCombine and so can't be assigned. Let it have it's height anchored.
				h = Height.Anchor (h);
				canSetHeight = !ForceValidatePosDim;
				break;
			case Dim.DimFactor factor:
				// Tries to get the SuperView height otherwise the view height.
				var sh = SuperView != null ? SuperView.Frame.Height : h;
				if (factor.IsFromRemaining ()) {
					sh -= Frame.Y;
				}
				h = Height.Anchor (sh);
				canSetHeight = !ForceValidatePosDim;
				break;
			default:
				canSetHeight = true;
				break;
			}
			resultHeight = h;

			return canSetHeight;
		}

		/// <summary>
		/// Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.
		/// </summary>
		/// <returns><see cref="Terminal.Gui.ColorScheme.Normal"/> if <see cref="Enabled"/> is <see langword="true"/>
		/// or <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>.
		/// If it's overridden can return other values.</returns>
		public virtual Attribute GetNormalColor ()
		{
			return Enabled ? ColorScheme.Normal : ColorScheme.Disabled;
		}

		/// <summary>
		/// Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.
		/// </summary>
		/// <returns><see cref="Terminal.Gui.ColorScheme.Focus"/> if <see cref="Enabled"/> is <see langword="true"/>
		/// or <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>.
		/// If it's overridden can return other values.</returns>
		public virtual Attribute GetFocusColor ()
		{
			return Enabled ? ColorScheme.Focus : ColorScheme.Disabled;
		}

		/// <summary>
		/// Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.
		/// </summary>
		/// <returns><see cref="Terminal.Gui.ColorScheme.HotNormal"/> if <see cref="Enabled"/> is <see langword="true"/>
		/// or <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>.
		/// If it's overridden can return other values.</returns>
		public virtual Attribute GetHotNormalColor ()
		{
			return Enabled ? ColorScheme.HotNormal : ColorScheme.Disabled;
		}

		/// <summary>
		/// Get the top superview of a given <see cref="View"/>.
		/// </summary>
		/// <returns>The superview view.</returns>
		public View GetTopSuperView (View view = null, View superview = null)
		{
			View top = superview ?? Application.Top;
			for (var v = view?.SuperView ?? (this?.SuperView); v != null; v = v.SuperView) {
				top = v;
				if (top == superview) {
					break;
				}
			}

			return top;
		}
	}
}
