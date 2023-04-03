using NStack;
using System;
using Terminal.Gui.Graphs;
using System.Text.Json.Serialization;

namespace Terminal.Gui {
	/// <summary>
	/// Specifies the border style for a <see cref="View"/> and to be used by the <see cref="Border"/> class.
	/// </summary>
	public enum BorderStyle {
		/// <summary>
		/// No border is drawn.
		/// </summary>
		None,
		/// <summary>
		/// The border is drawn with a single line limits.
		/// </summary>
		Single,
		/// <summary>
		/// The border is drawn with a double line limits.
		/// </summary>
		Double,
		/// <summary>
		/// The border is drawn with a single line and rounded corners limits.
		/// </summary>
		Rounded
	}

	/// <summary>
	/// Describes the thickness of a frame around a rectangle. Four <see cref="int"/> values describe
	///  the <see cref="Left"/>, <see cref="Top"/>, <see cref="Right"/>, and <see cref="Bottom"/> sides
	///  of the rectangle, respectively.
	/// </summary>
	public struct Thickness {
		/// <summary>
		/// Gets or sets the width, in integers, of the left side of the bounding rectangle.
		/// </summary>
		[JsonInclude]
		public int Left;
		/// <summary>
		/// Gets or sets the width, in integers, of the upper side of the bounding rectangle.
		/// </summary>
		[JsonInclude]
		public int Top;
		/// <summary>
		/// Gets or sets the width, in integers, of the right side of the bounding rectangle.
		/// </summary>
		[JsonInclude]
		public int Right;
		/// <summary>
		/// Gets or sets the width, in integers, of the lower side of the bounding rectangle.
		/// </summary>
		[JsonInclude]
		public int Bottom;

		/// <summary>
		/// Initializes a new instance of the <see cref="Thickness"/> structure that has the
		///  specified uniform length on each side.
		/// </summary>
		/// <param name="length"></param>
		public Thickness (int length)
		{
			if (length < 0) {
				throw new ArgumentException ("Invalid value for this property.");
			}

			Left = Top = Right = Bottom = length;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Thickness"/> structure that has specific
		///  lengths (supplied as a <see cref="int"/>) applied to each side of the rectangle.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="right"></param>
		/// <param name="bottom"></param>
		public Thickness (int left, int top, int right, int bottom)
		{
			if (left < 0 || top < 0 || right < 0 || bottom < 0) {
				throw new ArgumentException ("Invalid value for this property.");
			}

			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}

		/// <summary>Returns the fully qualified type name of this instance.</summary>
		/// <returns>The fully qualified type name.</returns>
		public override string ToString ()
		{
			return $"(Left={Left},Top={Top},Right={Right},Bottom={Bottom})";
		}
	}

	/// <summary>
	/// Draws a border, background, or both around another element.
	/// </summary>
	public class Border {
		private int marginFrame => DrawMarginFrame ? 1 : 0;

		/// <summary>
		/// A sealed <see cref="Toplevel"/> derived class to implement <see cref="Border"/> feature.
		/// This is only a wrapper to get borders on a toplevel and is recommended using another
		/// derived, like <see cref="Window"/> where is possible to have borders with or without
		/// border line or spacing around.
		/// </summary>
		public sealed class ToplevelContainer : Toplevel {
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
					if (Border.Child != null && (Border.Child.Width is Dim || Border.Child.Height is Dim)) {
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
				if (Border.Child != null && (Border.Child.Width is Dim || Border.Child.Height is Dim)) {
					frame = Rect.Empty;
				} else {
					frame = Frame;
				}
				AdjustContentView (frame);
			}

			/// <summary>
			/// Initializes with default null values.
			/// </summary>
			public ToplevelContainer () : this (null, string.Empty) { }

			/// <summary>
			/// Initializes a <see cref="ToplevelContainer"/> with a <see cref="LayoutStyle.Computed"/>
			/// </summary>
			/// <param name="border">The border.</param>
			/// <param name="title">The title.</param>
			public ToplevelContainer (Border border, string title = null)
			{
				Initialize (Rect.Empty, border, title ?? string.Empty);
			}

			/// <summary>
			/// Initializes a <see cref="ToplevelContainer"/> with a <see cref="LayoutStyle.Absolute"/>
			/// </summary>
			/// <param name="frame">The frame.</param>
			/// <param name="border">The border.</param>
			/// <param name="title">The title.</param>
			public ToplevelContainer (Rect frame, Border border, string title = null) : base (frame)
			{
				Initialize (frame, border, title ?? string.Empty);
			}

