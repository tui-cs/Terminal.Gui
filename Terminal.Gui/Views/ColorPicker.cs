using System;
using NStack;

namespace Terminal.Gui {

	/// <summary>
	/// The <see cref="ColorPicker"/> <see cref="View"/> Color picker.
	/// </summary>
	public class ColorPicker : View {
		/// <summary>
		/// Number of colors on a line.
		/// </summary>
		private static readonly int colorsPerLine = 8;

		/// <summary>
		/// Number of color lines.
		/// </summary>
		private static readonly int lineCount = 2;

		/// <summary>
		/// Horizontal zoom.
		/// </summary>
		private static readonly int horizontalZoom = 4;

		/// <summary>
		/// Vertical zoom.
		/// </summary>
		private static readonly int verticalZoom = 2;

		// Cursor runes.
		private static readonly Rune [] cursorRunes = new Rune []
		{
			0x250C, 0x2500, 0x2500, 0x2510,
			0x2514, 0x2500, 0x2500, 0x2518
		};

		/// <summary>
		/// Cursor for the selected color.
		/// </summary>
		public Point Cursor {
			get {
				return new Point (selectColorIndex % colorsPerLine, selectColorIndex / colorsPerLine);
			}

			set {
				var colorIndex = value.Y * colorsPerLine + value.X;
				SelectedColor = (Color)colorIndex;
			}
		}

		/// <summary>
		/// Fired when a color is picked.
		/// </summary>
		public event EventHandler ColorChanged;

		private int selectColorIndex = (int)Color.Black;

		/// <summary>
		/// Selected color.
		/// </summary>
		public Color SelectedColor {
			get {
				return (Color)selectColorIndex;
			}

			set {
				selectColorIndex = (int)value;
				ColorChanged?.Invoke (this, EventArgs.Empty);
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ColorPicker"/>.
		/// </summary>
		public ColorPicker () : base ("Color Picker")
		{
			Initialize ();
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ColorPicker"/>.
		/// </summary>
		/// <param name="title">Title.</param>
		public ColorPicker (ustring title) : base (title)
		{
			Initialize ();
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ColorPicker"/>.
		/// </summary>
		/// <param name="point">Location point.</param>
		/// <param name="title">Title.</param>
		public ColorPicker (Point point, ustring title) : this (point.X, point.Y, title)
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ColorPicker"/>.
		/// </summary>
		/// <param name="x">X location.</param>
		/// <param name="y">Y location.</param>
		/// <param name="title">Title</param>
		public ColorPicker (int x, int y, ustring title) : base (x, y, title)
		{
			Initialize ();
		}

		private void Initialize()
		{
			CanFocus = true;
			Width = colorsPerLine * horizontalZoom;
			Height = lineCount * verticalZoom + 1;

			AddCommands ();
			AddKeyBindings ();
		}

		/// <summary>
		/// Add the commands.
		/// </summary>
		private void AddCommands ()
		{
			AddCommand (Command.Left, () => MoveLeft ());
			AddCommand (Command.Right, () => MoveRight ());
			AddCommand (Command.LineUp, () => MoveUp ());
			AddCommand (Command.LineDown, () => MoveDown ());
		}

		/// <summary>
		/// Add the KeyBindinds.
		/// </summary>
		private void AddKeyBindings ()
		{
			AddKeyBinding (Key.CursorLeft, Command.Left);
			AddKeyBinding (Key.CursorRight, Command.Right);
			AddKeyBinding (Key.CursorUp, Command.LineUp);
			AddKeyBinding (Key.CursorDown, Command.LineDown);
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			base.Redraw (bounds);

			Driver.SetAttribute (HasFocus ? ColorScheme.Focus : GetNormalColor ());
			var colorIndex = 0;

			for (var y = 0; y < (Height.Anchor (0) - 1) / verticalZoom; y++) {
				for (var x = 0; x < Width.Anchor (0) / horizontalZoom; x++) {
					var foregroundColorIndex = y == 0 ? colorIndex + colorsPerLine : colorIndex - colorsPerLine;
					Driver.SetAttribute (Driver.MakeAttribute ((Color)foregroundColorIndex, (Color)colorIndex));
					var selected = x == Cursor.X && y == Cursor.Y;
					DrawColorBox (x, y, selected);
					colorIndex++;
				}
			}
		}

		/// <summary>
		/// Draw a box for one color.
		/// </summary>
		/// <param name="x">X location.</param>
		/// <param name="y">Y location</param>
		/// <param name="selected"></param>
		private void DrawColorBox (int x, int y, bool selected)
		{
			var index = 0;

			for (var zommedY = 0; zommedY < verticalZoom; zommedY++) {
				for (var zommedX = 0; zommedX < horizontalZoom; zommedX++) {
					Move (x * horizontalZoom + zommedX, y * verticalZoom + zommedY + 1);

					if (selected) {
						var character = cursorRunes [index];
						Driver.AddRune (character);
					} else {
						Driver.AddRune (' ');
					}

					index++;
				}
			}
		}

		/// <summary>
		/// Moves the selected item index to the previous column.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveLeft ()
		{
			if (Cursor.X > 0) SelectedColor--;
			return true;
		}

		/// <summary>
		/// Moves the selected item index to the next column.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveRight ()
		{
			if (Cursor.X < colorsPerLine - 1) SelectedColor++;
			return true;
		}

		/// <summary>
		/// Moves the selected item index to the previous row.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveUp ()
		{
			if (Cursor.Y > 0) SelectedColor -= colorsPerLine;
			return true;
		}

		/// <summary>
		/// Moves the selected item index to the next row.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveDown ()
		{
			if (Cursor.Y < lineCount - 1) SelectedColor += colorsPerLine;
			return true;
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			return false;
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) || !CanFocus)
				return false;

			SetFocus ();
			Cursor = new Point (me.X / horizontalZoom, (me.Y - 1) / verticalZoom);

			return true;
		}
	}
}
