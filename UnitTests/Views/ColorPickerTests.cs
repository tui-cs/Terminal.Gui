using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.ViewTests {
	public class ColorPickerTests {
		[Fact]
		public void Constructors ()
		{
			var colorPicker = new ColorPicker ();
			Assert.Equal (Color.Black, colorPicker.SelectedColor);
			Assert.Equal (new Point (0, 0), colorPicker.Cursor);
			Assert.True (colorPicker.CanFocus);
			Assert.Equal (new Rect (0, 0, 32, 5), colorPicker.Frame);

			colorPicker = new ColorPicker (5, 10, "Title");
			Assert.Equal (Color.Black, colorPicker.SelectedColor);
			Assert.Equal (new Point (0, 0), colorPicker.Cursor);
			Assert.True (colorPicker.CanFocus);
			Assert.Equal (new Rect (5, 10, 32, 5), colorPicker.Frame);

			colorPicker = new ColorPicker (new Point (10, 15), "Title");
			Assert.Equal (Color.Black, colorPicker.SelectedColor);
			Assert.Equal (new Point (0, 0), colorPicker.Cursor);
			Assert.True (colorPicker.CanFocus);
			Assert.Equal (new Rect (10, 15, 32, 5), colorPicker.Frame);
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var colorPicker = new ColorPicker ();
			Assert.Equal (Color.Black, colorPicker.SelectedColor);

			Assert.True (colorPicker.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (Color.Blue, colorPicker.SelectedColor);

			Assert.True (colorPicker.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (Color.BrightBlue, colorPicker.SelectedColor);

			Assert.True (colorPicker.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (Color.DarkGray, colorPicker.SelectedColor);

			Assert.True (colorPicker.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (Color.Black, colorPicker.SelectedColor);

			Assert.True (colorPicker.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (Color.Black, colorPicker.SelectedColor);

			Assert.True (colorPicker.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (Color.Black, colorPicker.SelectedColor);
		}

		[Fact]
		[AutoInitShutdown]
		public void MouseEvents ()
		{
			var colorPicker = new ColorPicker ();
			Assert.Equal (Color.Black, colorPicker.SelectedColor);

			Assert.False (colorPicker.MouseEvent (new MouseEvent ()));

			Assert.True (colorPicker.MouseEvent (new MouseEvent () { Flags = MouseFlags.Button1Clicked, X = 4, Y = 0 }));
			Assert.Equal (Color.Blue, colorPicker.SelectedColor);
		}

		[Fact]
		[AutoInitShutdown]
		public void SelectedColorAndCursor ()
		{
			var colorPicker = new ColorPicker ();
			colorPicker.SelectedColor = Color.White;
			Assert.Equal (7, colorPicker.Cursor.X);
			Assert.Equal (1, colorPicker.Cursor.Y);

			colorPicker.SelectedColor = Color.Black;
			Assert.Equal (0, colorPicker.Cursor.X);
			Assert.Equal (0, colorPicker.Cursor.Y);

			colorPicker.Cursor = new Point (7, 1);
			Assert.Equal (Color.White, colorPicker.SelectedColor);

			colorPicker.Cursor = new Point (0, 0);
			Assert.Equal (Color.Black, colorPicker.SelectedColor);
		}
	}
}