			private void Initialize (Rect frame, Border border, string title)
			{
				ColorScheme = Colors.TopLevel;
				if (border == null) {
					Border = new Border () {
						BorderStyle = BorderStyle.Single,
						BorderBrush = ColorScheme.Normal.Background,
						Title = (ustring)title
					};
				} else {
					Border = border;
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
					if (Border.Child == null) {
						Border.Child = new ChildContentView (this) {
							X = wp.X,
							Y = wp.Y,
							Width = Dim.Fill (wb.Width),
							Height = Dim.Fill (wb.Height)
						};
					} else {
						Border.Child.X = wp.X;
						Border.Child.Y = wp.Y;
						Border.Child.Width = Dim.Fill (wb.Width);
						Border.Child.Height = Dim.Fill (wb.Height);
					}
				} else {
					wb.Width = (2 * borderLength) + sumPadding.Right + sumPadding.Left;
					wb.Height = (2 * borderLength) + sumPadding.Bottom + sumPadding.Top;
					var cFrame = new Rect (borderLength + sumPadding.Left, borderLength + sumPadding.Top, frame.Width - wb.Width, frame.Height - wb.Height);
					if (Border.Child == null) {
						Border.Child = new ChildContentView (cFrame, this);
					} else {
						Border.Child.Frame = cFrame;
					}
				}
				if (Subviews?.Count == 0)
					base.Add (Border.Child);
				Border.ChildContainer = this;
			}

			/// <inheritdoc/>
			public override void Add (View view)
			{
				Border.Child.Add (view);
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
				var touched = view.Frame;
				Border.Child.Remove (view);

				if (Border.Child.InternalSubviews.Count < 1) {
					CanFocus = false;
				}
				RemoveMenuStatusBar (view);
			}

			/// <inheritdoc/>
			public override void RemoveAll ()
			{
				Border.Child.RemoveAll ();
			}

			/// <inheritdoc/>
			public override void Redraw (Rect bounds)
			{
				if (!NeedDisplay.IsEmpty) {
					Driver.SetAttribute (GetNormalColor ());
					Clear ();
				}
				var savedClip = Border.Child.ClipToBounds ();
				Border.Child.Redraw (Border.Child.Bounds);
				Driver.Clip = savedClip;

				ClearLayoutNeeded ();
				ClearNeedsDisplay ();

				Driver.SetAttribute (GetNormalColor ());
				Border.DrawContent (this, false);
				if (HasFocus)
					Driver.SetAttribute (ColorScheme.HotNormal);
				if (Border.DrawMarginFrame) {
					if (!ustring.IsNullOrEmpty (Border.Title))
						Border.DrawTitle (this);
					else
						Border.DrawTitle (this, Frame);
				}
				Driver.SetAttribute (GetNormalColor ());

				// Checks if there are any SuperView view which intersect with this window.
				if (SuperView != null) {
					SuperView.SetNeedsLayout ();
					SuperView.SetNeedsDisplay ();
				}
			}

			/// <inheritdoc/>
			public override void OnCanFocusChanged ()
			{
				if (Border.Child != null) {
					Border.Child.CanFocus = CanFocus;
				}
				base.OnCanFocusChanged ();
			}
		}

		private class ChildContentView : View {
			View instance;

			public ChildContentView (Rect frame, View instance) : base (frame)
			{
				this.instance = instance;
			}
			public ChildContentView (View instance)
			{
				this.instance = instance;
			}

			public override bool MouseEvent (MouseEvent mouseEvent)
			{
				return instance.MouseEvent (mouseEvent);
			}
		}

		/// <summary>
		/// Invoked when any property of Border changes (except <see cref="Child"/>).
		/// </summary>
		public event EventHandler BorderChanged;

		private BorderStyle borderStyle;
		private bool drawMarginFrame;
		private Thickness borderThickness;
		private Color borderBrush;
		private Color background;
		private Thickness padding;
		private bool effect3D;
		private Point effect3DOffset = new Point (1, 1);
		private Attribute? effect3DBrush;
		private ustring title = ustring.Empty;
		private View child;

