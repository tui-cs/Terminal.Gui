using System;
using System.Linq;
using System.Text.Json.Serialization;
using NStack;
using Terminal.Gui.Graphs;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui {

	/// <summary>
	/// The FrameView is a container frame that draws a frame around the contents. It is similar to
	/// a GroupBox in Windows.
	/// </summary>
	public class FrameView : View {

		//internal class FrameViewConfig : Configuration.Config<FrameViewConfig> {

		//	/// <summary>
		//	/// 
		//	/// </summary>
		//	/// 
		//	[JsonConverter (typeof (JsonStringEnumConverter))]
		//	public BorderStyle? DefaultBorderStyle { get; set; }

		//	public override void Apply ()
		//	{
		//		if (DefaultBorderStyle.HasValue) {
		//			FrameView.DefaultBorderStyle = DefaultBorderStyle.Value;
		//		}
		//	}

		//	public override void CopyUpdatedProperitesFrom (FrameViewConfig changedConfig)
		//	{
		//		if (changedConfig.DefaultBorderStyle.HasValue) {
		//			DefaultBorderStyle = changedConfig.DefaultBorderStyle;
		//		}
		//	}

		//	public override void GetHardCodedDefaults ()
		//	{
		//		DefaultBorderStyle = FrameView.DefaultBorderStyle;
		//	}
		//}

		//[Configuration.ConfigProperty]
		//internal static FrameViewConfig Config { get; set; } = new FrameViewConfig ();

		View contentView;
		ustring title;

		/// <summary>
		/// The title to be displayed for this <see cref="FrameView"/>.
		/// </summary>
		/// <value>The title.</value>
		public ustring Title {
			get => title;
			set {
				title = value;
				if (Border != null) {
					Border.Title = title;
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
			public ContentView (Rect frame) : base (frame) { }
			public ContentView () : base () { }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.FrameView"/> class using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <param name="frame">Frame.</param>
		/// <param name="title">Title.</param>
		/// <param name="views">Views.</param>
		/// <param name="border">The <see cref="Border"/>.</param>
		public FrameView (Rect frame, ustring title = null, View [] views = null, Border border = null) : base (frame)
		{
			Initialize (frame, title, views, border);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.FrameView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <param name="border">The <see cref="Border"/>.</param>
		public FrameView (ustring title, Border border = null)
		{
			Initialize (Rect.Empty, title, null, border);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.FrameView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		public FrameView () : this (title: string.Empty) { }

		/// <summary>
		/// The default <see cref="BorderStyle"/> for <see cref="FrameView"/>. The default is <see cref="BorderStyle.Single"/>.
		/// </summary>
		/// <remarks>
		/// This property can be set in a Theme to change the default <see cref="BorderStyle"/> for all <see cref="FrameView"/>s. 
		/// </remarks>
		[SerializableConfigurationProperty (Scope = typeof (ThemeScope)), JsonConverter (typeof (JsonStringEnumConverter))]
		public static BorderStyle DefaultBorderStyle { get; set; } = BorderStyle.Single;

		void Initialize (Rect frame, ustring title, View [] views = null, Border border = null)
		{
			if (title == null) title = ustring.Empty;
			this.Title = title;
			if (border == null) {
				Border = new Border () {
					BorderStyle = DefaultBorderStyle,
					Title = title
				};
			} else {
				Border = border;
				if (ustring.IsNullOrEmpty (border.Title)) {
					border.Title = title;
				}
			}
			AdjustContentView (frame, views);
		}

		void AdjustContentView (Rect frame, View [] views = null)
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
					contentView = new ContentView () {
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
					contentView = new ContentView (cFrame);
				} else {
					contentView.Frame = cFrame;
				}
			}
			if (views != null) {
				foreach (var view in views) {
					contentView.Add (view);
				}
			}
			if (Subviews?.Count == 0) {
				base.Add (contentView);
				contentView.Text = base.Text;
			}
			Border.Child = contentView;
		}

		void DrawFrame ()
		{
			DrawFrame (new Rect (0, 0, Frame.Width, Frame.Height), 0, fill: true);
		}

		/// <summary>
		/// Add the specified <see cref="View"/> to this container.
		/// </summary>
		/// <param name="view"><see cref="View"/> to add to this container</param>
		public override void Add (View view)
		{
			contentView.Add (view);
			if (view.CanFocus)
				CanFocus = true;
		}


		/// <summary>
		///   Removes a <see cref="View"/> from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public override void Remove (View view)
		{
			if (view == null)
				return;

			SetNeedsDisplay ();
			var touched = view.Frame;
			contentView.Remove (view);

			if (contentView.InternalSubviews.Count < 1)
				this.CanFocus = false;
		}

		/// <summary>
		///   Removes all <see cref="View"/>s from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public override void RemoveAll ()
		{
			contentView.RemoveAll ();
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if (!NeedDisplay.IsEmpty) {
				Driver.SetAttribute (GetNormalColor ());
				Clear ();
			}

			var savedClip = contentView.ClipToBounds ();
			contentView.Redraw (!NeedDisplay.IsEmpty ? contentView.Bounds : bounds);
			Driver.Clip = savedClip;

			ClearLayoutNeeded ();
			ClearNeedsDisplay ();

			if (!IgnoreBorderPropertyOnRedraw) {
				Driver.SetAttribute (GetNormalColor ());
				Border.DrawContent (this, false);
			} else {
				var lc = new LineCanvas ();

				if (Border?.BorderStyle != BorderStyle.None) {

					lc.AddLine (new Point (0, 0), bounds.Width - 1, Orientation.Horizontal, Border.BorderStyle);
					lc.AddLine (new Point (0, 0), bounds.Height - 1, Orientation.Vertical, Border.BorderStyle);

					lc.AddLine (new Point (bounds.Width - 1, bounds.Height - 1), -bounds.Width + 1, Orientation.Horizontal, Border.BorderStyle);
					lc.AddLine (new Point (bounds.Width - 1, bounds.Height - 1), -bounds.Height + 1, Orientation.Vertical, Border.BorderStyle);
				}

				//foreach (var subview in contentView.Subviews) {
				//	lc.AddLine (new Point (subview.Frame.X + 1, subview.Frame.Y + 1), subview.Frame.Width - 1, Orientation.Horizontal, subview.Border.BorderStyle);
				//	lc.AddLine (new Point (subview.Frame.X + 1, subview.Frame.Y + 1), subview.Frame.Height - 1, Orientation.Vertical, subview.Border.BorderStyle);

				//	lc.AddLine (new Point (subview.Frame.X + subview.Frame.Width, subview.Frame.Y + subview.Frame.Height), -subview.Frame.Width + 1, Orientation.Horizontal, subview.Border.BorderStyle);
				//	lc.AddLine (new Point (subview.Frame.X + subview.Frame.Width, subview.Frame.Y + subview.Frame.Height), -subview.Frame.Height + 1, Orientation.Vertical, subview.Border.BorderStyle);

				//}

				//Driver.SetAttribute (ColorScheme.Normal);
				//foreach (var p in lc.GenerateImage (bounds)) {
				//	this.AddRune (p.Key.X, p.Key.Y, p.Value);
				//}

				//// Redraw the lines so that focus/drag symbol renders
				//foreach (var subview in contentView.Subviews) {
				//	//	line.DrawSplitterSymbol ();
				//}


				//// Draw Titles over Border
				//foreach (var subview in contentView.Subviews) {
				//	// TODO: Use reflection to see if subview has a Title property
				//	if (subview is FrameView viewWithTite) {
				//		var rect = viewWithTite.Frame;
				//		rect.X = rect.X + 1;
				//		rect.Y = rect.Y + 2;
				//		// TODO: Do focus color correctly
				//		Driver.DrawWindowTitle (rect, viewWithTite.Title, padding.Left, padding.Top, padding.Right, padding.Bottom);
				//	}
				//}
			}
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

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			if (Subviews.Count == 0 || !Subviews.Any (subview => subview.CanFocus)) {
				Application.Driver?.SetCursorVisibility (CursorVisibility.Invisible);
			}

			return base.OnEnter (view);
		}

		/// <inheritdoc/>
		public override void OnCanFocusChanged ()
		{
			if (contentView != null) {
				contentView.CanFocus = CanFocus;
			}
			base.OnCanFocusChanged ();
		}
	}
}
