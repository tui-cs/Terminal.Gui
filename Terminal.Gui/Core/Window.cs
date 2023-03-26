//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// NOTE: Window is functionally identical to FrameView with the following exceptions. 
//  - Window is a Toplevel
//  - FrameView Does not support padding (but should)
//  - FrameView Does not support mouse dragging
//  - FrameView Does not support IEnumerable
// Any updates done here should probably be done in FrameView as well; TODO: Merge these classes

using System;
using System.Collections;
using System.Text.Json.Serialization;
using NStack;
using Terminal.Gui.Configuration;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui {
	/// <summary>
	/// A <see cref="Toplevel"/> <see cref="View"/> that draws a border around its <see cref="View.Frame"/> with a <see cref="Title"/> at the top.
	/// </summary>
	/// <remarks>
	/// The 'client area' of a <see cref="Window"/> is a rectangle deflated by one or more rows/columns from <see cref="View.Bounds"/>. A this time there is no
	/// API to determine this rectangle.
	/// </remarks>
	public partial class Window : Toplevel {
		View contentView;
		ustring title = ustring.Empty;

		/// <summary>
		/// The title to be displayed for this window.
		/// </summary>
		/// <value>The title</value>
		public ustring Title {
			get => title;
			set {
				if (!OnTitleChanging (title, value)) {
					var old = title;
					title = value;
					if (Border != null) {
						Border.Title = title;
					}
					OnTitleChanged (old, title);
				}
				SetNeedsDisplay ();
			}
		}

		/// <inheritdoc/>
		public override Border Border {
			get => base.Border;
			set {
				if (base.Border != null && base.Border.Child != null && value.Child == null) {
					value.Child = base.Border.Child;
				}
				base.Border = value;
				if (value == null) {
					return;
				}
				Rect frame;
				if (contentView != null && (contentView.Width is Dim || contentView.Height is Dim)) {
					frame = Rect.Empty;
				} else {
					frame = Frame;
				}
				AdjustContentView (frame);

				Border.BorderChanged += Border_BorderChanged;
			}
		}

		void Border_BorderChanged (object sender, EventArgs e)
		{
			Rect frame;
			if (contentView != null && (contentView.Width is Dim || contentView.Height is Dim)) {
				frame = Rect.Empty;
			} else {
				frame = Frame;
			}
			AdjustContentView (frame);
		}

		/// <summary>
		/// ContentView is an internal implementation detail of Window. It is used to host Views added with <see cref="Add(View)"/>. 
		/// Its ONLY reason for being is to provide a simple way for Window to expose to those SubViews that the Window's Bounds 
		/// are actually deflated due to the border. 
		/// </summary>
		class ContentView : View {
			Window instance;

			public ContentView (Rect frame, Window instance) : base (frame)
			{
				this.instance = instance;
			}
			public ContentView (Window instance) : base ()
			{
				this.instance = instance;
			}

			public override void OnCanFocusChanged ()
			{
				if (IsInitialized && MostFocused == null && CanFocus && Visible) {
					EnsureFocus ();
				}

				base.OnCanFocusChanged ();
			}

			public override bool OnMouseEvent (MouseEvent mouseEvent)
			{
				return instance.OnMouseEvent (mouseEvent);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.Window"/> class with an optional title using <see cref="LayoutStyle.Absolute"/> positioning.
		/// </summary>
		/// <param name="frame">Superview-relative rectangle specifying the location and size</param>
		/// <param name="title">Title</param>
		/// <remarks>
		/// This constructor initializes a Window with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Absolute"/>. Use constructors
		/// that do not take <c>Rect</c> parameters to initialize a Window with <see cref="LayoutStyle.Computed"/>. 
		/// </remarks>
		public Window (Rect frame, ustring title = null) : this (frame, title, padding: 0, border: null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> class with an optional title using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <remarks>
		///   This constructor initializes a View with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/>. 
		///   Use <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and <see cref="View.Height"/> properties to dynamically control the size and location of the view.
		/// </remarks>
		public Window (ustring title = null) : this (title, padding: 0, border: null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public Window () : this (title: null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> using <see cref="LayoutStyle.Absolute"/> positioning with the specified frame for its location, with the specified frame padding,
		/// and an optional title.
		/// </summary>
		/// <param name="frame">Superview-relative rectangle specifying the location and size</param>
		/// <param name="title">Title</param>
		/// <param name="padding">Number of characters to use for padding of the drawn frame.</param>
		/// <param name="border">The <see cref="Border"/>.</param>
		/// <remarks>
		/// This constructor initializes a Window with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Absolute"/>. Use constructors
		/// that do not take <c>Rect</c> parameters to initialize a Window with  <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/> 
		/// </remarks>
		public Window (Rect frame, ustring title = null, int padding = 0, Border border = null) : base (frame)
		{
			Initialize (title, frame, padding, border);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> using <see cref="LayoutStyle.Computed"/> positioning,
		/// and an optional title.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <param name="padding">Number of characters to use for padding of the drawn frame.</param>
		/// <param name="border">The <see cref="Border"/>.</param>
		/// <remarks>
		///   This constructor initializes a View with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/>. 
		///   Use <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and <see cref="View.Height"/> properties to dynamically control the size and location of the view.
		/// </remarks>
		public Window (ustring title = null, int padding = 0, Border border = null) : base ()
		{
			Initialize (title, Rect.Empty, padding, border);
		}

		/// <summary>
		/// The default <see cref="BorderStyle"/> for <see cref="FrameView"/>. The default is <see cref="BorderStyle.Single"/>.
		/// </summary>
		/// <remarks>
		/// This property can be set in a Theme to change the default <see cref="BorderStyle"/> for all <see cref="Window"/>s. 
		/// </remarks>
		///[SerializableConfigurationProperty (Scope = typeof (ThemeScope)), JsonConverter (typeof (JsonStringEnumConverter))]
		public static BorderStyle DefaultBorderStyle { get; set; } = BorderStyle.Single;

		void Initialize (ustring title, Rect frame, int padding = 0, Border border = null)
		{
			CanFocus = true;
			ColorScheme = Colors.Base;
			if (title == null) title = ustring.Empty;
			Title = title;
			if (border == null) {
				Border = new Border () {
					BorderStyle = DefaultBorderStyle,
					Padding = new Thickness (padding),
					Title = title
				};
			} else {
				Border = border;
				if (ustring.IsNullOrEmpty (border.Title)) {
					border.Title = title;
				}
			}
			AdjustContentView (frame);
		}

		void AdjustContentView (Rect frame)
		{
			var borderLength = Border.DrawMarginFrame ? 1 : 0;
			var sumPadding = Border.GetSumThickness ();
			var wp = new Point ();
			var wb = new Size ();
			if (frame == Rect.Empty) {
				wp.X = borderLength + sumPadding.Left;
				wp.Y = borderLength + sumPadding.Top;
				wb.Width = borderLength + sumPadding.Right;
				wb.Height = borderLength + sumPadding.Bottom;
				if (contentView == null) {
					contentView = new ContentView (this) {
						X = wp.X,
						Y = wp.Y,
						Width = Dim.Fill (wb.Width),
						Height = Dim.Fill (wb.Height)
					};
				} else {
					contentView.X = wp.X;
					contentView.Y = wp.Y;
					contentView.Width = Dim.Fill (wb.Width);
					contentView.Height = Dim.Fill (wb.Height);
				}
			} else {
				wb.Width = (2 * borderLength) + sumPadding.Right + sumPadding.Left;
				wb.Height = (2 * borderLength) + sumPadding.Bottom + sumPadding.Top;
				var cFrame = new Rect (borderLength + sumPadding.Left, borderLength + sumPadding.Top, frame.Width - wb.Width, frame.Height - wb.Height);
				if (contentView == null) {
					contentView = new ContentView (cFrame, this);
				} else {
					contentView.Frame = cFrame;
				}
			}
			if (Subviews?.Count == 0) {
				base.Add (contentView);
			}
			if (Border.Child != contentView) {
				Border.Child = contentView;
			}
		}

		///// <summary>
		///// Enumerates the various <see cref="View"/>s in the embedded <see cref="ContentView"/>.
		///// </summary>
		///// <returns>The enumerator.</returns>
		//public new IEnumerator GetEnumerator ()
		//{
		//	return contentView.GetEnumerator ();
		//}

		/// <inheritdoc/>
		public override void Add (View view)
		{
			contentView.Add (view);
			if (view.CanFocus) {
				CanFocus = true;
			}
			AddMenuStatusBar (view);
		}


		/// <inheritdoc/>
		public override void Remove (View view)
		{
			if (view == null) {
				return;
			}

			SetNeedsDisplay ();
			if (view == contentView) {
				base.Remove (view);
				contentView.Dispose ();
				return;
			} else {
				contentView.Remove (view);
			}

			if (contentView.InternalSubviews.Count < 1) {
				CanFocus = false;
			}
			RemoveMenuStatusBar (view);
			if (view != contentView && Focused == null) {
				FocusFirst ();
			}
		}

		/// <inheritdoc/>
		public override void RemoveAll ()
		{
			contentView.RemoveAll ();
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if (!NeedDisplay.IsEmpty || ChildNeedsDisplay || LayoutNeeded) {
				Driver.SetAttribute (GetNormalColor ());
				Clear ();
				contentView.SetNeedsDisplay ();
			}
			var savedClip = contentView.ClipToBounds ();

			// Redraw our contentView
			contentView.Redraw (!NeedDisplay.IsEmpty || ChildNeedsDisplay || LayoutNeeded ? contentView.Bounds : bounds);
			Driver.Clip = savedClip;

			ClearLayoutNeeded ();
			ClearNeedsDisplay ();

			Driver.SetAttribute (GetNormalColor ());
			Border.DrawContent (this, false);
		}

		/// <inheritdoc/>
		public override void OnCanFocusChanged ()
		{
			if (contentView != null) {
				contentView.CanFocus = CanFocus;
			}
			base.OnCanFocusChanged ();
		}

		/// <summary>
		///   The text displayed by the <see cref="Label"/>.
		/// </summary>
		public override ustring Text {
			get => contentView?.Text;
			set {
				base.Text = value;
				if (contentView != null) {
					contentView.Text = value;
				}
			}
		}

		/// <summary>
		/// Controls the text-alignment property of the label, changing it will redisplay the <see cref="Label"/>.
		/// </summary>
		/// <value>The text alignment.</value>
		public override TextAlignment TextAlignment {
			get => contentView.TextAlignment;
			set {
				base.TextAlignment = contentView.TextAlignment = value;
			}
		}
		/// <summary>
		/// Called before the <see cref="Window.Title"/> changes. Invokes the <see cref="TitleChanging"/> event, which can be cancelled.
		/// </summary>
		/// <param name="oldTitle">The <see cref="Window.Title"/> that is/has been replaced.</param>
		/// <param name="newTitle">The new <see cref="Window.Title"/> to be replaced.</param>
		/// <returns>`true` if an event handler canceled the Title change.</returns>
		public virtual bool OnTitleChanging (ustring oldTitle, ustring newTitle)
		{
			var args = new TitleEventArgs (oldTitle, newTitle);
			TitleChanging?.Invoke (this, args);
			return args.Cancel;
		}

		/// <summary>
		/// Event fired when the <see cref="Window.Title"/> is changing. Set <see cref="TitleEventArgs.Cancel"/> to 
		/// `true` to cancel the Title change.
		/// </summary>
		public event EventHandler<TitleEventArgs> TitleChanging;

		/// <summary>
		/// Called when the <see cref="Window.Title"/> has been changed. Invokes the <see cref="TitleChanged"/> event.
		/// </summary>
		/// <param name="oldTitle">The <see cref="Window.Title"/> that is/has been replaced.</param>
		/// <param name="newTitle">The new <see cref="Window.Title"/> to be replaced.</param>
		public virtual void OnTitleChanged (ustring oldTitle, ustring newTitle)
		{
			var args = new TitleEventArgs (oldTitle, newTitle);
			TitleChanged?.Invoke (this, args);
		}

		/// <summary>
		/// Event fired after the <see cref="Window.Title"/> has been changed. 
		/// </summary>
		public event EventHandler<TitleEventArgs> TitleChanged;
	}
}