		/// <summary>
		/// Specifies the <see cref="Gui.BorderStyle"/> for a view.
		/// </summary>
		[JsonInclude, JsonConverter (typeof (JsonStringEnumConverter))]
		public BorderStyle BorderStyle {
			get => borderStyle;
			set {
				if (value != BorderStyle.None && !drawMarginFrame) {
					// Ensures drawn the border lines.
					drawMarginFrame = true;
				}
				borderStyle = value;
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// Gets or sets if a margin frame is drawn around the <see cref="Child"/> regardless the <see cref="BorderStyle"/>
		/// </summary>
		[JsonInclude]
		public bool DrawMarginFrame {
			get => drawMarginFrame;
			set {
				if (borderStyle != BorderStyle.None
					&& (!value || !drawMarginFrame)) {
					// Ensures drawn the border lines.
					drawMarginFrame = true;
				} else {
					drawMarginFrame = value;
				}
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// Gets or sets the relative <see cref="Thickness"/> of a <see cref="Border"/>.
		/// </summary>
		[JsonInclude]
		public Thickness BorderThickness {
			get => borderThickness;
			set {
				borderThickness = value;
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="Color"/> that draws the outer border color.
		/// </summary>
		[JsonInclude, JsonConverter (typeof (Configuration.ColorJsonConverter))]
		public Color BorderBrush {
			get => borderBrush;
			set {
				borderBrush = value;
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="Color"/> that fills the area between the bounds of a <see cref="Border"/>.
		/// </summary>
		[JsonInclude, JsonConverter (typeof (Configuration.ColorJsonConverter))]
		public Color Background {
			get => background;
			set {
				background = value;
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// Gets or sets a <see cref="Thickness"/> value that describes the amount of space between a
		///  <see cref="Border"/> and its child element.
		/// </summary>
		[JsonInclude]
		public Thickness Padding {
			get => padding;
			set {
				padding = value;
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// Gets the rendered width of this element.
		/// </summary>
		[JsonIgnore]
		public int ActualWidth {
			get {
				var driver = Application.Driver;
				if (Parent?.Border == null) {
					return Math.Min (Child?.Frame.Width + (2 * marginFrame) + Padding.Right
						+ BorderThickness.Right + Padding.Left + BorderThickness.Left ?? 0, driver.Cols);
				}
				return Math.Min (Parent.Frame.Width, driver.Cols);
			}
		}
		/// <summary>
		/// Gets the rendered height of this element.
		/// </summary>
		[JsonIgnore]
		public int ActualHeight {
			get {
				var driver = Application.Driver;
				if (Parent?.Border == null) {
					return Math.Min (Child?.Frame.Height + (2 * marginFrame) + Padding.Bottom
						+ BorderThickness.Bottom + Padding.Top + BorderThickness.Top ?? 0, driver.Rows);
				}
				return Math.Min (Parent.Frame.Height, driver.Rows);
			}
		}

		/// <summary>
		/// Gets or sets the single child element of a <see cref="View"/>.
		/// </summary>
		[JsonIgnore]
		public View Child {
			get => child;
			set {
				child = value;
				if (child != null && Parent != null) {
					Parent.Initialized += Parent_Initialized;
					Parent.Removed += Parent_Removed;
				}
			}
		}

		private void Parent_Removed (object sender, SuperViewChangedEventArgs e)
		{
			BorderBrush = default;
			Background = default;
			child.Removed -= Parent_Removed;
		}

		private void Parent_Initialized (object s, EventArgs e)
		{
			SetMarginFrameTitleBrush ();
			child.Initialized -= Parent_Initialized;
		}

		private void SetMarginFrameTitleBrush ()
		{
			if (child != null) {
				var view = Parent?.Border != null ? Parent : child;
				if (view.ColorScheme != null) {
					if (borderBrush == default) {
						BorderBrush = view.GetNormalColor ().Foreground;
					}
					if (background == default) {
						Background = view.GetNormalColor ().Background;
					}
					return;
				}
			}
			BorderBrush = default;
			Background = default;
		}

		/// <summary>
		/// Gets the parent <see cref="Child"/> parent if any.
		/// </summary>
		[JsonIgnore]
		public View Parent { get => Child?.SuperView; }

		/// <summary>
		/// Gets or private sets by the <see cref="ToplevelContainer"/>
		/// </summary>
		[JsonIgnore]
		public ToplevelContainer ChildContainer { get; private set; }

		/// <summary>
		/// Gets or sets the 3D effect around the <see cref="Border"/>.
		/// </summary>
		[JsonInclude]
		public bool Effect3D {
			get => effect3D;
			set {
				effect3D = value;
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// Get or sets the offset start position for the <see cref="Effect3D"/>
		/// </summary>
		[JsonInclude]
		public Point Effect3DOffset {
			get => effect3DOffset;
			set {
				effect3DOffset = value;
				OnBorderChanged ();
			}
		}
		/// <summary>
		/// Gets or sets the color for the <see cref="Border"/>
		/// </summary>
		[JsonInclude, JsonConverter (typeof (Configuration.AttributeJsonConverter))]
		public Attribute? Effect3DBrush {
			get {
				if (effect3DBrush == null && effect3D) {
					return effect3DBrush = new Attribute (Color.Gray, Color.DarkGray);
				} else {
					return effect3DBrush;
				}
			}

			set {
				effect3DBrush = value;
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// The title to be displayed for this view.
		/// </summary>
		[JsonIgnore]
		public ustring Title {
			get => title;
			set {
				title = value;
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// Calculate the sum of the <see cref="Padding"/> and the <see cref="BorderThickness"/>
		/// </summary>
		/// <returns>The total of the <see cref="Border"/> <see cref="Thickness"/></returns>
		public Thickness GetSumThickness ()
		{
			return new Thickness () {
				Left = Padding.Left + BorderThickness.Left,
				Top = Padding.Top + BorderThickness.Top,
				Right = Padding.Right + BorderThickness.Right,
				Bottom = Padding.Bottom + BorderThickness.Bottom
			};
		}

		/// <summary>
		/// Drawn the <see cref="BorderThickness"/> more the <see cref="Padding"/>
		///  more the <see cref="Border.BorderStyle"/> and the <see cref="Effect3D"/>.
		/// </summary>
		/// <param name="view">The view to draw.</param>
		/// <param name="fill">If it will clear or not the content area.</param>
		public void DrawContent (View view = null, bool fill = true)
		{
			if (Child == null) {
				Child = view;
			}
			if (Parent?.Border != null) {
				DrawParentBorder (Parent.ViewToScreen (Parent.Bounds), fill);
			} else {
				DrawChildBorder (Child.ViewToScreen (Child.Bounds), fill);
			}
		}

		/// <summary>
		/// Same as <see cref="DrawContent"/> but drawing full frames for all borders.
		/// </summary>
		public void DrawFullContent ()
		{
			var borderThickness = BorderThickness;
			var padding = Padding;
			var marginFrame = DrawMarginFrame ? 1 : 0;
			var driver = Application.Driver;
			Rect scrRect;
			if (Parent?.Border != null) {
				scrRect = Parent.ViewToScreen (Parent.Bounds);
			} else {
				scrRect = Child.ViewToScreen (Child.Bounds);
			}
			Rect borderRect;
			if (Parent?.Border != null) {
				borderRect = scrRect;
			} else {
				borderRect = new Rect () {
					X = scrRect.X - marginFrame - padding.Left - borderThickness.Left,
					Y = scrRect.Y - marginFrame - padding.Top - borderThickness.Top,
					Width = ActualWidth,
					Height = ActualHeight
				};
			}
			var savedAttribute = driver.GetAttribute ();

			// Draw 3D effects
			if (Effect3D) {
				driver.SetAttribute ((Attribute)Effect3DBrush);

				var effectBorder = new Rect () {
					X = borderRect.X + Effect3DOffset.X,
					Y = borderRect.Y + Effect3DOffset.Y,
					Width = ActualWidth,
					Height = ActualHeight
				};
				//Child.Clear (effectBorder);
				for (int r = effectBorder.Y; r < Math.Min (effectBorder.Bottom, driver.Rows); r++) {
					for (int c = effectBorder.X; c < Math.Min (effectBorder.Right, driver.Cols); c++) {

						AddRuneAt (driver, c, r, (Rune)driver.Contents [r, c, 0]);
					}
				}
			}

			// Draw border thickness
			driver.SetAttribute (new Attribute (BorderBrush));
			Child.Clear (borderRect);

			borderRect = new Rect () {
				X = borderRect.X + borderThickness.Left,
				Y = borderRect.Y + borderThickness.Top,
				Width = Math.Max (borderRect.Width - borderThickness.Right - borderThickness.Left, 0),
				Height = Math.Max (borderRect.Height - borderThickness.Bottom - borderThickness.Top, 0)
			};
			if (borderRect != scrRect) {
				// Draw padding
				driver.SetAttribute (new Attribute (Background));
				Child.Clear (borderRect);
			}

			driver.SetAttribute (new Attribute (BorderBrush, Background));

			// Draw margin frame
			if (DrawMarginFrame) {
				if (Parent?.Border != null) {
					var sumPadding = GetSumThickness ();
					borderRect = new Rect () {
						X = scrRect.X + sumPadding.Left,
						Y = scrRect.Y + sumPadding.Top,
						Width = Math.Max (scrRect.Width - sumPadding.Right - sumPadding.Left, 0),
						Height = Math.Max (scrRect.Height - sumPadding.Bottom - sumPadding.Top, 0)
					};
				} else {
					borderRect = new Rect () {
						X = borderRect.X + padding.Left,
						Y = borderRect.Y + padding.Top,
						Width = Math.Max (borderRect.Width - padding.Right - padding.Left, 0),
						Height = Math.Max (borderRect.Height - padding.Bottom - padding.Top, 0)
					};
				}
				if (borderRect.Width > 0 && borderRect.Height > 0) {
					driver.DrawWindowFrame (borderRect, 1, 1, 1, 1, BorderStyle != BorderStyle.None, fill: true, this);
				}
			}
			driver.SetAttribute (savedAttribute);
		}

		private void DrawChildBorder (Rect frame, bool fill = true)
		{
			var drawMarginFrame = DrawMarginFrame ? 1 : 0;
			var sumThickness = GetSumThickness ();
			var padding = Padding;
			var effect3DOffset = Effect3DOffset;
			var driver = Application.Driver;

			var savedAttribute = driver.GetAttribute ();

			driver.SetAttribute (new Attribute (BorderBrush));

			// Draw the upper BorderThickness
			for (int r = frame.Y - drawMarginFrame - sumThickness.Top;
				r < frame.Y - drawMarginFrame - padding.Top; r++) {

				if (r < 0) {
					continue;
				}
				for (int c = frame.X - drawMarginFrame - sumThickness.Left;
					c < Math.Min (frame.Right + drawMarginFrame + sumThickness.Right, driver.Cols); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			// Draw the left BorderThickness
			for (int r = frame.Y - drawMarginFrame - padding.Top;
				r < Math.Min (frame.Bottom + drawMarginFrame + padding.Bottom, driver.Rows); r++) {

				if (r < 0) {
					continue;
				}
				for (int c = frame.X - drawMarginFrame - sumThickness.Left;
					c < frame.X - drawMarginFrame - padding.Left; c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			// Draw the right BorderThickness
			for (int r = frame.Y - drawMarginFrame - padding.Top;
				r < Math.Min (frame.Bottom + drawMarginFrame + padding.Bottom, driver.Rows); r++) {

				if (r < 0) {
					continue;
				}
				for (int c = frame.Right + drawMarginFrame + padding.Right;
					c < Math.Min (frame.Right + drawMarginFrame + sumThickness.Right, driver.Cols); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			// Draw the lower BorderThickness
			for (int r = frame.Bottom + drawMarginFrame + padding.Bottom;
				r < Math.Min (frame.Bottom + drawMarginFrame + sumThickness.Bottom, driver.Rows); r++) {
				for (int c = frame.X - drawMarginFrame - sumThickness.Left;
					c < Math.Min (frame.Right + drawMarginFrame + sumThickness.Right, driver.Cols); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			driver.SetAttribute (new Attribute (Background));

			// Draw the upper Padding
			for (int r = frame.Y - drawMarginFrame - padding.Top;
				r < frame.Y - drawMarginFrame; r++) {

				if (r < 0) {
					continue;
				}
				for (int c = frame.X - drawMarginFrame - padding.Left;
					c < Math.Min (frame.Right + drawMarginFrame + padding.Right, driver.Cols); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			// Draw the left Padding
			for (int r = frame.Y - drawMarginFrame;
				r < Math.Min (frame.Bottom + drawMarginFrame, driver.Rows); r++) {
				for (int c = frame.X - drawMarginFrame - padding.Left;
					c < frame.X - drawMarginFrame; c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			// Draw the right Padding
			for (int r = frame.Y - drawMarginFrame;
				r < Math.Min (frame.Bottom + drawMarginFrame, driver.Rows); r++) {
				for (int c = frame.Right + drawMarginFrame;
					c < Math.Min (frame.Right + drawMarginFrame + padding.Right, driver.Cols); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			// Draw the lower Padding
			for (int r = frame.Bottom + drawMarginFrame;
				r < Math.Min (frame.Bottom + drawMarginFrame + padding.Bottom, driver.Rows); r++) {
				for (int c = frame.X - drawMarginFrame - padding.Left;
					c < Math.Min (frame.Right + drawMarginFrame + padding.Right, driver.Cols); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			driver.SetAttribute (new Attribute (BorderBrush, Background));

			// Draw the MarginFrame
			if (DrawMarginFrame) {

				var rect = new Rect () {
					X = frame.X - drawMarginFrame,
					Y = frame.Y - drawMarginFrame,
					Width = frame.Width + (2 * drawMarginFrame),
					Height = frame.Height + (2 * drawMarginFrame)
				};
				if (rect.Width > 0 && rect.Height > 0) {
					driver.DrawWindowFrame (rect, 1, 1, 1, 1, BorderStyle != BorderStyle.None, fill, this);
					DrawTitle (Child);
				}

				//var rect = Child.ViewToScreen (new Rect (-1, -1, Child.Frame.Width + 2, Child.Frame.Height + 2));
				//if (rect.Width > 0 && rect.Height > 0) {

				//	var lc = new LineCanvas ();

				//	lc.AddLine (rect.Location, rect.Width-1, Orientation.Horizontal, BorderStyle);
				//	lc.AddLine (rect.Location, rect.Height-1, Orientation.Vertical, BorderStyle);

				//	lc.AddLine (new Point (rect.X, rect.Y + rect.Height-1), rect.Width, Orientation.Horizontal, BorderStyle);
				//	lc.AddLine (new Point (rect.X + rect.Width-1, rect.Y), rect.Height, Orientation.Vertical, BorderStyle);

				//	//driver.SetAttribute (new Attribute(Color.Red, Color.BrightYellow));
				//	foreach (var p in lc.GenerateImage (rect)) {
				//		AddRuneAt (driver, p.Key.X, p.Key.Y, p.Value);
				//	}
				//	DrawTitle (Child);
				//}
			}

			if (Effect3D) {
				driver.SetAttribute ((Attribute)Effect3DBrush);

				// Draw the upper Effect3D
				for (int r = frame.Y - drawMarginFrame - sumThickness.Top + effect3DOffset.Y;
					r >= 0 && r < frame.Y - drawMarginFrame - sumThickness.Top; r++) {
					for (int c = frame.X - drawMarginFrame - sumThickness.Left + effect3DOffset.X;
						c >= 0 && c < Math.Min (frame.Right + drawMarginFrame + sumThickness.Right + effect3DOffset.X, driver.Cols); c++) {

						AddRuneAt (driver, c, r, (Rune)driver.Contents [r, c, 0]);
					}
				}

				// Draw the left Effect3D
				for (int r = frame.Y - drawMarginFrame - sumThickness.Top + effect3DOffset.Y;
					r >= 0 && r < Math.Min (frame.Bottom + drawMarginFrame + sumThickness.Bottom + effect3DOffset.Y, driver.Rows); r++) {
					for (int c = frame.X - drawMarginFrame - sumThickness.Left + effect3DOffset.X;
						c >= 0 && c < frame.X - drawMarginFrame - sumThickness.Left; c++) {

						AddRuneAt (driver, c, r, (Rune)driver.Contents [r, c, 0]);
					}
				}

				// Draw the right Effect3D
				for (int r = frame.Y - drawMarginFrame - sumThickness.Top + effect3DOffset.Y;
					r >= 0 && r < Math.Min (frame.Bottom + drawMarginFrame + sumThickness.Bottom + effect3DOffset.Y, driver.Rows); r++) {
					for (int c = frame.Right + drawMarginFrame + sumThickness.Right;
						c >= 0 && c < Math.Min (frame.Right + drawMarginFrame + sumThickness.Right + effect3DOffset.X, driver.Cols); c++) {

						AddRuneAt (driver, c, r, (Rune)driver.Contents [r, c, 0]);
					}
				}

				// Draw the lower Effect3D
				for (int r = frame.Bottom + drawMarginFrame + sumThickness.Bottom;
					r >= 0 && r < Math.Min (frame.Bottom + drawMarginFrame + sumThickness.Bottom + effect3DOffset.Y, driver.Rows); r++) {
					for (int c = frame.X - drawMarginFrame - sumThickness.Left + effect3DOffset.X;
						c >= 0 && c < Math.Min (frame.Right + drawMarginFrame + sumThickness.Right + effect3DOffset.X, driver.Cols); c++) {

						AddRuneAt (driver, c, r, (Rune)driver.Contents [r, c, 0]);
					}
				}
			}
			driver.SetAttribute (savedAttribute);
		}

		private void DrawParentBorder (Rect frame, bool fill = true)
		{
			var sumThickness = GetSumThickness ();
			var borderThickness = BorderThickness;
			var effect3DOffset = Effect3DOffset;
			var driver = Application.Driver;

			var savedAttribute = driver.GetAttribute ();

			driver.SetAttribute (new Attribute (BorderBrush));

			// Draw the upper BorderThickness
			for (int r = frame.Y;
				r < Math.Min (frame.Y + borderThickness.Top, frame.Bottom); r++) {
				if (r < 0) {
					continue;
				}
				for (int c = frame.X;
					c < Math.Min (frame.Right, driver.Cols); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			// Draw the left BorderThickness
			for (int r = Math.Min (frame.Y + borderThickness.Top, frame.Bottom);
				r < Math.Min (frame.Bottom - borderThickness.Bottom, driver.Rows); r++) {

				if (r < 0) {
					continue;
				}
				for (int c = frame.X;
					c < Math.Min (frame.X + borderThickness.Left, frame.Right); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			// Draw the right BorderThickness
			for (int r = Math.Min (frame.Y + borderThickness.Top, frame.Bottom);
				r < Math.Min (frame.Bottom - borderThickness.Bottom, driver.Rows); r++) {

				if (r < 0) {
					continue;
				}
				for (int c = Math.Max (frame.Right - borderThickness.Right, frame.X);
					c < Math.Min (frame.Right, driver.Cols); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			// Draw the lower BorderThickness
			for (int r = Math.Max (frame.Bottom - borderThickness.Bottom, frame.Y);
				r < Math.Min (frame.Bottom, driver.Rows); r++) {
				for (int c = frame.X;
					c < Math.Min (frame.Right, driver.Cols); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			driver.SetAttribute (new Attribute (Background));

			// Draw the upper Padding
			for (int r = frame.Y + borderThickness.Top;
				r < Math.Min (frame.Y + sumThickness.Top, frame.Bottom - borderThickness.Bottom); r++) {

				if (r < 0) {
					continue;
				}
				for (int c = frame.X + borderThickness.Left;
					c < Math.Min (frame.Right - borderThickness.Right, driver.Cols); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			// Draw the left Padding
			for (int r = frame.Y + sumThickness.Top;
				r < Math.Min (frame.Bottom - sumThickness.Bottom, driver.Rows); r++) {

				if (r < 0) {
					continue;
				}
				for (int c = frame.X + borderThickness.Left;
					c < Math.Min (frame.X + sumThickness.Left, frame.Right - borderThickness.Right); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			// Draw the right Padding
			for (int r = frame.Y + sumThickness.Top;
				r < Math.Min (frame.Bottom - sumThickness.Bottom, driver.Rows); r++) {

				if (r < 0) {
					continue;
				}
				for (int c = Math.Max (frame.Right - sumThickness.Right, frame.X + sumThickness.Left);
					c < Math.Max (frame.Right - borderThickness.Right, frame.X + sumThickness.Left); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			// Draw the lower Padding
			for (int r = Math.Max (frame.Bottom - sumThickness.Bottom, frame.Y + borderThickness.Top);
				r < Math.Min (frame.Bottom - borderThickness.Bottom, driver.Rows); r++) {
				for (int c = frame.X + borderThickness.Left;
					c < Math.Min (frame.Right - borderThickness.Right, driver.Cols); c++) {

					AddRuneAt (driver, c, r, ' ');
				}
			}

			driver.SetAttribute (new Attribute (BorderBrush, Background));

			// Draw the MarginFrame
			if (DrawMarginFrame) {
				var rect = new Rect () {
					X = frame.X + sumThickness.Left,
					Y = frame.Y + sumThickness.Top,
					Width = Math.Max (frame.Width - sumThickness.Right - sumThickness.Left, 0),
					Height = Math.Max (frame.Height - sumThickness.Bottom - sumThickness.Top, 0)
				};
				if (rect.Width > 0 && rect.Height > 0) {
					driver.DrawWindowFrame (rect, 1, 1, 1, 1, BorderStyle != BorderStyle.None, fill, this);
					DrawTitle (Parent);
				}
			}

			if (Effect3D) {
				driver.SetAttribute ((Attribute)Effect3DBrush);

				// Draw the upper Effect3D
				for (int r = Math.Max (frame.Y + effect3DOffset.Y, 0);
					r < frame.Y; r++) {
					for (int c = Math.Max (frame.X + effect3DOffset.X, 0);
						c < Math.Min (frame.Right + effect3DOffset.X, driver.Cols); c++) {

						AddRuneAt (driver, c, r, (Rune)driver.Contents [r, c, 0]);
					}
				}

				// Draw the left Effect3D
				for (int r = Math.Max (frame.Y + effect3DOffset.Y, 0);
					r < Math.Min (frame.Bottom + effect3DOffset.Y, driver.Rows); r++) {
					for (int c = Math.Max (frame.X + effect3DOffset.X, 0);
						c < frame.X; c++) {

						AddRuneAt (driver, c, r, (Rune)driver.Contents [r, c, 0]);
					}
				}

				// Draw the right Effect3D
				for (int r = Math.Max (frame.Y + effect3DOffset.Y, 0);
					r < Math.Min (frame.Bottom + effect3DOffset.Y, driver.Rows); r++) {
					for (int c = frame.Right;
						c < Math.Min (frame.Right + effect3DOffset.X, driver.Cols); c++) {

						AddRuneAt (driver, c, r, (Rune)driver.Contents [r, c, 0]);
					}
				}

				// Draw the lower Effect3D
				for (int r = frame.Bottom;
					r < Math.Min (frame.Bottom + effect3DOffset.Y, driver.Rows); r++) {
					for (int c = Math.Max (frame.X + effect3DOffset.X, 0);
						c < Math.Min (frame.Right + effect3DOffset.X, driver.Cols); c++) {

						AddRuneAt (driver, c, r, (Rune)driver.Contents [r, c, 0]);
					}
				}
			}
			driver.SetAttribute (savedAttribute);
		}

		private void AddRuneAt (ConsoleDriver driver, int col, int row, Rune ch)
		{
			if (col < driver.Cols && row < driver.Rows && col > 0 && driver.Contents [row, col, 2] == 0
				&& Rune.ColumnWidth ((char)driver.Contents [row, col - 1, 0]) > 1) {

				driver.Contents [row, col, 1] = driver.GetAttribute ();
				return;
			}
			driver.Move (col, row);
			driver.AddRune (ch);
		}

		/// <summary>
		/// Draws the view <see cref="Title"/> to the screen.
		/// </summary>
		/// <param name="view">The view.</param>
		public void DrawTitle (View view)
		{
			var driver = Application.Driver;
			if (DrawMarginFrame) {
				driver.SetAttribute (new Attribute (BorderBrush, Background));
				if (view.HasFocus) {
					driver.SetAttribute (new Attribute (Child.ColorScheme.HotNormal.Foreground, Background));
				}
				var padding = view.Border.GetSumThickness ();
				Rect scrRect;
				if (view == Child) {
					scrRect = view.ViewToScreen (new Rect (0, 0, view.Frame.Width + 2, view.Frame.Height + 2));
					scrRect = new Rect (scrRect.X - 1, scrRect.Y - 1, scrRect.Width, scrRect.Height);
					driver.DrawWindowTitle (scrRect, Title, 0, 0, 0, 0);
				} else {
					scrRect = view.ViewToScreen (new Rect (0, 0, view.Frame.Width, view.Frame.Height));
					driver.DrawWindowTitle (scrRect, Parent.Border.Title,
						padding.Left, padding.Top, padding.Right, padding.Bottom);
				}
			}
			driver.SetAttribute (Child.GetNormalColor ());
		}

		/// <summary>
		/// Draws the <see cref="View.Text"/> to the screen.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="rect">The frame.</param>
		public void DrawTitle (View view, Rect rect)
		{
			var driver = Application.Driver;
			if (DrawMarginFrame) {
				driver.SetAttribute (new Attribute (BorderBrush, Background));
				if (view.HasFocus) {
					driver.SetAttribute (new Attribute (view.ColorScheme.HotNormal.Foreground, Background));
				}
				var padding = Parent.Border.GetSumThickness ();
				var scrRect = Parent.ViewToScreen (new Rect (0, 0, rect.Width, rect.Height));
				driver.DrawWindowTitle (scrRect, view.Text,
					padding.Left, padding.Top, padding.Right, padding.Bottom);
			}
			driver.SetAttribute (view.GetNormalColor ());
		}

		/// <summary>
		/// Invoke the <see cref="BorderChanged"/> event.
		/// </summary>
		public virtual void OnBorderChanged ()
		{
			BorderChanged?.Invoke (this, new EventArgs());
		}
	}
}